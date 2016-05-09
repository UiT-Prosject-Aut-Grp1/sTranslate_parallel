namespace sTranslate_parallel

module XltEnums =
    open System

    type PropertyTypes = 
        | Id      = 1 
        | Text    = 2
        | ToolTip = 3
        | Page    = 4
    type CriteriaTypes =
        | None      = 0
        | Exact     = 1
        | StartWith = 2
        | EndWith   = 3
        | Contains  = 4

    // Get enumeration state
    let getEnumState myType (value : string) =
        
        // Filters a string array and finds the correct Enumeration
        Enum.GetNames(myType)
        |> Seq.tryFind (fun x -> x.ToLower() = value.ToLower())

    // Creates an object of type PropertyType from the input string
    let toProperty value = 
        getEnumState typeof<PropertyTypes> value

    // Creates an object of type Criterias from the input string
    let toCriteria value =
        getEnumState typeof<CriteriaTypes> value