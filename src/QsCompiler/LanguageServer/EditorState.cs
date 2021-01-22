// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Execution;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using static Microsoft.Quantum.QsCompiler.ReservedKeywords.AssemblyConstants;

namespace Microsoft.Quantum.QsLanguageServer
{
    /// <summary>
    /// NOTE: This class intentionally does not give access to any FileContentManager that it manages,
    /// since it is responsible for coording access to (any routine of) the FileContentManager.
    /// </summary>
    internal class EditorState : IDisposable
    {
        private readonly ProjectManager projects;
        private readonly ProjectLoader projectLoader;

        public void Dispose() => this.projects.Dispose();

        private readonly Action<PublishDiagnosticParams> publish;
        private readonly Action<string, Dictionary<string, string?>, Dictionary<string, int>> sendTelemetry;
        private readonly Action<Uri>? onTemporaryProjectLoaded;

        /// <summary>
        /// needed to determine if the reality of a source file that has changed on disk is indeed given by the content on disk,
        /// or whether its current state as it is in the editor needs to be preserved
        /// </summary>
        private readonly ConcurrentDictionary<Uri, FileContentManager> openFiles =
            new ConcurrentDictionary<Uri, FileContentManager>();

        private FileContentManager? GetOpenFile(Uri key) => this.openFiles.TryGetValue(key, out var file) ? file : null;

        /// <summary>
        /// any edits in the editor to the listed files (keys) are ignored, while changes on disk are still being processed
        /// </summary>
        private readonly ConcurrentDictionary<Uri, byte> ignoreEditorUpdatesForFiles;

        internal void IgnoreEditorUpdatesFor(Uri uri) => this.ignoreEditorUpdatesForFiles.TryAdd(uri, default);

        private static bool ValidFileUri(Uri? file) => file != null && file.IsFile && file.IsAbsoluteUri;

        private bool IgnoreFile(Uri? file) => file == null || this.ignoreEditorUpdatesForFiles.ContainsKey(file) || file.LocalPath.ToLowerInvariant().Contains("vctmp");

        /// <summary>
        /// Calls the given publishDiagnostics Action with the changed diagnostics whenever they have changed,
        /// calls the given onException Action whenever the compiler encounters an internal error, and
        /// does nothing if the a given action is null.
        /// </summary>
        internal EditorState(
            ProjectLoader projectLoader,
            Action<PublishDiagnosticParams>? publishDiagnostics,
            Action<string, Dictionary<string, string?>, Dictionary<string, int>>? sendTelemetry,
            Action<string, MessageType>? log,
            Action<Exception>? onException,
            Action<Uri>? onTemporaryProjectLoaded)
        {
            this.ignoreEditorUpdatesForFiles = new ConcurrentDictionary<Uri, byte>();
            this.sendTelemetry = sendTelemetry ?? ((eventName, properties, measurements) => { });
            this.publish = param =>
            {
                var onProjFile = param.Uri.AbsolutePath.EndsWith(".csproj", StringComparison.InvariantCultureIgnoreCase);
                if (!param.Diagnostics.Any() || this.openFiles.ContainsKey(param.Uri) || onProjFile)
                {
                    // Some editors (e.g. Visual Studio) will actually ignore diagnostics for .csproj files.
                    // Since all errors on project loading are associated with the corresponding project file for publishing,
                    // we need to replace the project file ending before publishing. This issue is naturally resolved once we have our own project files...
                    var parentDir = Path.GetDirectoryName(param.Uri.AbsolutePath) ?? "";
                    var projFileWithoutExtension = Path.GetFileNameWithoutExtension(param.Uri.AbsolutePath);
                    if (onProjFile && Uri.TryCreate(Path.Combine(parentDir, $"{projFileWithoutExtension}.qsproj"), UriKind.Absolute, out var parentFolder))
                    {
                        param.Uri = parentFolder;
                    }
                    publishDiagnostics?.Invoke(param);
                }
            };

            this.projectLoader = projectLoader;
            this.projects = new ProjectManager(onException, log, this.publish);
            this.onTemporaryProjectLoaded = onTemporaryProjectLoaded;
        }

