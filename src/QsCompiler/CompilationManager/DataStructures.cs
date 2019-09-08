// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.TextProcessing;
using Microsoft.VisualStudio.LanguageServer.Protocol;


namespace Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures
{
    /// <summary>
    /// Contains all information managed by the ScopeTracking.
    /// All properties are readonly, and any modification leads to the creation of a new instance.
    /// </summary>
    internal class CodeLine 
    {
        internal readonly string Text;
        internal readonly string LineEnding; // contains the line break for this line (included in text)
        /// <summary>
        /// contains the text content of the line, without any end of line comment and *without* the line break
        /// </summary>
        public readonly string WithoutEnding;
        /// <summary>
        /// Contains the end of line comment without any leading or trailing whitespace, and without the comment slashes. 
        /// Is null if no such comment exists. 
        /// Documenting comments (i.e. trippe-slash comments) are *not* considered to be end of line comments. 
        /// All comments which *either* follow non-whitespace content or do not start with triple-slash are considered end of line comments.
        /// </summary>
        public readonly string EndOfLineComment;

        internal readonly int Indentation; // Note: This denotes the initial indentation at the beginning of the line
        internal readonly ImmutableArray<int> ExcessBracketPositions;

        // convention: first one opens a string, and we have the usual inclusions: {0,5} means the chars 0..4 are the content of a string
        // -> i.e. -1 means the string starts on a previous line, and an end delimiter that is equal to Text.Length means that the string continues on the next line
        internal readonly ImmutableArray<int> StringDelimiters; // note that this property is only properly initialized *after* knowing the sourrounding lines

        public CodeLine(string text, IEnumerable<int> delimiters, int eolComment, int indentation, IEnumerable<int> excessBrackets)
        {
            this.Text = text;
            this.LineEnding = Utils.EndOfLine.Match(text).Value; // empty string if the matching failed

            var lineLength = text.Length - LineEnding.Length;
            if (eolComment > lineLength) eolComment = lineLength;
            this.WithoutEnding = text.Substring(0, eolComment);
            var commentStr = text.Substring(eolComment, lineLength - eolComment).Trim();
            var isDocComment = String.IsNullOrWhiteSpace(this.WithoutEnding) && (commentStr.Length - commentStr.TrimStart('/').Length == 3);
            var hasComment = commentStr.StartsWith("//") && !isDocComment;
            this.EndOfLineComment = hasComment ? commentStr.Substring(2) : null;

            this.StringDelimiters = delimiters.ToImmutableArray();
            this.Indentation = indentation;
            this.ExcessBracketPositions = excessBrackets.ToImmutableArray();

            if (eolComment < 0 || (eolComment != text.Length && eolComment > text.Length - this.LineEnding.Length))
                throw new ArgumentOutOfRangeException(nameof(eolComment));
            ScopeTracking.VerifyStringDelimiters(text, delimiters);
            ScopeTracking.VerifyExcessBracketPositions(this, excessBrackets);
        }

        public CodeLine(string text, IEnumerable<int> delimiters, int eofComment) 
            : this(text, delimiters, eofComment, 0, new List<int>()) { }

        public static CodeLine Empty() => new CodeLine(String.Empty, Enumerable.Empty<int>(), 0);

        internal CodeLine SetText(string newText)
        { return new CodeLine(newText, StringDelimiters, WithoutEnding.Length, Indentation, ExcessBracketPositions); }

        internal CodeLine SetStringDelimiters(IEnumerable<int> newStringDelimiters)
        { return new CodeLine(Text, newStringDelimiters, WithoutEnding.Length, Indentation, ExcessBracketPositions); }

        internal CodeLine SetCommentIndex(int newCommentIndex)
        { return new CodeLine(Text, StringDelimiters, newCommentIndex, Indentation, ExcessBracketPositions); }

        internal CodeLine SetIndentation(int newIndentation)
        { return new CodeLine(Text, StringDelimiters, WithoutEnding.Length, newIndentation, ExcessBracketPositions); }

