// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module internal Microsoft.Quantum.QsCompiler.Experimental.OptimizationTools

open System.Collections.Generic
open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Experimental.Utils
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core


/// private helper class for FindDistinctQubits
type private DistinctQubitsNamespaces (parent : QsSyntaxTreeTransformation<_>) = 
    inherit NamespaceTransformation<HashSet<NonNullable<string>>>(parent)

    override this.onProvidedImplementation (argTuple, body) =
        argTuple |> toSymbolTuple |> flatten |> Seq.iter (function
            | VariableName name -> this.Transformation.InternalState.Add name |> ignore
            | _ -> ())
        base.onProvidedImplementation (argTuple, body)

/// private helper class for FindDistinctQubits
type private DistinctQubitsStatementKinds (parent : QsSyntaxTreeTransformation<_>) = 
    inherit StatementKindTransformation<HashSet<NonNullable<string>>>(parent)

    override this.onQubitScope stm =
        stm.Binding.Lhs |> flatten |> Seq.iter (function
        | VariableName name -> this.Transformation.InternalState.Add name |> ignore
        | _ -> ())
        base.onQubitScope stm

/// A SyntaxTreeTransformation that finds identifiers in each implementation that represent distict values.
/// Should be called at the QsCallable level, not as the QsNamespace level, as it's meant to operate on a single callable.
type private FindDistinctQubits(unsafe) =
    inherit QsSyntaxTreeTransformation<HashSet<NonNullable<string>>>(new HashSet<_>())

    internal new () as this = 
        new FindDistinctQubits("unsafe") then
            this.Namespaces <- new DistinctQubitsNamespaces(this)
            this.StatementKinds <- new DistinctQubitsStatementKinds(this)


/// private helper class for MutationChecker representing the shared transformation state
type private MutationCheckerState () = 

    member val DeclaredVariables : Set<NonNullable<string>> = Set.empty with get, set
    member val MutatedVariables : Set<NonNullable<string>> = Set.empty with get, set

    /// Contains the set of variables that this code doesn't declare but does mutate.
    member this.ExternalMutations = this.MutatedVariables - this.DeclaredVariables

/// private helper class for MutationChecker
type private MutationCheckerStatementKinds(parent : QsSyntaxTreeTransformation<_>) = 
    inherit StatementKindTransformation<MutationCheckerState>(parent)

    override this.onVariableDeclaration stm =
        flatten stm.Lhs |> Seq.iter (function
            | VariableName name -> this.Transformation.InternalState.DeclaredVariables <- this.Transformation.InternalState.DeclaredVariables.Add name
            | _ -> ())
        base.onVariableDeclaration stm

    override this.onValueUpdate stm =
        match stm.Lhs with
        | LocalVarTuple v ->
            flatten v |> Seq.iter (function
                | VariableName name -> this.Transformation.InternalState.MutatedVariables <- this.Transformation.InternalState.MutatedVariables.Add name
                | _ -> ())
        | _ -> ()
        base.onValueUpdate stm

/// A ScopeTransformation that tracks what variables the transformed code could mutate
type private MutationChecker(unsafe) =
    inherit QsSyntaxTreeTransformation<MutationCheckerState>(new MutationCheckerState())

    internal new () as this = 
        new MutationChecker("unsafe") then
            this.StatementKinds <- new MutationCheckerStatementKinds(this)


/// private helper class for ReferenceCounter
type private ReferenceCounterExpressionKinds(parent : ReferenceCounter) = 
    inherit ExpressionKindTransformation<Dictionary<NonNullable<string>, int>>(parent)

    override this.onIdentifier (sym, tArgs) =
        match sym with
        | LocalVariable name -> this.Transformation.InternalState.[name] <- parent.NumberOfUses name + 1
        | _ -> ()
        base.onIdentifier (sym, tArgs)

