// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.TypeAnalyzer

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.ContextRef
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput

type Construct =
    | Literal
    | Mutable
    | Param
    | Return
    | Conditional

type Scenario =
    | UseBigInt
    | StringNotArgumentToMessage
    | ConditionalOrMutable of ResolvedType
    | EntryPointParam of ResolvedType
    | EntryPointReturn of ResolvedType

type Context = { IsEntryPoint: bool; StringLiteralsOk: bool }

let shallowFindScenario context construct (ty: ResolvedType) =
    match construct with
    | Literal ->
        match ty.Resolution with
        | BigInt -> Some UseBigInt
        | String when not context.StringLiteralsOk -> Some StringNotArgumentToMessage
        | _ -> None
    | Conditional
    | Mutable -> ConditionalOrMutable ty |> Some
    | Param when context.IsEntryPoint -> EntryPointParam ty |> Some
    | Param -> None
    | Return when context.IsEntryPoint -> EntryPointReturn ty |> Some
    | Return -> None

let findScenarios context construct (ty: ResolvedType) =
    ty.ExtractAll(shallowFindScenario context construct >> Option.toList >> Seq.ofList)

let scenarioCapability scenario =
    let classical =
        match scenario with
        | UseBigInt -> ClassicalCapability.full
        | StringNotArgumentToMessage -> ClassicalCapability.full
        | ConditionalOrMutable ty ->
            match ty.Resolution with
            | Bool
            | Int -> ClassicalCapability.integral
            | _ -> ClassicalCapability.full
        | EntryPointParam ty ->
            match ty.Resolution with
            | TupleType _ -> ClassicalCapability.empty
            | Bool
            | Int -> ClassicalCapability.integral
            | _ -> ClassicalCapability.full
        | EntryPointReturn ty ->
            match ty.Resolution with
            | UnitType
            | Result
            | TupleType _
            | ArrayType _ -> ClassicalCapability.empty
            | Bool
            | Int -> ClassicalCapability.integral
            | _ -> ClassicalCapability.full

    RuntimeCapability.withClassical classical RuntimeCapability.bottom

let shallowTypeName (ty: ResolvedType) =
    match ty.Resolution with
    | ArrayType _ -> "array"
    | TupleType _ -> "tuple"
    | QsTypeKind.Operation _ -> "operation"
    | QsTypeKind.Function _ -> "function"
    | _ -> SyntaxTreeToQsharp.Default.ToCode ty

let describeScenario =
    function
    | UseBigInt -> "BigInt"
    | StringNotArgumentToMessage -> "string that is not an argument to Message"
    | ConditionalOrMutable ty -> "conditional expression or mutable variable of type " + shallowTypeName ty
    | EntryPointParam ty -> "entry point parameter of type " + shallowTypeName ty
    | EntryPointReturn ty -> "entry point return value of type " + shallowTypeName ty

let createPattern context construct range (ty: ResolvedType) =
    let scenarios = findScenarios context construct ty |> Seq.toList

    let capability =
        Seq.map scenarioCapability scenarios |> Seq.fold RuntimeCapability.merge RuntimeCapability.bottom

    if capability = RuntimeCapability.bottom then
        None
    else
        let diagnose (target: Target) =
            if RuntimeCapability.subsumes target.Capability capability then
                None
            else
                let unsupported =
                    Seq.filter (scenarioCapability >> RuntimeCapability.subsumes target.Capability >> not) scenarios

                let description = Seq.map describeScenario unsupported |> String.concat ", "
                let args = [ target.Name; description ]
                let range = QsNullable.defaultValue Range.Zero range
                QsCompilerDiagnostic.Error(ErrorCode.UnsupportedClassicalCapability, args) range |> Some

        Some
            {
                Capability = capability
                Diagnose = diagnose
                Properties = ()
            }

let rec flattenTuple =
    function
    | QsTupleItem x -> Seq.singleton x
    | QsTuple xs -> Seq.collect flattenTuple xs

let paramPatterns context callable =
    flattenTuple callable.ArgumentTuple
    |> Seq.choose (fun param ->
        let relativeRange = TypeRange.tryRange param.Type.Range
        let range = (callable.Location, relativeRange) ||> QsNullable.Map2(fun l r -> l.Offset + r)
        createPattern context Param range param.Type)

let returnPattern context callable =
    let ty = callable.Signature.ReturnType
    let relativeRange = TypeRange.tryRange ty.Range
    let range = (callable.Location, relativeRange) ||> QsNullable.Map2(fun l r -> l.Offset + r)
    createPattern context Return range ty

let analyzer (action: SyntaxTreeTransformation -> _) : _ seq =
    let transformation = LocatingTransformation TransformationOptions.NoRebuild
    let context = ref { IsEntryPoint = false; StringLiteralsOk = false }
    let patterns = ResizeArray()

    transformation.Namespaces <-
        { new NamespaceTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnCallableDeclaration callable =
                let isEntryPoint = Seq.exists BuiltIn.MarksEntryPoint callable.Attributes
                use _ = local { context.Value with IsEntryPoint = isEntryPoint } context

                let callable = base.OnCallableDeclaration callable
                paramPatterns context.Value callable |> patterns.AddRange
                returnPattern context.Value callable |> Option.iter patterns.Add
                callable
        }

    transformation.Statements <-
        { new StatementTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnStatement statement =
                match statement.Statement with
                | QsFailStatement _ ->
                    use _ = local { context.Value with StringLiteralsOk = true } context
                    base.OnStatement statement
                | QsVariableDeclaration binding when binding.Kind = MutableBinding ->
                    let statement = base.OnStatement statement

                    for var in statement.SymbolDeclarations.Variables do
                        let range = QsNullable<_>.Map (fun o -> o + var.Range) transformation.Offset
                        createPattern context.Value Mutable range var.Type |> Option.iter patterns.Add

                    statement
                | _ -> base.OnStatement statement
        }

    transformation.Expressions <-
        { new ExpressionTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnTypedExpression expression =
                let construct =
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

                Option.bind (fun c -> createPattern context.Value c range expression.ResolvedType) construct
                |> Option.iter patterns.Add

                expression
        }

    transformation.ExpressionKinds <-
        { new ExpressionKindTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnCallLikeExpression(callable, args) =
                match callable.Expression with
                | Identifier (GlobalCallable name, _) when name = BuiltIn.Message.FullName ->
                    let callable = transformation.Expressions.OnTypedExpression callable
                    use _ = local { context.Value with StringLiteralsOk = true } context
                    CallLikeExpression(callable, transformation.Expressions.OnTypedExpression args)
                | _ -> ``base``.OnCallLikeExpression(callable, args)
        }

    action transformation
    patterns
