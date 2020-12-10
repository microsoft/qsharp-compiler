// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Experimental

open System.Collections.Generic
open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.Experimental.OptimizationTools
open Microsoft.Quantum.QsCompiler.Experimental.Utils
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations


/// Represents all the functors applied to an operation call
type private Functors =
    {
        adjoint: bool
        controlled: int
    }
    static member None = { adjoint = false; controlled = 0 }

    member this.toSpecKind =
        match this.adjoint, this.controlled with
        | false, 0 -> QsBody
        | true, 0 -> QsAdjoint
        | false, _ -> QsControlled
        | true, _ -> QsControlledAdjoint

    member this.withAdjoint = { this with adjoint = not this.adjoint }
    member this.withControlled = { this with controlled = this.controlled + 1 }


/// Stores all the data needed to inline a callable
type private InliningInfo =
    {
        functors: Functors
        callable: QsCallable
        arg: TypedExpression
        specArgs: QsTuple<LocalVariableDeclaration<QsLocalSymbol>>
        body: QsScope
        returnType: ResolvedType
    }

    /// Tries to decompose a method expression into the method name and the functors applied.
    /// Assumes the input is zero or more functors applied to a global callable identifier.
    /// Returns None if the input is not a valid expression of this form.
    static member private TryGetQualNameAndFunctors method =
        match method.Expression with
        | Identifier (GlobalCallable qualName, _) -> Some(qualName, Functors.None)
        | AdjointApplication ex ->
            InliningInfo.TryGetQualNameAndFunctors ex
            |> Option.map (fun (qualName, functors) -> qualName, functors.withAdjoint)
        | ControlledApplication ex ->
            InliningInfo.TryGetQualNameAndFunctors ex
            |> Option.map (fun (qualName, functors) -> qualName, functors.withControlled)
        | _ -> None

    /// Tries to split a callable invocation into the functors applied, the callable, and the argument.
    /// Assumes the input is zero or more functors applied to a global callable identifier,
    /// applied to an expression representing the argument to the callable.
    /// Returns None if the input is not a valid expression of this form.
    static member private TrySplitCall(callables: ImmutableDictionary<QsQualifiedName, QsCallable>) =
        function
        | x when TypedExpression.IsPartialApplication x -> None
        | CallLikeExpression (method, arg) ->
            InliningInfo.TryGetQualNameAndFunctors method
            |> Option.map (fun (qualName, functors) -> functors, callables.[qualName], arg)
        | _ -> None

    /// Tries to find a specialization of the given callable that matches the given functors.
    /// If such a specialization is found, returns the implementation of that specialization.
    /// If no such specialization is found, returns None.
    static member private TryGetImpl callable (functors: Functors) =
        callable.Specializations
        |> Seq.tryFind (fun s -> s.Kind = functors.toSpecKind)
        |> Option.map (fun s -> s.Implementation)

    /// Tries to find a provided implementation for the given callable with the given specialization kind.
    /// Returns None if unable to find a provided implementation of the desired kind.
    static member private TryGetProvidedImpl callable functors =
        match InliningInfo.TryGetImpl callable functors, InliningInfo.TryGetImpl callable functors.withAdjoint with
        | Some (Provided (specArgs, body)), _
        | Some (Generated SelfInverse), Some (Provided (specArgs, body)) -> Some(specArgs, body)
        | _ -> None

    /// Tries to construct an InliningInfo from the given expression.
    /// Returns None if the expression is not a callable invocation that can be inlined.
    static member internal TryGetInfo callables expr =
        maybe {
            let! functors, callable, arg = InliningInfo.TrySplitCall callables expr.Expression
            let! specArgs, body = InliningInfo.TryGetProvidedImpl callable functors
            let body = ReplaceTypeParams(expr.TypeParameterResolutions).Statements.OnScope body

            let returnType =
                ReplaceTypeParams(expr.TypeParameterResolutions).Types.OnType callable.Signature.ReturnType

            return
                {
                    functors = functors
                    callable = callable
                    arg = arg
                    specArgs = specArgs
                    body = body
                    returnType = returnType
                }
        }


/// The SyntaxTreeTransformation used to inline callables
type CallableInlining private (_private_: string) =
    inherit TransformationBase()

    // The current callable we're in the process of transforming
    member val CurrentCallable: QsCallable option = None with get, set
    member val Renamer: VariableRenaming option = None with get, set

    new(callables) as this =
        new CallableInlining("_private_")
        then
            this.Namespaces <- new CallableInliningNamespaces(this)
            this.Statements <- new CallableInliningStatements(this, callables)
            this.Expressions <- new Core.ExpressionTransformation(this, Core.TransformationOptions.Disabled)
            this.Types <- new Core.TypeTransformation(this, Core.TransformationOptions.Disabled)

