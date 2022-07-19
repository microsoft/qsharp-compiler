// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxTree

#nowarn "44" // AccessModifier and Modifiers are deprecated.

open System
open System.Collections.Immutable
open System.Linq
open System.Runtime.InteropServices
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens


// IMPORTANT: if the data structures in this file are changed to classes,
// there are a bunch of places in the code that will break because they rely on structural equality!


type QsBindingKind =
    /// indicates a binding to one or more immutable variables - i.e. the value of the variable cannot be modified or re-bound
    | ImmutableBinding
    /// indicates a binding to one or more mutable variables - i.e. the value of the variables can be modified later on
    /// -> note that mutable bindings in Q# do *not* behave in exactly the way a reference binding would
    | MutableBinding


type QsSpecializationKind =
    /// indicates the specialization of a declared callable that is executed when the callable is called without any applied functors
    | QsBody
    /// indicates the specialization of a declared operation that is executed when the callable is called after applying an odd number of Adjoint functors
    | QsAdjoint
    /// indicates the specialization of a declared operation that is executed when the callable is called after applying one or more Controlled functors
    | QsControlled
    /// indicates the specialization of a declared operation that is executed when the callable is called after applying an odd number of Adjoint functors and one or more Controlled functors
    | QsControlledAdjoint


type QsCallableKind =
    /// Indicates a Q# callable whose effect(s) may be non-deterministic -
    /// in particular, any modifications of the quantum state or any probabilistic side effects are only possible within operations.
    | Operation
    /// Indicates a Q# callable whose effect(s) are fully deterministic -
    /// in particular, the callable is guaranteed to have the same effects whenever called with the same arguments.
    /// -> Any modifications of the quantum state or any probabilistic side effects are only possible within operations.
    | Function
    /// Indicates the type constructor for a user defined Q# type.
    | TypeConstructor


/// Note: Q# binding scopes are specific to qubit management and hence only valid within Q# operations.
type QsQubitScopeKind =
    /// Indicates a Q# allocation scope -
    /// i.e. a using directive indicating which (clean) qubits are to be allocated,
    /// followed by a statement block during which the initialized qubits are available,
    /// before being released again (in a clean state) at the end of the scope.
    | Allocate
    /// Indicates a Q# scope during which external qubits are to be borrowed -
    /// i.e. a borrow directive indicating which (dirty) qubits are to be borrowed,
    /// followed by a statement block during which the borrowed qubits are available.
    /// At the end of the scope the bound variables go out of scope and the qubits are "returned".
    /// The returned qubits are expected to be in the same state they were in when borrowed.
    | Borrow


/// used to represent a qualified name within Q# - i.e. a namespace name followed by a symbol name
type QsQualifiedName =
    {
        /// the name of the namespace in which a namespace element is declared
        Namespace: string
        /// the declared name of the namespace element
        Name: string
    }
    override this.ToString() =
        sprintf "%s.%s" this.Namespace this.Name


type SymbolTuple =
    /// indicates in invalid variable name
    | InvalidItem
    /// indicates a valid Q# variable name
    | VariableName of string
    /// indicates a tuple of Q# variable names or (nested) tuples of variable names
    | VariableNameTuple of ImmutableArray<SymbolTuple>
    /// indicates a place holder for a Q# variable that won't be used after the symbol tuple is bound to a value
    | DiscardedItem
    interface ITuple


/// use to represent all forms of Q# bindings
type QsBinding<'T> =
    {
        /// the kind of the binding (binding to mutable or immutable variables)
        Kind: QsBindingKind
        /// the variables to which the right hand side (Rhs) of the binding is bound given as symbol tuple
        Lhs: SymbolTuple
        /// the value which is bound to the variables on the left hand side (Lhs) of the binding
        Rhs: 'T
    }


type Identifier =
    /// an identifier referring to a locally declared variable visible on the current scope
    | LocalVariable of string
    /// in identifier referring to a globally declared callable -> note that type names are *not* represented as identifiers
    | GlobalCallable of QsQualifiedName
    /// an identifier of unknown origin - i.e. the identifier could not be associated with any globally declared callable or local variable
    | InvalidIdentifier


/// used to represent position information for declared variables
/// relative to the position of a chosen root-node (e.g. the specialization declaration)
type QsLocation =
    {
        /// position offset (line and character) for Range relative to the chosen root node
        Offset: Position
        /// range relative to Offset
        Range: Range
    }


/// used to represent the use of a type parameter within a fully resolved Q# type
[<CustomEquality>]
[<CustomComparison>]
type QsTypeParameter =
    // TODO: origin needs adapting if we ever allow to declare type parameters on specializations
    {
        /// The qualified name of the callable the type parameter belongs to.
        Origin: QsQualifiedName

        /// The name of the type parameter.
        TypeName: string

        /// The source code range where the type parameter occurs. Null for auto-generated or inferred types. Range is
        /// ignored when comparing type parameters.
        // TODO: RELEASE 2021-10: Remove obsolete property.
        [<Obsolete "Use ResolvedType.Range instead.">]
        Range: QsNullable<Range>
    }

    interface IComparable with
        member param1.CompareTo param2 =
            match param2 with
            | :? QsTypeParameter as param2 -> compare (param1.Origin, param1.TypeName) (param2.Origin, param2.TypeName)
            | _ -> ArgumentException "Types are different." |> raise

    override param1.Equals param2 =
        match param2 with
        | :? QsTypeParameter as param2 -> param1.Origin = param2.Origin && param1.TypeName = param2.TypeName
        | _ -> false

    override param.GetHashCode() = hash (param.Origin, param.TypeName)

    /// <summary>
    /// Returns a copy of this <see cref="QsTypeParameter"/> with updated fields.
    /// </summary>
    member param.With
        (
            [<Optional; DefaultParameterValue null>] ?origin,
            [<Optional; DefaultParameterValue null>] ?typeName,
            [<Optional; DefaultParameterValue null>] ?range
        ) =
        { param with
            Origin = defaultArg origin param.Origin
            TypeName = defaultArg typeName param.TypeName
            Range = defaultArg range param.Range
        }

    /// <summary>
    /// Creates a new <see cref="QsTypeParameter"/>.
    /// </summary>
    static member New(origin, name) =
        {
            Origin = origin
            TypeName = name
            Range = Null
        }

