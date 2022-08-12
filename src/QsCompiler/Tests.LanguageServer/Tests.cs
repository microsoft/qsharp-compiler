﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Builder = Microsoft.Quantum.QsCompiler.CompilationBuilder.Utils;
using Position = Microsoft.VisualStudio.LanguageServer.Protocol.Position;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

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
        public async Task TargetPackageIntrinsicsAsync()
        {
            var projectFile = ProjectLoaderTests.ProjectUri("test17");
            var projDir = Path.GetDirectoryName(projectFile.AbsolutePath) ?? "";
            var programFile = Path.Combine(projDir, "MeasureBell.qs");

            var initParams = TestUtils.GetInitializeParams();
            initParams.RootUri = new Uri(projDir);
            await this.rpc.NotifyWithParameterObjectAsync(Methods.Initialize.Name, initParams);

            var openParams = TestUtils.GetOpenFileParams(programFile);
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, openParams);
            var diagnostics = await this.GetFileDiagnosticsAsync(programFile);

            // Check that we are not getting any diagnostics,
            // and in particular that we are not ending up with errors for duplicate intrinsics
            // due to the use of target packages.
            Assert.IsNotNull(diagnostics);
            Assert.AreEqual(0, diagnostics!.Length);
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

        [TestMethod]
        public async Task TestLambdaHoverInfoAsync()
        {
            var projectFile = ProjectLoaderTests.ProjectUri("test16");
            var projectManager = await LoadProjectFileAsync(projectFile);
            var lambdaFile = new Uri(projectFile, "Lambda.qs");

            await TestUtils.TestAfterTypeCheckingAsync(projectManager, lambdaFile, () =>
            {
                AssertHover(new Position(2, 12), "x1", "String", true);
                AssertHover(new Position(2, 18), "x1", "Double", true);
                AssertHover(new Position(2, 27), "x1", "Double", false);

                AssertHover(new Position(4, 12), "q1", "Qubit[]", true);
                AssertHover(new Position(4, 24), "x", "Int", true);
                AssertHover(new Position(4, 29), "x", "Int", false);

                AssertHover(new Position(6, 12), "q2", "(Qubit[], Qubit, Qubit[])", true);
                AssertHover(new Position(6, 25), "x", "Int", true);
                AssertHover(new Position(6, 30), "x", "Int", false);
                AssertHover(new Position(6, 58), "x", "Double", true);
                AssertHover(new Position(6, 63), "x", "Double", false);

                AssertHover(new Position(8, 12), "f1", "(Int -> Int)", true);
                AssertHover(new Position(8, 18), "foo", "Int", true);
                AssertHover(new Position(8, 26), "foo", "Int", false);
                AssertHover(new Position(8, 36), "foo", "Int", false);

                AssertHover(new Position(10, 12), "f2", "((Int, Int) -> Int)", true);
                AssertHover(new Position(10, 18), "foo", "Int", true);
                AssertHover(new Position(10, 23), "bar", "Int", true);
                AssertHover(new Position(10, 31), "foo", "Int", false);
                AssertHover(new Position(10, 37), "bar", "Int", false);

                AssertHover(new Position(12, 12), "f3", "(Int -> Int)", true);
                AssertHover(new Position(12, 17), "foo", "Int", true);
                AssertHover(new Position(12, 24), "foo", "Int", false);

                AssertHover(new Position(14, 12), "i", "String", true);
                AssertHover(new Position(14, 18), "i", "Double", true);
                AssertHover(new Position(14, 27), "i", "Double", false);

                AssertHover(new Position(16, 12), "x", "Bool", true);
                AssertHover(new Position(16, 17), "x", "Bool", false);
                AssertHover(new Position(17, 16), "y", "Bool", true);
                AssertHover(new Position(17, 21), "y", "Bool", false);
                AssertHover(new Position(17, 26), "y", "Bool", false);
                AssertHover(new Position(18, 16), "y", "Int", true);
                AssertHover(new Position(18, 21), "y", "Int", false);
            });

            void AssertHover(Position position, string expectedName, string expectedType, bool expectedDeclaration)
            {
                var documentPosition = new TextDocumentPositionParams
                {
                    Position = position,
                    TextDocument = new TextDocumentIdentifier { Uri = lambdaFile! },
                };

                var expected =
                    (expectedDeclaration
                        ? $"Declaration of an immutable variable {expectedName}"
                        : $"Immutable variable {expectedName}")
                    + $"    \nType: {expectedType}";

                var actual = projectManager!.HoverInformation(documentPosition);
                Assert.AreEqual(expected, actual!.Contents.Third.Value);
            }
        }

        [TestMethod]
        public async Task TestLambdaReferencesAsync()
        {
            var projectFile = ProjectLoaderTests.ProjectUri("test16");
            var projectManager = await LoadProjectFileAsync(projectFile);
            var lambdaFile = new Uri(projectFile, "Lambda.qs");

            await TestUtils.TestAfterTypeCheckingAsync(projectManager, lambdaFile, () =>
            {
                AssertReferences(2, new[] { new Position(2, 12) });
                AssertReferences(2, new[] { new Position(2, 18), new Position(2, 27) });

                AssertReferences(2, new[] { new Position(4, 12) });
                AssertReferences(1, new[] { new Position(4, 24), new Position(4, 29) });

                AssertReferences(2, new[] { new Position(6, 12) });
                AssertReferences(1, new[] { new Position(6, 25), new Position(6, 30) });
                AssertReferences(1, new[] { new Position(6, 58), new Position(6, 63) });

                AssertReferences(2, new[] { new Position(8, 12) });
                AssertReferences(3, new[] { new Position(8, 18), new Position(8, 26), new Position(8, 36) });

                AssertReferences(2, new[] { new Position(10, 12) });
                AssertReferences(3, new[] { new Position(10, 18), new Position(10, 31) });
                AssertReferences(3, new[] { new Position(10, 23), new Position(10, 37) });

                AssertReferences(2, new[] { new Position(12, 12) });
                AssertReferences(3, new[] { new Position(12, 17), new Position(12, 24) });

                AssertReferences(1, new[] { new Position(14, 12) });
                AssertReferences(1, new[] { new Position(14, 18), new Position(14, 27) });

                AssertReferences(1, new[] { new Position(16, 12), new Position(16, 17) });
                AssertReferences(1, new[] { new Position(17, 16), new Position(17, 21), new Position(17, 26) });
                AssertReferences(1, new[] { new Position(18, 16), new Position(18, 21) });
                AssertReferences(1, new[] { new Position(19, 16), new Position(19, 21) });

                AssertReferences(3, new[] { new Position(22, 18), new Position(22, 31), new Position(22, 37) });
            });

            void AssertReferences(int symbolLength, IReadOnlyCollection<Position> positions)
            {
                foreach (var position in positions)
                {
                    var reference = new ReferenceParams
                    {
                        Position = position,
                        TextDocument = new TextDocumentIdentifier { Uri = lambdaFile! },
                    };

                    var expected = positions
                        .Select(p => new Location
                        {
                            Uri = lambdaFile!,
                            Range = new Range { Start = p, End = new Position(p.Line, p.Character + symbolLength) },
                        })
                        .ToImmutableHashSet();

                    if (projectManager!.SymbolReferences(reference)?.ToImmutableHashSet() is { } actual)
                    {
                        var expectedStr = string.Join(", ", expected.Select(LocationToString));
                        var actualStr = string.Join(", ", actual.Select(LocationToString));
                        Assert.IsTrue(actual.SetEquals(expected), $"Expected: {expectedStr}. Actual: {actualStr}.");
                    }
                    else
                    {
                        Assert.Fail("Actual references are null.");
                    }
                }
            }

            static string LocationToString(Location location)
            {
                var start = PositionToString(location.Range.Start);
                var end = PositionToString(location.Range.End);
                return $"Range({start} to {end})";
            }

            static string PositionToString(Position p) => $"Ln {p.Line}, Col {p.Character}";
        }

        [TestMethod]
        public async Task NotebookSyntaxRejectedAsync()
        {
            var projectFile = ProjectLoaderTests.ProjectUri("test18");
            var projDir = Path.GetDirectoryName(projectFile.AbsolutePath) ?? "";
            var programFileWithoutNamespace = Path.Combine(projDir, "Bell.qs");
            var programFileWithNamespace = Path.Combine(projDir, "Parity.qs");
            var programFileWithMagic = Path.Combine(projDir, "Magic.qs");

            var initParams = TestUtils.GetInitializeParams();
            initParams.RootUri = new Uri(projDir);
            await this.rpc.NotifyWithParameterObjectAsync(Methods.Initialize.Name, initParams);

            // By default, LanguageId will not contain "notebook"
            var openParams1 = TestUtils.GetOpenFileParams(programFileWithoutNamespace);
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, openParams1);
            Assert.AreEqual(DocumentKind.File, await this.GetFileDocumentKindAsync(programFileWithoutNamespace));
            var diagnostics1 = await this.GetFileDiagnosticsAsync(programFileWithoutNamespace);

            var openParams2 = TestUtils.GetOpenFileParams(programFileWithNamespace);
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, openParams2);
            Assert.AreEqual(DocumentKind.File, await this.GetFileDocumentKindAsync(programFileWithNamespace));
            var diagnostics2 = await this.GetFileDiagnosticsAsync(programFileWithNamespace);

            var openParams3 = TestUtils.GetOpenFileParams(programFileWithMagic);
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, openParams3);
            Assert.AreEqual(DocumentKind.File, await this.GetFileDocumentKindAsync(programFileWithMagic));
            var diagnostics3 = await this.GetFileDiagnosticsAsync(programFileWithMagic);

            Assert.IsNotNull(diagnostics1);
            Assert.AreEqual(3, diagnostics1!.Length);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual("QS4002", diagnostics1[i].Code);
                Assert.AreEqual(DiagnosticSeverity.Error, diagnostics1[i].Severity);
            }

            Assert.IsNotNull(diagnostics2);
            Assert.AreEqual(0, diagnostics2!.Length);

            Assert.AreEqual(1, diagnostics3!.Length);
            Assert.AreEqual("QS3001", diagnostics3[0].Code);
            Assert.AreEqual(DiagnosticSeverity.Error, diagnostics3[0].Severity);
        }

        [TestMethod]
        public async Task NotebookSyntaxAcceptedAsync()
        {
            var projectFile = ProjectLoaderTests.ProjectUri("test18");
            var projDir = Path.GetDirectoryName(projectFile.AbsolutePath) ?? "";
            var programFileWithoutNamespace = Path.Combine(projDir, "Bell.qs");
            var programFileWithNamespace = Path.Combine(projDir, "Parity.qs");
            var programFileWithMagic = Path.Combine(projDir, "Magic.qs");

            // Same value sent by Azure Notebooks
            var languageId = "qsharp-notebook";

            var notebookGuid = Guid.NewGuid();
            var uriWithoutNamespace = TestUtils.GenerateNotebookCellUri(notebookGuid);
            var uriWithNamespace = TestUtils.GenerateNotebookCellUri(notebookGuid);
            var uriWithMagic = TestUtils.GenerateNotebookCellUri(notebookGuid);

            // Azure Notebooks leaves RootUri=null, so do the same here
            var initParams = TestUtils.GetInitializeParams();
            await this.rpc.NotifyWithParameterObjectAsync(Methods.Initialize.Name, initParams);

            var openParams1 = TestUtils.GetOpenFileParams(programFileWithoutNamespace, uriWithoutNamespace, languageId);
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, openParams1);
            Assert.AreEqual(DocumentKind.NotebookCell, await this.GetFileDocumentKindAsync(uri: uriWithoutNamespace));
            var diagnostics1 = await this.GetFileDiagnosticsAsync(uri: uriWithoutNamespace);

            var openParams2 = TestUtils.GetOpenFileParams(programFileWithNamespace, uriWithNamespace, languageId);
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, openParams2);
            Assert.AreEqual(DocumentKind.NotebookCell, await this.GetFileDocumentKindAsync(uri: uriWithNamespace));
            var diagnostics2 = await this.GetFileDiagnosticsAsync(uri: uriWithNamespace);

            var openParams3 = TestUtils.GetOpenFileParams(programFileWithMagic, uriWithMagic, languageId);
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, openParams3);
            Assert.AreEqual(DocumentKind.NotebookCell, await this.GetFileDocumentKindAsync(uri: uriWithMagic));
            var diagnostics3 = await this.GetFileDiagnosticsAsync(uri: uriWithMagic);

            Assert.IsNotNull(diagnostics1);
            Assert.AreEqual(0, diagnostics1!.Length);

            Assert.IsNotNull(diagnostics2);
            Assert.AreEqual(1, diagnostics2!.Length);
            Assert.AreEqual("QS3027", diagnostics2[0].Code);
            Assert.AreEqual(DiagnosticSeverity.Error, diagnostics2[0].Severity);
        }

        [TestMethod]
        public async Task NotebookTypeCheckingAsync()
        {
            var projectFile = ProjectLoaderTests.ProjectUri("test18");
            var projDir = Path.GetDirectoryName(projectFile.AbsolutePath) ?? "";
            var okFile = Path.Combine(projDir, "Bell.qs");
            var badFile = Path.Combine(projDir, "Semantic.qs");

            // Same value sent by Azure Notebooks
            var languageId = "qsharp-notebook";

            var notebookGuid = Guid.NewGuid();
            var uriOk = TestUtils.GenerateNotebookCellUri(notebookGuid);
            var uriBad = TestUtils.GenerateNotebookCellUri(notebookGuid);

            // Azure Notebooks leaves RootUri=null, so do the same here
            var initParams = TestUtils.GetInitializeParams();
            await this.rpc.NotifyWithParameterObjectAsync(Methods.Initialize.Name, initParams);

            var openParamsBad = TestUtils.GetOpenFileParams(badFile, uriBad, languageId);
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, openParamsBad);
            Assert.AreEqual(DocumentKind.NotebookCell, await this.GetFileDocumentKindAsync(uri: uriBad));

            var openParamsOk = TestUtils.GetOpenFileParams(okFile, uriOk, languageId);
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, openParamsOk);
            Assert.AreEqual(DocumentKind.NotebookCell, await this.GetFileDocumentKindAsync(uri: uriOk));
            var diagnosticsOk = await this.GetFileDiagnosticsAsync(uri: uriOk);

            // Doing this second because want to make sure "ok" file is included in type checking
            // for "bad" file, since "bad" file depends on types defined in "ok" file
            var diagnosticsBad = (await this.GetFileDiagnosticsAsync(uri: uriBad))?
                .OrderBy(diag => diag.Range.Start.Line)
                .Select(diag => (diag.Code?.Second, diag.Severity, diag.Range.Start.Line))
                .ToList();

            Assert.IsNotNull(diagnosticsOk);
            Assert.AreEqual(0, diagnosticsOk!.Length);

            Assert.IsNotNull(diagnosticsBad);
            Assert.AreEqual(4, diagnosticsBad!.Count);
            Assert.AreEqual(("QS0001", DiagnosticSeverity.Error, 1), diagnosticsBad[0]);
            Assert.AreEqual(("QS6001", DiagnosticSeverity.Error, 4), diagnosticsBad[1]);
            Assert.AreEqual(("QS0001", DiagnosticSeverity.Error, 9), diagnosticsBad[2]);
            Assert.AreEqual(("QS0001", DiagnosticSeverity.Error, 12), diagnosticsBad[3]);
        }

        [TestMethod]
        public async Task NotebookDocumentHighlightAsync()
        {
            var projectFile = ProjectLoaderTests.ProjectUri("test18");
            var projDir = Path.GetDirectoryName(projectFile.AbsolutePath) ?? "";
            var file = Path.Combine(projDir, "Bell.qs");

            // Same value sent by Azure Notebooks
            var languageId = "qsharp-notebook";

            var notebookGuid = Guid.NewGuid();
            var uri = TestUtils.GenerateNotebookCellUri(notebookGuid);

            // Azure Notebooks leaves RootUri=null, so do the same here
            var initParams = TestUtils.GetInitializeParams();
            await this.rpc.NotifyWithParameterObjectAsync(Methods.Initialize.Name, initParams);

            var openParams = TestUtils.GetOpenFileParams(file, uri, languageId);
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, openParams);
            Assert.AreEqual(DocumentKind.NotebookCell, await this.GetFileDocumentKindAsync(uri: uri));

            var highlightParams = TestUtils.GetDocumentHighlightParams(uri, line: 1, col: 7);
            var ranges = (await this.rpc.InvokeWithParameterObjectAsync<DocumentHighlight[]>(Methods.TextDocumentDocumentHighlight.Name, highlightParams))?
                .Select(highlight => highlight.Range)
                .OrderBy(range => range.Start.Line)
                .ToList();

            Assert.IsNotNull(ranges);
            Assert.AreEqual(2, ranges!.Count);
            Assert.AreEqual(TestUtils.GetRange(startLine: 1, startCol: 4, endLine: 1, endCol: 13), ranges[0]);
            Assert.AreEqual(TestUtils.GetRange(startLine: 4, startCol: 10, endLine: 4, endCol: 19), ranges[1]);
        }

        [TestMethod]
        public async Task NotebookReferencesAsync()
        {
            var projectFile = ProjectLoaderTests.ProjectUri("test19");
            var projDir = Path.GetDirectoryName(projectFile.AbsolutePath) ?? "";
            var refQsFile = Path.Combine(projDir, "exampleReference", "Complex.qs");
            var refDllPath = Path.Combine(this.tempNotebookReferencesDir, "Test19.Imaginary.dll");
            TestUtils.BuildDll(ImmutableArray.Create<string>(refQsFile), refDllPath);

            // Same value sent by Azure Notebooks
            var languageId = "qsharp-notebook";
            var qsFile = Path.Combine(projDir, "NotebookCell.qs");

            var notebookGuid = Guid.NewGuid();
            var uri = TestUtils.GenerateNotebookCellUri(notebookGuid);

            // Azure Notebooks leaves RootUri=null, so do the same here
            var initParams = TestUtils.GetInitializeParams();
            await this.rpc.NotifyWithParameterObjectAsync(Methods.Initialize.Name, initParams);

            var openParams = TestUtils.GetOpenFileParams(qsFile, uri, languageId);
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, openParams);
            Assert.AreEqual(DocumentKind.NotebookCell, await this.GetFileDocumentKindAsync(uri: uri));

            // NotebookCell.qs uses types, functions, and operations referenced in the dll created above
            var diagnostics = await this.GetFileDiagnosticsAsync(uri: uri);
            Assert.IsNotNull(diagnostics);
            Assert.AreEqual(0, diagnostics!.Length);
        }

        [TestMethod]
        public async Task MagicCommandTrackingAsync()
        {
            var initParams = TestUtils.GetInitializeParams();
            await this.rpc.NotifyWithParameterObjectAsync(Methods.Initialize.Name, initParams);

            var notebookGuid = Guid.NewGuid();
            var cellUri = TestUtils.GenerateNotebookCellUri(notebookGuid);

            // Same value sent by Azure Notebooks
            var languageId = "qsharp-notebook";
            var openParams = TestUtils.GetOpenFileParams(cellUri, content: "", languageId);
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidOpen.Name, openParams);
            Assert.AreEqual(DocumentKind.NotebookCell, await this.GetFileDocumentKindAsync(uri: cellUri));

            var diagnosticsBlank = await this.GetFileDiagnosticsAsync(uri: cellUri);
            Assert.IsNotNull(diagnosticsBlank);
            Assert.AreEqual(0, diagnosticsBlank!.Length);

            var goodMagicText = "%simulate myOperation\n";
            var edits = new TextDocumentContentChangeEvent[]
            {
                new TextDocumentContentChangeEvent
                {
                    Range = new Range { Start = new Position(0, 0), End = new Position(0, 0) },
                    Text = goodMagicText,
                },
            };
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidChange.Name, TestUtils.GetChangedParams(cellUri, edits));
            var diagnosticsGoodMagic = await this.GetFileDiagnosticsAsync(uri: cellUri);
            Assert.IsNotNull(diagnosticsGoodMagic);
            Assert.AreEqual(0, diagnosticsGoodMagic!.Length);

            var operationText = "operation hi() : Unit {}";
            var operationTextWithNewlines = operationText + "\n\n";
            edits = new TextDocumentContentChangeEvent[]
            {
                new TextDocumentContentChangeEvent
                {
                    Range = new Range { Start = new Position(0, 0), End = new Position(0, 0) },
                    Text = operationTextWithNewlines,
                },
            };
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidChange.Name, TestUtils.GetChangedParams(cellUri, edits));
            var diagnosticsBadMagic = await this.GetFileDiagnosticsAsync(uri: cellUri);
            Assert.IsNotNull(diagnosticsBadMagic);
            Assert.AreEqual(1, diagnosticsBadMagic!.Length);
            Assert.AreEqual("QS3001", diagnosticsBadMagic[0].Code);
            Assert.AreEqual(DiagnosticSeverity.Error, diagnosticsBadMagic[0].Severity);

            edits = new TextDocumentContentChangeEvent[]
            {
                new TextDocumentContentChangeEvent
                {
                    Range = new Range { Start = new Position(0, 0), End = new Position(0, operationText.Length) },
                    Text = "",
                },
            };
            await this.rpc.InvokeWithParameterObjectAsync<Task>(Methods.TextDocumentDidChange.Name, TestUtils.GetChangedParams(cellUri, edits));
            var diagnosticsRevivedMagic = await this.GetFileDiagnosticsAsync(uri: cellUri);
            Assert.IsNotNull(diagnosticsRevivedMagic);
            Assert.AreEqual(0, diagnosticsRevivedMagic!.Length);
        }

        private static async Task<ProjectManager> LoadProjectFileAsync(Uri uri)
        {
            var projectManager = new ProjectManager(e => throw e);
            await projectManager.LoadProjectsAsync(
                new[] { uri }, CompilationContext.Editor.QsProjectLoader, enableLazyLoading: false);
            return projectManager;
        }
    }
}
