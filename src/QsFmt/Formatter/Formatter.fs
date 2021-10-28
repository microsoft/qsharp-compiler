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

/// <summary>
/// Parses the Q# source code into a <see cref="QsFmt.Formatter.SyntaxTree.Document"/>.
/// </summary>
let parse (source: string) =
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

let simpleRule (rule: unit Rewriter) = curry rule.Document ()

let versionToFormatRules (version: Version option) =
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

[<CompiledName "Format">]
let format qsharp_version source =
    let formatDocument document =
        let unparsed = printer.Document document

        // Test whether there is data loss during parsing and unparsing
        if unparsed = source then
            // The actual format process
            document |> versionToFormatRules qsharp_version |> printer.Document
        // Report error if the unparsing result does not match the original source
        else
            failwith (
                "The formatter's syntax tree is inconsistent with the input source code or unparsed wrongly. "
                + "Please let us know by filing a new issue in https://github.com/microsoft/qsharp-compiler/issues/new/choose."
                + "The unparsed code is: \n"
                + unparsed
            )

    parse source |> Result.map formatDocument

let versionToUpdateRules (version: Version option) =
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

[<CompiledName "Update">]
let update fileName qsharp_version source =
    let updateDocument document =

        let updatedDocument = versionToUpdateRules qsharp_version document

        let warningList =
            match qsharp_version with
            // The following line is provided as an example of different rules for different versions:
            //| Some v when v < new Version("1.5") -> []
            | None
            | Some _ -> updatedDocument |> checkArraySyntax fileName

        let printedDocument = updatedDocument |> printer.Document

        warningList |> List.iter (eprintfn "%s")
        printedDocument

    parse source |> Result.map updateDocument

[<CompiledName "Identity">]
let identity source =
    parse source |> Result.map printer.Document