        /// <summary>
        /// Extracts the EvaluatedInclude for all items of the given type in the given project instance,
        /// and returns the combined path of the project directory and the evaluated include.
        /// </summary>
        private static IEnumerable<string> GetItemsByType(ProjectInstance project, string itemType) =>
            project.Items
                .Where(item => item.ItemType.Equals(itemType, StringComparison.OrdinalIgnoreCase) && item.EvaluatedInclude != null)
                .Select(item => Path.Combine(project.Directory, item.EvaluatedInclude));

        /// <summary>
        /// If the given uri corresponds to a C# project file,
        /// determines if the project is consistent with a recognized Q# project using the ProjectLoader.
        /// Returns the project information containing the outputPath of the project
        /// along with the Q# source files as well as all project and dll references as out parameter if it is.
        /// Returns null if it isn't, or if the project file itself has been listed as to be ignored.
        /// Calls SendTelemetry with suitable data if the project is a recognized Q# project.
        /// </summary>
        internal bool QsProjectLoader(Uri projectFile, [NotNullWhen(true)] out ProjectInformation? info)
        {
            info = null;
            if (projectFile == null || !ValidFileUri(projectFile) || this.IgnoreFile(projectFile))
            {
                return false;
            }
            var projectInstance = this.projectLoader.TryGetQsProjectInstance(projectFile.LocalPath, out var telemetryProps);
            if (projectInstance == null)
            {
                return false;
            }

            var outputDir = projectInstance.GetPropertyValue("OutputPath");
            var targetFile = projectInstance.GetPropertyValue("TargetFileName");
            var outputPath = Path.Combine(projectInstance.Directory, outputDir, targetFile);

            var processorArchitecture = projectInstance.GetPropertyValue("ResolvedProcessorArchitecture");
            var resRuntimeCapability = projectInstance.GetPropertyValue("ResolvedRuntimeCapabilities");
#pragma warning disable 618
            var runtimeCapability = Enum.TryParse(resRuntimeCapability, out RuntimeCapabilities result)
                ? result.ToCapability()
#pragma warning restore 618
                : RuntimeCapability.FullComputation;

            var sourceFiles = GetItemsByType(projectInstance, "QSharpCompile");
            var csharpFiles = GetItemsByType(projectInstance, "Compile").Where(file => !file.EndsWith(".g.cs"));
            var projectReferences = GetItemsByType(projectInstance, "ProjectReference");
            var references = GetItemsByType(projectInstance, "Reference");

            var version = projectInstance.GetPropertyValue("QSharpLangVersion");
            var isExecutable = "QSharpExe".Equals(projectInstance.GetPropertyValue("ResolvedQSharpOutputType"), StringComparison.OrdinalIgnoreCase);
            var loadTestNames = "true".Equals(projectInstance.GetPropertyValue("ExposeReferencesViaTestNames"), StringComparison.OrdinalIgnoreCase);
            var defaultSimulator = projectInstance.GetPropertyValue("DefaultSimulator")?.Trim();

            var telemetryMeas = new Dictionary<string, int>();
            telemetryMeas["sources"] = sourceFiles.Count();
            telemetryMeas["csharpfiles"] = csharpFiles.Count();
            telemetryProps["defaultSimulator"] = defaultSimulator;
            this.sendTelemetry("project-load", telemetryProps, telemetryMeas); // does not send anything unless the corresponding flag is defined upon compilation
            info = new ProjectInformation(
                version,
                outputPath,
                runtimeCapability,
                isExecutable,
                string.IsNullOrWhiteSpace(processorArchitecture) ? "Unspecified" : processorArchitecture,
                loadTestNames,
                sourceFiles,
                projectReferences,
                references);
            return true;
        }

