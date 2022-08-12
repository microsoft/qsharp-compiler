// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

open System
open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Transformations.Core.Utils

type StatementKindTransformationBase(statementTransformation: _ -> StatementTransformationBase, options) =
    let node = if options.Rebuild then Fold else Walk

    member _.Types = statementTransformation().Expressions.Types

    member _.Expressions = statementTransformation().Expressions

    member _.Statements = statementTransformation ()

    member internal _.Common = statementTransformation().Expressions.Types.Common

    // TODO: RELEASE 2022-09: Remove member.
    [<Obsolete("Please use StatementKindTransformationBase(unit -> StatementTransformationBase, TransformationOptions) instead.")>]
    new(statementTransformation,
        expressionTransformation: unit -> ExpressionTransformationBase,
        options: TransformationOptions) = StatementKindTransformationBase(statementTransformation, options)

    new(options) as this =
        let expressions = ExpressionTransformationBase options
        let statements = StatementTransformationBase((fun () -> this), (fun () -> expressions), options)
        StatementKindTransformationBase((fun () -> statements), options)

    new(statementTransformation) =
        StatementKindTransformationBase(statementTransformation, TransformationOptions.Default)

    // TODO: RELEASE 2022-09: Remove member.
    [<Obsolete("Please use StatementKindTransformationBase(unit -> StatementTransformationBase) instead.")>]
    new(statementTransformation, expressionTransformation: unit -> ExpressionTransformationBase) =
        StatementKindTransformationBase(statementTransformation, TransformationOptions.Default)

    new() = StatementKindTransformationBase TransformationOptions.Default

    // subconstructs used within statements

    abstract OnSymbolTuple: SymbolTuple -> SymbolTuple

    default this.OnSymbolTuple syms =
        match syms with
        | VariableNameTuple tuple ->
            tuple |> Seq.map this.OnSymbolTuple |> ImmutableArray.CreateRange |> VariableNameTuple
        | VariableName name -> this.Common.OnLocalNameDeclaration name |> VariableName
        | DiscardedItem
        | InvalidItem -> syms

    abstract OnQubitInitializer: ResolvedInitializer -> ResolvedInitializer

    default this.OnQubitInitializer init =
        let transformed =
            match init.Resolution with
            | SingleQubitAllocation -> SingleQubitAllocation
            | QubitRegisterAllocation ex as orig ->
                QubitRegisterAllocation |> node.BuildOr orig (this.Expressions.OnTypedExpression ex)
            | QubitTupleAllocation is as orig ->
                QubitTupleAllocation
                |> node.BuildOr orig (is |> Seq.map this.OnQubitInitializer |> ImmutableArray.CreateRange)
            | InvalidInitializer -> InvalidInitializer

        ResolvedInitializer.New |> node.BuildOr init transformed

    abstract OnPositionedBlock:
        QsNullable<TypedExpression> * QsPositionedBlock -> QsNullable<TypedExpression> * QsPositionedBlock

    default this.OnPositionedBlock(intro: QsNullable<TypedExpression>, block: QsPositionedBlock) =
        let location = this.Common.OnRelativeLocation block.Location
        let comments = block.Comments
        let expr = intro |> QsNullable<_>.Map this.Expressions.OnTypedExpression
        let body = this.Statements.OnScope block.Body

        let positionedBlock (expr, body, location, comments) =
            expr, QsPositionedBlock.New comments location body

        positionedBlock |> node.BuildOr (intro, block) (expr, body, location, comments)

    // statements containing subconstructs or expressions

    abstract OnVariableDeclaration: QsBinding<TypedExpression> -> QsStatementKind

    default this.OnVariableDeclaration stm =
        let rhs = this.Expressions.OnTypedExpression stm.Rhs
        let lhs = this.OnSymbolTuple stm.Lhs

        QsVariableDeclaration << QsBinding<TypedExpression>.New stm.Kind
        |> node.BuildOr EmptyStatement (lhs, rhs)

    abstract OnValueUpdate: QsValueUpdate -> QsStatementKind

    default this.OnValueUpdate stm =
        let rhs = this.Expressions.OnTypedExpression stm.Rhs
        let lhs = this.Expressions.OnTypedExpression stm.Lhs
        QsValueUpdate << QsValueUpdate.New |> node.BuildOr EmptyStatement (lhs, rhs)

    abstract OnConditionalStatement: QsConditionalStatement -> QsStatementKind

    default this.OnConditionalStatement stm =
        let cases =
            stm.ConditionalBlocks
            |> Seq.map (fun (c, b) ->
                let cond, block = this.OnPositionedBlock(Value c, b)

                let invalidCondition () =
                    failwith "missing condition in if-statement"

                cond.ValueOrApply invalidCondition, block)
            |> ImmutableArray.CreateRange

        let defaultCase = stm.Default |> QsNullable<_>.Map (fun b -> this.OnPositionedBlock(Null, b) |> snd)

        QsConditionalStatement << QsConditionalStatement.New
        |> node.BuildOr EmptyStatement (cases, defaultCase)

    abstract OnForStatement: QsForStatement -> QsStatementKind

    default this.OnForStatement stm =
        let iterVals = this.Expressions.OnTypedExpression stm.IterationValues
        let loopVar = fst stm.LoopItem |> this.OnSymbolTuple
        let loopVarType = this.Types.OnType(snd stm.LoopItem)
        let body = this.Statements.OnScope stm.Body

        QsForStatement << QsForStatement.New
        |> node.BuildOr EmptyStatement ((loopVar, loopVarType), iterVals, body)

    abstract OnWhileStatement: QsWhileStatement -> QsStatementKind

    default this.OnWhileStatement stm =
        let condition = this.Expressions.OnTypedExpression stm.Condition
        let body = this.Statements.OnScope stm.Body
        QsWhileStatement << QsWhileStatement.New |> node.BuildOr EmptyStatement (condition, body)

    abstract OnRepeatStatement: QsRepeatStatement -> QsStatementKind

    default this.OnRepeatStatement stm =
        let repeatBlock = this.OnPositionedBlock(Null, stm.RepeatBlock) |> snd
        let successCondition, fixupBlock = this.OnPositionedBlock(Value stm.SuccessCondition, stm.FixupBlock)

        let invalidCondition () =
            failwith "missing success condition in repeat-statement"

        QsRepeatStatement << QsRepeatStatement.New
        |> node.BuildOr EmptyStatement (repeatBlock, successCondition.ValueOrApply invalidCondition, fixupBlock)

    abstract OnConjugation: QsConjugation -> QsStatementKind

    default this.OnConjugation stm =
        let outer = this.OnPositionedBlock(Null, stm.OuterTransformation) |> snd
        let inner = this.OnPositionedBlock(Null, stm.InnerTransformation) |> snd
        QsConjugation << QsConjugation.New |> node.BuildOr EmptyStatement (outer, inner)

    abstract OnExpressionStatement: TypedExpression -> QsStatementKind

    default this.OnExpressionStatement ex =
        let transformed = this.Expressions.OnTypedExpression ex
        QsExpressionStatement |> node.BuildOr EmptyStatement transformed

    abstract OnReturnStatement: TypedExpression -> QsStatementKind

    default this.OnReturnStatement ex =
        let transformed = this.Expressions.OnTypedExpression ex
        QsReturnStatement |> node.BuildOr EmptyStatement transformed

    abstract OnFailStatement: TypedExpression -> QsStatementKind

    default this.OnFailStatement ex =
        let transformed = this.Expressions.OnTypedExpression ex
        QsFailStatement |> node.BuildOr EmptyStatement transformed

    /// This method is defined for the sole purpose of eliminating code duplication for each of the specialization kinds.
    /// It is hence not intended and should never be needed for public use.
    member private this.OnQubitScopeKind(stm: QsQubitScope) =
        let kind = stm.Kind
        let rhs = this.OnQubitInitializer stm.Binding.Rhs
        let lhs = this.OnSymbolTuple stm.Binding.Lhs
        let body = this.Statements.OnScope stm.Body
        QsQubitScope << QsQubitScope.New kind |> node.BuildOr EmptyStatement ((lhs, rhs), body)

    abstract OnAllocateQubits: QsQubitScope -> QsStatementKind
    default this.OnAllocateQubits stm = this.OnQubitScopeKind stm

    abstract OnBorrowQubits: QsQubitScope -> QsStatementKind
    default this.OnBorrowQubits stm = this.OnQubitScopeKind stm

    abstract OnQubitScope: QsQubitScope -> QsStatementKind

    default this.OnQubitScope(stm: QsQubitScope) =
        match stm.Kind with
        | Allocate -> this.OnAllocateQubits stm
        | Borrow -> this.OnBorrowQubits stm

    // leaf nodes

    abstract OnEmptyStatement: unit -> QsStatementKind
    default this.OnEmptyStatement() = EmptyStatement

    // transformation root called on each statement

    abstract OnStatementKind: QsStatementKind -> QsStatementKind

    default this.OnStatementKind kind =
        if not options.Enable then
            kind
        else
            let transformed =
                match kind with
                | QsExpressionStatement ex -> this.OnExpressionStatement ex
                | QsReturnStatement ex -> this.OnReturnStatement ex
                | QsFailStatement ex -> this.OnFailStatement ex
                | QsVariableDeclaration stm -> this.OnVariableDeclaration stm
                | QsValueUpdate stm -> this.OnValueUpdate stm
                | QsConditionalStatement stm -> this.OnConditionalStatement stm
                | QsForStatement stm -> this.OnForStatement stm
                | QsWhileStatement stm -> this.OnWhileStatement stm
                | QsRepeatStatement stm -> this.OnRepeatStatement stm
                | QsConjugation stm -> this.OnConjugation stm
                | QsQubitScope stm -> this.OnQubitScope stm
                | EmptyStatement -> this.OnEmptyStatement()

            id |> node.BuildOr kind transformed

