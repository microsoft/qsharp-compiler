// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.ResultAnalyzer

open System
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

type ResultPatternKind =
    /// A return statement in the block of an if statement whose condition depends on a result.
    | ReturnInDependentBlock

    /// A set statement in the block of an if statement whose condition depends on a result, but which is reassigning a
    /// variable declared outside the block.
    | SetInDependentBlock of name: string

    /// An equality expression inside the condition of an if statement, where the operands are results
    | ConditionalEquality

    /// An equality expression outside the condition of an if statement, where the operands are results.
    | UnconditionalEquality

type ResultPattern =
    {
        Kind: ResultPatternKind
        Range: Range QsNullable
    }

    interface IPattern with
        member pattern.Capability inOperation =
            match pattern.Kind with
            | ReturnInDependentBlock
            | SetInDependentBlock _
            | UnconditionalEquality -> FullComputation
            | ConditionalEquality -> if inOperation then BasicMeasurementFeedback else FullComputation

        member pattern.Diagnose context =
            let error code args =
                if context.Capability.Implies((pattern :> IPattern).Capability context.IsInOperation) then
                    None
                else
                    QsCompilerDiagnostic.Error(code, args) (pattern.Range.ValueOr Range.Zero) |> Some

            let unsupported =
                if context.Capability = BasicMeasurementFeedback then
                    ErrorCode.ResultComparisonNotInOperationIf
                else
                    ErrorCode.UnsupportedResultComparison

            match pattern.Kind with
            | ReturnInDependentBlock ->
                if context.Capability = BasicMeasurementFeedback then
                    error ErrorCode.ReturnInResultConditionedBlock [ context.ProcessorArchitecture ]
                else
                    None
            | SetInDependentBlock name ->
                if context.Capability = BasicMeasurementFeedback then
                    error ErrorCode.SetInResultConditionedBlock [ name; context.ProcessorArchitecture ]
                else
                    None
            | ConditionalEquality -> error unsupported [ context.ProcessorArchitecture ]
            | UnconditionalEquality -> error unsupported [ context.ProcessorArchitecture ]

        member _.Explain _ = Seq.empty

type ResultContext = { InCondition: bool; FrozenVars: string Set }

/// Returns true if the expression is an equality or inequality comparison between two expressions of type Result.
let isResultEquality expression =
    let validType =
        function
        | InvalidType -> None
        | kind -> Some kind

    let binaryType lhs rhs =
        validType lhs.ResolvedType.Resolution |> Option.defaultValue rhs.ResolvedType.Resolution

    // This assumes that:
    // - Result has no subtype that supports equality comparisons.
    // - Compound types containing Result (e.g., tuples or arrays of results) do not support equality comparison.
    match expression.Expression with
    | EQ (lhs, rhs)
    | NEQ (lhs, rhs) -> binaryType lhs rhs = Result
    | _ -> false

let analyze (action: SyntaxTreeTransformation -> _) =
    let transformation = LocationTrackingTransformation TransformationOptions.NoRebuild
    let patterns = ResizeArray()
    let mutable dependsOnResult = false
    let mutable context = { InCondition = false; FrozenVars = Set.empty }

    let local context' =
        let oldContext = context
        context <- context'

        { new IDisposable with
            member _.Dispose() = context <- oldContext
        }

    transformation.Statements <-
        { new StatementTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnStatement statement =
                match statement.Statement with
                | QsReturnStatement _ when dependsOnResult ->
                    let range = statement.Location |> QsNullable<_>.Map (fun l -> l.Offset + l.Range)
                    patterns.Add { Kind = ReturnInDependentBlock; Range = range }

                | QsValueUpdate update ->
                    update.Lhs.ExtractAll (fun e ->
                        match e.Expression with
                        | Identifier (LocalVariable name, _) when Set.contains name context.FrozenVars ->
                            let range = (statement.Location, e.Range) ||> QsNullable.Map2(fun l r -> l.Offset + r)
                            [ { Kind = SetInDependentBlock name; Range = range } ]
                        | _ -> [])
                    |> patterns.AddRange

                | _ -> ()

                base.OnStatement statement
        }

    transformation.StatementKinds <-
        { new StatementKindTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnConditionalStatement statement =
                let oldDependsOnResult = dependsOnResult
                base.OnConditionalStatement statement |> ignore
                dependsOnResult <- oldDependsOnResult
                QsConditionalStatement statement

            override _.OnPositionedBlock(condition, block) =
                transformation.OnRelativeLocation block.Location |> ignore

                for c in condition do
                    use _ = local { context with InCondition = true }
                    transformation.Expressions.OnTypedExpression c |> ignore

                let knownVars = Seq.map (fun v -> v.VariableName) block.Body.KnownSymbols.Variables |> Set.ofSeq
                use _ = local (if dependsOnResult then { context with FrozenVars = knownVars } else context)
                transformation.Statements.OnScope block.Body |> ignore

                condition, block
        }

    transformation.Expressions <-
        { new ExpressionTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnTypedExpression expression =
                if isResultEquality expression then
                    let range = QsNullable.Map2(+) transformation.Offset expression.Range

                    if context.InCondition then
                        dependsOnResult <- true
                        patterns.Add { Kind = ConditionalEquality; Range = range }
                    else
                        patterns.Add { Kind = UnconditionalEquality; Range = range }

                base.OnTypedExpression expression
        }

    action transformation
    Seq.map (fun p -> p :> IPattern) patterns
