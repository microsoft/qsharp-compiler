// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.Program

open System
open System.Collections.Generic
open System.IO
open CommandLine
open Microsoft.Quantum.QsFmt.App.Arguments
open Microsoft.Quantum.QsFmt.App.DesignTimeBuild
open Microsoft.Quantum.QsFmt.Formatter

let runCommand (commandWithOptions: CommandWithOptions) inputs =

    let mutable paths = Set.empty
    let mutable filesUpdated = Set.empty
    let mutable filesFormatted = Set.empty

    let rec processOneInput input =
        // Make sure inputs are not processed more than once.
        if input |> Path.GetFullPath |> paths.Contains then
            eprintfn "This input has already been processed: %s" input
            { RunResult.Default with ExitCode = ExitCode.FileAlreadyProcessed }
        else
            paths <- input |> Path.GetFullPath |> paths.Add

            try
                if (File.GetAttributes input).HasFlag(FileAttributes.Directory) then
                    let newInputs =
                        let topLevelFiles = Directory.EnumerateFiles(input, "*.qs") |> List.ofSeq

                        if commandWithOptions.RecurseFlag then
                            topLevelFiles @ (Directory.EnumerateDirectories input |> List.ofSeq)
                        else
                            topLevelFiles

                    newInputs |> processManyInputs
                else
                    let source =
                        if commandWithOptions.BackupFlag then File.Copy(input, (input + "~"), true)
                        File.ReadAllText input

                    let result =
                        match commandWithOptions.CommandKind with
                        | Update ->
                            Formatter.performUpdate input commandWithOptions.QSharpVersion source
                            |> Result.map (fun isUpdated -> if isUpdated then filesUpdated <- filesUpdated.Add input)
                        | Format ->
                            Formatter.performFormat input commandWithOptions.QSharpVersion source
                            |> Result.map (fun isFormatted ->
                                if isFormatted then filesFormatted <- filesFormatted.Add input)
                        | UpdateAndFormat ->
                            Formatter.performUpdateAndFormat input commandWithOptions.QSharpVersion source
                            |> Result.map (fun (isUpdated, isFormatted) ->
                                if isUpdated then filesUpdated <- filesUpdated.Add input
                                if isFormatted then filesFormatted <- filesFormatted.Add input)

                    match result with
                    | Ok _ -> { RunResult.Default with FilesProcessed = 1 }
                    | Error errors ->
                        errors |> List.iter (eprintfn "%s, %O" input)
                        { RunResult.Default with ExitCode = ExitCode.SyntaxErrors; SyntaxErrors = errors }
            with
            | :? IOException as ex ->
                eprintfn "%s" ex.Message
                { RunResult.Default with ExitCode = ExitCode.IOError }
            | :? UnauthorizedAccessException as ex ->
                eprintfn "%s" ex.Message
                { RunResult.Default with ExitCode = ExitCode.UnauthorizedAccess }

    and foldResults (previousRunResult: RunResult) filePath =
        let newRunResult = (filePath |> processOneInput)

        {
            FilesProcessed = previousRunResult.FilesProcessed + newRunResult.FilesProcessed
            ExitCode = max previousRunResult.ExitCode newRunResult.ExitCode
            SyntaxErrors = previousRunResult.SyntaxErrors @ newRunResult.SyntaxErrors
        }

    and processManyInputs inputs =
        inputs |> Seq.fold foldResults RunResult.Default

    let runResults = processManyInputs inputs

    if runResults.ExitCode = ExitCode.Success then
        match commandWithOptions.CommandKind with
        | Update ->
            printfn "Updated %i files:" (Set.count filesUpdated)

            for file in filesUpdated do
                printfn "\tUpdated %s" file
        | Format ->
            printfn "Formatted %i files:" (Set.count filesFormatted)

            for file in filesFormatted do
                printfn "\tFormatted %s" file
        | UpdateAndFormat ->
            printfn "Updated %i files:" (Set.count filesUpdated)

            for file in filesUpdated do
                printfn "\tUpdated %s" file

            printfn "Formatted %i files:" (Set.count filesFormatted)

            for file in filesFormatted do
                printfn "\tFormatted %s" file

    runResults

[<CompiledName "Main">]
[<EntryPoint>]
let main args =

    let startTime = DateTime.Now
    let executionTime = Diagnostics.Stopwatch.StartNew()
    use _telemetryManagerHandle = Telemetry.initializeTelemetry args

    let parseArgsResult =
        try
            assemblyLoadContextSetup ()

            CommandLine.Parser.Default.ParseArguments<FormatArguments, UpdateArguments, UpdateAndFormatArguments> args
            |> Ok
        with
        | ex -> ex |> Result.Error

    let commandWithOptions =
        try
            match parseArgsResult with
            | Ok parsedArgs ->
                parsedArgs.MapResult(
                    (fun (options: IArguments) -> options |> CommandWithOptions.fromIArguments),
                    (fun (errors: IEnumerable<Error>) ->
                        if errors.IsHelp() then Result.Error ExitCode.Help
                        elif errors.IsVersion() then Result.Error ExitCode.Version
                        else Result.Error ExitCode.BadArguments)
                )
                |> Ok
            | Result.Error ex -> ex |> Result.Error
        with
        | ex -> ex |> Result.Error

    let runResult =
        try
            match commandWithOptions with
            | Ok commandWithOptions ->
                match commandWithOptions with
                | Ok commandWithOptions -> runCommand commandWithOptions commandWithOptions.Input
                | Error exitCode -> { RunResult.Default with ExitCode = exitCode }
                |> Ok
            | Result.Error ex -> ex |> Result.Error
        with
        | ex -> ex |> Result.Error

    Telemetry.logExecutionCompleted commandWithOptions runResult startTime executionTime.Elapsed

    match runResult with
    | Ok runResult -> runResult.ExitCode |> int
    | Error ex ->
        eprintf "Unexpected Error: %s" (ex.ToString())
        ExitCode.UnhandledException |> int
