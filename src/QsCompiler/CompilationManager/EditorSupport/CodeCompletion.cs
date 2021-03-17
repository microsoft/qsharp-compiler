// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.TextProcessing;
using Microsoft.Quantum.QsCompiler.TextProcessing.CodeCompletion;
using Microsoft.Quantum.QsCompiler.Transformations;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using static Microsoft.Quantum.QsCompiler.SyntaxGenerator;
using static Microsoft.Quantum.QsCompiler.TextProcessing.CodeCompletion.FragmentParsing;
using Position = Microsoft.Quantum.QsCompiler.DataTypes.Position;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    /// <summary>
    /// Data associated with a completion item that is used for resolving additional information.
    /// </summary>
    [DataContract]
    public class CompletionItemData
    {
        [DataMember(Name = "namespace")]
        private readonly string? @namespace;

        [DataMember(Name = "name")]
        private readonly string? name;

        /// <summary>
        /// The text document that the original completion request was made from.
        /// </summary>
        [DataMember(Name = "textDocument")]
        public TextDocumentIdentifier? TextDocument { get; }

        /// <summary>
        /// The qualified name of the completion item.
        /// </summary>
        public QsQualifiedName? QualifiedName =>
            this.@namespace == null || this.name == null
                ? null
                : new QsQualifiedName(this.@namespace, this.name);

        /// <summary>
        /// The source file the completion item is declared in.
        /// </summary>
        [DataMember(Name = "sourceFile")]
        public string? SourceFile { get; }

        public CompletionItemData(
            TextDocumentIdentifier? textDocument = null, QsQualifiedName? qualifiedName = null, string? sourceFile = null)
        {
            this.TextDocument = textDocument;
            this.@namespace = qualifiedName?.Namespace;
            this.name = qualifiedName?.Name;
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
        /// <exception cref="FileContentException">The file content changed while processing.</exception>
        public static CompletionList? Completions(
            this FileContentManager file, CompilationUnit compilation, Position? position)
        {
            if (file == null || compilation == null || position == null)
            {
                return null;
            }
            if (file.GetLine(position.Line).WithoutEnding.Length < position.Column)
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
        public static CompletionItem? ResolveCompletion(
            this CompilationUnit compilation, CompletionItem item, CompletionItemData? data, MarkupKind format)
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
        private static (CompletionScope?, QsFragmentKind?) GetCompletionEnvironment(
            FileContentManager file, Position position, out CodeFragment? fragment)
        {
            if (!file.ContainsPosition(position))
            {
                fragment = null;
                return (null, null);
            }
            var token = GetTokenAtOrBefore(file, position);
            if (token is null)
            {
                fragment = null;
                return (null, null);
            }

            fragment = token.GetFragment();
            var relativeIndentation = fragment.Indentation - file.IndentationAt(position);
            QsCompilerError.Verify(Math.Abs(relativeIndentation) <= 1);

            var parents = new[] { token }
                .Concat(token.GetNonEmptyParents())
                .Skip(relativeIndentation + 1)
                .Select(index => index.GetFragment())
                .ToImmutableList();
            var scope =
                parents.IsEmpty ? CompletionScope.TopLevel
                : parents.FirstOrDefault()?.Kind?.IsNamespaceDeclaration ?? false ? CompletionScope.NamespaceTopLevel
                : parents.Any(parent => parent.Kind?.IsFunctionDeclaration ?? false) ? CompletionScope.Function
                : parents.FirstOrDefault()?.Kind?.IsOperationDeclaration ?? false ? CompletionScope.OperationTopLevel
                : parents.Any(parent => parent.Kind?.IsOperationDeclaration ?? false) ? CompletionScope.Operation
                : null;
            var previous =
                relativeIndentation == 0 && IsPositionAfterDelimiter(file, fragment, position) ? fragment.Kind
                : relativeIndentation == 0 ? token.PreviousOnScope()?.GetFragment().Kind
                : relativeIndentation == 1 ? token.GetNonEmptyParent()?.GetFragment().Kind
                : null;
            return (scope, previous);
        }

        /// <summary>
        /// Returns completion items that match the given kind.
        /// </summary>
        private static IEnumerable<CompletionItem> GetCompletionsForKind(
            FileContentManager file,
            CompilationUnit compilation,
            Position position,
            CompletionKind kind,
            string namespacePrefix = "")
        {
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
            var currentNamespace = file.TryGetNamespaceAt(position);
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
        /// <exception cref="FileContentException">The file content changed while processing.</exception>
        private static IEnumerable<CompletionItem> GetFallbackCompletions(
            FileContentManager file, CompilationUnit compilation, Position position)
        {
            // If the character at the position is a dot but no valid namespace path precedes it (for example, in a
            // decimal number), then no completions are valid here.
            var nsPath = GetSymbolNamespacePath(file, position);
            if (nsPath == null &&
                position.Column > 0 &&
                position.Column <= file.GetLine(position.Line).Text.Length &&
                file.GetLine(position.Line).Text[position.Column - 1] == '.')
            {
                return Array.Empty<CompletionItem>();
            }

            var currentNamespace = file.TryGetNamespaceAt(position);
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
        private static IEnumerable<CompletionItem> GetLocalCompletions(
            FileContentManager file, CompilationUnit compilation, Position position, bool mutableOnly = false) =>
            compilation
                .TryGetLocalDeclarations(file, position, out _, includeDeclaredAtPosition: false)
                .Variables
                .Where(variable => !mutableOnly || variable.InferredInformation.IsMutable)
                .Select(variable => new CompletionItem
                {
                    Label = variable.VariableName,
                    Kind = CompletionItemKind.Variable
                });

        /// <summary>
        /// Returns completions for all accessible callables given the current namespace and the list of open
        /// namespaces, or an empty enumerable if symbols haven't been resolved yet.
        /// </summary>
        private static IEnumerable<CompletionItem> GetCallableCompletions(
            FileContentManager file,
            CompilationUnit compilation,
            string? currentNamespace,
            IEnumerable<string> openNamespaces)
        {
            if (!compilation.GlobalSymbols.ContainsResolutions)
            {
                return Array.Empty<CompletionItem>();
            }
            return
                compilation.GlobalSymbols.AccessibleCallables()
                .Where(callable =>
                    IsAccessibleAsUnqualifiedName(callable.QualifiedName, currentNamespace, openNamespaces))
                .Select(callable => new CompletionItem
                {
                    Label = callable.QualifiedName.Name,
                    Kind =
                        callable.Kind.IsTypeConstructor ? CompletionItemKind.Constructor : CompletionItemKind.Function,
                    Detail = callable.QualifiedName.Namespace,
                    Data = new CompletionItemData(
                        textDocument: new TextDocumentIdentifier { Uri = file.Uri },
                        qualifiedName: callable.QualifiedName,
                        sourceFile: callable.Source.AssemblyOrCodeFile)
                });
        }

        /// <summary>
        /// Returns completions for all accessible types given the current namespace and the list of open namespaces, or
        /// an empty enumerable if symbols haven't been resolved yet.
        /// </summary>
        private static IEnumerable<CompletionItem> GetTypeCompletions(
            FileContentManager file,
            CompilationUnit compilation,
            string? currentNamespace,
            IEnumerable<string> openNamespaces)
        {
            if (!compilation.GlobalSymbols.ContainsResolutions)
            {
                return Array.Empty<CompletionItem>();
            }
            return
                compilation.GlobalSymbols.AccessibleTypes()
                .Where(type => IsAccessibleAsUnqualifiedName(type.QualifiedName, currentNamespace, openNamespaces))
                .Select(type => new CompletionItem
                {
                    Label = type.QualifiedName.Name,
                    Kind = CompletionItemKind.Struct,
                    Detail = type.QualifiedName.Namespace,
                    Data = new CompletionItemData(
                        textDocument: new TextDocumentIdentifier { Uri = file.Uri },
                        qualifiedName: type.QualifiedName,
                        sourceFile: type.Source.AssemblyOrCodeFile)
                });
        }

        /// <summary>
        /// Returns completions for all named items in any accessible type, or an empty enumerable if symbols haven't
        /// been resolved yet.
        /// </summary>
        private static IEnumerable<CompletionItem> GetNamedItemCompletions(CompilationUnit compilation)
        {
            if (!compilation.GlobalSymbols.ContainsResolutions)
            {
                return Array.Empty<CompletionItem>();
            }
            return compilation.GlobalSymbols.AccessibleTypes()
                .SelectMany(type => ExtractItems(type.TypeItems))
                .Where(item => item.IsNamed)
                .Select(item => new CompletionItem
                {
                    Label = ((QsTypeItem.Named)item).Item.VariableName,
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
        private static IEnumerable<CompletionItem> GetGlobalNamespaceCompletions(
            CompilationUnit compilation, string prefix = "")
        {
            if (prefix.Length != 0 && !prefix.EndsWith("."))
            {
                prefix += ".";
            }
            return
                compilation.GlobalSymbols.NamespaceNames()
                .Where(name => name.StartsWith(prefix))
                .Select(name => NextNamespacePart(name, prefix.Length))
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
        private static IEnumerable<CompletionItem> GetNamespaceAliasCompletions(
            FileContentManager file, CompilationUnit compilation, Position position, string prefix = "")
        {
            if (prefix.Length != 0 && !prefix.EndsWith("."))
            {
                prefix += ".";
            }
            var ns = file.TryGetNamespaceAt(position);
            if (ns == null || !compilation.GlobalSymbols.NamespaceExists(ns))
            {
                return Array.Empty<CompletionItem>();
            }
            return compilation
                .GetOpenDirectives(ns)[file.FileName]
                .SelectNotNull(open => open.Item2?.Apply(alias => (open.Item1, alias)))
                .Where(open => open.alias.StartsWith(prefix))
                .GroupBy(open => NextNamespacePart(open.alias, prefix.Length))
                .Select(open => new CompletionItem
                {
                    Label = open.Key,
                    Kind = CompletionItemKind.Module,
                    Detail = open.Count() == 1 && prefix + open.Key == open.Single().alias
                        ? open.Single().Item1
                        : prefix + open.Key
                });
        }

        /// <summary>
        /// Returns documentation for the callable (if kind is Function or Constructor) or type (if kind is Struct) in
        /// the compilation unit with the given qualified name. Returns null if no documentation is available or the
        /// completion item data is missing properties.
        /// </summary>
        private static string? TryGetDocumentation(
            CompilationUnit compilation, CompletionItemData data, CompletionItemKind kind, bool useMarkdown)
        {
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
                        data.QualifiedName, data.QualifiedName.Namespace, data.SourceFile);
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
                            data.QualifiedName, data.QualifiedName.Namespace, data.SourceFile)
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
        private static IEnumerable<string> GetOpenNamespaces(
            FileContentManager file, CompilationUnit compilation, Position position)
        {
            var @namespace = file.TryGetNamespaceAt(position);
            if (@namespace == null || !compilation.GlobalSymbols.NamespaceExists(@namespace))
            {
                return Array.Empty<string>();
            }
            return compilation
                .GetOpenDirectives(@namespace)[file.FileName]
                .Where(open => open.Item2 == null) // Only include open directives without an alias.
                .Select(open => open.Item1)
                .Concat(new[] { @namespace });
        }

        /// <summary>
        /// Returns the namespace path for the qualified symbol at the given position, or null if there is no qualified
        /// symbol.
        /// </summary>
        /// <exception cref="FileContentException">The file content changed while processing.</exception>
        private static string? GetSymbolNamespacePath(FileContentManager file, Position position)
        {
            var fragment = file.TryGetFragmentAt(position, out _, includeEnd: true);
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
        /// <exception cref="FileContentException">The position is outside the fragment range.</exception>
        private static int GetTextIndexFromPosition(CodeFragment fragment, Position position)
        {
            var relativeLine = position.Line - fragment.Range.Start.Line;
            var lines = Utils.SplitLines(fragment.Text).DefaultIfEmpty("").ToImmutableList();
            var relativeChar = relativeLine == 0
                ? position.Column - fragment.Range.Start.Column
                : position.Column;
            var lineInRange = relativeLine >= 0 && relativeLine <= lines.Count;
            var charInRange = relativeChar >= 0 && relativeChar <= lines.ElementAt(relativeLine).Length;
            return lineInRange && charInRange
                ? lines.Take(relativeLine).Sum(line => line.Length) + relativeChar
                : throw new FileContentException("Position is outside the fragment range.");
        }

        /// <summary>
        /// Resolves the namespace alias and returns its full namespace name. If the alias couldn't be resolved, returns
        /// the alias unchanged.
        /// </summary>
        private static string ResolveNamespaceAlias(
            FileContentManager file, CompilationUnit compilation, Position position, string alias)
        {
            var nsName = file.TryGetNamespaceAt(position);
            if (nsName == null || !compilation.GlobalSymbols.NamespaceExists(nsName))
            {
                return alias;
            }
            return compilation.GlobalSymbols.TryResolveNamespaceAlias(alias, nsName, file.FileName) ?? alias;
        }

        /// <summary>
        /// Returns the token index at, or the closest token index before, the given position. Returns null if there is
        /// no token at or before the given position.
        /// </summary>
        private static CodeFragment.TokenIndex? GetTokenAtOrBefore(FileContentManager file, Position position)
        {
            var line = position.Line;
            var tokens = file.GetTokenizedLine(line);

            // If the current line is empty, find the last non-empty line before it.
            while (tokens.IsEmpty && line > 0)
            {
                tokens = file.GetTokenizedLine(--line);
            }

            var index = tokens.TakeWhile(ContextBuilder.TokensUpTo(position)).Count() - 1;
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
        private static Position GetDelimiterPosition(FileContentManager file, CodeFragment fragment)
        {
            if (fragment.FollowedBy == CodeFragment.MissingDelimiter)
            {
                throw new ArgumentException("Code fragment has a missing delimiter", nameof(fragment));
            }
            var end = fragment.Range.End;
            var position = file.FragmentEnd(ref end);
            return Position.Create(position.Line, position.Column - 1);
        }

        /// <summary>
        /// Returns true if the fragment has a delimiting character and the given position occurs after it.
        /// </summary>
        private static bool IsPositionAfterDelimiter(
                FileContentManager file, CodeFragment fragment, Position position) =>
            fragment.FollowedBy != CodeFragment.MissingDelimiter
            && GetDelimiterPosition(file, fragment) < position;

        /// <summary>
        /// Returns a substring of the fragment text before the given position.
        /// <para/>
        /// If the fragment is null or the position is after the fragment's delimiter, returns the empty string. If the
        /// position is after the end of the fragment text but before the delimiter, the entire text is returned with a
        /// space character appended to it.
        /// </summary>
        /// <exception cref="FileContentException">The position is outside the fragment range.</exception>
        private static string GetFragmentTextBeforePosition(
            FileContentManager file, CodeFragment? fragment, Position position)
        {
            if (fragment == null || IsPositionAfterDelimiter(file, fragment, position))
            {
                return "";
            }
            return fragment.Range.End < position
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
            string? currentNamespace,
            IEnumerable<string> openNamespaces) =>
            openNamespaces.Contains(qualifiedName.Namespace) &&
            (!qualifiedName.Name.StartsWith("_") || qualifiedName.Namespace == currentNamespace);
    }
}
