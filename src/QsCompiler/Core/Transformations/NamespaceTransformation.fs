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
    let Node = if options.Rebuild then Fold else Walk

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


    // subconstructs used within declarations 

    abstract member OnLocation : QsNullable<QsLocation> -> QsNullable<QsLocation>
    default this.OnLocation l = l

    abstract member OnDocumentation : ImmutableArray<string> -> ImmutableArray<string>
    default this.OnDocumentation doc = doc

    abstract member OnSource : Source -> Source
    default this.OnSource source = source

    abstract member OnAttribute : QsDeclarationAttribute -> QsDeclarationAttribute
    default this.OnAttribute att = att 

    abstract member OnItemName : string -> string
    default this.OnItemName name = name

    abstract member OnTypeItems : QsTuple<QsTypeItem> -> QsTuple<QsTypeItem>
    default this.OnTypeItems tItem = 
        match tItem with 
        | QsTuple items as original -> 
            let transformed = items |> Seq.map this.OnTypeItems |> ImmutableArray.CreateRange
            QsTuple |> Node.BuildOr original transformed
        | QsTupleItem (Anonymous itemType) as original -> 
            let t = this.Statements.Expressions.Types.OnType itemType
            QsTupleItem << Anonymous |> Node.BuildOr original t
        | QsTupleItem (Named item) as original -> 
            let loc  = item.Position, item.Range
            let name = this.OnItemName item.VariableName
            let t    = this.Statements.Expressions.Types.OnType item.Type
            let info = this.Statements.Expressions.OnExpressionInformation item.InferredInformation
            QsTupleItem << Named << LocalVariableDeclaration<_>.New info.IsMutable |> Node.BuildOr original (loc, name, t, info.HasLocalQuantumDependency)
            
    abstract member OnArgumentName : QsLocalSymbol -> QsLocalSymbol
    default this.OnArgumentName arg = 
        match arg with 
        | ValidName name -> ValidName |> Node.BuildOr arg (this.Statements.OnVariableName name)
        | InvalidName -> arg

    abstract member OnArgumentTuple : QsArgumentTuple -> QsArgumentTuple
    default this.OnArgumentTuple arg = 
        match arg with 
        | QsTuple items as original -> 
            let transformed = items |> Seq.map this.OnArgumentTuple |> ImmutableArray.CreateRange
            QsTuple |> Node.BuildOr original transformed 
        | QsTupleItem item as original -> 
            let loc  = item.Position, item.Range
            let name = this.OnArgumentName item.VariableName
            let t    = this.Statements.Expressions.Types.OnType item.Type
            let info = this.Statements.Expressions.OnExpressionInformation item.InferredInformation
            QsTupleItem << LocalVariableDeclaration<_>.New info.IsMutable |> Node.BuildOr original (loc, name, t, info.HasLocalQuantumDependency)

    abstract member OnSignature : ResolvedSignature -> ResolvedSignature
    default this.OnSignature (s : ResolvedSignature) = 
        let typeParams = s.TypeParameters 
        let argType = this.Statements.Expressions.Types.OnType s.ArgumentType
        let returnType = this.Statements.Expressions.Types.OnType s.ReturnType
        let info = this.Statements.Expressions.Types.OnCallableInformation s.Information
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

    abstract member OnGeneratedImplementation : QsGeneratorDirective -> QsGeneratorDirective
    default this.OnGeneratedImplementation (directive : QsGeneratorDirective) = 
        match directive with 
        | SelfInverse      -> this.OnSelfInverseDirective ();     SelfInverse     
        | Invert           -> this.OnInvertDirective();           Invert          
        | Distribute       -> this.OnDistributeDirective();       Distribute      
        | InvalidGenerator -> this.OnInvalidGeneratorDirective(); InvalidGenerator

    abstract member OnSpecializationImplementation : SpecializationImplementation -> SpecializationImplementation
    default this.OnSpecializationImplementation (implementation : SpecializationImplementation) = 
        let Build kind transformed = kind |> Node.BuildOr implementation transformed
        match implementation with 
        | External                  -> this.OnExternalImplementation();                  External
        | Intrinsic                 -> this.OnIntrinsicImplementation();                 Intrinsic
        | Generated dir             -> this.OnGeneratedImplementation dir             |> Build Generated
        | Provided (argTuple, body) -> this.OnProvidedImplementation (argTuple, body) |> Build Provided

    /// This method is defined for the sole purpose of eliminating code duplication for each of the specialization kinds. 
    /// It is hence not intended and should never be needed for public use. 
    member private this.OnSpecializationKind (spec : QsSpecialization) = 
        let source = this.OnSource spec.Source
        let loc = this.OnLocation spec.Location
        let attributes = spec.Attributes |> Seq.map this.OnAttribute |> ImmutableArray.CreateRange
        let typeArgs = spec.TypeArguments |> QsNullable<_>.Map (fun args -> args |> Seq.map this.Statements.Expressions.Types.OnType |> ImmutableArray.CreateRange)
        let signature = this.OnSignature spec.Signature
        let impl = this.OnSpecializationImplementation spec.Implementation 
        let doc = this.OnDocumentation spec.Documentation
        let comments = spec.Comments
        QsSpecialization.New spec.Kind (source, loc) |> Node.BuildOr spec (spec.Parent, attributes, typeArgs, signature, impl, doc, comments)

    abstract member OnBodySpecialization : QsSpecialization -> QsSpecialization
    default this.OnBodySpecialization spec = this.OnSpecializationKind spec
    
    abstract member OnAdjointSpecialization : QsSpecialization -> QsSpecialization
    default this.OnAdjointSpecialization spec = this.OnSpecializationKind spec

    abstract member OnControlledSpecialization : QsSpecialization -> QsSpecialization
    default this.OnControlledSpecialization spec = this.OnSpecializationKind spec

    abstract member OnControlledAdjointSpecialization : QsSpecialization -> QsSpecialization
    default this.OnControlledAdjointSpecialization spec = this.OnSpecializationKind spec

    abstract member OnSpecializationDeclaration : QsSpecialization -> QsSpecialization
    default this.OnSpecializationDeclaration (spec : QsSpecialization) = 
        match spec.Kind with 
        | QsSpecializationKind.QsBody               -> this.OnBodySpecialization spec
        | QsSpecializationKind.QsAdjoint            -> this.OnAdjointSpecialization spec
        | QsSpecializationKind.QsControlled         -> this.OnControlledSpecialization spec
        | QsSpecializationKind.QsControlledAdjoint  -> this.OnControlledAdjointSpecialization spec

    
    // type and callable declarations

    /// This method is defined for the sole purpose of eliminating code duplication for each of the callable kinds. 
    /// It is hence not intended and should never be needed for public use. 
    member private this.OnCallableKind (c : QsCallable) = 
        let source = this.OnSource c.Source
        let loc = this.OnLocation c.Location
        let attributes = c.Attributes |> Seq.map this.OnAttribute |> ImmutableArray.CreateRange
        let signature = this.OnSignature c.Signature
        let argTuple = this.OnArgumentTuple c.ArgumentTuple
        let specializations = c.Specializations |> Seq.sortBy (fun c -> c.Kind) |> Seq.map this.OnSpecializationDeclaration |> ImmutableArray.CreateRange
        let doc = this.OnDocumentation c.Documentation
        let comments = c.Comments
        QsCallable.New c.Kind (source, loc) |> Node.BuildOr c (c.FullName, attributes, c.Modifiers, argTuple, signature, specializations, doc, comments)

    abstract member OnOperation : QsCallable -> QsCallable
    default this.OnOperation c = this.OnCallableKind c

    abstract member OnFunction : QsCallable -> QsCallable
    default this.OnFunction c = this.OnCallableKind c

    abstract member OnTypeConstructor : QsCallable -> QsCallable
    default this.OnTypeConstructor c = this.OnCallableKind c

    abstract member OnCallableDeclaration : QsCallable -> QsCallable
    default this.OnCallableDeclaration (c : QsCallable) = 
        match c.Kind with 
        | QsCallableKind.Function           -> this.OnFunction c
        | QsCallableKind.Operation          -> this.OnOperation c
        | QsCallableKind.TypeConstructor    -> this.OnTypeConstructor c

    abstract member OnTypeDeclaration : QsCustomType -> QsCustomType
    default this.OnTypeDeclaration t =
        let source = this.OnSource t.Source
        let loc = this.OnLocation t.Location
        let attributes = t.Attributes |> Seq.map this.OnAttribute |> ImmutableArray.CreateRange
        let underlyingType = this.Statements.Expressions.Types.OnType t.Type
        let typeItems = this.OnTypeItems t.TypeItems
        let doc = this.OnDocumentation t.Documentation
        let comments = t.Comments
        QsCustomType.New (source, loc) |> Node.BuildOr t (t.FullName, attributes, t.Modifiers, typeItems, underlyingType, doc, comments)


    // transformation roots called on each namespace or namespace element

    abstract member OnNamespaceElement : QsNamespaceElement -> QsNamespaceElement
    default this.OnNamespaceElement element = 
        if not options.Enable then element else
        match element with
        | QsCustomType t    -> t |> this.OnTypeDeclaration     |> QsCustomType
        | QsCallable c      -> c |> this.OnCallableDeclaration |> QsCallable

    abstract member OnNamespace : QsNamespace -> QsNamespace 
    default this.OnNamespace ns = 
        if not options.Enable then ns else
        let name = ns.Name
        let doc = ns.Documentation.AsEnumerable().SelectMany(fun entry -> 
            entry |> Seq.map (fun doc -> entry.Key, this.OnDocumentation doc)).ToLookup(fst, snd)
        let elements = ns.Elements |> Seq.map this.OnNamespaceElement
        if options.Rebuild then QsNamespace.New (name, elements |> ImmutableArray.CreateRange, doc)
        else elements |> Seq.iter ignore; ns
