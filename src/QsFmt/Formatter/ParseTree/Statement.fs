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
        context.discard |> Node.toTerminal tokens |> SymbolDeclaration

    override _.VisitSymbolName context =
        context.name |> Node.toTerminal tokens |> SymbolDeclaration

    override visitor.VisitSymbolTuple context =
        let bindings = context._bindings |> Seq.map visitor.Visit
        let commas = context._commas |> Seq.map (Node.toTerminal tokens)

        {
            OpenParen = context.openParen |> Node.toTerminal tokens
            Items = Node.tupleItems bindings commas
            CloseParen = context.closeParen |> Node.toTerminal tokens
        }
        |> SymbolTuple

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

    override visitor.VisitQubitTuple(context: QSharpParser.QubitTupleContext) =
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

    let symbolBindingVisitor = SymbolBindingVisitor tokens
    let qubitInitializerVistor = QubitInitializerVistor tokens

    override _.VisitQubitBinding context =
        {
            Name = context.binding |> symbolBindingVisitor.Visit
            Equals = context.equals |> Node.toTerminal tokens
            Initializer = context.value |> qubitInitializerVistor.Visit
        }

type ForBindingVisitor(tokens) =
    inherit QSharpParserBaseVisitor<ForBinding>()

    let symbolBindingVisitor = SymbolBindingVisitor tokens
    let expressionVisitor = ExpressionVisitor tokens

    override _.VisitForBinding context =
        {
            Name = context.binding |> symbolBindingVisitor.Visit
            In = context.``in`` |> Node.toTerminal tokens
            Value = context.value |> expressionVisitor.Visit
        }

