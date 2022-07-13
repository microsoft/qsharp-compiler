// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    /// <summary>
    /// Compilation unit manager.
    /// </summary>
    /// <remarks>
    /// This class intentionally does not give access to any <see cref="FileContentManager"/> that it manages,
    /// since it is responsible for coordinating access to (any routine of) the <see cref="FileContentManager"/>.
    /// </remarks>
    public class CompilationUnitManager : IDisposable
    {
        internal bool EnableVerification { get; private set; }

        internal ProjectProperties BuildProperties => this.compilationUnit.BuildProperties;

        /// <summary>
        /// The keys are the file identifiers of the source files obtained by <see cref="GetFileId"/>
        /// for the file uri and the values are the content of each file.
        /// </summary>
        private readonly ConcurrentDictionary<string, FileContentManager> fileContentManagers;

        /// <summary>
        /// Contains the <see cref="CompilationUnit"/> to which all files managed in this class instance belong to.
        /// </summary>
        private readonly CompilationUnit compilationUnit;

        /// <summary>
        /// Used to log exceptions raised during processing.
        /// </summary>
        public Action<Exception> LogException { get; }

        /// <summary>
        /// Called whenever diagnostics within a file have changed and are ready for publishing.
        /// </summary>
        public Action<PublishDiagnosticParams> PublishDiagnostics { get; }

        /// <summary>
        /// General purpose logging routine.
        /// </summary>
        public Action<string, MessageType> Log { get; }

        /// <summary>
        /// Null if a global type checking has been queued but is not yet running.
        /// If not null, then a global type checking may be running and can be cancelled via this token source.
        /// </summary>
        private CancellationTokenSource? waitForTypeCheck;

        /// <summary>
        /// Used to track which files have changed during global type checking.
        /// </summary>
        private readonly ManagedHashSet<string> changedFiles;

        /// <summary>
        /// Used to synchronously execute all write access.
        /// </summary>
        protected ProcessingQueue Processing { get; }

        /// <summary>
        /// Initializes a <see cref="CompilationUnitManager"/> instance for a project with the given properties.
        /// </summary>
        /// <param name="publishDiagnostics">
        /// If provided, called whenever diagnostics within a file have changed and are ready for publishing.
        /// </param>
        public CompilationUnitManager(
            ProjectProperties buildProperties,
            Action<Exception>? exceptionLogger = null,
            Action<string, MessageType>? log = null,
            Action<PublishDiagnosticParams>? publishDiagnostics = null,
            bool syntaxCheckOnly = false)
        {
            this.EnableVerification = !syntaxCheckOnly;
            this.compilationUnit = new CompilationUnit(buildProperties);
            this.fileContentManagers = new ConcurrentDictionary<string, FileContentManager>();
            this.changedFiles = new ManagedHashSet<string>(new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion));
            this.PublishDiagnostics = publishDiagnostics ?? (_ => { });
            this.Log = log ?? ((_, __) => { });
            this.LogException = exceptionLogger ?? Console.Error.WriteLine;
            this.Processing = new ProcessingQueue(this.LogException);
            this.waitForTypeCheck = new CancellationTokenSource();
        }

        /// <summary>
        /// Cancels any asynchronously running ongoing global type checking. After all currently queued tasks
        /// have finished, locks all processing, flushes the unprocessed changes in each source file,
        /// synchronously runs a global type checking (unless verifications are disabled), and then executes
        /// <paramref name="execute"/>, returning its result.
        /// </summary>
        /// <returns>
        /// The result of <paramref name="execute"/>, or null if <paramref name="execute"/> is null.
        /// </returns>
        /// <remarks>
        /// Sets <see cref="waitForTypeCheck"/> to null, indicating that a global type checking is queued and not yet started.
        /// </remarks>
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

                    if (this.EnableVerification)
                    {
                        var task = this.SpawnGlobalTypeCheckingAsync(runSynchronously: true);
                        QsCompilerError.Verify(task.IsCompleted, "Global type checking hasn't completed.");
                    }

                    return execute?.Invoke();
                },
                out var result);

            return succeeded ? result : null;
        }

        /// <summary>
        /// Cancels any ongoing type checking, waits for all queued tasks to finish,
        /// and then disposes disposable content initialized within this <see cref="CompilationUnitManager"/>
        /// (in particular disposes the <see cref="Compilation"/>).
        /// </summary>
        /// <remarks>
        /// Any <see cref="FileContentManager"/> managed by this compilation is *not* disposed,
        /// to allow potentially sharing file content managers between different compilation units.
        /// </remarks>
        public void Dispose()
        {
            // We need to flush on disposing, since otherwise we may end up with some queued or running tasks (or global
            // type checking tasks spawned by those) trying to access disposed stuff. Disable verification to prevent
            // FlushAndExecute from spawning a new type-checking task.
            this.EnableVerification = false;
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
        /// Returns the URI based on which <paramref name="fileId"/> was constructed via <paramref name="uri"/>.
        /// </summary>
        /// <param name="fileId">A file ID assigned by the compilation unit manager.</param>
        /// <returns>
        /// False if <paramref name="fileId"/> is not compatible with an id generated by the compilation unit manager.
        /// </returns>
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
        /// Unsubscribes from all events that <see cref="SubscribeToFileManagerEvents"/> subscribes to for <paramref name="file"/>.
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
        /// Initializes a <see cref="FileContentManager"/> for document <paramref name="uri"/> with <paramref name="fileContent"/>.
        /// </summary>
        /// <param name="publishDiagnostics">
        /// If provided, called to publish the diagnostics generated upon processing <paramref name="fileContent"/>.
        /// </param>
        /// <exception cref="ArgumentException"><paramref name="uri"/> is not an absolute file URI.</exception>
        public static FileContentManager InitializeFileManager(
            Uri uri,
            string fileContent,
            Action<PublishDiagnosticParams>? publishDiagnostics = null,
            Action<Exception>? onException = null,
            bool isNotebook = false)
        {
            var file = new FileContentManager(uri, GetFileId(uri), isNotebook);
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
        /// Initializes a <see cref="FileContentManager"/> for each entry in <paramref name="files"/> and their contents.
        /// </summary>
        /// <param name="publishDiagnostics">
        /// If provided, called to publish the diagnostics generated upon content processing.
        /// </param>
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
        /// Adds <paramref name="file"/> to this compilation unit, adapting the diagnostics for all remaining files as needed.
        /// </summary>
        /// <param name="updatedContent">If provided, replaces the tracked content in the file manager.</param>
        /// <remarks>
        /// If a file with the same URI is already listed as a source file,
        /// replaces the current <see cref="FileContentManager"/> for that file with <paramref name="file"/>.
        /// </remarks>
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
        /// Adds <paramref name="files"/> to this compilation unit, adapting the diagnostics for all remaining files as needed.
        /// </summary>
        /// <remarks>
        /// If a file with the same URI is already listed as a source file,
        /// replaces the current <see cref="FileContentManager"/> for that file with the new one and initializes
        /// its content to the given one.
        /// <para/>
        /// Spawns a compilation unit wide type checking unless <paramref name="suppressVerification"/> is set to true,
        /// even if no files have been added.
        /// </remarks>
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

                // We should keep the file version in sync with the reported by the LSP.
                file.Version = param.TextDocument.Version;

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
        /// Called in order to process any queued changes in the file identified by <paramref name="uri"/> and
        /// - if verifications are enabled - trigger a semantic check.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="uri"/> is null or not an absolute file URI.</exception>
        /// <remarks>
        /// Does not do anything if <paramref name="uri"/> does not identify a source file of this compilation.
        /// </remarks>
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
        /// Removes the file identified by <paramref name="uri"/> from the list of source files for this compilation unit,
        /// publishes empty Diagnostics for that file unless <paramref name="publishEmptyDiagnostics"/> is set to false,
        /// and adapts all remaining diagnostics as needed.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="uri"/> is null or not an absolute file URI.</exception>
        /// <remarks>
        /// Does nothing if <paramref name="uri"/> does not identify a source file of this compilation unit.
        /// </remarks>
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
        /// Removes <paramref name="files"/> from the list of source files for this compilation unit,
        /// publishes empty Diagnostics for the removed files unless <paramref name="publishEmptyDiagnostics"/> is set to false,
        /// and adapts all remaining diagnostics as needed.
        /// </summary>
        /// <exception cref="ArgumentException">A URI in <paramref name="files"/> is not an absolute file URI.</exception>
        /// <remarks>
        /// <!-- TODO: Check this comment. Won't this remove up to the not-listed file? -->
        /// Does nothing if a file with the given Uri is not listed as source file.
        /// <para/>
        /// Spawns a compilation unit wide type checking unless <paramref name="suppressVerification"/> is set to true,
        /// even if no files have been removed.
        /// </remarks>
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
        /// Replaces the content from all referenced assemblies with <paramref name="references"/>.
        /// </summary>
        /// <remarks>
        /// Updates all diagnostics accordingly, unless <paramref name="suppressVerification"/> has been set to true.
        /// </remarks>
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
        /// Replaces the content from all referenced assemblies with <paramref name="references"/>,
        /// and updates all diagnostics accordingly.
        /// </summary>
        public Task UpdateReferencesAsync(References references) =>
            this.UpdateReferencesAsync(references, false);

        // routines related to global type checking (all calls to these need to be suppressed if verifications are disabled)

        /// <summary>
        /// If a global type checking is already queued, but hasn't started executing yet, does nothing and returns a completed task.
        /// If a global type checking is in progress, cancels that process via <see cref="waitForTypeCheck"/>,
        /// then queues <see cref="SpawnGlobalTypeCheckingAsync"/> for exectution into the task queue,
        /// and sets <see cref="waitForTypeCheck"/> to null, indicating that a type global type checking
        /// is queued and has not started executing yet.
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
        /// Updates all global symbols for all files in the compilation using a
        /// separate <see cref="CompilationUnit"/> instance.
        /// <para/>
        /// Copies the symbol information over to the Compilation property,
        /// updates the HeaderDiagnostics and clears the SemanticDiagnostics in all files, and publishes the updated diagnostics.
        /// <para/>
        /// Sets <see cref="waitForTypeCheck"/> to a new <see cref="CancellationTokenSource"/>,
        /// and then spawns a task calling <see cref="RunGlobalTypeChecking"/> on the computed
        /// content using the separate <see cref="CompilationUnit"/>,
        /// with a cancellation token from the new cancellation source.
        /// </summary>
        /// <param name="runSynchronously">
        /// Run the task synchronously ignoring any cancellations and return the completed task.
        /// </param>
        private Task SpawnGlobalTypeCheckingAsync(bool runSynchronously = false)
        {
            this.waitForTypeCheck = new CancellationTokenSource(); // set a handle for cancelling this type check
            var cancellationToken = runSynchronously ? CancellationToken.None : this.waitForTypeCheck.Token;

            // work with a separate compilation unit instance such that processing of all further edits can go on in parallel
            var sourceFiles = this.fileContentManagers.Values.OrderBy(m => m.FileName);
            this.changedFiles.RemoveAll(f => sourceFiles.Any(m => m.FileName == f));
            var compilation = new CompilationUnit(this.compilationUnit, sourceFiles.Select(file => file.SyncRoot));
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
        /// Runs a type checking on <paramref name="content"/> using <paramref name="compilation"/>,
        /// with <paramref name="cancellationToken"/>.
        /// </summary>
        /// <remarks>
        /// Once the type checking is done, locks all processing, and updates the Compilation property with the built content
        /// for all files that have not been modified while the type checking was running. Updates and publishes the semantic diagnostics for those files.
        /// <para/>
        /// For each file that has been modified while the type checking was running,
        /// queues a task calling <see cref="TypeCheckFile"/> on that file with <paramref name="cancellationToken"/>.
        /// </remarks>
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
        /// Updates and resolves all global symbols for <paramref name="fileId"/>, and runs a type checking
        /// on the obtained content using the Compilation property.
        /// </summary>
        /// <remarks>
        /// Does nothing if <paramref name="cancellationToken"/> is cancelled or if <paramref name="fileId"/>
        /// is not listed as a source file.
        /// <para/>
        /// Replaces the header diagnostics and the semantic diagnostics in the file with the obtained diagnostics, and publishes them.
        /// </remarks>
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
        /// </summary>
        /// <remarks>
        /// Null if no symbol exists at the specified position,
        /// or if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the file.
        /// </remarks>
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

        /// <summary>
        /// Returns the edits to format the file according to the specified settings.
        /// </summary>
        /// <remarks>
        /// Returns null if some parameters are unspecified (null),
        /// or if the specified file is not listed as source file
        /// </remarks>
        public TextEdit[]? Formatting(TextDocumentIdentifier? textDocument, bool format = true, bool update = true, int timeout = 3000)
        {
            var verb =
                update && format ? "update-and-format" :
                update ? "update" :
                format ? "format" :
                null;

            var qsFmtExe = this.compilationUnit.BuildProperties.QsFmtExe;
            var sdkPath = this.compilationUnit.BuildProperties.SdkPath;

            var fmtCommand = qsFmtExe?.Split();
            (string command, string dllPath) = fmtCommand != null && fmtCommand.Length > 0
                ? (fmtCommand[0], string.Join(" ", fmtCommand[1..]))
                : ("dotnet", Path.Combine(sdkPath ?? "", "tools", "qsfmt", "qsfmt.dll"));

            // It is possible for File.Exists/Directory.Exists to return false for both dllPath and sdkPath
            // despite that the command can indeed be successfully executed.
            if (verb == null)
            {
                return null;
            }

            TextEdit[]? FormatFile(FileContentManager file)
            {
                var tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".qs");
                var currentContent = file.GetFileContent();
                File.WriteAllText(tempFile, currentContent);

                // The exit code is selected looking at this: https://tldp.org/LDP/abs/html/exitcodes.html
                // Code 130 usually indicates "Script terminated by Control-C", and seem appropriate in this case.
                var exitCodeOnTimeout = 130;
                var commandArgs = $"{dllPath} {verb} --input {tempFile}";
                this.Log($"Invoking {verb} command: {command} {commandArgs}", MessageType.Info);
                var succeeded =
                    ProcessRunner.Run(command, commandArgs, out var _, out var errstream, out var exitCode, out var ex, timeout: timeout, exitCodeOnTimeOut: exitCodeOnTimeout)
                    && exitCode == 0 && ex == null;

                if (succeeded)
                {
                    var range = DataTypes.Range.Create(DataTypes.Position.Zero, file.End());
                    var edit = new TextEdit { Range = range.ToLsp(), NewText = File.ReadAllText(tempFile) };
                    File.Delete(tempFile);
                    return new[] { edit };
                }
                else if (exitCode == exitCodeOnTimeout)
                {
                    this.Log($"{verb} command timed out.", MessageType.Info);
                }
                else if (ex != null)
                {
                    if (qsFmtExe == null && this.compilationUnit.BuildProperties.SdkVersion > new Version(0, 21))
                    {
                        this.LogException(ex);
                    }
                }
                else
                {
                    this.Log($"Unknown error during {verb} (exit code {exitCode}): \n{errstream}", MessageType.Error);
                }

                return null;
            }

            var succeeded = this.Processing.QueueForExecution(
                () => this.FileQuery(
                    textDocument,
                    (file, _) => FormatFile(file),
                    suppressExceptionLogging: false), // since the operation is blocking, no exceptions should occur
                out var edits);
            return succeeded ? edits : null;
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
        /// Get all current diagnostics.
        /// </summary>
        /// <returns>
        /// If <paramref name="textDocument"/> is specified, an array with a single item containing all current diagnostics for the given file.
        /// If <paramref name="textDocument"/> is not specified, the diagnostics for all source files are returned.
        /// If <paramref name="textDocument"/> is not listed as a source file, null.
        /// </returns>
        /// <remarks>
        /// This method waits for all currently running or queued tasks to finish
        /// before accumulating the diagnostics by calling <see cref="FlushAndExecute"/>.
        /// </remarks>
        public PublishDiagnosticParams[]? GetDiagnostics(TextDocumentIdentifier? textDocument = null) =>
            this.FlushAndExecute(() =>
                textDocument != null
                    ? this.FileQuery(textDocument, (file, _) => new PublishDiagnosticParams[] { file.Diagnostics() })
                    : this.fileContentManagers.Values.Select(file => file.Diagnostics()).ToArray());

        /// <summary>
        /// Returns a sequence of all source files that are currently contained in this compilation unit.
        /// </summary>
        /// <remarks>
        /// Waits for all currently running or queued tasks to finish
        /// before constructing the requested information by calling <see cref="FlushAndExecute"/>.
        /// </remarks>
        public IEnumerable<Uri>? GetSourceFiles() =>
            this.FlushAndExecute(() => this.fileContentManagers.Keys.Select(id => new Uri(id)).ToImmutableArray().AsEnumerable());

        /// <summary>
        /// Gets whether a file is a notebook cell
        /// </summary>
        /// <returns>
        /// A boxed bool holding true if it is a notebook cell, else false
        /// </returns>
        /// <remarks>
        /// Waits for all currently running or queued tasks to finish
        /// before getting the file content by calling <see cref="FlushAndExecute"/>.
        /// </remarks>
        public object? FileIsNotebookCell(TextDocumentIdentifier textDocument) =>

            // Boxing needed here because FileQuery() is generic:
            // https://devblogs.microsoft.com/dotnet/try-out-nullable-reference-types/#the-issue-with-t
            this.FlushAndExecute(() =>
                this.FileQuery(textDocument, (file, _) => (object)file.IsNotebook));

        /// <summary>
        /// Gets the current file content (text representation) in memory.
        /// </summary>
        /// <returns>
        /// The current file content in memory, or null if <paramref name="textDocument"/> is not listed as a source file.
        /// </returns>
        /// <remarks>
        /// Waits for all currently running or queued tasks to finish
        /// before getting the file content by calling <see cref="FlushAndExecute"/>.
        /// </remarks>
        public string[]? FileContentInMemory(TextDocumentIdentifier textDocument) =>
            this.FlushAndExecute(() =>
                this.FileQuery(textDocument, (file, _) => file.GetLines(0, file.NrLines()).Select(line => line.Text).ToArray()));

        /// <summary>
        /// Gets the current tokenization of the file content in memory.
        /// Each returned array item contains the array of tokens on the line with the corresponding index.
        /// </summary>
        /// <returns>
        /// The current tokenization of the file content in memory, or null if <paramref name="textDocument"/>
        /// is not listed as a source file.
        /// </returns>
        /// <remarks>
        /// Waits for all currently running or queued tasks to finish
        /// before getting the file content by calling <see cref="FlushAndExecute"/>.
        /// </remarks>
        public QsFragmentKind?[][]? GetTokenization(TextDocumentIdentifier textDocument) =>
            this.FlushAndExecute(() =>
                this.FileQuery(textDocument, (file, _) => file.GetTokenizedLines(0, file.NrLines()).Select(line => line.Select(frag => frag.Kind).ToArray()).ToArray()));

        /// <summary>
        /// Returns the syntax tree for the current state of the compilation.
        /// </summary>
        /// <remarks>
        /// Waits for all currently running or queued tasks to finish
        /// before constructing the syntax tree by calling <see cref="FlushAndExecute"/>.
        /// </remarks>
        public IEnumerable<QsNamespace>? GetSyntaxTree() =>
            this.FlushAndExecute(() => this.compilationUnit.Build().Namespaces.AsEnumerable());

        /// <summary>
        /// Returns a <see cref="Compilation"/> containing all information about the current state of the compilation.
        /// </summary>
        /// <remarks>
        /// Waits for all currently running or queued tasks to finish
        /// before constructing the <see cref="Compilation"/> by calling <see cref="FlushAndExecute"/>.
        /// </remarks>
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
            public ImmutableHashSet<string> SourceFiles { get; }

            /// <summary>
            /// Contains the IDs assigned by the Q# compiler for all assemblies referenced in the compilation.
            /// </summary>
            public ImmutableHashSet<string> References { get; }

            /// <summary>
            /// Contains a dictionary that maps the ID of a file included in the compilation
            /// to the text representation of its content.
            /// </summary>
            public ImmutableDictionary<string, ImmutableArray<string>> FileContent { get; }

            /// <summary>
            /// Contains a dictionary that maps the ID of a file included in the compilation
            /// to the tokenization built based on its content.
            /// </summary>
            public ImmutableDictionary<string, ImmutableArray<ImmutableArray<CodeFragment>>> Tokenization { get; }

            /// <summary>
            /// Contains a dictionary that maps the name of a namespace to the compiled Q# namespace.
            /// </summary>
            public ImmutableDictionary<string, QsNamespace> SyntaxTree { get; }

            /// <summary>
            /// Contains the built Q# compilation.
            /// </summary>
            public QsCompilation BuiltCompilation { get; }

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
            public ImmutableDictionary<QsQualifiedName, QsCallable> Callables { get; }

            /// <summary>
            /// Contains a dictionary that given the fully qualified name of a compiled type returns its syntax tree.
            /// </summary>
            public ImmutableDictionary<QsQualifiedName, QsCustomType> Types { get; }

            /// <summary>
            /// Contains a dictionary that maps the ID of a file included in the compilation
            /// to all scope-related diagnostics generated during compilation.
            /// </summary>
            public ImmutableDictionary<string, ImmutableArray<Diagnostic>> ScopeDiagnostics { get; }

            /// <summary>
            /// Contains a dictionary that maps the ID of a file included in the compilation
            /// to all syntax-related diagnostics generated during compilation.
            /// </summary>
            public ImmutableDictionary<string, ImmutableArray<Diagnostic>> SyntaxDiagnostics { get; }

            /// <summary>
            /// Contains a dictionary that maps the ID of a file included in the compilation
            /// to all context-related diagnostics generated during compilation.
            /// </summary>
            public ImmutableDictionary<string, ImmutableArray<Diagnostic>> ContextDiagnostics { get; }

            /// <summary>
            /// Contains a dictionary that maps the ID of a file included in the compilation
            /// to all diagnostics generated during compilation related to header information for declarations.
            /// </summary>
            public ImmutableDictionary<string, ImmutableArray<Diagnostic>> HeaderDiagnostics { get; }

            /// <summary>
            /// Contains a dictionary that maps the ID of a file included in the compilation
            /// to all semantic diagnostics generated during compilation for the specified implementations.
            /// </summary>
            public ImmutableDictionary<string, ImmutableArray<Diagnostic>> SemanticDiagnostics { get; }

            /// <summary>
            /// Maps a <paramref name="file"/> ID assigned by the Q# compiler to all diagnostics generated during compilation.
            /// </summary>
            /// <returns>
            /// Returns the generated diagnostics, or an empty sequence if no file with <paramref name="file"/>
            /// ID has been included in the compilation.
            /// </returns>
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
            /// Gets non-documentation comments for a namespace.
            /// </summary>
            /// <remarks>
            /// If file <paramref name="sourceFile"/> does not exist in the compilation, or there is not
            /// exactly one (partial) namespace <paramref name="nsName"/>, returns a set of empty comments.
            /// </remarks>
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
            /// Given ID <paramref name="sourceFile"/> and namespace <paramref name="nsName"/>,
            /// returns the names and corresponding short form (if any) of all opened namespaces for the (part of the) namespace in that file.
            /// </summary>
            /// <returns>
            /// The names and corresponding short forms of the opened namespaces.
            /// </returns>
            /// <remarks>
            /// Returns an empty sequence if <paramref name="sourceFile"/> and/or <paramref name="nsName"/>
            /// do not exist in the compilation.
            /// </remarks>
            public IEnumerable<(string, string?)> OpenDirectives(string sourceFile, string nsName) =>
                this.openDirectivesForEachFile.TryGetValue(nsName, out var lookUp)
                    ? lookUp[sourceFile]
                    : Enumerable.Empty<(string, string?)>();

            /// <summary>
            /// Returns all the names of all callable and types defined in namespace <paramref name="nsName"/>.
            /// </summary>
            /// <returns>
            /// The names defined in namespace <paramref name="nsName"/>, or an empty sequence
            /// if <paramref name="nsName"/> does not exist in the compilation.
            /// </returns>
            /// <remarks>
            /// The returned names are unique and do not contain duplications e.g. for types and the corresponding constructor.
            /// </remarks>
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
