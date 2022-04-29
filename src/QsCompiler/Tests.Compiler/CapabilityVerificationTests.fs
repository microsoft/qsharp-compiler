// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.CapabilityVerificationTests

open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxTree
open System.IO
open Xunit

/// Compiles the capability verification test cases using the given capability.
let private compile capability =
    CompilerTests.Compile(
        "TestCases",
        [ "CapabilityTests/Verification.qs"; "CapabilityTests/Inference.qs" ],
        references = [ File.ReadAllLines("ReferenceTargets.txt").[2] ],
        capability = capability
    )

/// The FullComputation capability tester.
let private fullComputation = compile "FullComputation" |> CompilerTests

/// The BasicMeasurementFeedback capability tester.
let private basicMeasurementFeedback = compile "BasicMeasurementFeedback" |> CompilerTests

/// The BasicQuantumFunctionality capability tester.
let private basicQuantumFunctionality = compile "BasicQuantumFunctionality" |> CompilerTests

let private basicExecution = compile "BasicExecution" |> CompilerTests

/// The qualified name for the test case name.
let internal testName name =
    { Namespace = "Microsoft.Quantum.Testing.Capability"; Name = name }

/// Asserts that the tester produces the expected error codes for the test case with the given name.
let private expect (tester: CompilerTests) errorCodes name =
    tester.VerifyDiagnostics(testName name, errorCodes)

/// The names of all "simple" test cases: test cases that have exactly one unsupported result comparison error in
/// BasicQuantumFunctionality, and no errors in FullComputation.
let private simpleTests =
    [
        "ResultAsBool"
        "ResultAsBoolNeq"
        "ResultAsBoolOp"
        "ResultAsBoolNeqOp"
        "ResultAsBoolOpReturnIf"
        "ResultAsBoolNeqOpReturnIf"
        "ResultAsBoolOpReturnIfNested"
        "ResultAsBoolOpSetIf"
        "ResultAsBoolNeqOpSetIf"
        "ResultAsBoolOpElseSet"
        "NestedResultIfReturn"
        "ElifSet"
        "ElifElifSet"
        "ElifElseSet"
        "SetLocal"
        "SetTuple"
        "EmptyIf"
        "EmptyIfNeq"
        "EmptyIfOp"
        "EmptyIfNeqOp"
        "Reset"
        "ResetNeq"
        "OverrideBmfToFull"
        "OverrideBmfToBqf"
        "OverrideFullToBmf"
        "ExplicitBmf"
    ]

[<Fact>]
let ``Unknown allows all Result comparison`` () =
    List.iter (expect fullComputation []) simpleTests
    "SetReusedName" |> expect fullComputation [ Error ErrorCode.LocalVariableAlreadyExists ]

    [ "ResultTuple"; "ResultArray" ]
    |> List.iter (expect fullComputation [ Error ErrorCode.InvalidTypeInEqualityComparison ])

let ``BasicQuantumFunctionality allows callables without Result comparison`` () =
    [ "NoOp"; "OverrideBqfToBmf" ] |> List.iter (expect basicQuantumFunctionality [])

[<Fact>]
let ``BasicQuantumFunctionality restricts all Result comparison`` () =
    simpleTests
    |> List.iter (expect basicQuantumFunctionality [ Error ErrorCode.UnsupportedResultComparison ])

    "SetReusedName"
    |> expect
        basicQuantumFunctionality
        [
            Error ErrorCode.LocalVariableAlreadyExists
            Error ErrorCode.UnsupportedResultComparison
        ]

    [ "ResultTuple"; "ResultArray" ]
    |> List.iter (expect fullComputation [ Error ErrorCode.InvalidTypeInEqualityComparison ])

[<Fact>]
let ``BasicMeasurementFeedback restricts Result comparison in functions`` () =
    [ "ResultAsBool"; "ResultAsBoolNeq" ]
    |> List.iter (expect basicMeasurementFeedback [ Error ErrorCode.ResultComparisonNotInOperationIf ])

    [ "ResultTuple"; "ResultArray" ]
    |> List.iter (expect fullComputation [ Error ErrorCode.InvalidTypeInEqualityComparison ])

[<Fact>]
let ``BasicMeasurementFeedback restricts non-if Result comparison in operations`` () =
    [ "ResultAsBoolOp"; "ResultAsBoolNeqOp" ]
    |> List.iter (expect basicMeasurementFeedback [ Error ErrorCode.ResultComparisonNotInOperationIf ])

[<Fact>]
let ``BasicMeasurementFeedback restricts return from Result if`` () =
    [
        "ResultAsBoolOpReturnIf"
        "ResultAsBoolOpReturnIfNested"
        "ResultAsBoolNeqOpReturnIf"
        "NestedResultIfReturn"
    ]
    |> List.iter (Error ErrorCode.ReturnInResultConditionedBlock |> Seq.replicate 2 |> expect basicMeasurementFeedback)

[<Fact>]
let ``BasicMeasurementFeedback allows local mutable set from Result if`` () =
    "SetLocal" |> expect basicMeasurementFeedback []

