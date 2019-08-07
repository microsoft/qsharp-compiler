module Microsoft.Quantum.QsCompiler.CompilerOptimization.StatementRemoving

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open ComputationExpressions
open Utils
open OptimizingTransformation
open VariableRenaming


let rec isAllDiscarded = function
| DiscardedItem -> true
| VariableNameTuple items -> Seq.forall isAllDiscarded items
| _ -> false


type StatementRemover() =
    inherit OptimizingTransformation()

    let mutable renamer = None

    let isStatementNeeded stmt =
        match stmt.Statement with
        | QsVariableDeclaration s when isAllDiscarded s.Lhs && not (s.Rhs.InferredInformation.HasLocalQuantumDependency) -> false
        | QsScopeStatement s when s.Body.Statements.IsEmpty -> false
        | QsForStatement s when s.Body.Statements.IsEmpty -> false
        | QsWhileStatement s when s.Body.Statements.IsEmpty -> false
        | _ -> true


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
                |> Seq.collect (function
                    | {Statement = QsScopeStatement s} -> s.Body.Statements |> Seq.cast
                    | a -> Seq.singleton a)
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
        }
    }
