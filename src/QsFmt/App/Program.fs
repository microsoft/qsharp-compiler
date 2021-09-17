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

let doOne command inputFile =
    try
        let source = if inputFile = "-" then stdin.ReadToEnd() else File.ReadAllText inputFile
        match command source with
        | Ok result ->
            //printfn "%s:" inputFile
            //printfn "%s" result
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

let run command input =
    input |> Seq.fold (fun (rtrnCode: int) filePath -> max rtrnCode (filePath |> doOne command)) 0

[<CompiledName "Main">]
[<EntryPoint>]
let main args =
    let parser = ArgumentParser.Create()

    let rec processDirecories recurse dir =
        if dir <> "-" && (File.GetAttributes dir).HasFlag(FileAttributes.Directory) then
            let topLevelFiles = Directory.EnumerateFiles(dir, "*.qs") |> List.ofSeq
            if recurse then
                topLevelFiles @ (Directory.EnumerateDirectories dir |> Seq.map (processDirecories recurse) |> Seq.concat |> List.ofSeq)
            else
                topLevelFiles
        else
            [dir]

    try
        let results = parser.Parse args
        let recurseFlag = results.Contains Recurse
        let inputs = results.GetResult Input |> List.map (processDirecories recurseFlag) |> List.concat

        let command =
            match results.TryGetSubCommand() with
            | None // default to update command
            | Some Update -> Formatter.update
            | Some Format -> Formatter.format
            | _ -> failwith "unrecognized command used"
        inputs |> run command
    with
    | :? ArguParseException as ex ->
        eprintf "%s" ex.Message
        2
    | :? IOException as ex ->
        eprintfn "%s" ex.Message
        3
    | :? UnauthorizedAccessException as ex ->
        eprintfn "%s" ex.Message
        4
