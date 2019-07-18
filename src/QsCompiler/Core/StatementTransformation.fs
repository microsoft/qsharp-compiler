// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree


/// Convention: 
/// All methods starting with "on" implement the transformation for a statement of a certain kind.
/// All methods starting with "before" group a set of statements, and are called before applying the transformation
/// even if the corresponding transformation routine (starting with "on") is overridden.
[<AbstractClass>]
type StatementKindTransformation(?enable) =
    let enable = defaultArg enable true

    abstract member ScopeTransformation : QsScope -> QsScope
    abstract member ExpressionTransformation : TypedExpression -> TypedExpression
    abstract member TypeTransformation : ResolvedType -> ResolvedType
    abstract member LocationTransformation : QsNullable<QsLocation> -> QsNullable<QsLocation>

    abstract member onQubitInitializer : ResolvedInitializer -> ResolvedInitializer 
    default this.onQubitInitializer init = 
        match init.Resolution with 
        | SingleQubitAllocation      -> SingleQubitAllocation
        | QubitRegisterAllocation ex -> QubitRegisterAllocation (this.ExpressionTransformation ex)
        | QubitTupleAllocation is    -> QubitTupleAllocation ((is |> Seq.map this.onQubitInitializer).ToImmutableArray())
        | InvalidInitializer         -> InvalidInitializer
        |> ResolvedInitializer.New 

    abstract member beforeVariableDeclaration : SymbolTuple -> SymbolTuple
    default this.beforeVariableDeclaration syms = syms

    abstract member onSymbolTuple : SymbolTuple -> SymbolTuple
    default this.onSymbolTuple syms = syms


    abstract member onExpressionStatement : TypedExpression -> QsStatementKind
    default this.onExpressionStatement ex = QsExpressionStatement (this.ExpressionTransformation ex)

    abstract member onReturnStatement : TypedExpression -> QsStatementKind
    default this.onReturnStatement ex = QsReturnStatement (this.ExpressionTransformation ex)

    abstract member onFailStatement : TypedExpression -> QsStatementKind
    default this.onFailStatement ex = QsFailStatement (this.ExpressionTransformation ex)

    abstract member onVariableDeclaration : QsBinding<TypedExpression> -> QsStatementKind
    default this.onVariableDeclaration stm = 
        let lhs = this.onSymbolTuple stm.Lhs
        let rhs = this.ExpressionTransformation stm.Rhs
        QsBinding<TypedExpression>.New stm.Kind (lhs, rhs) |> QsVariableDeclaration

    abstract member onValueUpdate : QsValueUpdate -> QsStatementKind
    default this.onValueUpdate stm = 
        let lhs = this.ExpressionTransformation stm.Lhs
        let rhs = this.ExpressionTransformation stm.Rhs
        QsValueUpdate.New (lhs, rhs) |> QsValueUpdate

    abstract member onPositionedBlock : TypedExpression option * QsPositionedBlock -> TypedExpression option * QsPositionedBlock
    default this.onPositionedBlock (intro : TypedExpression option, block : QsPositionedBlock) = 
        let location = this.LocationTransformation block.Location
        let comments = block.Comments
        let expr = intro |> Option.map this.ExpressionTransformation
        let body = this.ScopeTransformation block.Body
        expr, QsPositionedBlock.New comments location body

    abstract member onConditionalStatement : QsConditionalStatement -> QsStatementKind
    default this.onConditionalStatement stm = 
        let cases = stm.ConditionalBlocks |> Seq.map (fun (c, b) -> 
            let cond, block = this.onPositionedBlock (Some c, b)
            cond |> Option.get, block)
        let defaultCase = stm.Default |> QsNullable<_>.Map (fun b -> this.onPositionedBlock (None, b) |> snd)
        QsConditionalStatement.New (cases, defaultCase) |> QsConditionalStatement

    abstract member onForStatement : QsForStatement -> QsStatementKind
    default this.onForStatement stm = 
        let loopVar = fst stm.LoopItem |> this.onSymbolTuple
        let iterVals = this.ExpressionTransformation stm.IterationValues
        let loopVarType = this.TypeTransformation (snd stm.LoopItem)
        let body = this.ScopeTransformation stm.Body
        QsForStatement.New ((loopVar, loopVarType), iterVals, body) |> QsForStatement

    abstract member onWhileStatement : QsWhileStatement -> QsStatementKind
    default this.onWhileStatement stm = 
        let condition = this.ExpressionTransformation stm.Condition
        let body = this.ScopeTransformation stm.Body
        QsWhileStatement.New (condition, body) |> QsWhileStatement

    abstract member onRepeatStatement : QsRepeatStatement -> QsStatementKind
    default this.onRepeatStatement stm = 
        let repeatBlock = this.onPositionedBlock (None, stm.RepeatBlock) |> snd
        let successCondition, fixupBlock = this.onPositionedBlock (Some stm.SuccessCondition, stm.FixupBlock)
        QsRepeatStatement.New (repeatBlock, successCondition |> Option.get, fixupBlock) |> QsRepeatStatement

    abstract member onQubitScope : QsQubitScope -> QsStatementKind
    default this.onQubitScope (stm : QsQubitScope) = 
        let kind = stm.Kind
        let lhs = this.onSymbolTuple stm.Binding.Lhs
        let rhs = this.onQubitInitializer stm.Binding.Rhs
        let body = this.ScopeTransformation stm.Body
        QsQubitScope.New kind ((lhs, rhs), body) |> QsQubitScope

    abstract member onScopeStatement : QsScopeStatement -> QsStatementKind
    default this.onScopeStatement (stm : QsScopeStatement) = 
        let body = this.ScopeTransformation stm.Body
        QsScopeStatement.New body |> QsScopeStatement

    abstract member onAllocateQubits : QsQubitScope -> QsStatementKind
    default this.onAllocateQubits stm = this.onQubitScope stm

    abstract member onBorrowQubits : QsQubitScope -> QsStatementKind
    default this.onBorrowQubits stm = this.onQubitScope stm


    member private this.dispatchQubitScope (stm : QsQubitScope) = 
        match stm.Kind with 
        | Allocate -> this.onAllocateQubits stm
        | Borrow   -> this.onBorrowQubits stm

    member this.Transform kind = 
        let beforeBinding (stm : QsBinding<TypedExpression>) = { stm with Lhs = this.beforeVariableDeclaration stm.Lhs }
        let beforeForStatement (stm : QsForStatement) = {stm with LoopItem = (this.beforeVariableDeclaration (fst stm.LoopItem), snd stm.LoopItem)} 
        let beforeQubitScope (stm : QsQubitScope) = {stm with Binding = {stm.Binding with Lhs = this.beforeVariableDeclaration stm.Binding.Lhs}}

        if not enable then kind else
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
        | QsQubitScope stm           -> this.dispatchQubitScope     (stm  |> beforeQubitScope)
        | QsScopeStatement stm       -> this.onScopeStatement       (stm)