        internal Uri QsTemporaryProjectLoader(Uri sourceFileUri, string? sdkVersion)
        {
            var sourceFolderPath = Path.GetDirectoryName(sourceFileUri.LocalPath) ?? "";
            var projectFileName = string.Join(
                "_x2f_", // arbitrary string to help avoid collisions
                sourceFolderPath
                    .Replace("_", "_x5f_") // arbitrary string to help avoid collisions
                    .Split(Path.GetInvalidFileNameChars()));
            var projectFolderPath = Directory.CreateDirectory(Path.Combine(
                Path.GetTempPath(),
                "qsharp",
                projectFileName)).FullName;
            var projectFilePath = Path.Combine(projectFolderPath, $"generated.csproj");
            using (var outputFile = new StreamWriter(projectFilePath))
            {
                outputFile.WriteLine(
                    TemporaryProject.GetFileContents(
                        compilationScope: Path.Combine(sourceFolderPath, "*.qs"),
                        sdkVersion: sdkVersion));
            }

            return new Uri(projectFilePath);
        }

        /// <summary>
        /// For each given uri, loads the corresponding project if the uri contains the project file for a Q# project,
        /// and publishes suitable diagnostics for it.
        /// </summary>
        public Task LoadProjectsAsync(IEnumerable<Uri> projects) =>
            this.projects.LoadProjectsAsync(projects, this.QsProjectLoader, this.GetOpenFile);

        /// <summary>
        /// If the given uri corresponds to the project file for a Q# project,
        /// updates that project in the list of tracked projects or adds it if needed, and publishes suitable diagnostics for it.
        /// </summary>
        public Task ProjectDidChangeOnDiskAsync(Uri project) =>
            this.projects.ProjectChangedOnDiskAsync(project, this.QsProjectLoader, this.GetOpenFile);

        /// <summary>
        /// Updates all tracked Q# projects that reference the assembly with the given uri
        /// either directly or indirectly via a reference to another Q# project, and publishes suitable diagnostics.
        /// </summary>
        public Task AssemblyDidChangeOnDiskAsync(Uri dllPath) =>
            this.projects.AssemblyChangedOnDiskAsync(dllPath);

        /// <summary>
        /// To be used whenever a .qs file is changed on disk.
        /// Reloads the file from disk and updates the project(s) which list it as souce file
        /// if the file is not listed as currently being open in the editor, publishing suitable diagnostics.
        /// If the file is listed as being open in the editor, updates all load diagnostics for the file,
        /// but does not update the file content, since the editor manages that one.
        /// </summary>
        public Task SourceFileDidChangeOnDiskAsync(Uri sourceFile) =>
            this.projects.SourceFileChangedOnDiskAsync(sourceFile, this.GetOpenFile);

        // routines related to tracking the editor state

        /// <summary>
        /// To be called whenever a file is opened in the editor.
        /// Does nothing if the given file is listed as to be ignored.
        /// Otherwise publishes suitable diagnostics for it.
        /// Invokes the given Action showError with a suitable message if the given file cannot be loaded.
        /// Invokes the given Action logError with a suitable message if the given file cannot be associated with a compilation unit,
        /// or if the given file is already listed as being open in the editor.
        /// </summary>
        /// <exception cref="ArgumentException">The URI of <paramref name="textDocument"/> is not an absolute file URI.</exception>
        internal Task OpenFileAsync(
            TextDocumentItem textDocument,
            Action<string, MessageType>? showError = null,
            Action<string, MessageType>? logError = null)
        {
            if (!ValidFileUri(textDocument.Uri))
            {
                throw new ArgumentException("invalid text document identifier");
            }
            _ = this.projects.ManagerTaskAsync(textDocument.Uri, (manager, associatedWithProject) =>
            {
                if (this.IgnoreFile(textDocument.Uri))
                {
                    return;
                }

                var newManager = CompilationUnitManager.InitializeFileManager(textDocument.Uri, textDocument.Text, this.publish, ex =>
                {
                    showError?.Invoke($"Failed to load file '{textDocument.Uri.LocalPath}'", MessageType.Error);
                    manager.LogException(ex);
                });

                // Currently it is not possible to handle both the behavior of VS and VS Code for changes on disk in a manner that will never fail.
                // To mitigate the impact of failures we choose to just log them as info.
                var file = this.openFiles.GetOrAdd(textDocument.Uri, newManager);
                // this may be the case (depending on the editor) e.g. when opening a version control diff ...
                if (file != newManager)
                {
                    showError?.Invoke(
                        $"Version control and opening multiple versions of the same file in the editor are currently not supported. \n" +
                        $"Intellisense has been disable for the file '{textDocument.Uri.LocalPath}'. An editor restart is required to enable intellisense again.",
                        MessageType.Error);
                    #if DEBUG
                    if (showError == null)
                    {
                        logError?.Invoke("Attempting to open a file that is already open in the editor.", MessageType.Error);
                    }
                    #endif

                    this.IgnoreEditorUpdatesFor(textDocument.Uri);
                    this.openFiles.TryRemove(textDocument.Uri, out FileContentManager _);
                    if (!associatedWithProject)
                    {
                        _ = manager.TryRemoveSourceFileAsync(textDocument.Uri);
                    }
                    this.publish(new PublishDiagnosticParams { Uri = textDocument.Uri, Diagnostics = Array.Empty<Diagnostic>() });
                    return;
                }

                if (!associatedWithProject)
                {
                    logError?.Invoke(
                        $"The file {textDocument.Uri.LocalPath} is not associated with a compilation unit. Only syntactic diagnostics are generated.",
                        MessageType.Info);
                }

                _ = manager.AddOrUpdateSourceFileAsync(file);
            });
            // reloading from disk in case we encountered a file already open error above
            return this.projects.SourceFileChangedOnDiskAsync(textDocument.Uri, this.GetOpenFile); // NOTE: relies on that the manager task is indeed executed first!
        }

