// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

#nowarn "44" // TODO: RELEASE 2022-09, reenable after OnArgumentName is removed.

open System
open System.Collections.Immutable
open System.Linq
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Transformations.Core.Utils

type NamespaceTransformationBase(statementTransformation: _ -> StatementTransformationBase, options) =
    let node = if options.Rebuild then Fold else Walk

    member _.Types = statementTransformation().Expressions.Types

    member _.Expressions = statementTransformation().Expressions

    member _.Statements = statementTransformation ()

    member internal _.Common = statementTransformation().Expressions.Types.Common

    new(options) =
        let statements = StatementTransformationBase options
        NamespaceTransformationBase((fun () -> statements), options)

    new(statementTransformation) = NamespaceTransformationBase(statementTransformation, TransformationOptions.Default)

    new() = NamespaceTransformationBase TransformationOptions.Default

    // subconstructs used within declarations

    // TODO: RELEASE 2022-09: Remove member.
    [<Obsolete "Use SyntaxTreeTransformation.OnAbsoluteLocation instead">]
    abstract OnLocation: QsNullable<QsLocation> -> QsNullable<QsLocation>

    // TODO: RELEASE 2022-09: Remove member.
    [<Obsolete "Use SyntaxTreeTransformation.OnAbsoluteLocation instead">]
    override this.OnLocation loc = loc

    abstract OnDocumentation: ImmutableArray<string> -> ImmutableArray<string>
    default this.OnDocumentation doc = doc

    abstract OnSource: Source -> Source
    default this.OnSource source = source

    abstract OnAttribute: QsDeclarationAttribute -> QsDeclarationAttribute
    default this.OnAttribute att = att

    // TODO: RELEASE 2022-09: Remove member.
    [<Obsolete "Use SyntaxTreeTransformation.OnItemNameDeclaration instead">]
    abstract OnItemName: string -> string

    // TODO: RELEASE 2022-09: Remove member.
    [<Obsolete "Use SyntaxTreeTransformation.OnItemNameDeclaration instead">]
    override this.OnItemName name = name

    abstract OnTypeItems: QsTuple<QsTypeItem> -> QsTuple<QsTypeItem>

    default this.OnTypeItems tItem =
        match tItem with
        | QsTuple items as original ->
            let transformed = items |> Seq.map this.OnTypeItems |> ImmutableArray.CreateRange
            QsTuple |> node.BuildOr original transformed
        | QsTupleItem (Anonymous itemType) as original ->
            let t = this.Types.OnType itemType
            QsTupleItem << Anonymous |> node.BuildOr original t
        | QsTupleItem (Named item) as original ->
            let loc = this.Common.OnSymbolLocation(item.Position, item.Range)
            let name = this.Common.OnItemNameDeclaration item.VariableName
            let t = this.Types.OnType item.Type
            let info = this.Expressions.OnExpressionInformation item.InferredInformation

            QsTupleItem << Named << LocalVariableDeclaration.New info.IsMutable
            |> node.BuildOr original (loc, name, t, info.HasLocalQuantumDependency)

    // TODO: RELEASE 2022-09: Remove.
    [<Obsolete "Use SyntaxTreeTransformation.OnLocalNameDeclaration or override OnArgumentTuple instead.">]
    abstract OnArgumentName: QsLocalSymbol -> QsLocalSymbol

    // TODO: RELEASE 2022-09: Remove.
    [<Obsolete "Use SyntaxTreeTransformation.OnLocalNameDeclaration or override OnArgumentTuple instead.">]
    override this.OnArgumentName arg =
        match arg with
        | ValidName name -> this.Common.OnLocalNameDeclaration name |> ValidName
        | InvalidName -> arg

    // TODO: RELEASE 2022-09: Remove member.
    [<Obsolete "Use SyntaxTreeTransformation.OnArgumentTuple instead.">]
    abstract OnArgumentTuple: QsArgumentTuple -> QsArgumentTuple

    // TODO: RELEASE 2022-09: Make this an internal member. Keep the following comment:
    // do not expose this - this handle is exposed as a virtual member in the SyntaxTreeTransformation itself
    [<Obsolete "Use SyntaxTreeTransformation.OnArgumentTuple instead.">]
    override this.OnArgumentTuple arg =
        match arg with
        | QsTuple items as original ->
            let transformed = items |> Seq.map this.Common.OnArgumentTuple |> ImmutableArray.CreateRange
            QsTuple |> node.BuildOr original transformed
        | QsTupleItem item as original ->
            let loc = this.Common.OnSymbolLocation(item.Position, item.Range)
            let name = this.OnArgumentName item.VariableName // replace with the implementation once the deprecated member is removed
            let t = this.Types.OnType item.Type
            let info = this.Expressions.OnExpressionInformation item.InferredInformation
            let newDecl = LocalVariableDeclaration.New info.IsMutable (loc, name, t, info.HasLocalQuantumDependency)
            QsTupleItem |> node.BuildOr original newDecl

    abstract OnSignature: ResolvedSignature -> ResolvedSignature

    default this.OnSignature(s: ResolvedSignature) =
        let typeParams = s.TypeParameters // if this had a range is should be handled by the corresponding Common nodes
        let argType = this.Types.OnType s.ArgumentType
        let returnType = this.Types.OnType s.ReturnType
        let info = this.Types.OnCallableInformation s.Information
        ResolvedSignature.New |> node.BuildOr s ((argType, returnType), info, typeParams)

    // specialization declarations and implementations

    abstract OnProvidedImplementation: QsArgumentTuple * QsScope -> QsArgumentTuple * QsScope

    default this.OnProvidedImplementation(argTuple, body) =
        let argTuple = this.Common.OnArgumentTuple argTuple
        let body = this.Statements.OnScope body
        argTuple, body

    abstract OnSelfInverseDirective: unit -> unit
    default this.OnSelfInverseDirective() = ()

    abstract OnInvertDirective: unit -> unit
    default this.OnInvertDirective() = ()

    abstract OnDistributeDirective: unit -> unit
    default this.OnDistributeDirective() = ()

    abstract OnInvalidGeneratorDirective: unit -> unit
    default this.OnInvalidGeneratorDirective() = ()

    abstract OnExternalImplementation: unit -> unit
    default this.OnExternalImplementation() = ()

    abstract OnIntrinsicImplementation: unit -> unit
    default this.OnIntrinsicImplementation() = ()

    abstract OnGeneratedImplementation: QsGeneratorDirective -> QsGeneratorDirective

    default this.OnGeneratedImplementation(directive: QsGeneratorDirective) =
        match directive with
        | SelfInverse ->
            this.OnSelfInverseDirective()
            SelfInverse
        | Invert ->
            this.OnInvertDirective()
            Invert
        | Distribute ->
            this.OnDistributeDirective()
            Distribute
        | InvalidGenerator ->
            this.OnInvalidGeneratorDirective()
            InvalidGenerator

    abstract OnSpecializationImplementation: SpecializationImplementation -> SpecializationImplementation

    default this.OnSpecializationImplementation(implementation: SpecializationImplementation) =
        let build kind transformed =
            kind |> node.BuildOr implementation transformed

        match implementation with
        | External ->
            this.OnExternalImplementation()
            External
        | Intrinsic ->
            this.OnIntrinsicImplementation()
            Intrinsic
        | Generated dir -> this.OnGeneratedImplementation dir |> build Generated
        | Provided (argTuple, body) -> this.OnProvidedImplementation(argTuple, body) |> build Provided

    /// This method is defined for the sole purpose of eliminating code duplication for each of the specialization kinds.
    /// It is hence not intended and should never be needed for public use.
    member private this.OnSpecializationKind(spec: QsSpecialization) =
        let source = this.OnSource spec.Source
        let loc = this.Common.OnAbsoluteLocation spec.Location
        let attributes = spec.Attributes |> Seq.map this.OnAttribute |> ImmutableArray.CreateRange

        let typeArgs =
            spec.TypeArguments
            |> QsNullable<_>.Map (fun args -> args |> Seq.map this.Types.OnType |> ImmutableArray.CreateRange)

        let signature = this.OnSignature spec.Signature
        let impl = this.OnSpecializationImplementation spec.Implementation
        let doc = this.OnDocumentation spec.Documentation
        let comments = spec.Comments

        QsSpecialization.New spec.Kind (source, loc)
        |> node.BuildOr spec (spec.Parent, attributes, typeArgs, signature, impl, doc, comments)

    abstract OnBodySpecialization: QsSpecialization -> QsSpecialization
    default this.OnBodySpecialization spec = this.OnSpecializationKind spec

    abstract OnAdjointSpecialization: QsSpecialization -> QsSpecialization
    default this.OnAdjointSpecialization spec = this.OnSpecializationKind spec

    abstract OnControlledSpecialization: QsSpecialization -> QsSpecialization
    default this.OnControlledSpecialization spec = this.OnSpecializationKind spec

    abstract OnControlledAdjointSpecialization: QsSpecialization -> QsSpecialization
    default this.OnControlledAdjointSpecialization spec = this.OnSpecializationKind spec

    abstract OnSpecializationDeclaration: QsSpecialization -> QsSpecialization

    default this.OnSpecializationDeclaration(spec: QsSpecialization) =
        match spec.Kind with
        | QsSpecializationKind.QsBody -> this.OnBodySpecialization spec
        | QsSpecializationKind.QsAdjoint -> this.OnAdjointSpecialization spec
        | QsSpecializationKind.QsControlled -> this.OnControlledSpecialization spec
        | QsSpecializationKind.QsControlledAdjoint -> this.OnControlledAdjointSpecialization spec

    // type and callable declarations

    /// This method is defined for the sole purpose of eliminating code duplication for each of the callable kinds.
    /// It is hence not intended and should never be needed for public use.
    member private this.OnCallableKind(c: QsCallable) =
        let source = this.OnSource c.Source
        let loc = this.Common.OnAbsoluteLocation c.Location
        let attributes = c.Attributes |> Seq.map this.OnAttribute |> ImmutableArray.CreateRange
        let signature = this.OnSignature c.Signature
        let argTuple = this.Common.OnArgumentTuple c.ArgumentTuple

        let specializations =
            c.Specializations
            |> Seq.sortBy (fun c -> c.Kind)
            |> Seq.map this.OnSpecializationDeclaration
            |> ImmutableArray.CreateRange

        let doc = this.OnDocumentation c.Documentation
        let comments = c.Comments

        QsCallable.New c.Kind (source, loc)
        |> node.BuildOr c (c.FullName, attributes, c.Access, argTuple, signature, specializations, doc, comments)

    abstract OnOperation: QsCallable -> QsCallable
    default this.OnOperation c = this.OnCallableKind c

    abstract OnFunction: QsCallable -> QsCallable
    default this.OnFunction c = this.OnCallableKind c

    abstract OnTypeConstructor: QsCallable -> QsCallable
    default this.OnTypeConstructor c = this.OnCallableKind c

    abstract OnCallableDeclaration: QsCallable -> QsCallable

    default this.OnCallableDeclaration(c: QsCallable) =
        match c.Kind with
        | QsCallableKind.Function -> this.OnFunction c
        | QsCallableKind.Operation -> this.OnOperation c
        | QsCallableKind.TypeConstructor -> this.OnTypeConstructor c

    abstract OnTypeDeclaration: QsCustomType -> QsCustomType

    default this.OnTypeDeclaration t =
        let source = this.OnSource t.Source
        let loc = this.Common.OnAbsoluteLocation t.Location
        let attributes = t.Attributes |> Seq.map this.OnAttribute |> ImmutableArray.CreateRange
        let underlyingType = this.Types.OnType t.Type
        let typeItems = this.OnTypeItems t.TypeItems
        let doc = this.OnDocumentation t.Documentation
        let comments = t.Comments

        QsCustomType.New(source, loc)
        |> node.BuildOr t (t.FullName, attributes, t.Access, typeItems, underlyingType, doc, comments)

    // transformation roots called on each namespace or namespace element

    abstract OnNamespaceElement: QsNamespaceElement -> QsNamespaceElement

    default this.OnNamespaceElement element =
        if not options.Enable then
            element
        else
            match element with
            | QsCustomType t -> t |> this.OnTypeDeclaration |> QsCustomType
            | QsCallable c -> c |> this.OnCallableDeclaration |> QsCallable

    abstract OnNamespace: QsNamespace -> QsNamespace

    default this.OnNamespace ns =
        if not options.Enable then
            ns
        else
            let name = ns.Name

            let doc =
                ns
                    .Documentation
                    .AsEnumerable()
                    .SelectMany(fun entry -> entry |> Seq.map (fun doc -> entry.Key, this.OnDocumentation doc))
                    .ToLookup(fst, snd)

            let elements = ns.Elements |> Seq.map this.OnNamespaceElement

            if options.Rebuild then
                QsNamespace.New(name, elements |> ImmutableArray.CreateRange, doc)
            else
                elements |> Seq.iter ignore
                ns
