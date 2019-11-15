// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree


/// Convention: 
/// All methods starting with "on" implement the walk for an expression of a certain kind.
/// All methods starting with "before" group a set of statements, and are called before walking the set
/// even if the corresponding walk routine (starting with "on") is overridden.
/// 
/// These classes differ from the "*Transformation" classes in that these classes visit every node in the
/// syntax tree, but don't create a new syntax tree, while the Transformation classes generate a new (or
/// at least partially new) tree from the old one.
/// Effectively, the Transformation classes implement fold, while the Walker classes implement iter.
[<AbstractClass>]
type StatementKindWalker(?enable) =
    let enable = defaultArg enable true

    abstract member ScopeWalker : QsScope -> unit
    abstract member ExpressionWalker : TypedExpression -> unit
    abstract member TypeWalker : ResolvedType -> unit
    abstract member LocationWalker : QsNullable<QsLocation> -> unit

    abstract member onQubitInitializer : ResolvedInitializer -> unit 
    default this.onQubitInitializer init = 
        match init.Resolution with 
        | SingleQubitAllocation      -> ()
        | QubitRegisterAllocation ex -> this.ExpressionWalker ex
        | QubitTupleAllocation is    -> is |> Seq.iter this.onQubitInitializer
        | InvalidInitializer         -> ()

    abstract member beforeVariableDeclaration : SymbolTuple -> unit
    default this.beforeVariableDeclaration syms = ()

    abstract member onSymbolTuple : SymbolTuple -> unit
    default this.onSymbolTuple syms = ()


    abstract member onExpressionStatement : TypedExpression -> unit
    default this.onExpressionStatement ex = this.ExpressionWalker ex

    abstract member onReturnStatement : TypedExpression -> unit
    default this.onReturnStatement ex = this.ExpressionWalker ex

    abstract member onFailStatement : TypedExpression -> unit
    default this.onFailStatement ex = this.ExpressionWalker ex

    abstract member onVariableDeclaration : QsBinding<TypedExpression> -> unit
    default this.onVariableDeclaration stm = 
        this.ExpressionWalker stm.Rhs
        this.onSymbolTuple stm.Lhs

    abstract member onValueUpdate : QsValueUpdate -> unit
    default this.onValueUpdate stm = 
        this.ExpressionWalker stm.Rhs
        this.ExpressionWalker stm.Lhs

    abstract member onPositionedBlock : TypedExpression option * QsPositionedBlock -> unit
    default this.onPositionedBlock (intro : TypedExpression option, block : QsPositionedBlock) = 
        this.LocationWalker block.Location
        intro |> Option.iter this.ExpressionWalker
        this.ScopeWalker block.Body

    abstract member onConditionalStatement : QsConditionalStatement -> unit
    default this.onConditionalStatement stm = 
        stm.ConditionalBlocks |> Seq.iter (fun (c, b) -> this.onPositionedBlock (Some c, b))
        stm.Default |> QsNullable<_>.Iter (fun b -> this.onPositionedBlock (None, b))

    abstract member onForStatement : QsForStatement -> unit
    default this.onForStatement stm = 
        this.ExpressionWalker stm.IterationValues
        fst stm.LoopItem |> this.onSymbolTuple
        this.TypeWalker (snd stm.LoopItem)
        this.ScopeWalker stm.Body

    abstract member onWhileStatement : QsWhileStatement -> unit
    default this.onWhileStatement stm = 
        this.ExpressionWalker stm.Condition
        this.ScopeWalker stm.Body

    abstract member onRepeatStatement : QsRepeatStatement -> unit
    default this.onRepeatStatement stm = 
        this.onPositionedBlock (None, stm.RepeatBlock)
        this.onPositionedBlock (Some stm.SuccessCondition, stm.FixupBlock)

    abstract member onConjugation : QsConjugation -> unit
    default this.onConjugation stm = 
        this.onPositionedBlock (None, stm.OuterTransformation)
        this.onPositionedBlock (None, stm.InnerTransformation)

    abstract member onQubitScope : QsQubitScope -> unit
    default this.onQubitScope (stm : QsQubitScope) = 
        this.onQubitInitializer stm.Binding.Rhs
        this.onSymbolTuple stm.Binding.Lhs
        this.ScopeWalker stm.Body

    abstract member onAllocateQubits : QsQubitScope -> unit
    default this.onAllocateQubits stm = this.onQubitScope stm

    abstract member onBorrowQubits : QsQubitScope -> unit
    default this.onBorrowQubits stm = this.onQubitScope stm


    member private this.dispatchQubitScope (stm : QsQubitScope) = 
        match stm.Kind with 
        | Allocate -> this.onAllocateQubits stm
        | Borrow   -> this.onBorrowQubits stm

    abstract member Walk : QsStatementKind -> unit
    default this.Walk kind = 
        let beforeBinding (stm : QsBinding<TypedExpression>) = this.beforeVariableDeclaration stm.Lhs
        let beforeForStatement (stm : QsForStatement) = this.beforeVariableDeclaration (fst stm.LoopItem)
        let beforeQubitScope (stm : QsQubitScope) = this.beforeVariableDeclaration stm.Binding.Lhs

        if not enable then () else
        match kind with
        | QsExpressionStatement ex   -> this.onExpressionStatement  ex
        | QsReturnStatement ex       -> this.onReturnStatement      ex
        | QsFailStatement ex         -> this.onFailStatement        ex
        | QsVariableDeclaration stm  -> beforeBinding               stm
                                        this.onVariableDeclaration  stm
        | QsValueUpdate stm          -> this.onValueUpdate          stm
        | QsConditionalStatement stm -> this.onConditionalStatement stm
        | QsForStatement stm         -> beforeForStatement          stm
                                        this.onForStatement         stm
        | QsWhileStatement stm       -> this.onWhileStatement       stm
        | QsRepeatStatement stm      -> this.onRepeatStatement      stm
        | QsConjugation stm          -> this.onConjugation          stm
        | QsQubitScope stm           -> beforeQubitScope            stm
                                        this.dispatchQubitScope     stm


and ScopeWalker(?enableStatementKindWalkers) =
    let enableStatementKind = defaultArg enableStatementKindWalkers true
    let expressionsWalker = new ExpressionWalker()

    abstract member Expression : ExpressionWalker
    default this.Expression = expressionsWalker

    abstract member StatementKind : StatementKindWalker
    default this.StatementKind = {
        new StatementKindWalker (enableStatementKind) with 
            override x.ScopeWalker s = this.Walk s
            override x.ExpressionWalker ex = this.Expression.Walk ex
            override x.TypeWalker t = this.Expression.Type.Walk t
            override x.LocationWalker l = this.onLocation l
        }

    abstract member onLocation : QsNullable<QsLocation> -> unit
    default this.onLocation loc = ()

    abstract member onStatement : QsStatement -> unit
    default this.onStatement stm = 
        this.onLocation stm.Location
        this.StatementKind.Walk stm.Statement

    abstract member Walk : QsScope -> unit 
    default this.Walk scope = 
        scope.Statements |> Seq.iter this.onStatement
