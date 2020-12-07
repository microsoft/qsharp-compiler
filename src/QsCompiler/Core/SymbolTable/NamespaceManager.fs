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
open Microsoft.Quantum.QsCompiler.ReservedKeywords
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Utils
open Newtonsoft.Json

/// Threadsafe class for global symbol management.
///
/// Takes a lookup for all callables and for all types declared within one of the assemblies referenced by the
/// compilation unit this namespace manager belongs to. The key for the given lookups is the name of the namespace the
/// declarations belongs to.
///
/// The namespace manager takes access modifiers into consideration when resolving symbols. Some methods bypass this
/// (e.g., when returning a list of all declarations). Individual methods document whether they follow or ignore access
/// modifiers.
type NamespaceManager(syncRoot: IReaderWriterLock,
                      callablesInRefs: IEnumerable<CallableDeclarationHeader>,
                      specializationsInRefs: IEnumerable<SpecializationDeclarationHeader * SpecializationImplementation>,
                      typesInRefs: IEnumerable<TypeDeclarationHeader>,
                      runtimeCapability,
                      isExecutable) =
    // This class itself does not use any concurrency,
    // so anything that is accessible within the class only does not apply any locks.
    // IMPORTANT: the syncRoot is intentionally not exposed externally, since with this class supporting mutation
    // access to that lock needs to be coordinated by whatever coordinates the mutations.

    /// the version number is incremented whenever a write operation is performed
    let mutable versionNumber = 0
    /// handle to avoid unnecessary work
    let mutable containsResolutions = true // initialized without any entries - hence it is resolved

    /// dictionary with all declared namespaces
    /// the key is the name of the namespace
    let Namespaces =
        let namespaces = Dictionary<_, _>()

        let callables =
            callablesInRefs.ToLookup(fun header -> header.QualifiedName.Namespace)

        let specializations =
            specializationsInRefs.ToLookup(fun (header, _) -> header.Parent.Namespace)

        let types =
            typesInRefs.ToLookup(fun header -> header.QualifiedName.Namespace)

        let getKeys (lookup: ILookup<_, _>) =
            lookup |> Seq.map (fun group -> group.Key)

        let namespacesInRefs =
            (getKeys callables)
                .Concat(getKeys specializations)
                .Concat(getKeys types)
            |> Seq.distinct

        for nsName in namespacesInRefs do
            namespaces.Add
                (nsName, new Namespace(nsName, [], callables.[nsName], specializations.[nsName], types.[nsName]))

        namespaces

    /// Returns the full name of all entry points currently resolved in any of the tracked source files.
    let GetEntryPoints () =
        let entryPoints =
            Namespaces.Values
            |> Seq.collect (fun ns ->
                ns.CallablesDefinedInAllSources()
                |> Seq.choose (fun kvPair ->
                    let cName, (source, (_, decl)) = kvPair.Key, kvPair.Value

                    if decl.ResolvedAttributes
                       |> Seq.exists BuiltIn.MarksEntryPoint then
                        Some({ Namespace = ns.Name; Name = cName }, source)
                    else
                        None))

        entryPoints.ToImmutableArray()

    /// <summary>
    /// If a namespace with the given name exists, returns that namespace
    /// as well as all imported namespaces for that namespace in the given source file.
    /// Filters namespaces that have been imported under a different name.
    /// Filters all unknown namespaces, i.e. imported namespaces that are not managed by this namespace manager.
    /// </summary>
    /// <exception cref="SymbolNotFoundException">
    /// A namespace with the given name was not found, or the source file does not contain the namespace.
    /// </exception>
    let OpenNamespaces (nsName, source) =
        let isKnownAndNotAliased (kv: KeyValuePair<_, _>) =
            if kv.Value <> null then
                None
            else
                Namespaces.TryGetValue kv.Key
                |> function
                | true, ns -> Some ns
                | false, _ -> None

        match Namespaces.TryGetValue nsName with
        | true, ns ->
            ns,
            ns.ImportedNamespaces source
            |> Seq.choose isKnownAndNotAliased
            |> Seq.toList
        | false, _ ->
            SymbolNotFoundException "The namespace with the given name was not found."
            |> raise

    /// Calls the resolver function on each namespace opened within the given namespace name and source file, and
    /// attempts to find an unambiguous resolution.
    let resolveInOpenNamespaces resolver (nsName, source) =
        let resolveWithNsName (ns: Namespace) =
            resolver ns
            |> ResolutionResult.Map(fun value -> (ns.Name, value))

        let currentNs, importedNs = OpenNamespaces(nsName, source)

        seq {
            yield resolveWithNsName currentNs

            yield
                Seq.map resolveWithNsName importedNs
                |> ResolutionResult.TryAtMostOne fst
        }
        |> ResolutionResult.TryFirstBest

    /// <summary>
    /// Given a qualifier for a symbol name, returns the corresponding namespace as Some
    /// if such a namespace or such a namespace short name within the given parent namespace and source file exists.
    /// </summary>
    /// <exception cref="SymbolNotFoundException">
    /// The qualifier's namespace or the parent namespace and source file were not found.
    /// </exception>
    let TryResolveQualifier qualifier (nsName, source) =
        let parentNs () =
            Namespaces.TryGetValue nsName
            |> tryToOption
            |> Option.defaultWith (fun () ->
                SymbolNotFoundException "The namespace with the given name was not found."
                |> raise)

        let nsAlias =
            Namespaces.TryGetValue
            >> tryToOption
            >> Option.orElseWith (fun () ->
                QsCompilerError.Raise "The corresponding namespace for a namespace short name could not be found."
                None)

        Namespaces.TryGetValue qualifier
        |> tryToOption
        |> Option.orElseWith (fun () ->
            (parentNs().NamespaceShortNames source).TryGetValue qualifier
            |> tryToOption
            |> Option.bind nsAlias)

    /// <summary>
    /// Returns the possible qualifications for the built-in type or callable used in the given namespace and source.
    /// where the given source may either be the name of a source file or of a referenced assembly.
    /// If the given source is not listed as source file of the namespace, assumes that the source if one of the references
    /// and returns the namespace name of the given built in type or callable as only possible qualification.
    /// </summary>
    /// <exception cref="SymbolNotFoundException">The namespace with the given name was not found.</exception>
    let PossibleQualifications (nsName, source) (builtIn: BuiltIn) =
        match Namespaces.TryGetValue nsName with
        | true, ns when ns.Sources.Contains source ->
            match (ns.ImportedNamespaces source).TryGetValue builtIn.FullName.Namespace with
            | true, null when not
                                  (ns.TryFindType builtIn.FullName.Name
                                   |> ResolutionResult.IsAccessible)
                              || nsName = builtIn.FullName.Namespace -> [ ""; builtIn.FullName.Namespace ]
            | true, null -> [ builtIn.FullName.Namespace ] // the built-in type or callable is shadowed
            | true, alias -> [ alias; builtIn.FullName.Namespace ]
            | false, _ -> [ builtIn.FullName.Namespace ]
        | true, _ -> [ builtIn.FullName.Namespace ]
        | false, _ ->
            SymbolNotFoundException "The namespace with the given name was not found."
            |> raise

    /// <summary>
    /// Given the qualified or unqualified name of a type used within the given parent namespace and source file,
    /// determines if such a type is accessible, and returns its namespace name and the source file or referenced
    /// assembly in which it is defined as Some if it is.
    /// </summary>
    /// <returns>
    /// 1. None if no such type exists, the type is inaccessible, or if the type name is unqualified and ambiguous.
    /// 2. An array of diagnostics.
    /// </returns>
    /// <exception cref="SymbolNotFoundException">
    /// A namespace with the given parent name was not found, or the source file does not contain the parent namespace.
    /// </exception>
    let tryResolveTypeName (parentNS, source) ((nsName, symName), symRange: QsNullable<Range>) =
        let checkQualificationForDeprecation qual =
            BuiltIn.Deprecated
            |> PossibleQualifications(parentNS, source)
            |> Seq.contains qual

        let success ns declSource deprecation access errs =
            let warnings =
                SymbolResolution.GenerateDeprecationWarning
                    ({ Namespace = ns; Name = symName }, symRange.ValueOr Range.Zero)
                    deprecation

            Some
                ({ Namespace = ns
                   Name = symName
                   Range = symRange },
                 declSource,
                 access),
            Array.append errs warnings

        let error code args =
            None, [| QsCompilerDiagnostic.Error (code, args) (symRange.ValueOr Range.Zero) |]

        let findUnqualified () =
            match resolveInOpenNamespaces (fun ns -> ns.TryFindType(symName, checkQualificationForDeprecation))
                      (parentNS, source) with
            | Found (nsName, (declSource, deprecation, access)) -> success nsName declSource deprecation access [||]
            | Ambiguous namespaces ->
                let names = String.Join(", ", namespaces)
                error ErrorCode.AmbiguousType [ symName; names ]
            | Inaccessible -> error ErrorCode.InaccessibleType [ symName ]
            | NotFound -> error ErrorCode.UnknownType [ symName ]

        let findQualified (ns: Namespace) qualifier =
            match ns.TryFindType(symName, checkQualificationForDeprecation) with
            | Found (declSource, deprecation, access) -> success ns.Name declSource deprecation access [||]
            | Ambiguous _ ->
                QsCompilerError.Raise "Qualified name should not be ambiguous"
                Exception() |> raise
            | Inaccessible -> error ErrorCode.InaccessibleTypeInNamespace [ symName; qualifier ]
            | NotFound -> error ErrorCode.UnknownTypeInNamespace [ symName; qualifier ]

        match nsName with
        | None -> findUnqualified ()
        | Some qualifier ->
            match TryResolveQualifier qualifier (parentNS, source) with
            | None -> error ErrorCode.UnknownNamespace [ qualifier ]
            | Some ns -> findQualified ns qualifier

    /// Fully (i.e. recursively) resolves the given Q# type used within the given parent in the given source file. The
    /// resolution consists of replacing all unqualified names for user defined types by their qualified name.
    ///
    /// Generates an array of diagnostics for the cases where no user defined type of the specified name (qualified or
    /// unqualified) can be found, or if the type is inaccessible. In that case, resolves the user defined type by
    /// replacing it with the Q# type denoting an invalid type.
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
    ///
    /// May throw an exception if the given parent and/or source file is inconsistent with the defined declarations.
    /// Throws a NotSupportedException if the QsType to resolve contains a MissingType.
    let resolveType (parent: QsQualifiedName, tpNames, source) qsType checkUdt =
        let processUDT =
            tryResolveTypeName (parent.Namespace, source)
            >> function
            | Some (udt, _, access), errs -> UserDefinedType udt, Array.append errs (checkUdt (udt, access))
            | None, errs -> InvalidType, errs

        let processTP (symName, symRange) =
            if tpNames |> Seq.contains symName then
                TypeParameter
                    { Origin = parent
                      TypeName = symName
                      Range = symRange },
                [||]
            else
                InvalidType,
                [| symRange.ValueOr Range.Zero
                   |> QsCompilerDiagnostic.Error(ErrorCode.UnknownTypeParameterName, [ symName ]) |]

        syncRoot.EnterReadLock()

        try
            SymbolResolution.ResolveType (processUDT, processTP) qsType
        finally
            syncRoot.ExitReadLock()

    /// Compares the accessibility of the parent declaration with the accessibility of the UDT being referenced. If the
    /// accessibility of a referenced type is less than the accessibility of the parent, returns a diagnostic using the
    /// given error code. Otherwise, returns an empty array.
    let checkUdtAccessibility code (parent, parentAccess) (udt: UserDefinedType, udtAccess) =
        if parentAccess = DefaultAccess
           && udtAccess = Internal then
            [| QsCompilerDiagnostic.Error (code, [ udt.Name; parent ]) (udt.Range.ValueOr Range.Zero) |]
        else
            [||]

    /// <summary>
    /// Checks whether the given parent and declaration should recognized as an entry point.
    /// Verifies the entry point signature and arguments, and generates and returns suitable diagnostics.
    /// The given offset and range are used to generate diagnostics and should correspond to location of the entry point attribute.
    /// </summary>
    /// <returns>
    /// True if the declaration should be recognized as entry point, which may be the case even if errors have been generated.
    /// </returns>
    /// <exception cref="SymbolNotFoundException">The parent namespace with the given name was not found.</exception>
    let validateEntryPoint (parent: QsQualifiedName) (offset, range) (decl: Resolution<'T, _>) =
        let orDefault (range: QsNullable<_>) = range.ValueOr Range.Zero
        let errs = new List<_>()

        match box decl.Defined with
        | :? CallableSignature as signature when not (signature.TypeParameters.Any()) ->

            // verify that the entry point has only a default body specialization
            let hasCharacteristics =
                signature.Characteristics.Characteristics
                |> function
                | EmptySet
                | InvalidSetExpr -> false
                | _ -> true

            match Namespaces.TryGetValue parent.Namespace with
            | false, _ ->
                SymbolNotFoundException "The parent namespace with the given name was not found."
                |> raise
            | true, ns ->
                let specializations =
                    ns.SpecializationsDefinedInAllSources parent.Name

                if hasCharacteristics
                   || specializations.Any(fst >> (<>) QsBody) then
                    errs.Add
                        (decl.Position,
                         signature.Characteristics.Range.ValueOr decl.Range
                         |> QsCompilerDiagnostic.Error(ErrorCode.InvalidEntryPointSpecialization, []))

            // validate entry point argument and return type
            let rec validateArgAndReturnTypes (isArg, inArray) (t: QsType) =
                match t.Type with
                | Qubit ->
                    (decl.Position,
                     t.Range
                     |> orDefault
                     |> QsCompilerDiagnostic.Error(ErrorCode.QubitTypeInEntryPointSignature, []))
                    |> Seq.singleton
                | UserDefinedType _ ->
                    (decl.Position,
                     t.Range
                     |> orDefault
                     |> QsCompilerDiagnostic.Error(ErrorCode.UserDefinedTypeInEntryPointSignature, []))
                    |> Seq.singleton
                | QsTypeKind.Operation _ ->
                    (decl.Position,
                     t.Range
                     |> orDefault
                     |> QsCompilerDiagnostic.Error(ErrorCode.CallableTypeInEntryPointSignature, []))
                    |> Seq.singleton
                | QsTypeKind.Function _ ->
                    (decl.Position,
                     t.Range
                     |> orDefault
                     |> QsCompilerDiagnostic.Error(ErrorCode.CallableTypeInEntryPointSignature, []))
                    |> Seq.singleton
                | TupleType ts when ts.Length > 1 && isArg ->
                    (decl.Position,
                     t.Range
                     |> orDefault
                     |> QsCompilerDiagnostic.Error(ErrorCode.InnerTupleInEntryPointArgument, []))
                    |> Seq.singleton
                | TupleType ts ->
                    ts
                    |> Seq.collect (validateArgAndReturnTypes (isArg, inArray))
                | ArrayType _ when isArg && inArray ->
                    (decl.Position,
                     t.Range
                     |> orDefault
                     |> QsCompilerDiagnostic.Error(ErrorCode.ArrayOfArrayInEntryPointArgument, []))
                    |> Seq.singleton
                | ArrayType bt -> validateArgAndReturnTypes (isArg, true) bt
                | _ -> Seq.empty

            let validateArgAndReturnTypes isArg = validateArgAndReturnTypes (isArg, false)

            let inErrs =
                signature.Argument.Items.Select snd
                |> Seq.collect (validateArgAndReturnTypes true)

            let outErrs =
                signature.ReturnType
                |> validateArgAndReturnTypes false

            let signatureErrs = inErrs.Concat outErrs
            errs.AddRange signatureErrs

            // currently, only return values of type Result, Result[], and tuples thereof are supported on quantum processors
            if runtimeCapability <> FullComputation then
                let invalid =
                    signature.ReturnType.ExtractAll(fun t ->
                        t.Type
                        |> function
                        | Result
                        | ArrayType _
                        | TupleType _
                        | InvalidType -> Seq.empty
                        | _ -> Seq.singleton t)

                if invalid.Any() then
                    errs.Add
                        (decl.Position,
                         signature.ReturnType.Range
                         |> orDefault
                         |> QsCompilerDiagnostic.Warning(WarningCode.NonResultTypeReturnedInEntryPoint, []))

            // validate entry point argument names
            let asCommandLineArg (str: string) =
                str.ToLowerInvariant() |> String.filter ((<>) '_')

            let reservedCommandLineArgs =
                CommandLineArguments.ReservedArguments.Concat CommandLineArguments.ReservedArgumentAbbreviations
                |> Seq.map asCommandLineArg
                |> Seq.toArray

            let nameAndRange (sym: QsSymbol) =
                sym.Symbol
                |> function
                | Symbol name -> Some(asCommandLineArg name, sym.Range)
                | _ -> None

            let simplifiedArgNames =
                signature.Argument.Items.Select fst
                |> Seq.choose nameAndRange
                |> Seq.toList

            let verifyArgument i (arg, range: QsNullable<_>) =
                if i > 0
                   && simplifiedArgNames.[..i - 1]
                      |> Seq.map fst
                      |> Seq.contains arg then
                    errs.Add
                        (decl.Position,
                         range.ValueOr decl.Range
                         |> QsCompilerDiagnostic.Error(ErrorCode.DuplicateEntryPointArgumentName, []))
                elif reservedCommandLineArgs.Contains arg then
                    errs.Add
                        (decl.Position,
                         range.ValueOr decl.Range
                         |> QsCompilerDiagnostic.Warning(WarningCode.ReservedEntryPointArgumentName, []))


            simplifiedArgNames |> List.iteri verifyArgument

            // check that there is no more than one entry point, and no entry point if the project is not executable
            if signatureErrs.Any() then
                false, errs
            elif not isExecutable then
                errs.Add
                    (offset,
                     range
                     |> orDefault
                     |> QsCompilerDiagnostic.Warning(WarningCode.EntryPointInLibrary, []))

                false, errs
            else
                GetEntryPoints()
                |> Seq.tryHead
                |> function
                | None -> isExecutable, errs
                | Some (epName, epSource) ->
                    let msgArgs =
                        [ sprintf "%s.%s" epName.Namespace epName.Name
                          epSource ]

                    errs.Add
                        (offset,
                         range
                         |> orDefault
                         |> QsCompilerDiagnostic.Error(ErrorCode.OtherEntryPointExists, msgArgs))

                    false, errs
        | _ ->
            errs.Add
                (offset,
                 range
                 |> orDefault
                 |> QsCompilerDiagnostic.Error(ErrorCode.InvalidEntryPointPlacement, []))

            false, errs


    /// <summary>
    /// Given the name of the namespace as well as the source file in which the attribute occurs, resolves the given
    /// attribute.
    ///
    /// Generates suitable diagnostics if a suitable attribute cannot be found or is not accessible, if the attribute
    /// argument contains expressions that are not supported, or if the resolved argument type does not match the
    /// expected argument type.
    /// </summary>
    /// <returns>The resolved attribute as well as the generated diagnostics.</returns>
    /// <exception cref="SymbolNotFoundException">
    /// A namespace with the given parent name was not found, or the source file does not contain the parent namespace.
    /// </exception>
    /// <remarks>
    /// The TypeId in the resolved attribute is set to Null if the unresolved Id is not a valid identifier or if the
    /// correct attribute cannot be determined, and is set to the corresponding type identifier otherwise.
    /// </remarks>
    member private this.ResolveAttribute (parentNS, source) attribute =
        let getAttribute ((nsName, symName), symRange) =
            match tryResolveTypeName (parentNS, source) ((nsName, symName), symRange) with
            | Some (udt, declSource, _), errs -> // declSource may be the name of an assembly!
                let fullName = sprintf "%s.%s" udt.Namespace udt.Name

                let validQualifications =
                    BuiltIn.Attribute
                    |> PossibleQualifications(udt.Namespace, declSource)

                match Namespaces.TryGetValue udt.Namespace with
                | true, ns ->
                    ns.TryGetAttributeDeclaredIn declSource (udt.Name, validQualifications)
                    |> function
                    | None ->
                        None,
                        [| symRange.ValueOr Range.Zero
                           |> QsCompilerDiagnostic.Error(ErrorCode.NotMarkedAsAttribute, [ fullName ]) |]
                    | Some argType -> Some(udt, argType), errs
                | false, _ ->
                    QsCompilerError.Raise "namespace for defined type not found"
                    None, errs
            | None, errs -> None, errs

        let resolved, msgs =
            SymbolResolution.ResolveAttribute getAttribute attribute

        resolved, msgs |> Array.map (fun m -> attribute.Position, m)

    /// <summary>
    /// Resolves the DefinedAttributes of the given declaration using ResolveAttribute and validates any entry points, if any.
    /// </summary>
    /// <returns>
    /// The resolved attributes as well as an array with diagnostics along with the declaration position.
    /// Each entry in the returned array of attributes is the resolution for the corresponding entry in the array of defined attributes.
    /// </returns>
    /// <exception cref="SymbolNotFoundException">The parent callable name was not found.</exception>
    member private this.ResolveAttributes (parent: QsQualifiedName, source) (decl: Resolution<'T, _>) =
        let isBuiltIn (builtIn: BuiltIn) (tId: UserDefinedType) =
            tId.Namespace = builtIn.FullName.Namespace
            && tId.Name = builtIn.FullName.Name

        let attr, msgs =
            decl.DefinedAttributes
            |> Seq.map (this.ResolveAttribute(parent.Namespace, source))
            |> Seq.toList
            |> List.unzip

        let errs = new List<_>(msgs |> Seq.collect id)
        let orDefault (range: QsNullable<_>) = range.ValueOr Range.Zero

        let validateAttributes (alreadyDefined: int list, resAttr) (att: QsDeclarationAttribute) =
            let returnInvalid msg =
                errs.AddRange msg
                alreadyDefined, { att with TypeId = Null } :: resAttr

            match att.TypeId with

            // known attribute
            | Value tId ->
                let attributeHash =
                    if tId |> isBuiltIn BuiltIn.Deprecated
                    then hash (tId.Namespace, tId.Name)
                    elif tId |> isBuiltIn BuiltIn.EnableTestingViaName
                    then hash (tId.Namespace, tId.Name)
                    else hash (tId.Namespace, tId.Name, NamespaceManager.ExpressionHash att.Argument)

                // the attribute is a duplication of another attribute on this declaration
                if alreadyDefined.Contains attributeHash then
                    (att.Offset,
                     tId.Range
                     |> orDefault
                     |> QsCompilerDiagnostic.Warning(WarningCode.DuplicateAttribute, [ tId.Name ]))
                    |> Seq.singleton
                    |> returnInvalid

                // the attribute marks an entry point
                elif tId |> isBuiltIn BuiltIn.EntryPoint then
                    let register, msgs =
                        validateEntryPoint parent (att.Offset, tId.Range) decl

                    errs.AddRange msgs

                    if register
                    then attributeHash :: alreadyDefined, att :: resAttr
                    else alreadyDefined, { att with TypeId = Null } :: resAttr

                // the attribute marks a unit test
                elif tId |> isBuiltIn BuiltIn.Test then
                    let isUnitToUnit (signature: CallableSignature) =
                        let isUnitType =
                            function
                            | Tuple _
                            | Missing -> false
                            | Item (itemType: QsType) -> itemType.Type = UnitType
                            | _ -> true // invalid type

                        match signature.Argument.Items |> Seq.toList with
                        | [] -> signature.ReturnType |> isUnitType
                        | [ (_, argType) ] ->
                            argType |> isUnitType
                            && signature.ReturnType |> isUnitType
                        | _ -> false

                    match box decl.Defined with
                    | :? CallableSignature as signature when signature |> isUnitToUnit
                                                             && not (signature.TypeParameters.Any()) ->
                        let arg =
                            att.Argument
                            |> AttributeAnnotation.NonInterpolatedStringArgument(fun ex -> ex.Expression)

                        let validExecutionTargets =
                            CommandLineArguments.BuiltInSimulators
                            |> Seq.map (fun x -> x.ToLowerInvariant())

                        if arg <> null
                           && (validExecutionTargets
                               |> Seq.contains (arg.ToLowerInvariant())
                               || SyntaxGenerator.FullyQualifiedName.IsMatch arg) then
                            attributeHash :: alreadyDefined, att :: resAttr
                        else
                            (att.Offset,
                             att.Argument.Range
                             |> orDefault
                             |> QsCompilerDiagnostic.Error(ErrorCode.InvalidExecutionTargetForTest, []))
                            |> Seq.singleton
                            |> returnInvalid
                    | _ ->
                        (att.Offset,
                         tId.Range
                         |> orDefault
                         |> QsCompilerDiagnostic.Error(ErrorCode.InvalidTestAttributePlacement, []))
                        |> Seq.singleton
                        |> returnInvalid

                // the attribute defines an alternative name for testing purposes
                elif tId |> isBuiltIn BuiltIn.EnableTestingViaName then
                    let arg =
                        att.Argument
                        |> AttributeAnnotation.NonInterpolatedStringArgument(fun ex -> ex.Expression)

                    match box decl.Defined with
                    | :? QsSpecializationGenerator ->
                        (att.Offset,
                         tId.Range
                         |> orDefault
                         |> QsCompilerDiagnostic.Error(ErrorCode.AttributeInvalidOnSpecialization, [ tId.Name ]))
                        |> Seq.singleton
                        |> returnInvalid
                    | _ when SyntaxGenerator.FullyQualifiedName.IsMatch arg ->
                        attributeHash :: alreadyDefined, att :: resAttr
                    | _ ->
                        (att.Offset,
                         tId.Range
                         |> orDefault
                         |> QsCompilerDiagnostic.Error(ErrorCode.ExpectingFullNameAsAttributeArgument, [ tId.Name ]))
                        |> Seq.singleton
                        |> returnInvalid

                // the attribute marks an attribute
                elif tId |> isBuiltIn BuiltIn.Attribute then
                    match box decl.Defined with
                    | :? CallableSignature ->
                        (att.Offset,
                         tId.Range
                         |> orDefault
                         |> QsCompilerDiagnostic.Error(ErrorCode.AttributeInvalidOnCallable, [ tId.Name ]))
                        |> Seq.singleton
                        |> returnInvalid
                    | :? QsSpecializationGenerator ->
                        (att.Offset,
                         tId.Range
                         |> orDefault
                         |> QsCompilerDiagnostic.Error(ErrorCode.AttributeInvalidOnSpecialization, [ tId.Name ]))
                        |> Seq.singleton
                        |> returnInvalid
                    | _ -> attributeHash :: alreadyDefined, att :: resAttr

                // the attribute marks a deprecation
                elif tId |> isBuiltIn BuiltIn.Deprecated then
                    match box decl.Defined with
                    | :? QsSpecializationGenerator ->
                        (att.Offset,
                         tId.Range
                         |> orDefault
                         |> QsCompilerDiagnostic.Error(ErrorCode.AttributeInvalidOnSpecialization, [ tId.Name ]))
                        |> Seq.singleton
                        |> returnInvalid
                    | _ -> attributeHash :: alreadyDefined, att :: resAttr

                // the attribute is another kind of attribute that requires no further verification at this point
                else
                    attributeHash :: alreadyDefined, att :: resAttr

            // unknown attribute, and an error has already been generated
            | _ -> alreadyDefined, att :: resAttr

        let resAttr =
            attr
            |> List.fold validateAttributes ([], [])
            |> snd

        resAttr.Reverse() |> ImmutableArray.CreateRange, errs.ToArray()

    /// Fully (i.e. recursively) resolves the given Q# type used within the given parent in the given source file. The
    /// resolution consists of replacing all unqualified names for user defined types by their qualified name.
    ///
    /// Generates an array of diagnostics for the cases where no user defined type of the specified name (qualified or
    /// unqualified) can be found or the type is inaccessible. In that case, resolves the user defined type by replacing
    /// it with the Q# type denoting an invalid type.
    ///
    /// Verifies that all used type parameters are defined in the given list of type parameters, and generates suitable
    /// diagnostics if they are not, replacing them by the Q# type denoting an invalid type. Returns the resolved type
    /// as well as an array with diagnostics.
    ///
    /// IMPORTANT: for performance reasons does *not* verify if the given the given parent and/or source file is
    /// consistent with the defined callables.
    ///
    /// May throw an exception if the given parent and/or source file is inconsistent with the defined declarations.
    /// Throws a NotSupportedException if the QsType to resolve contains a MissingType.
    member this.ResolveType (parent: QsQualifiedName, tpNames: ImmutableArray<_>, source: string) (qsType: QsType) =
        resolveType (parent, tpNames, source) qsType (fun _ -> [||])

    /// Resolves the underlying type as well as all named and unnamed items for the given type declaration in the
    /// specified source file using ResolveType.
    ///
    /// Generates the same diagnostics as ResolveType, as well as additional diagnostics when the accessibility of the
    /// type declaration is greater than the accessibility of any part of its underlying type.
    ///
    /// IMPORTANT: for performance reasons does *not* verify if the given the given parent and/or source file is
    /// consistent with the defined types.
    ///
    /// May throw an exception if the given parent and/or source file is inconsistent with the defined types. Throws an
    /// ArgumentException if the given type tuple is an empty QsTuple.
    member private this.ResolveTypeDeclaration (fullName: QsQualifiedName, source, modifiers) typeTuple =
        // Currently, type parameters for UDTs are not supported.
        let checkAccessibility =
            checkUdtAccessibility ErrorCode.TypeLessAccessibleThanParentType (fullName.Name, modifiers.Access)

        let resolveType qsType =
            resolveType (fullName, ImmutableArray<_>.Empty, source) qsType checkAccessibility

        SymbolResolution.ResolveTypeDeclaration resolveType typeTuple

    /// Given the namespace and the name of the callable that the given signature belongs to, as well as its kind and
    /// the source file it is declared in, fully resolves all Q# types in the signature using ResolveType.
    ///
    /// Generates the same diagnostics as ResolveType, as well as additional diagnostics when the accessibility of the
    /// callable declaration is greater than the accessibility of any type in its signature.
    ///
    /// Returns a new signature with the resolved types, the resolved argument tuple, as well as the array of
    /// diagnostics created during type resolution.
    ///
    /// The position offset information for the variables declared in the argument tuple will be set to Null. Positional
    /// information within types is set to Null if the parent callable is a type constructor.
    ///
    /// IMPORTANT: for performance reasons does *not* verify if the given the given parent and/or source file is
    /// consistent with the defined callables.
    ///
    /// May throw an exception if the given parent and/or source file is inconsistent with the defined callables. Throws
    /// an ArgumentException if the given list of characteristics is empty.
    member private this.ResolveCallableSignature (parentKind, parentName: QsQualifiedName, source, access)
                                                 (signature, specBundleCharacteristics)
                                                 =
        let checkAccessibility =
            checkUdtAccessibility ErrorCode.TypeLessAccessibleThanParentCallable (parentName.Name, access)

        let resolveType tpNames qsType =
            let res, errs =
                resolveType (parentName, tpNames, source) qsType checkAccessibility

            if parentKind <> TypeConstructor then res, errs else res.WithoutRangeInfo, errs // strip positional info for auto-generated type constructors

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
                    ns.SetSpecializationResolutions
                        (kvPair.Key, (fun _ _ -> Null, [||]), (fun _ _ -> ImmutableArray.Empty, [||]))
                    |> ignore

                    ns.SetCallableResolution (fst kvPair.Value) (kvPair.Key, Null, ImmutableArray.Empty)

            this.ContainsResolutions <- false

    /// Resolves and caches the attached attributes and underlying type of the types declared in all source files of each namespace.
    /// Returns the diagnostics generated upon resolution as well as the root position and file for each diagnostic as tuple.
    member private this.CacheTypeResolution(nsNames: ImmutableHashSet<_>) =
        let sortedNamespaces =
            Namespaces.Values
            |> Seq.sortBy (fun ns -> ns.Name)
            |> Seq.toList
        // Since attributes are declared as types, we first need to resolve all types ...
        let resolutionDiagnostics =
            sortedNamespaces
            |> Seq.collect (fun ns ->
                ns.TypesDefinedInAllSources()
                |> Seq.collect (fun kvPair ->
                    let tName, (source, qsType) = kvPair.Key, kvPair.Value
                    let fullName = { Namespace = ns.Name; Name = tName }

                    let resolved, resErrs =
                        qsType.Defined
                        |> this.ResolveTypeDeclaration(fullName, source, qsType.Modifiers)

                    ns.SetTypeResolution source (tName, resolved |> Value, ImmutableArray.Empty)

                    if fullName.ToString() |> (not << nsNames.Contains) then
                        resErrs
                    else
                        [| qsType.Range
                           |> QsCompilerDiagnostic.New
                               (Error ErrorCode.FullNameConflictsWithNamespace, [ fullName.ToString() ]) |]
                        |> Array.append resErrs
                    |> Array.map (fun msg -> source, (qsType.Position, msg))))
        // ... before we can resolve the corresponding attributes.
        let attributeDiagnostics =
            sortedNamespaces
            |> Seq.collect (fun ns ->
                ns.TypesDefinedInAllSources()
                |> Seq.collect (fun kvPair ->
                    let tName, (source, qsType) = kvPair.Key, kvPair.Value
                    let parentName = { Namespace = ns.Name; Name = tName }

                    let resolvedAttributes, msgs =
                        this.ResolveAttributes (parentName, source) qsType

                    ns.SetTypeResolution source (tName, qsType.Resolved, resolvedAttributes)
                    msgs |> Array.map (fun msg -> source, msg)))

        resolutionDiagnostics
            .Concat(attributeDiagnostics)
            .ToArray()

    /// Resolves and caches all attached attributes and specialization generation directives for all callables
    /// declared in all source files of each namespace, inserting inferred specializations if necessary and removing invalid specializations.
    /// Then resolves and caches the signature of the callables themselves.
    /// Returns the diagnostics generated upon resolution as well as the root position and file for each diagnostic as tuple.
    /// IMPORTANT: does *not* return diagnostics generated for type constructors - suitable diagnostics need to be generated upon type resolution.
    /// Throws an InvalidOperationException if the types corresponding to the attributes to resolve have not been resolved.
    member private this.CacheCallableResolutions(nsNames: ImmutableHashSet<string>) =
        // TODO: this needs to be adapted if we support external specializations
        let diagnostics =
            Namespaces.Values
            |> Seq.sortBy (fun ns -> ns.Name)
            |> Seq.collect (fun ns ->
                ns.CallablesDefinedInAllSources()
                |> Seq.sortBy (fun kv -> kv.Key)
                |> Seq.collect (fun kvPair ->
                    let source, (kind, signature) = kvPair.Value

                    let parent =
                        { Namespace = ns.Name
                          Name = kvPair.Key }

                    // we first need to resolve the type arguments to determine the right sets of specializations to consider
                    let typeArgsResolution specSource =
                        let typeResolution =
                            this.ResolveType(parent, ImmutableArray.Empty, specSource) // do not allow using type parameters within type specializations!

                        SymbolResolution.ResolveTypeArgument typeResolution

                    let mutable errs =
                        ns.SetSpecializationResolutions
                            (parent.Name, typeArgsResolution, (fun _ _ -> ImmutableArray.Empty, [||]))

                    // we then build the specialization bundles (one for each set of type and set arguments) and insert missing specializations
                    let definedSpecs =
                        ns.SpecializationsDefinedInAllSources parent.Name

                    let insertSpecialization typeArgs kind =
                        ns.InsertSpecialization (kind, typeArgs) (parent.Name, source)

                    let props, bundleErrs =
                        SymbolResolution.GetBundleProperties insertSpecialization (signature, source) definedSpecs

                    let bundleErrs = bundleErrs |> Array.concat
                    errs <- bundleErrs :: errs

                    // we remove the specializations which could not be bundled and resolve the newly inserted ones
                    for (specSource, (errPos, d)) in bundleErrs do
                        match d.Diagnostic with
                        | Information _
                        | Warning _ -> ()
                        | Error errCode ->
                            let removed =
                                ns.RemoveSpecialization (specSource, { Offset = errPos; Range = d.Range }) parent.Name

                            QsCompilerError.Verify
                                ((removed <= 1),
                                 sprintf
                                     "removed %i specializations based on error code %s"
                                     removed
                                     (errCode.ToString()))

                    let autoResErrs =
                        ns.SetSpecializationResolutions
                            (parent.Name, typeArgsResolution, (fun _ _ -> ImmutableArray.Empty, [||]))

                    // only then can we resolve the generators themselves, as well as the callable and specialization attributes
                    let callableAttributes, attrErrs =
                        this.ResolveAttributes (parent, source) signature

                    let resolution _ = SymbolResolution.ResolveGenerator props

                    let specErrs =
                        ns.SetSpecializationResolutions
                            (parent.Name, resolution, (fun attSource -> this.ResolveAttributes(parent, attSource)))

                    // and finally we resolve the overall signature (whose characteristics are the intersection of the one of all bundles)
                    let characteristics =
                        props.Values
                        |> Seq.map (fun bundle -> bundle.BundleInfo)
                        |> Seq.toList

                    let resolved, msgs =
                        (signature.Defined, characteristics)
                        |> this.ResolveCallableSignature(kind, parent, source, signature.Modifiers.Access) // no positional info for type constructors

                    ns.SetCallableResolution source (parent.Name, resolved |> Value, callableAttributes)

                    errs <-
                        (attrErrs |> Array.map (fun m -> source, m))
                        :: (msgs
                            |> Array.map (fun m -> source, (signature.Position, m)))
                           :: errs

                    let errs =
                        specErrs.Concat autoResErrs
                        |> errs.Concat
                        |> Array.concat

                    if kind = QsCallableKind.TypeConstructor then
                        [||]
                    elif parent.ToString() |> (not << nsNames.Contains) then
                        errs
                    else
                        signature.Range
                        |> QsCompilerDiagnostic.New
                            (Error ErrorCode.FullNameConflictsWithNamespace, [ parent.ToString() ])
                        |> (fun msg -> source, (signature.Position, msg))
                        |> Array.singleton
                        |> Array.append errs))

        diagnostics.ToArray()


    /// returns the current version number of the namespace manager -
    /// the version number is incremented whenever a write operation is performed
    member this.VersionNumber = versionNumber

    /// set to true if all types have been fully resolved and false otherwise
    member this.ContainsResolutions
        with get () = containsResolutions
        and private set value = containsResolutions <- value

    /// For each given namespace, automatically adds an open directive to all partial namespaces
    /// in all source files, if a namespace with that name indeed exists and is part of this compilation.
    /// Independent on whether the symbols have already been resolved, proceeds to resolves
    /// all types and callables as well as their attributes defined throughout all namespaces and caches the resolution.
    /// Checks whether there are fully qualified names conflict with a namespace name.
    /// Returns the generated diagnostics together with the Position of the declaration for which the diagnostics were generated.
    member this.ResolveAll(autoOpen: ImmutableHashSet<_>) =
        // TODO: this needs to be adapted if we support external specializations
        syncRoot.EnterWriteLock()
        versionNumber <- versionNumber + 1

        try
            let nsNames =
                Namespaces.Keys |> ImmutableHashSet.CreateRange

            let autoOpen =
                if autoOpen <> null then autoOpen else ImmutableHashSet.Empty

            let nsToAutoOpen = autoOpen.Intersect nsNames

            for opened in nsToAutoOpen do
                for ns in Namespaces.Values do
                    for source in ns.Sources do
                        this.AddOpenDirective (opened, Range.Zero) (null, Value Range.Zero) (ns.Name, source)
                        |> ignore
            // We need to resolve types before we resolve callables,
            // since the attribute resolution for callables relies on the corresponding types having been resolved.
            let typeDiagnostics = this.CacheTypeResolution nsNames
            let callableDiagnostics = this.CacheCallableResolutions nsNames
            this.ContainsResolutions <- true

            callableDiagnostics
                .Concat(typeDiagnostics)
                .ToLookup(fst,
                          (fun (_, (position, diagnostic)) ->
                              { diagnostic with
                                    QsCompilerDiagnostic.Range = position + diagnostic.Range }))
        finally
            syncRoot.ExitWriteLock()

    /// Returns a dictionary that maps each namespace name to a look-up
    /// that for each source file name contains the names of all imported namespaces in that file and namespace.
    member this.Documentation() =
        syncRoot.EnterReadLock()

        try
            let docs =
                Namespaces.Values
                |> Seq.map (fun ns -> ns.Name, ns.Documentation)

            docs.ToImmutableDictionary(fst, snd)
        finally
            syncRoot.ExitReadLock()

    /// <summary>
    /// Returns a look-up that contains the names of all namespaces imported within a certain source file for the given namespace.
    /// </summary>
    /// <exception cref="SymbolNotFoundException">The namespace with the given name was not found.</exception>
    member this.OpenDirectives nsName =
        syncRoot.EnterReadLock()

        try
            match Namespaces.TryGetValue nsName with
            | true, ns ->
                let imported =
                    ns.Sources
                    |> Seq.collect (fun source ->
                        ns.ImportedNamespaces source
                        |> Seq.choose (fun imported ->
                            if imported.Key <> ns.Name
                            then Some(source, new ValueTuple<_, _>(imported.Key, imported.Value))
                            else None))

                imported.ToLookup(fst, snd)
            | false, _ ->
                SymbolNotFoundException "The namespace with the given name was not found."
                |> raise
        finally
            syncRoot.ExitReadLock()

    /// <summary>
    /// Returns the headers of all imported specializations for callable with the given name.
    /// </summary>
    /// <exception cref="SymbolNotFoundException">
    /// The parent callable or its specializations were not found in references.
    /// </exception>
    member this.ImportedSpecializations(parent: QsQualifiedName) =
        // TODO: this may need to be adapted if we support external specializations
        syncRoot.EnterReadLock()

        try
            let imported =
                Namespaces.TryGetValue parent.Namespace
                |> function
                | false, _ ->
                    SymbolNotFoundException "The namespace with the given name was not found."
                    |> raise
                | true, ns ->
                    ns.SpecializationsInReferencedAssemblies.[parent.Name]
                        .ToImmutableArray()

            if imported.Length <> 0 then
                imported
            else
                SymbolNotFoundException "No specializations for a callable with the given name have been imported."
                |> raise
        finally
            syncRoot.ExitReadLock()

    /// <summary>
    /// Returns the resolved generation directive (if any) as well as the specialization headers
    /// for all specializations defined in source files for the callable with the given name.
    /// </summary>
    /// <exception cref="SymbolNotFoundException">
    /// The parent callable or its specializations were not found in sources.
    /// </exception>
    /// <exception cref="InvalidOperationException">Symbols have not been resolved.</exception>
    member this.DefinedSpecializations(parent: QsQualifiedName) =
        let notResolvedException =
            InvalidOperationException "specializations are not resolved"

        syncRoot.EnterReadLock()

        try
            if not this.ContainsResolutions then notResolvedException |> raise

            let defined =
                Namespaces.TryGetValue parent.Namespace
                |> function
                | false, _ ->
                    SymbolNotFoundException "The namespace with the given name was not found."
                    |> raise
                | true, ns ->
                    ns.SpecializationsDefinedInAllSources parent.Name
                    |> Seq.choose (fun (kind, (source, resolution)) ->
                        match resolution.Resolved with
                        | Null ->
                            QsCompilerError.Raise "everything should be resolved but isn't"
                            None
                        | Value gen ->
                            Some
                                (gen.Directive,
                                 { Kind = kind
                                   TypeArguments = gen.TypeArguments
                                   Information = gen.Information
                                   Parent = parent
                                   Attributes = resolution.ResolvedAttributes
                                   SourceFile = source
                                   Position = DeclarationHeader.Offset.Defined resolution.Position
                                   HeaderRange = DeclarationHeader.Range.Defined resolution.Range
                                   Documentation = resolution.Documentation }))

            defined.ToImmutableArray()
        finally
            syncRoot.ExitReadLock()

    /// Returns the source file and CallableDeclarationHeader of all callables imported from referenced assemblies,
    /// regardless of accessibility.
    member this.ImportedCallables() =
        // TODO: this needs to be adapted if we support external specializations
        syncRoot.EnterReadLock()

        try
            Namespaces.Values
            |> Seq.collect (fun ns -> ns.CallablesInReferencedAssemblies.SelectMany(fun g -> g.AsEnumerable()))
            |> fun callables -> callables.ToImmutableArray()
        finally
            syncRoot.ExitReadLock()

    /// Returns the declaration headers for all callables defined in source files, regardless of accessibility.
    ///
    /// Throws an InvalidOperationException if the symbols are not currently resolved.
    member this.DefinedCallables() =
        let notResolvedException =
            InvalidOperationException "callables are not resolved"

        syncRoot.EnterReadLock()

        try
            if not this.ContainsResolutions then notResolvedException |> raise

            let defined =
                Namespaces.Values
                |> Seq.collect (fun ns ->
                    ns.CallablesDefinedInAllSources()
                    |> Seq.choose (fun kvPair ->
                        let cName, (source, (kind, declaration)) = kvPair.Key, kvPair.Value

                        match declaration.Resolved with
                        | Null ->
                            QsCompilerError.Raise "everything should be resolved but isn't"
                            None
                        | Value (signature, argTuple) ->
                            Some
                                { Kind = kind
                                  QualifiedName = { Namespace = ns.Name; Name = cName }
                                  Attributes = declaration.ResolvedAttributes
                                  Modifiers = declaration.Modifiers
                                  SourceFile = source
                                  Position = DeclarationHeader.Offset.Defined declaration.Position
                                  SymbolRange = DeclarationHeader.Range.Defined declaration.Range
                                  Signature = signature
                                  ArgumentTuple = argTuple
                                  Documentation = declaration.Documentation }))

            defined.ToImmutableArray()
        finally
            syncRoot.ExitReadLock()

    /// Returns the declaration headers for all callables (either defined in source files or imported from referenced
    /// assemblies) that are accessible from source files in the compilation unit.
    ///
    /// Throws an InvalidOperationException if the symbols are not currently resolved.
    member this.AccessibleCallables() =
        Seq.append
            (Seq.map (fun callable -> callable, true) (this.DefinedCallables()))
            (Seq.map (fun callable -> callable, false) (this.ImportedCallables()))
        |> Seq.filter (fun (callable, sameAssembly) ->
            Namespace.IsDeclarationAccessible(sameAssembly, callable.Modifiers.Access))
        |> Seq.map fst

    /// Returns the source file and TypeDeclarationHeader of all types imported from referenced assemblies, regardless
    /// of accessibility.
    member this.ImportedTypes() =
        syncRoot.EnterReadLock()

        try
            Namespaces.Values
            |> Seq.collect (fun ns -> ns.TypesInReferencedAssemblies.SelectMany(fun g -> g.AsEnumerable()))
            |> fun types -> types.ToImmutableArray()
        finally
            syncRoot.ExitReadLock()

    /// Returns the declaration headers for all types defined in source files, regardless of accessibility.
    ///
    /// Throws an InvalidOperationException if the symbols are not currently resolved.
    member this.DefinedTypes() =
        let notResolvedException =
            InvalidOperationException "types are not resolved"

        syncRoot.EnterReadLock()

        try
            if not this.ContainsResolutions then notResolvedException |> raise

            let defined =
                Namespaces.Values
                |> Seq.collect (fun ns ->
                    ns.TypesDefinedInAllSources()
                    |> Seq.choose (fun kvPair ->
                        let tName, (source, qsType) = kvPair.Key, kvPair.Value

                        match qsType.Resolved with
                        | Null ->
                            QsCompilerError.Raise "everything should be resolved but isn't"
                            None
                        | Value (underlyingType, items) ->
                            Some
                                { QualifiedName = { Namespace = ns.Name; Name = tName }
                                  Attributes = qsType.ResolvedAttributes
                                  Modifiers = qsType.Modifiers
                                  SourceFile = source
                                  Position = DeclarationHeader.Offset.Defined qsType.Position
                                  SymbolRange = DeclarationHeader.Range.Defined qsType.Range
                                  Type = underlyingType
                                  TypeItems = items
                                  Documentation = qsType.Documentation }))

            defined.ToImmutableArray()
        finally
            syncRoot.ExitReadLock()

    /// Returns the declaration headers for all types (either defined in source files or imported from referenced
    /// assemblies) that are accessible from source files in the compilation unit.
    ///
    /// Throws an InvalidOperationException if the symbols are not currently resolved.
    member this.AccessibleTypes() =
        Seq.append
            (Seq.map (fun qsType -> qsType, true) (this.DefinedTypes()))
            (Seq.map (fun qsType -> qsType, false) (this.ImportedTypes()))
        |> Seq.filter (fun (qsType, sameAssembly) ->
            Namespace.IsDeclarationAccessible(sameAssembly, qsType.Modifiers.Access))
        |> Seq.map fst

    /// removes the given source file and all its content from all namespaces
    member this.RemoveSource source =
        syncRoot.EnterWriteLock()
        versionNumber <- versionNumber + 1

        try
            for ns in Namespaces.Values do
                ns.TryRemoveSource source |> ignore

            let keys = Namespaces.Keys |> List.ofSeq

            for key in keys do
                if Namespaces.[key].IsEmpty then Namespaces.Remove key |> ignore

            this.ClearResolutions()
        finally
            syncRoot.ExitWriteLock()

    /// clears all content from the symbol table
    member this.Clear() =
        syncRoot.EnterWriteLock()
        versionNumber <- versionNumber + 1

        try
            this.ContainsResolutions <- true
            Namespaces.Clear()
        finally
            syncRoot.ExitWriteLock()

    /// Adds every namespace along with all its content to target.
    /// IMPORTANT: if a namespace already exists in the target, replaces it!
    member this.CopyTo(target: NamespaceManager) =
        syncRoot.EnterReadLock()

        try
            for ns in Namespaces.Values do
                target.AddOrReplaceNamespace ns // ns will be deep copied
        finally
            syncRoot.ExitReadLock()

    /// If a namespace with the given name exists,
    /// makes a (deep) copy of that namespace and - if the given source file is not already listed as source for that namespace -
    /// adds the given source to the list of sources for the made copy, before returning the copy.
    /// If no namespace with the given name exists, returns a new Namespace with the given source file listed as source.
    /// NOTE: This routine does *not* modify this symbol table,
    /// and any modification to the returned namespace won't be reflected here -
    /// use AddOrReplaceNamespace to push back the modifications into the symbol table.
    member this.CopyForExtension(nsName, source) =
        syncRoot.EnterReadLock()

        try
            match Namespaces.TryGetValue nsName with
            | true, NS ->
                let copy = NS.Copy()

                if copy.TryAddSource source then
                    copy
                else
                    ArgumentException "partial namespace already exists"
                    |> raise
            | false, _ ->
                new Namespace(nsName, [ source ], ImmutableArray.Empty, ImmutableArray.Empty, ImmutableArray.Empty)
        finally
            syncRoot.ExitReadLock()

    /// Given a Namespace, makes a (deep) copy of that Namespace and replaces the existing namespace with that name
    /// by that copy, if such a namespace already exists, or adds the copy as a new namespace.
    /// -> Any modification to the namespace after pushing it into the symbol table (i.e. calling this routine) won't be reflected here.
    member this.AddOrReplaceNamespace(ns: Namespace) =
        syncRoot.EnterWriteLock()
        versionNumber <- versionNumber + 1

        try
            Namespaces.[ns.Name] <- ns.Copy()
            this.ClearResolutions true // force the clearing, since otherwise the newly added namespace may not be cleared
        finally
            syncRoot.ExitWriteLock()

    /// <summary>
    /// Adds the opened namespace to the list of imported namespaces for the given source and namespace.
    /// If the namespace to list as imported does not exists, or if the given alias cannot be used as namespace short name,
    /// adds the corresponding diagnostics to an array of diagnostics and returns them.
    /// Returns an empty array otherwise.
    /// </summary>
    /// <exception cref="SymbolNotFoundException">
    /// A namespace with the given parent name was not found, or the source file does not contain the parent namespace.
    /// </exception>
    member this.AddOpenDirective (opened, openedRange) (alias, aliasRange) (nsName, source) =
        syncRoot.EnterWriteLock()
        versionNumber <- versionNumber + 1

        try
            this.ClearResolutions()

            match Namespaces.TryGetValue nsName with
            | true, ns when ns.Sources.Contains source ->
                let validAlias =
                    String.IsNullOrWhiteSpace alias
                    || alias.Trim() |> Namespaces.ContainsKey |> not

                if validAlias && Namespaces.ContainsKey opened then
                    ns.TryAddOpenDirective source (opened, openedRange) (alias, aliasRange.ValueOr openedRange)
                elif validAlias then
                    [| openedRange
                       |> QsCompilerDiagnostic.Error(ErrorCode.UnknownNamespace, [ opened ]) |]
                else
                    [| aliasRange.ValueOr openedRange
                       |> QsCompilerDiagnostic.Error(ErrorCode.InvalidNamespaceAliasName, [ alias ]) |]
            | true, _ ->
                SymbolNotFoundException "The source file does not contain this namespace."
                |> raise
            | false, _ ->
                SymbolNotFoundException "The namespace with the given name was not found."
                |> raise
        finally
            syncRoot.ExitWriteLock()

    /// <summary>
    /// Given a qualified callable name, returns the corresponding CallableDeclarationHeader in a ResolutionResult if
    /// the qualifier can be resolved within the given parent namespace and source file, and the callable is accessible.
    ///
    /// If the callable is not defined an any of the references and the source file containing the callable declaration
    /// is specified (i.e. declSource is Some), throws the corresponding exception if no such callable exists in that
    /// file.
    /// </summary>
    /// <exception cref="SymbolNotFoundException">
    /// The callable's namespace or a namespace with the given parent name was not found, or the source file does not
    /// contain the parent namespace.
    /// </exception>
    member private this.TryGetCallableHeader (callableName: QsQualifiedName, declSource) (nsName, source) =
        let buildHeader fullName (source, kind, declaration) =
            let fallback () =
                (declaration.Defined, [ CallableInformation.Invalid ])
                |> this.ResolveCallableSignature(kind, callableName, source, declaration.Modifiers.Access)
                |> fst

            let resolvedSignature, argTuple =
                declaration.Resolved.ValueOrApply fallback

            { Kind = kind
              QualifiedName = fullName
              Attributes = declaration.ResolvedAttributes
              Modifiers = declaration.Modifiers
              SourceFile = source
              Position = DeclarationHeader.Offset.Defined declaration.Position
              SymbolRange = DeclarationHeader.Range.Defined declaration.Range
              Signature = resolvedSignature
              ArgumentTuple = argTuple
              Documentation = declaration.Documentation }

        let findInReferences (ns: Namespace) =
            ns.CallablesInReferencedAssemblies.[callableName.Name]
            |> Seq.map (fun callable ->
                if Namespace.IsDeclarationAccessible(false, callable.Modifiers.Access)
                then Found callable
                else Inaccessible)
            |> ResolutionResult.AtMostOne

        let findInSources (ns: Namespace) =
            function
            | Some source ->
                // OK to use CallableInSource because this is only evaluated if the callable is not in a
                // reference.
                let kind, declaration =
                    ns.CallableInSource source callableName.Name

                if Namespace.IsDeclarationAccessible(true, declaration.Modifiers.Access) then
                    Found
                        (buildHeader
                            { callableName with
                                  Namespace = ns.Name }
                             (source, kind, declaration))
                else
                    Inaccessible
            | None ->
                match ns
                    .CallablesDefinedInAllSources().TryGetValue callableName.Name with
                | true, (source, (kind, declaration)) ->
                    if Namespace.IsDeclarationAccessible(true, declaration.Modifiers.Access) then
                        Found
                            (buildHeader
                                { callableName with
                                      Namespace = ns.Name }
                                 (source, kind, declaration))
                    else
                        Inaccessible
                | false, _ -> NotFound

        syncRoot.EnterReadLock()

        try
            match (nsName, source)
                  |> TryResolveQualifier callableName.Namespace with
            | None -> NotFound
            | Some ns ->
                seq {
                    yield findInReferences ns
                    yield findInSources ns declSource
                }
                |> ResolutionResult.TryFirstBest
        finally
            syncRoot.ExitReadLock()

    /// <summary>
    /// Given a qualified callable name, returns the corresponding CallableDeclarationHeader in a ResolutionResult if
    /// the qualifier can be resolved within the given parent namespace and source file, and the callable is accessible.
    /// </summary>
    /// <exception cref="SymbolNotFoundException">
    /// The callable's namespace or a namespace with the given parent name was not found, or the source file does not
    /// contain the parent namespace.
    /// </exception>
    member this.TryGetCallable (callableName: QsQualifiedName) (nsName, source) =
        this.TryGetCallableHeader (callableName, None) (nsName, source)

    /// Given an unqualified callable name, returns the corresponding CallableDeclarationHeader in a ResolutionResult if
    /// the qualifier can be uniquely resolved within the given parent namespace and source file, and the callable is
    /// accessible.
    ///
    /// Returns an Ambiguous result with a list with namespaces containing a type with that name if the name cannot be
    /// uniquely resolved.
    member this.TryResolveAndGetCallable cName (nsName, source) =
        let toHeader (declaredNs, (declaredSource, _)) =
            match this.TryGetCallableHeader
                      ({ Namespace = declaredNs; Name = cName }, Some declaredSource)
                      (nsName, source) with
            | Found value -> value
            | _ ->
                QsCompilerError.Raise "Expected to find the header corresponding to a possible resolution"
                Exception() |> raise

        syncRoot.EnterReadLock()

        try
            resolveInOpenNamespaces (fun ns -> ns.TryFindCallable cName) (nsName, source)
            |> ResolutionResult.Map toHeader
        finally
            syncRoot.ExitReadLock()

    /// <summary>
    /// Given a qualified type name, returns the corresponding TypeDeclarationHeader in a ResolutionResult if the
    /// qualifier can be resolved within the given parent namespace and source file, and the type is accessible.
    ///
    /// If the type is not defined an any of the references and the source file containing the type declaration is
    /// specified (i.e. declSource is Some), throws the corresponding exception if no such type exists in that file.
    /// </summary>
    /// <exception cref="SymbolNotFoundException">
    /// The type's namespace or a namespace with the given parent name was not found, or the source file does not
    /// contain the parent namespace.
    /// </exception>
    member private this.TryGetTypeHeader (typeName: QsQualifiedName, declSource) (nsName, source) =
        let buildHeader fullName (source, declaration) =
            let fallback () =
                declaration.Defined
                |> this.ResolveTypeDeclaration(typeName, source, declaration.Modifiers)
                |> fst

            let underlyingType, items =
                declaration.Resolved.ValueOrApply fallback

            { QualifiedName = fullName
              Attributes = declaration.ResolvedAttributes
              Modifiers = declaration.Modifiers
              SourceFile = source
              Position = DeclarationHeader.Offset.Defined declaration.Position
              SymbolRange = DeclarationHeader.Range.Defined declaration.Range
              Type = underlyingType
              TypeItems = items
              Documentation = declaration.Documentation }

        let findInReferences (ns: Namespace) =
            ns.TypesInReferencedAssemblies.[typeName.Name]
            |> Seq.map (fun typeHeader ->
                if Namespace.IsDeclarationAccessible(false, typeHeader.Modifiers.Access)
                then Found typeHeader
                else Inaccessible)
            |> ResolutionResult.AtMostOne

        let findInSources (ns: Namespace) =
            function
            | Some source ->
                // OK to use TypeInSource because this is only evaluated if the type is not in a reference.
                let declaration = ns.TypeInSource source typeName.Name

                if Namespace.IsDeclarationAccessible(true, declaration.Modifiers.Access)
                then Found(buildHeader { typeName with Namespace = ns.Name } (source, declaration))
                else Inaccessible
            | None ->
                match ns
                    .TypesDefinedInAllSources().TryGetValue typeName.Name with
                | true, (source, declaration) ->
                    if Namespace.IsDeclarationAccessible(true, declaration.Modifiers.Access)
                    then Found(buildHeader { typeName with Namespace = ns.Name } (source, declaration))
                    else Inaccessible
                | false, _ -> NotFound

        syncRoot.EnterReadLock()

        try
            match (nsName, source)
                  |> TryResolveQualifier typeName.Namespace with
            | None -> NotFound
            | Some ns ->
                seq {
                    yield findInReferences ns
                    yield findInSources ns declSource
                }
                |> ResolutionResult.TryFirstBest
        finally
            syncRoot.ExitReadLock()

    /// <summary>
    /// Given a qualified type name, returns the corresponding TypeDeclarationHeader in a ResolutionResult if the
    /// qualifier can be resolved within the given parent namespace and source file, and the type is accessible.
    /// </summary>
    /// <exception cref="SymbolNotFoundException">
    /// The type's namespace or a namespace with the given parent name was not found, or the source file does not
    /// contain the parent namespace.
    /// </exception>
    member this.TryGetType (typeName: QsQualifiedName) (nsName, source) =
        this.TryGetTypeHeader (typeName, None) (nsName, source)

    /// Given an unqualified type name, returns the corresponding TypeDeclarationHeader in a ResolutionResult if the
    /// qualifier can be uniquely resolved within the given parent namespace and source file, and the type is
    /// accessible.
    ///
    /// Returns an Ambiguous result with a list with namespaces containing a type with that name if the name cannot be
    /// uniquely resolved.
    member this.TryResolveAndGetType tName (nsName, source) =
        let toHeader (declaredNs, (declaredSource, _, _)) =
            match this.TryGetTypeHeader ({ Namespace = declaredNs; Name = tName }, Some declaredSource) (nsName, source) with
            | Found value -> value
            | _ ->
                QsCompilerError.Raise "Expected to find the header corresponding to a possible resolution"
                Exception() |> raise

        syncRoot.EnterReadLock()

        try
            resolveInOpenNamespaces (fun ns -> ns.TryFindType tName) (nsName, source)
            |> ResolutionResult.Map toHeader
        finally
            syncRoot.ExitReadLock()

    /// <summary>
    /// Returns the fully qualified namespace name of the given namespace alias (short name). If the alias is already a fully qualified name,
    /// returns the name unchanged. Returns null if no such name exists within the given parent namespace and source file.
    /// </summary>
    /// <exception cref="SymbolNotFoundException">
    /// A namespace with the given parent name was not found, or the source file does not contain the parent namespace.
    /// </exception>
    member this.TryResolveNamespaceAlias alias (nsName, source) =
        syncRoot.EnterReadLock()

        try
            match TryResolveQualifier alias (nsName, source) with
            | None -> null
            | Some ns -> ns.Name
        finally
            syncRoot.ExitReadLock()

    /// Returns the names of all namespaces in which a callable is declared that has the given name and is accessible
    /// from source files in the compilation unit.
    member this.NamespacesContainingCallable cName =
        // FIXME: we need to handle the case where a callable/type with the same qualified name is declared in several references!
        syncRoot.EnterReadLock()

        try
            Namespaces.Values
            |> Seq.choose (fun ns ->
                ns.TryFindCallable cName
                |> ResolutionResult.ToOption
                |> Option.map (fun _ -> ns.Name))
            |> fun namespaces -> namespaces.ToImmutableArray()
        finally
            syncRoot.ExitReadLock()

    /// Returns the names of all namespaces in which a type is declared that has the given name and is accessible from
    /// source files in the compilation unit.
    member this.NamespacesContainingType tName =
        // FIXME: we need to handle the case where a callable/type with the same qualified name is declared in several references!
        syncRoot.EnterReadLock()

        try
            Namespaces.Values
            |> Seq.choose (fun ns ->
                ns.TryFindType tName
                |> ResolutionResult.ToOption
                |> Option.map (fun _ -> ns.Name))
            |> fun namespaces -> namespaces.ToImmutableArray()
        finally
            syncRoot.ExitReadLock()

    /// Returns the name of all namespaces declared in source files or referenced assemblies.
    member this.NamespaceNames() =
        syncRoot.EnterReadLock()

        try
            ImmutableArray.CreateRange Namespaces.Keys
        finally
            syncRoot.ExitReadLock()

    /// Returns true if the given namespace name exists in the symbol table.
    member this.NamespaceExists nsName = Namespaces.ContainsKey nsName


    /// Generates a hash for a resolved type. Does not incorporate any positional information.
    static member internal TypeHash(t: ResolvedType) =
        t.Resolution
        |> function
        | QsTypeKind.ArrayType b -> hash (0, NamespaceManager.TypeHash b)
        | QsTypeKind.TupleType ts ->
            hash
                (1,
                 (ts
                  |> Seq.map NamespaceManager.TypeHash
                  |> Seq.toList))
        | QsTypeKind.UserDefinedType udt -> hash (2, udt.Namespace, udt.Name)
        | QsTypeKind.TypeParameter tp -> hash (3, tp.Origin.Namespace, tp.Origin.Name, tp.TypeName)
        | QsTypeKind.Operation ((inT, outT), fList) ->
            hash
                (4,
                 (inT |> NamespaceManager.TypeHash),
                 (outT |> NamespaceManager.TypeHash),
                 (fList |> JsonConvert.SerializeObject))
        | QsTypeKind.Function (inT, outT) ->
            hash (5, (inT |> NamespaceManager.TypeHash), (outT |> NamespaceManager.TypeHash))
        | kind -> JsonConvert.SerializeObject kind |> hash

    /// Generates a hash for a typed expression. Does not incorporate any positional information.
    static member internal ExpressionHash(ex: TypedExpression) =
        ex.Expression
        |> function
        | StringLiteral (s, _) -> hash (6, s)
        | ValueTuple vs ->
            hash
                (7,
                 (vs
                  |> Seq.map NamespaceManager.ExpressionHash
                  |> Seq.toList))
        | ValueArray vs ->
            hash
                (8,
                 (vs
                  |> Seq.map NamespaceManager.ExpressionHash
                  |> Seq.toList))
        | NewArray (bt, idx) -> hash (9, NamespaceManager.TypeHash bt, NamespaceManager.ExpressionHash idx)
        | Identifier (GlobalCallable c, _) -> hash (10, c.Namespace, c.Name)
        | kind -> JsonConvert.SerializeObject kind |> hash

    /// Generates a hash containing full type information about all entries in the given source file.
    /// All entries in the source file have to be fully resolved beforehand.
    /// That hash does not contain any information about the imported namespaces, positional information, or about any documentation.
    /// Returns the generated hash as well as a separate hash providing information about the imported namespaces.
    /// Throws an InvalidOperationException if the given source file contains unresolved entries.
    member this.HeaderHash source =
        let invalidOperationEx =
            InvalidOperationException "everything needs to be resolved before constructing the HeaderString"

        if not this.ContainsResolutions then invalidOperationEx |> raise

        let inconsistentStateException () =
            QsCompilerError.Raise "contains unresolved entries despite supposedly being resolved"
            invalidOperationEx |> raise

        let attributesHash (attributes: QsDeclarationAttribute seq) =
            let getHash arg (id: UserDefinedType) =
                hash (id.Namespace, id.Name, NamespaceManager.ExpressionHash arg)

            attributes
            |> QsNullable<_>
                .Choose(fun att ->
                    att.TypeId
                    |> QsNullable<_>.Map(getHash att.Argument))
            |> Seq.toList

        let callableHash (kind, (signature, _), specs, attributes: QsDeclarationAttribute seq) =
            let signatureHash (signature: ResolvedSignature) =
                let argStr =
                    signature.ArgumentType
                    |> NamespaceManager.TypeHash

                let reStr =
                    signature.ReturnType |> NamespaceManager.TypeHash

                let nameOrInvalid =
                    function
                    | InvalidName -> InvalidName |> JsonConvert.SerializeObject
                    | ValidName sym -> sym

                let typeParams =
                    signature.TypeParameters
                    |> Seq.map nameOrInvalid
                    |> Seq.toList

                hash (argStr, reStr, typeParams)

            let specsStr =
                let genHash (gen: ResolvedGenerator) =
                    let tArgs =
                        gen.TypeArguments
                        |> QsNullable<_>
                            .Map(fun tArgs ->
                                tArgs
                                |> Seq.map NamespaceManager.TypeHash
                                |> Seq.toList)

                    hash (gen.Directive, hash tArgs)

                let kinds, gens =
                    specs |> Seq.sort |> Seq.toList |> List.unzip

                hash (kinds, gens |> List.map genHash)

            hash (kind, specsStr, signatureHash signature, attributes |> attributesHash)

        let typeHash (t, typeItems: QsTuple<QsTypeItem>, attributes) =
            let getItemHash (itemName, itemType) =
                hash (itemName, NamespaceManager.TypeHash itemType)

            let namedItems =
                typeItems.Items
                |> Seq.choose (function
                    | Named item -> Some item
                    | _ -> None)

            let itemHashes =
                namedItems.Select(fun d -> d.VariableName, d.Type)
                |> Seq.map getItemHash

            hash (NamespaceManager.TypeHash t, itemHashes |> Seq.toList, attributes |> attributesHash)

        syncRoot.EnterReadLock()

        try
            let relevantNamespaces =
                Namespaces.Values
                |> Seq.filter (fun ns -> ns.Sources.Contains source)
                |> Seq.sortBy (fun ns -> ns.Name)
                |> Seq.toList

            let callables =
                relevantNamespaces
                |> Seq.collect (fun ns ->
                    let inSource =
                        ns.CallablesDefinedInSource source
                        |> Seq.sortBy fst

                    inSource
                    |> Seq.map (fun (cName, (kind, signature)) ->
                        let specs =
                            ns.SpecializationsDefinedInAllSources cName
                            |> Seq.map (fun (kind, (_, resolution)) ->
                                kind, resolution.Resolved.ValueOrApply inconsistentStateException)

                        let resolved =
                            signature.Resolved.ValueOrApply inconsistentStateException

                        ns.Name, cName, (kind, resolved, specs, signature.ResolvedAttributes)))

            let types =
                relevantNamespaces
                |> Seq.collect (fun ns ->
                    let inSources =
                        ns.TypesDefinedInSource source |> Seq.sortBy fst

                    inSources
                    |> Seq.map (fun (tName, qsType) ->
                        let resolved, resItems =
                            qsType.Resolved.ValueOrApply inconsistentStateException

                        ns.Name, tName, (resolved, resItems, qsType.ResolvedAttributes)))

            let imports =
                relevantNamespaces
                |> Seq.collect (fun ns ->
                    ns.ImportedNamespaces source
                    |> Seq.sortBy (fun x -> x.Value)
                    |> Seq.map (fun opened -> ns.Name, opened.Value))

            let callablesHash =
                callables
                |> Seq.map (fun (ns, name, c) -> (ns, name, callableHash c))
                |> Seq.toList
                |> hash

            let typesHash =
                types
                |> Seq.map (fun (ns, name, t) -> ns, name, typeHash t)
                |> Seq.toList
                |> hash

            let importsHash = imports |> Seq.toList |> hash
            hash (callablesHash, typesHash), importsHash
        finally
            syncRoot.ExitReadLock()
