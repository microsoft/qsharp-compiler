// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Quantum.QsCompiler.TextProcessing;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Builder = Microsoft.Quantum.QsCompiler.CompilationBuilder.Utils;

namespace Microsoft.Quantum.QsLanguageServer.Testing
{
    internal class RandomInput
    {
        private Random rnd;
        internal const string TestInputDirectory = "RandomInputFiles";

        internal RandomInput(int seed)
        {
            this.rnd = new Random(seed);
        }

        internal RandomInput()
        {
            this.rnd = new Random();
        }

        internal int GetRandom() => this.rnd.Next();

        private void InsertRandom(ref string current, Func<string> insert, int maxOccurrences = 2)
        {
            for (var j = this.rnd.Next(0, maxOccurrences + 1); j > 0; --j)
            {
                current = current.Insert(this.rnd.Next(0, current.Length + 1), insert());
            }
        }

        private string GetRandomLine()
        {
            var current = Path.GetRandomFileName().Replace(".", "");
            this.InsertRandom(ref current, () => ";");
            this.InsertRandom(ref current, () => "{");
            this.InsertRandom(ref current, () => "}");
            this.InsertRandom(ref current, () => ".");
            this.InsertRandom(ref current, () => " ", 6);
            return current;
        }

        private static string[] allKeywords = Keywords.ReservedKeywords.ToArray();

        internal string[] GetRandomLines(int nrLines, bool withLanguageKeywords = true)
        {
            var lines = new string[nrLines];
            string GetKeyword() => $" {allKeywords[this.rnd.Next(0, allKeywords.Length)]} ";
            for (var nr = 0; nr < nrLines; ++nr)
            {
                lines[nr] = this.GetRandomLine();
                if (withLanguageKeywords)
                {
                    this.InsertRandom(ref lines[nr], GetKeyword, 6);
                }
            }
            return lines;
        }

        internal string GenerateRandomFile(int nrLines, bool? emptyLastLine, bool withLanguageKeywords = true, bool withQsExtension = false)
        {
            var filename = Path.Combine(TestInputDirectory, Path.GetRandomFileName()) + (withQsExtension ? ".qs" : "z"); // guarantee .qs extension exists or not
            var content = this.GetRandomLines(nrLines, withLanguageKeywords);
            using (StreamWriter sw = new StreamWriter(filename))
            {
                foreach (var line in content)
                {
                    if (this.rnd.Next(0, 3) == 0)
                    {
                        sw.WriteLine(); // inserting a couple of empty lines as well
                    }
                    sw.WriteLine(line);
                }
                sw.Write(emptyLastLine ?? this.rnd.Next(0, 2) == 0 ? string.Empty : this.GetRandomLine());
            }
            return filename;
        }

        private VisualStudio.LanguageServer.Protocol.Range GetRandomRange(IReadOnlyList<string> content)
        {
            var (startLine, endLine) = (this.rnd.Next(0, content.Count), this.rnd.Next(0, content.Count));
            var (startChar, endChar) = (this.rnd.Next(0, content[startLine].Length + 1), this.rnd.Next(0, content[endLine].Length + 1));

            ((startLine, startChar), (endLine, endChar)) =
                startLine <= endLine ?
                ((startLine, startChar), (endLine, endChar)) :
                ((endLine, endChar), (startLine, startChar));
            if (startLine == endLine && startChar > endChar)
            {
                (startChar, endChar) = (endChar, startChar);
            }

            var range = new VisualStudio.LanguageServer.Protocol.Range
            {
                Start = new Position(startLine, startChar),
                End = new Position(endLine, endChar)
            };
            Assert.IsTrue(TestUtils.IsValidRange(range));
            return range;
        }

        private TextDocumentContentChangeEvent GetRandomEdit(IReadOnlyList<string> content, int expectedNrLines, bool withLanguageKeywords)
        {
            var changeRange = this.GetRandomRange(content);
            var nrLinesRemoved = changeRange.End.Line - changeRange.Start.Line + 1;
            var nrLinesInserted = this.rnd.Next(1, expectedNrLines);
            var changeLength = TestUtils.GetRangeLength(changeRange, content);
            var changeText = string.Join(Environment.NewLine, this.GetRandomLines(nrLinesInserted, withLanguageKeywords));

            return new TextDocumentContentChangeEvent
            {
                Range = changeRange,
                RangeLength = changeLength,
                Text = changeText
            };
        }

        private TextDocumentContentChangeEvent DeleteAll(IReadOnlyList<string> content)
        {
            var changeRange = new VisualStudio.LanguageServer.Protocol.Range
            {
                Start = new Position(0, 0),
                End = content.Any() ? new Position(content.Count - 1, content.Last().Length) : new Position(0, 0)
            };
            var changeLength = TestUtils.GetRangeLength(changeRange, content);
            return new TextDocumentContentChangeEvent
            {
                Range = changeRange,
                RangeLength = changeLength,
                Text = string.Empty
            };
        }

        // the last edit will always be a delete all
        internal TextDocumentContentChangeEvent[] MakeRandomEdits(int nrEdits, ref List<string> content, int expectedNrLines, bool withLanguageKeywords)
        {
            var edits = new TextDocumentContentChangeEvent[nrEdits];
            if (nrEdits == 0)
            {
                return edits;
            }
            for (var i = 0; i < edits.Length - 1; ++i)
            {
                edits[i] = this.GetRandomEdit(content, expectedNrLines, withLanguageKeywords);
                TestUtils.ApplyEdit(edits[i], ref content);
            }
            edits[edits.Length - 1] = this.DeleteAll(content);
            TestUtils.ApplyEdit(edits.Last(), ref content);
            return edits;
        }
    }
}
