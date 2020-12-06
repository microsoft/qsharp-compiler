// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Linq
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.ReservedKeywords
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree


/// used to represent an unresolved attribute attached to a declaration
type AttributeAnnotation = {
    Id : QsSymbol
    Argument : QsExpression
    Position : Position
    Comments : QsComments
}
    with
    static member internal NonInterpolatedStringArgument inner = function
        | Item arg -> inner arg |> function
            | StringLiteral (str, interpol) when interpol.Length = 0 -> str
            | _ -> null
        | _ -> null


/// used internally for symbol resolution
type internal Resolution<'T,'R> = internal {
    Position : Position
    Range : Range
    Defined : 'T
    Resolved : QsNullable<'R>
    DefinedAttributes : ImmutableArray<AttributeAnnotation>
    ResolvedAttributes : ImmutableArray<QsDeclarationAttribute>
    Modifiers : Modifiers
    Documentation : ImmutableArray<string>
}

/// used internally for symbol resolution
type internal ResolvedGenerator = internal {
    TypeArguments   : QsNullable<ImmutableArray<ResolvedType>>
    Information     : CallableInformation
    Directive       : QsNullable<QsGeneratorDirective>
}

/// used to group the relevant sets of specializations (i.e. group them according to type- and set-arguments)
type SpecializationBundleProperties = internal {
    BundleInfo : CallableInformation
    DefinedGenerators : ImmutableDictionary<QsSpecializationKind, QsSpecializationGenerator>
}   with

    /// Given the type- and set-arguments associated with a certain specialization,
    /// determines the corresponding unique identifier for all specializations with the same type- and set-arguments.
    static member public BundleId (typeArgs : QsNullable<ImmutableArray<ResolvedType>>) =
        typeArgs |> QsNullable<_>.Map (fun args -> (args |> Seq.map (fun t -> t.WithoutRangeInfo)).ToImmutableArray())

    /// Returns an identifier for the bundle to which the given specialization declaration belongs to.
    /// Throws an InvalidOperationException if no (partial) resolution is defined for the given specialization.
    static member internal BundleId (spec : Resolution<_,_>) =
        match spec.Resolved with
        | Null -> InvalidOperationException "cannot determine id for unresolved specialization" |> raise
        | Value (gen : ResolvedGenerator) -> SpecializationBundleProperties.BundleId gen.TypeArguments

    /// Given a function that associates an item of the given array with a particular set of type- and set-arguments,
    /// as well as a function that associates it with a certain specialization kind,
    /// returns a dictionary that contains a dictionary mapping the specialization kind to the corresponding item for each set of type arguments.
    /// The keys for the returned dictionary are the BundleIds for particular sets of type- and characteristics-arguments.
    static member public Bundle (getTypeArgs : Func<_,_>, getKind : Func<_,QsSpecializationKind>) (specs : IEnumerable<'T>) =
        specs.ToLookup(new Func<_,_>(getTypeArgs.Invoke >> SpecializationBundleProperties.BundleId)).ToDictionary(
            (fun group -> group.Key),
            (fun group -> group.ToImmutableDictionary(getKind)))


/// Represents the outcome of resolving a symbol.
type ResolutionResult<'T> =
    /// A result indicating that the symbol resolved successfully.
    | Found of 'T
    /// A result indicating that an unqualified symbol is ambiguous, and it is possible to resolve it to more than one
    /// namespace. Includes the list of possible namespaces.
    | Ambiguous of string seq
    /// A result indicating that the symbol resolved to a declaration which is not accessible from the location
    /// referencing it.
    | Inaccessible
    /// A result indicating that no declaration with that name was found.
    | NotFound

/// Tools for processing resolution results.
module internal ResolutionResult =
    /// Applies the mapping function to the value of a Found result. If the result is not a Found, returns it unchanged.
    let internal Map mapping = function
        | Found value -> Found (mapping value)
        | Ambiguous namespaces -> Ambiguous namespaces
        | Inaccessible -> Inaccessible
        | NotFound -> NotFound

    /// Converts the resolution result to an option containing the Found value.
    let internal ToOption = function
        | Found value -> Some value
        | _ -> None

    /// Converts the resolution result to a 0- or 1-element list containing the Found value if the result is a Found.
    let private ToList = ToOption >> Option.toList

    /// Sorts the sequence of results in order of Found, Ambiguous, Inaccessible, and NotFound.
    ///
    /// If the sequence contains a Found result, the rest of the sequence after it is not automatically evaluated.
    /// Otherwise, the whole sequence may be evaluated by calling sort.
    let private Sort results =
        let choosers = [
            function | Found value -> Some (Found value) | _ -> None
            function | Ambiguous namespaces -> Some (Ambiguous namespaces) | _ -> None
            function | Inaccessible -> Some Inaccessible | _ -> None
            function | NotFound -> Some NotFound | _ -> None
        ]
        choosers
        |> Seq.map (fun chooser -> Seq.choose chooser results)
        |> Seq.concat

    /// Returns the first item of the sequence of results if there is one, or NotFound if the sequence is empty.
    let private TryHead<'T> : seq<ResolutionResult<'T>> -> ResolutionResult<'T> =
        Seq.tryHead >> Option.defaultValue NotFound

    /// Returns the first Found result in the sequence if it exists. If not, returns the first Ambiguous result if it
    /// exists. Repeats for Inaccessible and NotFound in this order. If the sequence is empty, returns NotFound.
    let internal TryFirstBest<'T> : seq<ResolutionResult<'T>> -> ResolutionResult<'T> =
        Sort >> TryHead

    /// Returns a Found result if there is only one in the sequence. If there is more than one, returns an Ambiguous
    /// result containing the namespaces of all Found results given by applying nsGetter to each value. Otherwise,
    /// returns the same value as TryFirstBest.
    let internal TryAtMostOne<'T> (nsGetter : 'T -> string) (results : seq<ResolutionResult<'T>>)
            : ResolutionResult<'T> =
        let found = results |> Seq.filter (function | Found _ -> true | _ -> false)
        if Seq.length found > 1
        then found
             |> Seq.map (Map nsGetter >> ToList)
             |> Seq.concat
             |> Ambiguous
        else found
             |> Seq.tryExactlyOne
             |> Option.defaultWith (fun () -> TryFirstBest results)

    /// Returns a Found result if there is only one in the sequence. If there is more than one, raises an exception.
    /// Otherwise, returns the same value as ResolutionResult.TryFirstBest.
    let internal AtMostOne<'T> : seq<ResolutionResult<'T>> -> ResolutionResult<'T> =
        TryAtMostOne (fun _ -> "") >> function
        | Ambiguous _ -> QsCompilerError.Raise "Resolution is ambiguous"
                         Exception () |> raise
        | result -> result

    /// Returns true if the resolution result indicates that a resolution exists (Found, Ambiguous or Inaccessible).
    let internal Exists = function
        | Found _
        | Ambiguous _
        | Inaccessible -> true
        | NotFound -> false

    /// Returns true if the resolution result indicates that a resolution is accessible (Found or Ambiguous).
    let internal IsAccessible = function
        | Found _
        | Ambiguous _ -> true
        | Inaccessible
        | NotFound -> false


module SymbolResolution = 

    // routines for giving deprecation warnings

    /// Returns true if any one of the given unresolved attributes indicates a deprecation.
    let internal IndicatesDeprecation checkQualification attribute = attribute.Id.Symbol |> function
        | Symbol sym -> sym = BuiltIn.Deprecated.FullName.Name && checkQualification ""
        | QualifiedSymbol (ns, sym) -> sym = BuiltIn.Deprecated.FullName.Name && (ns = BuiltIn.Deprecated.FullName.Namespace || checkQualification ns)
        | _ -> false

    /// Given the redirection extracted by TryFindRedirect,
    /// returns an array with diagnostics for using a type or callable with the given name at the given range.
    /// Returns an empty array if no redirection was determined, i.e. the given redirection was Null.
    let GenerateDeprecationWarning (fullName : QsQualifiedName, range) redirect = redirect |> function
        | Value redirect ->
            let usedName = sprintf "%s.%s" fullName.Namespace fullName.Name
            if String.IsNullOrWhiteSpace redirect then [| range |> QsCompilerDiagnostic.Warning (WarningCode.DeprecationWithoutRedirect, [usedName]) |]
            else [| range |> QsCompilerDiagnostic.Warning (WarningCode.DeprecationWithRedirect, [usedName; redirect]) |]
        | Null -> [| |]

    /// Applies the given getArgument function to the given attributes,
    /// and extracts and returns the non-interpolated string argument for all attributes for which getArgument returned Some.
    /// The returned sequence may contain null if the argument of an attribute for which getArgument returned Some was not a non-interpolated string.
    let private StringArgument (getArgument, getInner) attributes =
        let argument = getArgument >> function
            | Some redirect -> redirect |> AttributeAnnotation.NonInterpolatedStringArgument getInner |> Some
            | None -> None
        attributes |> Seq.choose argument

    /// Checks whether the given attributes indicate that the corresponding declaration has been deprecated.
    /// Returns a string containing the name of the type or callable to use instead as Value if this is the case, or Null otherwise.
    /// The returned string is empty or null if the declaration has been deprecated but an alternative is not specified or could not be determined.
    /// If several attributes indicate deprecation, a redirection is suggested based on the first deprecation attribute.
    let internal TryFindRedirectInUnresolved checkQualification attributes =
        let getRedirect (att : AttributeAnnotation) = if att |> IndicatesDeprecation checkQualification then Some att.Argument else None
        StringArgument (getRedirect, fun ex -> ex.Expression) attributes |> Seq.tryHead |> QsNullable<_>.FromOption

    /// Checks whether the given attributes indicate that the corresponding declaration has been deprecated.
    /// Returns a string containing the name of the type or callable to use instead as Value if this is the case, or Null otherwise.
    /// The returned string is empty or null if the declaration has been deprecated but an alternative is not specified or could not be determined.
    /// If several attributes indicate deprecation, a redirection is suggested based on the first deprecation attribute.
    let TryFindRedirect attributes =
        let getRedirect (att : QsDeclarationAttribute) = if att |> BuiltIn.MarksDeprecation then Some att.Argument else None
        StringArgument (getRedirect, fun ex -> ex.Expression) attributes |> Seq.tryHead |> QsNullable<_>.FromOption

    /// Converts the given string to a QsQualifiedName. 
    /// Returs the qualified name as some if the conversion succeeds, and None otherwise. 
    let private TryAsQualifiedName fullName = 
        let matchQualifiedName = SyntaxGenerator.FullyQualifiedName.Match fullName
        let asQualifiedName (str : string) = 
            let pieces = str.Split '.'
            {Namespace = String.Join('.', pieces.Take(pieces.Length-1)); Name = pieces.Last()}
        if matchQualifiedName.Success then Some (matchQualifiedName.Value |> asQualifiedName) else None

    /// Checks whether the given attributes define an alternative name that may be used when loading a type or callable for testing purposes.
    /// Returns the qualified name to use as Value if such a name is defined, or Null otherwise.
    /// If several attributes define such a name the returned Value is based on the first such attribute.
    let TryGetTestName attributes = 
        let getTestName (att : QsDeclarationAttribute) = if att |> BuiltIn.DefinesNameForTesting then Some att.Argument else None
        StringArgument (getTestName, fun ex -> ex.Expression) attributes |> Seq.tryHead |> Option.bind TryAsQualifiedName |> QsNullable<_>.FromOption

    /// Checks whether the given attributes indicate that the type or callable has been loaded via an alternative name for testing purposes.
    /// Returns the original qualified name as Value if this is the case, and Null otherwise.
    /// The returned Value is based on the first attribute that indicates the original name.
    let TryGetOriginalName attributes = 
        let loadedViaTestName (att : QsDeclarationAttribute) = if att |> BuiltIn.DefinesLoadedViaTestNameInsteadOf then Some att.Argument else None
        StringArgument (loadedViaTestName, fun ex -> ex.Expression) attributes |> Seq.tryHead |> Option.bind TryAsQualifiedName |> QsNullable<_>.FromOption

    /// Checks whether the given attributes indicate that the corresponding declaration contains a unit test.
    /// Returns a sequence of strings defining all execution targets on which the test should be run. Invalid execution targets are set to null.
    /// The returned sequence is empty if the declaration does not contain a unit test.
    let TryFindTestTargets attributes =
        let getTarget (att : QsDeclarationAttribute) = if att |> BuiltIn.MarksTestOperation then Some att.Argument else None
        let validTargets = CommandLineArguments.BuiltInSimulators.ToImmutableDictionary(fun t -> t.ToLowerInvariant())
        let targetName (target : string) =
            if target = null then null
            elif SyntaxGenerator.FullyQualifiedName.IsMatch target then target
            else target.ToLowerInvariant() |> validTargets.TryGetValue |> function
                | true, valid -> valid
                | false, _ -> null
        StringArgument (getTarget, fun ex -> ex.Expression) attributes
        |> Seq.map targetName |> ImmutableHashSet.CreateRange

    /// Returns the required runtime capability if the sequence of attributes contains at least one valid instance of
    /// the RequiresCapability attribute.
    let TryGetRequiredCapability attributes =
        let getCapability (att : QsDeclarationAttribute) = 
            if att |> BuiltIn.MarksRequiredCapability then att.Argument.Expression |> function 
                | ValueTuple vs when vs.Length = 2 -> Some vs.[0]
                | _ -> None
            else None
        let capabilities = 
            StringArgument (getCapability, fun ex -> ex.Expression) attributes
            |> QsNullable<_>.Choose RuntimeCapability.TryParse |> ImmutableHashSet.CreateRange
        if Seq.isEmpty capabilities then Null
        else capabilities |> Seq.reduce RuntimeCapability.Combine |> Value

    /// Checks whether the given attributes defines a code for an instruction within the quantum instruction set that matches this callable.
    /// Returns the string code as Value if this is the case, and Null otherwise.
    /// The returned Value is based on the first attribute that indicates the code.
    let TryGetTargetInstructionName attributes = 
        let loadedViaTestName (att : QsDeclarationAttribute) = if att |> BuiltIn.DefinesTargetInstruction then Some att.Argument else None
        StringArgument (loadedViaTestName, fun ex -> ex.Expression) attributes |> Seq.tryHead |> QsNullable<_>.FromOption


    // routines for resolving types and signatures

    /// helper function for ResolveType and ResolveCallableSignature
    let rec ResolveCharacteristics (ex : Characteristics) = // needs to preserve set parameters
        match ex.Characteristics with
        | EmptySet -> ResolvedCharacteristics.Empty
        | SimpleSet s -> SimpleSet s |> ResolvedCharacteristics.New
        | Intersection (s1, s2) -> Intersection (ResolveCharacteristics s1, ResolveCharacteristics s2) |> ResolvedCharacteristics.New
        | Union (s1, s2) -> Union (ResolveCharacteristics s1, ResolveCharacteristics s2) |> ResolvedCharacteristics.New
        | InvalidSetExpr -> InvalidSetExpr |> ResolvedCharacteristics.New

    /// helper function for ResolveType and ResolveCallableSignature
    let private AccumulateInner (resolve : 'a -> 'b * QsCompilerDiagnostic[]) build (items : IEnumerable<_>) =
        let inner = items.Select resolve |> Seq.toList // needed such that resolve is only evaluated once!
        let ts, errs = (inner.Select fst).ToImmutableArray(), inner.Select snd |> Array.concat
        build ts, errs

    /// Helper function for ResolveCallableSignature that verifies whether the given type parameters and the given return type
    /// are fully resolved by an argument of the given the argument type. Generates and returns suitable warnings if this is not the case.
    let private TypeParameterResolutionWarnings (argumentType : ResolvedType) (returnType : ResolvedType, range) typeParams =
        // FIXME: this verification needs to be done for each specialization individually once type specializations are fully supported
        let typeParamsResolvedByArg =
            let getTypeParams (t : ResolvedType) = t.Resolution |> function
                | QsTypeKind.TypeParameter (tp : QsTypeParameter) -> [tp.TypeName].AsEnumerable()
                | _ -> Enumerable.Empty()
            argumentType.ExtractAll getTypeParams |> Seq.toList
        let excessTypeParamWarn =
            let isUnresolvedByArg = function
                | (ValidName name, range) ->
                    if typeParamsResolvedByArg.Contains name then None
                    else range |> QsCompilerDiagnostic.Warning (WarningCode.TypeParameterNotResolvedByArgument, []) |> Some
                | _ -> None
            typeParams |> List.choose isUnresolvedByArg
        let unresolvableReturnType =
            let isUnresolved = function
                | QsTypeKind.TypeParameter (tp : QsTypeParameter) -> not (typeParamsResolvedByArg |> List.contains tp.TypeName)
                | _ -> false
            returnType.Exists isUnresolved
        let returnTypeErr = range |> QsCompilerDiagnostic.Warning (WarningCode.ReturnTypeNotResolvedByArgument, [])
        if unresolvableReturnType then returnTypeErr :: excessTypeParamWarn |> List.toArray else excessTypeParamWarn |> List.toArray

    /// Helper function for ResolveCallableSignature that resolves the given argument tuple
    /// using the given routine to resolve the declared item types.
    /// Throws an ArgumentException if the given argument is a QsTupleItem opposed to a QsTuple.
    let private ResolveArgumentTuple (resolveSymbol, resolveType) arg =
        let resolveArg (qsSym : QsSymbol, symType) =
            let range = qsSym.Range.ValueOr Range.Zero
            let t, tErrs = symType |> resolveType
            let variable, symErrs = resolveSymbol (qsSym.Symbol, range) t
            QsTupleItem variable, Array.concat [symErrs; tErrs]
        let rec resolveArgTupleItem = function
            | QsTupleItem (sym, symType) -> [(sym, symType)] |> AccumulateInner resolveArg (fun ts -> ts.[0])
            | QsTuple elements when elements.Length = 0 -> ArgumentException "argument tuple items cannot be empty tuples" |> raise
            | QsTuple elements when elements.Length = 1 -> resolveArgTupleItem elements.[0]
            | QsTuple elements -> elements |> AccumulateInner resolveArgTupleItem QsTuple
        match arg with
        | QsTuple elements -> elements |> AccumulateInner resolveArgTupleItem QsTuple
        | _ -> ArgumentException "the argument to a callable needs to be a QsTuple" |> raise

    /// Returns the LocalVariableDeclaration with the given name and type for an item within a type or callable declaration.
    /// Correspondingly, the item is immutable, has no quantum dependencies,
    /// the position information is set to null, and the range is set to the given one.
    let private DeclarationArgument range (name, t) =
        let info = {IsMutable = false; HasLocalQuantumDependency = false}
        {VariableName = name; Type = t; InferredInformation = info; Position = Null; Range = range}

    /// Give a list with the characteristics of all specializations as well as a routine for type resolution,
    /// fully resolves the given callable signature as well as its argument tuple.
    /// The position offset information for variables declared in the argument tuple will be set to Null.
    /// Returns the resolved signature and argument tuple, as well as an array with the diagnostics created during resolution.
    /// Throws an ArgumentException if the given list of specialization characteristics is empty.
    let internal ResolveCallableSignature (resolveType, specBundleInfos : CallableInformation list) (signature : CallableSignature) =
        let orDefault (range : QsNullable<_>) = range.ValueOr Range.Zero
        let typeParams, tpErrs =
            signature.TypeParameters |> Seq.fold (fun (tps, errs) qsSym ->
                let range = qsSym.Range |> orDefault
                let invalidTp = InvalidName, range
                match qsSym.Symbol with
                | QsSymbolKind.InvalidSymbol -> invalidTp :: tps, errs
                | QsSymbolKind.Symbol sym ->
                    if not (tps |> List.exists (fst >> (=)(ValidName sym))) then (ValidName sym, range) :: tps, errs
                    else invalidTp :: tps, (range |> QsCompilerDiagnostic.Error (ErrorCode.TypeParameterRedeclaration, [sym])) :: errs
                | _ -> invalidTp :: tps, (range |> QsCompilerDiagnostic.Error (ErrorCode.InvalidTypeParameterDeclaration, [])) :: errs
            ) ([], []) |> (fun (tps, errs) -> tps |> List.rev, errs |> List.rev |> List.toArray)
        let resolveArg (sym, range) t = sym |> function
            | QsSymbolKind.InvalidSymbol -> (InvalidName, t) |> DeclarationArgument range, [||]
            | QsSymbolKind.Symbol sym -> (ValidName sym, t) |> DeclarationArgument range, [||]
            | _ -> (InvalidName, t) |> DeclarationArgument range, [| range |> QsCompilerDiagnostic.Error (ErrorCode.ExpectingUnqualifiedSymbol, []) |]

        let resolveType =
            let validTpNames = typeParams |> List.choose (fst >> function | ValidName name -> Some name | InvalidName -> None)
            resolveType (validTpNames.ToImmutableArray())
        let argTuple, inErr = signature.Argument |> ResolveArgumentTuple (resolveArg, resolveType)
        let argType = argTuple.ResolveWith (fun x -> x.Type.WithoutRangeInfo)
        let returnType, outErr = signature.ReturnType |> resolveType
        let resolvedParams, resErrs =
            let errs = TypeParameterResolutionWarnings argType (returnType, signature.ReturnType.Range |> orDefault) typeParams
            (typeParams |> Seq.map fst).ToImmutableArray(), errs
        let callableInfo = CallableInformation.Common specBundleInfos
        let resolvedSig = { TypeParameters = resolvedParams; ArgumentType = argType; ReturnType = returnType; Information = callableInfo }
        (resolvedSig, argTuple), [inErr; outErr; resErrs; tpErrs] |> Array.concat

    /// Give a routine for type resolution, fully resolves the given user defined type as well as its items.
    /// The position offset information for the declared named items will be set to Null.
    /// Returns the underlying type as well as the item tuple, along with an array with the diagnostics created during resolution.
    /// Throws an ArgumentException if the given type tuple is an empty QsTuple.
    let internal ResolveTypeDeclaration resolveType (udtTuple : QsTuple<QsSymbol * QsType>) =
        let itemDeclarations = new List<LocalVariableDeclaration<string>>()
        let resolveItem (sym, range) t = sym |> function
            | QsSymbolKind.MissingSymbol
            | QsSymbolKind.InvalidSymbol -> Anonymous t, [||]
            | QsSymbolKind.Symbol sym when itemDeclarations.Exists (fun item -> item.VariableName = sym) ->
                Anonymous t, [| range |> QsCompilerDiagnostic.Error (ErrorCode.NamedItemAlreadyExists, [sym]) |]
            | QsSymbolKind.Symbol sym ->
                let info = {IsMutable = false; HasLocalQuantumDependency = false}
                itemDeclarations.Add { VariableName = sym; Type = t; InferredInformation = info; Position = Null; Range = range }
                (sym, t) |> DeclarationArgument range |> Named, [||]
            | _ -> Anonymous t, [| range |> QsCompilerDiagnostic.Error (ErrorCode.ExpectingUnqualifiedSymbol, []) |]
        let argTuple, errs = udtTuple |> function
            | QsTuple items when items.Length = 0 -> ArgumentException "underlying type in type declaration cannot be an empty tuple" |> raise
            | QsTuple _ -> udtTuple |> ResolveArgumentTuple (resolveItem, resolveType)
            | QsTupleItem _ -> ImmutableArray.Create udtTuple |> QsTuple |> ResolveArgumentTuple (resolveItem, resolveType)
        let underlyingType = argTuple.ResolveWith (function
            | Anonymous t -> t.WithoutRangeInfo
            | Named x -> x.Type.WithoutRangeInfo)
        (underlyingType, argTuple), errs

    /// Fully (i.e. recursively) resolves the given Q# type used within the given parent in the given source file.
    /// The resolution consists of replacing all unqualified names for user defined types by their qualified name.
    /// Generates an array of diagnostics for the cases where no user defined type of the specified name (qualified or unqualified) can be found.
    /// In that case, resolves the user defined type by replacing it with the Q# type denoting an invalid type.
    /// Verifies that all used type parameters are defined in the given list of type parameters,
    /// and generates suitable diagnostics if they are not, replacing them by the Q# type denoting an invalid type.
    /// Returns the resolved type as well as an array with diagnostics.
    /// IMPORTANT: for performance reasons does *not* verify if the given the given parent and/or source file is inconsistent with the defined callables. 
    /// May throw an ArgumentException if no namespace with the given name exists, or the given source file is not listed as source of that namespace. 
    /// Throws a NotSupportedException if the QsType to resolve contains a MissingType. 
    let rec internal ResolveType (processUDT, processTypeParameter) (qsType : QsType) = 
        let resolve = ResolveType (processUDT, processTypeParameter)
        let asResolvedType t = ResolvedType.New (true, t)
        let buildWith builder ts = builder ts |> asResolvedType
        let invalid = InvalidType |> asResolvedType
        let range = qsType.Range.ValueOr Range.Zero

        match qsType.Type with
        | ArrayType baseType -> [baseType] |> AccumulateInner resolve (buildWith (fun ts -> ArrayType ts.[0]))
        | TupleType items    -> items |> AccumulateInner resolve (buildWith TupleType)
        | QsTypeKind.TypeParameter sym -> sym.Symbol |> function
            | Symbol name -> processTypeParameter (name, sym.Range) |> fun (k, errs) -> k |> asResolvedType, errs
            | InvalidSymbol -> invalid, [||]
            | _ -> invalid, [| range |> QsCompilerDiagnostic.Error (ErrorCode.ExpectingUnqualifiedSymbol, []) |]
        | QsTypeKind.Operation ((arg,res), characteristics) ->
            let opInfo = {Characteristics = characteristics |> ResolveCharacteristics; InferredInformation = InferredCallableInformation.NoInformation}
            let builder (ts : ImmutableArray<_>) = QsTypeKind.Operation ((ts.[0], ts.[1]), opInfo)
            [arg; res] |> AccumulateInner resolve (buildWith builder)
        | QsTypeKind.Function (arg,res) -> [arg; res] |> AccumulateInner resolve (buildWith (fun ts -> QsTypeKind.Function (ts.[0], ts.[1])))
        | UserDefinedType name -> name.Symbol |> function
            | Symbol sym -> processUDT ((None, sym), name.Range) |> fun (k, errs) -> k |> asResolvedType, errs
            | QualifiedSymbol (ns, sym) -> processUDT ((Some ns, sym), name.Range) |> fun (k, errs) -> k |> asResolvedType, errs
            | InvalidSymbol -> invalid, [||]
            | MissingSymbol | OmittedSymbols | SymbolTuple _ -> invalid, [| range |> QsCompilerDiagnostic.Error (ErrorCode.ExpectingIdentifier, []) |]
        | UnitType    -> QsTypeKind.UnitType    |> asResolvedType, [||]
        | Int         -> QsTypeKind.Int         |> asResolvedType, [||]
        | BigInt      -> QsTypeKind.BigInt      |> asResolvedType, [||]
        | Double      -> QsTypeKind.Double      |> asResolvedType, [||]
        | Bool        -> QsTypeKind.Bool        |> asResolvedType, [||]
        | String      -> QsTypeKind.String      |> asResolvedType, [||]
        | Qubit       -> QsTypeKind.Qubit       |> asResolvedType, [||]
        | Result      -> QsTypeKind.Result      |> asResolvedType, [||]
        | Pauli       -> QsTypeKind.Pauli       |> asResolvedType, [||]
        | Range       -> QsTypeKind.Range       |> asResolvedType, [||]
        | InvalidType -> QsTypeKind.InvalidType |> asResolvedType, [||]
        | MissingType -> NotSupportedException "missing type cannot be resolved" |> raise

    /// Resolves the given attribute using the given function getAttribute to resolve the type id and expected argument type.
    /// Generates suitable diagnostics if a suitable attribute cannot be determined,
    /// or if the attribute argument contains expressions that are not supported,
    /// or if the resolved argument type does not match the expected argument type.
    /// The TypeId in the resolved attribute is set to Null if the unresolved Id is not a valid identifier
    /// or if the correct attribute cannot be determined, and is set to the corresponding type identifier otherwise.
    /// Throws an ArgumentException if a tuple-valued attribute argument does not contain at least one item.
    let internal ResolveAttribute getAttribute (attribute : AttributeAnnotation) =
        let asTypedExression range (exKind, exType) = {
            Expression = exKind
            TypeArguments = ImmutableArray.Empty
            ResolvedType = exType |> ResolvedType.New
            InferredInformation = {IsMutable = false; HasLocalQuantumDependency = false}
            Range = range}
        let invalidExpr range = (InvalidExpr, InvalidType) |> asTypedExression range
        let orDefault (range : QsNullable<_>) = range.ValueOr Range.Zero

        // We may in the future decide to support arbitary expressions as long as they can be evaluated at compile time.
        // At that point it may make sense to replace this with the standard resolution routine for typed expressions.
        // For now we support only a restrictive set of valid arguments.
        let rec ArgExression (ex : QsExpression) : TypedExpression * QsCompilerDiagnostic[] =
            let diagnostic code range = range |> orDefault |> QsCompilerDiagnostic.Error (code, [])
            // NOTE: if this is adapted, adapt the header hash as well
            match ex.Expression with
            | UnitValue          -> (UnitValue, UnitType) |> asTypedExression ex.Range, [||]
            | DoubleLiteral d    -> (DoubleLiteral d, Double) |> asTypedExression ex.Range, [||]
            | IntLiteral i       -> (IntLiteral i, Int) |> asTypedExression ex.Range, [||]
            | BigIntLiteral l    -> (BigIntLiteral l, BigInt) |> asTypedExression ex.Range, [||]
            | BoolLiteral b      -> (BoolLiteral b, Bool) |> asTypedExression ex.Range, [||]
            | ResultLiteral r    -> (ResultLiteral r, Result) |> asTypedExression ex.Range, [||]
            | PauliLiteral p     -> (PauliLiteral p, Pauli) |> asTypedExression ex.Range, [||]
            | StringLiteral (s, exs) ->
                if exs.Length <> 0 then invalidExpr ex.Range, [| ex.Range |> diagnostic ErrorCode.InterpolatedStringInAttribute |]
                else (StringLiteral (s, ImmutableArray.Empty), String) |> asTypedExression ex.Range, [||]
            | ValueTuple vs when vs.Length = 1 -> ArgExression (vs.First())
            | ValueTuple vs ->
                if vs.Length = 0 then ArgumentException "tuple valued attribute argument requires at least one tuple item" |> raise
                let innerExs, errs = aggregateInner vs
                let types = (innerExs |> Seq.map (fun ex -> ex.ResolvedType)).ToImmutableArray()
                (ValueTuple innerExs, TupleType types) |> asTypedExression ex.Range, errs
            | ValueArray vs ->
                let innerExs, errs = aggregateInner vs
                // we can make the following simple check since / as long as there is no variance behavior
                // for any of the supported attribute argument types
                let typeIfValid (ex : TypedExpression) =
                    match ex.ResolvedType.Resolution with
                    | InvalidType -> None
                    | _ -> Some ex.ResolvedType
                match innerExs |> Seq.choose typeIfValid |> Seq.distinct |> Seq.toList with
                | [bt] -> (ValueArray innerExs, ArrayType bt) |> asTypedExression ex.Range, errs
                | [] when innerExs.Length <> 0 -> (ValueArray innerExs, ResolvedType.New InvalidType |> ArrayType) |> asTypedExression ex.Range, errs
                | [] -> invalidExpr ex.Range, errs |> Array.append [| ex.Range |> diagnostic ErrorCode.EmptyValueArray |]
                | _ ->  invalidExpr ex.Range, errs |> Array.append [| ex.Range |> diagnostic ErrorCode.ArrayBaseTypeMismatch |]
            | NewArray (bt, idx) ->
                let onUdt (_, udtRange) = InvalidType, [| udtRange |> diagnostic ErrorCode.ArgumentOfUserDefinedTypeInAttribute |]
                let onTypeParam (_, tpRange) = InvalidType, [| tpRange |> diagnostic ErrorCode.TypeParameterizedArgumentInAttribute |]
                let resBaseType, typeErrs = ResolveType (onUdt, onTypeParam) bt
                let resIdx, idxErrs = ArgExression idx
                (NewArray (resBaseType, resIdx), ArrayType resBaseType) |> asTypedExression ex.Range, Array.concat [typeErrs; idxErrs]
            // TODO: detect constructor calls
            | InvalidExpr -> invalidExpr ex.Range, [||]
            | _ -> invalidExpr ex.Range, [| ex.Range |> diagnostic ErrorCode.InvalidAttributeArgument |]
        and aggregateInner vs =
            let innerExs, errs = vs |> Seq.map ArgExression |> Seq.toList |> List.unzip
            innerExs.ToImmutableArray(), Array.concat errs

        // Any user defined type that has been decorated with the attribute
        // "Attribute" defined in Microsoft.Quantum.Core may be used as attribute.
        let resArg, argErrs = ArgExression attribute.Argument
        let buildAttribute id = {TypeId = id; Argument = resArg; Offset = attribute.Position; Comments = attribute.Comments}
        let getAttribute (ns, sym) = getAttribute ((ns, sym), attribute.Id.Range) |> function
            | None, errs -> Null |> buildAttribute, errs |> Array.append argErrs
            | Some (name, argType : ResolvedType), errs ->
                // we can make the following simple check since / as long as there is no variance behavior
                // for any of the supported attribute argument types
                let isError (msg : QsCompilerDiagnostic) = msg.Diagnostic |> function | Error _ -> true | _ -> false
                let argIsInvalid = resArg.ResolvedType.Resolution = InvalidType || argErrs |> Array.exists isError
                if resArg.ResolvedType.WithoutRangeInfo <> argType.WithoutRangeInfo && not argIsInvalid then
                    let mismatchErr = attribute.Argument.Range |> orDefault |> QsCompilerDiagnostic.Error (ErrorCode.AttributeArgumentTypeMismatch, [])
                    Value name |> buildAttribute, Array.concat [errs; argErrs; [| mismatchErr |]]
                else Value name |> buildAttribute, errs |> Array.append argErrs
        match attribute.Id.Symbol with
        | Symbol sym -> getAttribute (None, sym)
        | QualifiedSymbol (ns, sym) -> getAttribute (Some ns, sym)
        | InvalidSymbol -> Null |> buildAttribute, argErrs
        | MissingSymbol | OmittedSymbols | SymbolTuple _ ->
            Null |> buildAttribute, [| attribute.Id.Range |> orDefault |> QsCompilerDiagnostic.Error (ErrorCode.InvalidAttributeIdentifier, []) |]


    // private routines for resolving specialization generation directives

    /// Resolves the given specialization generator to a suitable implementation under the assumption
    /// that at least one specialization for the same type- and set-arguments has been declared as intrinsic.
    /// In particular, resolves the given generator to either and intrinsic implementation,
    /// or to the generator directive "self" if allowSelf is set to true.
    /// Returns the generated implementation as Value, along with an array of diagnostics.
    /// Does *not* generate diagnostics for things that do not require semantic information to detect
    /// (these should be detected and raised upon context verification).
    let private NeedsToBeIntrinsic (gen : QsSpecializationGenerator, allowSelf) =
        let genRange = gen.Range.ValueOr Range.Zero
        let isSelfInverse = function | SelfInverse -> true | _ -> false
        let isInvalid = function | InvalidGenerator -> true | _ -> false
        match gen.Generator with
        | QsSpecializationGeneratorKind.Intrinsic
        | QsSpecializationGeneratorKind.AutoGenerated -> Intrinsic |> Value, [||]
        | QsSpecializationGeneratorKind.UserDefinedImplementation _ ->
            Intrinsic |> Value, [| genRange |> QsCompilerDiagnostic.Error (ErrorCode.UserDefinedImplementationForIntrinsic, []) |]
        | QsSpecializationGeneratorKind.FunctorGenerationDirective dir ->
            if isSelfInverse dir then (if allowSelf then Generated SelfInverse else Intrinsic) |> Value, [||] // a context error is raised if self is not valid
            elif isInvalid dir then Intrinsic |> Value, [||]
            else Intrinsic |> Value, [| genRange |> QsCompilerDiagnostic.Warning (WarningCode.GeneratorDirectiveWillBeIgnored, []) |]

    /// Resolves the given specialization generator to a "self" generator directive,
    /// and returns it as Value along with an array of diagnostics.
    /// Does *not* generate diagnostics for things that do not require semantic information to detect
    /// (these should be detected and raised upon context verification).
    let private NeedsToBeSelfInverse (gen : QsSpecializationGenerator) =
        let genRange = gen.Range.ValueOr Range.Zero
        let isSelfInverse = function | SelfInverse -> true | _ -> false
        let isInvalid = function | InvalidGenerator -> true | _ -> false
        let diagnostics = gen.Generator |> function
            | QsSpecializationGeneratorKind.Intrinsic
            | QsSpecializationGeneratorKind.AutoGenerated -> [||]
            | QsSpecializationGeneratorKind.UserDefinedImplementation _ ->
                [| genRange |> QsCompilerDiagnostic.Error (ErrorCode.NonSelfGeneratorForSelfadjoint, []) |]
            | QsSpecializationGeneratorKind.FunctorGenerationDirective dir ->
                if isSelfInverse dir || isInvalid dir then [||]
                else [| genRange |> QsCompilerDiagnostic.Error (ErrorCode.NonSelfGeneratorForSelfadjoint, []) |]
        Generated SelfInverse |> Value, diagnostics


    /// Given the generator of a body specialization declaration,
    /// returns Null if the generator indicates a user defined specialization, and returns the resolved implementation as Value otherwise.
    /// Generates and returns an array of diagnostics.
    /// If containsIntrinsic is set to true, generates a suitable error if the generator to resolve is incompatible, and resolves it to "intrinsic".
    /// The resolution corresponds to an invalid generator directive unless the generator is either intrinsic, user defined or "auto".
    /// Does *not* generate diagnostics for things that do not require semantic information to detect
    /// (these should be detected and raised upon context verification).
    let private ResolveBodyGeneratorDirective (opInfo : InferredCallableInformation) (gen : QsSpecializationGenerator) =
        if opInfo.IsIntrinsic then NeedsToBeIntrinsic (gen, false)
        else gen.Generator |> function
            | QsSpecializationGeneratorKind.Intrinsic -> Intrinsic |> Value, [||]
            | QsSpecializationGeneratorKind.UserDefinedImplementation _ -> Null, [||]
            | QsSpecializationGeneratorKind.FunctorGenerationDirective dir -> dir |> function
                | Distribute | SelfInverse | Invert | InvalidGenerator -> Generated InvalidGenerator |> Value, [||] // a context error is raised in this case
            | QsSpecializationGeneratorKind.AutoGenerated -> Intrinsic |> Value, [||] // todo: generate based on controlled if possible?

    /// Given the generator of an adjoint specialization declaration,
    /// returns Null if the generator indicates a user defined specialization, and returns the resolved implementation as Value otherwise.
    /// Generates and returns an array of diagnostics.
    /// If containsIntrinsic is set to true, generates a suitable error if the generator to resolve is incompatible,
    /// and resolves it to either "intrinsic" or - if containsSelfInverse is set to true - to "self".
    /// Otherwise it resolves any valid directive to either an "invert" or a "self" directive depending on whether containsSelfInverse is set to true.
    /// Does *not* generate diagnostics for things that do not require semantic information to detect
    /// (these should be detected and raised upon context verification).
    let private ResolveAdjointGeneratorDirective (info : InferredCallableInformation) (gen : QsSpecializationGenerator) =
        if info.IsSelfAdjoint then NeedsToBeSelfInverse gen
        elif info.IsIntrinsic then NeedsToBeIntrinsic (gen, true)
        else gen.Generator |> function
            | QsSpecializationGeneratorKind.Intrinsic -> Intrinsic |> Value, [||]
            | QsSpecializationGeneratorKind.UserDefinedImplementation _ -> Null, [||]
            | QsSpecializationGeneratorKind.FunctorGenerationDirective dir -> dir |> function
                | Distribute -> Generated InvalidGenerator |> Value, [||] // a context error is raised in this case
                | SelfInverse | Invert | InvalidGenerator -> Generated dir |> Value, [||]
            | QsSpecializationGeneratorKind.AutoGenerated -> Generated Invert |> Value, [||]

    /// Given the generator of a controlled specialization declaration,
    /// returns Null if the generator indicates a user defined specialization, and returns the resolved implementation as Value otherwise.
    /// Generates and returns an array of diagnostics.
    /// If containsIntrinsic is set to true, generates a suitable error if the generator to resolve is incompatible, and resolves it to "intrinsic".
    /// Otherwise resolves any valid directive to a "distribute" directive.
    /// Does *not* generate diagnostics for things that do not require semantic information to detect
    /// (these should be detected and raised upon context verification).
    let private ResolveControlledGeneratorDirective (info : InferredCallableInformation) (gen : QsSpecializationGenerator) =
        if info.IsIntrinsic then NeedsToBeIntrinsic (gen, false)
        else gen.Generator |> function
            | QsSpecializationGeneratorKind.Intrinsic -> Intrinsic |> Value, [||]
            | QsSpecializationGeneratorKind.UserDefinedImplementation _ -> Null, [||]
            | QsSpecializationGeneratorKind.FunctorGenerationDirective dir -> dir |> function
                | SelfInverse | Invert -> Generated InvalidGenerator |> Value, [||] // a context error is raised in this case
                | Distribute | InvalidGenerator -> Generated dir |> Value, [||]
            | QsSpecializationGeneratorKind.AutoGenerated -> Generated Distribute |> Value, [||]

    /// Given the generator of a controlled adjoint specialization declaration,
    /// returns Null if the generator indicates a user defined specialization, and returns the resolved implementation as Value otherwise.
    /// Generates and returns an array of diagnostics.
    /// If containsIntrinsic is set to true, generates a suitable error if the generator to resolve is incompatible,
    /// and resolves it to either "intrinsic" or - if containsSelfInverse is set to true - to "self".
    /// If this is not the case, all specialization generator directives are resolved to themselves unless they are specified as "auto".
    /// If an automatic determination of a suitable directive is requested (as indicated by "auto"), then it is resolved to
    /// a) a self inverse directive, if the corresponding adjoint specialization is self inverse, and
    /// b) to "invert" if the corresponding adjoint specialization is compiler generated but the controlled version is user defined, and
    /// b) to a "distribute" directive to be applied to the adjoint version otherwise.
    /// Does *not* generate diagnostics for things that do not require semantic information to detect
    /// (these should be detected and raised upon context verification).
    let private ResolveControlledAdjointGeneratorDirective (adjGenKind, ctlGenKind) (info : InferredCallableInformation) (gen : QsSpecializationGenerator) =
        if info.IsSelfAdjoint then NeedsToBeSelfInverse gen
        elif info.IsIntrinsic then NeedsToBeIntrinsic (gen, true)
        else gen.Generator |> function
            | QsSpecializationGeneratorKind.Intrinsic -> Intrinsic |> Value, [||]
            | QsSpecializationGeneratorKind.UserDefinedImplementation _ -> Null, [||]
            | QsSpecializationGeneratorKind.FunctorGenerationDirective dir -> dir |> function
                | Distribute | SelfInverse | Invert | InvalidGenerator -> Generated dir |> Value, [||]
            | QsSpecializationGeneratorKind.AutoGenerated -> (ctlGenKind, adjGenKind) |> function
                | UserDefinedImplementation _, FunctorGenerationDirective _
                | UserDefinedImplementation _, AutoGenerated -> Generated Invert |> Value, [||]
                | _ -> Generated Distribute |> Value, [||]


    // routines for resolving specialization declarations

    /// Resolves the type- and set-arguments of the given specialization using the given function.
    /// Returns the resolved arguments as well as an array of diagnostics.
    /// Does nothing and simply returns the resolution of the given specialization if a resolution has already been set.
    let internal ResolveTypeArgument typeResolution (_, spec : Resolution<QsSpecializationGenerator, _>) =
        let resolveGenerator () =
            let typeArgs, tErrs = spec.Defined.TypeArguments |> function
                | Null -> Null, [||]
                | Value targs ->
                    let resolved, errs = targs |> Seq.map typeResolution |> Seq.toList |> List.unzip
                    resolved.ToImmutableArray() |> Value, errs |> Array.concat
            let resolvedGen = {TypeArguments = typeArgs; Information = CallableInformation.Invalid; Directive = Null}
            Value resolvedGen, tErrs |> Array.map (fun msg -> spec.Position, msg)
        match spec.Resolved with
        | Value resolvedGen -> Value resolvedGen, [||]
        | Null -> resolveGenerator()

    /// Given a dictionary of all existing specializations for a particular set of type- and set-arguments
    /// that maps the specialization kind to the corresponding generator, as well as the characteristics and location of the callable declaration,
    /// determines the resolved characteristics of the specializations for these type- and set-arguments.
    /// Calls generateSpecialization for each missing specialization kind that can be inferred.
    /// Returns the resolved characteristics as well as an array of diagnostics.
    let private InferCharacteristicsAndMetadata generateSpecialization (specKinds : ImmutableDictionary<_,_>) (characteristics : Characteristics, declLocation : QsLocation) =
        let adjExists, ctlExists = specKinds.ContainsKey QsAdjoint, specKinds.ContainsKey QsControlled
        let bodyExists, ctlAdjExists = specKinds.ContainsKey QsBody, specKinds.ContainsKey QsControlledAdjoint

        let annotRange cond = if cond then characteristics.Range else Null
        let resolved, (supportsAdj, adjRange), (supportsCtl, ctlRange) =
            let declCharacteristics = ResolveCharacteristics characteristics
            if not declCharacteristics.AreInvalid then
                let supported = declCharacteristics.GetProperties()
                let adjSup, ctlSup = supported.Contains Adjointable, supported.Contains Controllable
                let additional = ResolvedCharacteristics.FromProperties (seq {
                    if (adjExists || ctlAdjExists) && not adjSup then yield Adjointable
                    if (ctlExists || ctlAdjExists) && not ctlSup then yield Controllable })
                let adj, ctl = (adjExists || ctlAdjExists || adjSup, annotRange adjSup), (ctlExists || ctlAdjExists || ctlSup, annotRange ctlSup)
                Union (declCharacteristics, additional) |> ResolvedCharacteristics.New, adj, ctl
            else declCharacteristics, (adjExists || ctlAdjExists, Null), (ctlExists || ctlAdjExists, Null)
        let metadata =
            let isIntrinsic = function | QsSpecializationGeneratorKind.Intrinsic -> true | _ -> false
            let intrinsic = specKinds.Values |> Seq.map (fun g -> g.Generator) |> Seq.exists isIntrinsic
            let selfGenerator = specKinds.Values |> Seq.exists (fun gen -> gen.Generator = FunctorGenerationDirective SelfInverse)
            {IsSelfAdjoint = selfGenerator; IsIntrinsic = intrinsic}

        let ctlAdjRange = annotRange (adjRange <> Null && ctlRange <> Null)
        let errs = Array.concat [
            if supportsAdj && not adjExists then yield generateSpecialization QsAdjoint (declLocation, adjRange)
            if supportsCtl && not ctlExists then yield generateSpecialization QsControlled (declLocation, ctlRange)
            if supportsAdj && supportsCtl && not ctlAdjExists then yield generateSpecialization QsControlledAdjoint (declLocation, ctlAdjRange)
            if not bodyExists then
                yield generateSpecialization QsBody (declLocation, Null)
                yield [| declLocation.Range |> QsCompilerDiagnostic.Warning  (WarningCode.MissingBodyDeclaration, []) |] ]
        let isError (m : QsCompilerDiagnostic) = m.Diagnostic |> function | Error _ -> true | _ -> false
        if errs |> Array.exists isError then InvalidSetExpr |> ResolvedCharacteristics.New, metadata, errs
        else resolved, metadata, errs

    /// Given the signature and source file of a callable as well as all specializations defined for it, constructs
    /// a dictionary that contains the bundle properties for each set of type- and set-arguments for which the callable has been specialized.
    /// The keys of the dictionary are given by the BundleIds obtained for the type- and set-arguments in question.
    /// Calls generateSpecialization for each specialization that is not listed in the given collection of specializations but can be inferred,
    /// either based on the declared characteristics of the parent callable or based on other existing specializations.
    /// Returns the constructed dictionary as well as an array of diagnostics.
    /// Throws an InvalidOperationException if no (partial) resolution is defined for any one of the given specializations.
    let internal GetBundleProperties generateSpecialization (parentSignature : Resolution<CallableSignature,_>, source) (definedSpecs : IEnumerable<_>) =
        let declCharacteristics = parentSignature.Defined.Characteristics // if we allow to specialize for certain set parameters, then these need to be resolved in parent
        let declLocation = {Offset = parentSignature.Position; Range = parentSignature.Range}
        let definedSpecs = definedSpecs.ToLookup (snd >> snd >> (SpecializationBundleProperties.BundleId : Resolution<_,_> -> _), id)

        let mutable errs = []
        let bundleProps (relevantSpecs : IEnumerable<_>, definedArgs) =
            let gens, bundleErrs =
                let relevantSpecs = relevantSpecs.ToLookup(fst, snd)
                let positionedErr errCode (specSource, res : Resolution<_,_>) = specSource, (res.Position, res.Range |> QsCompilerDiagnostic.Error (errCode, []))
                let errs =
                    (relevantSpecs.[QsBody].Skip(1) |> Seq.map (positionedErr ErrorCode.RedefinitionOfBody))
                    |> Seq.append (relevantSpecs.[QsAdjoint].Skip(1) |> Seq.map (positionedErr ErrorCode.RedefinitionOfAdjoint))
                    |> Seq.append (relevantSpecs.[QsControlled].Skip(1) |> Seq.map (positionedErr ErrorCode.RedefinitionOfControlled))
                    |> Seq.append (relevantSpecs.[QsControlledAdjoint].Skip(1) |> Seq.map (positionedErr ErrorCode.RedefinitionOfControlledAdjoint))
                relevantSpecs.ToImmutableDictionary((fun g -> g.Key), (fun g -> (g.First() |> snd).Defined)), errs.ToArray()
            errs <- bundleErrs :: errs
            let characteristics, metadata, affErrs = InferCharacteristicsAndMetadata (generateSpecialization definedArgs) gens (declCharacteristics, declLocation)
            errs <- (affErrs |> Array.map (fun msg -> source, (parentSignature.Position, msg))) :: errs
            {BundleInfo = {Characteristics = characteristics; InferredInformation = metadata}; DefinedGenerators = gens}

        let props = ImmutableDictionary.CreateBuilder()
        for group in definedSpecs do props.Add(group.Key, bundleProps(group, (group.First() |> snd |> snd).Defined.TypeArguments))
        props.ToImmutableDictionary(), errs

    /// Given a dictionary that maps the BundleId for each set of type- and set-arguments for which the callable has been specialized
    /// to the corresponding bundle properties determines the resolution for the given specialization of the given kind.
    /// Returns the resolved generator as well as an array of diagnostics generated during resolution.
    /// Throws an InvalidOperationException if no (partial) resolution is defined for the given specialization.
    /// Fails with the standard KeyNotFoundException if the given specialization is not part of a specialization bundle in the given properties dictionary.
    let internal ResolveGenerator (properties : ImmutableDictionary<_,_>) (kind, spec : Resolution<QsSpecializationGenerator, ResolvedGenerator>) =
        let bundle : SpecializationBundleProperties = properties.[SpecializationBundleProperties.BundleId spec]
        let impl, err = kind |> function
            | QsBody -> ResolveBodyGeneratorDirective bundle.BundleInfo.InferredInformation spec.Defined
            | QsAdjoint -> ResolveAdjointGeneratorDirective bundle.BundleInfo.InferredInformation spec.Defined
            | QsControlled -> ResolveControlledGeneratorDirective bundle.BundleInfo.InferredInformation spec.Defined
            | QsControlledAdjoint ->
                let getGenKindOrAuto kind = bundle.DefinedGenerators.TryGetValue kind |> function
                    | true, (gen : QsSpecializationGenerator) -> gen.Generator
                    | false, _ -> AutoGenerated // automatically inserted specializations won't be part of the bundle
                let adjGen, ctlGen = getGenKindOrAuto QsAdjoint, getGenKindOrAuto QsControlled
                ResolveControlledAdjointGeneratorDirective (adjGen, ctlGen) bundle.BundleInfo.InferredInformation spec.Defined
        let dir = impl |> function | Value (Generated dir) -> Value dir | _ -> Null
        let resolvedGen = spec.Resolved |> QsNullable<_>.Map (fun resolution -> {resolution with Information = bundle.BundleInfo; Directive = dir})
        resolvedGen, err |> Array.map (fun msg -> spec.Position, msg)
