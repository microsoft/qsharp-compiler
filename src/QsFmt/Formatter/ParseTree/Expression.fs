// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.ParseTree

open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree
open Microsoft.Quantum.QsFmt.Parser

type ExpressionVisitor(tokens) =
    inherit QSharpParserBaseVisitor<Expression>()

    let typeVisitor = TypeVisitor tokens

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

    override visitor.VisitArrayExpression context =
        let expressions = context._items |> Seq.map visitor.Visit

        let commas = context._commas |> Seq.map (Node.toTerminal tokens)

        {
            OpenParen = context.openBracket |> Node.toTerminal tokens
            Items = Node.tupleItems expressions commas
            CloseParen = context.closeBracket |> Node.toTerminal tokens
        }
        |> Tuple

    override visitor.VisitNewArrayExpression context =
        {
            New = context.``new`` |> Node.toTerminal tokens
            ArrayType = typeVisitor.Visit context.arrayType
            OpenBracket = context.openBracket |> Node.toTerminal tokens
            Length = visitor.Visit context.length
            CloseBracket = context.closeBracket |> Node.toTerminal tokens
        }
        |> NewArray

    override visitor.VisitNamedItemAccessExpression context =
        {
            Object = visitor.Visit context.obj
            Colon = context.colon |> Node.toTerminal tokens
            Name = context.name |> Node.toTerminal tokens
        }
        |> NamedItemAccess

    override visitor.VisitArrayAccessExpression context =
        {
            Array = visitor.Visit context.array
            OpenBracket = context.openBracket |> Node.toTerminal tokens
            Index = visitor.Visit context.index
            CloseBracket = context.closeBracket |> Node.toTerminal tokens
        }
        |> ArrayAccess

    override visitor.VisitUnwrapExpression context =
        {
            Operand = visitor.Visit context.operand
            PostfixOperator = context.operator |> Node.toTerminal tokens
        }
        |> PostfixOperator

    override visitor.VisitControlledExpression context =
        {
            PrefixOperator = context.functor |> Node.toTerminal tokens
            Operand = visitor.Visit context.operation
        }
        |> PrefixOperator

    override visitor.VisitAdjointExpression context =
        {
            PrefixOperator = context.functor |> Node.toTerminal tokens
            Operand = visitor.Visit context.operation
        }
        |> PrefixOperator

    override visitor.VisitCallExpression context =
        let expressions = context._arguments |> Seq.map visitor.Visit

        let commas = context._commas |> Seq.map (Node.toTerminal tokens)

        {
            Function = visitor.Visit context.``fun``
            Arguments =
                {
                    OpenParen = context.openParen |> Node.toTerminal tokens
                    Items = Node.tupleItems expressions commas
                    CloseParen = context.closeParen |> Node.toTerminal tokens
                }
        }
        |> Call

    override visitor.VisitNegationExpression context =
        {
            PrefixOperator = context.operator |> Node.toTerminal tokens
            Operand = visitor.Visit context.operand
        }
        |> PrefixOperator

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
            Operator = context.ellipsis |> Node.toTerminal tokens
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

    override visitor.VisitRightOpenRangeExpression context =
        {
            Operand = visitor.Visit context.left
            PostfixOperator = context.ellipsis |> Node.toTerminal tokens
        }
        |> PostfixOperator

    override visitor.VisitLeftOpenRangeExpression context =
        {
            PrefixOperator = context.ellipsis |> Node.toTerminal tokens
            Operand = visitor.Visit context.right
        }
        |> PrefixOperator

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
