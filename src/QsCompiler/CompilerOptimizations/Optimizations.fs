﻿namespace Microsoft.Quantum.QsCompiler.CompilerOptimization

open Microsoft.Quantum.QsCompiler.SyntaxExtensions

open Microsoft.Quantum.QsCompiler.CompilerOptimization.OptimizingTransformation
open Microsoft.Quantum.QsCompiler.CompilerOptimization.ConstantPropagation
open Microsoft.Quantum.QsCompiler.CompilerOptimization.LoopUnrolling
open Microsoft.Quantum.QsCompiler.CompilerOptimization.CallableInlining
open Microsoft.Quantum.QsCompiler.CompilerOptimization.VariableRenaming
open Microsoft.Quantum.QsCompiler.CompilerOptimization.StatementRemoving
open Microsoft.Quantum.QsCompiler.CompilerOptimization.PureCircuitFinding


type Optimizations() =

    static member optimize (tree: seq<_>) =
        let mutable tree = List.ofSeq tree
        let callables = GlobalCallableResolutions tree

        let optimizers = [
            ConstantPropagator(callables) :> OptimizingTransformation
            upcast LoopUnroller()
            upcast CallableInliner(callables)
            upcast StatementRemover()
        ]
        for opt in optimizers do
            tree <- List.map opt.Transform tree

        if optimizers |> List.exists (fun opt -> opt.checkChanged()) then
            Optimizations.optimize tree
        else
            List.map (PureCircuitFinder().Transform) tree

