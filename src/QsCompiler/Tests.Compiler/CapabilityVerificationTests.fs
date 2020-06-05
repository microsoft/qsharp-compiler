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

/// The empty diagnostics list.
let private success = List.empty<DiagnosticItem>

[<Fact>]
let ``Unknown supports result comparison in functions`` () =
    unknown.Verify (testName "ResultAsBool", success)

[<Fact>]
let ``QPRGen0 does not support result comparison in functions`` () =
    gen0.Verify (testName "ResultAsBool", [Error ErrorCode.UnsupportedResultComparison])

[<Fact (Skip = "TODO")>]
let ``QPRGen1 does not support result comparison in functions`` () =
    gen1.Verify (testName "ResultAsBool", [Error ErrorCode.UnsupportedResultComparison])
