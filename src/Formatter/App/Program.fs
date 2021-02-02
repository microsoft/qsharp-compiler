// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

open Argu
open QsFmt.Formatter
open System.IO

[<HelpDescription "Display this list of options.">]
type private Argument =
    | [<MainCommand; Unique>] Input of string

    interface IArgParserTemplate with
        member arg.Usage =
            match arg with
            | Input _ -> "File to format or \"-\" to read from standard input."

/// Runs the Q# formatter.
[<EntryPoint>]
let private main args =
    let parser = ArgumentParser.Create<Argument>()

    try
        let results = parser.Parse args
        let input = results.GetResult Input

        if input = "-" then stdin.ReadToEnd() else File.ReadAllText input
        |> Formatter.format
        |> printfn "%s"

        0
    with :? ArguParseException as ex ->
        eprintf "%s" ex.Message
        1
