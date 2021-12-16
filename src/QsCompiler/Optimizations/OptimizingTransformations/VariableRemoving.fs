// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Experimental

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.Experimental.OptimizationTools
open Microsoft.Quantum.QsCompiler.Experimental.Utils
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations


/// The SyntaxTreeTransformation used to remove useless statements
type VariableRemoval(_private_) =
    inherit TransformationBase()

    member val internal ReferenceCounter = None with get, set

    new() as this =
        new VariableRemoval("_private_")
        then
            this.Namespaces <- new VariableRemovalNamespaces(this)
            this.StatementKinds <- new VariableRemovalStatementKinds(this)
            this.Expressions <- new Core.ExpressionTransformation(this, Core.TransformationOptions.Disabled)
            this.Types <- new Core.TypeTransformation(this, Core.TransformationOptions.Disabled)

/// private helper class for VariableRemoval
and private VariableRemovalNamespaces(parent: VariableRemoval) =
    inherit NamespaceTransformationBase(parent)

    override __.OnProvidedImplementation(argTuple, body) =
        let r = ReferenceCounter()
        r.Statements.OnScope body |> ignore
        parent.ReferenceCounter <- Some r
        base.OnProvidedImplementation(argTuple, body)

/// private helper class for VariableRemoval
and private VariableRemovalStatementKinds(parent: VariableRemoval) =
    inherit Core.StatementKindTransformation(parent)

    member private stmtKind.onSymbolTuple syms =
        match syms with
        | VariableName item ->
            maybe {
                let! r = parent.ReferenceCounter
                let uses = r.NumberOfUses item
                do! check (uses = 0)
                return DiscardedItem
            }
            |? syms
        | VariableNameTuple items ->
            Seq.map stmtKind.onSymbolTuple items |> ImmutableArray.CreateRange |> VariableNameTuple
        | InvalidItem
        | DiscardedItem -> syms

    member private this.onQubitScopeKind(stm: QsQubitScope) =
        let kind = stm.Kind
        let rhs = this.OnQubitInitializer stm.Binding.Rhs
        let lhs = this.onSymbolTuple stm.Binding.Lhs
        let body = this.Statements.OnScope stm.Body
        QsQubitScope <| QsQubitScope.New kind ((lhs, rhs), body)

    override this.OnAllocateQubits stm = this.onQubitScopeKind stm

    override this.OnBorrowQubits stm = this.onQubitScopeKind stm

    override this.OnVariableDeclaration stm =
        let rhs = this.Expressions.OnTypedExpression stm.Rhs
        let lhs = this.onSymbolTuple stm.Lhs
        QsVariableDeclaration <| QsBinding<TypedExpression>.New stm.Kind (lhs, rhs)

    override this.OnForStatement stm =
        let iterVals = this.Expressions.OnTypedExpression stm.IterationValues
        let loopVar = fst stm.LoopItem |> this.onSymbolTuple
        let loopVarType = this.Expressions.Types.OnType(snd stm.LoopItem)
        let body = this.Statements.OnScope stm.Body
        QsForStatement <| QsForStatement.New((loopVar, loopVarType), iterVals, body)
