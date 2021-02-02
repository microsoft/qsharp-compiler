// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.Formatter.Formatter

open Antlr4.Runtime
open Microsoft.Quantum.QsFmt.Formatter.Errors
open Microsoft.Quantum.QsFmt.Formatter.ParseTree.Namespace
open Microsoft.Quantum.QsFmt.Formatter.Printer
open Microsoft.Quantum.QsFmt.Formatter.Rules
open Microsoft.Quantum.QsFmt.Formatter.Utils
open Microsoft.Quantum.QsFmt.Parser
open System.Collections.Immutable

/// <summary>
/// Parses the Q# source code into a <see cref="QsFmt.Formatter.SyntaxTree.Document"/>.
/// </summary>
let private parse (source: string) =
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

[<CompiledName "Format">]
let format source =
    parse source
    |> Result.map
        (curry collapsedSpaces.Document ()
         >> curry operatorSpacing.Document ()
         >> curry newLines.Document ()
         >> curry indentation.Document 0
         >> printer.Document)

[<CompiledName "Identity">]
let identity source =
    parse source |> Result.map printer.Document