        /// <summary>
        /// To be called whenever a file is changed within the editor (i.e. changes are not necessarily reflected on disk).
        /// Does nothing if the given file is listed as to be ignored.
        /// </summary>
        /// <exception cref="ArgumentException">The URI of the text document identifier in the given parameter is not an absolute file URI.</exception>
        internal Task DidChangeAsync(DidChangeTextDocumentParams param)
        {
            if (!ValidFileUri(param.TextDocument.Uri))
            {
                throw new ArgumentException("invalid text document identifier");
            }
            return this.projects.ManagerTaskAsync(param.TextDocument.Uri, (manager, __) =>
            {
                if (this.IgnoreFile(param.TextDocument.Uri))
                {
                    return;
                }

                // Currently it is not possible to handle both the behavior of VS and VS Code for changes on disk in a manner that will never fail.
                // To mitigate the impact of failures we choose to ignore them silently.
                if (!this.openFiles.ContainsKey(param.TextDocument.Uri))
                {
                    return;
                }
                _ = manager.SourceFileDidChangeAsync(param); // independent on whether the file does or doesn't belong to a project
            });
        }

        /// <summary>
        /// Used to reload the file content when a file is saved.
        /// Does nothing if the given file is listed as to be ignored.
        /// Expects to get the entire content of the file at the time of saving as argument.
        /// </summary>
        /// <exception cref="ArgumentException">The URI of <paramref name="textDocument"/> is not an absolute file URI.</exception>
        internal Task SaveFileAsync(TextDocumentIdentifier textDocument, string fileContent)
        {
            if (!ValidFileUri(textDocument.Uri))
            {
                throw new ArgumentException("invalid text document identifier");
            }
            return this.projects.ManagerTaskAsync(textDocument.Uri, (manager, __) =>
            {
                if (this.IgnoreFile(textDocument.Uri))
                {
                    return;
                }

                // Currently it is not possible to handle both the behavior of VS and VS Code for changes on disk in a manner that will never fail.
                // To mitigate the impact of failures we choose to ignore them silently and do our best to recover.
                if (!this.openFiles.TryGetValue(textDocument.Uri, out var file))
                {
                    file = CompilationUnitManager.InitializeFileManager(textDocument.Uri, fileContent, this.publish, manager.LogException);
                    this.openFiles.TryAdd(textDocument.Uri, file);
                    _ = manager.AddOrUpdateSourceFileAsync(file);
                }
                else
                {
                    _ = manager.AddOrUpdateSourceFileAsync(file, fileContent); // let's reload the file content on saving
                }
            });
        }

