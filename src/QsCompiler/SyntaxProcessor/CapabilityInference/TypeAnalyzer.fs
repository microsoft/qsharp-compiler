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

type Context = { IsEntryPoint: bool; StringLiteralsOk: bool }

let requiredCapability context usage (ty: ResolvedType) =
    let shallowCapability =
        match usage with
        | Conditional
        | Mutable _ ->
            function
            | Bool
            | Int -> ClassicalCapability.integral
            | _ -> ClassicalCapability.full
        | Return when context.IsEntryPoint ->
            // TODO: I think there's another place where the return type of the entry point is checked. That should be
            // combined with this.
            function
            | UnitType
            | Result
            | TupleType _
            | ArrayType _ -> ClassicalCapability.empty
            | Bool
            | Int -> ClassicalCapability.integral
            | _ -> ClassicalCapability.full
        | Return -> fun _ -> ClassicalCapability.empty
        | Literal ->
            function
            | BigInt -> ClassicalCapability.full
            | String when not context.StringLiteralsOk -> ClassicalCapability.full
            | _ -> ClassicalCapability.empty

    ty.ExtractAll(fun t -> shallowCapability t.Resolution |> Seq.singleton)
    |> Seq.fold max ClassicalCapability.empty

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
                let callable = base.OnCallableDeclaration callable

                let returnType = callable.Signature.ReturnType
                let returnRange = TypeRange.tryRange returnType.Range
                let range = (callable.Location, returnRange) ||> QsNullable.Map2(fun l r -> l.Offset + r)
                requiredCapability context Return returnType |> createPattern range |> Option.iter patterns.Add

                callable
        }

    transformation.Statements <-
        { new StatementTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnStatement statement =
                match statement.Statement with
                | QsFailStatement _ ->
                    use _ = local { context with StringLiteralsOk = true }
                    base.OnStatement statement
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
                    match expression.Expression with
                    | CONDITIONAL _ -> Some Conditional
                    | IntLiteral _
                    | BigIntLiteral _
                    | DoubleLiteral _
                    | BoolLiteral _
                    | StringLiteral _
                    | ResultLiteral _
                    | PauliLiteral _
                    | RangeLiteral _ -> Some Literal
                    | _ -> None

                let expression = base.OnTypedExpression expression
                let range = QsNullable.Map2(+) transformation.Offset expression.Range

                usage
                |> Option.bind (fun u -> requiredCapability context u expression.ResolvedType |> createPattern range)
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
