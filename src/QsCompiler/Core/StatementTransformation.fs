// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

open System
open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree


type StatementKindTransformationBase internal (unsafe) =
    let missingTransformation name _ = new InvalidOperationException(sprintf "No %s transformation has been specified." name) |> raise 

    member val internal ExpressionTransformationHandle = missingTransformation "expression" with get, set
    member val internal StatementTransformationHandle = missingTransformation "statement" with get, set

    // TODO: this should be a protected member
    abstract member Expressions : ExpressionTransformationBase
    default this.Expressions = this.ExpressionTransformationHandle()

    // TODO: this should be a protected member
    abstract member Statements : StatementTransformationBase
    default this.Statements = this.StatementTransformationHandle()

    new (statementTransformation : unit -> StatementTransformationBase, expressionTransformation : unit -> ExpressionTransformationBase) as this = 
        new StatementKindTransformationBase("unsafe") then 
            this.ExpressionTransformationHandle <- expressionTransformation
            this.StatementTransformationHandle <- statementTransformation

    new () as this = 
        new StatementKindTransformationBase("unsafe") then
            let expressionTransformation = new ExpressionTransformationBase()
            let statementTransformation = new StatementTransformationBase((fun _ -> this), (fun _ -> this.Expressions))
            this.ExpressionTransformationHandle <- fun _ -> expressionTransformation
            this.StatementTransformationHandle <- fun _ -> statementTransformation


    abstract member onQubitInitializer : ResolvedInitializer -> ResolvedInitializer 
    default this.onQubitInitializer init = 
        match init.Resolution with 
        | SingleQubitAllocation      -> SingleQubitAllocation
        | QubitRegisterAllocation ex -> QubitRegisterAllocation (this.Expressions.Transform ex)
        | QubitTupleAllocation is    -> QubitTupleAllocation ((is |> Seq.map this.onQubitInitializer).ToImmutableArray())
        | InvalidInitializer         -> InvalidInitializer
        |> ResolvedInitializer.New 

    abstract member beforeVariableDeclaration : SymbolTuple -> SymbolTuple
    default this.beforeVariableDeclaration syms = syms

    abstract member onSymbolTuple : SymbolTuple -> SymbolTuple
    default this.onSymbolTuple syms = syms


    abstract member onExpressionStatement : TypedExpression -> QsStatementKind
    default this.onExpressionStatement ex = this.Expressions.Transform ex |> QsExpressionStatement

    abstract member onReturnStatement : TypedExpression -> QsStatementKind
    default this.onReturnStatement ex = this.Expressions.Transform ex |> QsReturnStatement

    abstract member onFailStatement : TypedExpression -> QsStatementKind
    default this.onFailStatement ex = this.Expressions.Transform ex |> QsFailStatement

    abstract member onVariableDeclaration : QsBinding<TypedExpression> -> QsStatementKind
    default this.onVariableDeclaration stm = 
        let rhs = this.Expressions.Transform stm.Rhs
        let lhs = this.onSymbolTuple stm.Lhs
        QsBinding<TypedExpression>.New stm.Kind (lhs, rhs) |> QsVariableDeclaration

    abstract member onValueUpdate : QsValueUpdate -> QsStatementKind
    default this.onValueUpdate stm = 
        let rhs = this.Expressions.Transform stm.Rhs
        let lhs = this.Expressions.Transform stm.Lhs
        QsValueUpdate.New (lhs, rhs) |> QsValueUpdate

    abstract member onPositionedBlock : QsNullable<TypedExpression> * QsPositionedBlock -> QsNullable<TypedExpression> * QsPositionedBlock
    default this.onPositionedBlock (intro : QsNullable<TypedExpression>, block : QsPositionedBlock) = 
        let location = this.Statements.onLocation block.Location
        let comments = block.Comments
        let expr = intro |> QsNullable<_>.Map this.Expressions.Transform
        let body = this.Statements.Transform block.Body
        expr, QsPositionedBlock.New comments location body

    abstract member onConditionalStatement : QsConditionalStatement -> QsStatementKind
    default this.onConditionalStatement stm = 
        let cases = stm.ConditionalBlocks |> Seq.map (fun (c, b) -> 
            let cond, block = this.onPositionedBlock (Value c, b)
            let invalidCondition () = failwith "missing condition in if-statement"
            cond.ValueOrApply invalidCondition, block)
        let defaultCase = stm.Default |> QsNullable<_>.Map (fun b -> this.onPositionedBlock (Null, b) |> snd)
        QsConditionalStatement.New (cases, defaultCase) |> QsConditionalStatement

    abstract member onForStatement : QsForStatement -> QsStatementKind
    default this.onForStatement stm = 
        let iterVals = this.Expressions.Transform stm.IterationValues
        let loopVar = fst stm.LoopItem |> this.onSymbolTuple
        let loopVarType = this.Expressions.Types.Transform (snd stm.LoopItem)
        let body = this.Statements.Transform stm.Body
        QsForStatement.New ((loopVar, loopVarType), iterVals, body) |> QsForStatement

    abstract member onWhileStatement : QsWhileStatement -> QsStatementKind
    default this.onWhileStatement stm = 
        let condition = this.Expressions.Transform stm.Condition
        let body = this.Statements.Transform stm.Body
        QsWhileStatement.New (condition, body) |> QsWhileStatement

    abstract member onRepeatStatement : QsRepeatStatement -> QsStatementKind
    default this.onRepeatStatement stm = 
        let repeatBlock = this.onPositionedBlock (Null, stm.RepeatBlock) |> snd
        let successCondition, fixupBlock = this.onPositionedBlock (Value stm.SuccessCondition, stm.FixupBlock)
        let invalidCondition () = failwith "missing success condition in repeat-statement"
        QsRepeatStatement.New (repeatBlock, successCondition.ValueOrApply invalidCondition, fixupBlock) |> QsRepeatStatement

    abstract member onConjugation : QsConjugation -> QsStatementKind
    default this.onConjugation stm = 
        let outer = this.onPositionedBlock (Null, stm.OuterTransformation) |> snd
        let inner = this.onPositionedBlock (Null, stm.InnerTransformation) |> snd
        QsConjugation.New (outer, inner) |> QsConjugation

    abstract member onQubitScope : QsQubitScope -> QsStatementKind
    default this.onQubitScope (stm : QsQubitScope) = 
        let kind = stm.Kind
        let rhs = this.onQubitInitializer stm.Binding.Rhs
        let lhs = this.onSymbolTuple stm.Binding.Lhs
        let body = this.Statements.Transform stm.Body
        QsQubitScope.New kind ((lhs, rhs), body) |> QsQubitScope

    abstract member onAllocateQubits : QsQubitScope -> QsStatementKind
    default this.onAllocateQubits stm = this.onQubitScope stm

    abstract member onBorrowQubits : QsQubitScope -> QsStatementKind
    default this.onBorrowQubits stm = this.onQubitScope stm


    member private this.dispatchQubitScope (stm : QsQubitScope) = 
        match stm.Kind with 
        | Allocate -> this.onAllocateQubits stm
        | Borrow   -> this.onBorrowQubits stm

    abstract member Transform : QsStatementKind -> QsStatementKind
    default this.Transform kind = 
        let beforeBinding (stm : QsBinding<TypedExpression>) = { stm with Lhs = this.beforeVariableDeclaration stm.Lhs }
        let beforeForStatement (stm : QsForStatement) = {stm with LoopItem = (this.beforeVariableDeclaration (fst stm.LoopItem), snd stm.LoopItem)} 
        let beforeQubitScope (stm : QsQubitScope) = {stm with Binding = {stm.Binding with Lhs = this.beforeVariableDeclaration stm.Binding.Lhs}}
        match kind with
        | QsExpressionStatement ex   -> this.onExpressionStatement  (ex)
        | QsReturnStatement ex       -> this.onReturnStatement      (ex)
        | QsFailStatement ex         -> this.onFailStatement        (ex)
        | QsVariableDeclaration stm  -> this.onVariableDeclaration  (stm  |> beforeBinding)
        | QsValueUpdate stm          -> this.onValueUpdate          (stm)
        | QsConditionalStatement stm -> this.onConditionalStatement (stm)
        | QsForStatement stm         -> this.onForStatement         (stm  |> beforeForStatement)
        | QsWhileStatement stm       -> this.onWhileStatement       (stm)
        | QsRepeatStatement stm      -> this.onRepeatStatement      (stm)
        | QsConjugation stm          -> this.onConjugation          (stm)
        | QsQubitScope stm           -> this.dispatchQubitScope     (stm  |> beforeQubitScope)


