// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.ParseTree

open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree
open Microsoft.Quantum.QsFmt.Parser

type QubitInitializerVisitor(tokens) =
    inherit QSharpParserBaseVisitor<QubitInitializer>()

    let expressionVisitor = ExpressionVisitor tokens

    override _.VisitSingleQubit context =
        {
            Qubit = context.Qubit().Symbol |> Node.toTerminal tokens
            OpenParen = context.ParenLeft().Symbol |> Node.toTerminal tokens
            CloseParen = context.ParenRight().Symbol |> Node.toTerminal tokens
        }
        |> SingleQubit

    override _.VisitQubitArray context =
        {
            Qubit = context.Qubit().Symbol |> Node.toTerminal tokens
            OpenBracket = context.BracketLeft().Symbol |> Node.toTerminal tokens
            Length = expressionVisitor.Visit context.length
            CloseBracket = context.BracketRight().Symbol |> Node.toTerminal tokens
        }
        |> QubitArray

    override visitor.VisitQubitTuple(context: QSharpParser.QubitTupleContext) =
        let initializers = context.qubitInitializer () |> Seq.map visitor.Visit
        let commas = context.Comma() |> Seq.map (fun node -> Node.toTerminal tokens node.Symbol)

        {
            OpenParen = context.ParenLeft().Symbol |> Node.toTerminal tokens
            Items = Node.tupleItems initializers commas
            CloseParen = context.ParenRight().Symbol |> Node.toTerminal tokens
        }
        |> QubitTuple

type QubitBindingVisitor(tokens) =
    inherit QSharpParserBaseVisitor<QubitBinding>()

    let symbolBindingVisitor = SymbolBindingVisitor tokens
    let qubitInitializerVisitor = QubitInitializerVisitor tokens

    override _.VisitQubitBinding context =
        {
            Name = context.symbolBinding () |> symbolBindingVisitor.Visit
            Equals = context.Equal().Symbol |> Node.toTerminal tokens
            Initializer = context.qubitInitializer () |> qubitInitializerVisitor.Visit
        }

