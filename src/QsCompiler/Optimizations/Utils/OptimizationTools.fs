// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module internal Microsoft.Quantum.QsCompiler.Experimental.OptimizationTools

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Experimental.Utils
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core


/// A SyntaxTreeTransformation that finds identifiers in each implementation that represent distict qubit values.
/// Should be called at the specialization level, as it's meant to operate on a single implementation.
type internal FindDistinctQubits private (unsafe) =
    inherit QsSyntaxTreeTransformation<unit>()

    member val DistinctNames : Set<NonNullable<string>> = Set.empty with get, set

    internal new () as this = 
        new FindDistinctQubits("unsafe") then
            this.Namespaces <- new DistinctQubitsNamespaces(this)
            this.StatementKinds <- new DistinctQubitsStatementKinds(this)

/// private helper class for FindDistinctQubits
and private DistinctQubitsNamespaces (parent : FindDistinctQubits) = 
    inherit NamespaceTransformation<unit>(parent)

    override this.onProvidedImplementation (argTuple, body) =
        argTuple |> toSymbolTuple |> flatten |> Seq.iter (function
            | VariableName name -> parent.DistinctNames <- parent.DistinctNames.Add name 
            | _ -> ())
        base.onProvidedImplementation (argTuple, body)

/// private helper class for FindDistinctQubits
and private DistinctQubitsStatementKinds (parent : FindDistinctQubits) = 
    inherit StatementKindTransformation<unit>(parent)

    override this.onQubitScope stm =
        stm.Binding.Lhs |> flatten |> Seq.iter (function
        | VariableName name -> parent.DistinctNames <- parent.DistinctNames.Add name 
        | _ -> ())
        base.onQubitScope stm


/// A transformation that tracks what variables the transformed code could mutate.
/// Should be called at the specialization level, as it's meant to operate on a single implementation.
type internal MutationChecker private (unsafe) =
    inherit QsSyntaxTreeTransformation<unit>()

    member val DeclaredVariables : Set<NonNullable<string>> = Set.empty with get, set
    member val MutatedVariables : Set<NonNullable<string>> = Set.empty with get, set

    /// Contains the set of variables that this code doesn't declare but does mutate.
    member this.ExternalMutations = this.MutatedVariables - this.DeclaredVariables

    internal new () as this = 
        new MutationChecker("unsafe") then
            this.StatementKinds <- new MutationCheckerStatementKinds(this)

/// private helper class for MutationChecker
and private MutationCheckerStatementKinds(parent : MutationChecker) = 
    inherit StatementKindTransformation<unit>(parent)

    override this.onVariableDeclaration stm =
        flatten stm.Lhs |> Seq.iter (function
            | VariableName name -> parent.DeclaredVariables <- parent.DeclaredVariables.Add name
            | _ -> ())
        base.onVariableDeclaration stm

    override this.onValueUpdate stm =
        match stm.Lhs with
        | LocalVarTuple v ->
            flatten v |> Seq.iter (function
                | VariableName name -> parent.MutatedVariables <- parent.MutatedVariables.Add name
                | _ -> ())
        | _ -> ()
        base.onValueUpdate stm


/// A transformation that counts how many times each local variable is referenced.
/// Should be called at the specialization level, as it's meant to operate on a single implementation.
type internal ReferenceCounter private (unsafe) =
    inherit QsSyntaxTreeTransformation<unit>()

    member val internal UsedVariables = Map.empty with get, set

    /// Returns the number of times the variable with the given name is referenced
    member this.NumberOfUses name = this.UsedVariables.TryFind name |? 0

    internal new () as this = 
        new ReferenceCounter("unsafe") then
            this.ExpressionKinds <- new ReferenceCounterExpressionKinds(this)

/// private helper class for ReferenceCounter
and private ReferenceCounterExpressionKinds(parent : ReferenceCounter) = 
    inherit ExpressionKindTransformation<unit>(parent)

    override this.onIdentifier (sym, tArgs) =
        match sym with
        | LocalVariable name -> parent.UsedVariables <- parent.UsedVariables.Add (name, parent.NumberOfUses name + 1)
        | _ -> ()
        base.onIdentifier (sym, tArgs)