type StatementVisitor(tokens) =
    inherit QSharpParserBaseVisitor<Statement>()

    let expressionVisitor = ExpressionVisitor tokens

    let symbolBindingVisitor = SymbolBindingVisitor tokens
    let qubitBindingVisitor = QubitBindingVisitor tokens
    let forBindingVisitor = ForBindingVisitor tokens

    override _.DefaultResult = failwith "Unknown statement."

    override _.VisitChildren node =
        Node.toUnknown tokens node |> Statement.Unknown

    override _.VisitExpressionStatement context =
        {
            Expression = context.value |> expressionVisitor.Visit
            Semicolon = context.semicolon |> Node.toTerminal tokens
        }
        |> ExpressionStatement

    override _.VisitReturnStatement context =
        {
            Keyword = context.``return`` |> Node.toTerminal tokens
            Expression = expressionVisitor.Visit context.value
            Semicolon = context.semicolon |> Node.toTerminal tokens
        }
        |> ReturnStatement

    override _.VisitFailStatement context =
        {
            Keyword = context.fail |> Node.toTerminal tokens
            Expression = expressionVisitor.Visit context.value
            Semicolon = context.semicolon |> Node.toTerminal tokens
        }
        |> FailStatement

    override _.VisitLetStatement context =
        {
            Keyword = context.``let`` |> Node.toTerminal tokens
            Binding = symbolBindingVisitor.Visit context.binding
            Equals = context.equals |> Node.toTerminal tokens
            Value = expressionVisitor.Visit context.value
            Semicolon = context.semicolon |> Node.toTerminal tokens
        }
        |> LetStatement

    override _.VisitMutableStatement context =
        {
            Keyword = context.``mutable`` |> Node.toTerminal tokens
            Binding = symbolBindingVisitor.Visit context.binding
            Equals = context.equals |> Node.toTerminal tokens
            Value = expressionVisitor.Visit context.value
            Semicolon = context.semicolon |> Node.toTerminal tokens
        }
        |> MutableStatement

    override _.VisitSetStatement context =
        {
            Keyword = context.set |> Node.toTerminal tokens
            Binding = symbolBindingVisitor.Visit context.binding
            Equals = context.equals |> Node.toTerminal tokens
            Value = expressionVisitor.Visit context.value
            Semicolon = context.semicolon |> Node.toTerminal tokens
        }
        |> SetStatement

    override _.VisitUpdateStatement context =
        {
            SetKeyword = context.set |> Node.toTerminal tokens
            Name = context.name |> Node.toTerminal tokens
            Operator =
                { Prefix = Node.prefix tokens context.operator.Start.TokenIndex; Text = context.operator.GetText() }
            Value = expressionVisitor.Visit context.value
            Semicolon = context.semicolon |> Node.toTerminal tokens
        }
        |> UpdateStatement

    override _.VisitUpdateWithStatement context =
        {
            SetKeyword = context.set |> Node.toTerminal tokens
            Name = context.name |> Node.toTerminal tokens
            With = context.``with`` |> Node.toTerminal tokens
            Item = expressionVisitor.Visit context.index
            Arrow = context.arrow |> Node.toTerminal tokens
            Value = expressionVisitor.Visit context.value
            Semicolon = context.semicolon |> Node.toTerminal tokens
        }
        |> UpdateWithStatement

    override visitor.VisitIfStatement context =
        {
            Keyword = context.``if`` |> Node.toTerminal tokens
            Condition = expressionVisitor.Visit context.condition
            Block =
                {
                    OpenBrace = context.body.openBrace |> Node.toTerminal tokens
                    Items = context.body._statements |> Seq.map visitor.Visit |> List.ofSeq
                    CloseBrace = context.body.closeBrace |> Node.toTerminal tokens
                }
        }
        |> IfStatement

    override visitor.VisitElifStatement context =
        {
            Keyword = context.``elif`` |> Node.toTerminal tokens
            Condition = expressionVisitor.Visit context.condition
            Block =
                {
                    OpenBrace = context.body.openBrace |> Node.toTerminal tokens
                    Items = context.body._statements |> Seq.map visitor.Visit |> List.ofSeq
                    CloseBrace = context.body.closeBrace |> Node.toTerminal tokens
                }
        }
        |> ElifStatement

    override visitor.VisitElseStatement context =
        {
            Keyword = context.``else`` |> Node.toTerminal tokens
            Block =
                {
                    OpenBrace = context.body.openBrace |> Node.toTerminal tokens
                    Items = context.body._statements |> Seq.map visitor.Visit |> List.ofSeq
                    CloseBrace = context.body.closeBrace |> Node.toTerminal tokens
                }
        }
        |> ElseStatement

    override visitor.VisitForStatement context =
        {
            ForKeyword = context.``for`` |> Node.toTerminal tokens
            OpenParen = context.openParen |> Option.ofObj |> Option.map (Node.toTerminal tokens)
            Binding = context.binding |> forBindingVisitor.Visit
            CloseParen = context.closeParen |> Option.ofObj |> Option.map (Node.toTerminal tokens)
            Block =
                {
                    OpenBrace = context.body.openBrace |> Node.toTerminal tokens
                    Items = context.body._statements |> Seq.map visitor.Visit |> List.ofSeq
                    CloseBrace = context.body.closeBrace |> Node.toTerminal tokens
                }
        }
        |> ForStatement

    override visitor.VisitWhileStatement context =
        {
            Keyword = context.``while`` |> Node.toTerminal tokens
            Condition = expressionVisitor.Visit context.condition
            Block =
                {
                    OpenBrace = context.body.openBrace |> Node.toTerminal tokens
                    Items = context.body._statements |> Seq.map visitor.Visit |> List.ofSeq
                    CloseBrace = context.body.closeBrace |> Node.toTerminal tokens
                }
        }
        |> WhileStatement

    override visitor.VisitRepeatStatement context =
        {
            Keyword = context.repeat |> Node.toTerminal tokens
            Block =
                {
                    OpenBrace = context.body.openBrace |> Node.toTerminal tokens
                    Items = context.body._statements |> Seq.map visitor.Visit |> List.ofSeq
                    CloseBrace = context.body.closeBrace |> Node.toTerminal tokens
                }
        }
        |> RepeatStatement

    override visitor.VisitUntilStatement context =
        {
            UntilKeyword = context.until |> Node.toTerminal tokens
            Condition = expressionVisitor.Visit context.condition
            Coda =
                if context.body <> null then
                    {
                        Keyword = context.fixup |> Node.toTerminal tokens
                        Block =
                            {
                                OpenBrace = context.body.openBrace |> Node.toTerminal tokens
                                Items = context.body._statements |> Seq.map visitor.Visit |> List.ofSeq
                                CloseBrace = context.body.closeBrace |> Node.toTerminal tokens
                            }
                    }
                    |> Fixup
                else
                    context.semicolon |> Node.toTerminal tokens |> UntilStatementCoda.Semicolon
        }
        |> UntilStatement

    override visitor.VisitWithinStatement context =
        {
            Keyword = context.within |> Node.toTerminal tokens
            Block =
                {
                    OpenBrace = context.body.openBrace |> Node.toTerminal tokens
                    Items = context.body._statements |> Seq.map visitor.Visit |> List.ofSeq
                    CloseBrace = context.body.closeBrace |> Node.toTerminal tokens
                }
        }
        |> WithinStatement

    override visitor.VisitApplyStatement context =
        {
            Keyword = context.apply |> Node.toTerminal tokens
            Block =
                {
                    OpenBrace = context.body.openBrace |> Node.toTerminal tokens
                    Items = context.body._statements |> Seq.map visitor.Visit |> List.ofSeq
                    CloseBrace = context.body.closeBrace |> Node.toTerminal tokens
                }
        }
        |> ApplyStatement

    override visitor.VisitQubitDeclaration context =
        {
            Kind =
                match context.keyword.Text with
                | "use"
                | "using" -> Use
                | _ -> Borrow
            Keyword = context.keyword |> Node.toTerminal tokens
            OpenParen = context.openParen |> Option.ofObj |> Option.map (Node.toTerminal tokens)
            Binding = context.binding |> qubitBindingVisitor.Visit
            CloseParen = context.closeParen |> Option.ofObj |> Option.map (Node.toTerminal tokens)
            Coda =
                if context.body <> null then
                    {
                        OpenBrace = context.body.openBrace |> Node.toTerminal tokens
                        Items = context.body._statements |> Seq.map visitor.Visit |> List.ofSeq
                        CloseBrace = context.body.closeBrace |> Node.toTerminal tokens
                    }
                    |> Block
                else
                    context.semicolon |> Node.toTerminal tokens |> Semicolon
        }
        |> QubitDeclarationStatement
