// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Builder = Microsoft.Quantum.QsCompiler.CompilationBuilder.Utils;

namespace Microsoft.Quantum.QsLanguageServer.Testing
{
    [TestClass]
    public partial class BasicFunctionality
    {
        /* server functionality tests */

        [TestMethod]
        public void Connection()
        {
            Assert.IsNotNull(this.connection);
        }

        [TestMethod]
        public async Task ShutdownAsync()
        {
            // the shutdown request should *not* result in the server exiting, and calling multiple times is fine
            var response = await this.rpc.InvokeAsync<object>(Methods.Shutdown.Name);
            response = await this.rpc.InvokeAsync<object>(Methods.Shutdown.Name);
            Assert.IsNull(response);
            Assert.IsNotNull(this.rpc);
        }

        [TestMethod]
        public async Task InitializationAsync()
        {
            // any message sent before the initialization request needs to result in an error
            async Task AssertNotInitializedErrorUponInvokeAsync(string method) // argument here should not matter
            {
                var reply = await this.rpc.InvokeWithParameterObjectAsync<JToken>(method, new object());
                var protocolError = Utils.TryJTokenAs<ProtocolError>(reply);
                Assert.IsNotNull(protocolError);
                if (protocolError != null)
                {
                    Assert.AreEqual(ProtocolError.Codes.AwaitingInitialization, protocolError.Code);
                }
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
            var initReply = await this.rpc.InvokeWithParameterObjectAsync<InitializeResult>(Methods.Initialize.Name, initParams);
            Assert.IsNotNull(initReply);

            var init = await this.rpc.InvokeWithParameterObjectAsync<JToken>(Methods.Initialize.Name, initParams);
            var initializeError = Utils.TryJTokenAs<InitializeError>(init);
            Assert.IsTrue(initializeError != null ? initializeError.Retry : false);
        }

        [TestMethod]
        public async Task ServerCapabilitiesAsync()
        {
            // NOTE: these assertions need to be adapted when the server capabilities are changed
            var initParams = TestUtils.GetInitializeParams();
            Assert.IsNotNull(initParams.Capabilities.Workspace);

            // We use the null-forgiving operator, since we check that Workspace
            // is not null above.
            initParams.Capabilities.Workspace!.ApplyEdit = true;
            var initReply = await this.rpc.InvokeWithParameterObjectAsync<InitializeResult>(Methods.Initialize.Name, initParams);

            Assert.IsNotNull(initReply);
            Assert.IsNotNull(initReply.Capabilities);
            Assert.IsNotNull(initReply.Capabilities.TextDocumentSync);
            Assert.IsNotNull(initReply.Capabilities.TextDocumentSync!.Save);
            Assert.IsNull(initReply.Capabilities.CodeLensProvider);
            Assert.IsNotNull(initReply.Capabilities.CompletionProvider);
            Assert.IsTrue(initReply.Capabilities.CompletionProvider!.ResolveProvider);
            Assert.IsNotNull(initReply.Capabilities.CompletionProvider.TriggerCharacters);
            Assert.IsTrue(initReply.Capabilities.CompletionProvider.TriggerCharacters!.SequenceEqual(new[] { ".", "(" }));
            Assert.IsNotNull(initReply.Capabilities.SignatureHelpProvider?.TriggerCharacters);
            Assert.IsTrue(initReply.Capabilities.SignatureHelpProvider!.TriggerCharacters!.Any());
            Assert.IsNotNull(initReply.Capabilities.ExecuteCommandProvider?.Commands);
            Assert.IsTrue(initReply.Capabilities.ExecuteCommandProvider!.Commands.Contains(CommandIds.ApplyEdit));
            Assert.IsTrue(initReply.Capabilities.TextDocumentSync.OpenClose);
            initReply.Capabilities.TextDocumentSync.Save.AssertCapability(
                true,
                options => options.IncludeText);
            Assert.AreEqual(TextDocumentSyncKind.Incremental, initReply.Capabilities.TextDocumentSync.Change);
            initReply.Capabilities.DefinitionProvider.AssertCapability();
            initReply.Capabilities.ReferencesProvider.AssertCapability();
            initReply.Capabilities.DocumentHighlightProvider.AssertCapability();
            initReply.Capabilities.DocumentSymbolProvider.AssertCapability();
            initReply.Capabilities.WorkspaceSymbolProvider.AssertCapability(shouldHave: false);
            initReply.Capabilities.RenameProvider.AssertCapability();
            initReply.Capabilities.HoverProvider.AssertCapability();
            initReply.Capabilities.DocumentFormattingProvider.AssertCapability(shouldHave: true);
            initReply.Capabilities.DocumentRangeFormattingProvider.AssertCapability(shouldHave: false);
            initReply.Capabilities.CodeActionProvider.AssertCapability();
        }

        [TestMethod]
        public async Task OpenFileAsync()
        {
            var filename = this.inputGenerator.GenerateRandomFile(10, null);
            await this.SetupAsync();
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, TestUtils.GetOpenFileParams(filename));
        }

