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
                             arrayType: DataType,
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
            .Add("UseNoArgs", (new EntryPointOperation(Name = "UseNoArgs"), "{\"Name\":\"UseNoArgs\"}"))
            .Add("UseBoolArg",
                 (createEntryPointOperation ("UseBoolArg", [ createArgument ("BoolArg", DataType.BoolType, 0, []) ]),
                  "{\"Name\":\"UseBoolArg\",\"Arguments\":[{\"Name\":\"BoolArg\"}]}"))
            .Add("UseBoolArgWithValues",
                 (createEntryPointOperation
                     ("UseBoolArgWithValues",
                      [
                          createArgument
                              ("BoolArg",
                               DataType.BoolType,
                               0,
                               [ new ArgumentValue(Bool = true); new ArgumentValue(Bool = false) ])
                      ]),
                  "{\"Name\":\"UseBoolArgWithValues\",\"Arguments\":[{\"Name\":\"BoolArg\",\"Values\":[{\"Bool\":[true]},{\"Bool\":[false]}]}]}"))
            .Add("UseIntegerArg",
                 (createEntryPointOperation
                     ("UseIntegerArg", [ createArgument ("IntegerArg", DataType.IntegerType, 0, []) ]),
                  "{\"Name\":\"UseIntegerArg\",\"Arguments\":[{\"Type\":1,\"Name\":\"IntegerArg\"}]}"))
            .Add("UseIntegerArgWithValues",
                 (createEntryPointOperation
                     ("UseIntegerArgWithValues",
                      [
                          createArgument
                              ("IntegerArg",
                               DataType.IntegerType,
                               0,
                               [
                                   new ArgumentValue(Integer = int64 (11))
                                   new ArgumentValue(Integer = int64 (999))
                               ])
                      ]),
                  "{\"Name\":\"UseIntegerArgWithValues\",\"Arguments\":[{\"Type\":1,\"Name\":\"IntegerArg\",\"Values\":[{\"Integer\":[11]},{\"Integer\":[999]}]}]}"))
            .Add("UseDoubleArg",
                 (createEntryPointOperation
                     ("UseDoubleArg", [ createArgument ("DoubleArg", DataType.DoubleType, 0, []) ]),
                  "{\"Name\":\"UseDoubleArg\",\"Arguments\":[{\"Type\":2,\"Name\":\"DoubleArg\"}]}"))
            .Add("UseDoubleArgWithValues",
                 (createEntryPointOperation
                     ("UseDoubleArgWithValues",
                      [
                          createArgument
                              ("DoubleArg",
                               DataType.DoubleType,
                               0,
                               [ new ArgumentValue(Double = 0.1); new ArgumentValue(Double = 0.2) ])
                      ]),
                  "{\"Name\":\"UseDoubleArgWithValues\",\"Arguments\":[{\"Type\":2,\"Name\":\"DoubleArg\",\"Values\":[{\"Double\":[0.1]},{\"Double\":[0.2]}]}]}"))
            .Add("UsePauliArg",
                 (createEntryPointOperation ("UsePauliArg", [ createArgument ("PauliArg", DataType.PauliType, 0, []) ]),
                  "{\"Name\":\"UsePauliArg\",\"Arguments\":[{\"Type\":3,\"Name\":\"PauliArg\"}]}"))
            .Add("UsePauliArgWithValues",
                 (createEntryPointOperation
                     ("UsePauliArgWithValues",
                      [
                          createArgument
                              ("PauliArg",
                               DataType.PauliType,
                               0,
                               [
                                   new ArgumentValue(Pauli = PauliValue.PauliX)
                                   new ArgumentValue(Pauli = PauliValue.PauliY)
                                   new ArgumentValue(Pauli = PauliValue.PauliZ)
                               ])
                      ]),
                  "{\"Name\":\"UsePauliArgWithValues\",\"Arguments\":[{\"Type\":3,\"Name\":\"PauliArg\",\"Values\":[{\"Pauli\":[1]},{\"Pauli\":[2]},{\"Pauli\":[3]}]}]}"))
            .Add("UseRangeArg",
                 (createEntryPointOperation ("UseRangeArg", [ createArgument ("RangeArg", DataType.RangeType, 0, []) ]),
                  "{\"Name\":\"UseRangeArg\",\"Arguments\":[{\"Type\":4,\"Name\":\"RangeArg\"}]}"))
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
                  "{\"Name\":\"UseRangeArgWithValues\",\"Arguments\":[{\"Type\":4,\"Name\":\"RangeArg\",\"Values\":[{\"Range\":[{\"Start\":1,\"Step\":1,\"End\":10}]},{\"Range\":[{\"Start\":10,\"Step\":5,\"End\":100}]}]}]}"))
            .Add("UseResultArg",
                 (createEntryPointOperation
                     ("UseResultArg", [ createArgument ("ResultArg", DataType.ResultType, 0, []) ]),
                  "{\"Name\":\"UseResultArg\",\"Arguments\":[{\"Type\":5,\"Name\":\"ResultArg\"}]}"))
            .Add("UseResultArgWithValues",
                 (createEntryPointOperation
                     ("UseResultArgWithValues",
                      [
                          createArgument
                              ("ResultArg",
                               DataType.ResultType,
                               0,
                               [
                                   new ArgumentValue(Result = ResultValue.Zero)
                                   new ArgumentValue(Result = ResultValue.One)
                               ])
                      ]),
                  "{\"Name\":\"UseResultArgWithValues\",\"Arguments\":[{\"Type\":5,\"Name\":\"ResultArg\",\"Values\":[{\"Result\":[0]},{\"Result\":[1]}]}]}"))
            .Add("UseStringArg",
                 (createEntryPointOperation
                     ("UseStringArg", [ createArgument ("StringArg", DataType.StringType, 0, []) ]),
                  "{\"Name\":\"UseStringArg\",\"Arguments\":[{\"Type\":6,\"Name\":\"StringArg\"}]}"))
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
                  "{\"Name\":\"UseStringArgWithValues\",\"Arguments\":[{\"Type\":6,\"Name\":\"StringArg\",\"Values\":[{\"String\":[\"StringA\"]},{\"String\":[\"StringB\"]}]}]}"))
            .Add("UseBoolArrayArg",
                 (createEntryPointOperation
                     ("UseBoolArrayArg",
                      [
                          createArrayArgument ("BoolArrayArg", DataType.ArrayType, 0, DataType.BoolType, [])
                      ]),
                  "{\"Name\":\"UseBoolArrayArg\",\"Arguments\":[{\"Type\":7,\"Name\":\"BoolArrayArg\",\"ArrayType\":[0]}]}"))
            .Add("UseBoolArrayArgWithValues",
                 (createEntryPointOperation
                     ("UseBoolArrayArgWithValues",
                      [
                          createArrayArgument
                              ("BoolArrayArg",
                               DataType.ArrayType,
                               0,
                               DataType.BoolType,
                               [
                                   createBoolArrayArgumentValue ([ true; false; true ])
                                   createBoolArrayArgumentValue ([ false; true; false ])
                               ])
                      ]),
                  "{\"Name\":\"UseBoolArrayArgWithValues\",\"Arguments\":[{\"Type\":7,\"Name\":\"BoolArrayArg\",\"ArrayType\":[0],\"Values\":[{\"Array\":[{\"Bool\":[[true,false,true]]}]},{\"Array\":[{\"Bool\":[[false,true,false]]}]}]}]}"))
            .Add("UseIntegerArrayArg",
                 (createEntryPointOperation
                     ("UseIntegerArrayArg",
                      [
                          createArrayArgument ("IntegerArrayArg", DataType.ArrayType, 0, DataType.IntegerType, [])
                      ]),
                  "{\"Name\":\"UseIntegerArrayArg\",\"Arguments\":[{\"Type\":7,\"Name\":\"IntegerArrayArg\",\"ArrayType\":[1]}]}"))
            .Add("UseIntegerArrayArgWithValues",
                 (createEntryPointOperation
                     ("UseIntegerArrayArgWithValues",
                      [
                          createArrayArgument
                              ("IntegerArrayArg",
                               DataType.ArrayType,
                               0,
                               DataType.IntegerType,
                               [
                                   createIntegerArrayArgumentValue ([ int64 (999); int64 (-1); int64 (11) ])
                                   createIntegerArrayArgumentValue
                                       ([ int64 (2048); int64 (-1024); int64 (4096); int64 (-8192) ])
                               ])
                      ]),
                  "{\"Name\":\"UseIntegerArrayArgWithValues\",\"Arguments\":[{\"Type\":7,\"Name\":\"IntegerArrayArg\",\"ArrayType\":[1],\"Values\":[{\"Array\":[{\"Integer\":[[999,-1,11]]}]},{\"Array\":[{\"Integer\":[[2048,-1024,4096,-8192]]}]}]}]}"))
            .Add("UseDoubleArrayArg",
                 (createEntryPointOperation
                     ("UseDoubleArrayArg",
                      [
                          createArrayArgument ("DoubleArrayArg", DataType.ArrayType, 0, DataType.DoubleType, [])
                      ]),
                  "{\"Name\":\"UseDoubleArrayArg\",\"Arguments\":[{\"Type\":7,\"Name\":\"DoubleArrayArg\",\"ArrayType\":[2]}]}"))
            .Add("UseDoubleArrayArgWithValues",
                 (createEntryPointOperation
                     ("UseDoubleArrayArgWithValues",
                      [
                          createArrayArgument
                              ("DoubleArrayArg",
                               DataType.ArrayType,
                               0,
                               DataType.DoubleType,
                               [
                                   createDoubleArrayArgumentValue ([ 3.14159; 0.55; 1024.333; -8192.667 ])
                                   createDoubleArrayArgumentValue ([ 999.999; -101010.10; 0.0001 ])
                               ])
                      ]),
                  "{\"Name\":\"UseDoubleArrayArgWithValues\",\"Arguments\":[{\"Type\":7,\"Name\":\"DoubleArrayArg\",\"ArrayType\":[2],\"Values\":[{\"Array\":[{\"Double\":[[3.14159,0.55,1024.333,-8192.667]]}]},{\"Array\":[{\"Double\":[[999.999,-101010.1,0.0001]]}]}]}]}"))
            .Add("UsePauliArrayArg",
                 (createEntryPointOperation
                     ("UsePauliArrayArg",
                      [
                          createArrayArgument ("PauliArrayArg", DataType.ArrayType, 0, DataType.PauliType, [])
                      ]),
                  "{\"Name\":\"UsePauliArrayArg\",\"Arguments\":[{\"Type\":7,\"Name\":\"PauliArrayArg\",\"ArrayType\":[3]}]}"))
            .Add("UsePauliArrayArgWithValues",
                 (createEntryPointOperation
                     ("UsePauliArrayArgWithValues",
                      [
                          createArrayArgument
                              ("PauliArrayArg",
                               DataType.ArrayType,
                               0,
                               DataType.PauliType,
                               [
                                   createPauliArrayArgumentValue
                                       ([ PauliValue.PauliX; PauliValue.PauliY; PauliValue.PauliZ ])
                                   createPauliArrayArgumentValue ([ PauliValue.PauliI; PauliValue.PauliZ ])
                               ])
                      ]),
                  "{\"Name\":\"UsePauliArrayArgWithValues\",\"Arguments\":[{\"Type\":7,\"Name\":\"PauliArrayArg\",\"ArrayType\":[3],\"Values\":[{\"Array\":[{\"Pauli\":[[1,2,3]]}]},{\"Array\":[{\"Pauli\":[[0,3]]}]}]}]}"))
            .Add("UseRangeArrayArg",
                 (createEntryPointOperation
                     ("UseRangeArrayArg",
                      [
                          createArrayArgument ("RangeArrayArg", DataType.ArrayType, 0, DataType.RangeType, [])
                      ]),
                  "{\"Name\":\"UseRangeArrayArg\",\"Arguments\":[{\"Type\":7,\"Name\":\"RangeArrayArg\",\"ArrayType\":[4]}]}"))
            .Add("UseRangeArrayArgWithValues",
                 (createEntryPointOperation
                     ("UseRangeArrayArgWithValues",
                      [
                          createArrayArgument
                              ("RangeArrayArg",
                               DataType.ArrayType,
                               0,
                               DataType.RangeType,
                               [
                                   createRangeArrayArgumentValue
                                       ([ (int64 (1), int64 (1), int64 (10)); (int64 (10), int64 (5), int64 (100)) ])
                                   createRangeArrayArgumentValue ([ (int64 (1), int64 (2), int64 (10)) ])
                               ])
                      ]),
                  "{\"Name\":\"UseRangeArrayArgWithValues\",\"Arguments\":[{\"Type\":7,\"Name\":\"RangeArrayArg\",\"ArrayType\":[4],\"Values\":[{\"Array\":[{\"Range\":[[{\"Start\":1,\"Step\":1,\"End\":10},{\"Start\":10,\"Step\":5,\"End\":100}]]}]},{\"Array\":[{\"Range\":[[{\"Start\":1,\"Step\":2,\"End\":10}]]}]}]}]}"))
            .Add("UseResultArrayArg",
                 (createEntryPointOperation
                     ("UseResultArrayArg",
                      [
                          createArrayArgument ("ResultArrayArg", DataType.ArrayType, 0, DataType.ResultType, [])
                      ]),
                  "{\"Name\":\"UseResultArrayArg\",\"Arguments\":[{\"Type\":7,\"Name\":\"ResultArrayArg\",\"ArrayType\":[5]}]}"))
            .Add("UseResultArrayArgWithValues",
                 (createEntryPointOperation
                     ("UseResultArrayArgWithValues",
                      [
                          createArrayArgument
                              ("ResultArrayArg",
                               DataType.ArrayType,
                               0,
                               DataType.ResultType,
                               [
                                   createResultArrayArgumentValue ([ ResultValue.Zero; ResultValue.One ])
                                   createResultArrayArgumentValue ([ ResultValue.One; ResultValue.Zero ])
                               ])
                      ]),
                  "{\"Name\":\"UseResultArrayArgWithValues\",\"Arguments\":[{\"Type\":7,\"Name\":\"ResultArrayArg\",\"ArrayType\":[5],\"Values\":[{\"Array\":[{\"Result\":[[0,1]]}]},{\"Array\":[{\"Result\":[[1,0]]}]}]}]}"))
            .Add("UseMiscArgs",
                 (createEntryPointOperation
                     ("UseMiscArgs",
                      [
                          createArgument ("IntegerArg", DataType.BoolType, 0, [])
                          createArgument ("PauliArg", DataType.PauliType, 1, [])
                          createArrayArgument ("ResultArrayArg", DataType.ArrayType, 2, DataType.ResultType, [])
                      ]),
                  "{\"Name\":\"UseMiscArgs\",\"Arguments\":[{\"Name\":\"IntegerArg\"},{\"Type\":3,\"Name\":\"PauliArg\",\"Position\":1},{\"Type\":7,\"Name\":\"ResultArrayArg\",\"Position\":2,\"ArrayType\":[5]}]}"))

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
        Assert.True(entryPointOperation.ValueEquals(expectedEntryPointOperation))


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
