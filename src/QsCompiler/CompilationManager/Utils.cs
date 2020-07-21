// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Lsp = Microsoft.VisualStudio.LanguageServer.Protocol;
using Position = Microsoft.Quantum.QsCompiler.DataTypes.Position;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    public static class Utils
    {
        // characters interpreted as line breaks by Visual Studio:

        /// <summary>
        /// unicode for '\n'
        /// </summary>
        internal const string LF = "\u000A";

        /// <summary>
        /// unicode for '\r'
        /// </summary>
        internal const string CR = "\u000D"; // officially only giving a line break in combination with subsequent \n, but also causes a line break on its own...

        /// <summary>
        /// unicode for a line separator char
        /// </summary>
        internal const string LS = "\u2028";

        /// <summary>
        /// unicode for a paragraph separator char
        /// </summary>
        internal const string PS = "\u2029";

        /// <summary>
        /// unicode for a next line char
        /// </summary>
        internal const string NEL = "\u0085";

        /// <summary>
        /// contains the regex string that matches any char that is not a linebreak
        /// </summary>
        private static readonly string NonBreakingChar = $"[^{LF}{CR}{LS}{PS}{NEL}]";

        /// <summary>
        /// contains the regex string that matches a line break recognized by VisualStudio
        /// </summary>
        private static readonly string LineBreak = $"{CR}{LF}|{LF}|{CR}|{LS}|{PS}|{NEL}";

        // utils related to tracking the text content of files

        /// <summary>
        /// matches everything that could could be used as a symbol
        /// </summary>
        internal static readonly Regex ValidAsSymbol = new Regex(@"^[\p{L}_]([\p{L}\p{Nd}_]*)$");

        /// <summary>
        /// matches qualified symbols before the starting position (right-to-left), including incomplete qualified
        /// symbols that end with a dot
        /// </summary>
        internal static readonly Regex QualifiedSymbolRTL =
            new Regex(@"([\p{L}_][\p{L}\p{Nd}_]*\.?)+", RegexOptions.RightToLeft);

        /// <summary>
        /// matches a line and its line ending, and a *non-empty* line without line ending at the end
        /// </summary>
        private static readonly Regex EditorLine = new Regex($"({NonBreakingChar}*({LineBreak}))|({NonBreakingChar}+$)");

        /// <summary>
        /// matches a CR, LF, or CRLF occurence at the end of a string
        /// </summary>
        public static readonly Regex EndOfLine = new Regex($"({LineBreak})$"); // NOTE: *needs* to fail, if no line breaking character exists (scope tracking depends on it)

        /// <summary>
        /// Splits the given text into multiple lines, with the line ending of each line included in the line.
        /// </summary>
        public static string[] SplitLines(string text)
        {
            var matches = EditorLine.Matches(text);
            var lines = new string[matches.Count];
            var found = matches.GetEnumerator();
            for (var i = 0; found.MoveNext(); i += 1)
            {
                lines[i] = ((Match)found.Current).Value;
            }
            return lines;
        }

        /// <summary>
        /// to be used as "counter-piece" to SplitLines
        /// </summary>
        public static string JoinLines(string[] content) =>
            content == null ? null : string.Join("", content); // *DO NOT MODIFY* how lines are joined - the compiler functionality depends on it!

        /// <summary>
        /// Given a string, replaces the range [starChar, endChar) with the given string to insert.
        /// Returns null if the given text is null.
        /// Throws an ArgumentNullException if the given text to insert is null.
        /// Throws an ArgumentOutOfRangeException if the given start and end points do not denote a valid range within the string.
        /// </summary>
        internal static string GetChangedText(string lineText, int startChar, int endChar, string insert)
        {
            if (lineText == null)
            {
                return null;
            }
            if (insert == null)
            {
                throw new ArgumentNullException(nameof(insert));
            }
            if (startChar < 0 || startChar > lineText.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startChar));
            }
            if (endChar < startChar || endChar > lineText.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(endChar));
            }
            return lineText.Remove(startChar, endChar - startChar).Insert(startChar, insert);
        }

        /// <summary>
        /// Return a string with the new content of the (entire) lines in the range [start, end] where start and end are the start and end line of the given change.
        /// Verifies that the given change is consistent with the given file - i.e. the range is a valid range in file, and the text is not null, and
        /// throws the correspoding exceptions if this is not the case.
        /// </summary>
        internal static string GetTextChangedLines(FileContentManager file, TextDocumentContentChangeEvent change)
        {
            if (!IsValidRange(change.Range, file))
            {
                throw new ArgumentOutOfRangeException(nameof(change)); // range can be empty
            }
            if (change.Text == null)
            {
                throw new ArgumentNullException(nameof(change.Text), "the given text change is null");
            }

            var first = file.GetLine(change.Range.Start.Line).Text;
            var last = file.GetLine(change.Range.End.Line).Text;
            var prepend = first.Substring(0, change.Range.Start.Character);
            var append = last.Substring(change.Range.End.Character);
            return string.Concat(prepend, change.Text, append);
        }

        // general purpose type extensions

        /// <summary>
        /// Partitions the given IEnumerable into the elements for which predicate returns true and those for which it returns false.
        /// Returns (null, null) if the given IEnumerable is null.
        /// Throws an ArgumentNullException if predicate is null.
        /// </summary>
        public static (List<T>, List<T>) Partition<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            return (collection?.Where(predicate).ToList(), collection?.Where(x => !predicate(x)).ToList());
        }

        /// <summary>
        /// Returns true if the given lock is either ReadLockHeld, or is UpgradeableReadLockHeld, or isWriteLockHeld.
        /// Throws an ArgumentNullException if the given lock is null.
        /// </summary>
        public static bool IsAtLeastReadLockHeld(this ReaderWriterLockSlim syncRoot)
        {
            if (syncRoot == null)
            {
                throw new ArgumentNullException(nameof(syncRoot));
            }
            return syncRoot.IsReadLockHeld || syncRoot.IsUpgradeableReadLockHeld || syncRoot.IsWriteLockHeld;
        }

        // utils for dealing with positions and ranges

        /// <summary>
        /// Returns true if the given position is valid, i.e. if both the line and character are positive.
        /// Throws an ArgumentNullException is an argument is null.
        /// </summary>
        public static bool IsValidPosition(Lsp.Position pos)
        {
            if (pos == null)
            {
                throw new ArgumentNullException(nameof(pos));
            }
            return pos.Line >= 0 && pos.Character >= 0;
        }

        /// <summary>
        /// Returns true if the given position is valid, i.e. if the line is within the given file,
        /// and the character is within the text on that line (including text.Length).
        /// Throws an ArgumentNullException is an argument is null.
        /// </summary>
        internal static bool IsValidPosition(Position pos, FileContentManager file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            return pos.Line < file.NrLines() && pos.Column <= file.GetLine(pos.Line).Text.Length;
        }

        /// <summary>
        /// Verifies both positions, and returns true if the first position comes strictly before the second position.
        /// Throws an ArgumentNullException if a given position is null.
        /// Throws an ArgumentException if a given position is not valid.
        /// </summary>
        internal static bool IsSmallerThan(this Lsp.Position first, Lsp.Position second)
        {
            if (!IsValidPosition(first) || !IsValidPosition(second))
            {
                throw new ArgumentException("invalid position(s) given for comparison");
            }
            return first.Line < second.Line || (first.Line == second.Line && first.Character < second.Character);
        }

        /// <summary>
        /// Verifies both positions, and returns true if the first position comes before the second position, or if both positions are the same.
        /// Throws an ArgumentNullException if a given position is null.
        /// Throws an ArgumentException if a given position is not valid.
        /// </summary>
        internal static bool IsSmallerThanOrEqualTo(this Lsp.Position first, Lsp.Position second) =>
            !second.IsSmallerThan(first);

        /// <summary>
        /// Returns true if the given ranges overlap.
        /// Throws an ArgumentNullException if any of the given ranges is null.
        /// Throws an ArgumentException if any of the given ranges is not valid.
        /// </summary>
        internal static bool Overlaps(this Lsp.Range range1, Lsp.Range range2)
        {
            if (!IsValidRange(range1) || !IsValidRange(range2))
            {
                throw new ArgumentException("invalid range given for comparison");
            }
            var (first, second) = range1.Start.IsSmallerThan(range2.Start) ? (range1, range2) : (range2, range1);
            return second.Start.IsSmallerThan(first.End);
        }

        /// <summary>
        /// Verifies the given position and range, and returns true if the given position lays within the given range.
        /// If includeEnd is true then the end point of the range is considered to be part of the range,
        /// otherwise the range is considered to include the start but excludes the end point.
        /// Throws an ArgumentNullException if the given position or range is null.
        /// Throws an ArgumentException if the given position or range is not valid.
        /// </summary>
        internal static bool IsWithinRange(this Lsp.Position pos, Lsp.Range range, bool includeEnd = false)
        {
            if (!IsValidPosition(pos) || !IsValidRange(range))
            {
                throw new ArgumentException("invalid position or range given for comparison");
            }
            return range.Start.IsSmallerThanOrEqualTo(pos) && (includeEnd ? pos.IsSmallerThanOrEqualTo(range.End) : pos.IsSmallerThan(range.End));
        }

        /// <summary>
        /// Returns true if the given range is valid, i.e. if both start and end are valid positions, and start is smaller than or equal to end.
        /// Throws an ArgumentNullException if an argument is null.
        /// </summary>
        public static bool IsValidRange(Lsp.Range range) =>
            IsValidPosition(range?.Start) && IsValidPosition(range.End) && range.Start.IsSmallerThanOrEqualTo(range.End);

        /// <summary>
        /// Returns true if the given range is valid,
        /// i.e. if both start and end are valid positions within the given file, and start is smaller than or equal to end.
        /// Throws an ArgumentNullException if an argument is null.
        /// </summary>
        internal static bool IsValidRange(Lsp.Range range, FileContentManager file) =>
            IsValidPosition(range?.Start.ToQSharp(), file) && IsValidPosition(range.End.ToQSharp(), file) && range.Start.IsSmallerThanOrEqualTo(range.End);

        /// <summary>
        /// Returns the absolute position under the assumption that snd is relative to fst and both positions are zero-based.
        /// Throws an ArgumentNullException if a given position is null.
        /// Throws an ArgumentException if a given position is not valid.
        /// </summary>
        internal static Lsp.Position Add(this Lsp.Position fst, Lsp.Position snd)
        {
            if (!IsValidPosition(fst))
            {
                throw new ArgumentException(nameof(fst));
            }
            if (!IsValidPosition(snd))
            {
                throw new ArgumentException(nameof(snd));
            }
            return new Lsp.Position(fst.Line + snd.Line, snd.Line == 0 ? fst.Character + snd.Character : snd.Character);
        }

        /// <summary>
        /// Returns the position of fst relative to snd under the assumption that both positions are zero-based.
        /// Throws an ArgumentNullException if a given position is null.
        /// Throws an ArgumentException if a given position is not valid, or if fst is smaller than snd.
        /// </summary>
        internal static Lsp.Position Subtract(this Lsp.Position fst, Lsp.Position snd)
        {
            if (!IsValidPosition(fst))
            {
                throw new ArgumentException(nameof(fst));
            }
            if (!IsValidPosition(snd))
            {
                throw new ArgumentException(nameof(snd));
            }
            if (fst.IsSmallerThan(snd))
            {
                throw new ArgumentException(nameof(snd), "the position to subtract from needs to be larger than the position to subract");
            }
            var relPos = new Lsp.Position(fst.Line - snd.Line, fst.Line == snd.Line ? fst.Character - snd.Character : fst.Character);
            QsCompilerError.Verify(snd.Add(relPos).Equals(fst), "adding the relative position to snd does not equal fst");
            return relPos;
        }

        // tools for debugging

        public static string DiagnosticString(this Lsp.Range r) =>
            $"({r?.Start?.Line},{r?.Start?.Character}) - ({r?.End?.Line},{r?.End?.Character})";

        internal static string DiagnosticString(FileContentManager file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            var annotatedContent = new string[file.NrLines()];
            for (var lineNr = 0; lineNr < file.NrLines(); ++lineNr)
            {
                var line = file.GetLine(lineNr);
                var delimString = "[" + string.Join(",", line.StringDelimiters) + "] ";
                var prefix = delimString + string.Concat<string>(Enumerable.Repeat("*", line.ExcessBracketPositions.Count()));
                annotatedContent[lineNr] = $"{prefix}i{line.Indentation}: {line.WithoutEnding}";
            }
            return JoinLines(annotatedContent);
        }
    }
}
