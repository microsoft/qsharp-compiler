﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.TextProcessing;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    /// <summary>
    /// handles all communication with the Q# parser
    /// </summary>
    internal static class TextProcessor
    {
        // routines used for a first round of checking for syntactic errors related to how statements can be formatted (e.g. a non-empty statement ending in '}' is certainly incorrect)

        /// <summary>
        /// For any fragment where the code only consistes of whitespace,
        /// adds a warning to the returned diagnostics if such a fragment terminates in a semicolon,
        /// adds an error to the returned diagnostics if it ends in and opening bracket.
        /// Throws an ArgumentNullException if the given diagnostics or fragments are null.
        /// </summary>
        private static IEnumerable<Diagnostic> CheckForEmptyFragments(this IEnumerable<CodeFragment> fragments, string filename)
        {
            if (fragments == null)
            {
                throw new ArgumentNullException(nameof(fragments));
            }
            // opting to not complain about semicolons not following code anywhere in the file (i.e. on any scope)
            var diagnostics = fragments.Where(snippet => snippet.Text.Length == 0 && snippet.FollowedBy == ';')
                .Select(snippet => Warnings.EmptyStatementWarning(filename, snippet.GetRange().End))
                .Concat(fragments.Where(snippet => snippet.Text.Length == 0 && snippet.FollowedBy == '{')
                .Select(snippet => Errors.MisplacedOpeningBracketError(filename, snippet.GetRange().End)));
            return diagnostics.ToList(); // in case fragments change
        }

        /// <summary>
        /// Compares the saved fragment ending of the given fragment against the expected continuation and
        /// adds the corresponding error to the returned diagnostics if they don't match.
        /// Throws an ArgumentException if the code fragment kind of the given fragment is unspecified (i.e. null).
        /// Throws an ArgumentNullException if the diagnostics are null.
        /// </summary>
        private static IEnumerable<Diagnostic> CheckFragmentDelimiters(this CodeFragment fragment, string filename)
        {
            if (fragment?.Kind == null)
            {
                throw new ArgumentException("missing specification of the fragment kind");
            }
            var code = fragment.Kind.InvalidEnding;
            if (Diagnostics.ExpectedEnding(code) != fragment.FollowedBy)
            {
                yield return Errors.InvalidFragmentEnding(filename, code, fragment.GetRange().End);
            }
        }

        /// <summary>
        /// Calls the Q# parser on each fragment, splitting one fragment into several if necessary
        /// (i.e. modifies the list of given fragments!).
        /// Fragments for which the code only consists of whitespace are left unchanged (i.e. the Kind remains set to null).
        /// Adds a suitable error to the returned diagnostics for each fragment that cannot be processed.
        /// Raises an ArgumentNullException if the given diagnostics or fragments are null.
        /// </summary>
        private static IEnumerable<Diagnostic> ParseCode(ref List<CodeFragment> fragments, string filename)
        {
            if (fragments == null)
            {
                throw new ArgumentNullException(nameof(fragments));
            }
            var processedFragments = new List<CodeFragment>(fragments.Count());
            var diagnostics = new List<Diagnostic>();

            foreach (var snippet in fragments)
            {
                var snippetStart = snippet.GetRange().Start;
                var outputs = Parsing.ProcessCodeFragment(snippet.Text);
                for (var outputIndex = 0; outputIndex < outputs.Length; ++outputIndex)
                {
                    var output = outputs[outputIndex];
                    var fragmentRange = DiagnosticTools.GetAbsoluteRange(snippetStart, output.Range);
                    var fragment = new CodeFragment(
                        snippet.Indentation,
                        fragmentRange,
                        output.Text.Value,
                        outputIndex == outputs.Length - 1 ? snippet.FollowedBy : CodeFragment.MissingDelimiter,
                        output.Kind);
                    processedFragments.Add(fragment);

                    var checkEnding = true; // if there is already a diagnostic overlapping with the ending, then don't bother checking the ending
                    foreach (var fragmentDiagnostic in output.Diagnostics)
                    {
                        var generated = Diagnostics.Generate(filename, fragmentDiagnostic, fragmentRange.Start);
                        diagnostics.Add(generated);

                        var fragmentEnd = fragment.GetRange().End;
                        var diagnosticGoesUpToFragmentEnd = fragmentEnd.IsWithinRange(generated.Range) || fragmentEnd.Equals(generated.Range.End);
                        if (fragmentDiagnostic.Diagnostic.IsError && diagnosticGoesUpToFragmentEnd)
                        {
                            checkEnding = false;
                        }
                    }
                    if (checkEnding)
                    {
                        diagnostics.AddRange(fragment.CheckFragmentDelimiters(filename));
                    }
                }
                if (outputs.Length == 0)
                {
                    processedFragments.Add(snippet); // keep empty fragments around (note that the kind is set to null in this case!)
                }
            }
            QsCompilerError.RaiseOnFailure(() => ContextBuilder.VerifyTokenOrdering(processedFragments), "processed fragments are not ordered properly and/or overlap");
            fragments = processedFragments;
            return diagnostics;
        }

        // private utils related to extracting file content

        /// <summary>
        /// Checks that the given range is a valid range in file, and returns the text in the given range in concatenated form
        /// stripping (only) end of line comments (and not removing excess brackets).
        /// Note: the End position of the given range is *not* part of the returned string.
        /// </summary>
        private static string GetCodeSnippet(this FileContentManager file, LSP.Range range)
        {
            if (!Utils.IsValidRange(range, file))
            {
                throw new ArgumentException($"cannot extract code snippet for the given range \n range: {range.DiagnosticString()}");
            }
            string CodeLine(CodeLine line) => line.WithoutEnding + line.LineEnding;

            var start = range.Start.Line;
            var count = range.End.Line - start + 1;

            var firstLine = CodeLine(file.GetLine(start));
            if (count == 1)
            {
                return firstLine.Substring(range.Start.Character, range.End.Character - range.Start.Character);
            }

            var lastLine = CodeLine(file.GetLine(range.End.Line));
            var prepend = firstLine.Substring(range.Start.Character);
            var append = lastLine.Substring(0, range.End.Character);

            var middle = file.GetLines(start + 1, count - 2).Select(CodeLine).ToArray();
            if (middle.Length == 0)
            {
                return Utils.JoinLines(new string[] { prepend, append });
            }
            else
            {
                return Utils.JoinLines(new string[] { prepend, Utils.JoinLines(middle), append }); // Note: use JoinLines here to get accurate position infos for errors
            }
        }

        // private utils for determining suitable ranges to parse

        private static readonly Func<string, int> StatementEndDelimiters =
            code => code.LastIndexOfAny(CodeFragment.DelimitingChars.ToArray());

        private static readonly Func<string, int> StatementStartDelimiters =
            code => code.IndexOfAny(CodeFragment.DelimitingChars.ToArray());

        /// <summary>
        /// Finds the position of the statement end closest to the end of the line,
        /// ignoring strings and comments, but not excess brackets.
        /// </summary>
        private static int StatementEnd(this CodeLine line)
        {
            return line.FindInCode(StatementEndDelimiters, false);
        }

        /// <summary>
        /// Finds the position of the statement start closest to the beginning of the line,
        /// ignoring strings and comments, but not excess brackets.
        /// </summary>
        private static int StatementStart(this CodeLine line)
        {
            return line.FindInCode(StatementStartDelimiters, false);
        }

        /// <summary>
        /// Finds the position of the statement end closest to the end of the line fragment defined by start and count,
        /// ignoring strings and comments, but not excess brackets.
        /// </summary>
        private static int StatementEnd(this CodeLine line, int start, int count)
        {
            return line.FindInCode(StatementEndDelimiters, start, count, false);
        }

        /// <summary>
        /// Finds the position of the statement start closest to the beginning of the line fragment after start,
        /// ignoring strings and comments, but not excess brackets.
        /// </summary>
        private static int StatementStart(this CodeLine line, int start)
        {
            return line.FindInCode(StatementStartDelimiters, start, line.WithoutEnding.Length - start, false);
        }

        /// <summary>
        /// Returns the Position after the last character in the file (including comments).
        /// Throws an ArgumentNullException is file is null or does not have any content.
        /// </summary>
        public static Position End(this FileContentManager file)
        {
            if (file == null || file.NrLines() == 0)
            {
                throw new ArgumentNullException(nameof(file), "file is null or empty");
            }
            return new Position(file.NrLines() - 1, file.GetLine(file.NrLines() - 1).Text.Length);
        }

        /// <summary>
        /// Returns the Position right after where the last relevant (i.e. non-comment) code in the file ends,
        /// or the position (0,0) if no such line exists.
        /// Throws an ArgumentNullException if file is null or does not contain any lines.
        /// </summary>
        private static Position LastInFile(FileContentManager file)
        {
            if (file == null || file.NrLines() == 0)
            {
                throw new ArgumentNullException(nameof(file), "file is null or missing content");
            }
            var endIndex = file.NrLines();
            while (endIndex-- > 0 && file.GetLine(endIndex).WithoutEnding.Trim().Length == 0)
            {
            }
            return endIndex < 0 ? new Position(0, 0) : new Position(endIndex, file.GetLine(endIndex).WithoutEnding.Length);
        }

        /// <summary>
        /// Returns the position right after where the fragment containing the given position ends.
        /// If the closest previous ending was on the last character in a line, then the returned position is on the same line after the last character.
        /// Updates the given position to point to the first character in the fragment that contains code.
        /// Throws an ArgumentException if the given position is not smaller than the position after the last piece of code in the file (given by LastInFile).
        /// Throws an ArgumentNullException if any of the arguments is null.
        /// Throws an ArgumentException if the given position is not within file.
        /// </summary>
        internal static Position FragmentEnd(this FileContentManager file, ref Position current)
        {
            var lastInFile = LastInFile(file);
            if (!Utils.IsValidPosition(current, file))
            {
                throw new ArgumentException("given position is not within file");
            }
            if (lastInFile.IsSmallerThanOrEqualTo(current))
            {
                throw new ArgumentException("no fragment exists at the given position");
            }

            var text = file.GetLine(current.Line).WithoutEnding;
            if (current.Character > text.Length || text.Substring(current.Character).TrimEnd().Length == 0)
            {
                var trimmed = string.Empty;
                while (trimmed.Length == 0 && ++current.Line < file.NrLines())
                {
                    trimmed = file.GetLine(current.Line).WithoutEnding.TrimStart();
                }
                if (current.Line < file.NrLines())
                {
                    current.Character = file.GetLine(current.Line).WithoutEnding.Length - trimmed.Length;
                }
                else
                {
                    current = lastInFile;
                    return lastInFile;
                }
            }

            var endIndex = current.Line;
            var endChar = file.GetLine(endIndex).StatementStart(current.Character);
            while (endChar < 0 && ++endIndex < file.NrLines())
            {
                endChar = file.GetLine(endIndex).StatementStart();
            }
            return endIndex < file.NrLines() ? new Position(endIndex, endChar + 1) : lastInFile;
        }

        /// <summary>
        /// Returns the position right after where the fragment before the one containing the given position ends.
        /// If the closest previous ending was on the last character in a line, then the returned position is on the same line after the last character.
        /// If there is no such fragment, returns the position (0,0).
        /// Throws an ArgumentNullException if any of the arguments is null.
        /// Throws an ArgumentException if the given position is not within file.
        /// </summary>
        private static Position PositionAfterPrevious(this FileContentManager file, Position current)
        {
            if (!Utils.IsValidPosition(current, file))
            {
                throw new ArgumentException("given position is not within file");
            }
            var startIndex = current.Line;
            var startChar = file.GetLine(startIndex).StatementEnd(0, current.Character);
            while (startChar < 0 && startIndex-- > 0)
            {
                startChar = file.GetLine(startIndex).StatementEnd();
            }
            return startIndex < 0 ? new Position(0, 0) : new Position(startIndex, startChar + 1);
        }

        /// <summary>
        /// Extracts the code fragments based on the current file content that need to be re-processed due to content changes on the given lines.
        /// Ignores any whitespace or comments at the beginning of the file (whether they have changed or not).
        /// Ignores any whitespace or comments that occur after the last piece of code in the file.
        /// Throws an ArgumentNullException if any of the arguments is null.
        /// </summary>
        private static IEnumerable<CodeFragment> FragmentsToProcess(this FileContentManager file, SortedSet<int> changedLines)
        {
            // NOTE: I suggest not to touch this routine unless absolutely necessary...(things *will* break)
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (changedLines == null)
            {
                throw new ArgumentNullException(nameof(changedLines));
            }

            var iter = changedLines.GetEnumerator();
            var lastInFile = LastInFile(file);

            Position processed = new Position(0, 0);
            while (iter.MoveNext())
            {
                QsCompilerError.Verify(iter.Current >= 0 && iter.Current < file.NrLines(), "index out of range for changed line");
                if (processed.Line < iter.Current)
                {
                    var statementStart = file.PositionAfterPrevious(new Position(iter.Current, 0));
                    if (processed.IsSmallerThan(statementStart))
                    {
                        processed = statementStart;
                    }
                }

                while (processed.Line <= iter.Current && processed.IsSmallerThan(lastInFile))
                {
                    processed = processed.Copy(); // because we don't want to modify the ending of the previous code fragment ...
                    var nextEnding = file.FragmentEnd(ref processed);
                    var extractedPiece = file.GetCodeSnippet(new LSP.Range { Start = processed, End = nextEnding });

                    // constructing the CodeFragment -
                    // NOTE: its Range.End is the position of the delimiting char (if such a char exists), i.e. the position right after Code ends

                    // length = 0 can occur e.g. if the last piece of code in the file does not terminate with a statement ending
                    if (extractedPiece.Length > 0)
                    {
                        var code = file.GetLine(nextEnding.Line).ExcessBracketPositions.Contains(nextEnding.Character - 1)
                            ? extractedPiece.Substring(0, extractedPiece.Length - 1)
                            : extractedPiece;
                        if (code.Length == 0 || !CodeFragment.DelimitingChars.Contains(code.Last()))
                        {
                            code = $"{code}{CodeFragment.MissingDelimiter}";
                        }

                        var endChar = nextEnding.Character - (extractedPiece.Length - code.Length) - 1;
                        var codeRange = new LSP.Range { Start = processed, End = new Position(nextEnding.Line, endChar) };
                        yield return new CodeFragment(file.IndentationAt(codeRange.Start), codeRange, code.Substring(0, code.Length - 1), code.Last());
                    }
                    processed = nextEnding;
                }
            }
        }

        // routines called by the file content manager upon updating a file

        /// <summary>
        /// Given the start line of a change, and how many lines have been updated from there,
        /// computes the position where the syntax check will start and end.
        /// Throws an ArgumentNullException if file is null.
        /// Throws an ArgumentOutOfRangeException if the range [start, start + count) is not a valid range within the current file content.
        /// </summary>
        internal static LSP.Range GetSyntaxCheckDelimiters(this FileContentManager file, int start, int count)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (start < 0 || start >= file.NrLines())
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }
            if (count < 0 || start + count > file.NrLines())
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            // if no piece of code exists before the start of the modifications, then the check effectively starts at the beginning of the file
            var syntaxCheckStart = file.PositionAfterPrevious(new Position(start, 0)); // position (0,0) if there is no previous fragment
            // if the modification goes past what is currently the last piece of code, then the effectively the check extends to the end of the file
            var firstAfterModified = new Position(start + count, 0);
            var lastInFile = LastInFile(file);
            var syntaxCheckEnd = firstAfterModified.IsSmallerThan(lastInFile)
                ? file.FragmentEnd(ref firstAfterModified)
                : file.End();
            return new LSP.Range { Start = syntaxCheckStart, End = lastInFile.IsSmallerThanOrEqualTo(syntaxCheckEnd) ? file.End() : syntaxCheckEnd };
        }

        /// <summary>
        /// Dequeues all lines whose content has changed and extracts the code fragments overlapping with those lines that need to be reprocessed.
        /// Does nothing if no lines have been modified.
        /// Recomputes and pushes the syntax diagnostics for the extracted fragments and all end-of-file diagnostics otherwise.
        /// Processes the extracted fragment and inserts the processed fragments into the corresponding data structure
        /// Throws an ArgumentNullException if file is null.
        /// </summary>
        internal static void UpdateLanguageProcessing(this FileContentManager file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            file.SyncRoot.EnterUpgradeableReadLock();
            try
            {
                var changedLines = file.DequeueContentChanges();
                if (!changedLines.Any())
                {
                    return;
                }

                var reprocess = QsCompilerError.RaiseOnFailure(() => file.FragmentsToProcess(changedLines).ToList(), "processing the edited lines failed");
                var diagnostics = reprocess.CheckForEmptyFragments(file.FileName.Value)
                    .Concat(ParseCode(ref reprocess, file.FileName.Value)).ToList();

                QsCompilerError.RaiseOnFailure(() => file.TokensUpdate(reprocess), "the computed token update failed");
                QsCompilerError.RaiseOnFailure(() => file.AddSyntaxDiagnostics(diagnostics), "updating the SyntaxDiagnostics failed");
            }
            finally
            {
                file.SyncRoot.ExitUpgradeableReadLock();
            }
        }
    }
}
