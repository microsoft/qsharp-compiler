// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.CapabilityTests

open System.Collections.Immutable
open System.IO
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Xunit

type Level =
    | BasicExecution
    | AdaptiveExecution
    | BasicQuantumFunctionality
    | BasicMeasurementFeedback
    | FullComputation

type Case =
    {
        Name: string
        Capability: TargetCapability
        Diagnostics: Level -> DiagnosticItem list
    }

    override case.ToString() = case.Name

let private runtimeCapability opacity classical =
    TargetCapability.withResultOpacity opacity TargetCapability.bottom
    |> TargetCapability.withClassicalCompute classical

let private unsupportedResult =
    function
    | BasicExecution
    | BasicQuantumFunctionality -> [ Warning WarningCode.UnsupportedResultComparison ]
    | _ -> []

let private unsupportedClassical basic adaptive =
    function
    | BasicExecution -> Warning WarningCode.UnsupportedClassicalCapability |> List.replicate basic
    | AdaptiveExecution -> Warning WarningCode.UnsupportedClassicalCapability |> List.replicate adaptive
    | _ -> []

let private cases =
    [
        for name in
            [
                "NoOp"
                "CallLibraryBqf"
                "ReferenceLibraryBqf"
                "MessageStringLit"
                "MessageInterpStringLit"
                "UseRangeLit"
                "UseRangeVar"
                "LetToLet"
                "LetToCall"
                "ParamToLet"
                "LetArray"
                "LetArrayLitToFor"
                "LetArrayToFor"
                "LetRangeLitToFor"
                "LetRangeToFor"
                "LetToArraySize"
                "LetToArrayIndex"
                "LetToArraySlice"
                "LetToArrayIndexUpdate"
                "LetToArraySliceUpdate"
                "CallFunctor1"
                "CallFunctor2"
                "EntryPointReturnResult"
                "EntryPointReturnResultArray"
                "EntryPointReturnResultTuple"
            ] do
            {
                Name = name
                Capability = TargetCapability.bottom
                Diagnostics = fun _ -> []
            }

        for name in
            [
                "EmptyIfOp"
                "EmptyIfNeqOp"
                "Reset"
                "ResetNeq"
                "ExplicitBmf"
                "CallBmfB"
                "CallBmfFullB"
                "CallFullC"
                "CallFullOverrideC"
                "ReferenceBmfB"
            ] do
            {
                Name = name
                Capability = runtimeCapability ResultOpacity.controlled ClassicalCompute.empty
                Diagnostics = unsupportedResult
            }

        for name in [ "ResultTuple"; "ResultArray" ] do
            {
                Name = name
                Capability = runtimeCapability ResultOpacity.opaque ClassicalCompute.full
                Diagnostics =
                    function
                    | BasicExecution ->
                        Error ErrorCode.InvalidTypeInEqualityComparison
                        :: List.replicate 2 (Warning WarningCode.UnsupportedClassicalCapability)
                    | AdaptiveExecution ->
                        [
                            Error ErrorCode.InvalidTypeInEqualityComparison
                            Warning WarningCode.UnsupportedClassicalCapability
                        ]
                    | _ -> [ Error ErrorCode.InvalidTypeInEqualityComparison ]
            }

        for name in [ "EmptyIf"; "EmptyIfNeq" ] do
            {
                Name = name
                Capability = runtimeCapability ResultOpacity.transparent ClassicalCompute.empty
                Diagnostics =
                    function
                    | BasicExecution
                    | BasicQuantumFunctionality -> [ Warning WarningCode.UnsupportedResultComparison ]
                    | BasicMeasurementFeedback -> [ Warning WarningCode.ResultComparisonNotInOperationIf ]
                    | _ -> []
            }

        for name in
            [
                "ResultAsBool"
                "ResultAsBoolNeq"
                "ResultAsBoolOp"
                "ResultAsBoolNeqOp"
                "CallBmfFullC"
                "CallFullB"
            ] do
            {
                Name = name
                Capability = runtimeCapability ResultOpacity.transparent ClassicalCompute.full
                Diagnostics =
                    function
                    | BasicExecution ->
                        Warning WarningCode.UnsupportedResultComparison
                        :: List.replicate 2 (Warning WarningCode.UnsupportedClassicalCapability)
                    | AdaptiveExecution -> [ Warning WarningCode.UnsupportedClassicalCapability ]
                    | BasicQuantumFunctionality -> [ Warning WarningCode.UnsupportedResultComparison ]
                    | BasicMeasurementFeedback -> [ Warning WarningCode.ResultComparisonNotInOperationIf ]
                    | FullComputation -> []
            }

        for name in [ "ResultAsBoolOpReturnIf"; "ResultAsBoolNeqOpReturnIf" ] do
            {
                Name = name
                Capability = runtimeCapability ResultOpacity.transparent ClassicalCompute.full
                Diagnostics =
                    function
                    | BasicExecution ->
                        [
                            Warning WarningCode.UnsupportedClassicalCapability
                            Warning WarningCode.UnsupportedResultComparison
                        ]
                    | AdaptiveExecution -> [ Warning WarningCode.UnsupportedClassicalCapability ]
                    | BasicQuantumFunctionality -> [ Warning WarningCode.UnsupportedResultComparison ]
                    | BasicMeasurementFeedback -> Warning WarningCode.ReturnInResultConditionedBlock |> List.replicate 2
                    | FullComputation -> []
            }

        for name in [ "ResultAsBoolOpSetIf"; "ResultAsBoolNeqOpSetIf" ] do
            {
                Name = name
                Capability = runtimeCapability ResultOpacity.transparent ClassicalCompute.full
                Diagnostics =
                    function
                    | BasicExecution ->
                        Warning WarningCode.UnsupportedResultComparison
                        :: List.replicate 2 (Warning WarningCode.UnsupportedClassicalCapability)
                    | AdaptiveExecution -> [ Warning WarningCode.UnsupportedClassicalCapability ]
                    | BasicQuantumFunctionality -> [ Warning WarningCode.UnsupportedResultComparison ]
                    | BasicMeasurementFeedback -> [ Warning WarningCode.SetInResultConditionedBlock ]
                    | FullComputation -> []
            }

        for name in [ "ResultAsBoolOpElseSet"; "ElifSet"; "ElifElifSet"; "ElifElseSet" ] do
            {
                Name = name
                Capability = runtimeCapability ResultOpacity.transparent ClassicalCompute.full
                Diagnostics =
                    function
                    | BasicExecution ->
                        Warning WarningCode.UnsupportedResultComparison
                        :: List.replicate 2 (Warning WarningCode.UnsupportedClassicalCapability)
                    | BasicQuantumFunctionality -> [ Warning WarningCode.UnsupportedResultComparison ]
                    | AdaptiveExecution -> [ Warning WarningCode.UnsupportedClassicalCapability ]
                    | BasicMeasurementFeedback -> [ Warning WarningCode.SetInResultConditionedBlock ]
                    | FullComputation -> []
            }

        for name in
            [
                "MutableArray"
                "Recursion1"
                "Recursion2A"
                "Recursion2B"
                "Fail"
                "Repeat"
                "While"
                "TwoReturns"
                "UseBigInt"
                "ReturnBigInt"
                "UseStringArg"
                "MessageStringVar"
                "ReturnString"
                "FunctionValue"
                "FunctionExpression"
                "OperationValue"
                "OperationExpression"
                "CallFunctorOfLocalVar"
                "CallFunctorOfExpression"
            ] do
            {
                Name = name
                Capability = runtimeCapability ResultOpacity.opaque ClassicalCompute.full
                Diagnostics = unsupportedClassical 1 1
            }

        for name in [ "OverrideBqfToBmf"; "CallBmfA"; "CallBmfOverrideB"; "ReferenceBmfA" ] do
            {
                Name = name
                Capability = runtimeCapability ResultOpacity.controlled ClassicalCompute.empty
                Diagnostics = fun _ -> []
            }

        for name in [ "CallBmfFullA"; "CallFullA" ] do
            {
                Name = name
                Capability = runtimeCapability ResultOpacity.transparent ClassicalCompute.full
                Diagnostics = fun _ -> []
            }

        for name in [ "BmfRecursion"; "BmfRecursion3A" ] do
            {
                Name = name
                Capability = runtimeCapability ResultOpacity.controlled ClassicalCompute.full
                Diagnostics =
                    function
                    | BasicExecution ->
                        [
                            Warning WarningCode.UnsupportedClassicalCapability
                            Warning WarningCode.UnsupportedResultComparison
                        ]
                    | AdaptiveExecution -> [ Warning WarningCode.UnsupportedClassicalCapability ]
                    | BasicQuantumFunctionality -> [ Warning WarningCode.UnsupportedResultComparison ]
                    | BasicMeasurementFeedback
                    | FullComputation -> []
            }

        for name in [ "BmfRecursion3B"; "BmfRecursion3C" ] do
            {
                Name = name
                Capability = runtimeCapability ResultOpacity.controlled ClassicalCompute.full
                Diagnostics = unsupportedClassical 1 1
            }

        for name in [ "ConditionalBigInt"; "ConditionalString" ] do
            {
                Name = name
                Capability = runtimeCapability ResultOpacity.opaque ClassicalCompute.full
                Diagnostics = unsupportedClassical 4 4
            }

        for name in
            [
                "MutableToLet"
                "MutableToCall"
                "MutableArrayLitToFor"
                "MutableRangeLitToFor"
                "MutableToArraySize"
            ] do
            {
                Name = name
                Capability = runtimeCapability ResultOpacity.opaque ClassicalCompute.full
                Diagnostics = unsupportedClassical 2 1
            }

        for name in [ "MutableArrayToFor"; "MutableRangeToFor" ] do
            {
                Name = name
                Capability = runtimeCapability ResultOpacity.opaque ClassicalCompute.full
                Diagnostics = unsupportedClassical 2 2
            }

        for name in [ "MutableToArrayIndex"; "MutableToArrayIndexUpdate" ] do
            {
                Name = name
                Capability = runtimeCapability ResultOpacity.opaque ClassicalCompute.full
                Diagnostics = unsupportedClassical 3 1
            }

        for name in [ "MutableToArraySlice"; "MutableToArraySliceUpdate" ] do
            {
                Name = name
                Capability = runtimeCapability ResultOpacity.opaque ClassicalCompute.full
                Diagnostics = unsupportedClassical 3 2
            }

        for name in [ "NewArray"; "LetToNewArraySize" ] do
            {
                Name = name
                Capability = runtimeCapability ResultOpacity.opaque ClassicalCompute.full
                Diagnostics =
                    function
                    | BasicExecution
                    | AdaptiveExecution ->
                        [
                            Warning WarningCode.UnsupportedClassicalCapability
                            Warning WarningCode.DeprecatedNewArray
                        ]
                    | _ -> [ Warning WarningCode.DeprecatedNewArray ]
            }

        for name in [ "CallLibraryBmf"; "ReferenceLibraryBmf" ] do
            {
                Name = name
                Capability = runtimeCapability ResultOpacity.controlled ClassicalCompute.empty
                Diagnostics =
                    function
                    | BasicExecution
                    | BasicQuantumFunctionality -> [ Warning WarningCode.UnsupportedCallableCapability ]
                    | _ -> []
            }

        for name in [ "CallLibraryFull"; "ReferenceLibraryFull" ] do
            {
                Name = name
                Capability = runtimeCapability ResultOpacity.transparent ClassicalCompute.integral
                Diagnostics =
                    function
                    | BasicExecution
                    | BasicQuantumFunctionality
                    | BasicMeasurementFeedback -> [ Warning WarningCode.UnsupportedCallableCapability ]
                    | _ -> []
            }

        for name in
            [
                "EntryPointReturnBool"
                "EntryPointReturnInt"
                "EntryPointReturnBoolArray"
                "EntryPointReturnResultBoolTuple"
            ] do
            {
                Name = name
                Capability = TargetCapability.bottom
                Diagnostics =
                    function
                    | BasicExecution -> [ Warning WarningCode.UnsupportedClassicalCapability ]
                    | BasicQuantumFunctionality
                    | BasicMeasurementFeedback -> [ Warning WarningCode.NonResultTypeReturnedInEntryPoint ]
                    | _ -> []
            }

        for name in
            [
                "EntryPointReturnDouble"
                "EntryPointReturnDoubleArray"
                "EntryPointReturnResultDoubleTuple"
            ] do
            {
                Name = name
                Capability = TargetCapability.bottom
                Diagnostics =
                    function
                    | BasicExecution
                    | AdaptiveExecution -> [ Warning WarningCode.UnsupportedClassicalCapability ]
                    | BasicQuantumFunctionality
                    | BasicMeasurementFeedback -> [ Warning WarningCode.NonResultTypeReturnedInEntryPoint ]
                    | FullComputation -> []
            }

        for name in [ "EntryPointParamBool"; "EntryPointParamInt" ] do
            {
                Name = name
                Capability = TargetCapability.bottom
                Diagnostics = unsupportedClassical 1 0
            }

        {
            Name = "SetLocal"
            Capability = runtimeCapability ResultOpacity.controlled ClassicalCompute.integral
            Diagnostics =
                function
                | BasicExecution ->
                    [
                        Warning WarningCode.UnsupportedClassicalCapability
                        Warning WarningCode.UnsupportedResultComparison
                    ]
                | BasicQuantumFunctionality -> [ Warning WarningCode.UnsupportedResultComparison ]
                | _ -> []
        }
        {
            Name = "SetReusedName"
            Capability = runtimeCapability ResultOpacity.transparent ClassicalCompute.integral
            Diagnostics =
                function
                | BasicExecution ->
                    [
                        Error ErrorCode.LocalVariableAlreadyExists
                        Warning WarningCode.UnsupportedClassicalCapability
                        Warning WarningCode.UnsupportedResultComparison
                    ]
                | BasicQuantumFunctionality ->
                    [
                        Error ErrorCode.LocalVariableAlreadyExists
                        Warning WarningCode.UnsupportedResultComparison
                    ]
                | BasicMeasurementFeedback ->
                    Error ErrorCode.LocalVariableAlreadyExists
                    :: List.replicate 2 (Warning WarningCode.SetInResultConditionedBlock)
                | _ -> [ Error ErrorCode.LocalVariableAlreadyExists ]
        }
        {
            Name = "ResultAsBoolOpReturnIfNested"
            Capability = runtimeCapability ResultOpacity.transparent ClassicalCompute.full
            Diagnostics =
                function
                | BasicExecution ->
                    Warning WarningCode.UnsupportedResultComparison
                    :: List.replicate 3 (Warning WarningCode.UnsupportedClassicalCapability)
                | AdaptiveExecution -> Warning WarningCode.UnsupportedClassicalCapability |> List.replicate 3
                | BasicQuantumFunctionality -> [ Warning WarningCode.UnsupportedResultComparison ]
                | BasicMeasurementFeedback -> Warning WarningCode.ReturnInResultConditionedBlock |> List.replicate 2
                | FullComputation -> []
        }
        {
            Name = "NestedResultIfReturn"
            Capability = runtimeCapability ResultOpacity.transparent ClassicalCompute.full
            Diagnostics =
                function
                | BasicExecution ->
                    Warning WarningCode.UnsupportedResultComparison
                    :: List.replicate 2 (Warning WarningCode.UnsupportedClassicalCapability)
                | AdaptiveExecution -> Warning WarningCode.UnsupportedClassicalCapability |> List.replicate 2
                | BasicQuantumFunctionality -> [ Warning WarningCode.UnsupportedResultComparison ]
                | BasicMeasurementFeedback -> Warning WarningCode.ReturnInResultConditionedBlock |> List.replicate 2
                | FullComputation -> []
        }
        {
            Name = "SetTuple"
            Capability = runtimeCapability ResultOpacity.transparent ClassicalCompute.full
            Diagnostics =
                function
                | BasicExecution ->
                    Warning WarningCode.UnsupportedResultComparison
                    :: List.replicate 3 (Warning WarningCode.UnsupportedClassicalCapability)
                | AdaptiveExecution -> [ Warning WarningCode.UnsupportedClassicalCapability ]
                | BasicQuantumFunctionality -> [ Warning WarningCode.UnsupportedResultComparison ]
                | BasicMeasurementFeedback -> [ Warning WarningCode.SetInResultConditionedBlock ]
                | FullComputation -> []
        }
        {
            Name = "SetInNestedIf"
            Capability = runtimeCapability ResultOpacity.controlled ClassicalCompute.integral
            Diagnostics =
                function
                | BasicExecution ->
                    [
                        Warning WarningCode.UnsupportedResultComparison
                        Warning WarningCode.UnsupportedClassicalCapability
                    ]
                | BasicQuantumFunctionality -> [ Warning WarningCode.UnsupportedResultComparison ]
                | _ -> []
        }
        {
            Name = "OverrideBmfToBqf"
            Capability = TargetCapability.bottom
            Diagnostics = unsupportedResult
        }
        {
            Name = "OverrideFullToBmf"
            Capability = runtimeCapability ResultOpacity.controlled ClassicalCompute.empty
            Diagnostics =
                function
                | BasicExecution ->
                    Warning WarningCode.UnsupportedResultComparison
                    :: List.replicate 2 (Warning WarningCode.UnsupportedClassicalCapability)
                | AdaptiveExecution -> [ Warning WarningCode.UnsupportedClassicalCapability ]
                | BasicQuantumFunctionality -> [ Warning WarningCode.UnsupportedResultComparison ]
                | BasicMeasurementFeedback -> [ Warning WarningCode.ResultComparisonNotInOperationIf ]
                | _ -> []
        }
        {
            Name = "OverrideBmfToFull"
            Capability = runtimeCapability ResultOpacity.transparent ClassicalCompute.empty
            Diagnostics = unsupportedResult
        }
        {
            Name = "CallFullOverrideA"
            Capability = runtimeCapability ResultOpacity.transparent ClassicalCompute.empty
            Diagnostics =
                function
                | BasicExecution
                | BasicQuantumFunctionality
                | BasicMeasurementFeedback -> [ Warning WarningCode.UnsupportedCallableCapability ]
                | _ -> []
        }
        {
            Name = "CallFullOverrideB"
            Capability = runtimeCapability ResultOpacity.transparent ClassicalCompute.empty
            Diagnostics = fun _ -> []
        }
        {
            Name = "CallBmfOverrideA"
            Capability = runtimeCapability ResultOpacity.controlled ClassicalCompute.empty
            Diagnostics =
                function
                | BasicExecution
                | BasicQuantumFunctionality -> [ Warning WarningCode.UnsupportedCallableCapability ]
                | _ -> []
        }
        {
            Name = "CallBmfOverrideC"
            Capability = runtimeCapability ResultOpacity.transparent ClassicalCompute.full
            Diagnostics =
                function
                | BasicExecution ->
                    List.replicate 2 (Warning WarningCode.UnsupportedClassicalCapability)
                    @ List.replicate 2 (Warning WarningCode.UnsupportedResultComparison)
                | AdaptiveExecution -> [ Warning WarningCode.UnsupportedClassicalCapability ]
                | BasicQuantumFunctionality -> Warning WarningCode.UnsupportedResultComparison |> List.replicate 2
                | BasicMeasurementFeedback -> [ Warning WarningCode.ResultComparisonNotInOperationIf ]
                | FullComputation -> []
        }
        {
            Name = "CallLibraryOverride"
            Capability = runtimeCapability ResultOpacity.opaque ClassicalCompute.full
            Diagnostics =
                function
                | BasicExecution
                | AdaptiveExecution -> [ Warning WarningCode.UnsupportedCallableCapability ]
                | _ -> []
        }
        {
            Name = "LetToMutable"
            Capability = runtimeCapability ResultOpacity.opaque ClassicalCompute.integral
            Diagnostics = unsupportedClassical 1 0
        }
        {
            Name = "MutableToNewArraySize"
            Capability = runtimeCapability ResultOpacity.opaque ClassicalCompute.full
            Diagnostics =
                function
                | BasicExecution ->
                    Warning WarningCode.DeprecatedNewArray
                    :: List.replicate 3 (Warning WarningCode.UnsupportedClassicalCapability)
                | AdaptiveExecution ->
                    Warning WarningCode.DeprecatedNewArray
                    :: List.replicate 2 (Warning WarningCode.UnsupportedClassicalCapability)
                | _ -> [ Warning WarningCode.DeprecatedNewArray ]
        }
        {
            Name = "InvalidCallable"
            Capability = TargetCapability.bottom
            Diagnostics = fun _ -> [ Error ErrorCode.InvalidUseOfUnderscorePattern ]
        }
        {
            Name = "NotFoundCallable"
            Capability = TargetCapability.bottom
            Diagnostics = fun _ -> [ Error ErrorCode.UnknownIdentifier ]
        }
        {
            Name = "CallLibraryBmfWithNestedCall"
            Capability = runtimeCapability ResultOpacity.controlled ClassicalCompute.empty
            Diagnostics =
                function
                | BasicExecution
                | BasicQuantumFunctionality -> [ Warning WarningCode.UnsupportedCallableCapability ]
                | _ -> []
        }
        {
            Name = "CallLibraryFullWithNestedCall"
            Capability = runtimeCapability ResultOpacity.transparent ClassicalCompute.integral
            Diagnostics =
                function
                | BasicExecution
                | BasicQuantumFunctionality
                | BasicMeasurementFeedback -> [ Warning WarningCode.UnsupportedCallableCapability ]
                | _ -> []
        }
        {
            Name = "EntryPointReturnUnit"
            Capability = TargetCapability.bottom
            Diagnostics =
                function
                | BasicQuantumFunctionality
                | BasicMeasurementFeedback -> [ Warning WarningCode.NonResultTypeReturnedInEntryPoint ]
                | _ -> []
        }
        {
            Name = "EntryPointParamDouble"
            Capability = TargetCapability.bottom
            Diagnostics = unsupportedClassical 1 1
        }
    ]

