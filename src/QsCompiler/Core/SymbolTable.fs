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
open Microsoft.Quantum.QsCompiler.SymbolResolution
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxGenerator
open Microsoft.Quantum.QsCompiler.SyntaxTokens 
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Newtonsoft.Json


/// Represents the outcome of resolving a symbol in the symbol table.
type ResolutionResult<'T> =
    /// The symbol resolved successfully.
    | Found of 'T
    /// An unqualified symbol is ambiguous, and it is possible to resolve it to more than one namespace. Includes the
    /// list of possible namespaces.
    | Ambiguous of NonNullable<string> seq
    /// The symbol resolved to a declaration which is not accessible from the location referencing it.
    | Inaccessible
    /// No declaration with that name was found.
    | NotFound
    

/// Note that this class is *not* threadsafe!
type private PartialNamespace private
    (name : NonNullable<string>,
     source : NonNullable<string>,
     documentation : IEnumerable<ImmutableArray<string>>,
     openNS : IEnumerable<KeyValuePair<NonNullable<string>, string>>, 
     typeDecl : IEnumerable<KeyValuePair<NonNullable<string>, Resolution<QsTuple<QsSymbol * QsType>, ResolvedType * QsTuple<_>>>>,
     callableDecl : IEnumerable<KeyValuePair<NonNullable<string>, QsCallableKind * Resolution<CallableSignature, ResolvedSignature*QsTuple<_>>>>,
     specializations : IEnumerable<KeyValuePair<NonNullable<string>, List<QsSpecializationKind * Resolution<QsSpecializationGenerator, ResolvedGenerator>>>>) = 

    let keySelector (item : KeyValuePair<'k,'v>) = item.Key
    let valueSelector (item : KeyValuePair<'k,'v>) = item.Value
    let unresolved (location : QsLocation) (definition, attributes, modifiers, doc) = {
        Defined = definition
        DefinedAttributes = attributes
        Resolved = Null
        ResolvedAttributes = ImmutableArray.Empty
        Modifiers = modifiers
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
    /// includes functions, operations, *and* (auto-gnerated) type constructors 
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
    internal new (name, source) = new PartialNamespace(name, source, [], [new KeyValuePair<_,_>(name, null)], [], [], []) 

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
        shortNames.ToImmutableDictionary((fun kv -> NonNullable<string>.New kv.Value), (fun kv -> kv.Key))

    /// Gets the type with the given name from the dictionary of declared types.
    /// Fails with the standard key does not exist error if no such declaration exists.
    member internal this.GetType tName = TypeDeclarations.[tName]
    member internal this.ContainsType = TypeDeclarations.ContainsKey 
    member internal this.TryGetType = TypeDeclarations.TryGetValue

    /// Gets the callable with the given name from the dictionary of declared callable.
    /// Fails with the standard key does not exist error if no such declaration exists.
    member internal this.GetCallable cName = CallableDeclarations.[cName]
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
    member this.AddDocumenation (doc : IEnumerable<_>) = 
        AssociatedDocumentation.Add(doc.ToImmutableArray())

    /// If the given namespace name is not already listened as imported, adds the given namespace name to the list of open namespaces.
    /// -> Note that this routine will fail with the standard dictionary.Add error if an open directive for the given namespace name already exists. 
    /// -> The verification of whether a namespace with the given name exists in the first place needs to be done by the calling routine. 
    member this.AddOpenDirective (openedNS, alias) = 
        OpenNamespaces.Add(openedNS, alias) 

    /// Adds the given type declaration for the given type name to the dictionary of declared types.
    /// Adds the corresponding type constructor to the dictionary of declared callables. 
    /// The given location is associated with both the type constructur and the type itself and accessible via the record properties Position and SymbolRange. 
    /// -> Note that this routine will fail with the standard dictionary.Add error if either a type or a callable with that name already exists. 
    member this.AddType (location : QsLocation) (tName, typeTuple, attributes, modifiers, documentation) = 
        let mutable anonItemId = 0
        let withoutRange sym = {Symbol = sym; Range = Null}
        let replaceAnonymous (itemName : QsSymbol, itemType) = // positional info for types in type constructors is removed upon resolution 
            let anonItemName () = 
                anonItemId <- anonItemId + 1
                sprintf "__Item%i__" anonItemId |> NonNullable<string>.New
            match itemName.Symbol with
            | MissingSymbol -> QsTupleItem (Symbol (anonItemName()) |> withoutRange, itemType)
            | _ -> QsTupleItem (itemName.Symbol |> withoutRange, itemType) // no range info in auto-generated constructor

        let constructorSignature = 
            let constructorArgument = 
                let rec buildItem = function 
                    | QsTuple args -> (args |> Seq.map buildItem).ToImmutableArray() |> QsTuple
                    | QsTupleItem (n, t) -> replaceAnonymous (n, t)
                match typeTuple with 
                | QsTupleItem (n, t) -> ImmutableArray.Create (replaceAnonymous (n, t)) |> QsTuple
                | QsTuple _ -> buildItem typeTuple
            let returnType = {Type = UserDefinedType (QualifiedSymbol (this.Name, tName) |> withoutRange); Range = Null}
            {TypeParameters = ImmutableArray.Empty; Argument = constructorArgument; ReturnType = returnType; Characteristics = {Characteristics = EmptySet; Range = Null}}

        // There are a couple of reasons not just blindly attach all attributes associated with the type to the constructor:
        // For one, we would need to make sure that the range information for duplications is stripped such that e.g. rename commands are not executed multiple times. 
        // We would furthermore have to adapt the entry point verification logic below, since type constructors are not valid entry points.
        let deprecationWithoutRedirect = {
            Id = {Symbol = Symbol BuiltIn.Deprecated.Name; Range = Null}
            Argument = {Expression = StringLiteral (NonNullable<string>.New "", ImmutableArray.Empty); Range = Null}
            Position = location.Offset
            Comments = QsComments.Empty} 
        let constructorAttr = // we will attach any attribute that likely indicates a deprecation to the type constructor as well
            let validDeprecatedQualification qual = String.IsNullOrWhiteSpace qual || qual = BuiltIn.Deprecated.Namespace.Value
            if attributes |> Seq.exists (SymbolResolution.IndicatesDeprecation validDeprecatedQualification) then ImmutableArray.Create deprecationWithoutRedirect 
            else ImmutableArray.Empty

        TypeDeclarations.Add(tName, (typeTuple, attributes, modifiers, documentation) |> unresolved location)
        this.AddCallableDeclaration location (tName, (TypeConstructor, constructorSignature), constructorAttr, modifiers, ImmutableArray.Empty) 
        let bodyGen = {TypeArguments = Null; Generator = QsSpecializationGeneratorKind.Intrinsic; Range = Value location.Range}
        this.AddCallableSpecialization location QsBody (tName, bodyGen, ImmutableArray.Empty, ImmutableArray.Empty) 

    /// Adds a callable declaration of the given kind (operation or function) 
    /// with the given callable name and signature to the dictionary of declared callables.
    /// The given location is associated with the callable declaration and accessible via the record properties Position and SymbolRange. 
    /// -> Note that this routine will fail with the standard dictionary.Add error if a callable with that name already exists. 
    member this.AddCallableDeclaration location (cName, (kind, signature), attributes, modifiers, documentation) = 
        CallableDeclarations.Add(cName, (kind, (signature, attributes, modifiers, documentation) |> unresolved location))

    /// Adds the callable specialization defined by the given kind and generator for the callable of the given name to the dictionary of declared specializations. 
    /// The given location is associated with the given specialization and accessible via the record properties Position and HeaderRange. 
    /// -> Note that the verification of whether the corresponding callable declaration exists within the namespace is up to the calling routine. 
    /// *IMPORTANT*: both the verification of whether the length of the given array of type specialization 
    /// matches the number of type parameters in the callable declaration, and whether a specialization that clashes with this one
    /// already exists is up to the calling routine!
    member this.AddCallableSpecialization location kind (cName, generator : QsSpecializationGenerator, attributes, documentation) = 
    // NOTE: all types that are not specialized need to be resolved according to the file in which the callable is declared, 
    // but all specialized types need to be resolved according to *this* file  
        let spec = kind, (generator, attributes, {Access = DefaultAccess}, documentation) |> unresolved location
        match CallableSpecializations.TryGetValue cName with
        | true, specs -> specs.Add spec // it is up to the namespace to verify the type specializations
        | false, _ -> CallableSpecializations.Add(cName, new List<_>([spec]))

    /// Deletes the *explicitly* defined specialization at the specified location for the callable with the given name.
    /// Does not delete specializations that have been inserted by the compiler, i.e. specializations whose location matches the callable declaration location.
    /// Returns the number of removed specializations. 
    /// Throws the standard key does not exist exception if no specialization for the callable with that name exists.
    member internal this.RemoveCallableSpecialization (location : QsLocation) cName = 
        match CallableDeclarations.TryGetValue cName with 
        | true, (_, decl) when decl.Position = location.Offset && decl.Range = location.Range -> 0 
        | _ -> CallableSpecializations.[cName].RemoveAll (fun (_, res) -> location.Offset = res.Position && location.Range = res.Range)

    /// Sets the resolution for the type with the given name to the given type, and replaces the resolved attributes with the given values.
    /// Throws the standard key does not exist exception if no type with that name exists.
    member internal this.SetTypeResolution (tName, resolvedType, resAttributes) = 
        let qsType = TypeDeclarations.[tName]
        TypeDeclarations.[tName] <- {qsType with Resolved = resolvedType; ResolvedAttributes = resAttributes}

    /// Sets the resolution for the signature of the callable with the given name to the given signature,
    /// and replaces the resolved attributes with the given values.
    /// Throws the standard key does not exist exception if no callable with that name exists.
    member internal this.SetCallableResolution (cName, resolvedSignature, resAttributes) = 
        let (kind, signature) = CallableDeclarations.[cName]
        CallableDeclarations.[cName] <- (kind, {signature with Resolved = resolvedSignature; ResolvedAttributes = resAttributes})

    /// Applies the given functions computing the resolution of attributes and the generation directive 
    /// to all defined specializations of the callable with the given name, 
    /// and sets its resolution and resolved attributes to the computed values. 
    /// Collects and returns an array of all diagnostics generated by computeResolution. 
    /// Does nothing and returns an empty array if no specialization for a callable with the given name exists within this partial namespace.
    member internal this.SetSpecializationResolutions (cName, computeResolution, getResAttributes) = 
        match CallableSpecializations.TryGetValue cName with 
        | true, specs -> 
            [|0 .. specs.Count - 1|] |> Array.collect (fun index ->
                let kind, spec = specs.[index]
                let resAttr, attErrs = getResAttributes this.Source spec
                let res, errs = computeResolution this.Source (kind, spec)
                specs.[index] <- (kind, {spec with Resolved = res; ResolvedAttributes = resAttr})
                errs |> Array.append attErrs)
        | false, _ -> [||]


/// Note that this class is *not* threadsafe!
and Namespace private
    (name, parts : IEnumerable<KeyValuePair<NonNullable<string>,PartialNamespace>>,
     CallablesInReferences : ImmutableDictionary<NonNullable<string>, CallableDeclarationHeader>,
     SpecializationsInReferences : ILookup<NonNullable<string>, SpecializationDeclarationHeader * SpecializationImplementation>,
     TypesInReferences : ImmutableDictionary<NonNullable<string>, TypeDeclarationHeader>) =

    /// dictionary containing a PartialNamespaces for each source file which implements a part of this namespace -
    /// the key is the source file where each part of the namespace is defined
    let Parts = parts.ToDictionary((fun item -> item.Key), (fun item -> item.Value))
    let mutable TypesDefinedInAllSourcesCache = null
    let mutable CallablesDefinedInAllSourcesCache = null

    /// Given a non-nullable string, returns true if either a callable or a type with that name already exists
    /// either in a source file, or in a referenced assembly.
    let IsDefined arg = 
        CallablesInReferences.ContainsKey arg || TypesInReferences.ContainsKey arg || 
        Parts.Values.Any (fun partial -> partial.ContainsCallable arg || partial.ContainsType arg)

    /// Given a selector, determines the source files for which the given selector returns a Value.
    /// Returns that Value if exactly one such source file exists, or Null if no such file exists.
    /// Note that files contained in referenced assemblies are *not* considered to be source files for the namespace!
    /// Throws an ArgumentException if the selector returns a Value for more than one source file. 
    let FromSingleSource (selector : PartialNamespace -> _ Option) = 
        let sources = Parts.Values |> Seq.choose selector
        if sources.Count() > 1 then ArgumentException "given selector selects more than one partial namespace" |> raise
        if sources.Any() then Value (sources.Single()) else Null


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

        // ignore ambiguous/clashing references
        let FilterUnique (g : IGrouping<_,_>) = 
            if g.Count() > 1 then None // TODO: give warning??
            else g.Single() |> Some
        let typesInRefs = typesInRefs.GroupBy(fun t -> t.QualifiedName.Name) |> Seq.choose FilterUnique
        let callablesInRefs = callablesInRefs.GroupBy(fun c -> c.QualifiedName.Name) |> Seq.choose FilterUnique

        let types = typesInRefs.ToImmutableDictionary(fun t -> t.QualifiedName.Name)
        let callables = callablesInRefs.ToImmutableDictionary(fun c -> c.QualifiedName.Name)
        let specializations = specializationsInRefs.Where(fun (s, _) -> callables.ContainsKey s.Parent.Name).ToLookup(fun (s, _) -> s.Parent.Name)
        new Namespace(name, initialSources, callables, specializations, types)


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
            | Value id -> id.Namespace.Value = BuiltIn.Attribute.Namespace.Value && id.Name.Value = BuiltIn.Attribute.Name.Value
            | Null -> false
        let missingResolutionException () = InvalidOperationException "cannot get unresolved attribute" |> raise 
        let compareAttributeName (att : AttributeAnnotation) = att.Id.Symbol |> function 
            | Symbol sym when sym.Value = BuiltIn.Attribute.Name.Value && possibleQualifications.Contains "" -> true
            | QualifiedSymbol (ns, sym) when sym.Value = BuiltIn.Attribute.Name.Value && possibleQualifications.Contains ns.Value -> true
            | _ -> false
        match TypesInReferences.TryGetValue attName with 
        | true, tDecl -> if tDecl.Attributes |> Seq.exists marksAttribute then Some tDecl.Type else None
        | false, _ -> Parts.TryGetValue source |> function // ok only because/if we have covered that the type is not in a reference!
            | false, _ -> ArgumentException "given source file is not listed as source of the namespace" |> raise
            | true, partialNS -> partialNS.TryGetType attName |> function 
                | true, resolution when resolution.DefinedAttributes |> Seq.exists compareAttributeName -> 
                    resolution.Resolved.ValueOrApply missingResolutionException |> fst |> Some
                | _ -> None

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
    /// Callables include operations, functions, and auto-genrated type constructors for declared types.
    /// Note that files contained in referenced assemblies are *not* considered to be source files for the namespace!
    /// Throws an ArgumentException if the source file is not listed as source file of the namespace.
    member internal this.CallablesDefinedInSource source = 
        match Parts.TryGetValue source with 
        | true, partial -> partial.DefinedCallables.ToImmutableArray()
        | false, _ -> ArgumentException "given source file is not listed as source of the namespace" |> raise

    /// Returns all callables defined in a source file associated with this namespace.
    /// This excludes callables that are defined in files contained in referenced assemblies.
    /// Callables include operations, functions, and auto-genrated type constructors for declared types.
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
        match this.TryFindCallable cName with
        | Value _ -> (Parts.Values.SelectMany (fun partial -> 
            partial.GetSpecializations cName |> Seq.map (fun (kind, decl) -> kind, (partial.Source, decl)))).ToImmutableArray()        
        | Null -> ArgumentException "no callable with the given name exist within the namespace" |> raise

    /// If this namespace contains a declaration for the given type name, returns a Value with:
    ///
    ///     (1) the name of the source file or the name of the file within a referenced assembly in which it is
    ///         declared;
    ///     (2) a string option indicating the redirection for the type if it has been deprecated;
    ///     (3) true if the declaration is declared in the assembly currently being compiled, or false if it is declared
    ///         in a referenced assembly;
    ///     (4) the modifiers for the declaration.
    ///
    /// Returns Null otherwise.
    ///
    /// Whether the type has been deprecated is determined by checking the associated attributes for an attribute with the corresponding name. 
    /// Note that if the type is declared in a source files, the *unresolved* attributes will be checked.
    /// In that case checkDeprecation is used to validate the namespace qualification of the attribute. 
    /// If checkDeprecation is not specified, it is assumed that no qualification is needed in the relevant namespace and source file.
    member this.TryFindType (tName, ?checkDeprecation : (string -> bool)) = 
        let checkDeprecation = defaultArg checkDeprecation (fun qual -> String.IsNullOrWhiteSpace qual || qual = BuiltIn.Deprecated.Namespace.Value)
        match TypesInReferences.TryGetValue tName with 
        | true, tDecl -> Value (tDecl.SourceFile, tDecl.Attributes |> SymbolResolution.TryFindRedirect, false, tDecl.Modifiers)
        | false, _ -> FromSingleSource (fun partialNS -> partialNS.TryGetType tName |> function 
            | true, tDecl -> Some (partialNS.Source, tDecl.DefinedAttributes |> SymbolResolution.TryFindRedirectInUnresolved checkDeprecation, true, tDecl.Modifiers)
            | false, _ -> None)

    /// If this namespace contains a declaration for the given callable name, returns a Value with:
    ///
    ///     (1) the name of the source file or the name of the file within a referenced assembly in which it is
    ///         declared;
    ///     (2) a string option indicating the redirection for the type if it has been deprecated;
    ///     (3) true if the declaration is declared in the assembly currently being compiled, or false if it is declared
    ///         in a referenced assembly;
    ///     (4) the modifiers for the declaration.
    ///
    /// Returns Null otherwise.
    ///
    /// If the given callable corresponds to the (auto-generated) type constructor for a user defined type,
    /// returns the file in which that type is declared as source.
    /// Whether the callable has been deprecated is determined by checking the associated attributes for an attribute with the corresponding name. 
    /// Note that if the type is declared in a source files, the *unresolved* attributes will be checked.
    /// In that case checkDeprecation is used to validate the namespace qualification of the attribute. 
    /// If checkDeprecation is not specified, it is assumed that no qualification is needed in the relevant namespace and source file.
    member this.TryFindCallable (cName, ?checkDeprecation : (string -> bool)) = 
        let checkDeprecation = defaultArg checkDeprecation (fun qual -> String.IsNullOrWhiteSpace qual || qual = BuiltIn.Deprecated.Namespace.Value)
        match CallablesInReferences.TryGetValue cName with 
        | true, cDecl -> Value (cDecl.SourceFile, cDecl.Attributes |> SymbolResolution.TryFindRedirect, false, cDecl.Modifiers)
        | false, _ -> FromSingleSource (fun partialNS -> partialNS.TryGetCallable cName |> function
            | true, (_, cDecl) -> Some (partialNS.Source, cDecl.DefinedAttributes |> SymbolResolution.TryFindRedirectInUnresolved checkDeprecation, true, cDecl.Modifiers)
            | false, _ -> None)

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
    member this.AddDocumenation source doc =
        match Parts.TryGetValue source with 
        | true, partial -> partial.AddDocumenation doc
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
    /// The given location is associated with both the type constructur and the type itself and accessible via the record properties Position and SymbolRange. 
    /// If a type or callable with that name already exists, returns an array of suitable diagnostics.
    /// Throws an ArgumentException if the given source file is not listed as a source for (part of) the namespace.
    member this.TryAddType (source, location) ((tName, tRange), typeTuple, attributes, modifiers, documentation) : QsCompilerDiagnostic[] = 
        match Parts.TryGetValue source with 
        | true, partial when not (IsDefined tName) -> 
            TypesDefinedInAllSourcesCache <- null
            CallablesDefinedInAllSourcesCache <- null
            partial.AddType location (tName, typeTuple, attributes, modifiers, documentation); [||]
        | true, _ ->  this.TryFindType tName |> function
            | Value _ -> [| tRange |> QsCompilerDiagnostic.Error (ErrorCode.TypeRedefinition, [tName.Value]) |]
            | Null -> [| tRange |> QsCompilerDiagnostic.Error (ErrorCode.TypeConstructorOverlapWithCallable, [tName.Value]) |] 
        | false, _ -> ArgumentException "given source is not listed as a source of (parts of) the namespace" |> raise

    /// If no callable (function, operation, or type constructor) with the given name exists in this namespace, 
    /// adds a declaration for the callable of the given kind (operation or function) with the given name and signature 
    /// to the given source, and returns an empty array. 
    /// The given location is associated with the callable declaration and accessible via the record properties Position and SymbolRange. 
    /// If a callable with that name already exists, returns an array of suitable diagnostics.
    /// Throws an ArgumentException if the given source file is not listed as a source for (part of) the namespace.
    member this.TryAddCallableDeclaration (source, location) ((cName, cRange), (kind, signature), attributes, modifiers, documentation) =
        match Parts.TryGetValue source with 
        | true, partial when not (IsDefined cName) -> 
            CallablesDefinedInAllSourcesCache <- null
            partial.AddCallableDeclaration location (cName, (kind, signature), attributes, modifiers, documentation); [||]
        | true, _ -> this.TryFindType cName |> function 
            | Value _ -> [| cRange |> QsCompilerDiagnostic.Error (ErrorCode.CallableOverlapWithTypeConstructor, [cName.Value]) |]
            | Null -> [| cRange |> QsCompilerDiagnostic.Error (ErrorCode.CallableRedefinition, [cName.Value]) |]
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
                | Item item -> fct item |> function | UnitType | InvalidType -> true | _ -> false
                | _ -> false
            match CallablesInReferences.TryGetValue cName with
            | true, cDecl ->
                let unitReturn = cDecl.Signature.ReturnType |> unitOrInvalid (fun (t : ResolvedType) -> t.Resolution)
                unitReturn, cDecl.Signature.TypeParameters.Length
            | false, _ ->
                let _, cDecl = Parts.[declSource].GetCallable cName // ok only because/if we have covered that the callable is not in a reference!
                let unitReturn = cDecl.Defined.ReturnType |> unitOrInvalid (fun (t : QsType) -> t.Type)
                unitReturn, cDecl.Defined.TypeParameters.Length

        match Parts.TryGetValue source with 
        | true, partial ->
            match this.TryFindCallable cName with
            | Value (declSource, _, _, _) ->
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
            | Null -> [| cRange |> QsCompilerDiagnostic.Error (ErrorCode.SpecializationForUnknownCallable, [cName.Value]) |]
        | false, _ -> ArgumentException "given source is not listed as a source of (parts of) the namespace" |> raise

    /// Adds an auto-generated specialization of the given kind to the callable with the given name and declaration in the specified source file. 
    /// Sets the location to the same location as the callable declaration, with the range set to the message range if the given message range is not Null. 
    /// Return the diagnostics generated upon adding the specialization. 
    /// Throws an ArgumentException if the given source file is not listed as a source for (part of) the namespace.
    member internal this.InsertSpecialization (kind, typeArgs) (parentName : NonNullable<string>, source) (declLocation : QsLocation, msgRange : QsRangeInfo) =
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


/// Threadsafe class for global symbol management.
/// Takes a lookup for all callables and for all types declared within one of the assemblies 
/// referenced by the compilation unit this namespace manager belongs to.
/// The key for the given lookups is the name of the namespace the declarations belongs to.
and NamespaceManager 
    (syncRoot : IReaderWriterLock,
     callablesInRefs : IEnumerable<CallableDeclarationHeader>, 
     specializationsInRefs : IEnumerable<SpecializationDeclarationHeader * SpecializationImplementation>,
     typesInRefs : IEnumerable<TypeDeclarationHeader>) = 
    // This class itself does not use any concurrency, 
    // so anything that is accessible within the class only does not apply any locks.
    // IMPORTANT: the syncRoot is intentionally not exposed externally, since with this class supporting mutation
    // access to that lock needs to be coordinated by whatever coordinates the mutations.

    /// the version number is incremented whenever a write operation is performed
    let mutable versionNumber = 0;
    /// handle to avoid unnecessary work
    let mutable containsResolutions = true // initialized without any entries - hence it is resolved

    /// dictionary with all declared namespaces
    /// the key is the name of the namespace
    let Namespaces = 
        let namespaces = new Dictionary<NonNullable<string>, Namespace>()
        let callables = callablesInRefs.ToLookup(fun header -> header.QualifiedName.Namespace)
        let specializations = specializationsInRefs.ToLookup(fun (header,_) -> header.Parent.Namespace)
        let types = typesInRefs.ToLookup(fun header -> header.QualifiedName.Namespace)
        let getKeys (lookup : ILookup<_,_>) = lookup |> Seq.map (fun group -> group.Key)
        let namespacesInRefs = (getKeys callables).Concat(getKeys specializations).Concat(getKeys types) |> Seq.distinct
        for nsName in namespacesInRefs do
            namespaces.Add (nsName, new Namespace(nsName, [], callables.[nsName], specializations.[nsName], types.[nsName]))
        namespaces

    /// Returns the full name of all entry points currently resolved in any of the tracked source files. 
    let GetEntryPoints () = 
        let entryPoints = Namespaces.Values |> Seq.collect (fun ns -> 
            ns.CallablesDefinedInAllSources() |> Seq.choose (fun kvPair ->
                let cName, (source, (_, decl)) = kvPair.Key, kvPair.Value
                if decl.ResolvedAttributes |> Seq.exists BuiltIn.MarksEntryPoint then Some ({Namespace = ns.Name; Name = cName}, source) else None))
        entryPoints.ToImmutableArray()

    /// If a namespace with the given name exists, returns that namespace 
    /// as well as all imported namespaces for that namespace in the given source file.
    /// Filters namespaces that have been imported under a different name.
    /// Filters all unknown namespaces, i.e. imported namespaces that are not managed by this namespace manager.
    /// Throws an ArgumentException if no namespace with the given name exists, 
    /// or the given source file is not listed as source of that namespace.  
    let OpenNamespaces (nsName, source) = 
        let isKnownAndNotAliased (kv : KeyValuePair<_,_>) = 
            if kv.Value <> null then None
            else Namespaces.TryGetValue kv.Key |> function | true, ns -> Some ns | false, _ -> None
        match Namespaces.TryGetValue nsName with 
        | true, ns -> ns, ns.ImportedNamespaces source |> Seq.choose isKnownAndNotAliased |> Seq.toList 
        | false, _ -> ArgumentException("no namespace with the given name exists") |> raise

    /// If the given function containsSymbol returns a Value for the namespace with the given name,  
    /// returns a list containing only a single tuple with the given namespace name and the returned value.  
    /// Otherwise returns a list with all possible (namespaceName, returnedValue) tuples that yielded non-Null values 
    /// for namespaces opened within the given namespace and source file.
    /// Throws an ArgumentException if no namespace with the given name exists, 
    /// or the given source file is not listed as source of that namespace.  
    let PossibleResolutions containsSymbol (nsName, source) = 
        let containsSource (arg : Namespace) = 
            containsSymbol arg |> QsNullable<_>.Map (fun source -> (arg.Name, source)) 
        let currentNS, importedNS = OpenNamespaces (nsName, source)
        currentNS |> containsSource |> function
        | Value (nsName, source) -> [(nsName, source)]
        | Null -> importedNS |> List.choose (containsSource >> function | Value v -> Some v | Null -> None)

    /// Given a qualifier for a symbol name, returns the corresponding namespace as Some
    /// if such a namespace or such a namespace short name within the given parent namespace and source file exists. 
    /// Throws an ArgumentException if the qualifier does not correspond to a known namespace and the given parent namespace does not exist.
    let TryResolveQualifier qualifier (nsName, source) = 
        match Namespaces.TryGetValue qualifier with
        | false, _ -> Namespaces.TryGetValue nsName |> function // check if qualifier is a namespace short name
            | true, parentNS -> (parentNS.NamespaceShortNames source).TryGetValue qualifier |> function
                | true, unabbreviated -> Namespaces.TryGetValue unabbreviated |> function
                    | false, _ -> QsCompilerError.Raise "the corresponding namespace for a namespace short name could not be found"; None
                    | true, ns -> Some ns
                | false, _ -> None
            | false, _ -> ArgumentException "no namespace with the given name exists" |> raise
        | true, ns -> Some ns

    /// Returns the possible qualifications for the built-in type or callable used in the given namespace and source.
    /// where the given source may either be the name of a source file or of a referenced assembly. 
    /// If the given source is not listed as source file of the namespace, assumes that the source if one of the references 
    /// and returns the namespace name of the given built in type or callable as only possible qualification. 
    /// Throws an ArgumentException if no namespace with the given name exists. 
    let PossibleQualifications (nsName, source) (builtIn : BuiltIn) = 
        match Namespaces.TryGetValue nsName with 
        | true, ns when ns.Sources.Contains source -> (ns.ImportedNamespaces source).TryGetValue builtIn.Namespace |> function
            | true, null when ns.TryFindType builtIn.Name = Null || nsName.Value = builtIn.Namespace.Value -> [""; builtIn.Namespace.Value] 
            | true, null -> [builtIn.Namespace.Value] // the built-in type or callable is shadowed
            | true, alias -> [alias; builtIn.Namespace.Value]
            | false, _ -> [builtIn.Namespace.Value]
        | true, _ -> [builtIn.Namespace.Value];
        | false, _ -> ArgumentException "no namespace with the given name exists" |> raise

    /// Returns whether a declaration is accessible from the calling location, given whether the calling location is in
    /// the same assembly and/or namespace as the declaration, and the declaration's modifiers.
    let IsDeclarationAccessible sameAssembly sameNs modifiers =
        match modifiers.Access with
        | DefaultAccess -> true
        | Internal -> sameAssembly
        | Private -> sameAssembly && sameNs

    /// Given the qualified or unqualfied name of a type used within the given parent namespace and source file, determines if such a type exists 
    /// and returns its full name and the source file or referenced assembly in which it is defined as Some if it does. 
    /// Returns None if no such type exist, or if the type name is unqualified and ambiguous.
    /// Generates and returns an array with suitable diagnostics.
    /// Throws an ArgumentException if the given parent namespace does not exist,
    /// or if no source file with the given name is listed as source of that namespace. 
    let TryResolveTypeName (parentNS, source) ((nsName, symName), symRange) = 
        let orDefault (range : QsNullable<_>) = range.ValueOr QsCompilerDiagnostic.DefaultRange
        let checkQualificationForDeprecation qual = BuiltIn.Deprecated |> PossibleQualifications (parentNS, source) |> Seq.contains qual
        let buildAndReturn (ns, declSource, deprecation, modifiers, errs) =
            let deprecatedWarnings =
                deprecation
                |> SymbolResolution.GenerateDeprecationWarning ({Namespace = ns; Name = symName}, symRange |> orDefault)
            (Some ({Namespace = ns; Name = symName; Range = symRange}, declSource, modifiers),
             Array.append errs deprecatedWarnings)
        let tryFind (parentNS, source) (tName, tRange) =
            let allResolutions =
                PossibleResolutions
                    (fun ns -> ns.TryFindType (tName, checkQualificationForDeprecation))
                    (parentNS, source)
            let accessibleResolutions = allResolutions |> List.filter (fun (nsName, (_, _, sameAssembly, modifiers)) ->
                IsDeclarationAccessible sameAssembly (parentNS = nsName) modifiers)
            match accessibleResolutions with
            | [] ->
                if List.isEmpty allResolutions
                then Null, [| tRange |> orDefault |> QsCompilerDiagnostic.Error (ErrorCode.UnknownType, [tName.Value]) |]
                else Null, [| tRange |> orDefault |> QsCompilerDiagnostic.Error (ErrorCode.InaccessibleType, [tName.Value]) |]
            | [(nsName, (declSource, deprecated, _, modifiers))] ->
                Value (nsName, declSource, deprecated, modifiers), [||]
            | _ ->
                let diagArg = String.Join(", ", accessibleResolutions.Select (fun (ns,_) -> ns.Value))
                Null, [| tRange |> orDefault |> QsCompilerDiagnostic.Error (ErrorCode.AmbiguousType, [tName.Value; diagArg]) |]
        
        match nsName with
        | None -> tryFind (parentNS, source) (symName, symRange) |> function
            | Value (ns, declSource, deprecation, modifiers), errs ->
                buildAndReturn (ns, declSource, deprecation, modifiers, errs)
            | Null, errs -> None, errs
        | Some qualifier -> (parentNS, source) |> TryResolveQualifier qualifier |> function
            | None -> None, [| symRange |> orDefault |> QsCompilerDiagnostic.Error (ErrorCode.UnknownNamespace, [qualifier.Value]) |]
            | Some ns ->
                ns.TryFindType (symName, checkQualificationForDeprecation) |> function
                | Value (declSource, deprecation, sameAssembly, modifiers) ->
                    if IsDeclarationAccessible sameAssembly (parentNS = ns.Name) modifiers
                    then buildAndReturn (ns.Name, declSource, deprecation, modifiers, [||])
                    else None, [| symRange |> orDefault |> QsCompilerDiagnostic.Error (ErrorCode.InaccessibleTypeInNamespace, [symName.Value; qualifier.Value]) |]
                | _ -> None, [| symRange |> orDefault |> QsCompilerDiagnostic.Error (ErrorCode.UnknownTypeInNamespace, [symName.Value; qualifier.Value]) |]

    /// Fully (i.e. recursively) resolves the given Q# type used within the given parent in the given source file. The
    /// resolution consists of replacing all unqualified names for user defined types by their qualified name.
    ///
    /// Generates an array of diagnostics for the cases where no user defined type of the specified name (qualified or
    /// unqualified) can be found. In that case, resolves the user defined type by replacing it with the Q# type
    /// denoting an invalid type.
    ///
    /// Diagnostics can be generated in additional cases when UDTs are referenced by returning an array of diagnostics
    /// from the given checkUdt function.
    ///
    /// Verifies that all used type parameters are defined in the given list of type parameters, and generates suitable
    /// diagnostics if they are not, replacing them by the Q# type denoting an invalid type. Returns the resolved type
    /// as well as an array with diagnostics.
    ///
    /// IMPORTANT: for performance reasons does *not* verify if the given the given parent and/or source file is
    /// consistent with the defined callables.
    /// May throw an exception if the given parent and/or source file is inconsistent with the defined declarations. 
    /// Throws a NonSupportedException if the QsType to resolve contains a MissingType.
    let resolveType (parent : QsQualifiedName, tpNames, source) qsType checkUdt =
        let processUDT = TryResolveTypeName (parent.Namespace, source) >> function
            | Some (udt, _, modifiers), errs -> UserDefinedType udt, Array.append errs (checkUdt (udt, modifiers))
            | None, errs -> InvalidType, errs
        let processTP (symName, symRange) =
            if tpNames |> Seq.contains symName
            then TypeParameter {Origin = parent; TypeName = symName; Range = symRange}, [||]
            else InvalidType, [| symRange.ValueOr QsCompilerDiagnostic.DefaultRange
                                 |> QsCompilerDiagnostic.Error (ErrorCode.UnknownTypeParameterName, [symName.Value]) |]
        syncRoot.EnterReadLock()
        try SymbolResolution.ResolveType (processUDT, processTP) qsType
        finally syncRoot.ExitReadLock()

    /// Compares the accessibility of the parent declaration with the accessibility of the UDT being referenced. If the
    /// accessibility of a referenced type is less than the accessibility of the parent, returns a diagnostic using the
    /// given error code. Otherwise, returns an empty array.
    let checkUdtAccessibility code
                              (parent : NonNullable<string>, parentModifiers)
                              (udt : UserDefinedType, udtModifiers) =
        match (parentModifiers.Access, udtModifiers.Access) with
        | (DefaultAccess, DefaultAccess)
        | (Internal, DefaultAccess)
        | (Internal, Internal)
        | (Private, _) -> [||] // OK
        | _ ->
            // Error
            [| QsCompilerDiagnostic.Error
                   (code, [udt.Name.Value; parent.Value])
                   (udt.Range.ValueOr QsCompilerDiagnostic.DefaultRange) |]


    /// Given the name of the namespace as well as the source file in which the attribute occurs, resolves the given attribute.
    /// Generates suitable diagnostics if a suitable attribute cannot be determined, 
    /// or if the attribute argument contains expressions that are not supported, 
    /// or if the resolved argument type does not match the expected argument type. 
    /// Returns the resolved attribute as well as the generated diagnostics.
    /// The TypeId in the resolved attribute is set to Null if the unresolved Id is not a valid identifier 
    /// or if the correct attribute cannot be determined, and is set to the corresponding type identifier otherwise. 
    /// May throw an ArgumentException if the given parent namespace does not exist,
    /// or if no source file with the given name is listed as source of that namespace. 
    member private this.ResolveAttribute (parentNS, source) attribute = 
        let getAttribute ((nsName, symName), symRange) = 
            match TryResolveTypeName (parentNS, source) ((nsName, symName), symRange) with
            | Some (udt, declSource, _), errs -> // declSource may be the name of an assembly!
                let fullName = sprintf "%s.%s" udt.Namespace.Value udt.Name.Value
                let validQualifications = BuiltIn.Attribute |> PossibleQualifications (udt.Namespace, declSource)
                match Namespaces.TryGetValue udt.Namespace with 
                | true, ns -> ns.TryGetAttributeDeclaredIn declSource (udt.Name, validQualifications) |> function 
                    | None -> None, [| symRange.ValueOr QsCompilerDiagnostic.DefaultRange |> QsCompilerDiagnostic.Error (ErrorCode.NotMarkedAsAttribute, [fullName]) |] 
                    | Some argType -> Some (udt, argType), errs
                | false, _ -> QsCompilerError.Raise "namespace for defined type not found"; None, errs
            | None, errs -> None, errs
        let resolved, msgs = SymbolResolution.ResolveAttribute getAttribute attribute
        resolved, msgs |> Array.map (fun m -> attribute.Position, m)

    /// Resolves the DefinedAttributes of the given declaration using ResolveAttribute and validates any entry points, if any. 
    /// Returns the resolved attributes as well as an array with diagnostics along with the declaration position. 
    /// Each entry in the returned array of attributes is the resolution for the corresponding entry in the array of defined attributes. 
    /// May throw an ArgumentException if no parent callable with the given name exists. 
    member private this.ResolveAttributes (parent : QsQualifiedName, source) (decl : Resolution<'T,_>) = 
        let attr, msgs = decl.DefinedAttributes |> Seq.map (this.ResolveAttribute (parent.Namespace, source)) |> Seq.toList |> List.unzip
        let errs = new List<_>(msgs |> Seq.collect id)
        let validateAttributes (alreadyDefined : int list, resAttr) (att : QsDeclarationAttribute) = 
            let returnInvalid msg = 
                errs.AddRange msg
                alreadyDefined, {att with TypeId = Null} :: resAttr
            match att.TypeId with

            // known attribute
            | Value tId -> 
                let orDefault (range : QsNullable<_>) = range.ValueOr QsCompilerDiagnostic.DefaultRange
                let attributeHash = 
                    if tId.Namespace.Value = BuiltIn.Deprecated.Namespace.Value && tId.Name.Value = BuiltIn.Deprecated.Name.Value then hash (tId.Namespace.Value, tId.Name.Value) 
                    else hash (tId.Namespace.Value, tId.Name.Value, NamespaceManager.ExpressionHash att.Argument)

                // the attribute is a duplication of another attribute on this declaration
                if alreadyDefined.Contains attributeHash then 
                    (att.Offset, tId.Range |> orDefault 
                    |> QsCompilerDiagnostic.Warning (WarningCode.DuplicateAttribute, [tId.Name.Value])) 
                    |> Seq.singleton |> returnInvalid

                // the attribute marks an entry point 
                elif tId.Namespace.Value = BuiltIn.EntryPoint.Namespace.Value && tId.Name.Value = BuiltIn.EntryPoint.Name.Value then
                    match box decl.Defined with
                    | :? CallableSignature as signature when not (signature.TypeParameters.Any()) -> 
                        let validateArgAndReturnTypes (qsType : QsType) = 
                            qsType.ExtractAll (fun t -> t.Type |> function // ExtractAll recurs on all subtypes (e.g. callable in- and output types as well)
                            | Qubit -> (decl.Position, t.Range |> orDefault |> QsCompilerDiagnostic.Error (ErrorCode.QubitTypeInEntryPointSignature, [])) |> Seq.singleton 
                            | QsTypeKind.Operation _ -> (decl.Position, t.Range |> orDefault |> QsCompilerDiagnostic.Error (ErrorCode.CallableTypeInEntryPointSignature, [])) |> Seq.singleton 
                            | QsTypeKind.Function _ -> (decl.Position, t.Range |> orDefault |> QsCompilerDiagnostic.Error (ErrorCode.CallableTypeInEntryPointSignature, [])) |> Seq.singleton 
                            | UserDefinedType _ -> (decl.Position, t.Range |> orDefault |> QsCompilerDiagnostic.Error (ErrorCode.UserDefinedTypeInEntryPointSignature, [])) |> Seq.singleton 
                            | _ -> Seq.empty)
                        let argErrs = signature.Argument.Items.Select(snd).Append signature.ReturnType |> Seq.collect validateArgAndReturnTypes 
                        let hasCharacteristics = signature.Characteristics.Characteristics |> function | EmptySet | InvalidSetExpr -> false | _ -> true
                        match Namespaces.TryGetValue parent.Namespace with 
                        | false, _ -> ArgumentException "no namespace with the given name exists" |> raise
                        | true, ns when not ((ns.SpecializationsDefinedInAllSources parent.Name).Any(fst >> (<>)QsBody) || hasCharacteristics) -> ()
                        | _ -> errs.Add (decl.Position, signature.Characteristics.Range.ValueOr decl.Range |> QsCompilerDiagnostic.Error (ErrorCode.InvalidEntryPointSpecialization, []))
                        if argErrs.Any() then returnInvalid argErrs 
                        else GetEntryPoints() |> Seq.tryHead |> function
                            | None -> attributeHash :: alreadyDefined, att :: resAttr
                            | Some (epName, epSource) -> 
                                let msgArgs = [sprintf "%s.%s" epName.Namespace.Value epName.Name.Value; epSource.Value]
                                (att.Offset, tId.Range |> orDefault |> QsCompilerDiagnostic.Error (ErrorCode.MultipleEntryPoints, msgArgs)) |> Seq.singleton |> returnInvalid 
                    | _ -> (att.Offset, tId.Range |> orDefault |> QsCompilerDiagnostic.Error (ErrorCode.InvalidEntryPointPlacement, [])) |> Seq.singleton |> returnInvalid
                
                // the attribute marks a unit test
                elif tId.Namespace.Value = BuiltIn.Test.Namespace.Value && tId.Name.Value = BuiltIn.Test.Name.Value then
                    let isUnitToUnit (signature : CallableSignature) = 
                        let isUnitType = function 
                            | Tuple _ | Missing -> false
                            | Item (itemType : QsType) -> itemType.Type = UnitType
                            | _ -> true // invalid type
                        match signature.Argument.Items |> Seq.toList with 
                        | [] -> signature.ReturnType |> isUnitType
                        | [(_, argType)] -> argType |> isUnitType && signature.ReturnType |> isUnitType
                        | _ -> false
                    match box decl.Defined with 
                    | :? CallableSignature as signature when signature |> isUnitToUnit && not (signature.TypeParameters.Any()) -> 
                        let arg = att.Argument |> AttributeAnnotation.NonInterpolatedStringArgument (fun ex -> ex.Expression)
                        let validExecutionTargets = BuiltIn.ValidExecutionTargets |> Seq.map (fun x -> x.ToLowerInvariant())
                        if arg <> null && (validExecutionTargets |> Seq.contains (arg.ToLowerInvariant()) || SyntaxGenerator.FullyQualifiedName.IsMatch arg) then
                            attributeHash :: alreadyDefined, att :: resAttr 
                        else (att.Offset, att.Argument.Range |> orDefault |> QsCompilerDiagnostic.Error (ErrorCode.InvalidExecutionTargetForTest, [])) |> Seq.singleton |> returnInvalid 
                    | _ -> (att.Offset, tId.Range |> orDefault |> QsCompilerDiagnostic.Error (ErrorCode.InvalidTestAttributePlacement, [])) |> Seq.singleton |> returnInvalid

                // the attribute is another kind of attribute that requires no further verification at this point
                else attributeHash :: alreadyDefined, att :: resAttr 

            // unknown attribute, and an error has already been generated
            | _ -> alreadyDefined, att :: resAttr
        let resAttr = attr |> List.fold validateAttributes ([], []) |> snd
        resAttr.Reverse() |> ImmutableArray.CreateRange, errs.ToArray()

    /// Fully (i.e. recursively) resolves the given Q# type used within the given parent in the given source file. The
    /// resolution consists of replacing all unqualified names for user defined types by their qualified name.
    ///
    /// Generates an array of diagnostics for the cases where no user defined type of the specified name (qualified or
    /// unqualified) can be found. In that case, resolves the user defined type by replacing it with the Q# type
    /// denoting an invalid type.
    ///
    /// Verifies that all used type parameters are defined in the given list of type parameters, and generates suitable
    /// diagnostics if they are not, replacing them by the Q# type denoting an invalid type. Returns the resolved type
    /// as well as an array with diagnostics.
    ///
    /// IMPORTANT: for performance reasons does *not* verify if the given the given parent and/or source file is
    /// consistent with the defined callables.
    /// May throw an exception if the given parent and/or source file is inconsistent with the defined declarations. 
    /// Throws a NonSupportedException if the QsType to resolve contains a MissingType.
    member this.ResolveType (parent : QsQualifiedName, tpNames : ImmutableArray<_>, source : NonNullable<string>)
                            (qsType : QsType)
                            : ResolvedType * QsCompilerDiagnostic[] =
        resolveType (parent, tpNames, source) qsType (fun _ -> [||])

    /// Resolves the underlying type as well as all named and unnamed items for the given type declaration in the specified source file. 
    /// IMPORTANT: for performance reasons does *not* verify if the given the given parent and/or source file is consistent with the defined types. 
    /// May throw an exception if the given parent and/or source file is inconsistent with the defined types. 
    /// Throws an ArgumentException if the given type tuple is an empty QsTuple. 
    member private this.ResolveTypeDeclaration (fullName, source, modifiers) typeTuple =
        // Currently, type parameters for UDTs are not supported.
        let resolveType qsType =
            resolveType
                (fullName, ImmutableArray<_>.Empty, source)
                qsType
                (checkUdtAccessibility ErrorCode.TypeLessAccessibleThanParentType (fullName.Name, modifiers))
        SymbolResolution.ResolveTypeDeclaration resolveType typeTuple

    /// Given the namespace and the name of the callable that the given signature belongs to, as well as its kind and the source file it is declared in,
    /// fully resolves all Q# types in the signature using ResolveType.
    /// Returns a new signature with the resolved types, the resolved argument tuple, as well as the array of diagnostics created during type resolution.
    /// The position offset information for the variables declared in the argument tuple will be set to Null.
    /// Positional information within types is set to Null if the parent callable is a type constructor. 
    /// IMPORTANT: for performance reasons does *not* verify if the given the given parent and/or source file is consistent with the defined callables. 
    /// May throw an exception if the given parent and/or source file is inconsistent with the defined callables. 
    /// Throws an ArgumentException if the given list of characteristics is empty.
    member private this.ResolveCallableSignature (parentKind, parentName, source, modifiers)
                                                 (signature, specBundleCharacteristics) =
        let resolveType tpNames qsType =
            let res, errs =
                resolveType
                    (parentName, tpNames, source)
                    qsType
                    (checkUdtAccessibility ErrorCode.TypeLessAccessibleThanParentCallable (parentName.Name, modifiers))
            if parentKind <> TypeConstructor then res, errs
            else res.WithoutRangeInfo, errs // strip positional info for auto-generated type constructors
        SymbolResolution.ResolveCallableSignature (resolveType, specBundleCharacteristics) signature


    /// Sets the Resolved property for all type and callable declarations to Null, and the ResolvedAttributes to an empty array.
    /// Unless the clearing is forced, does nothing if the symbols are not currently resolved.
    member private this.ClearResolutions ?force = 
        let force = defaultArg force false
        if this.ContainsResolutions || force then
            for ns in Namespaces.Values do 
                for kvPair in ns.TypesDefinedInAllSources() do
                    ns.SetTypeResolution (fst kvPair.Value) (kvPair.Key, Null, ImmutableArray.Empty)
                for kvPair in ns.CallablesDefinedInAllSources() do
                    ns.SetSpecializationResolutions (kvPair.Key, (fun _ _ -> Null, [||]), fun _ _ -> ImmutableArray.Empty, [||]) |> ignore
                    ns.SetCallableResolution (fst kvPair.Value) (kvPair.Key, Null, ImmutableArray.Empty)
            this.ContainsResolutions <- false

    /// Resolves and caches the attached attributes and underlying type of the types declared in all source files of each namespace.
    /// Returns the diagnostics generated upon resolution as well as the root position and file for each diagnostic as tuple.
    member private this.CacheTypeResolution () = 
        let sortedNamespaces = Namespaces.Values |> Seq.sortBy (fun ns -> ns.Name.Value) |> Seq.toList
        // Since attributes are declared as types, we first need to resolve all types ...
        let resolutionDiagnostics = sortedNamespaces |> Seq.collect (fun ns ->
            ns.TypesDefinedInAllSources() |> Seq.collect (fun kvPair ->
                let tName, (source, qsType) = kvPair.Key, kvPair.Value
                let fullName = {Namespace = ns.Name; Name = tName}
                let resolved, msgs = qsType.Defined |> this.ResolveTypeDeclaration (fullName, source, qsType.Modifiers) 
                ns.SetTypeResolution source (tName, resolved |> Value, ImmutableArray.Empty) 
                msgs |> Array.map (fun msg -> source, (qsType.Position, msg))))
        // ... before we can resolve the corresponding attributes. 
        let attributeDiagnostics = sortedNamespaces |> Seq.collect (fun ns ->
            ns.TypesDefinedInAllSources() |> Seq.collect (fun kvPair ->
                let tName, (source, qsType) = kvPair.Key, kvPair.Value
                let parentName = {Namespace = ns.Name; Name = tName}
                let resolvedAttributes, msgs = this.ResolveAttributes (parentName, source) qsType 
                ns.SetTypeResolution source (tName, qsType.Resolved, resolvedAttributes) 
                msgs |> Array.map (fun msg -> source, msg)))
        resolutionDiagnostics.Concat(attributeDiagnostics).ToArray()

    /// Resolves and caches all attached attributes and specialization generation directives for all callables 
    /// declared in all source files of each namespace, inserting inferred specializations if necessary and removing invalid specializations. 
    /// Then resolves and caches the signature of the callables themselves. 
    /// Returns the diagnostics generated upon resolution as well as the root position and file for each diagnostic as tuple.
    /// IMPORTANT: does *not* return diagnostics generated for type constructors - suitable diagnostics need to be generated upon type resolution.
    /// Throws an InvalidOperationException if the types corresponding to the attributes to resolve have not been resolved. 
    member private this.CacheCallableResolutions () = 
        // TODO: this needs to be adapted if we support external specializations
        let diagnostics = Namespaces.Values |> Seq.sortBy (fun ns -> ns.Name.Value) |> Seq.collect (fun ns -> 
            ns.CallablesDefinedInAllSources() |> Seq.sortBy (fun kv -> kv.Key) |> Seq.collect (fun kvPair ->
                let source, (kind, signature) = kvPair.Value
                let parent = {Namespace = ns.Name; Name = kvPair.Key}

                // we first need to resolve the type arguments to determine the right sets of specializations to consider
                let typeArgsResolution specSource = 
                    let typeResolution = this.ResolveType (parent, ImmutableArray.Empty, specSource) // do not allow using type parameters within type specializations!
                    SymbolResolution.ResolveTypeArgument typeResolution
                let mutable errs = ns.SetSpecializationResolutions (parent.Name, typeArgsResolution, fun _ _ -> ImmutableArray.Empty, [||])

                // we then build the specialization bundles (one for each set of type and set arguments) and insert missing specializations
                let definedSpecs = ns.SpecializationsDefinedInAllSources parent.Name
                let insertSpecialization typeArgs kind = ns.InsertSpecialization (kind, typeArgs) (parent.Name, source)
                let props, bundleErrs = SymbolResolution.GetBundleProperties insertSpecialization (signature, source) definedSpecs
                let bundleErrs = bundleErrs |> Array.concat
                errs <- bundleErrs :: errs

                // we remove the specializations which could not be bundled and resolve the newly inserted ones
                for (specSource, (errPos, d)) in bundleErrs do
                    match d.Diagnostic with 
                    | Information _ | Warning _ -> ()
                    | Error errCode -> 
                        let removed = ns.RemoveSpecialization (specSource, {Offset = errPos; Range = d.Range}) parent.Name
                        QsCompilerError.Verify ((removed <= 1), sprintf "removed %i specializations based on error code %s" removed (errCode.ToString()))
                let autoResErrs = ns.SetSpecializationResolutions (parent.Name, typeArgsResolution, fun _ _ -> ImmutableArray.Empty, [||])

                // only then can we resolve the generators themselves, as well as the callable and specialization attributes
                let callableAttributes, attrErrs = this.ResolveAttributes (parent, source) signature
                let resolution _ = SymbolResolution.ResolveGenerator props 
                let specErrs = ns.SetSpecializationResolutions (parent.Name, resolution, fun attSource -> this.ResolveAttributes (parent, attSource))

                // and finally we resolve the overall signature (whose characteristics are the intersection of the one of all bundles)
                let characteristics = props.Values |> Seq.map (fun bundle -> bundle.BundleInfo) |> Seq.toList
                let resolved, msgs = (signature.Defined, characteristics) |> this.ResolveCallableSignature (kind, parent, source, signature.Modifiers) // no positional info for type constructors
                ns.SetCallableResolution source (parent.Name, resolved |> Value, callableAttributes)
                errs <- (attrErrs |> Array.map (fun m -> source, m)) :: (msgs |> Array.map (fun m -> source, (signature.Position, m))) :: errs

                if kind = QsCallableKind.TypeConstructor then [||] // don't return diagnostics for type constructors - everything will be captured upon type resolution
                else specErrs.Concat autoResErrs |> errs.Concat |> Array.concat))
        diagnostics.ToArray()


    /// returns the current version number of the namespace manager -
    /// the version number is incremented whenever a write operation is performed
    member this.VersionNumber = versionNumber

    /// set to true if all types have been fully resolved and false otherwise
    member this.ContainsResolutions 
        with get() = containsResolutions
        and private set value = containsResolutions <- value

    /// For each given namespace, automatically adds an open directive to all partial namespaces
    /// in all source files, if a namespace with that name indeed exists and is part of this compilation. 
    /// Independent on whether the symbols have already been resolved, proceeds to resolves 
    /// all types and callables as well as their attributes defined throughout all namespaces and caches the resolution. 
    /// Returns the diagnostics generated during resolution 
    /// together with the Position of the declaration for which the diagnostics were generated.
    member this.ResolveAll (autoOpen : ImmutableHashSet<_>) = 
        // TODO: this needs to be adapted if we support external specializations
        syncRoot.EnterWriteLock()
        versionNumber <- versionNumber + 1
        try let autoOpen = if autoOpen <> null then autoOpen else ImmutableHashSet.Empty
            let nsToAutoOpen = autoOpen.Intersect Namespaces.Keys 
            let zeroRange = (QsPositionInfo.Zero, QsPositionInfo.Zero)
            for ns in Namespaces.Values do 
                for source in ns.Sources do 
                    for opened in nsToAutoOpen do
                        this.AddOpenDirective (opened, zeroRange) (null, Value zeroRange) (ns.Name, source) |> ignore
            // We need to resolve types before we resolve callables, 
            // since the attribute resolution for callables relies on the corresponding types having been resolved.
            let typeDiagnostics = this.CacheTypeResolution()
            let callableDiagnostics = this.CacheCallableResolutions()
            this.ContainsResolutions <- true
            callableDiagnostics.Concat(typeDiagnostics).ToLookup(fst, snd)
        finally syncRoot.ExitWriteLock()

    /// Returns a dictionary that maps each namespace name to a look-up 
    /// that for each source file name contains the names of all imported namespaces in that file and namespace.
    member this.Documentation () = 
        syncRoot.EnterReadLock()
        try let docs = Namespaces.Values |> Seq.map (fun ns -> ns.Name, ns.Documentation)
            docs.ToImmutableDictionary(fst, snd)
        finally syncRoot.ExitReadLock()

    /// Returns a look-up that contains the names of all namespaces imported within a certain source file for the given namespace.
    /// Throws an ArgumentException if no namespace with the given name exists. 
    member this.OpenDirectives nsName = 
        syncRoot.EnterReadLock()
        try match Namespaces.TryGetValue nsName with 
            | true, ns -> 
                let imported = ns.Sources |> Seq.collect (fun source -> 
                    ns.ImportedNamespaces source |> Seq.choose (fun imported -> 
                        if imported.Key <> ns.Name then Some (source, new ValueTuple<_,_>(imported.Key, imported.Value)) else None))
                imported.ToLookup(fst, snd)
            | false, _ -> ArgumentException "no namespace with the given name exists" |> raise
        finally syncRoot.ExitReadLock()

    /// Returns the headers of all imported specializations for callable with the given name. 
    /// Throws an ArgumentException if no namespace or no callable with the given name exists.
    member this.ImportedSpecializations (parent : QsQualifiedName) = 
        // TODO: this may need to be adapted if we support external specializations
        syncRoot.EnterReadLock()
        try let imported = Namespaces.TryGetValue parent.Namespace |> function 
                | false, _ -> ArgumentException "no namespace with the given name exists" |> raise
                | true, ns -> ns.SpecializationsInReferencedAssemblies.[parent.Name].ToImmutableArray()
            if imported.Length <> 0 then imported 
            else ArgumentException "no specializations for a callable with the given name have been imported" |> raise 
        finally syncRoot.ExitReadLock()

    /// Returns the resolved generation directive (if any) as well as the specialization headers 
    /// for all specializations defined in source files for the callable with the given name.
    /// Throws an ArgumentException if no namespace or no callable with the given name exists.
    /// Throws an InvalidOperationException if the symbols are not currently resolved. 
    member this.DefinedSpecializations (parent : QsQualifiedName) = 
        let notResolvedException = InvalidOperationException "specializations are not resolved"
        syncRoot.EnterReadLock()
        try if not this.ContainsResolutions then notResolvedException |> raise
            let defined = Namespaces.TryGetValue parent.Namespace |> function
                | false, _ -> ArgumentException "no namespace with the given name exists" |> raise
                | true, ns -> ns.SpecializationsDefinedInAllSources parent.Name |> Seq.choose (fun (kind, (source, resolution)) ->
                    match resolution.Resolved with
                    | Null -> QsCompilerError.Raise "everything should be resolved but isn't"; None
                    | Value gen -> Some (gen.Directive, {
                        Kind = kind
                        TypeArguments = gen.TypeArguments
                        Information = gen.Information
                        Parent = parent
                        Attributes = resolution.ResolvedAttributes
                        SourceFile = source
                        Position = DeclarationHeader.Offset.Defined resolution.Position
                        HeaderRange = DeclarationHeader.Range.Defined resolution.Range
                        Documentation = resolution.Documentation
                    }))
            defined.ToImmutableArray()
        finally syncRoot.ExitReadLock()

    /// Returns the source file and CallableDeclarationHeader of all callables imported from referenced assemblies.
    member this.ImportedCallables () = 
        // TODO: this needs to be adapted if we support external specializations
        syncRoot.EnterReadLock()
        try let imported = Namespaces.Values |> Seq.collect (fun ns -> ns.CallablesInReferencedAssemblies.Values)
            imported.ToImmutableArray()
        finally syncRoot.ExitReadLock()

    /// Returns the declaration headers for all callables defined in source files.
    /// Throws an InvalidOperationException if the symbols are not currently resolved. 
    member this.DefinedCallables () = 
        let notResolvedException = InvalidOperationException "callables are not resolved"
        syncRoot.EnterReadLock()
        try if not this.ContainsResolutions then notResolvedException |> raise
            let defined = Namespaces.Values |> Seq.collect (fun ns -> 
                ns.CallablesDefinedInAllSources() |> Seq.choose (fun kvPair ->
                    let cName, (source, (kind, declaration)) = kvPair.Key, kvPair.Value
                    match declaration.Resolved with
                    | Null -> QsCompilerError.Raise "everything should be resolved but isn't"; None
                    | Value (signature, argTuple) -> Some {
                        Kind = kind
                        QualifiedName = {Namespace = ns.Name; Name = cName}
                        Attributes = declaration.ResolvedAttributes
                        Modifiers = declaration.Modifiers
                        SourceFile = source
                        Position = DeclarationHeader.Offset.Defined declaration.Position
                        SymbolRange = DeclarationHeader.Range.Defined declaration.Range
                        Signature = signature
                        ArgumentTuple = argTuple
                        Documentation = declaration.Documentation
                    }))
            defined.ToImmutableArray()
        finally syncRoot.ExitReadLock()

    /// Returns the source file and TypeDeclarationHeader of all types imported from referenced assemblies.
    member this.ImportedTypes() = 
        syncRoot.EnterReadLock()
        try let imported = Namespaces.Values |> Seq.collect (fun ns -> ns.TypesInReferencedAssemblies.Values)
            imported.ToImmutableArray()
        finally syncRoot.ExitReadLock()

    /// Returns the declaration headers for all types defined in source files.
    /// Throws an InvalidOperationException if the symbols are not currently resolved. 
    member this.DefinedTypes () = 
        let notResolvedException = InvalidOperationException "types are not resolved"
        syncRoot.EnterReadLock()
        try if not this.ContainsResolutions then notResolvedException |> raise
            let defined = Namespaces.Values |> Seq.collect (fun ns -> 
                ns.TypesDefinedInAllSources() |> Seq.choose (fun kvPair ->
                    let tName, (source, qsType) = kvPair.Key, kvPair.Value
                    match qsType.Resolved with
                    | Null -> QsCompilerError.Raise "everything should be resolved but isn't"; None
                    | Value (underlyingType, items) -> Some {
                        QualifiedName = {Namespace = ns.Name; Name = tName}
                        Attributes = qsType.ResolvedAttributes
                        Modifiers = qsType.Modifiers
                        SourceFile = source
                        Position = DeclarationHeader.Offset.Defined qsType.Position
                        SymbolRange = DeclarationHeader.Range.Defined qsType.Range
                        Type = underlyingType
                        TypeItems = items
                        Documentation = qsType.Documentation
                    }))
            defined.ToImmutableArray()
        finally syncRoot.ExitReadLock()

    /// removes the given source file and all its content from all namespaces 
    member this.RemoveSource source =
        syncRoot.EnterWriteLock()
        versionNumber <- versionNumber + 1
        try for ns in Namespaces.Values do ns.TryRemoveSource source |> ignore
            let keys = Namespaces.Keys |> List.ofSeq
            for key in keys do if Namespaces.[key].IsEmpty then Namespaces.Remove key |> ignore
            this.ClearResolutions()
        finally syncRoot.ExitWriteLock()    

    /// clears all content from the symbol table
    member this.Clear() = 
        syncRoot.EnterWriteLock()
        versionNumber <- versionNumber + 1
        try this.ContainsResolutions <- true
            Namespaces.Clear()
        finally syncRoot.ExitWriteLock()        
        
    /// Adds every namespace along with all its content to target.
    /// IMPORTANT: if a namespace already exists in the target, replaces it! 
    member this.CopyTo (target : NamespaceManager) =
        syncRoot.EnterReadLock()
        try for ns in Namespaces.Values do
                target.AddOrReplaceNamespace ns // ns will be deep copied
        finally syncRoot.ExitReadLock()

    /// If a namespace with the given name exists, 
    /// makes a (deep) copy of that namespace and - if the given source file is not already listed as source for that namespace - 
    /// adds the given source to the list of sources for the made copy, before returning the copy.
    /// If no namespace with the given name exists, returns a new Namespace with the given source file listed as source.
    /// NOTE: This routine does *not* modify this symbol table, 
    /// and any modification to the returned namespace won't be reflected here - 
    /// use AddOrReplaceNamespace to push back the modifications into the symbol table.
    member this.CopyForExtension (nsName, source) =
        syncRoot.EnterReadLock()
        try match Namespaces.TryGetValue nsName with 
            | true, NS ->
                let copy = NS.Copy()
                if copy.TryAddSource source then copy
                else ArgumentException "partial namespace already exists" |> raise
            | false, _ -> new Namespace(nsName, [source], ImmutableArray.Empty, ImmutableArray.Empty, ImmutableArray.Empty)
        finally syncRoot.ExitReadLock()

    /// Given a Namespace, makes a (deep) copy of that Namespace and replaces the existing namespace with that name
    /// by that copy, if such a namespace already exists, or adds the copy as a new namespace. 
    /// -> Any modification to the namespace after pushing it into the symbol table (i.e. calling this routine) won't be reflected here.
    member this.AddOrReplaceNamespace (ns : Namespace) =
        syncRoot.EnterWriteLock()
        versionNumber <- versionNumber + 1
        try Namespaces.[ns.Name] <- ns.Copy()
            this.ClearResolutions true // force the clearing, since otherwise the newly added namespace may not be cleared
        finally syncRoot.ExitWriteLock()

    /// Adds the opened namespace to the list of imported namespaces for the given source and namespace. 
    /// If the namespace to list as imported does not exists, or if the given alias cannot be used as namespace short name, 
    /// adds the corresponding diagnostics to an array of diagnostics and returns them. 
    /// Returns an empty array otherwise. 
    /// Throws an Argument exception if the given namespace or source file for which to add the open directive does not exist.
    member this.AddOpenDirective (opened, openedRange) (alias, aliasRange) (nsName, source) = 
        syncRoot.EnterWriteLock()
        versionNumber <- versionNumber + 1
        try this.ClearResolutions()
            match Namespaces.TryGetValue nsName with 
            | true, ns when ns.Sources.Contains source -> 
                let validAlias = String.IsNullOrWhiteSpace alias || NonNullable<string>.New (alias.Trim()) |> Namespaces.ContainsKey |> not // TODO: DISALLOW TWO ALIAS WITH THE SAME NAME?
                if validAlias && Namespaces.ContainsKey opened then ns.TryAddOpenDirective source (opened, openedRange) (alias, aliasRange.ValueOr openedRange)
                elif validAlias then [| openedRange |> QsCompilerDiagnostic.Error (ErrorCode.UnknownNamespace, [opened.Value]) |]
                else [| aliasRange.ValueOr openedRange |> QsCompilerDiagnostic.Error (ErrorCode.InvalidNamespaceAliasName, [alias]) |]
            | true, _ -> ArgumentException "given source file is not listed as source of the given namespace" |> raise
            | false, _ -> ArgumentException "no such namespace exists" |> raise        
        finally syncRoot.ExitWriteLock()

    /// Given a qualified callable name, returns the corresponding CallableDeclarationHeader as Value, 
    /// if the qualifier can be resolved within the given parent namespace and source file, and such a callable indeed exists. 
    /// If the callable is not defined an any of the references and the source file containing the callable declaration is specified (i.e. declSource is Some), 
    /// throws the corresponding exception if no such callable exists in that file. 
    /// Throws an ArgumentException if the qualifier does not correspond to a known namespace and the given parent namespace does not exist.
    member private this.TryGetCallableHeader (callableName : QsQualifiedName, declSource) (nsName, source) =
        let BuildHeader fullName (source, kind, declaration : Resolution<_,_>) = 
            let fallback () = (declaration.Defined, [CallableInformation.Invalid]) |> this.ResolveCallableSignature (kind, callableName, source, declaration.Modifiers) |> fst
            let resolvedSignature, argTuple = declaration.Resolved.ValueOrApply fallback
            {
                Kind = kind
                QualifiedName = fullName
                Attributes = declaration.ResolvedAttributes
                Modifiers = declaration.Modifiers
                SourceFile = source
                Position = DeclarationHeader.Offset.Defined declaration.Position
                SymbolRange = DeclarationHeader.Range.Defined declaration.Range 
                Signature = resolvedSignature
                ArgumentTuple = argTuple
                Documentation = declaration.Documentation
            }
        syncRoot.EnterReadLock()
        try match (nsName, source) |> TryResolveQualifier callableName.Namespace with
            | None -> NotFound
            | Some ns -> ns.CallablesInReferencedAssemblies.TryGetValue callableName.Name |> function
                | true, cDecl ->
                    if IsDeclarationAccessible false (nsName = cDecl.QualifiedName.Namespace) cDecl.Modifiers
                    then Found cDecl
                    else Inaccessible
                | false, _ -> declSource |> function
                    | Some source -> 
                        let kind, decl = ns.CallableInSource source callableName.Name // ok only because/if we have covered that the callable is not in a reference!
                        if IsDeclarationAccessible true (nsName = ns.Name) decl.Modifiers
                        then BuildHeader {callableName with Namespace = ns.Name} (source, kind, decl) |> Found
                        else Inaccessible
                    | None -> 
                        ns.CallablesDefinedInAllSources().TryGetValue callableName.Name |> function
                        | true, (source, (kind, decl)) ->
                            if IsDeclarationAccessible true (nsName = ns.Name) decl.Modifiers
                            then BuildHeader {callableName with Namespace = ns.Name} (source, kind, decl) |> Found
                            else Inaccessible
                        | false, _ -> NotFound
        finally syncRoot.ExitReadLock()

    /// Given a qualified callable name, returns the corresponding CallableDeclarationHeader as Value, 
    /// if the qualifier can be resolved within the given parent namespace and source file, and such a callable indeed exists. 
    /// Throws an ArgumentException if the qualifier does not correspond to a known namespace and the given parent namespace does not exist.
    member this.TryGetCallable (callableName : QsQualifiedName) (nsName, source) = this.TryGetCallableHeader (callableName, None) (nsName, source)

    /// If the given callable name can be uniquely resolved within the given namespace and source file, 
    /// returns the CallableDeclarationHeader with the information on that callable as Value.
    /// Returns Null as well as a list with namespaces containing a callable with that name if this is not the case. 
    member this.TryResolveAndGetCallable cName (nsName, source) = 
        syncRoot.EnterReadLock()
        try
            let allResolutions = (nsName, source) |> PossibleResolutions (fun ns -> ns.TryFindCallable cName)
            let accessibleResolutions =
                allResolutions |> List.filter (fun (declaredNS, (_, _, sameAssembly, modifiers)) ->
                    IsDeclarationAccessible sameAssembly (declaredNS = nsName) modifiers)
            match accessibleResolutions with
            | [] -> if not <| List.isEmpty allResolutions then Inaccessible else NotFound
            | [(declNS, (declSource, _, _, _))] ->
                this.TryGetCallableHeader ({Namespace = declNS; Name = cName}, Some declSource) (nsName, source) |> function
                | Found decl -> Found decl
                | _ ->
                    QsCompilerError.Raise "Expected to find the header corresponding to a possible resolution"
                    NotFound
            | _ -> accessibleResolutions.Select fst |> Ambiguous
        finally syncRoot.ExitReadLock()

    /// Given a qualified type name, returns the corresponding TypeDeclarationHeader as Value, 
    /// if the qualifier can be resolved within the given parent namespace and source file, and such a type indeed exists. 
    /// If the type is not defined an any of the references and the source file containing the type declaration is specified (i.e. declSource is Some), 
    /// throws the corresponding exception if no such type exists in that file. 
    /// Throws an ArgumentException if the qualifier does not correspond to a known namespace and the given parent namespace does not exist.
    member private this.TryGetTypeHeader (typeName : QsQualifiedName, declSource) (nsName, source) =
        let BuildHeader fullName (source, declaration) =
            let fallback () =
                declaration.Defined |> this.ResolveTypeDeclaration (typeName, source, declaration.Modifiers) |> fst
            let underlyingType, items = declaration.Resolved.ValueOrApply fallback
            {
                QualifiedName = fullName
                Attributes = declaration.ResolvedAttributes
                Modifiers = declaration.Modifiers
                SourceFile = source
                Position = DeclarationHeader.Offset.Defined declaration.Position
                SymbolRange = DeclarationHeader.Range.Defined declaration.Range
                Type = underlyingType
                TypeItems = items
                Documentation = declaration.Documentation
            }
        syncRoot.EnterReadLock()
        try
            match (nsName, source) |> TryResolveQualifier typeName.Namespace with 
            | None -> NotFound
            | Some ns -> ns.TypesInReferencedAssemblies.TryGetValue typeName.Name |> function
                | true, tDecl ->
                    if IsDeclarationAccessible false (nsName = tDecl.QualifiedName.Namespace) tDecl.Modifiers
                    then Found tDecl
                    else Inaccessible
                | false, _ -> declSource |> function
                    | Some source ->
                        let decl = ns.TypeInSource source typeName.Name // ok only because/if we have covered that the type is not in a reference!
                        if IsDeclarationAccessible true (nsName = ns.Name) decl.Modifiers
                        then BuildHeader {typeName with Namespace = ns.Name} (source, decl) |> Found
                        else Inaccessible
                    | None -> 
                        ns.TypesDefinedInAllSources().TryGetValue typeName.Name |> function
                        | true, (source, decl) ->
                            if IsDeclarationAccessible true (nsName = ns.Name) decl.Modifiers
                            then BuildHeader {typeName with Namespace = ns.Name} (source, decl) |> Found
                            else Inaccessible
                        | false, _ -> NotFound
        finally syncRoot.ExitReadLock()

    /// Given a qualified type name, returns the corresponding TypeDeclarationHeader as Value, 
    /// if the qualifier can be resolved within the given parent namespace and source file, and such a type indeed exists. 
    /// Throws an ArgumentException if the qualifier does not correspond to a known namespace and the given parent namespace does not exist.
    member this.TryGetType (typeName : QsQualifiedName) (nsName, source) = this.TryGetTypeHeader (typeName, None) (nsName, source)

    /// If the given type name can be uniquely resolved within the given namespace and source file, 
    /// returns the TypeDeclarationHeader with the information on that type as Value.
    /// Returns Null as well as a list with namespaces containing a type with that name if this is not the case. 
    member this.TryResolveAndGetType tName (nsName, source) = 
        syncRoot.EnterReadLock()
        try
            let allResolutions = (nsName, source) |> PossibleResolutions (fun ns -> ns.TryFindType tName)
            let accessibleResolutions =
                allResolutions |> List.filter (fun (declaredNS, (_, _, sameAssembly, modifiers)) ->
                    IsDeclarationAccessible sameAssembly (declaredNS = nsName) modifiers)
            match accessibleResolutions with
            | [] -> if not <| List.isEmpty allResolutions then Inaccessible else NotFound
            | [(declNS, (declSource, _, _, _))] ->
                this.TryGetTypeHeader ({Namespace = declNS; Name = tName}, Some declSource) (nsName, source) |> function
                | Found decl -> Found decl
                | _ ->
                    QsCompilerError.Raise "Expected to find the header corresponding to a possible resolution"
                    NotFound
            | _ -> accessibleResolutions.Select fst |> Ambiguous
        finally syncRoot.ExitReadLock()


    /// Returns the fully qualified namespace name of the given namespace alias (short name). If the alias is already a fully qualified name, 
    /// returns the name unchanged. Returns null if no such name exists within the given parent namespace and source file.
    /// Throws an ArgumentException if the given parent namespace does not exist.
    member this.TryResolveNamespaceAlias alias (nsName, source) =
        syncRoot.EnterReadLock()
        try match TryResolveQualifier alias (nsName, source) with
            | None -> null
            | Some ns -> ns.Name.Value
        finally syncRoot.ExitReadLock()

    /// Returns the names of all namespaces in which a callable with the given name is declared, including private and
    /// internal declarations.
    member this.NamespacesContainingCallable cName =
        // FIXME: we need to handle the case where a callable/type with the same qualified name is declared in several references!
        //
        // TODO: It may be useful to limit the results to only declarations which are accessible from a given location
        // (e.g., for code actions in the language server).
        syncRoot.EnterReadLock()
        try let tryFindCallable (ns : Namespace) = ns.TryFindCallable cName |> QsNullable<_>.Map (fun _ -> ns.Name)
            (Namespaces.Values |> QsNullable<_>.Choose tryFindCallable).ToImmutableArray()
        finally syncRoot.ExitReadLock()

    /// Returns the names of all namespaces in which a type with the given name is declared, including private and
    /// internal declarations.
    member this.NamespacesContainingType tName =
        // FIXME: we need to handle the case where a callable/type with the same qualified name is declared in several references!
        //
        // TODO: It may be useful to limit the results to only declarations which are accessible from a given location
        // (e.g., for code actions in the language server).
        syncRoot.EnterReadLock()
        try let tryFindType (ns : Namespace) = ns.TryFindType tName |> QsNullable<_>.Map (fun _ -> ns.Name)
            (Namespaces.Values |> QsNullable<_>.Choose tryFindType).ToImmutableArray()
        finally syncRoot.ExitReadLock()

    /// Returns the name of all namespaces declared in source files or referenced assemblies.
    member this.NamespaceNames () =
        syncRoot.EnterReadLock()
        try ImmutableArray.CreateRange Namespaces.Keys
        finally syncRoot.ExitReadLock()

    /// Returns true if the given namespace name exists in the symbol table.
    member this.NamespaceExists nsName =
        Namespaces.ContainsKey nsName


    /// Generates a hash for a resolved type. Does not incorporate any positional information.
    static member internal TypeHash (t : ResolvedType) = t.Resolution |> function
        | QsTypeKind.ArrayType b                    -> hash (0, NamespaceManager.TypeHash b)
        | QsTypeKind.TupleType ts                   -> hash (1, (ts |> Seq.map NamespaceManager.TypeHash |> Seq.toList))
        | QsTypeKind.UserDefinedType udt            -> hash (2, udt.Namespace.Value, udt.Name.Value)
        | QsTypeKind.TypeParameter tp               -> hash (3, tp.Origin.Namespace.Value, tp.Origin.Name.Value, tp.TypeName.Value)
        | QsTypeKind.Operation ((inT, outT), fList) -> hash (4, (inT |> NamespaceManager.TypeHash), (outT |> NamespaceManager.TypeHash), (fList |> JsonConvert.SerializeObject))
        | QsTypeKind.Function (inT, outT)           -> hash (5, (inT |> NamespaceManager.TypeHash), (outT |> NamespaceManager.TypeHash))
        | kind                                      -> JsonConvert.SerializeObject kind |> hash

    /// Generates a hash for a typed expression. Does not incorporate any positional information.
    static member internal ExpressionHash (ex : TypedExpression) = ex.Expression |> function
        | StringLiteral (s, _)              -> hash (6, s)
        | ValueTuple vs                     -> hash (7, (vs |> Seq.map NamespaceManager.ExpressionHash |> Seq.toList))
        | ValueArray vs                     -> hash (8, (vs |> Seq.map NamespaceManager.ExpressionHash |> Seq.toList))
        | NewArray (bt, idx)                -> hash (9, NamespaceManager.TypeHash bt, NamespaceManager.ExpressionHash idx)
        | Identifier (GlobalCallable c, _)  -> hash (10, c.Namespace.Value, c.Name.Value)
        | kind                              -> JsonConvert.SerializeObject kind |> hash

    /// Generates a hash containing full type information about all entries in the given source file.
    /// All entries in the source file have to be fully resolved beforehand.
    /// That hash does not contain any information about the imported namespaces, positional information, or about any documentation.
    /// Returns the generated hash as well as a separate hash providing information about the imported namespaces.
    /// Throws an InvalidOperationException if the given source file contains unresolved entries. 
    member this.HeaderHash source = 
        let invalidOperationEx = InvalidOperationException "everything needs to be resolved before constructing the HeaderString"
        if not this.ContainsResolutions then invalidOperationEx |> raise
        let inconsistentStateException () = 
            QsCompilerError.Raise "contains unresolved entries despite supposedly being resolved"
            invalidOperationEx |> raise

        let attributesHash (attributes : QsDeclarationAttribute seq) = 
            let getHash arg (id : UserDefinedType) = hash (id.Namespace.Value, id.Name.Value, NamespaceManager.ExpressionHash arg)
            attributes |> QsNullable<_>.Choose (fun att -> att.TypeId |> QsNullable<_>.Map (getHash att.Argument)) |> Seq.toList
        let callableHash (kind, (signature,_), specs, attributes : QsDeclarationAttribute seq) =
            let signatureHash (signature : ResolvedSignature) = 
                let argStr = signature.ArgumentType |> NamespaceManager.TypeHash
                let reStr = signature.ReturnType |> NamespaceManager.TypeHash
                let nameOrInvalid = function | InvalidName -> InvalidName |> JsonConvert.SerializeObject | ValidName sym -> sym.Value
                let typeParams = signature.TypeParameters |> Seq.map nameOrInvalid |> Seq.toList
                hash (argStr, reStr, typeParams)
            let specsStr =
                let genHash (gen : ResolvedGenerator) = 
                    let tArgs = gen.TypeArguments |> QsNullable<_>.Map (fun tArgs -> tArgs |> Seq.map NamespaceManager.TypeHash |> Seq.toList)
                    hash (gen.Directive, hash tArgs)
                let kinds, gens = specs |> Seq.sort |> Seq.toList |> List.unzip
                hash (kinds, gens |> List.map genHash)
            hash (kind, specsStr, signatureHash signature, attributes |> attributesHash)
        let typeHash (t, typeItems : QsTuple<QsTypeItem>, attributes) = 
            let getItemHash (itemName, itemType) = hash (itemName, NamespaceManager.TypeHash itemType)
            let namedItems = typeItems.Items |> Seq.choose (function | Named item -> Some item | _ -> None)
            let itemHashes = namedItems.Select (fun d -> d.VariableName, d.Type) |> Seq.map getItemHash
            hash (NamespaceManager.TypeHash t, itemHashes |> Seq.toList, attributes |> attributesHash)

        syncRoot.EnterReadLock()
        try let relevantNamespaces = 
                Namespaces.Values 
                |> Seq.filter (fun ns -> ns.Sources.Contains source) 
                |> Seq.sortBy (fun ns -> ns.Name) |> Seq.toList
            let callables = relevantNamespaces |> Seq.collect (fun ns -> 
                let inSource = ns.CallablesDefinedInSource source |> Seq.sortBy (fun (cName, _) -> cName.Value) 
                inSource |> Seq.map (fun (cName, (kind, signature)) -> 
                    let specs = ns.SpecializationsDefinedInAllSources cName |> Seq.map (fun (kind, (_, resolution)) -> 
                        kind, resolution.Resolved.ValueOrApply inconsistentStateException)
                    let resolved = signature.Resolved.ValueOrApply inconsistentStateException
                    ns.Name.Value, cName.Value, (kind, resolved, specs, signature.ResolvedAttributes)))
            let types = relevantNamespaces |> Seq.collect (fun ns -> 
                let inSources = ns.TypesDefinedInSource source |> Seq.sortBy (fun (tName,_) -> tName.Value)
                inSources |> Seq.map (fun (tName, qsType) ->
                    let resolved, resItems = qsType.Resolved.ValueOrApply inconsistentStateException
                    ns.Name.Value, tName.Value, (resolved, resItems, qsType.ResolvedAttributes)))
            let imports = relevantNamespaces |> Seq.collect (fun ns -> 
                ns.ImportedNamespaces source |> Seq.sortBy (fun x -> x.Value) |> Seq.map (fun opened -> ns.Name.Value, opened.Value)) 

            let callablesHash = callables |> Seq.map (fun (ns, name, c) -> (ns, name, callableHash c)) |> Seq.toList |> hash
            let typesHash = types |> Seq.map (fun (ns, name, t) -> ns, name, typeHash t) |> Seq.toList |> hash
            let importsHash = imports |> Seq.toList |> hash
            hash (callablesHash, typesHash), importsHash
        finally syncRoot.ExitReadLock() 

