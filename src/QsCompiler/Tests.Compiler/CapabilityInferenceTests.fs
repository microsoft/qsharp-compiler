// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.CapabilityInferenceTests

open System.IO
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Xunit

/// A mapping of all callables in the capability verification tests, after inferring capabilities.
let private callables =
    let compilation =
        CompilerTests.Compile(
            "TestCases",
            [ "CapabilityTests/Verification.qs"; "CapabilityTests/Inference.qs" ],
            references = [ File.ReadAllLines "ReferenceTargets.txt" |> Array.item 2 ]
        )

    GlobalCallableResolutions (Capabilities.infer compilation.BuiltCompilation).Namespaces

/// Asserts that the inferred capability of the callable with the given name matches the expected capability.
let private expect capability name =
    let fullName = CapabilityVerificationTests.testName name
    let actual = SymbolResolution.TryGetRequiredCapability callables[fullName].Attributes
    Assert.Contains(capability, actual)

let private createCapability opacity classical =
    RuntimeCapability.withResultOpacity opacity RuntimeCapability.bottom
    |> RuntimeCapability.withClassical classical

[<Fact>]
let ``Infers BasicQuantumFunctionality from syntax`` () =
    [
        "NoOp"

        // Tuples and arrays don't support equality, so they are inferred as BasicQuantumFunctionality for now. If tuple
        // and array equality is supported, ResultTuple and ResultArray should be inferred as FullComputation instead.
        "ResultTuple"
        "ResultArray"
    ]
    |> List.iter (createCapability ResultOpacity.opaque ClassicalCapability.limited |> expect)

[<Fact>]
let ``Infers BasicMeasurementFeedback from syntax`` () =
    [ "SetLocal"; "EmptyIfOp"; "EmptyIfNeqOp"; "Reset"; "ResetNeq" ]
    |> List.iter (createCapability ResultOpacity.controlled ClassicalCapability.limited |> expect)

[<Fact>]
let ``Infers FullComputation from syntax`` () =
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
        "SetReusedName"
        "SetTuple"
        "EmptyIf"
        "EmptyIfNeq"
    ]
    |> List.iter (createCapability ResultOpacity.transparent ClassicalCapability.limited |> expect)

[<Fact>]
let ``Infers unlimited classical capability from syntax`` () =
    [ "Recursion1"; "Recursion2A"; "Recursion2B" ]
    |> List.iter (createCapability ResultOpacity.opaque ClassicalCapability.unlimited |> expect)

[<Fact>]
let ``Allows overriding capabilities with attribute`` () =
    expect (createCapability ResultOpacity.opaque ClassicalCapability.limited) "OverrideBmfToBqf"

    [ "OverrideBqfToBmf"; "OverrideFullToBmf"; "ExplicitBmf" ]
    |> List.iter (createCapability ResultOpacity.controlled ClassicalCapability.limited |> expect)

    expect (createCapability ResultOpacity.transparent ClassicalCapability.limited) "OverrideBmfToFull"

[<Fact>]
let ``Infers single dependency`` () =
    [ "CallBmfA"; "CallBmfB" ]
    |> List.iter (createCapability ResultOpacity.controlled ClassicalCapability.limited |> expect)

[<Fact>]
let ``Infers two side-by-side dependencies`` () =
    expect (createCapability ResultOpacity.controlled ClassicalCapability.limited) "CallBmfFullB"

    [ "CallBmfFullA"; "CallBmfFullC" ]
    |> List.iter (createCapability ResultOpacity.transparent ClassicalCapability.limited |> expect)

[<Fact>]
let ``Infers two chained dependencies`` () =
    [ "CallFullA"; "CallFullB" ]
    |> List.iter (createCapability ResultOpacity.transparent ClassicalCapability.limited |> expect)

    expect (createCapability ResultOpacity.controlled ClassicalCapability.limited) "CallFullC"

[<Fact>]
let ``Allows safe override`` () =
    [ "CallFullOverrideA"; "CallFullOverrideB" ]
    |> List.iter (createCapability ResultOpacity.transparent ClassicalCapability.limited |> expect)

    expect (createCapability ResultOpacity.controlled ClassicalCapability.limited) "CallFullOverrideC"

[<Fact>]
let ``Allows unsafe override`` () =
    [ "CallBmfOverrideA"; "CallBmfOverrideB" ]
    |> List.iter (createCapability ResultOpacity.controlled ClassicalCapability.limited |> expect)

    expect (createCapability ResultOpacity.transparent ClassicalCapability.limited) "CallBmfOverrideC"

[<Fact>]
let ``Infers with direct recursion`` () =
    expect (createCapability ResultOpacity.controlled ClassicalCapability.unlimited) "BmfRecursion"

[<Fact>]
let ``Infers with indirect recursion`` () =
    [ "BmfRecursion3A"; "BmfRecursion3B"; "BmfRecursion3C" ]
    |> List.iter (createCapability ResultOpacity.controlled ClassicalCapability.unlimited |> expect)

[<Fact>]
let ``Infers with uncalled reference`` () =
    [ "ReferenceBmfA"; "ReferenceBmfB" ]
    |> List.iter (createCapability ResultOpacity.controlled ClassicalCapability.limited |> expect)