/// used to represent the use of a user defined type within a fully resolved Q# type
[<CustomEquality>]
[<CustomComparison>]
type UserDefinedType =
    {
        /// The name of the namespace in which the type is declared.
        Namespace: string

        /// The name of the declared type.
        Name: string

        /// The source code range where the user defined type name occurs. Null for auto-generated or inferred types.
        /// Range is ignored when comparing user defined types.
        // TODO: RELEASE 2021-10: Remove obsolete property.
        [<Obsolete "Use ResolvedType.Range instead.">]
        Range: QsNullable<Range>
    }

    interface IComparable with
        member udt1.CompareTo udt2 =
            match udt2 with
            | :? UserDefinedType as udt2 -> compare (udt1.Namespace, udt1.Name) (udt2.Namespace, udt2.Name)
            | _ -> ArgumentException "Types are different." |> raise

    override udt1.Equals udt2 =
        match udt2 with
        | :? UserDefinedType as udt2 -> udt1.Namespace = udt2.Namespace && udt1.Name = udt2.Name
        | _ -> false

    override udt.GetHashCode() = hash (udt.Namespace, udt.Name)

    member this.GetFullName() =
        { Namespace = this.Namespace; Name = this.Name }

    /// <summary>
    /// Returns a copy of this <see cref="UserDefinedType"/> with updated fields.
    /// </summary>
    member udt.With
        (
            [<Optional; DefaultParameterValue null>] ?ns,
            [<Optional; DefaultParameterValue null>] ?name,
            [<Optional; DefaultParameterValue null>] ?range
        ) =
        { udt with
            Namespace = defaultArg ns udt.Namespace
            Name = defaultArg name udt.Name
            Range = defaultArg range udt.Range
        }

    /// <summary>
    /// Creates a new <see cref="UserDefinedType"/>.
    /// </summary>
    static member New(ns, name) =
        {
            Namespace = ns
            Name = name
            Range = Null
        }

/// Fully resolved operation characteristics used to describe the properties of a Q# callable.
/// A resolved characteristic expression by construction never contains an empty or invalid set as inner expressions,
/// and necessarily contains the property "Adjointable" if it contains the property "SelfAdjoint".
type ResolvedCharacteristics =
    private
        {
            // the private constructor enforces the guarantees given for any instance of ResolvedCharacteristics
            // -> the static member New replaces the record constructor
            _Characteristics: CharacteristicsKind<ResolvedCharacteristics>
        }

    static member Empty = EmptySet |> ResolvedCharacteristics.New

    member this.AreInvalid =
        match this._Characteristics with
        | InvalidSetExpr -> true
        | _ -> false

    /// Contains the fully resolved characteristics used to describe the properties of a Q# callable.
    /// By construction never contains an empty or invalid set as inner expressions,
    /// and necessarily contains the property "Adjointable" if it contains the property "SelfAdjoint".
    member this.Expression = this._Characteristics

    /// Extracts all properties of a callable with the given characteristics using the given function to access the characteristics kind.
    /// Returns the extracted properties as Some if the extraction succeeds.
    /// Returns None if the properties cannot be determined either because the characteristics expression contains unresolved parameters or the expression is invalid.
    static member internal ExtractProperties(getKind: _ -> CharacteristicsKind<_>) =
        let rec yieldProperties ex =
            match getKind ex with
            | EmptySet -> Seq.empty |> Some
            | SimpleSet property -> seq { yield property } |> Some
            | Union (s1, s2) ->
                Option.map2 (fun (x: _ seq) y -> x.Concat y) (s1 |> yieldProperties) (s2 |> yieldProperties)
            | Intersection (s1, s2) ->
                Option.map2 (fun (x: _ seq) y -> x.Intersect y) (s1 |> yieldProperties) (s2 |> yieldProperties)
            | InvalidSetExpr -> None

        yieldProperties

    /// Returns a new characteristics expression that is the union of all properties, if the properties of the given characteristics can be determined.
    /// Returns the given characteristics expression unchanged otherwise.
    static member private Simplify(ex: ResolvedCharacteristics) =
        let uniqueProps =
            ex
            |> ResolvedCharacteristics.ExtractProperties(fun a -> a._Characteristics)
            |> Option.map (Seq.distinct >> Seq.toList)
        // it is fine (and necessary) to directly reassemble the unions,
        // since all property dependencies will already be satisfied (having been extracted from a ResolvedCharacteristics)
        let rec addProperties (current: ResolvedCharacteristics) =
            function
            | head :: tail ->
                tail |> addProperties { _Characteristics = Union(current, { _Characteristics = SimpleSet head }) }
            | _ -> current

        match uniqueProps with
        | Some (head :: tail) -> tail |> addProperties { _Characteristics = SimpleSet head }
        | _ -> ex

    /// Builds a ResolvedCharacteristics based on a compatible characteristics kind, and replaces the (inaccessible) record constructor.
    /// Returns an invalid expression if the properties for the built characteristics cannot be determined even after all parameters are known.
    /// Incorporates all empty sets such that they do not ever occur as part of an encompassing expression.
    static member New kind =
        let isEmpty (ex: ResolvedCharacteristics) =
            match ex._Characteristics with
            | EmptySet -> true
            | _ -> false

        match kind with
        | EmptySet -> { _Characteristics = EmptySet }
        | SimpleSet property -> { _Characteristics = SimpleSet property }
        | Union (s1, s2) when s1 |> isEmpty -> s2
        | Union (s1, s2) when s2 |> isEmpty -> s1
        | Union (s1, s2) when s1.AreInvalid || s2.AreInvalid -> { _Characteristics = InvalidSetExpr }
        | Union (s1, s2) -> { _Characteristics = Union(s1, s2) } |> ResolvedCharacteristics.Simplify
        | Intersection (s1, s2) when s1 |> isEmpty || s2 |> isEmpty -> { _Characteristics = EmptySet }
        | Intersection (s1, s2) when s1.AreInvalid || s2.AreInvalid -> { _Characteristics = InvalidSetExpr }
        | Intersection (s1, s2) -> { _Characteristics = Intersection(s1, s2) } |> ResolvedCharacteristics.Simplify
        | InvalidSetExpr -> { _Characteristics = InvalidSetExpr }

    /// <summary>
    /// Given the resolved characteristics of a set of specializations,
    /// determines and returns the minimal characteristics of any one of the specializations.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="characteristics"/> is empty.</exception>
    static member internal Common(characteristics: ResolvedCharacteristics list) =
        let rec common current =
            function
            | [] -> current
            | head :: tail ->
                // todo: if we support parameterizing over characteristics, we need to replace them in head by their worst-case value
                tail |> common { _Characteristics = Intersection(current, head) }

        if characteristics |> List.exists (fun a -> a._Characteristics = EmptySet) then
            { _Characteristics = EmptySet }
        elif characteristics |> List.exists (fun a -> a.AreInvalid) then
            { _Characteristics = InvalidSetExpr }
        else
            match characteristics with
            | [] -> ArgumentException "cannot determine common information for an empty sequence" |> raise
            | head :: tail -> common head tail |> ResolvedCharacteristics.Simplify

    /// Builds a ResolvedCharacteristics that represents the given transformation properties.
    static member FromProperties props =
        let addProperty prop a =
            { _Characteristics = Union(a, SimpleSet prop |> ResolvedCharacteristics.New) }

        let rec addProperties (current: ResolvedCharacteristics) =
            function
            | head :: tail -> addProperties (current |> addProperty head) tail
            | _ -> current

        match props |> Seq.distinct |> Seq.toList with
        | [] -> EmptySet |> ResolvedCharacteristics.New
        | head :: tail -> tail |> addProperties (SimpleSet head |> ResolvedCharacteristics.New)

    /// <summary>
    /// Determines which properties are supported by a callable with the given characteristics and returns them.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The properties cannot be determined either because the characteristics expression contains unresolved parameters or is invalid.
    /// </exception>
    member this.GetProperties() =
        match ResolvedCharacteristics.ExtractProperties (fun ex -> ex._Characteristics) this with
        | Some props -> props.ToImmutableHashSet()
        | None -> InvalidOperationException "properties cannot be determined" |> raise

    /// Determines which functors are supported by a callable with the given characteristics and returns them as a Value.
    /// Returns Null if the supported functors cannot be determined either because the characteristics expression contains unresolved parameters or is invalid.
    member this.SupportedFunctors =
        let getFunctor =
            function
            | Adjointable -> Some Adjoint
            | Controllable -> Some Controlled

        match ResolvedCharacteristics.ExtractProperties (fun ex -> ex._Characteristics) this with
        | Some props -> (props |> Seq.choose getFunctor).ToImmutableHashSet() |> Value
        | None -> Null


