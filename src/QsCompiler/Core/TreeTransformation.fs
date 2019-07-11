// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

open System.Collections.Immutable
open System.Linq
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree

type QsArgumentTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>


/// Convention: 
/// All methods starting with "on" implement the transformation syntax tree element.
/// All methods starting with "before" group a set of elements, and are called before applying the transformation
/// even if the corresponding transformation routine (starting with "on") is overridden.
type SyntaxTreeTransformation() =
    let scopeTransformation = new ScopeTransformation()

    abstract member Scope : ScopeTransformation
    default this.Scope = scopeTransformation


    abstract member beforeNamespaceElement : QsNamespaceElement -> QsNamespaceElement
    default this.beforeNamespaceElement e = e

    abstract member beforeCallable : QsCallable -> QsCallable
    default this.beforeCallable c = c

    abstract member beforeSpecialization : QsSpecialization -> QsSpecialization
    default this.beforeSpecialization spec = spec

    abstract member beforeSpecializationImplementation : SpecializationImplementation -> SpecializationImplementation
    default this.beforeSpecializationImplementation impl = impl

    abstract member beforeGeneratedImplementation : QsGeneratorDirective -> QsGeneratorDirective
    default this.beforeGeneratedImplementation dir = dir


    abstract member onLocation : QsLocation -> QsLocation
    default this.onLocation l = l

    abstract member onDocumentation : ImmutableArray<string> -> ImmutableArray<string>
    default this.onDocumentation doc = doc

    abstract member onSourceFile : NonNullable<string> -> NonNullable<string>
    default this.onSourceFile f = f

    abstract member onTypeItems : QsTuple<QsTypeItem> -> QsTuple<QsTypeItem>
    default this.onTypeItems tItem = 
        match tItem with 
        | QsTuple items -> (items |> Seq.map this.onTypeItems).ToImmutableArray() |> QsTuple
        | QsTupleItem (Anonymous itemType) -> 
            let t = this.Scope.Expression.Type.Transform itemType
            Anonymous t |> QsTupleItem
        | QsTupleItem (Named item) -> 
            let loc  = item.Position, item.Range
            let t    = this.Scope.Expression.Type.Transform item.Type
            let mut, qDep = item.InferredInformation.IsMutable, item.InferredInformation.HasLocalQuantumDependency
            LocalVariableDeclaration<_>.New mut (loc, item.VariableName, t, qDep) |> Named |> QsTupleItem
            
    abstract member onArgumentTuple : QsArgumentTuple -> QsArgumentTuple
    default this.onArgumentTuple arg = 
        match arg with 
        | QsTuple items -> (items |> Seq.map this.onArgumentTuple).ToImmutableArray() |> QsTuple
        | QsTupleItem item -> 
            let loc  = item.Position, item.Range
            let t    = this.Scope.Expression.Type.Transform item.Type
            let mut, qDep = item.InferredInformation.IsMutable, item.InferredInformation.HasLocalQuantumDependency
            LocalVariableDeclaration<_>.New mut (loc, item.VariableName, t, qDep) |> QsTupleItem

    abstract member onSignature : ResolvedSignature -> ResolvedSignature
    default this.onSignature (s : ResolvedSignature) = 
        let typeParams = s.TypeParameters 
        let argType = this.Scope.Expression.Type.Transform s.ArgumentType
        let returnType = this.Scope.Expression.Type.Transform s.ReturnType
        let info = this.Scope.Expression.Type.onCallableInformation s.Information
        ResolvedSignature.New ((argType, returnType), info, typeParams)
    

    abstract member onExternalImplementation : unit -> unit
    default this.onExternalImplementation () = ()

    abstract member onIntrinsicImplementation : unit -> unit
    default this.onIntrinsicImplementation () = ()

    abstract member onProvidedImplementation : QsArgumentTuple * QsScope -> QsArgumentTuple * QsScope
    default this.onProvidedImplementation (argTuple, body) = 
        let argTuple = this.onArgumentTuple argTuple
        let body = this.Scope.Transform body
        argTuple, body

    abstract member onSelfInverseDirective : unit -> unit
    default this.onSelfInverseDirective () = ()

    abstract member onInvertDirective : unit -> unit
    default this.onInvertDirective () = ()

    abstract member onDistributeDirective : unit -> unit
    default this.onDistributeDirective () = ()

    abstract member onInvalidGeneratorDirective : unit -> unit
    default this.onInvalidGeneratorDirective () = ()

    member this.dispatchGeneratedImplementation (dir : QsGeneratorDirective) = 
        match this.beforeGeneratedImplementation dir with 
        | SelfInverse      -> this.onSelfInverseDirective ();     SelfInverse     
        | Invert           -> this.onInvertDirective();           Invert          
        | Distribute       -> this.onDistributeDirective();       Distribute      
        | InvalidGenerator -> this.onInvalidGeneratorDirective(); InvalidGenerator

    member this.dispatchSpecializationImplementation (impl : SpecializationImplementation) = 
        match this.beforeSpecializationImplementation impl with 
        | External                  -> this.onExternalImplementation();                  External
        | Intrinsic                 -> this.onIntrinsicImplementation();                 Intrinsic
        | Generated dir             -> this.dispatchGeneratedImplementation dir       |> Generated
        | Provided (argTuple, body) -> this.onProvidedImplementation (argTuple, body) |> Provided


    abstract member onSpecializationImplementation : QsSpecialization -> QsSpecialization
    default this.onSpecializationImplementation (spec : QsSpecialization) = 
        let source = this.onSourceFile spec.SourceFile
        let loc = this.onLocation spec.Location
        let doc = this.onDocumentation spec.Documentation
        let comments = spec.Comments
        let typeArgs = spec.TypeArguments |> QsNullable<_>.Map (fun args -> (args |> Seq.map this.Scope.Expression.Type.Transform).ToImmutableArray())
        let signature = this.onSignature spec.Signature
        let impl = this.dispatchSpecializationImplementation spec.Implementation 
        QsSpecialization.New spec.Kind (source, loc) (spec.Parent, typeArgs, signature, impl, doc, comments)

    abstract member onBodySpecialization : QsSpecialization -> QsSpecialization
    default this.onBodySpecialization spec = this.onSpecializationImplementation spec
    
    abstract member onAdjointSpecialization : QsSpecialization -> QsSpecialization
    default this.onAdjointSpecialization spec = this.onSpecializationImplementation spec

    abstract member onControlledSpecialization : QsSpecialization -> QsSpecialization
    default this.onControlledSpecialization spec = this.onSpecializationImplementation spec

    abstract member onControlledAdjointSpecialization : QsSpecialization -> QsSpecialization
    default this.onControlledAdjointSpecialization spec = this.onSpecializationImplementation spec

    member this.dispatchSpecialization (spec : QsSpecialization) = 
        let spec = this.beforeSpecialization spec
        match spec.Kind with 
        | QsSpecializationKind.QsBody               -> this.onBodySpecialization spec
        | QsSpecializationKind.QsAdjoint            -> this.onAdjointSpecialization spec
        | QsSpecializationKind.QsControlled         -> this.onControlledSpecialization spec
        | QsSpecializationKind.QsControlledAdjoint  -> this.onControlledAdjointSpecialization spec


    abstract member onType : QsCustomType -> QsCustomType
    default this.onType t =
        let source = this.onSourceFile t.SourceFile 
        let loc = this.onLocation t.Location
        let doc = this.onDocumentation t.Documentation
        let comments = t.Comments
        let underlyingType = this.Scope.Expression.Type.Transform t.Type
        let typeItems = this.onTypeItems t.TypeItems
        QsCustomType.New (source, loc) (t.FullName, typeItems, underlyingType, doc, comments)

    abstract member onCallableImplementation : QsCallable -> QsCallable
    default this.onCallableImplementation (c : QsCallable) = 
        let source = this.onSourceFile c.SourceFile
        let loc = this.onLocation c.Location
        let doc = this.onDocumentation c.Documentation
        let comments = c.Comments
        let signature = this.onSignature c.Signature
        let argTuple = this.onArgumentTuple c.ArgumentTuple
        let specializations = c.Specializations |> Seq.map this.dispatchSpecialization
        QsCallable.New c.Kind (source, loc) (c.FullName, argTuple, signature, specializations, doc, comments)

    abstract member onOperation : QsCallable -> QsCallable
    default this.onOperation c = this.onCallableImplementation c

    abstract member onFunction : QsCallable -> QsCallable
    default this.onFunction c = this.onCallableImplementation c

    abstract member onTypeConstructor : QsCallable -> QsCallable
    default this.onTypeConstructor c = this.onCallableImplementation c


    member this.dispatchCallable (c : QsCallable) = 
        let c = this.beforeCallable c
        match c.Kind with 
        | QsCallableKind.Function           -> this.onFunction c
        | QsCallableKind.Operation          -> this.onOperation c
        | QsCallableKind.TypeConstructor    -> this.onTypeConstructor c

    member this.dispatchNamespaceElement element = 
        match this.beforeNamespaceElement element with
        | QsCustomType t    -> t |> this.onType           |> QsCustomType
        | QsCallable c      -> c |> this.dispatchCallable |> QsCallable

    abstract member Transform : QsNamespace -> QsNamespace 
    default this.Transform ns = 
        let name = ns.Name
        let doc = ns.Documentation.AsEnumerable().SelectMany(fun entry -> 
            entry |> Seq.map (fun doc -> entry.Key, this.onDocumentation doc)).ToLookup(fst, snd)
        let elements = ns.Elements |> Seq.map this.dispatchNamespaceElement
        QsNamespace.New (name, elements, doc)

