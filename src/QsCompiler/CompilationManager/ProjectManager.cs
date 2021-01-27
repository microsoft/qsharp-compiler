// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.Transformations;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    internal class ProjectProperties
    {
        public readonly string Version;
        public readonly string OutputPath;
        public readonly RuntimeCapability RuntimeCapability;
        public readonly bool IsExecutable;
        public readonly string ProcessorArchitecture;
        public readonly bool ExposeReferencesViaTestNames;

        public ProjectProperties(
            string version,
            string outputPath,
            RuntimeCapability runtimeCapability,
            bool isExecutable,
            string processorArchitecture,
            bool loadTestNames)
        {
            this.Version = version ?? "";
            this.OutputPath = outputPath;
            this.RuntimeCapability = runtimeCapability;
            this.IsExecutable = isExecutable;
            this.ProcessorArchitecture = processorArchitecture;
            this.ExposeReferencesViaTestNames = loadTestNames;
        }
    }

    public class ProjectInformation
    {
        public delegate bool Loader(Uri projectFile, [NotNullWhen(true)] out ProjectInformation? projectInfo);

        internal readonly ProjectProperties Properties;
        public readonly ImmutableArray<string> SourceFiles;
        public readonly ImmutableArray<string> ProjectReferences;
        public readonly ImmutableArray<string> References;

        internal static ProjectInformation Empty(
                string version,
                string outputPath,
                RuntimeCapability capability) =>
            new ProjectInformation(
                version,
                outputPath,
                capability,
                false,
                "Unspecified",
                false,
                Enumerable.Empty<string>(),
                Enumerable.Empty<string>(),
                Enumerable.Empty<string>());

        public ProjectInformation(
            string version,
            string outputPath,
            RuntimeCapability runtimeCapability,
            bool isExecutable,
            string processorArchitecture,
            bool loadTestNames,
            IEnumerable<string> sourceFiles,
            IEnumerable<string> projectReferences,
            IEnumerable<string> references)
        {
            this.Properties = new ProjectProperties(
                version, outputPath, runtimeCapability, isExecutable, processorArchitecture, loadTestNames);
            this.SourceFiles = sourceFiles.ToImmutableArray();
            this.ProjectReferences = projectReferences.ToImmutableArray();
            this.References = references.ToImmutableArray();
        }
    }

    public class ProjectManager : IDisposable
    {
        private class Project : IDisposable
        {
            public readonly Uri ProjectFile;

            public Uri? OutputPath { get; private set; }

            public ProjectProperties Properties { get; private set; }

            private bool isLoaded;

            /// <summary>
            /// contains the path of all specified source files,
            /// regardless of whether or not the path is valid, the file exists and could be loaded
            /// </summary>
            private ImmutableHashSet<Uri> specifiedSourceFiles = ImmutableHashSet<Uri>.Empty;

            /// <summary>
            /// contains the path to the dlls of all specified references,
            /// regardless of whether or not the path is valid, the file exists and could be loaded
            /// </summary>
            private ImmutableHashSet<Uri> specifiedReferences = ImmutableHashSet<Uri>.Empty;

            /// <summary>
            /// contains the path to the *project* file of all specified project references,
            /// regardless of whether or not the path is valid, and a project with the corresponding uri exists
            /// </summary>
            private ImmutableHashSet<Uri> specifiedProjectReferences = ImmutableHashSet<Uri>.Empty;

            /// <summary>
            /// contains the uris to all source files that have been successfully loaded and are incorporated into the compilation
            /// </summary>
            private ImmutableHashSet<Uri> loadedSourceFiles;

            /// <summary>
            /// contains the keys are the uris to all referenced dlls that have been successfully loaded and are incorporated into the compilation
            /// </summary>
            private References loadedReferences;

            /// <summary>
            /// contains the keys are the uris to the *project file* of all project references that have been successfully loaded and are incorporated into the compilation
            /// </summary>
            private References loadedProjectReferences;

            private readonly ProcessingQueue processing;
            internal readonly CompilationUnitManager Manager;
            private readonly Action<string, MessageType> log;

            /// <summary>
            /// Returns true if the file with the given uri has been specified to be a source file of this project.
            /// IMPORTANT: This routine queries the current state of the project and does *not* wait for queued or running tasks to finish!
            /// </summary>
            internal bool ContainsSourceFile(Uri sourceFile) =>
                this.specifiedSourceFiles?.Contains(sourceFile) ?? false;

            /// <summary>
            /// Returns true if any of the currently specified source files of this project satisfies the given criterion.
            /// If no criterion is given, returns true if the list of specified source files is not null.
            /// IMPORTANT: This routine queries the current state of the project and does *not* wait for queued or running tasks to finish!
            /// </summary>
            internal bool ContainsAnySourceFiles(Func<Uri, bool>? filter = null) =>
                this.specifiedSourceFiles?.Any(filter ?? (_ => true)) ?? false; // keep this as specified, *not* loaded!

            public void Dispose() =>
                this.processing.QueueForExecutionAsync(() => this.Manager.Dispose());

            private ImmutableArray<Diagnostic> generalDiagnostics;
            private ImmutableArray<Diagnostic> sourceFileDiagnostics;
            private ImmutableArray<Diagnostic> referenceDiagnostics;
            private ImmutableArray<Diagnostic> projectReferenceDiagnostics;

            /// <summary>
            /// Initializes the project for the given project file with the given project information.
            /// If an Action for publishing diagnostics is given and not null,
            /// that action is called whenever diagnostics for the project have changed and are ready for publishing.
            /// </summary>
            internal Project(
                Uri projectFile,
                ProjectInformation projectInfo,
                Action<Exception>? onException,
                Action<PublishDiagnosticParams>? publishDiagnostics,
                Action<string, MessageType>? log)
            {
                this.ProjectFile = projectFile;
                this.Properties = projectInfo.Properties;
                this.SetProjectInformation(projectInfo);

                var version = Version.TryParse(projectInfo.Properties.Version, out Version v) ? v : null;
                if (projectInfo.Properties.Version.Equals("Latest", StringComparison.InvariantCultureIgnoreCase))
                {
                    version = new Version(0, 3);
                }
                var ignore = version == null || version < new Version(0, 3) ? true : false;

                // We track the file contents for unsupported projects in case the files are migrated to newer projects while editing,
                // but we don't do any semantic verification, and we don't publish diagnostics for them.
                this.processing = new ProcessingQueue(onException);
                this.Manager = new CompilationUnitManager(
                    onException,
                    ignore ? null : publishDiagnostics,
                    syntaxCheckOnly: ignore,
                    this.Properties.RuntimeCapability,
                    this.Properties.IsExecutable,
                    this.Properties.ProcessorArchitecture);
                this.log = log ?? ((msg, severity) => Console.WriteLine($"{severity}: {msg}"));

                this.loadedSourceFiles = ImmutableHashSet<Uri>.Empty;
                this.loadedReferences = References.Empty;
                this.loadedProjectReferences = References.Empty;
            }

            /// <summary>
            /// Sets the output path and all specified source files, references and project references
            /// to those specified by the given project information.
            /// Generates a suitable diagnostics if the output uri cannot be determined.
            /// </summary>
            private void SetProjectInformation(ProjectInformation projectInfo)
            {
                this.Properties = projectInfo.Properties;
                this.isLoaded = false;

                var outputPath = projectInfo.Properties.OutputPath;
                try
                {
                    outputPath = Path.GetFullPath(outputPath);
                }
                catch
                {
                    outputPath = null;
                }
                var outputUri = Uri.TryCreate(outputPath, UriKind.Absolute, out Uri uri) ? uri : null;
                this.OutputPath = outputUri;

                this.generalDiagnostics = this.OutputPath == null
                    ? ImmutableArray.Create(Errors.LoadError(ErrorCode.InvalidProjectOutputPath, new[] { this.ProjectFile.LocalPath }, MessageSource(this.ProjectFile)))
                    : ImmutableArray<Diagnostic>.Empty;

                this.specifiedSourceFiles = projectInfo.SourceFiles
                    .SelectNotNull(f => Uri.TryCreate(f, UriKind.Absolute, out uri) ? uri : null)
                    .ToImmutableHashSet();
                this.specifiedReferences = projectInfo.References
                    .SelectNotNull(f => Uri.TryCreate(f, UriKind.Absolute, out uri) ? uri : null)
                    .ToImmutableHashSet();
                this.specifiedProjectReferences = projectInfo.ProjectReferences
                    .SelectNotNull(f => Uri.TryCreate(f, UriKind.Absolute, out uri) ? uri : null)
                    .ToImmutableHashSet();
            }

            /// <summary>
            /// If the project is not yet loaded, loads all specified source file, dll references and project references
            /// using the given dictionary to resolve the dll output paths for project references.
            /// Generates suitable load diagnostics.
            /// Calls the given action RemoveFiles with the uris of all files that are no longer part of this project.
            /// Calls the given function GetExistingFileManagers to get all existing managers for the files that are newly part of this project.
            /// Does *not* update the content of already existing file managers.
            /// </summary>
            private void LoadProject(
                IDictionary<Uri, Uri?> projectOutputPaths,
                Func<ImmutableHashSet<Uri>, Uri, IEnumerable<FileContentManager>>? getExistingFileManagers,
                Action<ImmutableHashSet<Uri>, Task>? removeFiles)
            {
                if (this.isLoaded)
                {
                    return;
                }
                this.isLoaded = true;

                this.log($"Loading project '{this.ProjectFile.LocalPath}'.", MessageType.Log);
                if (!this.Manager.EnableVerification)
                {
                    this.log(
                        $"The Q# language server functionality is partially disabled for project {this.ProjectFile.LocalPath}. " +
                        $"The full functionality will be available after updating the project to Q# version 0.3 or higher.",
                        MessageType.Warning);
                }
                this.LoadReferencedAssembliesAsync(this.specifiedReferences.Select(uri => uri.LocalPath), skipVerification: true);
                this.LoadProjectReferencesAsync(projectOutputPaths, this.specifiedProjectReferences.Select(uri => uri.LocalPath), skipVerification: true);
                this.LoadSourceFilesAsync(this.specifiedSourceFiles.Select(uri => uri.LocalPath), getExistingFileManagers, removeFiles, skipIfAlreadyLoaded: true)
                .ContinueWith(_ => this.log($"Done loading project '{this.ProjectFile.LocalPath}'", MessageType.Log), TaskScheduler.Default);
                this.Manager.PublishDiagnostics(this.CurrentLoadDiagnostics());
            }

            /// <summary>
            /// Loads the content of all specified source files, dll references and project references,
            /// using the given dictionary to resolve the dll output paths for project references.
            /// If the project information is specified, updates the project information with the given value before loading.
            /// Generates suitable load diagnostics.
            /// Calls the given action RemoveFiles with the uris of all files that are no longer part of this project.
            /// Calls the given function GetExistingFileManagers to get all existing managers for the files that are newly part of this project.
            /// Does *not* update the content of already existing file managers.
            /// </summary>
            internal Task LoadProjectAsync(
                IDictionary<Uri, Uri?> projectOutputPaths,
                Func<ImmutableHashSet<Uri>, Uri, IEnumerable<FileContentManager>>? getExistingFileManagers,
                Action<ImmutableHashSet<Uri>, Task>? removeFiles,
                ProjectInformation? projectInfo = null) =>
                this.processing.QueueForExecutionAsync(() =>
                {
                    if (projectInfo != null)
                    {
                        this.SetProjectInformation(projectInfo);
                    }
                    this.LoadProject(projectOutputPaths, getExistingFileManagers, removeFiles);
                });

            // private routines used whenever the project itself is updated
            // -> need to be called from within appropriately queued routines only!

            /// <summary>
            /// Helper function used to generate suitable diagnostics upon project reference loading.
            /// Returns a function that given the uri to a project files, returns the corresponding output path,
            /// if the corrsponding entry in the given dictionary indeed exist.
            /// If no such entry exists, generates a suitable error messages and adds it to the given list of diagnostics.
            /// </summary>
            private static Func<Uri, Uri?> GetProjectOutputPath(IDictionary<Uri, Uri?> projectOutputPaths, List<Diagnostic> diagnostics) => (projFile) =>
            {
                if (projectOutputPaths.TryGetValue(projFile, out var referencedProj))
                {
                    return referencedProj;
                }
                diagnostics.Add(Warnings.LoadWarning(WarningCode.ReferenceToUnknownProject, new[] { projFile.LocalPath }, MessageSource(projFile)));
                return null;
            };

            /// <summary>
            /// Loads the given project references from disk using the given mapping
            /// to determine the path to the built dll for each project file,
            /// and updates the load diagnostics accordingly.
            /// If skipVerification is set to true, does push the updated project references to the CompilationUnitManager,
            /// but suppresses the compilation unit wide type checking that would usually ensue.
            /// Otherwise replaces *all* project references in the CompilationUnitManager with the newly loaded ones.
            /// </summary>
            private Task LoadProjectReferencesAsync(
                IDictionary<Uri, Uri?> projectOutputPaths, IEnumerable<string> projectReferences, bool skipVerification = false)
            {
                var diagnostics = new List<Diagnostic>();
                var loadedHeaders = LoadProjectReferences(
                    projectReferences,
                    GetProjectOutputPath(projectOutputPaths, diagnostics),
                    diagnostics.Add,
                    this.Manager.LogException);

                this.loadedProjectReferences = new References(loadedHeaders, this.Properties.ExposeReferencesViaTestNames);
                var importedDeclarations = this.loadedReferences.CombineWith(
                    this.loadedProjectReferences,
                    (code, args) => diagnostics.Add(Errors.LoadError(code, args, MessageSource(this.ProjectFile))));
                this.projectReferenceDiagnostics = diagnostics.ToImmutableArray();
                return this.Manager.UpdateReferencesAsync(importedDeclarations, suppressVerification: skipVerification);
            }

            /// <summary>
            /// Given a dictionary mapping each project files to the corresponding output path of the built project dll,
            /// as well as a uri to the project file of the project reference to reload, reloads that project reference
            /// using the given dictionary, and adapting all load diagnostics accordingly.
            /// Updates the reloaded reference in the CompilationUnitManager.
            /// Publishes the updated load diagnostics using the publisher of the CompilationUnitManager.
            /// Does nothing if the given project reference is not referenced by this project.
            /// </summary>
            /// <exception cref="ArgumentException"><paramref name="projectReference"/> is not an absolute file URI.</exception>
            private void ReloadProjectReference(IDictionary<Uri, Uri?> projectOutputPaths, Uri projectReference)
            {
                if (!this.specifiedProjectReferences.Contains(projectReference) || !this.isLoaded)
                {
                    return;
                }

                var projRefId = CompilationUnitManager.GetFileId(projectReference);
                var diagnostics = new List<Diagnostic>();
                var loadedHeaders = LoadProjectReferences(
                    new string[] { projectReference.LocalPath },
                    GetProjectOutputPath(projectOutputPaths, diagnostics),
                    diagnostics.Add,
                    this.Manager.LogException);
                var loaded = new References(loadedHeaders, this.Properties.ExposeReferencesViaTestNames);

                QsCompilerError.Verify(
                    !loaded.Declarations.Any() ||
                    (loaded.Declarations.Count == 1 && loaded.Declarations.First().Key == projRefId),
                    $"loaded references upon loading {projectReference.LocalPath}: {string.Join(", ", loaded.Declarations.Select(r => r.Value))}");
                this.loadedProjectReferences = this.loadedProjectReferences.Remove(projRefId).CombineWith(loaded);
                var importedDeclarations = this.loadedReferences.CombineWith(
                    this.loadedProjectReferences,
                    (code, args) => diagnostics.Add(Errors.LoadError(code, args, MessageSource(this.ProjectFile))));

                this.projectReferenceDiagnostics = this.projectReferenceDiagnostics.RemoveAll(d =>
                        (d.Source == MessageSource(projectReference) && d.IsWarning() && d.Code != WarningCode.DuplicateProjectReference.Code())
                        || DiagnosticTools.ErrorType(ErrorCode.ConflictInReferences)(d))
                    .Concat(diagnostics).ToImmutableArray();
                this.Manager.PublishDiagnostics(this.CurrentLoadDiagnostics());
                this.Manager.UpdateReferencesAsync(importedDeclarations);
            }

            /// <summary>
            /// Loads the given referenced dlls from disk and updates the load diagnostics accordingly.
            /// If skipVerification is set to true, does push the updated references to the CompilationUnitManager,
            /// but suppresses the compilation unit wide type checking that would usually ensue.
            /// Otherwise replaces *all* references in the CompilationUnitManager with the newly loaded ones.
            /// </summary>
            private Task LoadReferencedAssembliesAsync(IEnumerable<string> references, bool skipVerification = false)
            {
                var diagnostics = new List<Diagnostic>();
                var loadedHeaders = LoadReferencedAssemblies(
                    references,
                    diagnostics.Add,
                    this.Manager.LogException);

                this.loadedReferences = new References(loadedHeaders, this.Properties.ExposeReferencesViaTestNames);
                var importedDeclarations = this.loadedReferences.CombineWith(
                    this.loadedProjectReferences,
                    (code, args) => diagnostics.Add(Errors.LoadError(code, args, MessageSource(this.ProjectFile))));
                this.referenceDiagnostics = diagnostics.ToImmutableArray();
                return this.Manager.UpdateReferencesAsync(importedDeclarations, suppressVerification: skipVerification);
            }

            /// <summary>
            /// Given a uri to the assembly to reload, reloads that reference, updates all load diagnostics accordingly,
            /// and updates the reloaded reference in the CompilationUnitManager.
            /// Publishes the updated load diagnostics using the publisher of the CompilationUnitManager.
            /// Does nothing if the given assembly is not referenced by this project.
            /// </summary>
            /// <exception cref="ArgumentException"><paramref name="reference"/> is not an absolute file URI.</exception>
            private void ReloadReferencedAssembly(Uri reference)
            {
                if (!this.specifiedReferences.Contains(reference) || !this.isLoaded)
                {
                    return;
                }

                var refId = CompilationUnitManager.GetFileId(reference);
                var diagnostics = new List<Diagnostic>();
                var loadedHeaders = LoadReferencedAssemblies(
                    new string[] { reference.LocalPath },
                    diagnostics.Add,
                    this.Manager.LogException);
                var loaded = new References(loadedHeaders, this.Properties.ExposeReferencesViaTestNames);

                QsCompilerError.Verify(
                    !loaded.Declarations.Any() ||
                    (loaded.Declarations.Count == 1 && loaded.Declarations.First().Key == refId),
                    $"loaded references upon loading {reference.LocalPath}: {string.Join(", ", loaded.Declarations.Select(r => r.Value))}");
                this.loadedReferences = this.loadedReferences.Remove(refId).CombineWith(loaded);
                var importedDeclarations = this.loadedReferences.CombineWith(
                    this.loadedProjectReferences,
                    (code, args) => diagnostics.Add(Errors.LoadError(code, args, MessageSource(this.ProjectFile))));

                this.referenceDiagnostics = this.referenceDiagnostics.RemoveAll(d =>
                        (d.Source == MessageSource(reference) && d.IsWarning() && d.Code != WarningCode.DuplicateBinaryFile.Code())
                        || DiagnosticTools.ErrorType(ErrorCode.ConflictInReferences)(d))
                    .Concat(diagnostics).ToImmutableArray();
                this.Manager.PublishDiagnostics(this.CurrentLoadDiagnostics());
                this.Manager.UpdateReferencesAsync(importedDeclarations);
            }

            /// <summary>
            /// Loads the given source files from disk and updates the load diagnostics accordingly.
            /// Removes all source files that are no longer specified or loaded from the CompilationUnitManager,
            /// and calls the given action RemoveFiles with the corresponding uris.
            /// Adds all source files that were not loaded before but are now to the CompilationUnitManager.
            /// Calls the given function GetExistingFileManagers to get all existing managers for files that are newly part of this project.
            /// If skipIfAlreadyLoaded is set to true, blindly adds those managers to the CompilationUnitManager without updating their content.
            /// Otherwise adds a *new* FileContentManager initialized with the content loaded from disk.
            /// For all other files, a new FileContentManager is initialized with the content loaded from disk.
            /// If skipIfAlreadyLoaded is set to true, the content of the files that were loaded before and are still loaded now
            /// is *not* updated in the CompilationUnitManager.
            /// Otherwise the FileContentManager of all files is replaced by a new one initialized with the content from disk.
            /// *Always* spawns a compilation unit wide type checking!
            /// </summary>
            private Task LoadSourceFilesAsync(
                IEnumerable<string> sourceFiles,
                Func<ImmutableHashSet<Uri>, Uri, IEnumerable<FileContentManager>>? getExistingFileManagers,
                Action<ImmutableHashSet<Uri>, Task>? removeFiles,
                bool skipIfAlreadyLoaded = false)
            {
                var diagnostics = new List<Diagnostic>();
                var loaded = LoadSourceFiles(sourceFiles, diagnostics.Add, this.Manager.LogException);
                this.sourceFileDiagnostics = diagnostics.ToImmutableArray();

                var doNotAdd = skipIfAlreadyLoaded ? this.loadedSourceFiles : Enumerable.Empty<Uri>();
                var addToManager = loaded.Keys.Except(doNotAdd).ToImmutableHashSet();
                var removeFromManager = this.loadedSourceFiles.Except(loaded.Keys);
                this.loadedSourceFiles = loaded.Keys.ToImmutableHashSet();

                var existingFileManagers = getExistingFileManagers?.Invoke(addToManager, this.ProjectFile)?.ToImmutableHashSet() ?? ImmutableHashSet<FileContentManager>.Empty;
                var knownFilesToAdd = skipIfAlreadyLoaded ? existingFileManagers : ImmutableHashSet<FileContentManager>.Empty;
                var newFilesToAdd = CompilationUnitManager.InitializeFileManagers(
                    addToManager.Except(knownFilesToAdd.Select(m => m.Uri)).ToImmutableDictionary(uri => uri, uri => loaded[uri]),
                    this.Manager.PublishDiagnostics,
                    this.Manager.LogException);

                var removal = this.Manager.TryRemoveSourceFilesAsync(removeFromManager, suppressVerification: true);
                removeFiles?.Invoke(removeFromManager, removal);
                return this.Manager.AddOrUpdateSourceFilesAsync(knownFilesToAdd.Union(newFilesToAdd));
            }

            /// <summary>
            /// Returns a copy of all current load diagnostics as PublishDiagnosticParams for the project file of this project.
            /// </summary>
            private PublishDiagnosticParams CurrentLoadDiagnostics()
            {
                Diagnostic[] diagnostics =
                    this.generalDiagnostics.Concat(
                    this.sourceFileDiagnostics).Concat(
                    this.projectReferenceDiagnostics).Concat(
                    this.referenceDiagnostics)
                    .Select(d => d.Copy()).ToArray();

                return new PublishDiagnosticParams
                {
                    Uri = this.ProjectFile,
                    Diagnostics = diagnostics
                };
            }

            // routines related to updating the loaded content of the project
            // -> i.e. asynchronous tasks that are queued into the Processing queue

            /// <summary>
            /// Given a dictionary mapping each project files to the corresponding output path of the built project dll,
            /// as well as a uri to the assembly to reload, reloads all project references with that output path, and/or any reference to that dll.
            /// Updates the load diagnostics accordingly, and publishes them using the publisher of the CompilationUnitManager.
            /// </summary>
            public Task ReloadAssemblyAsync(IDictionary<Uri, Uri?> projectOutputPaths, Uri dllPath)
            {
                var projectsWithThatOutputDll = projectOutputPaths.Where(pair => pair.Value == dllPath).Select(pair => pair.Key);
                return this.processing.QueueForExecutionAsync(() =>
                {
                    var updatedProjectReferences = this.specifiedProjectReferences.Intersect(projectsWithThatOutputDll);
                    foreach (var projFile in updatedProjectReferences)
                    {
                        this.ReloadProjectReference(projectOutputPaths, projFile);
                    }
                    this.ReloadReferencedAssembly(dllPath);
                });
            }

            /// <summary>
            /// Given a dictionary mapping each project files to the corresponding output path of the built project dll,
            /// as well as a uri to the project file of the project reference to reload, reloads that project reference
            /// using the given dictionary, and adapting all load diagnostics accordingly.
            /// Updates the reloaded reference in the CompilationUnitManager.
            /// Publishes the updated load diagnostics using the publisher of the CompilationUnitManager.
            /// Does nothing if the given project reference is not referenced by this project.
            /// </summary>
            public Task ReloadProjectReferenceAsync(IDictionary<Uri, Uri?> projectOutputPaths, Uri projectReference) =>
                this.processing.QueueForExecutionAsync(() =>
                    this.ReloadProjectReference(projectOutputPaths, projectReference));

            /// <summary>
            /// Given a uri to source file to reload, reloads that source file, and updates all load diagnostics accordingly,
            /// unless that file is open in the editor (i.e. openInEditor does not return null).
            /// If openInEditor returns null for the given source file,
            /// updates the content of the source file in the CompilationUnitManager.
            /// Publishes the updated load diagnostics using the publisher of the CompilationUnitManager.
            /// Does nothing if the given source file is open in the editor or not listed as a source file of this project.
            /// </summary>
            public Task ReloadSourceFileAsync(Uri sourceFile, Func<Uri, FileContentManager?>? openInEditor = null)
            {
                openInEditor ??= _ => null;

                return this.processing.QueueForExecutionAsync(() =>
                {
                    if (!this.specifiedSourceFiles.Contains(sourceFile) || !this.isLoaded || openInEditor(sourceFile) != null)
                    {
                        return;
                    }

                    var diagnostics = new List<Diagnostic>();
                    var loaded = LoadSourceFiles(new string[] { sourceFile.LocalPath }, diagnostics.Add, this.Manager.LogException);
                    QsCompilerError.Verify(loaded.Count() <= 1);

                    this.loadedSourceFiles = this.loadedSourceFiles.Remove(sourceFile).Concat(loaded.Keys).ToImmutableHashSet();
                    this.sourceFileDiagnostics = this.sourceFileDiagnostics
                        .RemoveAll(d => d.Source == MessageSource(sourceFile) && d.IsWarning() && d.Code != WarningCode.DuplicateSourceFile.Code())
                        .Concat(diagnostics).ToImmutableArray();
                    this.Manager.PublishDiagnostics(this.CurrentLoadDiagnostics());

                    var content = loaded.TryGetValue(sourceFile, out string fileContent) ? fileContent : null;
                    if (content == null)
                    {
                        this.Manager.TryRemoveSourceFileAsync(sourceFile);
                    }
                    else
                    {
                        var file = CompilationUnitManager.InitializeFileManager(sourceFile, content, this.Manager.PublishDiagnostics, this.Manager.LogException);
                        this.Manager.AddOrUpdateSourceFileAsync(file);
                    }
                });
            }

            /// <summary>
            /// If the given file is a loaded source file of this project,
            /// executes the given task for that file on the CompilationUnitManager.
            /// </summary>
            public bool ManagerTask(Uri file, Action<CompilationUnitManager> executeTask, IDictionary<Uri, Uri?> projectOutputPaths)
            {
                this.processing.QueueForExecution(
                    () =>
                    {
                        if (!this.specifiedSourceFiles.Contains(file))
                        {
                            return false;
                        }
                        if (!this.isLoaded)
                        {
                            try
                            {
                                QsCompilerError.RaiseOnFailure(() => this.LoadProject(projectOutputPaths, null, null), $"failed to load {this.ProjectFile.LocalPath}");
                            }
                            catch (Exception ex)
                            {
                                this.Manager.LogException(ex);
                            }
                        }

                        if (!this.loadedSourceFiles.Contains(file))
                        {
                            return false;
                        }
                        executeTask(this.Manager);
                        return true;
                    },
                    out bool didExecute);
                return didExecute;
            }

            /// <summary>
            /// Returns all diagnostics for this project accumulated upon loading.
            /// Note: this method waits for all currently running or queued tasks to finish before accumulating the diagnostics.
            /// </summary>
            public PublishDiagnosticParams? GetLoadDiagnostics() =>
                this.processing.QueueForExecution(
                    this.CurrentLoadDiagnostics,
                    out var param)
                ? param : null;
        }

        private readonly ProcessingQueue load;
        private readonly ConcurrentDictionary<Uri, Project> projects;
        private readonly CompilationUnitManager defaultManager;

        /// <summary>
        /// called whenever diagnostics within a file have changed and are ready for publishing -> may be null!
        /// </summary>
        private readonly Action<PublishDiagnosticParams>? publishDiagnostics;

        /// <summary>
        /// used to log exceptions raised during processing -> may be null!
        /// </summary>
        private readonly Action<Exception>? logException;

        /// <summary>
        /// general purpose logging routine used for major loading events -> may be null!
        /// </summary>
        private readonly Action<string, MessageType>? log;

        /// <summary>
        /// If an Action for publishing diagnostics is given and not null,
        /// that action is called whenever diagnostics for the project have changed and are ready for publishing.
        /// Any exceptions caught during processing are logged using the given exception logger.
        /// </summary>
        public ProjectManager(Action<Exception>? exceptionLogger, Action<string, MessageType>? log = null, Action<PublishDiagnosticParams>? publishDiagnostics = null)
        {
            this.load = new ProcessingQueue(exceptionLogger);
            this.projects = new ConcurrentDictionary<Uri, Project>();
            this.defaultManager = new CompilationUnitManager(exceptionLogger, publishDiagnostics, syntaxCheckOnly: true);
            this.publishDiagnostics = publishDiagnostics;
            this.logException = exceptionLogger;
            this.log = log;
        }

        /// <inheritdoc/>
        public void Dispose() =>
            this.load.QueueForExecution(() =>
            {
                foreach (var project in this.projects.Values)
                {
                    project.Dispose();
                }
            });

        /// <summary>
        /// Returns a function that given the uris of all files that have been added to a project,
        /// queries openInEditor to determine which of those files are currently open in the editor.
        /// Removes all such files from the default manager and returns their FileContentManagers.
        /// </summary>
        private Func<ImmutableHashSet<Uri>, Uri, IEnumerable<FileContentManager>> MigrateToProject(Func<Uri, FileContentManager?> openInEditor) =>
            (filesAddedToProject, projFile) =>
            {
                filesAddedToProject ??= ImmutableHashSet<Uri>.Empty;
                var openFiles = filesAddedToProject.SelectNotNull(openInEditor).ToImmutableArray();
                var removals = openFiles.Select(file =>
                {
                    this.log?.Invoke($"The file {file.Uri.LocalPath} has been associated with the compilation unit {projFile.LocalPath}.", MessageType.Log);
                    return this.defaultManager.TryRemoveSourceFileAsync(file.Uri, publishEmptyDiagnostics: false); // no need to clear diagnostics - new ones will be pushed by the project
                })
                .ToArray();
                if (removals.Any())
                {
                    Task.WaitAll(removals); // we *need* to wait here in order to make sure that change notifications are processed in order!!
                }
                return openFiles;
            };

        /// <summary>
        /// Returns a function that given the uris of all files that have been removed from a project,
        /// waits for the given removal task to finish before querying openInEditor
        /// to determine which of those files are currently open in the editor.
        /// Clears all verifications for those files and adds them to the default manager.
        /// The returned Action does nothing if the task passed as argument has been cancelled.
        /// The returned Action throws an ObjectDisposedException if the task passed as argument has been disposed.
        /// </summary>
        private Action<ImmutableHashSet<Uri>, Task> MigrateToDefaultManager(Func<Uri, FileContentManager?> openInEditor) =>
            (filesRemovedFromProject, removal) =>
            {
                if (removal.IsCanceled)
                {
                    return;
                }
                filesRemovedFromProject ??= ImmutableHashSet<Uri>.Empty;
                Task.WaitAll(removal); // we *need* to wait here in order to make sure that change notifications are processed in order!!
                var openFiles = filesRemovedFromProject.SelectNotNull(openInEditor).ToImmutableHashSet();
                foreach (var file in openFiles)
                {
                    this.log?.Invoke($"The file {file.Uri.LocalPath} is no longer associated with a compilation unit. Only syntactic diagnostics will be generated.", MessageType.Log);
                    file.ClearVerification();
                }
                this.defaultManager.AddOrUpdateSourceFilesAsync(openFiles);
            };

        // public routines related to tracking compilation units - i.e. routines handling coordination

        /// <summary>
        /// Used for initial project loading.
        /// Note that by calling this routine, all processing will be blocked until loading has finished...
        /// </summary>
        public Task LoadProjectsAsync(
            IEnumerable<Uri> projectFiles,
            ProjectInformation.Loader projectLoader,
            Func<Uri, FileContentManager?>? openInEditor = null)
        {
            openInEditor ??= _ => null;

            return this.load.QueueForExecutionAsync(() =>
            {
                foreach (var file in projectFiles)
                {
                    // ms build complains if a (design time) build is already in progress...
                    if (!projectLoader(file, out var info))
                    {
                        continue;
                    }
                    var project = new Project(file, info, this.logException, this.publishDiagnostics, this.log);
                    this.projects.AddOrUpdate(file, project, (k, v) => project);
                }

                var outputPaths = this.projects.ToImmutableDictionary(p => p.Key, p => p.Value.OutputPath);
                foreach (var file in projectFiles)
                {
                    if (!this.projects.TryGetValue(file, out var project))
                    {
                        continue;
                    }
                    if (project.ContainsAnySourceFiles(uri => openInEditor(uri) != null))
                    {
                        project.LoadProjectAsync(outputPaths, this.MigrateToProject(openInEditor), null);
                    }
                }
            });
        }

        /// <summary>
        /// To be used whenever a project file is added, removed or updated.
        /// *Not* to be used to update content (will not update content unless it is new/removed content)!
        /// </summary>
        public Task ProjectChangedOnDiskAsync(
            Uri projectFile,
            ProjectInformation.Loader projectLoader,
            Func<Uri, FileContentManager?>? openInEditor = null)
        {
            openInEditor ??= _ => null;

            // TODO: allow to cancel this task via cancellation token?
            return this.load.QueueForExecutionAsync(() =>
            {
                var existing = this.projects.TryRemove(projectFile, out Project current) ? current : null;

                if (!projectLoader(projectFile, out var info))
                {
                    existing?.LoadProjectAsync(
                        ImmutableDictionary<Uri, Uri?>.Empty,
                        null,
                        this.MigrateToDefaultManager(openInEditor),
                        ProjectInformation.Empty(
                            "Latest",
                            existing.OutputPath?.LocalPath ?? throw new Exception("Missing output path."),
                            RuntimeCapability.FullComputation))
                        ?.Wait(); // does need to block, or the call to the DefaultManager in ManagerTaskAsync needs to be adapted
                    if (existing != null)
                    {
                        this.ProjectReferenceChangedOnDiskChangeAsync(projectFile);
                    }
                    return;
                }

                var updated = existing ?? new Project(projectFile, info, this.logException, this.publishDiagnostics, this.log);
                this.projects.AddOrUpdate(projectFile, updated, (_, __) => updated);

                // If any of the files that are currently open in the editor is part of the project,
                // then we need to make sure to remove them from the default manager before adding them to the project.
                // Conversely, if a file that is open in the editor is removed from the project, we need to add it to the DefaultManager.
                updated.LoadProjectAsync(
                    this.projects.ToImmutableDictionary(p => p.Key, p => p.Value.OutputPath),
                    this.MigrateToProject(openInEditor),
                    this.MigrateToDefaultManager(openInEditor),
                    info)
                .ContinueWith(_ => this.ProjectReferenceChangedOnDiskChangeAsync(projectFile), TaskScheduler.Default);
            });
        }

        /// <summary>
        /// To be called whenever one of the tracked projects has been added, removed or updated
        /// in order to update all other projects referencing the modified one.
        /// </summary>
        private Task ProjectReferenceChangedOnDiskChangeAsync(Uri projFile) =>
            this.load.QueueForExecutionAsync(() =>
            {
                var projectOutputPaths = this.projects.ToImmutableDictionary(p => p.Key, p => p.Value.OutputPath);
                foreach (var project in this.projects.Values)
                {
                    project.ReloadProjectReferenceAsync(projectOutputPaths, projFile);
                }
            });

        /// <summary>
        /// To be called whenever a dll that may be referenced by one of the tracked projects is added, removed or changed on disk
        /// in order to update all projects referencing it accordingly.
        /// </summary>
        public Task AssemblyChangedOnDiskAsync(Uri dllPath) =>
            this.load.QueueForExecutionAsync(() =>
            {
                var projectOutputPaths = this.projects.ToImmutableDictionary(p => p.Key, p => p.Value.OutputPath);
                foreach (var project in this.projects.Values)
                {
                    project.ReloadAssemblyAsync(projectOutputPaths, dllPath);
                }
            });

        /// <summary>
        /// To be called whenever a source file that may belong to one of the tracked projects has changed on disk.
        /// For each tracked project reloads the source file from disk and updates the project accordingly,
        /// if the modified file is a source file of that project and not open in the editor
        /// (i.e. openInEditor is null or returns null for that file) at the time of execution.
        /// </summary>
        public Task SourceFileChangedOnDiskAsync(Uri sourceFile, Func<Uri, FileContentManager?>? openInEditor = null) =>
            this.load.QueueForExecutionAsync(() =>
            {
                foreach (var project in this.projects.Values)
                {
                    project.ReloadSourceFileAsync(sourceFile, openInEditor);
                }
            });

        // routines related to querying individual compilation managers (internally and externally)

        /// <summary>
        /// Returns the compilation unit manager for the project
        /// if the given file can be uniquely associated with a compilation unit.
        /// Returns the DefaultManager otherwise.
        /// NOTE: returns null if no CompilationUnitManager exists for the project, or if the given file is null.
        /// </summary>
        private CompilationUnitManager? Manager(Uri? file)
        {
            if (file == null)
            {
                return null;
            }
            var includedIn = this.projects.Values.Where(project => project.ContainsSourceFile(file));
            return includedIn.Count() == 1
                ? includedIn.Single().Manager
                : this.defaultManager;
        }

        /// <summary>
        /// If the given file can be uniquely associated with a compilation unit,
        /// executes the given Action on the CompilationUnitManager of that project (if one exists), passing true as second argument.
        /// Executes the given Action on the DefaultManager otherwise, passing false as second argument.
        /// </summary>
        public Task ManagerTaskAsync(Uri file, Action<CompilationUnitManager, bool> executeTask) =>
            this.load.QueueForExecutionAsync(() =>
            {
                var didExecute = false;
                var options = new ParallelOptions { TaskScheduler = TaskScheduler.Default };
                var projectOutputPaths = this.projects.ToImmutableDictionary(p => p.Key, p => p.Value.OutputPath);
                Parallel.ForEach(this.projects.Values, options, project =>
                {
                    if (project.ManagerTask(file, m => executeTask(m, true), projectOutputPaths))
                    {
                        didExecute = true;
                    }
                });
                if (!didExecute)
                {
                    executeTask(this.defaultManager, false);
                }
            });

        // editor commands that require blocking

        /// <summary>
        /// Returns the workspace edit that describes the changes to be done if the symbol at the given position - if any - is renamed to the given name.
        /// Returns null if no symbol exists at the specified position,
        /// or if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the file,
        /// or if a file affected by the rename operation belongs to several compilation units.
        /// </summary>
        public WorkspaceEdit? Rename(RenameParams? param, bool versionedChanges) // versionedChanges is unused (WorkspaceEdit contains both Changes and DocumentChanges, but the version nr is null)
        {
            if (param?.TextDocument?.Uri == null)
            {
                return null;
            }
            var success = this.load.QueueForExecution(
                () =>
                {
                    var options = new ParallelOptions { TaskScheduler = TaskScheduler.Default };
                    var projectOutputPaths = this.projects.ToImmutableDictionary(p => p.Key, p => p.Value.OutputPath);
                    var results = new ConcurrentBag<WorkspaceEdit>();

                    Parallel.ForEach(this.projects.Values, options, project => // the default manager does not support rename operations
                        {
                            project.ManagerTask(param.TextDocument.Uri, m => m.Rename(param)?.Apply(results.Add), projectOutputPaths);
                        });
                    return results;
                },
                out var edits);

            if (!success)
            {
                return null;
            }

            try
            {
                // if a file belongs to several compilation units, then this will fail
                var changes = edits.SelectMany(edit => edit.Changes)
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
                var documentChanges = edits.SelectMany(edit => edit.DocumentChanges).ToArray();
                return new WorkspaceEdit { Changes = changes, DocumentChanges = documentChanges };
            }
            catch
            {
                return null;
            }
        }

        // routines related to providing information for non-blocking editor commands
        // -> these commands need to be responsive and therefore won't wait for any processing to finish
        // -> if the query cannot be processed immediately, they simply return null

        /// <summary>
        /// Returns the source file and position where the item at the given position is declared at,
        /// if such a declaration exists, and returns null otherwise.
        /// Fails silently without logging anything if an exception occurs upon evaluating the query
        /// (occasional failures are to be expected as the evaluation is a readonly query running in parallel to the ongoing processing).
        /// </summary>
        public Location? DefinitionLocation(TextDocumentPositionParams? param) =>
            this.Manager(param?.TextDocument?.Uri)?.FileQuery(
                param?.TextDocument, (file, c) => file.DefinitionLocation(c, param?.Position?.ToQSharp()), suppressExceptionLogging: true);

        /// <summary>
        /// Returns the signature help information for a call expression if there is such an expression at the specified position.
        /// Returns null if the given file is listed as to be ignored,
        /// or if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the currently processed file content,
        /// or if no call expression exists at the specified position at this time,
        /// or if no signature help information can be provided for the call expression at the specified position.
        /// Fails silently without logging anything if an exception occurs upon evaluating the query
        /// (occasional failures are to be expected as the evaluation is a readonly query running in parallel to the ongoing processing).
        /// </summary>
        public SignatureHelp? SignatureHelp(TextDocumentPositionParams? param, MarkupKind format = MarkupKind.PlainText) =>
            this.Manager(param?.TextDocument?.Uri)?.FileQuery(
                param?.TextDocument, (file, c) => file.SignatureHelp(c, param?.Position?.ToQSharp(), format), suppressExceptionLogging: true);

        /// <summary>
        /// Returns information about the item at the specified position as Hover information.
        /// Returns null if some parameters are unspecified (null),
        /// or if the specified file is not listed as source file,
        /// or if the specified position is not a valid position within the currently processed file content,
        /// or if no token exists at the specified position.
        /// Fails silently without logging anything if an exception occurs upon evaluating the query
        /// (occasional failures are to be expected as the evaluation is a readonly query running in parallel to the ongoing processing).
        /// </summary>
        public Hover? HoverInformation(TextDocumentPositionParams? param, MarkupKind format = MarkupKind.PlainText) =>
            this.Manager(param?.TextDocument?.Uri)?.FileQuery(
                param?.TextDocument, (file, c) => file.HoverInformation(c, param?.Position?.ToQSharp(), format), suppressExceptionLogging: true);

        /// <summary>
        /// Returns an array with all usages of the identifier at the given position (if any) as DocumentHighlights.
        /// Returns if some parameters are unspecified (null),
        /// or if the specified file is not listed as source file,
        /// or if the specified position is not a valid position within the currently processed file content,
        /// or if no identifier exists at the specified position at this time.
        /// Fails silently without logging anything if an exception occurs upon evaluating the query
        /// (occasional failures are to be expected as the evaluation is a readonly query running in parallel to the ongoing processing).
        /// </summary>
        public DocumentHighlight[]? DocumentHighlights(TextDocumentPositionParams? param) =>
            this.Manager(param?.TextDocument?.Uri)?.FileQuery(
                param?.TextDocument, (file, c) => file.DocumentHighlights(c, param?.Position?.ToQSharp()), suppressExceptionLogging: true);

        /// <summary>
        /// Returns an array with all locations where the symbol at the given position - if any - is referenced.
        /// Returns null if some parameters are unspecified (null),
        /// or if the specified file is not listed as source file,
        /// or if the specified position is not a valid position within the currently processed file content,
        /// or if no symbol exists at the specified position at this time.
        /// Fails silently without logging anything if an exception occurs upon evaluating the query
        /// (occasional failures are to be expected as the evaluation is a readonly query running in parallel to the ongoing processing).
        /// </summary>
        public Location[]? SymbolReferences(ReferenceParams? param) =>
            this.Manager(param?.TextDocument?.Uri)?.FileQuery(
                param?.TextDocument, (file, c) => file.SymbolReferences(c, param?.Position?.ToQSharp(), param?.Context), suppressExceptionLogging: true);

        /// <summary>
        /// Returns the SymbolInformation for each namespace declaration,
        /// type declaration, and function or operation declaration within the specified file.
        /// Returns null if given uri is null or if the specified file is not listed as source file.
        /// Fails silently without logging anything if an exception occurs upon evaluating the query
        /// (occasional failures are to be expected as the evaluation is a readonly query running in parallel to the ongoing processing).
        /// </summary>
        public SymbolInformation[]? DocumentSymbols(DocumentSymbolParams? param) =>
            this.Manager(param?.TextDocument?.Uri)?.FileQuery(
                param?.TextDocument, (file, _) => file.DocumentSymbols(), suppressExceptionLogging: true);

        /// <summary>
        /// Returns a look-up of workspace edits suggested by the compiler for the given location and context.
        /// The key of the look-up is a suitable title for the corresponding edits that can be presented to the user.
        /// Returns null if given uri is null or if the specified file is not listed as source file.
        /// Fails silently without logging anything if an exception occurs upon evaluating the query
        /// (occasional failures are to be expected as the evaluation is a readonly query running in parallel to the ongoing processing).
        /// </summary>
        public ILookup<string, WorkspaceEdit>? CodeActions(CodeActionParams? param) =>
            this.Manager(param?.TextDocument?.Uri)?.FileQuery(
                param?.TextDocument, (file, c) => file.CodeActions(c, param?.Range?.ToQSharp(), param?.Context), suppressExceptionLogging: true);

        /// <summary>
        /// Returns a list of suggested completion items for the given location.
        /// <para/>
        /// Returns null if given uri or position is null, or if the specified file is not listed as source file. Fails silently
        /// without logging anything if an exception occurs upon evaluating the query (occasional failures are to be
        /// expected as the evaluation is a readonly query running in parallel to the ongoing processing).
        /// </summary>
        public CompletionList? Completions(TextDocumentPositionParams? param) =>
            this.Manager(param?.TextDocument?.Uri)?.FileQuery(
                param?.TextDocument,
                (file, compilation) => file.Completions(compilation, param?.Position?.ToQSharp()),
                suppressExceptionLogging: true);

        /// <summary>
        /// Resolves additional information for the given completion item.
        /// <para/>
        /// Returns null if the completion item data is null, the file URI given in the data is null, or if the file is
        /// not a source file.
        /// <para/>
        /// Fails silently without logging anything if an exception occurs upon evaluating the query (occasional
        /// failures are to be expected as the evaluation is a read-only query running in parallel to the ongoing
        /// processing).
        /// </summary>
        public CompletionItem? ResolveCompletion(CompletionItem item, CompletionItemData? data, MarkupKind format) =>
            this.Manager(data?.TextDocument?.Uri)?.FileQuery(
                data?.TextDocument,
                (_, compilation) => compilation.ResolveCompletion(item, data, format),
                suppressExceptionLogging: true);

        // routines related to querying the state of the project manager
        // -> these routines will wait for any processing to finish before executing the query

        /// <summary>
        /// Returns a copy of the current diagnostics generated upon loading.
        /// Note: this method waits for all currently running or queued tasks to finish
        /// before getting the project loading diagnostics by calling FlushAndExecute.
        /// </summary>
        public IEnumerable<Diagnostic>? GetProjectDiagnostics(Uri projectId)
        {
            if (projectId == null)
            {
                return null;
            }
            this.load.QueueForExecution(
                () =>
                {
                    if (!this.projects.TryGetValue(projectId, out Project project))
                    {
                        return null;
                    }

                    return project.GetLoadDiagnostics()?.Diagnostics;
                },
                out var diagnostics);
            return diagnostics;
        }

        /// <summary>
        /// If the given Uri corresponds to the id of one of the managed project,
        /// returns the diagnostics for all source files in that project, but *not* the diagnostics generated upon loading.
        /// If the given Uri corresponds to a source file in any of the managed projects (including in the DefaultManager),
        /// returns an array with a single item containing all current diagnostics for the given file.
        /// If the given Uri is null, returns the diagnostics for all source files in the default manager.
        /// Note: this method waits for all currently running or queued tasks to finish
        /// before accumulating the diagnostics by calling FlushAndExecute.
        /// </summary>
        public PublishDiagnosticParams[]? GetDiagnostics(Uri? file)
        {
            this.load.QueueForExecution(
                () =>
                {
                    if (file == null)
                    {
                        return this.defaultManager.GetDiagnostics();
                    }
                    if (this.projects.TryGetValue(file, out Project project))
                    {
                        return project.Manager?.GetDiagnostics();
                    }

                    // NOTE: the call below prevents any consolidating of the processing queues
                    // of the project manager and the compilation unit manager (dead locks)!
                    var manager = this.Manager(file);
                    return manager?.GetDiagnostics(new TextDocumentIdentifier { Uri = file });
                },
                out var diagnostics);
            return diagnostics;
        }

        /// <summary>
        /// Returns the content (text representation) of the given file,
        /// if it is listed as source of a project or in the default manager.
        /// Returns null if the given file is null.
        /// Note: this method waits for all currently running or queued tasks to finish
        /// before getting the file content by calling FlushAndExecute.
        /// </summary>
        public string[]? FileContentInMemory(TextDocumentIdentifier textDocument)
        {
            if (textDocument?.Uri == null)
            {
                return null;
            }
            this.load.QueueForExecution(
                () =>
                {
                    // NOTE: the call below prevents any consolidating of the processing queues
                    // of the project manager and the compilation unit manager (dead locks)!
                    var manager = this.Manager(textDocument.Uri);
                    return manager?.FileContentInMemory(textDocument);
                },
                out var content);
            return content;
        }

        // static routines related to loading the content needed for compilation

        public static string MessageSource(Uri uri) =>
            uri.IsAbsoluteUri && uri.IsFile
                ? CompilationUnitManager.GetFileId(uri)
                : uri.AbsolutePath;

        /// <summary>
        /// For the given sequence of file names verifies that a file with the corresponding full path exists,
        /// and returns a sequence containing the absolute path for all files that do.
        /// Sets the corresponding out parameter to a sequence with all duplicate file names,
        /// and to a sequence of names for which no such file exists respectively.
        /// Filters all file names that are null or only consist of whitespace.
        /// Generates suitable diagnostics for duplicate and not found files, and for invalid paths.
        /// Logs the generated diagnostics using onDiagnostics if the action has been specified and is not null.
        /// Catches exceptions related to path errors and logs them using onException if the action has been specified and is not null.
        /// </summary>
        public static IEnumerable<Uri> FilterFiles(
            IEnumerable<string> files,
            WarningCode duplicateFileWarning,
            Func<string, string, Diagnostic> fileNotFoundDiagnostic,
            out IEnumerable<Uri> notFound,
            out IEnumerable<Uri> duplicates,
            out IEnumerable<(string, Exception)> invalidPaths,
            Action<Diagnostic>? onDiagnostic = null,
            Action<Exception>? onException = null)
        {
            var exceptions = new List<(string, Exception)>();
            Uri? WithFullPath(string file)
            {
                try
                {
                    return new Uri(Path.GetFullPath(file));
                }
                catch (Exception ex)
                {
                    exceptions.Add((file, ex));
                    return null;
                }
            }

            var uris = files
                .Where(file => !string.IsNullOrWhiteSpace(file))
                .SelectNotNull(WithFullPath)
                .ToList();
            invalidPaths = exceptions.ToImmutableArray();
            duplicates = uris.GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).ToImmutableArray();
            var distinctSources = new HashSet<Uri>(uris);
            notFound = distinctSources.Where(f => !File.Exists(f.LocalPath)).ToImmutableArray();

            foreach (var file in duplicates)
            {
                onDiagnostic?.Invoke(Warnings.LoadWarning(duplicateFileWarning, new[] { file.LocalPath }, MessageSource(file)));
            }
            foreach (var file in notFound)
            {
                onDiagnostic?.Invoke(fileNotFoundDiagnostic(file.LocalPath, MessageSource(file)));
            }
            foreach (var (file, ex) in invalidPaths)
            {
                onDiagnostic?.Invoke(Errors.LoadError(ErrorCode.InvalidFilePath, new[] { file }, file));
                onException?.Invoke(ex);
            }
            return distinctSources.Where(f => File.Exists(f.LocalPath)).ToImmutableArray();
        }

        /// <summary>
        /// Uses FilterFiles to filter the source files specified in the given options (Input), and generates the corresponding errors and warnings.
        /// For each valid source file, generates the corrsponding TextDocumentIdentifier and reads the file content from disk.
        /// Generates a suitable error whenever the file content could not be loaded.
        /// Calls the given onDiagnostic action on all generated diagnostics.
        /// Returns the uri and file content for each file that could be loaded.
        /// </summary>
        public static ImmutableDictionary<Uri, string> LoadSourceFiles(
            IEnumerable<string> sourceFiles,
            Action<Diagnostic>? onDiagnostic = null,
            Action<Exception>? onException = null)
        {
            string? GetFileContent(Uri file)
            {
                try
                {
                    return File.ReadAllText(file.LocalPath);
                }
                catch (Exception ex)
                {
                    onDiagnostic?.Invoke(Errors.LoadError(ErrorCode.CouldNotLoadSourceFile, new[] { file.LocalPath }, MessageSource(file)));
                    onException?.Invoke(ex);
                    return null;
                }
            }

            static Diagnostic NotFoundDiagnostic(string notFound, string source) => Errors.LoadError(ErrorCode.UnknownSourceFile, new[] { notFound }, source);
            var found = FilterFiles(
                sourceFiles,
                WarningCode.DuplicateSourceFile,
                NotFoundDiagnostic,
                out IEnumerable<Uri> notFound,
                out IEnumerable<Uri> duplicates,
                out IEnumerable<(string, Exception)> invalidPaths,
                onDiagnostic,
                onException);
            return found
                .SelectNotNull(file => GetFileContent(file)?.Apply(content => (file, content)))
                .ToImmutableDictionary(source => source.file, source => source.content);
        }

        /// <summary>
        /// Loads the Q# data structures in a referenced assembly given the Uri to that assembly.
        /// Generates suitable diagostics if the specified assembly could not be found or its content could not be loaded,
        /// and calls the given onDiagnostic action on all generated diagnostics.
        /// Catches any thrown exception and calls onException on it if it is specified and not null.
        /// </summary>
        private static References.Headers? LoadReferencedDll(
            Uri asm,
            bool ignoreDllResources,
            Action<Diagnostic>? onDiagnostic = null,
            Action<Exception>? onException = null)
        {
            try
            {
                try
                {
                    // will throw if the file is not a valid assembly
                    AssemblyName.GetAssemblyName(asm.LocalPath);
                }
                catch (FileLoadException)
                {
                    // the file is already loaded -> we can ignore that one
                }
                if (!AssemblyLoader.LoadReferencedAssembly(asm, out var headers, ignoreDllResources))
                {
                    onDiagnostic?.Invoke(Errors.LoadError(ErrorCode.UnrecognizedContentInReference, new[] { asm.LocalPath }, MessageSource(asm)));
                }
                return headers;
            }
            catch (BadImageFormatException ex)
            {
                onDiagnostic?.Invoke(Errors.LoadError(ErrorCode.FileIsNotAnAssembly, new[] { asm.LocalPath }, MessageSource(asm)));
                onException?.Invoke(ex);
                return null;
            }
            catch (Exception ex)
            {
                onDiagnostic?.Invoke(Warnings.LoadWarning(WarningCode.CouldNotLoadBinaryFile, new[] { asm.LocalPath }, MessageSource(asm)));
                onException?.Invoke(ex);
                return null;
            }
        }

        private static Task<References.Headers?> LoadReferencedDllAsync(
            Uri asm,
            bool ignoreDllResources,
            Action<Diagnostic>? onDiagnostic = null,
            Action<Exception>? onException = null)
        {
            return Task.Run(() => LoadReferencedDll(asm, ignoreDllResources, onDiagnostic, onException));
        }

        /// <summary>
        /// Returns the file id used for the file with the given uri.
        /// Raises a QsCompilerError if the id could not be determined.
        /// </summary>
        private static string GetFileId(Uri uri) =>
            QsCompilerError.RaiseOnFailure(
                () => CompilationUnitManager.GetFileId(uri),
                "could not determine id for valid uri");

        /// <summary>
        /// Uses FilterFiles to filter the given project files, and generates the corresponding errors and warnings.
        /// For each existing project file, calls GetOutputPath on it to obtain the path to the built dll for the project.
        /// For any exception due to a failure of GetOutputPath the given onException action is invoked.
        /// A failure of GetOutputPath consists of it throwing an exception, or returning a path that does exist but not correspond to a valid dll.
        /// If no file exists at the returned path, generates a suitable error message.
        /// Calls the given onDiagnostic action on all generated diagnostics.
        /// Returns a dictionary that maps each project file for which the corresponding dll content could be loaded to the Q# attributes it contains.
        /// </summary>
        public static ImmutableDictionary<string, References.Headers> LoadProjectReferences(
            IEnumerable<string> refProjectFiles,
            Func<Uri, Uri?> getOutputPath,
            Action<Diagnostic>? onDiagnostic = null,
            Action<Exception>? onException = null)
        {
            References.Headers? LoadReferencedDll(Uri asm) =>
                ProjectManager.LoadReferencedDll(asm, false, onException: onException); // any exception here is really a failure of GetOutputPath and will be treated as an unexpected exception

            static Diagnostic NotFoundDiagnostic(string notFound, string source) => Errors.LoadError(ErrorCode.UnknownProjectReference, new[] { notFound }, source);
            var existingProjectFiles = FilterFiles(
                refProjectFiles,
                WarningCode.DuplicateProjectReference,
                NotFoundDiagnostic,
                out IEnumerable<Uri> notFound,
                out IEnumerable<Uri> duplicates,
                out IEnumerable<(string, Exception)> invalidPaths,
                onDiagnostic,
                onException);

            Uri? TryGetOutputPath(Uri projFile)
            {
                try
                {
                    return getOutputPath(projFile);
                }
                catch (Exception ex)
                {
                    onException?.Invoke(ex);
                    return null;
                }
            }

            var projectDlls = existingProjectFiles // maps the *dll path* back to the corresponding project file!
                .SelectNotNull(projFile => TryGetOutputPath(projFile)?.Apply(output => (projFile, output)))
                .ToImmutableDictionary(p => p.output, p => p.projFile); // FIXME: take care of different projects having the same output path...
            var (existingProjectDlls, missingDlls) = projectDlls.Keys.Partition(f => File.Exists(f.LocalPath));
            foreach (var projFile in missingDlls.Select(dll => projectDlls[dll]))
            {
                onDiagnostic?.Invoke(Errors.LoadError(ErrorCode.MissingProjectReferenceDll, new[] { projFile.LocalPath }, MessageSource(projFile)));
            }

            return existingProjectDlls
                .SelectNotNull(file => LoadReferencedDll(file)?.Apply(headers => (file, headers)))
                .ToImmutableDictionary(asm => GetFileId(projectDlls[asm.file]), asm => asm.headers);
        }

        /// <summary>
        /// Returns a dictionary that maps each existing dll to the Q# attributes it contains.
        /// Generates a suitable error message for each binary file that could not be loaded.
        /// Calls the given onDiagnostic action on all generated diagnostics.
        /// </summary>
        public static ImmutableDictionary<string, References.Headers> LoadReferencedAssemblies(
            IEnumerable<string> references,
            Action<Diagnostic>? onDiagnostic = null,
            Action<Exception>? onException = null,
            bool ignoreDllResources = false)
        {
            References.Headers? LoadReferencedDll(Uri asm) =>
                ProjectManager.LoadReferencedDll(asm, ignoreDllResources, onDiagnostic, onException);

            var assembliesToLoad = GetAssembliesToLoad(references, onDiagnostic, onException);
            return assembliesToLoad
                .SelectNotNull(file => LoadReferencedDll(file)?.Apply(headers => (file, headers)))
                .ToImmutableDictionary(asm => GetFileId(asm.file), asm => asm.headers);
        }

        /// <summary>
        /// Returns a dictionary that maps each existing dll to the Q# attributes it contains which are loaded in parallel.
        /// Generates a suitable error message for each binary file that could not be loaded.
        /// Calls the given onDiagnostic action on all generated diagnostics.
        /// </summary>
        /// <remarks>This method waits for <see cref="Task"/>s to complete and may deadlock if invoked through a <see cref="Task"/>.</remarks>
        public static ImmutableDictionary<string, References.Headers> LoadReferencedAssembliesInParallel(
            IEnumerable<string> references,
            Action<Diagnostic>? onDiagnostic = null,
            Action<Exception>? onException = null,
            bool ignoreDllResources = false)
        {
            var assembliesToLoad = GetAssembliesToLoad(references, onDiagnostic, onException);
            var assemblyLoadingTaskTuples = new List<(Uri Assembly, Task<References.Headers?> Task)>();
            foreach (var assembly in assembliesToLoad.ToList())
            {
                var loadingTask = LoadReferencedDllAsync(assembly, ignoreDllResources, onDiagnostic, onException);
                assemblyLoadingTaskTuples.Add((assembly, loadingTask));
            }

            var loadingTasks = assemblyLoadingTaskTuples.Aggregate(
                new List<Task<References.Headers?>>(assemblyLoadingTaskTuples.Count),
                (tasksList, tuple) =>
                {
                    tasksList.Add(tuple.Task);
                    return tasksList;
                });

            Task.WaitAll(loadingTasks.ToArray());
            return assemblyLoadingTaskTuples
                .SelectNotNull(tuple => tuple.Task.Result?.Apply(headers => (tuple.Assembly, headers)))
                .ToImmutableDictionary(assemblyHeaderTuple => GetFileId(assemblyHeaderTuple.Assembly), assemblyHeaderTuple => assemblyHeaderTuple.headers);
        }

        /// <summary>
        /// Uses FilterFiles to filter the given references binary files, and generates the corresponding errors and warnings.
        /// Ignores any binary files that contain mscorlib.dll or a similar variant in their name.
        /// </summary>
        private static IEnumerable<Uri> GetAssembliesToLoad(
            IEnumerable<string> references,
            Action<Diagnostic>? onDiagnostic = null,
            Action<Exception>? onException = null)
        {
            var relevant = references.Where(file => file.IndexOf("mscorlib.dll", StringComparison.InvariantCultureIgnoreCase) < 0);
            static Diagnostic NotFoundDiagnostic(string notFound, string source) => Warnings.LoadWarning(WarningCode.UnknownBinaryFile, new[] { notFound }, source);
            var assembliesToLoad = FilterFiles(
                relevant,
                WarningCode.DuplicateBinaryFile,
                NotFoundDiagnostic,
                out IEnumerable<Uri> notFound,
                out IEnumerable<Uri> duplicates,
                out IEnumerable<(string, Exception)> invalidPaths,
                onDiagnostic,
                onException);

            return assembliesToLoad;
        }
    }
}
