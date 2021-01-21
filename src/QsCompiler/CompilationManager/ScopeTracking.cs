// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Position = Microsoft.Quantum.QsCompiler.DataTypes.Position;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    public static class ScopeTracking
    {
        // routines used to verify properties that the ScopeTracking relies on before accepting an update

        /// <summary>
        /// Checks that all delimiters are within -1 and the string length, and that they are sorted in ascending order.
        /// </summary>
        /// <exception cref="ArgumentException">The checks failed.</exception>
        internal static void VerifyStringDelimiters(string text, IEnumerable<int> delimiters)
        {
            var last = -2;
            foreach (int delim in delimiters)
            {
                if (delim <= last)
                {
                    throw new ArgumentException($"the string delimiters need to be positive and sorted in ascending order");
                }
                last = delim;
            }
            if (last > text.Length)
            {
                throw new ArgumentException("out of range string delimiter");
            }
            if ((delimiters.Count() & 1) != 0)
            {
                throw new ArgumentException("expecting an even number of string delimiters");
            }
        }

        /// <summary>
        /// Checks that all positions are a valid index in the line text, and that they are sorted in ascending order.
        /// Checks that none of the positions lays within a string.
        /// </summary>
        /// <exception cref="ArgumentException">The checks failed.</exception>
        internal static void VerifyExcessBracketPositions(CodeLine line, IEnumerable<int> positions)
        {
            var last = -1;
            foreach (var pos in positions)
            {
                if (pos <= last)
                {
                    throw new ArgumentException($"the excess bracket positions need to be sorted in ascending order");
                }
                last = pos;
                if (IndexExcludingStrings(pos, line.StringDelimiters) < 0)
                {
                    throw new ArgumentException($"position for excess bracket is within a string");
                }
            }
            if (last >= line.WithoutEnding.Length)
            {
                throw new ArgumentException("out of range excess bracket position");
            }
        }

        /// <summary>
        /// Computes the updated code line based on the given previous line predecessing it,
        /// and compares its indentation with the current line at <paramref name="continueAt"/> in the given file.
        /// Returns the difference of the new indentation and the current one.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="continueAt"/> is less than zero or more than the number of lines in <paramref name="file"/>.
        /// </exception>
        internal static int GetIndentationChange(FileContentManager file, int continueAt, CodeLine previous) // previous: last element before the one at continueAt
        {
            if (continueAt < 0 || continueAt > file.NrLines())
            {
                throw new ArgumentOutOfRangeException(nameof(continueAt));
            }

            if (continueAt == file.NrLines())
            {
                return 0;
            }
            var continuation = file.GetLine(continueAt);
            var updatedContinuation = ComputeCodeLines(new string[] { continuation.Text }, previous).Single();
            return updatedContinuation.Indentation - continuation.Indentation;
        }

        // private utils for computing text updates

        /// <summary>
        /// Returns true if the given line is not null and the line contains a (last) delimiter
        /// that is equal to the text length, and returns false otherwise.
        /// </summary>
        private static bool ContinueString(CodeLine? line)
        {
            if (line == null)
            {
                return false;
            }
            var delimiters = line.StringDelimiters;
            return delimiters.Count() != 0 && delimiters.Last() == line.Text.Length;
        }

        /// <summary>
        /// Computes the location of the string delimiters within a given text.
        /// </summary>
        private static IEnumerable<int> ComputeStringDelimiters(string text, bool isContinuation)
        {
            var nrDelimiters = 0;
            if (isContinuation)
            {
                ++nrDelimiters;
                yield return -1;
            }
            var stringLength = text.Length;
            while (text != string.Empty)
            {
                var commentIndex = (nrDelimiters & 1) == 0 ? text.IndexOf("//") : -1; // only if we are currently not inside a string do we need to check for a potential comment start
                if (commentIndex < 0)
                {
                    commentIndex = text.Length;
                }

                var index = text.IndexOf('"');
                if ((nrDelimiters & 1) != 0)
                {
                    // if we are currently within a string, we need to ignore string delimiters of the form \"
                    while (index > 0 && text[index - 1] == '\\')
                    {
                        var next = text.Substring(index + 1).IndexOf('"');
                        index = next < 0 ? next : index + 1 + next;
                    }
                }

                if (commentIndex < index)
                {
                    break; // fine also if index = -1
                }
                if (index < 0)
                {
                    text = string.Empty;
                }
                else
                {
                    ++nrDelimiters;
                    yield return index + stringLength - text.Length;
                    text = text.Substring(index + 1);
                }
            }
            if ((nrDelimiters & 1) != 0)
            {
                yield return stringLength;
            }
        }

        // utils related to filtering irrelevant text for scope and error processing, and parsing

        private static int StartDelimiter(int delimiter) => delimiter; // used to make sure RemoveStrings and IndexInFullString are in sync

        private static int EndDelimiter(int delimiter) => delimiter + 1;

        /// <summary>
        /// Given an index computed before applying RemoveStrings, computes the index after applying RemoveStrings.
        /// Returns -1 if the given index is within a string.
        /// </summary>
        private static int IndexExcludingStrings(int indexInFullText, IEnumerable<int> delimiters)
        {
            var iter = delimiters.GetEnumerator();
            var index = indexInFullText;
            int GetStart(int pos) => pos < 0 ? 0 : StartDelimiter(pos);

            while (iter.MoveNext() && GetStart(iter.Current) <= index)
            {
                // Start- and EndDelimiter are used to make sure this function stays in sync with RemoveStrings
                index += GetStart(iter.Current);
                iter.MoveNext();
                index -= EndDelimiter(iter.Current); // note: it should not be possible, that we ever have the case iter.Current == text.Length (unless indexInFullText and delimiters mismatch...)
                if (indexInFullText <= iter.Current)
                {
                    // iter.Current, not EndDelimiter(iter.Current) is always correct
                    return -1;
                }
            }
            return index;
        }

        /// <summary>
        /// Given and index computed before applying RelevantCode, computes the index after applying RelevantCode.
        /// Returns -1 if the given index is negative, or is within a string or a comment, or denotes an excess bracket.
        /// </summary>
        private static int IndexInRelevantCode(int indexInFullText, CodeLine line)
        {
            if (indexInFullText < 0 || line.WithoutEnding.Length <= indexInFullText)
            {
                return -1;
            }

            var index = IndexExcludingStrings(indexInFullText, line.StringDelimiters);
            if (index < 0)
            {
                return index; // saving the trouble of computing the loop below
            }

            foreach (var pos in line.ExcessBracketPositions.Reverse().Select(p => IndexExcludingStrings(p, line.StringDelimiters)))
            {
                if (pos == index)
                {
                    return -1;
                }
                if (pos < index)
                {
                    --index;
                }
            }
            return index;
        }

        /// <summary>
        /// Given an index computed after applying RemoveStrings, computes the index in the original text.
        /// </summary>
        private static int IndexIncludingStrings(int indexInTrimmed, IEnumerable<int> delimiters)
        {
            var iter = delimiters.GetEnumerator();
            var index = indexInTrimmed;
            int GetStart(int pos) => pos < 0 ? 0 : StartDelimiter(pos);

            while (iter.MoveNext() && GetStart(iter.Current) <= index)
            {
                // Start- and EndDelimiter are used to make sure this function stays in sync with RemoveStrings
                index -= GetStart(iter.Current);
                iter.MoveNext();
                index += EndDelimiter(iter.Current); // note: it should not be possible, that we ever have the case iter.Current == text.Length (unless indexInTrimmed and delimiters mismatch...)
            }
            return index;
        }

        /// <summary>
        /// Given an index computed after applying RelevantCode, computes the index in the original text.
        /// </summary>
        private static int IndexInFullString(int indexInTrimmed, CodeLine line)
        {
            var index = IndexIncludingStrings(indexInTrimmed, line.StringDelimiters);
            foreach (var pos in line.ExcessBracketPositions)
            {
                if (pos <= index)
                {
                    ++index;
                }
            }
            if (index >= line.WithoutEnding.Length)
            {
                throw new ArgumentException("mismatch between the given index in the relevant code and the given line");
            }
            return index;
        }

        /// <summary>
        /// Verifies the given stringDelimiters and returns the given text without the content between the delimiters.
        /// </summary>
        private static string RemoveStrings(string text, IEnumerable<int> stringDelimiters)
        {
            QsCompilerError.RaiseOnFailure(() => VerifyStringDelimiters(text, stringDelimiters), "invalid delimiters for given text in call to RemoveStrings");

            var iter = stringDelimiters.GetEnumerator();
            var trimmed =
                iter.MoveNext() ?
                iter.Current < 0 ? string.Empty : text.Substring(0, StartDelimiter(iter.Current)) :
                text;

            while (iter.MoveNext() && iter.Current < text.Length)
            {
                // Note: if modifications here are needed, modify Start- and EndDelimiter to make sure these changes are reflected in IndexInFullString
                var end = iter.Current == text.Length ? text.Length : EndDelimiter(iter.Current); // end of a substring
                var start = iter.MoveNext() ? StartDelimiter(iter.Current) : text.Length;
                trimmed += text.Substring(end, start - end);
            }
            return trimmed;
        }

        /// <summary>
        /// Strips the text of all strings and a potential end of line comment.
        /// </summary>
        private static string RemoveStringsAndComment(CodeLine line)
        {
            return RemoveStrings(line.WithoutEnding + line.LineEnding, line.StringDelimiters);
        }

        /// <summary>
        /// Strips the text of all strings, excess closing brackets, and a potential end of line comment.
        /// </summary>
        private static string RelevantCode(CodeLine line)
        {
            var delimiters = line.StringDelimiters;
            var stripped = RemoveStringsAndComment(line); // will raise an exception if line is null
            foreach (var index in line.ExcessBracketPositions.Reverse().Select(pos => IndexExcludingStrings(pos, delimiters)))
            {
                stripped = stripped.Remove(index, 1);
            }
            return stripped;
        }

        // utils called upon language processing to get suitable substrings to parse, and related subroutines

        /// <summary>
        /// Computes the string delimiters for a truncated substring of length count starting at start based on the delimiters of the original string.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="start"/> or <paramref name="count"/> is smaller than zero.</exception>
        private static IEnumerable<int> TruncateStringDelimiters(IEnumerable<int> delimiters, int start, int count)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var nrDelim = 0;
            delimiters = delimiters.SkipWhile(delim => delim < start && ++nrDelim > 0);
            if (delimiters.Any() && (nrDelim & 1) != 0)
            {
                yield return -1;
            }

            var iter = delimiters.TakeWhile(delim => delim < start + count).GetEnumerator();
            for (nrDelim = 0; iter.MoveNext(); ++nrDelim)
            {
                yield return iter.Current - start;
            }

            if ((nrDelim & 1) != 0)
            {
                yield return count;
            }
        }

        /// <summary>
        /// Returns the result of FindIndex applied to the text on the given line when ignoring end of line comments, content within strings,
        /// and - if ingoreExcessBrackets is set - excessive closing brackets.
        /// The returned index is relative to the original text.
        /// If the value returned by FindIndex is smaller than zero it is returned unchanged.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Start and count do not define a valid range in the text of the given <paramref name="line"/>.</exception>
        internal static int FindInCode(this CodeLine line, Func<string, int> findIndex, bool ignoreExcessBrackets = true)
        {
            if (ignoreExcessBrackets)
            {
                return IndexInFullString(findIndex(RelevantCode(line)), line); // fine also for index = -1
            }
            else
            {
                return IndexIncludingStrings(findIndex(RemoveStringsAndComment(line)), line.StringDelimiters);
            }
        }

        /// <summary>
        /// Returns the result of FindIndex applied to the text on the substring of length count starting at start
        /// when ignoring end of line comments, content within strings, and - if ingoreExcessBrackets is set - excessive closing brackets.
        /// Important: This function returns the index relative to the original text, not the substring.
        /// If the value returned by FindIndex is smaller than zero it is returned unchanged.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> and <paramref name="count"/> do not define a valid range in the text of the given <paramref name="line"/>.</exception>
        internal static int FindInCode(this CodeLine line, Func<string, int> findIndex, int start, int count, bool ignoreExcessBrackets = true)
        {
            var truncatedDelims = TruncateStringDelimiters(line.StringDelimiters, start, count);
            var truncatedText = line.Text.Substring(start, count); // will throw if start and count are out of range
            var truncatedExcessClosings =
                line.ExcessBracketPositions
                .Where(pos => start <= pos && pos < start + count)
                .Select(pos => pos - start);

            var shiftedCommentIndex = line.WithoutEnding.Length - start;
            var truncatedLine = new CodeLine(truncatedText, truncatedDelims, shiftedCommentIndex < count ? shiftedCommentIndex : count, 0, truncatedExcessClosings); // line indentation is irrelevant here
            var foundIndex = FindInCode(truncatedLine, findIndex, ignoreExcessBrackets);
            return foundIndex < 0 ? foundIndex : start + foundIndex;
        }

        /// <summary>
        /// Givent a position, verifies that the position is within the given file, and
        /// returns the effective indentation (i.e. the indentation when ignoring excess brackets throughout the file)
        /// at that position (i.e. not including the char at the given position).
        /// </summary>
        internal static int IndentationAt(this FileContentManager file, Position pos)
        {
            if (!file.ContainsPosition(pos))
            {
                throw new ArgumentException("given position is not within file");
            }
            var line = file.GetLine(pos.Line);
            var index = pos.Column;

            // check if the given position is within a string or a comment, or denotes an excess bracket,
            // and find the next closest position that isn't
            if (index >= line.WithoutEnding.Length)
            {
                return line.FinalIndentation(); // not necessary, but saves doing the rest of the computation
            }
            index = line.FindInCode(trimmed => trimmed.Length - 1, 0, pos.Column); // if the given position is within a string, then this is the most convenient way to get the closest position that isn't..
            if (index < 0)
            {
                return line.Indentation; // perfectly valid scenario (if there is no relevant code before the given position)
            }

            // check how much the indentation changes in all valid (i.e. non-comment, non-string, non-excess-brackets) code up to that point
            // NOTE: per specification, this routine returns the indentation not incuding the char at the given position,
            // but if this char was within non-code text, then we need to include this char ... see (*)
            var indexInCode = IndexInRelevantCode(index, line);
            QsCompilerError.Verify(indexInCode >= 0, "index in code should be positive");
            var code = RelevantCode(line).Substring(0, index < pos.Column ? indexInCode + 1 : indexInCode); // (*) - yep, that's a bit awkward, but the cleanest I can come up with right now
            var indentation = line.Indentation + NrIndents(code) - NrUnindents(code);
            QsCompilerError.Verify(indentation >= 0, "computed indentation at any position in the file should be positive");
            return indentation;
        }

        // utils for computing indentations and excess closings

        private static int NrIndents(string text) => text.Length - text.Replace("{", "").Length;

        private static int NrUnindents(string text) => text.Length - text.Replace("}", "").Length;

        /// <summary>
        /// Returns the indentation at the end of the line.
        /// Note: the indentation saved in CodeLine is the indentation that a variable declared on that line would have.
        /// </summary>
        private static int FinalIndentation(this CodeLine line)
        {
            var code = RelevantCode(line);
            return line.Indentation + NrIndents(code) - NrUnindents(code);
        }

        /// <summary>
        /// Computes the number of excess brackets on the given line based in the given line number
        /// and the list of lines containing the excess closings before that line.
        /// </summary>
        private static int[] ComputeExcessClosings(CodeLine line, int effectiveIndent)
        {
            var additionalExcessClosings = new List<int>();
            var relevantText = RemoveStringsAndComment(line);
            for (var unprocessed = relevantText; unprocessed != string.Empty;)
            {
                var nextOpen = unprocessed.IndexOf('{');
                if (nextOpen < 0)
                {
                    nextOpen = unprocessed.Length;
                }
                var nextClose = unprocessed.IndexOf('}');
                if (nextClose < 0)
                {
                    nextClose = unprocessed.Length;
                }

                if (nextClose < nextOpen)
                {
                    if (--effectiveIndent + additionalExcessClosings.Count() < 0)
                    {
                        additionalExcessClosings.Add(IndexIncludingStrings(nextClose + relevantText.Length - unprocessed.Length, line.StringDelimiters));
                    }
                    unprocessed = unprocessed.Substring(nextClose + 1);
                }
                else if (nextOpen < nextClose)
                {
                    ++effectiveIndent;
                    unprocessed = unprocessed.Substring(nextOpen + 1);
                }
                else
                {
                    unprocessed = string.Empty;
                }
            }
            return additionalExcessClosings.ToArray();
        }

        // computing the objects needed to update the content in the editor state

        /// <summary>
        /// Based on the previous line, initializes the new CodeLines for the given texts
        /// with suitable string delimiters and the correct end of line comment position,
        /// leaving the indentation at its default value and the excess brackets uncomputed.
        /// The previous line being null or not provided indicates that there is no previous line.
        /// </summary>
        private static IEnumerable<CodeLine> InitializeCodeLines(IEnumerable<string> texts, CodeLine? previousLine = null)
        {
            var continueString = ContinueString(previousLine);
            foreach (string text in texts)
            {
                // computing suitable string delimiters
                var delimiters = ComputeStringDelimiters(text, continueString);
                var commentStart = IndexIncludingStrings(RemoveStrings(text, delimiters).IndexOf("//"), delimiters.ToArray());

                // initializes the code line with a default indentation of zero, that will be set to a suitable value during the computation of the excess brackets
                var line = new CodeLine(text, delimiters, commentStart < 0 ? text.Length : commentStart);
                continueString = ContinueString(line);
                yield return line;
            }
        }

        /// <summary>
        /// Given the initial indentation of a sequence of CodeLines, and a sequence of code lines with the correct string delimiters set,
        /// computes and sets the correct indentation level and excess bracket positions for each line.
        /// The previous line being null or not provided indicates that there is no previous line.
        /// </summary>
        private static IEnumerable<CodeLine> SetIndentations(IEnumerable<CodeLine> lines, int currentIndentation)
        {
            foreach (var line in lines)
            {
                var updated = line.SetIndentation(currentIndentation);

                var stripped = RemoveStringsAndComment(updated);
                var nrIndents = NrIndents(stripped);
                var nrUnindents = NrUnindents(stripped);

                if (currentIndentation - nrUnindents < 0)
                {
                    // in this case it is possible (but not necessarily the case) that there are excess brackets
                    // if closings occur before openings, then we can have excess brackets even when the indentation is larger than zero
                    updated = updated.SetExcessBrackets(ComputeExcessClosings(updated, currentIndentation));
                    currentIndentation += updated.ExcessBracketPositions.Count();
                }
                else
                {
                    updated = updated.SetExcessBrackets(new List<int>().AsReadOnly());
                }

                currentIndentation += nrIndents - nrUnindents;
                QsCompilerError.Verify(currentIndentation >= 0, "initial indentation should always be larger or equal to zero");
                yield return updated;
            }
        }

        /// <summary>
        /// Based on the previous line, computes the new CodeLines for the given texts.
        /// The previous line being null or not provided indicates that there is no previous line.
        /// </summary>
        private static IEnumerable<CodeLine> ComputeCodeLines(IEnumerable<string> texts, CodeLine? previousLine = null) =>
            SetIndentations(InitializeCodeLines(texts, previousLine), previousLine == null ? 0 : previousLine.FinalIndentation());

        /// <summary>
        /// Returns an enumerable sequence of new CodeLines when the initial indentation of the sequence is initialIndentation,
        /// (re-)computing the positions of excess brackets if needed.
        /// </summary>
        private static List<CodeLine> GetUpdatedLines(this IEnumerable<CodeLine> lines, int initialIndentation) =>
            SetIndentations(lines, initialIndentation).ToList();

        /// <summary>
        /// Computes the excess closing and scope error updates for the given replacements at the position specified by start and count in the given file.
        /// Returns a sequence of CodeLines for the remaining file, if the made replacements require updating the remaining file as well, and null otherwise.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="replacements"/> does not at least contain one <see cref="CodeLine"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The range defined by <paramref name="start"/> and <paramref name="count"/> is not within <paramref name="file"/>, or <paramref name="count"/> is less than 1.
        /// </exception>
        private static IEnumerable<CodeLine>? ComputeUpdates(FileContentManager file, int start, int count, CodeLine[] replacements)
        {
            if (start < 0 || start >= file.NrLines())
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }
            if (count < 1 || start + count > file.NrLines())
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (replacements.Length == 0)
            {
                throw new ArgumentException("replacements cannot be empty");
            }
            var continueAtInFile = start + count;
            var remainingLines = file.GetLines(continueAtInFile, file.NrLines() - continueAtInFile);

            // how much the effective indentation (i.e. absolute indentation plus nr of excess closings up to that point) changed determines how much an what we need to update:
            var indentationChange = GetIndentationChange(file, continueAtInFile, replacements.Last());
            var requiresStringDelimiterUpdate = ContinueString(file.GetLine(continueAtInFile - 1)) ^ ContinueString(replacements.Last());
            if (requiresStringDelimiterUpdate)
            {
                // we need to recompute everything if the interpretation of what is code and what is a string changes...
                // since the interpretation of the remaining file changed, we need to update the entire file from start onwards
                return ComputeCodeLines(remainingLines.Select(line => line.Text), replacements.Last()).ToList();
            }
            else if (indentationChange != 0)
            {
                // if the replacements has more effective closing brackets (not just excess closings!) than the current part that will be replaced has,
                // then we need check the text of the remaining file as well in order to compute the correct update
                // if it has less (indentationChange > 0), then we could in principle simplify things somewhat by simply discarding the corresponding number of excess closing brackets
                return remainingLines.GetUpdatedLines(remainingLines.First().Indentation + indentationChange);
            }
            else
            {
                return null;
            }
        }

        // routines used to compute scope diagnostics updates

        /// <summary>
        /// Given the total number of excess closings in the file
        /// checks for both an unclosed scope and a missing string ending on lastLine, and adds the corresponding error(s) to updatedScopeErrors.
        /// </summary>
        /// <exception cref="ArgumentException">The number of lines in <paramref name="file"/> is zero.</exception>
        private static IEnumerable<Diagnostic> CheckForMissingClosings(this FileContentManager file)
        {
            if (file.NrLines() == 0)
            {
                throw new ArgumentException("the number of lines in a file can never be zero");
            }
            var lastLine = file.GetLine(file.NrLines() - 1);
            if (lastLine.FinalIndentation() > 0)
            {
                yield return Errors.MissingClosingBracketError(file.FileName, Position.Create(file.NrLines() - 1, lastLine.Text.Length));
            }
            if (ContinueString(lastLine))
            {
                yield return Errors.MissingStringDelimiterError(file.FileName, Position.Create(file.NrLines() - 1, lastLine.Text.Length));
            }
        }

        /// <summary>
        /// Computes excess bracket errors for the given range of lines in file based on the corresponding CodeLine.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The range [<paramref name="start"/>, <paramref name="start"/> + <paramref name="count"/>) is not within <paramref name="file"/>.</exception>
        private static IEnumerable<Diagnostic> ComputeScopeDiagnostics(this FileContentManager file, int start, int count)
        {
            foreach (var line in file.GetLines(start, count))
            {
                foreach (var pos in line.ExcessBracketPositions)
                {
                    yield return Errors.ExcessBracketError(file.FileName, Position.Create(start, pos));
                }
                ++start;
            }
        }

        /// <summary>
        /// Computes excess bracket errors for the given range of lines in file based on the corresponding CodeLine.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> is not within <paramref name="file"/>.</exception>
        private static IEnumerable<Diagnostic> ComputeScopeDiagnostics(this FileContentManager file, int start) =>
            ComputeScopeDiagnostics(file, start, file == null ? 0 : file.NrLines() - start);

        // the actual update routine

        /// <summary>
        /// Attempts to perform the necessary updates when replacing the range [start, start + count) by newText for the given file
        /// wrapping each step in a QsCompilerError.RaiseOnFailure.
        /// </summary>
        private static void Update(this FileContentManager file, int start, int count, IEnumerable<string> newText)
        {
            CodeLine[] replacements = QsCompilerError.RaiseOnFailure(
                () => ComputeCodeLines(newText, start > 0 ? file.GetLine(start - 1) : null).ToArray(),
                "scope tracking update failed during computing the replacements");

            IEnumerable<CodeLine>? updateRemaining = QsCompilerError.RaiseOnFailure(
                () => ComputeUpdates(file, start, count, replacements),
                "scope tracking update failed during computing the updates");

            QsCompilerError.RaiseOnFailure(
                () =>
                {
                    if (updateRemaining == null)
                    {
                        file.ContentUpdate(start, count, replacements);
                    }
                    else
                    {
                        file.ContentUpdate(start, file.NrLines() - start, replacements.Concat(updateRemaining).ToArray());
                    }
                },
                "the proposed ContentUpdate failed");

            QsCompilerError.RaiseOnFailure(
                () =>
                {
                    if (updateRemaining == null)
                    {
                        file.AddScopeDiagnostics(file.ComputeScopeDiagnostics(start, replacements.Length));
                    }
                    else
                    {
                        file.AddScopeDiagnostics(file.ComputeScopeDiagnostics(start));
                    }
                    file.AddScopeDiagnostics(file.CheckForMissingClosings());
                },
                "updating the scope diagnostics failed");
        }

        // routine(s) called by the FileContentManager upon updating a file

        /// <summary>
        /// Attempts to compute an incremental update for the change specified by start, count and newText, and updates file accordingly.
        /// The given argument newText replaces the entire lines from start to (but not including) start + count.
        /// If the given change is null, then (only) the currently queued unprocessed changes are processed.
        /// </summary>
        internal static void UpdateScopeTacking(this FileContentManager file, TextDocumentContentChangeEvent? change)
        {
            /// <summary>
            /// Replaces the lines in the range [start, end] with those for the given text.
            /// </summary>
            void ComputeUpdate(int start, int end, string text)
            {
                QsCompilerError.Verify(start >= 0 && end >= start && end < file.NrLines(), "invalid range for update");

                // since both LF and CR in VS cause a line break on their own,
                // we need to check if the change causes subequent CR LF to merge into a single line break
                if (text.StartsWith(Utils.LF) && start > 0 && file.GetLine(start - 1).Text.EndsWith(Utils.CR))
                {
                    text = file.GetLine(--start).Text + text;
                }

                // we need to check if the change causes the next line to merge with the (last) changed line
                if (end + 1 < file.NrLines() && !Utils.EndOfLine.Match(text).Success)
                {
                    text = text + file.GetLine(++end).Text;
                }

                var newLines = Utils.SplitLines(text);
                var count = end - start + 1;

                // note that the last line in the file won't end with a line break,
                // and is hence only captured by SplitLines if it is not empty
                // -> we therefore manually add the last line in the file if it is empty
                if (newLines.Length == 0 || // the case if the file will be empty after the update
                    (start + count == file.NrLines() && Utils.EndOfLine.Match(newLines.Last()).Success))
                {
                    newLines = newLines.Concat(new string[] { string.Empty }).ToArray();
                }

                QsCompilerError.Verify(newLines.Any(), "should have at least one line to replace");
                file.Update(start, count, newLines);
            }

            file.SyncRoot.EnterUpgradeableReadLock();
            try
            {
                // process the currently queued changes if necessary
                if (file.DequeueUnprocessedChanges(out var start, out var text))
                {
                    ComputeUpdate(start, start, text);
                }

                // process the given change if necessary
                if (change != null)
                {
                    ComputeUpdate(change.Range.Start.Line, change.Range.End.Line, Utils.GetTextChangedLines(file, change));
                }
            }
            finally
            {
                file.SyncRoot.ExitUpgradeableReadLock();
            }
        }
    }
}
