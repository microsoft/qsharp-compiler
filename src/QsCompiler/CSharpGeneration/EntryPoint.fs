// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.CsharpGeneration.EntryPoint

open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open Microsoft.Quantum.QsCompiler.ReservedKeywords
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.RoslynWrapper
open System


/// An entry point parameter.
type private Parameter =
    { Name : string
      QSharpType : ResolvedType
      CSharpTypeName : string
      Description : string }

/// The name of the generated entry point class.
let entryPointClassName = "__QsEntryPoint__"

/// The namespace containing the non-generated parts of the entry point driver.
let private driverNamespace = "Microsoft.Quantum.EntryPointDriver"

/// A sequence of all of the named parameters in the argument tuple and their respective C# and Q# types.
let rec private parameters context doc = function
    | QsTupleItem variable ->
        match variable.VariableName with
        | ValidName name ->
            Seq.singleton { Name = name
                            QSharpType = variable.Type
                            CSharpTypeName = SimulationCode.roslynTypeName context variable.Type
                            Description = ParameterDescription doc name }
        | InvalidName -> Seq.empty
    | QsTuple items -> items |> Seq.map (parameters context doc) |> Seq.concat

/// An expression representing the name of an entry point option given its parameter name.
let private optionName (paramName : string) =
    let toKebabCaseIdent = ident "System.CommandLine.Parsing.StringExtensions.ToKebabCase"
    if paramName.Length = 1
    then literal ("-" + paramName)
    else literal "--" <+> invoke toKebabCaseIdent ``(`` [literal paramName] ``)``

/// A property containing a sequence of command-line options corresponding to each parameter given.
let private parameterOptionsProperty parameters =
    let optionTypeName = "System.CommandLine.Option"
    let optionsEnumerableTypeName = sprintf "System.Collections.Generic.IEnumerable<%s>" optionTypeName
    let option { Name = name; CSharpTypeName = typeName; Description = desc } =
        let createOption = ident (sprintf "%s.Options.CreateOption<%s>" driverNamespace typeName)
        let args = [optionName name; literal desc]
        invoke createOption ``(`` args ``)``
    let options = parameters |> Seq.map option |> Seq.toList
    ``property-arrow_get`` optionsEnumerableTypeName "Options" [``public``]
        get (``=>`` (``new array`` (Some optionTypeName) options))

/// A lambda that creates an instance of the default simulator if it is a custom simulator.
let private customSimulatorFactory name =
    let isCustomSimulator =
        not <| List.contains name [
            AssemblyConstants.QuantumSimulator
            AssemblyConstants.ToffoliSimulator
            AssemblyConstants.ResourcesEstimator
        ]
    let factory =
        if isCustomSimulator
        then ``new`` (``type`` name) ``(`` [] ``)``
        else upcast SyntaxFactory.ThrowExpression (``new`` (``type`` "InvalidOperationException") ``(`` [] ``)``)
    ``() =>`` [] factory :> ExpressionSyntax

/// A method that creates the argument tuple for the entry point, given the command-line parsing result.
let private createArgument context entryPoint =
    let inTypeName = SimulationCode.roslynTypeName context entryPoint.Signature.ArgumentType
    let parseResultName = "parseResult"
    let valueForArg (name, typeName) =
        ident parseResultName <.> (sprintf "ValueForOption<%s>" typeName |> ident, [optionName name])
    let argTuple =
        SimulationCode.mapArgumentTuple
            valueForArg
            context
            entryPoint.ArgumentTuple
            entryPoint.Signature.ArgumentType
    arrow_method inTypeName "CreateArgument" ``<<`` [] ``>>``
        ``(`` [param parseResultName ``of`` (``type`` "System.CommandLine.Parsing.ParseResult")] ``)``
        [``public``]
        (Some (``=>`` argTuple))

/// A tuple of the callable's name, argument type name, and return type name.
let private callableTypeNames context (callable : QsCallable) =
    let callableName =
        SimulationCode.userDefinedName None callable.FullName.Name
        |> sprintf "global::%s.%s" callable.FullName.Namespace
    let argTypeName = SimulationCode.roslynTypeName context callable.Signature.ArgumentType
    let returnTypeName = SimulationCode.roslynTypeName context callable.Signature.ReturnType
    callableName, argTypeName, returnTypeName

