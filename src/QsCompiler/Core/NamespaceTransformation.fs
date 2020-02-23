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

    abstract member BeforeNamespaceElement : QsNamespaceElement -> QsNamespaceElement
    default this.BeforeNamespaceElement e = e

    abstract member BeforeCallable : QsCallable -> QsCallable
    default this.BeforeCallable c = c

    abstract member BeforeSpecialization : QsSpecialization -> QsSpecialization
    default this.BeforeSpecialization spec = spec

    abstract member BeforeSpecializationImplementation : SpecializationImplementation -> SpecializationImplementation
    default this.BeforeSpecializationImplementation impl = impl

    abstract member BeforeGeneratedImplementation : QsGeneratorDirective -> QsGeneratorDirective
    default this.BeforeGeneratedImplementation dir = dir


    // subconstructs used within declarations 

    abstract member OnLocation : QsNullable<QsLocation> -> QsNullable<QsLocation>
    default this.OnLocation l = l

    abstract member OnDocumentation : ImmutableArray<string> -> ImmutableArray<string>
    default this.OnDocumentation doc = doc

    abstract member OnSourceFile : NonNullable<string> -> NonNullable<string>
    default this.OnSourceFile f = f

    abstract member OnAttribute : QsDeclarationAttribute -> QsDeclarationAttribute
    default this.OnAttribute att = att 

    abstract member OnTypeItems : QsTuple<QsTypeItem> -> QsTuple<QsTypeItem>
    default this.OnTypeItems tItem = 
        match tItem with 
        | QsTuple items as original -> 
            let transformed = items |> Seq.map this.OnTypeItems |> ImmutableArray.CreateRange
            QsTuple |> Node.BuildOr original transformed
        | QsTupleItem (Anonymous itemType) as original -> 
            let t = this.Statements.Expressions.Types.onType itemType
            QsTupleItem << Anonymous |> Node.BuildOr original t
        | QsTupleItem (Named item) as original -> 
            let loc  = item.Position, item.Range
            let t    = this.Statements.Expressions.Types.onType item.Type
            let info = this.Statements.Expressions.onExpressionInformation item.InferredInformation
            QsTupleItem << Named << LocalVariableDeclaration<_>.New info.IsMutable |> Node.BuildOr original (loc, item.VariableName, t, info.HasLocalQuantumDependency)
            
    abstract member OnArgumentTuple : QsArgumentTuple -> QsArgumentTuple
    default this.OnArgumentTuple arg = 
        match arg with 
        | QsTuple items as original -> 
            let transformed = items |> Seq.map this.OnArgumentTuple |> ImmutableArray.CreateRange
            QsTuple |> Node.BuildOr original transformed 
        | QsTupleItem item as original -> 
            let loc  = item.Position, item.Range
            let t    = this.Statements.Expressions.Types.onType item.Type
            let info = this.Statements.Expressions.onExpressionInformation item.InferredInformation
            QsTupleItem << LocalVariableDeclaration<_>.New info.IsMutable |> Node.BuildOr original (loc, item.VariableName, t, info.HasLocalQuantumDependency)

    abstract member OnSignature : ResolvedSignature -> ResolvedSignature
    default this.OnSignature (s : ResolvedSignature) = 
        let typeParams = s.TypeParameters 
        let argType = this.Statements.Expressions.Types.onType s.ArgumentType
        let returnType = this.Statements.Expressions.Types.onType s.ReturnType
        let info = this.Statements.Expressions.Types.onCallableInformation s.Information
        ResolvedSignature.New |> Node.BuildOr s ((argType, returnType), info, typeParams)

    
    // specialization declarations and implementations

    abstract member OnProvidedImplementation : QsArgumentTuple * QsScope -> QsArgumentTuple * QsScope
    default this.OnProvidedImplementation (argTuple, body) = 
        let argTuple = this.OnArgumentTuple argTuple
        let body = this.Statements.OnScope body
        argTuple, body

    abstract member OnSelfInverseDirective : unit -> unit
    default this.OnSelfInverseDirective () = ()

    abstract member OnInvertDirective : unit -> unit
    default this.OnInvertDirective () = ()

    abstract member OnDistributeDirective : unit -> unit
    default this.OnDistributeDirective () = ()

    abstract member OnInvalidGeneratorDirective : unit -> unit
    default this.OnInvalidGeneratorDirective () = ()

    abstract member OnExternalImplementation : unit -> unit
    default this.OnExternalImplementation () = ()

    abstract member OnIntrinsicImplementation : unit -> unit
    default this.OnIntrinsicImplementation () = ()

    member this.DispatchGeneratedImplementation (dir : QsGeneratorDirective) = 
        match this.BeforeGeneratedImplementation dir with 
        | SelfInverse      -> this.OnSelfInverseDirective ();     SelfInverse     
        | Invert           -> this.OnInvertDirective();           Invert          
        | Distribute       -> this.OnDistributeDirective();       Distribute      
        | InvalidGenerator -> this.OnInvalidGeneratorDirective(); InvalidGenerator

    member this.DispatchSpecializationImplementation (impl : SpecializationImplementation) = 
        let Build kind transformed = kind |> Node.BuildOr impl transformed
        match this.BeforeSpecializationImplementation impl with 
        | External                  -> this.OnExternalImplementation();                  External
        | Intrinsic                 -> this.OnIntrinsicImplementation();                 Intrinsic
        | Generated dir             -> this.DispatchGeneratedImplementation dir       |> Build Generated
        | Provided (argTuple, body) -> this.OnProvidedImplementation (argTuple, body) |> Build Provided

    abstract member OnSpecializationImplementation : QsSpecialization -> QsSpecialization
    default this.OnSpecializationImplementation (spec : QsSpecialization) = 
        let source = this.OnSourceFile spec.SourceFile
        let loc = this.OnLocation spec.Location
        let attributes = spec.Attributes |> Seq.map this.OnAttribute |> ImmutableArray.CreateRange
        let typeArgs = spec.TypeArguments |> QsNullable<_>.Map (fun args -> args |> Seq.map this.Statements.Expressions.Types.onType |> ImmutableArray.CreateRange)
        let signature = this.OnSignature spec.Signature
        let impl = this.DispatchSpecializationImplementation spec.Implementation 
        let doc = this.OnDocumentation spec.Documentation
        let comments = spec.Comments
        QsSpecialization.New spec.Kind (source, loc) |> Node.BuildOr spec (spec.Parent, attributes, typeArgs, signature, impl, doc, comments)

    abstract member OnBodySpecialization : QsSpecialization -> QsSpecialization
    default this.OnBodySpecialization spec = this.OnSpecializationImplementation spec
    
    abstract member OnAdjointSpecialization : QsSpecialization -> QsSpecialization
    default this.OnAdjointSpecialization spec = this.OnSpecializationImplementation spec

    abstract member OnControlledSpecialization : QsSpecialization -> QsSpecialization
    default this.OnControlledSpecialization spec = this.OnSpecializationImplementation spec

    abstract member OnControlledAdjointSpecialization : QsSpecialization -> QsSpecialization
    default this.OnControlledAdjointSpecialization spec = this.OnSpecializationImplementation spec

    member this.DispatchSpecialization (spec : QsSpecialization) = 
        let spec = this.BeforeSpecialization spec
        match spec.Kind with 
        | QsSpecializationKind.QsBody               -> this.OnBodySpecialization spec
        | QsSpecializationKind.QsAdjoint            -> this.OnAdjointSpecialization spec
        | QsSpecializationKind.QsControlled         -> this.OnControlledSpecialization spec
        | QsSpecializationKind.QsControlledAdjoint  -> this.OnControlledAdjointSpecialization spec

    
    // type and callable declarations and implementations

    abstract member OnType : QsCustomType -> QsCustomType
    default this.OnType t =
        let source = this.OnSourceFile t.SourceFile 
        let loc = this.OnLocation t.Location
        let attributes = t.Attributes |> Seq.map this.OnAttribute |> ImmutableArray.CreateRange
        let underlyingType = this.Statements.Expressions.Types.onType t.Type
        let typeItems = this.OnTypeItems t.TypeItems
        let doc = this.OnDocumentation t.Documentation
        let comments = t.Comments
        QsCustomType.New (source, loc) |> Node.BuildOr t (t.FullName, attributes, typeItems, underlyingType, doc, comments)

    abstract member OnCallableImplementation : QsCallable -> QsCallable
    default this.OnCallableImplementation (c : QsCallable) = 
        let source = this.OnSourceFile c.SourceFile
        let loc = this.OnLocation c.Location
        let attributes = c.Attributes |> Seq.map this.OnAttribute |> ImmutableArray.CreateRange
        let signature = this.OnSignature c.Signature
        let argTuple = this.OnArgumentTuple c.ArgumentTuple
        let specializations = c.Specializations |> Seq.sortBy (fun c -> c.Kind) |> Seq.map this.DispatchSpecialization |> ImmutableArray.CreateRange
        let doc = this.OnDocumentation c.Documentation
        let comments = c.Comments
        QsCallable.New c.Kind (source, loc) |> Node.BuildOr c (c.FullName, attributes, argTuple, signature, specializations, doc, comments)

    abstract member OnOperation : QsCallable -> QsCallable
    default this.OnOperation c = this.OnCallableImplementation c

    abstract member OnFunction : QsCallable -> QsCallable
    default this.OnFunction c = this.OnCallableImplementation c

    abstract member OnTypeConstructor : QsCallable -> QsCallable
    default this.OnTypeConstructor c = this.OnCallableImplementation c

    member this.DispatchCallable (c : QsCallable) = 
        let c = this.BeforeCallable c
        match c.Kind with 
        | QsCallableKind.Function           -> this.OnFunction c
        | QsCallableKind.Operation          -> this.OnOperation c
        | QsCallableKind.TypeConstructor    -> this.OnTypeConstructor c


    // transformation roots called on each namespace

    member this.DispatchNamespaceElement element = 
        match this.BeforeNamespaceElement element with
        | QsCustomType t    -> t |> this.OnType           |> QsCustomType
        | QsCallable c      -> c |> this.DispatchCallable |> QsCallable

    abstract member OnNamespace : QsNamespace -> QsNamespace 
    default this.OnNamespace ns = 
        if options.Disable then ns else
        let name = ns.Name
        let doc = ns.Documentation.AsEnumerable().SelectMany(fun entry -> 
            entry |> Seq.map (fun doc -> entry.Key, this.OnDocumentation doc)).ToLookup(fst, snd)
        let elements = ns.Elements |> Seq.map this.DispatchNamespaceElement |> ImmutableArray.CreateRange
        QsNamespace.New |> Node.BuildOr ns (name, elements, doc)

