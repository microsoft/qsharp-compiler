// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System.IO
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.EntryPointWrapping
open Xunit

type EntryPointWrappingTests() =

    let compileEntryPointWrappingTests testNumber =
        let coreFiles = [Path.Combine("TestCases", "LinkingTests", "Core.qs") |> Path.GetFullPath]
        let srcChunks = TestUtils.readAndChunkSourceFile "EntryPointWrapping.qs"
        srcChunks.Length >= testNumber + 1 |> Assert.True
        let shared = srcChunks.[0]
        let compilationDataStructures = TestUtils.buildContentWithFiles (shared + srcChunks.[testNumber]) coreFiles
        let processedCompilation = EntryPointWrapping.Apply compilationDataStructures.BuiltCompilation
        Assert.NotNull processedCompilation

        //Signatures.SignatureCheck
        //    [ Signatures.LambdaLiftingNS ]
        //    Signatures.LambdaLiftingSignatures.[testNumber - 1]
        //    processedCompilation

        processedCompilation

    let wrapperAPINamespaceName = "Microsoft.Quantum.Core";

    let makeVal n = sprintf "__rtrnVal%i__" n;

    let makeRecordBool n = sprintf "%s.BooleanRecordOutput(%s);" wrapperAPINamespaceName (makeVal n)

    let makeRecordInt n = sprintf "%s.IntegerRecordOutput(%s);" wrapperAPINamespaceName (makeVal n)

    let makeRecordDouble n = sprintf "%s.DoubleRecordOutput(%s);" wrapperAPINamespaceName (makeVal n)

    let makeRecordResult n = sprintf "%s.ResultRecordOutput(%s);" wrapperAPINamespaceName (makeVal n)

    let makeRecordStartTuple = sprintf "%s.TupleStartRecordOutput();" wrapperAPINamespaceName

    let makeRecordEndTuple = sprintf "%s.TupleEndRecordOutput();" wrapperAPINamespaceName

    let makeRecordStartArray = sprintf "%s.ArrayStartRecordOutput();" wrapperAPINamespaceName

    let makeRecordEndArray = sprintf "%s.ArrayEndRecordOutput();" wrapperAPINamespaceName

    let makeArrayLoop n1 n2 = sprintf "for %s in %s {" (makeVal n1) (makeVal n2)

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Return Int``() =
        let result = compileEntryPointWrappingTests 1

        let original =
            TestUtils.getCallableWithName result Signatures.EntryPointWrappingNS "Foo"

        Assert.False((original.Attributes |> Seq.exists BuiltIn.MarksEntryPoint), "The original entry point is still an entry point.")

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.EntryPointWrappingNS "_Foo"
            |> Seq.exactlyOne

        Assert.True((generated.Attributes |> Seq.exists BuiltIn.MarksEntryPoint), "The entry point wrapper is not an entry point.")

        let lines =
            generated
            |> TestUtils.getBodyFromCallable
            |> TestUtils.getLinesFromSpecialization

        let captureLine = sprintf "let %s = %O();" (makeVal 0) original.FullName

        let expectedContent =
            [|
                captureLine
                makeRecordInt 0
            |]
        Assert.True((lines = expectedContent), "The generated callable did not have the expected content.")
