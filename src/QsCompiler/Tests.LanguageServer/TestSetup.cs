﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace Microsoft.Quantum.QsLanguageServer.Testing
{
    public sealed partial class BasicFunctionality : IDisposable
    {
        /* basic setup */

        private Connection? connection;
        private JsonRpc rpc = null!; // Initialized in SetupServerConnectionAsync.
        private string tempNotebookReferencesDir = null!; // Initialized in SetupServerConnectionAsync.
        private readonly RandomInput inputGenerator = new RandomInput();
        private readonly Stack<PublishDiagnosticParams> receivedDiagnostics = new Stack<PublishDiagnosticParams>();
        private readonly ManualResetEvent projectLoaded = new ManualResetEvent(false);

        public Task<DocumentKind> GetFileDocumentKindAsync(string? filename = null, Uri? uri = null) =>
            this.rpc.InvokeWithParameterObjectAsync<DocumentKind>(
                Methods.WorkspaceExecuteCommand.Name,
                TestUtils.ServerCommand(CommandIds.FileDocumentKind, filename == null ? new TextDocumentIdentifier { Uri = uri ?? new Uri("file://unknown") } : TestUtils.GetTextDocumentIdentifier(filename)));

        public Task<string[]> GetFileContentInMemoryAsync(string filename) =>
            this.rpc.InvokeWithParameterObjectAsync<string[]>(
                Methods.WorkspaceExecuteCommand.Name,
                TestUtils.ServerCommand(CommandIds.FileContentInMemory, TestUtils.GetTextDocumentIdentifier(filename)));

        public Task<Diagnostic[]> GetFileDiagnosticsAsync(string? filename = null, Uri? uri = null) =>
            this.rpc.InvokeWithParameterObjectAsync<Diagnostic[]>(
                Methods.WorkspaceExecuteCommand.Name,
                TestUtils.ServerCommand(CommandIds.FileDiagnostics, filename == null ? new TextDocumentIdentifier { Uri = uri ?? new Uri("file://unknown") } : TestUtils.GetTextDocumentIdentifier(filename)));

        public Task SetupAsync()
        {
            var initParams = TestUtils.GetInitializeParams();

            // Notify, because we should not ever have to wait for completion, except when verifying Initialize itself
            // IMPORTANT: if Initialize throws an exception, this exception will get lost when using Notify, and not result in a test failure!
            return this.rpc.NotifyWithParameterObjectAsync(Methods.Initialize.Name, initParams);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.rpc?.Dispose();
            this.connection?.Dispose();
        }

        [TestInitialize]
        public async Task SetupServerConnectionAsync()
        {
            // Need to run MSBuildLocator for some tests like UpdateProjectFileAsync
            _ = VisualStudioInstanceWrapper.LazyVisualStudioInstance.Value;

            Directory.CreateDirectory(RandomInput.TestInputDirectory);
            var outputDir = new DirectoryInfo(RandomInput.TestInputDirectory);
            foreach (var file in outputDir.GetFiles())
            {
                file.Delete(); // deletes the files from previous test runs but not subfolders
            }

            this.tempNotebookReferencesDir = TestUtils.MakeTempDirectory();
            Environment.SetEnvironmentVariable("QSHARP_NOTEBOOK_REFERENCES_DIR", this.tempNotebookReferencesDir);

            var id = this.inputGenerator.GetRandom();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string serverReaderPipe = $"QsLanguageServerReaderPipe{id}";
                string serverWriterPipe = $"QsLanguageServerWriterPipe{id}";
                var readerPipe = new NamedPipeServerStream(serverWriterPipe, PipeDirection.InOut, 4, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 256, 256);
                var writerPipe = new NamedPipeServerStream(serverReaderPipe, PipeDirection.InOut, 4, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 256, 256);

                var server = Server.ConnectViaNamedPipe(serverWriterPipe, serverReaderPipe);
                await readerPipe.WaitForConnectionAsync().ConfigureAwait(true);
                await writerPipe.WaitForConnectionAsync().ConfigureAwait(true);
                this.connection = new Connection(readerPipe, writerPipe);
            }
            else
            {
                var readerPipe = new System.IO.Pipelines.Pipe();
                var writerPipe = new System.IO.Pipelines.Pipe();

                var server = new QsLanguageServer(sender: writerPipe.Writer.AsStream(), reader: readerPipe.Reader.AsStream());
                this.connection = new Connection(writerPipe.Reader.AsStream(), readerPipe.Writer.AsStream());
            }

            this.rpc = new JsonRpc(this.connection.Writer, this.connection.Reader, this)
            { SynchronizationContext = new QsSynchronizationContext() };
            this.rpc.StartListening();
        }

        [TestCleanup]
        public async Task TerminateServerConnectionAsync()
        {
            Directory.Delete(this.tempNotebookReferencesDir, true);
            await this.GetFileDiagnosticsAsync(); // forces a flush in the default compilation manager
            this.receivedDiagnostics.Clear();
            this.Dispose();
        }

        /* Methods to listen to server replies */

        [JsonRpcMethod(Methods.TextDocumentPublishDiagnosticsName)]
        public void CaptureDiagnostics(JToken arg)
        {
            var param = arg.ToObject<PublishDiagnosticParams>();
            if (param != null)
            {
                this.receivedDiagnostics.Push(param);
            }
        }

        [JsonRpcMethod(Methods.WindowLogMessageName)]
        public void LogToWindow(JToken arg)
        {
            var param = arg.ToObject<LogMessageParams>();
            if (param != null)
            {
                Console.WriteLine($"[{param.MessageType}]: {param.Message}");
                if (param.Message.StartsWith("Done loading project"))
                {
                    this.projectLoaded.Set();
                }
            }
        }
    }
}