/// used to represent information on Q# operations and expressions thereof generated and/or tracked during compilation
type InferredCallableInformation =
    {
        /// indicates whether the callable is a self-adjoint operation
        IsSelfAdjoint: bool
        /// indicates whether the callable is intrinsic, i.e. implemented by the target machine
        IsIntrinsic: bool
    }
    static member NoInformation = { IsSelfAdjoint = false; IsIntrinsic = false }

    /// Determines the information that was inferred for all given items.
    static member Common(infos: InferredCallableInformation seq) =
        let allAreIntrinsic = infos |> Seq.map (fun info -> info.IsIntrinsic) |> Seq.contains false |> not
        let allAreSelfAdjoint = infos |> Seq.map (fun info -> info.IsSelfAdjoint) |> Seq.contains false |> not
        { IsIntrinsic = allAreIntrinsic; IsSelfAdjoint = allAreSelfAdjoint }


/// Contains information associated with a fully resolved operation type.
/// That information includes on one hand information embedded into the type system that is provided by the user,
/// and on the other hand information that is inferred by the compiler and is not exposed to or available to the user.
type CallableInformation =
    {
        /// describes operation properties explicitly specified in the code via set expressions
        Characteristics: ResolvedCharacteristics
        /// contains inferred information on Q# operation types generated during compilation
        InferredInformation: InferredCallableInformation
    }
    static member NoInformation =
        {
            Characteristics = ResolvedCharacteristics.Empty
            InferredInformation = InferredCallableInformation.NoInformation
        }

    static member Invalid =
        {
            Characteristics = InvalidSetExpr |> ResolvedCharacteristics.New
            InferredInformation = InferredCallableInformation.NoInformation
        }

    /// <summary>
    /// Given a sequence of CallableInformation items,
    /// determines the common characteristics as well as the information that was inferred for all given items.
    /// Any positive property (either from characteristics, or from inferred information) in the returned CallableInformation holds true for any one of the given items.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="infos"/> is empty.</exception>
    static member Common(infos: CallableInformation seq) =
        let commonCharacteristics =
            infos |> Seq.map (fun info -> info.Characteristics) |> Seq.toList |> ResolvedCharacteristics.Common

        let inferredForAll =
            infos |> Seq.map (fun info -> info.InferredInformation) |> InferredCallableInformation.Common

        { Characteristics = commonCharacteristics; InferredInformation = inferredForAll }

