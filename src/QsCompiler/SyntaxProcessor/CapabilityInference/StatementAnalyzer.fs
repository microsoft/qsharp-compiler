// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.StatementAnalyzer

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

type StatementPattern =
    | StatementPattern of Range QsNullable

    interface IPattern with
        member _.Capability _ = RuntimeCapability.Base // TODO

        member _.Diagnose _ = None // TODO

        member _.Explain _ = Seq.empty

let analyze action =
    let transformation = SyntaxTreeTransformation TransformationOptions.NoRebuild
    let patterns = ResizeArray()
    let mutable numReturns = 0

    transformation.Statements <-
        { new StatementTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnStatement statement =
                let range = statement.Location |> QsNullable<_>.Map (fun l -> l.Offset + l.Range)

                match statement.Statement with
                | QsFailStatement _
                | QsRepeatStatement _
                | QsValueUpdate _ // TODO: Update-and-reassign only?
                | QsWhileStatement _ -> StatementPattern range |> patterns.Add
                | QsReturnStatement _ ->
                    numReturns <- numReturns + 1
                    if numReturns > 1 then StatementPattern range |> patterns.Add
                | _ -> ()

                base.OnStatement statement
        }

    action transformation
    Seq.map (fun p -> p :> IPattern) patterns
