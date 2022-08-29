// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

open System
open System.Text.RegularExpressions

type Trivia =
    /// A contiguous region of whitespace.
    | Whitespace of string

    /// A new line character.
    | NewLine of string

    /// A comment.
    | Comment of string

module Trivia =
    let (|Whitespace|NewLine|Comment|) =
        function
        | Whitespace ws -> Whitespace ws
        | NewLine nl -> NewLine nl
        | Comment comment -> Comment comment

    let spaces count =
        String.replicate count " " |> Whitespace

    let newLine = NewLine Environment.NewLine

    let isNewLine trivia =
        match trivia with
        | NewLine _ -> true
        | _ -> false

    let collapseSpaces =
        let replace str = Regex.Replace(str, "\s+", " ")

        function
        | Whitespace ws -> replace ws |> Whitespace
        | NewLine nl -> NewLine nl
        | Comment comment -> Comment comment

    /// <summary>
    /// Matches if the <paramref name="pattern"/> regex matches the start of the <paramref name="input"/> string. Yields
    /// the value of the match and the remaining string.
    /// </summary>
    let (|Prefix|_|) (pattern: string) (input: string) =
        let result = Regex.Match(input, "^" + pattern)

        if result.Success then Some(result.Value, input.[result.Length ..]) else None

    let rec ofString =
        function
        | "" -> []
        | Prefix "\r\n" (_, rest) -> NewLine "\r\n" :: ofString rest
        | Prefix "\r" (_, rest) -> NewLine "\r" :: ofString rest
        | Prefix "\n" (_, rest) -> NewLine "\n" :: ofString rest
        | Prefix "[^\S\r\n]+" (result, rest) -> Whitespace result :: ofString rest
        | Prefix "//[^\r\n]*" (result, rest) -> Comment result :: ofString rest
        | _ ->
            // TODO: Use option.
            failwith "String contains invalid trivia."

type Terminal = { Prefix: Trivia list; Text: string }

module Terminal =
    let mapPrefix mapper terminal =
        { terminal with Prefix = mapper terminal.Prefix }

type 'T SequenceItem = { Item: 'T option; Comma: Terminal option }

type 'T Tuple =
    {
        OpenParen: Terminal
        Items: 'T SequenceItem list
        CloseParen: Terminal
    }

module Tuple =
    let mapPrefix mapper tuple =
        { tuple with OpenParen = tuple.OpenParen |> Terminal.mapPrefix mapper }

type 'T PrefixOperator = { PrefixOperator: Terminal; Operand: 'T }

type 'T PostfixOperator = { Operand: 'T; PostfixOperator: Terminal }

type 'T InfixOperator =
    {
        Left: 'T
        InfixOperator: Terminal
        Right: 'T
    }

type 'T Block =
    {
        OpenBrace: Terminal
        Items: 'T list
        CloseBrace: Terminal
    }

module Block =
    let mapPrefix mapper block =
        { block with OpenBrace = block.OpenBrace |> Terminal.mapPrefix mapper }
