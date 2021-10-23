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
            5
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
                        0
                    | Error errors ->
                        // Change the "-" input to say "<Standard Input>" in the error
                        let input = if input = "-" then "<Standard Input>" else input
                        errors |> List.iter (eprintfn "%s, %O" input)
                        1
            with
            | :? IOException as ex ->
                eprintfn "%s" ex.Message
                3
            | :? UnauthorizedAccessException as ex ->
                eprintfn "%s" ex.Message
                4

    and doMany arguments inputs =
        inputs |> Seq.fold (fun (rtrnCode: int) filePath -> max rtrnCode (filePath |> doOne arguments)) 0

    doMany arguments inputs

let runUpdate (arguments: UpdateArguments) =
    match Arguments.fromUpdateArguments arguments with
    | Ok args -> args.Input |> run args
    | Error errorCode -> errorCode

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
    | Error errorCode -> errorCode

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
    | Error errorCode -> errorCode

[<CompiledName "Main">]
[<EntryPoint>]
let main args =

    assemblyLoadContextSetup ()

    let result = CommandLine.Parser.Default.ParseArguments<FormatArguments, UpdateArguments, UpdateAndFormatArguments> args

    result.MapResult(
        (fun (options: FormatArguments) -> options |> runFormat),
        (fun (options: UpdateArguments) -> options |> runUpdate),
        (fun (options: UpdateAndFormatArguments) -> options |> runUpdateAndFormat),
        (fun (_: IEnumerable<Error>) -> 2)
    )
