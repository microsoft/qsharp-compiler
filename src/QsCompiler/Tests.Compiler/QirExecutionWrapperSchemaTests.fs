// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System.Collections.Generic
open System.IO
open Bond
open Xunit
open Xunit.Abstractions
open Microsoft.Quantum.QsCompiler.BondSchemas.EntryPoint
open Microsoft.Quantum.QsCompiler.BondSchemas.QirExecutionWrapper

type QirExecutionWrapperSchemaTests(output: ITestOutputHelper) =

    let createEntryPointOperation() =
        let expectedEntryPointOperation = new EntryPointOperation()
        let argument = new Argument()
        argument.Name <- "argument name"
        argument.Position <- 0
        let arguments = new List<Argument>()
        arguments.Add(argument)
        expectedEntryPointOperation.Arguments <- arguments
        expectedEntryPointOperation.Name <- "operation name"
        expectedEntryPointOperation

    let createBytecode() =
        let bytes = Array.init 10 (fun i -> byte(i*i))
        let bytecode = new Bytecode()
        bytecode.Data <- new System.ArraySegment<byte>(bytes)
        bytecode

    [<Fact>]
    member this.SerializeAndDeserializeInFastBinary() =
        let qirWrapper = new QirExecutionWrapper()
        let bytecode = createBytecode()
        let entryPoint = createEntryPointOperation()
        qirWrapper.EntryPoint <- entryPoint
        qirWrapper.QirBytes <- Bonded<Bytecode>(bytecode)
        let memoryStream = new MemoryStream()

        // Serialize input.
        Protocols.SerializeToFastBinary(qirWrapper, memoryStream)

        // Deserialize and confirm that it is correct.
        let deserializedQirWrapper = Protocols.DeserializeFromFastBinary(memoryStream)
        Assert.True(Extensions.ValueEquals(deserializedQirWrapper, qirWrapper))
