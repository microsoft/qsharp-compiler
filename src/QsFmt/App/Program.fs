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
    | [<MainCommand; Unique>] Input of string

    interface IArgParserTemplate with
        member arg.Usage =
            match arg with
            | Input _ -> "File to format or \"-\" to read from standard input."

[<CompiledName "Main">]
[<EntryPoint>]
let main args =
    let parser = ArgumentParser.Create()

    try
        let results = parser.Parse args
        let input = results.GetResult Input
        let source = if input = "-" then stdin.ReadToEnd() else File.ReadAllText input

        match Formatter.parse source with
        | Ok document ->
            // Test whether there is data loss during parsing and unparsing
            if Formatter.unparse document = source then
                // The actuall format process
                printf "%s" (Formatter.formatDocument document)
                0

            // Report error if the unparsing result does match the original source
            else
                failwith (
                    "The formater does not work properly. "
                    + "Please let us know by filing a new issue in https://github.com/microsoft/qsharp-compiler/issues/new/choose."
                )
        | Error errors ->
            errors |> List.iter (eprintfn "%O")
            1

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
    | Failure (msg) ->
        eprintfn "%s" msg
        5
