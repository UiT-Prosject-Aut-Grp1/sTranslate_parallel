namespace sTranslate_parallel

module XltEnums =
    open System

    type PropertyTypes = 
        | Id      = 1 
        | Text    = 2
        | ToolTip = 3
        | Page    = 4
    type Criterias =
        | None      = 0
        | Exact     = 1
        | StartWith = 2
        | EndWith   = 3
        | Contains  = 4

    // Helper function to give the first element of a sequence, if it contains something
    let checkHead (s : seq<'a>) =
        if Seq.isEmpty s then
            None
        else 
            Some <| Seq.head s

    // Get enumeration state
    let GetEnumState myType (value : string) =
        
        // Filters a string array and finds the correct Enumeration
        Enum.GetNames(myType)
        |> Seq.filter (fun x -> x.ToLower() = value.ToLower())
        |> checkHead

    // Creates an object of type PropertyType from the input string
    let ToPropertyType value = 
        GetEnumState typeof<PropertyTypes> value

    // Creates an object of type Criterias from the input string
    let ToCriteria value =
        GetEnumState typeof<Criterias> value