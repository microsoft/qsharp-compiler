// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core
    

type QsSyntaxTreeTransformation<'T> (state : 'T) as this =     

    member this.InternalState = state

    abstract member InitializeExpressionTypeTransformation : unit -> ExpressionTypeTransformation<'T>
    default this.InitializeExpressionTypeTransformation () = new ExpressionTypeTransformation<'T>(this)

    abstract member InitializeExpressionKindTransformation : unit -> ExpressionKindTransformation<'T>
    default this.InitializeExpressionKindTransformation () = new ExpressionKindTransformation<'T>(this)

    abstract member InitializeExpressionTransformation : unit -> ExpressionTransformation<'T>
    default this.InitializeExpressionTransformation () = new ExpressionTransformation<'T>(this)
    
    abstract member InitializeStatementKindTransformation : unit -> StatementKindTransformation<'T>
    default this.InitializeStatementKindTransformation () = new StatementKindTransformation<'T>(this)
    
    abstract member InitializeStatementTransformation : unit -> StatementTransformation<'T>
    default this.InitializeStatementTransformation () = new StatementTransformation<'T>(this)
    
    abstract member InitializeNamespaceTransformation : unit -> NamespaceTransformation<'T>
    default this.InitializeNamespaceTransformation () = new NamespaceTransformation<'T>(this)
    
    member val ExpressionTypeTransformation = this.InitializeExpressionTypeTransformation()
    member val ExpressionKindTransformation = this.InitializeExpressionKindTransformation()
    member val ExpressionTransformation     = this.InitializeExpressionTransformation()
    member val StatementKindTransformation  = this.InitializeStatementKindTransformation()
    member val StatementTransformation      = this.InitializeStatementTransformation()
    member val NamespaceTransformation      = this.InitializeNamespaceTransformation()


and ExpressionTypeTransformation<'T>(parent) = 
    inherit ExpressionTypeTransformation()
    member this.Parent : QsSyntaxTreeTransformation<'T> = parent


and ExpressionKindTransformation<'T >(parent) = 
    inherit ExpressionKindTransformation()
    member this.Parent : QsSyntaxTreeTransformation<'T> = parent
    
    override this.ExpressionTransformation ex = this.Parent.ExpressionTransformation.Transform ex
    override this.TypeTransformation t = this.Parent.ExpressionTypeTransformation.Transform t

    
and ExpressionTransformation<'T>(parent) = 
    inherit ExpressionTransformation()
    member this.Parent : QsSyntaxTreeTransformation<'T> = parent


and StatementKindTransformation<'T>(parent) = 
    inherit StatementKindTransformation()
    member this.Parent : QsSyntaxTreeTransformation<'T> = parent

    override this.ScopeTransformation scope = this.Parent.StatementTransformation.Transform scope
    override this.ExpressionTransformation ex = this.Parent.ExpressionTransformation.Transform ex
    override this.TypeTransformation t = this.Parent.ExpressionTypeTransformation.Transform t
    override this.LocationTransformation loc = this.Parent.NamespaceTransformation.onLocation loc


and StatementTransformation<'T>(parent) = 
    inherit ScopeTransformation()
    member this.Parent : QsSyntaxTreeTransformation<'T> = parent


and NamespaceTransformation<'T>(parent) = 
    inherit SyntaxTreeTransformation()
    member this.Parent : QsSyntaxTreeTransformation<'T> = parent


