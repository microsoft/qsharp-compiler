// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.CapabilityTests

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
        Capability: RuntimeCapability
        Diagnostics: Level -> DiagnosticItem list
    }

    override case.ToString() = case.Name

let private createCapability opacity classical =
    RuntimeCapability.withResultOpacity opacity RuntimeCapability.bottom
    |> RuntimeCapability.withClassical classical

let private cases =
    [
        {
            Name = "NoOp"
            Capability = RuntimeCapability.bottom
            Diagnostics = fun _ -> []
        }

        for name in [ "ResultTuple"; "ResultArray" ] do
            {
                Name = name
                Capability = createCapability ResultOpacity.opaque ClassicalCapability.full
                Diagnostics =
                    function
                    | BasicExecution ->
                        [
                            Error ErrorCode.InvalidTypeInEqualityComparison
                            Error ErrorCode.UnsupportedClassicalCapability
                            Error ErrorCode.UnsupportedClassicalCapability
                        ]
                    | AdaptiveExecution ->
                        [
                            Error ErrorCode.InvalidTypeInEqualityComparison
                            Error ErrorCode.UnsupportedClassicalCapability
                        ]
                    | _ -> [ Error ErrorCode.InvalidTypeInEqualityComparison ]
            }

        {
            Name = "SetLocal"
            Capability = createCapability ResultOpacity.controlled ClassicalCapability.integral
            Diagnostics =
                function
                | BasicExecution ->
                    [
                        Error ErrorCode.UnsupportedResultComparison
                        Error ErrorCode.UnsupportedClassicalCapability
                    ]
                | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                | _ -> []
        }

        for name in [ "EmptyIfOp"; "EmptyIfNeqOp"; "Reset"; "ResetNeq" ] do
            {
                Name = name
                Capability = createCapability ResultOpacity.controlled ClassicalCapability.empty
                Diagnostics =
                    function
                    | BasicExecution
                    | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                    | _ -> []
            }

        for name in [ "EmptyIf"; "EmptyIfNeq" ] do
            {
                Name = name
                Capability = createCapability ResultOpacity.transparent ClassicalCapability.empty
                Diagnostics =
                    function
                    | BasicExecution
                    | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                    | BasicMeasurementFeedback -> [ Error ErrorCode.ResultComparisonNotInOperationIf ]
                    | _ -> []
            }

        {
            Name = "SetReusedName"
            Capability = createCapability ResultOpacity.transparent ClassicalCapability.integral
            Diagnostics =
                function
                | BasicExecution ->
                    [
                        Error ErrorCode.UnsupportedClassicalCapability
                        Error ErrorCode.UnsupportedResultComparison
                        Error ErrorCode.LocalVariableAlreadyExists
                    ]
                | BasicQuantumFunctionality ->
                    [
                        Error ErrorCode.UnsupportedResultComparison
                        Error ErrorCode.LocalVariableAlreadyExists
                    ]
                | BasicMeasurementFeedback ->
                    [
                        Error ErrorCode.LocalVariableAlreadyExists
                        Error ErrorCode.SetInResultConditionedBlock
                        Error ErrorCode.SetInResultConditionedBlock
                    ]
                | _ -> [ Error ErrorCode.LocalVariableAlreadyExists ]
        }

        for name in [ "ResultAsBool"; "ResultAsBoolNeq"; "ResultAsBoolOp"; "ResultAsBoolNeqOp" ] do
            {
                Name = name
                Capability = createCapability ResultOpacity.transparent ClassicalCapability.full
                Diagnostics =
                    function
                    | BasicExecution ->
                        [
                            Error ErrorCode.UnsupportedResultComparison
                            Error ErrorCode.UnsupportedClassicalCapability
                            Error ErrorCode.UnsupportedClassicalCapability
                        ]
                    | AdaptiveExecution -> [ Error ErrorCode.UnsupportedClassicalCapability ]
                    | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                    | BasicMeasurementFeedback -> [ Error ErrorCode.ResultComparisonNotInOperationIf ]
                    | FullComputation -> []
            }

        for name in [ "ResultAsBoolOpReturnIf"; "ResultAsBoolNeqOpReturnIf" ] do
            {
                Name = name
                Capability = createCapability ResultOpacity.transparent ClassicalCapability.full
                Diagnostics =
                    function
                    | BasicExecution ->
                        [
                            Error ErrorCode.UnsupportedResultComparison
                            Error ErrorCode.UnsupportedClassicalCapability
                        ]
                    | AdaptiveExecution -> [ Error ErrorCode.UnsupportedClassicalCapability ]
                    | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                    | BasicMeasurementFeedback -> Error ErrorCode.ReturnInResultConditionedBlock |> List.replicate 2
                    | FullComputation -> []
            }

        {
            Name = "ResultAsBoolOpReturnIfNested"
            Capability = createCapability ResultOpacity.transparent ClassicalCapability.full
            Diagnostics =
                function
                | BasicExecution ->
                    Error ErrorCode.UnsupportedResultComparison
                    :: List.replicate 3 (Error ErrorCode.UnsupportedClassicalCapability)
                | AdaptiveExecution -> Error ErrorCode.UnsupportedClassicalCapability |> List.replicate 3
                | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                | BasicMeasurementFeedback -> Error ErrorCode.ReturnInResultConditionedBlock |> List.replicate 2
                | FullComputation -> []
        }

        {
            Name = "NestedResultIfReturn"
            Capability = createCapability ResultOpacity.transparent ClassicalCapability.full
            Diagnostics =
                function
                | BasicExecution ->
                    [
                        Error ErrorCode.UnsupportedResultComparison
                        Error ErrorCode.UnsupportedClassicalCapability
                        Error ErrorCode.UnsupportedClassicalCapability
                    ]
                | AdaptiveExecution -> Error ErrorCode.UnsupportedClassicalCapability |> List.replicate 2
                | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                | BasicMeasurementFeedback -> Error ErrorCode.ReturnInResultConditionedBlock |> List.replicate 2
                | FullComputation -> []
        }

        for name in [ "ResultAsBoolOpSetIf"; "ResultAsBoolNeqOpSetIf" ] do
            {
                Name = name
                Capability = createCapability ResultOpacity.transparent ClassicalCapability.full
                Diagnostics =
                    function
                    | BasicExecution ->
                        [
                            Error ErrorCode.UnsupportedResultComparison
                            Error ErrorCode.UnsupportedClassicalCapability
                            Error ErrorCode.UnsupportedClassicalCapability
                        ]
                    | AdaptiveExecution -> [ Error ErrorCode.UnsupportedClassicalCapability ]
                    | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                    | BasicMeasurementFeedback -> [ Error ErrorCode.SetInResultConditionedBlock ]
                    | FullComputation -> []
            }

        {
            Name = "SetTuple"
            Capability = createCapability ResultOpacity.transparent ClassicalCapability.full
            Diagnostics =
                function
                | BasicExecution ->
                    [
                        Error ErrorCode.UnsupportedClassicalCapability
                        Error ErrorCode.UnsupportedResultComparison
                        Error ErrorCode.UnsupportedClassicalCapability
                        Error ErrorCode.UnsupportedClassicalCapability
                    ]
                | AdaptiveExecution -> [ Error ErrorCode.UnsupportedClassicalCapability ]
                | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                | BasicMeasurementFeedback -> [ Error ErrorCode.SetInResultConditionedBlock ]
                | FullComputation -> []
        }

        for name in [ "ResultAsBoolOpElseSet"; "ElifSet"; "ElifElifSet"; "ElifElseSet" ] do
            {
                Name = name
                Capability = createCapability ResultOpacity.transparent ClassicalCapability.full
                Diagnostics =
                    function
                    | BasicExecution ->
                        [
                            Error ErrorCode.UnsupportedResultComparison
                            Error ErrorCode.UnsupportedClassicalCapability
                            Error ErrorCode.UnsupportedClassicalCapability
                        ]
                    | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                    | AdaptiveExecution -> [ Error ErrorCode.UnsupportedClassicalCapability ]
                    | BasicMeasurementFeedback -> [ Error ErrorCode.SetInResultConditionedBlock ]
                    | FullComputation -> []
            }

        for name in
            [
                "Recursion1"
                "Recursion2A"
                "Recursion2B"
                "Fail"
                "Repeat"
                "While"
                "TwoReturns"
            ] do
            {
                Name = name
                Capability = createCapability ResultOpacity.opaque ClassicalCapability.full
                Diagnostics =
                    function
                    | BasicExecution
                    | AdaptiveExecution -> [ Error ErrorCode.UnsupportedClassicalCapability ]
                    | _ -> []
            }

        {
            Name = "OverrideBmfToBqf"
            Capability = RuntimeCapability.bottom
            Diagnostics =
                function
                | BasicExecution
                | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                | _ -> []
        }

        {
            Name = "OverrideBqfToBmf"
            Capability = createCapability ResultOpacity.controlled ClassicalCapability.empty
            Diagnostics = fun _ -> []
        }

        {
            Name = "OverrideFullToBmf"
            Capability = createCapability ResultOpacity.controlled ClassicalCapability.empty
            Diagnostics =
                function
                | BasicExecution ->
                    [
                        Error ErrorCode.UnsupportedResultComparison
                        Error ErrorCode.UnsupportedClassicalCapability
                        Error ErrorCode.UnsupportedClassicalCapability
                    ]
                | AdaptiveExecution -> [ Error ErrorCode.UnsupportedClassicalCapability ]
                | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                | BasicMeasurementFeedback -> [ Error ErrorCode.ResultComparisonNotInOperationIf ]
                | _ -> []
        }

        {
            Name = "ExplicitBmf"
            Capability = createCapability ResultOpacity.controlled ClassicalCapability.empty
            Diagnostics =
                function
                | BasicExecution
                | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                | _ -> []
        }

        {
            Name = "OverrideBmfToFull"
            Capability = createCapability ResultOpacity.transparent ClassicalCapability.empty
            Diagnostics =
                function
                | BasicExecution
                | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                | _ -> []
        }

        {
            Name = "CallBmfA"
            Capability = createCapability ResultOpacity.controlled ClassicalCapability.empty
            Diagnostics = fun _ -> []
        }

        {
            Name = "CallBmfB"
            Capability = createCapability ResultOpacity.controlled ClassicalCapability.empty
            Diagnostics =
                function
                | BasicExecution
                | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                | _ -> []
        }

        {
            Name = "CallBmfFullA"
            Capability = createCapability ResultOpacity.transparent ClassicalCapability.full
            Diagnostics = fun _ -> []
        }

        {
            Name = "CallBmfFullB"
            Capability = createCapability ResultOpacity.controlled ClassicalCapability.empty
            Diagnostics =
                function
                | BasicExecution
                | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                | _ -> []
        }

        {
            Name = "CallBmfFullC"
            Capability = createCapability ResultOpacity.transparent ClassicalCapability.full
            Diagnostics =
                function
                | BasicExecution ->
                    [
                        Error ErrorCode.UnsupportedResultComparison
                        Error ErrorCode.UnsupportedClassicalCapability
                        Error ErrorCode.UnsupportedClassicalCapability
                    ]
                | AdaptiveExecution -> [ Error ErrorCode.UnsupportedClassicalCapability ]
                | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                | BasicMeasurementFeedback -> [ Error ErrorCode.ResultComparisonNotInOperationIf ]
                | FullComputation -> []
        }

        {
            Name = "CallFullA"
            Capability = createCapability ResultOpacity.transparent ClassicalCapability.full
            Diagnostics = fun _ -> []
        }

        {
            Name = "CallFullB"
            Capability = createCapability ResultOpacity.transparent ClassicalCapability.full
            Diagnostics =
                function
                | BasicExecution ->
                    [
                        Error ErrorCode.UnsupportedResultComparison
                        Error ErrorCode.UnsupportedClassicalCapability
                        Error ErrorCode.UnsupportedClassicalCapability
                    ]
                | AdaptiveExecution -> [ Error ErrorCode.UnsupportedClassicalCapability ]
                | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                | BasicMeasurementFeedback -> [ Error ErrorCode.ResultComparisonNotInOperationIf ]
                | FullComputation -> []
        }

        {
            Name = "CallFullC"
            Capability = createCapability ResultOpacity.controlled ClassicalCapability.empty
            Diagnostics =
                function
                | BasicExecution
                | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                | _ -> []
        }

        {
            Name = "CallFullOverrideA"
            Capability = createCapability ResultOpacity.transparent ClassicalCapability.empty
            Diagnostics =
                function
                | BasicExecution
                | BasicQuantumFunctionality
                | BasicMeasurementFeedback -> [ Error ErrorCode.UnsupportedCallableCapability ]
                | _ -> []
        }

        {
            Name = "CallFullOverrideB"
            Capability = createCapability ResultOpacity.transparent ClassicalCapability.empty
            Diagnostics = fun _ -> []
        }

        {
            Name = "CallFullOverrideC"
            Capability = createCapability ResultOpacity.controlled ClassicalCapability.empty
            Diagnostics =
                function
                | BasicExecution
                | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                | _ -> []
        }

        {
            Name = "CallBmfOverrideA"
            Capability = createCapability ResultOpacity.controlled ClassicalCapability.empty
            Diagnostics =
                function
                | BasicExecution
                | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedCallableCapability ]
                | _ -> []
        }

        {
            Name = "CallBmfOverrideB"
            Capability = createCapability ResultOpacity.controlled ClassicalCapability.empty
            Diagnostics = fun _ -> []
        }

        {
            Name = "CallBmfOverrideC"
            Capability = createCapability ResultOpacity.transparent ClassicalCapability.full
            Diagnostics =
                function
                | BasicExecution ->
                    [
                        Error ErrorCode.UnsupportedResultComparison
                        Error ErrorCode.UnsupportedResultComparison
                        Error ErrorCode.UnsupportedClassicalCapability
                        Error ErrorCode.UnsupportedClassicalCapability
                    ]
                | AdaptiveExecution -> [ Error ErrorCode.UnsupportedClassicalCapability ]
                | BasicQuantumFunctionality -> Error ErrorCode.UnsupportedResultComparison |> List.replicate 2
                | BasicMeasurementFeedback -> [ Error ErrorCode.ResultComparisonNotInOperationIf ]
                | FullComputation -> []
        }

        for name in [ "BmfRecursion"; "BmfRecursion3A" ] do
            {
                Name = name
                Capability = createCapability ResultOpacity.controlled ClassicalCapability.full
                Diagnostics =
                    function
                    | BasicExecution ->
                        [
                            Error ErrorCode.UnsupportedResultComparison
                            Error ErrorCode.UnsupportedClassicalCapability
                        ]
                    | AdaptiveExecution -> [ Error ErrorCode.UnsupportedClassicalCapability ]
                    | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                    | BasicMeasurementFeedback
                    | FullComputation -> []
            }

        for name in [ "BmfRecursion3B"; "BmfRecursion3C" ] do
            {
                Name = name
                Capability = createCapability ResultOpacity.controlled ClassicalCapability.full
                Diagnostics =
                    function
                    | BasicExecution
                    | AdaptiveExecution -> [ Error ErrorCode.UnsupportedClassicalCapability ]
                    | _ -> []
            }

        {
            Name = "ReferenceBmfA"
            Capability = createCapability ResultOpacity.controlled ClassicalCapability.empty
            Diagnostics = fun _ -> []
        }

        {
            Name = "ReferenceBmfB"
            Capability = createCapability ResultOpacity.controlled ClassicalCapability.empty
            Diagnostics =
                function
                | BasicExecution
                | BasicQuantumFunctionality -> [ Error ErrorCode.UnsupportedResultComparison ]
                | _ -> []
        }

        for name in [ "MessageStringLit"; "MessageInterpStringLit"; "UseRangeLit"; "UseRangeVar" ] do
            {
                Name = name
                Capability = RuntimeCapability.bottom
                Diagnostics = fun _ -> []
            }

        for name in
            [
                "UseBigInt"
                "ReturnBigInt"
                "UseStringArg"
                "MessageStringVar"
                "ReturnString"
            ] do
            {
                Name = name
                Capability = createCapability ResultOpacity.opaque ClassicalCapability.full
                Diagnostics =
                    function
                    | BasicExecution
                    | AdaptiveExecution -> [ Error ErrorCode.UnsupportedClassicalCapability ]
                    | _ -> []
            }

        for name in [ "ConditionalBigInt"; "ConditionalString" ] do
            {
                Name = name
                Capability = createCapability ResultOpacity.opaque ClassicalCapability.full
                Diagnostics =
                    function
                    | BasicExecution
                    | AdaptiveExecution -> Error ErrorCode.UnsupportedClassicalCapability |> List.replicate 4
                    | _ -> []
            }

        for name in
            [
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
            ] do
            {
                Name = name
                Capability = RuntimeCapability.bottom
                Diagnostics = fun _ -> []
            }

        {
            Name = "LetToMutable"
            Capability = createCapability ResultOpacity.opaque ClassicalCapability.integral
            Diagnostics =
                function
                | BasicExecution -> [ Error ErrorCode.UnsupportedClassicalCapability ]
                | _ -> []
        }

        {
            Name = "MutableArray"
            Capability = createCapability ResultOpacity.opaque ClassicalCapability.full
            Diagnostics =
                function
                | BasicExecution
                | AdaptiveExecution -> [ Error ErrorCode.UnsupportedClassicalCapability ]
                | _ -> []
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
                Capability = createCapability ResultOpacity.opaque ClassicalCapability.full
                Diagnostics =
                    function
                    | BasicExecution -> Error ErrorCode.UnsupportedClassicalCapability |> List.replicate 2
                    | AdaptiveExecution -> [ Error ErrorCode.UnsupportedClassicalCapability ]
                    | _ -> []
            }

        for name in [ "MutableArrayToFor"; "MutableRangeToFor" ] do
            {
                Name = name
                Capability = createCapability ResultOpacity.opaque ClassicalCapability.full
                Diagnostics =
                    function
                    | BasicExecution
                    | AdaptiveExecution -> Error ErrorCode.UnsupportedClassicalCapability |> List.replicate 2
                    | _ -> []
            }

        for name in [ "MutableToArrayIndex"; "MutableToArrayIndexUpdate" ] do
            {
                Name = name
                Capability = createCapability ResultOpacity.opaque ClassicalCapability.full
                Diagnostics =
                    function
                    | BasicExecution -> Error ErrorCode.UnsupportedClassicalCapability |> List.replicate 3
                    | AdaptiveExecution -> [ Error ErrorCode.UnsupportedClassicalCapability ]
                    | _ -> []
            }

        for name in [ "MutableToArraySlice"; "MutableToArraySliceUpdate" ] do
            {
                Name = name
                Capability = createCapability ResultOpacity.opaque ClassicalCapability.full
                Diagnostics =
                    function
                    | BasicExecution -> Error ErrorCode.UnsupportedClassicalCapability |> List.replicate 3
                    | AdaptiveExecution -> Error ErrorCode.UnsupportedClassicalCapability |> List.replicate 2
                    | _ -> []
            }

        for name in [ "NewArray"; "LetToNewArraySize" ] do
            {
                Name = name
                Capability = createCapability ResultOpacity.opaque ClassicalCapability.full
                Diagnostics =
                    function
                    | BasicExecution
                    | AdaptiveExecution ->
                        [
                            Error ErrorCode.UnsupportedClassicalCapability
                            Warning WarningCode.DeprecatedNewArray
                        ]
                    | _ -> [ Warning WarningCode.DeprecatedNewArray ]
            }

        {
            Name = "MutableToNewArraySize"
            Capability = createCapability ResultOpacity.opaque ClassicalCapability.full
            Diagnostics =
                function
                | BasicExecution ->
                    [
                        Error ErrorCode.UnsupportedClassicalCapability
                        Error ErrorCode.UnsupportedClassicalCapability
                        Error ErrorCode.UnsupportedClassicalCapability
                        Warning WarningCode.DeprecatedNewArray
                    ]
                | AdaptiveExecution ->
                    [
                        Error ErrorCode.UnsupportedClassicalCapability
                        Error ErrorCode.UnsupportedClassicalCapability
                        Warning WarningCode.DeprecatedNewArray
                    ]
                | _ -> [ Warning WarningCode.DeprecatedNewArray ]
        }

        {
            Name = "InvalidCallable"
            Capability = RuntimeCapability.bottom
            Diagnostics = fun _ -> [ Error ErrorCode.InvalidUseOfUnderscorePattern ]
        }

        {
            Name = "NotFoundCallable"
            Capability = RuntimeCapability.bottom
            Diagnostics = fun _ -> [ Error ErrorCode.UnknownIdentifier ]
        }

        for name in [ "CallFunctor1"; "CallFunctor2" ] do
            {
                Name = name
                Capability = RuntimeCapability.bottom
                Diagnostics = fun _ -> []
            }

        for name in
            [
                "FunctionValue"
                "FunctionExpression"
                "OperationValue"
                "OperationExpression"
                "CallFunctorOfExpression"
            ] do
            {
                Name = name
                Capability = createCapability ResultOpacity.opaque ClassicalCapability.full
                Diagnostics =
                    function
                    | BasicExecution
                    | AdaptiveExecution -> [ Error ErrorCode.UnsupportedClassicalCapability ]
                    | _ -> []
            }

        {
            Name = "CallLibraryBqf"
            Capability = RuntimeCapability.bottom
            Diagnostics = fun _ -> []
        }

        {
            Name = "ReferenceLibraryOverride"
            Capability = RuntimeCapability.bottom
            Diagnostics = fun _ -> []
        }

        {
            Name = "CallLibraryBmf"
            Capability = createCapability ResultOpacity.controlled ClassicalCapability.empty
            Diagnostics =
                function
                | BasicExecution
                | BasicQuantumFunctionality ->
                    [
                        Error ErrorCode.UnsupportedCallableCapability
                        Warning WarningCode.UnsupportedResultComparison
                    ]
                | _ -> []
        }

        {
            Name = "CallLibraryBmfWithNestedCall"
            Capability = createCapability ResultOpacity.controlled ClassicalCapability.empty
            Diagnostics =
                function
                | BasicExecution
                | BasicQuantumFunctionality ->
                    [
                        Error ErrorCode.UnsupportedCallableCapability
                        Warning WarningCode.UnsupportedResultComparison
                        Warning WarningCode.UnsupportedCallableCapability
                    ]
                | _ -> []
        }

        for name in [ "CallLibraryFull"; "ReferenceLibraryFull" ] do
            {
                Name = name
                Capability = createCapability ResultOpacity.transparent ClassicalCapability.integral
                Diagnostics =
                    function
                    | BasicExecution
                    | BasicQuantumFunctionality ->
                        [
                            Error ErrorCode.UnsupportedCallableCapability
                            Warning WarningCode.UnsupportedResultComparison
                            Warning WarningCode.UnsupportedResultComparison
                        ]
                    | BasicMeasurementFeedback ->
                        [
                            Error ErrorCode.UnsupportedCallableCapability
                            Warning WarningCode.ResultComparisonNotInOperationIf
                            Warning WarningCode.ReturnInResultConditionedBlock
                            Warning WarningCode.SetInResultConditionedBlock
                        ]
                    | _ -> []
            }

        {
            Name = "ReferenceLibraryBqf"
            Capability = RuntimeCapability.bottom
            Diagnostics = fun _ -> []
        }

        {
            Name = "ReferenceLibraryBmf"
            Capability = createCapability ResultOpacity.controlled ClassicalCapability.empty
            Diagnostics =
                function
                | BasicExecution
                | BasicQuantumFunctionality ->
                    [
                        Error ErrorCode.UnsupportedCallableCapability
                        Warning WarningCode.UnsupportedResultComparison
                    ]
                | _ -> []
        }

        {
            Name = "CallLibraryFullWithNestedCall"
            Capability = createCapability ResultOpacity.transparent ClassicalCapability.integral
            Diagnostics =
                function
                | BasicExecution
                | BasicQuantumFunctionality ->
                    [
                        Error ErrorCode.UnsupportedCallableCapability
                        Warning WarningCode.UnsupportedResultComparison
                        Warning WarningCode.UnsupportedCallableCapability
                    ]
                | BasicMeasurementFeedback ->
                    [
                        Error ErrorCode.UnsupportedCallableCapability
                        Warning WarningCode.ResultComparisonNotInOperationIf
                        Warning WarningCode.UnsupportedCallableCapability
                    ]
                | _ -> []
        }

        {
            Name = "EntryPointReturnUnit"
            Capability = RuntimeCapability.bottom
            Diagnostics =
                function
                | BasicExecution
                | AdaptiveExecution
                | BasicQuantumFunctionality
                | BasicMeasurementFeedback -> [ Warning WarningCode.NonResultTypeReturnedInEntryPoint ]
                | FullComputation -> []
        }

        for name in
            [
                "EntryPointReturnResult"
                "EntryPointReturnResultArray"
                "EntryPointReturnResultTuple"
            ] do
            {
                Name = name
                Capability = RuntimeCapability.bottom
                Diagnostics = fun _ -> []
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
                Capability = RuntimeCapability.bottom
                Diagnostics =
                    function
                    | BasicExecution ->
                        [
                            Error ErrorCode.UnsupportedClassicalCapability
                            Warning WarningCode.NonResultTypeReturnedInEntryPoint
                        ]
                    | AdaptiveExecution
                    | BasicQuantumFunctionality
                    | BasicMeasurementFeedback -> [ Warning WarningCode.NonResultTypeReturnedInEntryPoint ]
                    | FullComputation -> []
            }

        for name in
            [
                "EntryPointReturnDouble"
                "EntryPointReturnDoubleArray"
                "EntryPointReturnResultDoubleTuple"
            ] do
            {
                Name = name
                Capability = RuntimeCapability.bottom
                Diagnostics =
                    function
                    | BasicExecution
                    | AdaptiveExecution ->
                        [
                            Error ErrorCode.UnsupportedClassicalCapability
                            Warning WarningCode.NonResultTypeReturnedInEntryPoint
                        ]
                    | BasicQuantumFunctionality
                    | BasicMeasurementFeedback -> [ Warning WarningCode.NonResultTypeReturnedInEntryPoint ]
                    | FullComputation -> []
            }

        for name in [ "EntryPointParamBool"; "EntryPointParamInt" ] do
            {
                Name = name
                Capability = RuntimeCapability.bottom
                Diagnostics =
                    function
                    | BasicExecution -> [ Error ErrorCode.UnsupportedClassicalCapability ]
                    | _ -> []
            }

        {
            Name = "EntryPointParamDouble"
            Capability = RuntimeCapability.bottom
            Diagnostics =
                function
                | BasicExecution
                | AdaptiveExecution -> [ Error ErrorCode.UnsupportedClassicalCapability ]
                | _ -> []
        }
    ]

