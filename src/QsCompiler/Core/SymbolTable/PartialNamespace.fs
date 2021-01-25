// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SymbolManagement

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Linq

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Utils

/// Represents the partial declaration of a namespace in a single file.
///
/// Note that this class is *not* thread-safe, and access modifiers are always ignored when looking up declarations.
type private PartialNamespace private (name: string,
                                       source: string,
                                       documentation: IEnumerable<ImmutableArray<string>>,
                                       openNS: IEnumerable<KeyValuePair<string, string>>,
                                       typeDecl: IEnumerable<KeyValuePair<string, Resolution<QsTuple<QsSymbol * QsType>, ResolvedType * QsTuple<QsTypeItem>>>>,
                                       callableDecl: IEnumerable<KeyValuePair<string, QsCallableKind * Resolution<CallableSignature, ResolvedSignature * QsTuple<LocalVariableDeclaration<QsLocalSymbol>>>>>,
                                       specializations: IEnumerable<KeyValuePair<string, List<QsSpecializationKind * Resolution<QsSpecializationGenerator, ResolvedGenerator>>>>) =

    let keySelector (item: KeyValuePair<'k, 'v>) = item.Key
    let valueSelector (item: KeyValuePair<'k, 'v>) = item.Value

    let unresolved (location: QsLocation) (definition, attributes, visibility, doc) =
        {
            Defined = definition
            DefinedAttributes = attributes
            Resolved = Null
            ResolvedAttributes = ImmutableArray.Empty
            Visibility = visibility
            Position = location.Offset
            Range = location.Range
            Documentation = doc
        }

    /// list containing all documentation for this namespace within this source file
    /// -> a list since the namespace can in principle occur several times in the same file each time with documentation
    let AssociatedDocumentation = documentation.ToList()
    /// list of namespaces open or aliased within this namespace and file
    let OpenNamespaces = openNS.ToDictionary(keySelector, valueSelector)
    /// dictionary of types declared within this namespace and file
    /// the key is the name of the type
    let TypeDeclarations = typeDecl.ToDictionary(keySelector, valueSelector)
    /// dictionary of callables declared within this namespace and file
    /// includes functions, operations, *and* (auto-generated) type constructors
    /// the key is the name of the callable
    let CallableDeclarations = callableDecl.ToDictionary(keySelector, valueSelector)
    /// dictionary of callable specializations declared within this namespace and file
    /// -> note that all specializations that are declared in a namespace *have* to extend a declarations in the same namespace,
    /// -> however, they may be declared in a source file (or even compilation unit) that is different from the one of the original declaration
    /// the key is the name of the callable, and the key to the returned map is the specialization kind (body, adjoint, controlled, or controlled adjoint)
    let CallableSpecializations =
        let specs = specializations |> Seq.map (fun entry -> entry.Key, (entry.Value.ToList()))
        specs.ToDictionary(fst, snd)

    /// constructor taking the name of the namespace as well as the name of the file it is declared in as arguments
    internal new(name, source) =
        new PartialNamespace(name, source, [], [ new KeyValuePair<_, _>(name, null) ], [], [], [])

    /// returns a new PartialNamespace that is an exact copy of this one
    /// -> any modification of the returned PartialNamespace is not reflected in this one
    member this.Copy() =
        let openNS = OpenNamespaces
        let doc = AssociatedDocumentation
        let typeDecl = TypeDeclarations
        let callableDecl = CallableDeclarations
        let specializations = CallableSpecializations
        new PartialNamespace(name, source, doc, openNS, typeDecl, callableDecl, specializations)


    /// name of the namespace
    member this.Name = name
    /// name of the source file this (part of) the namespace if declared in
    member this.Source = source
    /// contains all documentation associated with this namespace within this source file
    member this.Documentation = AssociatedDocumentation.ToImmutableArray()
    /// namespaces open or aliased within this part of the namespace - this includes the namespace itself
    member this.ImportedNamespaces = OpenNamespaces.ToImmutableDictionary()

    /// types defined within this (part of) the namespace
    /// -> NOTE: the returned enumerable is *not* immutable and may change over time!
    member internal this.DefinedTypes = TypeDeclarations.Select(fun item -> item.Key, item.Value)
    /// callables defined within this (part of) the namespace (includes auto-generated type constructors)
    /// -> NOTE: the returned enumerable is *not* immutable and may change over time!
    member internal this.DefinedCallables = CallableDeclarations.Select(fun item -> item.Key, item.Value)

    /// returns a dictionary with all currently known namespace short names and which namespace they represent
    member internal this.NamespaceShortNames =
        let shortNames = this.ImportedNamespaces |> Seq.filter (fun kv -> kv.Value <> null)
        shortNames.ToImmutableDictionary((fun kv -> kv.Value), (fun kv -> kv.Key))

    /// <summary>Gets the type with the given name from the dictionary of declared types.</summary>
    /// <exception cref="SymbolNotFoundException">A type with the given name was not found.</exception>
    member internal this.GetType tName =
        TypeDeclarations.TryGetValue tName
        |> tryOption
        |> Option.defaultWith (fun () -> SymbolNotFoundException "A type with the given name was not found." |> raise)

    member internal this.ContainsType = TypeDeclarations.ContainsKey

    member internal this.TryGetType = TypeDeclarations.TryGetValue

    /// <summary>Gets the callable with the given name from the dictionary of declared callable.</summary>
    /// <exception cref="SymbolNotFoundException">A callable with the given name was not found.</exception>
    member internal this.GetCallable cName =
        CallableDeclarations.TryGetValue cName
        |> tryOption
        |> Option.defaultWith (fun () ->
            SymbolNotFoundException "A callable with the given name was not found." |> raise)

    member internal this.ContainsCallable = CallableDeclarations.ContainsKey

    member internal this.TryGetCallable = CallableDeclarations.TryGetValue

    /// Given a callable name, returns all specializations for it defined within this part of the namespace.
    /// NOTE: The verification of whether a callable with that name has been declared in this namespace needs to be done by the calling routine.
    member internal this.GetSpecializations cName =
        match CallableSpecializations.TryGetValue cName with
        | true, specs -> specs.ToImmutableArray()
        | false, _ -> ImmutableArray.Empty // mustn't fail, since the query is valid even if the callable is declared in another file


    /// Adds the given lines of documentation to the list of documenting sections
    /// associated with this namespace within this source file.
    member this.AddDocumentation(doc: IEnumerable<_>) =
        AssociatedDocumentation.Add(doc.ToImmutableArray())

    /// If the given namespace name is not already listened as imported, adds the given namespace name to the list of open namespaces.
    /// -> Note that this routine will fail with the standard dictionary.Add error if an open directive for the given namespace name already exists.
    /// -> The verification of whether a namespace with the given name exists in the first place needs to be done by the calling routine.
    member this.AddOpenDirective(openedNS, alias) = OpenNamespaces.Add(openedNS, alias)

    /// Adds the given type declaration for the given type name to the dictionary of declared types.
    /// Adds the corresponding type constructor to the dictionary of declared callables.
    /// The given location is associated with both the type constructor and the type itself and accessible via the record properties Position and SymbolRange.
    /// -> Note that this routine will fail with the standard dictionary.Add error if either a type or a callable with that name already exists.
    member this.AddType (location: QsLocation) (tName, typeTuple, attributes, visibility, documentation) =
        let mutable anonItemId = 0
        let withoutRange sym = { Symbol = sym; Range = Null }

        let replaceAnonymous (itemName: QsSymbol, itemType) = // positional info for types in type constructors is removed upon resolution
            let anonItemName () =
                anonItemId <- anonItemId + 1
                sprintf "__Item%i__" anonItemId

            match itemName.Symbol with
            | MissingSymbol -> QsTupleItem(Symbol(anonItemName ()) |> withoutRange, itemType)
            | _ -> QsTupleItem(itemName.Symbol |> withoutRange, itemType) // no range info in auto-generated constructor

        let constructorSignature =
            let constructorArgument =
                let rec buildItem =
                    function
                    | QsTuple args -> (args |> Seq.map buildItem).ToImmutableArray() |> QsTuple
                    | QsTupleItem (n, t) -> replaceAnonymous (n, t)

                match typeTuple with
                | QsTupleItem (n, t) -> ImmutableArray.Create(replaceAnonymous (n, t)) |> QsTuple
                | QsTuple _ -> buildItem typeTuple

            let returnType = { Type = UserDefinedType(QualifiedSymbol(this.Name, tName) |> withoutRange); Range = Null }

            {
                TypeParameters = ImmutableArray.Empty
                Argument = constructorArgument
                ReturnType = returnType
                Characteristics = { Characteristics = EmptySet; Range = Null }
            }

        // There are a couple of reasons not just blindly attach all attributes associated with the type to the constructor:
        // For one, we would need to make sure that the range information for duplications is stripped such that e.g. rename commands are not executed multiple times.
        // We would furthermore have to adapt the entry point verification logic below, since type constructors are not valid entry points.
        let deprecationWithoutRedirect =
            {
                Id = { Symbol = Symbol BuiltIn.Deprecated.FullName.Name; Range = Null }
                Argument = { Expression = StringLiteral("", ImmutableArray.Empty); Range = Null }
                Position = location.Offset
                Comments = QsComments.Empty
            }

        let constructorAttr = // we will attach any attribute that likely indicates a deprecation to the type constructor as well
            let validDeprecatedQualification qual =
                String.IsNullOrWhiteSpace qual || qual = BuiltIn.Deprecated.FullName.Namespace

            if attributes |> Seq.exists (SymbolResolution.IndicatesDeprecation validDeprecatedQualification)
            then ImmutableArray.Create deprecationWithoutRedirect
            else ImmutableArray.Empty

        TypeDeclarations.Add(tName, (typeTuple, attributes, visibility, documentation) |> unresolved location)

        this.AddCallableDeclaration
            location
            (tName, (TypeConstructor, constructorSignature), constructorAttr, visibility, ImmutableArray.Empty)

        let bodyGen =
            {
                TypeArguments = Null
                Generator = QsSpecializationGeneratorKind.Intrinsic
                Range = Value location.Range
            }

        this.AddCallableSpecialization location QsBody (tName, bodyGen, ImmutableArray.Empty, visibility, ImmutableArray.Empty)

    /// Adds a callable declaration of the given kind (operation or function)
    /// with the given callable name and signature to the dictionary of declared callables.
    /// The given location is associated with the callable declaration and accessible via the record properties Position and SymbolRange.
    /// -> Note that this routine will fail with the standard dictionary.Add error if a callable with that name already exists.
    member this.AddCallableDeclaration location (cName, (kind, signature), attributes, modifiers, documentation) =
        CallableDeclarations.Add
            (cName, (kind, (signature, attributes, modifiers, documentation) |> unresolved location))

    /// Adds the callable specialization defined by the given kind and generator for the callable of the given name to the dictionary of declared specializations.
    /// The given location is associated with the given specialization and accessible via the record properties Position and HeaderRange.
    /// -> Note that the verification of whether the corresponding callable declaration exists within the namespace is up to the calling routine.
    /// *IMPORTANT*: both the verification of whether the length of the given array of type specialization
    /// matches the number of type parameters in the callable declaration, and whether a specialization that clashes with this one
    /// already exists is up to the calling routine!
    member this.AddCallableSpecialization location
                                          kind
                                          (cName, generator: QsSpecializationGenerator, attributes, visibility, documentation)
                                          =
        // NOTE: all types that are not specialized need to be resolved according to the file in which the callable is declared,
        // but all specialized types need to be resolved according to *this* file
        let spec = kind, (generator, attributes, visibility, documentation) |> unresolved location

        match CallableSpecializations.TryGetValue cName with
        | true, specs -> specs.Add spec // it is up to the namespace to verify the type specializations
        | false, _ -> CallableSpecializations.Add(cName, new List<_>([ spec ]))

    /// <summary>
    /// Deletes the *explicitly* defined specialization at the specified location for the callable with the given name.
    /// Does not delete specializations that have been inserted by the compiler, i.e. specializations whose location matches the callable declaration location.
    /// </summary>
    /// <returns>The number of removed specializations.</returns>
    /// <exception cref="SymbolNotFoundException">A callable with the given name was not found.</exception>
    member internal this.RemoveCallableSpecialization (location: QsLocation) cName =
        match CallableDeclarations.TryGetValue cName with
        | true, (_, decl) when decl.Position = location.Offset && decl.Range = location.Range -> 0
        | _ ->
            match CallableSpecializations.TryGetValue cName with
            | true, specs ->
                specs.RemoveAll(fun (_, res) -> location.Offset = res.Position && location.Range = res.Range)
            | false, _ -> SymbolNotFoundException "A callable with the given name was not found." |> raise

    /// <summary>
    /// Sets the resolution for the type with the given name to the given type, and replaces the resolved attributes with the given values.
    /// </summary>
    /// <exception cref="SymbolNotFoundException">A type with the given name was not found.</exception>
    member internal this.SetTypeResolution(tName, resolvedType, resAttributes) =
        match TypeDeclarations.TryGetValue tName with
        | true, qsType ->
            TypeDeclarations.[tName] <- { qsType with Resolved = resolvedType; ResolvedAttributes = resAttributes }
        | false, _ -> SymbolNotFoundException "A type with the given name was not found." |> raise

    /// <summary>
    /// Sets the resolution for the signature of the callable with the given name to the given signature,
    /// and replaces the resolved attributes with the given values.
    /// </summary>
    /// <exception cref="SymbolNotFoundException">A callable with the given name was not found.</exception>
    member internal this.SetCallableResolution(cName, resolvedSignature, resAttributes) =
        match CallableDeclarations.TryGetValue cName with
        | true, (kind, signature) ->
            let signature' = { signature with Resolved = resolvedSignature; ResolvedAttributes = resAttributes }
            CallableDeclarations.[cName] <- (kind, signature')
        | false, _ -> SymbolNotFoundException "A callable with the given name was not found." |> raise

    /// Applies the given functions computing the resolution of attributes and the generation directive
    /// to all defined specializations of the callable with the given name,
    /// and sets its resolution and resolved attributes to the computed values.
    /// Collects and returns an array of all diagnostics generated by computeResolution.
    /// Does nothing and returns an empty array if no specialization for a callable with the given name exists within this partial namespace.
    member internal this.SetSpecializationResolutions(cName, computeResolution, getResAttributes) =
        match CallableSpecializations.TryGetValue cName with
        | true, specs ->
            [| 0 .. specs.Count - 1 |]
            |> Array.collect (fun index ->
                let kind, spec = specs.[index]
                let resAttr, attErrs = getResAttributes this.Source spec
                let res, errs = computeResolution this.Source (kind, spec)
                specs.[index] <- (kind, { spec with Resolved = res; ResolvedAttributes = resAttr })
                errs |> Array.append attErrs)
        | false, _ -> [||]
