// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Execution;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.VisualStudio.LanguageServer.Protocol;


namespace Microsoft.Quantum.QsLanguageServer
{
    /// <summary>
    /// NOTE: This class intentionally does not give access to any FileContentManager that it manages,
    /// since it is responsible for coording access to (any routine of) the FileContentManager.
    /// </summary>
    internal class EditorState : IDisposable
    {
        private readonly ProjectManager Projects;
        private readonly ProjectLoader ProjectLoader;
        public void Dispose() => this.Projects.Dispose();

        private readonly Action<PublishDiagnosticParams> Publish;
        private readonly Action<string, Dictionary<string, string>, Dictionary<string, int>> SendTelemetry;

        /// <summary>
        /// needed to determine if the reality of a source file that has changed on disk is indeed given by the content on disk, 
        /// or whether its current state as it is in the editor needs to be preserved
        /// </summary>
        private readonly ConcurrentDictionary<Uri, FileContentManager> OpenFiles;
        private FileContentManager GetOpenFile(Uri key) => this.OpenFiles.TryGetValue(key, out var file) ? file : null;

        /// <summary>
        /// any edits in the editor to the listed files (keys) are ignored, while changes on disk are still being processed
        /// </summary>
        private readonly ConcurrentDictionary<Uri, byte> IgnoreEditorUpdatesForFiles;
        internal void IgnoreEditorUpdatesFor(Uri uri) => this.IgnoreEditorUpdatesForFiles.TryAdd(uri, new byte());

        private static bool ValidFileUri(Uri file) => file != null && file.IsFile && file.IsAbsoluteUri;
        private bool IgnoreFile(Uri file) => file == null || this.IgnoreEditorUpdatesForFiles.ContainsKey(file) || file.LocalPath.ToLowerInvariant().Contains("vctmp");

        /// <summary>
        /// Calls the given publishDiagnostics Action with the changed diagnostics whenever they have changed, 
        /// calls the given onException Action whenever the compiler encounters an internal error, and
        /// does nothing if the a given action is null.
        /// </summary>
        internal EditorState(ProjectLoader projectLoader, 
            Action<PublishDiagnosticParams> publishDiagnostics, Action<string, Dictionary<string, string>, Dictionary<string, int>> sendTelemetry,
            Action<string, MessageType> log, Action<Exception> onException)
        {
            this.IgnoreEditorUpdatesForFiles = new ConcurrentDictionary<Uri, byte>();
            this.SendTelemetry = sendTelemetry ?? ((_, __, ___) => { });
            this.Publish = param =>
            {
                var onProjFile = param.Uri.AbsolutePath.EndsWith(".csproj", StringComparison.InvariantCultureIgnoreCase);
                if (!param.Diagnostics.Any() || this.OpenFiles.ContainsKey(param.Uri) || onProjFile)
                {
                    // Some editors (e.g. Visual Studio) will actually ignore diagnostics for .csproj files.
                    // Since all errors on project loading are associated with the corresponding project file for publishing, 
                    // we need to replace the project file ending before publishing. This issue is naturally resolved once we have our own project files...
                    var parentDir = Path.GetDirectoryName(param.Uri.AbsolutePath);
                    var projFileWithoutExtension = Path.GetFileNameWithoutExtension(param.Uri.AbsolutePath);
                    if (onProjFile && Uri.TryCreate(Path.Combine(parentDir, $"{projFileWithoutExtension}.qsproj"), UriKind.Absolute, out var parentFolder))
                    { param.Uri = parentFolder; }
                    publishDiagnostics?.Invoke(param);
                }
            };

            this.ProjectLoader = projectLoader ?? throw new ArgumentNullException(nameof(projectLoader));
            this.Projects = new ProjectManager(onException, log, this.Publish);
            this.OpenFiles = new ConcurrentDictionary<Uri, FileContentManager>();
        }


