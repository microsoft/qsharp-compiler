// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System.IO
open Xunit
open Xunit.Abstractions
open Microsoft.Quantum.QsCompiler
open System.Text

type EntryPointGenerationTests(output: ITestOutputHelper) =

    let testCasesDirectory = Path.Combine("TestCases", "QirEntryPointTests")

    [<Theory>]
    [<InlineData("UseNoArgs")>]
    [<InlineData("UseBoolArgWithValues")>]
    [<InlineData("UseIntegerArgWithValues")>]
    [<InlineData("UseDoubleArgWithValues")>]
    [<InlineData("UsePauliArgWithValues")>]
    [<InlineData("UseRangeArgWithValues")>]
    [<InlineData("UseResultArgWithValues")>]
    [<InlineData("UseStringArgWithValues")>]
    [<InlineData("UseBoolArrayArgWithValues")>]
    [<InlineData("UseIntegerArrayArgWithValues")>]
    [<InlineData("UseDoubleArrayArgWithValues")>]
    [<InlineData("UsePauliArrayArgWithValues")>]
    [<InlineData("UseRangeArrayArgWithValues")>]
    [<InlineData("UseResultArrayArgWithValues")>]
    [<InlineData("UseMiscArgs")>]
    member this.GenerateEntryPoint(testFileName: string) =
        let executionInfo = SampleExecutionInfoHelper.sampleExecutionInformation.[testFileName]
        let expectedCpp = Path.Join(testCasesDirectory, (testFileName + ".cpp")) |> File.ReadAllText
        let cppMs = new MemoryStream()
        QirDriverGeneration.GenerateQirDriverCpp(executionInfo.EntryPoint, cppMs)
        let cppReader = new StreamReader(cppMs, Encoding.UTF8)
        let generatedCpp = cppReader.ReadToEnd()
        Assert.Equal(expectedCpp, generatedCpp)