[<Fact>]
let ``BasicMeasurementFeedback restricts non-local mutable set from Result if`` () =
    [ "ResultAsBoolOpSetIf"; "ResultAsBoolNeqOpSetIf"; "SetTuple" ]
    |> List.iter (expect basicMeasurementFeedback [ Error ErrorCode.SetInResultConditionedBlock ])

    "SetReusedName"
    |> expect
        basicMeasurementFeedback
        [
            Error ErrorCode.LocalVariableAlreadyExists
            Error ErrorCode.SetInResultConditionedBlock
            Error ErrorCode.SetInResultConditionedBlock
        ]

[<Fact>]
let ``BasicMeasurementFeedback restricts non-local mutable set from Result elif`` () =
    [ "ElifSet"; "ElifElifSet" ]
    |> List.iter (expect basicMeasurementFeedback [ Error ErrorCode.SetInResultConditionedBlock ])

[<Fact>]
let ``BasicMeasurementFeedback restricts non-local mutable set from Result else`` () =
    [ "ResultAsBoolOpElseSet"; "ElifElseSet" ]
    |> List.iter (expect basicMeasurementFeedback [ Error ErrorCode.SetInResultConditionedBlock ])

[<Fact>]
let ``BasicMeasurementFeedback restricts empty Result if function`` () =
    [ "EmptyIf"; "EmptyIfNeq" ]
    |> List.iter (expect basicMeasurementFeedback [ Error ErrorCode.ResultComparisonNotInOperationIf ])

[<Fact>]
let ``BasicMeasurementFeedback allows empty Result if operation`` () =
    [ "EmptyIfOp"; "EmptyIfNeqOp" ] |> List.iter (expect basicMeasurementFeedback [])

[<Fact>]
let ``BasicMeasurementFeedback allows operation call from Result if`` () =
    [ "Reset"; "ResetNeq"; "OverrideBmfToFull"; "OverrideBmfToBqf"; "ExplicitBmf" ]
    |> List.iter (expect basicMeasurementFeedback [])

[<Fact>]
let ``BasicExecution restricts language constructs`` () =
    [
        "Recursion1"
        "Recursion2A"
        "Recursion2B"
        "Fail"
        "Repeat"
        "While"
        "TwoReturns"
    ]
    |> List.iter (expect basicExecution [ Error ErrorCode.UnsupportedClassicalCapability ])

[<Fact>]
let ``FullComputation allows all library calls and references`` () =
    [
        "CallLibraryBqf"
        "ReferenceLibraryBqf"
        "CallLibraryBmf"
        "CallLibraryBmfWithNestedCall"
        "ReferenceLibraryBmf"
        "CallLibraryFull"
        "CallLibraryFullWithNestedCall"
        "ReferenceLibraryFull"
    ]
    |> List.iter (expect fullComputation [])

[<Fact>]
let ``BasicMeasurementFeedback restricts library calls and references`` () =
    [ "CallLibraryBqf"; "CallLibraryBmf"; "CallLibraryBmfWithNestedCall" ]
    |> List.iter (expect basicMeasurementFeedback [])

    [ "CallLibraryFull"; "ReferenceLibraryFull" ]
    |> List.iter (
        expect
            basicMeasurementFeedback
            [
                Error ErrorCode.UnsupportedCallableCapability
                Warning WarningCode.ResultComparisonNotInOperationIf
                Warning WarningCode.ReturnInResultConditionedBlock
                Warning WarningCode.SetInResultConditionedBlock
            ]
    )

    "CallLibraryFullWithNestedCall"
    |> expect
        basicMeasurementFeedback
        [
            Error ErrorCode.UnsupportedCallableCapability
            Warning WarningCode.ResultComparisonNotInOperationIf
            Warning WarningCode.UnsupportedCallableCapability
        ]

[<Fact>]
let ``BasicQuantumFunctionality restricts library calls and references`` () =
    [ "CallLibraryBqf"; "ReferenceLibraryBqf" ] |> List.iter (expect basicQuantumFunctionality [])

    [ "CallLibraryBmf"; "ReferenceLibraryBmf" ]
    |> List.iter (
        expect
            basicQuantumFunctionality
            [
                Error ErrorCode.UnsupportedCallableCapability
                Warning WarningCode.UnsupportedResultComparison
            ]
    )

    [ "CallLibraryFull"; "ReferenceLibraryFull" ]
    |> List.iter (
        expect
            basicQuantumFunctionality
            [
                Error ErrorCode.UnsupportedCallableCapability
                Warning WarningCode.UnsupportedResultComparison
                Warning WarningCode.UnsupportedResultComparison
            ]
    )

    "CallLibraryBmfWithNestedCall"
    |> expect
        basicQuantumFunctionality
        [
            Error ErrorCode.UnsupportedCallableCapability
            Warning WarningCode.UnsupportedResultComparison
            Warning WarningCode.UnsupportedCallableCapability
        ]
