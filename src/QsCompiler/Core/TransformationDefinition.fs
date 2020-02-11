// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core
    

type QsSyntaxTreeTransformation<'T> (state : 'T) as this =     

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
    
    member val Types           = this.NewExpressionTypeTransformation()
    member val ExpressionKinds = this.NewExpressionKindTransformation()
    member val Expressions     = this.NewExpressionTransformation()
    member val StatementKind   = this.NewStatementKindTransformation()
    member val Statements      = this.NewStatementTransformation()
    member val Namespaces      = this.NewNamespaceTransformation()


and ExpressionTypeTransformation<'T>(parent) = 
    inherit ExpressionTypeTransformation()
    member this.Parent : QsSyntaxTreeTransformation<'T> = parent


and ExpressionKindTransformation<'T >(parent) = 
    inherit ExpressionKindTransformation()
    member this.Parent : QsSyntaxTreeTransformation<'T> = parent
    
    override this.ExpressionTransformation ex = this.Parent.Expressions.Transform ex
    override this.TypeTransformation t = this.Parent.Types.Transform t

    
and ExpressionTransformation<'T>(parent) = 
    inherit ExpressionTransformation()
    member this.Parent : QsSyntaxTreeTransformation<'T> = parent


and StatementKindTransformation<'T>(parent) = 
    inherit StatementKindTransformation()
    member this.Parent : QsSyntaxTreeTransformation<'T> = parent

    override this.ScopeTransformation scope = this.Parent.Statements.Transform scope
    override this.ExpressionTransformation ex = this.Parent.Expressions.Transform ex
    override this.TypeTransformation t = this.Parent.Types.Transform t
    override this.LocationTransformation loc = this.Parent.Namespaces.onLocation loc


and StatementTransformation<'T>(parent) = 
    inherit ScopeTransformation()
    member this.Parent : QsSyntaxTreeTransformation<'T> = parent


and NamespaceTransformation<'T>(parent) = 
    inherit SyntaxTreeTransformation()
    member this.Parent : QsSyntaxTreeTransformation<'T> = parent


