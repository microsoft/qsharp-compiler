// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

#nowarn "44" // RELEASE 2022-09: Re-enable after updating the ICommonTransformation implementations.

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

// setup for syntax tree transformations with internal state

type SyntaxTreeTransformation<'T>(state, options) as this =
    /// Transformation invoked for all types encountered when traversing (parts of) the syntax tree.
    member val Types = TypeTransformation<'T>(this, options) with get, set

    /// Transformation invoked for all expression kinds encountered when traversing (parts of) the syntax tree.
    member val ExpressionKinds = ExpressionKindTransformation<'T>(this, options) with get, set

    /// Transformation invoked for all expressions encountered when traversing (parts of) the syntax tree.
    member val Expressions = ExpressionTransformation<'T>(this, options) with get, set

    /// Transformation invoked for all statement kinds encountered when traversing (parts of) the syntax tree.
    member val StatementKinds = StatementKindTransformation<'T>(this, options) with get, set

    /// Transformation invoked for all statements encountered when traversing (parts of) the syntax tree.
    member val Statements = StatementTransformation<'T>(this, options) with get, set

    /// Transformation invoked for all namespaces encountered when traversing (parts of) the syntax tree.
    member val Namespaces = NamespaceTransformation<'T>(this, options) with get, set

    /// Invokes the transformation for all namespaces in the given compilation.
    abstract OnCompilation: compilation: QsCompilation -> QsCompilation

    default this.OnCompilation compilation =

        if options.Rebuild then
            let namespaces =
                compilation.Namespaces |> Seq.map this.Namespaces.OnNamespace |> ImmutableArray.CreateRange

            QsCompilation.New(namespaces, compilation.EntryPoints)
        else
            compilation.Namespaces |> Seq.iter (this.Namespaces.OnNamespace >> ignore)
            compilation

    member _.SharedState = state

    new(state) = SyntaxTreeTransformation(state, TransformationOptions.Default)

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

