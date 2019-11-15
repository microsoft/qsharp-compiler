// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

open System.Collections.Immutable
open System.Linq
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree

//type QsArgumentTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>


/// Convention: 
/// All methods starting with "on" implement the walk for an expression of a certain kind.
/// All methods starting with "before" group a set of statements, and are called before walking the set
/// even if the corresponding walk routine (starting with "on") is overridden.
/// 
/// These classes differ from the "*Transformation" classes in that these classes visit every node in the
/// syntax tree, but don't create a new syntax tree, while the Transformation classes generate a new (or
/// at least partially new) tree from the old one.
/// Effectively, the Transformation classes implement fold, while the Walker classes implement iter.
type SyntaxTreeWalker() =
    let scopeWalker = new ScopeWalker()

    abstract member Scope : ScopeWalker
    default this.Scope = scopeWalker


    abstract member beforeNamespaceElement : QsNamespaceElement -> unit
    default this.beforeNamespaceElement e = ()

    abstract member beforeCallable : QsCallable -> unit
    default this.beforeCallable c = ()

    abstract member beforeSpecialization : QsSpecialization -> unit
    default this.beforeSpecialization spec = ()

    abstract member beforeSpecializationImplementation : SpecializationImplementation -> unit
    default this.beforeSpecializationImplementation impl = ()

    abstract member beforeGeneratedImplementation : QsGeneratorDirective -> unit
    default this.beforeGeneratedImplementation dir = ()


    abstract member onLocation : QsLocation -> unit
    default this.onLocation l = ()

    abstract member onDocumentation : ImmutableArray<string> -> unit
    default this.onDocumentation doc = ()

    abstract member onSourceFile : NonNullable<string> -> unit
    default this.onSourceFile f = ()

    abstract member onTypeItems : QsTuple<QsTypeItem> -> unit
    default this.onTypeItems tItem = 
        match tItem with 
        | QsTuple items -> items |> Seq.iter this.onTypeItems
        | QsTupleItem (Anonymous itemType) -> this.Scope.Expression.Type.Walk itemType
        | QsTupleItem (Named item) -> this.Scope.Expression.Type.Walk item.Type
            
    abstract member onArgumentTuple : QsArgumentTuple -> unit
    default this.onArgumentTuple arg = 
        match arg with 
        | QsTuple items -> items |> Seq.iter this.onArgumentTuple
        | QsTupleItem item -> this.Scope.Expression.Type.Walk item.Type

    abstract member onSignature : ResolvedSignature -> unit
    default this.onSignature (s : ResolvedSignature) = 
        this.Scope.Expression.Type.Walk s.ArgumentType
        this.Scope.Expression.Type.Walk s.ReturnType
        this.Scope.Expression.Type.onCallableInformation s.Information
    

    abstract member onExternalImplementation : unit -> unit
    default this.onExternalImplementation () = ()

    abstract member onIntrinsicImplementation : unit -> unit
    default this.onIntrinsicImplementation () = ()

    abstract member onProvidedImplementation : QsArgumentTuple * QsScope -> unit
    default this.onProvidedImplementation (argTuple, body) = 
        this.onArgumentTuple argTuple
        this.Scope.Walk body

    abstract member onSelfInverseDirective : unit -> unit
    default this.onSelfInverseDirective () = ()

    abstract member onInvertDirective : unit -> unit
    default this.onInvertDirective () = ()

    abstract member onDistributeDirective : unit -> unit
    default this.onDistributeDirective () = ()

    abstract member onInvalidGeneratorDirective : unit -> unit
    default this.onInvalidGeneratorDirective () = ()

    member this.dispatchGeneratedImplementation (dir : QsGeneratorDirective) = 
        this.beforeGeneratedImplementation dir
        match dir with 
        | SelfInverse      -> this.onSelfInverseDirective ()
        | Invert           -> this.onInvertDirective()
        | Distribute       -> this.onDistributeDirective()
        | InvalidGenerator -> this.onInvalidGeneratorDirective()

    member this.dispatchSpecializationImplementation (impl : SpecializationImplementation) = 
        this.beforeSpecializationImplementation impl
        match impl with 
        | External                  -> this.onExternalImplementation()
        | Intrinsic                 -> this.onIntrinsicImplementation()
        | Generated dir             -> this.dispatchGeneratedImplementation dir
        | Provided (argTuple, body) -> this.onProvidedImplementation (argTuple, body)


    abstract member onSpecializationImplementation : QsSpecialization -> unit
    default this.onSpecializationImplementation (spec : QsSpecialization) = 
        this.onSourceFile spec.SourceFile
        this.onLocation spec.Location
        spec.Attributes |> Seq.iter this.onAttribute
        spec.TypeArguments |> QsNullable<_>.Iter (fun args -> (args |> Seq.iter this.Scope.Expression.Type.Walk))
        this.onSignature spec.Signature
        this.dispatchSpecializationImplementation spec.Implementation 
        this.onDocumentation spec.Documentation

    abstract member onBodySpecialization : QsSpecialization -> unit
    default this.onBodySpecialization spec = this.onSpecializationImplementation spec
    
    abstract member onAdjointSpecialization : QsSpecialization -> unit
    default this.onAdjointSpecialization spec = this.onSpecializationImplementation spec

    abstract member onControlledSpecialization : QsSpecialization -> unit
    default this.onControlledSpecialization spec = this.onSpecializationImplementation spec

    abstract member onControlledAdjointSpecialization : QsSpecialization -> unit
    default this.onControlledAdjointSpecialization spec = this.onSpecializationImplementation spec

    member this.dispatchSpecialization (spec : QsSpecialization) = 
        this.beforeSpecialization spec
        match spec.Kind with 
        | QsSpecializationKind.QsBody               -> this.onBodySpecialization spec
        | QsSpecializationKind.QsAdjoint            -> this.onAdjointSpecialization spec
        | QsSpecializationKind.QsControlled         -> this.onControlledSpecialization spec
        | QsSpecializationKind.QsControlledAdjoint  -> this.onControlledAdjointSpecialization spec


    abstract member onType : QsCustomType -> unit
    default this.onType t =
        this.onSourceFile t.SourceFile 
        this.onLocation t.Location
        t.Attributes |> Seq.iter this.onAttribute
        this.Scope.Expression.Type.Walk t.Type
        this.onTypeItems t.TypeItems
        this.onDocumentation t.Documentation

    abstract member onCallableImplementation : QsCallable -> unit
    default this.onCallableImplementation (c : QsCallable) = 
        this.onSourceFile c.SourceFile
        this.onLocation c.Location
        c.Attributes |> Seq.iter this.onAttribute
        this.onSignature c.Signature
        this.onArgumentTuple c.ArgumentTuple
        c.Specializations |> Seq.iter this.dispatchSpecialization
        this.onDocumentation c.Documentation

    abstract member onOperation : QsCallable -> unit
    default this.onOperation c = this.onCallableImplementation c

    abstract member onFunction : QsCallable -> unit
    default this.onFunction c = this.onCallableImplementation c

    abstract member onTypeConstructor : QsCallable -> unit
    default this.onTypeConstructor c = this.onCallableImplementation c

    member this.dispatchCallable (c : QsCallable) = 
        this.beforeCallable c
        match c.Kind with 
        | QsCallableKind.Function           -> this.onFunction c
        | QsCallableKind.Operation          -> this.onOperation c
        | QsCallableKind.TypeConstructor    -> this.onTypeConstructor c


    abstract member onAttribute : QsDeclarationAttribute -> unit
    default this.onAttribute att = () 

    member this.dispatchNamespaceElement element = 
        this.beforeNamespaceElement element
        match element with
        | QsCustomType t    -> t |> this.onType
        | QsCallable c      -> c |> this.dispatchCallable

    abstract member Walk : QsNamespace -> unit 
    default this.Walk ns = 
        ns.Documentation.AsEnumerable() |> Seq.iter (fun grouping -> grouping |> Seq.iter this.onDocumentation)
        ns.Elements |> Seq.iter this.dispatchNamespaceElement
