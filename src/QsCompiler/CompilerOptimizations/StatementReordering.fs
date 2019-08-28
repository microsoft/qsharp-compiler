// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.CompilerOptimization.StatementReordering

open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open SideEffectChecking
open OptimizingTransformation


/// Returns whether a statements is purely classical.
/// The statement must have no classical or quantum side effects other than defining a variable.
let private isPureClassical stmt =
    let c = SideEffectChecker()
    c.onStatement stmt |> ignore
    not c.hasQuantum && not c.hasMutation && not c.hasInterrupts

/// Returns whether a statement is purely quantum.
/// The statement must have no classical side effects, but can have quantum side effects.
let private isPureQuantum stmt =
    let c = SideEffectChecker()
    c.onStatement stmt |> ignore
    c.hasQuantum && not c.hasMutation && not c.hasInterrupts

/// Reorders a list of statements such that the pure classical statements occur before the pure quantum statements
let rec private reorderStatements = function
| a :: b :: tail ->
    if isPureQuantum a && isPureClassical b
    then b :: reorderStatements (a :: tail)
    else a :: reorderStatements (b :: tail)
| x -> x

/// The SyntaxTreeTransformation used to remove useless statements
type internal StatementReorderer() =
    inherit OptimizingTransformation()

    override syntaxTree.Scope = { new ScopeTransformation() with
        override this.Transform scope =
            let parentSymbols = scope.KnownSymbols
            let statements = scope.Statements |> Seq.map this.onStatement |> List.ofSeq |> reorderStatements
            QsScope.New (statements, parentSymbols)
    }
