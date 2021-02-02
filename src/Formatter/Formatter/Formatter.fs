// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module QsFmt.Formatter.Formatter

open Antlr4.Runtime
open QsFmt.Formatter.Errors
open QsFmt.Formatter.ParseTree.Namespace
open QsFmt.Formatter.Printer
open QsFmt.Formatter.Rules
open QsFmt.Formatter.Utils
open QsFmt.Parser
open System.Collections.Immutable

/// <summary>
/// Parses the Q# source code into a <see cref="QsFmt.Formatter.SyntaxTree.Document"/>.
/// </summary>
let private parse (source: string) =
    let tokenStream = source |> AntlrInputStream |> QSharpLexer |> CommonTokenStream

    let parser = QSharpParser tokenStream
    let errorListener = ErrorListListener()
    parser.AddErrorListener errorListener
    let documentContext = parser.document ()

    if List.isEmpty errorListener.SyntaxErrors then
        let errorTokens = errorListener.SyntaxErrors |> Seq.map (fun error -> error.Token)
        let tokens = tokenStream.GetTokens() |> hideTokens errorTokens |> ImmutableArray.CreateRange
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
