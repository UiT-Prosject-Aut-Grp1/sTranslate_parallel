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

    // Record type containing a search to the database
    type Search = {
        Criteria : string
        FromText : string
        Property : string
        Context : string
        ToLang : string
    }

    // Record type so that we can cache the database to memory and not have the data context go out of scope
    type Translation = {
        FromText : string
        ToText : string
        Context : string
        Property : string
        Criteria : string
        FromLang : string
        ToLang : string
    }

    let fromLang = "en"
    
    // Copies the contents of a database row into a record type
    let toTranslation (xlt : dbSchema.ServiceTypes.Translation) = {
        FromText = xlt.FromText
        ToText = xlt.ToText
        Context = xlt.Context
        Property = xlt.Property
        Criteria = xlt.Criteria
        FromLang = xlt.FromLang
        ToLang = xlt.ToLang
    }

    // Copies the database to memory
    let getTranslations =
        let db = dbSchema.GetDataContext(Settings.ConnectionStrings.DbConnectionString)
        query {
            for row in db.Translation do 
                select row
        } |> Seq.toArray |> Array.map toTranslation

    // Returns the correct translation for the given search
    let findTranslation (s : Search) =
        // If fromtext does not contain a word, return an empty string
        match s.FromText.Trim() with
        | "" -> ""
        | _ ->
            
            // If toLanguageCode is not valid, sets it to default "no"
            let toLang =
                match s.ToLang with
                | null | "" -> "no"
                | _ -> s.ToLang

            // Search for a valid translation
            let result =
                getTranslations
                |> Array.tryFind ( fun row -> 
                    row.Criteria.ToLower() = s.Criteria.ToLower() &&
                    row.FromLang = fromLang &&
                    row.FromText = s.FromText &&
                    row.Property.ToLower() = s.Property.ToLower() &&
                    row.Context.ToLower() = s.Context.ToLower() &&
                    row.ToLang = toLang )

            match result with
                | Some x -> x.ToText
                | None -> s.FromText

    // Wraps FindTranslation inside an async
    let findTranslationAsync s = 
        async { return findTranslation s }

    // Takes a list of database searches, makes asyncs out of them, and combines them into a single parallel task.
    // Returns a list of results, that are in the same order as the searches.
    let getToTextAsync (searchList : Search list) =
        searchList
        |> List.map findTranslationAsync
        |> Async.Parallel
        |> Async.RunSynchronously