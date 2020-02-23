// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

open System
open System.Collections.Immutable
open System.Linq
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core.Utils

type QsArgumentTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>


type NamespaceTransformationBase internal (options : TransformationOptions, _internal_) =

    let missingTransformation name _ = new InvalidOperationException(sprintf "No %s transformation has been specified." name) |> raise 
    let Node = if options.DisableRebuild then Walk else Fold

    member val internal StatementTransformationHandle = missingTransformation "statement" with get, set
    member this.Statements = this.StatementTransformationHandle()

    new (statementTransformation : unit -> StatementTransformationBase, options : TransformationOptions) as this = 
        new NamespaceTransformationBase(options, "_internal_") then 
            this.StatementTransformationHandle <- statementTransformation

    new (options : TransformationOptions) as this = 
        new NamespaceTransformationBase(options, "_internal_") then
            let statementTransformation = new StatementTransformationBase(options)
            this.StatementTransformationHandle <- fun _ -> statementTransformation

    new (statementTransformation : unit -> StatementTransformationBase) =
        new NamespaceTransformationBase(statementTransformation, TransformationOptions.Default)

    new () = new NamespaceTransformationBase(TransformationOptions.Default)


    // methods invoked before selective nodes

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


    // subconstructs used within declarations 

    abstract member onLocation : QsNullable<QsLocation> -> QsNullable<QsLocation>
    default this.onLocation l = l

    abstract member onDocumentation : ImmutableArray<string> -> ImmutableArray<string>
    default this.onDocumentation doc = doc

    abstract member onSourceFile : NonNullable<string> -> NonNullable<string>
    default this.onSourceFile f = f

    abstract member onAttribute : QsDeclarationAttribute -> QsDeclarationAttribute
    default this.onAttribute att = att 

    abstract member onTypeItems : QsTuple<QsTypeItem> -> QsTuple<QsTypeItem>
    default this.onTypeItems tItem = 
        match tItem with 
        | QsTuple items as original -> 
            let transformed = items |> Seq.map this.onTypeItems |> ImmutableArray.CreateRange
            QsTuple |> Node.BuildOr original transformed
        | QsTupleItem (Anonymous itemType) as original -> 
            let t = this.Statements.Expressions.Types.Transform itemType
            QsTupleItem << Anonymous |> Node.BuildOr original t
        | QsTupleItem (Named item) as original -> 
            let loc  = item.Position, item.Range
            let t    = this.Statements.Expressions.Types.Transform item.Type
            let info = this.Statements.Expressions.onExpressionInformation item.InferredInformation
            QsTupleItem << Named << LocalVariableDeclaration<_>.New info.IsMutable |> Node.BuildOr original (loc, item.VariableName, t, info.HasLocalQuantumDependency)
            
    abstract member onArgumentTuple : QsArgumentTuple -> QsArgumentTuple
    default this.onArgumentTuple arg = 
        match arg with 
        | QsTuple items as original -> 
            let transformed = items |> Seq.map this.onArgumentTuple |> ImmutableArray.CreateRange
            QsTuple |> Node.BuildOr original transformed 
        | QsTupleItem item as original -> 
            let loc  = item.Position, item.Range
            let t    = this.Statements.Expressions.Types.Transform item.Type
            let info = this.Statements.Expressions.onExpressionInformation item.InferredInformation
            QsTupleItem << LocalVariableDeclaration<_>.New info.IsMutable |> Node.BuildOr original (loc, item.VariableName, t, info.HasLocalQuantumDependency)

    abstract member onSignature : ResolvedSignature -> ResolvedSignature
    default this.onSignature (s : ResolvedSignature) = 
        let typeParams = s.TypeParameters 
        let argType = this.Statements.Expressions.Types.Transform s.ArgumentType
        let returnType = this.Statements.Expressions.Types.Transform s.ReturnType
        let info = this.Statements.Expressions.Types.onCallableInformation s.Information
        ResolvedSignature.New |> Node.BuildOr s ((argType, returnType), info, typeParams)

    
    // specialization declarations and implementations

    abstract member onProvidedImplementation : QsArgumentTuple * QsScope -> QsArgumentTuple * QsScope
    default this.onProvidedImplementation (argTuple, body) = 
        let argTuple = this.onArgumentTuple argTuple
        let body = this.Statements.Transform body
        argTuple, body

    abstract member onSelfInverseDirective : unit -> unit
    default this.onSelfInverseDirective () = ()

    abstract member onInvertDirective : unit -> unit
    default this.onInvertDirective () = ()

    abstract member onDistributeDirective : unit -> unit
    default this.onDistributeDirective () = ()

    abstract member onInvalidGeneratorDirective : unit -> unit
    default this.onInvalidGeneratorDirective () = ()

    abstract member onExternalImplementation : unit -> unit
    default this.onExternalImplementation () = ()

    abstract member onIntrinsicImplementation : unit -> unit
    default this.onIntrinsicImplementation () = ()

    member this.dispatchGeneratedImplementation (dir : QsGeneratorDirective) = 
        match this.beforeGeneratedImplementation dir with 
        | SelfInverse      -> this.onSelfInverseDirective ();     SelfInverse     
        | Invert           -> this.onInvertDirective();           Invert          
        | Distribute       -> this.onDistributeDirective();       Distribute      
        | InvalidGenerator -> this.onInvalidGeneratorDirective(); InvalidGenerator

    member this.dispatchSpecializationImplementation (impl : SpecializationImplementation) = 
        let Build kind transformed = kind |> Node.BuildOr impl transformed
        match this.beforeSpecializationImplementation impl with 
        | External                  -> this.onExternalImplementation();                  External
        | Intrinsic                 -> this.onIntrinsicImplementation();                 Intrinsic
        | Generated dir             -> this.dispatchGeneratedImplementation dir       |> Build Generated
        | Provided (argTuple, body) -> this.onProvidedImplementation (argTuple, body) |> Build Provided

    abstract member onSpecializationImplementation : QsSpecialization -> QsSpecialization
    default this.onSpecializationImplementation (spec : QsSpecialization) = 
        let source = this.onSourceFile spec.SourceFile
        let loc = this.onLocation spec.Location
        let attributes = spec.Attributes |> Seq.map this.onAttribute |> ImmutableArray.CreateRange
        let typeArgs = spec.TypeArguments |> QsNullable<_>.Map (fun args -> args |> Seq.map this.Statements.Expressions.Types.Transform |> ImmutableArray.CreateRange)
        let signature = this.onSignature spec.Signature
        let impl = this.dispatchSpecializationImplementation spec.Implementation 
        let doc = this.onDocumentation spec.Documentation
        let comments = spec.Comments
        QsSpecialization.New spec.Kind (source, loc) |> Node.BuildOr spec (spec.Parent, attributes, typeArgs, signature, impl, doc, comments)

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

    
    // type and callable declarations and implementations

    abstract member onType : QsCustomType -> QsCustomType
    default this.onType t =
        let source = this.onSourceFile t.SourceFile 
        let loc = this.onLocation t.Location
        let attributes = t.Attributes |> Seq.map this.onAttribute |> ImmutableArray.CreateRange
        let underlyingType = this.Statements.Expressions.Types.Transform t.Type
        let typeItems = this.onTypeItems t.TypeItems
        let doc = this.onDocumentation t.Documentation
        let comments = t.Comments
        QsCustomType.New (source, loc) |> Node.BuildOr t (t.FullName, attributes, typeItems, underlyingType, doc, comments)

    abstract member onCallableImplementation : QsCallable -> QsCallable
    default this.onCallableImplementation (c : QsCallable) = 
        let source = this.onSourceFile c.SourceFile
        let loc = this.onLocation c.Location
        let attributes = c.Attributes |> Seq.map this.onAttribute |> ImmutableArray.CreateRange
        let signature = this.onSignature c.Signature
        let argTuple = this.onArgumentTuple c.ArgumentTuple
        let specializations = c.Specializations |> Seq.sortBy (fun c -> c.Kind) |> Seq.map this.dispatchSpecialization |> ImmutableArray.CreateRange
        let doc = this.onDocumentation c.Documentation
        let comments = c.Comments
        QsCallable.New c.Kind (source, loc) |> Node.BuildOr c (c.FullName, attributes, argTuple, signature, specializations, doc, comments)

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


    // transformation roots called on each namespace

    member this.dispatchNamespaceElement element = 
        match this.beforeNamespaceElement element with
        | QsCustomType t    -> t |> this.onType           |> QsCustomType
        | QsCallable c      -> c |> this.dispatchCallable |> QsCallable

    abstract member Transform : QsNamespace -> QsNamespace 
    default this.Transform ns = 
        if options.Disable then ns else
        let name = ns.Name
        let doc = ns.Documentation.AsEnumerable().SelectMany(fun entry -> 
            entry |> Seq.map (fun doc -> entry.Key, this.onDocumentation doc)).ToLookup(fst, snd)
        let elements = ns.Elements |> Seq.map this.dispatchNamespaceElement |> ImmutableArray.CreateRange
        QsNamespace.New |> Node.BuildOr ns (name, elements, doc)