and TypeTransformation<'T>(parentTransformation, options) =
    inherit TypeTransformationBase(options, parentTransformation :> ICommonTransformation)

    member _.Transformation = parentTransformation

    member _.SharedState = parentTransformation.SharedState

    new(parentTransformation: SyntaxTreeTransformation<_>) =
        TypeTransformation<'T>(parentTransformation, TransformationOptions.Default)

    new(sharedState: 'T, options) as this =
        TypeTransformation<'T>(SyntaxTreeTransformation(sharedState, options), options)
        then this.Transformation.Types <- this

    new(sharedState: 'T) = TypeTransformation(sharedState, TransformationOptions.Default)

and ExpressionKindTransformation<'T>(parentTransformation, options) as this =
    inherit ExpressionKindTransformationBase
        (
            (fun () -> this.Transformation.Expressions :> ExpressionTransformationBase),
            options
        )

    member _.Transformation: SyntaxTreeTransformation<_> = parentTransformation

    member _.SharedState = parentTransformation.SharedState

    new(sharedState: 'T, options) as this =
        ExpressionKindTransformation<'T>(SyntaxTreeTransformation(sharedState, options), options)
        then
            this.Transformation.Types <- TypeTransformation<'T>(this.Transformation, TransformationOptions.Disabled)
            this.Transformation.ExpressionKinds <- this

    new(parentTransformation: SyntaxTreeTransformation<_>) =
        ExpressionKindTransformation<'T>(parentTransformation, TransformationOptions.Default)

    new(sharedState: 'T) = ExpressionKindTransformation(sharedState, TransformationOptions.Default)

and ExpressionTransformation<'T>(parentTransformation, options) as this =
    inherit ExpressionTransformationBase
        (
            (fun () -> upcast this.Transformation.ExpressionKinds),
            (fun () -> upcast this.Transformation.Types),
            options
        )

    member _.Transformation: SyntaxTreeTransformation<_> = parentTransformation

    member _.SharedState = parentTransformation.SharedState

    new(sharedState: 'T, options) as this =
        ExpressionTransformation<'T>(SyntaxTreeTransformation(sharedState, options), options)
        then
            this.Transformation.Types <- TypeTransformation<'T>(this.Transformation, TransformationOptions.Disabled)
            this.Transformation.Expressions <- this

    new(parentTransformation: SyntaxTreeTransformation<_>) =
        ExpressionTransformation<'T>(parentTransformation, TransformationOptions.Default)

    new(sharedState: 'T) = ExpressionTransformation(sharedState, TransformationOptions.Default)

and StatementKindTransformation<'T>(parentTransformation, options: TransformationOptions) as this =
    inherit StatementKindTransformationBase
        (
            (fun () -> this.Transformation.Statements :> StatementTransformationBase),
            options
        )

    member _.Transformation: SyntaxTreeTransformation<_> = parentTransformation

    member _.SharedState = parentTransformation.SharedState

    new(sharedState: 'T, options) as this =
        StatementKindTransformation<'T>(SyntaxTreeTransformation(sharedState, options), options)
        then
            this.Transformation.Types <- TypeTransformation<'T>(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Expressions <-
                ExpressionTransformation<'T>(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.StatementKinds <- this

    new(parentTransformation: SyntaxTreeTransformation<_>) =
        StatementKindTransformation<'T>(parentTransformation, TransformationOptions.Default)

    new(sharedState: 'T) = StatementKindTransformation(sharedState, TransformationOptions.Default)

and StatementTransformation<'T>(parentTransformation, options) as this =
    inherit StatementTransformationBase
        (
            (fun () -> upcast this.Transformation.StatementKinds),
            (fun () -> upcast this.Transformation.Expressions),
            options
        )

    member _.Transformation: SyntaxTreeTransformation<_> = parentTransformation

    member _.SharedState = parentTransformation.SharedState

    new(sharedState: 'T, options) as this =
        StatementTransformation<'T>(SyntaxTreeTransformation(sharedState, options), options)
        then
            this.Transformation.Types <- TypeTransformation<'T>(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Expressions <-
                ExpressionTransformation<'T>(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Statements <- this

    new(parentTransformation: SyntaxTreeTransformation<_>) =
        StatementTransformation<'T>(parentTransformation, TransformationOptions.Default)

    new(sharedState: 'T) = StatementTransformation(sharedState, TransformationOptions.Default)

and NamespaceTransformation<'T>(parentTransformation, options) as this =
    inherit NamespaceTransformationBase((fun () -> upcast this.Transformation.Statements), options)

    member _.Transformation: SyntaxTreeTransformation<_> = parentTransformation

    member _.SharedState = parentTransformation.SharedState

    new(sharedState: 'T, options) as this =
        NamespaceTransformation<'T>(SyntaxTreeTransformation(sharedState, options), options)
        then
            this.Transformation.Types <- TypeTransformation<'T>(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Expressions <-
                ExpressionTransformation<'T>(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Statements <-
                StatementTransformation<'T>(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Namespaces <- this

    new(parentTransformation: SyntaxTreeTransformation<_>) =
        NamespaceTransformation<'T>(parentTransformation, TransformationOptions.Default)

    new(sharedState: 'T) = NamespaceTransformation(sharedState, TransformationOptions.Default)

// setup for syntax tree transformations without internal state

type SyntaxTreeTransformation(options) as this =
    /// Transformation invoked for all types encountered when traversing (parts of) the syntax tree.
    member val Types = TypeTransformation(this, options) with get, set

    /// Transformation invoked for all expression kinds encountered when traversing (parts of) the syntax tree.
    member val ExpressionKinds = ExpressionKindTransformation(this, options) with get, set

    /// Transformation invoked for all expressions encountered when traversing (parts of) the syntax tree.
    member val Expressions = ExpressionTransformation(this, options) with get, set

    /// Transformation invoked for all statement kinds encountered when traversing (parts of) the syntax tree.
    member val StatementKinds = StatementKindTransformation(this, options) with get, set

    /// Transformation invoked for all statements encountered when traversing (parts of) the syntax tree.
    member val Statements = StatementTransformation(this, options) with get, set

    /// Transformation invoked for all namespaces encountered when traversing (parts of) the syntax tree.
    member val Namespaces = NamespaceTransformation(this, options) with get, set

    /// Invokes the transformation for all namespaces in the given compilation.
    abstract OnCompilation: compilation: QsCompilation -> QsCompilation

    default this.OnCompilation compilation =
        if options.Rebuild then
            let namespaces =
                compilation.Namespaces |> Seq.map this.Namespaces.OnNamespace |> ImmutableArray.CreateRange

            QsCompilation.New(namespaces, compilation.EntryPoints)
        else
            compilation.Namespaces |> Seq.iter (this.Namespaces.OnNamespace >> ignore)
            compilation

    new() = SyntaxTreeTransformation TransformationOptions.Default

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

and ExpressionKindTransformation(parentTransformation, options) as this =
    inherit ExpressionKindTransformationBase
        (
            (fun () -> this.Transformation.Expressions :> ExpressionTransformationBase),
            options
        )

    member _.Transformation: SyntaxTreeTransformation = parentTransformation

    new(options) as this =
        ExpressionKindTransformation(SyntaxTreeTransformation options, options)
        then
            this.Transformation.Types <- TypeTransformation(this.Transformation, TransformationOptions.Disabled)
            this.Transformation.ExpressionKinds <- this

    new(parentTransformation) = ExpressionKindTransformation(parentTransformation, TransformationOptions.Default)

    new() = ExpressionKindTransformation TransformationOptions.Default

and ExpressionTransformation(parentTransformation, options) as this =
    inherit ExpressionTransformationBase
        (
            (fun () -> upcast this.Transformation.ExpressionKinds),
            (fun () -> upcast this.Transformation.Types),
            options
        )

    member _.Transformation: SyntaxTreeTransformation = parentTransformation

    new(options) as this =
        ExpressionTransformation(SyntaxTreeTransformation options, options)
        then
            this.Transformation.Types <- TypeTransformation(this.Transformation, TransformationOptions.Disabled)
            this.Transformation.Expressions <- this

    new(parentTransformation) = ExpressionTransformation(parentTransformation, TransformationOptions.Default)

    new() = ExpressionTransformation TransformationOptions.Default

and StatementKindTransformation(parentTransformation, options) as this =
    inherit StatementKindTransformationBase
        (
            (fun () -> this.Transformation.Statements :> StatementTransformationBase),
            options
        )

    member _.Transformation: SyntaxTreeTransformation = parentTransformation

    new(options) as this =
        StatementKindTransformation(SyntaxTreeTransformation options, options)
        then
            this.Transformation.Types <- TypeTransformation(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Expressions <-
                ExpressionTransformation(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.StatementKinds <- this

    new(parentTransformation) = StatementKindTransformation(parentTransformation, TransformationOptions.Default)

    new() = StatementKindTransformation TransformationOptions.Default

and StatementTransformation(parentTransformation, options) as this =
    inherit StatementTransformationBase
        (
            (fun () -> upcast this.Transformation.StatementKinds),
            (fun () -> upcast this.Transformation.Expressions),
            options
        )

    member _.Transformation: SyntaxTreeTransformation = parentTransformation

    new(options) as this =
        StatementTransformation(SyntaxTreeTransformation options, options)
        then
            this.Transformation.Types <- TypeTransformation(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Expressions <-
                ExpressionTransformation(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Statements <- this

    new(parentTransformation) = StatementTransformation(parentTransformation, TransformationOptions.Default)

    new() = StatementTransformation TransformationOptions.Default

and NamespaceTransformation(parentTransformation, options) as this =
    inherit NamespaceTransformationBase((fun () -> upcast this.Transformation.Statements), options)

    member _.Transformation: SyntaxTreeTransformation = parentTransformation

    new(options) as this =
        NamespaceTransformation(SyntaxTreeTransformation options, options)
        then
            this.Transformation.Types <- TypeTransformation(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Expressions <-
                ExpressionTransformation(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Statements <-
                StatementTransformation(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Namespaces <- this

    new(parentTransformation) = NamespaceTransformation(parentTransformation, TransformationOptions.Default)

    new() = NamespaceTransformation TransformationOptions.Default
