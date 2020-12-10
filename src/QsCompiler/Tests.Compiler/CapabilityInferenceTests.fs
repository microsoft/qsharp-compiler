module Microsoft.Quantum.QsCompiler.Testing.CapabilityInferenceTests

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.ReservedKeywords.AssemblyConstants
open Microsoft.Quantum.QsCompiler.SyntaxProcessing
open Microsoft.Quantum.QsCompiler.SyntaxTree
open System.IO
open Xunit

/// A mapping of all callables in the capability verification tests, after inferring capabilities.
let private callables =
    CompilerTests.Compile
        ("TestCases",
         [ "CapabilityTests/Verification.qs"; "CapabilityTests/Inference.qs" ],
         references = [ (File.ReadAllLines "ReferenceTargets.txt").[1] ])
    |> fun compilation -> compilation.BuiltCompilation
    |> CapabilityInference.InferCapabilities
    |> fun compilation -> compilation.Namespaces
    |> GlobalCallableResolutions

/// Asserts that the inferred capability of the callable with the given name matches the expected capability.
let private expect capability name =
    let fullName = CapabilityVerificationTests.testName name
    let actual = BuiltIn.TryGetRequiredCapability callables.[fullName].Attributes
    Assert.Equal(Value capability, actual)

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
let ``Infers with direction recursion`` () =
    expect BasicMeasurementFeedback "BmfRecursion"

[<Fact>]
let ``Infers with indirect recursion`` () =
    [ "BmfRecursion3A"; "BmfRecursion3B"; "BmfRecursion3C" ]
    |> List.iter (expect BasicMeasurementFeedback)

[<Fact>]
let ``Infers with uncalled reference`` () =
    [ "ReferenceBmfA"; "ReferenceBmfB" ] |> List.iter (expect BasicMeasurementFeedback)