/// The source code range for a type.
type TypeRange =
    /// The type is annotated explicitly at the given range.
    | Annotated of Range

    /// The type is inferred from an expression or other piece of source code at the given range.
    | Inferred of Range

    /// The type is automatically generated and has no range.
    | Generated

module TypeRange =
    /// <summary>
    /// Creates an <see cref="Annotated"/> type range if a range is given, or the <see cref="Generated"/> type range
    /// otherwise.
    /// </summary>
    let internal annotated = QsNullable<_>.Map Annotated >> QsNullable.defaultValue Generated

    /// <summary>
    /// Creates an <see cref="Inferred"/> type range if a range is given, or the <see cref="Generated"/> type range
    /// otherwise.
    /// </summary>
    [<CompiledName "Inferred">]
    let inferred = QsNullable<_>.Map Inferred >> QsNullable.defaultValue Generated

    /// Returns the range if it exists.
    [<CompiledName "TryRange">]
    let tryRange =
        function
        | Annotated range
        | Inferred range -> Value range
        | Generated -> Null

    /// <summary>
    /// Returns the original type range if it is not generated, otherwise returns <paramref name="ifGenerated"/>.
    /// </summary>
    [<CompiledName "OrElse">]
    let orElse ifGenerated =
        function
        | Annotated _
        | Inferred _ as range -> range
        | Generated -> ifGenerated

/// Fully resolved Q# type.
/// A Q# resolved type by construction never contains any arity-0 or arity-1 tuple types.
/// User defined types are represented as UserDefinedTypes.
/// Type parameters are represented as QsTypeParameters containing their origin and name.
[<CustomEquality>]
[<CustomComparison>]
type ResolvedType =
    private
        {
            kind: QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>
            range: TypeRange
        }

    interface ITuple

    interface IComparable with
        member this.CompareTo that =
            match that with
            | :? ResolvedType as that -> compare this.kind that.kind
            | _ -> ArgumentException "Types are different." |> raise

    override this.Equals that =
        match that with
        | :? ResolvedType as that -> this.kind = that.kind
        | _ -> false

    override this.GetHashCode() = hash this.kind

    /// Contains the fully resolved Q# type,
    /// where type parameters are represented as QsTypeParameters containing their origin (the namespace and the callable they belong to) and their name,
    /// and user defined types are resolved to their fully qualified name.
    /// By construction never contains any arity-0 or arity-1 tuple types.
    member this.Resolution = this.kind

    /// The source code range where this type originated. Range is ignored when comparing resolved types.
    member this.Range = this.range

module ResolvedType =
    /// <summary>
    /// Replaces singleton tuple types with its contained type.
    /// </summary>
    /// <exception cref="ArgumentException">The type kind is an empty tuple.</exception>
    let private normalizeTuple =
        function
        | TupleType items when items.IsEmpty -> ArgumentException "Tuple is empty." |> raise
        | TupleType items when items.Length = 1 -> (items.[0]: ResolvedType).Resolution
        | kind -> kind

    /// <summary>
    /// Creates a resolved type with no source code range.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="kind"/> is an empty tuple.</exception>
    [<CompiledName "Create">]
    let create range kind =
        { kind = normalizeTuple kind; range = range }

    /// <summary>
    /// Replaces the type kind of <paramref name="resolvedType"/> with <paramref name="kind"/>.
    /// </summary>
    [<CompiledName "WithKind">]
    let withKind kind resolvedType =
        { resolvedType with ResolvedType.kind = normalizeTuple kind }

    /// <summary>
    /// Replaces the range of <paramref name="resolvedType"/> with <paramref name="range"/>.
    /// </summary>
    [<CompiledName "WithRange">]
    let withRange range resolvedType = { resolvedType with range = range }

    /// <summary>
    /// Recursively replaces every range in <paramref name="resolvedType"/> with <paramref name="range"/>.
    /// </summary>
    [<CompiledName "WithAllRanges">]
    let rec withAllRanges range (resolvedType: ResolvedType) =
        match resolvedType.Resolution with
        | ArrayType item -> withAllRanges range item |> ArrayType
        | TupleType items -> items |> Seq.map (withAllRanges range) |> ImmutableArray.CreateRange |> TupleType
        | QsTypeKind.Operation ((input, output), info) ->
            QsTypeKind.Operation((withAllRanges range input, withAllRanges range output), info)
        | QsTypeKind.Function (input, output) ->
            QsTypeKind.Function(withAllRanges range input, withAllRanges range output)
        | kind -> kind
        |> create range

type ResolvedType with
    /// <summary>
    /// Creates a resolved type with no source code range.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="kind"/> is an empty tuple.</exception>
    static member New kind = ResolvedType.create Generated kind

    /// Given a map that for a type parameter returns the corresponding resolved type it is supposed to be replaced with,
    /// replaces the type parameters in the given type with their resolutions.
    static member ResolveTypeParameters (resolutions: ImmutableDictionary<_, _>) (t: ResolvedType) =
        let inner = ResolvedType.ResolveTypeParameters resolutions

        match t.Resolution with
        | _ when resolutions.IsEmpty -> t
        | QsTypeKind.TypeParameter tp ->
            match resolutions.TryGetValue((tp.Origin, tp.TypeName)) with
            | true, res -> res
            | false, _ -> t
        | QsTypeKind.TupleType ts ->
            t |> ResolvedType.withKind (ts |> Seq.map inner |> ImmutableArray.CreateRange |> TupleType)
        | QsTypeKind.ArrayType b -> t |> ResolvedType.withKind (inner b |> ArrayType)
        | QsTypeKind.Function (it, ot) -> t |> ResolvedType.withKind (QsTypeKind.Function(inner it, inner ot))
        | QsTypeKind.Operation ((it, ot), fList) ->
            t |> ResolvedType.withKind (QsTypeKind.Operation((inner it, inner ot), fList))
        | _ -> t


