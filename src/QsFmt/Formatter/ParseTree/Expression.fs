﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.ParseTree

open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree
open Microsoft.Quantum.QsFmt.Parser

type ExpressionVisitor(tokens) =
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

    override _.VisitBigIntegerExpression context =
        context.value |> Node.toTerminal tokens |> Literal

    override _.VisitDoubleExpression context =
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

    override visitor.VisitExponentExpression context =
        {
            Left = visitor.Visit context.left
            Operator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> BinaryOperator

    override visitor.VisitMultiplyExpression context =
        {
            Left = visitor.Visit context.left
            Operator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> BinaryOperator

    override visitor.VisitAddExpression context =
        {
            Left = visitor.Visit context.left
            Operator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> BinaryOperator

    override visitor.VisitShiftExpression context =
        {
            Left = visitor.Visit context.left
            Operator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> BinaryOperator

    override visitor.VisitCompareExpression context =
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

    override visitor.VisitBitwiseAndExpression context =
        {
            Left = visitor.Visit context.left
            Operator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> BinaryOperator

    override visitor.VisitBitwiseXorExpression context =
        {
            Left = visitor.Visit context.left
            Operator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> BinaryOperator

    override visitor.VisitBitwiseOrExpression context =
        {
            Left = visitor.Visit context.left
            Operator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> BinaryOperator

    override visitor.VisitAndExpression context =
        {
            Left = visitor.Visit context.left
            Operator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> BinaryOperator

    override visitor.VisitOrExpression context =
        {
            Left = visitor.Visit context.left
            Operator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> BinaryOperator

    override visitor.VisitRangeExpression context =
        {
            Left = visitor.Visit context.left
            Operator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> BinaryOperator

     override visitor.VisitConditionalExpression context =
        {
            Condition = visitor.Visit context.cond
            Question = context.question |> Node.toTerminal tokens
            IfTrue = visitor.Visit context.ifTrue
            Pipe = context.pipe |> Node.toTerminal tokens
            IfFalse = visitor.Visit context.ifFalse
        }
        |> Conditional

    override _.VisitOpenRangeExpression context =
        context.Ellipsis().Symbol |> Node.toTerminal tokens |> Missing

    override visitor.VisitUpdateExpression context =
        {
            Record = visitor.Visit context.record
            With = context.``with`` |> Node.toTerminal tokens
            Item = visitor.Visit context.item
            Arrow = context.arrow |> Node.toTerminal tokens
            Value = visitor.Visit context.value
        }
        |> Update
