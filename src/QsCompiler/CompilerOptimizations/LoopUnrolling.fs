module Microsoft.Quantum.QsCompiler.CompilerOptimization.LoopUnrolling

open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open ComputationExpressions
open Utils
open OptimizingTransformation


type LoopUnroller() =
    inherit OptimizingTransformation()

    override syntaxTree.Scope = { new ScopeTransformation() with
        override scope.StatementKind = { new StatementKindTransformation() with
            override stmtKind.ExpressionTransformation x = scope.Expression.Transform x
            override stmtKind.LocationTransformation x = scope.onLocation x
            override stmtKind.ScopeTransformation x = scope.Transform x
            override stmtKind.TypeTransformation x = scope.Expression.Type.Transform x

            override stmtKind.onForStatement stm =
                let loopVar = fst stm.LoopItem |> stmtKind.onSymbolTuple
                let iterVals = stmtKind.ExpressionTransformation stm.IterationValues
                let loopVarType = stmtKind.TypeTransformation (snd stm.LoopItem)
                let body = stmtKind.ScopeTransformation stm.Body
                maybe {
                    let! iterValsList =
                        match iterVals.Expression with
                        | RangeLiteral _ ->
                            rangeLiteralToSeq iterVals.Expression |> Seq.map (IntLiteral >> wrapExpr Int) |> List.ofSeq |> Some
                        | ValueArray va -> va |> List.ofSeq |> Some
                        | _ -> None
                    do! check (iterValsList.Length <= 100)
                    let iterRange = iterValsList |> List.map (fun x ->
                        let variableDecl = QsBinding.New ImmutableBinding (loopVar, x) |> QsVariableDeclaration |> wrapStmt
                        let innerScope = { stm.Body with Statements = stm.Body.Statements.Insert(0, variableDecl) }
                        innerScope |> QsScopeStatement.New |> QsScopeStatement |> wrapStmt)
                    let outerScope = QsScope.New (iterRange, stm.Body.KnownSymbols)
                    return outerScope |> QsScopeStatement.New |> QsScopeStatement |> stmtKind.Transform
                }
                |? (QsForStatement.New ((loopVar, loopVarType), iterVals, body) |> QsForStatement)
        }
    }
