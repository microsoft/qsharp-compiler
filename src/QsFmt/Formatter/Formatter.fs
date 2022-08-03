// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.Formatter.Formatter

open System
open System.Collections.Immutable
open Antlr4.Runtime
open Microsoft.Quantum.QsFmt.Formatter.Errors
open Microsoft.Quantum.QsFmt.Formatter.ParseTree.Namespace
open Microsoft.Quantum.QsFmt.Formatter.Printer
open Microsoft.Quantum.QsFmt.Formatter.Rules
open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree
open Microsoft.Quantum.QsFmt.Formatter.Utils
open Microsoft.Quantum.QsFmt.Parser
open System.IO

/// <summary>
/// Parses the Q# source code into a <see cref="QsFmt.Formatter.SyntaxTree.Document"/>.
/// </summary>
let internal parse (source: string) =
    let tokenStream = source |> AntlrInputStream |> QSharpLexer |> CommonTokenStream

    let parser = QSharpParser tokenStream
    parser.RemoveErrorListener ConsoleErrorListener.Instance
    let errorListener = ListErrorListener()
    parser.AddErrorListener errorListener

    let documentContext = parser.document ()

    if List.isEmpty errorListener.SyntaxErrors then
        let tokens = tokenStream.GetTokens() |> ImmutableArray.CreateRange
        documentContext |> toDocument tokens |> Ok
    else
        errorListener.SyntaxErrors |> Error

let internal simpleRule (rule: unit Rewriter) = curry rule.Document ()

/// <summary>
/// Tests whether there is data loss during parsing and unparsing.
/// Raises and ecception if the unparsing result does not match the original source
/// </summary>
let internal checkParsed source parsed =
    let unparsed = printer.Document parsed

    if unparsed <> source then
        failwith (
            "The formatter's syntax tree is inconsistent with the input source code or unparsed wrongly. "
            + "Please let us know by filing a new issue in https://github.com/microsoft/qsharp-compiler/issues/new/choose."
            + "The unparsed code is: \n"
            + unparsed
        )

    parsed


let internal versionToFormatRules (version: Version option) =
    let rules =
        match version with
        // The following lines are provided as examples of different rules for different versions:
        //| Some v when v < new Version("1.0") -> [simpleRule collapsedSpaces]
        //| Some v when v < new Version("1.5") -> [simpleRule operatorSpacing]
        | None
        | Some _ ->
            [
                simpleRule collapsedSpaces
                simpleRule operatorSpacing
                simpleRule newLines
                curry indentation.Document 0
            ]

    rules |> List.fold (>>) id

let internal formatDocument = versionToFormatRules

let performFormat fileName qsharpVersion source =
    parse source
    |> Result.map (fun ast ->
        checkParsed source ast |> ignore
        let formatted = formatDocument qsharpVersion ast |> printer.Document
        let isFormatted = formatted <> source
        if isFormatted then File.WriteAllText(fileName, formatted)
        isFormatted)

let internal versionToUpdateRules (version: Version option) =
    let rules =
        match version with
        // The following lines are provided as examples of different rules for different versions:
        //| Some v when v < new Version("1.0") -> [simpleRule forParensUpdate]
        //| Some v when v < new Version("1.5") -> [simpleRule unitUpdate]
        | None
        | Some _ ->
            [
                simpleRule qubitBindingUpdate
                simpleRule unitUpdate
                simpleRule forParensUpdate
                simpleRule specializationUpdate
                simpleRule arraySyntaxUpdate
                simpleRule booleanOperatorUpdate
            ]

    rules |> List.fold (>>) id

let internal updateDocument fileName qsharpVersion document =
    let updatedDocument = versionToUpdateRules qsharpVersion document

    let warningList =
        match qsharpVersion with
        // The following line is provided as an example of different rules for different versions:
        //| Some v when v < new Version("1.5") -> []
        | None
        | Some _ -> updatedDocument |> checkArraySyntax fileName

    warningList |> List.iter (eprintfn "%s")
    updatedDocument

let performUpdate fileName qsharpVersion source =
    parse source
    |> Result.map (fun ast ->
        checkParsed source ast |> ignore
        let updated = updateDocument fileName qsharpVersion ast |> printer.Document
        let isUpdated = updated <> source
        if isUpdated then File.WriteAllText(fileName, updated)
        isUpdated)

let performUpdateAndFormat fileName qsharpVersion source =
    parse source
    |> Result.map (fun ast ->
        checkParsed source ast |> ignore
        let updatedAST = updateDocument fileName qsharpVersion ast
        let updated = printer.Document updatedAST
        let isUpdated = updated <> source
        let formatted = formatDocument qsharpVersion updatedAST |> printer.Document
        let isFormatted = formatted <> updated
        if isUpdated || isFormatted then File.WriteAllText(fileName, formatted)
        (isUpdated, isFormatted))

[<CompiledName "Update">]
let update fileName qsharpVersion source =
    parse source
    |> Result.map (checkParsed source)
    |> Result.map (updateDocument fileName qsharpVersion)
    |> Result.map (printer.Document)

[<CompiledName "Format">]
let format qsharpVersion source =
    parse source
    |> Result.map (checkParsed source)
    |> Result.map (formatDocument qsharpVersion)
    |> Result.map (printer.Document)

[<CompiledName "UpdateAndFormat">]
let updateAndFormat fileName qsharpVersion source =
    parse source
    |> Result.map (checkParsed source)
    |> Result.map (updateDocument fileName qsharpVersion)
    |> Result.map (formatDocument qsharpVersion)
    |> Result.map (printer.Document)

[<CompiledName "Identity">]
let identity source =
    parse source |> Result.map printer.Document
