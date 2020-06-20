// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
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
        private readonly ConcurrentDictionary<NonNullable<string>, FileContentManager> FileContentManagers;
        /// <summary>
        /// contains the CompilationUnit to which all files managed in this class instance belong to
        /// </summary>
        private readonly CompilationUnit CompilationUnit;

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
        private CancellationTokenSource WaitForTypeCheck;
        /// <summary>
        /// used to track which files have changed during global type checking
        /// </summary>
        private readonly ManagedHashSet<NonNullable<string>> ChangedFiles;
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
            Action<Exception> exceptionLogger = null, Action<PublishDiagnosticParams> publishDiagnostics = null, bool syntaxCheckOnly = false,
            AssemblyConstants.RuntimeCapabilities capabilities = AssemblyConstants.RuntimeCapabilities.Unknown, bool isExecutable = false)
        {
            this.EnableVerification = !syntaxCheckOnly;
            this.CompilationUnit = new CompilationUnit(capabilities, isExecutable);
            this.FileContentManagers = new ConcurrentDictionary<NonNullable<string>, FileContentManager>();
            this.ChangedFiles = new ManagedHashSet<NonNullable<string>>(new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion));
            this.PublishDiagnostics = publishDiagnostics ?? (_ => { });
            this.LogException = exceptionLogger ?? Console.Error.WriteLine;
            this.Processing = new ProcessingQueue(this.LogException);
            this.WaitForTypeCheck = new CancellationTokenSource();
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
        public T FlushAndExecute<T>(Func<T> execute = null) where T : class
        {
            // To enforce an up-to-date content for executing the given function,
            // we want to do a (synchronous!) global type checking on flushing,
            // and hence cancel any ongoing type checking and set the WaitForTypeCheck handle to null. 
            // However, we *also* need to make sure that any global type checking that is queued prior to the flushing task 
            // (indicated by the WaitForTypeChecking handle currently being null) does not extend past it - 
            // i.e. during the flushing task we again need to cancel any ongoing type checking.  
            this.WaitForTypeCheck?.Cancel();
            this.WaitForTypeCheck = null;
            var succeeded = this.Processing.QueueForExecution(() =>
            {
                this.WaitForTypeCheck?.Cancel(); // needed in the case where WaitForTypeCheck above was null
                foreach (var file in this.FileContentManagers.Values)
                {
                    file.Flush();
                    this.PublishDiagnostics(file.Diagnostics());
                }
                var task = this.EnableVerification ? this.SpawnGlobalTypeCheckingAsync(runSynchronously: true) : Task.CompletedTask;
                QsCompilerError.Verify(task.IsCompleted, "global type checking hasn't completed"); 
                return execute?.Invoke();
            }
            , out T result);
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
                this.WaitForTypeCheck?.Dispose();
                this.CompilationUnit.Dispose();
                foreach (var file in this.FileContentManagers.Values) 
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
        /// Returns the string with the file ID associated with the given URI used throughout the compilation.
        /// </summary>
        public static bool TryGetFileId(Uri uri, out NonNullable<string> id)
        {
            id = NonNullable<string>.New("");
            if (uri == null || !uri.IsFile || !uri.IsAbsoluteUri) return false;
            id = NonNullable<string>.New(uri.IsUnc ? uri.LocalPath : uri.AbsolutePath);
            return true;
        }

        /// <summary>
        /// Given a file id assigned by the compilation unit manager, returns the URI based on which the ID was constructed as out parameter.
        /// Returns false if the given file id is not compatible with an id generated by the compilation unit manager. 
        /// </summary>
        public static bool TryGetUri(NonNullable<string> fileId, out Uri uri) =>
            Uri.TryCreate(Uri.UnescapeDataString(fileId.Value), UriKind.Absolute, out uri);

        /// <summary>
        /// Subscribes to diagnostics updated events, 
        /// to events indicating when queued changes in the file content manager have not yet been processed, 
        /// and to events indicating that the compilation unit wide semantic verification needs to be updated. 
        /// Throws an ArgumentNullException if the given file is null.
        /// </summary>
        private void SubscribeToFileManagerEvents(FileContentManager file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            file.TimerTriggeredUpdateEvent += this.TriggerFileUpdateAsync;
            if (this.EnableVerification) file.GlobalTypeCheckingEvent += this.QueueGlobalTypeCheckingAsync;
        }

        /// <summary>
        /// Unsubscribes from all events that SubscribeToFileManagerEvents subscribes to for the given file.
        /// Throws an ArgumentNullException if the given file is null. 
        /// </summary>
        private void UnsubscribeFromFileManagerEvents(FileContentManager file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            file.TimerTriggeredUpdateEvent -= this.TriggerFileUpdateAsync;
            if (this.EnableVerification) file.GlobalTypeCheckingEvent -= this.QueueGlobalTypeCheckingAsync;
        }


        /// <summary>
        /// Initializes a FileContentManager for the given document with the given content.
        /// If an Action for publishing is given, publishes the diagnostics generated upon processing the given content. 
        /// Throws an ArgumentNullException if the given file content is null. 
        /// Throws an ArgumentException if the uri of the given text document identifier is null or not an absolute file uri. 
        /// </summary>
        public static FileContentManager InitializeFileManager(Uri uri, string fileContent, 
            Action<PublishDiagnosticParams> publishDiagnostics = null, Action<Exception> onException = null)
        {
            if (!TryGetFileId(uri, out NonNullable<string> docKey)) throw new ArgumentException("invalid TextDocumentIdentifier");
            if (fileContent == null) throw new ArgumentNullException(nameof(fileContent));

            var file = new FileContentManager(uri, docKey);
            try { file.ReplaceFileContent(fileContent); }
            catch (Exception ex) { onException?.Invoke(ex); }
            publishDiagnostics?.Invoke(file.Diagnostics());
            return file;
        }

        /// <summary>
        /// Initializes a FileContentManager for each entry in the given dictionary of source files and their content. 
        /// If an Action for publishing is given, publishes the diagnostics generated upon content processing. 
        /// Throws an ArgumentNullException if the given dictionary of files and their content is null. 
        /// Throws an ArgumentException if any of the given uris is null or not an absolute file uri, or if any of the content is null. 
        /// </summary>
        public static ImmutableHashSet<FileContentManager> InitializeFileManagers(IDictionary<Uri, string> files,
            Action<PublishDiagnosticParams> publishDiagnostics = null, Action<Exception> onException = null)
        {
            if (files == null) throw new ArgumentNullException(nameof(files));
            if (files.Any(item => item.Value == null)) throw new ArgumentException("file content cannot be null");
            if (files.Any(item => !TryGetFileId(item.Key, out NonNullable<string> docKey))) throw new ArgumentException("invalid TextDocumentIdentifier");

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
        /// Throws an ArgumentNullException if any of the compulsory arguments is null or the set uri is.
        /// Throws an ArgumentException if the uri of the given text document identifier is null or not an absolute file uri. 
        /// </summary>
        public Task AddOrUpdateSourceFileAsync(FileContentManager file, string updatedContent = null)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            return this.Processing.QueueForExecutionAsync(() =>
            {
                this.CompilationUnit.RegisterDependentLock(file.SyncRoot);
                this.SubscribeToFileManagerEvents(file);
                this.FileContentManagers.AddOrUpdate(file.FileName, file, (k, v) => file);
                if (updatedContent != null) file.ReplaceFileContent(updatedContent);
                this.ChangedFiles.Add(file.FileName);
                if (this.EnableVerification && this.WaitForTypeCheck != null) file.Verify(this.CompilationUnit);
                this.PublishDiagnostics(file.Diagnostics());
            });
        }

        /// <summary>
        /// Adds the given source files to this compilation unit, adapting the diagnostics for all remaining files as needed.
        /// If a file with the same Uri is already listed as source file,
        /// replaces the current FileContentManager for that file with a new one and initialized its content to the given one. 
        /// Spawns a compilation unit wide type checking unless suppressVerification is set to true, even if no files have been added. 
        /// Throws an ArgumentNullException if the given source files are null.
        /// Throws an ArgumentException if an uri is not a valid absolute file uri, or if the content for a file is null. 
        /// </summary>
        public Task AddOrUpdateSourceFilesAsync(ImmutableHashSet<FileContentManager> files, bool suppressVerification = false)
        {
            if (files == null || files.Contains(null)) throw new ArgumentNullException(nameof(files));
            return this.Processing.QueueForExecutionAsync(() =>
            {
                foreach (var file in files)
                {
                    this.CompilationUnit.RegisterDependentLock(file.SyncRoot);
                    this.SubscribeToFileManagerEvents(file);
                    this.FileContentManagers.AddOrUpdate(file.FileName, file, (k, v) => file);
                    this.ChangedFiles.Add(file.FileName);
                    this.PublishDiagnostics(file.Diagnostics());
                }
                if (this.EnableVerification && !suppressVerification) this.QueueGlobalTypeCheckingAsync();
            });
        }

        /// <summary>
        /// Modifies the compilation and all diagnostics to reflect the given change.
        /// Throws an ArgumentNullException if any of the arguments is null.
        /// Throws a InvalidOperationException if file for which a change is given is not listed as source file.
        /// Throws an ArgumentException if the uri of the given text document identifier is null or not an absolute file uri. 
        /// </summary>
        public Task SourceFileDidChangeAsync(DidChangeTextDocumentParams param)
        {
            if (!TryGetFileId(param?.TextDocument?.Uri, out NonNullable<string> docKey)) throw new ArgumentException("invalid TextDocumentIdentifier");
            if (param.ContentChanges == null) throw new ArgumentNullException(nameof(param.ContentChanges));

            return this.Processing.QueueForExecutionAsync(() =>
            {
                var isSource = this.FileContentManagers.TryGetValue(docKey, out FileContentManager file);
                if (!isSource) throw new InvalidOperationException ($"changed file {docKey.Value} is not a source file of this compilation unit");

                this.ChangedFiles.Add(docKey);
                var publish = false;
                foreach (var change in param.ContentChanges) file.PushChange(change, out publish); // only the last one here is relevant 

                if (publish)
                {
                    if (this.EnableVerification && this.WaitForTypeCheck != null)
                    { file.AddTimerTriggeredUpdateEvent(); }
                    this.PublishDiagnostics(file.Diagnostics());
                }
                else file.AddTimerTriggeredUpdateEvent();
            });
        }

        /// <summary>
        /// Called in order to process any queued changes in the file with the given URI and 
        /// - if verifications are enabled - trigger a semantic check.
        /// Does not do anything if no file with the given URI is listed as source file of this compilation. 
        /// Throws an ArgumentException if the URI of the given text document identifier is null or not an absolute file URI. 
        /// </summary>
        private Task TriggerFileUpdateAsync(Uri uri)
        {
            if (!TryGetFileId(uri, out NonNullable<string> docKey)) throw new ArgumentException("invalid TextDocumentIdentifier");
            return this.Processing.QueueForExecutionAsync(() =>
            {
                // Note that we cannot fail if the file is no longer part of the compilation, 
                // because it is possible that between the time when a file has been removed and when that removal is executed
                // a file update has been triggered e.g. by timer...
                var isSource = this.FileContentManagers.TryGetValue(docKey, out FileContentManager file);
                if (!isSource) return; 

                // we need to mark a file as edited whenever some processing is spawned by the file, since
                // otherwise if a global type checking runs while changes are still queued in the file, 
                // then the automatically spawned processing of those changes will be (partially) overwritten 
                // when the global type checking results are pushed back in
                this.ChangedFiles.Add(docKey);
                file.Flush();
                if (this.EnableVerification && this.WaitForTypeCheck != null) file.Verify(this.CompilationUnit);
                this.PublishDiagnostics(file.Diagnostics());
            });
        }

        /// <summary>
        /// Removes the specified file from the list of source files for this compilation unit, 
        /// publishes empty Diagnostics for that file unless publishEmptyDiagnostics is set to false,
        /// and adapts all remaining diagnostics as needed.
        /// Does nothing if no file with the given Uri is listed as source file. 
        /// Throws an ArgumentException if the uri of the given text document identifier is null or not an absolute file uri. 
        /// </summary>
        public Task TryRemoveSourceFileAsync(Uri uri, bool publishEmptyDiagnostics = true)
        {
            if (!TryGetFileId(uri, out NonNullable<string> docKey)) throw new ArgumentException("invalid TextDocumentIdentifier");
            return this.Processing.QueueForExecutionAsync(() =>
            {
                if (!this.FileContentManagers.TryRemove(docKey, out FileContentManager file)) return;
                this.ChangedFiles.Add(docKey);
                this.CompilationUnit.UnregisterDependentLock(file.SyncRoot); // do *not* dispose of the FileContentManager!
                this.UnsubscribeFromFileManagerEvents(file);                 // ... but unsubscribe from all file events
                this.CompilationUnit.GlobalSymbols.RemoveSource(docKey);
                if (publishEmptyDiagnostics) this.PublishDiagnostics(new PublishDiagnosticParams { Uri = uri, Diagnostics = Array.Empty<Diagnostic>() });
                if (this.EnableVerification) this.QueueGlobalTypeCheckingAsync(); // we need to trigger a global type checking for the remaining files...
            });
        }

        /// <summary>
        /// Removes the specified files from the list of source files for this compilation unit, 
        /// publishes empty Diagnostics for the removed files unless publishEmptyDiagnostics is set to false, 
        /// and adapts all remaining diagnostics as needed.
        /// Does nothing if a file with the given Uri is not listed as source file. 
        /// Spawns a compilation unit wide type checking unless suppressVerification is set to true, even if no files have been removed. 
        /// Throws an ArgumentNullException if the given sequence of files are null.
        /// Throws an ArgumentException if the uri of the given text document identifier is null or not an absolute file uri. 
        /// </summary>
        public Task TryRemoveSourceFilesAsync(IEnumerable<Uri> files, bool suppressVerification = false, bool publishEmptyDiagnostics = true)
        {
            if (files == null) throw new ArgumentNullException(nameof(files));
            if (files.Any(uri => !TryGetFileId(uri, out var _))) throw new ArgumentException("invalid TextDocumentIdentifier");

            return this.Processing.QueueForExecutionAsync(() =>
            {
                foreach (var uri in files)
                {
                    TryGetFileId(uri, out NonNullable<string> docKey);
                    if (!this.FileContentManagers.TryRemove(docKey, out FileContentManager file)) return;
                    this.ChangedFiles.Add(docKey);
                    this.CompilationUnit.UnregisterDependentLock(file.SyncRoot); // do *not* dispose of the FileContentManager!
                    this.UnsubscribeFromFileManagerEvents(file);                 // ... but unsubscribe from all file events
                    this.CompilationUnit.GlobalSymbols.RemoveSource(docKey);
                    if (publishEmptyDiagnostics) this.PublishDiagnostics(new PublishDiagnosticParams { Uri = uri, Diagnostics = Array.Empty<Diagnostic>() });
                    if (this.EnableVerification) this.QueueGlobalTypeCheckingAsync(); // we need to trigger a global type checking for the remaining files...
                }
                if (this.EnableVerification && !suppressVerification) this.QueueGlobalTypeCheckingAsync();
            });
        }

        /// <summary>
        /// Replaces the content from all referenced assemblies with the given references.
        /// Updates all diagnostics accordingly, unless suppressVerification has been set to true. 
        /// Throws an ArgumentNullException if the given references are null. 
        /// </summary>
        internal Task UpdateReferencesAsync(References references, bool suppressVerification = false)
        {
            if (references == null) throw new ArgumentNullException(nameof(references));
            return this.Processing.QueueForExecutionAsync(() =>
            {
                this.CompilationUnit.UpdateReferences(references);
                if (this.EnableVerification && !suppressVerification) this.QueueGlobalTypeCheckingAsync();
            });
        }

        /// <summary>
        /// Replaces the content from all referenced assemblies with the given references, and updates all diagnostics accordingly. 
        /// Throws an ArgumentNullException if the given references are null. 
        /// </summary>
        public Task UpdateReferencesAsync(References references) =>
            UpdateReferencesAsync(references, false);


        // routines related to global type checking (all calls to these need to be suppressed if verifications are disabled)

        /// <summary>
        /// If a global type checking is already queued, but hasn't started executing yet, does nothing and returns a completed task. 
        /// If a global type checking is in progress, cancels that process via WaitForTypeChecking,
        /// then queues SpawnGlobalTypeCheckingAsync for exectution into the task queue, and sets WaitForTypeChecking to null, 
        /// indicating that a type global type checking is queued and has not started executing yet. 
        /// </summary>
        private Task QueueGlobalTypeCheckingAsync()
        {
            if (this.WaitForTypeCheck == null) return Task.CompletedTask; // type check is already queued
            this.WaitForTypeCheck.Cancel(); // cancel any ongoing type check...
            this.WaitForTypeCheck = null;   // ... and queue a new type check
            return this.Processing.QueueForExecutionAsync(() =>
                QsCompilerError.RaiseOnFailure(() =>
                this.SpawnGlobalTypeCheckingAsync(), "error while spawning global type checking"));
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
            this.WaitForTypeCheck = new CancellationTokenSource(); // set a handle for cancelling this type check
            var cancellationToken = runSynchronously ? new CancellationToken() : this.WaitForTypeCheck.Token;

            // work with a separate compilation unit instance such that processing of all further edits can go on in parallel
            var sourceFiles = this.FileContentManagers.Values.OrderBy(m => m.FileName);
            this.ChangedFiles.RemoveAll(f => sourceFiles.Any(m => m.FileName.Value == f.Value));
            var compilation = new CompilationUnit(this.CompilationUnit.RuntimeCapabilities, this.CompilationUnit.IsExecutable, this.CompilationUnit.Externals, sourceFiles.Select(file => file.SyncRoot));
            var content = compilation.UpdateGlobalSymbolsFor(sourceFiles);
            foreach (var file in sourceFiles) this.PublishDiagnostics(file.Diagnostics());

            // move the content of symbols over to the Compilation
            this.CompilationUnit.EnterWriteLock();
            try
            {
                this.CompilationUnit.GlobalSymbols.Clear();
                compilation.GlobalSymbols.CopyTo(this.CompilationUnit.GlobalSymbols);
                this.CompilationUnit.GlobalSymbols.ResolveAll(BuiltIn.NamespacesToAutoOpen);
            }
            finally { this.CompilationUnit.ExitWriteLock(); }

            // after the relevant information is extracted, the actual type checking can run in parallel
            var task = new Task(() =>
            {
                try
                {   // Do *not* remove the RaiseOnFailure! 
                    // -> keep it here, such that all internal exceptions are being piped through one central routine (in QsCompilerError)
                    QsCompilerError.RaiseOnFailure(() =>
                        this.RunGlobalTypeChecking(compilation, content, cancellationToken),
                        "error while running global type checking");
                }
                catch (Exception ex) { this.LogException(ex); }
            }, cancellationToken);

            if (runSynchronously) task.RunSynchronously();
            else if (!cancellationToken.IsCancellationRequested) task.Start();
            return task;
        }

        /// <summary>
        /// Runs a type checking on the given content using the given CompilationUnit instance, with the given cancellation token. 
        /// Once the type checking is done, locks all processing, and updates the Compilation property with the built content 
        /// for all files that have not been modified while the type checking was running. Updates and publishes the semantic diagnostics for those files.
        /// For each file that has been modified while the type checking was running, 
        /// queues a task calling TypeCheckFile on that file with the given cancellationToken.
        /// Throws an ArgumentNullException if any of the given arguments is null. 
        /// </summary>
        private void RunGlobalTypeChecking(CompilationUnit compilation, ImmutableDictionary<QsQualifiedName, (QsComments, FragmentTree)> content, CancellationToken cancellationToken)
        {
            if (compilation == null) throw new ArgumentNullException(nameof(compilation));
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (cancellationToken == null) throw new ArgumentNullException(nameof(cancellationToken));

            var diagnostics = QsCompilerError.RaiseOnFailure(() =>
                TypeChecking.RunTypeChecking(compilation, content, cancellationToken),
                "error while running type checking in background");

            this.Processing.QueueForExecution(() => // -> could be fast-tracked to be executed immediately, but needs exclusive access (write thread)
            {
                foreach (var file in this.FileContentManagers.Values) file.SyncRoot.EnterUpgradeableReadLock();
                this.ChangedFiles.SyncRoot.EnterReadLock(); // need to lock such that the content of the list remains the same during the execution of the block below
                try
                {
                    var changedFiles = this.ChangedFiles.ToImmutableHashSet();
                    if (cancellationToken.IsCancellationRequested) return;

                    var validCallables = compilation.GetCallables().Values.Where(c => !changedFiles.Contains(c.SourceFile));
                    var validTypes = compilation.GetTypes().Values.Where(t => !changedFiles.Contains(t.SourceFile));
                    this.CompilationUnit.UpdateCallables(validCallables);
                    this.CompilationUnit.UpdateTypes(validTypes);

                    if (cancellationToken.IsCancellationRequested) return;
                    var allDiagnostics = diagnostics.ToLookup(msg => NonNullable<string>.New(msg.Source));
                    foreach (var file in this.FileContentManagers.Values)
                    {
                        if (changedFiles.Contains(file.FileName)) continue;
                        file.ReplaceSemanticDiagnostics(allDiagnostics[file.FileName]);
                        this.PublishDiagnostics(file.Diagnostics());
                    }

                    foreach (var docKey in changedFiles) 
                    {
                        this.Processing.QueueForExecutionAsync(() =>
                            QsCompilerError.RaiseOnFailure(() => this.TypeCheckFile(docKey, cancellationToken),
                            "error while re-doing the type checking for an source file that had been modified during a global type check update"));
                    }
                }
                finally
                {
                    this.ChangedFiles.SyncRoot.ExitReadLock();
                    foreach (var file in this.FileContentManagers.Values) file.SyncRoot.ExitUpgradeableReadLock(); 
                }
            });
        }

        /// <summary>
        /// If the given cancellation token is not cancellation requested, and the file with the document id is listed as source file, 
        /// updates and resolves all global symbols for that file, and runs a type checking on the obtained content using the Compilation property.
        /// Replaces the header diagnostics and the semantic diagnostics in the file with the obtained diagnostics, and publishes them. 
        /// Throws an ArgumentNullException if the given cancellation token is null.
        /// </summary>
        private void TypeCheckFile(NonNullable<string> fileId, CancellationToken cancellationToken)
        {
            if (cancellationToken == null) throw new ArgumentNullException(nameof(cancellationToken));
            var isSource = this.FileContentManagers.TryGetValue(fileId, out FileContentManager file);
            if (!isSource || cancellationToken.IsCancellationRequested) return;

            this.CompilationUnit.EnterUpgradeableReadLock();
            file.SyncRoot.EnterUpgradeableReadLock();
            try
            {   // NOTE: invalidating the right diagnostics is kind of error prone, 
                // so instead of calling file.UpdateTypeChecking the type checking for the entire file is simply recomputed.

                var diagnostics = new List<Diagnostic>();
                var contentToCompile = file.UpdateGlobalSymbols(this.CompilationUnit, diagnostics);
                file.ImportGlobalSymbols(this.CompilationUnit, diagnostics);
                TypeChecking.ResolveGlobalSymbols(this.CompilationUnit.GlobalSymbols, diagnostics, file.FileName.Value);
                file.ReplaceHeaderDiagnostics(diagnostics);

                diagnostics = TypeChecking.RunTypeChecking(this.CompilationUnit, file.GetDeclarationTrees(contentToCompile), new CancellationToken());
                file.ReplaceSemanticDiagnostics(diagnostics);
                this.PublishDiagnostics(file.Diagnostics());
            }
            finally {
                file.SyncRoot.ExitUpgradeableReadLock();
                this.CompilationUnit.ExitUpgradeableReadLock();
            }
        }


        // editor commands that need to be blocking

        /// <summary>
        /// Returns the workspace edit that describes the changes to be done if the symbol at the given position - if any - is renamed to the given name. 
        /// Returns null if no symbol exists at the specified position,
        /// or if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the file.
        /// </summary>
        public WorkspaceEdit Rename(RenameParams param)
        {
            if (!TryGetFileId(param?.TextDocument?.Uri, out NonNullable<string> docKey)) return null;
            var success = this.Processing.QueueForExecution(() => // FIXME: the correct thing to do here would be to call FlushAndExecute...!
                this.FileContentManagers.TryGetValue(docKey, out FileContentManager file)
                    ? file.Rename(this.CompilationUnit, param.Position, param.NewName)
                    : null
            , out WorkspaceEdit edit);
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
        internal T FileQuery<T>(TextDocumentIdentifier textDocument,
            Func<FileContentManager, CompilationUnit, T> Query, bool suppressExceptionLogging = false)
            where T : class
        {
            T TryQueryFile(FileContentManager f)
            {
                try { return Query(f, this.CompilationUnit); }
                catch (Exception ex)
                {
                    #if DEBUG
                    this.LogException(ex);
                    #else
                    if (!suppressExceptionLogging) this.LogException(ex);
                    #endif
                    return null;
                }
            }
            if (!TryGetFileId(textDocument?.Uri, out NonNullable<string> docKey)) return null;
            var isSource = this.FileContentManagers.TryGetValue(docKey, out FileContentManager file);
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
        public PublishDiagnosticParams[] GetDiagnostics(TextDocumentIdentifier textDocument = null) =>
            this.FlushAndExecute(() =>
                textDocument != null
                    ? this.FileQuery(textDocument, (file, _) => new PublishDiagnosticParams[] { file.Diagnostics() })
                    : this.FileContentManagers.Values.Select(file => file.Diagnostics()).ToArray());

        /// <summary>
        /// Returns a sequence of all source files that are currently contained in this compilation unit.
        /// Note: this method waits for all currently running or queued tasks to finish 
        /// before constructing the requested information by calling FlushAndExecute.
        /// </summary>
        public IEnumerable<Uri> GetSourceFiles() =>
            this.FlushAndExecute(() => this.FileContentManagers.Keys.Select(id => new Uri(id.Value)).ToImmutableArray().AsEnumerable());

        /// <summary>
        /// Returns the current file content (text representation) in memory.
        /// Returns null if the given file is not listed as source file.
        /// Note: this method waits for all currently running or queued tasks to finish 
        /// before getting the file content by calling FlushAndExecute.
        /// </summary>
        public string[] FileContentInMemory(TextDocumentIdentifier textDocument) =>
            this.FlushAndExecute(() =>
                this.FileQuery(textDocument, (file, _) => file.GetLines(0, file.NrLines()).Select(line => line.Text).ToArray()));

        /// <summary>
        /// Returns the current tokenization of the file content in memory.
        /// Each returned array item contains the array of tokens on the line with the corresponding index. 
        /// Returns null if the given file is not listed as source file.
        /// Note: this method waits for all currently running or queued tasks to finish 
        /// before getting the file content by calling FlushAndExecute.
        /// </summary>
        public QsFragmentKind[][] GetTokenization(TextDocumentIdentifier textDocument) =>
            this.FlushAndExecute(() =>
                this.FileQuery(textDocument, (file, _) => file.GetTokenizedLines(0, file.NrLines()).Select(line => line.Select(frag => frag.Kind).ToArray()).ToArray()));

        /// <summary>
        /// Returns the syntax tree for the current state of the compilation.
        /// Note: this method waits for all currently running or queued tasks to finish 
        /// before constructing the syntax tree by calling FlushAndExecute.
        /// </summary>
        public IEnumerable<QsNamespace> GetSyntaxTree() =>
            this.FlushAndExecute(() => this.CompilationUnit.Build().Namespaces.AsEnumerable());

        /// <summary>
        /// Returns a Compilation object containing all information about the current state of the compilation. 
        /// Note: this method waits for all currently running or queued tasks to finish 
        /// before constructing the Compilation object by calling FlushAndExecute.
        /// </summary>
        public Compilation Build() => 
            this.FlushAndExecute(() => new Compilation(this));

        /// <summary>
        /// Class used to accumulate all information about the state of a compilation unit in immutable form. 
        /// </summary>
        public class Compilation
        {
            /// <summary>
            /// Contains the file IDs assigned by the Q# compiler for all source files included in the compilation. 
            /// </summary>
            public readonly ImmutableHashSet<NonNullable<string>> SourceFiles;
            /// <summary>
            /// Contains the IDs assigned by the Q# compiler for all assemblies referenced in the compilation. 
            /// </summary>
            public readonly ImmutableHashSet<NonNullable<string>> References;

            /// <summary>
            /// Contains a dictionary that maps the ID of a file included in the compilation 
            /// to the text representation of its content. 
            /// </summary>
            public readonly ImmutableDictionary<NonNullable<string>, ImmutableArray<string>> FileContent;
            /// <summary>
            /// Contains a dictionary that maps the ID of a file included in the compilation 
            /// to the tokenization built based on its content. 
            /// </summary>
            public readonly ImmutableDictionary<NonNullable<string>, ImmutableArray<ImmutableArray<CodeFragment>>> Tokenization;
            /// <summary>
            /// Contains a dictionary that maps the name of a namespace to the compiled Q# namespace.
            /// </summary>
            public readonly ImmutableDictionary<NonNullable<string>, QsNamespace> SyntaxTree;
            /// <summary>
            /// Contains the built Q# compilation.
            /// </summary>
            public readonly QsCompilation BuiltCompilation; 

            /// <summary>
            /// Contains a dictionary that maps the name of each namespace defined in the compilation to a look-up 
            /// containing the names and corresponding short form (if any) of all opened namespaces for that (part of the) namespace in a particular source file. 
            /// </summary>
            private readonly ImmutableDictionary<NonNullable<string>, ILookup<NonNullable<string>, (NonNullable<string>, string)>> OpenDirectivesForEachFile;
            /// <summary>
            /// Contains a dictionary that given the ID of a file included in the compilation 
            /// returns all tokenized code fragments containing namespace declarations in that file.
            /// </summary>
            private readonly ImmutableDictionary<NonNullable<string>, ImmutableArray<CodeFragment>> NamespaceDeclarations;
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
            public readonly ImmutableDictionary<NonNullable<string>, ImmutableArray<Diagnostic>> ScopeDiagnostics;
            /// <summary>
            /// Contains a dictionary that maps the ID of a file included in the compilation 
            /// to all syntax-related diagnostics generated during compilation. 
            /// </summary>
            public readonly ImmutableDictionary<NonNullable<string>, ImmutableArray<Diagnostic>> SyntaxDiagnostics;
            /// <summary>
            /// Contains a dictionary that maps the ID of a file included in the compilation 
            /// to all context-related diagnostics generated during compilation. 
            /// </summary>
            public readonly ImmutableDictionary<NonNullable<string>, ImmutableArray<Diagnostic>> ContextDiagnostics;
            /// <summary>
            /// Contains a dictionary that maps the ID of a file included in the compilation 
            /// to all diagnostics generated during compilation related to header information for declarations. 
            /// </summary>
            public readonly ImmutableDictionary<NonNullable<string>, ImmutableArray<Diagnostic>> HeaderDiagnostics;
            /// <summary>
            /// Contains a dictionary that maps the ID of a file included in the compilation 
            /// to all semantic diagnostics generated during compilation for the specified implementations. 
            /// </summary>
            public readonly ImmutableDictionary<NonNullable<string>, ImmutableArray<Diagnostic>> SemanticDiagnostics;

            /// <summary>
            /// Maps a file ID assigned by the Q# compiler to all diagnostics generated during compilation. 
            /// Returns an empty sequence if no file with the given ID has been included in the compilation. 
            /// </summary>
            public IEnumerable<Diagnostic> Diagnostics(NonNullable<string> file) =>
                this.SourceFiles.Contains(file) ? 
                    ScopeDiagnostics[file]
                        .Concat(SyntaxDiagnostics[file])
                        .Concat(ContextDiagnostics[file])
                        .Concat(HeaderDiagnostics[file])
                        .Concat(SemanticDiagnostics[file]) :
                    Enumerable.Empty<Diagnostic>();

            /// <summary>
            /// Returns all diagnostics generated during compilation.
            /// </summary>
            public IEnumerable<Diagnostic> Diagnostics() =>
                ScopeDiagnostics.Values
                    .Concat(SyntaxDiagnostics.Values)
                    .Concat(ContextDiagnostics.Values)
                    .Concat(HeaderDiagnostics.Values)
                    .Concat(SemanticDiagnostics.Values)
                    .SelectMany(d => d);

            /// <summary>
            /// If a source file with the given name exists in the compilation, and if there is exactly one (partial) namespace with the given name, 
            /// returns the (non-documenting) comments associated with that namespace declaration. 
            /// Returns a set of empty comments otherwise. 
            /// </summary>
            public QsComments NamespaceComments(NonNullable<string> sourceFile, NonNullable<string> nsName)
            {
                if (!this.NamespaceDeclarations.TryGetValue(sourceFile, out ImmutableArray<CodeFragment> namespaces)) return QsComments.Empty;
                var declarations = namespaces.Where(token => token.Kind?.DeclaredNamespaceName(null) == nsName.Value);
                return declarations.Count() == 1 ? declarations.Single().Comments : QsComments.Empty;
            }

            /// <summary>
            /// Given ID of a source file and the name of a namespace, 
            /// returns the names and corresponding short form (if any) of all opened namespaces for the (part of the) namespace in that file. 
            /// Returns an empy sequence if no source file with the given ID and/or namespace with the given name exists in the compilation. 
            /// </summary>
            public IEnumerable<(NonNullable<string>, string)> OpenDirectives(NonNullable<string> sourceFile, NonNullable<string> nsName) =>
                this.OpenDirectivesForEachFile
                    .TryGetValue(nsName, out ILookup<NonNullable<string>, (NonNullable<string>, string)> lookUp) 
                        ? lookUp[sourceFile]
                        : Enumerable.Empty<(NonNullable<string>, string)>();
            /// <summary>
            /// Returns all the names of all callable and types defined in the namespace with the given name.
            /// The returned names are unique and do not contain duplications e.g. for types and the corresponding constructor. 
            /// Returns an empty sequence if no namespace with the given name exists in the compilation. 
            /// </summary>
            public IEnumerable<NonNullable<string>> SymbolsDefinedInNamespace(NonNullable<string> nsName) =>
                this.SyntaxTree.TryGetValue(nsName, out QsNamespace ns) 
                    ? ns.Elements.Select(element => (element is QsNamespaceElement.QsCallable c) ? c.Item.FullName.Name.Value : null)
                        .Where(name => name != null).Select(name => NonNullable<string>.New(name)) 
                    : Enumerable.Empty<NonNullable<string>>();


            internal Compilation(CompilationUnitManager manager)
            {
                try
                {
                    this.BuiltCompilation = manager.CompilationUnit.Build();
                    this.SourceFiles = manager.FileContentManagers.Keys.ToImmutableHashSet();
                    this.References = manager.CompilationUnit.Externals.Declarations.Keys.ToImmutableHashSet();

                    this.FileContent = this.SourceFiles
                        .Select(file => (file, manager.FileContentManagers[file].GetLines().Select(line => line.Text).ToImmutableArray()))
                        .ToImmutableDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
                    this.Tokenization = this.SourceFiles
                        .Select(file => (file, manager.FileContentManagers[file].GetTokenizedLines().Select(line => line.Select(frag => frag.Copy()).ToImmutableArray()).ToImmutableArray()))
                        .ToImmutableDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
                    this.SyntaxTree = this.BuiltCompilation.Namespaces.ToImmutableDictionary(ns => ns.Name);

                    this.OpenDirectivesForEachFile = this.SyntaxTree.Keys.ToImmutableDictionary(
                        nsName => nsName, 
                        nsName => manager.CompilationUnit.GetOpenDirectives(nsName));
                    this.NamespaceDeclarations = this.SourceFiles
                        .Select(file => (file, manager.FileContentManagers[file].NamespaceDeclarationTokens().Select(t => t.GetFragmentWithClosingComments()).ToImmutableArray()))
                        .ToImmutableDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
                    this.Callables = this.SyntaxTree.Values.GlobalCallableResolutions();
                    this.Types = this.SyntaxTree.Values.GlobalTypeResolutions();

                    this.ScopeDiagnostics = this.SourceFiles
                        .Select(file => (file, manager.FileContentManagers[file].CurrentScopeDiagnostics()))
                        .ToImmutableDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
                    this.SyntaxDiagnostics = this.SourceFiles
                        .Select(file => (file, manager.FileContentManagers[file].CurrentSyntaxDiagnostics()))
                        .ToImmutableDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
                    this.ContextDiagnostics = this.SourceFiles
                        .Select(file => (file, manager.FileContentManagers[file].CurrentContextDiagnostics()))
                        .ToImmutableDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
                    this.HeaderDiagnostics = this.SourceFiles
                        .Select(file => (file, manager.FileContentManagers[file].CurrentHeaderDiagnostics()))
                        .ToImmutableDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
                    this.SemanticDiagnostics = this.SourceFiles
                        .Select(file => (file, manager.FileContentManagers[file].CurrentSemanticDiagnostics()))
                        .ToImmutableDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
                }
                catch (Exception ex) { manager.LogException(ex); }
            }
        }
    }
}
