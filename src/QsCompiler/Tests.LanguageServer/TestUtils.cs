// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Builder = Microsoft.Quantum.QsCompiler.CompilationBuilder.Utils;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.Quantum.QsLanguageServer.Testing
{
    internal static class TestUtils
    {
        internal static Uri GetUri(string filename)
        {
            return new Uri(Path.GetFullPath(filename));
        }

        internal static List<string> GetContent(string filename)
        {
            var content = File.ReadAllLines(Path.GetFullPath(filename)).ToList();
            if (content.Any() && File.ReadAllText(filename).EndsWith(Environment.NewLine))
            {
                content.Add(string.Empty); // ReadAllLines will ignore the last line if it is empty
            }
            for (var lineNr = 0; lineNr < content.Count - 1; ++lineNr)
            {
                content[lineNr] += Environment.NewLine;
            }
            return content;
        }

        internal static TextDocumentIdentifier GetTextDocumentIdentifier(string filename)
        {
            return new TextDocumentIdentifier { Uri = GetUri(filename) };
        }

        internal static InitializeParams GetInitializeParams()
        {
            return new InitializeParams
            {
                ProcessId = -1,
                RootPath = null,
                InitializationOptions = null,
                Capabilities = new ClientCapabilities
                {
                    Workspace = new WorkspaceClientCapabilities(),
                    TextDocument = new TextDocumentClientCapabilities(),
                    Experimental = new object()
                }
            };
        }

        internal static DidOpenTextDocumentParams GetOpenFileParams(string filename)
        {
            var file = Path.GetFullPath(filename);
            var content = File.ReadAllText(file);
            return new DidOpenTextDocumentParams
            { TextDocument = new TextDocumentItem { Uri = GetUri(filename), Text = content } };
        }

        internal static DidCloseTextDocumentParams GetCloseFileParams(string filename)
        {
            return new DidCloseTextDocumentParams { TextDocument = GetTextDocumentIdentifier(filename) };
        }

        internal static DidSaveTextDocumentParams GetSaveFileParams(string filename, string content)
        {
            return new DidSaveTextDocumentParams { TextDocument = new TextDocumentIdentifier { Uri = GetUri(filename) }, Text = content };
        }

        internal static DidChangeTextDocumentParams GetChangedFileParams(string filename, TextDocumentContentChangeEvent[] changes)
        {
            var fileId = new VersionedTextDocumentIdentifier { Uri = GetUri(filename) };
            return new DidChangeTextDocumentParams
            { TextDocument = fileId, ContentChanges = changes };
        }

        internal static TextDocumentPositionParams GetTextDocumentPositionParams(string filename, Position pos)
        {
            return new TextDocumentPositionParams
            { TextDocument = GetTextDocumentIdentifier(filename), Position = pos };
        }

        internal static ExecuteCommandParams ServerCommand(string command, params object[] args) =>
            new ExecuteCommandParams { Command = command, Arguments = args };

        // does not modify range
        internal static int GetRangeLength(VisualStudio.LanguageServer.Protocol.Range range, IReadOnlyList<string> content)
        {
            Assert.IsTrue(IsValidRange(range));
            if (range.Start.Line == range.End.Line)
            {
                return range.End.Character - range.Start.Character;
            }

            var changeLength = content[range.Start.Line].Length - range.Start.Character;
            for (var line = range.Start.Line + 1; line < range.End.Line; ++line)
            {
                changeLength += content[line].Length;
            }
            return changeLength + range.End.Character;
        }

        internal static void ApplyEdit(TextDocumentContentChangeEvent change, ref List<string> content)
        {
            if (!content.Any())
            {
                throw new ArgumentException("the given content has to have at least on line");
            }

            Assert.IsTrue(IsValidRange(change.Range) && change.Text != null);
            Assert.IsTrue(change.Range.End.Line < content.Count());
            Assert.IsTrue(change.Range.Start.Character <= content[change.Range.Start.Line].Length);
            Assert.IsTrue(change.Range.End.Character <= content[change.Range.End.Line].Length);

            var (startLine, startChar) = (change.Range.Start.Line, change.Range.Start.Character);
            var (endLine, endChar) = (change.Range.End.Line, change.Range.End.Character);

            var newText = string.Concat(content[startLine].Substring(0, startChar), change.Text, content[endLine].Substring(endChar));
            if (startLine > 0)
            {
                newText = content[--startLine] + newText;
            }
            if (endLine + 1 < content.Count)
            {
                newText = newText + content[++endLine];
            }
            var lineChanges = Builder.SplitLines(newText);
            if (lineChanges.Length == 0 || (endLine + 1 == content.Count() && Builder.EndOfLine.Match(lineChanges.Last()).Success))
            {
                lineChanges = lineChanges.Concat(new string[] { string.Empty }).ToArray();
            }

            content.RemoveRange(startLine, endLine - startLine + 1);
            content.InsertRange(startLine, lineChanges);
        }

        internal static bool IsValidRange(Range? range)
        {
            static bool IsValidPosition(Position position) => position.Line >= 0 && position.Character >= 0;

            static bool IsBeforeOrEqual(Position a, Position b) =>
                a.Line < b.Line || (a.Line == b.Line && a.Character <= b.Character);

            return !(range?.Start is null) &&
                   !(range.End is null) &&
                   IsValidPosition(range.Start) &&
                   IsValidPosition(range.End) &&
                   IsBeforeOrEqual(range.Start, range.End);
        }
    }
}