        /// <summary>
        /// Extracts the EvaluatedInclude for all items of the given type in the given project instance, 
        /// and returns the combined path of the project directory and the evaluated include. 
        /// </summary>
        private static IEnumerable<string> GetItemsByType(ProjectInstance project, string itemType) =>
            project?.Items
                ?.Where(item => item.ItemType == itemType && item.EvaluatedInclude != null)
                ?.Select(item => Path.Combine(project.Directory, item.EvaluatedInclude));

        /// <summary>
        /// If the given uri corresponds to a C# project file, 
        /// determines if the project is consistent with a recognized Q# project using the ProjectLoader.
        /// Returns the project information containing the outputPath of the project 
        /// along with the Q# source files as well as all project and dll references as out parameter if it is. 
        /// Returns null if it isn't, or if the project file itself has been listed as to be ignored. 
        /// Calls SendTelemetry with suitable data if the project is a recognized Q# project. 
        /// </summary>
        internal bool QsProjectLoader(Uri projectFile, out ProjectInformation info)
        {
            info = null;
            if (projectFile == null || !ValidFileUri(projectFile) || IgnoreFile(projectFile)) return false;
            var projectInstance = this.ProjectLoader.TryGetQsProjectInstance(projectFile.LocalPath, out var telemetryProps);
            if (projectInstance == null) return false;

            var outputDir = projectInstance.GetPropertyValue("OutputPath");
            var targetFile = projectInstance.GetPropertyValue("TargetFileName");
            var outputPath = Path.Combine(projectInstance.Directory, outputDir, targetFile);

            var resRuntimeCapability = projectInstance.GetPropertyValue("ResolvedRuntimeCapabilities");
            var runtimeCapabilities = Enum.TryParse(resRuntimeCapability, out AssemblyConstants.RuntimeCapabilities capability) 
                ? capability 
                : AssemblyConstants.RuntimeCapabilities.Unknown;

            var sourceFiles = GetItemsByType(projectInstance, "QsharpCompile");
            var csharpFiles = GetItemsByType(projectInstance, "Compile").Where(file => !file.EndsWith(".g.cs"));
            var projectReferences = GetItemsByType(projectInstance, "ProjectReference");
            var references = GetItemsByType(projectInstance, "Reference");

            var version = projectInstance.GetPropertyValue("QsharpLangVersion");
            var isExecutable = "QsharpExe".Equals(projectInstance.GetPropertyValue("ResolvedQsharpOutputType"), StringComparison.InvariantCultureIgnoreCase);
            var loadTestNames = "true".Equals(projectInstance.GetPropertyValue("ExposeReferencesViaTestNames"), StringComparison.InvariantCultureIgnoreCase);
            var defaultSimulator = projectInstance.GetPropertyValue("DefaultSimulator")?.Trim();

            var telemetryMeas = new Dictionary<string, int>();
            telemetryMeas["sources"] = sourceFiles.Count();
            telemetryMeas["csharpfiles"] = csharpFiles.Count();
            telemetryProps["defaultSimulator"] = defaultSimulator;
            this.SendTelemetry("project-load", telemetryProps, telemetryMeas); // does not send anything unless the corresponding flag is defined upon compilation
            info = new ProjectInformation(version, outputPath, runtimeCapabilities, isExecutable, loadTestNames, sourceFiles, projectReferences, references);
            return true;
        }

        /// <summary>
        /// For each given uri, loads the corresponding project if the uri contains the project file for a Q# project, 
        /// and publishes suitable diagnostics for it.
        /// Throws an ArgumentNullException if the given sequence of uris, or if any of the contained uris is null.
        /// </summary>
        public Task LoadProjectsAsync(IEnumerable<Uri> projects) =>
            this.Projects.LoadProjectsAsync(projects, this.QsProjectLoader, GetOpenFile);

