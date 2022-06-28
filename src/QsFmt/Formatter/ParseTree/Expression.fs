// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.ParseTree

open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree
open Microsoft.Quantum.QsFmt.Parser

module Expression =
    let toTypeArgs tokens (context: QSharpParser.TypeTupleContext) =
        let typeArgs = context.``type`` () |> Seq.map (TypeVisitor tokens).Visit
        let commas = context.Comma() |> Seq.map (fun node -> Node.toTerminal tokens node.Symbol)

        {
            OpenParen = context.Less().Symbol |> Node.toTerminal tokens
            Items = Node.tupleItems typeArgs commas
            CloseParen = context.Greater().Symbol |> Node.toTerminal tokens
        }

type SymbolBindingVisitor(tokens) =
    inherit QSharpParserBaseVisitor<SymbolBinding>()

    override _.DefaultResult = failwith "Unknown symbol binding."

    override _.VisitDiscardSymbol context =
        context.Underscore().Symbol |> Node.toTerminal tokens |> SymbolDeclaration

    override _.VisitSymbolName context =
        context.Identifier().Symbol |> Node.toTerminal tokens |> SymbolDeclaration

    override visitor.VisitSymbolTuple context =
        let bindings = context.symbolBinding () |> Seq.map visitor.Visit
        let commas = context.Comma() |> Seq.map (fun node -> Node.toTerminal tokens node.Symbol)

        {
            OpenParen = context.ParenLeft().Symbol |> Node.toTerminal tokens
            Items = Node.tupleItems bindings commas
            CloseParen = context.ParenRight().Symbol |> Node.toTerminal tokens
        }
        |> SymbolTuple

type InterpStringContentVisitor(tokens) =
    inherit QSharpParserBaseVisitor<InterpStringContent>()

    override _.VisitInterpStringEscapeContent context =
        context.InterpStringEscape().Symbol |> Node.toTerminal tokens |> Text

    override _.VisitInterpExpressionContent context =
        {
            OpenBrace = context.InterpBraceLeft().Symbol |> Node.toTerminal tokens
            Expression = context.expression () |> (ExpressionVisitor tokens).Visit
            CloseBrace = context.BraceRight().Symbol |> Node.toTerminal tokens
        }
        |> Expression

    override _.VisitInterpTextContent context =
        context.InterpStringText().Symbol |> Node.toTerminal tokens |> Text

