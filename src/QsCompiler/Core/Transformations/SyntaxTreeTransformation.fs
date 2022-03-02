﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

#nowarn "44" // RELEASE 2022-09: Re-enable after updating the ICommonTransformation implementations.

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

// setup for syntax tree transformations with internal state

type SyntaxTreeTransformation<'T> private (state: 'T, options: TransformationOptions, _internal_: string) as this =
    /// Transformation invoked for all types encountered when traversing (parts of) the syntax tree.
    member val Types = TypeTransformation<'T>(this, TransformationOptions.Default) with get, set

    /// Transformation invoked for all expression kinds encountered when traversing (parts of) the syntax tree.
    member val ExpressionKinds = ExpressionKindTransformation<'T>(this, TransformationOptions.Default) with get, set

    /// Transformation invoked for all expressions encountered when traversing (parts of) the syntax tree.
    member val Expressions = ExpressionTransformation<'T>(this, TransformationOptions.Default) with get, set

    /// Transformation invoked for all statement kinds encountered when traversing (parts of) the syntax tree.
    member val StatementKinds = StatementKindTransformation<'T>(this, TransformationOptions.Default) with get, set

    /// Transformation invoked for all statements encountered when traversing (parts of) the syntax tree.
    member val Statements = StatementTransformation<'T>(this, TransformationOptions.Default) with get, set

    /// Transformation invoked for all namespaces encountered when traversing (parts of) the syntax tree.
    member val Namespaces = new NamespaceTransformation<'T>(TransformationOptions.Default, _internal_) with get, set

    /// Invokes the transformation for all namespaces in the given compilation.
    member this.OnCompilation compilation =
        if options.Rebuild then
            let namespaces =
                compilation.Namespaces |> Seq.map this.Namespaces.OnNamespace |> ImmutableArray.CreateRange

            QsCompilation.New(namespaces, compilation.EntryPoints)
        else
            compilation.Namespaces |> Seq.iter (this.Namespaces.OnNamespace >> ignore)
            compilation

    member this.SharedState = state

    new(state, options) as this =
        SyntaxTreeTransformation(state, options, "_internal_")
        then
            this.Types <- TypeTransformation<'T>(this, options)
            this.ExpressionKinds <- ExpressionKindTransformation<'T>(this, options)
            this.Expressions <- ExpressionTransformation<'T>(this, options)
            this.StatementKinds <- StatementKindTransformation<'T>(this, options)
            this.Statements <- StatementTransformation<'T>(this, options)
            this.Namespaces <- NamespaceTransformation<'T>(this, options)

    new(state: 'T) = new SyntaxTreeTransformation<'T>(state, TransformationOptions.Default)

    abstract OnLocalNameDeclaration: name: string -> string
    default _.OnLocalNameDeclaration name = name

    // RELEASE 2022-09: Replace OnVariableName with the identity function.
    abstract OnLocalName: name: string -> string
    default this.OnLocalName name = this.Statements.OnVariableName name

    // RELEASE 2022-09: Replace OnItemName with the identity function.
    abstract OnItemNameDeclaration: name: string -> string
    default this.OnItemNameDeclaration name = this.Namespaces.OnItemName name

    abstract OnItemName: parentType: UserDefinedType * itemName: string -> string
    default _.OnItemName(_, itemName) = itemName

    abstract OnArgumentTuple: argTuple: QsArgumentTuple -> QsArgumentTuple

    default this.OnArgumentTuple argTuple =
        this.Namespaces.OnArgumentTuple argTuple

    // RELEASE 2022-09: Replace OnLocation with the identity function.
    abstract OnAbsoluteLocation: location: QsNullable<QsLocation> -> QsNullable<QsLocation>
    default this.OnAbsoluteLocation location = this.Namespaces.OnLocation location

    // RELEASE 2022-09: Replace OnLocation with the identity function.
    abstract OnRelativeLocation: location: QsNullable<QsLocation> -> QsNullable<QsLocation>
    default this.OnRelativeLocation location = this.Statements.OnLocation location

    abstract OnSymbolLocation: offset: QsNullable<Position> * range: Range -> QsNullable<Position> * Range
    default _.OnSymbolLocation(offset, range) = (offset, range)

    abstract OnExpressionRange: range: QsNullable<Range> -> QsNullable<Range>

    default this.OnExpressionRange range =
        this.Expressions.OnRangeInformation range

    // RELEASE 2022-09: Replace OnTypeRange with the identity function.
    abstract OnTypeRange: range: TypeRange -> TypeRange
    default this.OnTypeRange range = this.Types.OnTypeRange range

    interface ICommonTransformation with
        member this.OnLocalNameDeclaration name = this.OnLocalNameDeclaration name
        member this.OnLocalName name = this.OnLocalName name
        member this.OnItemNameDeclaration name = this.OnItemNameDeclaration name
        member this.OnItemName(parentType, itemName) = this.OnItemName(parentType, itemName)
        member this.OnArgumentTuple argTuple = this.OnArgumentTuple argTuple
        member this.OnAbsoluteLocation location = this.OnAbsoluteLocation location
        member this.OnRelativeLocation location = this.OnRelativeLocation location
        member this.OnSymbolLocation(offset, range) = this.OnSymbolLocation(offset, range)
        member this.OnExpressionRange range = this.OnExpressionRange range
        member this.OnTypeRange range = this.OnTypeRange range

and TypeTransformation<'T>(parentTransformation: SyntaxTreeTransformation<'T>, options) =
    inherit TypeTransformationBase(options, parentTransformation :> ICommonTransformation)

    member _.Transformation = parentTransformation

    member this.SharedState = this.Transformation.SharedState

    new(parentTransformation: SyntaxTreeTransformation<'T>) =
        TypeTransformation<'T>(parentTransformation, TransformationOptions.Default)

    new(sharedState: 'T, options) as this =
        TypeTransformation<'T>(SyntaxTreeTransformation(sharedState, options), options)
        then this.Transformation.Types <- this

    new(sharedState: 'T) = TypeTransformation(sharedState, TransformationOptions.Default)

and ExpressionKindTransformation<'T> private (createParentTransformation, options: TransformationOptions) as this =
    inherit ExpressionKindTransformationBase
        (
            (fun () -> this.Transformation.Expressions :> ExpressionTransformationBase),
            options
        )

    static let defaultParentTransformation sharedState options expressionKinds =
        let transformation = SyntaxTreeTransformation(sharedState, options)
        transformation.Types <- TypeTransformation<'T>(transformation, TransformationOptions.Disabled)
        transformation.ExpressionKinds <- expressionKinds
        transformation

    member val Transformation: SyntaxTreeTransformation<_> = createParentTransformation this

    member this.SharedState = this.Transformation.SharedState

    new(parentTransformation: SyntaxTreeTransformation<_>, options) =
        ExpressionKindTransformation((fun _ -> parentTransformation), options)

    new(sharedState: 'T, options) =
        ExpressionKindTransformation<'T>(defaultParentTransformation sharedState options, options)

    new(parentTransformation: SyntaxTreeTransformation<_>) =
        ExpressionKindTransformation<'T>(parentTransformation, TransformationOptions.Default)

    new(sharedState: 'T) = ExpressionKindTransformation(sharedState, TransformationOptions.Default)

and ExpressionTransformation<'T> private (createParentTransformation, options) as this =
    inherit ExpressionTransformationBase
        (
            (fun () -> upcast this.Transformation.ExpressionKinds),
            (fun () -> upcast this.Transformation.Types),
            options
        )

    static let defaultParentTransformation sharedState options expressions =
        let transformation = SyntaxTreeTransformation(sharedState, options)
        transformation.Types <- TypeTransformation<'T>(transformation, TransformationOptions.Disabled)
        transformation.Expressions <- expressions
        transformation

    member val Transformation: SyntaxTreeTransformation<_> = createParentTransformation this

    member this.SharedState = this.Transformation.SharedState

    new(parentTransformation, options) = ExpressionTransformation((fun _ -> parentTransformation), options)

    new(sharedState: 'T, options) =
        ExpressionTransformation<'T>(defaultParentTransformation sharedState options, options)

    new(parentTransformation: SyntaxTreeTransformation<_>) =
        ExpressionTransformation<'T>(parentTransformation, TransformationOptions.Default)

    new(sharedState: 'T) = new ExpressionTransformation<'T>(sharedState, TransformationOptions.Default)

and StatementKindTransformation<'T> private (createParentTransformation, options: TransformationOptions) as this =
    inherit StatementKindTransformationBase
        (
            (fun () -> this.Transformation.Statements :> StatementTransformationBase),
            options
        )

    static let defaultParentTransformation sharedState options statementKinds =
        let transformation = SyntaxTreeTransformation(sharedState, options)
        transformation.Types <- TypeTransformation<'T>(transformation, TransformationOptions.Disabled)
        transformation.Expressions <- ExpressionTransformation<'T>(transformation, TransformationOptions.Disabled)
        transformation.StatementKinds <- statementKinds
        transformation

    member val Transformation: SyntaxTreeTransformation<_> = createParentTransformation this

    member this.SharedState = this.Transformation.SharedState

    new(parentTransformation, options) = StatementKindTransformation((fun _ -> parentTransformation), options)

    new(sharedState: 'T, options) =
        StatementKindTransformation<'T>(defaultParentTransformation sharedState options, options)

    new(parentTransformation: SyntaxTreeTransformation<_>) =
        StatementKindTransformation<'T>(parentTransformation, TransformationOptions.Default)

    new(sharedState: 'T) = StatementKindTransformation(sharedState, TransformationOptions.Default)

and StatementTransformation<'T> private (createParentTransformation, options) as this =
    inherit StatementTransformationBase
        (
            (fun () -> upcast this.Transformation.StatementKinds),
            (fun () -> upcast this.Transformation.Expressions),
            options
        )

    static let defaultParentTransformation sharedState options statements =
        let transformation = SyntaxTreeTransformation(sharedState, options)
        transformation.Types <- TypeTransformation<'T>(transformation, TransformationOptions.Disabled)
        transformation.Expressions <- ExpressionTransformation<'T>(transformation, TransformationOptions.Disabled)
        transformation.Statements <- statements
        transformation

    member val Transformation: SyntaxTreeTransformation<_> = createParentTransformation this

    member this.SharedState = this.Transformation.SharedState

    new(parentTransformation, options) = StatementTransformation((fun _ -> parentTransformation), options)

    new(sharedState: 'T, options) =
        StatementTransformation<'T>(defaultParentTransformation sharedState options, options)

    new(parentTransformation: SyntaxTreeTransformation<_>) =
        StatementTransformation<'T>(parentTransformation, TransformationOptions.Default)

    new(sharedState: 'T) = StatementTransformation(sharedState, TransformationOptions.Default)

and NamespaceTransformation<'T> internal (options, _internal_: string) =
    inherit NamespaceTransformationBase(options, _internal_)
    let mutable _Transformation: SyntaxTreeTransformation<'T> option = None // will be set to a suitable Some value once construction is complete

    /// Handle to the parent SyntaxTreeTransformation.
    /// This handle is always safe to access and will be set to a suitable value
    /// even if no parent transformation has been specified upon construction.
    member this.Transformation
        with get () = _Transformation.Value
        and private set value =
            _Transformation <- Some value
            this.StatementTransformationHandle <- fun _ -> value.Statements :> StatementTransformationBase

    member this.SharedState = this.Transformation.SharedState

    new(parentTransformation: SyntaxTreeTransformation<'T>, options: TransformationOptions) as this =
        NamespaceTransformation<'T>(options, "_internal_")
        then this.Transformation <- parentTransformation

    new(sharedState: 'T, options: TransformationOptions) as this =
        NamespaceTransformation<'T>(options, "_internal_")
        then
            this.Transformation <- new SyntaxTreeTransformation<'T>(sharedState, options)
            this.Transformation.Types <- new TypeTransformation<'T>(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Expressions <-
                new ExpressionTransformation<'T>(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Statements <-
                new StatementTransformation<'T>(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Namespaces <- this

    new(parentTransformation: SyntaxTreeTransformation<'T>) =
        new NamespaceTransformation<'T>(parentTransformation, TransformationOptions.Default)

    new(sharedState: 'T) = new NamespaceTransformation<'T>(sharedState, TransformationOptions.Default)


// setup for syntax tree transformations without internal state

type SyntaxTreeTransformation private (options: TransformationOptions, _internal_: string) as this =
    /// Transformation invoked for all types encountered when traversing (parts of) the syntax tree.
    member val Types = TypeTransformation(this, TransformationOptions.Default) with get, set

    /// Transformation invoked for all expression kinds encountered when traversing (parts of) the syntax tree.
    member val ExpressionKinds = ExpressionKindTransformation(this, TransformationOptions.Default) with get, set

    /// Transformation invoked for all expressions encountered when traversing (parts of) the syntax tree.
    member val Expressions = ExpressionTransformation(this, TransformationOptions.Default) with get, set

    /// Transformation invoked for all statement kinds encountered when traversing (parts of) the syntax tree.
    member val StatementKinds = StatementKindTransformation(this, TransformationOptions.Default) with get, set

    /// Transformation invoked for all statements encountered when traversing (parts of) the syntax tree.
    member val Statements = StatementTransformation(this, TransformationOptions.Default) with get, set

    /// Transformation invoked for all namespaces encountered when traversing (parts of) the syntax tree.
    member val Namespaces = new NamespaceTransformation(TransformationOptions.Default, _internal_) with get, set

    /// Invokes the transformation for all namespaces in the given compilation.
    member this.OnCompilation compilation =
        if options.Rebuild then
            let namespaces =
                compilation.Namespaces |> Seq.map this.Namespaces.OnNamespace |> ImmutableArray.CreateRange

            QsCompilation.New(namespaces, compilation.EntryPoints)
        else
            compilation.Namespaces |> Seq.iter (this.Namespaces.OnNamespace >> ignore)
            compilation

    new(options: TransformationOptions) as this =
        SyntaxTreeTransformation(options, "_internal_")
        then
            this.Types <- new TypeTransformation(this, options)
            this.ExpressionKinds <- new ExpressionKindTransformation(this, options)
            this.Expressions <- new ExpressionTransformation(this, options)
            this.StatementKinds <- new StatementKindTransformation(this, options)
            this.Statements <- new StatementTransformation(this, options)
            this.Namespaces <- new NamespaceTransformation(this, options)

    new() = new SyntaxTreeTransformation(TransformationOptions.Default)

    abstract OnLocalNameDeclaration: name: string -> string
    default _.OnLocalNameDeclaration name = name

    // RELEASE 2022-09: Replace OnVariableName with the identity function.
    abstract OnLocalName: name: string -> string
    default this.OnLocalName name = this.Statements.OnVariableName name

    // RELEASE 2022-09: Replace OnItemName with the identity function.
    abstract OnItemNameDeclaration: name: string -> string
    default this.OnItemNameDeclaration name = this.Namespaces.OnItemName name

    abstract OnItemName: parentType: UserDefinedType * itemName: string -> string
    default _.OnItemName(_, itemName) = itemName

    abstract OnArgumentTuple: argTuple: QsArgumentTuple -> QsArgumentTuple

    default this.OnArgumentTuple argTuple =
        this.Namespaces.OnArgumentTuple argTuple

    // RELEASE 2022-09: Replace OnLocation with the identity function.
    abstract OnAbsoluteLocation: location: QsNullable<QsLocation> -> QsNullable<QsLocation>
    default this.OnAbsoluteLocation location = this.Namespaces.OnLocation location

    // RELEASE 2022-09: Replace OnLocation with the identity function.
    abstract OnRelativeLocation: location: QsNullable<QsLocation> -> QsNullable<QsLocation>
    default this.OnRelativeLocation location = this.Statements.OnLocation location

    abstract OnSymbolLocation: offset: QsNullable<Position> * range: Range -> QsNullable<Position> * Range
    default _.OnSymbolLocation(offset, range) = (offset, range)

    abstract OnExpressionRange: range: QsNullable<Range> -> QsNullable<Range>

    default this.OnExpressionRange range =
        this.Expressions.OnRangeInformation range

    // RELEASE 2022-09: Replace OnTypeRange with the identity function.
    abstract OnTypeRange: range: TypeRange -> TypeRange
    default this.OnTypeRange range = this.Types.OnTypeRange range

    interface ICommonTransformation with
        member this.OnLocalNameDeclaration name = this.OnLocalNameDeclaration name
        member this.OnLocalName name = this.OnLocalName name
        member this.OnItemNameDeclaration name = this.OnItemNameDeclaration name
        member this.OnItemName(parentType, itemName) = this.OnItemName(parentType, itemName)
        member this.OnArgumentTuple argTuple = this.OnArgumentTuple argTuple
        member this.OnAbsoluteLocation location = this.OnAbsoluteLocation location
        member this.OnRelativeLocation location = this.OnRelativeLocation location
        member this.OnSymbolLocation(offset, range) = this.OnSymbolLocation(offset, range)
        member this.OnExpressionRange range = this.OnExpressionRange range
        member this.OnTypeRange range = this.OnTypeRange range

and TypeTransformation(parentTransformation, options) =
    inherit TypeTransformationBase(options, parentTransformation :> ICommonTransformation)

    member _.Transformation = parentTransformation

    new(parentTransformation) = TypeTransformation(parentTransformation, TransformationOptions.Default)

    new(options: TransformationOptions) as this =
        TypeTransformation(SyntaxTreeTransformation options, options)
        then this.Transformation.Types <- this

    new() = TypeTransformation TransformationOptions.Default

and ExpressionKindTransformation private (createParentTransformation, options: TransformationOptions) as this =
    inherit ExpressionKindTransformationBase
        (
            (fun () -> this.Transformation.Expressions :> ExpressionTransformationBase),
            options
        )

    static let defaultParentTransformation options expressionKinds =
        let transformation = SyntaxTreeTransformation options
        transformation.Types <- TypeTransformation(transformation, TransformationOptions.Disabled)
        transformation.ExpressionKinds <- expressionKinds
        transformation

    member val Transformation: SyntaxTreeTransformation = createParentTransformation this

    new(parentTransformation, options) = ExpressionKindTransformation((fun _ -> parentTransformation), options)

    new(options) = ExpressionKindTransformation(defaultParentTransformation options, options)

    new(parentTransformation: SyntaxTreeTransformation) =
        ExpressionKindTransformation(parentTransformation, TransformationOptions.Default)

    new() = ExpressionKindTransformation TransformationOptions.Default

and ExpressionTransformation private (createParentTransformation, options) as this =
    inherit ExpressionTransformationBase
        (
            (fun () -> upcast this.Transformation.ExpressionKinds),
            (fun () -> upcast this.Transformation.Types),
            options
        )

    static let defaultParentTransformation options expressions =
        let transformation = SyntaxTreeTransformation options
        transformation.Types <- TypeTransformation(transformation, TransformationOptions.Disabled)
        transformation.Expressions <- expressions
        transformation

    member val Transformation: SyntaxTreeTransformation = createParentTransformation this

    new(parentTransformation, options) = ExpressionTransformation((fun _ -> parentTransformation), options)

    new(options) = ExpressionTransformation(defaultParentTransformation options, options)

    new(parentTransformation: SyntaxTreeTransformation) =
        ExpressionTransformation(parentTransformation, TransformationOptions.Default)

    new() = ExpressionTransformation TransformationOptions.Default

and StatementKindTransformation private (createParentTransformation, options: TransformationOptions) as this =
    inherit StatementKindTransformationBase
        (
            (fun () -> this.Transformation.Statements :> StatementTransformationBase),
            options
        )

    static let defaultParentTransformation options statementKinds =
        let transformation = SyntaxTreeTransformation options
        transformation.Types <- TypeTransformation(transformation, TransformationOptions.Disabled)
        transformation.Expressions <- ExpressionTransformation(transformation, TransformationOptions.Disabled)
        transformation.StatementKinds <- statementKinds
        transformation

    member val Transformation: SyntaxTreeTransformation = createParentTransformation this

    new(parentTransformation, options) = StatementKindTransformation((fun _ -> parentTransformation), options)

    new(options) = StatementKindTransformation(defaultParentTransformation options, options)

    new(parentTransformation: SyntaxTreeTransformation) =
        StatementKindTransformation(parentTransformation, TransformationOptions.Default)

    new() = StatementKindTransformation TransformationOptions.Default

and StatementTransformation private (createParentTransformation, options) as this =
    inherit StatementTransformationBase
        (
            (fun () -> upcast this.Transformation.StatementKinds),
            (fun () -> upcast this.Transformation.Expressions),
            options
        )

    static let defaultParentTransformation options statements =
        let transformation = SyntaxTreeTransformation options
        transformation.Types <- TypeTransformation(transformation, TransformationOptions.Disabled)
        transformation.Expressions <- ExpressionTransformation(transformation, TransformationOptions.Disabled)
        transformation.Statements <- statements
        transformation

    member val Transformation: SyntaxTreeTransformation = createParentTransformation this

    new(parentTransformation, options) = StatementTransformation((fun _ -> parentTransformation), options)

    new(options) = StatementTransformation(defaultParentTransformation options, options)

    new(parentTransformation: SyntaxTreeTransformation) =
        StatementTransformation(parentTransformation, TransformationOptions.Default)

    new() = StatementTransformation TransformationOptions.Default

and NamespaceTransformation internal (options, _internal_: string) =
    inherit NamespaceTransformationBase(options, _internal_)
    let mutable _Transformation: SyntaxTreeTransformation option = None // will be set to a suitable Some value once construction is complete

    /// Handle to the parent SyntaxTreeTransformation.
    /// This handle is always safe to access and will be set to a suitable value
    /// even if no parent transformation has been specified upon construction.
    member this.Transformation
        with get () = _Transformation.Value
        and private set value =
            _Transformation <- Some value
            this.StatementTransformationHandle <- fun _ -> value.Statements :> StatementTransformationBase

    new(parentTransformation: SyntaxTreeTransformation, options: TransformationOptions) as this =
        NamespaceTransformation(options, "_internal_")
        then this.Transformation <- parentTransformation

    new(options: TransformationOptions) as this =
        NamespaceTransformation(options, "_internal_")
        then
            this.Transformation <- new SyntaxTreeTransformation(options)
            this.Transformation.Types <- new TypeTransformation(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Expressions <-
                new ExpressionTransformation(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Statements <-
                new StatementTransformation(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Namespaces <- this

    new(parentTransformation: SyntaxTreeTransformation) =
        new NamespaceTransformation(parentTransformation, TransformationOptions.Default)

    new() = new NamespaceTransformation(TransformationOptions.Default)
