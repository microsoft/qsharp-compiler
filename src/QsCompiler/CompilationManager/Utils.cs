// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Lsp = Microsoft.VisualStudio.LanguageServer.Protocol;
using Position = Microsoft.Quantum.QsCompiler.DataTypes.Position;
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

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
        [return: NotNullIfNotNull("content")]
        public static string? JoinLines(string[] content) =>
            content == null ? null : string.Join("", content); // *DO NOT MODIFY* how lines are joined - the compiler functionality depends on it!

        /// <summary>
        /// Given a string, replaces the range [<paramref name="startChar"/>, <paramref name="endChar"/>) with <paramref name="insert"/>.
        /// Returns null if the given text is null.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startChar"/> and <paramref name="endChar"/> do not denote a valid range within <paramref name="lineText"/>.</exception>
        [return: NotNullIfNotNull("lineText")]
        internal static string? GetChangedText(string? lineText, int startChar, int endChar, string insert)
        {
            if (lineText == null)
            {
                return null;
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
        /// Return a string with the new content of the (entire) lines in the range [start, end] where start and end are
        /// the start and end line of the given change.
        /// </summary>
        /// <exception cref="ArgumentException">The range is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The range is not contained in <paramref name="file"/>.</exception>
        internal static string GetTextChangedLines(FileContentManager file, TextDocumentContentChangeEvent change)
        {
            if (!file.ContainsRange(change.Range.ToQSharp()))
            {
                throw new ArgumentOutOfRangeException(nameof(change)); // range can be empty
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
        /// </summary>
        public static (List<T>, List<T>) Partition<T>(this IEnumerable<T> collection, Func<T, bool> predicate) =>
            (collection.Where(predicate).ToList(), collection.Where(x => !predicate(x)).ToList());

        /// <summary>
        /// Returns true if the given lock is either ReadLockHeld, or is UpgradeableReadLockHeld, or isWriteLockHeld.
        /// </summary>
        public static bool IsAtLeastReadLockHeld(this ReaderWriterLockSlim syncRoot) =>
            syncRoot.IsReadLockHeld || syncRoot.IsUpgradeableReadLockHeld || syncRoot.IsWriteLockHeld;

        // utils for dealing with positions and ranges

        /// <summary>
        /// Converts the language server protocol position into a Q# compiler position.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="position"/> is invalid.</exception>
        public static Position ToQSharp(this Lsp.Position position) =>
            Position.Create(position.Line, position.Character);

        /// <summary>
        /// Converts the Q# compiler position into a language server protocol position.
        /// </summary>
        public static Lsp.Position ToLsp(this Position position) =>
            new Lsp.Position(position.Line, position.Column);

        /// <summary>
        /// Converts the language server protocol range into a Q# compiler range.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="range"/> is invalid.</exception>
        public static Range ToQSharp(this Lsp.Range range) =>
            Range.Create(range.Start.ToQSharp(), range.End.ToQSharp());

        /// <summary>
        /// Converts the Q# compiler range into a language server protocol range.
        /// </summary>
        public static Lsp.Range ToLsp(this Range range) =>
            new Lsp.Range { Start = range.Start.ToLsp(), End = range.End.ToLsp() };

        /// <summary>
        /// Returns true if the position is within the bounds of the file contents.
        /// </summary>
        internal static bool ContainsPosition(this FileContentManager file, Position position) =>
            position.Line < file.NrLines() && position.Column <= file.GetLine(position.Line).Text.Length;

        /// <summary>
        /// Returns true if the range is within the bounds of the file contents.
        /// </summary>
        internal static bool ContainsRange(this FileContentManager file, Range range) =>
            file.ContainsPosition(range.Start) && file.ContainsPosition(range.End) && range.Start <= range.End;

        // tools for debugging

        public static string DiagnosticString(this Range r) =>
            $"({r?.Start?.Line},{r?.Start?.Column}) - ({r?.End?.Line},{r?.End?.Column})";
    }
}
