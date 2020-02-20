// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Experimental

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.Experimental.OptimizationTools
open Microsoft.Quantum.QsCompiler.Experimental.Utils
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations


/// The SyntaxTreeTransformation used to remove useless statements
type VariableRemoval(unsafe) =
    inherit TransformationBase()

    member val internal ReferenceCounter = None with get, set

    new () as this = 
        new VariableRemoval("unsafe") then
            this.Namespaces <- new VariableRemovalNamespaces(this)
            this.StatementKinds <- new VariableRemovalStatementKinds(this)

/// private helper class for VariableRemoval
and private VariableRemovalNamespaces (parent : VariableRemoval) = 
    inherit NamespaceTransformationBase(parent)

    override __.onProvidedImplementation (argTuple, body) =
        let r = ReferenceCounter()
        r.Statements.Transform body |> ignore
        parent.ReferenceCounter <- Some r
        base.onProvidedImplementation (argTuple, body)

/// private helper class for VariableRemoval
and private VariableRemovalStatementKinds (parent : VariableRemoval) = 
    inherit Core.StatementKindTransformation(parent)

    override stmtKind.onSymbolTuple syms =
        match syms with
        | VariableName item ->
            maybe {
                let! r = parent.ReferenceCounter
                let uses = r.NumberOfUses item
                do! check (uses = 0)
                return DiscardedItem
            } |? syms
        | VariableNameTuple items -> Seq.map stmtKind.onSymbolTuple items |> ImmutableArray.CreateRange |> VariableNameTuple
        | InvalidItem | DiscardedItem -> syms

