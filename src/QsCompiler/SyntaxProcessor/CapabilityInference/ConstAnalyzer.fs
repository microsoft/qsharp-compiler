// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.ConstAnalyzer

open System
open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

type Context = { IsEntryPoint: bool; ConstOnly: bool }

let createPattern range =
    let capability = RuntimeCapability.withClassical ClassicalCapability.full RuntimeCapability.bottom

    let diagnose (target: Target) =
        let range = QsNullable.defaultValue Range.Zero range

        if RuntimeCapability.subsumes target.Capability capability then
            None
        else
            QsCompilerDiagnostic.Error(ErrorCode.UnsupportedClassicalCapability, [ target.Architecture ]) range
            |> Some

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
    let patterns = ResizeArray()
    let mutable variables = Map.empty
    let mutable context = { IsEntryPoint = false; ConstOnly = false }

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

            override _.OnProvidedImplementation(arg, body) =
                variables <-
                    flattenTuple arg
                    |> Seq.choose (fun v -> localName v.VariableName)
                    |> Seq.map (fun n -> n, context.IsEntryPoint)
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
                    use _ = local { context with ConstOnly = true }
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
                    use _ = local { context with ConstOnly = true }

                    transformation.Expressions.OnTypedExpression size
                    |> QubitRegisterAllocation
                    |> ResolvedInitializer.create init.Type.Range
                | _ -> base.OnQubitInitializer init

            override _.OnReturnStatement value =
                use _ = local { context with ConstOnly = not context.IsEntryPoint }
                transformation.Expressions.OnTypedExpression value |> QsReturnStatement

            override _.OnVariableDeclaration binding =
                if binding.Kind = ImmutableBinding then
                    let lhs = transformation.StatementKinds.OnSymbolTuple binding.Lhs
                    use _ = local { context with ConstOnly = true }

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
                let expression = base.OnTypedExpression expression
                let range = QsNullable.Map2(+) transformation.Offset expression.Range

                match expression.Expression with
                | CONDITIONAL _ -> if context.ConstOnly then createPattern range |> patterns.Add
                | Identifier (LocalVariable name, _) ->
                    let isMutable = Map.tryFind name variables |> Option.exists id
                    if context.ConstOnly && isMutable then createPattern range |> patterns.Add
                | _ -> ()

                expression
        }

    transformation.ExpressionKinds <-
        { new ExpressionKindTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnArrayItemAccess(array, index) =
                let array = transformation.Expressions.OnTypedExpression array
                use _ = local { context with ConstOnly = true }
                ArrayItem(array, transformation.Expressions.OnTypedExpression index)

            override _.OnCallLikeExpression(callable, arg) =
                let callable = transformation.Expressions.OnTypedExpression callable
                use _ = local { context with ConstOnly = true }
                CallLikeExpression(callable, transformation.Expressions.OnTypedExpression arg)

            override _.OnCopyAndUpdateExpression(container, index, value) =
                let container = transformation.Expressions.OnTypedExpression container

                let index =
                    // TODO: This duplicates the check for whether this is a UDT or array update.
                    match container.ResolvedType.Resolution, index.Expression with
                    | UserDefinedType udt, Identifier (LocalVariable name, Null) when
                        Map.containsKey name variables |> not
                        ->
                        let range = transformation.OnExpressionRange index.Range
                        let name = transformation.OnItemName(udt, name) |> LocalVariable
                        let ty = transformation.Types.OnType index.ResolvedType
                        let info = transformation.Expressions.OnExpressionInformation index.InferredInformation
                        TypedExpression.New(Identifier(name, Null), ImmutableDictionary.Empty, ty, info, range)
                    | _ ->
                        use _ = local { context with ConstOnly = true }
                        transformation.Expressions.OnTypedExpression index

                CopyAndUpdate(container, index, transformation.Expressions.OnTypedExpression value)

            override _.OnSizedArray(value, size) =
                let value = transformation.Expressions.OnTypedExpression value
                use _ = local { context with ConstOnly = true }
                SizedArray(value, transformation.Expressions.OnTypedExpression size)
        }

    action transformation
    patterns
