// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Optimizations

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.Optimizations.Utils
open Microsoft.Quantum.QsCompiler.Optimizations.MinorTransformations
open Microsoft.Quantum.QsCompiler.Optimizations.VariableRenaming
open Microsoft.Quantum.QsCompiler.Optimizations.VariableRemoving
open Microsoft.Quantum.QsCompiler.Optimizations.StatementRemoving
open Microsoft.Quantum.QsCompiler.Optimizations.ConstantPropagation
open Microsoft.Quantum.QsCompiler.Optimizations.LoopUnrolling
open Microsoft.Quantum.QsCompiler.Optimizations.CallableInlining
open Microsoft.Quantum.QsCompiler.Optimizations.StatementReordering
open Microsoft.Quantum.QsCompiler.Optimizations.PureCircuitFinding


/// This class has static functions to perform high-level combinations of optimizations
type Optimizations() =

    /// Optimizes the given sequence of namespaces, returning a new list of namespaces
    static member optimize (tree: seq<_>) =
        let mutable tree = List.ofSeq tree
        let callables = GlobalCallableResolutions tree |> Callables

        let removeFunctions = false
        let maxSize = 40

        tree <- List.map (StripAllKnownSymbols().Transform) tree
        tree <- tree |> List.map StripPositionInfo.Apply
        tree <- List.map (VariableRenamer().Transform) tree

        let optimizers: OptimizingTransformation list = [
            VariableRemover()
            StatementRemover(removeFunctions)
            ConstantPropagator(callables)
            LoopUnroller(callables, maxSize)
            CallableInliner(callables)
            StatementReorderer()
            PureCircuitFinder(callables)
        ]
        for opt in optimizers do
            tree <- List.map opt.Transform tree

        if optimizers |> List.exists (fun opt -> opt.checkChanged()) then
            Optimizations.optimize tree
        else
            tree
