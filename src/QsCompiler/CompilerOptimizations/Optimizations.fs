// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.CompilerOptimization

open Microsoft.Quantum.QsCompiler.SyntaxExtensions

open Microsoft.Quantum.QsCompiler.CompilerOptimization.Utils
open Microsoft.Quantum.QsCompiler.CompilerOptimization.MinorTransformations
open Microsoft.Quantum.QsCompiler.CompilerOptimization.VariableRenaming
open Microsoft.Quantum.QsCompiler.CompilerOptimization.VariableRemoving
open Microsoft.Quantum.QsCompiler.CompilerOptimization.StatementRemoving
open Microsoft.Quantum.QsCompiler.CompilerOptimization.ConstantPropagation
open Microsoft.Quantum.QsCompiler.CompilerOptimization.LoopUnrolling
open Microsoft.Quantum.QsCompiler.CompilerOptimization.CallableInlining
open Microsoft.Quantum.QsCompiler.CompilerOptimization.StatementReordering
open Microsoft.Quantum.QsCompiler.CompilerOptimization.PureCircuitFinding


/// This class has static functions to perform high-level combinations of optimizations
type Optimizations() =

    /// Optimizes the given sequence of namespaces, returning a new list of namespaces
    static member optimize (tree: seq<_>) =
        let mutable tree = List.ofSeq tree
        let callables = GlobalCallableResolutions tree |> Callables

        let removeFunctions = false
        let maxSize = 40

        tree <- List.map (StripAllRangeInformation().Transform) tree
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