        /// <summary>
        /// To be called whenever a file is closed in the editor.
        /// Does nothing if the given file is listed as to be ignored.
        /// Otherwise the file content is reloaded from disk (in case changes in the editor are discarded without closing), and the diagnostics are updated.
        /// Invokes the given Action onError with a suitable message if the given file is not listed as being open in the editor.
        /// </summary>
        /// <exception cref="ArgumentException">The URI of <paramref name="textDocument"/> is null or not an absolute file URI.</exception>
        internal Task CloseFileAsync(TextDocumentIdentifier textDocument, Action<string, MessageType>? onError = null)
        {
            if (textDocument is null || !ValidFileUri(textDocument.Uri))
            {
                throw new ArgumentException("invalid text document identifier");
            }
            _ = this.projects.ManagerTaskAsync(textDocument.Uri, (manager, associatedWithProject) => // needs to be *first* (due to the modification of OpenFiles)
            {
                if (this.IgnoreFile(textDocument.Uri))
                {
                    return;
                }

                // Currently it is not possible to handle both the behavior of VS and VS Code for changes on disk in a manner that will never fail.
                // To mitigate the impact of failures we choose to ignore them silently.
                var removed = this.openFiles.TryRemove(textDocument.Uri, out _);
                #if DEBUG
                if (!removed)
                {
                    onError?.Invoke($"Attempting to close file '{textDocument.Uri.LocalPath}' that is not currently listed as open in the editor.", MessageType.Error);
                }
                #endif
                if (!associatedWithProject)
                {
                    _ = manager.TryRemoveSourceFileAsync(textDocument.Uri);
                }
                this.publish(new PublishDiagnosticParams { Uri = textDocument.Uri, Diagnostics = Array.Empty<Diagnostic>() });
            });
            // When edits are made in a file, but those are discarded by closing the file and hitting "no, don't save",
            // no notification is sent for the now discarded changes;
            // hence we reload the file content from disk upon closing.
            return this.projects.SourceFileChangedOnDiskAsync(textDocument.Uri, this.GetOpenFile); // NOTE: relies on that the manager task is indeed executed first!
        }

        // routines related to providing information for editor commands

        /// <summary>
        /// Returns the workspace edit that describes the changes to be done if the symbol at the given position - if any - is renamed to the given name.
        /// Returns null if no symbol exists at the specified position,
        /// or if the specified uri is not a valid file uri.
        /// or if some parameters are unspecified (null) or inconsistent with the tracked editor state.
        /// </summary>
        public WorkspaceEdit? Rename(RenameParams param, bool versionedChanges = false) =>
            ValidFileUri(param?.TextDocument?.Uri) && !this.IgnoreFile(param?.TextDocument?.Uri) ? this.projects.Rename(param, versionedChanges) : null;

        /// <summary>
        /// Returns the source file and position where the item at the given position is declared at,
        /// if such a declaration exists, and returns the given position and file otherwise.
        /// Returns null if the given file is listed as to be ignored or if the information cannot be determined at this point.
        /// </summary>
        public Location? DefinitionLocation(TextDocumentPositionParams? param) =>
            ValidFileUri(param?.TextDocument?.Uri) && !this.IgnoreFile(param?.TextDocument?.Uri) ? this.projects.DefinitionLocation(param) : null;

        /// <summary>
        /// Returns the signature help information for a call expression if there is such an expression at the specified position.
        /// Returns null if the given file is listed as to be ignored,
        /// or if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the currently processed file content
        /// or if no call expression exists at the specified position at this time
        /// or if no signature help information can be provided for the call expression at the specified position.
        /// </summary>
        public SignatureHelp? SignatureHelp(TextDocumentPositionParams param, MarkupKind format = MarkupKind.PlainText) =>
            ValidFileUri(param?.TextDocument?.Uri) && !this.IgnoreFile(param?.TextDocument?.Uri) ? this.projects.SignatureHelp(param, format) : null;

        /// <summary>
        /// Returns information about the item at the specified position as Hover information.
        /// Returns null if the given file is listed as to be ignored,
        /// or if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the currently processed file content,
        /// or if no token exists at the specified position at this time.
        /// </summary>
        public Hover? HoverInformation(TextDocumentPositionParams param, MarkupKind format = MarkupKind.PlainText) =>
            ValidFileUri(param?.TextDocument?.Uri) && !this.IgnoreFile(param?.TextDocument?.Uri) ? this.projects.HoverInformation(param, format) : null;

