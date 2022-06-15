// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.ConstAnalyzer

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.ContextRef
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

type Context = { IsEntryPoint: bool; ConstOnly: bool }

let createPattern range =
    let capability = TargetCapability.withClassicalCompute ClassicalCompute.full TargetCapability.bottom

    let diagnose (target: Target) =
        let range = QsNullable.defaultValue Range.Zero range

        if TargetCapability.subsumes target.Capability capability then
            None
        else
            // TODO: The capability description string should be defined with the rest of the diagnostic message
            // instead of here, but this is easier after https://github.com/microsoft/qsharp-compiler/issues/1025.
            let description = "conditional expression or mutable variable in a constant context"
            let args = [ target.Name; description ]
            QsCompilerDiagnostic.Error (ErrorCode.UnsupportedClassicalCapability, args) range |> Some

    {
        Capability = capability
        Diagnose = diagnose
        Properties = ()
    }

let rec flattenTuple =
    function
    | QsTupleItem x -> Seq.singleton x
    | QsTuple xs -> Seq.collect flattenTuple xs

let localName symbol =
    match symbol with
    | ValidName name -> Some name
    | InvalidName -> None

let localVariables locals =
    Seq.map (fun v -> v.VariableName, v.InferredInformation.IsMutable) locals |> Map.ofSeq

let union map1 map2 =
    Map.fold (fun acc key value -> Map.add key value acc) map1 map2

let analyzer (action: SyntaxTreeTransformation -> _) : _ seq =
    let transformation = LocatingTransformation TransformationOptions.NoRebuild
    let context = ref { IsEntryPoint = false; ConstOnly = false }
    let mutable variables = Map.empty
    let patterns = ResizeArray()

    transformation.Namespaces <-
        { new NamespaceTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnCallableDeclaration callable =
                let isEntryPoint = Seq.exists BuiltIn.MarksEntryPoint callable.Attributes
                use _ = local { context.Value with IsEntryPoint = isEntryPoint } context
                base.OnCallableDeclaration callable

            override _.OnProvidedImplementation(arg, body) =
                variables <-
                    flattenTuple arg
                    |> Seq.choose (fun v -> localName v.VariableName)
                    |> Seq.map (fun n -> n, context.Value.IsEntryPoint)
                    |> Map.ofSeq

                ``base``.OnProvidedImplementation(arg, body)
        }

    transformation.Statements <-
        { new StatementTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnScope scope =
                let oldVariables = variables
                variables <- union (localVariables scope.KnownSymbols.Variables) variables
                let scope = base.OnScope scope
                variables <- oldVariables
                scope

            override _.OnStatement statement =
                let statement = base.OnStatement statement
                variables <- union (localVariables statement.SymbolDeclarations.Variables) variables
                statement
        }

    transformation.StatementKinds <-
        { new StatementKindTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnForStatement forS =
                let item = fst forS.LoopItem |> transformation.StatementKinds.OnSymbolTuple
                let itemType = snd forS.LoopItem |> transformation.Types.OnType

                let values =
                    use _ = local { context.Value with ConstOnly = true } context
                    transformation.Expressions.OnTypedExpression forS.IterationValues

                let body = transformation.Statements.OnScope forS.Body

                QsForStatement
                    {
                        LoopItem = item, itemType
                        IterationValues = values
                        Body = body
                    }

            override _.OnQubitInitializer init =
                match init.Resolution with
                | QubitRegisterAllocation size ->
                    use _ = local { context.Value with ConstOnly = true } context

                    transformation.Expressions.OnTypedExpression size
                    |> QubitRegisterAllocation
                    |> ResolvedInitializer.create init.Type.Range
                | _ -> base.OnQubitInitializer init

            override _.OnReturnStatement value =
                use _ = local { context.Value with ConstOnly = not context.Value.IsEntryPoint } context
                transformation.Expressions.OnTypedExpression value |> QsReturnStatement

            override _.OnVariableDeclaration binding =
                if binding.Kind = ImmutableBinding then
                    let lhs = transformation.StatementKinds.OnSymbolTuple binding.Lhs
                    use _ = local { context.Value with ConstOnly = true } context

                    QsVariableDeclaration
                        {
                            Kind = binding.Kind
                            Lhs = lhs
                            Rhs = transformation.Expressions.OnTypedExpression binding.Rhs
                        }
                else
                    base.OnVariableDeclaration binding
        }

    transformation.Expressions <-
        { new ExpressionTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnTypedExpression expression =
                let range = QsNullable.Map2 (+) transformation.Offset expression.Range

                match expression.Expression with
                | CONDITIONAL _ -> if context.Value.ConstOnly then createPattern range |> patterns.Add
                | Identifier (LocalVariable name, _) ->
                    let isMutable = Map.tryFind name variables |> Option.exists id
                    if context.Value.ConstOnly && isMutable then createPattern range |> patterns.Add
                | _ -> ()

                base.OnTypedExpression expression
        }

    transformation.ExpressionKinds <-
        { new ExpressionKindTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnArrayItemAccess(array, index) =
                let array = transformation.Expressions.OnTypedExpression array
                use _ = local { context.Value with ConstOnly = true } context
                ArrayItem(array, transformation.Expressions.OnTypedExpression index)

            override _.OnCallLikeExpression(callable, arg) =
                let callable = transformation.Expressions.OnTypedExpression callable
                use _ = local { context.Value with ConstOnly = true } context
                CallLikeExpression(callable, transformation.Expressions.OnTypedExpression arg)

            override _.OnCopyAndUpdateExpression(container, index, value) =
                let container = transformation.Expressions.OnTypedExpression container

                let index =
                    use _ = local { context.Value with ConstOnly = true } context
                    transformation.Expressions.OnTypedExpression index

                CopyAndUpdate(container, index, transformation.Expressions.OnTypedExpression value)

            override _.OnSizedArray(value, size) =
                let value = transformation.Expressions.OnTypedExpression value
                use _ = local { context.Value with ConstOnly = true } context
                SizedArray(value, transformation.Expressions.OnTypedExpression size)
        }

    action transformation
    patterns