        [TestMethod]
        public async Task CloseFileAsync()
        {
            var filename = this.inputGenerator.GenerateRandomFile(10, null);
            await this.SetupAsync();

            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, TestUtils.GetOpenFileParams(filename));
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidClose.Name, TestUtils.GetCloseFileParams(filename));

            // verify that a file can be closed immediately after it was opened
            var openTask = this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, TestUtils.GetOpenFileParams(filename));
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidClose.Name, TestUtils.GetCloseFileParams(filename));
        }

        [TestMethod]
        public async Task SaveFileAsync()
        {
            var fileSize = 10;
            var filename = this.inputGenerator.GenerateRandomFile(fileSize, null);
            var content = File.ReadAllText(Path.GetFullPath(filename));
            await this.SetupAsync();

            // verify that safe notification can be sent immediately after sending the open notification (even if the latter has not yet finished processing)
            var openFileTask = this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, TestUtils.GetOpenFileParams(filename));
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidSave.Name, TestUtils.GetSaveFileParams(filename, content));

            // check that the file content is indeed updated on save, according to the passed parameter
            var newContent = string.Join(Environment.NewLine, this.inputGenerator.GetRandomLines(10));
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidSave.Name, TestUtils.GetSaveFileParams(filename, newContent));
            var trackedContent = await this.GetFileContentInMemoryAsync(filename);
            Assert.AreEqual(newContent, Builder.JoinLines(trackedContent));
        }

        [TestMethod]
        public async Task EditFileAsync()
        {
            async Task RunTest(bool emptyLastLine)
            {
                var fileSize = 10;
                var filename = this.inputGenerator.GenerateRandomFile(fileSize, emptyLastLine);
                var content = TestUtils.GetContent(filename);
                await this.SetupAsync();

                // check that edits can be pushed immediately, even if the processing of the initial open command has not yet completed
                // and the file can be closed and no diagnostics are left even if some changes are still queued for processing
                var edits = this.inputGenerator.MakeRandomEdits(50, ref content, fileSize, false);
                Task[] processing = new Task[edits.Length];
                var openFileTask = this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, TestUtils.GetOpenFileParams(filename));
                for (var i = 0; i < edits.Length; ++i)
                {
                    processing[i] = this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidChange.Name, TestUtils.GetChangedFileParams(filename, new[] { edits[i] }));
                }

                var closeFileTask = this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidClose.Name, TestUtils.GetCloseFileParams(filename));
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
            async Task RunTest(bool emptyLastLine, bool useQsExtension)
            {
                var fileSize = 10;
                var filename = this.inputGenerator.GenerateRandomFile(fileSize, emptyLastLine, false, useQsExtension);
                var expectedContent = TestUtils.GetContent(filename);
                var openParams = TestUtils.GetOpenFileParams(filename);
                await this.SetupAsync();
                await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, openParams);

                // check that the file content is accurately reflected upon opening
                var trackedContent = await this.GetFileContentInMemoryAsync(filename);
                var expected = Builder.JoinLines(expectedContent.ToArray());
                var got = Builder.JoinLines(trackedContent);
                Assert.IsNotNull(trackedContent);
                Assert.AreEqual(expectedContent.Count(), trackedContent.Count(), $"expected: \n{expected} \ngot: \n{got}");
                Assert.AreEqual(expected, got);

                // check whether a single array of changes is processed correctly
                var edits = this.inputGenerator.MakeRandomEdits(50, ref expectedContent, fileSize, false);
                await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidChange.Name, TestUtils.GetChangedFileParams(filename, edits));
                trackedContent = await this.GetFileContentInMemoryAsync(filename);
                Assert.AreEqual(expectedContent.Count(), trackedContent.Count());
                Assert.AreEqual(Builder.JoinLines(expectedContent.ToArray()), Builder.JoinLines(trackedContent));

                // check if changes are also processed correctly if many changes (array of length one) are given in rapid succession
                for (var testRep = 0; testRep < 20; ++testRep)
                {
                    edits = this.inputGenerator.MakeRandomEdits(50, ref expectedContent, fileSize, false);

                    Task[] processing = new Task[edits.Length];
                    for (var i = 0; i < edits.Length; ++i)
                    {
                        processing[i] = this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidChange.Name, TestUtils.GetChangedFileParams(filename, new[] { edits[i] }));
                    }

                    for (var i = edits.Length - 1; i >= 0; --i)
                    {
                        await processing[i];
                    }

                    trackedContent = await this.GetFileContentInMemoryAsync(filename);

                    expected = Builder.JoinLines(expectedContent.ToArray());
                    got = Builder.JoinLines(trackedContent);
                    Assert.AreEqual(expectedContent.Count(), trackedContent.Count(), $"expected: \n{expected} \ngot: \n{got}");
                    Assert.AreEqual(expected, got);
                }
            }

            await RunTest(emptyLastLine: true, useQsExtension: true);
            await RunTest(emptyLastLine: true, useQsExtension: false);
            await RunTest(emptyLastLine: false, useQsExtension: true);
            await RunTest(emptyLastLine: false, useQsExtension: false);
        }

        [TestMethod]
        public async Task CodeContentTrackingAsync()
        {
            var fileSize = 10;
            var filename = this.inputGenerator.GenerateRandomFile(fileSize, null, true);
            var expectedContent = TestUtils.GetContent(filename);
            var openParams = TestUtils.GetOpenFileParams(filename);
            await this.SetupAsync();
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, openParams);

            for (var testRep = 0; testRep < 20; ++testRep)
            {
                var edits = this.inputGenerator.MakeRandomEdits(50, ref expectedContent, fileSize, true);

                Task[] processing = new Task[edits.Length];
                for (var i = 0; i < edits.Length; ++i)
                {
                    processing[i] = this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidChange.Name, TestUtils.GetChangedFileParams(filename, new[] { edits[i] }));
                }

                for (var i = edits.Length - 1; i >= 0; --i)
                {
                    await processing[i];
                }

                var trackedContent = await this.GetFileContentInMemoryAsync(filename);

                var expected = Builder.JoinLines(expectedContent.ToArray());
                var got = Builder.JoinLines(trackedContent);
                Assert.IsNotNull(trackedContent);
                Assert.AreEqual(expectedContent.Count(), trackedContent.Count(), $"expected: \n{expected} \ngot: \n{got}");
                Assert.AreEqual(expected, got);
            }
        }

        [TestMethod]
        public async Task UpdateProjectFileAsync()
        {
            var projectFile = ProjectLoaderTests.ProjectUri("test14");
            var projDir = Path.GetDirectoryName(projectFile.AbsolutePath) ?? "";
            var programFile = Path.Combine(projDir, "Teleport.qs");
            var projectFileContent = XDocument.Load(projectFile.AbsolutePath);
            var executionTarget = projectFileContent.Root!.Elements()
                .Where(element => element.Name == "PropertyGroup")
                .SelectMany(element => element.Elements().Where(child => child.Name == "ExecutionTarget"))
                .Single();

            var initParams = TestUtils.GetInitializeParams();
            initParams.RootPath = projDir;
            initParams.RootUri = new Uri(projDir);
            await this.rpc.NotifyWithParameterObjectAsync(Methods.Initialize.Name, initParams);

            var openParams = TestUtils.GetOpenFileParams(programFile);
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, openParams);
            var diagnostics1 = await this.GetFileDiagnosticsAsync(programFile);

            this.projectLoaded.Reset();
            executionTarget.SetValue("honeywell.qpu");
            File.WriteAllText(projectFile.AbsolutePath, projectFileContent.ToString());
            this.projectLoaded.WaitOne();
            var diagnostics2 = await this.GetFileDiagnosticsAsync(programFile);

            this.projectLoaded.Reset();
            executionTarget.SetValue("ionq.qpu");
            File.WriteAllText(projectFile.AbsolutePath, projectFileContent.ToString());
            this.projectLoaded.WaitOne();
            var diagnostics3 = await this.GetFileDiagnosticsAsync(programFile);

            Assert.IsNotNull(diagnostics1);
            Assert.AreEqual(2, diagnostics1!.Length);
            Assert.AreEqual("QS5023", diagnostics1[0].Code);
            Assert.AreEqual(DiagnosticSeverity.Error, diagnostics1[0].Severity);
            Assert.AreEqual("QS5023", diagnostics1[1].Code);
            Assert.AreEqual(DiagnosticSeverity.Error, diagnostics1[1].Severity);

            Assert.IsNotNull(diagnostics2);
            Assert.AreEqual(0, diagnostics2!.Length);

            Assert.IsNotNull(diagnostics3);
            Assert.AreEqual(2, diagnostics3!.Length);
            Assert.AreEqual("QS5023", diagnostics3[0].Code);
            Assert.AreEqual(DiagnosticSeverity.Error, diagnostics3[0].Severity);
            Assert.AreEqual("QS5023", diagnostics3[1].Code);
            Assert.AreEqual(DiagnosticSeverity.Error, diagnostics3[1].Severity);
        }

        [TestMethod]
        public async Task UpdateAndFormatAsync()
        {
            async Task RunFormattingTestAsync(string projectName)
            {
                var projectFile = ProjectLoaderTests.ProjectUri(projectName);
                var projDir = Path.GetDirectoryName(projectFile.LocalPath) ?? "";
                var telemetryEvents = new List<(string, int)>();
                var projectManager = await projectFile.LoadProjectAsync(
                    (eventName, _, measures) => telemetryEvents.Add((eventName, measures.TryGetValue("totalEdits", out var nrEdits) ? nrEdits : 0)));

                var fileToFormat = new Uri(Path.Combine(projDir, "format", "Unformatted.qs"));
                var expectedContent = File.ReadAllText(Path.Combine(projDir, "format", "Formatted.qs"));
                var param = new DocumentFormattingParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = fileToFormat },
                    Options = new FormattingOptions { TabSize = 2, InsertSpaces = false, OtherOptions = new Dictionary<string, object>() },
                };

                var edits = projectManager.Formatting(param);
                Assert.IsNotNull(edits);
                Assert.AreEqual(1, edits!.Length);
                Assert.AreEqual(expectedContent, edits[0].NewText);
                Assert.AreEqual(1, telemetryEvents.Count());
                Assert.AreEqual("formatting", telemetryEvents[0].Item1);
                Assert.AreEqual(edits.Length, telemetryEvents[0].Item2);
            }

            await RunFormattingTestAsync("test12");
            await RunFormattingTestAsync("test15");
        }
    }
}
