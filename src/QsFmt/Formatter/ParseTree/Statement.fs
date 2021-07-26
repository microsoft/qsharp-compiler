// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.ParseTree

open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree
open Microsoft.Quantum.QsFmt.Parser

/// <summary>
/// Creates syntax tree <see cref="SymbolBinding"/> nodes from a parse tree and the list of tokens.
/// </summary>
type SymbolBindingVisitor(tokens) =
    inherit QSharpParserBaseVisitor<SymbolBinding>()

    override _.DefaultResult = failwith "Unknown symbol binding."

    override _.VisitDiscardSymbol context =
        { Name = context.discard |> Node.toTerminal tokens; Type = None } |> SymbolDeclaration

    override _.VisitSymbolName context =
        { Name = context.name |> Node.toTerminal tokens; Type = None } |> SymbolDeclaration

    override visitor.VisitSymbolTuple context =
        let bindings = context._bindings |> Seq.map visitor.Visit
        let commas = context._commas |> Seq.map (Node.toTerminal tokens)

        {
            OpenParen = context.openParen |> Node.toTerminal tokens
            Items = Node.tupleItems bindings commas
            CloseParen = context.closeParen |> Node.toTerminal tokens
        }
        |> SymbolTuple

type QubitSymbolBindingVisitor(tokens) =
    inherit QSharpParserBaseVisitor<QubitSymbolBinding>()

    override _.DefaultResult = failwith "Unknown symbol binding."

    override _.VisitDiscardSymbol context =
        context.discard |> Node.toTerminal tokens |> QubitSymbolDeclaration

    override _.VisitSymbolName context =
        context.name |> Node.toTerminal tokens |> QubitSymbolDeclaration

    override visitor.VisitSymbolTuple context =
        let bindings = context._bindings |> Seq.map visitor.Visit
        let commas = context._commas |> Seq.map (Node.toTerminal tokens)

        {
            OpenParen = context.openParen |> Node.toTerminal tokens
            Items = Node.tupleItems bindings commas
            CloseParen = context.closeParen |> Node.toTerminal tokens
        }
        |> QubitSymbolTuple


type QubitInitializerVistor(tokens) =
    inherit QSharpParserBaseVisitor<QubitInitializer>()

    let expressionVisitor = ExpressionVisitor tokens

    override _.VisitSingleQubit context =
        {
            Qubit = context.qubit |> Node.toTerminal tokens
            OpenParen = context.openParen |> Node.toTerminal tokens
            CloseParen = context.closeParen |> Node.toTerminal tokens
        }
        |> SingleQubit

    override _.VisitQubitArray context =
        {
            Qubit = context.qubit |> Node.toTerminal tokens
            OpenBracket = context.openBracket |> Node.toTerminal tokens
            Length = context.length |> expressionVisitor.Visit
            CloseBracket = context.closeBracket |> Node.toTerminal tokens
        }
        |> QubitArray

    override visitor.VisitQubitTuple (context : QSharpParser.QubitTupleContext) =
        let initializers = context._initializers |> Seq.map visitor.Visit
        let commas = context._commas |> Seq.map (Node.toTerminal tokens)
    
        {
            OpenParen = context.openParen |> Node.toTerminal tokens
            Items = Node.tupleItems initializers commas
            CloseParen = context.closeParen |> Node.toTerminal tokens
        }
        |> QubitTuple

type QubitBindingVisitor(tokens) =
    inherit QSharpParserBaseVisitor<QubitBinding>()

    let qubitSymbolBindingVisitor = QubitSymbolBindingVisitor tokens
    let qubitInitializerVistor = QubitInitializerVistor tokens

    override _.VisitQubitBinding context =
        {
            Name = context.binding |> qubitSymbolBindingVisitor.Visit
            Equals = context.equals |> Node.toTerminal tokens
            Initializer = context.value |> qubitInitializerVistor.Visit
        }


