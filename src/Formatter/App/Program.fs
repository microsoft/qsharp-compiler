// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.Program

open Argu
open Microsoft.Quantum.QsFmt.Formatter
open System.IO

[<HelpDescription "Display this list of options.">]
type private Argument =
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

        match Formatter.format source with
        | Ok result ->
            printf "%s" result
            0
        | Error errors ->
            errors |> List.iter (eprintfn "%O")
            1
    with :? ArguParseException as ex ->
        eprintf "%s" ex.Message
        2