type private TestData() as data =
    inherit TheoryData<Case>()
    do List.iter data.Add cases

let private compile capability output =
    let files = [ "Capabilities.qs"; "General.qs"; "LinkingTests/Core.qs" ]
    let references = [ File.ReadAllLines("ReferenceTargets.txt")[2] ]
    TestUtils.buildFiles "TestCases" files references (Some capability) output

let private inferences =
    let compilation = compile TargetCapability.top TestUtils.Library
    GlobalCallableResolutions (Capabilities.infer compilation.BuiltCompilation).Namespaces

let private levels =
    seq {
        BasicExecution, TargetCapability.basicExecution
        AdaptiveExecution, TargetCapability.adaptiveExecution
        BasicQuantumFunctionality, TargetCapability.basicQuantumFunctionality
        BasicMeasurementFeedback, TargetCapability.basicMeasurementFeedback
        FullComputation, TargetCapability.fullComputation
    }
    |> Seq.map (fun (level, capability) -> level, compile capability TestUtils.Exe |> Diagnostics.byDeclaration)
    |> ImmutableArray.CreateRange

[<Theory>]
[<ClassData(typeof<TestData>)>]
let test case =
    let fullName = { Namespace = "Microsoft.Quantum.Testing.Capability"; Name = case.Name }
    let inferredCapability = SymbolResolution.TryGetRequiredCapability inferences[fullName].Attributes

    Assert.True(
        Seq.contains case.Capability inferredCapability,
        $"Unexpected capability for {case.Name}.\nExpected: %A{case.Capability}\nActual: %A{inferredCapability}"
    )

    for level, diagnostics in levels do
        Diagnostics.assertMatches (case.Diagnostics level |> Seq.map (fun e -> e, None)) diagnostics[fullName]
