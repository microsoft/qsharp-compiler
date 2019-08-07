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
            printfn "Found circuit: %s" (String.Join (", ", List.map printExpr circuit))
        pastCircuits <- []
        result

    override syntaxTree.Scope = { new ScopeTransformation() with
        override scope.StatementKind = { new StatementKindTransformation() with
            override stmtKind.ExpressionTransformation x = scope.Expression.Transform x
            override stmtKind.LocationTransformation x = scope.onLocation x
            override stmtKind.ScopeTransformation x = scope.Transform x
            override stmtKind.TypeTransformation x = scope.Expression.Type.Transform x

            override stmtKind.Transform kind =
                match kind with
                | QsExpressionStatement expr ->
                    currentCircuit <- currentCircuit @ [expr.Expression]
                | QsQubitScope _ -> ()
                | QsScopeStatement _ -> ()
                | QsVariableDeclaration {Rhs = expr} when expr.InferredInformation.HasLocalQuantumDependency |> not -> ()
                | _ ->
                    finishCircuit ()

                base.Transform kind
        }
    }