/// Fully resolved Q# expression
/// containing the content (kind) of the expression as well as its (fully resolved) type before and after application of type arguments.
type TypedExpression =
    {
        /// the content (kind) of the expression
        Expression: QsExpressionKind<TypedExpression, Identifier, ResolvedType>
        /// contains all type arguments implicitly or explicitly determined by the expression,
        /// i.e. the origin, name and concrete type of all type parameters whose type can either be inferred based on the expression,
        /// or who have explicitly been resolved by provided type arguments
        TypeArguments: ImmutableArray<QsQualifiedName * string * ResolvedType>
        /// the type of the expression after applying the type arguments
        ResolvedType: ResolvedType
        /// contains information generated and/or tracked by the compiler
        InferredInformation: InferredExpressionInformation
        /// the range at which the expression occurs relative to the statement (or partial statement) root
        /// -> is Null for invalid and auto-generated expressions
        Range: QsNullable<Range>
    }
    interface ITuple

    /// Contains a dictionary mapping the origin and name of all type parameters whose type can either be inferred based on the expression,
    /// or who have explicitly been resolved by provided type arguments to their concrete type within this expression
    member this.TypeParameterResolutions =
        this.TypeArguments.ToImmutableDictionary((fun (origin, name, _) -> origin, name), (fun (_, _, t) -> t))

    /// Given a dictionary containing the type resolutions for an expression,
    /// returns the corresponding ImmutableArray to initialize the TypeArguments with.
    static member AsTypeArguments(typeParamResolutions: ImmutableDictionary<_, _>) =
        typeParamResolutions
        |> Seq.map (fun kv -> fst kv.Key, snd kv.Key, kv.Value)
        |> ImmutableArray.CreateRange

    /// Returns true if the expression is a call-like expression, and the arguments contain a missing expression.
    /// Returns false otherwise.
    static member public IsPartialApplication kind =
        let rec containsMissing ex =
            match ex.Expression with
            | MissingExpr -> true
            | ValueTuple items -> items |> Seq.exists containsMissing
            | _ -> false

        match kind with
        | CallLikeExpression (_, args) -> args |> containsMissing
        | _ -> false


/// Fully resolved Q# initializer expression.
/// Initializer expressions are used (only) within Q# binding scopes, and trigger the allocation or borrowing of qubits on the target machine.
type ResolvedInitializer =
    private
        {
            // the private constructor enforces that the guarantees given for any instance of ResolvedInitializer
            // -> the static member New replaces the record constructor
            kind: QsInitializerKind<ResolvedInitializer, TypedExpression>
            resolvedType: ResolvedType
        }

    interface ITuple

    /// Contains the fully resolved Q# initializer.
    /// By construction never contains any arity-0 or arity-1 tuple types.
    member this.Resolution = this.kind

    /// the fully resolved Q# type of the initializer.
    member this.Type = this.resolvedType

module ResolvedInitializer =
    let create range kind =
        let arrayType = Qubit |> ResolvedType.create range |> ArrayType |> ResolvedType.create range

        let tupleType =
            Seq.map (fun init -> init.resolvedType)
            >> ImmutableArray.CreateRange
            >> TupleType
            >> ResolvedType.create range

        match kind with
        | QsInitializerKind.QubitTupleAllocation items when items.IsEmpty ->
            ArgumentException "Tuple initializer is empty." |> raise
        | QsInitializerKind.QubitTupleAllocation items when items.Length = 1 -> items.[0]
        | QsInitializerKind.QubitTupleAllocation items -> { kind = kind; resolvedType = tupleType items }
        | QsInitializerKind.QubitRegisterAllocation _ -> { kind = kind; resolvedType = arrayType }
        | QsInitializerKind.SingleQubitAllocation -> { kind = kind; resolvedType = ResolvedType.create range Qubit }
        | QsInitializerKind.InvalidInitializer -> { kind = kind; resolvedType = ResolvedType.create range InvalidType }

type ResolvedInitializer with
    /// <summary>
    /// Builds a ResolvedInitializer based on a compatible Q# initializer kind, and replaces the (inaccessible) record constructor.
    /// Replaces an arity-1 tuple by its item type.
    /// </summary>
    /// <exception cref="ArgumentException">The given type kind is an empty tuple.</exception>
    static member New kind =
        ResolvedInitializer.create Generated kind

type LocalVariableDeclaration<'Name> = LocalVariableDeclaration<'Name, ResolvedType>


/// used to attach information about which symbols are declared to each scope and statement
type LocalDeclarations =
    {
        // keeping things here as arrays for resource reasons
        /// contains all declared variables
        Variables: ImmutableArray<LocalVariableDeclaration<string>>
    }
    member this.IsEmpty = this.Variables.Length = 0
    static member Empty = { Variables = ImmutableArray.Empty }


type QsValueUpdate =
    {
        /// the Q# object whose value is to be set to the right hand side (Rhs) of the update
        Lhs: TypedExpression
        /// the Q# expression which is to be assigned to the left hand side (Lhs) of the update
        Rhs: TypedExpression
    }


type QsComments =
    {
        /// comments that occur before the statement in the code
        OpeningComments: ImmutableArray<string>
        /// comments that occur on (before or on the same line after) the ending of a block statement
        ClosingComments: ImmutableArray<string>
    }
    static member Empty = { OpeningComments = ImmutableArray.Empty; ClosingComments = ImmutableArray.Empty }


type QsScope =
    {
        /// the Q# statements contained in the scope
        Statements: ImmutableArray<QsStatement>
        /// contains all symbols visible at the beginning of this scope - i.e. all symbols declared up to this point in all parent scopes
        KnownSymbols: LocalDeclarations
    }