        /// <summary>
        /// If the given uri corresponds to the project file for a Q# project, 
        /// updates that project in the list of tracked projects or adds it if needed, and publishes suitable diagnostics for it.
        /// Throws an ArgumentNullException if the given uri is null.
        /// </summary>
        public Task ProjectDidChangeOnDiskAsync(Uri project) =>
            this.Projects.ProjectChangedOnDiskAsync(project, this.QsProjectLoader, GetOpenFile);

        /// <summary>
        /// Updates all tracked Q# projects that reference the assembly with the given uri
        /// either directly or indirectly via a reference to another Q# project, and publishes suitable diagnostics.
        /// Throws an ArgumentNullException if the given uri is null.
        /// </summary>
        public Task AssemblyDidChangeOnDiskAsync(Uri dllPath) =>
            this.Projects.AssemblyChangedOnDiskAsync(dllPath);

        /// <summary>
        /// To be used whenever a .qs file is changed on disk.
        /// Reloads the file from disk and updates the project(s) which list it as souce file 
        /// if the file is not listed as currently being open in the editor, publishing suitable diagnostics. 
        /// If the file is listed as being open in the editor, updates all load diagnostics for the file, 
        /// but does not update the file content, since the editor manages that one. 
        /// Throws an ArgumentNullException if the given uri is null.
        /// </summary>
        public Task SourceFileDidChangeOnDiskAsync(Uri sourceFile) =>
            this.Projects.SourceFileChangedOnDiskAsync(sourceFile, GetOpenFile);


        // routines related to tracking the editor state

        /// <summary>
        /// To be called whenever a file is opened in the editor.
        /// Does nothing if the given file is listed as to be ignored.
        /// Otherwise publishes suitable diagnostics for it. 
        /// Invokes the given Action showError with a suitable message if the given file cannot be loaded.  
        /// Invokes the given Action logError with a suitable message if the given file cannot be associated with a compilation unit,
        /// or if the given file is already listed as being open in the editor. 
        /// Throws an ArgumentException if the uri of the given text document identifier is null or not an absolute file uri. 
        /// Throws an ArgumentNullException if the given content is null.
        /// </summary>
        internal Task OpenFileAsync(TextDocumentItem textDocument, 
            Action<string, MessageType> showError = null, Action<string, MessageType> logError = null)
        {
            if (!ValidFileUri(textDocument?.Uri)) throw new ArgumentException("invalid text document identifier");
            if (textDocument.Text == null) throw new ArgumentNullException(nameof(textDocument.Text));
            _ = this.Projects.ManagerTaskAsync(textDocument.Uri, (manager, associatedWithProject) =>
            {
                if (IgnoreFile(textDocument.Uri)) return;
                var newManager = CompilationUnitManager.InitializeFileManager(textDocument.Uri, textDocument.Text, this.Publish, ex =>
                {
                    showError?.Invoke($"Failed to load file '{textDocument.Uri.LocalPath}'", MessageType.Error);
                    manager.LogException(ex);
                });

                // Currently it is not possible to handle both the behavior of VS and VS Code for changes on disk in a manner that will never fail. 
                // To mitigate the impact of failures we choose to just log them as info. 
                var file = this.OpenFiles.GetOrAdd(textDocument.Uri, newManager);
                if (file != newManager) // this may be the case (depending on the editor) e.g. when opening a version control diff ...
                {
                    showError?.Invoke($"Version control and opening multiple versions of the same file in the editor are currently not supported. \n" +
                        $"Intellisense has been disable for the file '{textDocument.Uri.LocalPath}'. An editor restart is required to enable intellisense again.", MessageType.Error);
                    #if DEBUG
                    if (showError == null) logError?.Invoke("Attempting to open a file that is already open in the editor.", MessageType.Error);
                    #endif

                    this.IgnoreEditorUpdatesFor(textDocument.Uri); 
                    this.OpenFiles.TryRemove(textDocument.Uri, out FileContentManager _);
                    if (!associatedWithProject) _ = manager.TryRemoveSourceFileAsync(textDocument.Uri);
                    this.Publish(new PublishDiagnosticParams { Uri = textDocument.Uri, Diagnostics = Array.Empty<Diagnostic>() });
                    return;
                }

                if (!associatedWithProject) logError?.Invoke(
                    $"The file {textDocument.Uri.LocalPath} is not associated with a compilation unit. Only syntactic diagnostics are generated."
                    , MessageType.Info);
                _ = manager.AddOrUpdateSourceFileAsync(file);
            });
            // reloading from disk in case we encountered a file already open error above
            return this.Projects.SourceFileChangedOnDiskAsync(textDocument.Uri, GetOpenFile); // NOTE: relies on that the manager task is indeed executed first!
        }

