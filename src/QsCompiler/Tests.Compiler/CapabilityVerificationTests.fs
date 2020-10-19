// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.CapabilityVerificationTests

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.ReservedKeywords.AssemblyConstants
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open System.IO
open Xunit

/// Compiles the capability verification test cases using the given capability.
let private compile capabilities =
    CompilerTests.Compile
        ("TestCases",
         ["CapabilityTests/Verification.qs"; "CapabilityTests/Inference.qs"],
         references = [File.ReadAllLines("ReferenceTargets.txt").[1]],
         capabilities = capabilities)

/// The unknown capability tester.
let private unknown = compile RuntimeCapabilities.FullComputation |> CompilerTests

/// The QPRGen0 capability tester.
let private gen0 = compile RuntimeCapabilities.BasicQuantumFunctionality |> CompilerTests

/// The QPRGen1 capability tester.
let private gen1 = compile RuntimeCapabilities.BasicMeasurementFeedback |> CompilerTests

/// The qualified name for the test case name.
let internal testName name =
    QsQualifiedName.New (NonNullable<_>.New "Microsoft.Quantum.Testing.Capability",
                         NonNullable<_>.New name)

/// Asserts that the tester produces the expected error codes for the test case with the given name.
let private expect (tester : CompilerTests) errorCodes name =
    tester.VerifyDiagnostics (testName name, Seq.map Error errorCodes)

/// The names of all "simple" test cases: test cases that have exactly one unsupported result comparison error in
/// QPRGen0, and no errors in Unknown.
let private simpleTests =
    [ "ResultAsBool"
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
      "OverrideGen1ToUnknown"
      "OverrideGen1ToGen0"
      "OverrideUnknownToGen1"
      "ExplicitGen1" ]

[<Fact>]
let ``Unknown allows all Result comparison`` () =
    List.iter (expect unknown []) simpleTests
    "SetReusedName" |> expect unknown [ErrorCode.LocalVariableAlreadyExists]
    [ "ResultTuple"
      "ResultArray" ]
    |> List.iter (expect unknown [ErrorCode.InvalidTypeInEqualityComparison])

let ``QPRGen0 allows callables without Result comparison`` () =
    [ "NoOp"
      "OverrideGen0ToGen1" ]
    |> List.iter (expect gen0 [])

[<Fact>]
let ``QPRGen0 restricts all Result comparison`` () =
    List.iter (expect gen0 [ErrorCode.UnsupportedResultComparison]) simpleTests
    "SetReusedName" |> expect gen0 [ErrorCode.LocalVariableAlreadyExists; ErrorCode.UnsupportedResultComparison]
    [ "ResultTuple"
      "ResultArray" ]
    |> List.iter (expect unknown [ErrorCode.InvalidTypeInEqualityComparison])

[<Fact>]
let ``QPRGen1 restricts Result comparison in functions`` () =
    [ "ResultAsBool"
      "ResultAsBoolNeq" ]
    |> List.iter (expect gen1 [ErrorCode.ResultComparisonNotInOperationIf])
    [ "ResultTuple"
      "ResultArray" ]
    |> List.iter (expect unknown [ErrorCode.InvalidTypeInEqualityComparison])

[<Fact>]
let ``QPRGen1 restricts non-if Result comparison in operations`` () =
    [ "ResultAsBoolOp"
      "ResultAsBoolNeqOp" ]
    |> List.iter (expect gen1 [ErrorCode.ResultComparisonNotInOperationIf])

[<Fact>]
let ``QPRGen1 restricts return from Result if`` () =
    [ "ResultAsBoolOpReturnIf"
      "ResultAsBoolOpReturnIfNested"
      "ResultAsBoolNeqOpReturnIf"
      "NestedResultIfReturn" ]
    |> List.iter (expect gen1 <| Seq.replicate 2 ErrorCode.ReturnInResultConditionedBlock)

[<Fact>]
let ``QPRGen1 allows local mutable set from Result if`` () = "SetLocal" |> expect gen1 []

[<Fact>]
let ``QPRGen1 restricts non-local mutable set from Result if`` () =
    [ "ResultAsBoolOpSetIf"
      "ResultAsBoolNeqOpSetIf"
      "SetTuple" ]
    |> List.iter (expect gen1 [ErrorCode.SetInResultConditionedBlock])
    "SetReusedName"
    |> expect gen1 (ErrorCode.LocalVariableAlreadyExists :: List.replicate 2 ErrorCode.SetInResultConditionedBlock)

[<Fact>]
let ``QPRGen1 restricts non-local mutable set from Result elif`` () =
    [ "ElifSet"
      "ElifElifSet" ]
    |> List.iter (expect gen1 [ErrorCode.SetInResultConditionedBlock])

[<Fact>]
let ``QPRGen1 restricts non-local mutable set from Result else`` () =
    [ "ResultAsBoolOpElseSet"
      "ElifElseSet" ]
    |> List.iter (expect gen1 [ErrorCode.SetInResultConditionedBlock])

[<Fact>]
let ``QPRGen1 restricts empty Result if function`` () =
    [ "EmptyIf"
      "EmptyIfNeq" ]
    |> List.iter (expect gen1 [ErrorCode.ResultComparisonNotInOperationIf])

[<Fact>]
let ``QPRGen1 allows empty Result if operation`` () =
    [ "EmptyIfOp"
      "EmptyIfNeqOp" ]
    |> List.iter (expect gen1 [])

[<Fact>]
let ``QPRGen1 allows operation call from Result if`` () =
    [ "Reset"
      "ResetNeq"
      "OverrideGen1ToUnknown"
      "OverrideGen1ToGen0"
      "ExplicitGen1" ]
    |> List.iter (expect gen1 [])

[<Fact>]
let ``Unknown allows all library calls and references`` () =
    [ "CallLibraryGen0"
      "ReferenceLibraryGen0"
      "CallLibraryGen1"
      "ReferenceLibraryGen1"
      "CallLibraryUnknown"
      "ReferenceLibraryUnknown" ]
    |> List.iter (expect unknown [])

[<Fact>]
let ``QPRGen1 restricts library calls and references`` () =
    [ "CallLibraryGen0"
      "CallLibraryGen1" ]
    |> List.iter (expect gen1 [])
    [ "CallLibraryUnknown"
      "ReferenceLibraryUnknown" ]
    |> List.iter (expect gen1 [ErrorCode.UnsupportedCapability])

[<Fact>]
let ``QPRGen0 restricts library calls and references`` () =
    [ "CallLibraryGen0"
      "ReferenceLibraryGen0" ]
    |> List.iter (expect gen0 [])
    [ "CallLibraryGen1"
      "ReferenceLibraryGen1"
      "CallLibraryUnknown"
      "ReferenceLibraryUnknown" ]
    |> List.iter (expect gen0 [ErrorCode.UnsupportedCapability])