        internal CodeLine SetExcessBrackets(IEnumerable<int> newExcessBrackets)
        { return new CodeLine(Text, StringDelimiters, WithoutEnding.Length, Indentation, newExcessBrackets); }
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
        /// returns a copy of the CodeFragment Range
        /// </summary>
        internal Range GetRange() => this.FragmentRange.Copy();

        private readonly Range FragmentRange;
        internal readonly Tuple<QsPositionInfo, QsPositionInfo> HeaderRange;
        internal readonly int Indentation;
        internal readonly string Text;
        internal readonly char FollowedBy;
        internal readonly QsComments Comments;
        public readonly QsFragmentKind Kind;
        internal bool IncludeInCompilation { get; private set; } // used to exclude certain code fragments from the compilation if they are e.g. misplaced

        internal const char MissingDelimiter = '@'; // arbitrarily chosen
        internal static readonly ImmutableArray<char> DelimitingChars = ImmutableArray.Create('}', '{', ';');

        private static Tuple<QsPositionInfo, QsPositionInfo> GetHeaderRange(string text, QsFragmentKind kind) =>
            kind == null ? QsCompilerDiagnostic.DefaultRange : kind.IsControlledAdjointDeclaration
                ? Parsing.HeaderDelimiters(2).Invoke(text ?? "")
                : Parsing.HeaderDelimiters(1).Invoke(text ?? "");

        /// <summary>
        /// Note that the only thing that may be set to null is the fragment kind - all other properties need to be set upon initialization
        /// </summary>
        private CodeFragment(int indent, Range r, string text, char next, QsComments comments, QsFragmentKind kind, bool include)
        {
            if (!Utils.IsValidRange(r)) throw new ArgumentException("invalid range for code fragment");
            if (!DelimitingChars.Contains(next) && next != MissingDelimiter) throw new ArgumentException("a CodeFragment needs to be followed by a DelimitingChar");
            this.Indentation = indent < 0 ? throw new ArgumentException("indentation needs to be positive") : indent;
            this.Text = text?.TrimEnd() ?? throw new ArgumentNullException(nameof(text));
            this.FollowedBy = next;
            this.Comments = comments ?? QsComments.Empty;
            this.Kind = kind; // nothing here should be modifiable
            this.FragmentRange = r.Copy();
            this.HeaderRange = GetHeaderRange(this.Text, this.Kind);
            this.IncludeInCompilation = include;
        }

        internal CodeFragment(int indent, Range r, string text, char next, QsFragmentKind kind = null) :
            this(indent, r, text, next, null, kind, true)
        { }

        internal CodeFragment Copy() =>
            new CodeFragment(this.Indentation, this.GetRange(), this.Text, this.FollowedBy, this.Comments, this.Kind, this.IncludeInCompilation);

        public bool Equals(CodeFragment other)
        {
            if (other == null) return false;
            return
                this.GetRange().Equals(other.GetRange()) &&
                this.Indentation == other.Indentation &&
                this.Text == other.Text &&
                this.FollowedBy == other.FollowedBy &&
                this.IncludeInCompilation == other.IncludeInCompilation &&
                (this.Kind == null ? other.Kind == null : this.Kind.Equals(other.Kind));
        }

        internal CodeFragment WithUpdatedLineNumber(int lineNrChange) => 
            this?.SetRange(this.GetRange().WithUpdatedLineNumber(lineNrChange));

        internal CodeFragment SetRange(Range range) =>
            new CodeFragment(this.Indentation, range, this.Text, this.FollowedBy, this.Comments, this.Kind, this.IncludeInCompilation); 

        internal CodeFragment SetIndentation(int indent) =>
            new CodeFragment(indent, this.FragmentRange, this.Text, this.FollowedBy, this.Comments, this.Kind, this.IncludeInCompilation); 

        internal CodeFragment SetCode(string code) =>
            new CodeFragment(this.Indentation, this.FragmentRange, code, this.FollowedBy, this.Comments, this.Kind, this.IncludeInCompilation); 

        internal CodeFragment SetFollowedBy(char delim) =>
            new CodeFragment(this.Indentation, this.FragmentRange, this.Text, delim, this.Comments, this.Kind, this.IncludeInCompilation);

