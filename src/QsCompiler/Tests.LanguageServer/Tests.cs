// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Builder = Microsoft.Quantum.QsCompiler.CompilationBuilder.Utils;


namespace Microsoft.Quantum.QsLanguageServer.Testing
{

    [TestClass]
    public partial class BasicFunctionality
    {
        // server functionality tests

        [TestMethod]
        public void Connection()
        { Assert.IsNotNull(connection); }

        [TestMethod]
        public async Task ShutdownAsync()
        {
            // the shutdown request should *not* result in the server exiting, and calling multiple times is fine
            var response = await rpc.InvokeAsync<object>(Methods.Shutdown.Name);
            response = await rpc.InvokeAsync<object>(Methods.Shutdown.Name);
            Assert.IsNull(response);
            Assert.IsNotNull(this.rpc);
        }

        [TestMethod]
        public async Task InitializationAsync()
        {
            // any message sent before the initialization request needs to result in an error

            async Task AssertNotInitializedErrorUponInvokeAsync(string method)  // argument here should not matter
            {
                var reply = await rpc.InvokeWithParameterObjectAsync<JToken>(method, new object());
                Assert.AreEqual(ProtocolError.Codes.AwaitingInitialization, Utils.TryJTokenAs<ProtocolError>(reply).Code);
            }
            
            await AssertNotInitializedErrorUponInvokeAsync(Methods.TextDocumentHover.Name);
            await AssertNotInitializedErrorUponInvokeAsync(Methods.TextDocumentSignatureHelp.Name);
            await AssertNotInitializedErrorUponInvokeAsync(Methods.TextDocumentDefinition.Name);
            await AssertNotInitializedErrorUponInvokeAsync(Methods.TextDocumentReferences.Name);
            await AssertNotInitializedErrorUponInvokeAsync(Methods.TextDocumentDocumentSymbol.Name);
            await AssertNotInitializedErrorUponInvokeAsync(Methods.TextDocumentRename.Name);
            await AssertNotInitializedErrorUponInvokeAsync(Methods.TextDocumentCodeAction.Name);

            // the initialization request may only be sent once according to speccs 
            // -> the content of initReply is verified in the ServerCapabilities test

            var initParams = TestUtils.GetInitializeParams();
            var initReply = await rpc.InvokeWithParameterObjectAsync<InitializeResult>(Methods.Initialize.Name, initParams);
            Assert.IsNotNull(initReply);

            var init = await rpc.InvokeWithParameterObjectAsync<JToken>(Methods.Initialize.Name, initParams);
            Assert.IsTrue(Utils.TryJTokenAs<InitializeError>(init).Retry);
        }

        [TestMethod]
        public async Task ServerCapabilitiesAsync()
        {
            // NOTE: these assertions need to be adapted when the server capabilities are changed
            var initParams = TestUtils.GetInitializeParams();
            initParams.Capabilities.Workspace.ApplyEdit = true;
            var initReply = await rpc.InvokeWithParameterObjectAsync<InitializeResult>(Methods.Initialize.Name, initParams);

            Assert.IsNotNull(initReply);
            Assert.IsNotNull(initReply.Capabilities);
            Assert.IsNotNull(initReply.Capabilities.TextDocumentSync);
            Assert.IsNotNull(initReply.Capabilities.TextDocumentSync.Save);
            Assert.IsNull(initReply.Capabilities.CodeLensProvider);
            Assert.IsNotNull(initReply.Capabilities.CompletionProvider);
            Assert.IsFalse(initReply.Capabilities.CompletionProvider.ResolveProvider);
            Assert.IsNotNull(initReply.Capabilities.CompletionProvider.TriggerCharacters);
            Assert.AreEqual(initReply.Capabilities.CompletionProvider.TriggerCharacters.Length, 0);
            Assert.IsNotNull(initReply.Capabilities.SignatureHelpProvider?.TriggerCharacters);
            Assert.IsTrue(initReply.Capabilities.SignatureHelpProvider.TriggerCharacters.Any());
            Assert.IsNotNull(initReply.Capabilities.ExecuteCommandProvider?.Commands);
            Assert.IsNotNull(initReply.Capabilities.ExecuteCommandProvider.Commands.Contains(CommandIds.ApplyEdit));
            Assert.IsTrue(initReply.Capabilities.TextDocumentSync.OpenClose);
            Assert.IsTrue(initReply.Capabilities.TextDocumentSync.Save.IncludeText);
            Assert.AreEqual(TextDocumentSyncKind.Incremental, initReply.Capabilities.TextDocumentSync.Change);
            Assert.IsTrue(initReply.Capabilities.DefinitionProvider);
            Assert.IsTrue(initReply.Capabilities.ReferencesProvider);
            Assert.IsTrue(initReply.Capabilities.DocumentHighlightProvider);
            Assert.IsTrue(initReply.Capabilities.DocumentSymbolProvider);
            Assert.IsFalse(initReply.Capabilities.WorkspaceSymbolProvider);
            Assert.IsTrue(initReply.Capabilities.RenameProvider);
            Assert.IsTrue(initReply.Capabilities.HoverProvider);
            Assert.IsFalse(initReply.Capabilities.DocumentFormattingProvider);
            Assert.IsFalse(initReply.Capabilities.DocumentRangeFormattingProvider);
            Assert.IsTrue(initReply.Capabilities.CodeActionProvider);
        }

