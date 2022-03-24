// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Experimental

#if MONO

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.Experimental.OptimizationTools
open Microsoft.Quantum.QsCompiler.Experimental.Utils
open Microsoft.Quantum.QsCompiler.SyntaxTree


/// The SyntaxTreeTransformation used to remove useless statements
type VariableRemoval() =
    inherit TransformationBase()

    member val internal ReferenceCounter = None with get, set

    override this.OnProvidedImplementation(argTuple, body) =
        let r = ReferenceCounter()
        r.OnScope body |> ignore
        this.ReferenceCounter <- Some r
        ``base``.OnProvidedImplementation(argTuple, body)

    override this.OnSymbolTuple syms =
        match syms with
        | VariableName item ->
            maybe {
                let! r = this.ReferenceCounter
                let uses = r.NumberOfUses item
                do! check (uses = 0)
                return DiscardedItem
            }
            |? syms
        | VariableNameTuple items ->
            Seq.map this.OnSymbolTuple items |> ImmutableArray.CreateRange |> VariableNameTuple
        | InvalidItem
        | DiscardedItem -> syms

#endif
