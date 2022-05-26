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
        InCondition: bool
        FrozenVars: string Set
    }

let createPattern kind range =
    let opacity =
        match kind with
        | ConditionalEqualityInOperation -> ResultOpacity.controlled
        | UnrestrictedEquality
        | ReturnInDependentBranch
        | SetInDependentBranch _ -> ResultOpacity.transparent

    let capability = RuntimeCapability.withResultOpacity opacity RuntimeCapability.bottom

    let diagnose (target: Target) =
        let range = QsNullable.defaultValue Range.Zero range

        match kind with
        | _ when RuntimeCapability.subsumes target.Capability capability -> None
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
        |> Option.map (fun (code, args) -> QsCompilerDiagnostic.Error(code, args) range)

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

// TODO: Remove the callableKind parameter as part of https://github.com/microsoft/qsharp-compiler/issues/1448.
let analyzer callableKind (action: SyntaxTreeTransformation -> _) : _ seq =
    let transformation = LocatingTransformation TransformationOptions.NoRebuild
    let mutable dependsOnResult = false
    let patterns = ResizeArray()

    let context =
        ref
            {
                CallableKind = Option.defaultValue Function callableKind
                InCondition = false
                FrozenVars = Set.empty
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
                | QsReturnStatement _ when dependsOnResult ->
                    let range = (transformation.Offset, statement.Location) ||> QsNullable.Map2(fun o l -> o + l.Range)
                    createPattern ReturnInDependentBranch range |> patterns.Add

                | QsValueUpdate update ->
                    update.Lhs.ExtractAll (fun e ->
                        match e.Expression with
                        | Identifier (LocalVariable name, _) when Set.contains name context.Value.FrozenVars ->
                            let range = QsNullable.Map2(+) transformation.Offset e.Range
                            [ createPattern (SetInDependentBranch name) range ]
                        | _ -> [])
                    |> patterns.AddRange

                | _ -> ()

                base.OnStatement statement
        }

    transformation.StatementKinds <-
        { new StatementKindTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnConditionalStatement statement =
                let oldDependsOnResult = dependsOnResult
                let kind = base.OnConditionalStatement statement
                dependsOnResult <- oldDependsOnResult
                kind

            override _.OnPositionedBlock(condition, block) =
                let location = transformation.OnRelativeLocation block.Location

                let condition =
                    QsNullable<_>.Map
                        (fun c ->
                            use _ = local { context.Value with InCondition = true } context
                            transformation.Expressions.OnTypedExpression c) condition

                let knownVars = Seq.map (fun v -> v.VariableName) block.Body.KnownSymbols.Variables |> Set.ofSeq
                let frozenVars = if dependsOnResult then knownVars else context.Value.FrozenVars
                use _ = local { context.Value with FrozenVars = frozenVars } context
                let body = transformation.Statements.OnScope block.Body

                condition,
                {
                    Body = body
                    Location = location
                    Comments = block.Comments
                }
        }

    transformation.Expressions <-
        { new ExpressionTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnTypedExpression expression =
                if isResultEquality expression then
                    let range = QsNullable.Map2(+) transformation.Offset expression.Range

                    if context.Value.CallableKind = Operation && context.Value.InCondition then
                        dependsOnResult <- true
                        createPattern ConditionalEqualityInOperation range |> patterns.Add
                    else
                        createPattern UnrestrictedEquality range |> patterns.Add

                base.OnTypedExpression expression
        }

    action transformation
    patterns