        [TestMethod]
        public async Task OpenFileAsync()
        {
            var filename = inputGenerator.GenerateRandomFile(10, null);
            await SetupAsync();
            await rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, TestUtils.GetOpenFileParams(filename));
        }

        [TestMethod]
        public async Task CloseFileAsync()
        {
            var filename = inputGenerator.GenerateRandomFile(10, null);
            await SetupAsync();

            await rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, TestUtils.GetOpenFileParams(filename));
            await rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidClose.Name, TestUtils.GetCloseFileParams(filename));

            // verify that a file can be closed immediately after it was opened
            var openTask = rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, TestUtils.GetOpenFileParams(filename));
            await rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidClose.Name, TestUtils.GetCloseFileParams(filename));
        }

        [TestMethod]
        public async Task SaveFileAsync()
        {
            var fileSize = 10;
            var filename = inputGenerator.GenerateRandomFile(fileSize, null);
            var content = File.ReadAllText(Path.GetFullPath(filename));
            await SetupAsync();

            // verify that safe notification can be sent immediately after sending the open notification (even if the latter has not yet finished processing)

            var openFileTask = rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, TestUtils.GetOpenFileParams(filename));
            await rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidSave.Name, TestUtils.GetSaveFileParams(filename, content));

            // check that the file content is indeed updated on save, according to the passed parameter 

            var newContent = String.Join(Environment.NewLine, inputGenerator.GetRandomLines(10));
            await rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidSave.Name, TestUtils.GetSaveFileParams(filename, newContent));
            var trackedContent = await this.GetFileContentInMemoryAsync(filename);
            Assert.AreEqual(newContent, Builder.JoinLines(trackedContent));
        }

        [TestMethod]
        public async Task EditFileAsync()
        {
            async Task RunTest(bool emptyLastLine)
            {
                var fileSize = 10;
                var filename = inputGenerator.GenerateRandomFile(fileSize, emptyLastLine);
                var content = TestUtils.GetContent(filename);
                await SetupAsync();

                // check that edits can be pushed immediately, even if the processing of the initial open command has not yet completed
                // and the file can be closed and no diagnostics are left even if some changes are still queued for processing

                var edits = inputGenerator.MakeRandomEdits(50, ref content, fileSize, false);
                Task[] processing = new Task[edits.Length];
                var openFileTask = rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, TestUtils.GetOpenFileParams(filename));
                for (var i = 0; i < edits.Length; ++i)
                { processing[i] = rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidChange.Name, TestUtils.GetChangedFileParams(filename, new[] { edits[i] })); }
                var closeFileTask = rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidClose.Name, TestUtils.GetCloseFileParams(filename));
                var finalDiagnostics = await this.GetFileDiagnosticsAsync(filename);

                // check that the file is no longer present in the default manager after closing (final diagnostics are null), and
                // check that empty diagnostics for the now closed file are the last diagnostics that have been pushed
                // note that the number of received diagnostics depends on how many changes have been queued/aggregated before processing

                Assert.IsNull(finalDiagnostics);
                Assert.IsTrue(this.receivedDiagnostics.Any());
                var lastReceived = this.receivedDiagnostics.Pop();
                Assert.IsNotNull(lastReceived);
                Assert.AreEqual(TestUtils.GetUri(filename), lastReceived.Uri);
                Assert.IsFalse(lastReceived.Diagnostics.Any());
            }
            await RunTest(emptyLastLine: true);
            await RunTest(emptyLastLine: false);
        }

        [TestMethod]
        public async Task TextContentTrackingAsync()
        {
            async Task RunTest(bool emptyLastLine)
            {
                var fileSize = 10;
                var filename = inputGenerator.GenerateRandomFile(fileSize, emptyLastLine, false);
                var expectedContent = TestUtils.GetContent(filename);
                var openParams = TestUtils.GetOpenFileParams(filename);
                await SetupAsync();
                await rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, openParams);

                // check that the file content is accurately reflected upon opening 

                var trackedContent = await this.GetFileContentInMemoryAsync(filename);
                var expected = Builder.JoinLines(expectedContent.ToArray());
                var got = Builder.JoinLines(trackedContent);
                Assert.IsNotNull(trackedContent);
                Assert.AreEqual(expectedContent.Count(), trackedContent.Count(), $"expected: \n{expected} \ngot: \n{got}");
                Assert.AreEqual(expected, got);

                // check whether a single array of changes is processed correctly

                var edits = inputGenerator.MakeRandomEdits(50, ref expectedContent, fileSize, false);
                await rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidChange.Name, TestUtils.GetChangedFileParams(filename, edits));
                trackedContent = await this.GetFileContentInMemoryAsync(filename);
                Assert.AreEqual(expectedContent.Count(), trackedContent.Count());
                Assert.AreEqual(Builder.JoinLines(expectedContent.ToArray()), Builder.JoinLines(trackedContent));

                // check if changes are also processed correctly if many changes (array of length one) are given in rapid succession

                for (var testRep = 0; testRep < 20; ++testRep)
                {
                    edits = inputGenerator.MakeRandomEdits(50, ref expectedContent, fileSize, false);

                    Task[] processing = new Task[edits.Length];
                    for (var i = 0; i < edits.Length; ++i)
                    { processing[i] = rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidChange.Name, TestUtils.GetChangedFileParams(filename, new[] { edits[i] })); }

                    for (var i = edits.Length - 1; i >= 0; --i) await processing[i];
                    trackedContent = await this.GetFileContentInMemoryAsync(filename);

                    expected = Builder.JoinLines(expectedContent.ToArray());
                    got = Builder.JoinLines(trackedContent);
                    Assert.AreEqual(expectedContent.Count(), trackedContent.Count(), $"expected: \n{expected} \ngot: \n{got}");
                    Assert.AreEqual(expected, got);
                }
            }
            await RunTest(emptyLastLine: true);
            await RunTest(emptyLastLine: false);
        }

        [TestMethod]
        public async Task CodeContentTrackingAsync()
        {
            var fileSize = 10;
            var filename = inputGenerator.GenerateRandomFile(fileSize, null, true);
            var expectedContent = TestUtils.GetContent(filename);
            var openParams = TestUtils.GetOpenFileParams(filename);
            await SetupAsync();
            await rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, openParams);

            for (var testRep = 0; testRep < 20; ++testRep)
            {
                var edits = inputGenerator.MakeRandomEdits(50, ref expectedContent, fileSize, true);

                Task[] processing = new Task[edits.Length];
                for (var i = 0; i < edits.Length; ++i)
                { processing[i] = rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidChange.Name, TestUtils.GetChangedFileParams(filename, new[] { edits[i] })); }

                for (var i = edits.Length - 1; i >= 0; --i) await processing[i];
                var trackedContent = await this.GetFileContentInMemoryAsync(filename);

                var expected = Builder.JoinLines(expectedContent.ToArray());
                var got = Builder.JoinLines(trackedContent);
                Assert.IsNotNull(trackedContent);
                Assert.AreEqual(expectedContent.Count(), trackedContent.Count(), $"expected: \n{expected} \ngot: \n{got}");
                Assert.AreEqual(expected, got);
            }
        }
    }
}