type private TestData() as data =
    inherit TheoryData<Case>()
    do List.iter data.Add cases

let private compile capability isExecutable =
    let files = [ "Capabilities.qs"; "General.qs"; "LinkingTests/Core.qs" ]
    let references = [ File.ReadAllLines("ReferenceTargets.txt")[2] ]
    CompilerTests.Compile("TestCases", files, references, capability, isExecutable)

let private inferences =
    let compilation = compile "" false
    GlobalCallableResolutions (Capabilities.infer compilation.BuiltCompilation).Namespaces

let levels =
    [
        BasicExecution, compile "BasicExecution" true |> CompilerTests
        AdaptiveExecution, compile "AdaptiveExecution" true |> CompilerTests
        BasicQuantumFunctionality, compile "BasicQuantumFunctionality" true |> CompilerTests
        BasicMeasurementFeedback, compile "BasicMeasurementFeedback" true |> CompilerTests
        FullComputation, compile "FullComputation" true |> CompilerTests
    ]

[<Theory>]
[<ClassData(typeof<TestData>)>]
[<CompiledName "Test">]
let test case =
    let fullName = { Namespace = "Microsoft.Quantum.Testing.Capability"; Name = case.Name }
    let inferredCapability = SymbolResolution.TryGetRequiredCapability inferences[fullName].Attributes

    Assert.True(
        Seq.contains case.Capability inferredCapability,
        $"Unexpected capability for {case.Name}.\nExpected: %A{case.Capability}\nActual: %A{inferredCapability}"
    )

    for level, tester in levels do
        tester.VerifyDiagnostics(fullName, case.Diagnostics level)