/// Generates the class name for an entry point class.
let private entryPointClassFullName (entryPoint : QsCallable) =
    { Namespace = entryPoint.FullName.Namespace; Name = entryPointClassName + entryPoint.FullName.Name }

/// Generates the Submit method for an entry point class.
let private submitMethod context entryPoint =
    let callableName, _, _ = callableTypeNames context entryPoint
    let parseResultParamName = "parseResult"
    let settingsParamName = "settings"
    let args =
        [
            ident callableName <|.|> ident "Info"
            ident "this" <.> (ident "CreateArgument", [ident parseResultParamName])
            ident settingsParamName :> ExpressionSyntax
        ]
    arrow_method "System.Threading.Tasks.Task<int>" "Submit" ``<<`` [] ``>>``
        ``(``
            [
                param parseResultParamName ``of`` (``type`` "System.CommandLine.Parsing.ParseResult")
                param settingsParamName ``of`` (``type`` (driverNamespace + ".AzureSettings"))
            ]
        ``)``
        [``public``]
        (Some (``=>`` (ident (driverNamespace + ".Azure") <.> (ident "Submit", args))))

/// Generates the Simulate method for an entry point class.
let private simulateMethod context entryPoint =
    let callableName, argTypeName, returnTypeName = callableTypeNames context entryPoint
    let simulationType = generic (driverNamespace + ".Simulation") ``<<`` [callableName; argTypeName; returnTypeName] ``>>``
    let parseResultParamName = "parseResult"
    let settingsParamName = "settings"
    let simulatorParamName = "simulator"
    let args =
        [
            ident "this" :> ExpressionSyntax
            ident "this" <.> (ident "CreateArgument", [ident parseResultParamName])
            ident settingsParamName :> ExpressionSyntax
            ident simulatorParamName :> ExpressionSyntax
        ]
    arrow_method "System.Threading.Tasks.Task<int>" "Simulate" ``<<`` [] ``>>``
        ``(``
            [
                param parseResultParamName ``of`` (``type`` "System.CommandLine.Parsing.ParseResult")
                param settingsParamName ``of`` (``type`` (driverNamespace + ".DriverSettings"))
                param simulatorParamName ``of`` (``type`` "string")
            ]
        ``)``
        [``public``]
        (Some (``=>`` (simulationType <.> (ident "Simulate", args))))

/// The class that adapts the entry point for use with the command-line parsing library and the driver.
let private entryPointClass context (entryPoint : QsCallable) =
    let property name typeName value = ``property-arrow_get`` typeName name [``public``] get (``=>`` value)
    let nameProperty =
        entryPoint.FullName.ToString()
        |> literal
        |> property "Name" "string"
    let summaryProperty =
        (PrintSummary entryPoint.Documentation false).Trim ()
        |> literal
        |> property "Summary" "string"
    let parameters = parameters context entryPoint.Documentation entryPoint.ArgumentTuple
    let members : MemberDeclarationSyntax list = [
        nameProperty
        summaryProperty
        parameterOptionsProperty parameters
        createArgument context entryPoint
        submitMethod context entryPoint
        simulateMethod context entryPoint
    ]
    let baseName = sprintf "%s.IEntryPoint" driverNamespace
    ``class`` ((entryPointClassFullName entryPoint).Name) ``<<`` [] ``>>``
        ``:`` (Some (simpleBase baseName)) ``,`` []
        [``internal``]
        ``{``
            members
        ``}``

/// Generates a namespace for a set of entry points that share the namespace
let private entryPointNamespace context name entryPoints =
    ``namespace`` name
        ``{``
            []
            [for ep in entryPoints -> entryPointClass context ep]
        ``}``

