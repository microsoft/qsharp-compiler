// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.CompilerOptimization.StatementRemoving

open System
open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open ComputationExpressions
open Utils
open VariableRenaming
open OptimizingTransformation


/// Returns whether all variables in a symbol tuple are discarded
let rec private isAllDiscarded = function
| DiscardedItem -> true
| VariableNameTuple items -> Seq.forall isAllDiscarded items
| _ -> false

/// Returns whether a statements is purely classical.
/// The statement must have no classical or quantum side effects other than defining a variable.
let private isPureClassical = function
| QsVariableDeclaration s when not (s.Rhs.InferredInformation.HasLocalQuantumDependency) -> true
| _ -> false

/// Returns whether a statement is purely quantum.
/// The statement must have no classical side effects, but can have quantum side effects.
let private isPureQuantum = function
| QsVariableDeclaration s when isAllDiscarded s.Lhs -> true
| QsExpressionStatement _ -> true
| _ -> false

/// Reorders a list of statements such that the pure classical statements occur before the pure quantum statements
let rec private reorderStatements (stmtList: QsStatement list): QsStatement list =
    match stmtList with
    | [] -> []
    | head :: tail ->
        let tail = reorderStatements tail
        match head :: tail with
        | head1 :: head2 :: tail
            when isPureQuantum head1.Statement && isPureClassical head2.Statement ->
            head2 :: reorderStatements (head1 :: tail)
        | a -> a


/// The SyntaxTreeTransformation used to remove useless statements
type internal StatementRemover() =
    inherit OptimizingTransformation()

    /// The VariableRenamed used to ensure unique variable names
    let mutable renamer = None

    /// Returns whether the given statement is needed.
    /// If this returns false, the given statement can be removed without changing the code.
    let isStatementNeeded stmt =
        match stmt.Statement with
        | QsVariableDeclaration s when isAllDiscarded s.Lhs && not (s.Rhs.InferredInformation.HasLocalQuantumDependency) -> false
        | QsScopeStatement s when s.Body.Statements.IsEmpty -> false
        | QsForStatement s when s.Body.Statements.IsEmpty -> false
        | QsWhileStatement s when s.Body.Statements.IsEmpty -> false
        | _ -> true

    /// Splits a single statement into a sequence of statements.
    /// Used to split variable declarations across lines, and to remove scope statements.
    /// If a statement should not be split, returns a sequence with a single element.
    let splitStatement stmt =
        match stmt.Statement with
        | QsVariableDeclaration s ->
            match s.Lhs, s.Rhs with
            | Tuple l, Tuple r ->
                if l.Length <> r.Length then
                    ArgumentException "Tuple lenghts do not match" |> raise
                Seq.zip l r |> Seq.map (QsBinding.New s.Kind >> QsVariableDeclaration >> wrapStmt)
            | _ -> Seq.singleton stmt
        | QsScopeStatement s -> s.Body.Statements |> Seq.cast
        | _ -> Seq.singleton stmt


    override syntaxTree.onProvidedImplementation (argTuple, body) =
        let renamerVal = VariableRenamer(argTuple)
        renamer <- Some renamerVal
        let body = renamerVal.Transform body

        let argTuple = syntaxTree.onArgumentTuple argTuple
        let body = syntaxTree.Scope.Transform body
        argTuple, body

    override syntaxTree.Scope = { new ScopeTransformation() with

        override this.Transform scope =
            let parentSymbols = scope.KnownSymbols
            let statements =
                scope.Statements
                |> Seq.map this.onStatement
                |> Seq.filter isStatementNeeded
                |> Seq.collect splitStatement
                |> List.ofSeq |> reorderStatements
            QsScope.New (statements, parentSymbols)

        override scope.StatementKind = { new StatementKindTransformation() with
            override stmtKind.ExpressionTransformation x = scope.Expression.Transform x
            override stmtKind.LocationTransformation x = scope.onLocation x
            override stmtKind.ScopeTransformation x = scope.Transform x
            override stmtKind.TypeTransformation x = scope.Expression.Type.Transform x

            override this.onSymbolTuple syms =
                match syms with
                | VariableName item ->
                    maybe {
                        let! r = renamer
                        let! uses = r.getNumUses item.Value
                        do! check (uses = 0)
                        return DiscardedItem
                    } |? syms
                | VariableNameTuple items -> Seq.map (this.onSymbolTuple) items |> ImmutableArray.CreateRange |> VariableNameTuple
                | InvalidItem | DiscardedItem -> syms

            override this.onQubitScope (stm : QsQubitScope) =
                let kind = stm.Kind
                let lhs = this.onSymbolTuple stm.Binding.Lhs
                let rhs = this.onQubitInitializer stm.Binding.Rhs
                let body = this.ScopeTransformation stm.Body
                if isAllDiscarded lhs then
                    QsScopeStatement.New body |> QsScopeStatement
                else
                    QsQubitScope.New kind ((lhs, rhs), body) |> QsQubitScope
        }
    }
