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

/// Asserts that the tester disallows the given test case.
let private restricts (tester : CompilerTests) name =
    tester.Verify (testName name, [Error ErrorCode.UnsupportedResultComparison])

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
      "EmptyIf"
      "EmptyIfNeq"
      "Reset"
      "ResetNeq" ]

let [<Fact>] ``Unknown allows all Result comparison`` () = List.iter (allows unknown) all
let [<Fact>] ``QPRGen0 restricts all Result comparison`` () = List.iter (restricts gen0) all

[<Fact(Skip = "QPRGen1 verification is not implemented yet")>]
let ``QPRGen1 restricts Result comparison in functions`` () =
    restricts gen1 "ResultAsBool"
    restricts gen1 "ResultAsBoolNeq"

[<Fact(Skip = "QPRGen1 verification is not implemented yet")>]
let ``QPRGen1 restricts non-if Result comparison in operations`` () =
    restricts gen1 "ResultAsBoolOp"
    restricts gen1 "ResultAsBoolNeqOp"

[<Fact(Skip = "QPRGen1 verification is not implemented yet")>]
let ``QPRGen1 restricts return from Result if`` () =
    restricts gen1 "ResultAsBoolOpReturnIf"
    restricts gen1 "ResultAsBoolNeqOpReturnIf"

[<Fact(Skip = "QPRGen1 verification is not implemented yet")>]
let ``QPRGen1 restricts mutable set from Result if`` () =
    restricts gen1 "ResultAsBoolOpSetIf"
    restricts gen1 "ResultAsBoolNeqOpSetIf"    

[<Fact>]
let ``QPRGen1 allows empty Result if`` () =
    allows gen1 "EmptyIf"
    allows gen1 "EmptyIfNeq"

[<Fact>]
let ``QPRGen1 allows operation call from Result if`` () =
    allows gen1 "Reset"
    allows gen1 "ResetNeq"
