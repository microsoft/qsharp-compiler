module Microsoft.Quantum.QsCompiler.Testing.CapabilityInferenceTests

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.ReservedKeywords.AssemblyConstants
open Microsoft.Quantum.QsCompiler.SyntaxProcessing
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Xunit

/// A mapping of all callables in the capability verification tests, after inferring capabilities.
let private callables =
    CompilerTests.Compile ("TestCases", [ "CapabilityVerification.qs" ])
    |> fun compilation -> compilation.BuiltCompilation
    |> CapabilityInference.InferCapabilities
    |> fun compilation -> compilation.Namespaces
    |> GlobalCallableResolutions

/// Asserts that the inferred capability of the callable with the given name matches the expected capability.
let private expect capability name =
    let actual =
        callables.[CapabilityVerificationTests.testName name].Attributes
        |> QsNullable<_>.Choose BuiltIn.GetCapability
    Assert.Equal<RuntimeCapabilities> ([ capability ], actual)

[<Fact>]
let ``Infers QPRGen0`` () =
    [ "NoOp"
      "ResultTuple"
      "ResultArray"
      "ResetOverrideLow" ]
    |> List.iter (expect RuntimeCapabilities.QPRGen0)

[<Fact>]
let ``Infers QPRGen1`` () =
    [ "SetLocal"
      "EmptyIfOp"
      "EmptyIfNeqOp"
      "Reset"
      "ResetNeq" ]
    |> List.iter (expect RuntimeCapabilities.QPRGen1)

[<Fact>]
let ``Infers Unknown`` () =
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
      "SetReusedName"
      "SetTuple"
      "EmptyIf"
      "EmptyIfNeq"
      "ResetOverrideHigh" ]
    |> List.iter (expect RuntimeCapabilities.Unknown)