type StatementVisitor(tokens) =
    inherit QSharpParserBaseVisitor<Statement>()

    let expressionVisitor = ExpressionVisitor tokens

    let symbolBindingVisitor = SymbolBindingVisitor tokens
    let qubitBindingVisitor = QubitBindingVisitor tokens

    override _.DefaultResult = failwith "Unknown statement."

    override _.VisitChildren node =
        Node.toUnknown tokens node |> Statement.Unknown

    override _.VisitReturnStatement context =
        {
            ReturnKeyword = context.``return`` |> Node.toTerminal tokens
            Expression = expressionVisitor.Visit context.value
            Semicolon = context.semicolon |> Node.toTerminal tokens
        }
        |> Return

    override _.VisitUseStatement context =
        {
            UseKeyword = context.``use`` |> Node.toTerminal tokens
            Binding = context.binding |> qubitBindingVisitor.Visit
            OpenParen = context.openParen |> function null -> None | s -> Node.toTerminal tokens s |> Some
            CloseParen = context.closeParen |> function null -> None | s -> Node.toTerminal tokens s |> Some
            Semicolon = context.semicolon |> Node.toTerminal tokens
        }
        |> Use

    override visitor.VisitUseBlock context =
        {
            UseKeyword = context.``use`` |> Node.toTerminal tokens
            Binding = context.binding |> qubitBindingVisitor.Visit
            OpenParen = context.openParen |> function null -> None | s -> Node.toTerminal tokens s |> Some
            CloseParen = context.closeParen |> function null -> None | s -> Node.toTerminal tokens s |> Some
            Block =
                {
                    OpenBrace = context.body.openBrace |> Node.toTerminal tokens
                    Items = context.body._statements |> Seq.map visitor.Visit |> List.ofSeq
                    CloseBrace = context.body.closeBrace |> Node.toTerminal tokens
                }
        }
        |> UseBlock

    override _.VisitBorrowStatement context =
        {
            BorrowKeyword = context.borrow |> Node.toTerminal tokens
            Binding = context.binding |> qubitBindingVisitor.Visit
            OpenParen = context.openParen |> function null -> None | s -> Node.toTerminal tokens s |> Some
            CloseParen = context.closeParen |> function null -> None | s -> Node.toTerminal tokens s |> Some
            Semicolon = context.semicolon |> Node.toTerminal tokens
        }
        |> Borrow

    override visitor.VisitBorrowBlock context =
        {
            BorrowKeyword = context.borrow |> Node.toTerminal tokens
            Binding = context.binding |> qubitBindingVisitor.Visit
            OpenParen = context.openParen |> function null -> None | s -> Node.toTerminal tokens s |> Some
            CloseParen = context.closeParen |> function null -> None | s -> Node.toTerminal tokens s |> Some
            Block =
                {
                    OpenBrace = context.body.openBrace |> Node.toTerminal tokens
                    Items = context.body._statements |> Seq.map visitor.Visit |> List.ofSeq
                    CloseBrace = context.body.closeBrace |> Node.toTerminal tokens
                }
        }
        |> BorrowBlock

    override _.VisitLetStatement context =
        {
            LetKeyword = context.``let`` |> Node.toTerminal tokens
            Binding = symbolBindingVisitor.Visit context.binding
            Equals = context.equals |> Node.toTerminal tokens
            Value = expressionVisitor.Visit context.value
            Semicolon = context.semicolon |> Node.toTerminal tokens
        }
        |> Let

    override visitor.VisitIfStatement context =
        {
            IfKeyword = context.``if`` |> Node.toTerminal tokens
            Condition = expressionVisitor.Visit context.condition
            Block =
                {
                    OpenBrace = context.body.openBrace |> Node.toTerminal tokens
                    Items = context.body._statements |> Seq.map visitor.Visit |> List.ofSeq
                    CloseBrace = context.body.closeBrace |> Node.toTerminal tokens
                }
        }
        |> If

    override visitor.VisitElseStatement context =
        {
            ElseKeyword = context.``else`` |> Node.toTerminal tokens
            Block =
                {
                    OpenBrace = context.body.openBrace |> Node.toTerminal tokens
                    Items = context.body._statements |> Seq.map visitor.Visit |> List.ofSeq
                    CloseBrace = context.body.closeBrace |> Node.toTerminal tokens
                }
        }
        |> Else
