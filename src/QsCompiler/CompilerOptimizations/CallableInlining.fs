// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.CompilerOptimization.CallableInlining

open System.Collections.Generic
open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open ComputationExpressions
open Utils
open OptimizingTransformation


/// Represents all the functors applied to an operation call
type private Functors = {
    adjoint: bool
    controlled: int
} with
    static member None = { adjoint = false; controlled = 0 }
    member this.toSpecKind =
        match this.adjoint, this.controlled with
        | false, 0 -> QsBody
        | true, 0 -> QsAdjoint
        | false, _ -> QsControlled
        | true, _ -> QsControlledAdjoint
    member this.withAdjoint = { this with adjoint = not this.adjoint }
    member this.withControlled = { this with controlled = this.controlled + 1 }


/// Tries to decompose a method expression into the method name and the functors applied.
/// Assumes the input is zero or more functors applied to a global callable identifier.
/// Returns None if the input is not a valid expression of this form.
let rec private tryGetQualNameAndFunctors method =
    match method.Expression with
    | Identifier (GlobalCallable qualName, _) -> Some (qualName, Functors.None)
    | AdjointApplication ex ->
        tryGetQualNameAndFunctors ex |> Option.map (fun (qualName, functors) -> qualName, functors.withAdjoint)
    | ControlledApplication ex ->
        tryGetQualNameAndFunctors ex |> Option.map (fun (qualName, functors) -> qualName, functors.withControlled)
    | _ -> None


/// Tries to split a callable invocation into the functors applied, the callable, and the argument.
/// Assumes the input is zero or more functors applied to a global callable identifier,
/// applied to an expression representing the argument to the callable.
/// Returns None if the input is not a valid expression of this form.
let private trySplitCall (callables: Callables) = function
    | CallLikeExpression (method, arg) ->
        tryGetQualNameAndFunctors method |> Option.map (fun (qualName, functors) ->
            functors, callables.get qualName, arg)
    | _ -> None


/// Tries to find a specialization of the given callable that matches the given functors.
/// If such a specialization is found, returns the implementation of that specialization.
/// If no such specialization is found, returns None.
let private tryGetImpl callable (functors: Functors) =
    callable.Specializations
    |> Seq.tryFind (fun s -> s.Kind = functors.toSpecKind)
    |> Option.map (fun s -> s.Implementation)

/// Tries to find a provided implementation for the given callable with the given specialization kind.
/// Returns None if unable to find a provided implementation of the desired kind.
let private tryGetProvidedImpl callable functors =
    match tryGetImpl callable functors, tryGetImpl callable functors.withAdjoint with
    | Some (Provided (specArgs, body)), _
    | Some (Generated SelfInverse), Some (Provided (specArgs, body)) -> Some (specArgs, body)
    | _ -> None


/// Stores all the data needed to inline a callable
type private InliningInfo = {
    functors: Functors
    callable: QsCallable
    arg: TypedExpression
    specArgs: QsArgumentTuple
    body: QsScope
}

/// A scope transformation that substitutes type parameters according to the given dictionary
type private ReplaceTypeParams(typeParams: ImmutableDictionary<QsTypeParameter, ResolvedType>) =
    inherit ScopeTransformation()

    override __.Expression = { new ExpressionTransformation() with
        override __.Type = { new ExpressionTypeTransformation() with
            override this.onTypeParameter tp =
                if typeParams.ContainsKey tp then
                    typeParams.[tp].Resolution
                else
                    let origin = tp.Origin
                    let name = tp.TypeName
                    let range = this.onRangeInformation tp.Range
                    QsTypeParameter.New (origin.Namespace, origin.Name, name, range) |> TypeParameter
        }
    }

/// Tries to construct an InliningInfo from the given expression.
/// Returns None if the expression is not a callable invocation that can be inlined.
let private tryGetInliningInfo callables expr =
    maybe {
        let! functors, callable, arg = trySplitCall callables expr.Expression
        let! specArgs, body = tryGetProvidedImpl callable functors
        let body = ReplaceTypeParams(expr.TypeParameterResolutions).Transform body
        return { functors = functors; callable = callable; arg = arg; specArgs = specArgs; body = body }
    }


/// Recursively finds all the callables that could be inlined into the given scope.
/// Includes callables that are invoked within the implementation of another call.
/// Eg. if function f invokes function g, and function g invokes function h,
/// then findAllCalls f will include both g and h (and possibly other callables).
/// Mutates the given HashSet by adding all the found callables to the set.
/// Is used to prevent inlining recursive functions into themselves forever.
let rec private findAllCalls (callables: Callables) (scope: QsScope) (found: HashSet<QsQualifiedName>): unit =
    scope |> findAllBaseStatements |> Seq.iter (function
        | QsExpressionStatement ex ->
            match tryGetInliningInfo callables ex with
            | Some ii ->
                // Only recurse if we haven't processed this callable yet
                if found.Add ii.callable.FullName then
                    findAllCalls callables ii.body found
            | None -> ()
        | _ -> ())

/// Returns whether the given callable could eventually inline the given callable.
/// Is used to prevent inlining recursive functions into themselves forever.
let private cannotReachCallable (callables: Callables) (scope: QsScope) (cannotReach: QsQualifiedName) =
    let mySet = HashSet()
    findAllCalls callables scope mySet
    not (mySet.Contains cannotReach)


/// The SyntaxTreeTransformation used to inline callables
type internal CallableInliner(callables) =
    inherit OptimizingTransformation()

    // The current callable we're in the process of transforming
    let mutable currentCallable: QsCallable option = None

    /// Inline an expression as a ScopeStatement, with many checks to ensure correctness.
    /// Returns None if the expression cannot be safely inlined.
    let safeInline expr =
        maybe {
            let! ii = tryGetInliningInfo callables expr

            do! check (countReturnStatements ii.body = 0)
            let! current = currentCallable
            do! check (cannotReachCallable callables ii.body current.FullName)
            do! check (ii.functors.controlled < 2)
            // TODO - support multiple Controlled functors
            do! check (cannotReachCallable callables ii.body ii.callable.FullName || isLiteral callables ii.arg)

            let newBinding = QsBinding.New ImmutableBinding (toSymbolTuple ii.specArgs, ii.arg)
            let newStatements = ii.body.Statements.Insert (0, newBinding |> QsVariableDeclaration |> wrapStmt)
            return {ii.body with Statements = newStatements} |> newScopeStatement
        }


    override __.onCallableImplementation c =
        let prev = currentCallable
        currentCallable <- Some c
        let result = base.onCallableImplementation c
        currentCallable <- prev
        result

    override __.Scope = { new ScopeTransformation() with

        override this.StatementKind = { new StatementKindTransformation() with
            override __.ExpressionTransformation x = this.Expression.Transform x
            override __.LocationTransformation x = this.onLocation x
            override __.ScopeTransformation x = this.Transform x
            override __.TypeTransformation x = this.Expression.Type.Transform x

            override __.onExpressionStatement ex =
                safeInline ex |? QsExpressionStatement ex
        }
    }
