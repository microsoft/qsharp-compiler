// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.CapabilityVerificationTests

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.ReservedKeywords.AssemblyConstants
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Xunit

/// Compiles the capability verification test cases using the given capability.
let private compile capabilities =
    CompilerTests.Compile ("TestCases", ["CapabilityVerification.qs"], capabilities = capabilities)

/// The unknown capability tester.
let private unknown = CompilerTests (compile RuntimeCapabilities.Unknown)

/// The QPRGen0 capability tester.
let private gen0 = CompilerTests (compile RuntimeCapabilities.QPRGen0)

/// The QPRGen1 capability tester.
let private gen1 = CompilerTests (compile RuntimeCapabilities.QPRGen1)

/// The qualified name for the test case name.
let private testName name =
    QsQualifiedName.New (NonNullable<_>.New "Microsoft.Quantum.Testing.CapabilityVerification",
                         NonNullable<_>.New name)

/// Asserts that the tester allows the given test case.
let private allows (tester : CompilerTests) name = tester.Verify (testName name, List.empty<DiagnosticItem>)

/// Asserts that the tester does not allow the given test case.
let private restricts (tester : CompilerTests) errorCode name = tester.Verify (testName name, [Error errorCode])

/// The names of all test cases.
let private all =
    [ "ResultAsBool"
      "ResultAsBoolNeq"
      "ResultAsBoolOp"
      "ResultAsBoolNeqOp"
      "ResultAsBoolOpReturnIf"
      "ResultAsBoolNeqOpReturnIf"
      "ResultAsBoolOpSetIf"
      "ResultAsBoolNeqOpSetIf"
      "ResultAsBoolOpElseSet"
      "ElifSet"
      "ElifElifSet"
      "ElifElseSet"
      "EmptyIf"
      "EmptyIfNeq"
      "EmptyIfOp"
      "EmptyIfNeqOp"
      "Reset"
      "ResetNeq" ]

let [<Fact>] ``Unknown allows all Result comparison`` () = List.iter (allows unknown) all

[<Fact>]
let ``QPRGen0 restricts all Result comparison`` () =
    List.iter (restricts gen0 ErrorCode.UnsupportedResultComparison) all

[<Fact (Skip = "QPRGen1 verification is not implemented yet")>]
let ``QPRGen1 restricts Result comparison in functions`` () =
    restricts gen1 ErrorCode.ResultComparisonNotInOperationIf "ResultAsBool"
    restricts gen1 ErrorCode.ResultComparisonNotInOperationIf "ResultAsBoolNeq"

[<Fact (Skip = "QPRGen1 verification is not implemented yet")>]
let ``QPRGen1 restricts non-if Result comparison in operations`` () =
    restricts gen1 ErrorCode.ResultComparisonNotInOperationIf "ResultAsBoolOp"
    restricts gen1 ErrorCode.ResultComparisonNotInOperationIf "ResultAsBoolNeqOp"

[<Fact (Skip = "QPRGen1 verification is not implemented yet")>]
let ``QPRGen1 restricts return from Result if`` () =
    restricts gen1 ErrorCode.ResultComparisonNotInOperationIf "ResultAsBoolOpReturnIf"
    restricts gen1 ErrorCode.ResultComparisonNotInOperationIf "ResultAsBoolNeqOpReturnIf"

[<Fact (Skip = "QPRGen1 verification is not implemented yet")>]
let ``QPRGen1 restricts mutable set from Result if`` () =
    restricts gen1 ErrorCode.ResultComparisonNotInOperationIf "ResultAsBoolOpSetIf"
    restricts gen1 ErrorCode.ResultComparisonNotInOperationIf "ResultAsBoolNeqOpSetIf"

[<Fact (Skip = "QPRGen1 verification is not implemented yet")>]
let ``QPRGen1 restricts mutable set from Result elif`` () =
    restricts gen1 ErrorCode.ResultComparisonNotInOperationIf "ElifSet"
    restricts gen1 ErrorCode.ResultComparisonNotInOperationIf "ElifElifSet"

[<Fact (Skip = "QPRGen1 verification is not implemented yet")>]
let ``QPRGen1 restricts mutable set from Result else`` () =
    restricts gen1 ErrorCode.ResultComparisonNotInOperationIf "ResultAsBoolOpElseSet"
    restricts gen1 ErrorCode.ResultComparisonNotInOperationIf "ElifElseSet"

[<Fact>]
let ``QPRGen1 restricts empty Result if function`` () =
    restricts gen1 ErrorCode.ResultComparisonNotInOperationIf "EmptyIf"
    restricts gen1 ErrorCode.ResultComparisonNotInOperationIf "EmptyIfNeq"

[<Fact>]
let ``QPRGen1 allows empty Result if operation`` () =
    allows gen1 "EmptyIfOp"
    allows gen1 "EmptyIfNeqOp"

[<Fact>]
let ``QPRGen1 allows operation call from Result if`` () =
    allows gen1 "Reset"
    allows gen1 "ResetNeq"
