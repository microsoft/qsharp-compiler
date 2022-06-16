// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System.IO
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations
open Xunit

type OutputRecordingTests() =

    let compileEntryPointWrappingTests testNumber =
        let coreFiles = [ Path.Combine("TestCases", "QirTests", "QirCore.qs") |> Path.GetFullPath ]
        let srcChunks = TestUtils.readAndChunkSourceFile "OutputRecording.qs"
        srcChunks.Length >= testNumber + 1 |> Assert.True
        let shared = srcChunks.[0]

        let compilationDataStructures =
            TestUtils.buildContentWithFiles (shared + srcChunks.[testNumber]) coreFiles

        let processedCompilation = AddOutputRecording.Apply(compilationDataStructures.BuiltCompilation, true)
        Assert.NotNull processedCompilation
        processedCompilation

    let wrapperAPINamespaceName = BuiltIn.Message.FullName.Namespace

    let makeVal n = sprintf "__rtrnVal%i__" n

    let makeRecordBool n =
        sprintf "%s.BooleanRecordOutput(%s);" wrapperAPINamespaceName (makeVal n)

    let makeRecordInt n =
        sprintf "%s.IntegerRecordOutput(%s);" wrapperAPINamespaceName (makeVal n)

    let makeRecordDouble n =
        sprintf "%s.DoubleRecordOutput(%s);" wrapperAPINamespaceName (makeVal n)

    let makeRecordResult n =
        sprintf "%s.ResultRecordOutput(%s);" wrapperAPINamespaceName (makeVal n)

    let makeRecordStartTuple = sprintf "%s.TupleStartRecordOutput();" wrapperAPINamespaceName

    let makeRecordEndTuple = sprintf "%s.TupleEndRecordOutput();" wrapperAPINamespaceName

    let makeRecordStartArray = sprintf "%s.ArrayStartRecordOutput();" wrapperAPINamespaceName

    let makeRecordEndArray = sprintf "%s.ArrayEndRecordOutput();" wrapperAPINamespaceName

    let makeArrayLoop n1 n2 =
        sprintf "for %s in %s {" (makeVal n1) (makeVal n2)

    let runWrappingTest result expectedContent =
        let original = TestUtils.getCallableWithName result Signatures.OutputRecordingNS "Foo"

        Assert.False(
            (original.Attributes |> Seq.exists BuiltIn.MarksEntryPoint),
            "The original entry point is still an entry point."
        )

        let generated = TestUtils.getCallableWithName result Signatures.OutputRecordingNS "Foo__Main"

        Assert.True(
            (generated.Attributes |> Seq.exists BuiltIn.MarksEntryPoint),
            "The entry point wrapper is not an entry point."
        )

        let lines =
            generated
            |> TestUtils.getBodyFromCallable
            |> TestUtils.getLinesFromSpecialization
            |> Seq.map (fun x -> x.Trim())
            |> Seq.toArray

        Assert.True((lines = expectedContent), "The generated callable did not have the expected content.")

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Return Int``() =
        let result = compileEntryPointWrappingTests 1

        let captureLine = sprintf "let %s = %s.%s();" (makeVal 0) Signatures.OutputRecordingNS "Foo"
        let expectedContent = [| captureLine; makeRecordInt 0 |]

        runWrappingTest result expectedContent

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Return Bool``() =
        let result = compileEntryPointWrappingTests 2

        let captureLine = sprintf "let %s = %s.%s();" (makeVal 0) Signatures.OutputRecordingNS "Foo"
        let expectedContent = [| captureLine; makeRecordBool 0 |]

        runWrappingTest result expectedContent

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Return Double``() =
        let result = compileEntryPointWrappingTests 3

        let captureLine = sprintf "let %s = %s.%s();" (makeVal 0) Signatures.OutputRecordingNS "Foo"
        let expectedContent = [| captureLine; makeRecordDouble 0 |]

        runWrappingTest result expectedContent

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Return Result``() =
        let result = compileEntryPointWrappingTests 4

        let captureLine = sprintf "let %s = %s.%s();" (makeVal 0) Signatures.OutputRecordingNS "Foo"
        let expectedContent = [| captureLine; makeRecordResult 0 |]

        runWrappingTest result expectedContent

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Return Tuple``() =
        let result = compileEntryPointWrappingTests 5

        let captureLine =
            sprintf "let (%s, %s) = %s.%s();" (makeVal 0) (makeVal 1) Signatures.OutputRecordingNS "Foo"

        let expectedContent =
            [|
                captureLine
                makeRecordStartTuple
                makeRecordInt 0
                makeRecordBool 1
                makeRecordEndTuple
            |]

        runWrappingTest result expectedContent

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Return Nested Tuple``() =
        let result = compileEntryPointWrappingTests 6

        let captureLine =
            sprintf
                "let (%s, (%s, %s)) = %s.%s();"
                (makeVal 0)
                (makeVal 1)
                (makeVal 2)
                Signatures.OutputRecordingNS
                "Foo"

        let expectedContent =
            [|
                captureLine
                makeRecordStartTuple
                makeRecordInt 0
                makeRecordStartTuple
                makeRecordBool 1
                makeRecordDouble 2
                makeRecordEndTuple
                makeRecordEndTuple
            |]

        runWrappingTest result expectedContent

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Return Array``() =
        let result = compileEntryPointWrappingTests 7

        let captureLine = sprintf "let %s = %s.%s();" (makeVal 0) Signatures.OutputRecordingNS "Foo"

        let expectedContent =
            [|
                captureLine
                makeRecordStartArray
                makeArrayLoop 1 0
                makeRecordInt 1
                "}"
                makeRecordEndArray
            |]

        runWrappingTest result expectedContent

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Return Empty Array``() =
        let result = compileEntryPointWrappingTests 8

        let captureLine = sprintf "let %s = %s.%s();" (makeVal 0) Signatures.OutputRecordingNS "Foo"

        let expectedContent =
            [|
                captureLine
                makeRecordStartArray
                makeArrayLoop 1 0
                makeRecordInt 1
                "}"
                makeRecordEndArray
            |]

        runWrappingTest result expectedContent

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Return Jagged Array``() =
        let result = compileEntryPointWrappingTests 9

        let captureLine = sprintf "let %s = %s.%s();" (makeVal 0) Signatures.OutputRecordingNS "Foo"

        let expectedContent =
            [|
                captureLine
                makeRecordStartArray
                makeArrayLoop 1 0
                makeRecordStartArray
                makeArrayLoop 2 1
                makeRecordInt 2
                "}"
                makeRecordEndArray
                "}"
                makeRecordEndArray
            |]

        runWrappingTest result expectedContent

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Return Array in Tuple``() =
        let result = compileEntryPointWrappingTests 10

        let captureLine =
            sprintf "let (%s, %s) = %s.%s();" (makeVal 0) (makeVal 1) Signatures.OutputRecordingNS "Foo"

        let expectedContent =
            [|
                captureLine
                makeRecordStartTuple
                makeRecordResult 0
                makeRecordStartArray
                makeArrayLoop 2 1
                makeRecordInt 2
                "}"
                makeRecordEndArray
                makeRecordEndTuple
            |]

        runWrappingTest result expectedContent

    [<Fact>]
    [<Trait("Category", "Parameters")>]
    member this.``Multiple Parameter Entry Point``() =
        let result = compileEntryPointWrappingTests 11

        let generated = TestUtils.getCallableWithName result Signatures.OutputRecordingNS "Foo__Main"

        let generatedParameters =
            generated.ArgumentTuple.Items
            |> Seq.map (fun x ->
                match x.VariableName with
                | ValidName name -> name, x.Type.Resolution
                | _ -> failwith "The generated callable did not have the expected parameters.")
            |> Seq.toArray

        let expectedParameters = [| ("a", ResolvedTypeKind.Int); ("b", ResolvedTypeKind.Bool) |]

        Assert.True(
            (generatedParameters = expectedParameters),
            "The generated callable did not have the expected parameters."
        )

        let captureLine = sprintf "let %s = %s.%s(a, b);" (makeVal 0) Signatures.OutputRecordingNS "Foo"
        let expectedContent = [| captureLine; makeRecordInt 0 |]

        runWrappingTest result expectedContent

    [<Fact>]
    [<Trait("Category", "Multiple Entry Points")>]
    member this.``Multiple Entry Points``() =
        let result = compileEntryPointWrappingTests 12

        let originalFoo = TestUtils.getCallableWithName result Signatures.OutputRecordingNS "Foo"

        Assert.False(
            (originalFoo.Attributes |> Seq.exists BuiltIn.MarksEntryPoint),
            "The original entry point 'Foo' is still an entry point."
        )

        let generatedFoo = TestUtils.getCallableWithName result Signatures.OutputRecordingNS "Foo__Main"

        Assert.True(
            (generatedFoo.Attributes |> Seq.exists BuiltIn.MarksEntryPoint),
            "The entry point wrapper for 'Foo' is not an entry point."
        )

        let originalBar = TestUtils.getCallableWithName result Signatures.OutputRecordingNS "Bar"

        Assert.False(
            (originalBar.Attributes |> Seq.exists BuiltIn.MarksEntryPoint),
            "The original entry point 'Bar' is still an entry point."
        )

        let generatedBar = TestUtils.getCallableWithName result Signatures.OutputRecordingNS "Bar__Main"

        Assert.True(
            (generatedBar.Attributes |> Seq.exists BuiltIn.MarksEntryPoint),
            "The entry point wrapper for 'Bar' is not an entry point."
        )

    [<Fact>]
    [<Trait("Category", "Multiple Entry Points")>]
    member this.``Don't Wrap Unit Entry Points``() =
        let result = compileEntryPointWrappingTests 13

        let originalFoo = TestUtils.getCallableWithName result Signatures.OutputRecordingNS "Foo"

        Assert.False(
            (originalFoo.Attributes |> Seq.exists BuiltIn.MarksEntryPoint),
            "The original entry point 'Foo' is still an entry point."
        )

        let generatedFoo = TestUtils.getCallableWithName result Signatures.OutputRecordingNS "Foo__Main"

        Assert.True(
            (generatedFoo.Attributes |> Seq.exists BuiltIn.MarksEntryPoint),
            "The entry point wrapper for 'Foo' is not an entry point."
        )

        let originalBar = TestUtils.getCallableWithName result Signatures.OutputRecordingNS "Bar"

        Assert.True(
            (originalBar.Attributes |> Seq.exists BuiltIn.MarksEntryPoint),
            "The original entry point 'Bar' is not an entry point."
        )

        let isNoGeneratedBar =
            TestUtils.getCallablesWithSuffix result Signatures.OutputRecordingNS "Bar__Main" |> Seq.isEmpty

        Assert.True((isNoGeneratedBar), "Found an unexpected entry point wrapper generated for 'Bar'.")
