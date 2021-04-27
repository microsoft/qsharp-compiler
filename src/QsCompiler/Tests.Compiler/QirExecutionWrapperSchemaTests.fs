// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System.Collections.Generic
open System.IO
open Bond
open Xunit
open Xunit.Abstractions
open Microsoft.Quantum.QsCompiler.BondSchemas.Execution

type QirExecutionWrapperSchemaTests(output: ITestOutputHelper) =

    let createExecutionInformation () =
        let expectedEntryPointOperation = new EntryPointOperation()
        let argument = new Argument()
        argument.Name <- "argument name"
        argument.Position <- 0
        let arguments = new List<Argument>()
        arguments.Add(argument)
        expectedEntryPointOperation.Arguments <- arguments
        expectedEntryPointOperation.Name <- "operation name"
        let executionInformation = new ExecutionInformation()
        executionInformation.EntryPoint <- expectedEntryPointOperation
        executionInformation.ArgumentValues <- new Dictionary<string, ArgumentValue>()

        executionInformation.ArgumentValues.["argument name"] <- new ArgumentValue(Integer =
            new System.Nullable<int64>(int64 (4)))

        executionInformation

    let createBytecode () =
        let bytes = Array.init 10 (fun i -> byte (i * i))
        new System.ArraySegment<byte>(bytes)

    [<Fact>]
    member this.SerializeAndDeserializeInFastBinary() =
        let qirWrapper = new QirExecutionWrapper()
        let bytecode = createBytecode ()
        qirWrapper.Executions <- new List<ExecutionInformation>()
        qirWrapper.Executions.Add(createExecutionInformation ())
        qirWrapper.QirBytecode <- bytecode
        let memoryStream = new MemoryStream()

        // Serialize input.
        Protocols.SerializeQirExecutionWrapperToFastBinary(qirWrapper, memoryStream)

        // Deserialize and confirm that it is correct.
        let deserializedQirWrapper = Protocols.DeserializeQirExecutionWrapperFromFastBinary(memoryStream)
        Assert.True(Extensions.ValueEquals(deserializedQirWrapper, qirWrapper))
