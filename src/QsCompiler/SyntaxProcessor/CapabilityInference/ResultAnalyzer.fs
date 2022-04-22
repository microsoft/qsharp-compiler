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
    /// An equality expression inside the condition of an if statement, where the operands are results
    | ConditionalEqualityInOperation

    /// An equality expression outside the condition of an if statement, where the operands are results.
    | UnrestrictedEquality

    /// A return statement in the block of an if statement whose condition depends on a result.
    | ReturnInDependentBlock

    /// A set statement in the block of an if statement whose condition depends on a result, but which is reassigning a
    /// variable declared outside the block.
    | SetInDependentBlock of name: string

type ResultPattern =
    {
        Kind: ResultPatternKind
        Range: Range QsNullable
    }

    interface IPattern with
        member pattern.Capability =
            match pattern.Kind with
            | ConditionalEqualityInOperation -> BasicMeasurementFeedback
            | UnrestrictedEquality
            | ReturnInDependentBlock
            | SetInDependentBlock _ -> FullComputation

        member pattern.Diagnose env =
            let error code args =
                if env.Capability.Implies (pattern :> IPattern).Capability then
                    None
                else
                    QsCompilerDiagnostic.Error(code, args) (pattern.Range.ValueOr Range.Zero) |> Some

            let unsupported =
                if env.Capability = BasicMeasurementFeedback then
                    ErrorCode.ResultComparisonNotInOperationIf
                else
                    ErrorCode.UnsupportedResultComparison

            match pattern.Kind with
            | ConditionalEqualityInOperation
            | UnrestrictedEquality -> error unsupported [ env.Architecture ]
            | ReturnInDependentBlock ->
                if env.Capability = BasicMeasurementFeedback then
                    error ErrorCode.ReturnInResultConditionedBlock [ env.Architecture ]
                else
                    None
            | SetInDependentBlock name ->
                if env.Capability = BasicMeasurementFeedback then
                    error ErrorCode.SetInResultConditionedBlock [ name; env.Architecture ]
                else
                    None

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

let analyze callableKind (action: SyntaxTreeTransformation -> _) =
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
                base.OnStatement statement |> ignore

                match statement.Statement with
                | QsReturnStatement _ when dependsOnResult ->
                    let range = (transformation.Offset, statement.Location) ||> QsNullable.Map2(fun o l -> o + l.Range)
                    patterns.Add { Kind = ReturnInDependentBlock; Range = range }

                | QsValueUpdate update ->
                    update.Lhs.ExtractAll (fun e ->
                        match e.Expression with
                        | Identifier (LocalVariable name, _) when Set.contains name context.FrozenVars ->
                            let range = QsNullable.Map2(+) transformation.Offset e.Range
                            [ { Kind = SetInDependentBlock name; Range = range } ]
                        | _ -> [])
                    |> patterns.AddRange

                | _ -> ()

                statement
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

                    if callableKind = Operation && context.InCondition then
                        dependsOnResult <- true
                        patterns.Add { Kind = ConditionalEqualityInOperation; Range = range }
                    else
                        patterns.Add { Kind = UnrestrictedEquality; Range = range }

                base.OnTypedExpression expression
        }

    action transformation
    Seq.map (fun p -> p :> IPattern) patterns
