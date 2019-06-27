// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.VisualStudio.LanguageServer.Protocol;


namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    // NOTE: The idea throughout this whole class is that anything inside this class can *only* 
    // be modified by the class itself! (not just on a shallow, but on a deep level!)

    /// Any read-only access to this class is threadsafe, however write access (currently) is not!
    public class FileContentManager : IDisposable
    {
        internal readonly Uri Uri;
        public readonly NonNullable<string> FileName;
        private readonly ManagedList<CodeLine> Content;
        private readonly ManagedList<ImmutableArray<CodeFragment>> Tokens;
        private readonly FileHeader Header;

        /// list of unprocessed updates that are all limited to the same (single) line
        private readonly Queue<TextDocumentContentChangeEvent> UnprocessedUpdates;
        /// contains the line numbers in the current content that have been modified
        private readonly ManagedSortedSet EditedContent;
        /// contains the line numbers in the current token list that have been modified
        private readonly ManagedSortedSet EditedTokens;
        /// contains the qualified names of the callables for which content has been modified
        private readonly ManagedList<QsQualifiedName> EditedCallables;

        // properties containing different kinds of diagnostics:

        private readonly ManagedList<Diagnostic> ScopeDiagnostics;
        private readonly ManagedList<Diagnostic> SyntaxDiagnostics;
        private readonly ManagedList<Diagnostic> ContextDiagnostics;
        private readonly ManagedList<Diagnostic> HeaderDiagnostics; // always replaced in their entirety, hence split out as a separate list
        private readonly ManagedList<Diagnostic> SemanticDiagnostics;

        // locks and other stuff used coordinate:

        /// used as sync root for all managed data structures
        internal readonly ReaderWriterLockSlim SyncRoot;

        /// used to periodically trigger processing the queued changes if no further editing takes place for a while
        private readonly System.Timers.Timer Timer;
        internal void AddTimerTriggeredUpdateEvent() => this.Timer.Start();

        // events and event handlers

        /// publish an event to notify all subscribers when the timer for queued changes expires 
        internal event TimerTriggeredUpdate TimerTriggeredUpdateEvent;
        internal delegate Task TimerTriggeredUpdate(Uri file);

        /// publish an event to notify all subscribers when the entire type checking needs to be rerun 
        internal event GlobalTypeChecking GlobalTypeCheckingEvent;
        internal delegate Task GlobalTypeChecking();

        internal void TriggerGlobalTypeChecking() =>
            this.GlobalTypeCheckingEvent?.Invoke();

        // constructors, "destructors" & property access:

        internal FileContentManager(Uri uri, NonNullable<string> fileName)
        {
            this.SyncRoot = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            this.Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            this.FileName = fileName;

            this.Content = new ManagedList<CodeLine>(this.SyncRoot);
            this.Content.Add(CodeLine.Empty()); // each new file per default has one line without text
            this.Tokens = new ManagedList<ImmutableArray<CodeFragment>>(this.SyncRoot);
            this.Tokens.Add(ImmutableArray<CodeFragment>.Empty);
            this.Header = new FileHeader(this.SyncRoot);

            this.UnprocessedUpdates = new Queue<TextDocumentContentChangeEvent>();
            this.EditedContent = new ManagedSortedSet(this.SyncRoot);
            this.EditedTokens = new ManagedSortedSet(this.SyncRoot);
            this.EditedCallables = new ManagedList<QsQualifiedName>(this.SyncRoot);

            this.ScopeDiagnostics = new ManagedList<Diagnostic>(this.SyncRoot);
            this.SyntaxDiagnostics = new ManagedList<Diagnostic>(this.SyncRoot);
            this.ContextDiagnostics = new ManagedList<Diagnostic>(this.SyncRoot);
            this.SemanticDiagnostics = new ManagedList<Diagnostic>(this.SyncRoot);
            this.HeaderDiagnostics = new ManagedList<Diagnostic>(this.SyncRoot);

            // in order to improve the editor experience it is best to not publish new diagnostics on every keystroke
            // instead we will only queue changes under certain circumstances
            // however, to make sure that queued changes are processed after a certain time of inactivity in the editor 
            // (or rather no file changes) we will use a timer to trigger automatic updates
            // -> the timer is started when an update is queued, and reset whenever an update takes place 
            this.Timer = new System.Timers.Timer(500);
            this.Timer.Elapsed += (_, __) => this.TimerTriggeredUpdateEvent?.Invoke(this.Uri);
            this.Timer.AutoReset = false; // let's restart manually when we queue changes
            this.Timer.Enabled = false; // the timer will be started when a change is queued
        }

        /// -> deregisters the sync root of this FileContentManager as a dependent lock for compilation unit associated with this file
        public void Dispose()
        {
            this.Timer.Dispose();
            this.SyncRoot.Dispose();
        }


        // diagnostics access and management

        /// returns a copy of the current scope diagnostics 
        internal ImmutableArray<Diagnostic> CurrentScopeDiagnostics() =>
            this.ScopeDiagnostics.Get().Select(m => m.Copy()).ToImmutableArray();

        /// returns a copy of the current syntax diagnostics 
        internal ImmutableArray<Diagnostic> CurrentSyntaxDiagnostics() =>
            this.SyntaxDiagnostics.Get().Select(m => m.Copy()).ToImmutableArray();

        /// returns a copy of the current context diagnostics 
        internal ImmutableArray<Diagnostic> CurrentContextDiagnostics() =>
            this.ContextDiagnostics.Get().Select(m => m.Copy()).ToImmutableArray();

        /// returns a copy of the current header diagnostics 
        internal ImmutableArray<Diagnostic> CurrentHeaderDiagnostics() =>
            this.HeaderDiagnostics.Get().Select(m => m.Copy()).ToImmutableArray();

        /// returns a copy of the current semantic diagnostics
        internal ImmutableArray<Diagnostic> CurrentSemanticDiagnostics() =>
            this.SemanticDiagnostics.Get().Select(m => m.Copy()).ToImmutableArray();


        /// Given the position where the syntax check starts and ends relative to the original file content before the update, and the lineNrChange,
        /// removes all diagnostics that are no longer valid due to that change, and
        /// updates the line numbers of the remaining diagnostics if needed.
        /// Throws an ArgumentNullException if the given diagnostics to update or the syntax check delimiters are null. 
        /// Throws an ArgumentException if the given start and end position do not denote a valid range.
        private void InvalidateOrUpdateBySyntaxCheckDelimeters(ManagedList<Diagnostic> diagnostics, Range syntaxCheckDelimiters, int lineNrChange)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
            if (!Utils.IsValidRange(syntaxCheckDelimiters)) throw new ArgumentException(nameof(syntaxCheckDelimiters));
            var (syntaxCheckStart, syntaxCheckEnd) = (syntaxCheckDelimiters.Start, syntaxCheckDelimiters.End);

            Diagnostic updateLineNrs(Diagnostic m) => m.SelectByStart(syntaxCheckEnd) ? m.WithUpdatedLineNumber(lineNrChange) : m;
            diagnostics.SyncRoot.EnterWriteLock();
            try
            {
                diagnostics.RemoveAll(m => m.SelectByStart(syntaxCheckStart, syntaxCheckEnd) || m.SelectByEnd(syntaxCheckStart, syntaxCheckEnd));  // remove any Diagnostic overlapping with the updated interval
                diagnostics.RemoveAll(m => m.SelectByStart(new Position(0, 0), syntaxCheckStart) && m.SelectByEnd(syntaxCheckEnd)); // these are also no longer valid 
                if (lineNrChange != 0) diagnostics.Transform(updateLineNrs);
            }
            finally { diagnostics.SyncRoot.ExitWriteLock(); }
        }


        /// adds the given sequence of scope diagnostics to the current list
        internal void AddScopeDiagnostics(IEnumerable<Diagnostic> updates) =>
            this.ScopeDiagnostics.AddRange(updates);

        /// Given the change specified by start, count and lineNrChange,
        /// removes all diagnostics that are no longer valid due to that change, and
        /// updates the line numbers of the remaining diagnostics if needed.
        /// In paricular, removes any end of file diagnostics (missing closings at the end of the file).
        /// [start, start + count) is the content range that has been updated, resulting in lineNrChange additional lines in that range.
        /// Throws an ArgumentOutOfRangeException if start or count are negative,
        /// or if lineNrChange is smaller than -count or if start + count + lineNrChange is larger than the current number of lines.
        private void InvalidateOrUpdateScopeDiagnostics(int start, int count, int lineNrChange)
        {
            if (start < 0) throw new ArgumentOutOfRangeException(nameof(start));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (lineNrChange < -count || start + count + lineNrChange > this.NrLines()) throw new ArgumentOutOfRangeException(nameof(lineNrChange));

            var end = start + count;
            Diagnostic updateLineNrs(Diagnostic m) => m.SelectByStartLine(end) ? m.WithUpdatedLineNumber(lineNrChange) : m;

            this.ScopeDiagnostics.SyncRoot.EnterWriteLock();
            try
            {
                this.ScopeDiagnostics.RemoveAll(DiagnosticTools.ErrorType(ErrorCode.MissingBracketError, ErrorCode.MissingStringDelimiterError));
                this.ScopeDiagnostics.RemoveAll(m => m.SelectByStartLine(start, end) || m.SelectByEndLine(start, end));  // remove any Diagnostic overlapping with the updated interval
                if (lineNrChange != 0) this.ScopeDiagnostics.Transform(updateLineNrs);
            }
            finally { this.ScopeDiagnostics.SyncRoot.ExitWriteLock(); }
        }


        /// adds the given sequence of syntax diagnostics to the current list
        internal void AddSyntaxDiagnostics(IEnumerable<Diagnostic> updates) =>
            this.SyntaxDiagnostics.AddRange(updates);

        /// Given the position where the syntax check starts and ends relative to the original file content before the update, and the lineNrChange,
        /// removes all diagnostics that are no longer valid due to that change, and
        /// updates the line numbers of the remaining diagnostics if needed.
        /// Throws an ArgumentNullException if the given diagnostics to update or the syntax check delimiters are null. 
        /// Throws an ArgumentException if the given start and end position do not denote a valid range.
        private void InvalidateOrUpdateSyntaxDiagnostics(Range syntaxCheckDelimiters, int lineNrChange) =>
            this.InvalidateOrUpdateBySyntaxCheckDelimeters(this.SyntaxDiagnostics, syntaxCheckDelimiters, lineNrChange);


        /// Given the line numbers for which the context diagnostics are now obsolete,
        /// removes all context diagnostics that start on a line marked as obsolete, 
        /// and replaces them with the given sequence of context diagnostics.
        /// Throws an ArgumentNullException if the if the given sequence of line numbers for which the context diagnostics are obsolete is null.
        /// Throws an ArgumentOutOfRangeException if that sequence contains a value that is negative.
        internal void UpdateContextDiagnostics(HashSet<int> obsolete, IEnumerable<Diagnostic> updates)
        {
            if (obsolete == null) throw new ArgumentNullException(nameof(obsolete));
            if (obsolete.Any() && obsolete.Min() < 0) throw new ArgumentOutOfRangeException(nameof(obsolete));

            this.ContextDiagnostics.SyncRoot.EnterWriteLock();
            try
            {
                this.ContextDiagnostics.RemoveAll(m => obsolete.Contains(m.Range.Start.Line));
                this.ContextDiagnostics.AddRange(updates);
            }
            finally { this.ContextDiagnostics.SyncRoot.ExitWriteLock(); }
        }

        /// Given the change specified by start, count and lineNrChange,
        /// removes all diagnostics that start in that range, and
        /// updates the line numbers of the remaining diagnostics if needed.
        /// [start, start + count) is the content range that has been updated, resulting in lineNrChange additional lines in that range.
        /// Throws an ArgumentOutOfRangeException if start or count are negative,
        /// or if lineNrChange is smaller than -count or if start + count + lineNrChange is larger than the current number of lines
        private void InvalidateOrUpdateContextDiagnostics(int start, int count, int lineNrChange)
        {
            if (start < 0) throw new ArgumentOutOfRangeException(nameof(start));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (lineNrChange < -count || start + count + lineNrChange > this.NrLines()) throw new ArgumentOutOfRangeException(nameof(lineNrChange));

            var end = start + count;
            Diagnostic updateLineNrs(Diagnostic m) => m.SelectByStartLine(end) ? m.WithUpdatedLineNumber(lineNrChange) : m;

            this.ContextDiagnostics.SyncRoot.EnterWriteLock();
            try
            {
                this.ContextDiagnostics.RemoveAll(m => m.SelectByStartLine(start, end));  // remove any Diagnostic overlapping with the updated interval
                if (lineNrChange != 0) this.ContextDiagnostics.Transform(updateLineNrs);
            }
            finally { this.ContextDiagnostics.SyncRoot.ExitWriteLock(); }
        }


        /// replaces the current header diagnostics with the given sequence
        internal void ReplaceHeaderDiagnostics(IEnumerable<Diagnostic> updates) =>
            this.HeaderDiagnostics.ReplaceAll(updates);

        /// Given the position where the syntax check starts and ends relative to the original file content before the update, and the lineNrChange,
        /// removes all diagnostics that are no longer valid due to that change, and
        /// updates the line numbers of the remaining diagnostics if needed.
        /// Throws an ArgumentNullException if the given diagnostics to update or the syntax check delimiters are null. 
        /// Throws an ArgumentException if the given start and end position do not denote a valid range.
        private void InvalidateOrUpdateHeaderDiagnostics(Range syntaxCheckDelimiters, int lineNrChange) =>
            this.InvalidateOrUpdateBySyntaxCheckDelimeters(this.HeaderDiagnostics, syntaxCheckDelimiters, lineNrChange);


        /// replaces the current semantic diagnostics with the given sequence
        internal void ReplaceSemanticDiagnostics(IEnumerable<Diagnostic> updates) =>
            this.SemanticDiagnostics.ReplaceAll(updates);

        /// adds the given sequence of semantic diagnostics to the current list
        internal void AddSemanticDiagnostics(IEnumerable<Diagnostic> updates) =>
            this.SemanticDiagnostics.AddRange(updates);

        /// Given the position where the syntax check starts and ends relative to the original file content before the update, and the lineNrChange,
        /// removes all diagnostics that are no longer valid due to that change, and
        /// updates the line numbers of the remaining diagnostics if needed.
        /// Throws an ArgumentNullException if the given diagnostics to update or the syntax check delimiters are null. 
        /// Throws an ArgumentException if the given start and end position do not denote a valid range.
        private void InvalidateOrUpdateSemanticDiagnostics(Range syntaxCheckDelimiters, int lineNrChange) =>
            this.InvalidateOrUpdateBySyntaxCheckDelimeters(this.SemanticDiagnostics, syntaxCheckDelimiters, lineNrChange);


        /// returns all current diagnostic as PublishDiagnosticParams
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
            finally { this.SyncRoot.ExitReadLock(); }
        }


        // external access to content & routines related to content updates

        /// any modification to the returned elements will not be reflected in file
        internal IEnumerable<CodeLine> GetLines(int index, int count) => this.Content.GetRange(index, count);
        internal IEnumerable<CodeLine> GetLines() => this.Content.Get();
        internal CodeLine GetLine(int index) => this.Content.GetItem(index);
        internal int NrLines() => this.Content.Count();

        /// Throws an ArgumentNullException if any of the given arguments is null.
        /// Throws an ArgumentOutOfRangeException if start and count are not valid for the current file content, where count needs to be at least one.
        /// Throws an ArgumentException if the replacements do not at least contain one element, or the indentation change is non-zero,
        /// or if a replacement does not have a suitable line ending. 
        private void VerifyContentUpdate(int start, int count, IReadOnlyList<CodeLine> replacements)
        {
            if (start < 0 || start >= this.NrLines()) throw new ArgumentOutOfRangeException(nameof(start));
            if (count < 1 || start + count > this.NrLines()) throw new ArgumentOutOfRangeException(nameof(count));
            // make sure properties are never set to null!
            if (replacements == null) throw new ArgumentNullException(nameof(replacements));
            if (replacements.Count == 0) throw new ArgumentException("expecting at least on replacement");

            // verify that the indentation change is zero
            var indentationChange = ScopeTracking.GetIndentationChange(this, start + count, replacements.Last()); // fine as long as we have at least one replacement
            if (indentationChange != 0) throw new ArgumentException("indentation needs to be zero for continuation");
            // Note that the CodeLines themselves are verifies upon initialization - in particular their string delimiters and the positions of excess closing brackets

            // verify that all lines have the necessary line endings
            for (var nr = 0; nr < replacements.Count; ++nr)
            {
                if (start + count == this.NrLines() && nr + 1 == replacements.Count)
                {
                    if (replacements[nr].LineEnding != String.Empty || Utils.EndOfLine.Match(replacements[nr].Text).Success)
                    { throw new ArgumentException("last line in the file must not end in a line ending"); }
                }
                else if (replacements[nr].LineEnding == String.Empty || !Utils.EndOfLine.Match(replacements[nr].Text).Success)
                { throw new ArgumentException("missing line ending for a given replacement"); }
            }
        }

        /// updates the file content in the range [start, start + count) with the given replacements for that range
        /// removes all diagnostics that is no longer valid (i.e. any diagnostics that overlaps with that interval), and any end-of-file diagnostics,
        /// and updates the line numbers of the remaining diagnostics
        /// marks all lines that need to be re-examined by the language processor as edited (i.e. adds them to EditedLines)
        internal void ContentUpdate(int start, int count, IReadOnlyList<CodeLine> replacements)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.VerifyContentUpdate(start, count, replacements);
                var origFileEnd = this.End();

                // update the content (needs to be done first!) and get the syntax check delimiters
                this.Content.Replace(start, count, replacements);
                var lineNrChange = replacements.Count - count;
                var syntaxCheckInUpdated = this.GetSyntaxCheckDelimiters(start, replacements.Count);
                var syntaxCheckInOriginal = new Range
                {
                    Start = syntaxCheckInUpdated.Start,
                    End = syntaxCheckInUpdated.End == this.End() ? origFileEnd : syntaxCheckInUpdated.End.WithUpdatedLineNumber(-lineNrChange)
                };

                // update the tokens and make sure the necessary connections get marked as edited 
                // -> needs to be done *before* updating the tracked line numbers!
                this.RemoveTokensInRange(syntaxCheckInOriginal); // making sure that the corresponding connections are marked as edited
                this.Tokens.Replace(start, count, replacements.Select(_ => ImmutableArray<CodeFragment>.Empty).ToList());

                // update all tracked line numbers 
                this.EditedContent.InvalidateOrUpdate(start, count, lineNrChange);
                this.EditedTokens.InvalidateOrUpdate(start, count, lineNrChange);
                this.Header.InvalidateOrUpdate(start, count, lineNrChange);

                // mark all lines in the range (syntaxCheck.Start.Line, syntaxCheck.End.Line) as edited
                // -> consider what happens when commenting out the let in "let ..[empty lines] ... valid statment on its own"
                var startCheck = syntaxCheckInUpdated.Start.Line < start ? syntaxCheckInUpdated.Start.Line + 1 : start;
                var edited = Enumerable.Range(startCheck, syntaxCheckInUpdated.End.Line - startCheck);
                QsCompilerError.Verify(edited.Any() || syntaxCheckInUpdated.End.Line == this.NrLines() - 1, "syntax check should not start and end on the same line");
                this.EditedContent.Add(edited.Any() ? edited : new int[] { syntaxCheckInUpdated.End.Line });
                if (this.End().Equals(syntaxCheckInUpdated.End)) this.EditedContent.Add(this.NrLines() - 1);

                // invalidate diagnostics affected by the change, and update the line numbers for the remaining diagnostics
                // NOTE: additional context diagnostics need to be removed for all affected connections (-> done upon dequeuing changes)
                InvalidateOrUpdateScopeDiagnostics(start, count, lineNrChange);
                InvalidateOrUpdateSyntaxDiagnostics(syntaxCheckInOriginal, lineNrChange);
                InvalidateOrUpdateContextDiagnostics(start, count, lineNrChange);
                InvalidateOrUpdateSemanticDiagnostics(syntaxCheckInOriginal, lineNrChange);
                InvalidateOrUpdateHeaderDiagnostics(syntaxCheckInOriginal, lineNrChange);
            }
            finally { this.SyncRoot.ExitWriteLock(); }
        }

        /// returns all lines that have been modified since the last call to DequeueChanges
        /// NOTE: the changed lines are guaranteed to be returned in ascending order
        internal SortedSet<int> DequeueContentChanges() =>
            this.EditedContent.Clear();


        // external access to tokens & routines related to tokens updates

        /// any modification to the returned elements will not be reflected in file
        internal IEnumerable<ImmutableArray<CodeFragment>> GetTokenizedLines(int index, int count) => this.Tokens.GetRange(index, count);
        internal IEnumerable<ImmutableArray<CodeFragment>> GetTokenizedLines() => this.Tokens.Get();
        internal ImmutableArray<CodeFragment> GetTokenizedLine(int index) => this.Tokens.GetItem(index);
        internal int NrTokenizedLines() => this.Tokens.Count();

        /// for each CodeFragment in the given collection, verifies its range against the current Content, 
        /// verifies that all fragments are ordered according to their range, and
        /// verifies that none of the fragments overlap with existing tokens 
        /// throws an ArgumentException if the verification fails
        private void VerifyTokenUpdate(IReadOnlyList<CodeFragment> fragments)
        {
            if (fragments.Any(fragment => !Utils.IsValidRange(fragment.GetRange(), this)))
            { throw new ArgumentException("the range of the given token to update is not a valid range within the current file content"); }
            ContextBuilder.VerifyTokenOrdering(fragments);

            // check that there are no overlapping fragments
            if (fragments.Select(fragment => fragment.GetRange()).Any(this.ContainsTokensOverlappingWith))
            {
                var fragmentRanges = fragments.Select(t => t.GetRange());
                var (min, max) = (fragmentRanges.Min(frag => frag.Start.Line), fragmentRanges.Max(frag => frag.End.Line));
                var existing = Enumerable.Range(min, max - min + 1)
                    .SelectMany(lineNr => this.GetTokenizedLine(lineNr).Select(t => t.WithUpdatedLineNumber(lineNr).GetRange().DiagnosticString() + $": {t.Text}"));
                throw new ArgumentException("the given fragments to update overlap with existing tokens - \n" +
                    $"Ranges for updates were: \n{String.Join("\n", fragments.Select(t => t.GetRange().DiagnosticString() + $": {t.Text}"))} \n" +
                    $"Ranges for existing are: \n{String.Join("\n", existing)}");
            }
        }

        /// Replaces Transformation with an Action that first executes the given Transformation (unless it is null, in which case it is ignored),
        /// and then replaces the tokens at lineNr with the ones returned by UpdateTokens when called on the current tokens 
        /// (i.e. the ones after having applied Transformation) on that line.
        /// Applies ModifiedTokens to the tokens at the given lineNr to obtain the list of tokens for which to mark all connections as edited. 
        /// Then replaces MarkEdited with an Action that first exectutes the given MarkEdited (unless it is null, in which case it is ignored),
        /// and then adds lineNr as well as all lines containing connections to mark to EditedTokens.
        /// Throws an ArgumentNullException if UpdatedTokens or ModifiedTokens is null.
        /// Throws an ArgumentException if any of the values returned by UpdatedTokens or ModifiedTokens is null.
        /// Throws an ArgumentOutOfRangeException if linrNr is not a valid index for the current Tokens.
        private void TransformAndMarkEdited(int lineNr,
            Func<ImmutableArray<CodeFragment>, ImmutableArray<CodeFragment>> UpdatedTokens,
            Func<ImmutableArray<CodeFragment>, ImmutableArray<CodeFragment>> ModifiedTokens,
            ref Action Transformation, ref Action MarkEdited)
        {
            this.Tokens.SyncRoot.EnterWriteLock();
            try
            {
                if (UpdatedTokens == null) throw new ArgumentNullException(nameof(UpdatedTokens));
                if (ModifiedTokens == null) throw new ArgumentNullException(nameof(ModifiedTokens));
                if (lineNr < 0 || lineNr >= this.Tokens.Count()) throw new ArgumentOutOfRangeException(nameof(lineNr));

                var PriorTransformation = Transformation ?? (() => { return; });
                Transformation = () =>
                {
                    PriorTransformation();
                    var updated = UpdatedTokens(this.GetTokenizedLine(lineNr));
                    if (updated == null) throw new ArgumentException($"{nameof(UpdatedTokens)} must not return null");
                    this.Tokens.Transform(lineNr, _ => updated);
                };

                var modified = ModifiedTokens(this.GetTokenizedLine(lineNr));
                if (modified == null) throw new ArgumentException($"{nameof(ModifiedTokens)} must not return null");
                var PriorMarkEdited = MarkEdited ?? (() => { return; });
                MarkEdited = () =>
                {
                    PriorMarkEdited();
                    this.EditedTokens.Add(lineNr);
                    foreach (var token in modified)
                    {
                        var index = this.GetTokenizedLine(lineNr).FindByValue(token);
                        QsCompilerError.Verify(index >= 0, "token not found"); // FIXME: throw a proper error
                        var tokenIndex = new CodeFragment.TokenIndex(this, lineNr, index);
                        var previous = tokenIndex.PreviousOnScope();
                        var next = tokenIndex.NextOnScope();
                        if (previous != null) this.EditedTokens.Add(previous.Line);
                        if (next != null) this.EditedTokens.Add(next.Line);
                    }
                };
            }
            finally { this.Tokens.SyncRoot.ExitWriteLock(); }
        }

        /// given a Range, removes all tokens currently safed in file that overlap with that range, and
        /// marks the lines with the removed tokens as well as any lines that contain connected tokens as edited
        /// tokens starting at range.End or ending at range.Start are *not* considered to be overlapping
        /// puts a write-lock on the Tokens during the entire routine
        /// throws an ArgumentNullException if the given range or it start or end position is null
        /// throws an ArgumentException if the given range is not a valid range
        /// throws an ArgumentOutOfRangeException if the line number of the range end is larger than the number of currently saved tokens
        private void RemoveTokensInRange(Range range)
        {
            this.Tokens.SyncRoot.EnterWriteLock();
            try
            {
                if (!Utils.IsValidRange(range)) throw new ArgumentException("invalid range"); // *don't* verify against the current file content
                if (range.End.Line >= this.Tokens.Count()) throw new ArgumentOutOfRangeException(nameof(range));

                Action ApplyTransformations = null;
                Action MarkEdited = null;
                void FilterAndMarkEdited(int index, Func<CodeFragment, bool> predicate) =>
                    TransformAndMarkEdited(index,
                        tokens => tokens.Where(predicate).ToImmutableArray(),
                        tokens => tokens.Where(x => !predicate(x)).ToImmutableArray(),
                        ref ApplyTransformations, ref MarkEdited);

                var (start, end) = (range.Start.Line, range.End.Line);
                FilterAndMarkEdited(start, ContextBuilder.NotOverlappingWith(range.WithUpdatedLineNumber(-start)));
                for (var i = start + 1; i < end; ++i) FilterAndMarkEdited(i, _ => false); // remove all
                if (start != end) FilterAndMarkEdited(end, ContextBuilder.TokensAfter(new Position(0, range.End.Character)));

                var enveloppingFragment = this.TryGetFragmentAt(range.Start);
                if (enveloppingFragment != null)
                {
                    start = enveloppingFragment.GetRange().Start.Line;
                    FilterAndMarkEdited(start, token => !range.Start.IsWithinRange(token.GetRange().WithUpdatedLineNumber(start)));
                }

                // which lines get marked as edited depends on the tokens prior to transformation, 
                // hence we accumulate all transformations and apply them only at the end 
                QsCompilerError.Verify(ApplyTransformations != null && MarkEdited != null, $"{nameof(ApplyTransformations)} and {nameof(MarkEdited)} in {nameof(RemoveTokensInRange)} should not be null");
                QsCompilerError.RaiseOnFailure(() => MarkEdited(), $"marking edited in {nameof(RemoveTokensInRange)} failed");
                QsCompilerError.RaiseOnFailure(() => ApplyTransformations(), $"applying transformations in {nameof(RemoveTokensInRange)} failed");
            }
            finally { this.Tokens.SyncRoot.ExitWriteLock(); }
        }

        /// Removes all Tokens that overlap with any of the given fragments, and adds the given fragments as tokens.
        /// Attaches end of line comments for the lines on which fragments have been modified to suitable tokens.
        /// Verifies the given fragments and 
        /// throws the corresponding exception if the verification fails.
        /// Throws an ArgumentNullException if any of the given fragments are null.
        internal void TokensUpdate(IReadOnlyList<CodeFragment> fragments)
        {
            if (fragments == null || fragments.Any(x => x == null)) throw new ArgumentNullException(nameof(fragments));
            if (!fragments.Any()) return;

            this.SyncRoot.EnterWriteLock();
            try
            {
                this.VerifyTokenUpdate(fragments);

                // update the Header if necessary                
                var newNSdecl = FileHeader.FilterNamespaceDeclarations(fragments).Select(fragment => fragment.GetRange().Start.Line);
                var newOpenDir = FileHeader.FilterOpenDirectives(fragments).Select(fragment => fragment.GetRange().Start.Line);
                var newTypeDecl = FileHeader.FilterTypeDeclarations(fragments).Select(fragment => fragment.GetRange().Start.Line);
                var newCallableDecl = FileHeader.FilterCallableDeclarations(fragments).Select(fragment => fragment.GetRange().Start.Line);

                this.Header.AddNamespaceDeclarations(newNSdecl); // fixme: check that these are disjoint sets...
                this.Header.AddOpenDirectives(newOpenDir);
                this.Header.AddTypeDeclarations(newTypeDecl);
                this.Header.AddCallableDeclarations(newCallableDecl);

                // update the Tokens
                Action ApplyTransformations = null;
                Action MarkEdited = null;
                while (fragments.Any())
                {
                    var startLine = fragments.First().GetRange().Start.Line;
                    var tokens =
                        fragments.TakeWhile(fragment => fragment.GetRange().Start.Line == startLine)
                        .Select(token => token.WithUpdatedLineNumber(-startLine)) // token ranges are relative to their start line! (to simplify updating...)
                        .ToImmutableArray();

                    ImmutableArray<CodeFragment> MergeAndAttachComments(ImmutableArray<CodeFragment> current)
                    {
                        // merge tokens and ...
                        var merged = ContextBuilder.MergeTokens(current, tokens).Select(token => token.ClearComments()).ToList();
                        if (!merged.Any()) return merged.ToImmutableArray();

                        // ... grab all comments associated with the first token and ...
                        var comments = new List<string>();
                        for (var lineNr = startLine; lineNr > 0 && !this.GetTokenizedLine(--lineNr).Any();)
                        { comments.Add(this.GetLine(lineNr).EndOfLineComment); }
                        comments.Reverse();
                        merged[0] = merged[0].SetOpeningComments(comments); // will be overwritten again if there is just one token
                        if (merged.Count > 1) comments.Clear();

                        // ... grab all comments associated with the last token
                        var relevantEndLine = startLine + merged[merged.Count - 1].GetRange().End.Line;
                        if (relevantEndLine != startLine && this.GetTokenizedLine(relevantEndLine).Any()) --relevantEndLine;
                        for (var lineNr = startLine; lineNr <= relevantEndLine; ++lineNr)
                        { comments.Add(this.GetLine(lineNr).EndOfLineComment); }
                        merged[merged.Count - 1] = merged[merged.Count - 1].SetOpeningComments(comments);
                        return merged.ToImmutableArray();
                    }

                    this.TransformAndMarkEdited(startLine, MergeAndAttachComments, _ => tokens, ref ApplyTransformations, ref MarkEdited);
                    fragments = fragments.SkipWhile(fragment => fragment.GetRange().Start.Line == startLine).ToList();
                }

                // which lines get marked as edited depends on the tokens after the transformations, 
                // hence we accumulate all mark-ups and apply them only after applying all transformations 
                QsCompilerError.RaiseOnFailure(() => ApplyTransformations(), $"applying transformations in {nameof(TokensUpdate)} failed");
                QsCompilerError.RaiseOnFailure(() => MarkEdited(), $"marking edited in {nameof(TokensUpdate)} failed");
            }
            finally { this.SyncRoot.ExitWriteLock(); }
        }

        /// Returns all lines that have been modified since the last call to DequeueChanges, 
        /// or any lines that contain children of a token on a modified line.
        internal SortedSet<int> DequeueTokenChanges()
        {
            this.EditedTokens.SyncRoot.EnterWriteLock();
            try
            {
                var currentlyEdited = this.EditedTokens.Clear();
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
            finally { this.EditedTokens.SyncRoot.ExitWriteLock(); }
        }

        /// Adds the given collection of qualified callables names to the list of callables 
        /// whose content has been edited since the last call to this routine.
        /// Invalidates all semantic diagnostics withing the corresponding Ranges.
        internal void MarkCallableAsContentEdited(IEnumerable<(Range, QsQualifiedName)> edited)
        {
            foreach (var (range, callableName) in edited)
            {
                this.InvalidateOrUpdateSemanticDiagnostics(range, 0);
                this.EditedCallables.Add(callableName);
            }
        }

        /// Returns a collection of the fully qualified names of the callables 
        /// whose content has been modified since the last call to this routine. 
        /// Note that these callable may no longer be defined in the first place. 
        internal IEnumerable<QsQualifiedName> DequeueContentEditedCallables() =>
            this.EditedCallables.Clear();


        // methods for tracking and processing file changes

        /// merges all unprocessed changes computing the new text that is to replace a (single!) line based on the current line content
        /// returns that line number and the text to replace that line with as out parameters
        /// returns true if there is anything to replace, and false if the queue was empty
        internal bool DequeueUnprocessedChanges(out int lineNr, out string textToInsert)
        {
            if (!this.UnprocessedUpdates.Any()) // empty queue
            {
                lineNr = -1;
                textToInsert = null;
                return false;
            }
            var change = this.UnprocessedUpdates.Peek();
            lineNr = change.Range.Start.Line;
            var newText = this.GetLine(lineNr).Text;

            while (this.UnprocessedUpdates.Any())
            {
                change = this.UnprocessedUpdates.Dequeue();
                newText = Utils.GetChangedText(newText, change.Range.Start.Character, change.Range.End.Character, change.Text);
            }
            textToInsert = newText;
            return true;
        }

        /// Calls all updating routines sequentially, wrapping each one in a QsCompilerError.RaiseOnFailure.
        /// If no change is given or the given change is null, (only) the currently queued changes are to be processed, 
        /// otherwise the currently queued changes as well as the given change is processed.
        private void Update(TextDocumentContentChangeEvent change = null)
        {
            this.SyncRoot.EnterUpgradeableReadLock();
            try
            {   // We need to enforce that generated type checking diagnostics are consistent with all other diagnostics.
                // In particular this means that we cannot have that the type checking runs after the ScopeTracking update completed 
                // but before the other updates (that generate the data structure based on which the type checking is done) are done. 
                QsCompilerError.RaiseOnFailure(() => this.UpdateScopeTacking(change), "error during scope tracking update");
                QsCompilerError.RaiseOnFailure(() => this.UpdateLanguageProcessing(), "error during language processing update");
                QsCompilerError.RaiseOnFailure(() => this.UpdateContext(), "error during context update");
            }
            finally { this.SyncRoot.ExitUpgradeableReadLock(); }
        }

        /// Does the semantic verification of everything that has not yet been verified.
        internal void Verify(CompilationUnit compilation) =>
            QsCompilerError.RaiseOnFailure(() => this.UpdateTypeChecking(compilation), "error during type checking update");

        /// Clears all Header and SemanticDiagnostics, 
        /// and marks the content of all currently defined callables as edited, such that it will be verified during the next call to Verify.
        internal void ClearVerification()
        {
            this.SyncRoot.EnterUpgradeableReadLock();
            try
            {
                var edited = this.CallablesWithContentModifications(Enumerable.Range(0, this.NrLines()));
                this.MarkCallableAsContentEdited(edited);
                this.HeaderDiagnostics.Clear();
                this.SemanticDiagnostics.Clear();
            }
            finally { this.SyncRoot.ExitUpgradeableReadLock(); }
        }

        /// Given a TextDocumentContentChangeEvent, determines whether it is necessary to immediately update the file,
        /// updates the file if necessary, and queues the change otherwise.
        /// An update is considered necessary if the given change replaces more than one line of the current content, 
        /// or if the inserted text cannot be a symbol or keyword (i.e. includes any whitespace, numbers and/or special characters).
        /// Sets the out parameter to true, if the given change has merely been queued but has not been processed, and false otherwise. 
        /// Throws an ArgumentNullException if the change or any of its fields are null.
        /// Throws an ArgumentException if the range of the change is invalid.
        internal void PushChange(TextDocumentContentChangeEvent change, out bool queuedChange)
        {
            // NOTE: since there may be still unprocessed changes aggregated in UnprocessedChanges we cannot verify the range of the change against the current file content, 
            // however, let's at least check that nothing is null, all entries are positive, and the range start is smaller than or equal to the range end

            if (change == null) throw new ArgumentNullException(nameof(change));
            if (!Utils.IsValidRange(change.Range)) throw new ArgumentException("range of the given change is invalid");
            this.Timer.Stop(); // will be restarted if needed

            var start = change.Range.Start.Line;
            var count = change.Range.End.Line - start + 1;
            var line = this.UnprocessedUpdates.Any() ? this.UnprocessedUpdates.Peek().Range.Start.Line : start;

            queuedChange = false;
            if (count == 1 && line == start)
            {
                // if the change contains any characters that cannot occur in a symbol, trigger an update
                this.UnprocessedUpdates.Enqueue(change);
                var trimmedText = change.Text.TrimStart(); // tabs etc inserted by the editor come squashed together with the next inserted character
                if (change.Text == String.Empty || 
                    trimmedText == "{" || trimmedText == "\"" || // let's not immediately trigger an update for these, hoping the matching one will come right after
                    trimmedText == "\\" || trimmedText == "/" || // ... and the same here
                    (Utils.ValidAsSymbol.IsMatch(trimmedText) && // Defer updates while the user is typing a symbol ...
                     !Char.IsWhiteSpace(change.Text, 0)))        // ... unless the update came with new indentation (needed for completion)
                {
                    this.Timer.Start(); // we can simply queue this update - no need to actually execute it
                    queuedChange = true;
                    return;
                }
                this.Update(); // update only the currently queued changes
            }
            else this.Update(change);
        }

        /// Replaces the entire file content with the given text.
        /// Forces the update to be processed rather than queued. 
        /// Throws an ArgumentNullException if the given text is null.
        internal void ReplaceFileContent(string text)
        {
            this.SyncRoot.EnterUpgradeableReadLock();
            try
            {
                if (text == null) throw new ArgumentNullException(nameof(text));
                var change = new TextDocumentContentChangeEvent
                { Range = new Range { Start = new Position(0, 0), End = this.End() }, RangeLength = 0, Text = text }; // fixme: range length is not accurate, but also not used...
                this.PushChange(change, out bool queued);
                if (queued) this.Flush();
            }
            finally { this.SyncRoot.ExitUpgradeableReadLock(); }
        }

        /// Forces the processing of all currently queued changes, 
        /// *without* calling the constructor onAutomaticUpdate argument with the update.
        internal void Flush()
        {
            this.Timer.Stop();
            this.Update();
        }


        // external access to the file header and other file properties

        /// Given a function returning an int array of line numbers, 
        /// applies FilterBy to the tokens on any of those lines and returns the TokenIndex of the tokens for which FilterBy returned true as IEnumerable. 
        /// If FilterBy is null, returns an IEnumerable with the token indices for all tokens on the lines specified by GetLineNumbers.
        /// Returns an empty List if GetLineNumbers is null.
        private List<CodeFragment.TokenIndex> FilterTokenIndices(Func<int[]> GetLineNumbers, Func<CodeFragment, bool> FilterBy = null)
        {
            if (GetLineNumbers == null) return new List<CodeFragment.TokenIndex>();
            bool Filter(CodeFragment frag) => FilterBy == null ? true : FilterBy(frag);
            this.SyncRoot.EnterReadLock();
            try
            {
                IEnumerable<CodeFragment.TokenIndex> GetFiltered(int lineNr) =>
                    Enumerable.Range(0, this.GetTokenizedLine(lineNr).Length)
                        .Select(i => new CodeFragment.TokenIndex(this, lineNr, i))
                        .Where(tokenIndex =>Filter(tokenIndex.GetFragment()));
                return GetLineNumbers().SelectMany(GetFiltered).ToList();
            }
            finally { this.SyncRoot.ExitReadLock(); }
        }

        /// Given a function returning an int array of line numbers, 
        /// applies FilterBy to the fragments on any of those lines and returns the resulting flattened fragment sequence as IEnumerable. 
        /// Note that the range of the returned fragments is the absolute range in the file. 
        /// If FilterBy is null, returns an IEnumerable with all fragments on the lines specified by GetLineNumbers.
        /// Returns an empty List if GetLineNumbers is null.
        private List<CodeFragment> FilterFragments(Func<int[]> GetLineNumbers, Func<CodeFragment, bool> FilterBy = null)
        {
            this.SyncRoot.EnterReadLock();
            try { return this.FilterTokenIndices(GetLineNumbers, FilterBy).Select(tokenIndex => tokenIndex.GetFragment()).ToList(); }
            finally { this.SyncRoot.ExitReadLock(); }
        }

        /// returns a list with all token indices corresponding to namespace declarations
        internal List<CodeFragment.TokenIndex> NamespaceDeclarationTokens() =>
            this.FilterTokenIndices(this.Header.GetNamespaceDeclarations, FileHeader.IsNamespaceDeclaration);

        /// returns a list with all token indices corresponding to open directives
        internal List<CodeFragment.TokenIndex> OpenDirectiveTokens() =>
            this.FilterTokenIndices(this.Header.GetOpenDirectives, FileHeader.IsOpenDirective);

        /// returns a list with all token indices corresponding to type declarations
        internal List<CodeFragment.TokenIndex> TypeDeclarationTokens() =>
            this.FilterTokenIndices(this.Header.GetTypeDeclarations, FileHeader.IsTypeDeclaration);

        /// returns a list with all token indices corresponding to operation or function declarations
        internal List<CodeFragment.TokenIndex> CallableDeclarationTokens() =>
            this.FilterTokenIndices(this.Header.GetCallableDeclarations, FileHeader.IsCallableDeclaration);

        /// returns all namespace declarations in the file sorted by the line number they are declared on
        internal IEnumerable<(NonNullable<string>, Range)> GetNamespaceDeclarations()
        {
            var decl = this.FilterFragments(this.Header.GetNamespaceDeclarations, FileHeader.IsNamespaceDeclaration);
            return decl.Select(fragment => (fragment.Kind.DeclaredNamespaceName(InternalUse.UnknownNamespace), fragment.GetRange()))
                .Where(tuple => tuple.Item1 != null)
                .Select(tuple => (NonNullable<string>.New(tuple.Item1), tuple.Item2));
        }

        /// returns all type declarations in the file sorted by the line number they are declared on
        internal IEnumerable<(NonNullable<string>, Range)> GetTypeDeclarations()
        {
            var decl = this.FilterFragments(this.Header.GetTypeDeclarations, FileHeader.IsTypeDeclaration);
            return decl.Select(fragment => (fragment.Kind.DeclaredTypeName(null), fragment.GetRange()))
                .Where(tuple => tuple.Item1 != null)
                .Select(tuple => (NonNullable<string>.New(tuple.Item1), tuple.Item2));
        }

        /// returns all callable declarations in the file sorted by the line number they are declared on
        internal IEnumerable<(NonNullable<string>, Range)> GetCallableDeclarations()
        {
            var decl = this.FilterFragments(this.Header.GetCallableDeclarations, FileHeader.IsCallableDeclaration);
            return decl.Select(fragment => (fragment.Kind.DeclaredCallableName(null), fragment.GetRange()))
                .Where(tuple => tuple.Item1 != null)
                .Select(tuple => (NonNullable<string>.New(tuple.Item1), tuple.Item2));
        }
    }
}