/// Used instead of QsScope to represent a statement block within a statement that (potentially) contains more than one such block.
/// This is the case e.g. for in if-statement which may contain several conditional blocks, or a repeat-until-success-statement, or a conjugation.
and QsPositionedBlock =
    {
        /// the Q# statement block to execute (only) if the associated condition evaluates to true
        /// -> note that the block is treated as a separate scope, i.e. variables declared within the block won't be visible after the end of the block
        Body: QsScope
        /// Handle for saving the location information for the conditional block.
        /// The position offset denotes the position relative to the beginning of the specialization declaration,
        /// and the range denotes the range of the block header.
        /// The location is Null for auto-generated statements.
        Location: QsNullable<QsLocation>
        /// contains comments in the code associated with the condition
        Comments: QsComments
    }


/// used to represent both a potential match statement as well as an if-statement
and QsConditionalStatement =
    {
        /// Contains a sequence of conditional blocks (a conditional and the corresponding block).
        /// A block is only executed if its condition is Null or the condition content evaluates to true,
        /// and if no preceding block has already been executed.
        ConditionalBlocks: ImmutableArray<TypedExpression * QsPositionedBlock>
        /// Statement block to be executed if none of the conditions for the conditional blocks evaluates to true.
        /// Note that comments associated with the default case are attached to the conditional statement.
        Default: QsNullable<QsPositionedBlock>
    }


and QsForStatement =
    {
        /// represents the symbol tuple into which the loop item is deconstructed in each iteration, and its fully resolved type
        /// -> this binding is always immutable
        LoopItem: SymbolTuple * ResolvedType
        /// the iterable expression over which the loop iterates
        IterationValues: TypedExpression
        /// the statement block that is executed for each iteration item
        Body: QsScope
    }


and QsWhileStatement =
    {
        /// If the condition evaluates to true, the body of the while-statement is executed.
        /// After execution, the condition is re-evaluated and the whole process is repeated as long as the condition evaluates to true.
        Condition: TypedExpression
        /// The statement block that is executed for each iteration of the while-loop,
        /// until the condition no longer evaluates to true.
        Body: QsScope
    }


/// Note: a Q# repeat statement is a quantum specific control flow statement and hence only valid within Q# operations.
/// The statement corresponds to a while(true)-loop with a break-condition in the middle.
and QsRepeatStatement =
    {
        /// the statement block that is repeatedly executed before evaluating the success condition, until the condition evaluate to true
        /// -> note that any declared variables are visible within both the success-condition and the fixup-block
        RepeatBlock: QsPositionedBlock
        /// The repeat statement terminates if the success condition after executing the repeat-block is evaluates to true.
        /// If it evaluates to false, the fixup-block is executed, before a renewed attempt at first executing the repeat-block prior to re-evaluating the condition.
        /// -> Note that any variables declared in the repeat-block are visible within the success-condition.
        SuccessCondition: TypedExpression
        /// statement block executed if the success-condition evaluates to false, before repeating the execution of the statement
        /// -> Note that any variables declared in the repeat-block are visible within the this block.
        FixupBlock: QsPositionedBlock
    }


/// used to represent a pattern of the form U*VU where the order of application is right to left and U* is the adjoint of U
and QsConjugation =
    {
        /// represents the outer transformation U in a pattern of the form U*VU where the order of application is right to left and U* is the adjoint of U
        OuterTransformation: QsPositionedBlock
        /// represents the inner transformation V in a pattern of the form U*VU where the order of application is right to left and U* is the adjoint of U
        InnerTransformation: QsPositionedBlock
    }


/// Statement block for the duration of which qubits are either allocated or borrowed on the target machine.
/// Note: Q# binding scopes are specific to qubit management and hence only valid within Q# operations.
and QsQubitScope =
    {
        /// indicates whether the qubits are allocated as clean qubits or borrowed ("dirty qubits")
        Kind: QsQubitScopeKind
        /// indicates what qubits to allocate/borrow and the names of the variables via which they can be accessed
        Binding: QsBinding<ResolvedInitializer>
        /// statement block during which the allocated/borrowed qubits are available
        /// -> allocated (clean) qubits are allocated in a zero state and are expected to be in the same state at the end of the block
        /// -> borrowed (dirty) qubits can be in an arbitrary state and are expected to be in the same state at the end of the block
        Body: QsScope
    }


and QsStatementKind =
    | QsExpressionStatement of TypedExpression
    | QsReturnStatement of TypedExpression
    | QsFailStatement of TypedExpression
    /// Includes both mutable and immutable bindings.
    | QsVariableDeclaration of QsBinding<TypedExpression>
    | QsValueUpdate of QsValueUpdate
    | QsConditionalStatement of QsConditionalStatement
    | QsForStatement of QsForStatement
    | QsWhileStatement of QsWhileStatement
    | QsRepeatStatement of QsRepeatStatement
    | QsConjugation of QsConjugation
    /// Includes both using and borrowing scopes.
    | QsQubitScope of QsQubitScope
    | EmptyStatement


and QsStatement =
    {
        Statement: QsStatementKind
        /// contains any symbol declared within this statement *that is visible after this statement ends*
        /// -> in particular, the loop variable within a for-statement is *not* visible after the statement ends, and hence not listed here (the same goes for qubit scopes)
        SymbolDeclarations: LocalDeclarations
        /// Handle for saving the location information for the statement.
        /// The position offset denotes the position relative to the beginning of the specialization declaration,
        /// and the range denotes the range of the block header.
        /// The location is Null for auto-generated statements.
        Location: QsNullable<QsLocation>
        /// contains comments in the code associated with this statement
        Comments: QsComments
    }


/// The source files of a syntax tree node.
type Source =
    {
        /// The path to the original source code file.
        CodeFile: string
        /// The path to the assembly file if the node was loaded from a reference.
        AssemblyFile: string QsNullable
    }

/// The module for source files.
module Source =
    /// The assembly file path for this source if one exists, otherwise the code file path.
    [<CompiledName "AssemblyOrCodeFile">]
    let assemblyOrCodeFile source =
        source.AssemblyFile.ValueOr source.CodeFile

    /// Whether the source is from a referenced assembly.
    [<CompiledName "IsReference">]
    let isReference (source: Source) = QsNullable.isValue source.AssemblyFile

