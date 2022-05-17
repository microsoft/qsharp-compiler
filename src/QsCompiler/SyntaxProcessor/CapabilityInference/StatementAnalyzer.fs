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
    let capability = RuntimeCapability.withClassical ClassicalCapability.full RuntimeCapability.bottom

    let diagnose (target: Target) =
        let range = QsNullable.defaultValue Range.Zero range

        if RuntimeCapability.subsumes target.Capability capability then
            None
        else
            QsCompilerDiagnostic.Error(ErrorCode.UnsupportedClassicalCapability, [ target.Architecture ]) range
            |> Some

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
                let statement = base.OnStatement statement
                let range = (transformation.Offset, statement.Location) ||> QsNullable.Map2(fun o l -> o + l.Range)

                match statement.Statement with
                | QsFailStatement _
                | QsRepeatStatement _
                | QsWhileStatement _ -> createPattern range |> patterns.Add
                | QsReturnStatement _ ->
                    numReturns <- numReturns + 1
                    if numReturns > 1 then createPattern range |> patterns.Add
                | _ -> ()

                statement
        }

    action transformation
    patterns
