// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module public Microsoft.Quantum.QsCompiler.Testing.SampleExecutionInfoHelper

open System.Collections.Generic
open Microsoft.Quantum.QsCompiler.BondSchemas.Execution

let private createRangeValueFromTuple (tuple: (int64 * int64 * int64)) =
    let rstart, rstep, rend = tuple
    RangeValue(Start = rstart, Step = rstep, End = rend)

let private createRangeArgumentValue (rstart: int64, rstep: int64, rend: int64) =
    let argumentValue = new ArgumentValue(Range = ((rstart, rstep, rend) |> createRangeValueFromTuple))
    argumentValue

let private createArrayArgumentValue (arrayValue: List<ArgumentValue>) = new ArgumentValue(Array = arrayValue)

let private createBoolArrayArgumentValue (lst: System.Nullable<bool> list) =
    let array = new List<ArgumentValue>()
    List.iter (fun v -> array.Add(new ArgumentValue(Bool = v))) lst
    array |> createArrayArgumentValue

let private createIntegerArrayArgumentValue (lst: System.Nullable<int64> list) =
    let array = new List<ArgumentValue>()
    List.iter (fun v -> array.Add(new ArgumentValue(Integer = v))) lst
    array |> createArrayArgumentValue

let private createDoubleArrayArgumentValue (lst: System.Nullable<double> list) =
    let array = new List<ArgumentValue>()
    List.iter (fun v -> array.Add(new ArgumentValue(Double = v))) lst
    array |> createArrayArgumentValue

let private createPauliArrayArgumentValue (lst: System.Nullable<PauliValue> list) =
    let array = new List<ArgumentValue>()
    List.iter (fun v -> array.Add(new ArgumentValue(Pauli = v))) lst
    array |> createArrayArgumentValue

let private createRangeArrayArgumentValue (lst: (int64 * int64 * int64) list) =
    let array = new List<ArgumentValue>()
    List.iter (fun v -> array.Add(createRangeArgumentValue (v))) lst
    array |> createArrayArgumentValue

let private createResultArrayArgumentValue (lst: System.Nullable<ResultValue> list) =
    let array = new List<ArgumentValue>()
    List.iter (fun v -> array.Add(new ArgumentValue(Result = v))) lst
    array |> createArrayArgumentValue

let private createArgument (name: string, argType: DataType, position: int) =
    new Argument(Name = name, Type = argType, Position = position)

let private createArrayArgument (name: string, arrayType: DataType, position: int32) =
    new Argument(Name = name,
                 Type = DataType.ArrayType,
                 Position = position,
                 ArrayType = new System.Nullable<DataType>(arrayType))

let private createEntryPointOperation (name: string, argsList) =
    let entryPointOperation = new EntryPointOperation(Name = name)
    List.iter (fun a -> entryPointOperation.Arguments.Add a) argsList
    entryPointOperation

let private createExecutionInformation (entryPoint: EntryPointOperation,
                                        argumentValues: Dictionary<string, ArgumentValue>) =
    let execution = new ExecutionInformation()
    execution.EntryPoint <- entryPoint
    execution.ArgumentValues <- argumentValues
    execution

let private createMultiArgumentMap (names: string list, values: ArgumentValue list) =
    let map = Dictionary<string, ArgumentValue>()

    for i in 0 .. names.Length - 1 do
        map.Add(names.Item(i), values.Item(i))

    map

let private createArgumentMap (name: string, value: ArgumentValue) =
    createMultiArgumentMap ([ name ], [ value ])

