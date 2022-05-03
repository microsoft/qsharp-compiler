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
        match ty.Resolution with
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
    let range = QsNullable.defaultValue Range.Zero range
    let capability = RuntimeCapability.withClassical classical RuntimeCapability.bottom

    let diagnose target =
        QsCompilerDiagnostic.Error(ErrorCode.UnsupportedClassicalCapability, [ target.Architecture ]) range
        |> Some
        |> Option.filter (fun _ -> target.Capability < capability)

    Some
        {
            Capability = capability
            Diagnose = diagnose
            Properties = ()
        }
    |> Option.filter (fun _ -> capability > RuntimeCapability.bottom)

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

    transformation.Statements <-
        { new StatementTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnStatement statement =
                let range = statement.Location |> QsNullable<_>.Map (fun l -> l.Offset + l.Range)

                match statement.Statement with
                | QsFailStatement _ ->
                    use _ = local { context with StringLiteralsOk = true }
                    base.OnStatement statement
                | QsReturnStatement value ->
                    requiredCapability context Return value.ResolvedType
                    |> createPattern range
                    |> Option.iter patterns.Add

                    base.OnStatement statement
                | QsVariableDeclaration binding when binding.Kind = MutableBinding ->
                    for var in statement.SymbolDeclarations.Variables do
                        requiredCapability context Mutable var.Type |> createPattern range |> Option.iter patterns.Add

                    base.OnStatement statement
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

                let range = QsNullable.Map2(+) transformation.Offset expression.Range

                requiredCapability context usage expression.ResolvedType
                |> createPattern range
                |> Option.iter patterns.Add

                base.OnTypedExpression expression
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
