﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.Formatter.Rules

open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree
open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree.Trivia

/// <summary>
/// Maps a <see cref="Trivia"/> list by applying the mapping function with the <see cref="Trivia"/> nodes before and
/// after the current node, then flattening the result.
/// </summary>
let collectWithAdjacent =
    let rec withBefore before mapping =
        function
        | [] -> []
        | [ x ] -> mapping before x None
        | x :: y :: rest -> mapping before x (Some y) @ withBefore (Some x) mapping (y :: rest)

    withBefore None

/// <summary>
/// Collapses adjacent whitespace characters in <paramref name="trivia"/> into a single space character.
/// </summary>
let collapseTriviaSpaces previous trivia _ =
    match previous, trivia with
    | Some (NewLine _), Whitespace _ -> [ trivia ]
    | _, Whitespace _ -> [ collapseSpaces trivia ]
    | _ -> [ trivia ]

let collapsedSpaces =
    { new Rewriter<_>() with
        override _.Terminal((), terminal) =
            terminal |> Terminal.mapPrefix (collectWithAdjacent collapseTriviaSpaces)
    }

let operatorSpacing =
    { new Rewriter<_>() with
        override _.Let((), lets) =
            let equals = { lets.Equals with Prefix = [ spaces 1 ] }
            { lets with Equals = equals }
    }

/// <summary>
/// Indents the <see cref="Trivia"/> list to the given indentation <paramref name="level"/>.
/// </summary>
let indentPrefix level =
    let indentTrivia previous trivia after =
        match previous, trivia, after with
        | Some (NewLine _), Whitespace _, _ -> [ spaces (4 * level) ]
        | _, NewLine _, Some (Comment _)
        | _, NewLine _, None -> [ trivia; spaces (4 * level) ]
        | _ -> [ trivia ]

    collectWithAdjacent indentTrivia

/// <summary>
/// Indents the <see cref="Terminal"/> token to the given indentation <paramref name="level"/>.
/// </summary>
let indentTerminal level =
    indentPrefix level |> Terminal.mapPrefix

let indentation =
    { new Rewriter<_>() with
        override _.Namespace(level, ns) =
            { base.Namespace(level, ns) with NamespaceKeyword = indentTerminal level ns.NamespaceKeyword }

        override _.NamespaceItem(level, item) =
            base.NamespaceItem(level, item) |> NamespaceItem.mapPrefix (indentPrefix level)

        override _.Statement(level, statement) =
            base.Statement(level, statement) |> Statement.mapPrefix (indentPrefix level)

        override _.Block(level, mapper, block) =
            { base.Block(level + 1, mapper, block) with CloseBrace = indentTerminal level block.CloseBrace }
    }

/// <summary>
/// Prepends the <paramref name="prefix"/> with a new line <see cref="Trivia"/> node if it does not already contain one.
/// </summary>
let ensureNewLine prefix =
    if List.exists isNewLine prefix then prefix else newLine :: prefix

let newLines =
    { new Rewriter<_>() with
        override _.NamespaceItem((), item) =
            base.NamespaceItem((), item) |> NamespaceItem.mapPrefix ensureNewLine

        override _.Statement((), statement) =
            let statement = base.Statement((), statement)

            match statement with
            | Else _ -> statement
            | _ -> Statement.mapPrefix ensureNewLine statement

        override _.Block((), mapper, block) =
            let block = base.Block((), mapper, block)

            if List.isEmpty block.Items then
                block
            else
                { block with CloseBrace = Terminal.mapPrefix ensureNewLine block.CloseBrace }
    }

/// <summary>
/// Gets the trivia from a terminal option.
/// </summary>
let getTrivia paren =
    match paren with
    | None -> []
    | Some p -> p.Prefix

let qubitBindingUpdate =
    { new Rewriter<_>() with
        override rewriter.QubitDeclaration(_, decl) =
            let openTrivia = decl.OpenParen |> getTrivia
            let closeTrivia = decl.CloseParen |> getTrivia

            let keyword =
                match decl.Kind with
                | Use -> "use"
                | Borrow -> "borrow"

            { decl with
                Keyword = rewriter.Terminal((), { decl.Keyword with Text = keyword })
                OpenParen = None
                Binding = decl.Binding |> QubitBinding.mapPrefix ((@) openTrivia)
                CloseParen = None
                Coda =
                    match decl.Coda with
                    | Semicolon semicolon -> semicolon |> Terminal.mapPrefix ((@) closeTrivia) |> Semicolon
                    | Block block ->
                        rewriter.Block((), rewriter.Statement, block) |> Block.mapPrefix ((@) closeTrivia) |> Block
            }
    }

