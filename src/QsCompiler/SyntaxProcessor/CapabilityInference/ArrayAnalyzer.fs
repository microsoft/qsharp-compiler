// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.ArrayAnalyzer

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

type ArrayUsage = NonLiteralSize

let createPattern _usage _range =
    {
        Capability = RuntimeCapability.bottom // TODO
        Diagnose = fun _ -> None // TODO
        Properties = ()
    }

let analyzer (action: SyntaxTreeTransformation -> _) : _ seq =
    let transformation = LocatingTransformation TransformationOptions.NoRebuild
    let patterns = ResizeArray()

    let checkSize (size: TypedExpression) =
        match size.Expression with
        | IntLiteral _ -> ()
        | _ ->
            let range = QsNullable.Map2(+) transformation.Offset size.Range
            createPattern NonLiteralSize range |> patterns.Add

    transformation.StatementKinds <-
        { new StatementKindTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnQubitInitializer init =
                match init.Resolution with
                | QubitRegisterAllocation size -> checkSize size
                | _ -> ()

                base.OnQubitInitializer init
        }

    transformation.ExpressionKinds <-
        { new ExpressionKindTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnExpressionKind expression =
                match expression with
                | SizedArray (_, size)
                | NewArray (_, size) -> checkSize size
                | _ -> ()

                base.OnExpressionKind expression
        }

    action transformation
    patterns
