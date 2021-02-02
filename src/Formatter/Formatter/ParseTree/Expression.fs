// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.ParseTree

open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree
open Microsoft.Quantum.QsFmt.Parser

/// <summary>
/// Creates syntax tree <see cref="Expression"/> nodes from a parse tree and the list of tokens.
/// </summary>
type internal ExpressionVisitor(tokens) =
    inherit QSharpParserBaseVisitor<Expression>()

    override _.DefaultResult = failwith "Unknown expression."

    override _.VisitChildren node =
        Node.toUnknown tokens node |> Expression.Unknown

    override _.VisitMissingExpression context =
        context.Underscore().Symbol |> Node.toTerminal tokens |> Missing

    override _.VisitIdentifierExpression context =
        { Prefix = Node.prefix tokens context.name.Start.TokenIndex; Text = context.name.GetText() }
        |> Literal

    override _.VisitIntegerExpression context =
        context.value |> Node.toTerminal tokens |> Literal

    override visitor.VisitTupleExpression context =
        let expressions = context._items |> Seq.map visitor.Visit

        let commas = context._commas |> Seq.map (Node.toTerminal tokens)

        {
            OpenParen = context.openParen |> Node.toTerminal tokens
            Items = Node.tupleItems expressions commas
            CloseParen = context.closeParen |> Node.toTerminal tokens
        }
        |> Tuple

    override visitor.VisitAddExpression context =
        {
            Left = visitor.Visit context.left
            Operator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> BinaryOperator

    override visitor.VisitEqualsExpression context =
        {
            Left = visitor.Visit context.left
            Operator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> BinaryOperator

    override visitor.VisitUpdateExpression context =
        {
            Record = visitor.Visit context.record
            With = context.``with`` |> Node.toTerminal tokens
            Item = visitor.Visit context.item
            Arrow = context.arrow |> Node.toTerminal tokens
            Value = visitor.Visit context.value
        }
        |> Update
