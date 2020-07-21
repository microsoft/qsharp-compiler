// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.TextProcessing;
using Microsoft.Quantum.QsCompiler.TextProcessing.CodeCompletion;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using static Microsoft.Quantum.QsCompiler.SyntaxGenerator;
using static Microsoft.Quantum.QsCompiler.TextProcessing.CodeCompletion.FragmentParsing;
using Lsp = Microsoft.VisualStudio.LanguageServer.Protocol;

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
            get => this.@namespace == null || this.name == null
                ? null
                : new QsQualifiedName(NonNullable<string>.New(this.@namespace), NonNullable<string>.New(this.name));
        }

        /// <summary>
        /// The source file the completion item is declared in.
        /// </summary>
        [DataMember(Name = "sourceFile")]
        public string SourceFile { get; }

        public CompletionItemData(
            TextDocumentIdentifier textDocument = null, QsQualifiedName qualifiedName = null, string sourceFile = null)
        {
            this.TextDocument = textDocument;
            this.@namespace = qualifiedName?.Namespace.Value;
            this.name = qualifiedName?.Name.Value;
            this.SourceFile = sourceFile;
        }
    }

    /// <summary>
    /// Provides code completion for the language server.
    /// </summary>
    internal static class CodeCompletion
    {
        /// <summary>
        /// Returns a list of suggested completion items for the given position.
        /// <para/>
        /// Returns null if any argument is null or the position is invalid.
        /// Returns an empty completion list if the given position is within a comment.
        /// </summary>
        public static CompletionList Completions(
            this FileContentManager file, CompilationUnit compilation, Lsp.Position position)
        {
            if (file == null || compilation == null || position == null || !Utils.IsValidPosition(position))
            {
                return null;
            }
            if (file.GetLine(position.Line).WithoutEnding.Length < position.Character)
            {
                return Enumerable.Empty<CompletionItem>().ToCompletionList(false);
            }

            var (scope, previous) = GetCompletionEnvironment(file, position, out var fragment);
            if (scope == null)
            {
                return GetFallbackCompletions(file, compilation, position).ToCompletionList(false);
            }

            var result = GetCompletionKinds(
                scope,
                previous != null ? QsNullable<QsFragmentKind>.NewValue(previous) : QsNullable<QsFragmentKind>.Null,
                GetFragmentTextBeforePosition(file, fragment, position));
            if (result is CompletionResult.Success success)
            {
                return success.Item
                    .SelectMany(kind => GetCompletionsForKind(file, compilation, position, kind))
                    .ToCompletionList(false);
            }
            else
            {
                return GetFallbackCompletions(file, compilation, position).ToCompletionList(false);
            }
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
            {
                return null;
            }
            var documentation = TryGetDocumentation(compilation, data, item.Kind, format == MarkupKind.Markdown);
            if (documentation != null)
            {
                item.Documentation = new MarkupContent { Kind = format, Value = documentation };
            }
            return item;
        }

        /// <summary>
        /// Returns the completion environment at the given position in the file or null if the environment cannot be
        /// determined. Stores the code fragment found at or before the given position into an out parameter.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the position is invalid.</exception>
        private static (CompletionScope, QsFragmentKind) GetCompletionEnvironment(
            FileContentManager file, Lsp.Position position, out CodeFragment fragment)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (!Utils.IsValidPosition(position))
            {
                throw new ArgumentException(nameof(position));
            }
            if (!Utils.IsValidPosition(position.ToQSharp(), file))
            {
                // FileContentManager.IndentationAt will fail if the position is not within the file.
                fragment = null;
                return (null, null);
            }

            var token = GetTokenAtOrBefore(file, position);
            if (token == null)
            {
                fragment = null;
                return (null, null);
            }

            fragment = token.GetFragment();
            var relativeIndentation = fragment.Indentation - file.IndentationAt(position.ToQSharp());
            QsCompilerError.Verify(Math.Abs(relativeIndentation) <= 1);
            var parents =
                new[] { token }.Concat(token.GetNonEmptyParents())
                .Skip(relativeIndentation + 1)
                .Select(t => t.GetFragment());

            CompletionScope scope = null;
            if (!parents.Any())
            {
                scope = CompletionScope.TopLevel;
            }
            else if (parents.Any() && parents.First().Kind.IsNamespaceDeclaration)
            {
                scope = CompletionScope.NamespaceTopLevel;
            }
            else if (parents.Where(parent => parent.Kind.IsFunctionDeclaration).Any())
            {
                scope = CompletionScope.Function;
            }
            else if (parents.Any() && parents.First().Kind.IsOperationDeclaration)
            {
                scope = CompletionScope.OperationTopLevel;
            }
            else if (parents.Where(parent => parent.Kind.IsOperationDeclaration).Any())
            {
                scope = CompletionScope.Operation;
            }

            QsFragmentKind previous = null;
            if (relativeIndentation == 0 && IsPositionAfterDelimiter(file, fragment, position))
            {
                previous = fragment.Kind;
            }
            else if (relativeIndentation == 0)
            {
                previous = token.PreviousOnScope()?.GetFragment().Kind;
            }
            else if (relativeIndentation == 1)
            {
                previous = token.GetNonEmptyParent()?.GetFragment().Kind;
            }

            return (scope, previous);
        }

        /// <summary>
        /// Returns completion items that match the given kind.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the position is invalid.</exception>
        private static IEnumerable<CompletionItem> GetCompletionsForKind(
            FileContentManager file,
            CompilationUnit compilation,
            Lsp.Position position,
            CompletionKind kind,
            string namespacePrefix = "")
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }
            if (!Utils.IsValidPosition(position))
            {
                throw new ArgumentException(nameof(position));
            }
            if (kind == null)
            {
                throw new ArgumentNullException(nameof(kind));
            }
            if (namespacePrefix == null)
            {
                throw new ArgumentNullException(nameof(namespacePrefix));
            }

            switch (kind)
            {
                case CompletionKind.Member member:
                    return GetCompletionsForKind(
                        file,
                        compilation,
                        position,
                        member.Item2,
                        ResolveNamespaceAlias(file, compilation, position, member.Item1));
                case CompletionKind.Keyword keyword:
                    return new[] { new CompletionItem { Label = keyword.Item, Kind = CompletionItemKind.Keyword } };
            }
            var currentNamespace = file.TryGetNamespaceAt(position.ToQSharp());
            var openNamespaces =
                namespacePrefix == "" ? GetOpenNamespaces(file, compilation, position) : new[] { namespacePrefix };
            switch (kind.Tag)
            {
                case CompletionKind.Tags.UserDefinedType:
                    return
                        GetTypeCompletions(file, compilation, currentNamespace, openNamespaces)
                        .Concat(GetGlobalNamespaceCompletions(compilation, namespacePrefix))
                        .Concat(GetNamespaceAliasCompletions(file, compilation, position, namespacePrefix));
                case CompletionKind.Tags.NamedItem:
                    return GetNamedItemCompletions(compilation);
                case CompletionKind.Tags.Namespace:
                    return
                        GetGlobalNamespaceCompletions(compilation, namespacePrefix)
                        .Concat(GetNamespaceAliasCompletions(file, compilation, position, namespacePrefix));
                case CompletionKind.Tags.Variable:
                    return GetLocalCompletions(file, compilation, position);
                case CompletionKind.Tags.MutableVariable:
                    return GetLocalCompletions(file, compilation, position, mutableOnly: true);
                case CompletionKind.Tags.Callable:
                    return
                        GetCallableCompletions(file, compilation, currentNamespace, openNamespaces)
                        .Concat(GetGlobalNamespaceCompletions(compilation, namespacePrefix))
                        .Concat(GetNamespaceAliasCompletions(file, compilation, position, namespacePrefix));
            }
            return Enumerable.Empty<CompletionItem>();
        }

        /// <summary>
        /// Returns completions meant to be used as a fallback in cases where the completion parser can't parse a code
        /// fragment. The fallback includes all possible completions regardless of context, except when the position is
        /// part of a qualified symbol, in which case only completions for symbols matching the namespace prefix will be
        /// included.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        private static IEnumerable<CompletionItem> GetFallbackCompletions(
            FileContentManager file, CompilationUnit compilation, Lsp.Position position)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }
            if (position == null)
            {
                throw new ArgumentNullException(nameof(position));
            }

            // If the character at the position is a dot but no valid namespace path precedes it (for example, in a
            // decimal number), then no completions are valid here.
            var nsPath = GetSymbolNamespacePath(file, position);
            if (nsPath == null &&
                position.Character > 0 &&
                position.Character <= file.GetLine(position.Line).Text.Length &&
                file.GetLine(position.Line).Text[position.Character - 1] == '.')
            {
                return Array.Empty<CompletionItem>();
            }

            var currentNamespace = file.TryGetNamespaceAt(position.ToQSharp());
            if (nsPath != null)
            {
                var resolvedNsPath = ResolveNamespaceAlias(file, compilation, position, nsPath);
                return
                    GetCallableCompletions(file, compilation, currentNamespace, new[] { resolvedNsPath })
                    .Concat(GetTypeCompletions(file, compilation, currentNamespace, new[] { resolvedNsPath }))
                    .Concat(GetGlobalNamespaceCompletions(compilation, resolvedNsPath))
                    .Concat(GetNamespaceAliasCompletions(file, compilation, position, nsPath));  // unresolved NS path
            }
            var openNamespaces = GetOpenNamespaces(file, compilation, position);
            return
                Keywords.ReservedKeywords
                .Select(keyword => new CompletionItem { Label = keyword, Kind = CompletionItemKind.Keyword })
                .Concat(GetLocalCompletions(file, compilation, position))
                .Concat(GetCallableCompletions(file, compilation, currentNamespace, openNamespaces))
                .Concat(GetTypeCompletions(file, compilation, currentNamespace, openNamespaces))
                .Concat(GetGlobalNamespaceCompletions(compilation))
                .Concat(GetNamespaceAliasCompletions(file, compilation, position));
        }

        /// <summary>
        /// Returns completions for local variables at the given position. If <paramref name="mutableOnly"/> is true,
        /// shows only completions for mutable local variables. Returns an empty enumerator if the position is invalid.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the position is invalid.</exception>
        private static IEnumerable<CompletionItem> GetLocalCompletions(
            FileContentManager file, CompilationUnit compilation, Lsp.Position position, bool mutableOnly = false)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }
            if (!Utils.IsValidPosition(position))
            {
                throw new ArgumentException(nameof(position));
            }
            return
                compilation
                .TryGetLocalDeclarations(file, position.ToQSharp(), out _, includeDeclaredAtPosition: false)
                .Variables
                .Where(variable => !mutableOnly || variable.InferredInformation.IsMutable)
                .Select(variable => new CompletionItem()
                {
                    Label = variable.VariableName.Value,
                    Kind = CompletionItemKind.Variable
                });
        }

        /// <summary>
        /// Returns completions for all accessible callables given the current namespace and the list of open
        /// namespaces, or an empty enumerable if symbols haven't been resolved yet.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any argument except <paramref name="currentNamespace"/> is null.
        /// </exception>
        private static IEnumerable<CompletionItem> GetCallableCompletions(
            FileContentManager file,
            CompilationUnit compilation,
            string currentNamespace,
            IEnumerable<string> openNamespaces)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }
            if (openNamespaces == null)
            {
                throw new ArgumentNullException(nameof(openNamespaces));
            }

            if (!compilation.GlobalSymbols.ContainsResolutions)
            {
                return Array.Empty<CompletionItem>();
            }
            return
                compilation.GlobalSymbols.AccessibleCallables()
                .Where(callable =>
                    IsAccessibleAsUnqualifiedName(callable.QualifiedName, currentNamespace, openNamespaces))
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
        /// Returns completions for all accessible types given the current namespace and the list of open namespaces, or
        /// an empty enumerable if symbols haven't been resolved yet.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any argument except <paramref name="currentNamespace"/> is null.
        /// </exception>
        private static IEnumerable<CompletionItem> GetTypeCompletions(
            FileContentManager file,
            CompilationUnit compilation,
            string currentNamespace,
            IEnumerable<string> openNamespaces)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }
            if (openNamespaces == null)
            {
                throw new ArgumentNullException(nameof(openNamespaces));
            }

            if (!compilation.GlobalSymbols.ContainsResolutions)
            {
                return Array.Empty<CompletionItem>();
            }
            return
                compilation.GlobalSymbols.AccessibleTypes()
                .Where(type => IsAccessibleAsUnqualifiedName(type.QualifiedName, currentNamespace, openNamespaces))
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
        /// Returns completions for all named items in any accessible type, or an empty enumerable if symbols haven't
        /// been resolved yet.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the argument is null.</exception>
        private static IEnumerable<CompletionItem> GetNamedItemCompletions(CompilationUnit compilation)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            if (!compilation.GlobalSymbols.ContainsResolutions)
            {
                return Array.Empty<CompletionItem>();
            }
            return compilation.GlobalSymbols.AccessibleTypes()
                .SelectMany(type => ExtractItems(type.TypeItems))
                .Where(item => item.IsNamed)
                .Select(item => new CompletionItem()
                {
                    Label = ((QsTypeItem.Named)item).Item.VariableName.Value,
                    Kind = CompletionItemKind.Field
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
            {
                throw new ArgumentNullException(nameof(compilation));
            }
            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            if (prefix.Length != 0 && !prefix.EndsWith("."))
            {
                prefix += ".";
            }
            return
                compilation.GlobalSymbols.NamespaceNames()
                .Where(name => name.Value.StartsWith(prefix))
                .Select(name => NextNamespacePart(name.Value, prefix.Length))
                .Distinct()
                .Select(name => new CompletionItem()
                {
                    Label = name,
                    Kind = CompletionItemKind.Module,
                    Detail = prefix + name
                });
        }

        /// <summary>
        /// Returns completions for namespace aliases with the given prefix that are accessible from the given position
        /// in the file.
        /// <para/>
        /// Note: a dot will be added after the given prefix if it is not the empty string, and doesn't already end with
        /// a dot.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the position is invalid.</exception>
        private static IEnumerable<CompletionItem> GetNamespaceAliasCompletions(
            FileContentManager file, CompilationUnit compilation, Lsp.Position position, string prefix = "")
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }
            if (!Utils.IsValidPosition(position))
            {
                throw new ArgumentException(nameof(position));
            }
            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            if (prefix.Length != 0 && !prefix.EndsWith("."))
            {
                prefix += ".";
            }
            var @namespace = file.TryGetNamespaceAt(position.ToQSharp());
            if (@namespace == null || !compilation.GlobalSymbols.NamespaceExists(NonNullable<string>.New(@namespace)))
            {
                return Array.Empty<CompletionItem>();
            }
            return compilation
                .GetOpenDirectives(NonNullable<string>.New(@namespace))[file.FileName]
                .Where(open => open.Item2 != null && open.Item2.StartsWith(prefix))
                .GroupBy(open => NextNamespacePart(open.Item2, prefix.Length))
                .Select(open => new CompletionItem()
                {
                    Label = open.Key,
                    Kind = CompletionItemKind.Module,
                    Detail = open.Count() == 1 && prefix + open.Key == open.Single().Item2
                        ? open.Single().Item1.Value
                        : prefix + open.Key
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
            {
                throw new ArgumentNullException(nameof(compilation));
            }
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (data.QualifiedName == null
                    || data.SourceFile == null
                    || !compilation.GlobalSymbols.NamespaceExists(data.QualifiedName.Namespace))
            {
                return null;
            }

            switch (kind)
            {
                case CompletionItemKind.Function:
                case CompletionItemKind.Constructor:
                    var result = compilation.GlobalSymbols.TryGetCallable(
                        data.QualifiedName, data.QualifiedName.Namespace, NonNullable<string>.New(data.SourceFile));
                    if (!(result is ResolutionResult<CallableDeclarationHeader>.Found callable))
                    {
                        return null;
                    }
                    var signature = callable.Item.PrintSignature();
                    var documentation = callable.Item.Documentation.PrintSummary(useMarkdown);
                    return signature.Trim() + "\n\n" + documentation.Trim();
                case CompletionItemKind.Struct:
                    var type =
                        compilation.GlobalSymbols.TryGetType(
                            data.QualifiedName, data.QualifiedName.Namespace, NonNullable<string>.New(data.SourceFile))
                        as ResolutionResult<TypeDeclarationHeader>.Found;
                    return type?.Item.Documentation.PrintSummary(useMarkdown).Trim();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Returns the names of all namespaces that have been opened without an alias and are accessible from the given
        /// position in the file. Returns an empty enumerator if the position is invalid.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the position is invalid.</exception>
        private static IEnumerable<string> GetOpenNamespaces(
            FileContentManager file, CompilationUnit compilation, Lsp.Position position)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }
            if (!Utils.IsValidPosition(position))
            {
                throw new ArgumentException(nameof(position));
            }

            var @namespace = file.TryGetNamespaceAt(position.ToQSharp());
            if (@namespace == null || !compilation.GlobalSymbols.NamespaceExists(NonNullable<string>.New(@namespace)))
            {
                return Array.Empty<string>();
            }
            return compilation
                .GetOpenDirectives(NonNullable<string>.New(@namespace))[file.FileName]
                .Where(open => open.Item2 == null) // Only include open directives without an alias.
                .Select(open => open.Item1.Value)
                .Concat(new[] { @namespace });
        }

        /// <summary>
        /// Returns the namespace path for the qualified symbol at the given position, or null if there is no qualified
        /// symbol.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the position is invalid.</exception>
        private static string GetSymbolNamespacePath(FileContentManager file, Lsp.Position position)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (!Utils.IsValidPosition(position))
            {
                throw new ArgumentException(nameof(position));
            }

            var fragment = file.TryGetFragmentAt(position.ToQSharp(), out _, includeEnd: true);
            if (fragment == null)
            {
                return null;
            }
            var startAt = GetTextIndexFromPosition(fragment, position);
            var match = Utils.QualifiedSymbolRTL.Match(fragment.Text, startAt);
            if (match.Success && match.Index + match.Length == startAt && match.Value.LastIndexOf('.') != -1)
            {
                return match.Value.Substring(0, match.Value.LastIndexOf('.'));
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the index in the fragment text corresponding to the given absolute position.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the position is outside the fragment range.</exception>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        private static int GetTextIndexFromPosition(CodeFragment fragment, Lsp.Position position)
        {
            if (fragment == null)
            {
                throw new ArgumentNullException(nameof(fragment));
            }
            if (position == null)
            {
                throw new ArgumentNullException(nameof(position));
            }

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
            FileContentManager file, CompilationUnit compilation, Lsp.Position position, string alias)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }
            if (!Utils.IsValidPosition(position))
            {
                throw new ArgumentException(nameof(position));
            }
            if (alias == null)
            {
                throw new ArgumentNullException(nameof(alias));
            }

            var nsName = file.TryGetNamespaceAt(position.ToQSharp());
            if (nsName == null || !compilation.GlobalSymbols.NamespaceExists(NonNullable<string>.New(nsName)))
            {
                return alias;
            }
            return compilation.GlobalSymbols.TryResolveNamespaceAlias(
                NonNullable<string>.New(alias), NonNullable<string>.New(nsName), file.FileName) ?? alias;
        }

        /// <summary>
        /// Returns the token index at, or the closest token index before, the given position. Returns null if there is
        /// no token at or before the given position.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the position is invalid.</exception>
        private static CodeFragment.TokenIndex GetTokenAtOrBefore(FileContentManager file, Lsp.Position position)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (!Utils.IsValidPosition(position))
            {
                throw new ArgumentException(nameof(position));
            }

            var line = position.Line;
            var tokens = file.GetTokenizedLine(line);

            // If the current line is empty, find the last non-empty line before it.
            while (tokens.IsEmpty && line > 0)
            {
                tokens = file.GetTokenizedLine(--line);
            }

            var index = tokens.TakeWhile(ContextBuilder.TokensUpTo(position.ToQSharp())).Count() - 1;
            if (index == -1)
            {
                return null;
            }
            return new CodeFragment.TokenIndex(file, line, index);
        }

        /// <summary>
        /// Returns the position of the delimiting character that follows the given code fragment.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the code fragment has a missing delimiter.</exception>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        private static Lsp.Position GetDelimiterPosition(FileContentManager file, CodeFragment fragment)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (fragment == null)
            {
                throw new ArgumentNullException(nameof(fragment));
            }
            if (fragment.FollowedBy == CodeFragment.MissingDelimiter)
            {
                throw new ArgumentException("Code fragment has a missing delimiter", nameof(fragment));
            }

            var end = fragment.GetRange().End;
            var position = file.FragmentEnd(ref end);
            return new Lsp.Position(position.Line, position.Column - 1);
        }

        /// <summary>
        /// Returns true if the fragment has a delimiting character and the given position occurs after it.
        /// </summary>
        private static bool IsPositionAfterDelimiter(
                FileContentManager file,
                CodeFragment fragment,
                Lsp.Position position) =>
            fragment.FollowedBy != CodeFragment.MissingDelimiter
            && GetDelimiterPosition(file, fragment).IsSmallerThan(position);

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
            FileContentManager file, CodeFragment fragment, Lsp.Position position)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (!Utils.IsValidPosition(position))
            {
                throw new ArgumentException(nameof(position));
            }

            if (fragment == null || IsPositionAfterDelimiter(file, fragment, position))
            {
                return "";
            }
            return fragment.GetRange().End.IsSmallerThan(position)
                ? fragment.Text + " "
                : fragment.Text.Substring(0, GetTextIndexFromPosition(fragment, position));
        }

        /// <summary>
        /// Returns the namespace part starting at the given starting position and ending at the next dot.
        /// </summary>
        private static string NextNamespacePart(string @namespace, int start) =>
            string.Concat(@namespace.Substring(start).TakeWhile(c => c != '.'));

        /// <summary>
        /// Converts an <see cref="IEnumerable{T}"/> of <see cref="CompletionItem"/>s to a
        /// <see cref="CompletionList"/>.
        /// </summary>
        private static CompletionList ToCompletionList(this IEnumerable<CompletionItem> items, bool isIncomplete) =>
            new CompletionList
            {
                IsIncomplete = isIncomplete,
                Items = items?.ToArray()
            };

        /// <summary>
        /// Returns true if the declaration with the given qualified name would be accessible if it was referenced using
        /// its unqualified name, given the current namespace and a list of open namespaces.
        /// <para/>
        /// Note: Names that start with "_" are treated as "private;" they are only accessible from the namespace in
        /// which they are declared.
        /// </summary>
        private static bool IsAccessibleAsUnqualifiedName(
            QsQualifiedName qualifiedName,
            string currentNamespace,
            IEnumerable<string> openNamespaces) =>
            openNamespaces.Contains(qualifiedName.Namespace.Value) &&
            (!qualifiedName.Name.Value.StartsWith("_") || qualifiedName.Namespace.Value == currentNamespace);
    }
}
