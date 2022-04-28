// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.TypeAnalyzer

open System
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

type TypeUsage =
    | Conditional
    | Literal
    | Mutable of name: string
    | Expression
    | Return

type Context = { StringLiteralsOk: bool }

let isAlwaysSupported _type = true // TODO

let createPattern _usage ty _range =
    if isAlwaysSupported ty then
        None
    else
        Some
            {
                Capability = RuntimeCapability.bottom // TODO
                Diagnose = fun _ -> None // TODO
                Properties = ()
            }

let analyzer (action: SyntaxTreeTransformation -> _) : _ seq =
    let transformation = LocationTrackingTransformation TransformationOptions.NoRebuild
    let patterns = ResizeArray()
    let mutable context = { StringLiteralsOk = false }

    let local context' =
        let oldContext = context
        context <- context'

        { new IDisposable with
            member _.Dispose() = context <- oldContext
        }

    transformation.Statements <-
        { new StatementTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnStatement statement =
                let range = statement.Location |> QsNullable<_>.Map (fun l -> l.Offset + l.Range)

                match statement.Statement with
                | QsReturnStatement expression ->
                    createPattern Return expression.ResolvedType range |> Option.iter patterns.Add
                | QsVariableDeclaration binding when binding.Kind = MutableBinding ->
                    for var in statement.SymbolDeclarations.Variables do
                        createPattern (Mutable var.VariableName) var.Type range |> Option.iter patterns.Add
                | _ -> ()

                base.OnStatement statement
        }

    transformation.Expressions <-
        { new ExpressionTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnTypedExpression expression =
                let range = QsNullable.Map2(+) transformation.Offset expression.Range

                match expression.Expression, expression.ResolvedType.Resolution with
                | CONDITIONAL _, _ ->
                    createPattern Conditional expression.ResolvedType range |> Option.iter patterns.Add
                | StringLiteral _, _ when context.StringLiteralsOk -> ()
                | RangeLiteral _, _ -> ()
                | _, BigInt
                | _, String
                | _, Range -> createPattern Expression expression.ResolvedType range |> Option.iter patterns.Add
                | _ -> ()

                base.OnTypedExpression expression
        }

    transformation.ExpressionKinds <-
        { new ExpressionKindTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnCallLikeExpression(callable, args) =
                match callable.Expression with
                | Identifier (GlobalCallable { Namespace = "Microsoft.Quantum.Intrinsic"; Name = "Message" }, _) ->
                    let callable = transformation.Expressions.OnTypedExpression callable
                    use _ = local { context with StringLiteralsOk = true }
                    CallLikeExpression(callable, transformation.Expressions.OnTypedExpression args)
                | _ -> ``base``.OnCallLikeExpression(callable, args)
        }

    action transformation
    patterns
