module Microsoft.Quantum.QsCompiler.CompilerOptimization.CallableInlining

open System.Collections.Generic
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open ComputationExpressions
open Types
open Utils
open OptimizingTransformation


let getMethodArg expr =
    match expr with
    | CallLikeExpression (method, arg) -> Some (method, arg)
    | _ -> None

let getQualNameAndSpecKind method =
    match method.Expression with
    | Identifier (GlobalCallable qualName, _) -> Some (qualName, QsBody)
    | AdjointApplication {Expression = Identifier (GlobalCallable qualName, _)} -> Some (qualName, QsAdjoint)
    | ControlledApplication {Expression = Identifier (GlobalCallable qualName, _)} -> Some (qualName, QsControlled)
    | AdjointApplication {Expression = ControlledApplication {Expression = Identifier (GlobalCallable qualName, _)}} -> Some (qualName, QsControlledAdjoint)
    | ControlledApplication {Expression = AdjointApplication {Expression = Identifier (GlobalCallable qualName, _)}} -> Some (qualName, QsControlledAdjoint)
    | _ -> None

let getScope callable specKind =
    match callable.Specializations |> Seq.tryFind (fun s -> s.Kind = specKind) with
    | Some {Implementation = Provided (specArgs, scope)} -> Some (specArgs, scope)
    | Some {Implementation = Generated SelfInverse} ->
        let newKind = match specKind with QsAdjoint -> Some QsBody | QsControlledAdjoint -> Some QsControlled | _ -> None
        match callable.Specializations |> Seq.tryFind (fun s -> Some s.Kind = newKind) with
        | Some {Implementation = Provided (specArgs, scope)} -> Some (specArgs, scope)
        | _ -> None
    | _ -> None

let tryInline (callables: Callables) (expr: Expr) =
    maybe {
        let! method, arg = getMethodArg expr
        let! qualName, specKind = getQualNameAndSpecKind method
        let callable = getCallable callables qualName
        let! specArgs, scope = getScope callable specKind
        return callable, arg, specArgs, scope
    }

let rec findAllCalls (callables: Callables) (scope: QsScope) (found: HashSet<QsQualifiedName>): unit =
    scope |> findAllBaseStatements |> Seq.map (function
        | QsExpressionStatement ex ->
            match tryInline callables ex.Expression with
            | Some (callable, _, _, newScope) ->
                if found.Add callable.FullName then
                    findAllCalls callables newScope found
            | None -> ()
        | _ -> ()
    ) |> List.ofSeq |> ignore

let callableCannotReach (callables: Callables) (scope: QsScope) (cannotReach: QsQualifiedName) =
    let mySet = HashSet()
    findAllCalls callables scope mySet
    not (mySet.Contains cannotReach)


type CallableInliner(compiledCallables) =
    inherit OptimizingTransformation()
    
    let callables = makeCallables compiledCallables

    let mutable currentCallable: QsCallable option = None
    let mutable statementsToAdd = []

    let safeInline expr =
        maybe {
            let! callable, arg, specArgs, scope = tryInline callables expr
            
            do! check (countReturnStatements scope = 0)
            let! current = currentCallable
            do! check (callableCannotReach callables scope current.FullName)
            
            let newBinding = QsBinding.New ImmutableBinding (toSymbolTuple callable.ArgumentTuple, arg)
            let newStatements = scope.Statements.Insert (0, newBinding |> QsVariableDeclaration |> wrapStmt)
            return {scope with Statements = newStatements} |> QsScopeStatement.New |> QsScopeStatement
        }


    override this.onCallableImplementation c =
        let prev = currentCallable
        currentCallable <- Some c
        let result = base.onCallableImplementation c
        currentCallable <- prev
        result

    override syntaxTree.Scope = { new ScopeTransformation() with

        override scope.Expression = { new ExpressionTransformation() with
            override expr.Kind = { new ExpressionKindTransformation() with 
                override exprKind.ExpressionTransformation ex = expr.Transform ex
                override exprKind.TypeTransformation t = expr.Type.Transform t

                override exprKind.onOperationCall (method, arg) =
                    let expr = CallLikeExpression (exprKind.ExpressionTransformation method, exprKind.ExpressionTransformation arg)
                    maybe {
                        let! scopeStatement = safeInline expr
                        statementsToAdd <- statementsToAdd @ [wrapStmt scopeStatement]
                        return UnitValue
                    } |? expr

                override this.onConditionalExpression (cond, ifTrue, ifFalse) =
                    CONDITIONAL (this.ExpressionTransformation cond, ifTrue, ifFalse)

                override this.onLogicalAnd (lhs, rhs) =
                    AND (this.ExpressionTransformation lhs, rhs)

                override this.onLogicalOr (lhs, rhs) =
                    OR (this.ExpressionTransformation lhs, rhs)
            }
        }

        override scope.StatementKind = { new StatementKindTransformation() with
            override stmtKind.ExpressionTransformation x = scope.Expression.Transform x
            override stmtKind.LocationTransformation x = scope.onLocation x
            override stmtKind.ScopeTransformation x = scope.Transform x
            override stmtKind.TypeTransformation x = scope.Expression.Type.Transform x

            override this.onExpressionStatement ex =
                match safeInline ex.Expression with
                | Some result -> result
                | None -> QsExpressionStatement (this.ExpressionTransformation ex)
        }
    }
