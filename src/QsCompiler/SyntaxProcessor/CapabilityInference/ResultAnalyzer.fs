// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.ResultAnalyzer

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.ContextRef
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

type ResultDependency =
    /// An equality expression inside the condition of an if statement, where the operands are results
    | ConditionalEqualityInOperation
    /// An equality expression outside the condition of an if statement, where the operands are results.
    | UnrestrictedEquality
    /// A return statement in the branch of an if statement whose condition depends on a result.
    | ReturnInDependentBranch
    /// A set statement in the branch of an if statement whose condition depends on a result, but which is reassigning a
    /// variable declared outside the block.
    | SetInDependentBranch of name: string

type Context =
    {
        CallableKind: QsCallableKind
        /// Variables declared outside of a result conditional branch are frozen inside the branch. They may not be set
        /// when the result opacity is controlled.
        FrozenVars: string Set
        InCondition: bool
    }

let createPattern kind range =
    let opacity =
        match kind with
        | ConditionalEqualityInOperation -> ResultOpacity.controlled
        | UnrestrictedEquality
        | ReturnInDependentBranch
        | SetInDependentBranch _ -> ResultOpacity.transparent

    let capability = TargetCapability.withResultOpacity opacity TargetCapability.bottom

    let diagnose (target: Target) =
        let range = QsNullable.defaultValue Range.Zero range

        match kind with
        | _ when TargetCapability.subsumes target.Capability capability -> None
        | ConditionalEqualityInOperation
        | UnrestrictedEquality ->
            let code =
                if target.Capability.ResultOpacity = ResultOpacity.opaque then
                    ErrorCode.UnsupportedResultComparison
                else
                    ErrorCode.ResultComparisonNotInOperationIf

            Some(code, [ target.Name ])
        | ReturnInDependentBranch ->
            if target.Capability.ResultOpacity = ResultOpacity.opaque then
                None
            else
                Some(ErrorCode.ReturnInResultConditionedBlock, [ target.Name ])
        | SetInDependentBranch name ->
            if target.Capability.ResultOpacity = ResultOpacity.opaque then
                None
            else
                Some(ErrorCode.SetInResultConditionedBlock, [ name; target.Name ])
        |> Option.map (fun (code, args) -> QsCompilerDiagnostic.Error (code, args) range)

    {
        Capability = capability
        Diagnose = diagnose
        Properties = ()
    }

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

let freezeContext dependsOnResult (block: QsPositionedBlock) (context: _ ref) =
    let knownVars = Seq.map (fun v -> v.VariableName) block.Body.KnownSymbols.Variables |> Set.ofSeq
    let frozenVars = if dependsOnResult then knownVars else context.Value.FrozenVars
    local { context.Value with FrozenVars = frozenVars } context

// TODO: Remove the callableKind parameter as part of https://github.com/microsoft/qsharp-compiler/issues/1448.
let analyzer callableKind (action: SyntaxTreeTransformation -> _) : _ seq =
    let transformation = LocatingTransformation TransformationOptions.NoRebuild
    let patterns = ResizeArray()

    let context =
        ref
            {
                CallableKind = Option.defaultValue Function callableKind
                FrozenVars = Set.empty
                InCondition = false
            }

    transformation.Namespaces <-
        { new NamespaceTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnCallableDeclaration callable =
                use _ = local { context.Value with CallableKind = callable.Kind } context
                base.OnCallableDeclaration callable
        }

    transformation.Statements <-
        { new StatementTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnStatement statement =
                transformation.OnRelativeLocation statement.Location |> ignore

                match statement.Statement with
                | QsReturnStatement _ when Set.isEmpty context.Value.FrozenVars |> not ->
                    let range = (transformation.Offset, statement.Location) ||> QsNullable.Map2(fun o l -> o + l.Range)
                    createPattern ReturnInDependentBranch range |> patterns.Add
                | QsValueUpdate update ->
                    update.Lhs.ExtractAll (fun e ->
                        match e.Expression with
                        | Identifier (LocalVariable name, _) when Set.contains name context.Value.FrozenVars ->
                            let range = QsNullable.Map2 (+) transformation.Offset e.Range
                            [ createPattern (SetInDependentBranch name) range ]
                        | _ -> [])
                    |> patterns.AddRange
                | _ -> ()

                base.OnStatement statement
        }

    transformation.StatementKinds <-
        { new StatementKindTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnConditionalStatement conditional =
                let mutable dependsOnResult = false

                for condition, block in conditional.ConditionalBlocks do
                    dependsOnResult <- dependsOnResult || condition.Exists isResultEquality
                    use _ = freezeContext dependsOnResult block context
                    transformation.StatementKinds.OnPositionedBlock(Value condition, block) |> ignore

                for block in conditional.Default do
                    use _ = freezeContext dependsOnResult block context
                    transformation.StatementKinds.OnPositionedBlock(Null, block) |> ignore

                QsConditionalStatement conditional

            override _.OnPositionedBlock(condition, block) =
                transformation.OnRelativeLocation block.Location |> ignore

                for c in condition do
                    use _ = local { context.Value with InCondition = true } context
                    transformation.Expressions.OnTypedExpression c |> ignore

                transformation.Statements.OnScope block.Body |> ignore
                condition, block
        }

    transformation.Expressions <-
        { new ExpressionTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnTypedExpression expression =
                if isResultEquality expression then
                    let dependency =
                        if context.Value.CallableKind = Operation && context.Value.InCondition then
                            ConditionalEqualityInOperation
                        else
                            UnrestrictedEquality

                    let range = QsNullable.Map2 (+) transformation.Offset expression.Range
                    createPattern dependency range |> patterns.Add

                base.OnTypedExpression expression
        }

    action transformation
    patterns
