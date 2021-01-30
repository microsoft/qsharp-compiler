/// Tools for creating syntax tree nodes from parse tree nodes.
module internal QsFmt.Formatter.ParseTree.Node

open System.Collections.Generic
open System.Collections.Immutable

open Antlr4.Runtime
open Antlr4.Runtime.Tree
open QsFmt.Formatter.SyntaxTree
open QsFmt.Parser

/// <summary>
/// The contiguous sequence of tokens in the hidden channel that occur before the token with the given
/// <paramref name="index"/> in <paramref name="tokens"/>.
/// </summary>
let private hiddenTokensBefore (tokens: IToken ImmutableArray) index =
    seq {
        for i = index - 1 downto 0 do
            tokens.[i]
    }
    |> Seq.takeWhile (fun token -> token.Channel = QSharpLexer.Hidden)
    |> Seq.rev

/// <summary>
/// The <see cref="Trivia"/> tokens that occur before the token with the given <paramref name="index"/> in
/// <paramref name="tokens"/>.
/// </summary>
let prefix tokens index =
    hiddenTokensBefore tokens index
    |> Seq.map (fun token -> token.Text)
    |> String.concat ""
    |> Trivia.ofString

/// <summary>
/// Creates a syntax tree <see cref="Terminal"/> node from the given parse tree <paramref name="terminal"/>.
/// </summary>
let toTerminal tokens (terminal: IToken) =
    { Prefix = prefix tokens terminal.TokenIndex
      Text = terminal.Text }

/// <summary>
/// Creates a syntax tree <see cref="Terminal"/> node from the given parse tree <paramref name="node"/> that represents
/// unknown or not yet supported syntax.
/// </summary>
let toUnknown (tokens: IToken ImmutableArray) (node: IRuleNode) =
    let text =
        seq { for i in node.SourceInterval.a .. node.SourceInterval.b -> tokens.[i] }
        |> Seq.map (fun token -> token.Text)
        |> Seq.fold (+) ""

    { Prefix = prefix tokens node.SourceInterval.a
      Text = text }

/// Zips two sequences. The shorter sequence is padded with the given padding element.
let private padZip (source1: _ seq, padding1) (source2: _ seq, padding2) =
    let enumerator1 = source1.GetEnumerator()
    let enumerator2 = source2.GetEnumerator()

    let next (enumerator: _ IEnumerator) =
        if enumerator.MoveNext() then Some enumerator.Current else None

    let nextPair _ =
        match next enumerator1, next enumerator2 with
        | None, None -> None
        | next1, next2 -> Some(next1 |> Option.defaultValue padding1, next2 |> Option.defaultValue padding2)

    Seq.initInfinite nextPair
    |> Seq.takeWhile Option.isSome
    |> Seq.choose id

/// <summary>
/// Creates a list of sequence items by pairing each item in <paramref name="items"/> with its respective comma in
/// <paramref name="commas"/>.
/// </summary>
let tupleItems items commas =
    padZip (items |> Seq.map Some, None) (commas |> Seq.map Some, None)
    |> Seq.map (fun (item, comma) -> { Item = item; Comma = comma })
    |> List.ofSeq
