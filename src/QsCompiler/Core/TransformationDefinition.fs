// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core
    

type QsSyntaxTreeTransformation<'T> (state : 'T, dummy) =     

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
        

and ExpressionTypeTransformation<'T> private (parent) = 
    inherit ExpressionTypeTransformation()
    let mutable _Parent : QsSyntaxTreeTransformation<'T> option = parent

    member this.Parent 
        with get () = _Parent.Value
        and private set value = _Parent <- Some value

    internal new() = ExpressionTypeTransformation<'T>(None)
    new (parent : QsSyntaxTreeTransformation<'T>) = ExpressionTypeTransformation<'T>(Some parent)

and ExpressionKindTransformation<'T> private (parent) = 
    inherit ExpressionKindTransformation()
    let mutable _Parent : QsSyntaxTreeTransformation<'T> option = parent

    member this.Parent 
        with get () = _Parent.Value
        and private set value = _Parent <- Some value

    internal new() = ExpressionKindTransformation<'T>(None)
    new (parent : QsSyntaxTreeTransformation<'T>) = ExpressionKindTransformation<'T>(Some parent)
    
    override this.ExpressionTransformation ex = this.Parent.Expressions.Transform ex
    override this.TypeTransformation t = this.Parent.Types.Transform t

    
and ExpressionTransformation<'T> private (parent) = 
    inherit ExpressionTransformation()
    let mutable _Parent : QsSyntaxTreeTransformation<'T> option = parent

    member this.Parent 
        with get () = _Parent.Value
        and private set value = _Parent <- Some value

    internal new() = ExpressionTransformation<'T>(None)
    new (parent : QsSyntaxTreeTransformation<'T>) = ExpressionTransformation<'T>(Some parent)

    override this.Kind = upcast this.Parent.ExpressionKinds
    override this.Type = upcast this.Parent.Types


and StatementKindTransformation<'T> private (parent) = 
    inherit StatementKindTransformation()
    let mutable _Parent : QsSyntaxTreeTransformation<'T> option = parent

    member this.Parent 
        with get () = _Parent.Value
        and private set value = _Parent <- Some value

    internal new() = StatementKindTransformation<'T>(None)
    new (parent : QsSyntaxTreeTransformation<'T>) = StatementKindTransformation<'T>(Some parent)

    override this.ScopeTransformation scope = this.Parent.Statements.Transform scope
    override this.ExpressionTransformation ex = this.Parent.Expressions.Transform ex
    override this.TypeTransformation t = this.Parent.Types.Transform t
    override this.LocationTransformation loc = this.Parent.Namespaces.onLocation loc


and StatementTransformation<'T> private (parent) = 
    inherit ScopeTransformation()
    let mutable _Parent : QsSyntaxTreeTransformation<'T> option = parent

    internal new() = StatementTransformation<'T>(None)
    new (parent : QsSyntaxTreeTransformation<'T>) = StatementTransformation<'T>(Some parent)

    member this.Parent 
        with get () = _Parent.Value
        and private set value = _Parent <- Some value

    override this.Expression = upcast this.Parent.Expressions
    override this.StatementKind = upcast this.Parent.StatementKinds


and NamespaceTransformation<'T> private (parent) = 
    inherit SyntaxTreeTransformation()
    let mutable _Parent : QsSyntaxTreeTransformation<'T> option = parent

    internal new() = NamespaceTransformation<'T>(None)
    new (parent : QsSyntaxTreeTransformation<'T>) = NamespaceTransformation<'T>(Some parent)

    member this.Parent 
        with get () = _Parent.Value
        and private set value = _Parent <- Some value

    override this.Scope = upcast this.Parent.Statements