/// private helper class for ReplaceTypeParams
type private ReplaceTypeParamsTypes(parent : QsSyntaxTreeTransformation<_>) = 
    inherit TypeTransformation<ImmutableDictionary<QsQualifiedName * NonNullable<string>, ResolvedType>>(parent)

    override this.onTypeParameter tp =
        let key = tp.Origin, tp.TypeName
        match this.Transformation.InternalState.TryGetValue key with 
        | true, t -> t.Resolution
        | _ -> TypeKind.TypeParameter tp

/// A transformation that substitutes type parameters according to the given dictionary
/// Should be called at the specialization level, as it's meant to operate on a single implementation.
/// Does *not* update the type paremeter resolution dictionaries. 
type internal ReplaceTypeParams private (typeParams: ImmutableDictionary<_, ResolvedType>, unsafe) =
    inherit QsSyntaxTreeTransformation<ImmutableDictionary<QsQualifiedName * NonNullable<string>, ResolvedType>>(typeParams)

    internal new (typeParams: ImmutableDictionary<_, ResolvedType>) as this = 
        new ReplaceTypeParams(typeParams, "unsafe") then
            this.Types <- new ReplaceTypeParamsTypes(this)


/// private helper class for SideEffectChecker
type private SideEffectCheckerExpressionKinds(parent : SideEffectChecker) = 
    inherit ExpressionKindTransformation<unit>(parent)

    override this.onFunctionCall (method, arg) =
        parent.AnyOutput <- true
        base.onFunctionCall (method, arg)

    override this.onOperationCall (method, arg) =
        parent.AnyQuantum <- true
        parent.AnyOutput <- true
        base.onOperationCall (method, arg)

/// private helper class for SideEffectChecker
and private SideEffectCheckerStatementKinds(parent : SideEffectChecker) = 
    inherit StatementKindTransformation<unit>(parent)

    override this.onValueUpdate stm =
        let mutatesState = match stm.Lhs with LocalVarTuple x when isAllDiscarded x -> false | _ -> true
        parent.AnyMutation <- parent.AnyMutation || mutatesState
        base.onValueUpdate stm

    override this.onReturnStatement stm =
        parent.AnyInterrupts <- true
        base.onReturnStatement stm

    override this.onFailStatement stm =
        parent.AnyInterrupts <- true
        base.onFailStatement stm

/// A ScopeTransformation that tracks what side effects the transformed code could cause
and internal SideEffectChecker private (unsafe) =
    inherit QsSyntaxTreeTransformation<unit>()

    /// Whether the transformed code might have any quantum side effects (such as calling operations)
    member val AnyQuantum = false with get, set
    /// Whether the transformed code might change the value of any mutable variable
    member val AnyMutation = false with get, set
    /// Whether the transformed code has any statements that interrupt normal control flow (such as returns)
    member val AnyInterrupts = false with get, set
    /// Whether the transformed code might output any messages to the console
    member val AnyOutput = false with get, set

    internal new () as this = 
        new SideEffectChecker("unsafe") then
            this.ExpressionKinds <- new SideEffectCheckerExpressionKinds(this)
            this.StatementKinds <- new SideEffectCheckerStatementKinds(this)


/// A ScopeTransformation that replaces one statement with zero or more statements
type [<AbstractClass>] internal StatementCollectorTransformation<'T>(parent : QsSyntaxTreeTransformation<_>) =
    inherit StatementTransformation<'T>(parent)

    abstract member CollectStatements: QsStatementKind -> QsStatementKind seq

    override this.Transform scope =
        let parentSymbols = scope.KnownSymbols
        let statements =
            scope.Statements
            |> Seq.map this.onStatement
            |> Seq.map (fun x -> x.Statement)
            |> Seq.collect this.CollectStatements
            |> Seq.map wrapStmt
        QsScope.New (statements, parentSymbols)


/// A SyntaxTreeTransformation that removes all known symbols from anywhere in the AST
type internal StripAllKnownSymbols(unsafe) =
    inherit QsSyntaxTreeTransformation<unit>()

    internal new () as this = 
        new StripAllKnownSymbols("unsafe") then
            this.Statements <- new StripAllKnownSymbolsStatements(this)

/// private helper class for StripAllKnownSymbols
and private StripAllKnownSymbolsStatements(parent : StripAllKnownSymbols) = 
    inherit StatementTransformation<unit>(parent)

    override this.Transform scope =
        QsScope.New (scope.Statements |> Seq.map this.onStatement, LocalDeclarations.Empty)