and ExpressionVisitor(tokens) =
    inherit QSharpParserBaseVisitor<Expression>()

    let typeVisitor = TypeVisitor tokens
    let symbolBindingVisitor = SymbolBindingVisitor tokens

    override _.DefaultResult = failwith "Unknown expression."

    override _.VisitChildren node =
        Node.toUnknown tokens node |> Expression.Unknown

    override _.VisitMissingExpression context =
        context.Underscore().Symbol |> Node.toTerminal tokens |> Missing

    override _.VisitIdentifierExpression context =
        let name = context.qualifiedName ()

        {
            Name = { Prefix = Node.prefix tokens name.Start.TokenIndex; Text = name.GetText() }
            TypeArgs = context.typeTuple () |> Option.ofObj |> Option.map (Expression.toTypeArgs tokens)
        }
        |> Identifier

    override _.VisitIntegerExpression context =
        context.IntegerLiteral().Symbol |> Node.toTerminal tokens |> Literal

    override _.VisitBigIntegerExpression context =
        context.BigIntegerLiteral().Symbol |> Node.toTerminal tokens |> Literal

    override _.VisitDoubleExpression context =
        context.DoubleLiteral().Symbol |> Node.toTerminal tokens |> Literal

    override _.VisitStringExpression context =
        { Prefix = Node.prefix tokens context.Start.TokenIndex; Text = context.GetText() } |> Literal

    override _.VisitInterpStringExpression context =
        {
            OpenQuote = context.DollarQuote().Symbol |> Node.toTerminal tokens
            Content = context.interpStringContent () |> Seq.map (InterpStringContentVisitor tokens).Visit |> List.ofSeq
            CloseQuote = context.InterpDoubleQuote().Symbol |> Node.toTerminal tokens
        }
        |> InterpString

    override _.VisitBoolExpression context =
        let literal = context.boolLiteral ()
        { Prefix = Node.prefix tokens literal.Start.TokenIndex; Text = literal.GetText() } |> Literal

    override _.VisitResultExpression context =
        let literal = context.resultLiteral ()
        { Prefix = Node.prefix tokens literal.Start.TokenIndex; Text = literal.GetText() } |> Literal

    override _.VisitPauliExpression context =
        let literal = context.pauliLiteral ()
        { Prefix = Node.prefix tokens literal.Start.TokenIndex; Text = literal.GetText() } |> Literal

    override visitor.VisitTupleExpression context =
        let expressions = context.expression () |> Seq.map visitor.Visit
        let commas = context.Comma() |> Seq.map (fun node -> Node.toTerminal tokens node.Symbol)

        {
            OpenParen = context.ParenLeft().Symbol |> Node.toTerminal tokens
            Items = Node.tupleItems expressions commas
            CloseParen = context.ParenRight().Symbol |> Node.toTerminal tokens
        }
        |> Tuple

    override visitor.VisitArrayExpression context =
        let expressions = context.expression () |> Seq.map visitor.Visit
        let commas = context.Comma() |> Seq.map (fun node -> Node.toTerminal tokens node.Symbol)

        {
            OpenParen = context.BracketLeft().Symbol |> Node.toTerminal tokens
            Items = Node.tupleItems expressions commas
            CloseParen = context.BracketRight().Symbol |> Node.toTerminal tokens
        }
        |> Tuple

    override visitor.VisitSizedArrayExpression context =
        {
            OpenBracket = context.BracketLeft().Symbol |> Node.toTerminal tokens
            Value = visitor.Visit context.value
            Comma = context.Comma().Symbol |> Node.toTerminal tokens
            Size = context.size().Identifier().Symbol |> Node.toTerminal tokens
            Equals = context.Equal().Symbol |> Node.toTerminal tokens
            Length = visitor.Visit context.length
            CloseBracket = context.BracketRight().Symbol |> Node.toTerminal tokens
        }
        |> NewSizedArray

    override visitor.VisitNewArrayExpression context =
        {
            New = context.New().Symbol |> Node.toTerminal tokens
            ItemType = context.``type`` () |> typeVisitor.Visit
            OpenBracket = context.BracketLeft().Symbol |> Node.toTerminal tokens
            Length = visitor.Visit context.length
            CloseBracket = context.BracketRight().Symbol |> Node.toTerminal tokens
        }
        |> NewArray

    override visitor.VisitNamedItemAccessExpression context =
        {
            Record = context.expression () |> visitor.Visit
            DoubleColon = context.DoubleColon().Symbol |> Node.toTerminal tokens
            Name = context.Identifier().Symbol |> Node.toTerminal tokens
        }
        |> NamedItemAccess

    override visitor.VisitArrayAccessExpression context =
        {
            Array = visitor.Visit context.array
            OpenBracket = context.BracketLeft().Symbol |> Node.toTerminal tokens
            Index = visitor.Visit context.index
            CloseBracket = context.BracketRight().Symbol |> Node.toTerminal tokens
        }
        |> ArrayAccess

    override visitor.VisitUnwrapExpression context =
        {
            Operand = context.expression () |> visitor.Visit
            PostfixOperator = context.Bang().Symbol |> Node.toTerminal tokens
        }
        |> PostfixOperator

    override visitor.VisitControlledExpression context =
        {
            PrefixOperator = context.ControlledFunctor().Symbol |> Node.toTerminal tokens
            Operand = context.expression () |> visitor.Visit
        }
        |> PrefixOperator

    override visitor.VisitAdjointExpression context =
        {
            PrefixOperator = context.AdjointFunctor().Symbol |> Node.toTerminal tokens
            Operand = context.expression () |> visitor.Visit
        }
        |> PrefixOperator

    override visitor.VisitCallExpression context =
        let expressions = context._args |> Seq.map visitor.Visit
        let commas = context.Comma() |> Seq.map (fun node -> Node.toTerminal tokens node.Symbol)

        {
            Callable = visitor.Visit context.callable
            Arguments =
                {
                    OpenParen = context.ParenLeft().Symbol |> Node.toTerminal tokens
                    Items = Node.tupleItems expressions commas
                    CloseParen = context.ParenRight().Symbol |> Node.toTerminal tokens
                }
        }
        |> Call

    override visitor.VisitPrefixOpExpression context =
        { PrefixOperator = context.op |> Node.toTerminal tokens; Operand = context.expression () |> visitor.Visit }
        |> PrefixOperator

    override visitor.VisitExponentExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.Caret().Symbol |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitMultiplyExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.op |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitAddExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.op |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitShiftExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.op |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitCompareExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.op |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitEqualsExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.op |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitBitwiseAndExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.TripleAmpersand().Symbol |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitBitwiseXorExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.TripleCaret().Symbol |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitBitwiseOrExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.TriplePipe().Symbol |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitAndExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.op |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitOrExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.op |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitRangeExpression context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.DoubleDot().Symbol |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> InfixOperator

    override visitor.VisitConditionalExpression context =
        {
            Condition = visitor.Visit context.cond
            Question = context.Question().Symbol |> Node.toTerminal tokens
            IfTrue = visitor.Visit context.``then``
            Pipe = context.Pipe().Symbol |> Node.toTerminal tokens
            IfFalse = visitor.Visit context.``else``
        }
        |> Conditional

    override visitor.VisitRightOpenRangeExpression context =
        {
            Operand = context.expression () |> visitor.Visit
            PostfixOperator = context.Ellipsis().Symbol |> Node.toTerminal tokens
        }
        |> PostfixOperator

    override visitor.VisitLeftOpenRangeExpression context =
        {
            PrefixOperator = context.Ellipsis().Symbol |> Node.toTerminal tokens
            Operand = context.expression () |> visitor.Visit
        }
        |> PrefixOperator

    override _.VisitOpenRangeExpression context =
        context.Ellipsis().Symbol |> Node.toTerminal tokens |> FullOpenRange

    override visitor.VisitUpdateExpression context =
        {
            Record = visitor.Visit context.record
            With = context.With().Symbol |> Node.toTerminal tokens
            Item = visitor.Visit context.index
            Arrow = context.ArrowLeft().Symbol |> Node.toTerminal tokens
            Value = visitor.Visit context.value
        }
        |> Update

    override visitor.VisitLambdaExpression context =
        {
            Binding = context.symbolBinding () |> symbolBindingVisitor.Visit
            Arrow = Node.toTerminal tokens context.arrow
            Body = context.expression () |> visitor.Visit
        }
        |> Lambda
