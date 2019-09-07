// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.VisualStudio.LanguageServer.Protocol; 
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;


namespace Microsoft.Quantum.QsLanguageServer
{
    public class QsLanguageServer : IDisposable
    {
        // properties required for basic functionality

        private readonly JsonRpc Rpc;
        private readonly ManualResetEvent DisconnectEvent; // used to keep the server running until it is no longer needed
        private ManualResetEvent WaitForInit; // set to null after initialization
        internal bool ReadyForExit { get; private set; }

        private string WorkspaceFolder = null;
        private readonly HashSet<Uri> ProjectsInWorkspace;
        private readonly FileWatcher FileWatcher;
        private readonly CoalesceingQueue FileEvents;

        private string ClientName;
        private ClientCapabilities ClientCapabilities;
        private readonly EditorState EditorState;

        /// <summary>
        /// helper function that selects a markup format from the given array of supported formats
        /// </summary>
        private MarkupKind ChooseFormat(MarkupKind[] supportedFormats) =>
            supportedFormats?.Any() ?? false
                ? supportedFormats.Contains(MarkupKind.Markdown) ? MarkupKind.Markdown : supportedFormats.First()
                : MarkupKind.PlainText;


        // methods required for basic functionality

        public QsLanguageServer(Stream sender, Stream reader)
        {
            this.WaitForInit = new ManualResetEvent(false);
            this.Rpc = new JsonRpc(sender, reader, this)
            { SynchronizationContext = new QsSynchronizationContext() };
            this.Rpc.StartListening();
            this.DisconnectEvent = new ManualResetEvent(false);
            this.Rpc.Disconnected += (object s, JsonRpcDisconnectedEventArgs e) => { this.DisconnectEvent.Set(); }; // let's make the server exit if the stream is disconnected
            this.ReadyForExit = false;

            void ProcessFileEvents(IEnumerable<FileEvent> e) =>
                this.OnDidChangeWatchedFiles(JToken.Parse(JsonConvert.SerializeObject(
                    new DidChangeWatchedFilesParams { Changes = e.ToArray() })));
            var fileEvents = Observable.FromEvent<FileWatcher.FileEventHandler, FileEvent>(
                    handler => this.FileWatcher.FileEvent += handler,
                    handler => this.FileWatcher.FileEvent -= handler)
                .Where(e => !e.Uri.LocalPath.EndsWith("tmp", StringComparison.InvariantCultureIgnoreCase) && !e.Uri.LocalPath.EndsWith('~'));

            this.ProjectsInWorkspace = new HashSet<Uri>();
            this.FileWatcher = new FileWatcher(_ => this.LogToWindow($"FileSystemWatcher encountered and error", MessageType.Error));
            this.FileEvents = new CoalesceingQueue();
            this.FileEvents.Subscribe(fileEvents, observable => ProcessFileEvents(observable));
            this.EditorState = new EditorState(new ProjectLoader(this.LogToWindow),
                diagnostics => this.PublishDiagnosticsAsync(diagnostics), (name, props, meas) => this.SendTelemetryAsync(name, props, meas),
                this.LogToWindow, this.OnInternalError);
            this.WaitForInit.Set();
        }

        public void WaitForShutdown()
        { this.DisconnectEvent.WaitOne(); }

        public void Dispose()
        {
            this.EditorState.Dispose();
            this.Rpc.Dispose();
            this.DisconnectEvent.Dispose();
            this.WaitForInit.Dispose();
        }


        // some utils for server -> client communication

        internal Task NotifyClientAsync(string method, object args) =>
            this.Rpc.NotifyWithParameterObjectAsync(method, args);  // no need to wait for completion

        internal Task PublishDiagnosticsAsync(PublishDiagnosticParams diagnostics) =>
            this.NotifyClientAsync(Methods.TextDocumentPublishDiagnosticsName, diagnostics);

