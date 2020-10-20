// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SymbolManagement

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Linq

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree

/// Represents a namespace and all of its declarations.
///
/// This class is *not* thread-safe.
///
/// Access modifiers are taken into consideration when resolving symbols. Some methods bypass this (e.g., when returning
/// a list of all declarations). Individual methods will mention if they adhere to symbol accessibility.
type Namespace private
    (name,
     parts : IEnumerable<KeyValuePair<NonNullable<string>,PartialNamespace>>,
     CallablesInReferences : ILookup<NonNullable<string>, CallableDeclarationHeader>,
     SpecializationsInReferences : ILookup<NonNullable<string>,
                                           SpecializationDeclarationHeader * SpecializationImplementation>,
     TypesInReferences : ILookup<NonNullable<string>, TypeDeclarationHeader>) =

    /// dictionary containing a PartialNamespaces for each source file which implements a part of this namespace -
    /// the key is the source file where each part of the namespace is defined
    let Parts = parts.ToDictionary((fun item -> item.Key), (fun item -> item.Value))
    let mutable TypesDefinedInAllSourcesCache = null
    let mutable CallablesDefinedInAllSourcesCache = null

    /// Returns true if the name is available for use in a new declaration.
    let isNameAvailable name =
        let isAvailableWith declarationsGetter accessibilityGetter sameAssembly =
            declarationsGetter name
            |> Seq.exists (fun name -> Namespace.IsDeclarationAccessible (sameAssembly, accessibilityGetter name))
            |> not

        let tryToList = function
            | true, value -> [value]
            | false, _ -> []

        isAvailableWith (fun name -> CallablesInReferences.[name]) (fun c -> c.Modifiers.Access) false &&
        isAvailableWith (fun name -> TypesInReferences.[name]) (fun t -> t.Modifiers.Access) false &&
        Parts.Values.All (fun partial ->
            isAvailableWith (partial.TryGetCallable >> tryToList) (fun c -> (snd c).Modifiers.Access) true &&
            isAvailableWith (partial.TryGetType >> tryToList) (fun t -> t.Modifiers.Access) true)

    /// Returns whether a declaration is accessible from the calling location, given whether the calling location is in
    /// the same assembly as the declaration, and the declaration's access modifier.
    static member IsDeclarationAccessible (sameAssembly, access) =
        match access with
        | DefaultAccess -> true
        | Internal -> sameAssembly

    /// name of the namespace
    member this.Name = name
    /// Immutable array with the names of all source files that contain (a part of) the namespace.
    /// Note that files contained in referenced assemblies that implement part of the namespace
    /// are *not* considered to be source files within the context of this Namespace instance!
    member internal this.Sources = Parts.Keys.ToImmutableHashSet()
    /// contains all types declared within one of the referenced assemblies as part of this namespace
    member this.TypesInReferencedAssemblies = TypesInReferences // access should be fine, since this is immutable
    /// contains all callables declared within one of the referenced assemblies as part of this namespace
    member this.CallablesInReferencedAssemblies = CallablesInReferences // access should be fine, since this is immutable
    /// contains all specializations declared within one of the referenced assemblies as part of this namespace
    member this.SpecializationsInReferencedAssemblies = SpecializationsInReferences // access should be fine, since this is immutable

    /// constructor taking the name of the namespace as well the name of the files in which (part of) it is declared in as arguments,
    /// as well as the information about all types and callables declared in referenced assemblies that belong to this namespace
    internal new (name, sources, callablesInRefs : IEnumerable<_>, specializationsInRefs : IEnumerable<_>, typesInRefs : IEnumerable<_>) =
        let initialSources = sources |> Seq.distinct |> Seq.map (fun source -> new KeyValuePair<_,_>(source, new PartialNamespace(name, source)))
        let typesInRefs = typesInRefs.Where (fun (header : TypeDeclarationHeader) -> header.QualifiedName.Namespace = name)
        let callablesInRefs = callablesInRefs.Where(fun (header : CallableDeclarationHeader) -> header.QualifiedName.Namespace = name)
        let specializationsInRefs = specializationsInRefs.Where(fun (header : SpecializationDeclarationHeader, _) -> header.Parent.Namespace = name)

        let discardConflicts getAccess (_, nameGroup) =
            // Only one externally accessible declaration with the same name is allowed.
            let isAccessible header = Namespace.IsDeclarationAccessible (false, getAccess header)
            if nameGroup |> Seq.filter isAccessible |> Seq.length > 1
            then nameGroup |> Seq.filter (not << isAccessible)
            else nameGroup

        let createLookup getName getAccess headers =
            headers
            |> Seq.groupBy getName
            |> Seq.map (discardConflicts getAccess)
            |> Seq.concat
            |> fun headers -> headers.ToLookup (Func<_, _> getName)

        let types = typesInRefs |> createLookup (fun t -> t.QualifiedName.Name) (fun t -> t.Modifiers.Access)
        let callables = callablesInRefs |> createLookup (fun c -> c.QualifiedName.Name) (fun c -> c.Modifiers.Access)
        let specializations =
            specializationsInRefs
                .Where(fun (s, _) -> callables.[s.Parent.Name].Any())
                .ToLookup(fun (s, _) -> s.Parent.Name)
        Namespace (name, initialSources, callables, specializations, types)

    /// returns true if the namespace currently contains no source files or referenced content
    member this.IsEmpty =
        not (this.Sources.Any() || this.TypesInReferencedAssemblies.Any() ||
            this.CallablesInReferencedAssemblies.Any() || this.SpecializationsInReferencedAssemblies.Any())

    /// returns a new Namespace that is an exact (deep) copy of this one
    /// -> any modification of the returned Namespace is not reflected in this one
    member this.Copy() =
        let partials = Parts |> Seq.map (fun part -> new KeyValuePair<_,_>(part.Key, part.Value.Copy()))
        new Namespace(name, partials, CallablesInReferences, SpecializationsInReferences, TypesInReferences)

    /// Returns a lookup that given the name of a source file,
    /// returns all documentation associated with this namespace defined in that file.
    member internal this.Documentation =
        Parts.Values.SelectMany(fun partial ->
            partial.Documentation |> Seq.map (fun doc -> partial.Source, doc)).ToLookup(fst, snd)

    /// Returns all namespaces that are open or aliased in the given source file for this namespace.
    /// The returned dictionary maps the names of the opened or aliased namespace to its alias if such an alias exists,
    /// and in particular also contains an entry for the namespace itself.
    /// Throws an ArgumentException if the given source file is not listed as source file for (part of) the namespace.
    member internal this.ImportedNamespaces source =
        match Parts.TryGetValue source with
        | true, partial -> partial.ImportedNamespaces
        | false, _ -> ArgumentException "given source file is not listed as a source file for this namespace" |> raise

    /// Returns a dictionary with all currently known namespace short names within the given source file and which namespace they represent.
    /// Throws an ArgumentException if the given source file is not listed as source file for (part of) the namespace.
    member internal this.NamespaceShortNames source =
        match Parts.TryGetValue source with
        | true, partial -> partial.NamespaceShortNames
        | false, _ -> ArgumentException "given source file is not listed as a source file for this namespace" |> raise

    /// If a type with the given name is defined in the specified source file or reference,
    /// checks if that type has been marked as attribute and returns its underlying type if it has.
    /// A type is considered to be marked as attribute if the list of defined attributes contains an attribute
    /// with name "Attribute" that is qualified by any of the given possible qualifications.
    /// If the list of possible qualifications contains an empty string, then the "Attribute" may be unqualified.
    /// Throws an ArgumentException if no such type exists in any of the references and the source file is not listed as source file of the namespace.
    /// Throws an InvalidOperationExeception if the corresponding type has not been resolved.
    member internal this.TryGetAttributeDeclaredIn source (attName, possibleQualifications : _ seq) =
        let marksAttribute (t : QsDeclarationAttribute) = t.TypeId |> function
            | Value id ->
                id.Namespace.Value = BuiltIn.Attribute.FullName.Namespace.Value &&
                id.Name.Value = BuiltIn.Attribute.FullName.Name.Value
            | Null -> false

        let missingResolutionException () = InvalidOperationException "cannot get unresolved attribute" |> raise

        let compareAttributeName (att : AttributeAnnotation) =
            match att.Id.Symbol with
            | Symbol sym when sym.Value = BuiltIn.Attribute.FullName.Name.Value && possibleQualifications.Contains "" ->
                true
            | QualifiedSymbol (ns, sym) when sym.Value = BuiltIn.Attribute.FullName.Name.Value &&
                                             possibleQualifications.Contains ns.Value ->
                true
            | _ -> false

        match Parts.TryGetValue source with
        | true, partial ->
            match partial.TryGetType attName with
            | true, resolution when Seq.exists compareAttributeName resolution.DefinedAttributes ->
                resolution.Resolved.ValueOrApply missingResolutionException |> fst |> Some
            | _ -> None
        | false, _ ->
            let referenceType =
                TypesInReferences.[attName]
                |> Seq.filter (fun qsType -> qsType.SourceFile = source)
                |> Seq.tryExactlyOne
            match referenceType with
            | Some qsType ->
                if Seq.exists marksAttribute qsType.Attributes
                then Some qsType.Type
                else None
            | None -> ArgumentException "given source file is not part of the namespace" |> raise

    /// Returns the type with the given name defined in the given source file within this namespace.
    /// Note that files contained in referenced assemblies are *not* considered to be source files for the namespace!
    /// Throws an ArgumentException if the source file is not listed as source file of the namespace,
    /// or if no type with the given name exists within this namespace in that source file.
    member internal this.TypeInSource source tName =
        match Parts.TryGetValue source with
        | true, partial -> partial.TryGetType tName |> function
            | true, typeDecl -> typeDecl
            | false, _ -> ArgumentException "no type with that name exist in the given source" |> raise
        | false, _ -> ArgumentException "given source file is not listed as source of the namespace" |> raise

    /// Returns all types defined in the given source file within this namespace.
    /// Note that files contained in referenced assemblies are *not* considered to be source files for the namespace!
    /// Throws an ArgumentException if the source file is not listed as source file of the namespace.
    member internal this.TypesDefinedInSource source =
        match Parts.TryGetValue source with
        | true, partial -> partial.DefinedTypes.ToImmutableArray()
        | false, _ -> ArgumentException "given source file is not listed as source of the namespace" |> raise

    /// Returns all types defined in a source file associated with this namespace.
    /// This excludes types that are defined in files contained in referenced assemblies.
    member internal this.TypesDefinedInAllSources () =
        if TypesDefinedInAllSourcesCache = null then
            let getInfos (partial : PartialNamespace) =
                partial.DefinedTypes |> Seq.map (fun (tName, decl) -> tName, (partial.Source, decl))
            TypesDefinedInAllSourcesCache <- (Parts.Values.SelectMany getInfos).ToImmutableDictionary(fst, snd)
        TypesDefinedInAllSourcesCache

    /// Returns the callable with the given name defined in the given source file within this namespace.
    /// Note that files contained in referenced assemblies are *not* considered to be source files for the namespace!
    /// Throws an ArgumentException if the source file is not listed as source file of the namespace,
    /// or if no callable with the given name exists within this namespace in that source file.
    member internal this.CallableInSource source cName =
        match Parts.TryGetValue source with
        | true, partial -> partial.TryGetCallable cName |> function
            | true, callable -> callable
            | false, _ -> ArgumentException "no callable with that name exist in the given source" |> raise
        | false, _ -> ArgumentException "given source file is not listed as source of the namespace" |> raise

    /// Returns all callables defined in the given source file within this namespace.
    /// Callables include operations, functions, and auto-generated type constructors for declared types.
    /// Note that files contained in referenced assemblies are *not* considered to be source files for the namespace!
    /// Throws an ArgumentException if the source file is not listed as source file of the namespace.
    member internal this.CallablesDefinedInSource source =
        match Parts.TryGetValue source with
        | true, partial -> partial.DefinedCallables.ToImmutableArray()
        | false, _ -> ArgumentException "given source file is not listed as source of the namespace" |> raise

    /// Returns all callables defined in a source file associated with this namespace.
    /// This excludes callables that are defined in files contained in referenced assemblies.
    /// Callables include operations, functions, and auto-generated type constructors for declared types.
    member internal this.CallablesDefinedInAllSources () =
        if CallablesDefinedInAllSourcesCache = null then
            let getInfos (partial : PartialNamespace) =
                partial.DefinedCallables |> Seq.map (fun (cName, decl) -> cName, (partial.Source, decl))
            CallablesDefinedInAllSourcesCache <- (Parts.Values.SelectMany getInfos).ToImmutableDictionary(fst, snd)
        CallablesDefinedInAllSourcesCache

    /// Returns all specializations for the callable with the given name defined in a source file associated with this namespace,
    /// This excludes specializations that are defined in files contained in referenced assemblies.
    /// Throws an ArgumentException if no callable with the given name is defined in this namespace.
    member internal this.SpecializationsDefinedInAllSources cName =
        let getSpecializationInPartial (partial : PartialNamespace) =
            partial.GetSpecializations cName
            |> Seq.map (fun (kind, decl) -> kind, (partial.Source, decl))

        if this.TryFindCallable cName |> ResolutionResult.Exists
        then (Parts.Values.SelectMany getSpecializationInPartial).ToImmutableArray()
        else ArgumentException "no callable with the given name exist within the namespace" |> raise


    /// Returns a resolution result for the type with the given name containing the name of the source file or
    /// referenced assembly in which it is declared, a string indicating the redirection if it has been deprecated, and
    /// its access modifier. Resolution is based on accessibility to source files in this compilation unit.
    ///
    /// Whether the type has been deprecated is determined by checking the associated attributes for an attribute with
    /// the corresponding name. Note that if the type is declared in a source files, the *unresolved* attributes will be
    /// checked. In that case checkDeprecation is used to validate the namespace qualification of the attribute. If
    /// checkDeprecation is not specified, it is assumed that no qualification is needed in the relevant namespace and
    /// source file.
    member this.TryFindType (tName, ?checkDeprecation : (string -> bool)) =
        let checkDeprecation =
            defaultArg checkDeprecation
                       (fun qual -> String.IsNullOrWhiteSpace qual || qual = BuiltIn.Deprecated.FullName.Namespace.Value)

        let resolveReferenceType (typeHeader : TypeDeclarationHeader) =
            if Namespace.IsDeclarationAccessible (false, typeHeader.Modifiers.Access)
            then Found (typeHeader.SourceFile,
                        SymbolResolution.TryFindRedirect typeHeader.Attributes,
                        typeHeader.Modifiers.Access)
            else Inaccessible

        let findInPartial (partial : PartialNamespace) =
            match partial.TryGetType tName with
            | true, qsType ->
                if Namespace.IsDeclarationAccessible (true, qsType.Modifiers.Access)
                then Found (partial.Source,
                            SymbolResolution.TryFindRedirectInUnresolved checkDeprecation qsType.DefinedAttributes,
                            qsType.Modifiers.Access)
                else Inaccessible
            | false, _ -> NotFound

        seq { yield Seq.map resolveReferenceType TypesInReferences.[tName] |> ResolutionResult.AtMostOne
              yield Seq.map findInPartial Parts.Values |> ResolutionResult.AtMostOne }
        |> ResolutionResult.TryFirstBest

    /// Returns a resolution result for the callable with the given name containing the name of the source file or
    /// referenced assembly in which it is declared, and a string indicating the redirection if it has been deprecated.
    /// Resolution is based on accessibility to source files in this compilation unit.
    ///
    /// If the given callable corresponds to the (auto-generated) type constructor for a user defined type, returns the
    /// file in which that type is declared as the source.
    ///
    /// Whether the callable has been deprecated is determined by checking the associated attributes for an attribute
    /// with the corresponding name. Note that if the type is declared in a source files, the *unresolved* attributes
    /// will be checked. In that case checkDeprecation is used to validate the namespace qualification of the attribute.
    /// If checkDeprecation is not specified, it is assumed that no qualification is needed in the relevant namespace
    /// and source file.
    member this.TryFindCallable (cName, ?checkDeprecation : (string -> bool)) =
        let checkDeprecation =
            defaultArg checkDeprecation
                       (fun qual -> String.IsNullOrWhiteSpace qual || qual = BuiltIn.Deprecated.FullName.Namespace.Value)

        let resolveReferenceCallable (callable : CallableDeclarationHeader) =
            if Namespace.IsDeclarationAccessible (false, callable.Modifiers.Access)
            then Found (callable.SourceFile, SymbolResolution.TryFindRedirect callable.Attributes)
            else Inaccessible

        let findInPartial (partial : PartialNamespace) =
            match partial.TryGetCallable cName with
            | true, (_, callable) ->
                if Namespace.IsDeclarationAccessible (true, callable.Modifiers.Access)
                then Found (partial.Source,
                            SymbolResolution.TryFindRedirectInUnresolved checkDeprecation callable.DefinedAttributes)
                else Inaccessible
            | false, _ -> NotFound

        seq { yield Seq.map resolveReferenceCallable CallablesInReferences.[cName] |> ResolutionResult.AtMostOne
              yield Seq.map findInPartial Parts.Values |> ResolutionResult.AtMostOne }
        |> ResolutionResult.TryFirstBest

    /// Sets the resolution for the type with the given name in the given source file to the given type,
    /// and replaces the resolved attributes with the given values.
    /// Fails with the standard key does not exist error if no such source file exists or no type with that name exists in that file.
    member internal this.SetTypeResolution source (tName, resolution, resAttributes) =
        TypesDefinedInAllSourcesCache <- null
        CallablesDefinedInAllSourcesCache <- null
        Parts.[source].SetTypeResolution (tName, resolution, resAttributes)

    /// Sets the resolution for the signature of the callable with the given name in the given source file
    /// to the given signature, and replaces the resolved attributes with the given values.
    /// Fails with the standard key does not exist error if no such source file exists or no callable with that name exists in that file.
    member internal this.SetCallableResolution source (cName, resolution, resAttributes) =
        CallablesDefinedInAllSourcesCache <- null
        Parts.[source].SetCallableResolution (cName, resolution, resAttributes)

    /// Applies the given functions computing the resolution of attributes and the generation directive
    /// to all defined specializations of the callable with the given name,
    /// and sets its resolution and resolved attributes to the computed values.
    /// Returns a list with the name of the source file and each generated diagnostic.
    /// Fails with the standard key does not exist error if no callable with that name exists.
    member internal this.SetSpecializationResolutions (cName, computeResolution, getResAttributes) =
        CallablesDefinedInAllSourcesCache <- null
        let setResolutions (partial : PartialNamespace) =
            partial.SetSpecializationResolutions (cName, computeResolution, getResAttributes)
            |> Array.map (fun err -> partial.Source, err)
        Parts.Values |> Seq.map setResolutions |> Seq.toList

    /// If the given source is not currently listed as source file for (part of) the namespace,
    /// adds the given file name to the list of sources and returns true.
    /// Returns false otherwise.
    member internal this.TryAddSource source =
        if not (Parts.ContainsKey source) then
            TypesDefinedInAllSourcesCache <- null
            CallablesDefinedInAllSourcesCache <- null
            Parts.Add(source, new PartialNamespace(this.Name, source))
            true
        else false

    /// If the given source is currently listed as source file for (part of) the namespace,
    /// removes it from that list (and all declarations along with it) and returns true.
    /// Returns false otherwise.
    member internal this.TryRemoveSource source =
        if Parts.Remove source then
            TypesDefinedInAllSourcesCache <- null
            CallablesDefinedInAllSourcesCache <- null
            true
        else false

    /// Adds the given lines of documentation to the list of documenting sections
    /// associated with this namespace within the given source file.
    /// Throws an ArgumentException if the given source file is not listed as a source for (part of) the namespace.
    member this.AddDocumentation source doc =
        match Parts.TryGetValue source with
        | true, partial -> partial.AddDocumentation doc
        | false, _ -> ArgumentException "given source is not listed as a source of (parts of) the namespace" |> raise

    /// Adds the given namespace name to the list of opened namespaces for the part of the namespace defined in the given source file.
    /// Generates suitable diagnostics at the given range if the given namespace has already been opened and/or opened under a different alias,
    /// or if the given alias is already in use for a different namespace.
    /// Throws an ArgumentException if the given source file is not listed as a source for (part of) the namespace.
    member internal this.TryAddOpenDirective source (openedNS, nsRange) (alias, aliasRange) =
        let alias = if String.IsNullOrWhiteSpace alias then null else alias.Trim()
        let aliasIsSameAs str = (str = null && alias = null) || (str <> null && alias <> null && str = alias)
        match Parts.TryGetValue source with
        | true, partial ->
            let imported = partial.ImportedNamespaces
            match imported.TryGetValue openedNS with
            | true, existing when aliasIsSameAs existing && existing = null -> [| nsRange |> QsCompilerDiagnostic.Warning (WarningCode.NamespaceAleadyOpen, []) |]
            | true, existing when aliasIsSameAs existing -> [| nsRange |> QsCompilerDiagnostic.Warning (WarningCode.NamespaceAliasIsAlreadyDefined, []) |]
            | true, existing when existing <> null -> [| nsRange |> QsCompilerDiagnostic.Error (ErrorCode.AliasForNamespaceAlreadyExists, [existing]) |]
            | true, _ -> [| nsRange |> QsCompilerDiagnostic.Error (ErrorCode.AliasForOpenedNamespace, []) |]
            | false, _ when alias <> null && imported.ContainsValue alias -> [| aliasRange |> QsCompilerDiagnostic.Error (ErrorCode.InvalidNamespaceAliasName, [alias]) |]
            | false, _ ->
                TypesDefinedInAllSourcesCache <- null
                CallablesDefinedInAllSourcesCache <- null
                partial.AddOpenDirective(openedNS, alias); [||]
        | false, _ -> ArgumentException "given source is not listed as a source of (parts of) the namespace" |> raise

    /// If no type with the given name exists in this namespace, adds the given type declaration
    /// as well as the corresponding constructor declaration to the given source, and returns an empty array.
    /// The given location is associated with both the type constructor and the type itself and accessible via the record properties Position and SymbolRange.
    /// If a type or callable with that name already exists, returns an array of suitable diagnostics.
    /// Throws an ArgumentException if the given source file is not listed as a source for (part of) the namespace.
    member this.TryAddType (source, location) ((tName, tRange), typeTuple, attributes, modifiers, documentation) : QsCompilerDiagnostic[] =
        match Parts.TryGetValue source with
        | true, partial when isNameAvailable tName ->
            TypesDefinedInAllSourcesCache <- null
            CallablesDefinedInAllSourcesCache <- null
            partial.AddType location (tName, typeTuple, attributes, modifiers, documentation); [||]
        | true, _ ->
            match this.TryFindType tName with
            | Found _
            | Ambiguous _ ->
                [| tRange |> QsCompilerDiagnostic.Error (ErrorCode.TypeRedefinition, [tName.Value]) |]
            | _ ->
                [| tRange |> QsCompilerDiagnostic.Error (ErrorCode.TypeConstructorOverlapWithCallable, [tName.Value]) |]
        | false, _ -> ArgumentException "given source is not listed as a source of (parts of) the namespace" |> raise

    /// If no callable (function, operation, or type constructor) with the given name exists in this namespace,
    /// adds a declaration for the callable of the given kind (operation or function) with the given name and signature
    /// to the given source, and returns an empty array.
    /// The given location is associated with the callable declaration and accessible via the record properties Position and SymbolRange.
    /// If a callable with that name already exists, returns an array of suitable diagnostics.
    /// Throws an ArgumentException if the given source file is not listed as a source for (part of) the namespace.
    member this.TryAddCallableDeclaration (source, location) ((cName, cRange), (kind, signature), attributes, modifiers, documentation) =
        match Parts.TryGetValue source with
        | true, partial when isNameAvailable cName ->
            CallablesDefinedInAllSourcesCache <- null
            partial.AddCallableDeclaration location (cName, (kind, signature), attributes, modifiers, documentation); [||]
        | true, _ ->
            match this.TryFindType cName with
            | Found _
            | Ambiguous _ ->
                [| cRange |> QsCompilerDiagnostic.Error (ErrorCode.CallableOverlapWithTypeConstructor, [cName.Value]) |]
            | _ ->
                [| cRange |> QsCompilerDiagnostic.Error (ErrorCode.CallableRedefinition, [cName.Value]) |]
        | false, _ -> ArgumentException "given source is not listed as a source of (parts of) the namespace" |> raise

    /// If a declaration for a callable of the given name exists within this namespace,
    /// verifies that no specialization of the given kind that clashes with the give specialization already exists,
    /// and adds the specialization defined by the given generator for the given kind to the dictionary of specializations in the given source.
    /// The given location is associated with the given specialization and accessible via the record properties Position and HeaderRange.
    /// Returns an array with suitable diagnostics if a clashing specialization already exists, and/or
    /// if the length of the type arguments in the given generator does not match the number of type parameters of the callable declaration.
    /// If no declaration for the given callable name exists within this namespace, returns an array with suitable diagnostics.
    /// Throws an ArgumentException if the given source file is not listed as a source for (part of) the namespace.
    /// IMPORTANT: The verification of whether the given specialization kind (body, adjoint, controlled, or controlled adjoint) may exist
    /// for the given callable is up to the calling routine.
    member this.TryAddCallableSpecialization kind (source, location : QsLocation) ((cName, cRange), generator : QsSpecializationGenerator, attributes, documentation) =
        let getRelevantDeclInfo (declSource : NonNullable<string>) =
            let unitOrInvalid fct = function
                | Item item -> match fct item with
                               | UnitType
                               | InvalidType -> true
                               | _ -> false
                | _ -> false

            // Check if the declaration's source file is local first, then look in references.
            match Parts.TryGetValue declSource with
            | true, partial ->
                let _, cDecl = partial.GetCallable cName
                let unitReturn = cDecl.Defined.ReturnType |> unitOrInvalid (fun (t : QsType) -> t.Type)
                unitReturn, cDecl.Defined.TypeParameters.Length
            | false, _ ->
                let cDecl = CallablesInReferences.[cName] |> Seq.filter (fun c -> c.SourceFile = source) |> Seq.exactlyOne
                let unitReturn = cDecl.Signature.ReturnType |> unitOrInvalid (fun (t : ResolvedType) -> t.Resolution)
                unitReturn, cDecl.Signature.TypeParameters.Length

        match Parts.TryGetValue source with
        | true, partial ->
            match this.TryFindCallable cName with
            | Found (declSource, _) ->
                let AddAndClearCache () =
                    CallablesDefinedInAllSourcesCache <- null
                    partial.AddCallableSpecialization location kind (cName, generator, attributes, documentation)
                // verify that the given specializations are indeed compatible with the defined type parameters
                let qFunctorSupport, nrTypeParams = getRelevantDeclInfo declSource
                let givenNrTypeParams = generator.TypeArguments |> function | Value args -> Some args.Length | Null -> None
                if givenNrTypeParams.IsSome && givenNrTypeParams.Value <> nrTypeParams then
                    [| location.Range |> QsCompilerDiagnostic.Error (ErrorCode.TypeSpecializationMismatch, [nrTypeParams.ToString()]) |]
                // verify if a unit return value is required for the given specialization kind
                elif not qFunctorSupport then kind |> function
                    | QsBody -> AddAndClearCache(); [||]
                    | QsAdjoint -> [| location.Range |> QsCompilerDiagnostic.Error (ErrorCode.RequiredUnitReturnForAdjoint, []) |]
                    | QsControlled -> [| location.Range |> QsCompilerDiagnostic.Error (ErrorCode.RequiredUnitReturnForControlled, []) |]
                    | QsControlledAdjoint -> [| location.Range |> QsCompilerDiagnostic.Error (ErrorCode.RequiredUnitReturnForControlledAdjoint, []) |]
                else AddAndClearCache(); [||]
            | _ -> [| cRange |> QsCompilerDiagnostic.Error (ErrorCode.SpecializationForUnknownCallable, [cName.Value]) |]
        | false, _ -> ArgumentException "given source is not listed as a source of (parts of) the namespace" |> raise

    /// Adds an auto-generated specialization of the given kind to the callable with the given name and declaration in the specified source file.
    /// Sets the location to the same location as the callable declaration, with the range set to the message range if the given message range is not Null.
    /// Return the diagnostics generated upon adding the specialization.
    /// Throws an ArgumentException if the given source file is not listed as a source for (part of) the namespace.
    member internal this.InsertSpecialization (kind, typeArgs) (parentName : NonNullable<string>, source) (declLocation : QsLocation, msgRange : QsNullable<Range>) =
        let location = {Offset = declLocation.Offset; Range = msgRange.ValueOr declLocation.Range}
        let generator = {TypeArguments = typeArgs; Generator = AutoGenerated; Range = msgRange}
        let doc = ImmutableArray.Create(sprintf "automatically generated %A specialization for %s.%s" kind this.Name.Value parentName.Value)
        this.TryAddCallableSpecialization kind (source, location) ((parentName, declLocation.Range), generator, ImmutableArray.Empty, doc)

    /// Deletes the specialization(s) defined at the specified location and source file for the callable with the given name.
    /// Returns the number of removed specializations.
    /// Throws an ArgumentException if the given source file is not listed as a source for (part of) the namespace.
    /// Throws the standard key does not exist exception if no callable with that name exists.
    member internal this.RemoveSpecialization (source, location) cName =
        match Parts.TryGetValue source with
        | true, partial -> partial.RemoveCallableSpecialization location cName
        | false, _ -> ArgumentException "given source is not listed as a source of (parts of) the namespace" |> raise
