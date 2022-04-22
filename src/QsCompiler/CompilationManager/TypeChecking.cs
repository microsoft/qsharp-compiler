// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.EditorSupport;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.DependencyAnalysis;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.SymbolManagement;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.TextProcessing;
using Microsoft.Quantum.QsCompiler.Transformations;
using Microsoft.VisualStudio.LanguageServer.Protocol;

using Position = Microsoft.Quantum.QsCompiler.DataTypes.Position;
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    internal static class TypeChecking
    {
        /// <summary>
        /// Builds the corresponding <see cref="HeaderEntry{T}"/> objects for <paramref name="tokens"/>,
        /// throwing the corresponding exceptions if the building fails.
        /// </summary>
        /// <param name="tokens">A collection of the token indices that contain the header item.</param>
        /// <param name="getDeclaration">A function that extracts the declaration.</param>
        /// <returns>
        /// All <see cref="HeaderEntry{T}"/> objects for which the extracted name of the declaration is valid.
        /// </returns>
        private static IEnumerable<(CodeFragment.TokenIndex, HeaderEntry<T>)> GetHeaderItems<T>(
                this FileContentManager file,
                IEnumerable<CodeFragment.TokenIndex> tokens,
                Func<CodeFragment, QsNullable<Tuple<QsSymbol, T>>> getDeclaration,
                string? keepInvalid) =>
            tokens.SelectNotNull(tIndex =>
            {
                var attributes = ImmutableArray.CreateBuilder<AttributeAnnotation>();
                CodeFragment precedingFragment = tIndex.GetFragment();
                for (var preceding = tIndex.PreviousOnScope(includeEmpty: true); preceding != null; preceding = preceding.PreviousOnScope())
                {
                    precedingFragment = preceding.GetFragment();
                    if (precedingFragment.IncludeInCompilation && precedingFragment.Kind is QsFragmentKind.DeclarationAttribute att)
                    {
                        var offset = precedingFragment.Range.Start;
                        attributes.Add(new AttributeAnnotation(att.Item1, att.Item2, offset, precedingFragment.Comments));
                    }
                    else
                    {
                        break;
                    }
                }

                var docComments = file.DocumentingComments(tIndex.GetFragment().Range.Start);
                var headerEntry = HeaderEntry<T>.From(getDeclaration, tIndex, attributes.ToImmutableArray(), docComments, keepInvalid);
                return headerEntry?.Apply(entry => (tIndex, entry));
            });

        /// <summary>
        /// Extracts all documenting comments in <paramref name="file"/> preceding the fragment at <paramref name="pos"/>,
        /// ignoring any attribute annotations unless <paramref name="ignorePrecedingAttributes"/> is set to false.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="pos"/> is not a valid position within <paramref name="file"/>.</exception>
        /// <remarks>
        /// Documenting comments may be separated by an empty lines.
        /// <para/>
        /// Strips the preceding triple-slash for the comments, as well as whitespace and the line break at the end.
        /// </remarks>
        internal static ImmutableArray<string> DocumentingComments(this FileContentManager file, Position pos, bool ignorePrecedingAttributes = true)
        {
            if (!file.ContainsPosition(pos))
            {
                throw new ArgumentException(nameof(pos));
            }

            bool RelevantToken(CodeFragment token) => !(ignorePrecedingAttributes && token.Kind is QsFragmentKind.DeclarationAttribute);
            bool IsDocCommentLine(string text) => text.StartsWith("///") || text == string.Empty;

            var lastPreceding = file.GetTokenizedLine(pos.Line)
                .TakeWhile(ContextBuilder.TokensUpTo(Position.Create(0, pos.Column)))
                .LastOrDefault(RelevantToken)?.WithLineNumOffset(pos.Line);
            for (var lineNr = pos.Line; lastPreceding == null && lineNr-- > 0;)
            {
                lastPreceding = file.GetTokenizedLine(lineNr).LastOrDefault(RelevantToken)?.WithLineNumOffset(lineNr);
            }

            var firstRelevant = lastPreceding == null ? 0 : lastPreceding.Range.End.Line + 1;
            return file.GetLines(firstRelevant, pos.Line > firstRelevant ? pos.Line - firstRelevant : 0)
                .Select(line => line.Text.Trim()).Where(IsDocCommentLine).Select(line => line.TrimStart('/'))
                .SkipWhile(text => string.IsNullOrWhiteSpace(text)).Reverse()
                .SkipWhile(text => string.IsNullOrWhiteSpace(text)).Reverse()
                .ToImmutableArray();
        }

        /// <summary>
        /// Returns the header items corresponding to all namespaces declared in <paramref name="file"/>,
        /// or null if <paramref name="file"/> is null.
        /// </summary>
        /// <remarks>
        /// For namespaces with an invalid namespace name, the symbol name in the header item will be set to an UnknownNamespace.
        /// </remarks>
        private static IEnumerable<(CodeFragment.TokenIndex, HeaderEntry<object>)> GetNamespaceHeaderItems(this FileContentManager file) =>
            file.GetHeaderItems(file.NamespaceDeclarationTokens(), frag => frag.Kind.DeclaredNamespace(), ReservedKeywords.InternalUse.UnknownNamespace);

        /// <summary>
        /// Returns the header items corresponding to all open directives with a valid name in <paramref name="file"/>,
        /// or null if <paramref name="file"/> is null.
        /// </summary>
        private static IEnumerable<(CodeFragment.TokenIndex, HeaderEntry<(string?, QsNullable<Range>)>)> GetOpenDirectivesHeaderItems(
            this FileContentManager file) => file.GetHeaderItems(
                file.OpenDirectiveTokens(),
                frag =>
                {
                    var dir = frag.Kind.OpenedNamespace();
                    if (dir.IsNull)
                    {
                        return QsNullable<Tuple<QsSymbol, (string?, QsNullable<Range>)>>.Null;
                    }

                    QsNullable<Tuple<QsSymbol, (string?, QsNullable<Range>)>> OpenedAs(string? a, QsNullable<Range> r) =>
                        QsNullable<Tuple<QsSymbol, (string?, QsNullable<Range>)>>.NewValue(new Tuple<QsSymbol, (string?, QsNullable<Range>)>(dir.Item.Item1, (a, r)));

                    var alias = dir.Item.Item2;
                    if (alias.IsNull)
                    {
                        return OpenedAs(null, QsNullable<Range>.Null);
                    }

                    var aliasName = alias.Item.Symbol.AsDeclarationName(null);
                    QsCompilerError.Verify(aliasName != null || alias.Item.Symbol.IsInvalidSymbol, "could not extract namespace short name");
                    return OpenedAs(aliasName, alias.Item.Range);
                },
                null);

        /// <summary>
        /// Returns the header items corresponding to all type declarations with a valid name in <paramref name="file"/>,
        /// or null if <paramref name="file"/> is null.
        /// </summary>
        private static IEnumerable<(CodeFragment.TokenIndex, HeaderEntry<TypeDefinition>)>
            GetTypeDeclarationHeaderItems(this FileContentManager file) =>
            file.GetHeaderItems(file.TypeDeclarationTokens(), frag => frag.Kind.DeclaredType(), null);

        /// <summary>
        /// Returns the header items corresponding to all callable declarations with a valid name in <paramref name="file"/>,
        /// or null if <paramref name="file"/> is null.
        /// </summary>
        private static IEnumerable<(CodeFragment.TokenIndex, HeaderEntry<Tuple<QsCallableKind, CallableDeclaration>>)>
            GetCallableDeclarationHeaderItems(this FileContentManager file) =>
            file.GetHeaderItems(file.CallableDeclarationTokens(), frag => frag.Kind.DeclaredCallable(), null);

        /// <summary>
        /// Defines a function that extracts the specialization declaration for <paramref name="fragment"/>
        /// that contains a specialization that can be used to build a <see cref="HeaderEntry{T}"/> for
        /// the specialization.
        /// </summary>
        /// <param name="parent">The <see cref="HeaderEntry{T}"/> of the parent.</param>
        /// <remarks>
        /// The symbol saved in that <see cref="HeaderEntry{T}"/> then is the name of the specialized callable,
        /// and its declaration contains the specialization kind as well as the range info for the specialization intro.
        /// <para/>
        /// The function returns null if the <see cref="CodeFragment.Kind"/> of <paramref name="fragment"/> is null.
        /// </remarks>
        /// <!-- TODO: help -->
        private static QsNullable<Tuple<QsSymbol, (QsSpecializationKind, QsSpecializationGenerator, Range)>> SpecializationDeclaration(
            HeaderEntry<Tuple<QsCallableKind, CallableDeclaration>> parent, CodeFragment fragment)
        {
            var specDecl = fragment.Kind?.DeclaredSpecialization();
            var @null = QsNullable<Tuple<QsSymbol, (QsSpecializationKind, QsSpecializationGenerator, Range)>>.Null;
            if (fragment.Kind is null || !specDecl.HasValue || specDecl.Value.IsNull)
            {
                return @null;
            }

            var (specKind, generator) = (specDecl.Value.Item.Item1.Item1, specDecl.Value.Item.Item1.Item2);
            if (specKind == null)
            {
                return @null;
            }

            var introRange = fragment.Kind.IsControlledAdjointDeclaration
                    ? Parsing.HeaderDelimiters(2).Invoke(fragment.Text)
                    : Parsing.HeaderDelimiters(1).Invoke(fragment.Text);
            var parentSymbol = parent.PositionedSymbol;
            var sym = new QsSymbol(
                QsSymbolKind<QsSymbol>.NewSymbol(parentSymbol.Item1),
                QsNullable<Range>.NewValue(parentSymbol.Item2));
            var returnTuple = new Tuple<QsSymbol, (QsSpecializationKind, QsSpecializationGenerator, Range)>(sym, (specKind, generator, introRange));
            return QsNullable<Tuple<QsSymbol, (QsSpecializationKind, QsSpecializationGenerator, Range)>>.NewValue(returnTuple);
        }

        /// <summary>
        /// Returns the closest proceeding item for <paramref name="pos"/> in <paramref name="items"/>.
        /// </summary>
        /// <param name="items">A collection of positioned items.</param>
        /// <exception cref="ArgumentException">No item precedes <paramref name="pos"/>.</exception>
        private static T ContainingParent<T>(Position pos, IReadOnlyCollection<(Position, T)> items)
        {
            var preceding = items.TakeWhile(tuple => tuple.Item1 < pos);
            return preceding.Any()
                ? preceding.Last().Item2
                : throw new ArgumentException("no preceding item exists");
        }

        /// <summary>
        /// Calls <paramref name="add"/> on each item in <paramref name="itemsToAdd"/>,
        /// and adds the returned diagnostics to <paramref name="diagnostics"/>.
        /// </summary>
        /// <returns>
        /// A list of the token indices and the corresponding header items for which no errors were generated.
        /// </returns>
        private static List<(TItem, HeaderEntry<THeader>)> AddItems<TItem, THeader>(
            IEnumerable<(TItem, HeaderEntry<THeader>)> itemsToAdd,
            Func<Position, Tuple<string, Range>, THeader, ImmutableArray<AttributeAnnotation>, ImmutableArray<string>, QsCompilerDiagnostic[]> add,
            string fileName,
            List<Diagnostic> diagnostics)
        {
            var itemsToCompile = new List<(TItem, HeaderEntry<THeader>)>();
            foreach (var (tIndex, headerItem) in itemsToAdd)
            {
                var messages = add(headerItem.Position, headerItem.PositionedSymbol, headerItem.Declaration, headerItem.Attributes, headerItem.Documentation).ToList();
                if (!messages.Any(msg => msg.Diagnostic.IsError))
                {
                    itemsToCompile.Add((tIndex, headerItem));
                }

                diagnostics.AddRange(messages.Select(msg => Diagnostics.Generate(fileName, msg, headerItem.Position)));
            }

            return itemsToCompile;
        }

        /// <summary>
        /// Updates <paramref name="compilation"/> with the information about all globally declared types and callables in <paramref name="file"/>.
        /// </summary>
        /// <returns>
        /// A lookup for all callables that are to be included in the compilation,
        /// with either a list of the token indices that contain its specializations to be included in the compilation,
        /// or a list consisting of the token index of the callable declaration, if the declaration does not contain any specializations.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The lock for <paramref name="compilation"/> cannot be set because a dependent lock is the gating lock ("outermost lock").
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="file"/> is not at least read-locked, since the returned token indices will only be valid until the next write
        /// operation that affects the tokens in the file.
        /// </exception>
        /// <remarks>
        /// Adds the generated diagnostics to <paramref name="diagnostics"/>.
        /// <para/>
        /// This routine assumes that all empty or invalid fragments have been excluded from compilation prior to calling this routine.
        /// </remarks>
        internal static ImmutableDictionary<QsQualifiedName, (QsComments, IEnumerable<CodeFragment.TokenIndex>?)> UpdateGlobalSymbols(
            this FileContentManager file, CompilationUnit compilation, List<Diagnostic> diagnostics)
        {
            if (!file.SyncRoot.IsAtLeastReadLockHeld())
            {
                throw new InvalidOperationException("file needs to be locked in order to update global symbols");
            }

            compilation.EnterUpgradeableReadLock();
            try
            {
                compilation.GlobalSymbols.RemoveSource(file.FileName);

                // add all namespace declarations
                var namespaceHeaders = file.GetNamespaceHeaderItems().ToImmutableArray();
                var distinctNamespaces = namespaceHeaders.Select(tuple => tuple.Item2).GroupBy(header => header.SymbolName)
                    .Select(headers => compilation.GlobalSymbols.CopyForExtension(headers.First().SymbolName, file.FileName))
                    .ToImmutableDictionary(ns => ns.Name); // making sure the namespaces are extended even if they occur multiple times in the same file
                var namespaces = namespaceHeaders.Select(tuple => (tuple.Item2.Position, distinctNamespaces[tuple.Item2.SymbolName])).ToList();

                // add documenting comments to the namespace declarations
                foreach (var header in namespaceHeaders)
                {
                    distinctNamespaces[header.Item2.SymbolName].AddDocumentation(file.FileName, header.Item2.Documentation);
                }

                // add all type declarations
                var typesToCompile = AddItems(
                    file.GetTypeDeclarationHeaderItems(),
                    (pos, name, decl, att, doc) =>
                        ContainingParent(pos, namespaces).TryAddType(
                            file.FileName,
                            new QsLocation(pos, name.Item2),
                            name,
                            decl.UnderlyingType,
                            att,
                            decl.Access.ValueOr(Access.Public),
                            doc),
                    file.FileName,
                    diagnostics);

                var tokensToCompile = new List<(QsQualifiedName, (QsComments, IEnumerable<CodeFragment.TokenIndex>?))>();
                foreach (var headerItem in typesToCompile)
                {
                    var ns = ContainingParent(headerItem.Item2.Position, namespaces);
                    tokensToCompile.Add((new QsQualifiedName(ns.Name, headerItem.Item2.SymbolName), (headerItem.Item2.Comments, null)));
                }

                // add all callable declarations
                var callablesToCompile = AddItems(
                    file.GetCallableDeclarationHeaderItems(),
                    (pos, name, decl, att, doc) =>
                        ContainingParent(pos, namespaces).TryAddCallableDeclaration(
                            file.FileName,
                            new QsLocation(pos, name.Item2),
                            name,
                            Tuple.Create(decl.Item1, decl.Item2.Signature),
                            att,
                            decl.Item2.Access.ValueOr(Access.Public),
                            doc),
                    file.FileName,
                    diagnostics);

                // add all callable specilizations -> TOOD: needs to be adapted for specializations outside the declaration body (not yet supported)
                foreach (var headerItem in callablesToCompile)
                {
                    var ns = ContainingParent(headerItem.Item2.Position, namespaces);
                    var tIndicesToCompile = AddSpecializationsToNamespace(file, ns, headerItem, diagnostics);
                    var (nsName, cName) = (ns.Name, headerItem.Item2.SymbolName);
                    var callableDeclComments = headerItem.Item2.Comments;
                    tokensToCompile.Add((new QsQualifiedName(nsName, cName), (callableDeclComments, tIndicesToCompile)));
                }

                // push the modified namespace back into the symbol table, while adding all open directives
                foreach (var ns in distinctNamespaces.Values)
                {
                    compilation.GlobalSymbols.AddOrReplaceNamespace(ns);
                }

                return tokensToCompile.ToImmutableDictionary(entry => entry.Item1, entry => entry.Item2);
            }
            finally
            {
                compilation.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Adds all specializations defined within the declaration body of <paramref name="parent"/> to <paramref name="ns"/>.
        /// </summary>
        /// <param name="file">The file of the declaration.</param>
        /// <param name="ns">The namespace of the declaration.</param>
        /// <param name="parent">The header item for a callable declaration.</param>
        /// <returns>
        /// If the given callable declaration contains specializations,
        /// a list of the token indices that contain the specializations to be included in the compilation.
        /// If the given callable does not contain any specializations,
        /// a list of token indices containing only the token of the callable declaration.
        /// </returns>
        /// <remarks>
        /// If the declaration body consists of only statements, adds the specialization corresponding to the default body.
        /// <para/>
        /// Adds the diagnostics generated during the process to <paramref name="diagnostics"/>.
        /// </remarks>
        private static List<CodeFragment.TokenIndex> AddSpecializationsToNamespace(
            FileContentManager file,
            Namespace ns,
            (CodeFragment.TokenIndex, HeaderEntry<Tuple<QsCallableKind, CallableDeclaration>>) parent,
            List<Diagnostic> diagnostics)
        {
            var contentToCompile = new List<CodeFragment.TokenIndex>();
            var callableDecl = parent.Item2.Declaration;
            var parentName = parent.Item2.PositionedSymbol;

            // adding all specializations
            var directChildren = parent.Item1.GetChildren(deep: false);
            var specializationTokens = directChildren.Where(tIndex => FileHeader.IsCallableSpecialization(tIndex.GetFragment()));
            var specializations = file.GetHeaderItems(specializationTokens, frag => SpecializationDeclaration(parent.Item2, frag), null);
            foreach (var (tIndex, headerItem) in specializations)
            {
                var position = headerItem.Position;
                var (specKind, generator, introRange) = headerItem.Declaration;
                var location = new QsLocation(position, introRange);
                var messages = ns.TryAddCallableSpecialization(specKind, file.FileName, location, parentName, generator, headerItem.Attributes, headerItem.Documentation);
                if (!messages.Any(msg => msg.Diagnostic.IsError))
                {
                    contentToCompile.Add(tIndex);
                }

                foreach (var msg in messages)
                {
                    diagnostics.Add(Diagnostics.Generate(file.FileName, msg, position));
                }
            }

            // verify that either no specialization occurs, or that no other fragment than specializations occur
            var directlyContainedStatements = directChildren.Select(token => token.GetFragment())
                .Where(fragment => fragment.Kind != null && fragment.IncludeInCompilation && !FileHeader.IsCallableSpecialization(fragment));
            if (specializations.Any() && directlyContainedStatements.Any())
            {
                foreach (var statement in directlyContainedStatements)
                {
                    var msgRange = Parsing.HeaderDelimiters(1).Invoke(statement.Text);
                    var msg = QsCompilerDiagnostic.Error(ErrorCode.NotWithinSpecialization, Enumerable.Empty<string>(), msgRange);
                    diagnostics.Add(Diagnostics.Generate(file.FileName, msg, statement.Range.Start));
                }
            }

            // if the declaration directly contains statements and no specialization, auto-generate a default body for the callable
            if (!specializations.Any())
            {
                var location = new QsLocation(parent.Item2.Position, parentName.Item2);
                var genRange = QsNullable<Range>.NewValue(location.Range); // set to the range of the callable name
                var omittedSymbol = new QsSymbol(QsSymbolKind<QsSymbol>.OmittedSymbols, QsNullable<Range>.Null);
                var generatorKind = QsSpecializationGeneratorKind<QsSymbol>.NewUserDefinedImplementation(omittedSymbol);
                var generator = new QsSpecializationGenerator(QsNullable<ImmutableArray<QsType>>.Null, generatorKind, genRange);
                var messages = ns.TryAddCallableSpecialization(
                    QsSpecializationKind.QsBody,
                    file.FileName,
                    location,
                    parentName,
                    generator,
                    ImmutableArray<AttributeAnnotation>.Empty,
                    ImmutableArray<string>.Empty);
                QsCompilerError.Verify(!messages.Any(), "compiler returned diagnostic(s) for automatically inserted specialization");
                contentToCompile.Add(parent.Item1);
            }

            return contentToCompile;
        }

        /// <summary>
        /// Resolves all type declarations, callable declarations and callable specializations in <paramref name="symbols"/>,
        /// and verifies that the type declarations do not have any cyclic dependencies.
        /// </summary>
        /// <remarks>
        /// If <paramref name="fileName"/> is null,
        /// adds all diagnostics generated during resolution and verification to <paramref name="diagnostics"/>.
        /// <para/>
        /// If <paramref name="fileName"/> is not null, adds only the diagnostics for the file with that name to <paramref name="diagnostics"/>.
        /// </remarks>
        internal static void ResolveGlobalSymbols(NamespaceManager symbols, List<Diagnostic> diagnostics, string? fileName = null)
        {
            var declDiagnostics = symbols.ResolveAll(BuiltIn.NamespacesToAutoOpen);
            var cycleDiagnostics = SyntaxProcessing.SyntaxTree.CheckDefinedTypesForCycles(symbols.DefinedTypes());

            void AddDiagnostics(string source, IEnumerable<QsCompilerDiagnostic> msgs) =>
                diagnostics.AddRange(msgs.Select(msg => Diagnostics.Generate(source, msg)));

            if (fileName != null)
            {
                if (declDiagnostics.Contains(fileName))
                {
                    AddDiagnostics(fileName, declDiagnostics[fileName]);
                }

                if (cycleDiagnostics.Contains(fileName))
                {
                    AddDiagnostics(fileName, cycleDiagnostics[fileName]);
                }
            }
            else
            {
                foreach (var grouping in declDiagnostics)
                {
                    AddDiagnostics(grouping.Key, grouping);
                }

                foreach (var grouping in cycleDiagnostics)
                {
                    AddDiagnostics(grouping.Key, grouping);
                }
            }
        }

        /// <summary>
        /// Updates the symbol information in <paramref name="compilation"/> with all (validly placed) open directives in <paramref name="file"/>,
        /// and adds the generated diagnostics for <paramref name="file"/> *only* to <paramref name="diagnostics"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The lock for <paramref name="compilation"/> cannot be set because a dependent lock is the gating lock ("outermost lock").</exception>
        internal static void ImportGlobalSymbols(this FileContentManager file, CompilationUnit compilation, List<Diagnostic> diagnostics)
        {
            // While in principle the file does not need to be externally locked for this routine to evaluate correctly,
            // it is to be expected that the file is indeed locked, such that the information about open directives is consistent with the one on header items -
            // of course there is no way to verify that even if the file is locked, let's still call QsCompilerError.Verify on the file lock.
            QsCompilerError.Verify(file.SyncRoot.IsAtLeastReadLockHeld(), "file should be locked when calling ImportAndResolveGlobalSymbols");
            file.SyncRoot.EnterReadLock(); // there is no reason to also set a lock for the compilation - AddOpenDirective and ResolveAll will set a suitable lock
            try
            {
                var namespaces = file.GetNamespaceHeaderItems().Select(tuple => (tuple.Item2.Position, tuple.Item2.SymbolName)).ToList();
                AddItems(
                    file.GetOpenDirectivesHeaderItems(),
                    (pos, name, alias, _, __) => compilation.GlobalSymbols.AddOpenDirective(name.Item1, name.Item2, alias.Item1, alias.Item2, ContainingParent(pos, namespaces), file.FileName),
                    file.FileName,
                    diagnostics);
            }
            finally
            {
                file.SyncRoot.ExitReadLock();
            }
        }

        /// <summary>
        /// Builds a <see cref="FragmentTree"/> containing <paramref name="content"/> for a certain parent.
        /// </summary>
        /// <param name="content">A grouping of token indices.</param>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="file"/> is not at least read-locked, since token indices are only ever valid until the next write operation to the file they are associated with.
        /// </exception>
        /// <remarks>
        /// Assumes that all given token indices in <paramref name="content"/> are associated with <paramref name="file"/>.
        /// </remarks>
        internal static ImmutableDictionary<QsQualifiedName, (QsComments, FragmentTree)> GetDeclarationTrees(
            this FileContentManager file,
            ImmutableDictionary<QsQualifiedName, (QsComments, IEnumerable<CodeFragment.TokenIndex>?)> content)
        {
            if (!file.SyncRoot.IsAtLeastReadLockHeld())
            {
                throw new InvalidOperationException("file needs to be locked in order to build the FragmentTrees from the given token indices");
            }

            IEnumerable<CodeFragment.TokenIndex> ChildrenToCompile(CodeFragment.TokenIndex token) =>
                token.GetChildren(deep: false).Where(child => child.GetFragment().IncludeInCompilation);

            IReadOnlyList<FragmentTree.TreeNode> BuildNodes(IEnumerable<CodeFragment.TokenIndex> tokens, Position? rootPos) =>
                tokens.Select(token =>
                {
                    var fragment = token.GetFragmentWithClosingComments();
                    var parentPos = rootPos ?? fragment.Range.Start;
                    return new FragmentTree.TreeNode(fragment, BuildNodes(ChildrenToCompile(token), parentPos), parentPos);
                })
                .ToList().AsReadOnly();

            var specRoots = content.Select(item =>
            {
                var nodes = item.Value.Item2?.Apply(tokens => BuildNodes(tokens, null));
                return (item.Key, item.Value.Item1, nodes);
            });
            return specRoots.ToImmutableDictionary(
                spec => spec.Key,
                spec => (spec.Item2, new FragmentTree(file.FileName, spec.Key.Namespace, spec.Key.Name, spec.nodes)));
        }

        /// <summary>
        /// Updates and resolves all global symbols in each file in <paramref name="files"/>,
        /// and modifies <paramref name="compilation"/> accordingly in the process.
        /// </summary>
        /// <remarks>
        /// Updates and pushes all header diagnostics in <paramref name="files"/>,
        /// and clears all semantic diagnostics without pushing them.
        /// </remarks>
        internal static ImmutableDictionary<QsQualifiedName, (QsComments, FragmentTree)> UpdateGlobalSymbolsFor(
            this CompilationUnit compilation, IEnumerable<FileContentManager> files)
        {
            compilation.EnterWriteLock();
            foreach (var file in files)
            {
                file.SyncRoot.EnterUpgradeableReadLock();
            }

            try
            {
                // get the fragment trees for the declaration content of all files and callables
                var diagnostics = new List<Diagnostic>();
                var content = files.SelectMany(file =>
                    file.GetDeclarationTrees(file.UpdateGlobalSymbols(compilation, diagnostics)))
                    .ToImmutableDictionary(pair => pair.Key, pair => pair.Value);

                // add all open directives to each file (do not resolve yet)
                foreach (var file in files)
                {
                    file.ImportGlobalSymbols(compilation, diagnostics);
                }

                // do the resolution for all files, and construct the corresponding diagnostics
                ResolveGlobalSymbols(compilation.GlobalSymbols, diagnostics);

                // replace the header diagnostics in each file, and publish them
                var messages = diagnostics.ToLookup(d => d.Source);
                foreach (var file in files)
                {
                    file.ReplaceHeaderDiagnostics(messages[file.FileName]);
                }

                // return the declaration content of all files and callables
                return content;
            }
            finally
            {
                foreach (var file in files)
                {
                    file.SyncRoot.ExitUpgradeableReadLock();
                }

                compilation.ExitWriteLock();
            }
        }

        // routines for building statements

        /// <summary>
        /// Builds the <see cref="QsScope"/> containing <paramref name="nodes"/>,
        /// calling <see cref="BuildStatement"/> for each of them, and using <paramref name="context"/> to verify and track all symbols.
        /// </summary>
        /// <param name="nodes">A list of tree nodes.</param>
        /// <param name="requiredFunctors">A set of required functors.</param>
        /// <remarks>
        /// The declarations the scope inherits from its parents are assumed to be the current declarations in <paramref name="context"/>.
        /// <para/>
        /// If <paramref name="requiredFunctors"/> is specified, then each operation called within the built scope needs to support these functors.
        /// If <paramref name="requiredFunctors"/> is null, then the functors to support are determined by the parent scope.
        /// </remarks>
        private static QsScope BuildScope(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext context,
            List<Diagnostic> diagnostics,
            ImmutableHashSet<QsFunctor>? requiredFunctors = null)
        {
            var inheritedSymbols = context.Symbols.CurrentDeclarations;
            context.Symbols.BeginScope(requiredFunctors);
            var statements = BuildStatements(nodes, context, diagnostics);
            context.Symbols.EndScope();
            return new QsScope(statements, inheritedSymbols);
        }

        /// <summary>
        /// If the current node is not followed by an opening bracket, builds a scope that implicitly starts with the
        /// statement after the current node, and continues until the end of the current scope. Otherwise, builds a
        /// scope using the current node's children.
        /// </summary>
        /// <seealso cref="BuildScope"/>
        private static QsScope BuildImplicitScope(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext context,
            List<Diagnostic> diagnostics,
            ImmutableHashSet<QsFunctor>? requiredFunctors = null)
        {
            var children = nodes.Current.Fragment.FollowedBy == '{'
                ? nodes.Current.Children.GetEnumerator()
                : nodes;

            return BuildScope(children, context, diagnostics, requiredFunctors);
        }

        /// <summary>
        /// Applies <paramref name="build"/> to the position relative to <paramref name="node"/> and <paramref name="context"/>
        /// to get the desired object as well as a list of diagnostics.
        /// </summary>
        /// <param name="node">The tree root.</param>
        /// <returns>
        /// The build object.
        /// </returns>
        /// <remarks>
        /// Adds the generated diagnostics to <paramref name="diagnostics"/>.
        /// </remarks>
        private static T BuildStatement<T>(
            FragmentTree.TreeNode node,
            Func<QsLocation, ScopeContext, Tuple<T, QsCompilerDiagnostic[]>> build,
            ScopeContext context,
            List<Diagnostic> diagnostics)
        {
            context.Inference.UseSyntaxTreeNodeLocation(node.RootPosition, node.RelativePosition);
            var location = new QsLocation(node.RelativePosition, node.Fragment.HeaderRange);
            var (statement, buildDiagnostics) = build(location, context);
            diagnostics.AddRange(buildDiagnostics
                .Select(diagnostic => Diagnostics.Generate(context.Symbols.SourceFile, diagnostic, node.RootPosition + node.RelativePosition)));

            return statement;
        }

        /// <summary>
        /// If the current tree node of <paramref name="nodes"/> is a using-block intro,
        /// builds the corresponding using-statement updating <paramref name="context"/> in the process,
        /// and moves <paramref name="nodes"/> to the next node.
        /// </summary>
        /// <param name="proceed">True if <paramref name="nodes"/> contains another node at the end of the routine.</param>
        /// <param name="statement">The resulting build statement, or null if this routine returns false.</param>
        /// <returns>
        /// True if the statement has been built, false otherwise.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="context"/> does not currently contain an open scope.</exception>
        /// <remarks>
        /// <paramref name="proceed"/> is set to true if either <paramref name="nodes"/> has not been moved (no statement built),
        /// or if the last <see cref="IEnumerator.MoveNext"/> call on <paramref name="nodes"/> returned true, and is otherwise set to false.
        /// <para/>
        /// Adds the diagnostics generated during the building to <paramref name="diagnostics"/>.
        /// <para/>
        /// This routine will fail if accessing the current item of <paramref name="nodes"/> fails.
        /// </remarks>
        private static bool TryBuildUsingStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            [NotNullWhen(true)] out QsStatement? statement)
        {
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }

            if (nodes.Current.Fragment.Kind is QsFragmentKind.UsingBlockIntro allocate)
            {
                context.Symbols.BeginScope(); // pushing a scope such that the declared variables are not available outside the body
                var allocationScope = BuildStatement(
                    nodes.Current,
                    (relPos, ctx) => Statements.NewAllocateScope(nodes.Current.Fragment.Comments, relPos, ctx, allocate.Item1, allocate.Item2),
                    context,
                    diagnostics);
                statement = allocationScope(BuildImplicitScope(nodes, context, diagnostics));
                context.Symbols.EndScope();
                proceed = nodes.MoveNext();
                return true;
            }

            (statement, proceed) = (null, true);
            return false;
        }

        /// <summary>
        /// If the current tree node of <paramref name="nodes"/> is a borrowing-block intro,
        /// builds the corresponding borrowing-statement updating <paramref name="context"/> in the process,
        /// and moves <paramref name="nodes"/> to the next node.
        /// </summary>
        /// <param name="proceed">True if <paramref name="nodes"/> contains another node at the end of the routine.</param>
        /// <param name="statement">The built statement, or null if this routine returns false.</param>
        /// <returns>
        /// True if the statement has been built, and false otherwise.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="context"/> does not currently contain an open scope.</exception>
        /// <remarks>
        /// <paramref name="proceed"/> is set to true if either <paramref name="nodes"/> has not been moved (no statement built),
        /// or if the last <see cref="IEnumerator.MoveNext"/> call on <paramref name="nodes"/> returned true, and is otherwise set to false.
        /// <para/>
        /// Adds the diagnostics generated during the building to <paramref name="diagnostics"/>.
        /// <para/>
        /// This routine will fail if accessing the current item of <paramref name="nodes"/> fails.
        /// </remarks>
        private static bool TryBuildBorrowStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            [NotNullWhen(true)] out QsStatement? statement)
        {
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }

            if (nodes.Current.Fragment.Kind is QsFragmentKind.BorrowingBlockIntro borrow)
            {
                context.Symbols.BeginScope(); // pushing a scope such that the declared variables are not available outside the body
                var borrowingScope = BuildStatement(
                    nodes.Current,
                    (relPos, ctx) => Statements.NewBorrowScope(nodes.Current.Fragment.Comments, relPos, ctx, borrow.Item1, borrow.Item2),
                    context,
                    diagnostics);
                statement = borrowingScope(BuildImplicitScope(nodes, context, diagnostics));
                context.Symbols.EndScope();
                proceed = nodes.MoveNext();
                return true;
            }

            (statement, proceed) = (null, true);
            return false;
        }

        /// <summary>
        /// If the current tree node of <paramref name="nodes"/> is a repeat-until-success (RUS) intro,
        /// builds the corresponding RUS-statement updating <paramref name="context"/> in the process,
        /// and moves <paramref name="nodes"/> to the next node.
        /// </summary>
        /// <param name="proceed">True if <paramref name="nodes"/> contains another node at the end of the routine.</param>
        /// <param name="statement">The built statement, or null if this routine returns false.</param>
        /// <returns>
        /// True if the statement has been built, and false otherwise.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="context"/> does not currently contain an open scope, or the repeat header is not followed by a until-success clause.
        /// </exception>
        /// <remarks>
        /// <paramref name="proceed"/> is set to true if either <paramref name="nodes"/> has not been moved (no statement built),
        /// or if the last <see cref="IEnumerator.MoveNext"/> call on <paramref name="nodes"/> returned true, and is otherwise set to false.
        /// <para/>
        /// Adds the diagnostics generated during the building to <paramref name="diagnostics"/>.
        /// <para/>
        /// This routine will fail if accessing the current item of <paramref name="nodes"/> fails.
        /// </remarks>
        private static bool TryBuildRepeatStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            [NotNullWhen(true)] out QsStatement? statement)
        {
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }

            if (nodes.Current.Fragment.Kind?.IsRepeatIntro ?? false)
            {
                context.Symbols.BeginScope();
                var headerNode = nodes.Current;
                var inheritedSymbols = context.Symbols.CurrentDeclarations;
                var repeatBody = new QsScope(BuildStatements(nodes.Current.Children.GetEnumerator(), context, diagnostics), inheritedSymbols);

                if (nodes.MoveNext() && nodes.Current.Fragment.Kind is QsFragmentKind.UntilSuccess untilCond)
                {
                    inheritedSymbols = context.Symbols.CurrentDeclarations;
                    var conditionalBlock = BuildStatement(
                        nodes.Current,
                        (relPos, ctx) => Statements.NewConditionalBlock(nodes.Current.Fragment.Comments, relPos, ctx, untilCond.Item1),
                        context,
                        diagnostics);
                    var fixupBody = untilCond.Item2
                        ? new QsScope(BuildStatements(nodes.Current.Children.GetEnumerator(), context, diagnostics), inheritedSymbols)
                        : new QsScope(ImmutableArray<QsStatement>.Empty, inheritedSymbols);
                    var (successCondition, fixupBlock) = conditionalBlock(fixupBody); // here, condition = true implies the block is *not* executed
                    context.Symbols.EndScope();

                    statement = BuildStatement(
                        headerNode,
                        (relPos, ctx) =>
                        {
                            var repeatBlock = new QsPositionedBlock(repeatBody, QsNullable<QsLocation>.NewValue(relPos), headerNode.Fragment.Comments);
                            return Statements.NewRepeatStatement(ctx.Symbols, repeatBlock, successCondition, fixupBlock);
                        },
                        context,
                        diagnostics);
                    proceed = nodes.MoveNext();
                    return true;
                }
                else
                {
                    throw new ArgumentException("repeat header needs to be followed by an until-clause and a fixup-block");
                }
            }

            (statement, proceed) = (null, true);
            return false;
        }

        /// <summary>
        /// If the current tree node of <paramref name="nodes"/> is a for-loop intro,
        /// builds the corresponding for-statement updating <paramref name="context"/> in the process,
        /// and moves <paramref name="nodes"/> to the next node.
        /// </summary>
        /// <param name="proceed">True if <paramref name="nodes"/> contains another node at the end of the routine.</param>
        /// <param name="statement">The built statement, or null if this routine returns false.</param>
        /// <returns>
        /// True if the statement has been built, and false otherwise.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="context"/> does not currently contain an open scope.</exception>
        /// <remarks>
        /// <paramref name="proceed"/> is set to true if either <paramref name="nodes"/> has not been moved (no statement built),
        /// or if the last <see cref="IEnumerator.MoveNext"/> call on <paramref name="nodes"/> returned true, and is otherwise set to false.
        /// <para/>
        /// Adds the diagnostics generated during the building to <paramref name="diagnostics"/>.
        /// <para/>
        /// This routine will fail if accessing the current item of <paramref name="nodes"/> fails.
        /// </remarks>
        private static bool TryBuildForStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            [NotNullWhen(true)] out QsStatement? statement)
        {
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }

            if (nodes.Current.Fragment.Kind is QsFragmentKind.ForLoopIntro forStatement)
            {
                context.Symbols.BeginScope(); // pushing a scope such that the declared variables are not available outside the body
                var forLoop = BuildStatement(
                    nodes.Current,
                    (relPos, symbols) => Statements.NewForStatement(nodes.Current.Fragment.Comments, relPos, symbols, forStatement.Item1, forStatement.Item2),
                    context,
                    diagnostics);
                var body = BuildScope(nodes.Current.Children.GetEnumerator(), context, diagnostics);
                statement = forLoop(body);
                context.Symbols.EndScope();
                proceed = nodes.MoveNext();
                return true;
            }

            (statement, proceed) = (null, true);
            return false;
        }

        /// <summary>
        /// If the current tree node of <paramref name="nodes"/> is a while-loop intro,
        /// builds the corresponding while-statement updating <paramref name="context"/> in the process,
        /// and moves <paramref name="nodes"/> to the next node.
        /// </summary>
        /// <param name="proceed">True if <paramref name="nodes"/> contains another node at the end of the routine.</param>
        /// <param name="statement">The built statement, or null if this routine returns false.</param>
        /// <returns>
        /// True if the statement has been built, and false otherwise.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="context"/> does not currently contain an open scope.</exception>
        /// <remarks>
        /// <paramref name="proceed"/> is set to true if either <paramref name="nodes"/> has not been moved (no statement built),
        /// or if the last <see cref="IEnumerator.MoveNext"/> call on <paramref name="nodes"/> returned true, and is otherwise set to false.
        /// <para/>
        /// Adds the diagnostics generated during the building to <paramref name="diagnostics"/>.
        /// <para/>
        /// This routine will fail if accessing the current item of <paramref name="nodes"/> fails.
        /// </remarks>
        private static bool TryBuildWhileStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            [NotNullWhen(true)] out QsStatement? statement)
        {
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }

            if (nodes.Current.Fragment.Kind is QsFragmentKind.WhileLoopIntro whileStatement)
            {
                context.Symbols.BeginScope(); // pushing a scope such that the declared variables are not available outside the body
                var whileLoop = BuildStatement(
                    nodes.Current,
                    (relPos, ctx) => Statements.NewWhileStatement(nodes.Current.Fragment.Comments, relPos, ctx, whileStatement.Item),
                    context,
                    diagnostics);
                var body = BuildScope(nodes.Current.Children.GetEnumerator(), context, diagnostics);
                statement = whileLoop(body);
                context.Symbols.EndScope();
                proceed = nodes.MoveNext();
                return true;
            }

            (statement, proceed) = (null, true);
            return false;
        }

        /// <summary>
        /// If the current tree node of <paramref name="nodes"/> is an if-statement into,
        /// builds the corresponding if-statement updating <paramref name="context"/> in the process,
        /// and moves <paramref name="nodes"/> to the next node.
        /// </summary>
        /// <param name="proceed">True if <paramref name="nodes"/> contains another node at the end of the routine.</param>
        /// <param name="statement">The built statement, or null if this routine returns false.</param>
        /// <returns>
        /// True if the statement has been built, and false otherwise.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="context"/> does not currently contain an open scope.</exception>
        /// <remarks>
        /// <paramref name="proceed"/> is set to true if either <paramref name="nodes"/> has not been moved (no statement built),
        /// or if the last <see cref="IEnumerator.MoveNext"/> call on <paramref name="nodes"/> returned true, and is otherwise set to false.
        /// <para/>
        /// Adds the diagnostics generated during the building to <paramref name="diagnostics"/>.
        /// <para/>
        /// This routine will fail if accessing the current item of <paramref name="nodes"/> fails.
        /// </remarks>
        private static bool TryBuildIfStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            [NotNullWhen(true)] out QsStatement? statement)
        {
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }

            if (nodes.Current.Fragment.Kind is QsFragmentKind.IfClause ifCond)
            {
                var rootPosition = nodes.Current.RootPosition;

                // if block
                var buildClause = BuildStatement(
                    nodes.Current,
                    (relPos, ctx) => Statements.NewConditionalBlock(nodes.Current.Fragment.Comments, relPos, ctx, ifCond.Item),
                    context,
                    diagnostics);
                var ifBlock = buildClause(BuildScope(nodes.Current.Children.GetEnumerator(), context, diagnostics));

                // elif blocks
                proceed = nodes.MoveNext();
                var elifBlocks = new List<Tuple<TypedExpression, QsPositionedBlock>>();
                while (proceed && nodes.Current.Fragment.Kind is QsFragmentKind.ElifClause elifCond)
                {
                    buildClause = BuildStatement(
                        nodes.Current,
                        (relPos, ctx) => Statements.NewConditionalBlock(nodes.Current.Fragment.Comments, relPos, ctx, elifCond.Item),
                        context,
                        diagnostics);
                    elifBlocks.Add(buildClause(BuildScope(nodes.Current.Children.GetEnumerator(), context, diagnostics)));
                    proceed = nodes.MoveNext();
                }

                // else block
                var elseBlock = QsNullable<QsPositionedBlock>.Null;
                if (proceed && nodes.Current.Fragment.Kind.IsElseClause)
                {
                    var scope = BuildScope(nodes.Current.Children.GetEnumerator(), context, diagnostics);
                    var elseLocation = new QsLocation(nodes.Current.RelativePosition, nodes.Current.Fragment.HeaderRange);
                    elseBlock = QsNullable<QsPositionedBlock>.NewValue(
                        new QsPositionedBlock(scope, QsNullable<QsLocation>.NewValue(elseLocation), nodes.Current.Fragment.Comments));
                    proceed = nodes.MoveNext();
                }

                statement = Statements.NewIfStatement(ifBlock.Item1, ifBlock.Item2, elifBlocks, elseBlock);
                return true;
            }

            (statement, proceed) = (null, true);
            return false;
        }

        /// <summary>
        /// If the current tree node of <paramref name="nodes"/> is a within-block intro,
        /// builds the corresponding conjugation updating <paramref name="context"/> in the process,
        /// and moves <paramref name="nodes"/> to the next node.
        /// </summary>
        /// <param name="proceed">True if <paramref name="nodes"/> contains another node at the end of the routine.</param>
        /// <param name="statement">The built statement, or null if this routine returns false.</param>
        /// <returns>
        /// True if the statement has been built, and false otherwise.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="context"/> does not currently contain an open scope.</exception>
        /// <remarks>
        /// <paramref name="proceed"/> is set to true if either <paramref name="nodes"/> has not been moved (no statement built),
        /// or if the last <see cref="IEnumerator.MoveNext"/> call on <paramref name="nodes"/> returned true, and is otherwise set to false.
        /// <para/>
        /// Adds the diagnostics generated during the building to <paramref name="diagnostics"/>.
        /// <para/>
        /// This routine will fail if accessing the current item of <paramref name="nodes"/> fails.
        /// </remarks>
        private static bool TryBuildConjugationStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            [NotNullWhen(true)] out QsStatement? statement)
        {
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }

            QsNullable<QsLocation> RelativeLocation(FragmentTree.TreeNode node) =>
                QsNullable<QsLocation>.NewValue(new QsLocation(node.RelativePosition, node.Fragment.HeaderRange));

            if (nodes.Current.Fragment.Kind?.IsWithinBlockIntro ?? false)
            {
                // The requirement for outer blocks in conjugations is always that an adjoint can be auto-generated for them,
                // independent on what functor specializations need to be auto-generated for the containing operation.
                var requiredFunctorSupport = ImmutableHashSet.Create(QsFunctor.Adjoint);
                var outerTranformation = BuildScope(nodes.Current.Children.GetEnumerator(), context, diagnostics, requiredFunctorSupport);
                var outer = new QsPositionedBlock(outerTranformation, RelativeLocation(nodes.Current), nodes.Current.Fragment.Comments);

                if (nodes.MoveNext() && nodes.Current.Fragment.Kind.IsApplyBlockIntro)
                {
                    var innerTransformation = BuildScope(nodes.Current.Children.GetEnumerator(), context, diagnostics);
                    var inner = new QsPositionedBlock(innerTransformation, RelativeLocation(nodes.Current), nodes.Current.Fragment.Comments);
                    var built = Statements.NewConjugation(outer, inner);
                    diagnostics.AddRange(built.Item2.Select(diagnostic => Diagnostics.Generate(
                        context.Symbols.SourceFile, diagnostic, nodes.Current.RootPosition)));

                    statement = built.Item1;
                    proceed = nodes.MoveNext();
                    return true;
                }
                else
                {
                    throw new ArgumentException("within-block needs to be followed by an apply-block");
                }
            }

            (statement, proceed) = (null, true);
            return false;
        }

        /// <summary>
        /// If the current tree node of <paramref name="nodes"/> is a let-statement,
        /// builds the corresponding let-statement updating <paramref name="context"/> in the process,
        /// and moves <paramref name="nodes"/> to the next node.
        /// </summary>
        /// <param name="proceed">True if <paramref name="nodes"/> contains another node at the end of the routine.</param>
        /// <param name="statement">The built statement, or null if this routine returns false.</param>
        /// <returns>
        /// True if the statement has been built, and false otherwise.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="context"/> does not currently contain an open scope.</exception>
        /// <remarks>
        /// <paramref name="proceed"/> is set to true if either <paramref name="nodes"/> has not been moved (no statement built),
        /// or if the last <see cref="IEnumerator.MoveNext"/> call on <paramref name="nodes"/> returned true, and is otherwise set to false.
        /// <para/>
        /// Adds the diagnostics generated during the building to <paramref name="diagnostics"/>.
        /// <para/>
        /// This routine will fail if accessing the current item of <paramref name="nodes"/> fails.
        /// </remarks>
        private static bool TryBuildLetStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            [NotNullWhen(true)] out QsStatement? statement)
        {
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }

            if (nodes.Current.Fragment.Kind is QsFragmentKind.ImmutableBinding letStatement)
            {
                statement = BuildStatement(
                    nodes.Current,
                    (relPos, ctx) => Statements.NewImmutableBinding(nodes.Current.Fragment.Comments, relPos, ctx, letStatement.Item1, letStatement.Item2),
                    context,
                    diagnostics);
                proceed = nodes.MoveNext();
                return true;
            }

            (statement, proceed) = (null, true);
            return false;
        }

        /// <summary>
        /// If the current tree node of <paramref name="nodes"/> is a mutable-statement,
        /// builds the corresponding mutable-statement updating <paramref name="context"/> in the process,
        /// and moves <paramref name="nodes"/> to the next node.
        /// </summary>
        /// <param name="proceed">True if <paramref name="nodes"/> contains another node at the end of the routine.</param>
        /// <param name="statement">The built statement, or null if this routine returns false.</param>
        /// <returns>
        /// True if the statement has been built, and false otherwise.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="context"/> does not currently contain an open scope.</exception>
        /// <remarks>
        /// <paramref name="proceed"/> is set to true if either <paramref name="nodes"/> has not been moved (no statement built),
        /// or if the last <see cref="IEnumerator.MoveNext"/> call on <paramref name="nodes"/> returned true, and is otherwise set to false.
        /// <para/>
        /// Adds the diagnostics generated during the building to <paramref name="diagnostics"/>.
        /// <para/>
        /// This routine will fail if accessing the current item of <paramref name="nodes"/> fails.
        /// </remarks>
        private static bool TryBuildMutableStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            [NotNullWhen(true)] out QsStatement? statement)
        {
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }

            if (nodes.Current.Fragment.Kind is QsFragmentKind.MutableBinding mutableStatement)
            {
                statement = BuildStatement(
                    nodes.Current,
                    (relPos, ctx) => Statements.NewMutableBinding(nodes.Current.Fragment.Comments, relPos, ctx, mutableStatement.Item1, mutableStatement.Item2),
                    context,
                    diagnostics);
                proceed = nodes.MoveNext();
                return true;
            }

            (statement, proceed) = (null, true);
            return false;
        }

        /// <summary>
        /// If the current tree node of <paramref name="nodes"/> is a set-statement,
        /// builds the corresponding set-statement updating <paramref name="context"/> in the process,
        /// and moves <paramref name="nodes"/> to the next node.
        /// </summary>
        /// <param name="proceed">True if <paramref name="nodes"/> contains another node at the end of the routine.</param>
        /// <param name="statement">The built statement, or null if this routine returns false.</param>
        /// <returns>
        /// True if the statement has been built, and false otherwise.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="context"/> does not currently contain an open scope.</exception>
        /// <remarks>
        /// <paramref name="proceed"/> is set to true if either <paramref name="nodes"/> has not been moved (no statement built),
        /// or if the last <see cref="IEnumerator.MoveNext"/> call on <paramref name="nodes"/> returned true, and is otherwise set to false.
        /// <para/>
        /// Adds the diagnostics generated during the building to <paramref name="diagnostics"/>.
        /// <para/>
        /// This routine will fail if accessing the current item of <paramref name="nodes"/> fails.
        /// </remarks>
        private static bool TryBuildSetStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            [NotNullWhen(true)] out QsStatement? statement)
        {
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }

            if (nodes.Current.Fragment.Kind is QsFragmentKind.ValueUpdate setStatement)
            {
                statement = BuildStatement(
                    nodes.Current,
                    (relPos, ctx) => Statements.NewValueUpdate(nodes.Current.Fragment.Comments, relPos, ctx, setStatement.Item1, setStatement.Item2),
                    context,
                    diagnostics);
                proceed = nodes.MoveNext();
                return true;
            }

            (statement, proceed) = (null, true);
            return false;
        }

        /// <summary>
        /// If the current tree node of <paramref name="nodes"/> is a fail-statement,
        /// builds the corresponding fail-statement updating <paramref name="context"/> in the process,
        /// and moves <paramref name="nodes"/> to the next node.
        /// </summary>
        /// <param name="proceed">True if <paramref name="nodes"/> contains another node at the end of the routine.</param>
        /// <param name="statement">The built statement, or null if this routine returns false.</param>
        /// <returns>
        /// True if the statement has been built, and false otherwise.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="context"/> does not currently contain an open scope.</exception>
        /// <remarks>
        /// <paramref name="proceed"/> is set to true if either <paramref name="nodes"/> has not been moved (no statement built),
        /// or if the last <see cref="IEnumerator.MoveNext"/> call on <paramref name="nodes"/> returned true, and is otherwise set to false.
        /// <para/>
        /// Adds the diagnostics generated during the building to <paramref name="diagnostics"/>.
        /// <para/>
        /// This routine will fail if accessing the current item of <paramref name="nodes"/> fails.
        /// </remarks>
        private static bool TryBuildFailStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            [NotNullWhen(true)] out QsStatement? statement)
        {
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }

            if (nodes.Current.Fragment.Kind is QsFragmentKind.FailStatement failStatement)
            {
                statement = BuildStatement(
                    nodes.Current,
                    (relPos, ctx) => Statements.NewFailStatement(nodes.Current.Fragment.Comments, relPos, ctx, failStatement.Item),
                    context,
                    diagnostics);
                proceed = nodes.MoveNext();
                return true;
            }

            (statement, proceed) = (null, true);
            return false;
        }

        /// <summary>
        /// If the current tree node of <paramref name="nodes"/> is a return-statement,
        /// builds the corresponding return-statement updating <paramref name="context"/> in the process,
        /// and moves <paramref name="nodes"/> to the next node.
        /// </summary>
        /// <param name="proceed">True if <paramref name="nodes"/> contains another node at the end of the routine.</param>
        /// <param name="statement">The built statement, or null if this routine returns false.</param>
        /// <returns>
        /// True if the statement has been built, and false otherwise.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="context"/> does not currently contain an open scope.</exception>
        /// <remarks>
        /// <paramref name="proceed"/> is set to true if either <paramref name="nodes"/> has not been moved (no statement built),
        /// or if the last <see cref="IEnumerator.MoveNext"/> call on <paramref name="nodes"/> returned true, and is otherwise set to false.
        /// <para/>
        /// Adds the diagnostics generated during the building to <paramref name="diagnostics"/>.
        /// <para/>
        /// This routine will fail if accessing the current item of <paramref name="nodes"/> fails.
        /// </remarks>
        private static bool TryBuildReturnStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            [NotNullWhen(true)] out QsStatement? statement)
        {
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }

            if (nodes.Current.Fragment.Kind is QsFragmentKind.ReturnStatement returnStatement)
            {
                statement = BuildStatement(
                    nodes.Current,
                    (relPos, ctx) => Statements.NewReturnStatement(nodes.Current.Fragment.Comments, relPos, ctx, returnStatement.Item),
                    context,
                    diagnostics);
                proceed = nodes.MoveNext();
                return true;
            }

            (statement, proceed) = (null, true);
            return false;
        }

        /// <summary>
        /// If the current tree node of <paramref name="nodes"/> is an expression-statement,
        /// builds the corresponding expression-statement updating <paramref name="context"/> in the process,
        /// and moves <paramref name="nodes"/> to the next node.
        /// </summary>
        /// <param name="proceed">True if <paramref name="nodes"/> contains another node at the end of the routine.</param>
        /// <param name="statement">The built statement, or null if this routine returns false.</param>
        /// <returns>
        /// True if the statement has been built, and false otherwise.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="context"/> does not currently contain an open scope.</exception>
        /// <remarks>
        /// <paramref name="proceed"/> is set to true if either <paramref name="nodes"/> has not been moved (no statement built),
        /// or if the last <see cref="IEnumerator.MoveNext"/> call on <paramref name="nodes"/> returned true, and is otherwise set to false.
        /// <para/>
        /// Adds the diagnostics generated during the building to <paramref name="diagnostics"/>.
        /// <para/>
        /// This routine will fail if accessing the current item of <paramref name="nodes"/> fails.
        /// </remarks>
        private static bool TryBuildExpressionStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            [NotNullWhen(true)] out QsStatement? statement)
        {
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }

            if (nodes.Current.Fragment.Kind is QsFragmentKind.ExpressionStatement expressionStatement)
            {
                statement = BuildStatement(
                    nodes.Current,
                    (relPos, ctx) => Statements.NewExpressionStatement(nodes.Current.Fragment.Comments, relPos, ctx, expressionStatement.Item),
                    context,
                    diagnostics);
                proceed = nodes.MoveNext();
                return true;
            }

            (statement, proceed) = (null, true);
            return false;
        }

        /// <summary>
        /// Builds the corrsponding array of Q# statements for <paramref name="nodes"/> (ignoring invalid fragments)
        /// using and updating <paramref name="context"/> and adding the generated diagnostics to <paramref name="diagnostics"/>.
        /// </summary>
        /// <param name="nodes">The sequence of tree nodes for which the corresponding Q# statements should be built.</param>
        /// <exception cref="ArgumentException">
        /// Any statement is missing a header or a required continuation, or <paramref name="context"/> does not currently contain an open scope,
        /// or any of the fragments in <paramref name="nodes"/> is null.
        /// </exception>
        /// <remarks>
        /// Each statement must have a suitable statement header followed by the required continuation(s), if any.
        /// </remarks>
        private static ImmutableArray<QsStatement> BuildStatements(
            IEnumerator<FragmentTree.TreeNode> nodes, ScopeContext context, List<Diagnostic> diagnostics)
        {
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }

            var proceed = nodes.MoveNext();
            var statements = new List<QsStatement>();
            while (proceed)
            {
                if (nodes.Current.Fragment.Kind == null)
                {
                    throw new ArgumentException("fragment kind cannot be null", nameof(nodes.Current.Fragment));
                }
                else if (TryBuildExpressionStatement(nodes, context, diagnostics, out proceed, out var expressionStatement))
                {
                    statements.Add(expressionStatement);
                }
                else if (TryBuildLetStatement(nodes, context, diagnostics, out proceed, out var letStatement))
                {
                    statements.Add(letStatement);
                }
                else if (TryBuildMutableStatement(nodes, context, diagnostics, out proceed, out var mutableStatement))
                {
                    statements.Add(mutableStatement);
                }
                else if (TryBuildSetStatement(nodes, context, diagnostics, out proceed, out var setStatement))
                {
                    statements.Add(setStatement);
                }
                else if (TryBuildFailStatement(nodes, context, diagnostics, out proceed, out var failStatement))
                {
                    statements.Add(failStatement);
                }
                else if (TryBuildReturnStatement(nodes, context, diagnostics, out proceed, out var returnStatement))
                {
                    statements.Add(returnStatement);
                }
                else if (TryBuildForStatement(nodes, context, diagnostics, out proceed, out var forStatement))
                {
                    statements.Add(forStatement);
                }
                else if (TryBuildWhileStatement(nodes, context, diagnostics, out proceed, out var whileStatement))
                {
                    statements.Add(whileStatement);
                }
                else if (TryBuildIfStatement(nodes, context, diagnostics, out proceed, out var ifStatement))
                {
                    statements.Add(ifStatement);
                }
                else if (TryBuildRepeatStatement(nodes, context, diagnostics, out proceed, out var repeatStatement))
                {
                    statements.Add(repeatStatement);
                }
                else if (TryBuildConjugationStatement(nodes, context, diagnostics, out proceed, out var conjugationStatement))
                {
                    statements.Add(conjugationStatement);
                }
                else if (TryBuildBorrowStatement(nodes, context, diagnostics, out proceed, out var borrowingStatement))
                {
                    statements.Add(borrowingStatement);
                }
                else if (TryBuildUsingStatement(nodes, context, diagnostics, out proceed, out var usingStatement))
                {
                    statements.Add(usingStatement);
                }
                else if (nodes.Current.Fragment.Kind.IsInvalidFragment)
                {
                    proceed = nodes.MoveNext();
                }
                else
                {
                    throw new ArgumentException($"node of kind {nodes.Current.Fragment.Kind} is not a valid statement header");
                }
            }

            return statements.ToImmutableArray();
        }

        /// <summary>
        /// Builds the user defined implementation based on the children of <paramref name="root"/>.
        /// </summary>
        /// <param name="root">The specialization root.</param>
        /// <remarks>
        /// The implementation takes <paramref name="argTuple"/> as argument and needs to support auto-generation for <paramref name="requiredFunctorSupport"/>.
        /// <para/>
        /// Uses <paramref name="context"/> to resolve the symbols used within the implementation, and generates suitable diagnostics in the process.
        /// <para/>
        /// If necessary, generates suitable diagnostics for functor arguments (only!), which are discriminated by the missing position information
        /// for argument variables defined in the callable declaration).
        /// <para/>
        /// If the expected return type for the specialization is not Unit, verifies that all paths return a value or fail, generating suitable diagnostics.
        /// <para/>
        /// Adds the generated diagnostics to <paramref name="diagnostics"/>.
        /// </remarks>
        private static SpecializationImplementation BuildUserDefinedImplementation(
            FragmentTree.TreeNode root,
            string sourceFile,
            QsTuple<LocalVariableDeclaration<QsLocalSymbol, ResolvedType>> argTuple,
            ImmutableHashSet<QsFunctor> requiredFunctorSupport,
            ScopeContext context,
            List<Diagnostic> diagnostics)
        {
            // the variable defined on the declaration need to be verified upon building the callable (otherwise we get duplicate diagnostics),
            // but they need to be pushed *first* such that we get suitable re-declaration errors on those defined on the specialization
            // -> the position information is set to null (only) for variables defined in the declaration
            var (variablesOnDeclation, variablesOnSpecialization) = SyntaxGenerator.ExtractItems(argTuple).Partition(decl => decl.Position.IsNull);

            context.Symbols.BeginScope(requiredFunctorSupport);
            foreach (var decl in variablesOnDeclation)
            {
                context.Symbols.TryAddVariableDeclartion(decl);
            }

            var specPos = root.Fragment.Range.Start;
            foreach (var decl in variablesOnSpecialization)
            {
                var msgs = context.Symbols.TryAddVariableDeclartion(decl).Item2;
                var position = specPos + decl.Position.Item;
                diagnostics.AddRange(msgs.Select(msg => Diagnostics.Generate(sourceFile, msg, position)));
            }

            var implementation = BuildScope(root.Children.GetEnumerator(), context, diagnostics);
            context.Symbols.EndScope();

            // Finalize types.
            diagnostics.AddRange(context.Inference.AmbiguousDiagnostics
                .Select(diagnostic => Diagnostics.Generate(sourceFile, diagnostic)));
            var resolver = InferenceContextModule.Resolver(context.Inference);
            implementation = resolver.Statements.OnScope(implementation);

            var (allPathsReturn, returnDiagnostics) = SyntaxProcessing.SyntaxTree.AllPathsReturnValueOrFail(implementation);
            var rootPosition = root.Fragment.Range.Start;
            diagnostics.AddRange(returnDiagnostics.Select(d => Diagnostics.Generate(sourceFile, d, rootPosition)));

            if (!context.ReturnType.Resolution.IsUnitType && !context.ReturnType.Resolution.IsInvalidType && !allPathsReturn)
            {
                var errRange = Parsing.HeaderDelimiters(root.Fragment.Kind?.IsControlledAdjointDeclaration ?? false ? 2 : 1).Invoke(root.Fragment.Text);
                var missingReturn = new QsCompilerDiagnostic(DiagnosticItem.NewError(ErrorCode.MissingReturnOrFailStatement), Enumerable.Empty<string>(), errRange);
                diagnostics.Add(Diagnostics.Generate(sourceFile, missingReturn, specPos));
            }

            return SpecializationImplementation.NewProvided(argTuple, implementation);
        }

        /// <summary>
        /// Determines the necessary functor support required for each operation call within a user defined implementation of <paramref name="spec"/>.
        /// </summary>
        /// <param name="directives">
        /// A function that returns the generator directive for <paramref name="spec"/>, or null if none has been defined for that kind.
        /// </param>
        private static IEnumerable<QsFunctor> RequiredFunctorSupport(
            QsSpecializationKind spec,
            Func<QsSpecializationKind, QsGeneratorDirective?> directives)
        {
            var adjDir = directives(QsSpecializationKind.QsAdjoint);
            var ctlDir = directives(QsSpecializationKind.QsControlled);
            var ctlAdjDir = directives(QsSpecializationKind.QsControlledAdjoint);

            if (spec.IsQsBody && adjDir != null && !(adjDir.IsSelfInverse || adjDir.IsInvalidGenerator))
            {
                if (adjDir.IsInvert)
                {
                    yield return QsFunctor.Adjoint;
                }
                else
                {
                    QsCompilerError.Raise("expecting adjoint functor generator directive to be 'invert'");
                }
            }

            if (spec.IsQsBody && ctlDir != null && !ctlDir.IsInvalidGenerator)
            {
                if (ctlDir.IsDistribute)
                {
                    yield return QsFunctor.Controlled;
                }
                else
                {
                    QsCompilerError.Raise("expecting controlled functor generator directive to be 'distribute'");
                }
            }

            // since a directive for a controlled adjoint specialization requires an auto-generation based on either the adjoint or the controlled version
            // when the latter may or may not be auto-generated, we also need to check the controlled adjoint specialization
            if (ctlAdjDir != null && !(ctlAdjDir.IsSelfInverse || ctlAdjDir.IsInvalidGenerator))
            {
                if (ctlAdjDir.IsDistribute)
                {
                    if (spec.IsQsAdjoint || (spec.IsQsBody && adjDir != null))
                    {
                        yield return QsFunctor.Controlled;
                    }
                }
                else if (ctlAdjDir.IsInvert)
                {
                    if (spec.IsQsControlled || (spec.IsQsBody && ctlDir != null))
                    {
                        yield return QsFunctor.Adjoint;
                    }
                }
                else
                {
                    QsCompilerError.Raise("expecting controlled adjoint functor generator directive to be either 'invert' or 'distribute'");
                }
            }
        }

        /// <summary>
        /// Builds and returns the specializations for <paramref name="specsRoot"/>.
        /// </summary>
        /// <param name="specsRoot">The root of the specialization declaration.</param>
        /// <param name="parentSignature">The signature of the callable that <paramref name="specsRoot"/> belongs to.</param>
        /// <param name="argTuple">The argument tuple of the callable that <paramref name="specsRoot"/> belongs to.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="specsRoot"/> is neither a specialization declaration, nor a callable declaration, or the callable the specialization
        /// belongs to does not support that specialization according to the given <see cref="NamespaceManager"/>.
        /// </exception>
        /// <remarks>
        /// If <paramref name="specsRoot"/> is a callable declaration, a default body specialization with its children as the implementation is returned -
        /// provided the children are exclusively valid statements. Fails with the corresponding exception otherwise.
        /// <para/>
        /// Adds the generated diagnostics to <paramref name="diagnostics"/>.
        /// </remarks>
        private static ImmutableArray<QsSpecialization> BuildSpecializations(
            FragmentTree specsRoot,
            ResolvedSignature parentSignature,
            QsTuple<LocalVariableDeclaration<QsLocalSymbol, ResolvedType>> argTuple,
            CompilationUnit compilation,
            List<Diagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ImmutableArray<QsSpecialization>.Empty;
            }

            var definedSpecs = compilation.GlobalSymbols.DefinedSpecializations(new QsQualifiedName(specsRoot.Namespace, specsRoot.Callable))
                .ToLookup(s => s.Item2.Kind).ToImmutableDictionary(
                    specs => specs.Key,
                    specs => QsCompilerError.RaiseOnFailure(specs.Single, "more than one specialization of the same kind exists")); // currently not supported

            QsSpecialization GetSpecialization(
                    SpecializationDeclarationHeader spec,
                    ResolvedSignature signature,
                    SpecializationImplementation implementation,
                    QsComments? comments = null) =>
                new QsSpecialization(
                    spec.Kind,
                    spec.Parent,
                    spec.Attributes,
                    spec.Source,
                    QsNullable<QsLocation>.Null,
                    spec.TypeArguments,
                    SyntaxGenerator.WithoutRangeInfo(signature),
                    implementation,
                    spec.Documentation,
                    comments ?? QsComments.Empty);

            QsSpecialization BuildSpecialization(
                QsSpecializationKind kind,
                ResolvedSignature signature,
                QsSpecializationGeneratorKind<QsSymbol> gen,
                FragmentTree.TreeNode root,
                Func<QsSymbol, Tuple<QsTuple<LocalVariableDeclaration<QsLocalSymbol, ResolvedType>>, QsCompilerDiagnostic[]>> buildArg,
                QsComments? comments = null)
            {
                if (!definedSpecs.TryGetValue(kind, out var defined))
                {
                    throw new ArgumentException($"missing entry for {kind} specialization of {specsRoot.Namespace}.{specsRoot.Callable}");
                }

                var (directive, spec) = defined;
                var implementation = directive.IsValue ? SpecializationImplementation.NewGenerated(directive.Item) : null;

                // a user defined implementation is ignored if it is invalid to specify such (e.g. for self-adjoint or intrinsic operations)
                if (implementation == null && gen is QsSpecializationGeneratorKind<QsSymbol>.UserDefinedImplementation userDefined)
                {
                    var specPos = root.Fragment.Range.Start;
                    var (arg, messages) = buildArg(userDefined.Item);
                    diagnostics.AddRange(messages.Select(message =>
                        Diagnostics.Generate(spec.Source.AssemblyOrCodeFile, message, specPos)));

                    QsGeneratorDirective? GetDirective(QsSpecializationKind k) => definedSpecs.TryGetValue(k, out defined) && defined.Item1.IsValue ? defined.Item1.Item : null;
                    var requiredFunctorSupport = RequiredFunctorSupport(kind, GetDirective).ToImmutableHashSet();
                    var context = ScopeContext.Create(
                        compilation.GlobalSymbols,
                        compilation.BuildProperties.RuntimeCapability,
                        compilation.BuildProperties.ProcessorArchitecture,
                        spec);
                    implementation = BuildUserDefinedImplementation(
                        root, spec.Source.AssemblyOrCodeFile, arg, requiredFunctorSupport, context, diagnostics);
                    QsCompilerError.Verify(context.Symbols.AllScopesClosed, "all scopes should be closed");
                }

                implementation ??= SpecializationImplementation.Intrinsic;
                return GetSpecialization(spec, signature, implementation, comments);
            }

            var parentCharacteristics = parentSignature.Information.Characteristics; // note that if this one is invalid, the corresponding specializations are still compiled
            var supportedFunctors = parentCharacteristics.SupportedFunctors.ValueOr(ImmutableHashSet<QsFunctor>.Empty);
            var ctlArgPos = QsNullable<Position>.NewValue(Position.Zero); // position relative to the start of the specialization, i.e. zero-position
            var controlledSignature = SyntaxGenerator.BuildControlled(parentSignature);

            QsSpecialization? BuildSpec(FragmentTree.TreeNode root)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                bool InvalidCharacteristicsOrSupportedFunctors(params QsFunctor[] functors) =>
                    parentCharacteristics.AreInvalid || !functors.Any(f => !supportedFunctors.Contains(f));
                if (!definedSpecs.Values.Any(d => d.Item2.Position is DeclarationHeader.Offset.Defined pos && pos.Item == root.Fragment.Range.Start))
                {
                    return null; // only process specializations that are valid
                }

                if (FileHeader.IsCallableDeclaration(root.Fragment))
                {
                    // no specializations have been defined -> one default body
                    var genArg = new QsSymbol(QsSymbolKind<QsSymbol>.OmittedSymbols, QsNullable<Range>.Null);
                    var defaultGen = QsSpecializationGeneratorKind<QsSymbol>.NewUserDefinedImplementation(genArg);
                    return BuildSpecialization(QsSpecializationKind.QsBody, parentSignature, defaultGen, root, argTuple.BuildArgumentBody);
                }
                else if (root.Fragment.Kind is QsFragmentKind.BodyDeclaration bodyDecl)
                {
                    return BuildSpecialization(
                        QsSpecializationKind.QsBody,
                        parentSignature,
                        bodyDecl.Item.Generator,
                        root,
                        argTuple.BuildArgumentBody,
                        root.Fragment.Comments);
                }
                else if (root.Fragment.Kind is QsFragmentKind.AdjointDeclaration adjDecl &&
                    InvalidCharacteristicsOrSupportedFunctors(QsFunctor.Adjoint))
                {
                    return BuildSpecialization(
                        QsSpecializationKind.QsAdjoint,
                        parentSignature,
                        adjDecl.Item.Generator,
                        root,
                        argTuple.BuildArgumentAdjoint,
                        root.Fragment.Comments);
                }
                else if (root.Fragment.Kind is QsFragmentKind.ControlledDeclaration ctlDecl &&
                    InvalidCharacteristicsOrSupportedFunctors(QsFunctor.Controlled))
                {
                    return BuildSpecialization(
                        QsSpecializationKind.QsControlled,
                        controlledSignature,
                        ctlDecl.Item.Generator,
                        root,
                        sym => argTuple.BuildArgumentControlled(sym, ctlArgPos),
                        root.Fragment.Comments);
                }
                else if (root.Fragment.Kind is QsFragmentKind.ControlledAdjointDeclaration ctlAdjDecl &&
                    InvalidCharacteristicsOrSupportedFunctors(QsFunctor.Adjoint, QsFunctor.Controlled))
                {
                    return BuildSpecialization(
                        QsSpecializationKind.QsControlledAdjoint,
                        controlledSignature,
                        ctlAdjDecl.Item.Generator,
                        root,
                        sym => argTuple.BuildArgumentControlledAdjoint(sym, ctlArgPos),
                        root.Fragment.Comments);
                }
                else
                {
                    throw new ArgumentException("the given implementation root is not a valid specialization declaration");
                }
            }

            // build the specializations defined in the source code
            var existing = ImmutableArray.CreateBuilder<QsSpecialization>();
            specsRoot.Specializations?.SelectNotNull(BuildSpec).Apply(existing.AddRange);
            if (cancellationToken.IsCancellationRequested)
            {
                return existing.ToImmutableArray();
            }

            // additionally we need to build the specializations that are missing in the source code
            var existingKinds = existing.Select(t => t.Kind).ToImmutableHashSet();
            foreach (var (directive, spec) in definedSpecs.Values)
            {
                if (existingKinds.Contains(spec.Kind))
                {
                    continue;
                }

                var implementation = directive.IsValue
                    ? SpecializationImplementation.NewGenerated(directive.Item)
                    : SpecializationImplementation.Intrinsic;
                if (spec.Kind.IsQsBody || spec.Kind.IsQsAdjoint)
                {
                    existing.Add(GetSpecialization(spec, parentSignature, implementation));
                }
                else if (spec.Kind.IsQsControlled || spec.Kind.IsQsControlledAdjoint)
                {
                    existing.Add(GetSpecialization(spec, controlledSignature, implementation));
                }
                else
                {
                    QsCompilerError.Raise("unknown functor kind");
                }
            }

            return existing.ToImmutableArray();
        }

        // externally accessible routines for compilation evaluation

        /// <summary>
        /// Given access to a <see cref="NamespaceManager"/> containing all global declarations and their resolutions via <paramref name="compilation"/>,
        /// type checks each <see cref="FragmentTree"/> from <paramref name="roots"/> until the process is cancelled via <paramref name="cancellationToken"/>.
        /// </summary>
        /// <returns>
        /// A list of all accumulated diagnostics, or null if the request has been cancelled.
        /// </returns>
        /// <remarks>
        /// For each namespace and callable name that occurs in each <see cref="FragmentTree"/> builds the corresponding <see cref="QsCallable"/>.
        /// Updates <paramref name="compilation"/> with all built callables. Checks all types defined in the <see cref="NamespaceManager"/> for cycles.
        /// </remarks>
        internal static List<Diagnostic>? RunTypeChecking(
            CompilationUnit compilation,
            ImmutableDictionary<QsQualifiedName, (QsComments, FragmentTree)> roots,
            CancellationToken cancellationToken)
        {
            var diagnostics = new List<Diagnostic>();
            compilation.EnterUpgradeableReadLock();
            try
            {
                // check that the declarations for the types and callables to be built from the given FragmentTrees exist in the given CompilationUnit
                var typeRoots = roots.Where(root => root.Value.Item2.Specializations == null);
                var typeDeclarations = typeRoots.ToImmutableDictionary(
                    root => root.Key,
                    root =>
                    {
                        var result = compilation.GlobalSymbols.TryGetType(root.Key, root.Value.Item2.Namespace, root.Value.Item2.Source);
                        return result is ResolutionResult<TypeDeclarationHeader>.Found type
                            ? type.Item
                            : throw new ArgumentException("type to build is no longer present in the given NamespaceManager");
                    });
                var callableRoots = roots.Where(root => root.Value.Item2.Specializations != null);
                var callableDeclarations = callableRoots.ToImmutableDictionary(
                    root => root.Key,
                    root =>
                    {
                        var result = compilation.GlobalSymbols.TryGetCallable(root.Key, root.Value.Item2.Namespace, root.Value.Item2.Source);
                        return result is ResolutionResult<CallableDeclarationHeader>.Found callable
                            ? callable.Item
                            : throw new ArgumentException("callable to build is no longer present in the given NamespaceManager");
                    });

                (QsQualifiedName, ImmutableArray<QsSpecialization>) GetSpecializations(
                    KeyValuePair<QsQualifiedName, (QsComments, FragmentTree)> specsRoot)
                {
                    var info = callableDeclarations[specsRoot.Key];
                    var specs = BuildSpecializations(specsRoot.Value.Item2, info.Signature, info.ArgumentTuple, compilation, diagnostics, cancellationToken);
                    return (specsRoot.Key, specs);
                }

                QsCallable? GetCallable((QsQualifiedName, ImmutableArray<QsSpecialization>) specItems)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return null;
                    }

                    var (parent, specs) = specItems;
                    var info = callableDeclarations[parent];
                    var declaredVariables = SyntaxGenerator.ExtractItems(info.ArgumentTuple);

                    // verify the variable declarations in the callable declaration
                    var symbolTracker = new SymbolTracker(compilation.GlobalSymbols, info.Source.AssemblyOrCodeFile, parent); // only ever used to verify declaration args
                    symbolTracker.BeginScope();
                    foreach (var decl in declaredVariables)
                    {
                        var offset = info.Position is DeclarationHeader.Offset.Defined pos ? pos.Item : null;
                        QsCompilerError.Verify(offset != null, "missing position information for built callable");
                        var msgs = symbolTracker.TryAddVariableDeclartion(decl).Item2
                            .Select(msg => Diagnostics.Generate(info.Source.AssemblyOrCodeFile, msg, offset));
                        diagnostics.AddRange(msgs);
                    }

                    symbolTracker.EndScope();
                    QsCompilerError.Verify(symbolTracker.AllScopesClosed, "all scopes should be closed");
                    return new QsCallable(
                        info.Kind,
                        parent,
                        info.Attributes,
                        info.Access,
                        info.Source,
                        QsNullable<QsLocation>.Null,
                        info.Signature,
                        info.ArgumentTuple,
                        specs,
                        info.Documentation,
                        roots[parent].Item1);
                }

                var callables = callableRoots.Select(GetSpecializations).Select(GetCallable).ToImmutableArray();
                var types = typeDeclarations.Select(decl => new QsCustomType(
                    decl.Key,
                    decl.Value.Attributes,
                    decl.Value.Access,
                    decl.Value.Source,
                    decl.Value.Location,
                    decl.Value.Type,
                    decl.Value.TypeItems,
                    decl.Value.Documentation,
                    roots[decl.Key].Item1))
                    .ToImmutableArray();

                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                compilation.UpdateCallables(callables);
                compilation.UpdateTypes(types);

                var graph = new CallGraph(compilation.GetCallables().Values);
                diagnostics.AddRange(CapabilityDiagnostics(compilation, graph, callables.Select(c => c!.FullName)));
                diagnostics.AddRange(CycleDiagnostics(compilation.GetCallables(), graph, callableDeclarations));
                return diagnostics;
            }
            finally
            {
                compilation.ExitUpgradeableReadLock();
            }
        }

        private static IEnumerable<Diagnostic> CapabilityDiagnostics(
            CompilationUnit compilation, CallGraph graph, IEnumerable<QsQualifiedName> delta)
        {
            var target = new Target(
                compilation.BuildProperties.RuntimeCapability, compilation.BuildProperties.ProcessorArchitecture);

            foreach (var name in delta)
            {
                var callable = compilation.GetCallables()[name];
                foreach (var diagnostic in Capabilities.Diagnose(target, compilation.GlobalSymbols, graph, callable))
                {
                    yield return Diagnostics.Generate(callable.Source.AssemblyOrCodeFile, diagnostic);
                }
            }
        }

        private static IEnumerable<Diagnostic> CycleDiagnostics(
            IReadOnlyDictionary<QsQualifiedName, QsCallable> callables,
            CallGraph graph,
            ImmutableDictionary<QsQualifiedName, CallableDeclarationHeader> headers)
        {
            bool CycleSelector(ImmutableArray<CallGraphNode> cycle) => // only cycles where all callables are type parameterized need to be checked
                !cycle.Any(node => callables.TryGetValue(node.CallableName, out var decl) && decl.Signature.TypeParameters.IsEmpty);

            foreach (var (diag, parent) in graph.VerifyAllCycles(CycleSelector))
            {
                // Only keep diagnostics for callables that are currently available in the editor.
                if (headers.TryGetValue(parent, out var header))
                {
                    var offset = header.Position is DeclarationHeader.Offset.Defined { Item: var p } ? p : null;
                    yield return Diagnostics.Generate(header.Source.AssemblyOrCodeFile, diag, offset);
                }
            }
        }

        /// <summary>
        /// Returns all locally declared symbols visible at <paramref name="relativePosition"/>,
        /// assuming that the position corresponds to a piece of code within <paramref name="scope"/>.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Any of the statements contained in <paramref name="scope"/> is not annotated with a valid position, or relativePosition is not a valid position.
        /// </exception>
        /// <remarks>
        /// <paramref name="relativePosition"/> is expected to be relative to the beginning of the specialization declaration -
        /// or rather to be consistent with the position information saved for statements.
        /// <para/>
        /// If <paramref name="relativePosition"/> lays outside a piece of code e.g. after a scope ending the returned declarations may be inaccurate.
        /// </remarks>
        public static LocalDeclarations LocalDeclarationsAt(this QsScope scope, Position relativePosition) =>
            SyntaxUtils.LocalsInScope(scope, relativePosition, false);

        /// <summary>
        /// Recomputes the globally defined symbols within <paramref name="file"/> and updates the symbols in <paramref name="compilation"/> accordingly.
        /// </summary>
        /// <remarks>
        /// Replaces all header diagnostics in <paramref name="file"/> with the diagnostics generated during the symbol update.
        /// <para/>
        /// If neither the globally declared types/callables nor the imported namespaces have changed,
        /// directly proceeds to do the type checking for the content of the callable that has been modified only.
        /// <para/>
        /// If the globally declared types/callables have not changed, but the imported namespaces have,
        /// directly proceeds to do the type checking for the entire file content, independent on what parts have been changed.
        /// <para/>
        /// If the globally declared types/callables have changed, a global type checking event is triggered,
        /// since the type checking for the entire compilation unit and all compilation units depending on it needs to be recomputed.
        /// </remarks>
        internal static void UpdateTypeChecking(this FileContentManager file, CompilationUnit compilation)
        {
            compilation.EnterWriteLock();
            file.SyncRoot.EnterUpgradeableReadLock();
            try
            {
                var diagnostics = new List<Diagnostic>();
                var editedCallables = file.DequeueContentEditedCallables();

                // note: we can't return here because even if no callables have been marked as edited, other things like types an open directives may have been...
                var (oldHeader, oldImports) = compilation.GlobalSymbols.HeaderHash(file.FileName);
                var contentTokens = file.UpdateGlobalSymbols(compilation, diagnostics);
                file.ImportGlobalSymbols(compilation, diagnostics);
                ResolveGlobalSymbols(compilation.GlobalSymbols, diagnostics, file.FileName);
                var (newHeader, newImports) = compilation.GlobalSymbols.HeaderHash(file.FileName);
                var (sameHeader, sameImports) = (oldHeader == newHeader, oldImports == newImports);
                file.ReplaceHeaderDiagnostics(diagnostics);

                if (!sameHeader)
                {
                    file.TriggerGlobalTypeChecking();
                    return;
                }

                var declarationTrees = file.GetDeclarationTrees(
                    sameHeader && sameImports
                    ? contentTokens.Where(element => editedCallables.Contains(element.Key)).ToImmutableDictionary()
                    : contentTokens);

                diagnostics = QsCompilerError.RaiseOnFailure(() => RunTypeChecking(compilation, declarationTrees, CancellationToken.None), "error on running type checking");
                if (diagnostics != null)
                {
                    CheckForGlobalCycleChange(file, diagnostics);
                    if (sameImports)
                    {
                        file.AddAndFinalizeSemanticDiagnostics(diagnostics); // diagnostics have been cleared already for the edited callables (only)
                    }
                    else
                    {
                        file.ReplaceSemanticDiagnostics(diagnostics);
                    }
                }
            }
            finally
            {
                file.SyncRoot.ExitUpgradeableReadLock();
                compilation.ExitWriteLock();
            }
        }

        private static void CheckForGlobalCycleChange(FileContentManager file, List<Diagnostic> diagnostics)
        {
            var numCycleDiagnosticsChange = file.CurrentSemanticDiagnostics().Count(DiagnosticTools.ErrorType(ErrorCode.InvalidCyclicTypeParameterResolution))
                - diagnostics.Count(DiagnosticTools.ErrorType(ErrorCode.InvalidCyclicTypeParameterResolution));

            if (numCycleDiagnosticsChange != 0 || diagnostics.Any(x => x.Source != file.FileName))
            {
                file.TriggerGlobalTypeChecking();
            }
        }
    }
}
