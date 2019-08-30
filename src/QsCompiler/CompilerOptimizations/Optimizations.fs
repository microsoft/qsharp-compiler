﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.CompilerOptimization

open Microsoft.Quantum.QsCompiler.SyntaxExtensions

open Microsoft.Quantum.QsCompiler.CompilerOptimization.Types
open Microsoft.Quantum.QsCompiler.CompilerOptimization.OptimizingTransformation
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
        let callables = GlobalCallableResolutions tree |> makeCallables

        let optimizers: OptimizingTransformation list = [
            ConstantPropagator(callables)
            LoopUnroller(callables, 40)
            CallableInliner(callables)
            StatementReorderer()
            PureCircuitFinder()
        ]
        for opt in optimizers do
            tree <- List.map opt.Transform tree

        if optimizers |> List.exists (fun opt -> opt.checkChanged()) then
            Optimizations.optimize tree
        else
            tree