/// private helper class for CallableInlining
and private CallableInliningNamespaces(parent: CallableInlining) =
    inherit NamespaceTransformationBase(parent)

    override __.OnNamespace x =
        let x = base.OnNamespace x
        VariableRenaming().Namespaces.OnNamespace x

    override __.OnCallableDeclaration c =
        let renamerVal = VariableRenaming()
        let c = renamerVal.Namespaces.OnCallableDeclaration c
        parent.CurrentCallable <- Some c
        parent.Renamer <- Some renamerVal
        base.OnCallableDeclaration c

/// private helper class for CallableInlining
and private CallableInliningStatements(parent: CallableInlining, callables: ImmutableDictionary<_, _>) =
    inherit StatementCollectorTransformation(parent)

    /// Recursively finds all the callables that could be inlined into the given scope.
    /// Includes callables that are invoked within the implementation of another call.
    /// Eg. if function f invokes function g, and function g invokes function h,
    /// then findAllCalls f will include both g and h (and possibly other callables).
    /// Mutates the given HashSet by adding all the found callables to the set.
    /// Is used to prevent inlining recursive functions into themselves forever.
    let rec findAllCalls (callables: ImmutableDictionary<QsQualifiedName, QsCallable>)
                         (scope: QsScope)
                         (found: HashSet<QsQualifiedName>)
                         : unit =
        scope
        |> findAllSubStatements
        |> Seq.iter (function
            | QsExpressionStatement ex ->
                match InliningInfo.TryGetInfo callables ex with
                | Some ii ->
                    // Only recurse if we haven't processed this callable yet
                    if found.Add ii.callable.FullName then findAllCalls callables ii.body found
                | None -> ()
            | _ -> ())

    /// Returns whether the given callable could eventually inline the given callable.
    /// Is used to prevent inlining recursive functions into themselves forever.
    let cannotReachCallable (scope: QsScope) (cannotReach: QsQualifiedName) =
        let mySet = HashSet()
        findAllCalls callables scope mySet
        not (mySet.Contains cannotReach)

    let tryInline expr =
        maybe {
            let! ii = InliningInfo.TryGetInfo callables expr

            let! currentCallable = parent.CurrentCallable
            let! renamer = parent.Renamer
            renamer.RenamingStack <- [ Map.empty ]
            do! check (cannotReachCallable ii.body currentCallable.FullName)
            do! check (ii.functors.controlled < 2)
            // TODO - support multiple Controlled functors
            do! check (cannotReachCallable ii.body ii.callable.FullName || isLiteral callables ii.arg)

            let newBinding = QsBinding.New ImmutableBinding (toSymbolTuple ii.specArgs, ii.arg)

            let newStatements =
                ii.body.Statements.Insert(0, newBinding |> QsVariableDeclaration |> wrapStmt)
                |> Seq.map renamer.Statements.OnStatement
                |> Seq.map (fun s -> s.Statement)
                |> ImmutableArray.CreateRange

            return ii, newStatements
        }

    /// Inline an expression representing a callable with no return statements.
    /// Returns None if the expression cannot be safely inlined.
    let safeInline expr =
        maybe {
            let! ii, newStatements = tryInline expr
            do! check (countReturnStatements ii.body = 0)
            return newStatements
        }

    /// Inline an expression representing a callable with exactly one return statement.
    /// This single return statement must be at the end of the function body, as otherwise
    /// there's either dead code or an implicit return statement at the end of the body.
    /// Returns None if the expression cannot be safely inlined.
    let safeInlineReturn expr =
        maybe {
            let! ii, newStatements = tryInline expr

            do! check (countReturnStatements ii.body = 1)
            let lastStatement = newStatements.[newStatements.Length - 1]

            let! returnExpr =
                match lastStatement with
                | QsReturnStatement ex -> Some ex
                | _ -> None

            let newStatements = newStatements.RemoveAt(newStatements.Length - 1)

            return newStatements, returnExpr
        }

    /// Given a statement, returns a sequence of statements to replace this statement with.
    /// Inlines simple calls that have exactly 0 or 1 return statements.
    override __.CollectStatements stmt =
        maybe {
            match stmt with
            | QsExpressionStatement ex ->
                match safeInline ex with
                | Some stmts -> return upcast stmts
                | None ->
                    let! stmts, returnExpr = safeInlineReturn ex
                    return Seq.append stmts [ QsExpressionStatement returnExpr ]
            | QsVariableDeclaration s ->
                let! stmts, returnExpr = safeInlineReturn s.Rhs
                return Seq.append stmts [ QsVariableDeclaration { s with Rhs = returnExpr } ]
            | QsValueUpdate s ->
                let! stmts, returnExpr = safeInlineReturn s.Rhs
                return Seq.append stmts [ QsValueUpdate { s with Rhs = returnExpr } ]
            | _ -> return! None
        }
        |? Seq.singleton stmt
