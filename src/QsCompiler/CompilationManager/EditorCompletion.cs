﻿using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.TextProcessing;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    /// <summary>
    /// Data associated with a completion item that is used for resolving additional information.
    /// </summary>
    [DataContract]
    public class CompletionItemData
    {
        [DataMember(Name = "namespace")]
        private string @namespace;

        [DataMember(Name = "name")]
        private string name;

        /// <summary>
        /// The text document that the original completion request was made from.
        /// </summary>
        [DataMember(Name = "textDocument")]
        public TextDocumentIdentifier TextDocument { get; set; }

        /// <summary>
        /// The qualified name of the completion item.
        /// </summary>
        public QsQualifiedName QualifiedName
        {
            get =>
                @namespace == null || name == null
                ? null
                : new QsQualifiedName(NonNullable<string>.New(@namespace), NonNullable<string>.New(name));
            set
            {
                @namespace = value.Namespace.Value;
                name = value.Name.Value;
            }
        }

        /// <summary>
        /// The source file the completion item is declared in.
        /// </summary>
        [DataMember(Name = "sourceFile")]
        public string SourceFile { get; set; }
    }

    /// <summary>
    /// Provides code completion for the language server.
    /// </summary>
    internal static class EditorCompletion
    {
        /// <summary>
        /// Completion items for built-in type keywords.
        /// </summary>
        private static readonly IEnumerable<CompletionItem> typeKeywords =
            new[]
            {
                // TODO: Move this list to the Keywords module.
                Keywords.qsUnit,
                Keywords.qsInt,
                Keywords.qsBigInt,
                Keywords.qsDouble,
                Keywords.qsBool,
                Keywords.qsQubit,
                Keywords.qsResult,
                Keywords.qsPauli,
                Keywords.qsRange,
                Keywords.qsString
            }
            .Select(keyword => new CompletionItem { Label = keyword.id, Kind = CompletionItemKind.Keyword });

        /// <summary>
        /// Completion items for built-in characteristic keywords.
        /// </summary>
        private static readonly IEnumerable<CompletionItem> characteristicKeywords =
            new[]
            {
                // TODO: Move this list to the Keywords module.
                Keywords.qsAdjSet,
                Keywords.qsCtlSet
            }
            .Select(keyword => new CompletionItem { Label = keyword.id, Kind = CompletionItemKind.Keyword });

        /// <summary>
        /// Returns a list of suggested completion items for the given position.
        /// <para/>
        /// Returns null if any argument is null. Returns an empty enumerator if the position is invalid.
        /// </summary>
        public static CompletionList Completions(
            this FileContentManager file, CompilationUnit compilation, Position position)
        {
            if (file == null || compilation == null || position == null)
                return null;

            var nsPath = GetSymbolNamespacePath(file, position);
            var resolvedNsPath = nsPath == null ? null : ResolveNamespaceAlias(file, compilation, position, nsPath);

            // If the character at the position is a dot but no valid namespace path precedes it (for example, in a
            // decimal number), then no completions are valid here.
            if (nsPath == null &&
                position.Character > 0 &&
                file.GetLine(position.Line).Text[position.Character - 1] == '.')
            {
                return new CompletionList() { IsIncomplete = false, Items = Array.Empty<CompletionItem>() };
            }

            // TODO: Support context-aware completions for additional contexts.
            IEnumerable<CompletionItem> completions;
            if (IsInNamespaceContext(file, position))
            {
                var fragment = GetLastTokenBefore(file, position)?.GetFragment();
                QsCompilerError.Verify(fragment != null, "Namespace context should have at least one token");
                var textUpToPosition =
                    fragment.GetRange().End.IsSmallerThan(position)
                    ? fragment.Text
                    : fragment.Text.Substring(0, GetTextIndexFromPosition(fragment, position));
                completions =
                    CompletionParsing.GetExpectedIdentifiers(textUpToPosition)
                    .SelectMany(kind => GetCompletionsForKind(file, compilation, position, resolvedNsPath, kind));
            }
            else if (resolvedNsPath != null)
            {
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
                    .Select(keyword => new CompletionItem() { Label = keyword, Kind = CompletionItemKind.Keyword })
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
            return new CompletionList() { IsIncomplete = false, Items = completions.ToArray() };
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
                item.Documentation = new MarkupContent() { Kind = format, Value = documentation };
            return item;
        }

        /// <summary>
        /// Returns true if the given position in the file is at the top-level of a namespace.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        private static bool IsInNamespaceContext(FileContentManager file, Position position)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (position == null)
                throw new ArgumentNullException(nameof(position));
            var token = GetLastTokenBefore(file, position);
            return token?.GetNonEmptyParent()?.GetFragment().Kind.IsNamespaceDeclaration ?? false;
        }

        /// <summary>
        /// Returns completion items that match the given identifier kind.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument except namespacePrefix is null.</exception>
        private static IEnumerable<CompletionItem> GetCompletionsForKind(
            FileContentManager file,
            CompilationUnit compilation,
            Position position,
            string namespacePrefix,
            CompletionParsing.IdentifierKind kind)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));
            if (position == null)
                throw new ArgumentNullException(nameof(position));
            if (kind == null)
                throw new ArgumentNullException(nameof(kind));

            if (kind is CompletionParsing.IdentifierKind.Keyword keyword)
                return new[] { new CompletionItem { Label = keyword.Item, Kind = CompletionItemKind.Keyword } };
            if (kind.IsType)
                return
                    GetTypeCompletions(file, compilation, GetOpenNamespaces(file, compilation, position))
                    .Concat(typeKeywords);
            if (kind.IsNamespace)
                return
                    GetGlobalNamespaceCompletions(compilation, namespacePrefix ?? "")
                    .Concat(GetNamespaceAliasCompletions(file, compilation, position));
            if (kind.IsCharacteristic)
                return characteristicKeywords;
            return Array.Empty<CompletionItem>();
        }

        /// <summary>
        /// Returns completions for local variables at the given position. Returns an empty enumerator if the position
        /// is invalid.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        private static IEnumerable<CompletionItem> GetLocalCompletions(
            FileContentManager file, CompilationUnit compilation, Position position)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));
            if (position == null)
                throw new ArgumentNullException(nameof(position));
            return
                compilation
                .TryGetLocalDeclarations(file, position, out var _)
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
                    Data = new CompletionItemData()
                    {
                        TextDocument = new TextDocumentIdentifier() { Uri = file.Uri },
                        QualifiedName = callable.QualifiedName,
                        SourceFile = callable.SourceFile.Value
                    }
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
                    Data = new CompletionItemData()
                    {
                        TextDocument = new TextDocumentIdentifier() { Uri = file.Uri },
                        QualifiedName = type.QualifiedName,
                        SourceFile = type.SourceFile.Value
                    }
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
        private static IEnumerable<CompletionItem> GetNamespaceAliasCompletions(
            FileContentManager file, CompilationUnit compilation, Position position)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));
            if (position == null)
                throw new ArgumentNullException(nameof(position));

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
        private static bool IsDeclaringNewSymbol(FileContentManager file, Position position)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (position == null)
                throw new ArgumentNullException(nameof(position));

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
                case QsFragmentKind.TypeDefinition td:
                    return PositionIsWithinSymbol(td.Item1);
                case QsFragmentKind.FunctionDeclaration fd:
                    return PositionIsWithinSymbol(fd.Item1);
                case QsFragmentKind.OperationDeclaration od:
                    return PositionIsWithinSymbol(od.Item1);
                case QsFragmentKind.BorrowingBlockIntro bbi:
                    return PositionIsWithinSymbol(bbi.Item1);
                case QsFragmentKind.UsingBlockIntro ubi:
                    return PositionIsWithinSymbol(ubi.Item1);
                case QsFragmentKind.ForLoopIntro fli:
                    return PositionIsWithinSymbol(fli.Item1);
                case QsFragmentKind.MutableBinding mb:
                    return PositionIsWithinSymbol(mb.Item1);
                case QsFragmentKind.ImmutableBinding ib:
                    return PositionIsWithinSymbol(ib.Item1);
                case QsFragmentKind.OpenDirective od:
                    return od.Item2.IsValue && PositionIsWithinSymbol(od.Item2.Item);
                case QsFragmentKind.NamespaceDeclaration nd:
                    return PositionIsWithinSymbol(nd.Item);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns the names of all namespaces that have been opened without an alias and are visible from the given
        /// position in the file. Returns an empty enumerator if the position is invalid.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        private static IEnumerable<string> GetOpenNamespaces(
            FileContentManager file, CompilationUnit compilation, Position position)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));
            if (position == null)
                throw new ArgumentNullException(nameof(position));

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
        private static string GetSymbolNamespacePath(FileContentManager file, Position position)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (position == null)
                throw new ArgumentNullException(nameof(position));

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
            var lines = Utils.SplitLines(fragment.Text);
            var relativeChar =
                relativeLine == 0 ? position.Character - fragment.GetRange().Start.Character : position.Character;
            if (relativeLine < 0 ||
                relativeLine >= lines.Length ||
                relativeChar < 0 ||
                relativeChar > lines[relativeLine].Length)
            {
                throw new ArgumentException("Position is outside the fragment range", nameof(position));
            }
            return lines.Take(relativeLine).Sum(line => line.Length) + relativeChar;
        }

        /// <summary>
        /// Resolves the namespace alias and returns its full namespace name. If the alias couldn't be resolved, returns
        /// the alias unchanged.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        private static string ResolveNamespaceAlias(
            FileContentManager file, CompilationUnit compilation, Position position, string alias)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));
            if (position == null)
                throw new ArgumentNullException(nameof(position));
            if (alias == null)
                throw new ArgumentNullException(nameof(alias));

            var nsName = file.TryGetNamespaceAt(position);
            if (nsName == null)
                return alias;
            return compilation.GlobalSymbols.TryResolveNamespaceAlias(
                NonNullable<string>.New(alias), NonNullable<string>.New(nsName), file.FileName) ?? alias;
        }

        /// <summary>
        /// Returns the index of the last token before the given position in the file. Returns null if there are no
        /// tokens before the given position.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        private static CodeFragment.TokenIndex GetLastTokenBefore(FileContentManager file, Position position)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (position == null)
                throw new ArgumentNullException(nameof(position));

            var lineNumber = position.Line;
            var tokens = file.GetTokenizedLine(lineNumber);

            // If the current line is empty, find the last non-empty line before it.
            while (tokens.IsEmpty && lineNumber > 0)
                tokens = file.GetTokenizedLine(--lineNumber);

            var index = tokens.TakeWhile(ContextBuilder.TokensUpTo(position)).Count() - 1;
            return index == -1 ? null : new CodeFragment.TokenIndex(file, lineNumber, index);
        }
    }
}
