// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.TextProcessing;
using Lsp = Microsoft.VisualStudio.LanguageServer.Protocol;
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures
{
    /// <summary>
    /// Contains all information managed by the ScopeTracking.
    /// All properties are readonly, and any modification leads to the creation of a new instance.
    /// </summary>
    internal class CodeLine
    {
        internal enum StringContext
        {
            NoOpenString,
            OpenInterpolatedArgument,
            OpenString,
            OpenInterpolatedString,
            OpenStringInOpenInterpolatedArgument,
        }

        internal readonly string Text;
        internal readonly string LineEnding; // contains the line break for this line (included in text)

        /// <summary>
        /// contains the text content of the line, without any end of line or doc comment and *without* the line break
        /// </summary>
        public readonly string WithoutEnding;

        /// <summary>
        /// Contains the end of line comment without any leading or trailing whitespace, and without the comment slashes.
        /// Is null if no such comment exists.
        /// Documenting comments (i.e. triple-slash comments) are *not* considered to be end of line comments.
        /// All comments which *either* follow non-whitespace content or do not start with triple-slash are considered end of line comments.
        /// </summary>
        public readonly string? EndOfLineComment;

        internal readonly int Indentation; // Note: This denotes the initial indentation at the beginning of the line
        internal readonly ImmutableArray<int> ExcessBracketPositions;

        // convention: first one opens a string, and we have the usual inclusions: {0,5} means the chars 0..4 are the content of a string
        // -> i.e. -1 means the string starts on a previous line, and an end delimiter that is equal to Text.Length means that the string continues on the next line
        internal readonly ImmutableArray<int> StringDelimiters; // note that this property is only properly initialized *after* knowing the surrounding lines

        internal readonly StringContext BeginningStringContext;
        internal readonly StringContext EndingStringContext;

        public CodeLine(
            string text,
            string lineEnding,
            string withoutEnding,
            string? endOfLineComment,
            int indentation,
            IEnumerable<int> excessBracketPositions,
            IEnumerable<int> stringDelimiters,
            StringContext beginningStringContext,
            StringContext endingStringContext)
        {
            this.Text = text;
            this.LineEnding = lineEnding;
            this.WithoutEnding = withoutEnding;
            this.EndOfLineComment = endOfLineComment;
            this.Indentation = indentation;
            this.ExcessBracketPositions = excessBracketPositions.ToImmutableArray();
            this.StringDelimiters = stringDelimiters.ToImmutableArray();
            this.BeginningStringContext = beginningStringContext;
            this.EndingStringContext = endingStringContext;

            ScopeTracking.VerifyStringDelimiters(text, stringDelimiters);
            ScopeTracking.VerifyExcessBracketPositions(this, excessBracketPositions);
        }

        private CodeLine(StringContext beginningStringContext)
        {
            this.Text = string.Empty;
            this.LineEnding = string.Empty;
            this.WithoutEnding = string.Empty;
            this.EndOfLineComment = null;
            this.Indentation = 0;
            this.ExcessBracketPositions = ImmutableArray<int>.Empty;
            this.StringDelimiters = ImmutableArray<int>.Empty;
            this.BeginningStringContext = beginningStringContext;
            this.EndingStringContext = beginningStringContext;
        }

        public CodeLine(string text, StringContext beginningStringContext = StringContext.NoOpenString)
            : this(text, beginningStringContext, 0, new List<int>())
        {
        }

        public CodeLine(string text, StringContext beginningStringContext, int indentation, IEnumerable<int> excessBrackets)
        {
            this.Text = text;
            this.LineEnding = Utils.EndOfLine.Match(text).Value; // empty string if the matching failed

            this.BeginningStringContext = beginningStringContext;
            int commentStart;
            var delimiters = ComputeStringDelimiters(text, ref beginningStringContext, out commentStart);
            this.EndingStringContext = beginningStringContext; // beginningStringContext has been updated to the context at the end of the line

            var lineLength = text.Length - this.LineEnding.Length;
            // if there is a comment
            if (commentStart >= 0)
            {
                this.WithoutEnding = text.Substring(0, commentStart);
                var commentStr = text.Substring(commentStart, lineLength - commentStart).Trim();
                var isDocComment = string.IsNullOrWhiteSpace(this.WithoutEnding) && (commentStr.Length - commentStr.TrimStart('/').Length == 3);
                this.EndOfLineComment = isDocComment ? null : commentStr.Substring(2);
            }
            // else there is no comment
            else
            {
                this.WithoutEnding = text.Substring(0, lineLength);
                this.EndOfLineComment = null;
            }

            this.StringDelimiters = delimiters.ToImmutableArray();
            this.Indentation = indentation;
            this.ExcessBracketPositions = excessBrackets.ToImmutableArray();
            ScopeTracking.VerifyStringDelimiters(text, delimiters);
            ScopeTracking.VerifyExcessBracketPositions(this, excessBrackets);
        }

        public CodeLine(string text, StringContext beginningStringContext, IEnumerable<int> delimiters, int commentStart, int indentation, IEnumerable<int> excessBrackets)
        {
            this.Text = text;
            this.LineEnding = Utils.EndOfLine.Match(text).Value; // empty string if the matching failed

            this.BeginningStringContext = beginningStringContext;
            this.EndingStringContext = beginningStringContext;

            foreach (var delim in delimiters)
            {
                if (delim == -1 || delim == text.Length)
                {
                    continue;
                }

                var inputDelimiter = text[delim].ToString();
                // If the input is a " preceded by a $ than the input needs to be updated to $"
                if (delim > 0 && inputDelimiter == "\"" && text[delim - 1] == '$')
                {
                    inputDelimiter = "$\"";
                }

                this.EndingStringContext = MoveToNextState(this.EndingStringContext, inputDelimiter);
            }
            this.EndingStringContext = beginningStringContext; // beginningStringContext has been updated to the context at the end of the line

            var lineLength = text.Length - this.LineEnding.Length;
            // if there is a comment
            if (commentStart >= 0)
            {
                this.WithoutEnding = text.Substring(0, commentStart);
                var commentStr = text.Substring(commentStart, lineLength - commentStart).Trim();
                var isDocComment = string.IsNullOrWhiteSpace(this.WithoutEnding) && (commentStr.Length - commentStr.TrimStart('/').Length == 3);
                this.EndOfLineComment = isDocComment ? null : commentStr.Substring(2);
            }
            // else there is no comment
            else
            {
                this.WithoutEnding = text.Substring(0, lineLength);
                this.EndOfLineComment = null;
            }

            this.StringDelimiters = delimiters.ToImmutableArray();
            this.Indentation = indentation;
            this.ExcessBracketPositions = excessBrackets.ToImmutableArray();
            ScopeTracking.VerifyStringDelimiters(text, delimiters);
            ScopeTracking.VerifyExcessBracketPositions(this, excessBrackets);
        }

        /// <summary>
        /// Computes the location of the string delimiters within a given text.
        /// </summary>
        private static IEnumerable<int> ComputeStringDelimiters(string text, ref StringContext stringContext, out int commentIndex)
        {
            var builder = ImmutableArray.CreateBuilder<int>();

            if (stringContext >= StringContext.OpenString)
            {
                builder.Add(-1);
            }

            commentIndex = -1;
            var stringLength = text.Length;
            while (text != string.Empty)
            {
                // Check for a comment start if we are outside a string
                commentIndex = stringContext >= StringContext.OpenString ? -1 : text.IndexOf("//");

                var index = -1;
                switch (stringContext)
                {
                    case StringContext.NoOpenString:
                        // Find the next "
                        index = text.IndexOf('"');
                        break;
                    case StringContext.OpenInterpolatedArgument:
                        // Find the next " or }
                        index = text.IndexOfAny(new[] { '"', '}' });
                        break;
                    case StringContext.OpenInterpolatedString:
                        // Find the next " or {, neither or which being preceded by \
                        index = text.IndexOfAny(new[] { '"', '{' });
                        while (index > 0 && text[index - 1] == '\\')
                        {
                            var next = text.Substring(index + 1).IndexOfAny(new[] { '"', '{' });
                            index = next < 0 ? next : index + 1 + next;
                        }
                        break;
                    case StringContext.OpenString:
                    case StringContext.OpenStringInOpenInterpolatedArgument:
                        // Find the next " not preceded by \
                        index = text.IndexOf('"');
                        while (index > 0 && text[index - 1] == '\\')
                        {
                            var next = text.Substring(index + 1).IndexOf('"');
                            index = next < 0 ? next : index + 1 + next;
                        }
                        break;
                }

                if (index < 0 || (commentIndex > -1 && commentIndex < index))
                {
                    break;
                }

                var inputDelimiter = text[index].ToString();
                // If the input is a " preceded by a $ than the input needs to be updated to $"
                if (index > 0 && inputDelimiter == "\"" && text[index - 1] == '$')
                {
                    inputDelimiter = "$\"";
                }

                builder.Add(index + stringLength - text.Length);
                text = text.Substring(index + 1);
                stringContext = MoveToNextState(stringContext, inputDelimiter);
            }

            // commentIndex is only nonzero if we found \\ and we were not in a string,
            // so it should be the genuine start of a comment
            if (commentIndex > -1)
            {
                commentIndex += stringLength - text.Length;
            }

            if (stringContext >= StringContext.OpenString)
            {
                builder.Add(stringLength);
            }

            return builder.ToImmutable();
        }

        private static StringContext MoveToNextState(StringContext curr, string input)
        {
            switch (input)
            {
                case "\"":
                    switch (curr)
                    {
                        case StringContext.NoOpenString:
                            return StringContext.OpenString;
                        case StringContext.OpenInterpolatedArgument:
                            return StringContext.OpenStringInOpenInterpolatedArgument;
                        case StringContext.OpenString:
                        case StringContext.OpenInterpolatedString:
                            return StringContext.NoOpenString;
                        case StringContext.OpenStringInOpenInterpolatedArgument:
                            return StringContext.OpenInterpolatedArgument;
                        default:
                            return curr;
                    }
                case "{":
                    switch (curr)
                    {
                        case StringContext.OpenInterpolatedArgument:
                            throw new ArgumentException("Cannot have '{' or '}' nested inside of interpolated argument.");
                        case StringContext.OpenInterpolatedString:
                            return StringContext.OpenInterpolatedArgument;
                        default:
                            return curr;
                    }
                case "}":
                    switch (curr)
                    {
                        case StringContext.OpenInterpolatedArgument:
                            return StringContext.OpenInterpolatedString;
                        default:
                            return curr;
                    }
                case "$\"":
                    switch (curr)
                    {
                        case StringContext.NoOpenString:
                            return StringContext.OpenInterpolatedString;
                        case StringContext.OpenInterpolatedArgument:
                            throw new ArgumentException("Cannot have interpolated string nested inside of interpolated argument.");
                        case StringContext.OpenString:
                        case StringContext.OpenInterpolatedString:
                            return StringContext.NoOpenString;
                        case StringContext.OpenStringInOpenInterpolatedArgument:
                            return StringContext.OpenInterpolatedArgument;
                        default:
                            return curr;
                    }
                default:
                    return curr;
            }
        }

        public static CodeLine Empty(StringContext beginningStringContext = StringContext.NoOpenString) =>
            new CodeLine(beginningStringContext);

        internal CodeLine SetIndentation(int newIndentation)
        {
            return new CodeLine(
                this.Text,
                this.LineEnding,
                this.WithoutEnding,
                this.EndOfLineComment,
                newIndentation,
                this.ExcessBracketPositions,
                this.StringDelimiters,
                this.BeginningStringContext,
                this.EndingStringContext);
        }

        internal CodeLine SetExcessBrackets(IEnumerable<int> newExcessBrackets)
        {
            return new CodeLine(
                this.Text,
                this.LineEnding,
                this.WithoutEnding,
                this.EndOfLineComment,
                this.Indentation,
                newExcessBrackets,
                this.StringDelimiters,
                this.BeginningStringContext,
                this.EndingStringContext);
        }
    }

    /// <summary>
    /// Contains all information managed by the LanguageProcessor.
    /// All properties except IncludeInCompilation are readonly, and any modification leads to the creation of a new instance.
    /// Access to IncludeInCompilation is limited to be via the TokenIndex subclass.
    /// Note that GetRange will return a new instance of the CodeFragment Range upon each call,
    /// and modifications to the returned instance won't be reflected in the CodeFragment.
    /// </summary>
    public class CodeFragment
    {
        /// <summary>
        /// The code fragment's range.
        /// </summary>
        internal Range Range { get; }

        internal readonly Range HeaderRange;
        internal readonly int Indentation;
        internal readonly string Text;
        internal readonly char FollowedBy;
        internal readonly QsComments Comments;
        public readonly QsFragmentKind? Kind;

        internal bool IncludeInCompilation { get; private set; } // used to exclude certain code fragments from the compilation if they are e.g. misplaced

        internal const char MissingDelimiter = '@'; // arbitrarily chosen
        internal static readonly ImmutableArray<char> DelimitingChars = ImmutableArray.Create('}', '{', ';');

        private static Range GetHeaderRange(string text, QsFragmentKind? kind) =>
            kind == null ? Range.Zero : kind.IsControlledAdjointDeclaration
                ? Parsing.HeaderDelimiters(2).Invoke(text ?? "")
                : Parsing.HeaderDelimiters(1).Invoke(text ?? "");

        /// <summary>
        /// Note that the only thing that may be set to null is the fragment kind - all other properties need to be set upon initialization
        /// </summary>
        private CodeFragment(int indent, Range range, string text, char next, QsComments? comments, QsFragmentKind? kind, bool include)
        {
            if (!DelimitingChars.Contains(next) && next != MissingDelimiter)
            {
                throw new ArgumentException("a CodeFragment needs to be followed by a DelimitingChar");
            }
            this.Indentation = indent < 0 ? throw new ArgumentException("indentation needs to be positive") : indent;
            this.Text = text.TrimEnd();
            this.FollowedBy = next;
            this.Comments = comments ?? QsComments.Empty;
            this.Kind = kind; // nothing here should be modifiable
            this.Range = range;
            this.HeaderRange = GetHeaderRange(this.Text, this.Kind);
            this.IncludeInCompilation = include;
        }

        internal CodeFragment(int indent, Range range, string text, char next, QsFragmentKind? kind = null)
            : this(indent, range, text, next, null, kind, true)
        {
        }

        internal CodeFragment Copy() =>
            new CodeFragment(this.Indentation, this.Range, this.Text, this.FollowedBy, this.Comments, this.Kind, this.IncludeInCompilation);

        public bool Equals(CodeFragment other)
        {
            if (other == null)
            {
                return false;
            }
            return
                this.Range == other.Range &&
                this.Indentation == other.Indentation &&
                this.Text == other.Text &&
                this.FollowedBy == other.FollowedBy &&
                this.IncludeInCompilation == other.IncludeInCompilation &&
                (this.Kind == null ? other.Kind == null : this.Kind.Equals(other.Kind));
        }

        internal CodeFragment WithLineNumOffset(int offset) => this.SetRange(this.Range.WithLineNumOffset(offset));

        internal CodeFragment SetRange(Range range) =>
            new CodeFragment(this.Indentation, range, this.Text, this.FollowedBy, this.Comments, this.Kind, this.IncludeInCompilation);

        internal CodeFragment SetIndentation(int indent) =>
            new CodeFragment(indent, this.Range, this.Text, this.FollowedBy, this.Comments, this.Kind, this.IncludeInCompilation);

        internal CodeFragment SetCode(string code) =>
            new CodeFragment(this.Indentation, this.Range, code, this.FollowedBy, this.Comments, this.Kind, this.IncludeInCompilation);

        internal CodeFragment SetFollowedBy(char delim) =>
            new CodeFragment(this.Indentation, this.Range, this.Text, delim, this.Comments, this.Kind, this.IncludeInCompilation);

        internal CodeFragment SetKind(QsFragmentKind kind) =>
            new CodeFragment(this.Indentation, this.Range, this.Text, this.FollowedBy, this.Comments, kind, this.IncludeInCompilation);

        internal CodeFragment ClearComments() =>
            new CodeFragment(this.Indentation, this.Range, this.Text, this.FollowedBy, null, this.Kind, this.IncludeInCompilation);

        internal CodeFragment SetOpeningComments(IEnumerable<string?> commentsBefore)
        {
            var relevantComments = commentsBefore.SkipWhile(c => c == null).Reverse();
            relevantComments = relevantComments.SkipWhile(c => c == null).Reverse();
            var comments = new QsComments(relevantComments.Select(c => c ?? string.Empty).ToImmutableArray(), this.Comments.ClosingComments);
            return new CodeFragment(this.Indentation, this.Range, this.Text, this.FollowedBy, comments, this.Kind, this.IncludeInCompilation);
        }

        internal CodeFragment SetClosingComments(IEnumerable<string?> commentsAfter)
        {
            var relevantComments = commentsAfter.SkipWhile(c => c == null).Reverse();
            relevantComments = relevantComments.SkipWhile(c => c == null).Reverse();
            var comments = new QsComments(this.Comments.OpeningComments, relevantComments.Select(c => c ?? string.Empty).ToImmutableArray());
            return new CodeFragment(this.Indentation, this.Range, this.Text, this.FollowedBy, comments, this.Kind, this.IncludeInCompilation);
        }

        /// <summary>
        /// A class to conveniently walk the saved tokens.
        /// This class is a subclass of CodeFragment to limit access to IncludeInCompilation to be via TokenIndex.
        /// </summary>
        internal class TokenIndex // not disposable because File mustn't be disposed since several token indices may be using it
        {
            private readonly FileContentManager file;

            public int Line { get; private set; }

            public int Index { get; private set; }

            /// <summary>
            /// Verifies the given line number and index *only* against the Tokens listed in file (and not against the
            /// content) and initializes an instance of TokenIndex.
            /// </summary>
            /// <exception cref="ArgumentOutOfRangeException">The line or index are negative.</exception>
            /// <exception cref="FileContentException">
            /// The line is outside the bounds of the file, or the index is outside the bounds of the line.
            /// </exception>
            internal TokenIndex(FileContentManager file, int line, int index)
            {
                if (line < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(line));
                }
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                if (line >= file.NrTokenizedLines())
                {
                    throw new FileContentException("Line exceeds the bounds of the file.");
                }
                if (index >= file.GetTokenizedLine(line).Length)
                {
                    throw new FileContentException("Token exceeds the bounds of the line.");
                }

                this.file = file;
                this.Line = line;
                this.Index = index;
            }

            internal TokenIndex(TokenIndex tIndex)
                : this(tIndex.file, tIndex.Line, tIndex.Index)
            {
            }

            private bool IsWithinFile() => // used within class methods, since the file itself may change during the lifetime of the token ...
                this.Line < this.file.NrTokenizedLines() && this.Index < this.file.GetTokenizedLine(this.Line).Length;

            /// <summary>
            /// Marks the token returned by GetToken for the associated file as excluded from the compilation.
            /// </summary>
            /// <exception cref="FileContentException">
            /// The line or index are no longer within the associated file.
            /// </exception>
            internal void MarkAsExcluded()
            {
                if (!this.IsWithinFile())
                {
                    throw new FileContentException("Token index is no longer valid within its associated file.");
                }
                this.file.GetTokenizedLine(this.Line)[this.Index].IncludeInCompilation = false;
            }

            /// <summary>
            /// Marks the token returned by GetToken for the associated file as included in the compilation.
            /// </summary>
            /// <exception cref="FileContentException">
            /// The line or index are no longer within the associated file.
            /// </exception>
            internal void MarkAsIncluded()
            {
                if (!this.IsWithinFile())
                {
                    throw new FileContentException("Token index is no longer valid within its associated file.");
                }
                this.file.GetTokenizedLine(this.Line)[this.Index].IncludeInCompilation = true;
            }

            /// <summary>
            /// Returns the corresponding fragment for the token at the saved TokenIndex -
            /// i.e. a copy of the token where its range denotes the absolute range within the file.
            /// </summary>
            /// <exception cref="FileContentException">
            /// The line or index are no longer within the associated file.
            /// </exception>
            internal CodeFragment GetFragment()
            {
                if (!this.IsWithinFile())
                {
                    throw new FileContentException("Token index is no longer valid within its associated file.");
                }
                return this.file.GetTokenizedLine(this.Line)[this.Index].WithLineNumOffset(this.Line);
            }

            /// <summary>
            /// Returns the corresponding fragment for the token at the saved TokenIndex including any closing comments for that fragment -
            /// i.e. a copy of the token where its range denotes the absolute range within the file.
            /// </summary>
            /// <exception cref="FileContentException">
            /// The line or index are no longer within the associated file.
            /// </exception>
            internal CodeFragment GetFragmentWithClosingComments()
            {
                var fragment = this.GetFragment();
                // get any comments attached to a potential empty closing fragment
                // -> note that the fragment containing a closing bracket has an indentation level that is one higher than the matching parent! (giving the same *after* closing)
                var allChildren = this.GetChildren(deep: false);
                if (allChildren.Any())
                {
                    var lastChild = allChildren.Last().GetFragment();
                    if (lastChild.FollowedBy == '}' && !lastChild.IncludeInCompilation)
                    {
                        fragment = fragment.SetClosingComments(lastChild.Comments.OpeningComments);
                    }
                }
                return fragment;
            }

            /// <summary>
            /// Returns the TokenIndex of the next token in File or null if no such token exists.
            /// </summary>
            /// <exception cref="FileContentException">
            /// The line or index are no longer within the associated file.
            /// </exception>
            public TokenIndex? Next()
            {
                if (!this.IsWithinFile())
                {
                    throw new FileContentException("Token index is no longer valid within its associated file.");
                }
                var res = new TokenIndex(this);
                if (++res.Index < res.file.GetTokenizedLine(res.Line).Length)
                {
                    return res;
                }
                res.Index = 0;
                while (++res.Line < res.file.NrTokenizedLines() && res.file.GetTokenizedLine(res.Line).Length == 0)
                {
                }
                return res.Line == res.file.NrTokenizedLines() ? null : res;
            }

            /// <summary>
            /// Returns the TokenIndex of the previous token in File or null if no such token exists.
            /// </summary>
            /// <exception cref="FileContentException">
            /// The line or index are no longer within the associated file.
            /// </exception>
            public TokenIndex? Previous()
            {
                if (!this.IsWithinFile())
                {
                    throw new FileContentException("Token index is no longer valid within its associated file.");
                }
                var res = new TokenIndex(this);
                if (res.Index-- > 0)
                {
                    return res;
                }
                while (res.Index < 0 && res.Line-- > 0)
                {
                    res.Index = res.file.GetTokenizedLine(res.Line).Length - 1;
                }
                return res.Line < 0 ? null : res;
            }
        }
    }

    /// <summary>
    /// struct used to do the (local) type checking and build the SyntaxTree
    /// </summary>
    internal struct FragmentTree
    {
        internal struct TreeNode
        {
            public readonly CodeFragment Fragment;
            public readonly IReadOnlyList<TreeNode> Children;

            /// <summary>
            /// The position of the root node that all child node positions are relative to.
            /// </summary>
            public Position RootPosition { get; }

            /// <summary>
            /// The position of this node relative to the root node.
            /// </summary>
            public Position RelativePosition { get; }

            /// <summary>
            /// Builds the TreeNode consisting of the given fragment and children.
            /// RelativeToRoot is set to the position of the fragment start relative to the given parent start position.
            /// </summary>
            /// <exception cref="ArgumentException"><paramref name="parentStart"/> is larger than the start position of <paramref name="fragment"/>.</exception>
            public TreeNode(CodeFragment fragment, IReadOnlyList<TreeNode> children, Position parentStart)
            {
                if (fragment.Range.Start < parentStart)
                {
                    throw new ArgumentException("parentStart needs to be smaller than or equal to the fragment start.", nameof(parentStart));
                }
                this.Fragment = fragment;
                this.Children = children;
                this.RootPosition = parentStart;
                this.RelativePosition = fragment.Range.Start - parentStart;
            }
        }

        public readonly string Source;
        public readonly string Namespace;
        public readonly string Callable;
        public readonly IReadOnlyList<TreeNode>? Specializations;

        public FragmentTree(string source, string ns, string callable, IEnumerable<TreeNode>? specs)
        {
            this.Source = source;
            this.Namespace = ns;
            this.Callable = callable;
            this.Specializations = specs?.ToList()?.AsReadOnly();
        }
    }

    /// <summary>
    /// struct used for convenience to manage header information in the symbol table
    /// </summary>
    internal struct HeaderEntry<T>
    {
        internal Position Position { get; }

        internal readonly string SymbolName;
        internal readonly Tuple<string, Range> PositionedSymbol;

        internal readonly T Declaration;
        internal readonly ImmutableArray<AttributeAnnotation> Attributes;
        internal readonly ImmutableArray<string> Documentation;
        internal readonly QsComments Comments;

        private HeaderEntry(
            CodeFragment.TokenIndex tIndex,
            Position offset,
            (string, Range) sym,
            T decl,
            ImmutableArray<AttributeAnnotation> attributes,
            ImmutableArray<string> doc,
            QsComments comments)
        {
            this.Position = offset;
            this.SymbolName = sym.Item1;
            this.PositionedSymbol = new Tuple<string, Range>(sym.Item1, sym.Item2);
            this.Declaration = decl;
            this.Attributes = attributes;
            this.Documentation = doc;
            this.Comments = comments ?? QsComments.Empty;
        }

        /// <summary>
        /// Tries to construct a HeaderItem from the given token index using the given GetDeclaration.
        /// If the construction succeeds, returns the HeaderItem.
        /// If the symbol of the extracted declaration is not an unqualified symbol,
        /// verifies that it corresponds instead to an invalid symbol and returns null unless the keepInvalid parameter has been set to a string value.
        /// If the keepInvalid parameter has been set to a (non-null) string, uses that string as the SymbolName for the returned HeaderEntry instance.
        /// </summary>
        /// <exception cref="ArgumentException">The symbol of the extracted declaration is not an unqualified or invalid symbol.</exception>
        /// <exception cref="ArgumentException">The extracted declaration is Null.</exception>
        internal static HeaderEntry<T>? From(
            Func<CodeFragment, QsNullable<Tuple<QsSymbol, T>>> getDeclaration,
            CodeFragment.TokenIndex tIndex,
            ImmutableArray<AttributeAnnotation> attributes,
            ImmutableArray<string> doc,
            string? keepInvalid = null)
        {
            var fragment = tIndex.GetFragmentWithClosingComments();
            var extractedDecl = getDeclaration(fragment);
            var (sym, decl) =
                extractedDecl.IsNull
                ? throw new ArgumentException("extracted declaration is Null")
                : extractedDecl.Item;

            var symName = sym.Symbol.AsDeclarationName(keepInvalid);
            if (symName == null && !sym.Symbol.IsInvalidSymbol)
            {
                throw new ArgumentException("extracted declaration does not have a suitable name");
            }

            var symRange = sym.Range.IsNull ? Range.Zero : sym.Range.Item;
            return symName == null
                ? (HeaderEntry<T>?)null
                : new HeaderEntry<T>(tIndex, fragment.Range.Start, (symName, symRange), decl, attributes, doc, fragment.Comments);
        }
    }

    /// <summary>
    /// this class contains information about everything that impacts type checking on a global scope
    /// </summary>
    internal class FileHeader // *don't* dispose of the sync root!
    {
        public readonly ReaderWriterLockSlim SyncRoot;

        // IMPORTANT: quite a couple of places rely on these being sorted!
        private readonly ManagedSortedSet namespaceDeclarations;
        private readonly ManagedSortedSet openDirectives;
        private readonly ManagedSortedSet typeDeclarations;
        private readonly ManagedSortedSet callableDeclarations;

        public FileHeader(ReaderWriterLockSlim syncRoot)
        {
            this.SyncRoot = syncRoot;
            this.namespaceDeclarations = new ManagedSortedSet(syncRoot);
            this.openDirectives = new ManagedSortedSet(syncRoot);
            this.typeDeclarations = new ManagedSortedSet(syncRoot);
            this.callableDeclarations = new ManagedSortedSet(syncRoot);
        }

        /// <summary>
        /// Invalidates (i.e. removes) all elements in the range [start, start + count), and
        /// updates all elements that are larger than or equal to start + count with the given lineNrChange.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="start"/> or <paramref name="count"/> are negative, or <paramref name="lineNrChange"/> is smaller than -<paramref name="count"/>.
        /// </exception>
        public void InvalidateOrUpdate(int start, int count, int lineNrChange)
        {
            this.namespaceDeclarations.InvalidateOrUpdate(start, count, lineNrChange);
            this.openDirectives.InvalidateOrUpdate(start, count, lineNrChange);
            this.typeDeclarations.InvalidateOrUpdate(start, count, lineNrChange);
            this.callableDeclarations.InvalidateOrUpdate(start, count, lineNrChange);
        }

        /// <summary>
        /// Returns an array with the *sorted* line numbers containing namespace declarations.
        /// </summary>
        public int[] GetNamespaceDeclarations()
        {
            return this.namespaceDeclarations.ToArray();
        }

        /// <summary>
        /// Returns an array with the *sorted* line numbers containing open directives.
        /// </summary>
        public int[] GetOpenDirectives()
        {
            return this.openDirectives.ToArray();
        }

        /// <summary>
        /// Returns an array with the *sorted* line numbers containing type declarations.
        /// </summary>
        public int[] GetTypeDeclarations()
        {
            return this.typeDeclarations.ToArray();
        }

        /// <summary>
        /// Returns an array with the *sorted* line numbers containing callable declarations.
        /// </summary>
        public int[] GetCallableDeclarations()
        {
            return this.callableDeclarations.ToArray();
        }

        public void AddNamespaceDeclarations(IEnumerable<int> declarations) =>
            this.namespaceDeclarations.Add(declarations);

        public void AddOpenDirectives(IEnumerable<int> declarations) =>
            this.openDirectives.Add(declarations);

        public void AddTypeDeclarations(IEnumerable<int> declarations) =>
            this.typeDeclarations.Add(declarations);

        public void AddCallableDeclarations(IEnumerable<int> declarations) =>
            this.callableDeclarations.Add(declarations);

        public static bool IsNamespaceDeclaration(CodeFragment fragment) =>
            fragment?.Kind != null && fragment.IncludeInCompilation && fragment.Kind.IsNamespaceDeclaration;

        public static bool IsOpenDirective(CodeFragment fragment) =>
            fragment?.Kind != null && fragment.IncludeInCompilation && fragment.Kind.IsOpenDirective;

        public static bool IsTypeDeclaration(CodeFragment fragment) =>
            fragment?.Kind != null && fragment.IncludeInCompilation && fragment.Kind.IsTypeDefinition;

        public static bool IsCallableDeclaration(CodeFragment fragment) =>
            fragment?.Kind != null && fragment.IncludeInCompilation && (
                fragment.Kind.IsOperationDeclaration ||
                fragment.Kind.IsFunctionDeclaration);

        public static bool IsCallableSpecialization(CodeFragment fragment) =>
            fragment?.Kind != null && fragment.IncludeInCompilation && (
                fragment.Kind.IsBodyDeclaration ||
                fragment.Kind.IsAdjointDeclaration ||
                fragment.Kind.IsControlledDeclaration ||
                fragment.Kind.IsControlledAdjointDeclaration);

        public static bool IsHeaderItem(CodeFragment fragment) =>
            IsNamespaceDeclaration(fragment) ||
            IsOpenDirective(fragment) ||
            IsTypeDeclaration(fragment) ||
            IsCallableDeclaration(fragment) ||
            IsCallableSpecialization(fragment);

        public static IEnumerable<CodeFragment>? FilterNamespaceDeclarations(IEnumerable<CodeFragment> fragments) =>
            fragments?.Where(IsNamespaceDeclaration);

        public static IEnumerable<CodeFragment>? FilterOpenDirectives(IEnumerable<CodeFragment> fragments) =>
            fragments?.Where(IsOpenDirective);

        public static IEnumerable<CodeFragment>? FilterTypeDeclarations(IEnumerable<CodeFragment> fragments) =>
            fragments?.Where(IsTypeDeclaration);

        public static IEnumerable<CodeFragment>? FilterCallableDeclarations(IEnumerable<CodeFragment> fragments) =>
            fragments?.Where(IsCallableDeclaration);

        public static IEnumerable<CodeFragment>? FilterCallableSpecializations(IEnumerable<CodeFragment> fragments) =>
            fragments?.Where(IsCallableSpecialization);
    }

    /// <summary>
    /// threadsafe wrapper to <see cref="SortedSet{T}"/> whose generic type argument is <see cref="int"/>.
    /// </summary>
    public class ManagedSortedSet // *don't* dispose of the sync root!
    {
        private SortedSet<int> set = new SortedSet<int>();
        public readonly ReaderWriterLockSlim SyncRoot;

        public ManagedSortedSet(ReaderWriterLockSlim syncRoot)
        {
            this.SyncRoot = syncRoot;
        }

        public int[] ToArray()
        {
            this.SyncRoot.EnterReadLock();
            try
            {
                return this.set.ToArray();
            }
            finally
            {
                this.SyncRoot.ExitReadLock();
            }
        }

        public void Add(IEnumerable<int> items)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.set.UnionWith(items);
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void Add(params int[] items)
        {
            this.Add((IEnumerable<int>)items);
        }

        /// <summary>
        /// Clears all elements from the set and returned the removed elements.
        /// </summary>
        public SortedSet<int> Clear()
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                return this.set;
            }
            finally
            {
                this.set = new SortedSet<int>();
                this.SyncRoot.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes all elements in the range [start, start + count) from the set, and
        /// updates all elements that are larger than or equal to start + count with lineNr => lineNr + lineNrChange.
        /// Returns the number of removed elements.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> or <paramref name="count"/> are negative, or <paramref name="lineNrChange"/> is smaller than -<paramref name="count"/>.</exception>
        public int InvalidateOrUpdate(int start, int count, int lineNrChange)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (lineNrChange < -count)
            {
                throw new ArgumentOutOfRangeException(nameof(lineNrChange));
            }

            this.SyncRoot.EnterWriteLock();
            try
            {
                var nrRemoved = this.set.RemoveWhere(lineNr => start <= lineNr && lineNr < start + count);
                if (lineNrChange != 0)
                {
                    var updatedLineNrs = this.set.Where(lineNr => lineNr >= start + count).Select(lineNr => lineNr + lineNrChange).ToArray(); // calling ToArray to make sure updateLineNrs is not affected by RemoveWhere below
                    this.set.RemoveWhere(lineNr => start <= lineNr);
                    this.set.UnionWith(updatedLineNrs);
                }
                return nrRemoved;
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }
    }

    /// <summary>
    /// threadsafe wrapper to <see cref="HashSet{T}"/>
    /// </summary>
    public class ManagedHashSet<T> // *don't* dispose of the sync root!
    {
        private readonly HashSet<T> content = new HashSet<T>();
        public readonly ReaderWriterLockSlim SyncRoot;

        public ManagedHashSet(IEnumerable<T> collection, ReaderWriterLockSlim syncRoot)
        {
            this.content = new HashSet<T>(collection);
            this.SyncRoot = syncRoot;
        }

        public ManagedHashSet(ReaderWriterLockSlim syncRoot)
            : this(Enumerable.Empty<T>(), syncRoot)
        {
        }

        public void Add(T item)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content.Add(item);
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void RemoveAll(Func<T, bool> condition)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content.RemoveWhere(item => condition(item));
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public ImmutableHashSet<T> ToImmutableHashSet()
        {
            this.SyncRoot.EnterReadLock();
            try
            {
                return this.content.ToImmutableHashSet();
            }
            finally
            {
                this.SyncRoot.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// threadsafe wrapper to <see cref="List{T}"/>
    /// </summary>
    public class ManagedList<T> // *don't* dispose of the sync root!
    {
        private List<T> content = new List<T>();
        public readonly ReaderWriterLockSlim SyncRoot;

        private ManagedList(List<T> content, ReaderWriterLockSlim syncRoot)
        {
            this.content = content;
            this.SyncRoot = syncRoot;
        }

        public ManagedList(IEnumerable<T> collection, ReaderWriterLockSlim syncRoot)
            : this(collection.ToList(), syncRoot)
        {
        }

        public ManagedList(ReaderWriterLockSlim syncRoot)
            : this(new List<T>(), syncRoot)
        {
        }

        // members for content manipulation

        public void Add(T item)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content.Add(item);
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void AddRange(IEnumerable<T> elements)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content.AddRange(elements);
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void RemoveRange(int index, int count)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content.RemoveRange(index, count);
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void RemoveAll(Func<T, bool> condition)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content.RemoveAll(item => condition(item));
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void InsertRange(int index, IEnumerable<T> newContent)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content.InsertRange(index, newContent);
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public IEnumerable<T> Get()
        {
            this.SyncRoot.EnterReadLock();
            try
            {
                // creates a shallow copy
                return this.content.GetRange(0, this.content.Count);
            }
            finally
            {
                this.SyncRoot.ExitReadLock();
            }
        }

        public IEnumerable<T> GetRange(int index, int count)
        {
            this.SyncRoot.EnterReadLock();
            try
            {
                // creates a shallow copy
                return this.content.GetRange(index, count);
            }
            finally
            {
                this.SyncRoot.ExitReadLock();
            }
        }

        public T GetItem(int index)
        {
            this.SyncRoot.EnterReadLock();
            try
            {
                return this.content[index];
            }
            finally
            {
                this.SyncRoot.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets the item at the index if the index does not exceed the bounds of the list.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0.</exception>
        internal bool TryGetItem(int index, [MaybeNullWhen(false)] out T item)
        {
            this.SyncRoot.EnterReadLock();
            try
            {
                if (index < this.content.Count)
                {
                    item = this.content[index];
                    return true;
                }
                item = default;
                return false;
            }
            finally
            {
                this.SyncRoot.ExitReadLock();
            }
        }

        public void SetItem(int index, T newItem)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content[index] = newItem;
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void Replace(int start, int count, IReadOnlyList<T> replacements)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                if (replacements.Count - count != 0)
                {
                    this.content.RemoveRange(start, count);
                    this.content.InsertRange(start, replacements);
                }
                else
                {
                    for (var offset = 0; offset < count; ++offset)
                    {
                        this.content[start + offset] = replacements[offset];
                    }
                }
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void ReplaceAll(IEnumerable<T> replacement)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content = replacement.ToList();
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void ReplaceAll(ManagedList<T> replacement)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content = replacement.ToList();
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void Transform(int index, Func<T, T> transformation)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content[index] = transformation(this.content[index]);
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void Transform(Func<T, T> transformation)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                for (var index = 0; index < this.content.Count(); ++index)
                {
                    this.content[index] = transformation(this.content[index]);
                }
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public List<T> Clear()
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                return this.content;
            }
            finally
            {
                this.content = new List<T>();
                this.SyncRoot.ExitWriteLock();
            }
        }

        public int Count()
        {
            this.SyncRoot.EnterReadLock();
            try
            {
                return this.content.Count();
            }
            finally
            {
                this.SyncRoot.ExitReadLock();
            }
        }

        public List<T> ToList()
        {
            this.SyncRoot.EnterReadLock();
            try
            {
                return this.content.ToList();
            }
            finally
            {
                this.SyncRoot.ExitReadLock();
            }
        }
    }
}
