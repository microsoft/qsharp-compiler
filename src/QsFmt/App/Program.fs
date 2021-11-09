﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.Program

open System
open System.Collections.Generic
open System.IO
open CommandLine
open Microsoft.Quantum.QsFmt.App.Arguments
open Microsoft.Quantum.QsFmt.App.DesignTimeBuild
open Microsoft.Quantum.QsFmt.App.Telemetry
open Microsoft.Quantum.QsFmt.Formatter

let makeFullPath input =
    if input = "-" then input else Path.GetFullPath input

let run (commandWithOptions: CommandWithOptions) inputs =

    let mutable paths = Set.empty

    let rec doOne input =
        // Make sure inputs are not processed more than once.
        if input |> makeFullPath |> paths.Contains then
            // Change the "-" input to say "<Standard Input>" in the error
            let input = if input = "-" then "<Standard Input>" else input
            eprintfn "This input has already been processed: %s" input
            { RunResult.Default with ExitCode = ExitCode.FileAlreadyProcessed }
        else
            paths <- input |> makeFullPath |> paths.Add

            try
                if input <> "-" && (File.GetAttributes input).HasFlag(FileAttributes.Directory) then
                    let newInputs =
                        let topLevelFiles = Directory.EnumerateFiles(input, "*.qs") |> List.ofSeq

                        if commandWithOptions.RecurseFlag then
                            topLevelFiles @ (Directory.EnumerateDirectories input |> List.ofSeq)
                        else
                            topLevelFiles

                    newInputs |> doMany
                else
                    let source =
                        if input = "-" then
                            stdin.ReadToEnd()
                        else
                            if commandWithOptions.BackupFlag then File.Copy(input, (input + "~"), true)
                            File.ReadAllText input

                    let command =
                        match commandWithOptions.CommandKind with
                        | Update -> Formatter.update input commandWithOptions.QSharpVersion
                        | Format -> Formatter.format commandWithOptions.QSharpVersion
                        | UpdateAndFormat -> Formatter.updateAndFormat input commandWithOptions.QSharpVersion
                        | InvalidCommand -> Formatter.identity

                    match command source with
                    | Ok result ->
                        if input = "-" then printf "%s" result else File.WriteAllText(input, result)
                        { RunResult.Default with FilesProcessed = 1 }
                    | Error errors ->
                        // Change the "-" input to say "<Standard Input>" in the error
                        let input = if input = "-" then "<Standard Input>" else input
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
        let newRunResult = (filePath |> doOne)

        {
            FilesProcessed = previousRunResult.FilesProcessed + newRunResult.FilesProcessed
            ExitCode = max previousRunResult.ExitCode newRunResult.ExitCode
            SyntaxErrors = previousRunResult.SyntaxErrors @ newRunResult.SyntaxErrors
        }

    and doMany inputs =
        inputs |> Seq.fold foldResults RunResult.Default

    doMany inputs

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

[<CompiledName "Main">]
[<EntryPoint>]
let main args =

    let startTime = DateTime.Now
    use _telemetryManagerHandle = Telemetry.initializeTelemetry args

    let parseArgsResult =
        try
            assemblyLoadContextSetup ()

            Ok(
                CommandLine.Parser.Default.ParseArguments<FormatArguments, UpdateArguments, UpdateAndFormatArguments>
                    args
            )
        with
        | ex -> Result.Error(ex)

    let runResult =
        try
            match parseArgsResult with
            | Ok parsedArgs ->
                Ok(
                    parsedArgs.MapResult(
                        (fun (options: FormatArguments) -> options |> runFormat),
                        (fun (options: UpdateArguments) -> options |> runUpdate),
                        (fun (options: UpdateAndFormatArguments) -> options |> runUpdateAndFormat),
                        (fun (_: IEnumerable<Error>) -> { RunResult.Default with ExitCode = ExitCode.BadArguments })
                    )
                )
            | Result.Error ex -> Result.Error(ex)
        with
        | ex -> Result.Error(ex)

    Telemetry.logExecutionCompleted parseArgsResult runResult

    match runResult with
    | Ok runResult -> runResult.ExitCode |> int
    | Error ex -> ExitCode.UnhandledException |> int