and ScopeTransformation(?enableStatementKindTransformations) =
    let enableStatementKind = defaultArg enableStatementKindTransformations true
    let expressionsTransformation = new ExpressionTransformation()

    abstract member Expression : ExpressionTransformation
    default this.Expression = expressionsTransformation

    abstract member StatementKind : StatementKindTransformation
    default this.StatementKind = {
        new StatementKindTransformation (enableStatementKind) with 
            override x.ScopeTransformation s = this.Transform s
            override x.ExpressionTransformation ex = this.Expression.Transform ex
            override x.TypeTransformation t = this.Expression.Type.Transform t
            override x.LocationTransformation l = this.onLocation l
        }

    abstract member onLocation : QsNullable<QsLocation> -> QsNullable<QsLocation>
    default this.onLocation loc = loc

    abstract member onStatement : QsStatement -> QsStatement
    default this.onStatement stm = 
        let location = this.onLocation stm.Location
        let comments = stm.Comments
        let kind = this.StatementKind.Transform stm.Statement
        let varDecl = stm.SymbolDeclarations.Variables
        QsStatement.New comments location (kind, varDecl)

    abstract member Transform : QsScope -> QsScope 
    default this.Transform scope = 
        let parentSymbols = scope.KnownSymbols
        let statements = scope.Statements |> Seq.map this.onStatement
        QsScope.New (statements, parentSymbols)
