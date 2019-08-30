// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.CompilerOptimization.StatementRemoving

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open ComputationExpressions
open Utils
open VariableRenaming
open SideEffectChecking


/// The SyntaxTreeTransformation used to remove useless statements
type internal StatementRemover() =
    inherit SyntaxTreeTransformation()

    /// The VariableRenamer used to ensure unique variable names
    let mutable renamer = None



    let rec transformStatement stmt =
        let c = SideEffectChecker()
        c.StatementKind.Transform stmt |> ignore
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
        | QsScopeStatement s -> s.Body.Statements |> Seq.map (fun x -> x.Statement)
        | _ when not c.hasQuantum && not c.hasMutation && not c.hasInterrupts -> Seq.empty
        | a -> Seq.singleton a



    override syntaxTree.onProvidedImplementation (argTuple, body) =
        let renamerVal = VariableRenamer(argTuple)
        renamer <- Some renamerVal
        let body = renamerVal.Transform body

        let argTuple = syntaxTree.onArgumentTuple argTuple
        let body = syntaxTree.Scope.Transform body
        argTuple, body

    override __.Scope = { new ScopeTransformation() with

        override this.Transform scope =
            let parentSymbols = scope.KnownSymbols
            let statements =
                scope.Statements
                |> Seq.map this.onStatement
                |> Seq.map (fun x -> x.Statement)
                |> Seq.collect transformStatement
                |> Seq.map wrapStmt
            QsScope.New (statements, parentSymbols)

        override this.StatementKind = { new StatementKindTransformation() with
            override __.ExpressionTransformation x = this.Expression.Transform x
            override __.LocationTransformation x = this.onLocation x
            override __.ScopeTransformation x = this.Transform x
            override __.TypeTransformation x = this.Expression.Type.Transform x

            override stmtKind.onSymbolTuple syms =
                match syms with
                | VariableName item ->
                    maybe {
                        let! r = renamer
                        let! uses = r.getNumUses item.Value
                        do! check (uses = 0)
                        return DiscardedItem
                    } |? syms
                | VariableNameTuple items -> Seq.map stmtKind.onSymbolTuple items |> ImmutableArray.CreateRange |> VariableNameTuple
                | InvalidItem | DiscardedItem -> syms
        }
    }
