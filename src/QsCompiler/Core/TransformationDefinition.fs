// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

open System.Collections.Immutable
open System.Numerics
open System.Runtime.CompilerServices
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open System

type private ExpressionKind = QsExpressionKind<TypedExpression,Identifier,ResolvedType>
type private ExpressionType = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>
    

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
    member this.Parent : QsSyntaxTreeTransformation<'T> = parent

and ExpressionKindTransformation<'T >(parent) = 
    member this.Parent : QsSyntaxTreeTransformation<'T> = parent
        
and ExpressionTransformation<'T>(parent) = 
    member this.Parent : QsSyntaxTreeTransformation<'T> = parent

and StatementKindTransformation<'T>(parent) = 
    member this.Parent : QsSyntaxTreeTransformation<'T> = parent

and StatementTransformation<'T>(parent) = 
    member this.Parent : QsSyntaxTreeTransformation<'T> = parent

and NamespaceTransformation<'T>(parent) = 
    member this.Parent : QsSyntaxTreeTransformation<'T> = parent


