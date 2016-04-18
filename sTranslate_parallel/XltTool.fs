namespace sTranslate_parallel

module XltTool =
    open System
    open System.Data
    open System.Data.Linq
    open FSharp.Data.TypeProviders
    open Microsoft.FSharp.Linq
    open FSharp.Configuration
    open XltEnums

    // Get typed access to App.config to fetch the connection string later
    type Settings = AppSettings<"App.config">

    // SQL Type Provider. Give it a dummy connection string for now to satisfy the compiler.
    type dbSchema = SqlDataConnection<ConnectionStringName = "dbConnectionString">

    // A search to the database
    type Search = {
        Criteria : string
        FromText : string
        Property : string
        Context : string
        ToLanguageCode : string
    }

    // Record type so that we can cache the database to memory and not have the data context go out of scope
    type Translation = {
        Id : int
        FromText : string
        ToText : string
        Context : string
        Property : string
        Criteria : string
        FromLang : string
        ToLang : string
    }

    // Copies the contents of a database row into a record type
    let toTranslation (xlt : dbSchema.ServiceTypes.Translation) = {
        Id = xlt.Id
        FromText = xlt.FromText
        ToText = xlt.ToText
        Context = xlt.Context
        Property = xlt.Property
        Criteria = xlt.Criteria
        FromLang = xlt.FromLang
        ToLang = xlt.ToLang
    }

    // Database only supports translating from english for now
    let FromLanguageCode = "en"

    // The cached version of the database
    let mutable _translateColl : List<Translation> = []

    // Copies the database to memory
    let GetTranslations =
        use db = dbSchema.GetDataContext(Settings.ConnectionStrings.DbConnectionString)
        query {
            for row in db.Translation do 
                select row
        } |> Seq.toList |> List.map toTranslation 
    
    let GetCachedCollection =
        if _translateColl = [] then
            _translateColl <- GetTranslations
        _translateColl

    let FindTranslation (criteria : string) (fromText : string) (property : string) (context : string) toLanguageCode =
        // If fromtext does not contain a word, return an empty string
        if fromText.Trim() = "" then
            ""
        else
            let criteriaOption = ToCriteria criteria
            let propertyOption = ToPropertyType property
            // If criteria or property was not found in enums, return the original string
            if criteriaOption = None || propertyOption = None then
                fromText
            else
                // If toLanguageCode is not valid, sets it to default "no"
                let toLang =
                    match toLanguageCode with
                    | null | "" -> "no"
                    | _ -> toLanguageCode

                // Get the database 
                let collection = GetCachedCollection
                
                // Search for a valid translation
                let result =
                    collection
                    |> Seq.filter (fun row -> row.Criteria.ToLower() = criteriaOption.Value.ToLower())
                    |> Seq.filter (fun row -> row.FromLang = FromLanguageCode)
                    |> Seq.filter (fun row -> row.FromText = fromText)
                    |> Seq.filter (fun row -> row.Property.ToLower() = propertyOption.Value.ToLower())
                    |> Seq.filter (fun row -> row.Context.ToLower() = context.ToLower())
                    |> Seq.filter (fun row -> row.ToLang = toLang)
                    |> checkHead

                match result with
                    | Some x -> x.ToText
                    | None -> fromText
    
    let FindTranslationAsync (criteria : string) (fromText : string) (property : string) (context : string) toLanguageCode = 
        async { return FindTranslation (criteria : string) (fromText : string) (property : string) (context : string) toLanguageCode }

    // Takes a list of database searches and returns a list of results, with the results in the same order.
    let ToTextBatch (searchList : Search list) =
        searchList
        |> List.map (fun s -> FindTranslationAsync s.Criteria s.FromText s.Property s.Context s.ToLanguageCode)
        |> Async.Parallel
        |> Async.RunSynchronously