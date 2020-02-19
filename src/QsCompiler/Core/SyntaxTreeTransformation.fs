// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core


type QsSyntaxTreeTransformation<'T> private (state : 'T, unsafe : string) =

    let mutable _Types           = new TypeTransformation<'T>(TransformationOptions.Default, unsafe)
    let mutable _ExpressionKinds = new ExpressionKindTransformation<'T>(TransformationOptions.Default, unsafe)
    let mutable _Expressions     = new ExpressionTransformation<'T>(TransformationOptions.Default, unsafe)
    let mutable _StatementKinds  = new StatementKindTransformation<'T>(TransformationOptions.Default, unsafe)
    let mutable _Statements      = new StatementTransformation<'T>(TransformationOptions.Default, unsafe)
    let mutable _Namespaces      = new NamespaceTransformation<'T>(TransformationOptions.Default, unsafe)

    member this.Types
        with get() = _Types
        and set value = _Types <- value

    member this.ExpressionKinds
        with get() = _ExpressionKinds
        and set value = _ExpressionKinds <- value

    member this.Expressions
        with get() = _Expressions
        and set value = _Expressions <- value

    member this.StatementKinds
        with get() = _StatementKinds
        and set value = _StatementKinds <- value

    member this.Statements
        with get() = _Statements
        and set value = _Statements <- value

    member this.Namespaces
        with get() = _Namespaces
        and set value = _Namespaces <- value


    member this.InternalState = state

    new (state) as this =
        QsSyntaxTreeTransformation(state, "unsafe") then
            this.Types           <- new TypeTransformation<'T>(this)
            this.ExpressionKinds <- new ExpressionKindTransformation<'T>(this)
            this.Expressions     <- new ExpressionTransformation<'T>(this)
            this.StatementKinds  <- new StatementKindTransformation<'T>(this)
            this.Statements      <- new StatementTransformation<'T>(this)
            this.Namespaces      <- new NamespaceTransformation<'T>(this)