/// Returns the driver settings object.
let private driverSettings context =
    let newDriverSettings = driverNamespace + ".DriverSettings" |> ``type`` |> SyntaxFactory.ObjectCreationExpression
    let namedArg (name : string) expr = SyntaxFactory.NameColon name |> (SyntaxFactory.Argument expr).WithNameColon
    let immutableList elements = invoke (ident "System.Collections.Immutable.ImmutableList.Create") ``(`` elements ``)``
    let simulatorOptionAliases =
        [ literal <| "--" + fst CommandLineArguments.SimulatorOption
          literal <| "-" + snd CommandLineArguments.SimulatorOption ]
        |> immutableList
    let defaultSimulator =
        context.assemblyConstants.TryGetValue AssemblyConstants.DefaultSimulator
        |> fun (_, value) -> if String.IsNullOrWhiteSpace value then AssemblyConstants.QuantumSimulator else value
    let defaultExecutionTarget =
        context.assemblyConstants.TryGetValue AssemblyConstants.ExecutionTarget
        |> (fun (_, value) -> if value = null then "" else value)
        |> literal
    [
        namedArg "simulatorOptionAliases" simulatorOptionAliases
        namedArg "quantumSimulatorName" <| literal AssemblyConstants.QuantumSimulator
        namedArg "toffoliSimulatorName" <| literal AssemblyConstants.ToffoliSimulator
        namedArg "resourcesEstimatorName" <| literal AssemblyConstants.ResourcesEstimator
        namedArg "defaultSimulatorName" <| literal defaultSimulator
        namedArg "defaultExecutionTarget" <| defaultExecutionTarget
        namedArg "createDefaultCustomSimulator" <| customSimulatorFactory defaultSimulator
    ]
    |> SyntaxFactory.SeparatedList
    |> SyntaxFactory.ArgumentList
    |> newDriverSettings.WithArgumentList
    :> ExpressionSyntax

/// The main method for the standalone executable.
let private mainMethod context entryPoints =

    let entryPointArrayMembers =
        [
            for ep in entryPoints do
                let name = entryPointClassFullName ep
                ``new`` (``type`` (name.ToString())) ``(`` [] ``)``
        ]

    let entryPointArray =
        ``new array`` (Some (driverNamespace + ".IEntryPoint")) entryPointArrayMembers

    let driver = ``new`` (``type`` (driverNamespace + ".Driver")) ``(`` [driverSettings context; entryPointArray] ``)``
    let commandLineArgsName = "args"
    arrow_method "System.Threading.Tasks.Task<int>" "Main" ``<<`` [] ``>>``
        ``(`` [param commandLineArgsName ``of`` (``type`` "string[]")] ``)``
        [``private``; ``static``; async]
        (Some (``=>`` (await (driver <.> (ident "Run", [ident commandLineArgsName])))))

/// Generates a namespace for the main function
let private mainNamespace context entryPoints =
    let mainClass =
        ``class`` entryPointClassName ``<<`` [] ``>>``
            ``:`` None ``,`` []
            [``internal``]
            ``{``
                [mainMethod context entryPoints]
            ``}``

    ``namespace`` entryPointClassName
        ``{``
            []
            [mainClass]
        ``}``

/// Generates the C# source code for the file containing the Main function.
let generateMainSource context entryPoints =
    let mainNS = mainNamespace context entryPoints
    ``compilation unit`` [] (Seq.map using SimulationCode.autoNamespaces) [mainNS :> MemberDeclarationSyntax]
    |> ``with leading comments`` SimulationCode.autogenComment
    |> SimulationCode.formatSyntaxTree

/// Generates C# source code for a standalone executable that runs the Q# entry point.
let generateSource context (entryPoints : seq<QsCallable>) =
    let entryPointNamespaces = entryPoints |> Seq.groupBy (fun ep -> ep.FullName.Namespace)
    let namespaces = [for ns, eps in entryPointNamespaces -> entryPointNamespace context ns eps :> MemberDeclarationSyntax]
    ``compilation unit`` [] (Seq.map using SimulationCode.autoNamespaces) namespaces
    |> ``with leading comments`` SimulationCode.autogenComment
    |> SimulationCode.formatSyntaxTree
