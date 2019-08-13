module Microsoft.Quantum.QsCompiler.CompilerOptimization.PureCircuitFinding

open System
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open Printer
open OptimizingTransformation


type PureCircuitFinder() =
    inherit OptimizingTransformation()

    let mutable pastCircuits = []
    let mutable currentCircuit = []

    let finishCircuit () =
        if not (List.isEmpty currentCircuit) then
            pastCircuits <- pastCircuits @ [currentCircuit]
        currentCircuit <- []

    override syntaxTree.onProvidedImplementation (argTuple, body) =
        let result = base.onProvidedImplementation (argTuple, body)
        finishCircuit ()
        printfn "Analyzed provided implementation:"
        for circuit in pastCircuits do
            printfn "  Found circuit:"
            for op in circuit do
                printfn "    %s"  (printExpr op)
        printfn ""
        pastCircuits <- []
        result

    override syntaxTree.Scope = { new ScopeTransformation() with

        override scope.Transform x =
            finishCircuit ()
            let result = base.Transform x
            finishCircuit ()
            result

        override scope.StatementKind = { new StatementKindTransformation() with
            override stmtKind.ExpressionTransformation x = scope.Expression.Transform x
            override stmtKind.LocationTransformation x = scope.onLocation x
            override stmtKind.ScopeTransformation x = scope.Transform x
            override stmtKind.TypeTransformation x = scope.Expression.Type.Transform x

            override stmtKind.Transform kind =
                match kind with
                | QsExpressionStatement s -> currentCircuit <- currentCircuit @ [s.Expression]
                | _ -> finishCircuit ()
                base.Transform kind
        }
    }