        internal CodeFragment SetKind(QsFragmentKind kind) =>
            new CodeFragment(this.Indentation, this.FragmentRange, this.Text, this.FollowedBy, this.Comments, kind, this.IncludeInCompilation);

        internal CodeFragment ClearComments() =>
            new CodeFragment(this.Indentation, this.FragmentRange, this.Text, this.FollowedBy, null, this.Kind, this.IncludeInCompilation);

        internal CodeFragment SetOpeningComments(IEnumerable<string> commentsBefore)
        {
            var relevantComments = commentsBefore.SkipWhile(c => c == null).Reverse();
            relevantComments = relevantComments.SkipWhile(c => c == null).Reverse();
            var comments = new QsComments(relevantComments.Select(c => c ?? String.Empty).ToImmutableArray(), this.Comments.ClosingComments);
            return new CodeFragment(this.Indentation, this.FragmentRange, this.Text, this.FollowedBy, comments, this.Kind, this.IncludeInCompilation);
        }

        internal CodeFragment SetClosingComments(IEnumerable<string> commentsAfter)
        {
            var relevantComments = commentsAfter.SkipWhile(c => c == null).Reverse();
            relevantComments = relevantComments.SkipWhile(c => c == null).Reverse();
            var comments = new QsComments(this.Comments.OpeningComments, relevantComments.Select(c => c ?? String.Empty).ToImmutableArray());
            return new CodeFragment(this.Indentation, this.FragmentRange, this.Text, this.FollowedBy, comments, this.Kind, this.IncludeInCompilation);
        }


        /// <summary>
        /// A class to conveniently walk the saved tokens.
        /// This class is a subclass of CodeFragment to limit access to IncludeInCompilation to be via TokenIndex.
        /// </summary>
        internal class TokenIndex // not disposable because File mustn't be disposed since several token indices may be using it
        {
            private readonly FileContentManager File;
            public int Line { get; private set; }
            public int Index { get; private set; }

            /// <summary>
            /// Verifies the given line number and index *only* against the Tokens listed in file (and not against the content) 
            /// and initializes an instance of TokenIndex.
            /// Throws an ArgumentNullException if file is null.
            /// Throws an ArgumentOutOfRangeException if line or index are negative, 
            /// or line is larger than or equal to the number of Tokens lists in file, 
            /// or index is larger than or equal to the number of Tokens on the given line.
            /// </summary>
            internal TokenIndex(FileContentManager file, int line, int index)
            {
                this.File = file ?? throw new ArgumentNullException(nameof(file));
                if (line < 0 || line >= file.NrTokenizedLines()) throw new ArgumentOutOfRangeException(nameof(line));
                if (index < 0 || index >= file.GetTokenizedLine(line).Length) throw new ArgumentOutOfRangeException(nameof(index));

                this.Line = line;
                this.Index = index;
            }

            internal TokenIndex(TokenIndex tIndex)
                : this(tIndex.File, tIndex.Line, tIndex.Index) { }

            private bool IsWithinFile() => // used within class methods, since the file itself may change during the lifetime of the token ...
                this.Line < this.File.NrTokenizedLines() && this.Index < this.File.GetTokenizedLine(this.Line).Length;


            /// <summary>
            /// Marks the token returned by GetToken for the associated file as excluded from the compilation.
            /// Throws an InvalidOperationException if Line and Index are no longer within the associated file.
            /// </summary>
            internal void MarkAsExcluded()
            {
                if (!this.IsWithinFile()) throw new InvalidOperationException("token index is no longer valid within its associated file");
                this.File.GetTokenizedLine(this.Line)[this.Index].IncludeInCompilation = false;
            }

            /// <summary>
            /// Marks the token returned by GetToken for the associated file as included in the compilation.
            /// Throws an InvalidOperationException if Line and Index are no longer within the associated file.
            /// </summary>
            internal void MarkAsIncluded()
            {
                if (!this.IsWithinFile()) throw new InvalidOperationException("token index is no longer valid within its associated file");
                this.File.GetTokenizedLine(this.Line)[this.Index].IncludeInCompilation = true;
            }

