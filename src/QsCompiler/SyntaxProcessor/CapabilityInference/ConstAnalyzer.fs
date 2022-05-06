// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.ConstAnalyzer

open System
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

type Context = { IsEntryPoint: bool; IsConstRequired: bool }

let createPattern range =
    let range = QsNullable.defaultValue Range.Zero range
    let capability = RuntimeCapability.withClassical ClassicalCapability.full RuntimeCapability.bottom

    let diagnose target =
        QsCompilerDiagnostic.Error(ErrorCode.UnsupportedClassicalCapability, [ target.Architecture ]) range
        |> Some
        |> Option.filter (fun _ -> target.Capability < capability)

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
    // TODO: Set IsEntryPoint.
    let mutable context = { IsEntryPoint = false; IsConstRequired = false }

    let local context' =
        let oldContext = context
        context <- context'

        { new IDisposable with
            member _.Dispose() = context <- oldContext
        }

    transformation.Namespaces <-
        { new NamespaceTransformation(transformation, TransformationOptions.NoRebuild) with
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
            override _.OnQubitInitializer init =
                match init.Resolution with
                | QubitRegisterAllocation size ->
                    use _ = local { context with IsConstRequired = true }
                    let size = transformation.Expressions.OnTypedExpression size
                    QubitRegisterAllocation size |> ResolvedInitializer.create init.Type.Range
                | _ -> base.OnQubitInitializer init

            override _.OnReturnStatement value =
                use _ = local { context with IsConstRequired = not context.IsEntryPoint }
                let value = transformation.Expressions.OnTypedExpression value
                QsReturnStatement value

            override _.OnVariableDeclaration binding =
                if binding.Kind = ImmutableBinding then
                    let lhs = transformation.StatementKinds.OnSymbolTuple binding.Lhs
                    use _ = local { context with IsConstRequired = true }
                    let rhs = transformation.Expressions.OnTypedExpression binding.Rhs

                    QsVariableDeclaration
                        {
                            Kind = binding.Kind
                            Lhs = lhs
                            Rhs = rhs
                        }
                else
                    QsVariableDeclaration binding
        }

    transformation.Expressions <-
        { new ExpressionTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnTypedExpression expression =
                match expression.Expression with
                | CONDITIONAL _ -> if context.IsConstRequired then createPattern expression.Range |> patterns.Add
                | Identifier (LocalVariable name, _) ->
                    let isMutable = Map.tryFind name variables |> Option.exists id

                    if context.IsConstRequired && isMutable then createPattern expression.Range |> patterns.Add
                | _ -> ()

                base.OnTypedExpression expression
        }

    transformation.ExpressionKinds <-
        { new ExpressionKindTransformation(transformation, TransformationOptions.NoRebuild) with
            override _.OnCallLikeExpression(callable, arg) =
                let callable = transformation.Expressions.OnTypedExpression callable
                use _ = local { context with IsConstRequired = true }
                let arg = transformation.Expressions.OnTypedExpression arg
                CallLikeExpression(callable, arg)

            override _.OnNewArray(ty, size) =
                let ty = transformation.Types.OnType ty
                use _ = local { context with IsConstRequired = true }
                let size = transformation.Expressions.OnTypedExpression size
                NewArray(ty, size)

            override _.OnSizedArray(value, size) =
                let value = transformation.Expressions.OnTypedExpression value
                use _ = local { context with IsConstRequired = true }
                let size = transformation.Expressions.OnTypedExpression size
                SizedArray(value, size)
        }

    action transformation
    patterns
