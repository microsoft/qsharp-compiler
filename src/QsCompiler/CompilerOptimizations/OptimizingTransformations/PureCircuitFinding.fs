// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.CompilerOptimization.PureCircuitFinding

open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open Utils
open PureCircuitAPI
open MinorTransformations


/// Returns whether an expression is an operation call
let private isOperation expr =
    match expr.Expression with
    | x when TypedExpression.IsPartialApplication x -> false
    | CallLikeExpression (method, _) ->
        match method.ResolvedType.Resolution with
        | TypeKind.Operation _ -> true
        | _ -> false
    | _ -> false


/// The SyntaxTreeTransformation used to find and optimize pure circuits
type internal PureCircuitFinder(callables) =
    inherit OptimizingTransformation()

    let mutable distinctQubitFinder = None

    override __.onCallableImplementation c =
        let r = FindDistinctQubits()
        r.onCallableImplementation c |> ignore
        distinctQubitFinder <- Some r
        base.onCallableImplementation c

    override __.Scope = { new ScopeTransformation() with

        override this.Transform scope =
            let mutable circuit = []
            let mutable newStatements = []

            let finishCircuit () =
                if circuit <> [] then
                    let newCircuit = optimizeExprList callables distinctQubitFinder.Value.distinctNames circuit
                    (*if newCircuit <> circuit then
                        printfn "Removed %d gates" (circuit.Length - newCircuit.Length)
                        printfn "Old: %O" (List.map (fun x -> printExpr x.Expression) circuit)
                        printfn "New: %O" (List.map (fun x -> printExpr x.Expression) newCircuit)
                        printfn ""*)
                    newStatements <- newStatements @ List.map (QsExpressionStatement >> wrapStmt) newCircuit
                    circuit <- []

            for stmt in scope.Statements do
                match stmt.Statement with
                | QsExpressionStatement expr when isOperation expr ->
                    circuit <- circuit @ [expr]
                | _ ->
                    finishCircuit()
                    newStatements <- newStatements @ [this.onStatement stmt]
            finishCircuit()

            QsScope.New (newStatements, scope.KnownSymbols)
    }