        /// <summary>
        /// To be called whenever a file is changed within the editor (i.e. changes are not necessarily reflected on disk).
        /// Does nothing if the given file is listed as to be ignored.
        /// Throws an ArgumentException if the uri of the text document identifier in the given parameter is null or not an absolute file uri. 
        /// Throws an ArgumentNullException if the given content changes are null. 
        /// </summary>
        internal Task DidChangeAsync(DidChangeTextDocumentParams param)
        {
            if (!ValidFileUri(param?.TextDocument?.Uri)) throw new ArgumentException("invalid text document identifier");
            if (param.ContentChanges == null) throw new ArgumentNullException(nameof(param.ContentChanges));
            return this.Projects.ManagerTaskAsync(param.TextDocument.Uri, (manager, __) =>
            {
                if (IgnoreFile(param.TextDocument.Uri)) return;

                // Currently it is not possible to handle both the behavior of VS and VS Code for changes on disk in a manner that will never fail. 
                // To mitigate the impact of failures we choose to ignore them silently. 
                if (!this.OpenFiles.ContainsKey(param.TextDocument.Uri)) return;
                _ = manager.SourceFileDidChangeAsync(param); // independent on whether the file does or doesn't belong to a project
            });
        }

        /// <summary>
        /// Used to reload the file content when a file is saved.
        /// Does nothing if the given file is listed as to be ignored.
        /// Expects to get the entire content of the file at the time of saving as argument.
        /// Throws an ArgumentException if the uri of the given text document identifier is null or not an absolute file uri. 
        /// Throws an ArgumentNullException if the given content is null.
        /// </summary>
        internal Task SaveFileAsync(TextDocumentIdentifier textDocument, string fileContent)
        {
            if (!ValidFileUri(textDocument?.Uri)) throw new ArgumentException("invalid text document identifier");
            if (fileContent == null) throw new ArgumentNullException(nameof(fileContent));
            return this.Projects.ManagerTaskAsync(textDocument.Uri, (manager, __) =>
            {
                if (IgnoreFile(textDocument.Uri)) return;

                // Currently it is not possible to handle both the behavior of VS and VS Code for changes on disk in a manner that will never fail. 
                // To mitigate the impact of failures we choose to ignore them silently and do our best to recover. 
                if (!this.OpenFiles.TryGetValue(textDocument.Uri, out var file))
                {
                    file = CompilationUnitManager.InitializeFileManager(textDocument.Uri, fileContent, this.Publish, manager.LogException);
                    this.OpenFiles.TryAdd(textDocument.Uri, file);
                    _ = manager.AddOrUpdateSourceFileAsync(file);
                }
                else _ = manager.AddOrUpdateSourceFileAsync(file, fileContent); // let's reload the file content on saving
            });
        }

