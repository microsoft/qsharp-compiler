// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Position = Microsoft.Quantum.QsCompiler.DataTypes.Position;
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    internal static class ContextBuilder
    {
        // utils for managing syntax tokens

        /// <summary>
        /// Verifies that all tokens are ordered according to their range.
        /// </summary>
        /// <exception cref="ArgumentException">Not all <paramref name="tokens"/> are ordered according to their range.</exception>
        internal static void VerifyTokenOrdering(IEnumerable<CodeFragment> tokens)
        {
            Position? previousEnding = null;
            foreach (var token in tokens)
            {
                if (!(previousEnding is null) && previousEnding > token.Range.Start)
                {
                    throw new ArgumentException($"the given tokens to update are not ordered according to their range - \n" +
                        $"Ranges were: {string.Join("\n", tokens.Select(t => t.Range.DiagnosticString()))}");
                }
                previousEnding = token.Range.End;
            }
        }

        /// <summary>
        /// Returns the TokenIndex for the first token in the given file, or null if no such token exists.
        /// </summary>
        internal static CodeFragment.TokenIndex? FirstToken(this FileContentManager file)
        {
            var lineNr = 0;
            while (file.GetTokenizedLine(lineNr).Length == 0 && ++lineNr < file.NrLines())
            {
            }
            return lineNr == file.NrLines()
                ? null
                : new CodeFragment.TokenIndex(file, lineNr, 0);
        }

        /// <summary>
        /// Returns the TokenIndex for the last token in the given file, or null if no such token exists.
        /// </summary>
        internal static CodeFragment.TokenIndex? LastToken(this FileContentManager file)
        {
            var lastNonEmpty = file.NrLines();
            while (lastNonEmpty-- > 0 && file.GetTokenizedLine(lastNonEmpty).Length == 0)
            {
            }
            return lastNonEmpty < 0
                ? null
                : new CodeFragment.TokenIndex(file, lastNonEmpty, file.GetTokenizedLine(lastNonEmpty).Length - 1);
        }

        /// <summary>
        /// Returns true if the given token is fully included in the given range.
        /// </summary>
        internal static bool IsWithinRange(this CodeFragment token, Range range) =>
            range.Contains(token.Range.Start) && range.ContainsEnd(token.Range.End);

        /// <summary>
        /// Returns a function that returns true if a given fragment ends at or before the given position.
        /// </summary>
        internal static Func<CodeFragment, bool> TokensUpTo(Position pos) => token =>
            token.Range.End <= pos;

        /// <summary>
        /// Returns a function that returns true if a given fragment starts (strictly) before the given position.
        /// </summary>
        internal static Func<CodeFragment, bool> TokensStartingBefore(Position pos) => token =>
            token.Range.Start < pos;

        /// <summary>
        /// Returns a function that returns true if a given fragment starts at or after the given position.
        /// </summary>
        internal static Func<CodeFragment, bool> TokensAfter(Position pos) => token =>
            pos <= token.Range.Start;

        /// <summary>
        /// Returns a function that returns true if a given fragment does not overlap with the specified range.
        /// </summary>
        internal static Func<CodeFragment, bool> NotOverlappingWith(Range relRange) => token =>
            token.IsWithinRange(Range.Create(Position.Zero, relRange.Start))
            || TokensAfter(relRange.End)(token);

        /// <summary>
        /// Returns the CodeFragment at the given position if such a fragment exists and null otherwise.
        /// If the given position is equal to the end of the fragment, that fragment is returned if includeEnd is set to true.
        /// If a fragment is determined for the given position, returns the corresponding token index as out parameter.
        /// Note that token indices are no longer valid as soon as the file is modified (possibly e.g. by queued background processing).
        /// Any query or attempt operation on the returned token index may result in an exception once it lost its validity.
        /// Returns null if the given file or the specified position is null,
        /// or if the specified position is not within the current Content range.
        /// </summary>
        public static CodeFragment? TryGetFragmentAt(
            this FileContentManager file,
            Position? pos,
            out CodeFragment.TokenIndex? tIndex,
            bool includeEnd = false)
        {
            tIndex = null;
            if (file == null || pos == null || !file.ContainsPosition(pos))
            {
                return null;
            }
            var start = pos.Line;
            var previous = file.GetTokenizedLine(start).Where(token => token.Range.Start.Column <= pos.Column).ToImmutableArray();
            while (!previous.Any() && --start >= 0)
            {
                previous = file.GetTokenizedLine(start);
            }
            if (!previous.Any())
            {
                return null;
            }

            var lastPreceding = previous.Last().WithLineNumOffset(start);
            var overlaps = includeEnd
                ? pos <= lastPreceding.Range.End
                : pos < lastPreceding.Range.End;
            tIndex = overlaps ? new CodeFragment.TokenIndex(file, start, previous.Length - 1) : null;
            return overlaps ? lastPreceding : null;
        }

        /// <summary>
        /// Returns the name of the namespace to which the given position belongs.
        /// Returns null if the given file or position is null, or if no such namespace can be found
        /// (e.g. because the namespace name is invalid).
        /// </summary>
        public static string? TryGetNamespaceAt(this FileContentManager file, Position pos)
        {
            if (file == null || pos == null || !file.ContainsPosition(pos))
            {
                return null;
            }
            var namespaces = file.GetNamespaceDeclarations();
            var preceding = namespaces.TakeWhile(tuple => tuple.Item2.Start < pos);
            return preceding.Any() ? preceding.Last().Item1 : null;
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
        public static ((string, Position), (QsSpecializationKind?, Position?))? TryGetClosestSpecialization(
            this FileContentManager file, Position pos)
        {
            QsSpecializationKind? GetSpecializationKind(CodeFragment fragment)
            {
                var specDecl = fragment.Kind.DeclaredSpecialization();
                if (specDecl.IsNull)
                {
                    return null;
                }
                var ((kind, gen), typeArgs) = specDecl.Item; // note: if we want to support type specializations we need to compute the signature of the spec to find the right one
                return kind;
            }

            if (file == null || pos == null || !file.ContainsPosition(pos))
            {
                return null;
            }
            file.SyncRoot.EnterReadLock();
            try
            {
                var declarations = file.CallableDeclarationTokens();
                var precedingDecl = declarations.TakeWhile(tIndex => tIndex.GetFragment().Range.Start < pos);
                if (!precedingDecl.Any())
                {
                    return null;
                }

                var closestCallable = precedingDecl.Last();
                var callablePosition = closestCallable.GetFragment().Range.Start;
                var callableName = closestCallable.GetFragment().Kind.DeclaredCallableName(null);
                if (callableName == null)
                {
                    return null;
                }

                var specializations = FileHeader.FilterCallableSpecializations(closestCallable.GetChildren(deep: false).Select(tIndex => tIndex.GetFragment()));
                var precedingSpec = specializations.TakeWhile(fragment => fragment.Range.Start < pos);
                var lastPreceding = precedingSpec.Any() ? precedingSpec.Last() : null;

                if (specializations.Any() && lastPreceding == null)
                {
                    // the given position is within a callable declaration
                    return ((callableName, callablePosition), (null, null));
                }
                return lastPreceding == null
                    ? ((callableName, callablePosition), (QsSpecializationKind.QsBody, callablePosition))
                    : ((callableName, callablePosition), (GetSpecializationKind(lastPreceding), lastPreceding.Range.Start));
            }
            finally
            {
                file.SyncRoot.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns true if the given file contains any tokens overlapping with the given fragment.
        /// The range of the tokens in the file is assumed to be relative to their start line (the index at which they are listed),
        /// whereas the range of the given fragment is assumed to be the absolute range.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="range"/> is not a valid range within <paramref name="file"/>.</exception>
        internal static bool ContainsTokensOverlappingWith(this FileContentManager file, Range range)
        {
            if (!file.ContainsRange(range))
            {
                throw new ArgumentOutOfRangeException(nameof(range));
            }

            var (start, end) = (range.Start.Line, range.End.Line);
            if (start != end && file.GetTokenizedLines(start + 1, end - start - 1).SelectMany(x => x).Any())
            {
                return true;
            }

            var inRange = file.GetTokenizedLine(start).Where(TokensAfter(Position.Create(0, range.Start.Column))); // checking tokens overlapping with range.Start below
            inRange = start == end
                ? inRange.Where(TokensStartingBefore(Position.Create(0, range.End.Column)))
                : inRange.Concat(file.GetTokenizedLine(end).Where(TokensStartingBefore(Position.Create(0, range.End.Column))));
            if (inRange.Any())
            {
                QsCompilerError.Raise($"{range.DiagnosticString()} overlaps for start = {start}, end = {end}, \n\n" +
                    $"{string.Join("\n", file.GetTokenizedLine(start).Select(x => $"{x.Range.DiagnosticString()}"))},\n\n " +
                    $"{string.Join("\n", file.GetTokenizedLine(end).Select(x => $"{x.Range.DiagnosticString()}"))},");
                return true;
            }

            var overlapsWithStart = file.TryGetFragmentAt(range.Start, out _);
            return overlapsWithStart != null;
        }

        /// <summary>
        /// Assuming both the current tokens and the tokens to update are sorted according to their range,
        /// merges the current and updated tokens such that the merged collection is sorted as well.
        /// </summary>
        /// <exception cref="QsCompilerException">The token verification for the merged collection failed.</exception>
        internal static List<CodeFragment> MergeTokens(IEnumerable<CodeFragment> current, IEnumerable<CodeFragment> updated)
        {
            var merged = new List<CodeFragment>(0);
            void NextBatch(ref IEnumerable<CodeFragment> batch, IEnumerable<CodeFragment> next)
            {
                if (next.Any())
                {
                    var start = next.First().Range.Start;
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
        /// </summary>
        internal static int FindByValue(this IReadOnlyList<CodeFragment> list, CodeFragment token)
        {
            var index = -1;
            var tokenRange = token.Range;
            while (++index < list.Count && list[index].Range.Start < tokenRange.Start)
            {
            }
            return index < list.Count && list[index].Equals(token) ? index : -1;
        }

        /// <summary>
        /// Returns the index of the closest preceding non-empty token with the next lower indentation level.
        /// Returns null if no such token exists.
        /// </summary>
        internal static CodeFragment.TokenIndex? GetNonEmptyParent(this CodeFragment.TokenIndex tIndex)
        {
            var current = tIndex;
            var indentation = current.GetFragment().Indentation;
            while ((current = current.Previous()) != null)
            {
                var fragment = current.GetFragment();
                if (fragment.Kind != null && fragment.Indentation < indentation)
                {
                    break; // ignore empty fragments
                }
            }
            return current != null && current.GetFragment().Indentation == indentation - 1 ? current : null;
        }

        /// <summary>
        /// Returns an IEnumerable with the indices of the closest preceding non-empty tokens with increasingly lower indentation level.
        /// </summary>
        internal static IEnumerable<CodeFragment.TokenIndex> GetNonEmptyParents(this CodeFragment.TokenIndex tIndex)
        {
            for (var current = tIndex.GetNonEmptyParent(); current != null; current = current.GetNonEmptyParent())
            {
                yield return current;
            }
        }

        /// <summary>
        /// Returnes an IEnumerable with the indices of all children of the given token.
        /// If deep is set to true (default value), then the returned children are all following tokens
        /// with a higher indentation level than the token corresponding to tIndex
        /// up to the point where we are at the same indentation level again.
        /// If deep is set to false, then of those only the tokens with an indentation level that is precisely
        /// one larger than the one of the parent token are returned.
        /// </summary>
        internal static IEnumerable<CodeFragment.TokenIndex> GetChildren(this CodeFragment.TokenIndex tIndex, bool deep = true)
        {
            var current = tIndex;
            var indentation = current.GetFragment().Indentation;
            while ((current = current.Next()) != null && current.GetFragment().Indentation > indentation)
            {
                if (deep || current.GetFragment().Indentation == indentation + 1)
                {
                    yield return current;
                }
            }
        }

        /// <summary>
        /// Returns the index of the preceding non-empty token on the same indentation level, or null if no such token exists.
        /// Includes empty tokens if includeEmpty is set to true.
        /// </summary>
        internal static CodeFragment.TokenIndex? PreviousOnScope(this CodeFragment.TokenIndex tIndex, bool includeEmpty = false)
        {
            var current = tIndex;
            var indentation = current.GetFragment().Indentation;
            while ((current = current.Previous()) != null)
            {
                var fragment = current.GetFragment();
                if (fragment.Indentation <= indentation && (fragment.Kind != null || includeEmpty))
                {
                    break;
                }
            }
            return current != null && current.GetFragment().Indentation == indentation ? current : null;
        }

        /// <summary>
        /// Returns the index of the next non-empty token on the same indenation level, or null if no such token exists.
        /// Includes empty tokens if includeEmpty is set to true.
        /// </summary>
        internal static CodeFragment.TokenIndex? NextOnScope(this CodeFragment.TokenIndex tIndex, bool includeEmpty = false)
        {
            var current = tIndex;
            var indentation = current.GetFragment().Indentation;
            while ((current = current.Next()) != null)
            {
                var fragment = current.GetFragment();
                if (fragment.Indentation <= indentation && (fragment.Kind != null || includeEmpty))
                {
                    break;
                }
            }
            return current != null && current.GetFragment().Indentation == indentation ? current : null;
        }

        // routines related to "reconstructing" the syntax tree from the saved tokens to do context checks

        /// <summary>
        /// Returns the context object for the given token index, ignoring empty fragments.
        /// </summary>
        private static Context.SyntaxTokenContext GetContext(this CodeFragment.TokenIndex tokenIndex)
        {
            QsNullable<QsFragmentKind> Nullable(CodeFragment? token, bool precedesSelf) =>
                token?.Kind == null
                ? QsNullable<QsFragmentKind>.Null
                : precedesSelf && !token.IncludeInCompilation // fragments that *follow * self need to be re-evaluated first
                    ? QsNullable<QsFragmentKind>.NewValue(QsFragmentKind.InvalidFragment)
                    : QsNullable<QsFragmentKind>.NewValue(token.Kind);

            var fragment = tokenIndex.GetFragment();
            var headerRange = fragment?.HeaderRange ?? Range.Zero;

            var self = Nullable(fragment, false); // making sure that errors for fragments excluded from compilation still get logged
            var previous = Nullable(tokenIndex.PreviousOnScope()?.GetFragment(), true); // excludes empty tokens
            var next = Nullable(tokenIndex.NextOnScope()?.GetFragment(), false);  // excludes empty tokens
            var parents = tokenIndex.GetNonEmptyParents().Select(tIndex => Nullable(tIndex.GetFragment(), true)).ToArray();
            return new Context.SyntaxTokenContext(headerRange, self, previous, next, parents);
        }

        /// <summary>
        /// Given an SortedSet of changed lines, verifies the context for each token on one of these lines,
        /// and adds the computed diagnostics to the ones returned as out parameter.
        /// Marks the token indices which are to be excluded from compilation due to context errors.
        /// Returns the line numbers for which the context diagnostics have been recomputed.
        /// </summary>
        private static HashSet<int> VerifyContext(this FileContentManager file, SortedSet<int> changedLines, out List<Diagnostic> diagnostics)
        {
            IEnumerable<CodeFragment.TokenIndex> TokenIndices(int lineNr) =>
                Enumerable.Range(0, file.GetTokenizedLine(lineNr).Count()).Select(index => new CodeFragment.TokenIndex(file, lineNr, index));

            var tokensToVerify = changedLines.SelectMany(TokenIndices);
            var verifiedLines = new HashSet<int>();

            List<Diagnostic> Verify(CodeFragment.TokenIndex tokenIndex)
            {
                var messages = new List<Diagnostic>();
                var fragment = tokenIndex.GetFragment();
                var context = tokenIndex.GetContext();

                var (include, verifications) = Context.VerifySyntaxTokenContext(context);
                foreach (var msg in verifications)
                {
                    messages.Add(Diagnostics.Generate(file.FileName, msg, fragment.Range.Start));
                }

                if (include)
                {
                    tokenIndex.MarkAsIncluded();
                }
                else
                {
                    tokenIndex.MarkAsExcluded();
                }
                verifiedLines.Add(tokenIndex.Line);

                // if a token is newly included in or excluded from the compilation,
                // then this may impact context information for all following tokens
                var changedStatus = include ^ fragment.IncludeInCompilation;
                var next = tokenIndex.NextOnScope();
                if (changedStatus && next != null && !changedLines.Contains(next.Line))
                {
                    // NOTE: since we invalidate context diagnostics on a per-line basis, we need to verify the whole line!
                    var tokens = TokenIndices(next.Line);
                    foreach (var token in tokens)
                    {
                        messages.AddRange(Verify(token));
                    }
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
        /// </summary>
        internal static IEnumerable<(Range, QsQualifiedName)> CallablesWithContentModifications(this FileContentManager file, IEnumerable<int> changedLines)
        {
            var lastInFile = file.LastToken()?.GetFragment()?.Range?.End ?? file.End();
            var callables = file.GetCallableDeclarations().Select(tuple => // these are sorted according to their line number
            {
                var ns = file.TryGetNamespaceAt(tuple.Item2.Start);
                QsCompilerError.Verify(ns != null, "namespace for callable declaration should not be null"); // invalid namespace names default to an unknown namespace name, but remain included in the compilation
                return (tuple.Item2.Start, new QsQualifiedName(ns, tuple.Item1));
            }).ToList();

            // NOTE: The range of modifications that has to trigger an update of the syntax tree for a callable
            // does need to go up to and include modifications to the line containing the next callable!
            // Otherwise inserting a callable declaration in the middle of an existing callable does not trigger the right behavior!
            (Range, QsQualifiedName) TypeCheckingRange((Position, QsQualifiedName) lastPreceding, IEnumerable<(Position, QsQualifiedName)> next)
            {
                var callableStart = lastPreceding.Item1;
                var callableEnd = next.Any() ? next.First().Item1 : lastInFile;
                return (Range.Create(callableStart, callableEnd), lastPreceding.Item2);
            }

            foreach (var lineNr in changedLines)
            {
                bool Precedes((Position, QsQualifiedName) tuple) => tuple.Item1.Line < lineNr;
                var preceding = callables.TakeWhile(Precedes);
                var following = callables.SkipWhile(Precedes);

                if (preceding.Any())
                {
                    yield return TypeCheckingRange(preceding.Last(), following);
                }
                if (following.Any() && following.First().Start.Line == lineNr)
                {
                    yield return TypeCheckingRange(following.First(), following.Skip(1));
                }
            }
        }

        /// <summary>
        /// Dequeues all lines whose tokens has changed and verifies the positions of these tokens.
        /// Does nothing if no lines have been modified.
        /// Recomputes and pushes the context diagnostics for the processed tokens otherwise.
        /// </summary>
        internal static void UpdateContext(this FileContentManager file)
        {
            file.SyncRoot.EnterUpgradeableReadLock();
            try
            {
                var changedLines = file.DequeueTokenChanges();
                if (!changedLines.Any())
                {
                    return;
                }
                QsCompilerError.RaiseOnFailure(
                    () =>
                    {
                        var verifiedLines = file.VerifyContext(changedLines, out List<Diagnostic> diagnostics);
                        file.UpdateContextDiagnostics(verifiedLines, diagnostics);
                    },
                    "updating the ContextDiagnostics failed");

                var edited = file.CallablesWithContentModifications(changedLines);
                file.MarkCallableAsContentEdited(edited);
            }
            finally
            {
                file.SyncRoot.ExitUpgradeableReadLock();
            }
        }
    }
}
