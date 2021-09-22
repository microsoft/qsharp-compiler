// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.Program

open Argu
open Microsoft.Quantum.QsFmt.Formatter
open System
open System.IO

/// A command-line argument.
[<HelpDescription "Display this list of options.">]
type Argument =
    /// The path to the input file.
    | [<MainCommand; Unique; Last>] Input of string list
    | [<InheritAttribute; Unique; AltCommandLine("-b")>] Backup
    | [<InheritAttribute; Unique; AltCommandLine("-r")>] Recurse
    | [<SubCommand; CliPrefix(CliPrefix.None)>] Update
    | [<SubCommand; CliPrefix(CliPrefix.None)>] Format

    interface IArgParserTemplate with
        member arg.Usage =
            match arg with
            | Input _ -> "File to format or \"-\" to read from standard input."
            | Backup -> "Create backup files of input files."
            | Recurse -> "Process the input folder recursively."
            | Update _ -> "Update depreciated syntax in the input files."
            | Format _ -> "Format the source code in input files."

type CommandKind =
    | Update
    | Format

type Arguments =
    {
        CommandKind : CommandKind
        RecurseFlag : bool
        BackupFlag : bool
        Inputs : string list
    }

let rec doOne arguments input =
    try
        if input <> "-" && (File.GetAttributes input).HasFlag(FileAttributes.Directory) then
            let newInputs =
                let topLevelFiles = Directory.EnumerateFiles(input, "*.qs") |> List.ofSeq

                if arguments.RecurseFlag then
                    topLevelFiles @ (Directory.EnumerateDirectories input |> List.ofSeq)
                else
                    topLevelFiles

            newInputs |> run arguments
        else
            let source =
                if input = "-" then
                    stdin.ReadToEnd()
                else
                    if arguments.BackupFlag then
                        File.Copy(input, (input + "~"), true)
                    File.ReadAllText input

            let command =
                match arguments.CommandKind with
                | Update ->
                        Formatter.update input
                | Format ->
                    Formatter.format

            match command source with
            | Ok result ->
                if input = "-" then
                    printf "%s" result
                else
                    File.WriteAllText(input, result)
                0
            | Error errors ->
                // Change the "-" input to say "<Standard Input>" in the error
                let input =
                    if input = "-" then
                        "<Standard Input>"
                    else
                        input
                errors |> List.iter (eprintfn "%s, %O" input)
                1
    with
    | :? IOException as ex ->
        eprintfn "%s" ex.Message
        3
    | :? UnauthorizedAccessException as ex ->
        eprintfn "%s" ex.Message
        4

and run arguments inputs =
    inputs
    |> Seq.fold (fun (rtrnCode: int) filePath -> max rtrnCode (filePath |> doOne arguments)) 0

[<CompiledName "Main">]
[<EntryPoint>]
let main args =
    let parser = ArgumentParser.Create()

    try
        let results = parser.Parse args
        let args =
            {
                CommandKind =
                    match results.TryGetSubCommand() with
                    | None // default to update command
                    | Some Argument.Update -> Update
                    | Some Argument.Format -> Format
                    | _ -> failwith "unrecognized command used"
                RecurseFlag = results.Contains Recurse
                BackupFlag = results.Contains Backup
                Inputs = results.GetResult Input
            }

        args.Inputs |> run args
    with
    | :? ArguParseException as ex ->
        eprintf "%s" ex.Message
        2