// C# API for Source.
type Source with
    /// The assembly file path for this source if one exists, otherwise the code file path.
    member source.AssemblyOrCodeFile = Source.assemblyOrCodeFile source

    /// Whether the source is from a referenced assembly.
    member source.IsReference = Source.isReference source

    /// <summary>
    /// Returns a copy of this source with the given <paramref name="codeFile"/> or <paramref name="assemblyFile"/> if
    /// provided.
    /// </summary>
    member source.With
        (
            [<Optional; DefaultParameterValue null>] ?codeFile,
            [<Optional; DefaultParameterValue null>] ?assemblyFile
        ) =
        { source with
            CodeFile = codeFile |> Option.defaultValue source.CodeFile
            AssemblyFile = assemblyFile |> QsNullable.ofOption |> QsNullable.orElse source.AssemblyFile
        }


/// used to represent an attribute attached to a type, callable, or specialization declaration.
type QsDeclarationAttribute =
    {
        /// Identifies the user defined type that the attribute instantiates.
        /// Is Null only if the correct attribute could not be determined. Attributes set to Null should be ignored.
        TypeId: QsNullable<UserDefinedType>

        /// <summary>
        /// The range of <see cref="TypeId"/> relative to <see cref="Offset"/>.
        /// </summary>
        TypeIdRange: QsNullable<Range>

        /// Contains the argument with which the attribute is instantiated.
        Argument: TypedExpression

        /// Represents the position in the source file where the attribute is used.
        Offset: Position

        /// contains comments in the code associated with the attached attribute
        Comments: QsComments
    }

    /// <summary>
    /// Creates a new <see cref="QsDeclarationAttribute"/>.
    /// </summary>
    static member New(typeId, typeIdRange, argument, offset, comments) =
        {
            TypeId = typeId
            TypeIdRange = typeIdRange
            Argument = argument
            Offset = offset
            Comments = comments
        }

/// Fully resolved Q# callable signature
type ResolvedSignature =
    {
        /// contains the names of the type parameters for the callable,
        /// represented either as valid name containing a non-nullable string, or as an invalid name token
        TypeParameters: ImmutableArray<QsLocalSymbol>
        /// the fully resolve argument type
        ArgumentType: ResolvedType
        /// the fully resolved return type
        ReturnType: ResolvedType
        /// contains the functors that the callable supports (necessarily empty for functions)
        Information: CallableInformation
    }


type SpecializationImplementation =
    /// indicates the an implementation for this specialization is provided in Q# by the user
    /// the first item contains the argument tuple for the specialization, the second one the actual implementation
    | Provided of QsTuple<LocalVariableDeclaration<QsLocalSymbol>> * QsScope
    /// indicates that the target machine needs to provide the implementation for this specialization
    /// -> note that for any given callable, either all specializations need to be declared as intrinsic, or none of them
    | Intrinsic
    /// indicates that the specialization is defined in another assembly
    | External
    /// indicates that the specialization is to be generated by the compiler according to the given functor generator directive
    | Generated of QsGeneratorDirective // Invert and Distribute will be replaced by Provided before sending to code gen


/// For each callable various specialization exist describing how it acts
/// depending on the type of the argument it is called with (type specializations),
/// and/or which functors are applied to the call.
type QsSpecialization =
    {
        /// contains the functor specialization kind (specialization for body, adjoint, controlled, or controlled adjoint)
        Kind: QsSpecializationKind
        /// the fully qualified name of the callable this specialization extends
        Parent: QsQualifiedName
        /// contains all attributes associated with the specialization
        Attributes: ImmutableArray<QsDeclarationAttribute>
        /// The source where the specialization is declared in (not necessarily the same as the one of the callable it
        /// extends).
        Source: Source
        /// Contains the location information for the declared specialization.
        /// The position offset represents the position in the source file where the specialization is declared,
        /// and the range contains the range of the corresponding specialization header.
        /// For auto-generated specializations, the location is set to the location of the parent callable declaration.
        Location: QsNullable<QsLocation>
        /// contains the type arguments for which the implementation is specialized
        TypeArguments: QsNullable<ImmutableArray<ResolvedType>>
        /// full resolved signature of the specialization - i.e. signature including functor arguments after resolving all type specializations
        Signature: ResolvedSignature
        /// the implementation for this callable specialization
        Implementation: SpecializationImplementation
        /// content of documenting comments associated with the specialization
        Documentation: ImmutableArray<string>
        /// contains comments in the code associated with this specialization
        Comments: QsComments
    }
    member this.AddAttribute att =
        { this with Attributes = this.Attributes.Add att }

    member this.AddAttributes(att: _ seq) =
        { this with Attributes = this.Attributes.AddRange att }

    member this.WithImplementation impl = { this with Implementation = impl }

    member this.WithParent(getName: Func<_, _>) =
        { this with Parent = getName.Invoke(this.Parent) }

    member this.WithSource source = { this with Source = source }


