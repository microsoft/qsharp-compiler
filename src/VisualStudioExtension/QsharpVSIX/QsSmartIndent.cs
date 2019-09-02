using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;

namespace Microsoft.Quantum.QsLanguageExtensionVS
{
    [ContentType("Q#")]
    [Export(typeof(ISmartIndentProvider))]
    internal class QsSmartIndentProvider : ISmartIndentProvider
    {
        public ISmartIndent CreateSmartIndent(ITextView textView) => new QsSmartIndent(textView);
    }

    internal class QsSmartIndent : ISmartIndent
    {
        /// <summary>
        /// A list of all opening and closing bracket pairs that affect indentation.
        /// </summary>
        private static readonly IImmutableList<(string open, string close)> brackets = ImmutableList.Create(new[]
        {
            ("[", "]"),
            ("(", ")"),
            ("{", "}")
        });

        /// <summary>
        /// The text view that this smart indent is handling indentation for.
        /// </summary>
        private readonly ITextView textView;

        /// <summary>
        /// Creates a new smart indent.
        /// </summary>
        /// <param name="textView">The text view that this smart indent is handling indentation for.</param>
        public QsSmartIndent(ITextView textView)
        {
            this.textView = textView;

            // The ISmartIndent interface is only for indenting blank lines or indenting after pressing enter. To
            // decrease the indent after typing a closing bracket, we have to watch for changes manually.
            textView.TextBuffer.ChangedHighPriority += TextBuffer_ChangedHighPriority;
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Returns the number of spaces to place at the start of the line, or null if there is no desired indentation.
        /// </summary>
        public int? GetDesiredIndentation(ITextSnapshotLine line)
        {
            // Note: The ISmartIndent interface requires that the return type is nullable, but we always return a
            // value.

            if (line.LineNumber == 0)
                return 0;

            ITextSnapshotLine lastNonEmptyLine = GetLastNonEmptyLine(line);
            int desiredIndent = GetIndentation(lastNonEmptyLine.GetText());
            int indentSize = textView.Options.GetIndentSize();
            if (StartsBlock(lastNonEmptyLine.GetText()))
                desiredIndent += indentSize;
            if (EndsBlock(line.GetText()))
                desiredIndent -= indentSize;
            return Math.Max(0, desiredIndent);
        }

        private void TextBuffer_ChangedHighPriority(object sender, TextContentChangedEventArgs e)
        {
            foreach (ITextChange change in e.Changes)
            {
                if (EndsBlock(change.NewText))
                {
                    ITextSnapshotLine line = e.After.GetLineFromPosition(change.NewPosition);
                    int indent = GetIndentation(line.GetText());
                    int desiredIndent = GetDesiredIndentation(line) ?? 0;
                    if (indent != desiredIndent)
                        e.After.TextBuffer.Replace(
                            new Span(line.Start.Position, line.GetText().TakeWhile(IsIndentation).Count()),
                            CreateIndentation(desiredIndent));
                }
            }
        }

        /// <summary>
        /// Returns the current indentation of the line in number of spaces.
        /// </summary>
        private int GetIndentation(string line) =>
            line
            .TakeWhile(IsIndentation)
            .Aggregate(0, (indent, c) => indent + (c == '\t' ? textView.Options.GetTabSize() : 1));

        /// <summary>
        /// Returns a string containing spaces or tabs (depending on the text view options) to match the given
        /// indentation.
        /// </summary>
        private string CreateIndentation(int indent)
        {
            if (textView.Options.IsConvertTabsToSpacesEnabled())
                return new string(' ', indent);
            else
                return
                    new string('\t', indent / textView.Options.GetTabSize()) +
                    new string(' ', indent % textView.Options.GetTabSize());
        }

        /// <summary>
        /// Returns true if the end of the line starts a block.
        /// </summary>
        private static bool StartsBlock(string line) =>
            brackets.Any(bracket => line.TrimEnd().EndsWith(bracket.open));

        /// <summary>
        /// Returns true if the beginning of the line ends a block.
        /// </summary>
        private static bool EndsBlock(string line) =>
            brackets.Any(bracket => line.TrimStart().StartsWith(bracket.close));

        /// <summary>
        /// Returns true if the character is an indentation character (a space or a tab).
        /// </summary>
        private static bool IsIndentation(char c) => c == ' ' || c == '\t';

        /// <summary>
        /// Returns the last non-empty line before the given line. A non-empty line is any line that contains at least
        /// one non-whitespace character. If all of the lines before the given line are empty, returns the first line of
        /// the snapshot instead. 
        /// <para/>
        /// Returns null if the given line is the first line in the snapshot.
        /// </summary>
        private static ITextSnapshotLine GetLastNonEmptyLine(ITextSnapshotLine line)
        {
            int lineNumber = line.LineNumber - 1;
            if (lineNumber < 0)
                return null;
            ITextSnapshot snapshot = line.Snapshot;
            while (lineNumber > 0 && string.IsNullOrWhiteSpace(snapshot.GetLineFromLineNumber(lineNumber).GetText()))
                lineNumber--;
            return snapshot.GetLineFromLineNumber(lineNumber);
        }
    }
}
