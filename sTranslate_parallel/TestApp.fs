open System
open System.Data
open System.Data.Linq
open System.IO
open System.Text
open FSharp.Data.TypeProviders
open Microsoft.FSharp.Linq
open sTranslate_fs.XltTool
open sTranslate_fs.XltEnums

let logFile = "D:\LOGS\sTranslate_fs_GetToText.csv"
let testFile = "D:\LOGS\StressTest.csv"
let addToList (myList:List<'a>) element = element::myList

let StressTest translateFunction fileName numLoops = 
    printfn "Using search data in: %s" fileName
    let startTime = DateTime.Now
    // Initialize accumulator variables
    let mutable searchCounter = 0
    let mutable loopTimes : List<TimeSpan> = []
    // Create string array of each line in the .csv file
    let lines = System.IO.File.ReadAllLines(fileName, Encoding.GetEncoding("ISO-8859-1"))
    // Do the .csv search numLoops number of times
    for i in 1 .. numLoops do
        let loopStartTime = DateTime.Now
        for line in lines do
            // Get search criteria from the current line
            let splitLine = line.Split([|';'|])
            let fromText = splitLine.[0]
            let context = splitLine.[1]
            let property = ToPropertyType splitLine.[2]
            let criteria = ToCriteria splitLine.[3]
            let toLang = splitLine.[4]
            // Call the translation function
            let toText = translateFunction criteria fromText property context toLang
            toText |> ignore
            //printfn "EN: %s; NO: %s" fromText toText
            // Track number of individual searches
            searchCounter <- searchCounter+1
        // Time the individual loop
        loopTimes <- addToList loopTimes (DateTime.Now.Subtract(loopStartTime))
        // Track completion
        let pctComplete = Math.Floor (float i)/(float numLoops)*100.0
        printf "\r%i%%" (int pctComplete)
    printfn ""
    // Time the entire stresstest
    let elapsedTime = DateTime.Now.Subtract(startTime) 
    (searchCounter,elapsedTime,List.rev loopTimes)

[<EntryPoint>]
let main argv = 
    
    // Call the stresstest
    let translateFunction = GetToText
    let fileName = testFile
    let numLoops = 50
    let (searchCounter,elapsedTime,loopTimes) = StressTest translateFunction fileName numLoops
    
    // Print test results
    printfn "Duration: %A sec" elapsedTime
    printfn "Searches: %i" searchCounter
    printfn "Loops: %i" numLoops
    printfn "Looptimes: %A" loopTimes

    // Saves to file
    let outFile = new StreamWriter(logFile)
    let dataFrame = loopTimes
                    |> Seq.iter (fun y -> outFile.WriteLine(y.ToString()))
    outFile.Close() |> ignore

    // Keypress to close terminal
    System.Console.ReadKey() |> ignore

    0 // return an integer exit code
