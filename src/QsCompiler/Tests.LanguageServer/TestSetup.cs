// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;


namespace Microsoft.Quantum.QsLanguageServer.Testing
{
    public sealed partial class BasicFunctionality : IDisposable
    {
        // basic setup 

        private Connection connection;
        private JsonRpc rpc;
        private readonly RandomInput inputGenerator = new RandomInput();
        private readonly Stack<PublishDiagnosticParams> receivedDiagnostics = new Stack<PublishDiagnosticParams>();

        public Task<string[]> GetFileContentInMemoryAsync(string filename) =>
            rpc.InvokeWithParameterObjectAsync<string[]>(
                Methods.WorkspaceExecuteCommand.Name,
                TestUtils.ServerCommand(CommandIds.FileContentInMemory, TestUtils.GetTextDocumentIdentifier(filename)));

        public Task<Diagnostic[]> GetFileDiagnosticsAsync(string filename = null) =>
            rpc.InvokeWithParameterObjectAsync<Diagnostic[]>(
                Methods.WorkspaceExecuteCommand.Name,
                TestUtils.ServerCommand(CommandIds.FileDiagnostics, filename == null ? new TextDocumentIdentifier { Uri = null } : TestUtils.GetTextDocumentIdentifier(filename)));


        public Task SetupAsync()
        {
            var initParams = TestUtils.GetInitializeParams();
            // Notify, because we should not ever have to wait for completion, except when verifying Initialize itself
            // IMPORTANT: if Initialize throws an exception, this exception will get lost when using Notify, and not result in a test failure!
            return rpc.NotifyWithParameterObjectAsync(Methods.Initialize.Name, initParams);
        }

        public void Dispose()
        {
            rpc?.Dispose();
            this.connection?.Dispose();
        }

        [TestInitialize]
        public async Task SetupServerConnectionAsync()
        {
            Directory.CreateDirectory(RandomInput.testInputDirectory);
            var outputDir = new DirectoryInfo(RandomInput.testInputDirectory);
            foreach (var file in outputDir.GetFiles()) file.Delete(); // deletes the files from previous test runs but not subfolders 

            var id = inputGenerator.GetRandom();
            string ServerReaderPipe = $"QsLanguageServerReaderPipe{id}";
            string ServerWriterPipe = $"QsLanguageServerWriterPipe{id}";
            var readerPipe = new NamedPipeServerStream(ServerWriterPipe, PipeDirection.InOut, 4, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 256, 256);
            var writerPipe = new NamedPipeServerStream(ServerReaderPipe, PipeDirection.InOut, 4, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 256, 256);

            var server = Server.ConnectViaNamedPipe(ServerWriterPipe, ServerReaderPipe);
            await readerPipe.WaitForConnectionAsync().ConfigureAwait(true);
            await writerPipe.WaitForConnectionAsync().ConfigureAwait(true);

            this.connection = new Connection(readerPipe, writerPipe);
            this.rpc = new JsonRpc(connection.Writer, connection.Reader, this)
            { SynchronizationContext = new QsSynchronizationContext() };
            this.rpc.StartListening();
        }

        [TestCleanup]
        public async Task TerminateServerConnectionAsync()
        {
            await this.GetFileDiagnosticsAsync(); // forces a flush in the default compilation manager
            this.receivedDiagnostics.Clear();
            this.Dispose();
        }


        // Methods to listen to server replies

        [JsonRpcMethod(Methods.TextDocumentPublishDiagnosticsName)]
        public void CaptureDiagnostics(JToken arg)
        {
            var param = arg.ToObject<PublishDiagnosticParams>();
            receivedDiagnostics.Push(param);
        }
    }
}