and TypeTransformation<'T> internal (options, unsafe) =
    inherit TypeTransformationBase(options)
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = None

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    new (parentTransformation : QsSyntaxTreeTransformation<'T>, options : TransformationOptions) as this = 
        new TypeTransformation<'T>(options, "unsafe") then
            this.Transformation <- parentTransformation

    new (sharedInternalState : 'T, options : TransformationOptions) as this =
        TypeTransformation<'T>(options, "unsafe") then
            this.Transformation <- new QsSyntaxTreeTransformation<'T>(sharedInternalState)
            this.Transformation.Types <- this

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = 
        new TypeTransformation<'T>(parentTransformation, TransformationOptions.Default)
            
    new (sharedInternalState : 'T) = 
        new TypeTransformation<'T>(sharedInternalState, TransformationOptions.Default)


and ExpressionKindTransformation<'T> internal (options, unsafe) =
    inherit ExpressionKindTransformationBase(options, unsafe)
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = None

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = 
            _Transformation <- Some value
            this.ExpressionTransformationHandle <- fun _ -> value.Expressions :> ExpressionTransformationBase
            this.TypeTransformationHandle <- fun _ -> value.Types :> TypeTransformationBase

    new (parentTransformation : QsSyntaxTreeTransformation<'T>, options : TransformationOptions) as this = 
        ExpressionKindTransformation<'T>(options, "unsafe") then 
            this.Transformation <- parentTransformation

    new (sharedInternalState : 'T, options : TransformationOptions) as this =
        ExpressionKindTransformation<'T>(options, "unsafe") then
            this.Transformation <- new QsSyntaxTreeTransformation<'T>(sharedInternalState)
            this.Transformation.ExpressionKinds <- this

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = 
        new ExpressionKindTransformation<'T>(parentTransformation, TransformationOptions.Default)
        
    new (sharedInternalState : 'T) = 
        new ExpressionKindTransformation<'T>(sharedInternalState, TransformationOptions.Default)


and ExpressionTransformation<'T> internal (options, unsafe) =
    inherit ExpressionTransformationBase(options, unsafe)
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = None

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = 
            _Transformation <- Some value
            this.ExpressionKindTransformationHandle <- fun _ -> value.ExpressionKinds :> ExpressionKindTransformationBase
            this.TypeTransformationHandle <- fun _ -> value.Types :> TypeTransformationBase

    new (parentTransformation : QsSyntaxTreeTransformation<'T>, options : TransformationOptions) as this = 
        ExpressionTransformation<'T>(options, "unsafe") then
            this.Transformation <- parentTransformation

    new (sharedInternalState : 'T, options : TransformationOptions) as this =
        ExpressionTransformation<'T>(options, "unsafe") then
            this.Transformation <- new QsSyntaxTreeTransformation<'T>(sharedInternalState)
            this.Transformation.Expressions <- this

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = 
        new ExpressionTransformation<'T>(parentTransformation, TransformationOptions.Default)
    
    new (sharedInternalState : 'T) = 
        new ExpressionTransformation<'T>(sharedInternalState, TransformationOptions.Default)


and StatementKindTransformation<'T> internal (options, unsafe) =
    inherit StatementKindTransformationBase(options, unsafe)
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = None

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = 
            _Transformation <- Some value
            this.StatementTransformationHandle <- fun _ -> value.Statements :> StatementTransformationBase
            this.ExpressionTransformationHandle <- fun _ -> value.Expressions :> ExpressionTransformationBase

    new (parentTransformation : QsSyntaxTreeTransformation<'T>, options : TransformationOptions) as this = 
        StatementKindTransformation<'T>(options, "unsafe") then
            this.Transformation <- parentTransformation
        
    new (sharedInternalState : 'T, options : TransformationOptions) as this =
        StatementKindTransformation<'T>(options, "unsafe") then
            this.Transformation <- new QsSyntaxTreeTransformation<'T>(sharedInternalState)
            this.Transformation.StatementKinds <- this

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = 
        new StatementKindTransformation<'T>(parentTransformation, TransformationOptions.Default)
    
    new (sharedInternalState : 'T) = 
        new StatementKindTransformation<'T>(sharedInternalState, TransformationOptions.Default)


and StatementTransformation<'T> internal (options, unsafe) =
    inherit StatementTransformationBase(options, unsafe)
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = None

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = 
            _Transformation <- Some value
            this.StatementKindTransformationHandle <- fun _ -> value.StatementKinds :> StatementKindTransformationBase
            this.ExpressionTransformationHandle <- fun _ -> value.Expressions :> ExpressionTransformationBase

    new (parentTransformation : QsSyntaxTreeTransformation<'T>, options : TransformationOptions) as this = 
        StatementTransformation<'T>(options, "unsafe") then
            this.Transformation <- parentTransformation

    new (sharedInternalState : 'T, options : TransformationOptions) as this =
        StatementTransformation<'T>(options, "unsafe") then
            this.Transformation <- new QsSyntaxTreeTransformation<'T>(sharedInternalState)
            this.Transformation.Statements <- this

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = 
        new StatementTransformation<'T>(parentTransformation, TransformationOptions.Default)
    
    new (sharedInternalState : 'T) = 
        new StatementTransformation<'T>(sharedInternalState, TransformationOptions.Default)


and NamespaceTransformation<'T> internal (options, unsafe : string) =
    inherit NamespaceTransformationBase(options, unsafe)
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = None

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = 
            _Transformation <- Some value
            this.StatementTransformationHandle <- fun _ -> value.Statements :> StatementTransformationBase

    new (parentTransformation : QsSyntaxTreeTransformation<'T>, options : TransformationOptions) as this = 
        NamespaceTransformation<'T>(options, "unsafe") then
            this.Transformation <- parentTransformation

    new (sharedInternalState : 'T, options : TransformationOptions) as this =
        NamespaceTransformation<'T>(options, "unsafe") then
            this.Transformation <- new QsSyntaxTreeTransformation<'T>(sharedInternalState)
            this.Transformation.Namespaces <- this

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = 
        new NamespaceTransformation<'T>(parentTransformation, TransformationOptions.Default)
    
    new (sharedInternalState : 'T) = 
        new NamespaceTransformation<'T>(sharedInternalState, TransformationOptions.Default)


