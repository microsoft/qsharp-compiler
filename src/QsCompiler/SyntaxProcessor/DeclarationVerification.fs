// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

[<System.Runtime.CompilerServices.Extension>]
module Microsoft.Quantum.QsCompiler.SyntaxProcessing.Declarations

open System
open System.Collections.Immutable
open System.Linq
open System.Runtime.CompilerServices
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.VerificationTools
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree


// some convenient utils to call from C# on fragments

/// If the given symbol is a simple symbol, returns its name.
/// If the given symbol is invalid, returns the given fallback onInvalid.
/// Returns null otherwise.
[<Extension>]
let public AsDeclarationName sym onInvalid =
    match sym with
    | Symbol name -> name
    | InvalidSymbol -> onInvalid
    | _ -> null

let private NameOnly onInvalid arg =
    (arg |> QsNullable<_>.Map(fun (sym, _) -> AsDeclarationName sym.Symbol onInvalid)).ValueOr null

/// If the given fragment kind is a namespace declaration,
/// returns a tuple with the namespace symbol and null (to make the signature of this routine compatible with the remaining ones) as Value.
/// Returns Null otherwise.
[<Extension>]
let public DeclaredNamespace this: QsNullable<QsSymbol * obj> =
    match this with
    | NamespaceDeclaration sym -> (sym, null) |> Value
    | _ -> Null

/// If the given fragment kind is a namespace declaration,
/// returns the name of the namespace as string.
/// Returns the given fallback onInvalid if the name of the namespace is invalid.
/// Returns null otherwise.
[<Extension>]
let public DeclaredNamespaceName this onInvalid =
    this |> DeclaredNamespace |> NameOnly onInvalid

/// If the given fragment kind is an open directive,
/// returns a tuple with the symbol of the opened namespace and the defined short name (if any) as Value.
/// Returns Null otherwise.
[<Extension>]
let public OpenedNamespace this: QsNullable<QsSymbol * QsNullable<QsSymbol>> =
    match this with
    | OpenDirective (nsName, alias) -> (nsName, alias) |> Value
    | _ -> Null

/// If the given fragment kind is an open directive,
/// returns the name of the opened namespace as string.
/// Returns the given fallback onInvalid if the name of the namespace to open is invalid.
/// Returns null otherwise.
[<Extension>]
let public OpenedNamespaceName this onInvalid =
    this |> OpenedNamespace |> NameOnly onInvalid

/// If the given fragment kind is a type declaration,
/// returns the symbol for the type as well as the declared underlying type and modifiers as Value.
/// Returns Null otherwise.
[<Extension>]
let public DeclaredType this =
    match this with
    | TypeDefinition (mods, sym, decl) -> (sym, (mods, decl)) |> Value
    | _ -> Null

/// If the given fragment kind is a type declaration,
/// returns the name of the declared type as string.
/// Returns the given fallback onInvalid if the name of the declared type is invalid.
/// Returns null otherwise.
[<Extension>]
let public DeclaredTypeName this onInvalid =
    this |> DeclaredType |> NameOnly onInvalid

/// If the given fragment kind is a callable declaration,
/// returns the symbol for the callable as well as its declared kind, signature and modifiers as Value.
/// Returns Null otherwise.
[<Extension>]
let public DeclaredCallable this =
    match this with
    | FunctionDeclaration (mods, sym, decl) -> (sym, (QsCallableKind.Function, mods, decl)) |> Value
    | OperationDeclaration (mods, sym, decl) -> (sym, (QsCallableKind.Operation, mods, decl)) |> Value
    | _ -> Null

/// If the given fragment kind is a callable declaration,
/// returns the name of the declared callable as string.
/// Returns the given fallback onInvalid if the name of the declared callable is invalid.
/// Returns null otherwise.
[<Extension>]
let public DeclaredCallableName this onInvalid =
    this |> DeclaredCallable |> NameOnly onInvalid

