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
open Microsoft.Quantum.Telemetry

type RunResult =
    {
        FilesProcessed: int
        ExitCode: int
        SyntaxErrors: Errors.SyntaxError list
    }
    with
        static member Default =
            {
                FilesProcessed = 0
                ExitCode = 0
                SyntaxErrors = []
            }

let makeFullPath input =
    if input = "-" then input else Path.GetFullPath input

let run arguments inputs =

    let mutable paths = Set.empty

    let rec doOne arguments input =
        // Make sure inputs are not processed more than once.
        if input |> makeFullPath |> paths.Contains then
            // Change the "-" input to say "<Standard Input>" in the error
            let input = if input = "-" then "<Standard Input>" else input
            eprintfn "This input has already been processed: %s" input
            { RunResult.Default with ExitCode = 5 }
        else
            paths <- input |> makeFullPath |> paths.Add

            try
                if input <> "-" && (File.GetAttributes input).HasFlag(FileAttributes.Directory) then
                    let newInputs =
                        let topLevelFiles = Directory.EnumerateFiles(input, "*.qs") |> List.ofSeq

                        if arguments.RecurseFlag then
                            topLevelFiles @ (Directory.EnumerateDirectories input |> List.ofSeq)
                        else
                            topLevelFiles

                    newInputs |> doMany arguments
                else
                    let source =
                        if input = "-" then
                            stdin.ReadToEnd()
                        else
                            if arguments.BackupFlag then File.Copy(input, (input + "~"), true)
                            File.ReadAllText input

                    let command =
                        match arguments.CommandKind with
                        | Update -> Formatter.update input arguments.QSharp_Version
                        | Format -> Formatter.format arguments.QSharp_Version
                        | UpdateAndFormat -> Formatter.updateAndFormat input arguments.QSharp_Version

                    match command source with
                    | Ok result ->
                        if input = "-" then printf "%s" result else File.WriteAllText(input, result)
                        { RunResult.Default with FilesProcessed = 1 }
                    | Error errors ->
                        // Change the "-" input to say "<Standard Input>" in the error
                        let input = if input = "-" then "<Standard Input>" else input
                        errors |> List.iter (eprintfn "%s, %O" input)
                        { RunResult.Default with ExitCode = 1; SyntaxErrors = errors }
            with
            | :? IOException as ex ->
                eprintfn "%s" ex.Message
                { RunResult.Default with ExitCode = 3 }
            | :? UnauthorizedAccessException as ex ->
                eprintfn "%s" ex.Message
                { RunResult.Default with ExitCode = 4 }

    and foldResults (previousRunResult: RunResult) filePath =
        let newRunResult = (filePath |> doOne arguments)
        {
            FilesProcessed = previousRunResult.FilesProcessed + newRunResult.FilesProcessed
            ExitCode = max previousRunResult.ExitCode newRunResult.ExitCode
            SyntaxErrors = previousRunResult.SyntaxErrors @ newRunResult.SyntaxErrors
        }

    and doMany arguments inputs =
        inputs |> Seq.fold foldResults RunResult.Default

    doMany arguments inputs

let runUpdate (arguments: UpdateArguments) =
    match Arguments.fromUpdateArguments arguments with
    | Ok args -> args.Input |> run args
    | Error errorCode -> { RunResult.Default with ExitCode = errorCode }

let runFormat (arguments: FormatArguments) =
    let asUpdateArguments : UpdateArguments =
        {
            Backup = arguments.Backup
            Recurse = arguments.Recurse
            QdkVersion = arguments.QdkVersion
            InputFiles = arguments.InputFiles
            ProjectFile = arguments.ProjectFile
        }

    match Arguments.fromUpdateArguments asUpdateArguments with
    | Ok args -> args.Input |> run { args with CommandKind = Format }
    | Error errorCode -> { RunResult.Default with ExitCode = errorCode }

let runUpdateAndFormat (arguments: UpdateAndFormatArguments) =
    let asUpdateArguments : UpdateArguments =
        {
            Backup = arguments.Backup
            Recurse = arguments.Recurse
            QdkVersion = arguments.QdkVersion
            InputFiles = arguments.InputFiles
            ProjectFile = arguments.ProjectFile
        }

    match Arguments.fromUpdateArguments asUpdateArguments with
    | Ok args -> args.Input |> run { args with CommandKind = UpdateAndFormat }
    | Error errorCode -> { RunResult.Default with ExitCode = errorCode }

type ParseArgsResult =
    | Success of ParserResult<obj>
    | Error of Exception

type ExecutionResult =
    | Success of RunResult
    | Error of Exception

type InputType =
    | Files
    | Project
    | Unknown

type CommandType =
    | Update
    | Format
    | UpdateFormat
    | Unknown

