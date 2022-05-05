// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.StatementAnalyzer

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

let createPattern range =
    let range = QsNullable.defaultValue Range.Zero range
    let capability = RuntimeCapability.withClassical ClassicalCapability.full RuntimeCapability.bottom

    let diagnose target =
        QsCompilerDiagnostic.Error(ErrorCode.UnsupportedClassicalCapability, [ target.Architecture ]) range
        |> Some
        |> Option.filter (fun _ -> target.Capability < capability)

    {
        Capability = capability
        Diagnose = diagnose
        Properties = ()
    }

let analyzer (action: SyntaxTreeTransformation -> _) : _ seq =
    let transformation = LocatingTransformation TransformationOptions.NoRebuild
    let patterns = ResizeArray()
    let mutable numReturns = 0

    transformation.Statements <-
        { new StatementTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnStatement statement =
                let offset = QsNullable.defaultValue Position.Zero transformation.Offset
                let range = statement.Location |> QsNullable<_>.Map (fun l -> offset + l.Offset + l.Range)

                match statement.Statement with
                | QsFailStatement _
                | QsRepeatStatement _
                | QsWhileStatement _ -> createPattern range |> patterns.Add
                | QsReturnStatement _ ->
                    numReturns <- numReturns + 1
                    if numReturns > 1 then createPattern range |> patterns.Add
                | _ -> ()

                base.OnStatement statement
        }

    action transformation
    patterns