/// A ScopeTransformation that counts how many times each variable is referenced
and internal ReferenceCounter(unsafe) =
    inherit QsSyntaxTreeTransformation<Dictionary<NonNullable<string>, int>>(new Dictionary<_,_>())

    /// Returns the number of times the variable with the given name is referenced
    member this.NumberOfUses name = 
        match this.InternalState.TryGetValue name with
        | true, nr -> nr
        | _ -> 0

    internal new () as this = 
        new ReferenceCounter("unsafe") then
            this.ExpressionKinds <- new ReferenceCounterExpressionKinds(this)


/// A ScopeTransformation that substitutes type parameters according to the given dictionary
type internal ReplaceTypeParams(typeParams: ImmutableDictionary<_, ResolvedType>) =
    inherit ScopeTransformation()

    override __.Expression = { new ExpressionTransformationBase() with
        override __.Types = { new TypeTransformationBase() with
            override __.onTypeParameter tp =
                let key = tp.Origin, tp.TypeName
                match typeParams.TryGetValue key with 
                | true, t -> t.Resolution
                | _ -> TypeKind.TypeParameter tp
            }
    }


/// A ScopeTransformation that tracks what side effects the transformed code could cause
type internal SideEffectChecker() =
    inherit ScopeTransformation()

    let mutable anyQuantum = false
    let mutable anyMutation = false
    let mutable anyInterrupts = false
    let mutable anyOutput = false

    /// Whether the transformed code might have any quantum side effects (such as calling operations)
    member __.hasQuantum = anyQuantum
    /// Whether the transformed code might change the value of any mutable variable
    member __.hasMutation = anyMutation
    /// Whether the transformed code has any statements that interrupt normal control flow (such as returns)
    member __.hasInterrupts = anyInterrupts
    /// Whether the transformed code might output any messages to the console
    member __.hasOutput = anyOutput

    override __.Expression = { new ExpressionTransformationBase() with
        override expr.ExpressionKinds = { new ExpressionKindTransformationBase() with
            override __.Expressions = expr

            override __.onFunctionCall (method, arg) =
                anyOutput <- true
                base.onFunctionCall (method, arg)

            override __.onOperationCall (method, arg) =
                anyQuantum <- true
                anyOutput <- true
                base.onOperationCall (method, arg)
        }
    }

    override this.StatementKind = { new StatementKindTransformation() with
        override __.ScopeTransformation s = this.Transform s
        override __.ExpressionTransformation ex = this.Expression.Transform ex
        override __.TypeTransformation t = t
        override __.LocationTransformation l = l

        override __.onValueUpdate stm =
            let mutatesState = match stm.Lhs with LocalVarTuple x when isAllDiscarded x -> false | _ -> true
            anyMutation <- anyMutation || mutatesState
            base.onValueUpdate stm

        override __.onReturnStatement stm =
            anyInterrupts <- true
            base.onReturnStatement stm

        override __.onFailStatement stm =
            anyInterrupts <- true
            base.onFailStatement stm
    }


/// A ScopeTransformation that replaces one statement with zero or more statements
type [<AbstractClass>] internal StatementCollectorTransformation() =
    inherit ScopeTransformation()

    abstract member TransformStatement: QsStatementKind -> QsStatementKind seq

    override this.Transform scope =
        let parentSymbols = scope.KnownSymbols
        let statements =
            scope.Statements
            |> Seq.map this.onStatement
            |> Seq.map (fun x -> x.Statement)
            |> Seq.collect this.TransformStatement
            |> Seq.map wrapStmt
        QsScope.New (statements, parentSymbols)


/// A SyntaxTreeTransformation that removes all known symbols from anywhere in the AST
type internal StripAllKnownSymbols() =
    inherit SyntaxTreeTransformation()

    override __.Scope = { new ScopeTransformation() with
        override this.Transform scope =
            QsScope.New (scope.Statements |> Seq.map this.onStatement, LocalDeclarations.Empty)
    }
