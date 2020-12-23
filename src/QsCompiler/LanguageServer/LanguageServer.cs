// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.QsCompiler.SymbolManagement;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace Microsoft.Quantum.QsLanguageServer
{
    public class QsLanguageServer : IDisposable
    {
        // properties required for basic functionality

        private readonly JsonRpc rpc;
        private readonly ManualResetEvent disconnectEvent; // used to keep the server running until it is no longer needed
        private ManualResetEvent? waitForInit; // set to null after initialization

        internal bool ReadyForExit { get; private set; }

        private readonly System.Timers.Timer internalErrorTimer; // used to avoid spamming users with a lot of errors at once
        private bool showInteralErrorMessage = true; // set via timer as needed

        private string? workspaceFolder = null;
        private readonly HashSet<Uri> projectsInWorkspace;
        private readonly FileWatcher fileWatcher;
        private readonly CoalesceingQueue fileEvents;

        private string? clientName;
        private Version? clientVersion;
        private ClientCapabilities? clientCapabilities;
        private readonly EditorState editorState;

        /// <summary>
        /// Returns true if the client name matches the given name.
        /// </summary>
        private bool ClientNameIs(string name) =>
            name != null && name.Equals(this.clientName, StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        /// Returns true if the client version is the same as the given version or later, or if the client version is
        /// unknown.
        /// </summary>
        private bool ClientVersionIsAtLeast(Version version) =>
            version == null || this.clientVersion == null || this.clientVersion >= version;

        /// <summary>
        /// helper function that selects a markup format from the given array of supported formats
        /// </summary>
        private MarkupKind ChooseFormat(MarkupKind[]? supportedFormats) =>
            supportedFormats?.Any() ?? false
                ? supportedFormats.Contains(MarkupKind.Markdown) ? MarkupKind.Markdown : supportedFormats.First()
                : MarkupKind.PlainText;

        // methods required for basic functionality

        public QsLanguageServer(Stream? sender, Stream? reader)
        {
            this.waitForInit = new ManualResetEvent(false);
            this.rpc = new JsonRpc(sender, reader, this)
            {
                SynchronizationContext = new QsSynchronizationContext()
            };
            this.rpc.StartListening();
            this.disconnectEvent = new ManualResetEvent(false);
            this.rpc.Disconnected += (object? s, JsonRpcDisconnectedEventArgs e) => { this.disconnectEvent.Set(); }; // let's make the server exit if the stream is disconnected
            this.ReadyForExit = false;

            this.internalErrorTimer = new System.Timers.Timer(60000);
            this.internalErrorTimer.Elapsed += (_, __) => { this.showInteralErrorMessage = true; };
            this.internalErrorTimer.AutoReset = false;

            void ProcessFileEvents(IEnumerable<FileEvent> e) =>
                this.OnDidChangeWatchedFiles(JToken.Parse(JsonConvert.SerializeObject(
                    new DidChangeWatchedFilesParams { Changes = e.ToArray() })));
            this.fileWatcher = new FileWatcher(_ =>
                this.LogToWindow($"FileSystemWatcher encountered and error", MessageType.Error));
            var fileEvents = Observable.FromEvent<FileWatcher.FileEventHandler, FileEvent>(
                    handler => this.fileWatcher.FileEvent += handler,
                    handler => this.fileWatcher.FileEvent -= handler)
                .Where(e => !e.Uri.LocalPath.EndsWith("tmp", StringComparison.InvariantCultureIgnoreCase) && !e.Uri.LocalPath.EndsWith('~'));

            this.projectsInWorkspace = new HashSet<Uri>();
            this.fileEvents = new CoalesceingQueue();
            this.fileEvents.Subscribe(fileEvents, observable => ProcessFileEvents(observable));
            this.editorState = new EditorState(
                new ProjectLoader(this.LogToWindow),
                diagnostics => this.PublishDiagnosticsAsync(diagnostics),
                (name, props, meas) => this.SendTelemetryAsync(name, props, meas),
                this.LogToWindow,
                this.OnInternalError,
                this.OnTemporaryProjectLoaded);
            this.waitForInit.Set();
        }

        public void WaitForShutdown()
        {
            this.disconnectEvent.WaitOne();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.editorState.Dispose();
            this.rpc.Dispose();
            this.disconnectEvent.Dispose();
            this.waitForInit?.Dispose();
        }

        // some utils for server -> client communication

        internal Task NotifyClientAsync(string method, object args) =>
            this.rpc.NotifyWithParameterObjectAsync(method, args);  // no need to wait for completion

        internal Task<T> InvokeAsync<T>(string method, object args) =>
           this.rpc.InvokeWithParameterObjectAsync<T>(method, args);

        internal Task PublishDiagnosticsAsync(PublishDiagnosticParams diagnostics) =>
            this.NotifyClientAsync(Methods.TextDocumentPublishDiagnosticsName, diagnostics);

        internal async void CheckDotNetSdkVersion()
        {
            var installedVersionsOfDotNetSdk = DotNetSdkHelper.GetInstalledVersionsOfDotNetSdks();
            if (installedVersionsOfDotNetSdk == null)
            {
                this.LogToWindow("Unable to detect .NET SDK versions", MessageType.Error);
            }
            else
            {
                if (!installedVersionsOfDotNetSdk.DotNetSdk31Installed
                    && !installedVersionsOfDotNetSdk.DotNetSdk5Installed)
                {
                    const string dotnetSdkUrl = "https://dotnet.microsoft.com/download/dotnet-core/";
                    this.LogToWindow($".NET Core SDK 3.1 or greater not found. Quantum Development Kit Extension requires .NET Core SDK version 3.1 or greater ({dotnetSdkUrl}).", MessageType.Error);
                    var downloadAction = new MessageActionItem { Title = "Download" };
                    var cancelAction = new MessageActionItem { Title = "No, thanks" };
                    var selectedAction = await this.ShowDialogInWindowAsync(
                        "Quantum Development Kit Extension requires either .NET Core SDK version 3.1 or greater. Please install the latest .NET Core SDK and restart Visual Studio.",
                        MessageType.Error,
                        new[] { downloadAction, cancelAction });
                    if (selectedAction != null
                        && selectedAction.Title == downloadAction.Title)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = dotnetSdkUrl,
                            UseShellExecute = true,
                            CreateNoWindow = true,
                        });
                    }
                }
            }
        }

        /// <summary>
        /// does not actually do anything unless the corresponding flag is defined upon compilation
        /// </summary>
        internal Task SendTelemetryAsync(
                string eventName,
                Dictionary<string, string?> properties,
                Dictionary<string, int> measurements) =>
            #if TELEMETRY
            this.NotifyClientAsync(Methods.TelemetryEventName, new Dictionary<string, object>
            {
                ["event"] = eventName,
                ["properties"] = properties,
                ["measurements"] = measurements
            });
            #else
            Task.CompletedTask;
            #endif

        /// <summary>
        /// to be called when the server encounters an internal error (i.e. a QsCompilerError is raised)
        /// </summary>
        internal void OnInternalError(Exception ex)
        {
            const string line = "\n=============================\n";
            const string logLocation = "the output window"; // TODO: Generate a proper error log in a file somewhere.
            const string message =
                "The Q# Language Server has encountered an error. Diagnostics will be reloaded upon saving the file.";

            switch (ex)
            {
                case FileContentException _:
                    this.LogToWindow($"A file query couldn't access file content: {ex.Message}", MessageType.Info);
                    break;
                case SymbolNotFoundException _:
                    this.LogToWindow($"A file query couldn't find a symbol: {ex.Message}", MessageType.Info);
                    break;
                default:
                    this.LogToWindow($"{line}{ex}{line}", MessageType.Error);
                    if (this.showInteralErrorMessage)
                    {
                        this.showInteralErrorMessage = false;
                        this.internalErrorTimer.Start();
                        this.ShowInWindow(
                            $"{message}\nDetails on the encountered error have been logged to {logLocation}.",
                            MessageType.Error);
                    }
                    break;
            }
        }

        private void OnTemporaryProjectLoaded(Uri projectUri) =>
            this.fileWatcher.ListenAsync(Path.GetDirectoryName(projectUri.LocalPath), false, null, "*.csproj").Wait();

        // jsonrpc methods for initialization and shut down

        private Task InitializeWorkspaceAsync(ImmutableDictionary<Uri, ImmutableHashSet<string>> folders)
        {
            var folderItems = folders.SelectMany(entry => entry.Value.Select(name => Path.Combine(entry.Key.LocalPath, name)));
            var initialProjects = folderItems.Select(item =>
            {
                if (!item.EndsWith(".csproj") || !Uri.TryCreate(item, UriKind.Absolute, out var uri))
                {
                    return null;
                }
                this.projectsInWorkspace.Add(uri);
                return uri;
            })
            .Where(fileEvent => fileEvent != null).ToImmutableArray();
            return this.editorState.LoadProjectsAsync(initialProjects);
        }

        [JsonRpcMethod(Methods.InitializeName)]
        public object Initialize(JToken arg)
        {
            var doneWithInit = this.waitForInit?.WaitOne(20000) ?? false;
            if (!doneWithInit)
            {
                return new InitializeError { Retry = true };
            }

            arg.SelectToken("capabilities.textDocument.codeAction")?.Replace(null); // setting this to null for now, since we are not using it and the deserialization causes issues
            var param = Utils.TryJTokenAs<InitializeParams>(arg);
            this.clientCapabilities = param.Capabilities;

            if (param.InitializationOptions is JObject options)
            {
                if (options.TryGetValue("name", out var name))
                {
                    this.clientName = name.ToString();
                }
                if (options.TryGetValue("version", out var version)
                        && !Version.TryParse(version.ToString(), out this.clientVersion))
                {
                    this.clientVersion = null;
                }
            }
            bool supportsCompletion = !this.ClientNameIs("VisualStudio") || this.ClientVersionIsAtLeast(new Version(16, 3));
            bool useTriggerCharWorkaround = this.ClientNameIs("VisualStudio") && !this.ClientVersionIsAtLeast(new Version(16, 4));

            var rootUri = param.RootUri ?? (Uri.TryCreate(param?.RootPath, UriKind.Absolute, out var uri) ? uri : null);
            this.workspaceFolder = rootUri != null && rootUri.IsAbsoluteUri && rootUri.IsFile && Directory.Exists(rootUri.LocalPath) ? rootUri.LocalPath : null;
            this.LogToWindow($"workspace folder: {this.workspaceFolder ?? "(Null)"}", MessageType.Info);
            this.fileWatcher.ListenAsync(this.workspaceFolder, true, dict => this.InitializeWorkspaceAsync(dict), "*.csproj", "*.dll", "*.qs").Wait(); // not strictly necessary to wait but less confusing

            var capabilities = new ServerCapabilities
            {
                TextDocumentSync = new TextDocumentSyncOptions(),
                CompletionProvider = supportsCompletion ? new CompletionOptions() : null,
                SignatureHelpProvider = new SignatureHelpOptions(),
                ExecuteCommandProvider = new ExecuteCommandOptions(),
            };
            capabilities.TextDocumentSync.Change = TextDocumentSyncKind.Incremental;
            capabilities.TextDocumentSync.OpenClose = true;
            capabilities.TextDocumentSync.Save = new SaveOptions { IncludeText = true };
            capabilities.CodeActionProvider = this.clientCapabilities?.Workspace?.ApplyEdit ?? true;
            capabilities.DefinitionProvider = true;
            capabilities.ReferencesProvider = true;
            capabilities.DocumentSymbolProvider = true;
            capabilities.WorkspaceSymbolProvider = false;
            capabilities.RenameProvider = true;
            capabilities.HoverProvider = true;
            capabilities.DocumentHighlightProvider = true;
            capabilities.SignatureHelpProvider.TriggerCharacters = new[] { "(", "," };
            capabilities.ExecuteCommandProvider.Commands = new[] { CommandIds.ApplyEdit }; // do not declare internal capabilities
            if (capabilities.CompletionProvider != null)
            {
                capabilities.CompletionProvider.ResolveProvider = true;
                capabilities.CompletionProvider.TriggerCharacters =
                    useTriggerCharWorkaround ? new[] { " ", ".", "(" } : new[] { ".", "(" };
            }

            this.waitForInit = null;
            return new InitializeResult { Capabilities = capabilities };
        }

        [JsonRpcMethod(Methods.InitializedName)]
        public void OnInitialized(JToken token)
        {
            // nothing to do here
        }

        [JsonRpcMethod(Methods.ShutdownName)]
        public object? Shutdown() // shut down and exit is fine even if the server was never initialized
        {
            this.ReadyForExit = true; // there's nothing else to do here
            return null;
        }

        [JsonRpcMethod(Methods.ExitName)]
        public void Exit() // shut down and exit is fine even if the server was never initialized
        {
            this.Dispose();
            this.disconnectEvent.Set();
        }

        // jsonrpc methods called by the language server protocol

        [JsonRpcMethod(Methods.TextDocumentDidOpenName)]
        public Task OnTextDocumentDidOpenAsync(JToken arg)
        {
            if (this.waitForInit != null)
            {
                return Task.CompletedTask;
            }
            var param = Utils.TryJTokenAs<DidOpenTextDocumentParams>(arg);
            return this.editorState.OpenFileAsync(
                param.TextDocument,
                this.ShowInWindow,
                this.workspaceFolder != null ? this.LogToWindow : (Action<string, MessageType>?)null);
        }

        [JsonRpcMethod(Methods.TextDocumentDidCloseName)]
        public Task OnTextDocumentDidCloseAsync(JToken arg)
        {
            if (this.waitForInit != null)
            {
                return Task.CompletedTask;
            }
            var param = Utils.TryJTokenAs<DidCloseTextDocumentParams>(arg);
            return this.editorState.CloseFileAsync(param.TextDocument, this.LogToWindow);
        }

        [JsonRpcMethod(Methods.TextDocumentDidSaveName)]
        public Task OnTextDocumentDidSaveAsync(JToken arg)
        {
            if (this.waitForInit != null)
            {
                return Task.CompletedTask;
            }
            var param = Utils.TryJTokenAs<DidSaveTextDocumentParams>(arg);
            return this.editorState.SaveFileAsync(param.TextDocument, param.Text);
        }

        [JsonRpcMethod(Methods.TextDocumentDidChangeName)]
        public Task OnTextDocumentChangedAsync(JToken arg)
        {
            if (this.waitForInit != null)
            {
                return Task.CompletedTask;
            }
            var param = Utils.TryJTokenAs<DidChangeTextDocumentParams>(arg);
            return this.editorState.DidChangeAsync(param);
        }

        [JsonRpcMethod(Methods.TextDocumentRenameName)]
        public object? OnTextDocumentRename(JToken arg)
        {
            if (this.waitForInit != null)
            {
                return ProtocolError.AwaitingInitialization;
            }
            var param = Utils.TryJTokenAs<RenameParams>(arg);
            var versionedChanges = this.clientCapabilities?.Workspace?.WorkspaceEdit?.DocumentChanges ?? false;
            try
            {
                return QsCompilerError.RaiseOnFailure(
                    () => this.editorState.Rename(param, versionedChanges),
                    "Rename threw an exception");
            }
            catch
            {
                return null;
            }
        }

        [JsonRpcMethod(Methods.TextDocumentDefinitionName)]
        public object OnTextDocumentDefinition(JToken arg)
        {
            if (this.waitForInit != null)
            {
                return ProtocolError.AwaitingInitialization;
            }
            var param = Utils.TryJTokenAs<TextDocumentPositionParams>(arg);
            var defaultLocation = new Location
            {
                Uri = param?.TextDocument?.Uri,
                Range = param?.Position != null
                    ? new VisualStudio.LanguageServer.Protocol.Range { Start = param.Position, End = param.Position }
                    : null
            };
            try
            {
                return QsCompilerError.RaiseOnFailure(
                    () => this.editorState.DefinitionLocation(param) ?? defaultLocation,
                    "GoToDefinition threw an exception");
            }
            catch
            {
                return defaultLocation;
            }
        }

        [JsonRpcMethod(Methods.TextDocumentDocumentHighlightName)]
        public object OnHighlightRequest(JToken arg)
        {
            if (this.waitForInit != null)
            {
                return ProtocolError.AwaitingInitialization;
            }
            var param = Utils.TryJTokenAs<TextDocumentPositionParams>(arg);
            try
            {
                return QsCompilerError.RaiseOnFailure(
                    () => this.editorState.DocumentHighlights(param) ?? Array.Empty<DocumentHighlight>(),
                    "DocumentHighlight threw an exception");
            }
            catch
            {
                return Array.Empty<DocumentHighlight>();
            }
        }

        [JsonRpcMethod(Methods.TextDocumentReferencesName)]
        public object OnTextDocumentReferences(JToken arg)
        {
            if (this.waitForInit != null)
            {
                return ProtocolError.AwaitingInitialization;
            }
            var param = Utils.TryJTokenAs<ReferenceParams>(arg);
            try
            {
                return QsCompilerError.RaiseOnFailure(
                    () => this.editorState.SymbolReferences(param) ?? Array.Empty<Location>(),
                    "FindReferences threw an exception");
            }
            catch
            {
                return Array.Empty<Location>();
            }
        }

        [JsonRpcMethod(Methods.TextDocumentHoverName)]
        public object? OnHoverRequest(JToken arg)
        {
            if (this.waitForInit != null)
            {
                return ProtocolError.AwaitingInitialization;
            }
            var param = Utils.TryJTokenAs<TextDocumentPositionParams>(arg);
            var supportedFormats = this.clientCapabilities?.TextDocument?.Hover?.ContentFormat;
            var format = this.ChooseFormat(supportedFormats);
            try
            {
                return QsCompilerError.RaiseOnFailure(
                    () => this.editorState.HoverInformation(param, format),
                    "HoverInformation threw an exception");
            }
            catch
            {
                return null;
            }
        }

        [JsonRpcMethod(Methods.TextDocumentSignatureHelpName)]
        public Task<object?> OnSignatureHelp(JToken arg)
        {
            if (this.waitForInit != null)
            {
                return Task.Run<object?>(() => ProtocolError.AwaitingInitialization);
            }
            var param = Utils.TryJTokenAs<TextDocumentPositionParams>(arg);
            var supportedFormats = this.clientCapabilities?.TextDocument?.SignatureHelp?.SignatureInformation?.DocumentationFormat;
            var format = this.ChooseFormat(supportedFormats);
            var task = new Task<object?>(() =>
            {
                // We need to give the file manager some time to actually process the change first,
                // otherwise we will return null.
                Thread.Sleep(100);
                try
                {
                    return QsCompilerError.RaiseOnFailure(
                        () => this.editorState.SignatureHelp(param, format),
                        "SignatureHelp threw an exception");
                }
                catch
                {
                    return null;
                }
            });
            task.Start(TaskScheduler.Default);
            return task;
        }

        [JsonRpcMethod(Methods.TextDocumentDocumentSymbolName)]
        public object OnTextDocumentSymbol(JToken arg) // list all symbols found in a given text document
        {
            if (this.waitForInit != null)
            {
                return ProtocolError.AwaitingInitialization;
            }
            var param = Utils.TryJTokenAs<DocumentSymbolParams>(arg);
            try
            {
                return QsCompilerError.RaiseOnFailure(
                    () => this.editorState.DocumentSymbols(param) ?? Array.Empty<SymbolInformation>(),
                    "DocumentSymbols threw an exception");
            }
            catch
            {
                return Array.Empty<SymbolInformation>();
            }
        }

        [JsonRpcMethod(Methods.TextDocumentCompletionName)]
        public Task<object?> OnTextDocumentCompletion(JToken arg)
        {
            if (this.waitForInit != null)
            {
                return Task.Run<object?>(() => ProtocolError.AwaitingInitialization);
            }
            var param = Utils.TryJTokenAs<TextDocumentPositionParams>(arg);
            var task = new Task<object?>(() =>
            {
                // Wait for the file manager to finish processing any changes
                // that happened right before this completion request.
                Thread.Sleep(50);
                try
                {
                    return QsCompilerError.RaiseOnFailure(
                        () => this.editorState.Completions(param),
                        "Completions threw an exception");
                }
                catch
                {
                    return null;
                }
            });
            task.Start(TaskScheduler.Default);
            return task;
        }

        [JsonRpcMethod(Methods.TextDocumentCompletionResolveName)]
        public object? OnTextDocumentCompletionResolve(JToken arg)
        {
            if (this.waitForInit != null)
            {
                return ProtocolError.AwaitingInitialization;
            }
            var param = Utils.TryJTokenAs<CompletionItem>(arg);
            var supportedFormats = this.clientCapabilities?.TextDocument?.SignatureHelp?.SignatureInformation?.DocumentationFormat;
            var format = this.ChooseFormat(supportedFormats);
            try
            {
                var data = Utils.TryJTokenAs<CompletionItemData>(JToken.FromObject(param?.Data));
                return QsCompilerError.RaiseOnFailure(
                    () => this.editorState.ResolveCompletion(param, data, format),
                    "ResolveCompletion threw an exception");
            }
            catch
            {
                return null;
            }
        }

        [JsonRpcMethod(Methods.TextDocumentCodeActionName)]
        public object OnCodeAction(JToken arg)
        {
            Command BuildCommand(string title, WorkspaceEdit edit)
            {
                var commandArgs = new ApplyWorkspaceEditParams { Label = $"code action \"{title}\"", Edit = edit };
                return new Command { Title = title, CommandIdentifier = CommandIds.ApplyEdit, Arguments = new object[] { commandArgs } };
            }

            if (this.waitForInit != null)
            {
                return ProtocolError.AwaitingInitialization;
            }
            var param = Utils.TryJTokenAs<Workarounds.CodeActionParams>(arg).ToCodeActionParams();
            try
            {
                return
                    QsCompilerError.RaiseOnFailure(
                        () => this.editorState.CodeActions(param)
                            ?.SelectMany(vs => vs.Select(v => BuildCommand(vs.Key, v)))
                            ?? Enumerable.Empty<Command>(),
                        "CodeAction threw an exception")
                    .ToArray();
            }
            catch
            {
                return Array.Empty<Command>();
            }
        }

        [JsonRpcMethod(Methods.WorkspaceExecuteCommandName)]
        public object? OnExecuteCommand(JToken arg)
        {
            ExecuteCommandParams param = Utils.TryJTokenAs<ExecuteCommandParams>(arg);
            object? CastAndExecute<T>(Func<T, object?> command) where T : class =>
                QsCompilerError.RaiseOnFailure(
                    () => command(Utils.TryJTokenAs<T>((JObject)param.Arguments.Single())), // currently all supported commands take a single argument
                    "ExecuteCommand threw an exception");
            try
            {
                return
                    param.Command == CommandIds.ApplyEdit ? CastAndExecute<ApplyWorkspaceEditParams>(edit =>
                        this.rpc.InvokeWithParameterObjectAsync<ApplyWorkspaceEditResponse>(Methods.WorkspaceApplyEditName, edit)) :
                    param.Command == CommandIds.FileContentInMemory ? CastAndExecute<TextDocumentIdentifier>(this.editorState.FileContentInMemory) :
                    param.Command == CommandIds.FileDiagnostics ? CastAndExecute<TextDocumentIdentifier>(this.editorState.FileDiagnostics) :
                    null;
            }
            catch
            {
                return null;
            }
        }

        [JsonRpcMethod(Methods.WorkspaceDidChangeWatchedFilesName)]
        public void OnDidChangeWatchedFiles(JToken arg)
        {
            if (this.waitForInit != null)
            {
                return;
            }
            var param = Utils.TryJTokenAs<DidChangeWatchedFilesParams>(arg);

            FileEvent[] PreprocessEvent(FileEvent fileEvent)
            {
                var fileName = fileEvent.Uri.LocalPath;
                var events = new FileEvent[] { fileEvent };

                if (fileName.EndsWith(".csproj"))
                {
                    if (fileEvent.FileChangeType == FileChangeType.Created)
                    {
                        this.projectsInWorkspace.Add(fileEvent.Uri);
                    }
                    if (fileEvent.FileChangeType == FileChangeType.Deleted)
                    {
                        this.projectsInWorkspace.Remove(fileEvent.Uri);
                    }
                }
                if (fileName.EndsWith(".qs") && fileEvent.FileChangeType != FileChangeType.Changed)
                {
                    bool FileIsWithinProjectDir(Uri projFile)
                    {
                        var projDir = Uri.TryCreate(Path.GetDirectoryName(projFile.LocalPath), UriKind.Absolute, out var uri) ? uri : null;
                        QsCompilerError.Verify(projDir != null, "could not determine project directory");
                        return fileName.StartsWith(projDir.LocalPath);
                    }
                    var projEvents = this.projectsInWorkspace.Where(FileIsWithinProjectDir)
                        .Select(projFile => new FileEvent { Uri = projFile, FileChangeType = FileChangeType.Changed });
                    events = projEvents.Concat(events).ToArray();
                }
                return events;
            }

            // to avoid unnecessary work, we need to coalesce before *and* after inserting additional change event for project files!
            var changes = CoalesceingQueue.Coalesce(param.Changes);
            changes = CoalesceingQueue.Coalesce(changes.SelectMany(PreprocessEvent));

            bool IsDll(FileEvent e) => e.Uri.LocalPath.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase);
            bool IsProjectFile(FileEvent e) => e.Uri.LocalPath.EndsWith(".csproj", StringComparison.InvariantCultureIgnoreCase);
            bool IsSourceFile(FileEvent e) => e.Uri.LocalPath.EndsWith(".qs", StringComparison.InvariantCultureIgnoreCase);

            foreach (var fileEvent in changes.Where(IsSourceFile))
            {
                // Unfortunately we have a rather annoying difference between VS and VS Code -
                // for VS, deleting a file from disk will close it in the editor whereas for VS Code it won't.
                // The problem is now that for VS, we do not get a close file notification in that case...
                // We hence inject close notifications for VS if a file has been deleted on disk.
                // While this will hopefully cover the most common cases of edits in- and outside the editor,
                // it is not currently possible to get the correct behavior for all cases!
                if (fileEvent.FileChangeType == FileChangeType.Deleted && this.ClientNameIs("VisualStudio"))
                {
                    this.LogToWindow($"The file '{fileEvent.Uri.LocalPath}' has been deleted on disk.", MessageType.Info);
                    _ = this.editorState.CloseFileAsync(new TextDocumentIdentifier { Uri = fileEvent.Uri });
                }
                else
                {
                    _ = this.editorState.SourceFileDidChangeOnDiskAsync(fileEvent.Uri);
                }
            }

            foreach (var fileEvent in changes.Where(IsProjectFile))
            {
                _ = this.editorState.ProjectDidChangeOnDiskAsync(fileEvent.Uri);
            }

            foreach (var fileEvent in changes.Where(IsDll))
            {
                _ = this.editorState.AssemblyDidChangeOnDiskAsync(fileEvent.Uri);
            }
        }
    }
}
