// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.VisualStudio.LanguageServer.Protocol;


namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    internal static class ContextBuilder
    {
        // utils for managing syntax tokens

        /// <summary>
        /// Verifies that all tokens are ordered according to their range.
        /// Throws an ArgumentNullException if the given tokens are null, or if any of the contained elements are.
        /// Throws an ArgumentException if this is not the case.
        /// </summary>
        internal static void VerifyTokenOrdering(IEnumerable<CodeFragment> tokens)
        {
            if (tokens == null || tokens.Any(x => x == null)) throw new ArgumentNullException(nameof(tokens));

            Position previousEnding = null;
            foreach (var token in tokens)
            {
                var range = token.GetRange();
                if (!(previousEnding?.IsSmallerThanOrEqualTo(range.Start) ?? true))
                {
                    throw new ArgumentException($"the given tokens to update are not ordered according to their range - \n" +
                        $"Ranges were: {String.Join("\n", tokens.Select(t => t.GetRange().DiagnosticString()))}");
                }
                previousEnding = range.End;
            }
        }

        /// <summary>
        /// Returns the TokenIndex for the first token in the given file, or null if no such token exists.
        /// Throws an ArgumentNullException if file is null.
        /// </summary>
        internal static CodeFragment.TokenIndex FirstToken(this FileContentManager file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            var lineNr = 0;
            while (file.GetTokenizedLine(lineNr).Length == 0 && ++lineNr < file.NrLines()) ;
            return lineNr == file.NrLines() 
                ? null 
                : new CodeFragment.TokenIndex(file, lineNr, 0);
        }

        /// <summary>
        /// Returns the TokenIndex for the last token in the given file, or null if no such token exists.
        /// Throws an ArgumentNullException if file is null.
        /// </summary>
        internal static CodeFragment.TokenIndex LastToken(this FileContentManager file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            var lastNonEmpty = file.NrLines();
            while (lastNonEmpty-- > 0 && file.GetTokenizedLine(lastNonEmpty).Length == 0) ;
            return lastNonEmpty < 0
                ? null
                : new CodeFragment.TokenIndex(file, lastNonEmpty, file.GetTokenizedLine(lastNonEmpty).Length - 1);
        }

        /// <summary>
        /// Returns true if the given token is fully included in the given range.
        /// Throws an ArgumentNullException if token or the range delimiters are null.
        /// Throws an ArgumentException if the given range is not valid.
        /// </summary>
        internal static bool IsWithinRange(this CodeFragment token, Range range)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            if (!Utils.IsValidRange(range)) throw new ArgumentException("invalid range");
            var tokenRange = token.GetRange();
            return tokenRange.Start.IsWithinRange(range) && tokenRange.End.IsWithinRange(range, includeEnd: true);
        }

        /// <summary>
        /// Returns a function that returns true if a given fragment ends at or before the given position.
        /// </summary>
        internal static Func<CodeFragment, bool> TokensUpTo(Position pos) =>
            (CodeFragment token) => token.GetRange().End.IsSmallerThanOrEqualTo(pos);

        /// <summary>
        /// Returns a function that returns true if a given fragment starts (strictly) before the given position.
        /// </summary>
        internal static Func<CodeFragment, bool> TokensStartingBefore(Position pos) =>
            (CodeFragment token) => token.GetRange().Start.IsSmallerThan(pos);

        /// <summary>
        /// Returns a function that returns true if a given fragment starts at or after the given position.
        /// </summary>
        internal static Func<CodeFragment, bool> TokensAfter(Position pos) =>
            (CodeFragment token) => pos.IsSmallerThanOrEqualTo(token.GetRange().Start);

        /// <summary>
        /// Returns a function that returns true if a given fragment does not overlap with the specified range.
        /// </summary>
        internal static Func<CodeFragment, bool> NotOverlappingWith(Range relRange) =>
            token =>
                token.IsWithinRange(new Range { Start = new Position(0, 0), End = relRange.Start }) ||
                (ContextBuilder.TokensAfter(relRange.End))(token);

        /// <summary>
        /// Returns the CodeFragment at the given position if such a fragment exists and null otherwise.
        /// If the given position is equal to the end of the fragment, that fragment is returned if includeEnd is set to true.
        /// Returns null if the given file or the specified position is null,
        /// or if the specified position is not within the current Content range.
        /// </summary>
        public static CodeFragment TryGetFragmentAt(this FileContentManager file, Position pos, bool includeEnd = false)
        {
            if (file == null || pos == null || !Utils.IsValidPosition(pos, file)) return null;
            var start = pos.Line;
            var previous = file.GetTokenizedLine(start).Where(token => token.GetRange().Start.IsSmallerThanOrEqualTo(new Position(0, pos.Character)));
            while (!previous.Any() && --start >= 0) previous = file.GetTokenizedLine(start);
            if (!previous.Any()) return null;

            var lastPreceding = previous.Last().WithUpdatedLineNumber(start);
            var overlaps = includeEnd
                ? pos.IsSmallerThanOrEqualTo(lastPreceding.GetRange().End) 
                : pos.IsSmallerThan(lastPreceding.GetRange().End);
            return overlaps ? lastPreceding : null;
        }

        /// <summary>
        /// Returns the name of the namespace to which the given position belongs.
        /// Returns null if the given file or position is null, or if no such namespace can be found 
        /// (e.g. because the namespace name is invalid).
        /// </summary>
        public static string TryGetNamespaceAt(this FileContentManager file, Position pos)
        {
            if (file == null || pos == null || !Utils.IsValidPosition(pos, file)) return null;
            var namespaces = file.GetNamespaceDeclarations();
            var preceding = namespaces.TakeWhile(tuple => tuple.Item2.Start.IsSmallerThan(pos));
            return preceding.Any() ? preceding.Last().Item1.Value : null;
        }

        /// <summary>
        /// Returns the position and the kind of the closest specialization preceding the given position, 
        /// and the name of the callable it belongs to as well as its position as Nullable.
        /// Returns null if the given file or position is null, or if no preceding callable can be found (e.g. because the callable name is invalid).
        /// If a callable name but no specializations (preceding or otherwise) within that callable can be found, 
        /// assumes that the correct specialization is an auto-inserted default body, 
        /// and returns QsBody as well as the start position of the callable declaration along with the callable name and its position. 
        /// If a callable name as well as existing specializations can be found, but no specialization precedes the given position,
        /// returns null for the specialization kind as well as for its position. 
        /// </summary>
        public static ((NonNullable<string>, Position), (QsSpecializationKind, Position))? TryGetClosestSpecialization(this FileContentManager file, Position pos)
        {
            QsSpecializationKind GetSpecializationKind(CodeFragment fragment)
            {
                var specDecl = fragment.Kind.DeclaredSpecialization();
                if (specDecl.IsNull) return null;
                var ((kind, gen), typeArgs) = specDecl.Item; // note: once we support type specializations we need to compute the signature of the spec to find the right one
                return kind;
            }

            if (file == null || pos == null || !Utils.IsValidPosition(pos, file)) return null;
            file.SyncRoot.EnterReadLock();
            try {
                var declarations = file.CallableDeclarationTokens();
                var precedingDecl = declarations.TakeWhile(tIndex => tIndex.GetFragment().GetRange().Start.IsSmallerThan(pos));
                if (!precedingDecl.Any()) return null;

                var closestCallable = precedingDecl.Last();
                var callablePosition = closestCallable.GetFragment().GetRange().Start;
                var callableName = closestCallable.GetFragment().Kind.DeclaredCallableName(null);
                if (callableName == null) return null;

                var specializations = FileHeader.FilterCallableSpecializations(closestCallable.GetChildren(deep: false).Select(tIndex => tIndex.GetFragment()));
                var precedingSpec = specializations.TakeWhile(fragment => fragment.GetRange().Start.IsSmallerThan(pos));
                var lastPreceding = precedingSpec.Any() ? precedingSpec.Last() : null;

                if (specializations.Any() && lastPreceding == null) // the given position is within a callable declaration
                { return ((NonNullable<string>.New(callableName), callablePosition), (null, null)); } 
                return lastPreceding == null
                    ? ((NonNullable<string>.New(callableName), callablePosition), (QsSpecializationKind.QsBody, callablePosition))
                    : ((NonNullable<string>.New(callableName), callablePosition), (GetSpecializationKind(lastPreceding), lastPreceding.GetRange().Start));
            }
            finally { file.SyncRoot.ExitReadLock(); }
        }

        /// <summary>
        /// Returns true if the given file contains any tokens overlapping with the given fragment.
        /// The range of the tokens in the file is assumed to be relative to their start line (the index at which they are listed),
        /// whereas the range of the given fragment is assumed to be the absolute range.
        /// Throws an ArgumentNullException if the given file or range is null.
        /// Throws an ArgumentOutOfRangeException if the given range is not a valid range within file.
        /// </summary>
        internal static bool ContainsTokensOverlappingWith(this FileContentManager file, Range range)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (!Utils.IsValidRange(range, file)) throw new ArgumentOutOfRangeException(nameof(range));

            var (start, end) = (range.Start.Line, range.End.Line);
            if (start != end && file.GetTokenizedLines(start + 1, end - start - 1).SelectMany(x => x).Any())
            { return true; };

            var inRange = file.GetTokenizedLine(start).Where(TokensAfter(new Position(0, range.Start.Character))); // checking tokens overlapping with range.Start below
            inRange = start == end
                ? inRange.Where(TokensStartingBefore(new Position(0, range.End.Character)))
                : inRange.Concat(file.GetTokenizedLine(end).Where(TokensStartingBefore(new Position(0, range.End.Character))));
            if (inRange.Any())
            {
                QsCompilerError.Raise($"{range.DiagnosticString()} overlaps for start = {start}, end = {end}, \n\n" +
                    $"{String.Join("\n", file.GetTokenizedLine(start).Select(x => $"{x.GetRange().DiagnosticString()}"))},\n\n " +
                    $"{String.Join("\n", file.GetTokenizedLine(end).Select(x => $"{x.GetRange().DiagnosticString()}"))},");
                return true;
            }

            var overlapsWithStart = file.TryGetFragmentAt(range.Start);
            return overlapsWithStart != null;
        }

        /// <summary>
        /// Assuming both the current tokens and the tokens to update are sorted according to their range,
        /// merges the current and updated tokens such that the merged collection is sorted as well.
        /// Throws an ArgumentNullException if either current or updated, or any of their elements are null.
        /// Throws an ArgumentException if the token verification for the merged collection fails.
        /// </summary>
        internal static List<CodeFragment> MergeTokens(IEnumerable<CodeFragment> current, IEnumerable<CodeFragment> updated)
        {
            if (current == null || current.Any(x => x == null)) throw new ArgumentNullException(nameof(current));
            if (updated == null || updated.Any(x => x == null)) throw new ArgumentNullException(nameof(updated));

            var merged = new List<CodeFragment>(0);
            void NextBatch(ref IEnumerable<CodeFragment> batch, IEnumerable<CodeFragment> next)
            {
                if (next.Any())
                {
                    var start = next.First().GetRange().Start;
                    merged.AddRange(batch.TakeWhile(TokensUpTo(start)));
                    batch = batch.SkipWhile(TokensUpTo(start)).ToList();
                }
                else
                {
                    merged.AddRange(batch);
                    batch = Enumerable.Empty<CodeFragment>();
                }
            }

            while (updated.Any() || current.Any())
            {
                NextBatch(ref current, updated);
                NextBatch(ref updated, current);
            }

            var mergedTokens = merged.ToList();
            QsCompilerError.RaiseOnFailure(() => VerifyTokenOrdering(mergedTokens), "merged tokens are not ordered"); 
            return mergedTokens;
        }

        /// <summary>
        /// Comparing for equality by value,
        /// returns the index of the first element in the given list of CodeFragments that matches the given token, 
        /// or -1 if no such element exists. 
        /// Throws an ArgumentNullException if the given list is null.
        /// </summary>
        internal static int FindByValue(this IReadOnlyList<CodeFragment> list, CodeFragment token)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (token == null)
            {
                var nrNonNull = list.TakeWhile(x => x != null).Count();
                return nrNonNull == list.Count ? -1 : nrNonNull;
            }

            var index = -1;
            var tokenRange = token.GetRange();
            while (++index < list.Count && list[index].GetRange().Start.IsSmallerThan(tokenRange.Start));
            return index < list.Count && list[index].Equals(token) ? index : -1;
        }

        /// <summary>
        /// Returns the index of the closest preceding non-empty token with the next lower indentation level.
        /// Returns null if no such token exists.
        /// Throws an ArgumentNullException if tIndex is null.
        /// </summary>
        private static CodeFragment.TokenIndex GetNonEmptyParent(this CodeFragment.TokenIndex tIndex)
        {
            if (tIndex == null) throw new ArgumentNullException(nameof(tIndex));

            var tokenIndex = new CodeFragment.TokenIndex(tIndex);
            var indentation = tokenIndex.GetFragment().Indentation;
            while (--tokenIndex != null) 
            {
                var fragment = tokenIndex.GetFragment();
                if (fragment.Kind != null && fragment.Indentation < indentation) break; // ignore empty fragments
            }
            return tokenIndex != null && tokenIndex.GetFragment().Indentation == indentation - 1 ? tokenIndex : null;
        }

        /// <summary>
        /// Returns an IEnumerable with the indices of the closest preceding non-empty tokens with increasingly lower indentation level.
        /// Throws an ArgumentNullException if tIndex is null.
        /// </summary>
        private static IEnumerable<CodeFragment.TokenIndex> GetNonEmptyParents(this CodeFragment.TokenIndex tIndex)
        {
            if (tIndex == null) throw new ArgumentNullException(nameof(tIndex));
            for (var current = tIndex.GetNonEmptyParent(); current != null; current = current.GetNonEmptyParent())
            { yield return current; }
        }

        /// <summary>
        /// Returnes an IEnumerable with the indices of all children of the given token.
        /// If deep is set to true (default value), then the returned children are all following tokens 
        /// with a higher indentation level than the token corresponding to tIndex 
        /// up to the point where we are at the same indentation level again.
        /// If deep is set to false, then of those only the tokens with an indentation level that is precisely 
        /// one larger than the one of the parent token are returned.
        /// Throws an ArgumentNullException if tIndex is null.
        /// </summary>
        internal static IEnumerable<CodeFragment.TokenIndex> GetChildren(this CodeFragment.TokenIndex tIndex, bool deep = true)
        {
            if (tIndex == null) throw new ArgumentNullException(nameof(tIndex));
            var tokenIndex = new CodeFragment.TokenIndex(tIndex);
            var indentation = tokenIndex.GetFragment().Indentation;
            while (++tokenIndex != null && tokenIndex.GetFragment().Indentation > indentation)
            { if (deep || tokenIndex.GetFragment().Indentation == indentation + 1) yield return tokenIndex; }
        }

        /// <summary>
        /// Returns the index of the preceding non-empty token on the same indenation level, or null if no such token exists.
        /// Includes empty tokens if includeEmpty is set to true.
        /// Throws an ArgumentNullException if tIndex is null.
        /// </summary>
        internal static CodeFragment.TokenIndex PreviousOnScope(this CodeFragment.TokenIndex tIndex, bool includeEmpty = false)
        {
            if (tIndex == null) throw new ArgumentNullException(nameof(tIndex));

            var tokenIndex = new CodeFragment.TokenIndex(tIndex);
            var indentation = tokenIndex.GetFragment().Indentation;
            while (--tokenIndex != null)
            {
                var fragment = tokenIndex.GetFragment();
                if (fragment.Indentation <= indentation && (fragment.Kind != null || includeEmpty)) break;
            }
            return tokenIndex != null && tokenIndex.GetFragment().Indentation == indentation ? tokenIndex : null;
        }

        /// <summary>
        /// Returns the index of the next non-empty token on the same indenation level, or null if no such token exists.
        /// Includes empty tokens if includeEmpty is set to true.
        /// Throws an ArgumentNullException if tIndex is null.
        /// </summary>
        internal static CodeFragment.TokenIndex NextOnScope(this CodeFragment.TokenIndex tIndex, bool includeEmpty = false)
        {
            if (tIndex == null) throw new ArgumentNullException(nameof(tIndex));

            var tokenIndex = new CodeFragment.TokenIndex(tIndex); 
            var indentation = tokenIndex.GetFragment().Indentation;
            while (++tokenIndex != null)
            {
                var fragment = tokenIndex.GetFragment();
                if (fragment.Indentation <= indentation && (fragment.Kind != null || includeEmpty)) break;
            }
            return tokenIndex != null && tokenIndex.GetFragment().Indentation == indentation ? tokenIndex : null;
        }


        // routines related to "reconstructing" the syntax tree from the saved tokens to do context checks

        /// <summary>
        /// Returns the context object for the given token index, ignoring empty fragments. 
        /// Throws an ArgumentNullException if the given token index is null.
        /// </summary>
        private static Context.SyntaxTokenContext GetContext(this CodeFragment.TokenIndex tokenIndex)
        {
            if (tokenIndex == null) throw new ArgumentNullException(nameof(tokenIndex));
            QsNullable<QsFragmentKind> Nullable(CodeFragment fragment) =>
                fragment?.Kind == null
                ? QsNullable<QsFragmentKind>.Null
                : fragment.IncludeInCompilation 
                ? QsNullable<QsFragmentKind>.NewValue(fragment.Kind)
                : QsNullable<QsFragmentKind>.NewValue(QsFragmentKind.InvalidFragment);

            var self = tokenIndex.GetFragment();
            var previous = tokenIndex.PreviousOnScope()?.GetFragment(); // excludes empty tokens
            var next = tokenIndex.NextOnScope()?.GetFragment(); // excludes empty tokens
            var parents = tokenIndex.GetNonEmptyParents().Select(tIndex => Nullable(tIndex.GetFragment())).ToArray();
            var nullableSelf = self?.Kind == null // special treatment such that errors for fragments excluded from compilation still get logged...
                ? QsNullable<QsFragmentKind>.Null
                : QsNullable<QsFragmentKind>.NewValue(self.Kind);
            var headerRange = self?.HeaderRange ?? QsCompilerDiagnostic.DefaultRange;
            return new Context.SyntaxTokenContext(headerRange, nullableSelf, Nullable(previous), Nullable(next), parents);
        }

        /// <summary>
        /// Given an SortedSet of changed lines, verifies the context for each token on one of these lines, 
        /// and adds the computed diagnostics to the ones returned as out parameter. 
        /// Marks the token indices which are to be excluded from compilation due to context errors. 
        /// Returns the line numbers for which the context diagnostics have been recomputed.
        /// Throws an ArgumentNullException if any of the arguments is null. 
        /// </summary>
        private static HashSet<int> VerifyContext(this FileContentManager file, SortedSet<int> changedLines, out List<Diagnostic> diagnostics)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (changedLines == null) throw new ArgumentNullException(nameof(changedLines));

            IEnumerable<CodeFragment.TokenIndex> TokenIndices(int lineNr) =>
                Enumerable.Range(0, file.GetTokenizedLine(lineNr).Count()).Select(index => new CodeFragment.TokenIndex(file, lineNr, index));

            var tokensToVerify = changedLines.SelectMany(TokenIndices);
            var verifiedLines = new HashSet<int>();

            List<Diagnostic> Verify (CodeFragment.TokenIndex tokenIndex)
            {
                var messages = new List<Diagnostic>();
                var fragment = tokenIndex.GetFragment();
                var context = tokenIndex.GetContext();

                var fragmentStart = fragment.GetRange().Start;
                var (include, verifications) = Context.VerifySyntaxTokenContext(context);
                foreach (var msg in verifications)
                { messages.Add(Diagnostics.Generate(file.FileName.Value, msg, fragmentStart)); }

                if (include) tokenIndex.MarkAsIncluded();
                else tokenIndex.MarkAsExcluded();
                verifiedLines.Add(tokenIndex.Line);

                // if a token is newly included in or excluded from the compilation, 
                // then this may impact all following tokens
                var changedStatus = include ^ fragment.IncludeInCompilation;
                var next = tokenIndex.NextOnScope();
                if (changedStatus && next != null && !changedLines.Contains(next.Line))
                {
                    // NOTE: since we invalidate context diagnostics on a per-line basis, we need to verify the whole line!
                    var tokens = TokenIndices(next.Line);
                    foreach (var token in tokens) messages.AddRange(Verify(token));
                }
                return messages;
            }

            diagnostics = tokensToVerify.SelectMany(tIndex => Verify(tIndex)).ToList();
            return verifiedLines;
        }


        // external routines for context verification

        /// <summary>
        /// Given the line number of the lines that contain tokens that (possibly) have been modified,
        /// checks which callable declaration they can potentially belong to and returns the fully qualified name of those callables.
        /// Throws an ArgumentNullException if the given file or the collection of changed lines is null.
        /// </summary>
        internal static IEnumerable<(Range, QsQualifiedName)> CallablesWithContentModifications(this FileContentManager file, IEnumerable<int> changedLines)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (changedLines == null) throw new ArgumentNullException(nameof(changedLines));

            var lastInFile = file.LastToken()?.GetFragment()?.GetRange()?.End ?? file.End();
            var callables = file.GetCallableDeclarations().Select(tuple => // these are sorted according to their line number 
            {
                var ns = file.TryGetNamespaceAt(tuple.Item2.Start); 
                QsCompilerError.Verify(ns != null, "namespace for callable declaration should not be null"); // invalid namespace names default to an unknown namespace name, but remain included in the compilation
                return (tuple.Item2.Start, new QsQualifiedName(NonNullable<string>.New(ns), tuple.Item1));
            }).ToList();

            // NOTE: The range of modifications that has to trigger an update of the syntax tree for a callable 
            // does need to go up to and include modifications to the line containing the next callable!
            // Otherwise inserting a callable declaration in the middle of an existing callable does not trigger the right behavior!
            (Range, QsQualifiedName) TypeCheckingRange((Position, QsQualifiedName) lastPreceding, IEnumerable<(Position, QsQualifiedName)> next)
            {
                var callableStart = lastPreceding.Item1;
                var callableEnd = next.Any() ? next.First().Item1 : lastInFile;
                return (new Range { Start = callableStart, End = callableEnd }, lastPreceding.Item2);
            }

            foreach (var lineNr in changedLines)
            {
                bool precedes((Position, QsQualifiedName) tuple) => tuple.Item1.Line < lineNr;
                var preceding = callables.TakeWhile(precedes);
                var following = callables.SkipWhile(precedes);

                if (preceding.Any()) 
                { yield return TypeCheckingRange(preceding.Last(), following); }
                if (following.Any() && following.First().Item1.Line == lineNr)
                { yield return TypeCheckingRange(following.First(), following.Skip(1)); }
            }
        }

        /// <summary>
        /// Dequeues all lines whose tokens has changed and verifies the positions of these tokens.
        /// Does nothing if no lines have been modified. 
        /// Recomputes and pushes the context diagnostics for the processed tokens otherwise.
        /// Throws an ArgumentNullException if file is null. 
        /// </summary>
        internal static void UpdateContext(this FileContentManager file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            file.SyncRoot.EnterUpgradeableReadLock();
            try
            {
                var changedLines = file.DequeueTokenChanges();
                if (!changedLines.Any()) return;
                QsCompilerError.RaiseOnFailure(() =>
                {
                    var verifiedLines = file.VerifyContext(changedLines, out List<Diagnostic> diagnostics);
                    file.UpdateContextDiagnostics(verifiedLines, diagnostics);
                }, "updating the ContextDiagnostics failed");

                var edited = file.CallablesWithContentModifications(changedLines);
                file.MarkCallableAsContentEdited(edited);
            }
            finally { file.SyncRoot.ExitUpgradeableReadLock(); }
        }
    }
}
