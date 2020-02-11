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

    let mutable _ExpressionTypeTransformation = new ExpressionTypeTransformation<_> ()
    let mutable _ExpressionKindTransformation = new ExpressionKindTransformation<_> ()
    let mutable _ExpressionTransformation = new ExpressionTransformation<_> ()
    let mutable _StatementKindTransformation  = new StatementKindTransformation<_> ()
    let mutable _StatementTransformation = new StatementTransformation<_> ()
    let mutable _NamespaceTransformation = new NamespaceTransformation<_> ()

    member this.ExpressionTypeTransformation
        with get () = _ExpressionTypeTransformation
        and private set t = 
            _ExpressionTypeTransformation <- t
            t.Parent <- this

    member this.ExpressionKindTransformation 
        with get () = _ExpressionKindTransformation
        and private set t = 
            _ExpressionKindTransformation <- t
            t.Parent <- this

    member this.ExpressionTransformation 
        with get () = _ExpressionTransformation
        and private set t = 
            _ExpressionTransformation <- t
            t.Parent <- this

    member this.StatementKindTransformation 
        with get () = _StatementKindTransformation
        and private set t = 
            _StatementKindTransformation <- t
            t.Parent <- this

    member this.StatementTransformation
        with get () = _StatementTransformation
        and private set t = 
            _StatementTransformation <- t
            t.Parent <- this

    member this.NamespaceTransformation 
        with get () = _NamespaceTransformation
        and private set t = 
            _NamespaceTransformation <- t
            t.Parent <- this

    new() as this = 
        let foo = ()
        QsSyntaxTreeTransformation(0) then 
            this.ExpressionTypeTransformation.Parent <- this
            this.ExpressionKindTransformation.Parent <- this
            this.ExpressionTransformation.Parent <- this
            this.StatementKindTransformation.Parent <- this
            this.StatementTransformation.Parent <- this
            this.NamespaceTransformation.Parent <- this

