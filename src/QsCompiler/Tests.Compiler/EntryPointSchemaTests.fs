﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System.Collections.Generic
open System.IO
open System.Text
open Bond
open Xunit
open Xunit.Abstractions
open Microsoft.Quantum.QsCompiler.BondSchemas.EntryPoint

type EntryPointSchemaTests(output: ITestOutputHelper) =

    let testCasesDirectory = Path.Combine("TestCases", "QirEntryPointTests")

    let createRangeValueFromTuple (tuple: (int64 * int64 * int64)) =
        let rstart, rstep, rend = tuple
        RangeValue(Start = rstart, Step = rstep, End = rend)

    let createRangeArgumentValue (rstart: int64, rstep: int64, rend: int64) =
        let argumentValue = new ArgumentValue(Range = ((rstart, rstep, rend) |> createRangeValueFromTuple))
        argumentValue

    let createArrayArgumentValue (arrayValue: ArrayValue) =
        let argumentValue = new ArgumentValue(Array = arrayValue)
        argumentValue

    let createBoolArrayArgumentValue (lst: bool list) =
        let arrayValue = new ArrayValue(Bool = new List<bool>())
        List.iter (fun v -> arrayValue.Bool.Add v) lst
        arrayValue |> createArrayArgumentValue

    let createIntegerArrayArgumentValue (lst: int64 list) =
        let arrayValue = new ArrayValue(Integer = new List<int64>())
        List.iter (fun v -> arrayValue.Integer.Add v) lst
        arrayValue |> createArrayArgumentValue

    let createDoubleArrayArgumentValue (lst: double list) =
        let arrayValue = new ArrayValue(Double = new List<double>())
        List.iter (fun v -> arrayValue.Double.Add v) lst
        arrayValue |> createArrayArgumentValue

    let createPauliArrayArgumentValue (lst: PauliValue list) =
        let arrayValue = new ArrayValue(Pauli = new List<PauliValue>())
        List.iter (fun v -> arrayValue.Pauli.Add v) lst
        arrayValue |> createArrayArgumentValue

    let createRangeArrayArgumentValue (lst: (int64 * int64 * int64) list) =
        let arrayValue = new ArrayValue(Range = new List<RangeValue>())
        List.iter (fun v -> arrayValue.Range.Add(v |> createRangeValueFromTuple)) lst
        arrayValue |> createArrayArgumentValue

    let createResultArrayArgumentValue (lst: ResultValue list) =
        let arrayValue = new ArrayValue(Result = new List<ResultValue>())
        List.iter (fun v -> arrayValue.Result.Add v) lst
        arrayValue |> createArrayArgumentValue

    let createArgument (name: string, argType: DataType, position: int, valuesList: ArgumentValue list) =
        let argument = new Argument(Name = name, Type = argType, Position = position)
        List.iter (fun v -> argument.Values.Add v) valuesList
        argument

    let createArrayArgument
        (
            name: string,
            argType: DataType,
            position: int32,
            arrayType: System.Nullable<DataType>,
            valuesList: ArgumentValue list
        ) =
        let argument = new Argument(Name = name, Type = argType, Position = position, ArrayType = arrayType)
        List.iter (fun v -> argument.Values.Add v) valuesList
        argument

    let createEntryPointOperation (name: string, argsList) =
        let entryPointOperation = new EntryPointOperation(Name = name)
        List.iter (fun a -> entryPointOperation.Arguments.Add a) argsList
        entryPointOperation

    let sampleEntryPointOperations =
        Map
            .empty
            .Add("UseNoArgs", new EntryPointOperation(Name = "UseNoArgs"))
            .Add(
                "UseBoolArg",
                createEntryPointOperation ("UseBoolArg", [ createArgument ("BoolArg", DataType.BoolType, 0, []) ])
            )
            .Add(
                "UseBoolArgWithValues",
                createEntryPointOperation (
                    "UseBoolArgWithValues",
                    [
                        createArgument (
                            "BoolArg",
                            DataType.BoolType,
                            0,
                            [
                                new ArgumentValue(Bool = System.Nullable(true))
                                new ArgumentValue(Bool = System.Nullable(false))
                            ]
                        )
                    ]
                )
            )
            .Add(
                "UseIntegerArg",
                createEntryPointOperation (
                    "UseIntegerArg",
                    [ createArgument ("IntegerArg", DataType.IntegerType, 0, []) ]
                )
            )
            .Add(
                "UseIntegerArgWithValues",
                createEntryPointOperation (
                    "UseIntegerArgWithValues",
                    [
                        createArgument (
                            "IntegerArg",
                            DataType.IntegerType,
                            0,
                            [
                                new ArgumentValue(Integer = System.Nullable(int64 (11)))
                                new ArgumentValue(Integer = System.Nullable(int64 (999)))
                            ]
                        )
                    ]
                )
            )
            .Add(
                "UseDoubleArg",
                createEntryPointOperation ("UseDoubleArg", [ createArgument ("DoubleArg", DataType.DoubleType, 0, []) ])
            )
            .Add(
                "UseDoubleArgWithValues",
                createEntryPointOperation (
                    "UseDoubleArgWithValues",
                    [
                        createArgument (
                            "DoubleArg",
                            DataType.DoubleType,
                            0,
                            [
                                new ArgumentValue(Double = System.Nullable(0.1))
                                new ArgumentValue(Double = System.Nullable(0.2))
                            ]
                        )
                    ]
                )
            )
            .Add(
                "UsePauliArg",
                createEntryPointOperation ("UsePauliArg", [ createArgument ("PauliArg", DataType.PauliType, 0, []) ])
            )
            .Add(
                "UsePauliArgWithValues",
                createEntryPointOperation (
                    "UsePauliArgWithValues",
                    [
                        createArgument (
                            "PauliArg",
                            DataType.PauliType,
                            0,
                            [
                                new ArgumentValue(Pauli = System.Nullable(PauliValue.PauliX))
                                new ArgumentValue(Pauli = System.Nullable(PauliValue.PauliY))
                                new ArgumentValue(Pauli = System.Nullable(PauliValue.PauliZ))
                            ]
                        )
                    ]
                )
            )
            .Add(
                "UseRangeArg",
                createEntryPointOperation ("UseRangeArg", [ createArgument ("RangeArg", DataType.RangeType, 0, []) ])
            )
            .Add(
                "UseRangeArgWithValues",
                createEntryPointOperation (
                    "UseRangeArgWithValues",
                    [
                        createArgument (
                            "RangeArg",
                            DataType.RangeType,
                            0,
                            [
                                createRangeArgumentValue (int64 (1), int64 (1), int64 (10))
                                createRangeArgumentValue (int64 (10), int64 (5), int64 (100))
                            ]
                        )
                    ]
                )
            )
            .Add(
                "UseResultArg",
                createEntryPointOperation ("UseResultArg", [ createArgument ("ResultArg", DataType.ResultType, 0, []) ])
            )
            .Add(
                "UseResultArgWithValues",
                createEntryPointOperation (
                    "UseResultArgWithValues",
                    [
                        createArgument (
                            "ResultArg",
                            DataType.ResultType,
                            0,
                            [
                                new ArgumentValue(Result = System.Nullable(ResultValue.Zero))
                                new ArgumentValue(Result = System.Nullable(ResultValue.One))
                            ]
                        )
                    ]
                )
            )
            .Add(
                "UseStringArg",
                createEntryPointOperation ("UseStringArg", [ createArgument ("StringArg", DataType.StringType, 0, []) ])
            )
            .Add(
                "UseStringArgWithValues",
                createEntryPointOperation (
                    "UseStringArgWithValues",
                    [
                        createArgument (
                            "StringArg",
                            DataType.StringType,
                            0,
                            [ new ArgumentValue(String = "StringA"); new ArgumentValue(String = "StringB") ]
                        )
                    ]
                )
            )
            .Add(
                "UseBoolArrayArg",
                createEntryPointOperation (
                    "UseBoolArrayArg",
                    [
                        createArrayArgument (
                            "BoolArrayArg",
                            DataType.ArrayType,
                            0,
                            System.Nullable(DataType.BoolType),
                            []
                        )
                    ]
                )
            )
            .Add(
                "UseBoolArrayArgWithValues",
                createEntryPointOperation (
                    "UseBoolArrayArgWithValues",
                    [
                        createArrayArgument (
                            "BoolArrayArg",
                            DataType.ArrayType,
                            0,
                            System.Nullable(DataType.BoolType),
                            [
                                createBoolArrayArgumentValue ([ true; false; true ])
                                createBoolArrayArgumentValue ([ false; true; false ])
                            ]
                        )
                    ]
                )
            )
            .Add(
                "UseIntegerArrayArg",
                createEntryPointOperation (
                    "UseIntegerArrayArg",
                    [
                        createArrayArgument (
                            "IntegerArrayArg",
                            DataType.ArrayType,
                            0,
                            System.Nullable(DataType.IntegerType),
                            []
                        )
                    ]
                )
            )
            .Add(
                "UseIntegerArrayArgWithValues",
                createEntryPointOperation (
                    "UseIntegerArrayArgWithValues",
                    [
                        createArrayArgument (
                            "IntegerArrayArg",
                            DataType.ArrayType,
                            0,
                            System.Nullable(DataType.IntegerType),
                            [
                                createIntegerArrayArgumentValue ([ int64 (999); int64 (-1); int64 (11) ])
                                createIntegerArrayArgumentValue (
                                    [ int64 (2048); int64 (-1024); int64 (4096); int64 (-8192) ]
                                )
                            ]
                        )
                    ]
                )
            )
            .Add(
                "UseDoubleArrayArg",
                createEntryPointOperation (
                    "UseDoubleArrayArg",
                    [
                        createArrayArgument (
                            "DoubleArrayArg",
                            DataType.ArrayType,
                            0,
                            System.Nullable(DataType.DoubleType),
                            []
                        )
                    ]
                )
            )
            .Add(
                "UseDoubleArrayArgWithValues",
                createEntryPointOperation (
                    "UseDoubleArrayArgWithValues",
                    [
                        createArrayArgument (
                            "DoubleArrayArg",
                            DataType.ArrayType,
                            0,
                            System.Nullable(DataType.DoubleType),
                            [
                                createDoubleArrayArgumentValue ([ 3.14159; 0.55; 1024.333; -8192.667 ])
                                createDoubleArrayArgumentValue ([ 999.999; -101010.10; 0.0001 ])
                            ]
                        )
                    ]
                )
            )
            .Add(
                "UsePauliArrayArg",
                createEntryPointOperation (
                    "UsePauliArrayArg",
                    [
                        createArrayArgument (
                            "PauliArrayArg",
                            DataType.ArrayType,
                            0,
                            System.Nullable(DataType.PauliType),
                            []
                        )
                    ]
                )
            )
            .Add(
                "UsePauliArrayArgWithValues",
                createEntryPointOperation (
                    "UsePauliArrayArgWithValues",
                    [
                        createArrayArgument (
                            "PauliArrayArg",
                            DataType.ArrayType,
                            0,
                            System.Nullable(DataType.PauliType),
                            [
                                createPauliArrayArgumentValue (
                                    [ PauliValue.PauliX; PauliValue.PauliY; PauliValue.PauliZ ]
                                )
                                createPauliArrayArgumentValue ([ PauliValue.PauliI; PauliValue.PauliZ ])
                            ]
                        )
                    ]
                )
            )
            .Add(
                "UseRangeArrayArg",
                createEntryPointOperation (
                    "UseRangeArrayArg",
                    [
                        createArrayArgument (
                            "RangeArrayArg",
                            DataType.ArrayType,
                            0,
                            System.Nullable(DataType.RangeType),
                            []
                        )
                    ]
                )
            )
            .Add(
                "UseRangeArrayArgWithValues",
                createEntryPointOperation (
                    "UseRangeArrayArgWithValues",
                    [
                        createArrayArgument (
                            "RangeArrayArg",
                            DataType.ArrayType,
                            0,
                            System.Nullable(DataType.RangeType),
                            [
                                createRangeArrayArgumentValue (
                                    [ (int64 (1), int64 (1), int64 (10)); (int64 (10), int64 (5), int64 (100)) ]
                                )
                                createRangeArrayArgumentValue ([ (int64 (1), int64 (2), int64 (10)) ])
                            ]
                        )
                    ]
                )
            )
            .Add(
                "UseResultArrayArg",
                createEntryPointOperation (
                    "UseResultArrayArg",
                    [
                        createArrayArgument (
                            "ResultArrayArg",
                            DataType.ArrayType,
                            0,
                            System.Nullable(DataType.ResultType),
                            []
                        )
                    ]
                )
            )
            .Add(
                "UseResultArrayArgWithValues",
                createEntryPointOperation (
                    "UseResultArrayArgWithValues",
                    [
                        createArrayArgument (
                            "ResultArrayArg",
                            DataType.ArrayType,
                            0,
                            System.Nullable(DataType.ResultType),
                            [
                                createResultArrayArgumentValue ([ ResultValue.Zero; ResultValue.One ])
                                createResultArrayArgumentValue ([ ResultValue.One; ResultValue.Zero ])
                            ]
                        )
                    ]
                )
            )
            .Add(
                "UseMiscArgs",
                createEntryPointOperation (
                    "UseMiscArgs",
                    [
                        createArgument ("IntegerArg", DataType.BoolType, 0, [])
                        createArgument ("PauliArg", DataType.PauliType, 1, [])
                        createArrayArgument (
                            "ResultArrayArg",
                            DataType.ArrayType,
                            2,
                            System.Nullable(DataType.ResultType),
                            []
                        )
                    ]
                )
            )

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
    [<InlineData("UseBoolArrayArg")>]
    [<InlineData("UseBoolArrayArgWithValues")>]
    [<InlineData("UseIntegerArrayArg")>]
    [<InlineData("UseIntegerArrayArgWithValues")>]
    [<InlineData("UseDoubleArrayArg")>]
    [<InlineData("UseDoubleArrayArgWithValues")>]
    [<InlineData("UsePauliArrayArg")>]
    [<InlineData("UsePauliArrayArgWithValues")>]
    [<InlineData("UseRangeArrayArg")>]
    [<InlineData("UseRangeArrayArgWithValues")>]
    [<InlineData("UseResultArrayArg")>]
    [<InlineData("UseResultArrayArgWithValues")>]
    [<InlineData("UseMiscArgs")>]
    member this.DeserializeFromJson(sampleName: string) =
        let expectedEntryPointOperation = sampleEntryPointOperations.[sampleName]
        let sourceJson = Path.Join(testCasesDirectory, sampleName + ".json") |> File.ReadAllText
        let memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(sourceJson))
        let entryPointOperation = Protocols.DeserializeFromJson(memoryStream)
        Assert.True(Extensions.ValueEquals(entryPointOperation, expectedEntryPointOperation))


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
    [<InlineData("UseBoolArrayArg")>]
    [<InlineData("UseBoolArrayArgWithValues")>]
    [<InlineData("UseIntegerArrayArg")>]
    [<InlineData("UseIntegerArrayArgWithValues")>]
    [<InlineData("UseDoubleArrayArg")>]
    [<InlineData("UseDoubleArrayArgWithValues")>]
    [<InlineData("UsePauliArrayArg")>]
    [<InlineData("UsePauliArrayArgWithValues")>]
    [<InlineData("UseRangeArrayArg")>]
    [<InlineData("UseRangeArrayArgWithValues")>]
    [<InlineData("UseResultArrayArg")>]
    [<InlineData("UseResultArrayArgWithValues")>]
    [<InlineData("UseMiscArgs")>]
    member this.SerializeToJson(sampleName: string) =
        let entryPointOperation = sampleEntryPointOperations.[sampleName]
        let expectedJson = Path.Join(testCasesDirectory, sampleName + ".json") |> File.ReadAllText
        let memoryStream = new MemoryStream()
        Protocols.SerializeToJson(entryPointOperation, memoryStream)
        let reader = new StreamReader(memoryStream, Encoding.UTF8)
        let json = reader.ReadToEnd()
        Assert.Equal(expectedJson, json)
