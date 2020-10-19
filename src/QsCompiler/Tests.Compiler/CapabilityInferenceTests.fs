module Microsoft.Quantum.QsCompiler.Testing.CapabilityInferenceTests

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.ReservedKeywords.AssemblyConstants
open Microsoft.Quantum.QsCompiler.SyntaxProcessing
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Xunit

/// A mapping of all callables in the capability verification tests, after inferring capabilities.
let private callables =
    CompilerTests.Compile ("TestCases", [ "CapabilityTests/Verification.qs"; "CapabilityTests/Inference.qs" ])
    |> fun compilation -> compilation.BuiltCompilation
    |> CapabilityInference.InferCapabilities
    |> fun compilation -> compilation.Namespaces
    |> GlobalCallableResolutions

/// Asserts that the inferred capability of the callable with the given name matches the expected capability.
let private expect capability name =
    let actual =
        callables.[CapabilityVerificationTests.testName name].Attributes
        |> QsNullable<_>.Choose BuiltIn.RequiredCapability
    Assert.Equal<RuntimeCapabilities> ([ capability ], actual)

[<Fact>]
let ``Infers QPRGen0 by source code`` () =
    [ "NoOp"

      // Tuples and arrays don't support equality, so they are inferred as QPRGen0 for now. If tuple and array equality
      // is supported, ResultTuple and ResultArray should be inferred as Unknown instead.
      "ResultTuple"
      "ResultArray" ]
    |> List.iter (expect RuntimeCapabilities.QPRGen0)

[<Fact>]
let ``Infers QPRGen1 by source code`` () =
    [ "SetLocal"
      "EmptyIfOp"
      "EmptyIfNeqOp"
      "Reset"
      "ResetNeq" ]
    |> List.iter (expect RuntimeCapabilities.QPRGen1)

[<Fact>]
let ``Infers Unknown by source code`` () =
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
      "EmptyIfNeq" ]
    |> List.iter (expect RuntimeCapabilities.Unknown)

[<Fact>]
let ``Allows overriding capabilities with attribute`` () =
    expect RuntimeCapabilities.QPRGen0 "OverrideGen1ToGen0"
    [ "OverrideGen0ToGen1"
      "OverrideUnknownToGen1"
      "ExplicitGen1" ]
    |> List.iter (expect RuntimeCapabilities.QPRGen1)
    expect RuntimeCapabilities.Unknown "OverrideGen1ToUnknown"

[<Fact>]
let ``Infers single dependency`` () =
    [ "CallGen1A"
      "CallGen1B" ]
    |> List.iter (expect RuntimeCapabilities.QPRGen1)

[<Fact>]
let ``Infers two side-by-side dependencies`` () =
    expect RuntimeCapabilities.QPRGen1 "CallGen1UnknownB"
    [ "CallGen1UnknownA"
      "CallGen1UnknownC" ]
    |> List.iter (expect RuntimeCapabilities.Unknown)

[<Fact>]
let ``Infers two chained dependencies`` () =
    [ "CallUnknownA"
      "CallUnknownB" ]
    |> List.iter (expect RuntimeCapabilities.Unknown)
    expect RuntimeCapabilities.QPRGen1 "CallUnknownC"

[<Fact>]
let ``Allows safe override`` () =
    [ "CallUnknownOverrideA"
      "CallUnknownOverrideB" ]
    |> List.iter (expect RuntimeCapabilities.Unknown)
    expect RuntimeCapabilities.QPRGen1 "CallUnknownOverrideC"

[<Fact>]
let ``Allows unsafe override`` () =
    [ "CallQPRGen1OverrideA"
      "CallQPRGen1OverrideB" ]
    |> List.iter (expect RuntimeCapabilities.QPRGen1)
    expect RuntimeCapabilities.Unknown "CallQPRGen1OverrideC"

[<Fact>]
let ``Infers with direction recursion`` () =
    expect RuntimeCapabilities.QPRGen1 "Gen1Recursion"

[<Fact>]
let ``Infers with indirect recursion`` () =
    [ "Gen1Recursion3A"
      "Gen1Recursion3B"
      "Gen1Recursion3C" ]
    |> List.iter (expect RuntimeCapabilities.QPRGen1)

[<Fact>]
let ``Infers with uncalled reference`` () =
    [ "ReferenceGen1A"
      "ReferenceGen1B" ]
    |> List.iter (expect RuntimeCapabilities.QPRGen1)
