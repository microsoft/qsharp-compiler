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

type SyntaxTreeTransformation<'T> private (state: 'T, options: TransformationOptions, _internal_: string) as this =
    /// Transformation invoked for all types encountered when traversing (parts of) the syntax tree.
    member val Types = TypeTransformation<'T>(this, TransformationOptions.Default) with get, set

    /// Transformation invoked for all expression kinds encountered when traversing (parts of) the syntax tree.
    member val ExpressionKinds =
        new ExpressionKindTransformation<'T>(TransformationOptions.Default, _internal_) with get, set

    /// Transformation invoked for all expressions encountered when traversing (parts of) the syntax tree.
    member val Expressions = new ExpressionTransformation<'T>(TransformationOptions.Default, _internal_) with get, set

    /// Transformation invoked for all statement kinds encountered when traversing (parts of) the syntax tree.
    member val StatementKinds =
        new StatementKindTransformation<'T>(TransformationOptions.Default, _internal_) with get, set

    /// Transformation invoked for all statements encountered when traversing (parts of) the syntax tree.
    member val Statements = new StatementTransformation<'T>(TransformationOptions.Default, _internal_) with get, set

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

    new(state: 'T, options: TransformationOptions) as this =
        SyntaxTreeTransformation<'T>(state, options, "_internal_")
        then
            this.Types <- new TypeTransformation<'T>(this, options)
            this.ExpressionKinds <- new ExpressionKindTransformation<'T>(this, options)
            this.Expressions <- new ExpressionTransformation<'T>(this, options)
            this.StatementKinds <- new StatementKindTransformation<'T>(this, options)
            this.Statements <- new StatementTransformation<'T>(this, options)
            this.Namespaces <- new NamespaceTransformation<'T>(this, options)

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

and ExpressionKindTransformation<'T> internal (options, _internal_) =
    inherit ExpressionKindTransformationBase(options, _internal_)
    let mutable _Transformation: SyntaxTreeTransformation<'T> option = None // will be set to a suitable Some value once construction is complete

    /// Handle to the parent SyntaxTreeTransformation.
    /// This handle is always safe to access and will be set to a suitable value
    /// even if no parent transformation has been specified upon construction.
    member this.Transformation
        with get () = _Transformation.Value
        and private set value =
            _Transformation <- Some value
            this.ExpressionTransformationHandle <- fun _ -> value.Expressions :> ExpressionTransformationBase

    member this.SharedState = this.Transformation.SharedState

    new(parentTransformation: SyntaxTreeTransformation<'T>, options: TransformationOptions) as this =
        ExpressionKindTransformation<'T>(options, "_internal_")
        then this.Transformation <- parentTransformation

    new(sharedState: 'T, options: TransformationOptions) as this =
        ExpressionKindTransformation<'T>(options, "_internal_")
        then
            this.Transformation <- new SyntaxTreeTransformation<'T>(sharedState, options)
            this.Transformation.Types <- new TypeTransformation<'T>(this.Transformation, TransformationOptions.Disabled)
            this.Transformation.ExpressionKinds <- this

    new(parentTransformation: SyntaxTreeTransformation<'T>) =
        new ExpressionKindTransformation<'T>(parentTransformation, TransformationOptions.Default)

    new(sharedState: 'T) = new ExpressionKindTransformation<'T>(sharedState, TransformationOptions.Default)


and ExpressionTransformation<'T> internal (options, _internal_) =
    inherit ExpressionTransformationBase(options, _internal_)
    let mutable _Transformation: SyntaxTreeTransformation<'T> option = None // will be set to a suitable Some value once construction is complete

    /// Handle to the parent SyntaxTreeTransformation.
    /// This handle is always safe to access and will be set to a suitable value
    /// even if no parent transformation has been specified upon construction.
    member this.Transformation
        with get () = _Transformation.Value
        and private set value =
            _Transformation <- Some value

            this.ExpressionKindTransformationHandle <-
                fun _ -> value.ExpressionKinds :> ExpressionKindTransformationBase

            this.TypeTransformationHandle <- fun _ -> value.Types :> TypeTransformationBase

    member this.SharedState = this.Transformation.SharedState

    new(parentTransformation: SyntaxTreeTransformation<'T>, options: TransformationOptions) as this =
        ExpressionTransformation<'T>(options, "_internal_")
        then this.Transformation <- parentTransformation

    new(sharedState: 'T, options: TransformationOptions) as this =
        ExpressionTransformation<'T>(options, "_internal_")
        then
            this.Transformation <- new SyntaxTreeTransformation<'T>(sharedState, options)
            this.Transformation.Types <- new TypeTransformation<'T>(this.Transformation, TransformationOptions.Disabled)
            this.Transformation.Expressions <- this

    new(parentTransformation: SyntaxTreeTransformation<'T>) =
        new ExpressionTransformation<'T>(parentTransformation, TransformationOptions.Default)

    new(sharedState: 'T) = new ExpressionTransformation<'T>(sharedState, TransformationOptions.Default)


and StatementKindTransformation<'T> internal (options, _internal_) =
    inherit StatementKindTransformationBase(options, _internal_)
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
        StatementKindTransformation<'T>(options, "_internal_")
        then this.Transformation <- parentTransformation

    new(sharedState: 'T, options: TransformationOptions) as this =
        StatementKindTransformation<'T>(options, "_internal_")
        then
            this.Transformation <- new SyntaxTreeTransformation<'T>(sharedState, options)
            this.Transformation.Types <- new TypeTransformation<'T>(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Expressions <-
                new ExpressionTransformation<'T>(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.StatementKinds <- this

    new(parentTransformation: SyntaxTreeTransformation<'T>) =
        new StatementKindTransformation<'T>(parentTransformation, TransformationOptions.Default)

    new(sharedState: 'T) = new StatementKindTransformation<'T>(sharedState, TransformationOptions.Default)


and StatementTransformation<'T> internal (options, _internal_) =
    inherit StatementTransformationBase(options, _internal_)
    let mutable _Transformation: SyntaxTreeTransformation<'T> option = None // will be set to a suitable Some value once construction is complete

    /// Handle to the parent SyntaxTreeTransformation.
    /// This handle is always safe to access and will be set to a suitable value
    /// even if no parent transformation has been specified upon construction.
    member this.Transformation
        with get () = _Transformation.Value
        and private set value =
            _Transformation <- Some value
            this.StatementKindTransformationHandle <- fun _ -> value.StatementKinds :> StatementKindTransformationBase
            this.ExpressionTransformationHandle <- fun _ -> value.Expressions :> ExpressionTransformationBase

    member this.SharedState = this.Transformation.SharedState

    new(parentTransformation: SyntaxTreeTransformation<'T>, options: TransformationOptions) as this =
        StatementTransformation<'T>(options, "_internal_")
        then this.Transformation <- parentTransformation

    new(sharedState: 'T, options: TransformationOptions) as this =
        StatementTransformation<'T>(options, "_internal_")
        then
            this.Transformation <- new SyntaxTreeTransformation<'T>(sharedState, options)
            this.Transformation.Types <- new TypeTransformation<'T>(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Expressions <-
                new ExpressionTransformation<'T>(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Statements <- this

    new(parentTransformation: SyntaxTreeTransformation<'T>) =
        new StatementTransformation<'T>(parentTransformation, TransformationOptions.Default)

    new(sharedState: 'T) = new StatementTransformation<'T>(sharedState, TransformationOptions.Default)


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
    member val ExpressionKinds =
        new ExpressionKindTransformation(TransformationOptions.Default, _internal_) with get, set

    /// Transformation invoked for all expressions encountered when traversing (parts of) the syntax tree.
    member val Expressions = new ExpressionTransformation(TransformationOptions.Default, _internal_) with get, set

    /// Transformation invoked for all statement kinds encountered when traversing (parts of) the syntax tree.
    member val StatementKinds = new StatementKindTransformation(TransformationOptions.Default, _internal_) with get, set

    /// Transformation invoked for all statements encountered when traversing (parts of) the syntax tree.
    member val Statements = new StatementTransformation(TransformationOptions.Default, _internal_) with get, set

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

and ExpressionKindTransformation internal (options, _internal_) =
    inherit ExpressionKindTransformationBase(options, _internal_)
    let mutable _Transformation: SyntaxTreeTransformation option = None // will be set to a suitable Some value once construction is complete

    /// Handle to the parent SyntaxTreeTransformation.
    /// This handle is always safe to access and will be set to a suitable value
    /// even if no parent transformation has been specified upon construction.
    member this.Transformation
        with get () = _Transformation.Value
        and private set value =
            _Transformation <- Some value
            this.ExpressionTransformationHandle <- fun _ -> value.Expressions :> ExpressionTransformationBase

    new(parentTransformation: SyntaxTreeTransformation, options: TransformationOptions) as this =
        ExpressionKindTransformation(options, "_internal_")
        then this.Transformation <- parentTransformation

    new(options: TransformationOptions) as this =
        ExpressionKindTransformation(options, "_internal_")
        then
            this.Transformation <- new SyntaxTreeTransformation(options)
            this.Transformation.Types <- new TypeTransformation(this.Transformation, TransformationOptions.Disabled)
            this.Transformation.ExpressionKinds <- this

    new(parentTransformation: SyntaxTreeTransformation) =
        new ExpressionKindTransformation(parentTransformation, TransformationOptions.Default)

    new() = new ExpressionKindTransformation(TransformationOptions.Default)


and ExpressionTransformation internal (options, _internal_) =
    inherit ExpressionTransformationBase(options, _internal_)
    let mutable _Transformation: SyntaxTreeTransformation option = None // will be set to a suitable Some value once construction is complete

    /// Handle to the parent SyntaxTreeTransformation.
    /// This handle is always safe to access and will be set to a suitable value
    /// even if no parent transformation has been specified upon construction.
    member this.Transformation
        with get () = _Transformation.Value
        and private set value =
            _Transformation <- Some value

            this.ExpressionKindTransformationHandle <-
                fun _ -> value.ExpressionKinds :> ExpressionKindTransformationBase

            this.TypeTransformationHandle <- fun _ -> value.Types :> TypeTransformationBase

    new(parentTransformation: SyntaxTreeTransformation, options: TransformationOptions) as this =
        ExpressionTransformation(options, "_internal_")
        then this.Transformation <- parentTransformation

    new(options: TransformationOptions) as this =
        ExpressionTransformation(options, "_internal_")
        then
            this.Transformation <- new SyntaxTreeTransformation(options)
            this.Transformation.Types <- new TypeTransformation(this.Transformation, TransformationOptions.Disabled)
            this.Transformation.Expressions <- this

    new(parentTransformation: SyntaxTreeTransformation) =
        new ExpressionTransformation(parentTransformation, TransformationOptions.Default)

    new() = new ExpressionTransformation(TransformationOptions.Default)


and StatementKindTransformation internal (options, _internal_) =
    inherit StatementKindTransformationBase(options, _internal_)
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
        StatementKindTransformation(options, "_internal_")
        then this.Transformation <- parentTransformation

    new(options: TransformationOptions) as this =
        StatementKindTransformation(options, "_internal_")
        then
            this.Transformation <- new SyntaxTreeTransformation(options)
            this.Transformation.Types <- new TypeTransformation(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Expressions <-
                new ExpressionTransformation(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.StatementKinds <- this

    new(parentTransformation: SyntaxTreeTransformation) =
        new StatementKindTransformation(parentTransformation, TransformationOptions.Default)

    new() = new StatementKindTransformation(TransformationOptions.Default)


and StatementTransformation internal (options, _internal_) =
    inherit StatementTransformationBase(options, _internal_)
    let mutable _Transformation: SyntaxTreeTransformation option = None // will be set to a suitable Some value once construction is complete

    /// Handle to the parent SyntaxTreeTransformation.
    /// This handle is always safe to access and will be set to a suitable value
    /// even if no parent transformation has been specified upon construction.
    member this.Transformation
        with get () = _Transformation.Value
        and private set value =
            _Transformation <- Some value
            this.StatementKindTransformationHandle <- fun _ -> value.StatementKinds :> StatementKindTransformationBase
            this.ExpressionTransformationHandle <- fun _ -> value.Expressions :> ExpressionTransformationBase

    new(parentTransformation: SyntaxTreeTransformation, options: TransformationOptions) as this =
        StatementTransformation(options, "_internal_")
        then this.Transformation <- parentTransformation

    new(options: TransformationOptions) as this =
        StatementTransformation(options, "_internal_")
        then
            this.Transformation <- new SyntaxTreeTransformation(options)
            this.Transformation.Types <- new TypeTransformation(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Expressions <-
                new ExpressionTransformation(this.Transformation, TransformationOptions.Disabled)

            this.Transformation.Statements <- this

    new(parentTransformation: SyntaxTreeTransformation) =
        new StatementTransformation(parentTransformation, TransformationOptions.Default)

    new() = new StatementTransformation(TransformationOptions.Default)


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
