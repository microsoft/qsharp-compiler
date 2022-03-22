// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module internal Microsoft.Quantum.QsCompiler.Experimental.OptimizationTools

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.Experimental.Utils
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core


/// A SyntaxTreeTransformation that finds identifiers in each implementation that represent distict qubit values.
/// Should be called at the specialization level, as it's meant to operate on a single implementation.
type internal FindDistinctQubits () =
    inherit MonoTransformation()

    member val DistinctNames: Set<string> = Set.empty with get, set

    override this.OnProvidedImplementation(argTuple, body) =
        argTuple
        |> toSymbolTuple
        |> flatten
        |> Seq.iter (function
            | VariableName name -> this.DistinctNames <- this.DistinctNames.Add name
            | _ -> ())

        ``base``.OnProvidedImplementation(argTuple, body)

    override this.OnQubitScope stm =
        stm.Binding.Lhs
        |> flatten
        |> Seq.iter (function
            | VariableName name -> this.DistinctNames <- this.DistinctNames.Add name
            | _ -> ())

        base.OnQubitScope stm

/// A transformation that tracks what variables the transformed code could mutate.
/// Should be called at the specialization level, as it's meant to operate on a single implementation.
type internal MutationChecker () =
    inherit MonoTransformation()

    member val DeclaredVariables: Set<string> = Set.empty with get, set
    member val MutatedVariables: Set<string> = Set.empty with get, set

    /// Contains the set of variables that this code doesn't declare but does mutate.
    member this.ExternalMutations = this.MutatedVariables - this.DeclaredVariables

    override this.OnVariableDeclaration stm =
        flatten stm.Lhs
        |> Seq.iter (function
            | VariableName name -> this.DeclaredVariables <- this.DeclaredVariables.Add name
            | _ -> ())

        base.OnVariableDeclaration stm

    override this.OnValueUpdate stm =
        match stm.Lhs with
        | LocalVarTuple v ->
            flatten v
            |> Seq.iter (function
                | VariableName name -> this.MutatedVariables <- this.MutatedVariables.Add name
                | _ -> ())
        | _ -> ()

        base.OnValueUpdate stm

/// A transformation that counts how many times each local variable is referenced.
/// Should be called at the specialization level, as it's meant to operate on a single implementation.
type internal ReferenceCounter () =
    inherit MonoTransformation()

    member val internal UsedVariables = Map.empty with get, set

    /// Returns the number of times the variable with the given name is referenced
    member this.NumberOfUses name = this.UsedVariables.TryFind name |? 0

    override this.OnIdentifier(sym, tArgs) =
        match sym with
        | LocalVariable name -> this.UsedVariables <- this.UsedVariables.Add(name, this.NumberOfUses name + 1)
        | _ -> ()

        ``base``.OnIdentifier(sym, tArgs)

/// A transformation that substitutes type parameters according to the given dictionary
/// Should be called at the specialization level, as it's meant to operate on a single implementation.
/// Does *not* update the type parameter resolution dictionaries.
type internal ReplaceTypeParams (typeParams: ImmutableDictionary<_, ResolvedType>) =
    inherit MonoTransformation()

    override __.OnTypeParameter tp =
        let key = tp.Origin, tp.TypeName

        match typeParams.TryGetValue key with
        | true, t -> t.Resolution
        | _ -> TypeKind.TypeParameter tp

/// A ScopeTransformation that tracks what side effects the transformed code could cause
type internal SideEffectChecker () =
    inherit MonoTransformation()

    /// Whether the transformed code might have any quantum side effects (such as calling operations)
    member val HasQuantum = false with get, set
    /// Whether the transformed code might change the value of any mutable variable
    member val HasMutation = false with get, set
    /// Whether the transformed code has any statements that interrupt normal control flow (such as returns)
    member val HasInterrupts = false with get, set
    /// Whether the transformed code might output any messages to the console
    member val HasOutput = false with get, set

    override this.OnFunctionCall(method, arg) =
        this.HasOutput <- true
        ``base``.OnFunctionCall(method, arg)

    override this.OnOperationCall(method, arg) =
        this.HasQuantum <- true
        this.HasOutput <- true
        ``base``.OnOperationCall(method, arg)

    override this.OnValueUpdate stm =
        let mutatesState =
            match stm.Lhs with
            | LocalVarTuple x when isAllDiscarded x -> false
            | _ -> true

        this.HasMutation <- this.HasMutation || mutatesState
        base.OnValueUpdate stm

    override this.OnReturnStatement stm =
        this.HasInterrupts <- true
        base.OnReturnStatement stm

    override this.OnFailStatement stm =
        this.HasInterrupts <- true
        base.OnFailStatement stm

/// A SyntaxTreeTransformation that removes all known symbols from anywhere in the AST
type internal StripAllKnownSymbols () =
    inherit MonoTransformation()

    override this.OnScope scope =
        QsScope.New(scope.Statements |> Seq.map this.OnStatement, LocalDeclarations.Empty)