            /// <summary>
            /// Returns the corresponding fragment for the token at the saved TokenIndex - 
            /// i.e. a copy of the token where its range denotes the absolute range within the file.
            /// Throws an InvalidOperationException if the token is no longer within the file associated with it.
            /// </summary>
            internal CodeFragment GetFragment()
            {
                if (!this.IsWithinFile()) throw new InvalidOperationException("token index is no longer valid within its associated file");
                return this.File.GetTokenizedLine(this.Line)[this.Index].WithUpdatedLineNumber(this.Line);
            }

            /// <summary>
            /// Returns the corresponding fragment for the token at the saved TokenIndex including any closing comments for that fragment - 
            /// i.e. a copy of the token where its range denotes the absolute range within the file.
            /// Throws an InvalidOperationException if the token is no longer within the file associated with it.
            /// </summary>
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
                    { fragment = fragment.SetClosingComments(lastChild.Comments.OpeningComments); }
                }
                return fragment;
            }

            /// <summary>
            /// Returns the TokenIndex of the next token in File or null if no such token exists.
            /// Throws an ArgumentNullException if the TokenIndex to increment is null.
            /// Throws an InvalidOperationException if the token is no longer within the file associated with it.
            /// </summary>
            public static TokenIndex operator ++(TokenIndex tIndex)
            {
                if (tIndex == null) throw new ArgumentNullException(nameof(tIndex));
                if (!tIndex.IsWithinFile()) throw new InvalidOperationException("token index is no longer valid within its associated file");
                var res = new TokenIndex(tIndex); // the overload for ++ must *not* mutate the argument - this is handled/done by the compiler 
                if (++res.Index < res.File.GetTokenizedLine(res.Line).Length) return res;
                res.Index = 0;
                while (++res.Line < res.File.NrTokenizedLines() && res.File.GetTokenizedLine(res.Line).Length == 0) ;
                return res.Line == res.File.NrTokenizedLines() ? null : res;
            }

            /// <summary>
            /// Returns the TokenIndex of the previous token in File or null if no such token exists.
            /// Throws an ArgumentNullException if the TokenIndex to decrement is null.
            /// Throws an InvalidOperationException if the token is no longer within the file associated with it.
            /// </summary>
            public static TokenIndex operator --(TokenIndex tIndex)
            {
                if (tIndex == null) throw new ArgumentNullException(nameof(tIndex));
                if (!tIndex.IsWithinFile()) throw new InvalidOperationException("token index is no longer valid within its associated file");
                var res = new TokenIndex(tIndex); // the overload for -- must *not* mutate the argument - this is handled/done by the compiler 
                if (res.Index-- > 0) return res;
                while (res.Index < 0 && res.Line-- > 0) res.Index = res.File.GetTokenizedLine(res.Line).Length - 1;
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
            private readonly Position relPosition;
            private readonly Position rootPosition;
            public readonly CodeFragment Fragment;
            public readonly IReadOnlyList<TreeNode> Children;
            public Position GetRootPosition() => rootPosition.Copy();
            public Position GetPositionRelativeToRoot() => relPosition.Copy();

            /// <summary>
            /// Builds the TreeNode consisting of the given fragment and children.
            /// RelativeToRoot is set to the position of the fragment start relative to the given parent start position.
            /// Throws an ArgumentException if the given parent start position is invalid, or larger than the fragment start position.
            /// Throws an ArgumentNullException if any of the given arguments is null.
            /// </summary>
            public TreeNode(CodeFragment fragment, IReadOnlyList<TreeNode> children, Position parentStart)
            {
                this.Fragment = fragment ?? throw new ArgumentNullException(nameof(fragment));
                this.Children = children ?? throw new ArgumentNullException(nameof(children));

                var fragStart = fragment.GetRange().Start;
                if (!Utils.IsValidPosition(parentStart)) throw new ArgumentException(nameof(parentStart));
                if (fragStart.IsSmallerThan(parentStart)) throw new ArgumentException(nameof(parentStart), "parentStart needs to be smaller than or equal to the fragment start");
                this.rootPosition = parentStart;
                this.relPosition = fragStart.Subtract(parentStart);
            }
        }
        public readonly NonNullable<string> Source;
        public readonly NonNullable<string> Namespace;
        public readonly NonNullable<string> Callable;
        public readonly IReadOnlyList<TreeNode> Specializations;