        /// <summary>
        /// does not actually do anything unless the corresponding flag is defined upon compilation
        /// </summary>
        internal Task SendTelemetryAsync(string eventName,
            Dictionary<string, string> properties, Dictionary<string, int> measurements) =>
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
            var line = "\n=============================\n";
            this.LogToWindow($"{line}{ex}{line}", MessageType.Error);
            var logLocation = "the output window";  // todo: generate a proper error log in a file somewhere
            var message = "The Q# Language Server has encountered an error. Diagnostics will be reloaded upon saving the file.";
            this.ShowInWindow($"{message}\nDetails on the encountered error have been logged to {logLocation}.", MessageType.Error);
        }


        // jsonrpc methods for initialization and shut down

        private Task InitializeWorkspaceAsync(ImmutableDictionary<Uri, ImmutableHashSet<string>> folders)
        {
            var folderItems = folders.SelectMany(entry => entry.Value.Select(name => Path.Combine(entry.Key.LocalPath, name)));
            var initialProjects = folderItems.Select(item =>
            {
                if (!item.EndsWith(".csproj") || !Uri.TryCreate(item, UriKind.Absolute, out Uri uri)) return null;
                this.ProjectsInWorkspace.Add(uri);
                return uri; 
            })
            .Where(fileEvent => fileEvent != null).ToImmutableArray();
            return this.EditorState.LoadProjectsAsync(initialProjects);
        }

        [JsonRpcMethod(Methods.InitializeName)]
        public object Initialize(JToken arg)
        {
            var doneWithInit = this.WaitForInit?.WaitOne(20000) ?? false;
            if (!doneWithInit) return new InitializeError { Retry = true };
            var param = Utils.TryJTokenAs<InitializeParams>(arg);

            this.ClientName = param.InitializationOptions as string;
            this.ClientCapabilities = param.Capabilities;

            var rootUri = param.RootUri ?? (Uri.TryCreate(param?.RootPath, UriKind.Absolute, out Uri uri) ? uri : null);
            this.WorkspaceFolder = rootUri != null && rootUri.IsAbsoluteUri && rootUri.IsFile && Directory.Exists(rootUri.LocalPath) ? rootUri.LocalPath : null;
            this.LogToWindow($"workspace folder: {this.WorkspaceFolder ?? "(Null)"}", MessageType.Info);
            this.FileWatcher.ListenAsync(this.WorkspaceFolder, true, dict => this.InitializeWorkspaceAsync(dict), "*.csproj", "*.dll", "*.qs").Wait(); // not strictly necessary to wait but less confusing

            var capabilities = new ServerCapabilities
            {
                TextDocumentSync = new TextDocumentSyncOptions(),
                // Disable completion in Visual Studio until a bug where completion is not dismissed after typing
                // whitespace is fixed.
                CompletionProvider = "VisualStudio".Equals(this.ClientName, StringComparison.InvariantCultureIgnoreCase)
                    ? null
                    : new CompletionOptions(),
                SignatureHelpProvider = new SignatureHelpOptions(),
                ExecuteCommandProvider = new ExecuteCommandOptions(),
            };
            capabilities.TextDocumentSync.Change = TextDocumentSyncKind.Incremental;
            capabilities.TextDocumentSync.OpenClose = true;
            capabilities.TextDocumentSync.Save = new SaveOptions { IncludeText = true };
            capabilities.CodeActionProvider = this.ClientCapabilities.Workspace.ApplyEdit;
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
                capabilities.CompletionProvider.TriggerCharacters = new[] { "." };
            }

