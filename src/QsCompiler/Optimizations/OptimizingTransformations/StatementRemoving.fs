// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Experimental

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Experimental.OptimizationTools
open Microsoft.Quantum.QsCompiler.Experimental.Utils
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations


/// The SyntaxTreeTransformation used to remove useless statements
type StatementRemoval private (_private_: string) =
    inherit TransformationBase()

    new(removeFunctions: bool) as this =
        new StatementRemoval("_private_")
        then
            this.Statements <- new VariableRemovalStatements(this, removeFunctions)
            this.Expressions <- new Core.ExpressionTransformation(this, Core.TransformationOptions.Disabled)
            this.Types <- new Core.TypeTransformation(this, Core.TransformationOptions.Disabled)

/// private helper class for StatementRemoval
and private VariableRemovalStatements(parent: StatementRemoval, removeFunctions) =
    inherit StatementCollectorTransformation(parent)

    /// Given a statement, returns a sequence of statements to replace this statement with.
    /// Removes useless statements, such as variable declarations with discarded values.
    /// Replaces ScopeStatements and empty qubit allocations with the statements they contain.
    /// Splits tuple declaration and updates into single declarations or updates for each variable.
    /// Splits QsQubitScopes by replacing register allocations with single qubit allocations.
    override __.CollectStatements stmt =

        let c = SideEffectChecker()
        c.StatementKinds.OnStatementKind stmt |> ignore

        let c2 = MutationChecker()
        c2.StatementKinds.OnStatementKind stmt |> ignore

        match stmt with
        | QsVariableDeclaration { Lhs = lhs }
        | QsValueUpdate { Lhs = LocalVarTuple lhs } when isAllDiscarded lhs && not c.HasQuantum -> Seq.empty
        | QsVariableDeclaration s ->
            jointFlatten (s.Lhs, s.Rhs) |> Seq.map (QsBinding.New s.Kind >> QsVariableDeclaration)
        | QsValueUpdate s -> jointFlatten (s.Lhs, s.Rhs) |> Seq.map (QsValueUpdate.New >> QsValueUpdate)
        | QsQubitScope s when isAllDiscarded s.Binding.Lhs -> s.Body.Statements |> Seq.map (fun x -> x.Statement)
        | QsQubitScope s ->
            let mutable newStatements = []

            let myList =
                jointFlatten (s.Binding.Lhs, s.Binding.Rhs)
                |> Seq.collect (fun (l, r) ->
                    match l, r.Resolution with
                    | VariableName name, QubitRegisterAllocation { Expression = IntLiteral num } ->
                        let elemI =
                            fun i -> Identifier(LocalVariable(sprintf "__qsItem%d__%s__" i name), Null)

                        let expr =
                            Seq.init (safeCastInt64 num) (elemI >> wrapExpr Qubit)
                            |> ImmutableArray.CreateRange
                            |> ValueArray
                            |> wrapExpr (ArrayType(ResolvedType.New Qubit))

                        let newStmt =
                            QsVariableDeclaration(QsBinding.New QsBindingKind.ImmutableBinding (l, expr))

                        newStatements <- wrapStmt newStmt :: newStatements

                        Seq.init (safeCastInt64 num) (fun i ->
                            VariableName(sprintf "__qsItem%d__%s__" i name),
                            ResolvedInitializer.New SingleQubitAllocation)
                    | DiscardedItem, _ -> Seq.empty
                    | _ -> Seq.singleton (l, r))
                |> List.ofSeq

            match myList with
            | [] -> newScopeStatement s.Body |> Seq.singleton
            | [ lhs, rhs ] ->
                let newBody =
                    QsScope.New(s.Body.Statements.InsertRange(0, newStatements), s.Body.KnownSymbols)

                QsQubitScope.New s.Kind ((lhs, rhs), newBody) |> QsQubitScope |> Seq.singleton
            | _ ->
                let lhs =
                    List.map fst myList |> ImmutableArray.CreateRange |> VariableNameTuple

                let rhs =
                    List.map snd myList |> ImmutableArray.CreateRange |> QubitTupleAllocation |> ResolvedInitializer.New

                let newBody =
                    QsScope.New(s.Body.Statements.InsertRange(0, newStatements), s.Body.KnownSymbols)

                QsQubitScope.New s.Kind ((lhs, rhs), newBody) |> QsQubitScope |> Seq.singleton
        | ScopeStatement s -> s.Body.Statements |> Seq.map (fun x -> x.Statement)
        | _ when not c.HasQuantum
                 && c2.ExternalMutations.IsEmpty
                 && not c.HasInterrupts
                 && (not c.HasOutput || removeFunctions) -> Seq.empty
        | a -> Seq.singleton a