        public FragmentTree(NonNullable<string> source, NonNullable<string> ns, NonNullable<string> callable, IEnumerable<TreeNode> specs)
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
        private readonly Position Position; 
        internal Position GetPosition() => this.Position.Copy();

        internal readonly NonNullable<string> SymbolName;
        internal readonly Tuple<NonNullable<string>, Tuple<QsPositionInfo, QsPositionInfo>> PositionedSymbol;

        internal readonly T Declaration;
        internal readonly ImmutableArray<AttributeAnnotation> Attributes;
        internal readonly ImmutableArray<string> Documentation;
        internal readonly QsComments Comments;

        private HeaderEntry(CodeFragment.TokenIndex tIndex, Position offset, 
            (NonNullable<string>, Tuple<QsPositionInfo, QsPositionInfo>) sym, T decl, ImmutableArray<AttributeAnnotation> attributes, ImmutableArray<string> doc, QsComments comments)
        {
            if (tIndex == null) throw new ArgumentNullException(nameof(tIndex));
            if (!Utils.IsValidPosition(offset)) throw new ArgumentException(nameof(offset));

            this.Position = offset.Copy();
            this.SymbolName = sym.Item1;
            this.PositionedSymbol = new Tuple<NonNullable<string>, Tuple<QsPositionInfo, QsPositionInfo>>(sym.Item1, sym.Item2);
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
        /// Throws an ArgumentException if this verification fails as well. 
        /// Throws an ArgumentException if the extracted declaration is Null.
        /// Throws an ArgumentNullException if the given token index is null.
        /// </summary>
        static internal HeaderEntry<T>? From(Func<CodeFragment, QsNullable<Tuple<QsSymbol, T>>> GetDeclaration, 
            CodeFragment.TokenIndex tIndex, ImmutableArray<AttributeAnnotation> attributes, ImmutableArray<string> doc, string keepInvalid = null)
        {
            if (GetDeclaration == null) throw new ArgumentNullException(nameof(GetDeclaration));
            if (tIndex == null) throw new ArgumentNullException(nameof(tIndex));

            var fragment = tIndex.GetFragmentWithClosingComments();
            var fragmentStart = fragment.GetRange().Start;

            var extractedDecl = GetDeclaration(fragment);
            var (sym, decl) =
                extractedDecl.IsNull
                ? throw new ArgumentException("extracted declaration is Null")
                : extractedDecl.Item;

            var symName = sym.Symbol.AsDeclarationName(keepInvalid);
            if (symName == null && !sym.Symbol.IsInvalidSymbol) throw new ArgumentException("extracted declaration does not have a suitable name");

            var symRange =
                sym.Range.IsNull
                ? new Tuple<QsPositionInfo, QsPositionInfo>(QsPositionInfo.Zero, QsPositionInfo.Zero)
                : sym.Range.Item;

            return symName == null 
                ? (HeaderEntry<T>?)null 
                : new HeaderEntry<T>(tIndex, fragmentStart, (NonNullable<string>.New(symName), symRange), decl, attributes, doc, fragment.Comments);
        }
    }


    /// <summary>
    /// this class contains information about everything that impacts type checking on a global scope
    /// </summary>
    internal class FileHeader // *don't* dispose of the sync root!
    {
        public readonly ReaderWriterLockSlim SyncRoot;

        // IMPORTANT: quite a couple of places rely on these being sorted!
        private readonly ManagedSortedSet NamespaceDeclarations;
        private readonly ManagedSortedSet OpenDirectives;
        private readonly ManagedSortedSet TypeDeclarations;
        private readonly ManagedSortedSet CallableDeclarations;

        public FileHeader(ReaderWriterLockSlim syncRoot)
        {
            this.SyncRoot = syncRoot ?? throw new ArgumentNullException(nameof(syncRoot));
            this.NamespaceDeclarations = new ManagedSortedSet(syncRoot);
            this.OpenDirectives = new ManagedSortedSet(syncRoot);
            this.TypeDeclarations = new ManagedSortedSet(syncRoot);
            this.CallableDeclarations = new ManagedSortedSet(syncRoot);
        }

