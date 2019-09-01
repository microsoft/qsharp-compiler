﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SymbolTracker

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Linq
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SymbolManagement
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.VerificationTools
open Microsoft.Quantum.QsCompiler.SyntaxTokens 
open Microsoft.Quantum.QsCompiler.SyntaxTree


/// Used to represent all properties that need to be tracked for verifying the built syntax tree, but are not needed after.
/// Specifically, the tracked properties are pushed and popped for each scope. 
type private TrackedScope = private {
    /// used to track all local variables defined on this scope, as well as their inferred information
    LocalVariables : Dictionary<NonNullable<string>, LocalVariableDeclaration<NonNullable<string>>>
    /// contains the set of functors that each operation called on this scope needs to support
    RequiredFunctorSupport : ImmutableHashSet<QsFunctor>
}
    with

    /// Given all functors that need to be applied to a particular operation call, 
    /// combines them into the set of functors that that operation needs to support.
    static member internal CombinedFunctorSupport functors = 
        let rec requiredSupport current = function
            | [] -> current
            | Adjoint :: tail -> 
                if not (current |> List.contains Adjoint) then requiredSupport (Adjoint :: current) tail
                else requiredSupport [for f in current do if f <> Adjoint then yield f] tail
            | Controlled :: tail -> 
                if current |> List.contains Controlled then requiredSupport current tail
                else requiredSupport (Controlled :: current) tail
        requiredSupport [] functors