/// If the given fragment kind is a specialization declaration,
/// returns its kind and generator as well as its type specializations as Value.
/// Returns Null otherwise.
/// The type specializations are given as either an Value containing an immutable array of Q# types,
/// or as Null, if no type specializations have been declared.
[<Extension>]
let public DeclaredSpecialization this
                                  : QsNullable<(QsSpecializationKind * QsSpecializationGenerator) * QsNullable<ImmutableArray<QsType>>> =
    match this with
    | BodyDeclaration gen -> ((QsSpecializationKind.QsBody, gen), Null) |> Value
    | AdjointDeclaration gen -> ((QsSpecializationKind.QsAdjoint, gen), Null) |> Value
    | ControlledDeclaration gen -> ((QsSpecializationKind.QsControlled, gen), Null) |> Value
    | ControlledAdjointDeclaration gen -> ((QsSpecializationKind.QsControlledAdjoint, gen), Null) |> Value
    | _ -> Null


// some utils for building the syntax tree

/// Returns true if the given symbol kind either corresponds to omitted symbols
/// or denotes an invalid symbol (up to arity-1 tuple wrapping).
let rec private isOmitted =
    function
    | SymbolTuple syms when syms.Length = 1 -> isOmitted syms.[0].Symbol
    | InvalidSymbol
    | OmittedSymbols -> true
    | _ -> false

/// Verifies whether the given Q# symbol indeed consists solely of omitted symbols,
/// and returns an array with suitable diagnostics if this is not the case.
/// If the given symbol consists of an empty symbol tuple, raises a DeprecatedArgumentForFunctorGenerator warning.
let private verifyIsOmittedOrUnit mismatchErr (arg: QsSymbol) =
    match arg.Symbol with
    | sym when sym |> isOmitted -> [||]
    | SymbolTuple syms when syms.Length = 0 ->
        [| arg.RangeOrDefault
           |> QsCompilerDiagnostic.Warning(WarningCode.DeprecatedArgumentForFunctorGenerator, []) |]
    | _ -> [| arg.RangeOrDefault |> QsCompilerDiagnostic.Error mismatchErr |]

/// Returns the name and the range of the symbol as Some,
/// if the given symbol kind either corresponds to an unqualified symbol
/// or denotes an invalid symbol (up to arity-1 tuple wrapping).
/// Returns None otherwise.
let rec private unqualifiedSymbol (qsSym: QsSymbol) =
    match qsSym.Symbol with
    | SymbolTuple syms when syms.Length = 1 -> unqualifiedSymbol syms.[0]
    | InvalidSymbol -> Some(InvalidName, qsSym.Range)
    | Symbol name -> Some(ValidName name, qsSym.Range)
    | _ -> None

/// Returns the name and the range of the symbol and a DeprecatedArgumentForFunctorGenerator if necessary,
/// if the given symbol kind either corresponds to a tuple of
/// either an unqualified or an invalid symbol and either omitted symbols or an invalid symbol,
/// or denotes an invalid symbol (up to arity-1 tuple wrapping).
/// Returns and InvalidName and the range of the symbol otherwise, as well as an array with suitable diagnostics.
let rec private singleAdditionalArg mismatchErr (qsSym: QsSymbol) =
    let singleAndOmitted =
        function
        | InvalidSymbol -> Some(InvalidName, qsSym.Range)
        | SymbolTuple syms when syms.Length = 2 && syms.[1].Symbol |> isOmitted -> syms.[0] |> unqualifiedSymbol
        | _ -> None

    let nameAndRange withDeprecatedWarning =
        function
        | Some name when not withDeprecatedWarning -> name, [||]
        | Some name ->
            name,
            [| qsSym.RangeOrDefault
               |> QsCompilerDiagnostic.Warning(WarningCode.DeprecatedArgumentForFunctorGenerator, []) |]
        | None -> (InvalidName, qsSym.Range), [| qsSym.RangeOrDefault |> QsCompilerDiagnostic.Error mismatchErr |]

    match qsSym.Symbol with
    | SymbolTuple syms when syms.Length = 1 -> singleAdditionalArg mismatchErr syms.[0]
    | Symbol _ -> qsSym |> unqualifiedSymbol |> nameAndRange true
    | sym -> sym |> singleAndOmitted |> nameAndRange false


