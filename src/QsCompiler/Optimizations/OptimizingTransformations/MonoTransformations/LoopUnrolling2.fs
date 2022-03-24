// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Experimental

#if MONO

open Microsoft.Quantum.QsCompiler.Experimental.Utils
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree


/// The SyntaxTreeTransformation used to unroll loops
type LoopUnrolling (callables, maxSize) =
    inherit TransformationBase()

    override __.OnNamespace x =
        let x = base.OnNamespace x
        VariableRenaming().OnNamespace x

    override this.OnForStatement stm =
        let loopVar = fst stm.LoopItem |> this.OnSymbolTuple
        let iterVals = this.OnTypedExpression stm.IterationValues
        let loopVarType = this.OnType(snd stm.LoopItem)
        let body = this.OnScope stm.Body

        maybe {
            let! iterValsList =
                match iterVals.Expression with
                | RangeLiteral _ when isLiteral callables iterVals ->
                    rangeLiteralToSeq iterVals.Expression |> Seq.map (IntLiteral >> wrapExpr Int) |> List.ofSeq |> Some
                | ValueArray va -> va |> List.ofSeq |> Some
                | _ -> None

            do! check (iterValsList.Length <= maxSize)

            let iterRange =
                iterValsList
                |> List.map (fun x ->
                    let variableDecl = QsBinding.New ImmutableBinding (loopVar, x) |> QsVariableDeclaration |> wrapStmt

                    let innerScope = { stm.Body with Statements = stm.Body.Statements.Insert(0, variableDecl) }
                    innerScope |> newScopeStatement |> wrapStmt)

            let outerScope = QsScope.New(iterRange, stm.Body.KnownSymbols)
            return outerScope |> newScopeStatement |> this.OnStatementKind
        }
        |? (QsForStatement.New((loopVar, loopVarType), iterVals, body) |> QsForStatement)

#endif
