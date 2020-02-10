// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Experimental

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.Experimental.OptimizationTools
open Microsoft.Quantum.QsCompiler.Experimental.PureCircuitAPI
open Microsoft.Quantum.QsCompiler.Experimental.Utils
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core


/// The SyntaxTreeTransformation used to find and optimize pure circuits
type PureCircuitFinder(callables) =
    inherit OptimizingTransformation()

    /// Returns whether an expression is an operation call
    let isOperation expr =
        match expr.Expression with
        | x when TypedExpression.IsPartialApplication x -> false
        | CallLikeExpression (method, _) ->
            match method.ResolvedType.Resolution with
            | TypeKind.Operation _ -> true
            | _ -> false
        | _ -> false

    let mutable distinctQubitFinder = None

    override __.onCallableImplementation c =
        let r = FindDistinctQubits()
        r.onCallableImplementation c |> ignore
        distinctQubitFinder <- Some r
        base.onCallableImplementation c

    override __.Scope = { new ScopeTransformation() with

        override this.Transform scope =
            let mutable circuit = ImmutableArray.Empty
            let mutable newStatements = ImmutableArray.Empty

            let finishCircuit () =
                if circuit.Length <> 0 then
                    let newCircuit = optimizeExprList callables distinctQubitFinder.Value.distinctNames circuit
                    (*if newCircuit <> circuit then
                        printfn "Removed %d gates" (circuit.Length - newCircuit.Length)
                        printfn "Old: %O" (List.map (fun x -> printExpr x.Expression) circuit)
                        printfn "New: %O" (List.map (fun x -> printExpr x.Expression) newCircuit)
                        printfn ""*)
                    newStatements <- newStatements.AddRange (Seq.map (QsExpressionStatement >> wrapStmt) newCircuit)
                    circuit <- ImmutableArray.Empty

            for stmt in scope.Statements do
                match stmt.Statement with
                | QsExpressionStatement expr when isOperation expr ->
                    circuit <- circuit.Add expr
                | _ ->
                    finishCircuit()
                    newStatements <- newStatements.Add (this.onStatement stmt)
            finishCircuit()

            QsScope.New (newStatements, scope.KnownSymbols)
    }
