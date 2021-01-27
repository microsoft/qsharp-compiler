// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module internal Microsoft.Quantum.QsCompiler.Experimental.OptimizationTools

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.Experimental.Utils
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations


/// A SyntaxTreeTransformation that finds identifiers in each implementation that represent distict qubit values.
/// Should be called at the specialization level, as it's meant to operate on a single implementation.
type internal FindDistinctQubits private (_private_) =
    inherit Core.SyntaxTreeTransformation()

    member val DistinctNames: Set<string> = Set.empty with get, set

    internal new() as this =
        new FindDistinctQubits("_private_")
        then
            this.Namespaces <- new DistinctQubitsNamespaces(this)
            this.StatementKinds <- new DistinctQubitsStatementKinds(this)
            this.Expressions <- new Core.ExpressionTransformation(this, Core.TransformationOptions.Disabled)
            this.Types <- new Core.TypeTransformation(this, Core.TransformationOptions.Disabled)

/// private helper class for FindDistinctQubits
and private DistinctQubitsNamespaces(parent: FindDistinctQubits) =
    inherit Core.NamespaceTransformation(parent)

    override this.OnProvidedImplementation(argTuple, body) =
        argTuple
        |> toSymbolTuple
        |> flatten
        |> Seq.iter (function
            | VariableName name -> parent.DistinctNames <- parent.DistinctNames.Add name
            | _ -> ())

        base.OnProvidedImplementation(argTuple, body)

/// private helper class for FindDistinctQubits
and private DistinctQubitsStatementKinds(parent: FindDistinctQubits) =
    inherit Core.StatementKindTransformation(parent)

    override this.OnQubitScope stm =
        stm.Binding.Lhs
        |> flatten
        |> Seq.iter (function
            | VariableName name -> parent.DistinctNames <- parent.DistinctNames.Add name
            | _ -> ())

        base.OnQubitScope stm


/// A transformation that tracks what variables the transformed code could mutate.
/// Should be called at the specialization level, as it's meant to operate on a single implementation.
type internal MutationChecker private (_private_) =
    inherit Core.SyntaxTreeTransformation()

    member val DeclaredVariables: Set<string> = Set.empty with get, set
    member val MutatedVariables: Set<string> = Set.empty with get, set

    /// Contains the set of variables that this code doesn't declare but does mutate.
    member this.ExternalMutations = this.MutatedVariables - this.DeclaredVariables

    internal new() as this =
        new MutationChecker("_private_")
        then
            this.StatementKinds <- new MutationCheckerStatementKinds(this)
            this.Expressions <- new Core.ExpressionTransformation(this, Core.TransformationOptions.Disabled)
            this.Types <- new Core.TypeTransformation(this, Core.TransformationOptions.Disabled)

/// private helper class for MutationChecker
and private MutationCheckerStatementKinds(parent: MutationChecker) =
    inherit Core.StatementKindTransformation(parent)

    override this.OnVariableDeclaration stm =
        flatten stm.Lhs
        |> Seq.iter (function
            | VariableName name -> parent.DeclaredVariables <- parent.DeclaredVariables.Add name
            | _ -> ())

        base.OnVariableDeclaration stm

    override this.OnValueUpdate stm =
        match stm.Lhs with
        | LocalVarTuple v ->
            flatten v
            |> Seq.iter (function
                | VariableName name -> parent.MutatedVariables <- parent.MutatedVariables.Add name
                | _ -> ())
        | _ -> ()

        base.OnValueUpdate stm


/// A transformation that counts how many times each local variable is referenced.
/// Should be called at the specialization level, as it's meant to operate on a single implementation.
type internal ReferenceCounter private (_private_) =
    inherit Core.SyntaxTreeTransformation()

    member val internal UsedVariables = Map.empty with get, set

    /// Returns the number of times the variable with the given name is referenced
    member this.NumberOfUses name = this.UsedVariables.TryFind name |? 0

    internal new() as this =
        new ReferenceCounter("_private_")
        then
            this.ExpressionKinds <- new ReferenceCounterExpressionKinds(this)
            this.Types <- new Core.TypeTransformation(this, Core.TransformationOptions.Disabled)

/// private helper class for ReferenceCounter
and private ReferenceCounterExpressionKinds(parent: ReferenceCounter) =
    inherit Core.ExpressionKindTransformation(parent)

    override this.OnIdentifier(sym, tArgs) =
        match sym with
        | LocalVariable name -> parent.UsedVariables <- parent.UsedVariables.Add(name, parent.NumberOfUses name + 1)
        | _ -> ()

        base.OnIdentifier(sym, tArgs)


