// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core
    

type QsSyntaxTreeTransformation<'T> private (state : 'T, dummy) =     

    let mutable _Types           = new ExpressionTypeTransformation<'T>()
    let mutable _ExpressionKinds = new ExpressionKindTransformation<'T>() 
    let mutable _Expressions     = new ExpressionTransformation<'T>()     
    let mutable _StatementKinds  = new StatementKindTransformation<'T>()  
    let mutable _Statements      = new StatementTransformation<'T>()      
    let mutable _Namespaces      = new NamespaceTransformation<'T>()      

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

    abstract member NewExpressionTypeTransformation : unit -> ExpressionTypeTransformation<'T>
    default this.NewExpressionTypeTransformation () = new ExpressionTypeTransformation<'T>(this)

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
            this.Types           <- this.NewExpressionTypeTransformation()
            this.ExpressionKinds <- this.NewExpressionKindTransformation()
            this.Expressions     <- this.NewExpressionTransformation()
            this.StatementKinds  <- this.NewStatementKindTransformation()
            this.Statements      <- this.NewStatementTransformation()
            this.Namespaces      <- this.NewNamespaceTransformation()
        

and ExpressionTypeTransformation<'T> private (parentTransformation) = 
    inherit ExpressionTypeTransformation()
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = parentTransformation

    member this.Transformation 
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    internal new() = ExpressionTypeTransformation<'T>(None)
    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = ExpressionTypeTransformation<'T>(Some parentTransformation)


and ExpressionKindTransformation<'T> private (parentTransformation) = 
    inherit ExpressionKindTransformation()
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = parentTransformation

    member this.Transformation 
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    internal new() = ExpressionKindTransformation<'T>(None)
    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = ExpressionKindTransformation<'T>(Some parentTransformation)
    
    override this.ExpressionTransformation ex = this.Transformation.Expressions.Transform ex
    override this.TypeTransformation t = this.Transformation.Types.Transform t

    
and ExpressionTransformation<'T> private (parentTransformation) = 
    inherit ExpressionTransformation()
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = parentTransformation

    member this.Transformation 
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    internal new() = ExpressionTransformation<'T>(None)
    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = ExpressionTransformation<'T>(Some parentTransformation)

    override this.Kind = upcast this.Transformation.ExpressionKinds
    override this.Type = upcast this.Transformation.Types


and StatementKindTransformation<'T> private (parentTransformation) = 
    inherit StatementKindTransformation()
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = parentTransformation

    member this.Transformation 
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    internal new() = StatementKindTransformation<'T>(None)
    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = StatementKindTransformation<'T>(Some parentTransformation)

    override this.ScopeTransformation scope = this.Transformation.Statements.Transform scope
    override this.ExpressionTransformation ex = this.Transformation.Expressions.Transform ex
    override this.TypeTransformation t = this.Transformation.Types.Transform t
    override this.LocationTransformation loc = this.Transformation.Namespaces.onLocation loc


and StatementTransformation<'T> private (parentTransformation) = 
    inherit ScopeTransformation()
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = parentTransformation

    internal new() = StatementTransformation<'T>(None)
    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = StatementTransformation<'T>(Some parentTransformation)

    member this.Transformation 
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    override this.Expression = upcast this.Transformation.Expressions
    override this.StatementKind = upcast this.Transformation.StatementKinds


and NamespaceTransformation<'T> private (parentTransformation) = 
    inherit SyntaxTreeTransformation()
    let mutable _Transformation : QsSyntaxTreeTransformation<'T> option = parentTransformation

    internal new() = NamespaceTransformation<'T>(None)
    new (parentTransformation : QsSyntaxTreeTransformation<'T>) = NamespaceTransformation<'T>(Some parentTransformation)

    member this.Transformation 
        with get () = _Transformation.Value
        and private set value = _Transformation <- Some value

    override this.Scope = upcast this.Transformation.Statements


