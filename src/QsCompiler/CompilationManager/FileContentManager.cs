// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Lsp = Microsoft.VisualStudio.LanguageServer.Protocol;
using Position = Microsoft.Quantum.QsCompiler.DataTypes.Position;
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    // NOTE: The idea throughout this whole class is that anything inside this class can *only*
    // be modified by the class itself! (not just on a shallow, but on a deep level!)

    /// <summary>
    /// Any read-only access to this class is threadsafe, however write access (currently) is not!
    /// </summary>
    public class FileContentManager : IDisposable
    {
        internal readonly Uri Uri;
        public readonly string FileName;
        private readonly ManagedList<CodeLine> content;
        private readonly ManagedList<ImmutableArray<CodeFragment>> tokens;
        private readonly FileHeader header;

        /// <summary>
        /// list of unprocessed updates that are all limited to the same (single) line
        /// </summary>
        private readonly Queue<TextDocumentContentChangeEvent> unprocessedUpdates;

        /// <summary>
        /// contains the line numbers in the current content that have been modified
        /// </summary>
        private readonly ManagedSortedSet editedContent;

        /// <summary>
        /// contains the line numbers in the current token list that have been modified
        /// </summary>
        private readonly ManagedSortedSet editedTokens;

        /// <summary>
        /// contains the qualified names of the callables for which content has been modified
        /// </summary>
        private readonly ManagedList<QsQualifiedName> editedCallables;

        // properties containing different kinds of diagnostics:

        private readonly ManagedList<Diagnostic> scopeDiagnostics;
        private readonly ManagedList<Diagnostic> syntaxDiagnostics;
        private readonly ManagedList<Diagnostic> contextDiagnostics;
        private readonly ManagedList<Diagnostic> semanticDiagnostics;
        private readonly ManagedList<Diagnostic> headerDiagnostics;

        /// <summary>
        /// used to store partially computed semantic diagnostics until they are ready for publishing
        /// </summary>
        private readonly ManagedList<Diagnostic> updatedSemanticDiagnostics;

        /// <summary>
        /// used to store partially computed header diagnostics until they are ready for publishing
        /// </summary>
        private readonly ManagedList<Diagnostic> updatedHeaderDiagnostics;

        // locks and other stuff used coordinate:

        /// <summary>
        /// used as sync root for all managed data structures
        /// </summary>
        internal readonly ReaderWriterLockSlim SyncRoot;

        /// <summary>
        /// used to periodically trigger processing the queued changes if no further editing takes place for a while
        /// </summary>
        private readonly System.Timers.Timer timer;

        internal void AddTimerTriggeredUpdateEvent() => this.timer.Start();

        // events and event handlers

        /// <summary>
        /// publish an event to notify all subscribers when the timer for queued changes expires
        /// </summary>
        internal event TimerTriggeredUpdate? TimerTriggeredUpdateEvent;

        internal delegate Task TimerTriggeredUpdate(Uri file);

        /// <summary>
        /// publish an event to notify all subscribers when the entire type checking needs to be re-run
        /// </summary>
        internal event GlobalTypeChecking? GlobalTypeCheckingEvent;

        internal delegate Task GlobalTypeChecking();

        internal void TriggerGlobalTypeChecking() =>
            this.GlobalTypeCheckingEvent?.Invoke();

        // constructors, "destructors" & property access:

        internal FileContentManager(Uri uri, string fileName)
        {
            this.SyncRoot = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            this.Uri = uri;
            this.FileName = fileName;

            this.content = new ManagedList<CodeLine>(this.SyncRoot);
            this.content.Add(CodeLine.Empty()); // each new file per default has one line without text
            this.tokens = new ManagedList<ImmutableArray<CodeFragment>>(this.SyncRoot);
            this.tokens.Add(ImmutableArray<CodeFragment>.Empty);
            this.header = new FileHeader(this.SyncRoot);

            this.unprocessedUpdates = new Queue<TextDocumentContentChangeEvent>();
            this.editedContent = new ManagedSortedSet(this.SyncRoot);
            this.editedTokens = new ManagedSortedSet(this.SyncRoot);
            this.editedCallables = new ManagedList<QsQualifiedName>(this.SyncRoot);

            this.scopeDiagnostics = new ManagedList<Diagnostic>(this.SyncRoot);
            this.syntaxDiagnostics = new ManagedList<Diagnostic>(this.SyncRoot);
            this.contextDiagnostics = new ManagedList<Diagnostic>(this.SyncRoot);
            this.semanticDiagnostics = new ManagedList<Diagnostic>(this.SyncRoot);
            this.headerDiagnostics = new ManagedList<Diagnostic>(this.SyncRoot);
            this.updatedSemanticDiagnostics = new ManagedList<Diagnostic>(this.SyncRoot);
            this.updatedHeaderDiagnostics = new ManagedList<Diagnostic>(this.SyncRoot);

            // in order to improve the editor experience it is best to not publish new diagnostics on every keystroke
            // instead we will only queue changes under certain circumstances
            // however, to make sure that queued changes are processed after a certain time of inactivity in the editor
            // (or rather no file changes) we will use a timer to trigger automatic updates
            // -> the timer is started when an update is queued, and reset whenever an update takes place
            this.timer = new System.Timers.Timer(500);
            this.timer.Elapsed += (_, __) => this.TimerTriggeredUpdateEvent?.Invoke(this.Uri);
            this.timer.AutoReset = false; // let's restart manually when we queue changes
            this.timer.Enabled = false; // the timer will be started when a change is queued
        }

        /// <summary>
        /// De-registers the sync root of this FileContentManager as a dependent lock for compilation unit associated with this file.
        /// </summary>
        public void Dispose()
        {
            this.timer.Dispose();
            this.SyncRoot.Dispose();
        }

        // diagnostics access and management

        /// <summary>
        /// Returns a copy of the current scope diagnostics.
        /// </summary>
        internal ImmutableArray<Diagnostic> CurrentScopeDiagnostics() =>
            this.scopeDiagnostics.Get().Select(m => m.Copy()).ToImmutableArray();

        /// <summary>
        /// Returns a copy of the current syntax diagnostics.
        /// </summary>
        internal ImmutableArray<Diagnostic> CurrentSyntaxDiagnostics() =>
            this.syntaxDiagnostics.Get().Select(m => m.Copy()).ToImmutableArray();

        /// <summary>
        /// Returns a copy of the current context diagnostics.
        /// </summary>
        internal ImmutableArray<Diagnostic> CurrentContextDiagnostics() =>
            this.contextDiagnostics.Get().Select(m => m.Copy()).ToImmutableArray();

        /// <summary>
        /// Returns a copy of the current header diagnostics.
        /// </summary>
        internal ImmutableArray<Diagnostic> CurrentHeaderDiagnostics() =>
            this.headerDiagnostics.Get().Select(m => m.Copy()).ToImmutableArray();

        /// <summary>
        /// Returns a copy of the current semantic diagnostics.
        /// </summary>
        internal ImmutableArray<Diagnostic> CurrentSemanticDiagnostics() =>
            this.semanticDiagnostics.Get().Select(m => m.Copy()).ToImmutableArray();

        /// <summary>
        /// Given the position where the syntax check starts and ends relative to the original file content before the update, and the lineNrChange,
        /// removes all diagnostics that are no longer valid due to that change, and
        /// updates the line numbers of the remaining diagnostics if needed.
        /// </summary>
        private static void InvalidateOrUpdateBySyntaxCheckDelimeters(ManagedList<Diagnostic> diagnostics, Range syntaxCheckDelimiters, int lineNrChange)
        {
            Diagnostic UpdateLineNrs(Diagnostic m) => m.SelectByStart(syntaxCheckDelimiters.End) ? m.WithLineNumOffset(lineNrChange) : m;
            diagnostics.SyncRoot.EnterWriteLock();
            try
            {
                // remove all cycle related diagnostics
                diagnostics.RemoveAll(DiagnosticTools.ErrorType(ErrorCode.InvalidCyclicTypeParameterResolution));
                // remove any Diagnostic overlapping with the updated interval
                diagnostics.RemoveAll(m => m.SelectByStart(syntaxCheckDelimiters) || m.SelectByEnd(syntaxCheckDelimiters));
                // these are also no longer valid
                diagnostics.RemoveAll(m => m.SelectByStart(Range.Create(Position.Zero, syntaxCheckDelimiters.Start)) && m.SelectByEnd(syntaxCheckDelimiters.End));
                if (lineNrChange != 0)
                {
                    diagnostics.Transform(UpdateLineNrs);
                }
            }
            finally
            {
                diagnostics.SyncRoot.ExitWriteLock();
            }
        }

        /// <summary>
        /// Updates the line numbers of diagnostics that start after the syntax check end delimiter in both lists of diagnostics,
        /// and removes all diagnostics that overlap with the given range for the syntax check update in the updated diagnostics only.
        /// </summary>
        /// <exception cref="ArgumentException">The given start and end position do not denote a valid range.</exception>
        private static void DelayInvalidateOrUpdate(
            ManagedList<Diagnostic> diagnostics,
            ManagedList<Diagnostic> updated,
            Range syntaxCheckDelimiters,
            int lineNrChange)
        {
            InvalidateOrUpdateBySyntaxCheckDelimeters(updated, syntaxCheckDelimiters, lineNrChange);
            Diagnostic UpdateLineNrs(Diagnostic m) => m.SelectByStart(syntaxCheckDelimiters.End) ? m.WithLineNumOffset(lineNrChange) : m;
            if (lineNrChange != 0)
            {
                diagnostics.Transform(UpdateLineNrs);
            }
        }

        /// <summary>
        /// Adds the given sequence of scope diagnostics to the current list.
        /// </summary>
        internal void AddScopeDiagnostics(IEnumerable<Diagnostic> updates) =>
            this.scopeDiagnostics.AddRange(updates);

        /// <summary>
        /// Given the change specified by start, count and lineNrChange,
        /// removes all diagnostics that are no longer valid due to that change, and
        /// updates the line numbers of the remaining diagnostics if needed.
        /// In paricular, removes any end of file diagnostics (missing closings at the end of the file).
        /// [start, start + count) is the content range that has been updated, resulting in lineNrChange additional lines in that range.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="start"/> or <paramref name="count"/> are negative, or <paramref name="lineNrChange"/>
        /// is smaller than -<paramref name="count"/> or <paramref name="start"/> + <paramref name="count"/> + <paramref name="lineNrChange"/> is larger than the current number of lines.
        /// </exception>
        private void InvalidateOrUpdateScopeDiagnostics(int start, int count, int lineNrChange)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (lineNrChange < -count || start + count + lineNrChange > this.NrLines())
            {
                throw new ArgumentOutOfRangeException(nameof(lineNrChange));
            }

            var end = start + count;
            Diagnostic UpdateLineNrs(Diagnostic m) => m.SelectByStartLine(end) ? m.WithLineNumOffset(lineNrChange) : m;

            this.scopeDiagnostics.SyncRoot.EnterWriteLock();
            try
            {
                this.scopeDiagnostics.RemoveAll(DiagnosticTools.ErrorType(ErrorCode.MissingBracketError, ErrorCode.MissingStringDelimiterError));
                this.scopeDiagnostics.RemoveAll(m => m.SelectByStartLine(start, end) || m.SelectByEndLine(start, end));  // remove any Diagnostic overlapping with the updated interval
                if (lineNrChange != 0)
                {
                    this.scopeDiagnostics.Transform(UpdateLineNrs);
                }
            }
            finally
            {
                this.scopeDiagnostics.SyncRoot.ExitWriteLock();
            }
        }

        /// <summary>
        /// Adds the given sequence of syntax diagnostics to the current list.
        /// </summary>
        internal void AddSyntaxDiagnostics(IEnumerable<Diagnostic> updates) =>
            this.syntaxDiagnostics.AddRange(updates);

        /// <summary>
        /// Given the position where the syntax check starts and ends relative to the original file content before the update, and the lineNrChange,
        /// removes all diagnostics that are no longer valid due to that change, and
        /// updates the line numbers of the remaining diagnostics if needed.
        /// </summary>
        /// <exception cref="ArgumentException">The given start and end position do not denote a valid range.</exception>
        private void InvalidateOrUpdateSyntaxDiagnostics(Range syntaxCheckDelimiters, int lineNrChange) =>
            InvalidateOrUpdateBySyntaxCheckDelimeters(this.syntaxDiagnostics, syntaxCheckDelimiters, lineNrChange);

        /// <summary>
        /// Given the line numbers for which the context diagnostics are now obsolete,
        /// removes all context diagnostics that start on a line marked as obsolete,
        /// and replaces them with the given sequence of context diagnostics.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="obsolete"/> contains a value that is negative.</exception>
        internal void UpdateContextDiagnostics(HashSet<int> obsolete, IEnumerable<Diagnostic> updates)
        {
            if (obsolete.Any() && obsolete.Min() < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(obsolete));
            }

            this.contextDiagnostics.SyncRoot.EnterWriteLock();
            try
            {
                this.contextDiagnostics.RemoveAll(m => obsolete.Contains(m.Range.Start.Line));
                this.contextDiagnostics.AddRange(updates);
            }
            finally
            {
                this.contextDiagnostics.SyncRoot.ExitWriteLock();
            }
        }

        /// <summary>
        /// Given the change specified by start, count and lineNrChange,
        /// removes all diagnostics that start in that range, and
        /// updates the line numbers of the remaining diagnostics if needed.
        /// [start, start + count) is the content range that has been updated, resulting in lineNrChange additional lines in that range.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="start"/> or <paramref name="count"/> are negative, or <paramref name="lineNrChange"/>
        /// is smaller than -<paramref name="count"/> or <paramref name="start"/> + <paramref name="count"/> + <paramref name="lineNrChange"/> is larger than the current number of lines.
        /// </exception>
        private void InvalidateOrUpdateContextDiagnostics(int start, int count, int lineNrChange)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (lineNrChange < -count || start + count + lineNrChange > this.NrLines())
            {
                throw new ArgumentOutOfRangeException(nameof(lineNrChange));
            }

            var end = start + count;
            Diagnostic UpdateLineNrs(Diagnostic m) => m.SelectByStartLine(end) ? m.WithLineNumOffset(lineNrChange) : m;

            this.contextDiagnostics.SyncRoot.EnterWriteLock();
            try
            {
                this.contextDiagnostics.RemoveAll(m => m.SelectByStartLine(start, end));  // remove any Diagnostic overlapping with the updated interval
                if (lineNrChange != 0)
                {
                    this.contextDiagnostics.Transform(UpdateLineNrs);
                }
            }
            finally
            {
                this.contextDiagnostics.SyncRoot.ExitWriteLock();
            }
        }

        /// <summary>
        /// Replaces the header diagnostics with the given sequence and finalizes the header diagnostics update.
        /// </summary>
        internal void ReplaceHeaderDiagnostics(IEnumerable<Diagnostic> updates)
        {
            this.updatedHeaderDiagnostics.ReplaceAll(updates);
            this.headerDiagnostics.ReplaceAll(this.updatedHeaderDiagnostics);
        }

        /// <summary>
        /// Given the position where the syntax check starts and ends relative to the original file content before the update, and the lineNrChange,
        /// removes all diagnostics that are no longer valid due to that change, and
        /// updates the line numbers of the remaining diagnostics if needed.
        /// </summary>
        /// <exception cref="ArgumentException">The given start and end position do not denote a valid range.</exception>
        private void InvalidateOrUpdateHeaderDiagnostics(Range syntaxCheckDelimiters, int lineNrChange) =>
            DelayInvalidateOrUpdate(this.headerDiagnostics, this.updatedHeaderDiagnostics, syntaxCheckDelimiters, lineNrChange);

        /// <summary>
        /// Replaces the semantic diagnostics with the given sequence and finalizes the semantic diagnostics update.
        /// </summary>
        internal void ReplaceSemanticDiagnostics(IEnumerable<Diagnostic> updates)
        {
            this.updatedSemanticDiagnostics.ReplaceAll(updates);
            this.semanticDiagnostics.ReplaceAll(this.updatedSemanticDiagnostics);
        }

        /// <summary>
        /// Adds the given sequence of semantic diagnostics and finalizes the semantic diagnostics update.
        /// </summary>
        internal void AddAndFinalizeSemanticDiagnostics(IEnumerable<Diagnostic> updates)
        {
            this.updatedSemanticDiagnostics.AddRange(updates);
            this.semanticDiagnostics.ReplaceAll(this.updatedSemanticDiagnostics);
        }

        /// <summary>
        /// Given the position where the syntax check starts and ends relative to the original file content before the update, and the lineNrChange,
        /// removes all diagnostics that are no longer valid due to that change, and
        /// updates the line numbers of the remaining diagnostics if needed.
        /// </summary>
        /// <exception cref="ArgumentException">The given start and end position do not denote a valid range.</exception>
        private void InvalidateOrUpdateSemanticDiagnostics(Range syntaxCheckDelimiters, int lineNrChange) =>
            DelayInvalidateOrUpdate(this.semanticDiagnostics, this.updatedSemanticDiagnostics, syntaxCheckDelimiters, lineNrChange);

        /// <summary>
        /// Returns all current diagnostic as PublishDiagnosticParams.
        /// </summary>
        public PublishDiagnosticParams Diagnostics()
        {
            this.SyncRoot.EnterReadLock();
            try
            {
                Diagnostic[] diagnostics =
                    this.CurrentScopeDiagnostics().Concat(
                    this.CurrentSyntaxDiagnostics()).Concat(
                    this.CurrentContextDiagnostics()).Concat(
                    this.CurrentHeaderDiagnostics()).Concat(
                    this.CurrentSemanticDiagnostics()).ToArray();

                return new PublishDiagnosticParams
                {
                    Uri = this.Uri,
                    Diagnostics = diagnostics
                };
            }
            finally
            {
                this.SyncRoot.ExitReadLock();
            }
        }

        // external access to content & routines related to content updates

        /// <summary>
        /// Any modification to the returned elements will not be reflected in file.
        /// </summary>
        internal IEnumerable<CodeLine> GetLines(int index, int count) => this.content.GetRange(index, count);

        internal IEnumerable<CodeLine> GetLines() => this.content.Get();

        /// <summary>
        /// Gets the code line at the given index.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0.</exception>
        /// <exception cref="FileContentException"><paramref name="index"/> exceeds the bounds of the file.</exception>
        internal CodeLine GetLine(int index) =>
            this.content.TryGetItem(index, out var line)
                ? line
                : throw new FileContentException("Index exceeds the bounds of the file.");

        internal int NrLines() => this.content.Count();

        /// <summary>
        /// Verify content update.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> and <paramref name="count"/> are not valid for the current file content, or <paramref name="count"/> is less than 1.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="replacements"/> does not contain at least one element, or the indentation change is non-zero, or a replacement does not have a suitable line ending.
        /// </exception>
        private void VerifyContentUpdate(int start, int count, IReadOnlyList<CodeLine> replacements)
        {
            if (start < 0 || start >= this.NrLines())
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }
            if (count < 1 || start + count > this.NrLines())
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (replacements.Count == 0)
            {
                throw new ArgumentException("expecting at least on replacement");
            }

            // verify that the indentation change is zero
            var indentationChange = ScopeTracking.GetIndentationChange(this, start + count, replacements.Last()); // fine as long as we have at least one replacement
            if (indentationChange != 0)
            {
                throw new ArgumentException("indentation needs to be zero for continuation");
            }
            // Note that the CodeLines themselves are verifies upon initialization - in particular their string delimiters and the positions of excess closing brackets

            // verify that all lines have the necessary line endings
            for (var nr = 0; nr < replacements.Count; ++nr)
            {
                if (start + count == this.NrLines() && nr + 1 == replacements.Count)
                {
                    if (replacements[nr].LineEnding != string.Empty || Utils.EndOfLine.Match(replacements[nr].Text).Success)
                    {
                        throw new ArgumentException("last line in the file must not end in a line ending");
                    }
                }
                else if (replacements[nr].LineEnding == string.Empty || !Utils.EndOfLine.Match(replacements[nr].Text).Success)
                {
                    throw new ArgumentException("missing line ending for a given replacement");
                }
            }
        }

        /// <summary>
        /// Updates the file content in the range [start, start + count) with the given replacements for that range.
        /// Removes all diagnostics that is no longer valid (i.e. any diagnostics that overlaps with that interval), and any end-of-file diagnostics,
        /// and updates the line numbers of the remaining diagnostics.
        /// Marks all lines that need to be re-examined by the language processor as edited (i.e. adds them to EditedLines).
        /// </summary>
        internal void ContentUpdate(int start, int count, IReadOnlyList<CodeLine> replacements)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.VerifyContentUpdate(start, count, replacements);
                var origFileEnd = this.End();

                // update the content (needs to be done first!) and get the syntax check delimiters
                this.content.Replace(start, count, replacements);
                var lineNrChange = replacements.Count - count;
                var syntaxCheckInUpdated = this.GetSyntaxCheckDelimiters(start, replacements.Count);
                var syntaxCheckInOriginal = Range.Create(
                    syntaxCheckInUpdated.Start,
                    syntaxCheckInUpdated.End == this.End()
                        ? origFileEnd
                        : Position.Create(
                            syntaxCheckInUpdated.End.Line - lineNrChange,
                            syntaxCheckInUpdated.End.Column));

                // update the tokens and make sure the necessary connections get marked as edited
                // -> needs to be done *before* updating the tracked line numbers!
                this.RemoveTokensInRange(syntaxCheckInOriginal); // making sure that the corresponding connections are marked as edited
                this.tokens.Replace(start, count, replacements.Select(_ => ImmutableArray<CodeFragment>.Empty).ToList());

                // update all tracked line numbers
                this.editedContent.InvalidateOrUpdate(start, count, lineNrChange);
                this.editedTokens.InvalidateOrUpdate(start, count, lineNrChange);
                this.header.InvalidateOrUpdate(start, count, lineNrChange);

                // mark all lines in the range (syntaxCheck.Start.Line, syntaxCheck.End.Line) as edited
                // -> consider what happens when commenting out the let in "let ..[empty lines] ... valid statment on its own"
                var startCheck = syntaxCheckInUpdated.Start.Line < start ? syntaxCheckInUpdated.Start.Line + 1 : start;
                var edited = Enumerable.Range(startCheck, syntaxCheckInUpdated.End.Line - startCheck);
                QsCompilerError.Verify(edited.Any() || syntaxCheckInUpdated.End.Line == this.NrLines() - 1, "syntax check should not start and end on the same line");
                this.editedContent.Add(edited.Any() ? edited : new int[] { syntaxCheckInUpdated.End.Line });
                if (this.End().Equals(syntaxCheckInUpdated.End))
                {
                    this.editedContent.Add(this.NrLines() - 1);
                }

                // invalidate diagnostics affected by the change, and update the line numbers for the remaining diagnostics
                // NOTE: additional context diagnostics need to be removed for all affected connections (-> done upon dequeuing changes)
                this.InvalidateOrUpdateScopeDiagnostics(start, count, lineNrChange);
                this.InvalidateOrUpdateSyntaxDiagnostics(syntaxCheckInOriginal, lineNrChange);
                this.InvalidateOrUpdateContextDiagnostics(start, count, lineNrChange);
                this.InvalidateOrUpdateHeaderDiagnostics(syntaxCheckInOriginal, lineNrChange);
                this.InvalidateOrUpdateSemanticDiagnostics(syntaxCheckInOriginal, lineNrChange);
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        /// <summary>
        /// Returns all lines that have been modified since the last call to DequeueChanges.
        /// NOTE: The changed lines are guaranteed to be returned in ascending order.
        /// </summary>
        internal SortedSet<int> DequeueContentChanges() =>
            this.editedContent.Clear();

        // external access to tokens & routines related to tokens updates

        /// <summary>
        /// Any modification to the returned elements will not be reflected in file.
        /// </summary>
        internal IEnumerable<ImmutableArray<CodeFragment>> GetTokenizedLines(int index, int count) => this.tokens.GetRange(index, count);

        internal IEnumerable<ImmutableArray<CodeFragment>> GetTokenizedLines() => this.tokens.Get();

        internal ImmutableArray<CodeFragment> GetTokenizedLine(int index) => this.tokens.GetItem(index);

        internal int NrTokenizedLines() => this.tokens.Count();

        /// <summary>
        /// For each CodeFragment in the given collection, verifies its range against the current Content,
        /// verifies that all fragments are ordered according to their range, and
        /// verifies that none of the fragments overlap with existing tokens.
        /// </summary>
        /// <exception cref="ArgumentException">The verification failed.</exception>
        private void VerifyTokenUpdate(IReadOnlyList<CodeFragment> fragments)
        {
            if (fragments.Any(fragment => !this.ContainsRange(fragment.Range)))
            {
                throw new ArgumentException("the range of the given token to update is not a valid range within the current file content");
            }
            ContextBuilder.VerifyTokenOrdering(fragments);

            // check that there are no overlapping fragments
            if (fragments.Select(fragment => fragment.Range).Any(this.ContainsTokensOverlappingWith))
            {
                var fragmentRanges = fragments.Select(t => t.Range);
                var (min, max) = (fragmentRanges.Min(frag => frag.Start.Line), fragmentRanges.Max(frag => frag.End.Line));
                var existing = Enumerable.Range(min, max - min + 1)
                    .SelectMany(lineNr => this.GetTokenizedLine(lineNr).Select(t => t.WithLineNumOffset(lineNr).Range.DiagnosticString() + $": {t.Text}"));
                throw new ArgumentException("the given fragments to update overlap with existing tokens - \n" +
                    $"Ranges for updates were: \n{string.Join("\n", fragments.Select(t => t.Range.DiagnosticString() + $": {t.Text}"))} \n" +
                    $"Ranges for existing are: \n{string.Join("\n", existing)}");
            }
        }

        /// <summary>
        /// Returns an Action as out parameter that replaces the tokens at lineNr
        /// with the ones returned by UpdateTokens when called on the current tokens
        /// (i.e. the ones after having applied Transformation) on that line.
        /// Applies ModifiedTokens to the tokens at the given lineNr to obtain the list of tokens for which to mark all connections as edited.
        /// Then constructs and returns an Action as out parameter
        /// that adds lineNr as well as all lines containing connections to mark to EditedTokens.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNr"/> is not a valid index for the current Tokens.</exception>
        /// <exception cref="ArgumentException">Any of the values returned by <paramref name="updatedTokens"/> or <paramref name="modifiedTokens"/> is null.</exception>
        private void TransformAndMarkEdited(
            int lineNr,
            Func<ImmutableArray<CodeFragment>, ImmutableArray<CodeFragment>> updatedTokens,
            Func<ImmutableArray<CodeFragment>, ImmutableArray<CodeFragment>> modifiedTokens,
            out Action transformation,
            out Action markEdited)
        {
            this.tokens.SyncRoot.EnterWriteLock();
            try
            {
                if (lineNr < 0 || lineNr >= this.tokens.Count())
                {
                    throw new ArgumentOutOfRangeException(nameof(lineNr));
                }

                transformation = () =>
                {
                    var updated = updatedTokens(this.GetTokenizedLine(lineNr));
                    if (updated == null)
                    {
                        throw new ArgumentException($"{nameof(updatedTokens)} must not return null");
                    }
                    this.tokens.Transform(lineNr, _ => updated);
                };

                var modified = modifiedTokens(this.GetTokenizedLine(lineNr));
                if (modified == null)
                {
                    throw new ArgumentException($"{nameof(modifiedTokens)} must not return null");
                }
                markEdited = () =>
                {
                    this.editedTokens.Add(lineNr);
                    foreach (var token in modified)
                    {
                        var index = this.GetTokenizedLine(lineNr).FindByValue(token);
                        QsCompilerError.Verify(index >= 0, "token not found"); // FIXME: throw a proper error
                        var tokenIndex = new CodeFragment.TokenIndex(this, lineNr, index);
                        var previous = tokenIndex.PreviousOnScope();
                        var next = tokenIndex.NextOnScope();
                        if (previous != null)
                        {
                            this.editedTokens.Add(previous.Line);
                        }
                        if (next != null)
                        {
                            this.editedTokens.Add(next.Line);
                        }
                    }
                };
            }
            finally
            {
                this.tokens.SyncRoot.ExitWriteLock();
            }
        }

        /// <summary>
        /// Given a Range, removes all tokens currently safed in file that overlap with that range, and
        /// marks the lines with the removed tokens as well as any lines that contain connected tokens as edited.
        /// Tokens starting at range.End or ending at range.Start are *not* considered to be overlapping.
        /// Futs a write-lock on the Tokens during the entire routine.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The line number of the <paramref name="range"/> end is larger than the number of currently saved tokens.</exception>
        private void RemoveTokensInRange(Range range)
        {
            this.tokens.SyncRoot.EnterWriteLock();
            try
            {
                if (range.End.Line >= this.tokens.Count())
                {
                    throw new ArgumentOutOfRangeException(nameof(range));
                }

                var applyTransformations = new List<Action>();
                var markEdited = new List<Action>();
                void FilterAndMarkEdited(int index, Func<CodeFragment, bool> predicate)
                {
                    this.TransformAndMarkEdited(
                        index,
                        tokens => tokens.Where(predicate).ToImmutableArray(),
                        tokens => tokens.Where(x => !predicate(x)).ToImmutableArray(),
                        out var transformation,
                        out var edited);
                    applyTransformations.Add(transformation);
                    markEdited.Add(edited);
                }

                var (start, end) = (range.Start.Line, range.End.Line);
                FilterAndMarkEdited(start, ContextBuilder.NotOverlappingWith(range.WithLineNumOffset(-start)));
                for (var i = start + 1; i < end; ++i)
                {
                    FilterAndMarkEdited(i, _ => false); // remove all
                }
                if (start != end)
                {
                    FilterAndMarkEdited(end, ContextBuilder.TokensAfter(Position.Create(0, range.End.Column)));
                }

                var envelopingFragment = this.TryGetFragmentAt(range.Start, out _);
                if (envelopingFragment != null)
                {
                    var envelopeStart = envelopingFragment.Range.Start.Line;
                    FilterAndMarkEdited(envelopeStart, token =>
                        !token.Range.WithLineNumOffset(envelopeStart).Contains(range.Start));
                }

                // which lines get marked as edited depends on the tokens prior to transformation,
                // hence we accumulate all transformations and apply them only at the end
                QsCompilerError.RaiseOnFailure(
                    () =>
                    {
                        foreach (var edited in markEdited)
                        {
                            edited();
                        }
                    },
                    $"marking edited in {nameof(this.RemoveTokensInRange)} failed");
                QsCompilerError.RaiseOnFailure(
                    () =>
                    {
                        foreach (var transformation in applyTransformations)
                        {
                            transformation();
                        }
                    },
                    $"applying transformations in {nameof(this.RemoveTokensInRange)} failed");
            }
            finally
            {
                this.tokens.SyncRoot.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes all Tokens that overlap with any of the given fragments, and adds the given fragments as tokens.
        /// Attaches end of line comments for the lines on which fragments have been modified to suitable tokens.
        /// Verifies the given fragments and
        /// throws the corresponding exception if the verification fails.
        /// </summary>
        internal void TokensUpdate(IReadOnlyList<CodeFragment> fragments)
        {
            if (!fragments.Any())
            {
                return;
            }

            this.SyncRoot.EnterWriteLock();
            try
            {
                this.VerifyTokenUpdate(fragments);

                // update the Header if necessary
                var newNSdecl = FileHeader.FilterNamespaceDeclarations(fragments).Select(fragment => fragment.Range.Start.Line);
                var newOpenDir = FileHeader.FilterOpenDirectives(fragments).Select(fragment => fragment.Range.Start.Line);
                var newTypeDecl = FileHeader.FilterTypeDeclarations(fragments).Select(fragment => fragment.Range.Start.Line);
                var newCallableDecl = FileHeader.FilterCallableDeclarations(fragments).Select(fragment => fragment.Range.Start.Line);

                this.header.AddNamespaceDeclarations(newNSdecl); // fixme: check that these are disjoint sets...
                this.header.AddOpenDirectives(newOpenDir);
                this.header.AddTypeDeclarations(newTypeDecl);
                this.header.AddCallableDeclarations(newCallableDecl);

                // update the Tokens
                var applyTransformations = new List<Action>();
                var markEdited = new List<Action>();
                while (fragments.Any())
                {
                    var startLine = fragments.First().Range.Start.Line;
                    var tokens =
                        fragments.TakeWhile(fragment => fragment.Range.Start.Line == startLine)
                        .Select(token => token.WithLineNumOffset(-startLine)) // token ranges are relative to their start line! (to simplify updating...)
                        .ToImmutableArray();

                    ImmutableArray<CodeFragment> MergeAndAttachComments(ImmutableArray<CodeFragment> current)
                    {
                        // merge tokens and ...
                        var merged = ContextBuilder.MergeTokens(current, tokens).Select(token => token.ClearComments()).ToList();
                        if (!merged.Any())
                        {
                            return merged.ToImmutableArray();
                        }

                        // ... grab all comments associated with the first token and ...
                        var comments = new List<string?>();
                        for (var lineNr = startLine; lineNr > 0 && !this.GetTokenizedLine(--lineNr).Any();)
                        {
                            comments.Add(this.GetLine(lineNr).EndOfLineComment);
                        }
                        comments.Reverse();
                        merged[0] = merged[0].SetOpeningComments(comments); // will be overwritten again if there is just one token
                        if (merged.Count > 1)
                        {
                            comments.Clear();
                        }

                        // ... grab all comments associated with the last token
                        var relevantEndLine = startLine + merged[merged.Count - 1].Range.End.Line;
                        if (relevantEndLine != startLine && this.GetTokenizedLine(relevantEndLine).Any())
                        {
                            --relevantEndLine;
                        }
                        for (var lineNr = startLine; lineNr <= relevantEndLine; ++lineNr)
                        {
                            comments.Add(this.GetLine(lineNr).EndOfLineComment);
                        }
                        merged[merged.Count - 1] = merged[merged.Count - 1].SetOpeningComments(comments);
                        return merged.ToImmutableArray();
                    }

                    this.TransformAndMarkEdited(startLine, MergeAndAttachComments, _ => tokens, out var transformation, out var edited);
                    applyTransformations.Add(transformation);
                    markEdited.Add(edited);
                    fragments = fragments.SkipWhile(fragment => fragment.Range.Start.Line == startLine).ToList();
                }

                // which lines get marked as edited depends on the tokens after the transformations,
                // hence we accumulate all mark-ups and apply them only after applying all transformations
                QsCompilerError.RaiseOnFailure(
                    () =>
                    {
                        foreach (var transformation in applyTransformations)
                        {
                            transformation();
                        }
                    },
                    $"applying transformations in {nameof(this.TokensUpdate)} failed");
                QsCompilerError.RaiseOnFailure(
                    () =>
                    {
                        foreach (var edited in markEdited)
                        {
                            edited();
                        }
                    },
                    $"marking edited in {nameof(this.TokensUpdate)} failed");
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        /// <summary>
        /// Returns all lines that have been modified since the last call to DequeueChanges,
        /// or any lines that contain children of a token on a modified line.
        /// </summary>
        internal SortedSet<int> DequeueTokenChanges()
        {
            this.editedTokens.SyncRoot.EnterWriteLock();
            try
            {
                var currentlyEdited = this.editedTokens.Clear();
                IEnumerable<CodeFragment.TokenIndex> GetTokenIndices(int lineNr) =>
                    Enumerable.Range(0, this.GetTokenizedLine(lineNr).Length).Select(index => new CodeFragment.TokenIndex(this, lineNr, index));

                // get all children for the tokens on the currently edited lines
                var obsolete = currentlyEdited.SelectMany(GetTokenIndices);
                var children = obsolete.SelectMany(tIndex => tIndex.GetChildren());

                // add all lines containing children to the list of currently edited lines
                // -> any issue related to having the wrong parent may now have come up or be resolved for those
                currentlyEdited.UnionWith(children.Select(tIndex => tIndex.Line).ToList());
                return currentlyEdited;
            }
            finally
            {
                this.editedTokens.SyncRoot.ExitWriteLock();
            }
        }

        /// <summary>
        /// Adds the given collection of qualified callables names to the list of callables
        /// whose content has been edited since the last call to this routine.
        /// Invalidates all semantic diagnostics withing the corresponding Ranges.
        /// </summary>
        internal void MarkCallableAsContentEdited(IEnumerable<(Range, QsQualifiedName)> edited)
        {
            foreach (var (range, callableName) in edited)
            {
                InvalidateOrUpdateBySyntaxCheckDelimeters(this.updatedSemanticDiagnostics, range, 0);
                this.editedCallables.Add(callableName);
            }
        }

        /// <summary>
        /// Returns a collection of the fully qualified names of the callables
        /// whose content has been modified since the last call to this routine.
        /// Note that these callable may no longer be defined in the first place.
        /// </summary>
        internal IEnumerable<QsQualifiedName> DequeueContentEditedCallables() =>
            this.editedCallables.Clear();

        // methods for tracking and processing file changes

        /// <summary>
        /// Merges all unprocessed changes computing the new text that is to replace a (single!) line based on the current line content.
        /// Returns that line number and the text to replace that line with as out parameters.
        /// Returns true if there is anything to replace, and false if the queue was empty.
        /// </summary>
        internal bool DequeueUnprocessedChanges(out int lineNr, [NotNullWhen(true)] out string? textToInsert)
        {
            if (!this.unprocessedUpdates.Any())
            {
                // empty queue
                lineNr = -1;
                textToInsert = null;
                return false;
            }
            var change = this.unprocessedUpdates.Peek();
            lineNr = change.Range.Start.Line;
            var newText = this.GetLine(lineNr).Text;

            while (this.unprocessedUpdates.Any())
            {
                change = this.unprocessedUpdates.Dequeue();
                newText = Utils.GetChangedText(newText, change.Range.Start.Character, change.Range.End.Character, change.Text);
            }
            textToInsert = newText;
            return true;
        }

        /// <summary>
        /// Calls all updating routines sequentially, wrapping each one in a QsCompilerError.RaiseOnFailure.
        /// If no change is given or the given change is null, (only) the currently queued changes are to be processed,
        /// otherwise the currently queued changes as well as the given change is processed.
        /// </summary>
        private void Update(TextDocumentContentChangeEvent? change = null)
        {
            this.SyncRoot.EnterUpgradeableReadLock();
            try
            {
                // We need to enforce that generated type checking diagnostics are consistent with all other diagnostics.
                // In particular this means that we cannot have that the type checking runs after the ScopeTracking update completed
                // but before the other updates (that generate the data structure based on which the type checking is done) are done.
                QsCompilerError.RaiseOnFailure(() => this.UpdateScopeTacking(change), "error during scope tracking update");
                QsCompilerError.RaiseOnFailure(() => this.UpdateLanguageProcessing(), "error during language processing update");
                QsCompilerError.RaiseOnFailure(() => this.UpdateContext(), "error during context update");
            }
            finally
            {
                this.SyncRoot.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Does the semantic verification of everything that has not yet been verified.
        /// Updates and all header and semantic diagnostics.
        /// Pushes the updated diagnostics if no further computation is needed.
        /// If a global type checking is needed, triggers the corresponding event
        /// and does not push updates to semantic diagnostic.
        /// </summary>
        internal void Verify(CompilationUnit compilation) =>
            QsCompilerError.RaiseOnFailure(() => this.UpdateTypeChecking(compilation), "error during type checking update");

        /// <summary>
        /// Clears all Header and SemanticDiagnostics,
        /// and marks the content of all currently defined callables as edited, such that it will be verified during the next call to Verify.
        /// </summary>
        internal void ClearVerification()
        {
            this.SyncRoot.EnterUpgradeableReadLock();
            try
            {
                var edited = this.CallablesWithContentModifications(Enumerable.Range(0, this.NrLines()));
                this.MarkCallableAsContentEdited(edited);
                this.headerDiagnostics.Clear();
                this.updatedHeaderDiagnostics.Clear();
                this.semanticDiagnostics.Clear();
                this.updatedSemanticDiagnostics.Clear();
            }
            finally
            {
                this.SyncRoot.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Given a TextDocumentContentChangeEvent, determines whether it is necessary to immediately update the file,
        /// updates the file if necessary, and queues the change otherwise.
        /// An update is considered necessary if the given change replaces more than one line of the current content,
        /// or if the inserted text cannot be a symbol or keyword (i.e. includes any whitespace, numbers and/or special characters).
        /// Sets the out parameter to true if diagnostics are to be published.
        /// </summary>
        /// <exception cref="ArgumentException">The range of <paramref name="change"/> is invalid.</exception>
        internal void PushChange(TextDocumentContentChangeEvent change, out bool publishDiagnostics)
        {
            // NOTE: since there may be still unprocessed changes aggregated in UnprocessedChanges we cannot verify the range of the change against the current file content,
            // however, let's at least check that all entries are positive, and the range start is smaller than or equal to the range end

            this.timer.Stop(); // will be restarted if needed
            var range = change.Range.ToQSharp();
            var count = range.End.Line - range.Start.Line + 1;
            var line = this.unprocessedUpdates.Any()
                ? this.unprocessedUpdates.Peek().Range.Start.Line
                : range.Start.Line;
            publishDiagnostics = true;
            if (count == 1 && line == range.Start.Line)
            {
                this.unprocessedUpdates.Enqueue(change);

                var trimmedText = change.Text.TrimStart(); // tabs etc inserted by the editor come squashed together with the next inserted character
                if (change.Text == string.Empty ||
                    // let's not immediately trigger an update for these, hoping the matching one will come right after
                    trimmedText == "{" || trimmedText == "\"" ||
                    // ... and the same here
                    trimmedText == "\\" || trimmedText == "/")
                {
                    this.timer.Start(); // we can simply queue this update - no need to actually execute it
                    publishDiagnostics = false;
                    return;
                }

                // For changes that are part of typing a symbol, the file needs to be updated immediately for code
                // completion, but diagnostics should be delayed until the user has a chance to finish typing.
                if (Utils.ValidAsSymbol.IsMatch(trimmedText))
                {
                    publishDiagnostics = false;
                }

                this.Update();
            }
            else
            {
                this.Update(change);
            }
        }

        /// <summary>
        /// Replaces the entire file content with the given text.
        /// Forces the update to be processed rather than queued.
        /// </summary>
        internal void ReplaceFileContent(string text)
        {
            this.SyncRoot.EnterUpgradeableReadLock();
            try
            {
                var change = new TextDocumentContentChangeEvent
                {
                    Range = new Lsp.Range { Start = new Lsp.Position(), End = this.End().ToLsp() },
                    // fixme: range length is not accurate, but also not used...
                    RangeLength = 0,
                    Text = text
                };
                this.PushChange(change, out bool processed);
                if (!processed)
                {
                    this.Flush();
                }
            }
            finally
            {
                this.SyncRoot.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Forces the processing of all currently queued changes,
        /// *without* calling the constructor onAutomaticUpdate argument with the update.
        /// </summary>
        internal void Flush()
        {
            this.timer.Stop();
            this.Update();
        }

        // external access to the file header and other file properties

        /// <summary>
        /// Given a function returning an int array of line numbers,
        /// applies FilterBy to the tokens on any of those lines and returns the TokenIndex of the tokens for which FilterBy returned true as IEnumerable.
        /// If FilterBy is null, returns an IEnumerable with the token indices for all tokens on the lines specified by GetLineNumbers.
        /// Returns an empty List if GetLineNumbers is null.
        /// </summary>
        private List<CodeFragment.TokenIndex> FilterTokenIndices(Func<int[]> getLineNumbers, Func<CodeFragment, bool>? filterBy = null)
        {
            if (getLineNumbers == null)
            {
                return new List<CodeFragment.TokenIndex>();
            }
            bool Filter(CodeFragment frag) => filterBy == null ? true : filterBy(frag);
            this.SyncRoot.EnterReadLock();
            try
            {
                IEnumerable<CodeFragment.TokenIndex> GetFiltered(int lineNr) =>
                    Enumerable.Range(0, this.GetTokenizedLine(lineNr).Length)
                        .Select(i => new CodeFragment.TokenIndex(this, lineNr, i))
                        .Where(tokenIndex => Filter(tokenIndex.GetFragment()));
                return getLineNumbers().SelectMany(GetFiltered).ToList();
            }
            finally
            {
                this.SyncRoot.ExitReadLock();
            }
        }

        /// <summary>
        /// Given a function returning an int array of line numbers,
        /// applies FilterBy to the fragments on any of those lines and returns the resulting flattened fragment sequence as IEnumerable.
        /// Note that the range of the returned fragments is the absolute range in the file.
        /// If FilterBy is null, returns an IEnumerable with all fragments on the lines specified by GetLineNumbers.
        /// Returns an empty List if GetLineNumbers is null.
        /// </summary>
        private List<CodeFragment> FilterFragments(Func<int[]> getLineNumbers, Func<CodeFragment, bool>? filterBy = null)
        {
            this.SyncRoot.EnterReadLock();
            try
            {
                return this.FilterTokenIndices(getLineNumbers, filterBy).Select(tokenIndex => tokenIndex.GetFragment()).ToList();
            }
            finally
            {
                this.SyncRoot.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns a list with all token indices corresponding to namespace declarations.
        /// </summary>
        internal List<CodeFragment.TokenIndex> NamespaceDeclarationTokens() =>
            this.FilterTokenIndices(this.header.GetNamespaceDeclarations, FileHeader.IsNamespaceDeclaration);

        /// <summary>
        /// Returns a list with all token indices corresponding to open directives.
        /// </summary>
        internal List<CodeFragment.TokenIndex> OpenDirectiveTokens() =>
            this.FilterTokenIndices(this.header.GetOpenDirectives, FileHeader.IsOpenDirective);

        /// <summary>
        /// Returns a list with all token indices corresponding to type declarations.
        /// </summary>
        internal List<CodeFragment.TokenIndex> TypeDeclarationTokens() =>
            this.FilterTokenIndices(this.header.GetTypeDeclarations, FileHeader.IsTypeDeclaration);

        /// <summary>
        /// Returns a list with all token indices corresponding to operation or function declarations.
        /// </summary>
        internal List<CodeFragment.TokenIndex> CallableDeclarationTokens() =>
            this.FilterTokenIndices(this.header.GetCallableDeclarations, FileHeader.IsCallableDeclaration);

        /// <summary>
        /// Returns all namespace declarations in the file sorted by the line number they are declared on.
        /// </summary>
        public IEnumerable<(string, Range)> GetNamespaceDeclarations()
        {
            var decl = this.FilterFragments(this.header.GetNamespaceDeclarations, FileHeader.IsNamespaceDeclaration);
            return decl.Select(fragment => (fragment.Kind.DeclaredNamespaceName(InternalUse.UnknownNamespace), fragment.Range))
                .Where(tuple => tuple.Item1 != null);
        }

        /// <summary>
        /// Returns all type declarations in the file sorted by the line number they are declared on.
        /// </summary>
        public IEnumerable<(string, Range)> GetTypeDeclarations()
        {
            var decl = this.FilterFragments(this.header.GetTypeDeclarations, FileHeader.IsTypeDeclaration);
            return decl.Select(fragment => (fragment.Kind.DeclaredTypeName(null), fragment.Range))
                .Where(tuple => tuple.Item1 != null);
        }

        /// <summary>
        /// Returns all callable declarations in the file sorted by the line number they are declared on.
        /// </summary>
        public IEnumerable<(string, Range)> GetCallableDeclarations()
        {
            var decl = this.FilterFragments(this.header.GetCallableDeclarations, FileHeader.IsCallableDeclaration);
            return decl.Select(fragment => (fragment.Kind.DeclaredCallableName(null), fragment.Range))
                .Where(tuple => tuple.Item1 != null);
        }
    }
}
