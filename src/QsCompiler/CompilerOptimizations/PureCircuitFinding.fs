// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.CompilerOptimization.PureCircuitFinding

open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open Utils
open Printer
open PureCircuitAPI
open OptimizingTransformation


/// The SyntaxTreeTransformation used to find and optimize pure circuits
type internal PureCircuitFinder() =
    inherit OptimizingTransformation()

    override __.Scope = { new ScopeTransformation() with

        override this.Transform scope =
            let mutable circuit = []
            let mutable newStatements = []

            let finishCircuit () =
                if circuit <> [] then
                    let newCircuit = optimizeExprList circuit
                    if newCircuit <> circuit then
                        printfn "Removed %d gates" (circuit.Length - newCircuit.Length)
                        printfn "Old: %O" (List.map (fun x -> printExpr x.Expression) circuit)
                        printfn "New: %O" (List.map (fun x -> printExpr x.Expression) newCircuit)
                        printfn ""
                    newStatements <- newStatements @ List.map (QsExpressionStatement >> wrapStmt) newCircuit
                    circuit <- []

            for stmt in scope.Statements do
                match stmt.Statement with
                | QsExpressionStatement expr -> circuit <- circuit @ [expr]
                | _ ->
                    finishCircuit()
                    newStatements <- newStatements @ [this.onStatement stmt]
            finishCircuit()

            QsScope.New (newStatements, scope.KnownSymbols)


        override this.StatementKind = { new StatementKindTransformation () with
            override __.ScopeTransformation s = this.Transform s
            override __.ExpressionTransformation ex = this.Expression.Transform ex
            override __.TypeTransformation t = this.Expression.Type.Transform t
            override __.LocationTransformation l = this.onLocation l
        }
    }