/// private helper class for ReplaceTypeParams
type private ReplaceTypeParamsTypes(parent: Core.SyntaxTreeTransformation<_>) =
    inherit Core.TypeTransformation<ImmutableDictionary<QsQualifiedName * string, ResolvedType>>(parent)

    override this.OnTypeParameter tp =
        let key = tp.Origin, tp.TypeName

        match this.SharedState.TryGetValue key with
        | true, t -> t.Resolution
        | _ -> TypeKind.TypeParameter tp

/// A transformation that substitutes type parameters according to the given dictionary
/// Should be called at the specialization level, as it's meant to operate on a single implementation.
/// Does *not* update the type paremeter resolution dictionaries.
type internal ReplaceTypeParams private (typeParams: ImmutableDictionary<_, ResolvedType>, _private_) =
    inherit Core.SyntaxTreeTransformation<ImmutableDictionary<QsQualifiedName * string, ResolvedType>>(typeParams)

    internal new(typeParams: ImmutableDictionary<_, ResolvedType>) as this =
        new ReplaceTypeParams(typeParams, "_private_")
        then this.Types <- new ReplaceTypeParamsTypes(this)


/// private helper class for SideEffectChecker
type private SideEffectCheckerExpressionKinds(parent: SideEffectChecker) =
    inherit Core.ExpressionKindTransformation(parent)

    override this.OnFunctionCall(method, arg) =
        parent.HasOutput <- true
        base.OnFunctionCall(method, arg)

    override this.OnOperationCall(method, arg) =
        parent.HasQuantum <- true
        parent.HasOutput <- true
        base.OnOperationCall(method, arg)

/// private helper class for SideEffectChecker
and private SideEffectCheckerStatementKinds(parent: SideEffectChecker) =
    inherit Core.StatementKindTransformation(parent)

    override this.OnValueUpdate stm =
        let mutatesState =
            match stm.Lhs with
            | LocalVarTuple x when isAllDiscarded x -> false
            | _ -> true

        parent.HasMutation <- parent.HasMutation || mutatesState
        base.OnValueUpdate stm

    override this.OnReturnStatement stm =
        parent.HasInterrupts <- true
        base.OnReturnStatement stm

    override this.OnFailStatement stm =
        parent.HasInterrupts <- true
        base.OnFailStatement stm

/// A ScopeTransformation that tracks what side effects the transformed code could cause
and internal SideEffectChecker private (_private_) =
    inherit Core.SyntaxTreeTransformation()

    /// Whether the transformed code might have any quantum side effects (such as calling operations)
    member val HasQuantum = false with get, set
    /// Whether the transformed code might change the value of any mutable variable
    member val HasMutation = false with get, set
    /// Whether the transformed code has any statements that interrupt normal control flow (such as returns)
    member val HasInterrupts = false with get, set
    /// Whether the transformed code might output any messages to the console
    member val HasOutput = false with get, set

    internal new() as this =
        new SideEffectChecker("_private_")
        then
            this.ExpressionKinds <- new SideEffectCheckerExpressionKinds(this)
            this.StatementKinds <- new SideEffectCheckerStatementKinds(this)
            this.Types <- new Core.TypeTransformation(this, Core.TransformationOptions.Disabled)


/// A ScopeTransformation that replaces one statement with zero or more statements
[<AbstractClass>]
type internal StatementCollectorTransformation(parent: Core.SyntaxTreeTransformation) =
    inherit Core.StatementTransformation(parent)

    abstract CollectStatements: QsStatementKind -> QsStatementKind seq

    override this.OnScope scope =
        let parentSymbols = scope.KnownSymbols

        let statements =
            scope.Statements
            |> Seq.map this.OnStatement
            |> Seq.map (fun x -> x.Statement)
            |> Seq.collect this.CollectStatements
            |> Seq.map wrapStmt

        QsScope.New(statements, parentSymbols)


/// A SyntaxTreeTransformation that removes all known symbols from anywhere in the AST
type internal StripAllKnownSymbols(_private_) =
    inherit Core.SyntaxTreeTransformation()

    internal new() as this =
        new StripAllKnownSymbols("_private_")
        then
            this.Statements <- new StripAllKnownSymbolsStatements(this)
            this.Expressions <- new Core.ExpressionTransformation(this, Core.TransformationOptions.Disabled)
            this.Types <- new Core.TypeTransformation(this, Core.TransformationOptions.Disabled)

/// private helper class for StripAllKnownSymbols
and private StripAllKnownSymbolsStatements(parent: StripAllKnownSymbols) =
    inherit Core.StatementTransformation(parent)

    override this.OnScope scope =
        QsScope.New(scope.Statements |> Seq.map this.OnStatement, LocalDeclarations.Empty)
