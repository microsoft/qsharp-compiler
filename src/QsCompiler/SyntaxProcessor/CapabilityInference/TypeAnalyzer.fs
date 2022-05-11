// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.TypeAnalyzer

open System
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

type Usage =
    | Mutable
    | Return
    | Conditional
    | Literal
    | Expression

type Context = { IsEntryPoint: bool; StringLiteralsOk: bool }

let rec requiredCapability context usage (ty: ResolvedType) =
    match usage with
    | Conditional
    | Mutable _ ->
        match ty.Resolution with
        | Bool
        | Int -> ClassicalCapability.integral
        | _ -> ClassicalCapability.full
    | Return when context.IsEntryPoint ->
        // TODO: I think there's another place where the return type of the entry point is checked. That should be
        // combined with this.
        match ty.Resolution with
        | Result -> ClassicalCapability.empty
        | Bool
        | Int -> ClassicalCapability.integral
        | ArrayType t -> requiredCapability context usage t
        | TupleType ts when not ts.IsEmpty -> Seq.map (requiredCapability context usage) ts |> Seq.max
        | _ -> ClassicalCapability.full
    | Return -> ClassicalCapability.empty
    | Literal ->
        match ty.Resolution with
        | BigInt -> ClassicalCapability.full
        | String -> if context.StringLiteralsOk then ClassicalCapability.empty else ClassicalCapability.full
        | _ -> ClassicalCapability.empty
    | Expression ->
        match ty.Resolution with
        | BigInt
        | Range
        | String -> ClassicalCapability.full
        | _ -> ClassicalCapability.empty

let createPattern range classical =
    let capability = RuntimeCapability.withClassical classical RuntimeCapability.bottom

    if capability = RuntimeCapability.bottom then
        None
    else
        let diagnose (target: Target) =
            let range = QsNullable.defaultValue Range.Zero range

            if RuntimeCapability.subsumes target.Capability capability then
                None
            else
                QsCompilerDiagnostic.Error(ErrorCode.UnsupportedClassicalCapability, [ target.Architecture ]) range
                |> Some

        Some
            {
                Capability = capability
                Diagnose = diagnose
                Properties = ()
            }

let analyzer (action: SyntaxTreeTransformation -> _) : _ seq =
    let transformation = LocatingTransformation TransformationOptions.NoRebuild
    let patterns = ResizeArray()
    let mutable context = { IsEntryPoint = false; StringLiteralsOk = false }

    let local context' =
        let oldContext = context
        context <- context'

        { new IDisposable with
            member _.Dispose() = context <- oldContext
        }

    transformation.Namespaces <-
        { new NamespaceTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnCallableDeclaration callable =
                use _ = local { context with IsEntryPoint = Seq.exists BuiltIn.MarksEntryPoint callable.Attributes }
                base.OnCallableDeclaration callable
        }

    transformation.Statements <-
        { new StatementTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnStatement statement =
                match statement.Statement with
                | QsFailStatement _ ->
                    use _ = local { context with StringLiteralsOk = true }
                    base.OnStatement statement
                | QsReturnStatement value ->
                    let statement = base.OnStatement statement
                    let range = (transformation.Offset, statement.Location) ||> QsNullable.Map2(fun o l -> o + l.Range)

                    requiredCapability context Return value.ResolvedType
                    |> createPattern range
                    |> Option.iter patterns.Add

                    statement
                | QsVariableDeclaration binding when binding.Kind = MutableBinding ->
                    let statement = base.OnStatement statement

                    for var in statement.SymbolDeclarations.Variables do
                        let range = QsNullable<_>.Map (fun o -> o + var.Range) transformation.Offset
                        requiredCapability context Mutable var.Type |> createPattern range |> Option.iter patterns.Add

                    statement
                | _ -> base.OnStatement statement
        }

    transformation.Expressions <-
        { new ExpressionTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnTypedExpression expression =
                let usage =
                    match expression.Expression, expression.ResolvedType.Resolution with
                    | CONDITIONAL _, _ -> Conditional
                    | IntLiteral _, _
                    | BigIntLiteral _, _
                    | DoubleLiteral _, _
                    | BoolLiteral _, _
                    | StringLiteral _, _
                    | ResultLiteral _, _
                    | PauliLiteral _, _
                    | RangeLiteral _, _ -> Literal
                    | _ -> Expression

                let expression = base.OnTypedExpression expression
                let range = QsNullable.Map2(+) transformation.Offset expression.Range

                requiredCapability context usage expression.ResolvedType
                |> createPattern range
                |> Option.iter patterns.Add

                expression
        }

    transformation.ExpressionKinds <-
        { new ExpressionKindTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnCallLikeExpression(callable, args) =
                match callable.Expression with
                | Identifier (GlobalCallable name, _) when name = BuiltIn.Message.FullName ->
                    let callable = transformation.Expressions.OnTypedExpression callable
                    use _ = local { context with StringLiteralsOk = true }
                    CallLikeExpression(callable, transformation.Expressions.OnTypedExpression args)
                | _ -> ``base``.OnCallLikeExpression(callable, args)
        }

    action transformation
    patterns
