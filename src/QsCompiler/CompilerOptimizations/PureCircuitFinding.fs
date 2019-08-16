module Microsoft.Quantum.QsCompiler.CompilerOptimization.PureCircuitFinding

open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open Utils
open Printer
open PureCircuitAPI
open OptimizingTransformation


type PureCircuitFinder() =
    inherit OptimizingTransformation()


    override syntaxTree.Scope = { new ScopeTransformation() with

        override scope.Transform x =
            let mutable circuit = []
            let mutable newStatements = []

            let finishCircuit () =
                if circuit <> [] then
                    let newCircuit = optimizeExprList circuit
                    (*if newCircuit <> circuit then
                        printfn "Removed %d gates" (circuit.Length - newCircuit.Length)
                        printfn "Old: %O" (List.map (fun x -> printExpr x.Expression) circuit)
                        printfn "New: %O" (List.map (fun x -> printExpr x.Expression) newCircuit)
                        printfn ""*)
                    newStatements <- newStatements @ List.map (QsExpressionStatement >> wrapStmt) newCircuit
                    circuit <- []

            for stmt in x.Statements do
                match stmt.Statement with
                | QsExpressionStatement expr -> circuit <- circuit @ [expr]
                | _ ->
                    finishCircuit()
                    newStatements <- newStatements @ [scope.onStatement stmt]
            finishCircuit()

            QsScope.New (newStatements, x.KnownSymbols)


        override this.StatementKind = { new StatementKindTransformation () with 
            override x.ScopeTransformation s = this.Transform s
            override x.ExpressionTransformation ex = this.Expression.Transform ex
            override x.TypeTransformation t = this.Expression.Type.Transform t
            override x.LocationTransformation l = this.onLocation l
        }
    }