/// describes a Q# function, operation, or type constructor
type QsCallable =
    {
        /// contains the callable kind (function, operation, or type constructor)
        Kind: QsCallableKind
        /// contains the name of the callable
        FullName: QsQualifiedName
        /// contains all attributes associated with the callable
        Attributes: ImmutableArray<QsDeclarationAttribute>
        /// The accessibility of the callable.
        Access: Access
        /// The source where the callable is declared in.
        Source: Source
        /// Contains the location information for the declared callable.
        /// The position offset represents the position in the source file where the callable is declared,
        /// and the range contains the range occupied by its name relative to that position.
        /// The location is Null for auto-generated callable constructed e.g. when lifting code blocks or lambdas to a global scope.
        Location: QsNullable<QsLocation>
        /// full resolved signature of the callable
        Signature: ResolvedSignature
        /// the argument tuple containing the names of the argument tuple items
        /// represented either as valid name containing a non-nullable string or as an invalid name token
        /// as well as their type
        ArgumentTuple: QsTuple<LocalVariableDeclaration<QsLocalSymbol>>
        /// all specializations declared for this callable -
        /// each call to the callable is dispatched to a suitable specialization
        /// depending on the type of the argument it is called with
        /// and/or which functors are applied to the call
        Specializations: ImmutableArray<QsSpecialization>
        /// content of documenting comments associated with the callable
        Documentation: ImmutableArray<string>
        /// contains comments in the code associated with this declarations
        Comments: QsComments
    }

    member this.AddAttribute att =
        { this with Attributes = this.Attributes.Add att }

    member this.AddAttributes(att: _ seq) =
        { this with Attributes = this.Attributes.AddRange att }

    member this.WithSpecializations(getSpecs: Func<_, _>) =
        { this with Specializations = getSpecs.Invoke(this.Specializations) }

    member this.WithFullName(getName: Func<_, _>) =
        { this with FullName = getName.Invoke(this.FullName) }

    member this.WithSource source = { this with Source = source }

    [<Newtonsoft.Json.JsonIgnore>]
    member this.IsIntrinsic = this.Signature.Information.InferredInformation.IsIntrinsic

    [<Newtonsoft.Json.JsonIgnore>]
    member this.IsSelfAdjoint = this.Signature.Information.InferredInformation.IsSelfAdjoint

/// used to represent the named and anonymous items in a user defined type
type QsTypeItem =
    /// represents a named item in a user defined type
    | Named of LocalVariableDeclaration<string>
    /// represents an anonymous item in a user defined type
    | Anonymous of ResolvedType


/// describes a Q# user defined type
type QsCustomType =
    {
        /// contains the name of the type
        FullName: QsQualifiedName
        /// contains all attributes associated with the type
        Attributes: ImmutableArray<QsDeclarationAttribute>
        /// The accessibility of the type.
        Access: Access
        /// The source where the type is declared in.
        Source: Source
        /// Contains the location information for the declared type.
        /// The position offset represents the position in the source file where the type is declared,
        /// and the range contains the range occupied by the type name relative to that position.
        /// The location is Null for auto-generated types defined by the compiler.
        Location: QsNullable<QsLocation>
        /// Contains the underlying Q# type.
        /// Note that a user defined type is *not* considered to be a subtype of its underlying type,
        /// but rather its own, entirely distinct type,
        /// and the underlying type is merely the argument type of the (auto-generated) type constructor associated with the user defined type.
        Type: ResolvedType
        /// contains the type tuple defining the named and anonymous items of the type
        TypeItems: QsTuple<QsTypeItem>
        /// content of documenting comments associated with the type
        Documentation: ImmutableArray<string>
        /// contains comments in the code associated with this declarations
        Comments: QsComments
    }

    member this.AddAttribute att =
        { this with Attributes = this.Attributes.Add att }

    member this.AddAttributes(att: _ seq) =
        { this with Attributes = this.Attributes.AddRange att }

    member this.WithFullName(getName: Func<_, _>) =
        { this with FullName = getName.Invoke(this.FullName) }

    member this.WithSource source = { this with Source = source }


/// Describes a valid Q# namespace element.
/// Q# namespaces may (only directly) contain type declarations, functions, operation,
/// and specializations for declared functions and operations (which are represented as part of the callable they belong to).
type QsNamespaceElement =
    /// denotes a Q# callable is either a function or operation (type constructors are auto-generated)
    | QsCallable of QsCallable
    /// denotes a Q# user defined type
    | QsCustomType of QsCustomType
    member this.GetFullName() =
        match this with
        | QsCallable call -> call.FullName
        | QsCustomType typ -> typ.FullName


/// Describes a Q# namespace.
/// Any valid Q# code has to be contained in a namespace, and Q# namespaces may only contain
/// type declaration, function, operations, and specializations for declared function and operations.
type QsNamespace =
    {
        /// the name of the namespace -
        /// represented as non-nullable string, since Q# does not support nested namespaces
        Name: string
        /// all elements contained in the namespace - i.e. functions, operations, and user defined types
        /// Note: specializations for declared callables must be contained in the same namespace as the callable declaration,
        /// and are represented as part of the callable they belong to.
        Elements: ImmutableArray<QsNamespaceElement>
        /// Contains all documentation for this namespace within this compilation unit.
        /// The key is the name of the source file the documentation has been specified in.
        Documentation: ILookup<string, ImmutableArray<string>>
    }
    member this.WithElements(getElements: Func<_, _>) =
        { this with Elements = getElements.Invoke(this.Elements) }

/// Describes a compiled Q# library or executable.
type QsCompilation =
    {
        /// Contains all compiled namespaces
        Namespaces: ImmutableArray<QsNamespace>
        /// Contains the names of all entry points of the compilation.
        /// In the case of a library the array is empty.
        EntryPoints: ImmutableArray<QsQualifiedName>
    }

    /// Returns the names of all callables that are part of the public API surface that can be used from outside Q#.
    /// If includeReferences is true, returns the names of the callables that are part of this surface,
    /// independent on whether they have been defined in the source code of this compilation unit or in a referenced assembly.
    /// Returns only the names of callables defined in source code otherwise.
    member this.InteroperableSurface includeReferences =
        let isInteropSourceApi (c: QsCallable) =
            c.Signature.TypeParameters.IsEmpty
            && c.Access = Public
            && (includeReferences || c.Source.AssemblyFile = Null)

        this.Namespaces
        |> Seq.collect (fun ns -> ns.Elements)
        |> Seq.choose (function
            | QsCallable c when c |> isInteropSourceApi -> Some c.FullName
            | _ -> None)
