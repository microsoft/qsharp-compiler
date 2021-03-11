// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.Formatter.ParseTree.Node

open Antlr4.Runtime
open Antlr4.Runtime.Tree
open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree
open Microsoft.Quantum.QsFmt.Parser
open System.Collections.Generic
open System.Collections.Immutable

/// <summary>
/// The contiguous sequence of tokens in the hidden channel that occur before the token with the given
/// <paramref name="index"/> in <paramref name="tokens"/>.
/// </summary>
let hiddenTokensBefore (tokens: IToken ImmutableArray) index =
    seq {
        for i = index - 1 downto 0 do
            tokens.[i]
    }
    |> Seq.takeWhile (fun token -> token.Channel = QSharpLexer.Hidden)
    |> Seq.rev

let prefix tokens index =
    hiddenTokensBefore tokens index
    |> Seq.map (fun token -> token.Text)
    |> String.concat ""
    |> Trivia.ofString

let toTerminal tokens (terminal: IToken) =
    { Prefix = prefix tokens terminal.TokenIndex; Text = terminal.Text }

let toUnknown (tokens: IToken ImmutableArray) (node: IRuleNode) =
    let text =
        seq { for i in node.SourceInterval.a .. node.SourceInterval.b -> tokens.[i] }
        |> Seq.map (fun token -> token.Text)
        |> Seq.fold (+) ""

    { Prefix = prefix tokens node.SourceInterval.a; Text = text }

/// Zips two sequences. The shorter sequence is padded with the given padding element.
let padZip (source1: _ seq, padding1) (source2: _ seq, padding2) =
    let enumerator1 = source1.GetEnumerator()
    let enumerator2 = source2.GetEnumerator()

    let next (enumerator: _ IEnumerator) =
        if enumerator.MoveNext() then Some enumerator.Current else None

    let nextPair _ =
        match next enumerator1, next enumerator2 with
        | None, None -> None
        | next1, next2 -> Some(next1 |> Option.defaultValue padding1, next2 |> Option.defaultValue padding2)

    Seq.initInfinite nextPair |> Seq.takeWhile Option.isSome |> Seq.choose id

let tupleItems items commas =
    padZip (items |> Seq.map Some, None) (commas |> Seq.map Some, None)
    |> Seq.map (fun (item, comma) -> { Item = item; Comma = comma })
    |> List.ofSeq
