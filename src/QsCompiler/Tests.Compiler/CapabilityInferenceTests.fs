module Microsoft.Quantum.QsCompiler.Testing.CapabilityInferenceTests

open System.IO
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Xunit

/// A mapping of all callables in the capability verification tests, after inferring capabilities.
let private callables =
    CompilerTests.Compile(
        "TestCases",
        [ "CapabilityTests/Verification.qs"; "CapabilityTests/Inference.qs" ],
        references = [ (File.ReadAllLines "ReferenceTargets.txt")[2] ]
    )
    |> fun compilation -> compilation.BuiltCompilation
    |> Capabilities.infer
    |> fun compilation -> compilation.Namespaces
    |> GlobalCallableResolutions

/// Asserts that the inferred capability of the callable with the given name matches the expected capability.
let private expect capability name =
    let fullName = CapabilityVerificationTests.testName name
    let actual = SymbolResolution.TryGetRequiredCapability callables[fullName].Attributes
    Assert.Contains(capability, actual)

let private runtimeCapability opacity classical =
    RuntimeCapability.withResultOpacity opacity RuntimeCapability.bottom
    |> RuntimeCapability.withClassical classical

[<Fact>]
let ``Infers BasicQuantumFunctionality by source code`` () =
    [
        "NoOp"

        // Tuples and arrays don't support equality, so they are inferred as BasicQuantumFunctionality for now. If tuple
        // and array equality is supported, ResultTuple and ResultArray should be inferred as FullComputation instead.
        "ResultTuple"
        "ResultArray"
    ]
    |> List.iter (runtimeCapability ResultOpacity.opaque ClassicalCapability.empty |> expect)

[<Fact>]
let ``Infers BasicMeasurementFeedback by source code`` () =
    [ "SetLocal"; "EmptyIfOp"; "EmptyIfNeqOp"; "Reset"; "ResetNeq" ]
    |> List.iter (runtimeCapability ResultOpacity.controlled ClassicalCapability.empty |> expect)

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
    |> List.iter (runtimeCapability ResultOpacity.transparent ClassicalCapability.empty |> expect)

[<Fact>]
let ``Allows overriding capabilities with attribute`` () =
    expect RuntimeCapability.BasicQuantumFunctionality "OverrideBmfToBqf"

    [ "OverrideBqfToBmf"; "OverrideFullToBmf"; "ExplicitBmf" ]
    |> List.iter (expect RuntimeCapability.BasicMeasurementFeedback)

    expect RuntimeCapability.FullComputation "OverrideBmfToFull"

[<Fact>]
let ``Infers single dependency`` () =
    [ "CallBmfA"; "CallBmfB" ]
    |> List.iter (runtimeCapability ResultOpacity.controlled ClassicalCapability.empty |> expect)

[<Fact>]
let ``Infers two side-by-side dependencies`` () =
    expect (runtimeCapability ResultOpacity.controlled ClassicalCapability.empty) "CallBmfFullB"

    [ "CallBmfFullA"; "CallBmfFullC" ]
    |> List.iter (runtimeCapability ResultOpacity.transparent ClassicalCapability.empty |> expect)

[<Fact>]
let ``Infers two chained dependencies`` () =
    [ "CallFullA"; "CallFullB" ]
    |> List.iter (runtimeCapability ResultOpacity.transparent ClassicalCapability.empty |> expect)

    expect (runtimeCapability ResultOpacity.controlled ClassicalCapability.empty) "CallFullC"

[<Fact>]
let ``Allows safe override`` () =
    [ "CallFullOverrideA"; "CallFullOverrideB" ] |> List.iter (expect RuntimeCapability.FullComputation)

    expect (runtimeCapability ResultOpacity.controlled ClassicalCapability.empty) "CallFullOverrideC"

[<Fact>]
let ``Allows unsafe override`` () =
    [ "CallBmfOverrideA"; "CallBmfOverrideB" ]
    |> List.iter (expect RuntimeCapability.BasicMeasurementFeedback)

    expect (runtimeCapability ResultOpacity.transparent ClassicalCapability.empty) "CallBmfOverrideC"

[<Fact>]
let ``Infers with direction recursion`` () =
    expect (runtimeCapability ResultOpacity.controlled ClassicalCapability.full) "BmfRecursion"

[<Fact>]
let ``Infers with indirect recursion`` () =
    [ "BmfRecursion3A"; "BmfRecursion3B"; "BmfRecursion3C" ]
    |> List.iter (runtimeCapability ResultOpacity.controlled ClassicalCapability.full |> expect)

[<Fact>]
let ``Infers with uncalled reference`` () =
    [ "ReferenceBmfA"; "ReferenceBmfB" ]
    |> List.iter (runtimeCapability ResultOpacity.controlled ClassicalCapability.empty |> expect)