        /// <summary>
        /// To be called whenever a file is closed in the editor.
        /// Does nothing if the given file is listed as to be ignored.
        /// Otherwise the file content is reloaded from disk (in case changes in the editor are discarded without closing), and the diagnostics are updated.
        /// Invokes the given Action onError with a suitable message if the given file is not listed as being open in the editor. 
        /// Throws an ArgumentException if the uri of the given text document identifier is null or not an absolute file uri. 
        /// </summary>
        internal Task CloseFileAsync(TextDocumentIdentifier textDocument, Action<string, MessageType> onError = null)
        {
            if (!ValidFileUri(textDocument?.Uri)) throw new ArgumentException("invalid text document identifier");
            _ = this.Projects.ManagerTaskAsync(textDocument.Uri, (manager, associatedWithProject) => // needs to be *first* (due to the modification of OpenFiles)
            {
                if (IgnoreFile(textDocument.Uri)) return;

                // Currently it is not possible to handle both the behavior of VS and VS Code for changes on disk in a manner that will never fail. 
                // To mitigate the impact of failures we choose to ignore them silently.
                var removed = this.OpenFiles.TryRemove(textDocument.Uri, out FileContentManager __);
                #if DEBUG
                if (!removed) onError?.Invoke($"Attempting to close file '{textDocument.Uri.LocalPath}' that is not currently listed as open in the editor.", MessageType.Error);
                #endif
                if (!associatedWithProject) _ = manager.TryRemoveSourceFileAsync(textDocument.Uri);
                this.Publish(new PublishDiagnosticParams { Uri = textDocument.Uri, Diagnostics = Array.Empty<Diagnostic>() });
            });
            // When edits are made in a file, but those are discarded by closing the file and hitting "no, don't save",
            // no notification is sent for the now discarded changes;
            // hence we reload the file content from disk upon closing. 
            return this.Projects.SourceFileChangedOnDiskAsync(textDocument.Uri, GetOpenFile); // NOTE: relies on that the manager task is indeed executed first!
        }


        // routines related to providing information for editor commands

        /// <summary>
        /// Returns the workspace edit that describes the changes to be done if the symbol at the given position - if any - is renamed to the given name. 
        /// Returns null if no symbol exists at the specified position, 
        /// or if the specified uri is not a valid file uri. 
        /// or if some parameters are unspecified (null) or inconsistent with the tracked editor state. 
        /// </summary>
        public WorkspaceEdit Rename(RenameParams param, bool versionedChanges = false) =>
            ValidFileUri(param?.TextDocument?.Uri) && !IgnoreFile(param.TextDocument.Uri) ? this.Projects.Rename(param, versionedChanges) : null;

        /// <summary>
        /// Returns the source file and position where the item at the given position is declared at,
        /// if such a declaration exists, and returns the given position and file otherwise.
        /// Returns null if the given file is listed as to be ignored or if the information cannot be determined at this point.
        /// </summary>
        public Location DefinitionLocation(TextDocumentPositionParams param) =>
            ValidFileUri(param?.TextDocument?.Uri) && !IgnoreFile(param.TextDocument.Uri) ? this.Projects.DefinitionLocation(param) : null;

        /// <summary>
        /// Returns the signature help information for a call expression if there is such an expression at the specified position.
        /// Returns null if the given file is listed as to be ignored,
        /// or if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the currently processed file content
        /// or if no call expression exists at the specified position at this time
        /// or if no signature help information can be provided for the call expression at the specified position.
        /// </summary>
        public SignatureHelp SignatureHelp(TextDocumentPositionParams param, MarkupKind format = MarkupKind.PlainText) =>
            ValidFileUri(param?.TextDocument?.Uri) && !IgnoreFile(param.TextDocument.Uri) ? this.Projects.SignatureHelp(param, format) : null;

        /// <summary>
        /// Returns information about the item at the specified position as Hover information.
        /// Returns null if the given file is listed as to be ignored,
        /// or if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the currently processed file content,
        /// or if no token exists at the specified position at this time.
        /// </summary>
        public Hover HoverInformation(TextDocumentPositionParams param, MarkupKind format = MarkupKind.PlainText) =>
            ValidFileUri(param?.TextDocument?.Uri) && !IgnoreFile(param.TextDocument.Uri) ? this.Projects.HoverInformation(param, format) : null;

