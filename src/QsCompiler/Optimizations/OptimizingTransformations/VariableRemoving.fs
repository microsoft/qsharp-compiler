// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Experimental

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.Experimental.OptimizationTools
open Microsoft.Quantum.QsCompiler.Experimental.Utils
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

    override stmtKind.OnSymbolTuple syms =
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
            Seq.map stmtKind.OnSymbolTuple items |> ImmutableArray.CreateRange |> VariableNameTuple
        | InvalidItem
        | DiscardedItem -> syms
