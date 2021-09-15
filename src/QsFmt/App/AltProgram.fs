// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.AltProgram

open CommandLine
open CommandLine.Text
open Microsoft.Quantum.QsFmt.Formatter
open System
open System.IO
open System.Collections.Generic

[<Verb("update", isDefault = true, HelpText = "Updates depreciated syntax in the input files.")>]
type UpdateOptions = {
    [<Option('b', "backup", HelpText = "Option to create backup files of input files.")>] Backup : bool
    [<Option('r', "recurse", HelpText = "Option to process the input folder recursively.")>] Recurse : bool
    [<Value(0, Min = 1, MetaName = "Input", HelpText = "Input paths. Can be multiple folders or files.")>] Input : string seq
}
with
    [<Usage(ApplicationAlias = "qsfmt")>]
    static member examples
        with get() = seq {
           yield Example("Updates depreciated syntax in the input files", {Backup = false; Recurse = false; Input = seq {"Path\To\My\File.qs"} }) }

[<Verb("format", HelpText = "Formats the source code in input files.")>]
type FormatOptions = {
    [<Option('b', "backup", HelpText = "Option to create backup files of input files.")>] Backup : bool
    [<Option('r', "recurse", HelpText = "Option to process the input folder recursively.")>] Recurse : bool
    [<Value(0, Min = 1, MetaName = "Input", HelpText = "Input paths. Can be multiple folders or files.")>] Input : string seq
}
with
    [<Usage(ApplicationAlias = "qsfmt")>]
    static member examples
        with get() = seq {
           yield Example("Formats the source code in input files", {Backup = false; Recurse = false; Input = seq {"Path\To\My\File.qs"} }) }

let doOne command inputFile =
    try
        let source = if inputFile = "-" then stdin.ReadToEnd() else File.ReadAllText inputFile
        match command source with
        | Ok result ->
            printfn "%s:" inputFile
            printfn "%s" result
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

let runUpdate (options : UpdateOptions) =
    options.Input |> Seq.fold (fun (rtrnCode: int) filePath -> max rtrnCode (filePath |> doOne Formatter.update)) 0

let runFormat (options : FormatOptions) =
    options.Input |> Seq.fold (fun (rtrnCode: int) filePath -> max rtrnCode (filePath |> doOne Formatter.format)) 0

//[<CompiledName "Main">]
//[<EntryPoint>]
let main (args : string []) =
    let result = CommandLine.Parser.Default.ParseArguments<UpdateOptions, FormatOptions> args
    result.MapResult(
        (fun (options: UpdateOptions) -> options |> runUpdate),
        (fun (options: FormatOptions) -> options |> runFormat),
        (fun (_ : IEnumerable<Error>) -> 2)
    )
