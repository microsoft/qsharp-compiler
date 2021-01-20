// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    /// <summary>
    /// NOTE: This class intentionally does not give access to any FileContentManager that it manages,
    /// since it is responsible for coordinating access to (any routine of) the FileContentManager.
    /// </summary>
    public class CompilationUnitManager : IDisposable
    {
        internal readonly bool EnableVerification;

        /// <summary>
        /// the keys are the file identifiers of the source files obtained by GetFileId for the file uri and the values are the content of each file
        /// </summary>
        private readonly ConcurrentDictionary<string, FileContentManager> fileContentManagers;

        /// <summary>
        /// contains the CompilationUnit to which all files managed in this class instance belong to
        /// </summary>
        private readonly CompilationUnit compilationUnit;

        /// <summary>
        /// used to log exceptions raised during processing
        /// </summary>
        public readonly Action<Exception> LogException;

        /// <summary>
        /// called whenever diagnostics within a file have changed and are ready for publishing
        /// </summary>
        public readonly Action<PublishDiagnosticParams> PublishDiagnostics;

        /// <summary>
        /// WaitForTypeCheck is null if a global type checking has been queued but is not yet running.
        /// If WaitForTypeCheck is not null then a global type checking may be running and can be cancelled via WaitForTypeCheck.
        /// </summary>
        private CancellationTokenSource? waitForTypeCheck;

        /// <summary>
        /// used to track which files have changed during global type checking
        /// </summary>
        private readonly ManagedHashSet<string> changedFiles;

        /// <summary>
        /// used to synchronously execute all write access
        /// </summary>
        protected readonly ProcessingQueue Processing;

        /// <summary>
        /// Initializes a CompilationUnitManager instance for a project with the given properties.
        /// If an <see cref="Action"/> for publishing diagnostics is given and is not null,
        /// that action is called whenever diagnostics within a file have changed and are ready for publishing.
        /// </summary>
        public CompilationUnitManager(
            Action<Exception>? exceptionLogger = null,
            Action<PublishDiagnosticParams>? publishDiagnostics = null,
            bool syntaxCheckOnly = false,
            RuntimeCapability? capability = null,
            bool isExecutable = false,
            string processorArchitecture = "Unspecified")
        {
            this.EnableVerification = !syntaxCheckOnly;
            this.compilationUnit = new CompilationUnit(capability ?? RuntimeCapability.FullComputation, isExecutable, processorArchitecture);
            this.fileContentManagers = new ConcurrentDictionary<string, FileContentManager>();
            this.changedFiles = new ManagedHashSet<string>(new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion));
            this.PublishDiagnostics = publishDiagnostics ?? (_ => { });
            this.LogException = exceptionLogger ?? Console.Error.WriteLine;
            this.Processing = new ProcessingQueue(this.LogException);
            this.waitForTypeCheck = new CancellationTokenSource();
        }

        /// <summary>
        /// Cancels any asynchronously running, ongoing global type checking and sets WaitForTypeCheck to null,
        /// indicating that a global type checking is queued and not yet started.
        /// After all currently queued tasks have finished,
        /// locks all processing, flushes the unprocessed changes in each source file,
        /// synchronously runs a global type checking (unless verifications are disabled),
        /// and then executes the given function, returning its result.
        /// Returns null if the given function to execute is null, but does everything else.
        /// </summary>
        public T? FlushAndExecute<T>(Func<T?>? execute = null)
            where T : class
        {
            // To enforce an up-to-date content for executing the given function,
            // we want to do a (synchronous!) global type checking on flushing,
            // and hence cancel any ongoing type checking and set the WaitForTypeCheck handle to null.
            // However, we *also* need to make sure that any global type checking that is queued prior to the flushing task
            // (indicated by the WaitForTypeChecking handle currently being null) does not extend past it -
            // i.e. during the flushing task we again need to cancel any ongoing type checking.
            this.waitForTypeCheck?.Cancel();
            this.waitForTypeCheck = null;
            var succeeded = this.Processing.QueueForExecution(
                () =>
                {
                    this.waitForTypeCheck?.Cancel(); // needed in the case where WaitForTypeCheck above was null
                    foreach (var file in this.fileContentManagers.Values)
                    {
                        file.Flush();
                        this.PublishDiagnostics(file.Diagnostics());
                    }
                    var task = this.EnableVerification ? this.SpawnGlobalTypeCheckingAsync(runSynchronously: true) : Task.CompletedTask;
                    QsCompilerError.Verify(task.IsCompleted, "global type checking hasn't completed");
                    return execute?.Invoke();
                },
                out var result);
            return succeeded ? result : null;
        }

        /// <summary>
        /// Cancels any ongoing type checking, waits for all queued tasks to finish,
        /// and then disposes disposable content initialized within this CompilationUnitManager (in particular disposes the Compilation).
        /// Any FileContentManager managed by this compilation is *not* disposed,
        /// to allow potentially sharing file content managers between different compilation units.
        /// </summary>
        public void Dispose()
        {
            // we need to flush on disposing, since otherwise we may end up with some queued or running tasks
            // - or (global type checking) tasks spawned by those - trying to access disposed stuff
            this.FlushAndExecute<object>(() =>
            {
                this.waitForTypeCheck?.Dispose();
                this.compilationUnit.Dispose();
                foreach (var file in this.fileContentManagers.Values)
                {
                    // do *not* dispose of the FileContentManagers!
                    this.UnsubscribeFromFileManagerEvents(file);
                    this.PublishDiagnostics(new PublishDiagnosticParams { Uri = file.Uri, Diagnostics = Array.Empty<Diagnostic>() });
                }
                return null;
            });
        }

        // routines related to tracking the source files

        /// <summary>
        /// Converts a URI into the file ID used during compilation if the URI is an absolute file URI.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="uri"/> is not an absolute file URI.</exception>
        public static string GetFileId(Uri uri)
        {
            if (!uri.IsAbsoluteUri || !uri.IsFile)
            {
                throw new ArgumentException("The URI is not an absolute file URI.", nameof(uri));
            }
            return uri.LocalPath;
        }

        /// <summary>
        /// Converts a URI into the file ID used during compilation if the URI is an absolute file URI.
        /// </summary>
        /// <returns>True if converting the URI to a file ID succeeded.</returns>
        [Obsolete("Use GetFileId instead after ensuring that the URI is an absolute file URI.")]
        public static bool TryGetFileId(Uri? uri, [MaybeNullWhen(false)] out string fileId)
        {
            if (!(uri is null) && uri.IsFile && uri.IsAbsoluteUri)
            {
                fileId = uri.LocalPath;
                return true;
            }
            fileId = default;
            return false;
        }

        /// <summary>
        /// Given a file id assigned by the compilation unit manager, returns the URI based on which the ID was constructed as out parameter.
        /// Returns false if the given file id is not compatible with an id generated by the compilation unit manager.
        /// </summary>
        public static bool TryGetUri(string fileId, out Uri uri) =>
            Uri.TryCreate(Uri.UnescapeDataString(fileId), UriKind.Absolute, out uri);

        /// <summary>
        /// Subscribes to diagnostics updated events,
        /// to events indicating when queued changes in the file content manager have not yet been processed,
        /// and to events indicating that the compilation unit wide semantic verification needs to be updated.
        /// </summary>
        private void SubscribeToFileManagerEvents(FileContentManager file)
        {
            file.TimerTriggeredUpdateEvent += this.TriggerFileUpdateAsync;
            if (this.EnableVerification)
            {
                file.GlobalTypeCheckingEvent += this.QueueGlobalTypeCheckingAsync;
            }
        }

        /// <summary>
        /// Unsubscribes from all events that SubscribeToFileManagerEvents subscribes to for the given file.
        /// </summary>
        private void UnsubscribeFromFileManagerEvents(FileContentManager file)
        {
            file.TimerTriggeredUpdateEvent -= this.TriggerFileUpdateAsync;
            if (this.EnableVerification)
            {
                file.GlobalTypeCheckingEvent -= this.QueueGlobalTypeCheckingAsync;
            }
        }

        /// <summary>
        /// Initializes a FileContentManager for the given document with the given content.
        /// If an Action for publishing is given, publishes the diagnostics generated upon processing the given content.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="uri"/> is not an absolute file URI.</exception>
        public static FileContentManager InitializeFileManager(
            Uri uri,
            string fileContent,
            Action<PublishDiagnosticParams>? publishDiagnostics = null,
            Action<Exception>? onException = null)
        {
            var file = new FileContentManager(uri, GetFileId(uri));
            try
            {
                file.ReplaceFileContent(fileContent);
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
            }
            publishDiagnostics?.Invoke(file.Diagnostics());
            return file;
        }

        /// <summary>
        /// Initializes a FileContentManager for each entry in the given dictionary of source files and their content.
        /// If an Action for publishing is given, publishes the diagnostics generated upon content processing.
        /// </summary>
        /// <exception cref="ArgumentException">Any of the given URIs in <paramref name="files"/> is not an absolute file URI.</exception>
        public static ImmutableHashSet<FileContentManager> InitializeFileManagers(
            IDictionary<Uri, string> files,
            Action<PublishDiagnosticParams>? publishDiagnostics = null,
            Action<Exception>? onException = null)
        {
            if (files.Any(item => !item.Key.IsAbsoluteUri || !item.Key.IsFile))
            {
                throw new ArgumentException("invalid TextDocumentIdentifier");
            }
            return files.AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount > 1 ? Environment.ProcessorCount - 1 : Environment.ProcessorCount)
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism) // we are fine with a slower performance if the work is trivial
                .Select(entry => InitializeFileManager(entry.Key, entry.Value, publishDiagnostics, onException))
                .ToImmutableHashSet();
        }

        /// <summary>
        /// Adds the given source file to this compilation unit, adapting the diagnostics for all remaining files as needed.
        /// If a file with that Uri is already listed as source file,
        /// replaces the current FileContentManager for that file with the given one.
        /// If the content to update is specified and not null, replaces the tracked content in the file manager with the given one.
        /// </summary>
        public Task AddOrUpdateSourceFileAsync(FileContentManager file, string? updatedContent = null) =>
            this.Processing.QueueForExecutionAsync(() =>
            {
                this.compilationUnit.RegisterDependentLock(file.SyncRoot);
                this.SubscribeToFileManagerEvents(file);
                this.fileContentManagers.AddOrUpdate(file.FileName, file, (k, v) => file);
                if (updatedContent != null)
                {
                    file.ReplaceFileContent(updatedContent);
                }
                this.changedFiles.Add(file.FileName);
                if (this.EnableVerification && this.waitForTypeCheck != null)
                {
                    file.Verify(this.compilationUnit);
                }
                this.PublishDiagnostics(file.Diagnostics());
            });

        /// <summary>
        /// Adds the given source files to this compilation unit, adapting the diagnostics for all remaining files as needed.
        /// If a file with the same Uri is already listed as source file,
        /// replaces the current FileContentManager for that file with a new one and initialized its content to the given one.
        /// Spawns a compilation unit wide type checking unless suppressVerification is set to true, even if no files have been added.
        /// </summary>
        public Task AddOrUpdateSourceFilesAsync(ImmutableHashSet<FileContentManager> files, bool suppressVerification = false) =>
            this.Processing.QueueForExecutionAsync(() =>
            {
                foreach (var file in files)
                {
                    this.compilationUnit.RegisterDependentLock(file.SyncRoot);
                    this.SubscribeToFileManagerEvents(file);
                    this.fileContentManagers.AddOrUpdate(file.FileName, file, (k, v) => file);
                    this.changedFiles.Add(file.FileName);
                    this.PublishDiagnostics(file.Diagnostics());
                }
                if (this.EnableVerification && !suppressVerification)
                {
                    this.QueueGlobalTypeCheckingAsync();
                }
            });

        /// <summary>
        /// Modifies the compilation and all diagnostics to reflect the given change.
        /// </summary>
        /// <exception cref="ArgumentException">The URI of the given text document identifier is null or not an absolute file URI.</exception>
        /// <exception cref="InvalidOperationException">The file for which a change is given is not listed as a source file.</exception>
        public Task SourceFileDidChangeAsync(DidChangeTextDocumentParams param) =>
            this.Processing.QueueForExecutionAsync(() =>
            {
                var docKey = GetFileId(param.TextDocument.Uri);
                var isSource = this.fileContentManagers.TryGetValue(docKey, out FileContentManager file);
                if (!isSource)
                {
                    throw new InvalidOperationException($"changed file {docKey} is not a source file of this compilation unit");
                }

                this.changedFiles.Add(docKey);
                var publish = false;
                foreach (var change in param.ContentChanges)
                {
                    file.PushChange(change, out publish); // only the last one here is relevant
                }

                if (publish)
                {
                    if (this.EnableVerification && this.waitForTypeCheck != null)
                    {
                        file.AddTimerTriggeredUpdateEvent();
                    }
                    this.PublishDiagnostics(file.Diagnostics());
                }
                else
                {
                    file.AddTimerTriggeredUpdateEvent();
                }
            });

        /// <summary>
        /// Called in order to process any queued changes in the file with the given URI and
        /// - if verifications are enabled - trigger a semantic check.
        /// Does not do anything if no file with the given URI is listed as source file of this compilation.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="uri"/> is null or not an absolute file URI.</exception>
        private Task TriggerFileUpdateAsync(Uri uri)
        {
            var docKey = GetFileId(uri);
            return this.Processing.QueueForExecutionAsync(() =>
            {
                // Note that we cannot fail if the file is no longer part of the compilation,
                // because it is possible that between the time when a file has been removed and when that removal is executed
                // a file update has been triggered e.g. by timer...
                var isSource = this.fileContentManagers.TryGetValue(docKey, out FileContentManager file);
                if (!isSource)
                {
                    return;
                }

                // we need to mark a file as edited whenever some processing is spawned by the file, since
                // otherwise if a global type checking runs while changes are still queued in the file,
                // then the automatically spawned processing of those changes will be (partially) overwritten
                // when the global type checking results are pushed back in
                this.changedFiles.Add(docKey);
                file.Flush();
                if (this.EnableVerification && this.waitForTypeCheck != null)
                {
                    file.Verify(this.compilationUnit);
                }
                this.PublishDiagnostics(file.Diagnostics());
            });
        }

        /// <summary>
        /// Removes the specified file from the list of source files for this compilation unit,
        /// publishes empty Diagnostics for that file unless publishEmptyDiagnostics is set to false,
        /// and adapts all remaining diagnostics as needed.
        /// Does nothing if no file with the given Uri is listed as source file.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="uri"/> is null or not an absolute file URI.</exception>
        public Task TryRemoveSourceFileAsync(Uri uri, bool publishEmptyDiagnostics = true)
        {
            var docKey = GetFileId(uri);
            return this.Processing.QueueForExecutionAsync(() =>
            {
                if (!this.fileContentManagers.TryRemove(docKey, out FileContentManager file))
                {
                    return;
                }
                this.changedFiles.Add(docKey);
                this.compilationUnit.UnregisterDependentLock(file.SyncRoot); // do *not* dispose of the FileContentManager!
                this.UnsubscribeFromFileManagerEvents(file);                 // ... but unsubscribe from all file events
                this.compilationUnit.GlobalSymbols.RemoveSource(docKey);
                if (publishEmptyDiagnostics)
                {
                    this.PublishDiagnostics(new PublishDiagnosticParams { Uri = uri, Diagnostics = Array.Empty<Diagnostic>() });
                }
                if (this.EnableVerification)
                {
                    this.QueueGlobalTypeCheckingAsync(); // we need to trigger a global type checking for the remaining files...
                }
            });
        }

        /// <summary>
        /// Removes the specified files from the list of source files for this compilation unit,
        /// publishes empty Diagnostics for the removed files unless publishEmptyDiagnostics is set to false,
        /// and adapts all remaining diagnostics as needed.
        /// Does nothing if a file with the given Uri is not listed as source file.
        /// Spawns a compilation unit wide type checking unless suppressVerification is set to true, even if no files have been removed.
        /// </summary>
        /// <exception cref="ArgumentException">A URI in <paramref name="files"/> is not an absolute file URI.</exception>
        public Task TryRemoveSourceFilesAsync(IEnumerable<Uri> files, bool suppressVerification = false, bool publishEmptyDiagnostics = true)
        {
            if (files.Any(uri => !uri.IsAbsoluteUri || !uri.IsFile))
            {
                throw new ArgumentException("invalid TextDocumentIdentifier");
            }

            return this.Processing.QueueForExecutionAsync(() =>
            {
                foreach (var uri in files)
                {
                    var docKey = GetFileId(uri);
                    if (!this.fileContentManagers.TryRemove(docKey, out FileContentManager file))
                    {
                        return;
                    }
                    this.changedFiles.Add(docKey);
                    this.compilationUnit.UnregisterDependentLock(file.SyncRoot); // do *not* dispose of the FileContentManager!
                    this.UnsubscribeFromFileManagerEvents(file);                 // ... but unsubscribe from all file events
                    this.compilationUnit.GlobalSymbols.RemoveSource(docKey);
                    if (publishEmptyDiagnostics)
                    {
                        this.PublishDiagnostics(new PublishDiagnosticParams { Uri = uri, Diagnostics = Array.Empty<Diagnostic>() });
                    }
                    if (this.EnableVerification)
                    {
                        this.QueueGlobalTypeCheckingAsync(); // we need to trigger a global type checking for the remaining files...
                    }
                }
                if (this.EnableVerification && !suppressVerification)
                {
                    this.QueueGlobalTypeCheckingAsync();
                }
            });
        }

        /// <summary>
        /// Replaces the content from all referenced assemblies with the given references.
        /// Updates all diagnostics accordingly, unless suppressVerification has been set to true.
        /// </summary>
        internal Task UpdateReferencesAsync(References references, bool suppressVerification = false)
        {
            return this.Processing.QueueForExecutionAsync(() =>
            {
                this.compilationUnit.UpdateReferences(references);
                if (this.EnableVerification && !suppressVerification)
                {
                    this.QueueGlobalTypeCheckingAsync();
                }
            });
        }

        /// <summary>
        /// Replaces the content from all referenced assemblies with the given references, and updates all diagnostics accordingly.
        /// </summary>
        public Task UpdateReferencesAsync(References references) =>
            this.UpdateReferencesAsync(references, false);

        // routines related to global type checking (all calls to these need to be suppressed if verifications are disabled)

        /// <summary>
        /// If a global type checking is already queued, but hasn't started executing yet, does nothing and returns a completed task.
        /// If a global type checking is in progress, cancels that process via WaitForTypeChecking,
        /// then queues SpawnGlobalTypeCheckingAsync for exectution into the task queue, and sets WaitForTypeChecking to null,
        /// indicating that a type global type checking is queued and has not started executing yet.
        /// </summary>
        private Task QueueGlobalTypeCheckingAsync()
        {
            if (this.waitForTypeCheck == null)
            {
                return Task.CompletedTask; // type check is already queued
            }
            this.waitForTypeCheck.Cancel(); // cancel any ongoing type check...
            this.waitForTypeCheck = null;   // ... and queue a new type check
            return this.Processing.QueueForExecutionAsync(() =>
                QsCompilerError.RaiseOnFailure(
                    () => this.SpawnGlobalTypeCheckingAsync(), "error while spawning global type checking"));
        }

        /// <summary>
        /// Updates all global symbols for all files in the compilation using a separate CompilationUnit instance.
        /// Copies the symbol information over to the Compilation property,
        /// updates the HeaderDiagnostics and clears the SemanticDiagnostics in all files, and publishes the updated diagnostics.
        /// Sets WaitForTypeCheck to a new CancellationTokenSource,
        /// and then spawns a task calling RunGlobalTypeChecking on the computed content using the separate CompilationUnit,
        /// with a cancellation token from the new cancellation source.
        /// If runSynchronously is set to true, then the task is run synchronously ignoring any cancellations and the completed task is returned.
        /// </summary>
        private Task SpawnGlobalTypeCheckingAsync(bool runSynchronously = false)
        {
            this.waitForTypeCheck = new CancellationTokenSource(); // set a handle for cancelling this type check
            var cancellationToken = runSynchronously ? CancellationToken.None : this.waitForTypeCheck.Token;

            // work with a separate compilation unit instance such that processing of all further edits can go on in parallel
            var sourceFiles = this.fileContentManagers.Values.OrderBy(m => m.FileName);
            this.changedFiles.RemoveAll(f => sourceFiles.Any(m => m.FileName == f));
            var compilation = new CompilationUnit(
                this.compilationUnit.RuntimeCapability,
                this.compilationUnit.IsExecutable,
                this.compilationUnit.ProcessorArchitecture,
                this.compilationUnit.Externals,
                sourceFiles.Select(file => file.SyncRoot));
            var content = compilation.UpdateGlobalSymbolsFor(sourceFiles);
            foreach (var file in sourceFiles)
            {
                this.PublishDiagnostics(file.Diagnostics());
            }

            // move the content of symbols over to the Compilation
            this.compilationUnit.EnterWriteLock();
            try
            {
                this.compilationUnit.GlobalSymbols.Clear();
                compilation.GlobalSymbols.CopyTo(this.compilationUnit.GlobalSymbols);
                this.compilationUnit.GlobalSymbols.ResolveAll(BuiltIn.NamespacesToAutoOpen);
            }
            finally
            {
                this.compilationUnit.ExitWriteLock();
            }

            // after the relevant information is extracted, the actual type checking can run in parallel
            var task = new Task(
                () =>
                {
                    try
                    {
                        // Do *not* remove the RaiseOnFailure!
                        // -> keep it here, such that all internal exceptions are being piped through one central routine (in QsCompilerError)
                        QsCompilerError.RaiseOnFailure(
                            () => this.RunGlobalTypeChecking(compilation, content, cancellationToken),
                            "error while running global type checking");
                    }
                    catch (Exception ex)
                    {
                        this.LogException(ex);
                    }
                },
                cancellationToken);

            if (runSynchronously)
            {
                task.RunSynchronously();
            }
            else if (!cancellationToken.IsCancellationRequested)
            {
                task.Start();
            }
            return task;
        }

        /// <summary>
        /// Runs a type checking on the given content using the given CompilationUnit instance, with the given cancellation token.
        /// Once the type checking is done, locks all processing, and updates the Compilation property with the built content
        /// for all files that have not been modified while the type checking was running. Updates and publishes the semantic diagnostics for those files.
        /// For each file that has been modified while the type checking was running,
        /// queues a task calling TypeCheckFile on that file with the given cancellationToken.
        /// </summary>
        private void RunGlobalTypeChecking(CompilationUnit compilation, ImmutableDictionary<QsQualifiedName, (QsComments, FragmentTree)> content, CancellationToken cancellationToken)
        {
            var diagnostics = QsCompilerError.RaiseOnFailure(
                () => TypeChecking.RunTypeChecking(compilation, content, cancellationToken),
                "error while running type checking in background");

            this.Processing.QueueForExecution(() => // -> could be fast-tracked to be executed immediately, but needs exclusive access (write thread)
            {
                foreach (var file in this.fileContentManagers.Values)
                {
                    file.SyncRoot.EnterUpgradeableReadLock();
                }
                this.changedFiles.SyncRoot.EnterReadLock(); // need to lock such that the content of the list remains the same during the execution of the block below
                try
                {
                    var changedFiles = this.changedFiles.ToImmutableHashSet();
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var validCallables = compilation.GetCallables().Values.Where(c => !changedFiles.Contains(c.Source.AssemblyOrCodeFile));
                    var validTypes = compilation.GetTypes().Values.Where(t => !changedFiles.Contains(t.Source.AssemblyOrCodeFile));
                    this.compilationUnit.UpdateCallables(validCallables);
                    this.compilationUnit.UpdateTypes(validTypes);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    var allDiagnostics = diagnostics.ToLookup(msg => msg.Source);
                    foreach (var file in this.fileContentManagers.Values)
                    {
                        if (changedFiles.Contains(file.FileName))
                        {
                            continue;
                        }
                        file.ReplaceSemanticDiagnostics(allDiagnostics[file.FileName]);
                        this.PublishDiagnostics(file.Diagnostics());
                    }

                    foreach (var docKey in changedFiles)
                    {
                        this.Processing.QueueForExecutionAsync(() =>
                            QsCompilerError.RaiseOnFailure(
                                () => this.TypeCheckFile(docKey, cancellationToken),
                                "error while re-doing the type checking for an source file that had been modified during a global type check update"));
                    }
                }
                finally
                {
                    this.changedFiles.SyncRoot.ExitReadLock();
                    foreach (var file in this.fileContentManagers.Values)
                    {
                        file.SyncRoot.ExitUpgradeableReadLock();
                    }
                }
            });
        }

        /// <summary>
        /// If the given cancellation token is not cancellation requested, and the file with the document id is listed as source file,
        /// updates and resolves all global symbols for that file, and runs a type checking on the obtained content using the Compilation property.
        /// Replaces the header diagnostics and the semantic diagnostics in the file with the obtained diagnostics, and publishes them.
        /// </summary>
        private void TypeCheckFile(string fileId, CancellationToken cancellationToken)
        {
            var isSource = this.fileContentManagers.TryGetValue(fileId, out FileContentManager file);
            if (!isSource || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            this.compilationUnit.EnterUpgradeableReadLock();
            file.SyncRoot.EnterUpgradeableReadLock();
            try
            {
                // NOTE: invalidating the right diagnostics is kind of error prone,
                // so instead of calling file.UpdateTypeChecking the type checking for the entire file is simply recomputed.

                var diagnostics = new List<Diagnostic>();
                var contentToCompile = file.UpdateGlobalSymbols(this.compilationUnit, diagnostics);
                file.ImportGlobalSymbols(this.compilationUnit, diagnostics);
                TypeChecking.ResolveGlobalSymbols(this.compilationUnit.GlobalSymbols, diagnostics, file.FileName);
                file.ReplaceHeaderDiagnostics(diagnostics);

                diagnostics = TypeChecking.RunTypeChecking(this.compilationUnit, file.GetDeclarationTrees(contentToCompile), CancellationToken.None);
                diagnostics?.Apply(file.ReplaceSemanticDiagnostics);
                this.PublishDiagnostics(file.Diagnostics());
            }
            finally
            {
                file.SyncRoot.ExitUpgradeableReadLock();
                this.compilationUnit.ExitUpgradeableReadLock();
            }
        }

        // editor commands that need to be blocking

        /// <summary>
        /// Returns the workspace edit that describes the changes to be done if the symbol at the given position - if any - is renamed to the given name.
        /// Returns null if no symbol exists at the specified position,
        /// or if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the file.
        /// </summary>
        public WorkspaceEdit? Rename(RenameParams? param)
        {
            if (param?.TextDocument?.Uri is null
                || !param.TextDocument.Uri.IsAbsoluteUri
                || !param.TextDocument.Uri.IsFile)
            {
                return null;
            }

            // FIXME: the correct thing to do here would be to call FlushAndExecute...!
            var docKey = GetFileId(param.TextDocument.Uri);
            var success = this.Processing.QueueForExecution(
                () => this.fileContentManagers.TryGetValue(docKey, out FileContentManager file)
                    ? file.Rename(this.compilationUnit, param.Position.ToQSharp(), param.NewName)
                    : null,
                out var edit);
            return success ? edit : null;
        }

        // routines related to providing information for non-blocking editor commands
        // -> these commands need to be responsive and therefore won't wait for any processing to finish
        // -> if the query cannot be processed immediately, they simply return null

        /// <param name="suppressExceptionLogging">
        /// Whether to suppress logging of exceptions from the query.
        /// <para/>
        /// NOTE: In debug mode, exceptions are always logged even if this parameter is true.
        /// </param>
        internal T? FileQuery<T>(
            TextDocumentIdentifier? textDocument,
            Func<FileContentManager, CompilationUnit, T?> query,
            bool suppressExceptionLogging = false)
            where T : class
        {
            T? TryQueryFile(FileContentManager f)
            {
                try
                {
                    return query(f, this.compilationUnit);
                }
                catch (Exception ex)
                {
#if DEBUG
                    this.LogException(ex);
#else
                    if (!suppressExceptionLogging)
                    {
                        this.LogException(ex);
                    }
#endif
                    return null;
                }
            }
            if (textDocument?.Uri is null || !textDocument.Uri.IsAbsoluteUri || !textDocument.Uri.IsFile)
            {
                return null;
            }
            var docKey = GetFileId(textDocument.Uri);
            var isSource = this.fileContentManagers.TryGetValue(docKey, out FileContentManager file);
            return isSource ? TryQueryFile(file) : null;
        }

        // routines giving read access to the compilation state (e.g. for the command line compiler, or testing/debugging)
        // -> these routines will wait for any processing to finish before executing the query

        /// <summary>
        /// If a TextDocumentIdentifier is given, returns an array with a single item containing all current diagnostics for the given file.
        /// Returns null if the given file is not listed as source file.
        /// If no TextDocumentIdentifier is given, then the diagnostics for all source files are returned.
        /// Note: this method waits for all currently running or queued tasks to finish
        /// before accumulating the diagnostics by calling FlushAndExecute.
        /// </summary>
        public PublishDiagnosticParams[]? GetDiagnostics(TextDocumentIdentifier? textDocument = null) =>
            this.FlushAndExecute(() =>
                textDocument != null
                    ? this.FileQuery(textDocument, (file, _) => new PublishDiagnosticParams[] { file.Diagnostics() })
                    : this.fileContentManagers.Values.Select(file => file.Diagnostics()).ToArray());

        /// <summary>
        /// Returns a sequence of all source files that are currently contained in this compilation unit.
        /// Note: this method waits for all currently running or queued tasks to finish
        /// before constructing the requested information by calling FlushAndExecute.
        /// </summary>
        public IEnumerable<Uri>? GetSourceFiles() =>
            this.FlushAndExecute(() => this.fileContentManagers.Keys.Select(id => new Uri(id)).ToImmutableArray().AsEnumerable());

        /// <summary>
        /// Returns the current file content (text representation) in memory.
        /// Returns null if the given file is not listed as source file.
        /// Note: this method waits for all currently running or queued tasks to finish
        /// before getting the file content by calling FlushAndExecute.
        /// </summary>
        public string[]? FileContentInMemory(TextDocumentIdentifier textDocument) =>
            this.FlushAndExecute(() =>
                this.FileQuery(textDocument, (file, _) => file.GetLines(0, file.NrLines()).Select(line => line.Text).ToArray()));

        /// <summary>
        /// Returns the current tokenization of the file content in memory.
        /// Each returned array item contains the array of tokens on the line with the corresponding index.
        /// Returns null if the given file is not listed as source file.
        /// Note: this method waits for all currently running or queued tasks to finish
        /// before getting the file content by calling FlushAndExecute.
        /// </summary>
        public QsFragmentKind?[][]? GetTokenization(TextDocumentIdentifier textDocument) =>
            this.FlushAndExecute(() =>
                this.FileQuery(textDocument, (file, _) => file.GetTokenizedLines(0, file.NrLines()).Select(line => line.Select(frag => frag.Kind).ToArray()).ToArray()));

        /// <summary>
        /// Returns the syntax tree for the current state of the compilation.
        /// Note: this method waits for all currently running or queued tasks to finish
        /// before constructing the syntax tree by calling FlushAndExecute.
        /// </summary>
        public IEnumerable<QsNamespace>? GetSyntaxTree() =>
            this.FlushAndExecute(() => this.compilationUnit.Build().Namespaces.AsEnumerable());

        /// <summary>
        /// Returns a Compilation object containing all information about the current state of the compilation.
        /// Note: this method waits for all currently running or queued tasks to finish
        /// before constructing the Compilation object by calling FlushAndExecute.
        /// </summary>
        public Compilation? Build() => this.FlushAndExecute(() =>
        {
            try
            {
                return new Compilation(this);
            }
            catch (Exception ex)
            {
                this.LogException(ex);
                return null;
            }
        });

        /// <summary>
        /// Class used to accumulate all information about the state of a compilation unit in immutable form.
        /// </summary>
        public class Compilation
        {
            /// <summary>
            /// Contains the file IDs assigned by the Q# compiler for all source files included in the compilation.
            /// </summary>
            public readonly ImmutableHashSet<string> SourceFiles;

            /// <summary>
            /// Contains the IDs assigned by the Q# compiler for all assemblies referenced in the compilation.
            /// </summary>
            public readonly ImmutableHashSet<string> References;

            /// <summary>
            /// Contains a dictionary that maps the ID of a file included in the compilation
            /// to the text representation of its content.
            /// </summary>
            public readonly ImmutableDictionary<string, ImmutableArray<string>> FileContent;

            /// <summary>
            /// Contains a dictionary that maps the ID of a file included in the compilation
            /// to the tokenization built based on its content.
            /// </summary>
            public readonly ImmutableDictionary<string, ImmutableArray<ImmutableArray<CodeFragment>>> Tokenization;

            /// <summary>
            /// Contains a dictionary that maps the name of a namespace to the compiled Q# namespace.
            /// </summary>
            public readonly ImmutableDictionary<string, QsNamespace> SyntaxTree;

            /// <summary>
            /// Contains the built Q# compilation.
            /// </summary>
            public readonly QsCompilation BuiltCompilation;

            /// <summary>
            /// Contains a dictionary that maps the name of each namespace defined in the compilation to a look-up
            /// containing the names and corresponding short form (if any) of all opened namespaces for that (part of the) namespace in a particular source file.
            /// </summary>
            private readonly ImmutableDictionary<string, ILookup<string, (string, string?)>> openDirectivesForEachFile;

            /// <summary>
            /// Contains a dictionary that given the ID of a file included in the compilation
            /// returns all tokenized code fragments containing namespace declarations in that file.
            /// </summary>
            private readonly ImmutableDictionary<string, ImmutableArray<CodeFragment>> namespaceDeclarations;

            /// <summary>
            /// Contains a dictionary that given the fully qualified name of a compiled callable returns its syntax tree.
            /// </summary>
            public readonly ImmutableDictionary<QsQualifiedName, QsCallable> Callables;

            /// <summary>
            /// Contains a dictionary that given the fully qualified name of a compiled type returns its syntax tree.
            /// </summary>
            public readonly ImmutableDictionary<QsQualifiedName, QsCustomType> Types;

            /// <summary>
            /// Contains a dictionary that maps the ID of a file included in the compilation
            /// to all scope-related diagnostics generated during compilation.
            /// </summary>
            public readonly ImmutableDictionary<string, ImmutableArray<Diagnostic>> ScopeDiagnostics;

            /// <summary>
            /// Contains a dictionary that maps the ID of a file included in the compilation
            /// to all syntax-related diagnostics generated during compilation.
            /// </summary>
            public readonly ImmutableDictionary<string, ImmutableArray<Diagnostic>> SyntaxDiagnostics;

            /// <summary>
            /// Contains a dictionary that maps the ID of a file included in the compilation
            /// to all context-related diagnostics generated during compilation.
            /// </summary>
            public readonly ImmutableDictionary<string, ImmutableArray<Diagnostic>> ContextDiagnostics;

            /// <summary>
            /// Contains a dictionary that maps the ID of a file included in the compilation
            /// to all diagnostics generated during compilation related to header information for declarations.
            /// </summary>
            public readonly ImmutableDictionary<string, ImmutableArray<Diagnostic>> HeaderDiagnostics;

            /// <summary>
            /// Contains a dictionary that maps the ID of a file included in the compilation
            /// to all semantic diagnostics generated during compilation for the specified implementations.
            /// </summary>
            public readonly ImmutableDictionary<string, ImmutableArray<Diagnostic>> SemanticDiagnostics;

            /// <summary>
            /// Maps a file ID assigned by the Q# compiler to all diagnostics generated during compilation.
            /// Returns an empty sequence if no file with the given ID has been included in the compilation.
            /// </summary>
            public IEnumerable<Diagnostic> Diagnostics(string file) =>
                this.SourceFiles.Contains(file) ?
                    this.ScopeDiagnostics[file]
                        .Concat(this.SyntaxDiagnostics[file])
                        .Concat(this.ContextDiagnostics[file])
                        .Concat(this.HeaderDiagnostics[file])
                        .Concat(this.SemanticDiagnostics[file]) :
                    Enumerable.Empty<Diagnostic>();

            /// <summary>
            /// Returns all diagnostics generated during compilation.
            /// </summary>
            public IEnumerable<Diagnostic> Diagnostics() =>
                this.ScopeDiagnostics.Values
                    .Concat(this.SyntaxDiagnostics.Values)
                    .Concat(this.ContextDiagnostics.Values)
                    .Concat(this.HeaderDiagnostics.Values)
                    .Concat(this.SemanticDiagnostics.Values)
                    .SelectMany(d => d);

            /// <summary>
            /// If a source file with the given name exists in the compilation, and if there is exactly one (partial) namespace with the given name,
            /// returns the (non-documenting) comments associated with that namespace declaration.
            /// Returns a set of empty comments otherwise.
            /// </summary>
            public QsComments NamespaceComments(string sourceFile, string nsName)
            {
                if (!this.namespaceDeclarations.TryGetValue(sourceFile, out ImmutableArray<CodeFragment> namespaces))
                {
                    return QsComments.Empty;
                }
                var declarations = namespaces.Where(token => token.Kind?.DeclaredNamespaceName(null) == nsName);
                return declarations.Count() == 1 ? declarations.Single().Comments : QsComments.Empty;
            }

            /// <summary>
            /// Given ID of a source file and the name of a namespace,
            /// returns the names and corresponding short form (if any) of all opened namespaces for the (part of the) namespace in that file.
            /// Returns an empy sequence if no source file with the given ID and/or namespace with the given name exists in the compilation.
            /// </summary>
            public IEnumerable<(string, string?)> OpenDirectives(string sourceFile, string nsName) =>
                this.openDirectivesForEachFile.TryGetValue(nsName, out var lookUp)
                    ? lookUp[sourceFile]
                    : Enumerable.Empty<(string, string?)>();

            /// <summary>
            /// Returns all the names of all callable and types defined in the namespace with the given name.
            /// The returned names are unique and do not contain duplications e.g. for types and the corresponding constructor.
            /// Returns an empty sequence if no namespace with the given name exists in the compilation.
            /// </summary>
            public IEnumerable<string> SymbolsDefinedInNamespace(string nsName) =>
                this.SyntaxTree.TryGetValue(nsName, out var ns)
                    ? ns.Elements.SelectNotNull(element => (element as QsNamespaceElement.QsCallable)?.Item.FullName.Name)
                    : Enumerable.Empty<string>();

            internal Compilation(CompilationUnitManager manager)
            {
                this.BuiltCompilation = manager.compilationUnit.Build();
                this.SourceFiles = manager.fileContentManagers.Keys.ToImmutableHashSet();
                this.References = manager.compilationUnit.Externals.Declarations.Keys.ToImmutableHashSet();

                this.FileContent = this.SourceFiles
                    .Select(file => (file, manager.fileContentManagers[file].GetLines().Select(line => line.Text).ToImmutableArray()))
                    .ToImmutableDictionary(tuple => tuple.file, tuple => tuple.Item2);
                this.Tokenization = this.SourceFiles
                    .Select(file => (file, manager.fileContentManagers[file].GetTokenizedLines().Select(line => line.Select(frag => frag.Copy()).ToImmutableArray()).ToImmutableArray()))
                    .ToImmutableDictionary(tuple => tuple.file, tuple => tuple.Item2);
                this.SyntaxTree = this.BuiltCompilation.Namespaces.ToImmutableDictionary(ns => ns.Name);

                this.openDirectivesForEachFile = this.SyntaxTree.Keys.ToImmutableDictionary(
                    nsName => nsName,
                    nsName => manager.compilationUnit.GetOpenDirectives(nsName));
                this.namespaceDeclarations = this.SourceFiles
                    .Select(file => (file, manager.fileContentManagers[file].NamespaceDeclarationTokens().Select(t => t.GetFragmentWithClosingComments()).ToImmutableArray()))
                    .ToImmutableDictionary(tuple => tuple.file, tuple => tuple.Item2);
                this.Callables = this.SyntaxTree.Values.GlobalCallableResolutions();
                this.Types = this.SyntaxTree.Values.GlobalTypeResolutions();

                this.ScopeDiagnostics = this.SourceFiles
                    .Select(file => (file, manager.fileContentManagers[file].CurrentScopeDiagnostics()))
                    .ToImmutableDictionary(tuple => tuple.file, tuple => tuple.Item2);
                this.SyntaxDiagnostics = this.SourceFiles
                    .Select(file => (file, manager.fileContentManagers[file].CurrentSyntaxDiagnostics()))
                    .ToImmutableDictionary(tuple => tuple.file, tuple => tuple.Item2);
                this.ContextDiagnostics = this.SourceFiles
                    .Select(file => (file, manager.fileContentManagers[file].CurrentContextDiagnostics()))
                    .ToImmutableDictionary(tuple => tuple.file, tuple => tuple.Item2);
                this.HeaderDiagnostics = this.SourceFiles
                    .Select(file => (file, manager.fileContentManagers[file].CurrentHeaderDiagnostics()))
                    .ToImmutableDictionary(tuple => tuple.file, tuple => tuple.Item2);
                this.SemanticDiagnostics = this.SourceFiles
                    .Select(file => (file, manager.fileContentManagers[file].CurrentSemanticDiagnostics()))
                    .ToImmutableDictionary(tuple => tuple.file, tuple => tuple.Item2);
            }
        }
    }
}
