// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.ParseTree

open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree
open Microsoft.Quantum.QsFmt.Parser

module Expression =
    let toTypeArgs tokens (context: QSharpParser.TypeTupleContext) =
        let typeArgs = context._typeArgs |> Seq.map (TypeVisitor tokens).Visit

        let commas = context._commas |> Seq.map (Node.toTerminal tokens)

        {
            OpenParen = context.openBracket |> Node.toTerminal tokens
            Items = Node.tupleItems typeArgs commas
            CloseParen = context.closeBracket |> Node.toTerminal tokens
        }


type InterpStringContentVisitor(tokens) =
    inherit QSharpParserBaseVisitor<InterpStringContent>()

    override _.VisitInterpStringEscapeContent context =
        context.InterpStringEscape().Symbol |> Node.toTerminal tokens |> Text

    override _.VisitInterpExpressionContent context =
        {
            OpenBrace = context.openBrace |> Node.toTerminal tokens
            Expression = (ExpressionVisitor tokens).Visit context.exp
            CloseBrace = context.closeBrace |> Node.toTerminal tokens
        }
        |> Expression

    override _.VisitInterpTextContent context =
        context.InterpStringText().Symbol |> Node.toTerminal tokens |> Text

and ExpressionVisitor(tokens) =
    inherit QSharpParserBaseVisitor<Expression>()

    let typeVisitor = TypeVisitor tokens

    override _.DefaultResult = failwith "Unknown expression."

    override _.VisitChildren node =
        Node.toUnknown tokens node |> Expression.Unknown

    override _.VisitMissingExpression context =
        context.Underscore().Symbol |> Node.toTerminal tokens |> Missing

    override _.VisitIdentifierExpression context =
        {
            Name = { Prefix = Node.prefix tokens context.name.Start.TokenIndex; Text = context.name.GetText() }
            TypeArgs = Option.ofObj context.types |> Option.map (Expression.toTypeArgs tokens)
        }
        |> Identifier

    override _.VisitIntegerExpression context =
        context.value |> Node.toTerminal tokens |> Literal

    override _.VisitBigIntegerExpression context =
        context.value |> Node.toTerminal tokens |> Literal

    override _.VisitDoubleExpression context =
        context.value |> Node.toTerminal tokens |> Literal

    override _.VisitStringExpression context =
        { Prefix = Node.prefix tokens context.Start.TokenIndex; Text = context.GetText() } |> Literal

    override _.VisitInterpStringExpression context =
        {
            OpenQuote = context.openQuote |> Node.toTerminal tokens
            Content = context._content |> Seq.map ((InterpStringContentVisitor tokens).Visit) |> List.ofSeq
            CloseQuote = context.closeQuote |> Node.toTerminal tokens
        }
        |> InterpString

    override _.VisitBoolExpression context =
        { Prefix = Node.prefix tokens context.value.Start.TokenIndex; Text = context.value.GetText() }
        |> Literal

    override _.VisitResultExpression context =
        { Prefix = Node.prefix tokens context.value.Start.TokenIndex; Text = context.value.GetText() }
        |> Literal

    override _.VisitPauliExpression context =
        { Prefix = Node.prefix tokens context.value.Start.TokenIndex; Text = context.value.GetText() }
        |> Literal

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

    override visitor.VisitSizedArrayExpression context =
        {
            OpenBracket = context.openBracket |> Node.toTerminal tokens
            Value = visitor.Visit context.value
            Comma = context.comma |> Node.toTerminal tokens
            Size = context.size.terminal |> Node.toTerminal tokens
            Equals = context.equals |> Node.toTerminal tokens
            Length = visitor.Visit context.length
            CloseBracket = context.closeBracket |> Node.toTerminal tokens
        }
        |> NewSizedArray

    override visitor.VisitNewArrayExpression context =
        {
            New = context.``new`` |> Node.toTerminal tokens
            ItemType = typeVisitor.Visit context.itemType
            OpenBracket = context.openBracket |> Node.toTerminal tokens
            Length = visitor.Visit context.length
            CloseBracket = context.closeBracket |> Node.toTerminal tokens
        }
        |> NewArray

    override visitor.VisitNamedItemAccessExpression context =
        {
            Record = visitor.Visit context.record
            DoubleColon = context.colon |> Node.toTerminal tokens
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
        { Operand = visitor.Visit context.operand; PostfixOperator = context.operator |> Node.toTerminal tokens }
        |> PostfixOperator

    override visitor.VisitControlledExpression context =
        { PrefixOperator = context.functor |> Node.toTerminal tokens; Operand = visitor.Visit context.operation }
        |> PrefixOperator

    override visitor.VisitAdjointExpression context =
        { PrefixOperator = context.functor |> Node.toTerminal tokens; Operand = visitor.Visit context.operation }
        |> PrefixOperator

    override visitor.VisitCallExpression context =
        let expressions = context._arguments |> Seq.map visitor.Visit

        let commas = context._commas |> Seq.map (Node.toTerminal tokens)

        {
            Callable = visitor.Visit context.callable
            Arguments =
                {
                    OpenParen = context.openParen |> Node.toTerminal tokens
                    Items = Node.tupleItems expressions commas
                    CloseParen = context.closeParen |> Node.toTerminal tokens
                }
        }
        |> Call

    override visitor.VisitPrefixOpExpression context =
        { PrefixOperator = context.operator |> Node.toTerminal tokens; Operand = visitor.Visit context.operand }
        |> PrefixOperator

    override visitor.VisitExponentExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitMultiplyExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitAddExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitShiftExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitCompareExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitEqualsExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitBitwiseAndExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitBitwiseXorExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitBitwiseOrExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitAndExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitOrExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.operator |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitRangeExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.ellipsis |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

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
        { Operand = visitor.Visit context.left; PostfixOperator = context.ellipsis |> Node.toTerminal tokens }
        |> PostfixOperator

    override visitor.VisitLeftOpenRangeExpression context =
        { PrefixOperator = context.ellipsis |> Node.toTerminal tokens; Operand = visitor.Visit context.right }
        |> PrefixOperator

    override _.VisitOpenRangeExpression context =
        context.Ellipsis().Symbol |> Node.toTerminal tokens |> FullOpenRange

    override visitor.VisitUpdateExpression context =
        {
            Record = visitor.Visit context.record
            With = context.``with`` |> Node.toTerminal tokens
            Item = visitor.Visit context.item
            Arrow = context.arrow |> Node.toTerminal tokens
            Value = visitor.Visit context.value
        }
        |> Update