let private StripRangeInfo = StripPositionInfo.Default.Namespaces.OnArgumentTuple

/// Given the declared argument tuple of a callable, and the declared symbol tuple for the corresponding body specialization,
/// verifies that the symbol tuple indeed has the expected shape for that specialization.
/// Returns the argument tuple for the specialization (to make the signature of this routine compatible with the remaining ones),
/// as well as an array of diagnostics.
[<Extension>]
let public BuildArgumentBody (this: QsTuple<LocalVariableDeclaration<QsLocalSymbol>>) (arg: QsSymbol) =
    this |> StripRangeInfo, arg |> verifyIsOmittedOrUnit (ErrorCode.BodyGenArgMismatch, [])

/// Given the declared argument tuple of a callable, and the declared symbol tuple for the corresponding ajoint specialization,
/// verifies that the symbol tuple indeed has the expected shape for that specialization.
/// Returns the argument tuple for the specialization (to make the signature of this routine compatible with the remaining ones),
/// as well as an array of diagnostics.
[<Extension>]
let public BuildArgumentAdjoint (this: QsTuple<LocalVariableDeclaration<QsLocalSymbol>>) (arg: QsSymbol) =
    this |> StripRangeInfo, arg |> verifyIsOmittedOrUnit (ErrorCode.AdjointGenArgMismatch, [])

/// Given the declared argument tuple of a callable, and the declared symbol tuple for the corresponding controlled specialization,
/// verifies that the symbol tuple indeed has the expected shape for that specialization.
/// Returns the argument tuple for the specialization, as well as an array of diagnostics.
[<Extension>]
let public BuildArgumentControlled (this: QsTuple<LocalVariableDeclaration<QsLocalSymbol>>) (arg: QsSymbol, pos) =
    let ctrlQs, diagnostics = arg |> singleAdditionalArg (ErrorCode.ControlledGenArgMismatch, [])
    SyntaxGenerator.WithControlQubits this pos ctrlQs |> StripRangeInfo, diagnostics

/// Given the declared argument tuple of a callable, and the declared symbol tuple for the corresponding controlled adjoint specialization,
/// verifies that the symbol tuple indeed has the expected shape for that specialization.
/// Returns the argument tuple for the specialization, as well as an array of diagnostics.
[<Extension>]
let public BuildArgumentControlledAdjoint (this: QsTuple<LocalVariableDeclaration<QsLocalSymbol>>) (arg: QsSymbol, pos) =
    let ctrlQs, diagnostics = arg |> singleAdditionalArg (ErrorCode.ControlledAdjointGenArgMismatch, [])
    SyntaxGenerator.WithControlQubits this pos ctrlQs |> StripRangeInfo, diagnostics


// some utils related to providing information on declared arguments

/// Given an immutable array with local variable declarations returns an immutable array containing only the declarations with a valid name.
[<Extension>]
let public ValidDeclarations (this: ImmutableArray<LocalVariableDeclaration<QsLocalSymbol>>) =
    let withValidName (d: LocalVariableDeclaration<_>) =
        match d.VariableName with
        | ValidName name ->
            let mut, qDep = d.InferredInformation.IsMutable, d.InferredInformation.HasLocalQuantumDependency
            Some(LocalVariableDeclaration<_>.New mut ((d.Position, d.Range), name, d.Type, qDep))
        | _ -> None

    (this |> Seq.choose withValidName).ToImmutableArray()

/// For each contained declaration in the given LocalDeclarations object,
/// applies the given function to its position offset and builds a new declaration with the position offset set to the returned value.
/// Returns the built declarations as LocalDeclarations object.
[<Extension>]
let public WithAbsolutePosition (this: LocalDeclarations) (updatePosition: Func<QsNullable<Position>, Position>) =
    LocalDeclarations.New
        (this
            .Variables
            .Select(Func<_, _>(fun (d: LocalVariableDeclaration<_>) ->
                        { d with Position = updatePosition.Invoke d.Position |> Value }))
            .ToImmutableArray())
