﻿using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.TextProcessing;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using static Microsoft.Quantum.QsCompiler.TextProcessing.CompletionParsing;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    /// <summary>
    /// Data associated with a completion item that is used for resolving additional information.
    /// </summary>
    [DataContract]
    public class CompletionItemData
    {
        [DataMember(Name = "namespace")]
        private readonly string @namespace;

        [DataMember(Name = "name")]
        private readonly string name;

        /// <summary>
        /// The text document that the original completion request was made from.
        /// </summary>
        [DataMember(Name = "textDocument")]
        public TextDocumentIdentifier TextDocument { get; }

        /// <summary>
        /// The qualified name of the completion item.
        /// </summary>
        public QsQualifiedName QualifiedName
        {
            get => @namespace == null || name == null
                ? null
                : new QsQualifiedName(NonNullable<string>.New(@namespace), NonNullable<string>.New(name));
        }

        /// <summary>
        /// The source file the completion item is declared in.
        /// </summary>
        [DataMember(Name = "sourceFile")]
        public string SourceFile { get; }

        public CompletionItemData(
            TextDocumentIdentifier textDocument = null, QsQualifiedName qualifiedName = null, string sourceFile = null)
        {
            TextDocument = textDocument;
            @namespace = qualifiedName?.Namespace.Value;
            name = qualifiedName?.Name.Value;
            SourceFile = sourceFile;
        }
    }

    // Provides code completion for the language server.
    internal static partial class EditorSupport
    {
        /// <summary>
        /// Completion items for built-in type keywords.
        /// </summary>
        private static readonly IEnumerable<CompletionItem> typeKeywords =
            new[]
            {
                Types.Unit,
                Types.Int,
                Types.BigInt,
                Types.Double,
                Types.Bool,
                Types.Qubit,
                Types.Result,
                Types.Pauli,
                Types.Range,
                Types.String
            }
            .Select(type => new CompletionItem { Label = type, Kind = CompletionItemKind.Keyword });

        /// <summary>
        /// Completion items for built-in characteristic keywords.
        /// </summary>
        private static readonly IEnumerable<CompletionItem> characteristicKeywords =
            new[]
            {
                Types.AdjSet,
                Types.CtlSet
            }
            .Select(characteristic => new CompletionItem { Label = characteristic, Kind = CompletionItemKind.Keyword });

        /// <summary>
        /// Returns a list of suggested completion items for the given position.
        /// <para/>
        /// Returns null if any argument is null or the position is invalid.
        /// </summary>
        public static CompletionList Completions(
            this FileContentManager file, CompilationUnit compilation, Position position)
        {
            if (file == null || compilation == null || position == null || !Utils.IsValidPosition(position))
                return null;

            // If the character at the position is a dot but no valid namespace path precedes it (for example, in a
            // decimal number), then no completions are valid here.
            var nsPath = GetSymbolNamespacePath(file, position);
            if (nsPath == null &&
                position.Character > 0 &&
                file.GetLine(position.Line).Text[position.Character - 1] == '.')
            {
                return new CompletionList { IsIncomplete = false, Items = Array.Empty<CompletionItem>() };
            }

            IEnumerable<CompletionItem> completions;
            var env = GetCompletionEnvironment(file, position);
            if (env != null)
            {
                var fragment = GetTokenAtOrBefore(file, position)?.GetFragment();
                completions =
                    GetExpectedIdentifiers(env, GetFragmentTextBeforePosition(file, fragment, position))
                    .SelectMany(kind => GetCompletionsForKind(file, compilation, position, kind));
            }
            else if (nsPath != null)
            {
                var resolvedNsPath = ResolveNamespaceAlias(file, compilation, position, nsPath);
                completions =
                    GetCallableCompletions(file, compilation, new[] { resolvedNsPath })
                    .Concat(GetTypeCompletions(file, compilation, new[] { resolvedNsPath }))
                    .Concat(GetGlobalNamespaceCompletions(compilation, resolvedNsPath));
            }
            else if (!IsDeclaringNewSymbol(file, position))
            {
                var openNamespaces = GetOpenNamespaces(file, compilation, position);
                completions =
                    Keywords.ReservedKeywords
                    .Select(keyword => new CompletionItem { Label = keyword, Kind = CompletionItemKind.Keyword })
                    .Concat(GetLocalCompletions(file, compilation, position))
                    .Concat(GetCallableCompletions(file, compilation, openNamespaces))
                    .Concat(GetTypeCompletions(file, compilation, openNamespaces))
                    .Concat(GetGlobalNamespaceCompletions(compilation))
                    .Concat(GetNamespaceAliasCompletions(file, compilation, position));
            }
            else
            {
                completions = Array.Empty<CompletionItem>();
            }
            return new CompletionList { IsIncomplete = false, Items = completions.ToArray() };
        }

        /// <summary>
        /// Updates the given completion item with additional information if any is available. The completion item
        /// returned is a reference to the same completion item that was given; the given completion item is mutated
        /// with the additional information.
        /// <para/>
        /// Returns null (and the item is not updated) if any argument is null.
        /// </summary>
        public static CompletionItem ResolveCompletion(
            this CompilationUnit compilation, CompletionItem item, CompletionItemData data, MarkupKind format)
        {
            if (compilation == null || item == null || data == null)
                return null;
            var documentation = TryGetDocumentation(compilation, data, item.Kind, format == MarkupKind.Markdown);
            if (documentation != null)
                item.Documentation = new MarkupContent { Kind = format, Value = documentation };
            return item;
        }

        /// <summary>
        /// Returns the completion environment at the given position in the file or null if the environment cannot be
        /// determined.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the position is invalid.</exception>
        private static CompletionEnvironment GetCompletionEnvironment(FileContentManager file, Position position)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (!Utils.IsValidPosition(position))
                throw new ArgumentException(nameof(position));

            var token = GetTokenAtOrBefore(file, position);
            var relativeIndentation = token.GetFragment().Indentation - file.IndentationAt(position);
            var parent =
                new[] { token }.Concat(token.GetNonEmptyParents()).Skip(relativeIndentation + 1).FirstOrDefault();

            // TODO: Support context-aware completions for additional environments.
            if (parent != null && parent.GetFragment().Kind.IsNamespaceDeclaration)
                return CompletionEnvironment.NamespaceTopLevel;
            return null;
        }

        /// <summary>
        /// Returns completion items that match the given identifier kind.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the position is invalid.</exception>
        private static IEnumerable<CompletionItem> GetCompletionsForKind(
            FileContentManager file,
            CompilationUnit compilation,
            Position position,
            IdentifierKind kind,
            string namespacePrefix = "")
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));
            if (!Utils.IsValidPosition(position))
                throw new ArgumentException(nameof(position));
            if (kind == null)
                throw new ArgumentNullException(nameof(kind));
            if (namespacePrefix == null)
                throw new ArgumentNullException(nameof(namespacePrefix));

            switch (kind)
            {
                case IdentifierKind.Member member:
                    return GetCompletionsForKind(file, compilation, position, member.Item2,
                                                 ResolveNamespaceAlias(file, compilation, position, member.Item1));
                case IdentifierKind.Keyword keyword:
                    return new[] { new CompletionItem { Label = keyword.Item, Kind = CompletionItemKind.Keyword } };
            }
            switch (kind.Tag)
            {
                case IdentifierKind.Tags.Type:
                    var namespaces = namespacePrefix == ""
                        ? GetOpenNamespaces(file, compilation, position)
                        : new[] { namespacePrefix };
                    return
                        (namespacePrefix == "" ? typeKeywords : Array.Empty<CompletionItem>())
                        .Concat(GetTypeCompletions(file, compilation, namespaces))
                        .Concat(GetGlobalNamespaceCompletions(compilation, namespacePrefix))
                        .Concat(GetNamespaceAliasCompletions(file, compilation, position));
                case IdentifierKind.Tags.Namespace:
                    return
                        GetGlobalNamespaceCompletions(compilation, namespacePrefix)
                        .Concat(GetNamespaceAliasCompletions(file, compilation, position));
                case IdentifierKind.Tags.Characteristic:
                    return characteristicKeywords;
            }
            return Array.Empty<CompletionItem>();
        }

        /// <summary>
        /// Returns completions for local variables at the given position. Returns an empty enumerator if the position
        /// is invalid.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the position is invalid.</exception>
        private static IEnumerable<CompletionItem> GetLocalCompletions(
            FileContentManager file, CompilationUnit compilation, Position position)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));
            if (!Utils.IsValidPosition(position))
                throw new ArgumentException(nameof(position));
            return
                compilation
                .TryGetLocalDeclarations(file, position, out _)
                .Variables
                .Select(variable => new CompletionItem()
                {
                    Label = variable.VariableName.Value,
                    Kind = CompletionItemKind.Variable
                });
        }

        /// <summary>
        /// Returns completions for all callables in any of the given namespaces.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        private static IEnumerable<CompletionItem> GetCallableCompletions(
            FileContentManager file, CompilationUnit compilation, IEnumerable<string> namespaces)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));
            if (namespaces == null)
                throw new ArgumentNullException(nameof(namespaces));
            return
                compilation.GlobalSymbols.DefinedCallables()
                .Concat(compilation.GlobalSymbols.ImportedCallables())
                .Where(callable => namespaces.Contains(callable.QualifiedName.Namespace.Value))
                .Select(callable => new CompletionItem()
                {
                    Label = callable.QualifiedName.Name.Value,
                    Kind =
                        callable.Kind.IsTypeConstructor ? CompletionItemKind.Constructor : CompletionItemKind.Function,
                    Detail = callable.QualifiedName.Namespace.Value,
                    Data = new CompletionItemData(
                        textDocument: new TextDocumentIdentifier { Uri = file.Uri },
                        qualifiedName: callable.QualifiedName,
                        sourceFile: callable.SourceFile.Value)
                });
        }

        /// <summary>
        /// Returns completions for all types in any of the given namespaces.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        private static IEnumerable<CompletionItem> GetTypeCompletions(
            FileContentManager file, CompilationUnit compilation, IEnumerable<string> namespaces)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));
            if (namespaces == null)
                throw new ArgumentNullException(nameof(namespaces));
            return
                compilation.GlobalSymbols.DefinedTypes()
                .Concat(compilation.GlobalSymbols.ImportedTypes())
                .Where(type => namespaces.Contains(type.QualifiedName.Namespace.Value))
                .Select(type => new CompletionItem()
                {
                    Label = type.QualifiedName.Name.Value,
                    Kind = CompletionItemKind.Struct,
                    Detail = type.QualifiedName.Namespace.Value,
                    Data = new CompletionItemData(
                        textDocument: new TextDocumentIdentifier { Uri = file.Uri },
                        qualifiedName: type.QualifiedName,
                        sourceFile: type.SourceFile.Value)
                });
        }

        /// <summary>
        /// Returns completions for all global namespaces with the given prefix. The completion names contain only the
        /// word after the prefix and before the next dot.
        /// <para/>
        /// Note: a dot will be added after the given prefix if it is not the empty string, and doesn't already end with
        /// a dot.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        private static IEnumerable<CompletionItem> GetGlobalNamespaceCompletions(
            CompilationUnit compilation, string prefix = "")
        {
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));
            if (prefix == null)
                throw new ArgumentNullException(nameof(prefix));

            if (prefix.Length != 0 && !prefix.EndsWith("."))
                prefix += ".";
            return
                compilation.GlobalSymbols.NamespaceNames()
                .Where(name => name.Value.StartsWith(prefix))
                .Select(name => String.Concat(name.Value.Substring(prefix.Length).TakeWhile(c => c != '.')))
                .Distinct()
                .Select(name => new CompletionItem()
                {
                    Label = name,
                    Kind = CompletionItemKind.Module,
                    Detail = prefix.TrimEnd('.')
                });
        }

        /// <summary>
        /// Returns completions for namespace aliases that are visible at the given position in the file. Returns an
        /// empty enumerator if the position is invalid.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the position is invalid.</exception>
        private static IEnumerable<CompletionItem> GetNamespaceAliasCompletions(
            FileContentManager file, CompilationUnit compilation, Position position)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));
            if (!Utils.IsValidPosition(position))
                throw new ArgumentException(nameof(position));

            var @namespace = file.TryGetNamespaceAt(position);
            return
                @namespace == null
                ? Array.Empty<CompletionItem>()
                : compilation
                    .GetOpenDirectives(NonNullable<string>.New(@namespace))[file.FileName]
                    .Where(open => open.Item2 != null)
                    .Select(open => new CompletionItem()
                    {
                        Label = open.Item2,
                        Kind = CompletionItemKind.Module,
                        Detail = open.Item1.Value
                    });
        }

        /// <summary>
        /// Returns documentation for the callable (if kind is Function or Constructor) or type (if kind is Struct) in
        /// the compilation unit with the given qualified name. Returns null if no documentation is available or the
        /// completion item data is missing properties.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        private static string TryGetDocumentation(
            CompilationUnit compilation, CompletionItemData data, CompletionItemKind kind, bool useMarkdown)
        {
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.QualifiedName == null || data.SourceFile == null)
                return null;

            switch (kind)
            {
                case CompletionItemKind.Function:
                case CompletionItemKind.Constructor:
                    var callable = compilation.GlobalSymbols.TryGetCallable(
                        data.QualifiedName, data.QualifiedName.Namespace, NonNullable<string>.New(data.SourceFile));
                    if (callable.IsNull)
                        return null;
                    var signature = callable.Item.PrintSignature();
                    var documentation = callable.Item.Documentation.PrintSummary(useMarkdown);
                    return signature.Trim() + "\n\n" + documentation.Trim();
                case CompletionItemKind.Struct:
                    var type =
                        compilation.GlobalSymbols.TryGetType(
                            data.QualifiedName, data.QualifiedName.Namespace, NonNullable<string>.New(data.SourceFile))
                        .Item;
                    return type?.Documentation.PrintSummary(useMarkdown).Trim();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Returns true if a new symbol is being declared at the given position. Returns false if the position is
        /// invalid.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the position is invalid.</exception>
        private static bool IsDeclaringNewSymbol(FileContentManager file, Position position)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (!Utils.IsValidPosition(position))
                throw new ArgumentException(nameof(position));

            var fragment = file.TryGetFragmentAt(position, includeEnd: true);
            if (fragment == null)
                return false;

            // If the symbol is invalid, there is no range available, but assume the user is typing in the symbol now.
            var offset = fragment.GetRange().Start;
            bool PositionIsWithinSymbol(QsSymbol symbol) =>
                symbol.Symbol.IsInvalidSymbol ||
                position.IsWithinRange(DiagnosticTools.GetAbsoluteRange(offset, symbol.Range.Item), includeEnd: true);

            switch (fragment.Kind)
            {
                case QsFragmentKind.TypeDefinition td: return PositionIsWithinSymbol(td.Item1);
                case QsFragmentKind.FunctionDeclaration fd: return PositionIsWithinSymbol(fd.Item1);
                case QsFragmentKind.OperationDeclaration od: return PositionIsWithinSymbol(od.Item1);
                case QsFragmentKind.BorrowingBlockIntro bbi: return PositionIsWithinSymbol(bbi.Item1);
                case QsFragmentKind.UsingBlockIntro ubi: return PositionIsWithinSymbol(ubi.Item1);
                case QsFragmentKind.ForLoopIntro fli: return PositionIsWithinSymbol(fli.Item1);
                case QsFragmentKind.MutableBinding mb: return PositionIsWithinSymbol(mb.Item1);
                case QsFragmentKind.ImmutableBinding ib: return PositionIsWithinSymbol(ib.Item1);
                case QsFragmentKind.OpenDirective od: return od.Item2.IsValue && PositionIsWithinSymbol(od.Item2.Item);
                case QsFragmentKind.NamespaceDeclaration nd: return PositionIsWithinSymbol(nd.Item);
                default: return false;
            }
        }

        /// <summary>
        /// Returns the names of all namespaces that have been opened without an alias and are visible from the given
        /// position in the file. Returns an empty enumerator if the position is invalid.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the position is invalid.</exception>
        private static IEnumerable<string> GetOpenNamespaces(
            FileContentManager file, CompilationUnit compilation, Position position)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));
            if (!Utils.IsValidPosition(position))
                throw new ArgumentException(nameof(position));

            var @namespace = file.TryGetNamespaceAt(position);
            return
                @namespace == null
                ? Array.Empty<string>()
                : compilation
                    .GetOpenDirectives(NonNullable<string>.New(@namespace))[file.FileName]
                    .Where(open => open.Item2 == null)  // Only include open directives without an alias.
                    .Select(open => open.Item1.Value)
                    .Concat(new[] { @namespace });
        }

        /// <summary>
        /// Returns the namespace path for the qualified symbol at the given position, or null if there is no qualified
        /// symbol.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the position is invalid.</exception>
        private static string GetSymbolNamespacePath(FileContentManager file, Position position)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (!Utils.IsValidPosition(position))
                throw new ArgumentException(nameof(position));

            var fragment = file.TryGetFragmentAt(position, includeEnd: true);
            if (fragment == null)
                return null;
            var startAt = GetTextIndexFromPosition(fragment, position);
            var match = Utils.QualifiedSymbolRTL.Match(fragment.Text, startAt);
            if (match.Success && match.Index + match.Length == startAt && match.Value.LastIndexOf('.') != -1)
                return match.Value.Substring(0, match.Value.LastIndexOf('.'));
            else
                return null;
        }

        /// <summary>
        /// Returns the index in the fragment text corresponding to the given absolute position.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the position is outside the fragment range.</exception>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        private static int GetTextIndexFromPosition(CodeFragment fragment, Position position)
        {
            if (fragment == null)
                throw new ArgumentNullException(nameof(fragment));
            if (position == null)
                throw new ArgumentNullException(nameof(position));

            var relativeLine = position.Line - fragment.GetRange().Start.Line;
            var lines = Utils.SplitLines(fragment.Text).DefaultIfEmpty("");
            var relativeCharacter =
                relativeLine == 0 ? position.Character - fragment.GetRange().Start.Character : position.Character;
            if (relativeLine < 0 ||
                relativeLine >= lines.Count() ||
                relativeCharacter < 0 ||
                relativeCharacter > lines.ElementAt(relativeLine).Length)
            {
                throw new ArgumentException("Position is outside the fragment range", nameof(position));
            }
            return lines.Take(relativeLine).Sum(line => line.Length) + relativeCharacter;
        }

        /// <summary>
        /// Resolves the namespace alias and returns its full namespace name. If the alias couldn't be resolved, returns
        /// the alias unchanged.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the position is invalid.</exception>
        private static string ResolveNamespaceAlias(
            FileContentManager file, CompilationUnit compilation, Position position, string alias)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));
            if (!Utils.IsValidPosition(position))
                throw new ArgumentException(nameof(position));
            if (alias == null)
                throw new ArgumentNullException(nameof(alias));

            var nsName = file.TryGetNamespaceAt(position);
            if (nsName == null)
                return alias;
            return compilation.GlobalSymbols.TryResolveNamespaceAlias(
                NonNullable<string>.New(alias), NonNullable<string>.New(nsName), file.FileName) ?? alias;
        }

        /// <summary>
        /// Returns the token index at, or the closest token index before, the given position. Returns null if there is
        /// no token at or before the given position.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the position is invalid.</exception>
        private static CodeFragment.TokenIndex GetTokenAtOrBefore(FileContentManager file, Position position)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (!Utils.IsValidPosition(position))
                throw new ArgumentException(nameof(position));

            var line = position.Line;
            var tokens = file.GetTokenizedLine(line);

            // If the current line is empty, find the last non-empty line before it.
            while (tokens.IsEmpty && line > 0)
                tokens = file.GetTokenizedLine(--line);

            var index = tokens.TakeWhile(ContextBuilder.TokensUpTo(position)).Count() - 1;
            if (index == -1)
                return null;
            return new CodeFragment.TokenIndex(file, line, index);
        }

        /// <summary>
        /// Returns the position of the delimiting character that follows the given code fragment.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the code fragment has a missing delimiter.</exception>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        private static Position GetDelimiterPosition(FileContentManager file, CodeFragment fragment)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (fragment == null)
                throw new ArgumentNullException(nameof(fragment));
            if (fragment.FollowedBy == CodeFragment.MissingDelimiter)
                throw new ArgumentException("Code fragment has a missing delimiter", nameof(fragment));

            var end = fragment.GetRange().End;
            var position = file.FragmentEnd(ref end);
            return new Position(position.Line, position.Character - 1);
        }

        /// <summary>
        /// Returns a substring of the fragment text before the given position.
        /// <para/>
        /// If the fragment is null or the position is after the fragment's delimiter, returns the empty string. If the
        /// position is after the end of the fragment text but before the delimiter, the entire text is returned with a
        /// space character appended to it.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when file or position is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the position is invalid.</exception>
        private static string GetFragmentTextBeforePosition(
            FileContentManager file, CodeFragment fragment, Position position)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (!Utils.IsValidPosition(position))
                throw new ArgumentException(nameof(position));

            if (fragment == null
                || fragment.FollowedBy != CodeFragment.MissingDelimiter
                && GetDelimiterPosition(file, fragment).IsSmallerThan(position))
            {
                return "";
            }
            return fragment.GetRange().End.IsSmallerThan(position)
                ? fragment.Text + " "
                : fragment.Text.Substring(0, GetTextIndexFromPosition(fragment, position));
        }
    }
}
