// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core.Utils


// setup for syntax tree transformations with internal state

type SyntaxTreeTransformation<'T> private (state: 'T, options: TransformationOptions, _internal_: string) =

    let mutable _Types = new TypeTransformation<'T>(TransformationOptions.Default, _internal_)
    let mutable _ExpressionKinds = new ExpressionKindTransformation<'T>(TransformationOptions.Default, _internal_)
    let mutable _Expressions = new ExpressionTransformation<'T>(TransformationOptions.Default, _internal_)
    let mutable _StatementKinds = new StatementKindTransformation<'T>(TransformationOptions.Default, _internal_)
    let mutable _Statements = new StatementTransformation<'T>(TransformationOptions.Default, _internal_)
    let mutable _Namespaces = new NamespaceTransformation<'T>(TransformationOptions.Default, _internal_)

    /// Transformation invoked for all types encountered when traversing (parts of) the syntax tree.
    member this.Types
        with get () = _Types
        and set value = _Types <- value

    /// Transformation invoked for all expression kinds encountered when traversing (parts of) the syntax tree.
    member this.ExpressionKinds
        with get () = _ExpressionKinds
        and set value = _ExpressionKinds <- value

    /// Transformation invoked for all expressions encountered when traversing (parts of) the syntax tree.
    member this.Expressions
        with get () = _Expressions
        and set value = _Expressions <- value

    /// Transformation invoked for all statement kinds encountered when traversing (parts of) the syntax tree.
    member this.StatementKinds
        with get () = _StatementKinds
        and set value = _StatementKinds <- value

    /// Transformation invoked for all statements encountered when traversing (parts of) the syntax tree.
    member this.Statements
        with get () = _Statements
        and set value = _Statements <- value

    /// Transformation invoked for all namespaces encountered when traversing (parts of) the syntax tree.
    member this.Namespaces
        with get () = _Namespaces
        and set value = _Namespaces <- value

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


and TypeTransformation<'T> internal (options, _internal_) =
    inherit TypeTransformationBase(options)
    let mutable _Transformation: SyntaxTreeTransformation<'T> option = None // will be set to a suitable Some value once construction is complete

    /// Handle to the parent SyntaxTreeTransformation.
    /// This handle is always safe to access and will be set to a suitable value
    /// even if no parent transformation has been specified upon construction.
    member this.Transformation
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    member this.SharedState = this.Transformation.SharedState

    new(parentTransformation: SyntaxTreeTransformation<'T>, options: TransformationOptions) as this =
        new TypeTransformation<'T>(options, "_internal_")
        then this.Transformation <- parentTransformation

    new(sharedState: 'T, options: TransformationOptions) as this =
        TypeTransformation<'T>(options, "_internal_")
        then
            this.Transformation <- new SyntaxTreeTransformation<'T>(sharedState, options)
            this.Transformation.Types <- this

    new(parentTransformation: SyntaxTreeTransformation<'T>) =
        new TypeTransformation<'T>(parentTransformation, TransformationOptions.Default)

    new(sharedState: 'T) = new TypeTransformation<'T>(sharedState, TransformationOptions.Default)


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
            this.TypeTransformationHandle <- fun _ -> value.Types :> TypeTransformationBase

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
            this.ExpressionTransformationHandle <- fun _ -> value.Expressions :> ExpressionTransformationBase

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

type SyntaxTreeTransformation private (options: TransformationOptions, _internal_: string) =

    let mutable _Types = new TypeTransformation(TransformationOptions.Default, _internal_)
    let mutable _ExpressionKinds = new ExpressionKindTransformation(TransformationOptions.Default, _internal_)
    let mutable _Expressions = new ExpressionTransformation(TransformationOptions.Default, _internal_)
    let mutable _StatementKinds = new StatementKindTransformation(TransformationOptions.Default, _internal_)
    let mutable _Statements = new StatementTransformation(TransformationOptions.Default, _internal_)
    let mutable _Namespaces = new NamespaceTransformation(TransformationOptions.Default, _internal_)

    /// Transformation invoked for all types encountered when traversing (parts of) the syntax tree.
    member this.Types
        with get () = _Types
        and set value = _Types <- value

    /// Transformation invoked for all expression kinds encountered when traversing (parts of) the syntax tree.
    member this.ExpressionKinds
        with get () = _ExpressionKinds
        and set value = _ExpressionKinds <- value

    /// Transformation invoked for all expressions encountered when traversing (parts of) the syntax tree.
    member this.Expressions
        with get () = _Expressions
        and set value = _Expressions <- value

    /// Transformation invoked for all statement kinds encountered when traversing (parts of) the syntax tree.
    member this.StatementKinds
        with get () = _StatementKinds
        and set value = _StatementKinds <- value

    /// Transformation invoked for all statements encountered when traversing (parts of) the syntax tree.
    member this.Statements
        with get () = _Statements
        and set value = _Statements <- value

    /// Transformation invoked for all namespaces encountered when traversing (parts of) the syntax tree.
    member this.Namespaces
        with get () = _Namespaces
        and set value = _Namespaces <- value

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


and TypeTransformation internal (options, _internal_) =
    inherit TypeTransformationBase(options)
    let mutable _Transformation: SyntaxTreeTransformation option = None // will be set to a suitable Some value once construction is complete

    /// Handle to the parent SyntaxTreeTransformation.
    /// This handle is always safe to access and will be set to a suitable value
    /// even if no parent transformation has been specified upon construction.
    member this.Transformation
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    new(parentTransformation: SyntaxTreeTransformation, options: TransformationOptions) as this =
        new TypeTransformation(options, "_internal_")
        then this.Transformation <- parentTransformation

    new(options: TransformationOptions) as this =
        TypeTransformation(options, "_internal_")
        then
            this.Transformation <- new SyntaxTreeTransformation(options)
            this.Transformation.Types <- this

    new(parentTransformation: SyntaxTreeTransformation) =
        new TypeTransformation(parentTransformation, TransformationOptions.Default)

    new() = new TypeTransformation(TransformationOptions.Default)


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
            this.TypeTransformationHandle <- fun _ -> value.Types :> TypeTransformationBase

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
            this.ExpressionTransformationHandle <- fun _ -> value.Expressions :> ExpressionTransformationBase

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
