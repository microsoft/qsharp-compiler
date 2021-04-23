// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open Xunit
open Xunit.Abstractions
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.BondSchemas.EntryPoint
open System.IO
open System.Text

type QirDriverGenerationTests(output: ITestOutputHelper) =

    let testCasesDirectory = Path.Combine("TestCases", "QirEntryPointTests")

    [<Theory>]
    [<InlineData("UseManyArgsWithValues")>]
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
        let expectedArgs = (Path.Join(testCasesDirectory, (testFileName + ".txt")) |> File.ReadAllText).Trim()
        let entryPointOperationJson = Path.Join(testCasesDirectory, (testFileName + ".json")) |> File.ReadAllText
        let entryPointOperationMs = new MemoryStream(Encoding.UTF8.GetBytes(entryPointOperationJson))
        let entryPointOperation = Protocols.DeserializeFromJson(entryPointOperationMs)
        let generatedArgs = QirDriverGeneration.GenerateCommandLineArguments(entryPointOperation.Arguments)
        Assert.Equal(expectedArgs, generatedArgs)
