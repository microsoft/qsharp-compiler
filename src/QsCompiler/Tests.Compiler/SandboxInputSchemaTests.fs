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
open Microsoft.Quantum.QsCompiler.BondSchemas.SandboxInput

type SandboxInputSchemaTests(output: ITestOutputHelper) =

    let createEntryPointOperation() =
        let expectedEntryPointOperation = new EntryPointOperation()
        let argument = new Argument()
        argument.Name <- "name"
        argument.Position <- 44
        let arguments = new List<Argument>()
        arguments.Add(argument)
        expectedEntryPointOperation.Arguments <- arguments
        expectedEntryPointOperation.Name <- "other name"
        expectedEntryPointOperation

    let createBytecode() =
        let bytes = Array.init 10 (fun i -> byte(i*i))
        let bytecode = new Bytecode()
        bytecode.Data <- new System.ArraySegment<byte>(bytes)
        bytecode

    [<Fact>]
    member this.SerializeAndDeserializeInFastBinary() =
        let sandboxInput = new Input()
        let bytecode = createBytecode()
        let entryPoint = createEntryPointOperation()
        sandboxInput.EntryPoint <- entryPoint
        sandboxInput.QirBytes <- Bonded<Bytecode>(bytecode)
        let memoryStream = new MemoryStream(15)

        // Serialize input.
        Protocols.SerializeToFastBinary(sandboxInput, memoryStream)

        // Deserialize and confirm that it is correct.
        let deserializedInput = Protocols.DeserializeFromFastBinary(memoryStream)
        Assert.True(Extensions.ValueEquals(deserializedInput.EntryPoint, entryPoint))
        let deserializedBytecode = deserializedInput.QirBytes.Deserialize()
        Assert.Equal(deserializedBytecode.Data.Count, bytecode.Data.Count)
        for i in 0..bytecode.Data.Count - 1 do
            Assert.Equal(deserializedBytecode.Data.Item(i), bytecode.Data.Item(i))
