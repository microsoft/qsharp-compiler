// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System.Collections.Immutable
open System.Text.RegularExpressions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.EntryPointWrapping
open Xunit
open Microsoft.Quantum.QsCompiler
open System.IO

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

    let assertLambdaFunctorsByLine result line parentName expectedFunctors =
        let regexMatch = Regex.Match(line, sprintf "_[a-z0-9]{32}_%s" parentName)
        Assert.True(regexMatch.Success, "The original callable did not have the expected content.")

        TestUtils.getCallableWithName result Signatures.LambdaLiftingNS regexMatch.Value
        |> TestUtils.assertCallSupportsFunctors expectedFunctors

    let makeVal n = sprintf "__rtrnVal%i__" n;

    let makeRecordInt n = sprintf "Message($\"Int: {%s}\");" (makeVal n)

    let makeRecordBool n = sprintf "Message($\"Bool: {%s}\");" (makeVal n)

    let makeRecordDouble n = sprintf "Message($\"Double: {%s}\");" (makeVal n)

    let makeRecordResult n = sprintf "Message($\"Result: {%s}\");" (makeVal n)

    let makeRecordStartTuple = "Message($\"Tuple Start\");"

    let makeRecordEndTuple = "Message($\"Tuple End\");"

    let makeRecordStartArray = "Message($\"Array Start\");"

    let makeRecordEndArray = "Message($\"Array End\");"

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

        Assert.True((original.Attributes |> Seq.exists BuiltIn.MarksEntryPoint), "The entry point wrapper is not an entry point.")

        let lines =
            generated
            |> TestUtils.getBodyFromCallable
            |> TestUtils.getLinesFromSpecialization

        let captureLine = sprintf "let %s = %s()" (makeVal 0) original.FullName.Name

        let expectedContent =
            [|
                captureLine
                makeRecordInt 0
            |]
        Assert.True((lines = expectedContent), "The generated callable did not have the expected content.")