/// The SymbolTracker class does *not* make a copy of the given NamespaceManager, 
/// but instead will throw an InvalidOperationException upon accessing its content if that content has been modified 
/// (i.e. if the version number of the NamespaceManager has changed). 
/// The constructor throws an ArgumentException if the given NamespaceManager does not contain all resolutions, 
/// or if a callable with the given parent name does not exist in the given NamespaceManager.
type SymbolTracker<'P>(globals : NamespaceManager, sourceFile, parent : QsQualifiedName) =  
// TODO: once we support type specialiations, the parent needs to be the specialization name rather than the callable name

    do  if not globals.ContainsResolutions then ArgumentException "the content of the given namespace manager needs to be resolved" |> raise

    /// Contains all properties that need to be tracked for verifying the built syntax tree, but are not needed after.
    /// In particular, contains a "stack" of all local declarations up to this point in each scope, 
    /// as well as which functors need to be supported by the operations called within each scope. 
    let mutable pushedScopes : TrackedScope list = []
    
    /// contains the version number of the global symbols manager upon initialization
    let expectedVersionGlobals = globals.VersionNumber
    
    /// Returns the manager for all globally declared callables an types.
    /// Throws an invalid operation exception if the content of the manager have been modified since the initialization of this symbol tracker.
    let GlobalSymbols () = 
        if globals.VersionNumber <> expectedVersionGlobals then 
            InvalidOperationException "the content of the namespace manager associated with this symbol tracker has changed" |> raise
        globals

    /// recursively sets all range information of a ResolvedType to Null
    let StripRangeInfo: ResolvedType -> ResolvedType = SyntaxGenerator.StripRangeInfo.Apply

    /// Denotes a property of the parent callable associated with this symbol tracker,  
    /// such as information about the kind, the type parameters, the returns type, and the source file it is declared in.
    /// IMPORTANT: these need to be adapted if we want to support type specializations and/or external specializations!
    let parentIsOperation, typeParameters, expectedReturnType = 
        match GlobalSymbols().TryGetCallable parent (parent.Namespace, sourceFile) with 
        | Null -> ArgumentException "the given NamespaceManager does not contain a callable with the given parent name" |> raise
        | Value decl -> 
            let isOperation = decl.Kind |> function | QsCallableKind.Operation -> true | _ -> false
            let validTypeParams = decl.Signature.TypeParameters |> Seq.choose (function | ValidName name -> Some name | InvalidName -> None)
            isOperation, validTypeParams.ToImmutableArray(), decl.Signature.ReturnType |> StripRangeInfo // NOTE: valid only since we do not yet support external and/or type specializations
    
    /// If a local variable with the given name is visible on the current scope, 
    /// returns the dictionary that contains its declaration as Value.
    /// Returns Null otherwise. 
    let localVariableWithName (name : NonNullable<string>) = 
        let rec varDecl = function 
            | [] -> Null
            | (head : TrackedScope) :: _ when head.LocalVariables.ContainsKey name -> Value head.LocalVariables
            | _ :: tail -> varDecl tail
        varDecl pushedScopes

    /// If a type declaration for a type with the given name exists in GlobalSymbols, 
    /// returns a its its header information as Value. Returns Null otherwise.  
    /// If no namespace is specified, the namespace resolution is done under the assumption that the unqualified name is used within the 
    /// source file, namespace, and callable associated with this symbol tracker instance. 
    let globalTypeWithName (ns, name : NonNullable<string>) = ns |> function
        | None -> GlobalSymbols().TryResolveAndGetType name (parent.Namespace, sourceFile)
        | Some nsName -> GlobalSymbols().TryGetType (QsQualifiedName.New (nsName, name)) (parent.Namespace, sourceFile), Seq.empty

    /// If a callable declaration (including type constructors!) for a callable with the given name exists in GlobalSymbols, 
    /// returns a its header information as Value. Returns Null otherwise. 
    /// If no namespace is specified, the namespace resolution is done under the assumption that the unqualified name is used within the 
    /// source file, namespace, and callable associated with this symbol tracker instance. 
    let globalCallableWithName (ns, name : NonNullable<string>) = ns |> function
        | None -> GlobalSymbols().TryResolveAndGetCallable name (parent.Namespace, sourceFile) 
        | Some nsName -> GlobalSymbols().TryGetCallable (QsQualifiedName.New (nsName, name)) (parent.Namespace, sourceFile), Seq.empty

    /// the namespace and callable declaration within which the symbols tracked by this SymbolTracker instance are used
    member this.Parent = parent
    /// set to true if the parent callable associated with this symbol tracker is an operation
    member this.WithinOperation = parentIsOperation
    /// the source file within which the Parent (a callable declaration) associated with this SymbolTracker instance is declared
    member this.SourceFile = sourceFile
    /// the expected type for all values returned within the scope this symbol tracker is used for (contains no range information)
    member this.ExpectedReturnType = expectedReturnType
    /// returns true if no scope is currently open
    member this.AllScopesClosed = pushedScopes.Length = 0

    /// Returns the set of functors that need to be supported by each operation called within the current scope.
    /// Returns an empty set if no scope is currently open.
    member this.RequiredFunctorSupport = 
        match pushedScopes with 
        | head :: _ -> head.RequiredFunctorSupport
        | [] -> ImmutableHashSet.Empty

    /// Pushes a new scope onto the stack and opens it.
    /// If the given set of functors to support is not null, operations called within the newly opened scope need to support these functors. 
    /// Otherwise the set of functors to support is determined by the parent scope if a parent scope exist, or empty if no parent scope exists.
    member this.BeginScope functorSupport =
        let scopeToPush = { 
            LocalVariables = new Dictionary<_,_>(); 
            RequiredFunctorSupport = if functorSupport = null then this.RequiredFunctorSupport else functorSupport} 
        pushedScopes <- scopeToPush :: pushedScopes

    /// Pushes a new scope onto the stack and opens it.
    /// Operations called within the newly opened scope need to support the same set of functors as the parent scope.
    /// If no parent scope exists, then the set of functors to support is assumed to be empty.
    member this.BeginScope () = this.BeginScope this.RequiredFunctorSupport

    /// pops the most recent scope from the stack, thus closing it
    member this.EndScope() = 
        if pushedScopes.Length = 0 then InvalidOperationException "no scope is currently open" |> raise
        pushedScopes <- pushedScopes.Tail


    /// Verifies that no global type or callable with the same name as the one in the given variable declaration already exists, 
    /// and that no local variable with that name is visible on the current scope. 
    /// If the verification fails, does nothing and adds a suitable diagnostic to the returned array of diagnostics, 
    /// and otherwise pushes the given variable declaration as local variable into the current scope.
    /// Returns true and an empty array of diagnostics if the declaration has been successfully added, 
    /// and returns false as well as an array with diagnostics otherwise. 
    /// Throws an InvalidOperationException if no scope is currently open.
    member this.TryAddVariableDeclartion (decl : LocalVariableDeclaration<NonNullable<string>>) =
        if pushedScopes.Length = 0 then InvalidOperationException "no scope is currently open" |> raise
        if (globalTypeWithName (None, decl.VariableName)) |> fst <> Null then false, [| decl.Range |> QsCompilerDiagnostic.Error (ErrorCode.GlobalTypeAlreadyExists, []) |]
        elif (globalCallableWithName (None, decl.VariableName)) |> fst <> Null then false, [| decl.Range |> QsCompilerDiagnostic.Error (ErrorCode.GlobalCallableAlreadyExists, []) |]
        elif (localVariableWithName decl.VariableName) <> Null then false, [| decl.Range |> QsCompilerDiagnostic.Error (ErrorCode.LocalVariableAlreadyExists, []) |]
        else pushedScopes.Head.LocalVariables.Add(decl.VariableName, decl); true, [||]

    /// If the variable name in the given variable declaration is valid, 
    /// verifies that no global type or callable with that name already exists, 
    /// and that no local variable with that name is visible on the current scope. 
    /// If that is the case, pushed the given variable declaration as local variable into the current scope.
    /// If the verification fails, does nothing and adds a suitable diagnostic to the returned array of diagnostics.
    /// If the variable name is not valid, does nothing and does not generate any diagnostics. 
    /// Returns true and an empty array of diagnostics if the declaration has been successfully added, 
    /// and returns false as well as an array with diagnostics otherwise. 
    /// Throws an InvalidOperationException if no scope is currently open.
    member this.TryAddVariableDeclartion (decl : LocalVariableDeclaration<QsLocalSymbol>) =
        if pushedScopes.Length = 0 then InvalidOperationException "no scope is currently open" |> raise
        match decl.VariableName with 
        | InvalidName -> false, [||]
        | ValidName name -> 
            let mut, qDep = decl.InferredInformation.IsMutable, decl.InferredInformation.HasLocalQuantumDependency
            LocalVariableDeclaration<_>.New mut ((decl.Position, decl.Range), name, decl.Type, qDep) 
            |> this.TryAddVariableDeclartion

    /// Updates the quantum dependency of the local variable with the given name.
    /// Throws an ArgumentException if no local variable with the given name is visible on the current scope.
    /// Throws an InvalidOperationException if no scope is currently open, or the variable is immutable.
    member internal this.UpdateQuantumDependency varName localQdep =
        if pushedScopes.Length = 0 then InvalidOperationException "no scope is currently open" |> raise
        match localVariableWithName varName with 
        | Value dict ->
            let existing = dict.[varName]
            if not existing.InferredInformation.IsMutable then InvalidOperationException "cannot update information for immutable variable" |> raise
            else dict.[varName] <- {existing with InferredInformation = {existing.InferredInformation with HasLocalQuantumDependency = localQdep}}
        | Null -> ArgumentException "no local variable with the given name exists on the current scope" |> raise

    /// returns all *local* declarations on the current scope *and* all parent scopes up to this point
    member this.CurrentDeclarations = 
        let varDeclarations = seq { for scope in pushedScopes do for decl in scope.LocalVariables.Values do yield decl }
        LocalDeclarations.New varDeclarations

    /// Given a Q# symbol used as identifier within the context associated with this symbol tracker, 
    /// returns the resolved identifier, it's type, whether it is mutable, and whether it has any quantum dependencies as LocalVariableDeclaration,  
    /// along with an immutable array containing the names of its type parameters as UnqualifiedSymbols 
    /// if the identifier is type parameterized (currently only supported for globally declared callables).
    /// Note that the location information in the returned variable declaration will be set to an arbitrary location, 
    /// and the range information will be stripped for all types.
    /// If the given symbol is not a valid name for an identifier, or the corresponding variable is not visible on the current scope,
    /// calls the given addDiagnostics function with a suitable diagnostic. 
    member this.ResolveIdentifier addDiagnostic (qsSym : QsSymbol) = 
        let defaultLoc = Null, QsCompilerDiagnostic.DefaultRange // dummy location for the purpose of returning the necessary information as local variable declaration
        let invalid = 
            let properties = (defaultLoc, InvalidIdentifier, ResolvedType.New InvalidType, false)
            properties |> LocalVariableDeclaration.New false, ImmutableArray<_>.Empty

        let buildCallable kind (fullName : QsQualifiedName) (decl : ResolvedSignature) = 
            let argType, returnType = decl.ArgumentType |> StripRangeInfo, decl.ReturnType |> StripRangeInfo
            let idType = kind ((argType, returnType), decl.Information) |> ResolvedType.New 
            LocalVariableDeclaration.New false (defaultLoc, GlobalCallable fullName, idType, false), decl.TypeParameters

        let resolveGlobal = function
            | Value (decl : CallableDeclarationHeader), _ -> decl.Kind |> function 
                | QsCallableKind.Operation -> buildCallable QsTypeKind.Operation decl.QualifiedName decl.Signature
                | QsCallableKind.TypeConstructor
                | QsCallableKind.Function -> buildCallable (fst >> QsTypeKind.Function) decl.QualifiedName decl.Signature
            | Null, (possibleResolutions : NonNullable<string> seq) -> 
                let ambiguousCallableErr = (ErrorCode.AmbiguousCallable, [String.Join(", ", possibleResolutions |> Seq.map (fun nsName -> nsName.Value))])
                let errCode = if possibleResolutions.Count() > 1 then ambiguousCallableErr else (ErrorCode.UnknownIdentifier, [])
                qsSym.RangeOrDefault |> QsCompilerDiagnostic.Error errCode |> addDiagnostic;
                invalid
        
        let resolveNative sym =
            match localVariableWithName sym with 
            | Value dict ->
                let decl = dict.[sym]
                let properties = (defaultLoc, LocalVariable sym, decl.Type |> StripRangeInfo, decl.InferredInformation.HasLocalQuantumDependency)
                properties |> LocalVariableDeclaration.New decl.InferredInformation.IsMutable, ImmutableArray<_>.Empty
            | Null -> globalCallableWithName (None, sym) |> resolveGlobal 

        match qsSym.Symbol with 
        | InvalidSymbol -> invalid
        | Symbol sym -> resolveNative sym
        | QualifiedSymbol (ns, sym) -> globalCallableWithName (Some ns, sym) |> resolveGlobal
        | _ -> qsSym.RangeOrDefault |> QsCompilerDiagnostic.Error (ErrorCode.ExpectingIdentifier, []) |> addDiagnostic; 
               invalid

    /// Given a Q# type, resolves it calling the NamespaceManager associated with this symbol tracker.
    /// For each diagnostic generated during the resolution, calls the given addDiagnostics function on it. 
    /// Returns the resolved type, with all range information stripped.
    member this.ResolveType addDiagnostic (qsType : QsType) = 
        let (resolved, errs) = GlobalSymbols().ResolveType (this.Parent, typeParameters, this.SourceFile) qsType 
        for err in errs do addDiagnostic err
        resolved |> StripRangeInfo

    /// Given the fully qualified name of a user defined type, returns its declaration as Some.
    /// Adds a suitable error using the given function and returns None if no declaration can be found.
    member private this.TryGetTypeDeclaration addError (udt : UserDefinedType) = 
        match globalTypeWithName (Some udt.Namespace, udt.Name) with
        | Value decl, _ -> Value decl
        | Null, _ -> // may occur when the return type of a referenced callable is defined in an assembly that is not referenced
            addError (ErrorCode.IndirectlyReferencedExpressionType, [sprintf "%s.%s" udt.Namespace.Value udt.Name.Value]); Null

    /// Given the fully qualified name of a user defined type, returns its underlying type where all range information is stripped.
    /// Adds a suitable diagnostic and returns an invalid type if the underlying type could not be determined.
    member this.GetUnderlyingType addError (udt : UserDefinedType) = 
        match this.TryGetTypeDeclaration addError udt with
        | Value decl -> decl.Type |> StripRangeInfo
        | Null -> InvalidType |> ResolvedType.New 
        
    /// Given the fully qualified name of a user defined type as well as the identifier specifying an item, 
    /// returns the type of the item where all range information is stripped.
    /// Adds a suitable diagnostic and returns an invalid type if the item type could not be determined.
    member this.GetItemType (item : Identifier) addError (udt : UserDefinedType) = 
        let namedWithName name = function | Named n when n.VariableName.Value = name -> Some n | _ -> None
        match this.TryGetTypeDeclaration addError udt with
        | Null -> InvalidType |> ResolvedType.New 
        | Value decl -> item |> function 
            | InvalidIdentifier -> InvalidType |> ResolvedType.New
            | GlobalCallable _ -> addError (ErrorCode.ExpectingItemName, []); InvalidType |> ResolvedType.New
            | LocalVariable name -> decl.TypeItems.Items |> Seq.choose (namedWithName name.Value) |> Seq.toList |> function
                | [itemDecl] -> itemDecl.Type |> StripRangeInfo
                | _ -> addError (ErrorCode.UnknownItemName, [udt.Name.Value; name.Value]); InvalidType |> ResolvedType.New
