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
    let actual = SymbolResolution.TryGetRequiredCapability callables.[fullName].Attributes
    Assert.Contains(capability, actual)

[<Fact>]
let ``Infers BasicQuantumFunctionality by source code`` () =
    [
        "NoOp"

        // Tuples and arrays don't support equality, so they are inferred as BasicQuantumFunctionality for now. If tuple
        // and array equality is supported, ResultTuple and ResultArray should be inferred as FullComputation instead.
        "ResultTuple"
        "ResultArray"
    ]
    |> List.iter (expect BasicQuantumFunctionality)

[<Fact>]
let ``Infers BasicMeasurementFeedback by source code`` () =
    [ "SetLocal"; "EmptyIfOp"; "EmptyIfNeqOp"; "Reset"; "ResetNeq" ]
    |> List.iter (expect BasicMeasurementFeedback)

[<Fact>]
let ``Infers FullComputation by source code`` () =
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
    |> List.iter (expect FullComputation)

[<Fact>]
let ``Allows overriding capabilities with attribute`` () =
    expect BasicQuantumFunctionality "OverrideBmfToBqf"

    [ "OverrideBqfToBmf"; "OverrideFullToBmf"; "ExplicitBmf" ]
    |> List.iter (expect BasicMeasurementFeedback)

    expect FullComputation "OverrideBmfToFull"

[<Fact>]
let ``Infers single dependency`` () =
    [ "CallBmfA"; "CallBmfB" ] |> List.iter (expect BasicMeasurementFeedback)

[<Fact>]
let ``Infers two side-by-side dependencies`` () =
    expect BasicMeasurementFeedback "CallBmfFullB"
    [ "CallBmfFullA"; "CallBmfFullC" ] |> List.iter (expect FullComputation)

[<Fact>]
let ``Infers two chained dependencies`` () =
    [ "CallFullA"; "CallFullB" ] |> List.iter (expect FullComputation)
    expect BasicMeasurementFeedback "CallFullC"

[<Fact>]
let ``Allows safe override`` () =
    [ "CallFullOverrideA"; "CallFullOverrideB" ] |> List.iter (expect FullComputation)

    expect BasicMeasurementFeedback "CallFullOverrideC"

[<Fact>]
let ``Allows unsafe override`` () =
    [ "CallBmfOverrideA"; "CallBmfOverrideB" ] |> List.iter (expect BasicMeasurementFeedback)

    expect FullComputation "CallBmfOverrideC"

[<Fact>]
let ``Infers with direct recursion`` () =
    expect BasicMeasurementFeedback "BmfRecursion"

[<Fact>]
let ``Infers with indirect recursion`` () =
    [ "BmfRecursion3A"; "BmfRecursion3B"; "BmfRecursion3C" ]
    |> List.iter (expect BasicMeasurementFeedback)

[<Fact>]
let ``Infers with uncalled reference`` () =
    [ "ReferenceBmfA"; "ReferenceBmfB" ] |> List.iter (expect BasicMeasurementFeedback)