        /// <summary>
        /// Invalidates (i.e. removes) all elements in the range [start, start + count), and 
        /// updates all elements that are larger than or equal to start + count with the given lineNrChange.
        /// Throws an ArgumentOutOfRange exception if start or count are negative, or if lineNrChange is smaller than -count.
        /// </summary>
        public void InvalidateOrUpdate(int start, int count, int lineNrChange)
        {
            this.NamespaceDeclarations.InvalidateOrUpdate(start, count, lineNrChange);
            this.OpenDirectives.InvalidateOrUpdate(start, count, lineNrChange);
            this.TypeDeclarations.InvalidateOrUpdate(start, count, lineNrChange);
            this.CallableDeclarations.InvalidateOrUpdate(start, count, lineNrChange);
        }

        /// <summary>
        /// Returns an array with the *sorted* line numbers containing namespace declarations.
        /// </summary>
        public int[] GetNamespaceDeclarations()
        { return this.NamespaceDeclarations.ToArray(); }

        /// <summary>
        /// Returns an array with the *sorted* line numbers containing open directives. 
        /// </summary>
        public int[] GetOpenDirectives()
        { return this.OpenDirectives.ToArray(); }

        /// <summary>
        /// Returns an array with the *sorted* line numbers containing type declarations. 
        /// </summary>
        public int[] GetTypeDeclarations()
        { return this.TypeDeclarations.ToArray(); }

        /// <summary>
        /// Returns an array with the *sorted* line numbers containing callable declarations. 
        /// </summary>
        public int[] GetCallableDeclarations()
        { return this.CallableDeclarations.ToArray(); }


        public void AddNamespaceDeclarations(IEnumerable<int> declarations) =>
            this.NamespaceDeclarations.Add(declarations); 

        public void AddOpenDirectives(IEnumerable<int> declarations) => 
            this.OpenDirectives.Add(declarations); 

        public void AddTypeDeclarations(IEnumerable<int> declarations) => 
            this.TypeDeclarations.Add(declarations); 

        public void AddCallableDeclarations(IEnumerable<int> declarations) => 
            this.CallableDeclarations.Add(declarations); 


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

        public static IEnumerable<CodeFragment> FilterNamespaceDeclarations(IEnumerable<CodeFragment> fragments) =>
            fragments?.Where(IsNamespaceDeclaration); 

        public static IEnumerable<CodeFragment> FilterOpenDirectives(IEnumerable<CodeFragment> fragments) =>
            fragments?.Where(IsOpenDirective); 

        public static IEnumerable<CodeFragment> FilterTypeDeclarations(IEnumerable<CodeFragment> fragments) => 
            fragments?.Where(IsTypeDeclaration); 

        public static IEnumerable<CodeFragment> FilterCallableDeclarations(IEnumerable<CodeFragment> fragments) =>
            fragments?.Where(IsCallableDeclaration);

        public static IEnumerable<CodeFragment> FilterCallableSpecializations(IEnumerable<CodeFragment> fragments) =>
            fragments?.Where(IsCallableSpecialization);
    }


    /// <summary>
    /// threadsafe wrapper to SortedSet<int>
    /// </summary>
    public class ManagedSortedSet // *don't* dispose of the sync root!
    {
        private SortedSet<int> Set = new SortedSet<int>();
        public readonly ReaderWriterLockSlim SyncRoot;

        public ManagedSortedSet(ReaderWriterLockSlim syncRoot)
        { this.SyncRoot = syncRoot; }

        public int[] ToArray()
        {
            this.SyncRoot.EnterReadLock();
            try { return this.Set.ToArray(); }
            finally { this.SyncRoot.ExitReadLock(); }
        }

        public void Add(IEnumerable<int> items)
        {
            this.SyncRoot.EnterWriteLock();
            try { this.Set.UnionWith(items); }
            finally { this.SyncRoot.ExitWriteLock(); }
        }