            this.WaitForInit = null;
            return new InitializeResult { Capabilities = capabilities };
        }

        [JsonRpcMethod(Methods.InitializedName)]
        public void OnInitialized(JToken _) { } // nothing to do here

        [JsonRpcMethod(Methods.ShutdownName)]
        public object Shutdown() // shut down and exit is fine even if the server was never initialized
        {
            this.ReadyForExit = true; // there's nothing else to do here
            return null;
        }

        [JsonRpcMethod(Methods.ExitName)]
        public void Exit() // shut down and exit is fine even if the server was never initialized
        {
            this.Dispose();
            this.DisconnectEvent.Set();
        }


        // jsonrpc methods called by the language server protocol

        [JsonRpcMethod(Methods.TextDocumentDidOpenName)]
        public Task OnTextDocumentDidOpenAsync(JToken arg)
        {
            if (this.WaitForInit != null) return Task.CompletedTask;
            var param = Utils.TryJTokenAs<DidOpenTextDocumentParams>(arg);
            return this.EditorState.OpenFileAsync(param.TextDocument, this.ShowInWindow,
                this.WorkspaceFolder != null ? this.LogToWindow : (Action<string, MessageType>)null);
        }

        [JsonRpcMethod(Methods.TextDocumentDidCloseName)]
        public Task OnTextDocumentDidCloseAsync(JToken arg)
        {
            if (this.WaitForInit != null) return Task.CompletedTask;
            var param = Utils.TryJTokenAs<DidCloseTextDocumentParams>(arg);
            return this.EditorState.CloseFileAsync(param.TextDocument, this.LogToWindow);
        }

        [JsonRpcMethod(Methods.TextDocumentDidSaveName)]
        public Task OnTextDocumentDidSaveAsync(JToken arg)
        {
            if (this.WaitForInit != null) return Task.CompletedTask;
            var param = Utils.TryJTokenAs<DidSaveTextDocumentParams>(arg);
            return this.EditorState.SaveFileAsync(param.TextDocument, param.Text);
        }

        [JsonRpcMethod(Methods.TextDocumentDidChangeName)]
        public Task OnTextDocumentChangedAsync(JToken arg)
        {
            if (this.WaitForInit != null) return Task.CompletedTask;
            var param = Utils.TryJTokenAs<DidChangeTextDocumentParams>(arg);
            return this.EditorState.DidChangeAsync(param);
        }

        [JsonRpcMethod(Methods.TextDocumentRenameName)]
        public object OnTextDocumentRename(JToken arg)
        {
            if (WaitForInit != null) return ProtocolError.AwaitingInitialization;
            var param = Utils.TryJTokenAs<RenameParams>(arg);
            var versionedChanges = this.ClientCapabilities?.Workspace?.WorkspaceEdit?.DocumentChanges ?? false;
            try
            {
                return QsCompilerError.RaiseOnFailure(() =>
                this.EditorState.Rename(param, versionedChanges),
                "Rename threw an exception");
            }
            catch { return null; }
        }

        [JsonRpcMethod(Methods.TextDocumentDefinitionName)]
        public object OnTextDocumentDefinition(JToken arg)
        {
            if (WaitForInit != null) return ProtocolError.AwaitingInitialization;
            var param = Utils.TryJTokenAs<TextDocumentPositionParams>(arg);
            var defaultLocation = new Location
            {
                Uri = param?.TextDocument?.Uri,
                Range = param?.Position == null ? null : new Range { Start = param.Position, End = param.Position }
            };
            try
            {
                return QsCompilerError.RaiseOnFailure(() =>
                this.EditorState.DefinitionLocation(param) ?? defaultLocation,
                "GoToDefinition threw an exception");
            }
            catch { return defaultLocation; }
        }

        [JsonRpcMethod(Methods.TextDocumentDocumentHighlightName)]
        public object OnHighlightRequest(JToken arg)
        {
            if (WaitForInit != null) return ProtocolError.AwaitingInitialization;
            var param = Utils.TryJTokenAs<TextDocumentPositionParams>(arg);
            try
            {
                return QsCompilerError.RaiseOnFailure(() =>
                this.EditorState.DocumentHighlights(param) ?? new DocumentHighlight[0],
                "DocumentHighlight threw an exception"); 
            }
            catch { return new DocumentHighlight[0]; }
        }

        [JsonRpcMethod(Methods.TextDocumentReferencesName)]
        public object OnTextDocumentReferences(JToken arg)
        {
            if (WaitForInit != null) return ProtocolError.AwaitingInitialization;
            var param = Utils.TryJTokenAs<ReferenceParams>(arg);
            try
            {
                return QsCompilerError.RaiseOnFailure(() =>
                this.EditorState.SymbolReferences(param) ?? new Location[0],
                "FindReferences threw an exception");
            }
            catch { return new Location[0]; }
        }

        [JsonRpcMethod(Methods.TextDocumentHoverName)]
        public object OnHoverRequest(JToken arg)
        {
            if (WaitForInit != null) return ProtocolError.AwaitingInitialization;
            var param = Utils.TryJTokenAs<TextDocumentPositionParams>(arg);
            var supportedFormats = this.ClientCapabilities?.TextDocument?.Hover?.ContentFormat;
            var format = ChooseFormat(supportedFormats); 
            try
            {
                return QsCompilerError.RaiseOnFailure(() => 
                this.EditorState.HoverInformation(param, format), 
                "HoverInformation threw an exception");
            }
            catch { return null; } 
        }

        [JsonRpcMethod(Methods.TextDocumentSignatureHelpName)]
        public object OnSignatureHelp(JToken arg)
        {
            if (WaitForInit != null) return ProtocolError.AwaitingInitialization;
            var param = Utils.TryJTokenAs<TextDocumentPositionParams>(arg);
            var supportedFormats = this.ClientCapabilities?.TextDocument?.SignatureHelp?.SignatureInformation?.DocumentationFormat;
            var format = ChooseFormat(supportedFormats);
            var task = new Task<SignatureHelp>(() =>
            {
                // We need to give the file manager some time to actually process the change first, otherwise we will return null. 
                // Unfortunatly, the VS Client seems to block and wait for a response from this request, 
                // such that the editor becomes unresponsive if we take too long. 
                Thread.Sleep(100);
                try
                {
                    return QsCompilerError.RaiseOnFailure(() => 
                    this.EditorState.SignatureHelp(param, format), 
                    "SignatureHelp threw an exception");
                }
                catch { return null; } 
            });
            task.Start(TaskScheduler.Default);
            return task; 
        }

        [JsonRpcMethod(Methods.TextDocumentDocumentSymbolName)]
        public object OnTextDocumentSymbol(JToken arg) // list all symbols found in a given text document
        {
            if (WaitForInit != null) return ProtocolError.AwaitingInitialization;
            var param = Utils.TryJTokenAs<DocumentSymbolParams>(arg);
            try
            {
                return QsCompilerError.RaiseOnFailure(() =>
                this.EditorState.DocumentSymbols(param) ?? new SymbolInformation[0], 
                "DocumentSymbols threw an exception");
            }
            catch { return new SymbolInformation[0]; } 
        }

        [JsonRpcMethod(Methods.TextDocumentCodeActionName)]
        public object OnCodeAction(JToken arg)
        {
            Command BuildCommand(string title, WorkspaceEdit edit)
            {
                var commandArgs = new ApplyWorkspaceEditParams { Label = $"code action \"{title}\"", Edit = edit };
                return new Command { Title = title, CommandIdentifier = CommandIds.ApplyEdit, Arguments = new object[] { commandArgs } };
            }

            if (this.WaitForInit != null) return ProtocolError.AwaitingInitialization;
            var param = Utils.TryJTokenAs<CodeActionParams>(arg);
            try
            {
                return QsCompilerError.RaiseOnFailure(() =>
                this.EditorState.CodeActions(param)
                    ?.SelectMany(vs => vs.Select(v => BuildCommand(vs.Key, v))) 
                    ?? Enumerable.Empty<Command>(),
                "CodeAction threw an exception")
                .ToArray();
            }
            catch { return new Command[0]; }
        }

        [JsonRpcMethod(Methods.WorkspaceExecuteCommandName)]
        public object OnExecuteCommand(JToken arg)
        {
            var param = Utils.TryJTokenAs<ExecuteCommandParams>(arg);
            object CastAndExecute<A>(Func<A, object> command) where A : class =>
                QsCompilerError.RaiseOnFailure<object>(() =>
                command(Utils.TryJTokenAs<A>(param.Arguments.Single() as JObject)), // currently all supported commands take a single argument
                "ExecuteCommand threw an exception");
            try
            {
                return
                    param.Command == CommandIds.ApplyEdit ? CastAndExecute<ApplyWorkspaceEditParams>(edit => 
                        this.Rpc.InvokeWithParameterObjectAsync<ApplyWorkspaceEditResponse>(Methods.WorkspaceApplyEditName, edit)) :
                    param.Command == CommandIds.FileContentInMemory ? CastAndExecute<TextDocumentIdentifier>(this.EditorState.FileContentInMemory) :
                    param.Command == CommandIds.FileDiagnostics ? CastAndExecute<TextDocumentIdentifier>(this.EditorState.FileDiagnostics) :
                    null;
            }
            catch { return null; }
        }

        [JsonRpcMethod(Methods.WorkspaceDidChangeWatchedFilesName)]
        public void OnDidChangeWatchedFiles(JToken arg)
        {
            if (this.WaitForInit != null) return;
            var param = Utils.TryJTokenAs<DidChangeWatchedFilesParams>(arg);

            FileEvent[] PreprocessEvent(FileEvent fileEvent)
            {
                var fileName = fileEvent.Uri.LocalPath;
                var events = new FileEvent[] { fileEvent };

                if (fileName.EndsWith(".csproj"))
                {
                    if (fileEvent.FileChangeType == FileChangeType.Created) this.ProjectsInWorkspace.Add(fileEvent.Uri);
                    if (fileEvent.FileChangeType == FileChangeType.Deleted) this.ProjectsInWorkspace.Remove(fileEvent.Uri);
                }
                if (fileName.EndsWith(".qs") && fileEvent.FileChangeType != FileChangeType.Changed)
                {
                    bool FileIsWithinProjectDir(Uri projFile)
                    {
                        var projDir = Uri.TryCreate(Path.GetDirectoryName(projFile.LocalPath), UriKind.Absolute, out Uri uri) ? uri : null;
                        QsCompilerError.Verify(projDir != null, "could not determine project directory");
                        return fileName.StartsWith(projDir.LocalPath);
                    }
                    var projEvents = this.ProjectsInWorkspace.Where(FileIsWithinProjectDir)
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
                if (fileEvent.FileChangeType == FileChangeType.Deleted &&
                    "VisualStudio".Equals(this.ClientName, StringComparison.InvariantCultureIgnoreCase))
                {
                    this.LogToWindow($"The file '{fileEvent.Uri.LocalPath}' has been deleted on disk.", MessageType.Info);
                    _ = this.EditorState.CloseFileAsync(new TextDocumentIdentifier { Uri = fileEvent.Uri });
                }
                else _ = this.EditorState.SourceFileDidChangeOnDiskAsync(fileEvent.Uri);
            }

            foreach (var fileEvent in changes.Where(IsProjectFile))
            { _ = this.EditorState.ProjectDidChangeOnDiskAsync(fileEvent.Uri); }

            foreach (var fileEvent in changes.Where(IsDll))
            { _ = this.EditorState.AssemblyDidChangeOnDiskAsync(fileEvent.Uri); }
        }

        [JsonRpcMethod(Methods.TextDocumentCompletionName)]
        public async Task<CompletionList> OnTextDocumentCompletionAsync(JToken arg)
        {
            // Wait for the file manager to finish processing any changes that happened right before this completion
            // request.
            await Task.Delay(50);
            try
            {
                return QsCompilerError.RaiseOnFailure(
                    () => EditorState.Completions(Utils.TryJTokenAs<TextDocumentPositionParams>(arg)),
                    "Completions threw an exception");
            }
            catch
            {
                return null;
            }
        }

        [JsonRpcMethod(Methods.TextDocumentCompletionResolveName)]
        public CompletionItem OnTextDocumentCompletionResolve(JToken arg)
        {
            try
            {
                var item = Utils.TryJTokenAs<CompletionItem>(arg);
                var data = Utils.TryJTokenAs<CompletionItemData>(JToken.FromObject(item?.Data));
                var format = ChooseFormat(
                    this.ClientCapabilities?.TextDocument?.SignatureHelp?.SignatureInformation?.DocumentationFormat);
                return QsCompilerError.RaiseOnFailure(
                    () => EditorState.ResolveCompletion(item, data, format),
                    "ResolveCompletion threw an exception");
            }
            catch
            {
                return null;
            }
        }
    }
}