let sampleExecutionInformation =
    Map
        .empty
        .Add("UseNoArgs",
             createExecutionInformation
                 (createEntryPointOperation ("UseNoArgs", []), new Dictionary<string, ArgumentValue>()))
        .Add("UseBoolArgWithValues",
             createExecutionInformation
                 (createEntryPointOperation
                     ("UseBoolArgWithValues", [ createArgument ("BoolArg", DataType.BoolType, 0) ]),
                  createArgumentMap ("BoolArg", new ArgumentValue(Bool = new System.Nullable<bool>(true)))))
        .Add("UseIntegerArgWithValues",
             createExecutionInformation
                 (createEntryPointOperation
                     ("UseIntegerArgWithValues", [ createArgument ("IntegerArg", DataType.IntegerType, 0) ]),
                  createArgumentMap ("IntegerArg", new ArgumentValue(Integer = new System.Nullable<int64>(int64 (11))))))
        .Add("UseDoubleArgWithValues",
             createExecutionInformation
                 (createEntryPointOperation
                     ("UseDoubleArgWithValues", [ createArgument ("DoubleArg", DataType.DoubleType, 0) ]),
                  createArgumentMap ("DoubleArg", new ArgumentValue(Double = new System.Nullable<float>(0.1)))))
        .Add("UsePauliArgWithValues",
             createExecutionInformation
                 (createEntryPointOperation
                     ("UsePauliArgWithValues", [ createArgument ("PauliArg", DataType.PauliType, 0) ]),
                  createArgumentMap
                      ("PauliArg", new ArgumentValue(Pauli = new System.Nullable<PauliValue>(PauliValue.PauliY)))))
        .Add("UseRangeArgWithValues",
             createExecutionInformation
                 (createEntryPointOperation
                     ("UseRangeArgWithValues", [ createArgument ("RangeArg", DataType.RangeType, 0) ]),
                  createArgumentMap ("RangeArg", createRangeArgumentValue (int64 (1), int64 (2), int64 (10)))))
        .Add("UseResultArgWithValues",
             createExecutionInformation
                 (createEntryPointOperation
                     ("UseResultArgWithValues", [ createArgument ("ResultArg", DataType.ResultType, 0) ]),
                  createArgumentMap
                      ("ResultArg", new ArgumentValue(Result = new System.Nullable<ResultValue>(ResultValue.One)))))
        .Add("UseStringArgWithValues",
             createExecutionInformation
                 (createEntryPointOperation
                     ("UseStringArgWithValues", [ createArgument ("StringArg", DataType.StringType, 0) ]),
                  createArgumentMap ("StringArg", new ArgumentValue(String = "String A"))))
        .Add("UseBoolArrayArgWithValues",
             createExecutionInformation
                 (createEntryPointOperation
                     ("UseBoolArrayArgWithValues", [ createArrayArgument ("BoolArrayArg", DataType.BoolType, 0) ]),
                  createArgumentMap
                      ("BoolArrayArg",
                       createBoolArrayArgumentValue
                           ([
                               new System.Nullable<bool>(true)
                               System.Nullable<bool>(false)
                               System.Nullable<bool>(true)
                           ]))))
        .Add("UseIntegerArrayArgWithValues",
             createExecutionInformation
                 (createEntryPointOperation
                     ("UseIntegerArrayArgWithValues",
                      [ createArrayArgument ("IntegerArrayArg", DataType.IntegerType, 0) ]),
                  createArgumentMap
                      ("IntegerArrayArg",
                       createIntegerArrayArgumentValue
                           ([
                               new System.Nullable<int64>(int64 (999))
                               System.Nullable<int64>(int64 (-1))
                               System.Nullable<int64>(int64 (11))
                           ]))))
        .Add("UseDoubleArrayArgWithValues",
             createExecutionInformation
                 (createEntryPointOperation
                     ("UseDoubleArrayArgWithValues", [ createArrayArgument ("DoubleArrayArg", DataType.DoubleType, 0) ]),
                  createArgumentMap
                      ("DoubleArrayArg",
                       createDoubleArrayArgumentValue
                           ([
                               new System.Nullable<double>(3.14159)
                               System.Nullable<double>(0.55)
                               System.Nullable<double>(1024.333)
                               System.Nullable<double>(-8192.667)
                           ]))))
        .Add("UsePauliArrayArgWithValues",
             createExecutionInformation
                 (createEntryPointOperation
                     ("UsePauliArrayArgWithValues", [ createArrayArgument ("PauliArrayArg", DataType.PauliType, 0) ]),
                  createArgumentMap
                      ("PauliArrayArg",
                       createPauliArrayArgumentValue
                           ([
                               new System.Nullable<PauliValue>(PauliValue.PauliI)
                               new System.Nullable<PauliValue>(PauliValue.PauliX)
                               new System.Nullable<PauliValue>(PauliValue.PauliY)
                               new System.Nullable<PauliValue>(PauliValue.PauliZ)
                           ]))))
        .Add("UseRangeArrayArgWithValues",
             createExecutionInformation
                 (createEntryPointOperation
                     ("UseRangeArrayArgWithValues", [ createArrayArgument ("RangeArrayArg", DataType.RangeType, 0) ]),
                  createArgumentMap
                      ("RangeArrayArg",
                       createRangeArrayArgumentValue
                           ([ (int64 (1), int64 (1), int64 (10)); (int64 (10), int64 (5), int64 (100)) ]))))
        .Add("UseResultArrayArgWithValues",
             createExecutionInformation
                 (createEntryPointOperation
                     ("UseResultArrayArgWithValues", [ createArrayArgument ("ResultArrayArg", DataType.ResultType, 0) ]),
                  createArgumentMap
                      ("ResultArrayArg",
                       createResultArrayArgumentValue
                           ([
                               new System.Nullable<ResultValue>(ResultValue.One)
                               new System.Nullable<ResultValue>(ResultValue.Zero)
                           ]))))
        .Add("UseMiscArgs",
             createExecutionInformation
                 (createEntryPointOperation
                     ("UseMiscArgs",
                      [
                          createArgument ("IntegerArg", DataType.IntegerType, 0)
                          createArgument ("StringArg", DataType.StringType, 1)
                      ]),
                  createMultiArgumentMap
                      ([ "IntegerArg"; "StringArg" ],
                       [
                           new ArgumentValue(Integer = new System.Nullable<int64>(int64 (30)))
                           new ArgumentValue(String = "String value")
                       ])))