type ExecutionCompleted =
    {
        StartTime: DateTime
        Command: CommandType
        RecurseFlag: bool
        BackupFlag: bool
        InputType: InputType
        QSharpVersion: string
        UnhandledException: Exception option
        [<SerializeJson>]
        SyntaxErrors: Errors.SyntaxError list option
        ExecutionTime: TimeSpan
        FilesProcessed: int
        ExitCode: int
    }

type ArgumentOptions =
    {
        Command: CommandType
        RecurseFlag: bool
        BackupFlag: bool
        InputType: InputType
        QSharpVersion: string
    }
    with
        static member Default =
            {
                Command = CommandType.Unknown
                RecurseFlag = false
                BackupFlag = false
                InputType = InputType.Unknown
                QSharpVersion = ""
            }

[<CompiledName "Main">]
[<EntryPoint>]
let main args =

    let startTime = DateTime.Now
    let telemetryConfig =
        TelemetryManagerConfig(
            AppId = "QsFmt",
            HostingEnvironmentVariableName = "QSFMT_HOSTING_ENV",
            TelemetryOptOutVariableName = "QSFMT_TELEMETRY_OPT_OUT",
            MaxTeardownUploadTime = TimeSpan.FromSeconds(2.0),
            OutOfProcessUpload = true,
            ExceptionLoggingOptions =
                ExceptionLoggingOptions(CollectTargetSite = true, CollectSanitizedStackTrace = true),
            SendTelemetryInitializedEvent = false,
            SendTelemetryTearDownEvent = false,
            TestMode = true
        )
    use _telemetryManagerHandle = TelemetryManager.Initialize(telemetryConfig, args)

    let parseArgsResult =
        try
            assemblyLoadContextSetup ()
            ParseArgsResult.Success ( CommandLine.Parser.Default.ParseArguments<FormatArguments, UpdateArguments, UpdateAndFormatArguments> args )
        with
        | ex -> ParseArgsResult.Error ( ex )

    let executionResult =
        try
            match parseArgsResult with
            | ParseArgsResult.Success parsedArgs ->
                Success (
                    parsedArgs.MapResult(
                        (fun (options: FormatArguments) -> options |> runFormat),
                        (fun (options: UpdateArguments) -> options |> runUpdate),
                        (fun (options: UpdateAndFormatArguments) -> options |> runUpdateAndFormat),
                        (fun (_: IEnumerable<Error>) -> { RunResult.Default with ExitCode = 2 })
                    )
                )
            | ParseArgsResult.Error ex -> Error ( ex )
        with
        | ex -> Error ( ex )

    let syntaxErrors =
        match executionResult with
        | Success runResult -> Some ( runResult.SyntaxErrors )
        | Error ex -> None

    let filesProcessed =
        match executionResult with
        | Success runResult -> runResult.FilesProcessed
        | Error ex -> 0

    let exitCode =
        match executionResult with
        | Success runResult -> runResult.ExitCode
        | Error ex -> -1

    let unhandledException =
        match  executionResult with
        | Success runResult -> None
        | Error ex -> Some ( ex )

    let argumentOptions =
        match parseArgsResult with
        | ParseArgsResult.Success parsedArgs ->
            parsedArgs.MapResult(
                (fun (options: FormatArguments) ->
                    {
                        RecurseFlag = options.Recurse
                        BackupFlag = options.Backup
                        Command = Format
                        InputType = if String.IsNullOrEmpty(options.ProjectFile) then Project else Files
                        QSharpVersion = options.QdkVersion
                    }
                ),
                (fun (options: UpdateArguments) ->
                    {
                        RecurseFlag = options.Recurse
                        BackupFlag = options.Backup
                        Command = Update
                        InputType = if String.IsNullOrEmpty(options.ProjectFile) then Project else Files
                        QSharpVersion = options.QdkVersion
                    }
                ),
                (fun (options: UpdateAndFormatArguments) ->
                    {
                        RecurseFlag = options.Recurse
                        BackupFlag = options.Backup
                        Command = UpdateFormat
                        InputType = if String.IsNullOrEmpty(options.ProjectFile) then Project else Files
                        QSharpVersion = options.QdkVersion
                    }
                ),
                (fun (_: IEnumerable<Error>) -> ArgumentOptions.Default )
            )
        | ParseArgsResult.Error ex -> ArgumentOptions.Default

    let executionCompletedEvent =
        {
            StartTime = startTime
            Command = argumentOptions.Command
            RecurseFlag = argumentOptions.RecurseFlag
            BackupFlag = argumentOptions.BackupFlag
            InputType = argumentOptions.InputType
            QSharpVersion = argumentOptions.QSharpVersion
            UnhandledException = unhandledException
            SyntaxErrors = syntaxErrors
            ExecutionTime = DateTime.Now - startTime
            FilesProcessed = filesProcessed
            ExitCode = exitCode
        }

    TelemetryManager.LogObject(executionCompletedEvent)

    exitCode
