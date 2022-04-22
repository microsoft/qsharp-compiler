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

type TypePatternKind =
    | Conditional
    | Literal
    | Mutable of name: string
    | Expression
    | Return

type TypePattern =
    {
        Kind: TypePatternKind
        Type: ResolvedType
        Range: Range QsNullable
    }

    interface IPattern with
        member _.Capability = RuntimeCapability.Base // TODO

        member _.Diagnose _ = None // TODO

        member _.Explain(_, _, _) = Seq.empty

type TypeContext = { StringLiteralsOk: bool }

let isAlwaysSupported (_: ResolvedType) = true // TODO

let analyze (_: AnalyzerEnvironment) (action: AnalyzerAction) =
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
                    patterns.Add
                        {
                            Kind = Return
                            Type = expression.ResolvedType
                            Range = range
                        }
                | QsVariableDeclaration binding when binding.Kind = MutableBinding ->
                    for var in statement.SymbolDeclarations.Variables do
                        patterns.Add
                            {
                                Kind = Mutable var.VariableName
                                Type = var.Type
                                Range = range
                            }
                | _ -> ()

                base.OnStatement statement
        }

    transformation.Expressions <-
        { new ExpressionTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnTypedExpression expression =
                let range = QsNullable.Map2(+) transformation.Offset expression.Range

                match expression.Expression, expression.ResolvedType.Resolution with
                | CONDITIONAL _, _ ->
                    patterns.Add
                        {
                            Kind = Conditional
                            Type = expression.ResolvedType
                            Range = range
                        }
                | StringLiteral _, _ when context.StringLiteralsOk -> ()
                | RangeLiteral _, _ -> ()
                | _, BigInt
                | _, String
                | _, Range ->
                    patterns.Add
                        {
                            Kind = Expression
                            Type = expression.ResolvedType
                            Range = range
                        }
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
    Seq.choose (fun p -> if isAlwaysSupported p.Type then None else p :> IPattern |> Some) patterns
