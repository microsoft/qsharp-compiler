// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.CompilerOptimization.StatementRemoving

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree

open Utils
open MinorTransformations


/// The SyntaxTreeTransformation used to remove useless statements
type internal StatementRemover() =
    inherit OptimizingTransformation()

    override __.Scope = upcast { new StatementCollectorTransformation() with

        /// Given a statement, returns a sequence of statements to replace this statement with.
        /// Removes useless statements, such as variable declarations with discarded values.
        /// Simplifies statements, such as replacing ScopeStatements with their contents.
        /// Splits QsQubitScopes by replacing register allocations with single qubit allocations.
        override __.TransformStatement stmt =
            let c = SideEffectChecker()
            c.StatementKind.Transform stmt |> ignore

            let c2 = MutationChecker()
            c2.StatementKind.Transform stmt |> ignore

            match stmt with
            | QsVariableDeclaration {Lhs = lhs}
            | QsValueUpdate {Lhs = LocalVarTuple lhs}
                when isAllDiscarded lhs && not c.hasQuantum -> Seq.empty
            | QsVariableDeclaration s ->
                jointFlatten (s.Lhs, s.Rhs) |> Seq.map (QsBinding.New s.Kind >> QsVariableDeclaration)
            | QsValueUpdate s ->
                jointFlatten (s.Lhs, s.Rhs) |> Seq.map (QsValueUpdate.New >> QsValueUpdate)
            | QsQubitScope s when isAllDiscarded s.Binding.Lhs ->
                s.Body.Statements |> Seq.map (fun x -> x.Statement)
            | QsQubitScope s ->
                let mutable newStatements = []
                let myList = jointFlatten (s.Binding.Lhs, s.Binding.Rhs) |> Seq.collect (fun (l, r) ->
                    match l, r.Resolution with
                    | VariableName name, QubitRegisterAllocation {Expression = IntLiteral num} ->
                        let elemI = fun i -> Identifier (LocalVariable (NonNullable<_>.New (sprintf "__qsItem%d__%s__" i name.Value)), Null)
                        let expr = Seq.init (safeCastInt64 num) (elemI >> wrapExpr Qubit) |> ImmutableArray.CreateRange |> ValueArray |> wrapExpr (ArrayType (ResolvedType.New Qubit))
                        let newStmt = QsVariableDeclaration (QsBinding.New QsBindingKind.ImmutableBinding (l, expr))
                        newStatements <- wrapStmt newStmt :: newStatements
                        Seq.init (safeCastInt64 num) (fun i ->
                            VariableName (NonNullable<_>.New (sprintf "__qsItem%d__%s__" i name.Value)),
                            ResolvedInitializer.New SingleQubitAllocation)
                    | DiscardedItem, _ -> Seq.empty
                    | _ -> Seq.singleton (l, r)) |> List.ofSeq
                match myList with
                | [] -> newScopeStatement s.Body |> Seq.singleton
                | [lhs, rhs] ->
                    let newBody = QsScope.New (s.Body.Statements.InsertRange (0, newStatements), s.Body.KnownSymbols)
                    QsQubitScope.New s.Kind ((lhs, rhs), newBody) |> QsQubitScope |> Seq.singleton
                | _ ->
                    let lhs = List.map fst myList |> ImmutableArray.CreateRange |> VariableNameTuple
                    let rhs = List.map snd myList |> ImmutableArray.CreateRange |> QubitTupleAllocation |> ResolvedInitializer.New
                    let newBody = QsScope.New (s.Body.Statements.InsertRange (0, newStatements), s.Body.KnownSymbols)
                    QsQubitScope.New s.Kind ((lhs, rhs), newBody) |> QsQubitScope |> Seq.singleton
            | ScopeStatement s -> s.Body.Statements |> Seq.map (fun x -> x.Statement)
            | _ when not c.hasQuantum && c2.externalMutations.IsEmpty && not c.hasInterrupts && not c.hasOutput -> Seq.empty
            | a -> Seq.singleton a
    }
