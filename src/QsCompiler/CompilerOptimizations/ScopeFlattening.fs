module Microsoft.Quantum.QsCompiler.CompilerOptimization.ScopeFlattening

open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open OptimizingTransformation


let isVarDecl = function
| {Statement = QsVariableDeclaration _} -> true
| _ -> false


type ScopeFlattener() =
    inherit OptimizingTransformation()

    override syntaxTree.Scope = { new ScopeTransformation() with
        override this.Transform scope =
            let parentSymbols = scope.KnownSymbols
            let statements =
                scope.Statements
                |> Seq.map this.onStatement
                |> Seq.collect (function
                    | {Statement = QsScopeStatement s} when s.Body.Statements.IsEmpty -> Seq.empty
                    | {Statement = QsScopeStatement s} when s.Body.Statements |> Seq.exists isVarDecl |> not -> upcast s.Body.Statements
                    | a -> Seq.singleton a)
            QsScope.New (statements, parentSymbols)
    }