type ForBindingVisitor(tokens) =
    inherit QSharpParserBaseVisitor<ForBinding>()

    let symbolBindingVisitor = SymbolBindingVisitor tokens
    let expressionVisitor = ExpressionVisitor tokens

    override _.VisitForBinding context =
        {
            Name = context.symbolBinding () |> symbolBindingVisitor.Visit
            In = context.In().Symbol |> Node.toTerminal tokens
            Value = context.expression () |> expressionVisitor.Visit
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
            Expression = context.expression () |> expressionVisitor.Visit
            Semicolon = context.Semicolon().Symbol |> Node.toTerminal tokens
        }
        |> ExpressionStatement

    override _.VisitReturnStatement context =
        {
            Keyword = context.Return().Symbol |> Node.toTerminal tokens
            Expression = context.expression () |> expressionVisitor.Visit
            Semicolon = context.Semicolon().Symbol |> Node.toTerminal tokens
        }
        |> ReturnStatement

    override _.VisitFailStatement context =
        {
            Keyword = context.Fail().Symbol |> Node.toTerminal tokens
            Expression = context.expression () |> expressionVisitor.Visit
            Semicolon = context.Semicolon().Symbol |> Node.toTerminal tokens
        }
        |> FailStatement

    override _.VisitLetStatement context =
        {
            Keyword = context.Let().Symbol |> Node.toTerminal tokens
            Binding = context.symbolBinding () |> symbolBindingVisitor.Visit
            Equals = context.Equal().Symbol |> Node.toTerminal tokens
            Value = context.expression () |> expressionVisitor.Visit
            Semicolon = context.Semicolon().Symbol |> Node.toTerminal tokens
        }
        |> LetStatement

    override _.VisitMutableStatement context =
        {
            Keyword = context.Mutable().Symbol |> Node.toTerminal tokens
            Binding = context.symbolBinding () |> symbolBindingVisitor.Visit
            Equals = context.Equal().Symbol |> Node.toTerminal tokens
            Value = context.expression () |> expressionVisitor.Visit
            Semicolon = context.Semicolon().Symbol |> Node.toTerminal tokens
        }
        |> MutableStatement

    override _.VisitSetStatement context =
        {
            Keyword = context.Set().Symbol |> Node.toTerminal tokens
            Binding = context.symbolBinding () |> symbolBindingVisitor.Visit
            Equals = context.Equal().Symbol |> Node.toTerminal tokens
            Value = context.expression () |> expressionVisitor.Visit
            Semicolon = context.Semicolon().Symbol |> Node.toTerminal tokens
        }
        |> SetStatement

    override _.VisitUpdateStatement context =
        let operator = context.updateOperator ()

        {
            SetKeyword = context.Set().Symbol |> Node.toTerminal tokens
            Name = context.Identifier().Symbol |> Node.toTerminal tokens
            Operator = { Prefix = Node.prefix tokens operator.Start.TokenIndex; Text = operator.GetText() }
            Value = context.expression () |> expressionVisitor.Visit
            Semicolon = context.Semicolon().Symbol |> Node.toTerminal tokens
        }
        |> UpdateStatement

    override _.VisitUpdateWithStatement context =
        {
            SetKeyword = context.Set().Symbol |> Node.toTerminal tokens
            Name = context.Identifier().Symbol |> Node.toTerminal tokens
            With = context.WithEqual().Symbol |> Node.toTerminal tokens
            Item = expressionVisitor.Visit context.index
            Arrow = context.ArrowLeft().Symbol |> Node.toTerminal tokens
            Value = expressionVisitor.Visit context.value
            Semicolon = context.Semicolon().Symbol |> Node.toTerminal tokens
        }
        |> UpdateWithStatement

    override visitor.VisitIfStatement context =
        {
            Keyword = context.If().Symbol |> Node.toTerminal tokens
            Condition = context.expression () |> expressionVisitor.Visit
            Block = context.scope () |> visitor.CreateBlock
        }
        |> IfStatement

    override visitor.VisitElifStatement context =
        {
            Keyword = context.Elif().Symbol |> Node.toTerminal tokens
            Condition = context.expression () |> expressionVisitor.Visit
            Block = context.scope () |> visitor.CreateBlock
        }
        |> ElifStatement

    override visitor.VisitElseStatement context =
        { Keyword = context.Else().Symbol |> Node.toTerminal tokens; Block = context.scope () |> visitor.CreateBlock }
        |> ElseStatement

    override visitor.VisitForStatement context =
        {
            ForKeyword = context.For().Symbol |> Node.toTerminal tokens
            OpenParen =
                context.ParenLeft() |> Option.ofObj |> Option.map (fun node -> Node.toTerminal tokens node.Symbol)
            Binding = context.forBinding () |> forBindingVisitor.Visit
            CloseParen =
                context.ParenRight() |> Option.ofObj |> Option.map (fun node -> Node.toTerminal tokens node.Symbol)
            Block = context.scope () |> visitor.CreateBlock
        }
        |> ForStatement

    override visitor.VisitWhileStatement context =
        {
            Keyword = context.While().Symbol |> Node.toTerminal tokens
            Condition = context.expression () |> expressionVisitor.Visit
            Block = context.scope () |> visitor.CreateBlock
        }
        |> WhileStatement

    override visitor.VisitRepeatStatement context =
        { Keyword = context.Repeat().Symbol |> Node.toTerminal tokens; Block = context.scope () |> visitor.CreateBlock }
        |> RepeatStatement

    override visitor.VisitUntilStatement context =
        let scope = context.scope ()

        let coda =
            if isNull scope then
                context.Semicolon().Symbol |> Node.toTerminal tokens |> UntilStatementCoda.Semicolon
            else
                { Keyword = context.Fixup().Symbol |> Node.toTerminal tokens; Block = visitor.CreateBlock scope }
                |> Fixup

        {
            UntilKeyword = context.Until().Symbol |> Node.toTerminal tokens
            Condition = context.expression () |> expressionVisitor.Visit
            Coda = coda
        }
        |> UntilStatement

    override visitor.VisitWithinStatement context =
        { Keyword = context.Within().Symbol |> Node.toTerminal tokens; Block = context.scope () |> visitor.CreateBlock }
        |> WithinStatement

    override visitor.VisitApplyStatement context =
        { Keyword = context.Apply().Symbol |> Node.toTerminal tokens; Block = context.scope () |> visitor.CreateBlock }
        |> ApplyStatement

    override visitor.VisitQubitDeclaration context =
        let scope = context.scope ()

        let coda =
            if isNull scope then
                context.Semicolon().Symbol |> Node.toTerminal tokens |> Semicolon
            else
                visitor.CreateBlock scope |> Block

        {
            Kind =
                match context.keyword.Text with
                | "use"
                | "using" -> Use
                | _ -> Borrow
            Keyword = context.keyword |> Node.toTerminal tokens
            OpenParen =
                context.ParenLeft() |> Option.ofObj |> Option.map (fun node -> Node.toTerminal tokens node.Symbol)
            Binding = context.qubitBinding () |> qubitBindingVisitor.Visit
            CloseParen =
                context.ParenRight() |> Option.ofObj |> Option.map (fun node -> Node.toTerminal tokens node.Symbol)
            Coda = coda
        }
        |> QubitDeclarationStatement

    member internal visitor.CreateBlock(scope: QSharpParser.ScopeContext) =
        {
            OpenBrace = scope.BraceLeft().Symbol |> Node.toTerminal tokens
            Items = scope.statement () |> Seq.map visitor.Visit |> List.ofSeq
            CloseBrace = scope.BraceRight().Symbol |> Node.toTerminal tokens
        }
