// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System.IO
open System.Text
open Bond
open Xunit
open Xunit.Abstractions
open Microsoft.Quantum.QsCompiler.BondSchemas.EntryPoint

type EntryPointSchemaTests(output: ITestOutputHelper) =

    let createBoolArgumentValue(value: bool) =
        let argumentValue = new ArgumentValue(Type = DataType.BoolType, Bool = value)
        argumentValue

    let createIntegerArgumentValue(value: int64) =
        let argumentValue = new ArgumentValue(Type = DataType.IntegerType, Integer = value)
        argumentValue

    let createDoubleArgumentValue(value: double) =
        let argumentValue = new ArgumentValue(Type = DataType.DoubleType, Double = value)
        argumentValue

    let createPauliArgumentValue(value: PauliValue) =
        let argumentValue = new ArgumentValue(Type = DataType.PauliType, Pauli = value)
        argumentValue

    let createRangeArgumentValue(rstart: int64, rstep: int64, rend: int64) =
        let rangeValue = RangeValue(Start = rstart, Step = rstep, End = rend)
        let argumentValue = new ArgumentValue(Type = DataType.RangeType, Range = rangeValue)
        argumentValue

    let createResultArgumentValue(value: ResultValue) =
        let argumentValue = new ArgumentValue(Type = DataType.ResultType, Result = value)
        argumentValue

    let createStringArgumentValue(value: string) =
        let argumentValue = new ArgumentValue(Type = DataType.StringType, String = value)
        argumentValue

    let createArrayArgumentValue(value: ArrayValue) =
        let argumentValue = new ArgumentValue(Type = DataType.ArrayType, Array = value)
        argumentValue

    let createArgument(name: string, argType: DataType, valuesList: ArgumentValue list) =
        let argument = new Argument(Name = name, Type = argType)
        List.iter (fun v -> argument.Values.Add v) valuesList
        argument

    let createEntryPointOperation(name: string, argsList) =
        let entryPointOperation = new EntryPointOperation(Name = name)
        List.iter (fun a -> entryPointOperation.Arguments.Add a) argsList
        entryPointOperation
    
    let sampleEntryPointOperations =
        Map.empty.
            Add(
                "UseNoArgs",
                (
                    new EntryPointOperation(Name = "UseNoArgs"),
                    "{\"Name\":\"UseNoArgs\"}"
                )).
            Add(
                "UseBoolArg",
                (
                    createEntryPointOperation(
                        "UseBoolArg",
                        [createArgument(
                            "BoolArg",
                            DataType.BoolType,
                            [])]),
                    "{\"Name\":\"UseBoolArg\",\"Arguments\":[{\"Name\":\"BoolArg\"}]}"
                )).
            Add(
                "UseBoolArgWithValues",
                (
                    createEntryPointOperation(
                        "UseBoolArgWithValues",
                        [createArgument(
                            "BoolArg",
                            DataType.BoolType,
                            [createBoolArgumentValue(true); createBoolArgumentValue(false)])]),
                    "{\"Name\":\"UseBoolArgWithValues\",\"Arguments\":[{\"Name\":\"BoolArg\",\"Values\":[{\"Bool\":[true]},{\"Bool\":[false]}]}]}"
                )).
            Add(
                "UseIntegerArg",
                (
                    createEntryPointOperation(
                        "UseIntegerArg",
                        [createArgument(
                            "IntegerArg",
                            DataType.IntegerType,
                            [])]),
                    "{\"Name\":\"UseIntegerArg\",\"Arguments\":[{\"Type\":1,\"Name\":\"IntegerArg\"}]}"
                )).
            Add(
                "UseIntegerArgWithValues",
                (
                    createEntryPointOperation(
                        "UseIntegerArgWithValues",
                        [createArgument(
                            "IntegerArg",
                            DataType.IntegerType,
                            [createIntegerArgumentValue(int64(11)); createIntegerArgumentValue(int64(999))])]),
                    "{\"Name\":\"UseIntegerArgWithValues\",\"Arguments\":[{\"Type\":1,\"Name\":\"IntegerArg\",\"Values\":[{\"Type\":1,\"Integer\":[11]},{\"Type\":1,\"Integer\":[999]}]}]}"
                )).
            Add(
                "UseDoubleArg",
                (
                    createEntryPointOperation(
                        "UseDoubleArg",
                        [createArgument(
                            "DoubleArg",
                            DataType.DoubleType,
                            [])]),
                    "{\"Name\":\"UseDoubleArg\",\"Arguments\":[{\"Type\":2,\"Name\":\"DoubleArg\"}]}"
                )).
            Add(
                "UseDoubleArgWithValues",
                (
                    createEntryPointOperation(
                        "UseDoubleArgWithValues",
                        [createArgument(
                            "DoubleArg",
                            DataType.DoubleType,
                            [createDoubleArgumentValue(0.1); createDoubleArgumentValue(0.2)])]),
                    "{\"Name\":\"UseDoubleArgWithValues\",\"Arguments\":[{\"Type\":2,\"Name\":\"DoubleArg\",\"Values\":[{\"Type\":2,\"Double\":[0.1]},{\"Type\":2,\"Double\":[0.2]}]}]}"
                )).
            Add(
                "UsePauliArg",
                (
                    createEntryPointOperation(
                        "UsePauliArg",
                        [createArgument(
                            "PauliArg",
                            DataType.PauliType,
                            [])]),
                    "{\"Name\":\"UsePauliArg\",\"Arguments\":[{\"Type\":3,\"Name\":\"PauliArg\"}]}"
                )).
            Add(
                "UsePauliArgWithValues",
                (
                    createEntryPointOperation(
                        "UsePauliArgWithValues",
                        [createArgument(
                            "PauliArg",
                            DataType.PauliType,
                            [createPauliArgumentValue(PauliValue.PauliX); createPauliArgumentValue(PauliValue.PauliY); createPauliArgumentValue(PauliValue.PauliZ)])]),
                    "{\"Name\":\"UsePauliArgWithValues\",\"Arguments\":[{\"Type\":3,\"Name\":\"PauliArg\",\"Values\":[{\"Type\":3,\"Pauli\":[1]},{\"Type\":3,\"Pauli\":[2]},{\"Type\":3,\"Pauli\":[3]}]}]}"
                )).
            Add(
                "UseRangeArg",
                (
                    createEntryPointOperation(
                        "UseRangeArg",
                        [createArgument(
                            "RangeArg",
                            DataType.RangeType,
                            [])]),
                    "{\"Name\":\"UseRangeArg\",\"Arguments\":[{\"Type\":4,\"Name\":\"RangeArg\"}]}"
                )).
            Add(
                "UseRangeArgWithValues",
                (
                    createEntryPointOperation(
                        "UseRangeArgWithValues",
                        [createArgument(
                            "RangeArg",
                            DataType.RangeType,
                            [createRangeArgumentValue(int64(1), int64(1), int64(10)); createRangeArgumentValue(int64(10), int64(5), int64(100))])]),
                    "{\"Name\":\"UseRangeArgWithValues\",\"Arguments\":[{\"Type\":4,\"Name\":\"RangeArg\",\"Values\":[{\"Type\":4,\"Range\":[{\"Start\":1,\"Step\":1,\"End\":10}]},{\"Type\":4,\"Range\":[{\"Start\":10,\"Step\":5,\"End\":100}]}]}]}"
                )).
            Add(
                "UseResultArg",
                (
                    createEntryPointOperation(
                        "UseResultArg",
                        [createArgument(
                            "ResultArg",
                            DataType.ResultType,
                            [])]),
                    "{\"Name\":\"UseResultArg\",\"Arguments\":[{\"Type\":5,\"Name\":\"ResultArg\"}]}"
                )).
            Add(
                "UseResultArgWithValues",
                (
                    createEntryPointOperation(
                        "UseResultArgWithValues",
                        [createArgument(
                            "ResultArg",
                            DataType.ResultType,
                            [createResultArgumentValue(ResultValue.Zero);createResultArgumentValue(ResultValue.One)])]),
                    "{\"Name\":\"UseResultArgWithValues\",\"Arguments\":[{\"Type\":5,\"Name\":\"ResultArg\",\"Values\":[{\"Type\":5,\"Result\":[0]},{\"Type\":5,\"Result\":[1]}]}]}"
                )).
            Add(
                "UseStringArg",
                (
                    createEntryPointOperation(
                        "UseStringArg",
                        [createArgument(
                            "StringArg",
                            DataType.StringType,
                            [])]),
                    "{\"Name\":\"UseStringArg\",\"Arguments\":[{\"Type\":6,\"Name\":\"StringArg\"}]}"
                )).
            Add(
                "UseStringArgWithValues",
                (
                    createEntryPointOperation(
                        "UseStringArgWithValues",
                        [createArgument(
                            "StringArg",
                            DataType.StringType,
                            [createStringArgumentValue("StringA");createStringArgumentValue("StringB")])]),
                    "{\"Name\":\"UseStringArgWithValues\",\"Arguments\":[{\"Type\":6,\"Name\":\"StringArg\",\"Values\":[{\"Type\":6,\"String\":[\"StringA\"]},{\"Type\":6,\"String\":[\"StringB\"]}]}]}"
                ))

    [<Theory>]
    [<InlineData("UseNoArgs")>]
    [<InlineData("UseBoolArg")>]
    [<InlineData("UseBoolArgWithValues")>]
    [<InlineData("UseIntegerArg")>]
    [<InlineData("UseIntegerArgWithValues")>]
    [<InlineData("UseDoubleArg")>]
    [<InlineData("UseDoubleArgWithValues")>]
    [<InlineData("UsePauliArg")>]
    [<InlineData("UsePauliArgWithValues")>]
    [<InlineData("UseRangeArg")>]
    [<InlineData("UseRangeArgWithValues")>]
    [<InlineData("UseResultArg")>]
    [<InlineData("UseResultArgWithValues")>]
    [<InlineData("UseStringArg")>]
    [<InlineData("UseStringArgWithValues")>]
    member this.``SerializeToJson``(sampleName: string) =
        let memoryStream = new MemoryStream()
        
        let entryPointOperation, expectedJson = sampleEntryPointOperations.[sampleName]
        Protocols.SerializeToJson(entryPointOperation, memoryStream)
        let reader = new StreamReader(memoryStream, Encoding.UTF8)
        let json = reader.ReadToEnd()
        Assert.Equal(expectedJson, json)
