// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core


type QsSyntaxTreeTransformation<'T> private (state : 'T, unsafe : string) =

    let mutable _Types           = new TypeTransformation<'T>(unsafe)
    let mutable _ExpressionKinds = new ExpressionKindTransformation<'T>(unsafe)
    let mutable _Expressions     = new ExpressionTransformation<'T>(unsafe)
    let mutable _StatementKinds  = new StatementKindTransformation<'T>(unsafe)
    let mutable _Statements      = new StatementTransformation<'T>(unsafe)
    let mutable _Namespaces      = new NamespaceTransformation<'T>(unsafe)

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


and TypeTransformation<'T> internal (unsafe) =
    inherit TypeTransformationBase()
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = None

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) as this = 
        new TypeTransformation<'T>("unsafe") then
            this.Transformation <- parentTransformation

    new (sharedInternalState : 'T) as this =
        TypeTransformation<'T>("unsafe") then
            this.Transformation <- new QsSyntaxTreeTransformation<'T>(sharedInternalState)
            this.Transformation.Types <- this
            

and ExpressionKindTransformation<'T> internal (unsafe) =
    inherit ExpressionKindTransformationBase(unsafe)
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = None

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = 
            _Transformation <- Some value
            this.ExpressionTransformationHandle <- fun _ -> value.Expressions :> ExpressionTransformationBase
            this.TypeTransformationHandle <- fun _ -> value.Types :> TypeTransformationBase

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) as this = 
        ExpressionKindTransformation<'T>("unsafe") then 
            this.Transformation <- parentTransformation

    new (sharedInternalState : 'T) as this =
        ExpressionKindTransformation<'T>("unsafe") then
            this.Transformation <- new QsSyntaxTreeTransformation<'T>(sharedInternalState)
            this.Transformation.ExpressionKinds <- this


and ExpressionTransformation<'T> internal (unsafe) =
    inherit ExpressionTransformationBase(unsafe)
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = None

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = 
            _Transformation <- Some value
            this.ExpressionKindTransformationHandle <- fun _ -> value.ExpressionKinds :> ExpressionKindTransformationBase
            this.TypeTransformationHandle <- fun _ -> value.Types :> TypeTransformationBase

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) as this = 
        ExpressionTransformation<'T>("unsafe") then
            this.Transformation <- parentTransformation

    new (sharedInternalState : 'T) as this =
        ExpressionTransformation<'T>("unsafe") then
            this.Transformation <- new QsSyntaxTreeTransformation<'T>(sharedInternalState)
            this.Transformation.Expressions <- this


and StatementKindTransformation<'T> internal (unsafe) =
    inherit StatementKindTransformationBase(unsafe)
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = None

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = 
            _Transformation <- Some value
            this.StatementTransformationHandle <- fun _ -> value.Statements :> StatementTransformationBase
            this.ExpressionTransformationHandle <- fun _ -> value.Expressions :> ExpressionTransformationBase

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) as this = 
        StatementKindTransformation<'T>("unsafe") then
            this.Transformation <- parentTransformation
        
    new (sharedInternalState : 'T) as this =
        StatementKindTransformation<'T>("unsafe") then
            this.Transformation <- new QsSyntaxTreeTransformation<'T>(sharedInternalState)
            this.Transformation.StatementKinds <- this


and StatementTransformation<'T> internal (unsafe) =
    inherit StatementTransformationBase(unsafe)
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = None

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = 
            _Transformation <- Some value
            this.StatementKindTransformationHandle <- fun _ -> value.StatementKinds :> StatementKindTransformationBase
            this.ExpressionTransformationHandle <- fun _ -> value.Expressions :> ExpressionTransformationBase

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) as this = 
        StatementTransformation<'T>("unsafe") then
            this.Transformation <- parentTransformation

    new (sharedInternalState : 'T) as this =
        StatementTransformation<'T>("unsafe") then
            this.Transformation <- new QsSyntaxTreeTransformation<'T>(sharedInternalState)
            this.Transformation.Statements <- this


and NamespaceTransformation<'T> internal (unsafe : string) =
    inherit NamespaceTransformationBase(unsafe)
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = None

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = 
            _Transformation <- Some value
            this.StatementTransformationHandle <- fun _ -> value.Statements :> StatementTransformationBase

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) as this = 
        NamespaceTransformation<'T>("unsafe") then
            this.Transformation <- parentTransformation

    new (sharedInternalState : 'T) as this =
        NamespaceTransformation<'T>("unsafe") then
            this.Transformation <- new QsSyntaxTreeTransformation<'T>(sharedInternalState)
            this.Transformation.Namespaces <- this


type QsSyntaxTreeTransformation private (unsafe : string) =

    let mutable _Types           = new TypeTransformation(unsafe)
    let mutable _ExpressionKinds = new ExpressionKindTransformation(unsafe)
    let mutable _Expressions     = new ExpressionTransformation(unsafe)
    let mutable _StatementKinds  = new StatementKindTransformation(unsafe)
    let mutable _Statements      = new StatementTransformation(unsafe)
    let mutable _Namespaces      = new NamespaceTransformation(unsafe)

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

    new () as this =
        QsSyntaxTreeTransformation("unsafe") then
            this.Types           <- new TypeTransformation(this)
            this.ExpressionKinds <- new ExpressionKindTransformation(this)
            this.Expressions     <- new ExpressionTransformation(this)
            this.StatementKinds  <- new StatementKindTransformation(this)
            this.Statements      <- new StatementTransformation(this)
            this.Namespaces      <- new NamespaceTransformation(this)


and TypeTransformation internal (unsafe) =
    inherit TypeTransformationBase()
    let mutable _Transformation : QsSyntaxTreeTransformation option = None

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    new (parentTransformation : QsSyntaxTreeTransformation) as this = 
        new TypeTransformation("unsafe") then
            this.Transformation <- parentTransformation

    new () as this =
        TypeTransformation("unsafe") then
            this.Transformation <- new QsSyntaxTreeTransformation()
            this.Transformation.Types <- this
            

and ExpressionKindTransformation internal (unsafe) =
    inherit ExpressionKindTransformationBase(unsafe)
    let mutable _Transformation : QsSyntaxTreeTransformation option = None

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = 
            _Transformation <- Some value
            this.ExpressionTransformationHandle <- fun _ -> value.Expressions :> ExpressionTransformationBase
            this.TypeTransformationHandle <- fun _ -> value.Types :> TypeTransformationBase

    new (parentTransformation : QsSyntaxTreeTransformation) as this = 
        ExpressionKindTransformation("unsafe") then 
            this.Transformation <- parentTransformation

    new () as this =
        ExpressionKindTransformation("unsafe") then
            this.Transformation <- new QsSyntaxTreeTransformation()
            this.Transformation.ExpressionKinds <- this


and ExpressionTransformation internal (unsafe) =
    inherit ExpressionTransformationBase(unsafe)
    let mutable _Transformation : QsSyntaxTreeTransformation option = None

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = 
            _Transformation <- Some value
            this.ExpressionKindTransformationHandle <- fun _ -> value.ExpressionKinds :> ExpressionKindTransformationBase
            this.TypeTransformationHandle <- fun _ -> value.Types :> TypeTransformationBase

    new (parentTransformation : QsSyntaxTreeTransformation) as this = 
        ExpressionTransformation("unsafe") then
            this.Transformation <- parentTransformation

    new () as this =
        ExpressionTransformation("unsafe") then
            this.Transformation <- new QsSyntaxTreeTransformation()
            this.Transformation.Expressions <- this


and StatementKindTransformation internal (unsafe) =
    inherit StatementKindTransformationBase(unsafe)
    let mutable _Transformation : QsSyntaxTreeTransformation option = None

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = 
            _Transformation <- Some value
            this.StatementTransformationHandle <- fun _ -> value.Statements :> StatementTransformationBase
            this.ExpressionTransformationHandle <- fun _ -> value.Expressions :> ExpressionTransformationBase

    new (parentTransformation : QsSyntaxTreeTransformation) as this = 
        StatementKindTransformation("unsafe") then
            this.Transformation <- parentTransformation
        
    new () as this =
        StatementKindTransformation("unsafe") then
            this.Transformation <- new QsSyntaxTreeTransformation()
            this.Transformation.StatementKinds <- this


and StatementTransformation internal (unsafe) =
    inherit StatementTransformationBase(unsafe)
    let mutable _Transformation : QsSyntaxTreeTransformation option = None

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = 
            _Transformation <- Some value
            this.StatementKindTransformationHandle <- fun _ -> value.StatementKinds :> StatementKindTransformationBase
            this.ExpressionTransformationHandle <- fun _ -> value.Expressions :> ExpressionTransformationBase

    new (parentTransformation : QsSyntaxTreeTransformation) as this = 
        StatementTransformation("unsafe") then
            this.Transformation <- parentTransformation

    new () as this =
        StatementTransformation("unsafe") then
            this.Transformation <- new QsSyntaxTreeTransformation()
            this.Transformation.Statements <- this


and NamespaceTransformation internal (unsafe : string) =
    inherit NamespaceTransformationBase(unsafe)
    let mutable _Transformation : QsSyntaxTreeTransformation option = None

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = 
            _Transformation <- Some value
            this.StatementTransformationHandle <- fun _ -> value.Statements :> StatementTransformationBase

    new (parentTransformation : QsSyntaxTreeTransformation) as this = 
        NamespaceTransformation("unsafe") then
            this.Transformation <- parentTransformation

    new () as this =
        NamespaceTransformation("unsafe") then
            this.Transformation <- new QsSyntaxTreeTransformation()
            this.Transformation.Namespaces <- this
