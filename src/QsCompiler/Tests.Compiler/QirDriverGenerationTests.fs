// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open Xunit
open Xunit.Abstractions
open Microsoft.Quantum.QsCompiler
open System.IO

type QirDriverGenerationTests(output: ITestOutputHelper) =

    let testCasesDirectory = Path.Combine("TestCases", "QirEntryPointTests")

    [<Theory>]
    [<InlineData("UseMiscArgs")>]
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
    member this.GenerateArgs(testFileName: string) =
        let executionInformation = SampleExecutionInfoHelper.sampleExecutionInformation.[testFileName]
        let expectedArgs = (Path.Join(testCasesDirectory, (testFileName + ".txt")) |> File.ReadAllText).Trim()
        let generatedArgs = QirDriverGeneration.GenerateCommandLineArguments(executionInformation)
        Assert.Equal(expectedArgs, generatedArgs)