and StatementTransformationBase(statementKindTransformation, expressionTransformation, options) =
    let node = if options.Rebuild then Fold else Walk

    member _.Types = expressionTransformation().Types

    member _.Expressions: ExpressionTransformationBase = expressionTransformation ()

    member _.StatementKinds = statementKindTransformation ()

    member internal _.Common = expressionTransformation().Types.Common

    new(options) as this =
        let expressions = ExpressionTransformationBase options
        let kinds = StatementKindTransformationBase((fun () -> this), options)
        StatementTransformationBase((fun () -> kinds), (fun () -> expressions), options)

    new(statementKindTransformation, expressionTransformation) =
        StatementTransformationBase(
            statementKindTransformation,
            expressionTransformation,
            TransformationOptions.Default
        )

    new() = StatementTransformationBase TransformationOptions.Default

    // supplementary statement information

    // TODO: RELEASE 2022-09: Remove member.
    [<Obsolete "Use SyntaxTreeTransformation.OnRelativeLocation instead">]
    abstract OnLocation: QsNullable<QsLocation> -> QsNullable<QsLocation>

    // TODO: RELEASE 2022-09: Remove member.
    [<Obsolete "Use SyntaxTreeTransformation.OnRelativeLocation instead">]
    override this.OnLocation loc = loc

    // TODO: RELEASE 2022-09: Remove member.
    [<Obsolete "Use ExpressionTransformationBase.OnLocalName instead">]
    abstract OnVariableName: string -> string

    // TODO: RELEASE 2022-09: Remove member.
    [<Obsolete "Use ExpressionTransformationBase.OnLocalName instead">]
    override this.OnVariableName name = name

    abstract OnLocalDeclarations: LocalDeclarations -> LocalDeclarations

    default this.OnLocalDeclarations decl =
        let onLocalVariableDeclaration (local: LocalVariableDeclaration<string>) =
            let loc = this.Common.OnSymbolLocation(local.Position, local.Range)
            let name = this.Common.OnLocalName local.VariableName
            let varType = this.Types.OnType local.Type
            let info = this.Expressions.OnExpressionInformation local.InferredInformation
            LocalVariableDeclaration.New info.IsMutable (loc, name, varType, info.HasLocalQuantumDependency)

        let variableDeclarations = decl.Variables |> Seq.map onLocalVariableDeclaration

        if options.Rebuild then
            LocalDeclarations.New(variableDeclarations |> ImmutableArray.CreateRange)
        else
            variableDeclarations |> Seq.iter ignore
            decl

    // transformation roots called on each statement or statement block

    abstract OnStatement: QsStatement -> QsStatement

    default this.OnStatement stm =
        if not options.Enable then
            stm
        else
            let location = this.Common.OnRelativeLocation stm.Location
            let comments = stm.Comments
            let kind = this.StatementKinds.OnStatementKind stm.Statement
            let varDecl = this.OnLocalDeclarations stm.SymbolDeclarations
            QsStatement.New comments location |> node.BuildOr stm (kind, varDecl)

    abstract OnScope: QsScope -> QsScope

    default this.OnScope scope =
        if not options.Enable then
            scope
        else
            let parentSymbols = this.OnLocalDeclarations scope.KnownSymbols
            let statements = scope.Statements |> Seq.map this.OnStatement

            if options.Rebuild then
                QsScope.New(statements |> ImmutableArray.CreateRange, parentSymbols)
            else
                statements |> Seq.iter ignore
                scope
