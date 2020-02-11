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
    

type QsSyntaxTreeTransformation<'T> private (state : 'T, init : QsSyntaxTreeTransformationInitialization<'T>) as this =     

    member this.InternalState = state

    member val ExpressionTypeTransformation = init.ExpressionTypeTransformation this
    member val ExpressionKindTransformation = init.ExpressionKindTransformation this
    member val ExpressionTransformation     = init.ExpressionTransformation this
    member val StatementKindTransformation  = init.StatementKindTransformation this
    member val StatementTransformation      = init.StatementTransformation this
    member val NamespaceTransformation      = init.NamespaceTransformation this

    new (state : 'T) =
        let init = {
            ExpressionTypeTransformation = fun this -> new ExpressionTypeTransformation<'T> (this)
            ExpressionKindTransformation = fun this -> new ExpressionKindTransformation<'T> (this)
            ExpressionTransformation     = fun this -> new ExpressionTransformation<'T> (this)
            StatementKindTransformation  = fun this -> new StatementKindTransformation<'T> (this)
            StatementTransformation      = fun this -> new StatementTransformation<'T> (this)
            NamespaceTransformation      = fun this -> new NamespaceTransformation<'T> (this)
        }
        QsSyntaxTreeTransformation(state, init)

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


and QsSyntaxTreeTransformationInitialization<'T> = {
    ExpressionTypeTransformation : QsSyntaxTreeTransformation<'T> -> ExpressionTypeTransformation<'T>
    ExpressionKindTransformation : QsSyntaxTreeTransformation<'T> -> ExpressionKindTransformation<'T>
    ExpressionTransformation : QsSyntaxTreeTransformation<'T> -> ExpressionTransformation<'T>
    StatementKindTransformation : QsSyntaxTreeTransformation<'T> -> StatementKindTransformation<'T>
    StatementTransformation : QsSyntaxTreeTransformation<'T> -> StatementTransformation<'T>
    NamespaceTransformation : QsSyntaxTreeTransformation<'T> -> NamespaceTransformation<'T>
}

