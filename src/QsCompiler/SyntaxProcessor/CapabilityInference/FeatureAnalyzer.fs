// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.FeatureAnalyzer

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

type Feature =
    | Fail
    | IndeterminateLoop
    | MultipleReturns
    | NontrivialCallee
    | DefaultArray

let createPattern range feature =
    let capability = TargetCapability.withClassicalCompute ClassicalCompute.full TargetCapability.bottom

    let diagnose (target: Target) =
        let range = QsNullable.defaultValue Range.Zero range

        if TargetCapability.subsumes target.Capability capability then
            None
        else
            // TODO: The capability description string should be defined with the rest of the diagnostic message
            // instead of here, but this is easier after https://github.com/microsoft/qsharp-compiler/issues/1025.
            let description =
                match feature with
                | Fail -> "fail statement"
                | IndeterminateLoop -> "repeat or while loop"
                | MultipleReturns -> "multiple returns"
                | NontrivialCallee -> "callee that is not a global identifier or functor"
                | DefaultArray -> "default-initialized array constructor"

            let args = [ target.Name; description ]
            QsCompilerDiagnostic.Error (ErrorCode.UnsupportedClassicalCapability, args) range |> Some

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
                transformation.OnRelativeLocation statement.Location |> ignore
                let range = (transformation.Offset, statement.Location) ||> QsNullable.Map2(fun o l -> o + l.Range)

                match statement.Statement with
                | QsFailStatement _ -> createPattern range Fail |> patterns.Add
                | QsRepeatStatement _
                | QsWhileStatement _ -> createPattern range IndeterminateLoop |> patterns.Add
                | QsReturnStatement _ ->
                    numReturns <- numReturns + 1
                    if numReturns > 1 then createPattern range MultipleReturns |> patterns.Add
                | _ -> ()

                base.OnStatement statement
        }

    transformation.Expressions <-
        { new ExpressionTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnTypedExpression expression =
                let range = QsNullable.Map2 (+) transformation.Offset expression.Range

                match expression.Expression with
                | CallLikeExpression (callee, _) ->
                    let isTrivial =
                        function
                        | InvalidExpr
                        | Identifier (GlobalCallable _, _)
                        | Identifier (InvalidIdentifier, _)
                        | AdjointApplication _
                        | ControlledApplication _ -> true
                        | _ -> false

                    if callee.Exists(isTrivial >> not) then createPattern range NontrivialCallee |> patterns.Add
                | NewArray _ -> createPattern range DefaultArray |> patterns.Add
                | _ -> ()

                base.OnTypedExpression expression
        }

    action transformation
    patterns