        /// <summary>
        /// Returns an array with all usages of the identifier at the given position (if any) as DocumentHighlights.
        /// Returns null if the given file is listed as to be ignored,
        /// or if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the currently processed file content,
        /// or if no identifier exists at the specified position at this time.
        /// </summary>
        public DocumentHighlight[]? DocumentHighlights(TextDocumentPositionParams param) =>
            ValidFileUri(param?.TextDocument?.Uri) && !this.IgnoreFile(param?.TextDocument?.Uri) ? this.projects.DocumentHighlights(param) : null;

        /// <summary>
        /// Returns an array with all locations where the symbol at the given position - if any - is referenced.
        /// Returns null if the given file is listed as to be ignored,
        /// or if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the currently processed file content,
        /// or if no symbol exists at the specified position at this time.
        /// </summary>
        public Location[]? SymbolReferences(ReferenceParams param) =>
            ValidFileUri(param?.TextDocument?.Uri) && !this.IgnoreFile(param?.TextDocument?.Uri) ? this.projects.SymbolReferences(param) : null;

        /// <summary>
        /// Returns the SymbolInformation for each namespace declaration, type declaration, and function or operation declaration.
        /// Returns null if the given file is listed as to be ignored, or if the given parameter or its uri is null.
        /// </summary>
        public SymbolInformation[]? DocumentSymbols(DocumentSymbolParams param) =>
            ValidFileUri(param?.TextDocument?.Uri) && !this.IgnoreFile(param?.TextDocument?.Uri) ? this.projects.DocumentSymbols(param) : null;

        /// <summary>
        /// Returns a look-up of workspace edits suggested by the compiler for the given location and context.
        /// The key of the look-up is a suitable title for the corresponding edits that can be presented to the user.
        /// Returns null if the given file is listed as to be ignored, or if the given parameter or its uri is null.
        /// </summary>
        public ILookup<string, WorkspaceEdit>? CodeActions(CodeActionParams param) =>
            ValidFileUri(param?.TextDocument?.Uri) && !this.IgnoreFile(param?.TextDocument?.Uri) ? this.projects.CodeActions(param) : null;

        /// <summary>
        /// Returns a list of suggested completion items for the given location.
        /// <para/>
        /// Returns null if the given file is listed as to be ignored, or if the given parameter or its uri or position is null.
        /// </summary>
        public CompletionList? Completions(TextDocumentPositionParams param) =>
            ValidFileUri(param?.TextDocument?.Uri) && !this.IgnoreFile(param?.TextDocument?.Uri)
            ? this.projects.Completions(param)
            : null;

        /// <summary>
        /// Resolves additional information for the given completion item. Returns the original completion item if no
        /// additional information is available, or if the completion item is no longer valid or accurate.
        /// <para/>
        /// Returns null if any parameter is null or the file given in the original completion request is invalid or
        /// ignored.
        /// </summary>
        internal CompletionItem? ResolveCompletion(CompletionItem? item, CompletionItemData data, MarkupKind format) =>
            item != null && ValidFileUri(data?.TextDocument?.Uri) && !this.IgnoreFile(data?.TextDocument?.Uri)
            ? this.projects.ResolveCompletion(item, data, format)
            : null;

        // utils to query the editor state server for testing purposes
        // -> explicitly part of this class because any access to the resources may need to be coordinated as well

        /// <summary>
        /// Waits for all currently running or queued tasks to finish before getting the file content in memory.
        /// -> Method to be used for testing/diagnostic purposes only!
        /// </summary>
        internal string[]? FileContentInMemory(TextDocumentIdentifier textDocument) =>
            this.projects.FileContentInMemory(textDocument);

        /// <summary>
        /// Waits for all currently running or queued tasks to finish before getting the diagnostics for the given file.
        /// -> Method to be used for testing purposes only!
        /// </summary>
        internal Diagnostic[]? FileDiagnostics(TextDocumentIdentifier textDocument)
        {
            var allDiagnostics = this.projects.GetDiagnostics(textDocument?.Uri);
            return allDiagnostics?.Count() == 1 ? allDiagnostics.Single().Diagnostics : null; // count is > 1 if the given uri corresponds to a project file
        }
    }
}
