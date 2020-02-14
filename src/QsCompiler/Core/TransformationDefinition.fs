// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core


type QsSyntaxTreeTransformation<'T> private (state : 'T, dummy) =

    let mutable _Types           = new TypeTransformation<'T>(None)
    let mutable _ExpressionKinds = new ExpressionKindTransformation<'T>(None)
    let mutable _Expressions     = new ExpressionTransformation<'T>(None)
    let mutable _StatementKinds  = new StatementKindTransformation<'T>(None)
    let mutable _Statements      = new StatementTransformation<'T>(None)
    let mutable _Namespaces      = new NamespaceTransformation<'T>(None)

    member this.Types
        with get() = _Types
        and private set value = _Types <- value

    member this.ExpressionKinds
        with get() = _ExpressionKinds
        and private set value = _ExpressionKinds <- value

    member this.Expressions
        with get() = _Expressions
        and private set value = _Expressions <- value

    member this.StatementKinds
        with get() = _StatementKinds
        and private set value = _StatementKinds <- value

    member this.Statements
        with get() = _Statements
        and private set value = _Statements <- value

    member this.Namespaces
        with get() = _Namespaces
        and private set value = _Namespaces <- value


    member this.InternalState = state

    abstract member NewTypeTransformation : unit -> TypeTransformation<'T>
    default this.NewTypeTransformation () = new TypeTransformation<'T>(this)

    abstract member NewExpressionKindTransformation : unit -> ExpressionKindTransformation<'T>
    default this.NewExpressionKindTransformation () = new ExpressionKindTransformation<'T>(this)

    abstract member NewExpressionTransformation : unit -> ExpressionTransformation<'T>
    default this.NewExpressionTransformation () = new ExpressionTransformation<'T>(this)

    abstract member NewStatementKindTransformation : unit -> StatementKindTransformation<'T>
    default this.NewStatementKindTransformation () = new StatementKindTransformation<'T>(this)

    abstract member NewStatementTransformation : unit -> StatementTransformation<'T>
    default this.NewStatementTransformation () = new StatementTransformation<'T>(this)

    abstract member NewNamespaceTransformation : unit -> NamespaceTransformation<'T>
    default this.NewNamespaceTransformation () = new NamespaceTransformation<'T>(this)

    new (state) as this =
        QsSyntaxTreeTransformation(state, 0) then
            this.Types           <- this.NewTypeTransformation()
            this.ExpressionKinds <- this.NewExpressionKindTransformation()
            this.Expressions     <- this.NewExpressionTransformation()
            this.StatementKinds  <- this.NewStatementKindTransformation()
            this.Statements      <- this.NewStatementTransformation()
            this.Namespaces      <- this.NewNamespaceTransformation()


and TypeTransformation<'T> internal (parentTransformation) =
    inherit TypeTransformationBase()
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = parentTransformation

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = TypeTransformation<'T>(Some parentTransformation)
    new (sharedInternalState : 'T) as this =
        TypeTransformation<'T>(None) then
            this.Transformation <- {
                new QsSyntaxTreeTransformation<'T>(sharedInternalState) with
                    override parent.NewTypeTransformation () = this
            }


and ExpressionKindTransformation<'T> internal (parentTransformation) =
    inherit ExpressionKindTransformation()
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = parentTransformation

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = ExpressionKindTransformation<'T>(Some parentTransformation)
    new (sharedInternalState : 'T) as this =
        ExpressionKindTransformation<'T>(None) then
            this.Transformation <- {
                new QsSyntaxTreeTransformation<'T>(sharedInternalState) with
                    override parent.NewExpressionKindTransformation () = this
            }

    override this.ExpressionTransformation ex = this.Transformation.Expressions.Transform ex
    override this.TypeTransformation t = this.Transformation.Types.Transform t


and ExpressionTransformation<'T> internal (parentTransformation) =
    inherit ExpressionTransformation()
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = parentTransformation

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = ExpressionTransformation<'T>(Some parentTransformation)
    new (sharedInternalState : 'T) as this =
        ExpressionTransformation<'T>(None) then
            this.Transformation <- {
                new QsSyntaxTreeTransformation<'T>(sharedInternalState) with
                    override parent.NewExpressionTransformation () = this
            }

    override this.Kind = upcast this.Transformation.ExpressionKinds
    override this.Type = upcast this.Transformation.Types


and StatementKindTransformation<'T> internal (parentTransformation) =
    inherit StatementKindTransformation()
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = parentTransformation

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = StatementKindTransformation<'T>(Some parentTransformation)
    new (sharedInternalState : 'T) as this =
        StatementKindTransformation<'T>(None) then
            this.Transformation <- {
                new QsSyntaxTreeTransformation<'T>(sharedInternalState) with
                    override parent.NewStatementKindTransformation () = this
            }

    override this.ScopeTransformation scope = this.Transformation.Statements.Transform scope
    override this.ExpressionTransformation ex = this.Transformation.Expressions.Transform ex
    override this.TypeTransformation t = this.Transformation.Types.Transform t
    override this.LocationTransformation loc = this.Transformation.Statements.onLocation loc


and StatementTransformation<'T> internal (parentTransformation) =
    inherit ScopeTransformation()
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = parentTransformation

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = StatementTransformation<'T>(Some parentTransformation)
    new (sharedInternalState : 'T) as this =
        StatementTransformation<'T>(None) then
            this.Transformation <- {
                new QsSyntaxTreeTransformation<'T>(sharedInternalState) with
                    override parent.NewStatementTransformation () = this
            }

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    override this.Expression = upcast this.Transformation.Expressions
    override this.StatementKind = upcast this.Transformation.StatementKinds


and NamespaceTransformation<'T> internal (parentTransformation) =
    inherit SyntaxTreeTransformation()
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = parentTransformation

    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = NamespaceTransformation<'T>(Some parentTransformation)
    new (sharedInternalState : 'T) as this =
        NamespaceTransformation<'T>(None) then
            this.Transformation <- {
                new QsSyntaxTreeTransformation<'T>(sharedInternalState) with
                    override parent.NewNamespaceTransformation () = this
            }

    member this.Transformation
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    override this.Scope = upcast this.Transformation.Statements


