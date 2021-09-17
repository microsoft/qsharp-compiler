// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.Program

open Argu
open Microsoft.Quantum.QsFmt.Formatter
open System
open System.IO

// ToDo: implement the --backup flag

/// A command-line argument.
[<HelpDescription "Display this list of options.">]
type Argument =
    /// The path to the input file.
    | [<MainCommand; Unique; Last>] Input of string list
    //| [<InheritAttribute; Unique; AltCommandLine("-b")>] Backup
    | [<InheritAttribute; Unique; AltCommandLine("-r")>] Recurse
    | [<SubCommand; CliPrefix(CliPrefix.None)>] Update
    | [<SubCommand; CliPrefix(CliPrefix.None)>] Format

    interface IArgParserTemplate with
        member arg.Usage =
            match arg with
            | Input _ -> "File to format or \"-\" to read from standard input."
            //| Backup -> "Create backup files of input files."
            | Recurse -> "Process the input folder recursively."
            | Update _ -> "Update depreciated syntax in the input files."
            | Format _ -> "Format the source code in input files."

let rec doOne command recurse input =
    try
        if input <> "-" && (File.GetAttributes input).HasFlag(FileAttributes.Directory) then
            let newInputs =
                let topLevelFiles = Directory.EnumerateFiles(input, "*.qs") |> List.ofSeq

                if recurse then
                    topLevelFiles @ (Directory.EnumerateDirectories input |> List.ofSeq)
                else
                    topLevelFiles

            newInputs |> run command recurse
        else
            let source = if input = "-" then stdin.ReadToEnd() else File.ReadAllText input

            match command source with
            | Ok result ->
                printf "%s" result
                0
            | Error errors ->
                errors |> List.iter (eprintfn "%O")
                1
    with
    | :? IOException as ex ->
        eprintfn "%s" ex.Message
        3
    | :? UnauthorizedAccessException as ex ->
        eprintfn "%s" ex.Message
        4

and run command recurse inputs =
    inputs
    |> Seq.fold (fun (rtrnCode: int) filePath -> max rtrnCode (filePath |> doOne command recurse)) 0

[<CompiledName "Main">]
[<EntryPoint>]
let main args =
    let parser = ArgumentParser.Create()

    try
        let results = parser.Parse args
        let inputs = results.GetResult Input
        let recurseFlag = results.Contains Recurse

        let command =
            match results.TryGetSubCommand() with
            | None // default to update command
            | Some Update -> Formatter.update
            | Some Format -> Formatter.format
            | _ -> failwith "unrecognized command used"

        inputs |> run command recurseFlag
    with
    | :? ArguParseException as ex ->
        eprintf "%s" ex.Message
        2
