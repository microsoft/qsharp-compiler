// Copyright (c) Microsoft Corporation.
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

    let createArrayArgument (name: string,
                             argType: DataType,
                             position: int32,
                             arrayType: System.Nullable<DataType>,
                             valuesList: ArgumentValue list) =
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
            .Add("UseNoArgs",
                 (new EntryPointOperation(Name = "UseNoArgs"),
                  Path.Join(testCasesDirectory, "UseNoArgs.json") |> File.ReadAllText))
            .Add("UseBoolArg",
                 (createEntryPointOperation ("UseBoolArg", [ createArgument ("BoolArg", DataType.BoolType, 0, []) ]),
                  Path.Join(testCasesDirectory, "UseBoolArg.json") |> File.ReadAllText))
            .Add("UseBoolArgWithValues",
                 (createEntryPointOperation
                     ("UseBoolArgWithValues",
                      [
                          createArgument
                              ("BoolArg",
                               DataType.BoolType,
                               0,
                               [
                                   new ArgumentValue(Bool = System.Nullable(true))
                                   new ArgumentValue(Bool = System.Nullable(false))
                               ])
                      ]),
                  Path.Join(testCasesDirectory, "UseBoolArgWithValues.json") |> File.ReadAllText))
            .Add("UseIntegerArg",
                 (createEntryPointOperation
                     ("UseIntegerArg", [ createArgument ("IntegerArg", DataType.IntegerType, 0, []) ]),
                  Path.Join(testCasesDirectory, "UseIntegerArg.json") |> File.ReadAllText))
            .Add("UseIntegerArgWithValues",
                 (createEntryPointOperation
                     ("UseIntegerArgWithValues",
                      [
                          createArgument
                              ("IntegerArg",
                               DataType.IntegerType,
                               0,
                               [
                                   new ArgumentValue(Integer = System.Nullable(int64 (11)))
                                   new ArgumentValue(Integer = System.Nullable(int64 (999)))
                               ])
                      ]),
                  Path.Join(testCasesDirectory, "UseIntegerArgWithValues.json") |> File.ReadAllText))
            .Add("UseDoubleArg",
                 (createEntryPointOperation
                     ("UseDoubleArg", [ createArgument ("DoubleArg", DataType.DoubleType, 0, []) ]),
                  Path.Join(testCasesDirectory, "UseDoubleArg.json") |> File.ReadAllText))
            .Add("UseDoubleArgWithValues",
                 (createEntryPointOperation
                     ("UseDoubleArgWithValues",
                      [
                          createArgument
                              ("DoubleArg",
                               DataType.DoubleType,
                               0,
                               [
                                   new ArgumentValue(Double = System.Nullable(0.1))
                                   new ArgumentValue(Double = System.Nullable(0.2))
                               ])
                      ]),
                  Path.Join(testCasesDirectory, "UseDoubleArgWithValues.json") |> File.ReadAllText))
            .Add("UsePauliArg",
                 (createEntryPointOperation ("UsePauliArg", [ createArgument ("PauliArg", DataType.PauliType, 0, []) ]),
                  Path.Join(testCasesDirectory, "UsePauliArg.json") |> File.ReadAllText))
            .Add("UsePauliArgWithValues",
                 (createEntryPointOperation
                     ("UsePauliArgWithValues",
                      [
                          createArgument
                              ("PauliArg",
                               DataType.PauliType,
                               0,
                               [
                                   new ArgumentValue(Pauli = System.Nullable(PauliValue.PauliX))
                                   new ArgumentValue(Pauli = System.Nullable(PauliValue.PauliY))
                                   new ArgumentValue(Pauli = System.Nullable(PauliValue.PauliZ))
                               ])
                      ]),
                  Path.Join(testCasesDirectory, "UsePauliArgWithValues.json") |> File.ReadAllText))
            .Add("UseRangeArg",
                 (createEntryPointOperation ("UseRangeArg", [ createArgument ("RangeArg", DataType.RangeType, 0, []) ]),
                  Path.Join(testCasesDirectory, "UseRangeArg.json") |> File.ReadAllText))
            .Add("UseRangeArgWithValues",
                 (createEntryPointOperation
                     ("UseRangeArgWithValues",
                      [
                          createArgument
                              ("RangeArg",
                               DataType.RangeType,
                               0,
                               [
                                   createRangeArgumentValue (int64 (1), int64 (1), int64 (10))
                                   createRangeArgumentValue (int64 (10), int64 (5), int64 (100))
                               ])
                      ]),
                  Path.Join(testCasesDirectory, "UseRangeArgWithValues.json") |> File.ReadAllText))
            .Add("UseResultArg",
                 (createEntryPointOperation
                     ("UseResultArg", [ createArgument ("ResultArg", DataType.ResultType, 0, []) ]),
                  Path.Join(testCasesDirectory, "UseResultArg.json") |> File.ReadAllText))
            .Add("UseResultArgWithValues",
                 (createEntryPointOperation
                     ("UseResultArgWithValues",
                      [
                          createArgument
                              ("ResultArg",
                               DataType.ResultType,
                               0,
                               [
                                   new ArgumentValue(Result = System.Nullable(ResultValue.Zero))
                                   new ArgumentValue(Result = System.Nullable(ResultValue.One))
                               ])
                      ]),
                  Path.Join(testCasesDirectory, "UseResultArgWithValues.json") |> File.ReadAllText))
            .Add("UseStringArg",
                 (createEntryPointOperation
                     ("UseStringArg", [ createArgument ("StringArg", DataType.StringType, 0, []) ]),
                  Path.Join(testCasesDirectory, "UseStringArg.json") |> File.ReadAllText))
            .Add("UseStringArgWithValues",
                 (createEntryPointOperation
                     ("UseStringArgWithValues",
                      [
                          createArgument
                              ("StringArg",
                               DataType.StringType,
                               0,
                               [ new ArgumentValue(String = "StringA"); new ArgumentValue(String = "StringB") ])
                      ]),
                  Path.Join(testCasesDirectory, "UseStringArgWithValues.json") |> File.ReadAllText))
            .Add("UseBoolArrayArg",
                 (createEntryPointOperation
                     ("UseBoolArrayArg",
                      [
                          createArrayArgument
                              ("BoolArrayArg", DataType.ArrayType, 0, System.Nullable(DataType.BoolType), [])
                      ]),
                  Path.Join(testCasesDirectory, "UseBoolArrayArg.json") |> File.ReadAllText))
            .Add("UseBoolArrayArgWithValues",
                 (createEntryPointOperation
                     ("UseBoolArrayArgWithValues",
                      [
                          createArrayArgument
                              ("BoolArrayArg",
                               DataType.ArrayType,
                               0,
                               System.Nullable(DataType.BoolType),
                               [
                                   createBoolArrayArgumentValue ([ true; false; true ])
                                   createBoolArrayArgumentValue ([ false; true; false ])
                               ])
                      ]),
                  Path.Join(testCasesDirectory, "UseBoolArrayArgWithValues.json") |> File.ReadAllText))
            .Add("UseIntegerArrayArg",
                 (createEntryPointOperation
                     ("UseIntegerArrayArg",
                      [
                          createArrayArgument
                              ("IntegerArrayArg", DataType.ArrayType, 0, System.Nullable(DataType.IntegerType), [])
                      ]),
                  Path.Join(testCasesDirectory, "UseIntegerArrayArg.json") |> File.ReadAllText))
            .Add("UseIntegerArrayArgWithValues",
                 (createEntryPointOperation
                     ("UseIntegerArrayArgWithValues",
                      [
                          createArrayArgument
                              ("IntegerArrayArg",
                               DataType.ArrayType,
                               0,
                               System.Nullable(DataType.IntegerType),
                               [
                                   createIntegerArrayArgumentValue ([ int64 (999); int64 (-1); int64 (11) ])
                                   createIntegerArrayArgumentValue
                                       ([ int64 (2048); int64 (-1024); int64 (4096); int64 (-8192) ])
                               ])
                      ]),
                  Path.Join(testCasesDirectory, "UseIntegerArrayArgWithValues.json") |> File.ReadAllText))
            .Add("UseDoubleArrayArg",
                 (createEntryPointOperation
                     ("UseDoubleArrayArg",
                      [
                          createArrayArgument
                              ("DoubleArrayArg", DataType.ArrayType, 0, System.Nullable(DataType.DoubleType), [])
                      ]),
                  Path.Join(testCasesDirectory, "UseDoubleArrayArg.json") |> File.ReadAllText))
            .Add("UseDoubleArrayArgWithValues",
                 (createEntryPointOperation
                     ("UseDoubleArrayArgWithValues",
                      [
                          createArrayArgument
                              ("DoubleArrayArg",
                               DataType.ArrayType,
                               0,
                               System.Nullable(DataType.DoubleType),
                               [
                                   createDoubleArrayArgumentValue ([ 3.14159; 0.55; 1024.333; -8192.667 ])
                                   createDoubleArrayArgumentValue ([ 999.999; -101010.10; 0.0001 ])
                               ])
                      ]),
                  Path.Join(testCasesDirectory, "UseDoubleArrayArgWithValues.json") |> File.ReadAllText))
            .Add("UsePauliArrayArg",
                 (createEntryPointOperation
                     ("UsePauliArrayArg",
                      [
                          createArrayArgument
                              ("PauliArrayArg", DataType.ArrayType, 0, System.Nullable(DataType.PauliType), [])
                      ]),
                  Path.Join(testCasesDirectory, "UsePauliArrayArg.json") |> File.ReadAllText))
            .Add("UsePauliArrayArgWithValues",
                 (createEntryPointOperation
                     ("UsePauliArrayArgWithValues",
                      [
                          createArrayArgument
                              ("PauliArrayArg",
                               DataType.ArrayType,
                               0,
                               System.Nullable(DataType.PauliType),
                               [
                                   createPauliArrayArgumentValue
                                       ([ PauliValue.PauliX; PauliValue.PauliY; PauliValue.PauliZ ])
                                   createPauliArrayArgumentValue ([ PauliValue.PauliI; PauliValue.PauliZ ])
                               ])
                      ]),
                  Path.Join(testCasesDirectory, "UsePauliArrayArgWithValues.json") |> File.ReadAllText))
            .Add("UseRangeArrayArg",
                 (createEntryPointOperation
                     ("UseRangeArrayArg",
                      [
                          createArrayArgument
                              ("RangeArrayArg", DataType.ArrayType, 0, System.Nullable(DataType.RangeType), [])
                      ]),
                  Path.Join(testCasesDirectory, "UseRangeArrayArg.json") |> File.ReadAllText))
            .Add("UseRangeArrayArgWithValues",
                 (createEntryPointOperation
                     ("UseRangeArrayArgWithValues",
                      [
                          createArrayArgument
                              ("RangeArrayArg",
                               DataType.ArrayType,
                               0,
                               System.Nullable(DataType.RangeType),
                               [
                                   createRangeArrayArgumentValue
                                       ([ (int64 (1), int64 (1), int64 (10)); (int64 (10), int64 (5), int64 (100)) ])
                                   createRangeArrayArgumentValue ([ (int64 (1), int64 (2), int64 (10)) ])
                               ])
                      ]),
                  Path.Join(testCasesDirectory, "UseRangeArrayArgWithValues.json") |> File.ReadAllText))
            .Add("UseResultArrayArg",
                 (createEntryPointOperation
                     ("UseResultArrayArg",
                      [
                          createArrayArgument
                              ("ResultArrayArg", DataType.ArrayType, 0, System.Nullable(DataType.ResultType), [])
                      ]),
                  Path.Join(testCasesDirectory, "UseResultArrayArg.json") |> File.ReadAllText))
            .Add("UseResultArrayArgWithValues",
                 (createEntryPointOperation
                     ("UseResultArrayArgWithValues",
                      [
                          createArrayArgument
                              ("ResultArrayArg",
                               DataType.ArrayType,
                               0,
                               System.Nullable(DataType.ResultType),
                               [
                                   createResultArrayArgumentValue ([ ResultValue.Zero; ResultValue.One ])
                                   createResultArrayArgumentValue ([ ResultValue.One; ResultValue.Zero ])
                               ])
                      ]),
                  Path.Join(testCasesDirectory, "UseResultArrayArgWithValues.json") |> File.ReadAllText))
            .Add("UseMiscArgs",
                 (createEntryPointOperation
                     ("UseMiscArgs",
                      [
                          createArgument ("IntegerArg", DataType.BoolType, 0, [])
                          createArgument ("PauliArg", DataType.PauliType, 1, [])
                          createArrayArgument
                              ("ResultArrayArg", DataType.ArrayType, 2, System.Nullable(DataType.ResultType), [])
                      ]),
                  Path.Join(testCasesDirectory, "UseMiscArgs.json") |> File.ReadAllText))

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
        let expectedEntryPointOperation, sourceJson = sampleEntryPointOperations.[sampleName]
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
        let entryPointOperation, expectedJson = sampleEntryPointOperations.[sampleName]
        let memoryStream = new MemoryStream()
        Protocols.SerializeToJson(entryPointOperation, memoryStream)
        let reader = new StreamReader(memoryStream, Encoding.UTF8)
        let json = reader.ReadToEnd()
        Assert.Equal(expectedJson, json)
