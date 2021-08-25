// Copyright (c) Microsoft Corporation.
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
        | "Unit" -> {Prefix = []; Text = "()"} |> Literal
        | "Int" -> {Prefix = []; Text = "0"} |> Literal
        | "BigInt" -> {Prefix = []; Text = "0L"} |> Literal
        | "Double" -> {Prefix = []; Text = "0.0"} |> Literal
        | "Bool" -> {Prefix = []; Text = "false"} |> Literal
        | "String" -> {Prefix = []; Text = "\"\""} |> Literal
        | "Result" -> {Prefix = []; Text = "Zero"} |> Literal
        | "Pauli" -> {Prefix = []; Text = "PauliI"} |> Literal
        | "Range" -> {Prefix = []; Text = "1..0"} |> Literal // ToDo: Double-check that this literal is correct
        | _ -> {Prefix = []; Text = "TODO: Throw Error/Warning"} |> Literal // ToDo

    let rec getDefaultValue (``type`` : Type) =
        let space = " " |> Trivia.ofString
        match ``type`` with
        | Type.BuiltIn builtIn -> getBuiltInDefault builtIn
        | Type.Tuple tuple ->
            {
                OpenParen = {Prefix = []; Text = "("}
                Items =
                    tuple.Items
                    |> List.mapi (fun i item ->
                        {
                            Item = item.Item |> Option.map (fun t ->
                                let value = getDefaultValue t
                                // for all items after the first, we need to inject a space before each item
                                // for example: (0,0) goes to (0, 0)
                                if i > 0 then
                                    value |> Expression.mapPrefix ((@) space)
                                else
                                    value)
                            Comma = item.Comma
                        })
                CloseParen = {Prefix = []; Text = ")"}
            }
            |> Tuple
        | Array arrayType -> {Prefix = []; Text = "TODO: Implement Array-Type Handling"} |> Literal // ToDo
        | _ -> {Prefix = []; Text = "TODO: Throw Error/Warning"} |> Literal // ToDo

    { new Rewriter<_>() with
        override rewriter.Expression(_, expression) =

            let sizedArrayFromNewArray (newArray : NewArray) =
                let space = " " |> Trivia.ofString
                {
                    OpenBracket = rewriter.Terminal((), newArray.OpenBracket |> Terminal.mapPrefix (fun _ -> newArray.New.Prefix))
                    Value = getDefaultValue newArray.ItemType
                    Comma = rewriter.Terminal((), {Prefix = []; Text = ","})
                    Size = rewriter.Terminal((), {Prefix = space; Text = "size"})
                    Equals = rewriter.Terminal((), {Prefix = space; Text = "="})
                    Length = rewriter.Expression((), newArray.Length |> Expression.mapPrefix ((@) space))
                    CloseBracket = rewriter.Terminal((), newArray.CloseBracket)
                }

            match expression with
            | NewArray newArray -> newArray |> sizedArrayFromNewArray |> NewSizedArray
            | _ -> base.Expression((), expression)
    }
