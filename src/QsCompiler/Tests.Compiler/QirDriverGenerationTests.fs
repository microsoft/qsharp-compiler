// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open Xunit
open Xunit.Abstractions
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.BondSchemas.EntryPoint

type QirDriverGenerationTests(output: ITestOutputHelper) =

    [<Fact>]
    member this.GenerateCommandLineArguments() =
        let expectedArguments =
            "--int-value 1 --integer-array 1 2 3 4 5 --double-value 0.5 --double-array 0.1 0.2 0.3 0.4 0.5 --bool-value true --bool-array true true false --pauli-value paulii --pauli-array paulix paulii pauliy pauliz --range-value 1 2 10 --range-array 1 2 10 5 5 50 10 1 20 --result-value 1 --result-array 1 0 1 1 --string-value \"a sample string\" --string-array \"String A\" \"String B\" \"StringC\""

        // Initialize arguments array
        let argList = new ResizeArray<Argument>()
        let numArguments = 13

        for i = 0 to numArguments do
            let arg = new Argument()
            arg.Position <- i
            arg.Values <- new ResizeArray<ArgumentValue>()
            arg.Values.Add(new ArgumentValue())
            argList.Add(arg)
        argList.Item(0).Name <- "int-value"
        argList.Item(0).Type <- DataType.IntegerType
        argList.Item(0).Values.Item(0).Integer <- 1L
        argList.Item(1).Name <- "integer-array"
        argList.Item(1).Type <- DataType.ArrayType
        argList.Item(1).ArrayType <- DataType.IntegerType
        let array1 = new ArrayValue()
        array1.Integer <- [| 1L; 2L; 3L; 4L; 5L |] |> ResizeArray
        argList.Item(1).Values.Item(0).Array <- array1
        argList.Item(2).Name <- "double-value"
        argList.Item(2).Type <- DataType.DoubleType
        argList.Item(2).Values.Item(0).Double <- 0.5
        argList.Item(3).Name <- "double-array"
        argList.Item(3).Type <- DataType.ArrayType
        argList.Item(3).ArrayType <- DataType.DoubleType
        let array3 = new ArrayValue()
        array3.Double <- [| 0.1; 0.2; 0.3; 0.4; 0.5 |] |> ResizeArray
        argList.Item(3).Values.Item(0).Array <- array3
        argList.Item(4).Name <- "bool-value"
        argList.Item(4).Type <- DataType.BoolType
        argList.Item(4).Values.Item(0).Bool <- true
        argList.Item(5).Name <- "bool-array"
        argList.Item(5).Type <- DataType.ArrayType
        argList.Item(5).ArrayType <- DataType.BoolType
        let array5 = new ArrayValue()
        array5.Bool <- [| true; true; false |] |> ResizeArray
        argList.Item(5).Values.Item(0).Array <- array5
        argList.Item(6).Name <- "pauli-value"
        argList.Item(6).Type <- DataType.PauliType
        argList.Item(6).Values.Item(0).Pauli <- PauliValue.PauliI
        argList.Item(7).Name <- "pauli-array"
        argList.Item(7).Type <- DataType.ArrayType
        argList.Item(7).ArrayType <- DataType.PauliType
        let array7 = new ArrayValue()
        array7.Pauli <- [| PauliValue.PauliX; PauliValue.PauliI; PauliValue.PauliY; PauliValue.PauliZ |] |> ResizeArray
        argList.Item(7).Values.Item(0).Array <- array7
        argList.Item(8).Name <- "range-value"
        argList.Item(8).Type <- DataType.RangeType
        let range8 = new RangeValue()
        range8.Start <- 1L
        range8.Step <- 2L
        range8.End <- 10L
        argList.Item(8).Values.Item(0).Range <- range8
        argList.Item(9).Name <- "range-array"
        argList.Item(9).Type <- DataType.ArrayType
        argList.Item(9).ArrayType <- DataType.RangeType
        let range9_0 = new RangeValue()
        range9_0.Start <- 1L
        range9_0.Step <- 2L
        range9_0.End <- 10L
        let range9_1 = new RangeValue()
        range9_1.Start <- 5L
        range9_1.Step <- 5L
        range9_1.End <- 50L
        let range9_2 = new RangeValue()
        range9_2.Start <- 10L
        range9_2.Step <- 1L
        range9_2.End <- 20L
        let array9 = new ArrayValue()
        array9.Range <- [| range9_0; range9_1; range9_2 |] |> ResizeArray
        argList.Item(9).Values.Item(0).Array <- array9
        argList.Item(10).Name <- "result-value"
        argList.Item(10).Type <- DataType.ResultType
        argList.Item(10).Values.Item(0).Result <- ResultValue.One
        argList.Item(11).Name <- "result-array"
        argList.Item(11).Type <- DataType.ArrayType
        argList.Item(11).ArrayType <- DataType.ResultType
        let array11 = new ArrayValue()
        array11.Result <- [| ResultValue.One; ResultValue.Zero; ResultValue.One; ResultValue.One |] |> ResizeArray
        argList.Item(11).Values.Item(0).Array <- array11
        argList.Item(12).Name <- "string-value"
        argList.Item(12).Type <- DataType.StringType
        argList.Item(12).Values.Item(0).String <- "a sample string"
        argList.Item(13).Name <- "string-array"
        argList.Item(13).Type <- DataType.ArrayType
        argList.Item(13).ArrayType <- DataType.StringType
        let array13 = new ArrayValue()
        array13.String <- [| "String A"; "String B"; "StringC" |] |> ResizeArray
        argList.Item(13).Values.Item(0).Array <- array13

        let actualArguments = QirDriverGeneration.GenerateCommandLineArguments(argList)
        Assert.Equal(expectedArguments, actualArguments)

        // Now reverse values in the argument list and ensure that the same result is obtained.
        argList.Reverse()
        let actualArguments = QirDriverGeneration.GenerateCommandLineArguments(argList)
        Assert.Equal(expectedArguments, actualArguments)