and StatementTransformationBase internal (unsafe) =
    let missingTransformation name _ = new InvalidOperationException(sprintf "No %s transformation has been specified." name) |> raise 

    member val internal ExpressionTransformationHandle = missingTransformation "expression" with get, set
    member val internal StatementKindTransformationHandle = missingTransformation "statement kind" with get, set

    // TODO: this should be a protected member
    abstract member Expressions : ExpressionTransformationBase
    default this.Expressions = this.ExpressionTransformationHandle()

    // TODO: this should be a protected member
    abstract member StatementKinds : StatementKindTransformationBase
    default this.StatementKinds = this.StatementKindTransformationHandle()

    new (statementTransformation : unit -> StatementKindTransformationBase, expressionTransformation : unit -> ExpressionTransformationBase) as this = 
        new StatementTransformationBase("unsafe") then 
            this.ExpressionTransformationHandle <- expressionTransformation
            this.StatementKindTransformationHandle <- statementTransformation

    new () as this = 
        new StatementTransformationBase("unsafe") then
            let expressionTransformation = new ExpressionTransformationBase()
            let statementTransformation = new StatementKindTransformationBase((fun _ -> this), (fun _ -> this.Expressions))
            this.ExpressionTransformationHandle <- fun _ -> expressionTransformation
            this.StatementKindTransformationHandle <- fun _ -> statementTransformation


    abstract member onLocation : QsNullable<QsLocation> -> QsNullable<QsLocation>
    default this.onLocation loc = loc

    abstract member onLocalDeclarations : LocalDeclarations -> LocalDeclarations
    default this.onLocalDeclarations decl = 
        let onLocalVariableDeclaration (local : LocalVariableDeclaration<NonNullable<string>>) = 
            let loc = local.Position, local.Range
            let info = this.Expressions.onExpressionInformation local.InferredInformation
            let varType = this.Expressions.Types.Transform local.Type 
            LocalVariableDeclaration.New info.IsMutable (loc, local.VariableName, varType, info.HasLocalQuantumDependency)
        let variableDeclarations = decl.Variables |> Seq.map onLocalVariableDeclaration |> ImmutableArray.CreateRange
        LocalDeclarations.New variableDeclarations

    abstract member onStatement : QsStatement -> QsStatement
    default this.onStatement stm = 
        let location = this.onLocation stm.Location
        let comments = stm.Comments
        let kind = this.StatementKinds.Transform stm.Statement
        let varDecl = this.onLocalDeclarations stm.SymbolDeclarations
        QsStatement.New comments location (kind, varDecl)

    abstract member Transform : QsScope -> QsScope 
    default this.Transform scope = 
        let parentSymbols = this.onLocalDeclarations scope.KnownSymbols
        let statements = scope.Statements |> Seq.map this.onStatement
        QsScope.New (statements, parentSymbols)