        public void Add(params int[] items)
        { this.Add((IEnumerable<int>)items); }

        /// <summary>
        /// Clears all elements from the set and returned the removed elements.
        /// </summary>
        public SortedSet<int> Clear()
        {
            this.SyncRoot.EnterWriteLock();
            try { return this.Set; }
            finally
            {
                this.Set = new SortedSet<int>();
                this.SyncRoot.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes all elements in the range [start, start + count) from the set, and
        /// updates all elements that are larger than or equal to start + count with lineNr => lineNr + lineNrChange.
        /// Returns the number of removed elements.
        /// Throws an ArgumentOutOfRange exception if start or count are negative, or if lineNrChange is smaller than -count.
        /// </summary>
        public int InvalidateOrUpdate(int start, int count, int lineNrChange)
        {
            if (start < 0) throw new ArgumentOutOfRangeException(nameof(start));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (lineNrChange < -count) throw new ArgumentOutOfRangeException(nameof(lineNrChange));

            this.SyncRoot.EnterWriteLock();
            try
            {
                var nrRemoved = this.Set.RemoveWhere(lineNr => start <= lineNr && lineNr < start + count);
                if (lineNrChange != 0)
                {
                    var updatedLineNrs = this.Set.Where(lineNr => lineNr >= start + count).Select(lineNr => lineNr + lineNrChange).ToArray(); // calling ToArray to make sure updateLineNrs is not affected by RemoveWhere below
                    this.Set.RemoveWhere(lineNr => start <= lineNr);
                    this.Set.UnionWith(updatedLineNrs);
                }
                return nrRemoved;
            }
            finally { this.SyncRoot.ExitWriteLock(); }
        }
    }


    /// <summary>
    /// threadsafe wrapper to HashSet<T>
    /// </summary>
    public class ManagedHashSet<T> // *don't* dispose of the sync root!
    {
        private readonly HashSet<T> Content = new HashSet<T>();
        public readonly ReaderWriterLockSlim SyncRoot;

        public ManagedHashSet(IEnumerable<T> collection, ReaderWriterLockSlim syncRoot)
        { Content = new HashSet<T>(collection); SyncRoot = syncRoot; }

        public ManagedHashSet(ReaderWriterLockSlim syncRoot)
            : this(Enumerable.Empty<T>(), syncRoot) { }

        public void Add(T item)
        {
            this.SyncRoot.EnterWriteLock();
            try { this.Content.Add(item); }
            finally { this.SyncRoot.ExitWriteLock(); }
        }

        public void RemoveAll(Func<T, bool> condition)
        {
            this.SyncRoot.EnterWriteLock();
            try { this.Content.RemoveWhere(item => condition(item)); }
            finally { this.SyncRoot.ExitWriteLock(); }
        }

        public ImmutableHashSet<T> ToImmutableHashSet()
        {
            this.SyncRoot.EnterReadLock();
            try { return this.Content.ToImmutableHashSet(); }
            finally { this.SyncRoot.ExitReadLock(); }
        }
    }


    /// <summary>
    /// threadsafe wrapper to List<T>
    /// </summary>
    public class ManagedList<T> // *don't* dispose of the sync root!
    {
        private List<T> Content = new List<T>();
        public readonly ReaderWriterLockSlim SyncRoot;

        private ManagedList(List<T> content, ReaderWriterLockSlim syncRoot)
        { Content = content; SyncRoot = syncRoot; }

        public ManagedList(IEnumerable<T> collection, ReaderWriterLockSlim syncRoot)
            : this(collection.ToList(), syncRoot) { }

        public ManagedList(ReaderWriterLockSlim syncRoot)
            : this(new List<T>(), syncRoot) { }


        // members for content manipulation

        public void Add(T item)
        {
            this.SyncRoot.EnterWriteLock();
            try { this.Content.Add(item); }
            finally { this.SyncRoot.ExitWriteLock(); }
        }

        public void AddRange(IEnumerable<T> elements)
        {
            this.SyncRoot.EnterWriteLock();
            try { this.Content.AddRange(elements); }
            finally { this.SyncRoot.ExitWriteLock(); }
        }

        public void RemoveRange(int index, int count)
        {
            this.SyncRoot.EnterWriteLock();
            try { this.Content.RemoveRange(index, count); }
            finally { this.SyncRoot.ExitWriteLock(); }
        }

        public void RemoveAll(Func<T, bool> condition)
        {
            this.SyncRoot.EnterWriteLock();
            try { this.Content.RemoveAll(item => condition(item)); }
            finally { this.SyncRoot.ExitWriteLock(); }
        }

        public void InsertRange(int index, IEnumerable<T> newContent)
        {
            this.SyncRoot.EnterWriteLock();
            try { this.Content.InsertRange(index, newContent); }
            finally { this.SyncRoot.ExitWriteLock(); }
        }

        public IEnumerable<T> Get()
        {
            this.SyncRoot.EnterReadLock();
            try { return this.Content.GetRange(0, this.Content.Count); } // creates a shallow copy
            finally { this.SyncRoot.ExitReadLock(); }
        }

        public IEnumerable<T> GetRange(int index, int count) 
        {
            this.SyncRoot.EnterReadLock();
            try { return this.Content.GetRange(index, count); } // creates a shallow copy
            finally { this.SyncRoot.ExitReadLock(); }
        }

        public T GetItem(int index) 
        {
            this.SyncRoot.EnterReadLock();
            try { return this.Content[index]; }
            finally { this.SyncRoot.ExitReadLock(); }
        }

        public void SetItem(int index, T newItem)
        {
            this.SyncRoot.EnterWriteLock();
            try { this.Content[index] = newItem; }
            finally { this.SyncRoot.ExitWriteLock(); }
        }

        public void Replace(int start, int count, IReadOnlyList<T> replacements)
        {
            if (replacements == null) throw new ArgumentNullException(nameof(replacements));
            this.SyncRoot.EnterWriteLock();
            try
            {
                if (replacements.Count - count != 0)
                {
                    this.Content.RemoveRange(start, count);
                    this.Content.InsertRange(start, replacements);
                }
                else
                {
                    for (var offset = 0; offset < count; ++offset)
                    { this.Content[start + offset] = replacements[offset]; }
                }
            }
            finally{ this.SyncRoot.ExitWriteLock(); }
        }

        public void ReplaceAll(IEnumerable<T> replacement)
        {
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));
            this.SyncRoot.EnterWriteLock();
            try { this.Content = replacement.ToList(); }
            finally { this.SyncRoot.ExitWriteLock(); }
        }