let arraySyntaxUpdate =

    let getBuiltInDefault builtIn =
        match builtIn.Text with
        | "Unit" -> { Prefix = []; Text = "()" } |> Literal |> Some
        | "Int" -> { Prefix = []; Text = "0" } |> Literal |> Some
        | "BigInt" -> { Prefix = []; Text = "0L" } |> Literal |> Some
        | "Double" -> { Prefix = []; Text = "0.0" } |> Literal |> Some
        | "Bool" -> { Prefix = []; Text = "false" } |> Literal |> Some
        | "String" -> { Prefix = []; Text = "\"\"" } |> Literal |> Some
        | "Result" -> { Prefix = []; Text = "Zero" } |> Literal |> Some
        | "Pauli" -> { Prefix = []; Text = "PauliI" } |> Literal |> Some
        | "Range" -> { Prefix = []; Text = "1..0" } |> Literal |> Some // ToDo: Double-check that this literal is correct
        | _ -> None // TODO: Throw Warning

    let rec getDefaultValue (``type``: Type) =
        let space = " " |> Trivia.ofString
        match ``type`` with
        | Type.BuiltIn builtIn -> getBuiltInDefault builtIn
        | Type.Tuple tuple ->
            let items =
                tuple.Items
                |> List.mapi
                    (fun i item ->
                        match item.Item with
                        | Some t ->
                            // When Item has a value, map the Type to and Expression, if valid
                            match getDefaultValue t with
                            | Some value ->
                                // If the Type was mapped to an Expression successfully, create an Expression-SequenceItem
                                {
                                    Item =
                                        // For all items after the first, we need to inject a space before each item
                                        // For example: (0,0) goes to (0, 0)
                                        if i > 0 then
                                            value |> Expression.mapPrefix ((@) space) |> Some
                                        else
                                            value |> Some
                                    Comma = item.Comma
                                }
                                |> Some
                            | None -> None
                        | None ->
                            // A Type-SequenceItem object with Item=None becomes an Expression-SequenceItem with Item=None
                            // ToDo: Don't know what the use-case is for an Item of None
                            { Item = None; Comma = item.Comma } |> Some)
            // If any of the items are None (which means invalid for update) return None
            if items |> List.forall Option.isSome then
                {
                    OpenParen = { Prefix = []; Text = tuple.OpenParen.Text }
                    Items = items |> List.choose id
                    CloseParen = { Prefix = []; Text = tuple.CloseParen.Text }
                }
                |> Tuple
                |> Some
            else
                None
        | Type.Array arrayType ->
            arrayType.ItemType |> getDefaultValue |> Option.map (fun value ->
            {
                OpenBracket = { Prefix = []; Text = arrayType.OpenBracket.Text }
                Value = value
                Comma = { Prefix = []; Text = "," }
                Size = { Prefix = space; Text = "size" }
                Equals = { Prefix = space; Text = "=" }
                Length = { Prefix = space; Text = "0" } |> Literal
                CloseBracket = { Prefix = []; Text = arrayType.CloseBracket.Text }
            }
            |> NewSizedArray)
        | _ -> None

    { new Rewriter<_>() with
        override rewriter.Expression(_, expression) =
            let space = " " |> Trivia.ofString
            match expression with
            | NewArray newArray ->
                match getDefaultValue newArray.ItemType with
                | Some value ->
                    {
                        OpenBracket =
                            rewriter.Terminal(
                                (),
                                newArray.OpenBracket |> Terminal.mapPrefix (fun _ -> newArray.New.Prefix)
                            )
                        Value = value
                        Comma = rewriter.Terminal((), { Prefix = []; Text = "," })
                        Size = rewriter.Terminal((), { Prefix = space; Text = "size" })
                        Equals = rewriter.Terminal((), { Prefix = space; Text = "=" })
                        Length = rewriter.Expression((), newArray.Length |> Expression.mapPrefix ((@) space))
                        CloseBracket = rewriter.Terminal((), newArray.CloseBracket)
                    }
                    |> NewSizedArray
                | None -> newArray |> NewArray // If the conversion is invalid, just leave the node as-is
            | _ -> base.Expression((), expression)
    }

let updateChecker =
    let mutable lineNumber = 1;
    let mutable charNumber = 1;
    { new Reducer<string list>() with
        override _.Combine(x, y) = x @ y

        override _.Terminal terminal =
            for trivia in terminal.Prefix do
                match trivia with
                | Whitespace space -> charNumber <- charNumber + space.Length
                | NewLine nl ->
                    lineNumber <- lineNumber + 1
                    charNumber <- 1
                | Comment comment -> charNumber <- charNumber + comment.Length
            charNumber <- charNumber + terminal.Text.Length
            []

        override reducer.Expression expression =
            match expression with
            | NewArray newArray ->
                let lineBefore, charBefore = lineNumber, charNumber
                let subWarnings = base.Expression expression
                let warning = sprintf "I'm a warning from (%i, %i) to (%i, %i)!" lineBefore charBefore lineNumber charNumber
                reducer.Combine(subWarnings, [warning])
            | _ -> base.Expression expression

        override _.Document document =
            lineNumber <- 1
            charNumber <- 1
            base.Document document
    }