        /// <summary>
        /// Returns an array with all usages of the identifier at the given position (if any) as DocumentHighlights.
        /// Returns null if the given file is listed as to be ignored,
        /// or if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the currently processed file content,
        /// or if no identifier exists at the specified position at this time.
        /// </summary>
        public DocumentHighlight[] DocumentHighlights(TextDocumentPositionParams param) =>
            ValidFileUri(param?.TextDocument?.Uri) && !IgnoreFile(param.TextDocument.Uri) ? this.Projects.DocumentHighlights(param) : null;

        /// <summary>
        /// Returns an array with all locations where the symbol at the given position - if any - is referenced. 
        /// Returns null if the given file is listed as to be ignored,
        /// or if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the currently processed file content,
        /// or if no symbol exists at the specified position at this time.
        /// </summary>
        public Location[] SymbolReferences(ReferenceParams param) =>
            ValidFileUri(param?.TextDocument?.Uri) && !IgnoreFile(param.TextDocument.Uri) ? this.Projects.SymbolReferences(param) : null;

        /// <summary>
        /// Returns the SymbolInformation for each namespace declaration, type declaration, and function or operation declaration.
        /// Returns null if the given file is listed as to be ignored, or if the given parameter or its uri is null.
        /// </summary>
        public SymbolInformation[] DocumentSymbols(DocumentSymbolParams param) =>
            ValidFileUri(param?.TextDocument?.Uri) && !IgnoreFile(param.TextDocument.Uri) ? this.Projects.DocumentSymbols(param) : null;

        /// <summary>
        /// Returns a look-up of workspace edits suggested by the compiler for the given location and context.
        /// The key of the look-up is a suitable title for the corresponding edits that can be presented to the user. 
        /// Returns null if the given file is listed as to be ignored, or if the given parameter or its uri is null.
        /// </summary>
        public ILookup<string, WorkspaceEdit> CodeActions(CodeActionParams param) =>
            ValidFileUri(param?.TextDocument?.Uri) && !IgnoreFile(param.TextDocument.Uri) ? this.Projects.CodeActions(param) : null;

        /// <summary>
        /// Returns a list of suggested completion items for the given location.
        /// <para/>
        /// Returns null if the given file is listed as to be ignored, or if the given parameter or its uri or position is null.
        /// </summary>
        public CompletionList Completions(TextDocumentPositionParams param) =>
            ValidFileUri(param?.TextDocument?.Uri) && !IgnoreFile(param.TextDocument.Uri)
            ? this.Projects.Completions(param)
            : null;

        /// <summary>
        /// Resolves additional information for the given completion item. Returns the original completion item if no
        /// additional information is available, or if the completion item is no longer valid or accurate.
        /// <para/>
        /// Returns null if any parameter is null or the file given in the original completion request is invalid or
        /// ignored.
        /// </summary>
        internal CompletionItem ResolveCompletion(CompletionItem item, CompletionItemData data, MarkupKind format) =>
            item != null && ValidFileUri(data?.TextDocument?.Uri) && !IgnoreFile(data.TextDocument.Uri)
            ? this.Projects.ResolveCompletion(item, data, format)
            : null;


        // utils to query the editor state server for testing purposes 
        // -> explicitly part of this class because any access to the resources may need to be coordinated as well

        /// <summary>
        /// Waits for all currently running or queued tasks to finish before getting the file content in memory.
        /// -> Method to be used for testing/diagnostic purposes only!
        /// </summary>
        internal string[] FileContentInMemory(TextDocumentIdentifier textDocument) =>
            this.Projects.FileContentInMemory(textDocument);

        /// <summary>
        /// Waits for all currently running or queued tasks to finish before getting the diagnostics for the given file.
        /// -> Method to be used for testing purposes only!
        /// </summary>
        internal Diagnostic[] FileDiagnostics(TextDocumentIdentifier textDocument)
        {
            var allDiagnostics = this.Projects.GetDiagnostics(textDocument?.Uri);
            return allDiagnostics?.Count() == 1 ? allDiagnostics.Single().Diagnostics : null; // count is > 1 if the given uri corresponds to a project file
        }
    }
}
