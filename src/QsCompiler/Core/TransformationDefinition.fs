// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core


type QsSyntaxTreeTransformation<'T> private (state : 'T, unsafe) =

    let mutable _Types           = new TypeTransformation<'T>(None)
    let mutable _ExpressionKinds = new ExpressionKindTransformation<'T>(None)
    let mutable _Expressions     = new ExpressionTransformation<'T>(None)
    let mutable _StatementKinds  = new StatementKindTransformation<'T>(None)
    let mutable _Statements      = new StatementTransformation<'T>(None)
    let mutable _Namespaces      = new NamespaceTransformation<'T>(None)

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
        QsSyntaxTreeTransformation(state, 0) then
            this.Types           <- new TypeTransformation<'T>(this)
            this.ExpressionKinds <- new ExpressionKindTransformation<'T>(this)
            this.Expressions     <- new ExpressionTransformation<'T>(this)
            this.StatementKinds  <- new StatementKindTransformation<'T>(this)
            this.Statements      <- new StatementTransformation<'T>(this)
            this.Namespaces      <- new NamespaceTransformation<'T>(this)


and TypeTransformation<'T> internal (parentTransformation) =
    inherit TypeTransformationBase()
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = parentTransformation

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = TypeTransformation<'T>(Some parentTransformation)
    new (sharedInternalState : 'T) as this =
        TypeTransformation<'T>(None) then
            this.Transformation <- new QsSyntaxTreeTransformation<'T>(sharedInternalState)
            this.Transformation.Types <- this
            

and ExpressionKindTransformation<'T> internal (parentTransformation : QsSyntaxTreeTransformation<'T> option) =
    inherit ExpressionKindTransformationBase(
        (fun _ -> parentTransformation.Value.Expressions :> ExpressionTransformationBase),
        (fun _ -> parentTransformation.Value.Types :> TypeTransformationBase)) 
    let mutable _Transformation = parentTransformation

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = 
            _Transformation <- Some value
            this.ExpressionTransformationHandle <- fun _ -> value.Expressions :> ExpressionTransformationBase
            this.TypeTransformationHandle <- fun _ -> value.Types :> TypeTransformationBase

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = ExpressionKindTransformation<'T>(Some parentTransformation)
    new (sharedInternalState : 'T) as this =
        ExpressionKindTransformation<'T>(None) then
            this.Transformation <- new QsSyntaxTreeTransformation<'T>(sharedInternalState)
            this.Transformation.ExpressionKinds <- this


and ExpressionTransformation<'T> internal (parentTransformation : QsSyntaxTreeTransformation<'T> option) =
    inherit ExpressionTransformationBase(
        (fun _ -> parentTransformation.Value.ExpressionKinds :> ExpressionKindTransformationBase),
        (fun _ -> parentTransformation.Value.Types :> TypeTransformationBase))
    let mutable _Transformation = parentTransformation

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = 
            _Transformation <- Some value
            this.ExpressionKindTransformationHandle <- fun _ -> value.ExpressionKinds :> ExpressionKindTransformationBase
            this.TypeTransformationHandle <- fun _ -> value.Types :> TypeTransformationBase

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = ExpressionTransformation<'T>(Some parentTransformation)
    new (sharedInternalState : 'T) as this =
        ExpressionTransformation<'T>(None) then
            this.Transformation <- new QsSyntaxTreeTransformation<'T>(sharedInternalState)
            this.Transformation.Expressions <- this


and StatementKindTransformation<'T> internal (parentTransformation) =
    inherit StatementKindTransformation()
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = parentTransformation

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = StatementKindTransformation<'T>(Some parentTransformation)
    new (sharedInternalState : 'T) as this =
        StatementKindTransformation<'T>(None) then
            this.Transformation <- new QsSyntaxTreeTransformation<'T>(sharedInternalState)
            this.Transformation.StatementKinds <- this

    override this.ScopeTransformation scope = this.Transformation.Statements.Transform scope
    override this.ExpressionTransformation ex = this.Transformation.Expressions.Transform ex
    override this.TypeTransformation t = this.Transformation.Types.Transform t
    override this.LocationTransformation loc = this.Transformation.Statements.onLocation loc


and StatementTransformation<'T> internal (parentTransformation) =
    inherit ScopeTransformation()
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = parentTransformation

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = StatementTransformation<'T>(Some parentTransformation)
    new (sharedInternalState : 'T) as this =
        StatementTransformation<'T>(None) then
            this.Transformation <- new QsSyntaxTreeTransformation<'T>(sharedInternalState)
            this.Transformation.Statements <- this

    override this.Expression = upcast this.Transformation.Expressions
    override this.StatementKind = upcast this.Transformation.StatementKinds


and NamespaceTransformation<'T> internal (parentTransformation) =
    inherit SyntaxTreeTransformation()
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = parentTransformation

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = NamespaceTransformation<'T>(Some parentTransformation)
    new (sharedInternalState : 'T) as this =
        NamespaceTransformation<'T>(None) then
            this.Transformation <- new QsSyntaxTreeTransformation<'T>(sharedInternalState)
            this.Transformation.Namespaces <- this

    override this.Scope = upcast this.Transformation.Statements


