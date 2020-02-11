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
    

type ExpressionTypeTransformation<'T when 'T :> QsSyntaxTreeTransformation>() = 

    let mutable _Parent : 'T option = None
    member this.Parent 
        with get () = _Parent.Value
        and internal set value = _Parent <- Some value

and ExpressionKindTransformation<'T when 'T :> QsSyntaxTreeTransformation>() = 

    let mutable _Parent : 'T option = None
    member this.Parent 
        with get () = _Parent.Value
        and internal set value = _Parent <- Some value
        
and ExpressionTransformation<'T when 'T :> QsSyntaxTreeTransformation>() = 

    let mutable _Parent : 'T option = None
    member this.Parent 
        with get () = _Parent.Value
        and internal set value = _Parent <- Some value

and StatementKindTransformation<'T when 'T :> QsSyntaxTreeTransformation>() = 

    let mutable _Parent : 'T option = None
    member this.Parent 
        with get () = _Parent.Value
        and internal set value = _Parent <- Some value

and StatementTransformation<'T when 'T :> QsSyntaxTreeTransformation>() = 

    let mutable _Parent : 'T option = None
    member this.Parent 
        with get () = _Parent.Value
        and internal set value = _Parent <- Some value

and NamespaceTransformation<'T when 'T :> QsSyntaxTreeTransformation>() = 

    let mutable _Parent : 'T option = None
    member this.Parent 
        with get () = _Parent.Value
        and internal set value = _Parent <- Some value


and QsSyntaxTreeTransformation private (dummy) = 

    member val ExpressionTypeTransformation = new ExpressionTypeTransformation<_> ()
    member val ExpressionKindTransformation = new ExpressionKindTransformation<_> ()
    member val ExpressionTransformation = new ExpressionTransformation<_> ()
    member val StatementKindTransformation  = new StatementKindTransformation<_> ()
    member val StatementTransformation = new StatementTransformation<_> ()
    member val NamespaceTransformation = new NamespaceTransformation<_> ()

    new() as this = QsSyntaxTreeTransformation(0) then 
        this.ExpressionTypeTransformation.Parent <- this
        this.ExpressionKindTransformation.Parent <- this
        this.ExpressionTransformation.Parent <- this
        this.StatementKindTransformation.Parent <- this
        this.StatementTransformation.Parent <- this
        this.NamespaceTransformation.Parent <- this