        public void ReplaceAll(ManagedList<T> replacement)
        {
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));
            this.SyncRoot.EnterWriteLock();
            try { this.Content = replacement.ToList(); }
            finally { this.SyncRoot.ExitWriteLock(); }
        }

        public void Transform(int index, Func<T, T> transformation)
        {
            if (transformation == null) throw new ArgumentNullException(nameof(transformation));
            this.SyncRoot.EnterWriteLock();
            try { this.Content[index] = transformation(this.Content[index]); }
            finally { this.SyncRoot.ExitWriteLock(); }
        }

        public void Transform(Func<T, T> transformation)
        {
            if (transformation == null) throw new ArgumentNullException(nameof(transformation));
            this.SyncRoot.EnterWriteLock();
            try
            {
                for (var index = 0; index < this.Content.Count(); ++index)
                { this.Content[index] = transformation(this.Content[index]); }
            }
            finally { this.SyncRoot.ExitWriteLock(); }
        }

        public List<T> Clear()
        {
            this.SyncRoot.EnterWriteLock();
            try { return this.Content; }
            finally
            {
                this.Content = new List<T>();
                this.SyncRoot.ExitWriteLock();
            }
        }

        public int Count()
        {
            this.SyncRoot.EnterReadLock();
            try { return this.Content.Count(); }
            finally { this.SyncRoot.ExitReadLock(); }
        }

        public List<T> ToList()
        {
            this.SyncRoot.EnterReadLock();
            try { return this.Content.ToList(); }
            finally { this.SyncRoot.ExitReadLock(); }
        }
    }
}
