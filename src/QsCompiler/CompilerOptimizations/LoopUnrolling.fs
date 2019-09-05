// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.CompilerOptimization.LoopUnrolling

open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open ComputationExpressions
open Utils
open OptimizingTransformation


/// The SyntaxTreeTransformation used to unroll loops
type internal LoopUnroller(callables, maxSize) =
    inherit OptimizingTransformation()

    override __.Scope = { new ScopeTransformation() with
        override scope.StatementKind = { new StatementKindTransformation() with
            override __.ExpressionTransformation x = scope.Expression.Transform x
            override __.LocationTransformation x = scope.onLocation x
            override __.ScopeTransformation x = scope.Transform x
            override __.TypeTransformation x = scope.Expression.Type.Transform x

            override this.onForStatement stm =
                let loopVar = fst stm.LoopItem |> this.onSymbolTuple
                let iterVals = this.ExpressionTransformation stm.IterationValues
                let loopVarType = this.TypeTransformation (snd stm.LoopItem)
                let body = this.ScopeTransformation stm.Body
                maybe {
                    let! iterValsList =
                        match iterVals.Expression with
                        | RangeLiteral _ when isLiteral callables iterVals ->
                            rangeLiteralToSeq iterVals.Expression |> Seq.map (IntLiteral >> wrapExpr Int) |> List.ofSeq |> Some
                        | ValueArray va -> va |> List.ofSeq |> Some
                        | _ -> None
                    do! check (iterValsList.Length <= maxSize)
                    let iterRange = iterValsList |> List.map (fun x ->
                        let variableDecl = QsBinding.New ImmutableBinding (loopVar, x) |> QsVariableDeclaration |> wrapStmt
                        let innerScope = { stm.Body with Statements = stm.Body.Statements.Insert(0, variableDecl) }
                        innerScope |> QsScopeStatement.New |> QsScopeStatement |> wrapStmt)
                    let outerScope = QsScope.New (iterRange, stm.Body.KnownSymbols)
                    return outerScope |> QsScopeStatement.New |> QsScopeStatement |> this.Transform
                }
                |? (QsForStatement.New ((loopVar, loopVarType), iterVals, body) |> QsForStatement)
        }
    }
