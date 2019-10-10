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
open Microsoft.Quantum.QsCompiler.SyntaxTree


type PreEvalution =

    /// Attempts to pre-evaluate the given sequence of namespaces as much as possible
    static member All (arg : QsNamespace seq) =
        let removeFunctions = false
        let maxSize = 40

        let rec evaluate (tree : _ list) = 
            let mutable tree = tree
            tree <- List.map (StripAllKnownSymbols().Transform) tree
            tree <- List.map (VariableRenamer().Transform) tree

            let callables = GlobalCallableResolutions tree |> Callables // needs to be constructed in every iteration
            let optimizers: OptimizingTransformation list = [
                VariableRemover()
                StatementRemover(removeFunctions)
                ConstantPropagator(callables)
                LoopUnroller(callables, maxSize)
                CallableInliner(callables)
                StatementReorderer()
                PureCircuitFinder(callables)
            ]
            for opt in optimizers do tree <- List.map opt.Transform tree
            if optimizers |> List.exists (fun opt -> opt.checkChanged()) then evaluate tree 
            else tree

        arg |> Seq.map StripPositionInfo.Apply |> List.ofSeq |> evaluate