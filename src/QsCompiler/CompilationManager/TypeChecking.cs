﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.SymbolManagement;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.TextProcessing;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    using QsRangeInfo = QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>;

    internal static class TypeChecking
    {
        /// <summary>
        /// Given a collections of the token indices that contain the header item,
        /// as well as function that extracts the declaration, builds the corresponding HeaderEntries,
        /// throwing the corresponding exceptions if the building fails.
        /// Returns all HeaderEntries for which the extracted name of the declaration is valid.
        /// Returns null if the given collection of tokens is null.
        /// Throws an ArgumentNullException if the given function for extracting the declaration is null.
        /// </summary>
        private static IEnumerable<(CodeFragment.TokenIndex, HeaderEntry<T>)> GetHeaderItems<T>(
                this FileContentManager file,
                IEnumerable<CodeFragment.TokenIndex> tokens,
                Func<CodeFragment, QsNullable<Tuple<QsSymbol, T>>> getDeclaration,
                string keepInvalid) =>
            tokens?.Select(tIndex =>
            {
                var attributes = ImmutableArray.CreateBuilder<AttributeAnnotation>();
                CodeFragment precedingFragment = tIndex.GetFragment();
                for (var preceding = tIndex.PreviousOnScope(includeEmpty: true); preceding != null; preceding = preceding.PreviousOnScope())
                {
                    precedingFragment = preceding.GetFragment();
                    if (precedingFragment.IncludeInCompilation && precedingFragment.Kind is QsFragmentKind.DeclarationAttribute att)
                    {
                        var offset = DiagnosticTools.AsTuple(precedingFragment.GetRange().Start);
                        attributes.Add(new AttributeAnnotation(att.Item1, att.Item2, offset, precedingFragment.Comments));
                    }
                    else
                    {
                        break;
                    }
                }
                var docComments = file.DocumentingComments(tIndex.GetFragment().GetRange().Start);
                return (tIndex, HeaderEntry<T>.From(getDeclaration, tIndex, attributes.ToImmutableArray(), docComments, keepInvalid));
            })
            ?.Where(tuple => tuple.Item2 != null).Select(tuple => (tuple.Item1, tuple.Item2.Value));

        /// <summary>
        /// Extracts all documenting comments in the given file preceding the fragment at the given position,
        /// ignoring any attribute annotations unless ignorePrecedingAttributes is set to false.
        /// Documenting comments may be separated by an empty lines.
        /// Strips the preceding triple-slash for the comments, as well as whitespace and the line break at the end.
        /// Returns null if no documenting comment is given, or if the documenting comments do not have any non-whitespace content.
        /// Throws an ArgumentNullException if the given file or position is null.
        /// Throws an ArgumentException if the given position is not a valid position within the given file.
        /// </summary>
        internal static ImmutableArray<string> DocumentingComments(this FileContentManager file, Position pos, bool ignorePrecedingAttributes = true)
        {
            if (!Utils.IsValidPosition(pos, file))
            {
                throw new ArgumentException(nameof(pos));
            }
            bool RelevantToken(CodeFragment token) => !(ignorePrecedingAttributes && token.Kind is QsFragmentKind.DeclarationAttribute);
            bool IsDocCommentLine(string text) => text.StartsWith("///") || text == string.Empty;

            var lastPreceding = file.GetTokenizedLine(pos.Line)
                .TakeWhile(ContextBuilder.TokensUpTo(new Position(0, pos.Character)))
                .LastOrDefault(RelevantToken)?.WithUpdatedLineNumber(pos.Line);
            for (var lineNr = pos.Line; lastPreceding == null && lineNr-- > 0;)
            {
                lastPreceding = file.GetTokenizedLine(lineNr).LastOrDefault(RelevantToken)?.WithUpdatedLineNumber(lineNr);
            }

            var firstRelevant = lastPreceding == null ? 0 : lastPreceding.GetRange().End.Line + 1;
            return file.GetLines(firstRelevant, pos.Line > firstRelevant ? pos.Line - firstRelevant : 0)
                .Select(line => line.Text.Trim()).Where(IsDocCommentLine).Select(line => line.TrimStart('/'))
                .SkipWhile(text => string.IsNullOrWhiteSpace(text)).Reverse()
                .SkipWhile(text => string.IsNullOrWhiteSpace(text)).Reverse()
                .ToImmutableArray();
        }

        /// <summary>
        /// Returns the HeaderItems corresponding to all namespaces declared in the given file, or null if the given file is null.
        /// For namespaces with an invalid namespace name the symbol name in the header item will be set to an UnknownNamespace.
        /// </summary>
        private static IEnumerable<(CodeFragment.TokenIndex, HeaderEntry<object>)> GetNamespaceHeaderItems(this FileContentManager file) =>
            file.GetHeaderItems(file?.NamespaceDeclarationTokens(), frag => frag.Kind.DeclaredNamespace(), ReservedKeywords.InternalUse.UnknownNamespace);

        /// <summary>
        /// Returns the HeaderItems corresponding to all open directives with a valid name in the given file, or null if the given file is null.
        /// </summary>
        private static IEnumerable<(CodeFragment.TokenIndex, HeaderEntry<(string, QsRangeInfo)>)> GetOpenDirectivesHeaderItems(
            this FileContentManager file) => file.GetHeaderItems(
                file?.OpenDirectiveTokens(),
                frag =>
                {
                    var dir = frag.Kind.OpenedNamespace();
                    if (dir.IsNull)
                    {
                        return QsNullable<Tuple<QsSymbol, (string, QsRangeInfo)>>.Null;
                    }
                    QsNullable<Tuple<QsSymbol, (string, QsRangeInfo)>> OpenedAs(string a, QsRangeInfo r) =>
                        QsNullable<Tuple<QsSymbol, (string, QsRangeInfo)>>.NewValue(new Tuple<QsSymbol, (string, QsRangeInfo)>(dir.Item.Item1, (a, r)));

                    var alias = dir.Item.Item2;
                    if (alias.IsNull)
                    {
                        return OpenedAs(null, QsRangeInfo.Null);
                    }
                    var aliasName = alias.Item.Symbol.AsDeclarationName(null);
                    QsCompilerError.Verify(aliasName != null || alias.Item.Symbol.IsInvalidSymbol, "could not extract namespace short name");
                    return OpenedAs(aliasName, alias.Item.Range);
                },
                null);

        /// <summary>
        /// Returns the HeaderItems corresponding to all type declarations with a valid name in the given file, or null if the given file is null.
        /// </summary>
        private static IEnumerable<(CodeFragment.TokenIndex, HeaderEntry<Tuple<Modifiers, QsTuple<Tuple<QsSymbol, QsType>>>>)> GetTypeDeclarationHeaderItems(
            this FileContentManager file) => file.GetHeaderItems(file?.TypeDeclarationTokens(), frag => frag.Kind.DeclaredType(), null);

        /// <summary>
        /// Returns the HeaderItems corresponding to all callable declarations with a valid name in the given file, or null if the given file is null.
        /// </summary>
        private static IEnumerable<(CodeFragment.TokenIndex, HeaderEntry<Tuple<QsCallableKind, Modifiers, CallableSignature>>)> GetCallableDeclarationHeaderItems(
            this FileContentManager file) => file.GetHeaderItems(file?.CallableDeclarationTokens(), frag => frag.Kind.DeclaredCallable(), null);

        /// <summary>
        /// Given the HeaderEntry of the parent, defines a function that extracts the specialization declaration
        /// for a CodeFragment that contains a specialization, that can be used to build a HeaderEntry for the specialization.
        /// The symbol saved in that HeaderEntry then is the name of the specialized callable,
        /// and its declaration contains the specialization kind as well as the range info for the specialization intro.
        /// The function returns Null if the Kind of the given fragment is null.
        /// </summary>
        private static QsNullable<Tuple<QsSymbol, (QsSpecializationKind, QsSpecializationGenerator, Tuple<QsPositionInfo, QsPositionInfo>)>> SpecializationDeclaration(
            HeaderEntry<Tuple<QsCallableKind, Modifiers, CallableSignature>> parent, CodeFragment fragment)
        {
            var specDecl = fragment.Kind?.DeclaredSpecialization();
            var @null = QsNullable<Tuple<QsSymbol, (QsSpecializationKind, QsSpecializationGenerator, Tuple<QsPositionInfo, QsPositionInfo>)>>.Null;
            if (!specDecl.HasValue || specDecl.Value.IsNull)
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
                QsRangeInfo.NewValue(parentSymbol.Item2));
            var returnTuple = new Tuple<QsSymbol, (QsSpecializationKind, QsSpecializationGenerator, Tuple<QsPositionInfo, QsPositionInfo>)>(sym, (specKind, generator, introRange));
            return QsNullable<Tuple<QsSymbol, (QsSpecializationKind, QsSpecializationGenerator, Tuple<QsPositionInfo, QsPositionInfo>)>>.NewValue(returnTuple);
        }

        /// <summary>
        /// Given a collection of positioned items, returns the closest proceeding item for the given position.
        /// Throws an ArgumentNullException if the given position or collection of items is null.
        /// Throws an ArugmentException if the given position is not a valid position, or if no item precedes the given position.
        /// </summary>
        private static T ContainingParent<T>(Position pos, IReadOnlyCollection<(Position, T)> items)
        {
            if (!Utils.IsValidPosition(pos))
            {
                throw new ArgumentException(nameof(pos));
            }
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var preceding = items.TakeWhile(tuple => tuple.Item1.IsSmallerThan(pos));
            return preceding.Any()
                ? preceding.Last().Item2
                : throw new ArgumentException("no preceding item exists");
        }

        /// <summary>
        /// Calls the given function on each of the given items to add,
        /// and adds the returned diagnostics to the given list of diagnostics.
        /// Returns a List of the token indices and the corresponding header items for which no errors were generated.
        /// Throws an ArgumentNullException if the given diagnostics or items, or the function to add them is null.
        /// </summary>
        private static List<(TItem, HeaderEntry<THeader>)> AddItems<TItem, THeader>(
            IEnumerable<(TItem, HeaderEntry<THeader>)> itemsToAdd,
            Func<Position, Tuple<NonNullable<string>, Tuple<QsPositionInfo, QsPositionInfo>>, THeader, ImmutableArray<AttributeAnnotation>, ImmutableArray<string>, QsCompilerDiagnostic[]> add,
            string fileName,
            List<Diagnostic> diagnostics)
        {
            if (itemsToAdd == null)
            {
                throw new ArgumentNullException(nameof(itemsToAdd));
            }
            if (add == null)
            {
                throw new ArgumentNullException(nameof(add));
            }
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            var itemsToCompile = new List<(TItem, HeaderEntry<THeader>)>();
            foreach (var (tIndex, headerItem) in itemsToAdd)
            {
                var position = headerItem.GetPosition();
                var messages = add(position, headerItem.PositionedSymbol, headerItem.Declaration, headerItem.Attributes, headerItem.Documentation).ToList();
                if (!messages.Any(msg => msg.Diagnostic.IsError))
                {
                    itemsToCompile.Add((tIndex, headerItem));
                }
                diagnostics.AddRange(messages.Select(msg => Diagnostics.Generate(fileName, msg, position)));
            }
            return itemsToCompile;
        }

        /// <summary>
        /// Updates the given compilation with the information about all globally declared types and callables in the given file.
        /// Adds the generated diagnostics to the given list of diagnostics.
        /// Returns a lookup for all callables that are to be included in the compilation,
        /// with either a list of the token indices that contain its specializations to be included in the compilation,
        /// or a list consisting of the token index of the callable declaration, if the declaration does not contain any specializations.
        /// Note: This routine assumes that all empty or invalid fragments have been excluded from compilation prior to calling this routine.
        /// Throws an ArgumentNullException if any of the given arguments is null.
        /// Throws an InvalidOperationException if the given file is not at least read-locked,
        /// since the returned token indices will only be valid until the next write operation that affects the tokens in the file.
        /// Throws an InvalidOperationException if the lock for the given compilation cannot be set because a dependent lock is the gating lock ("outermost lock").
        /// </summary>
        internal static ImmutableDictionary<QsQualifiedName, (QsComments, IEnumerable<CodeFragment.TokenIndex>)> UpdateGlobalSymbols(
            this FileContentManager file, CompilationUnit compilation, List<Diagnostic> diagnostics)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }
            if (!file.SyncRoot.IsAtLeastReadLockHeld())
            {
                throw new InvalidOperationException("file needs to be locked in order to update global symbols");
            }

            compilation.EnterUpgradeableReadLock();
            try
            {
                compilation.GlobalSymbols.RemoveSource(file.FileName);
                QsLocation Location(Position pos, Tuple<QsPositionInfo, QsPositionInfo> range) => new QsLocation(DiagnosticTools.AsTuple(pos), range);

                // add all namespace declarations
                var namespaceHeaders = file.GetNamespaceHeaderItems().ToImmutableArray();
                var distinctNamespaces = namespaceHeaders.Select(tuple => tuple.Item2).GroupBy(header => header.SymbolName)
                    .Select(headers => compilation.GlobalSymbols.CopyForExtension(headers.First().SymbolName, file.FileName))
                    .ToImmutableDictionary(ns => ns.Name); // making sure the namespaces are extended even if they occur multiple times in the same file
                var namespaces = namespaceHeaders.Select(tuple => (tuple.Item2.GetPosition(), distinctNamespaces[tuple.Item2.SymbolName])).ToList();

                // add documenting comments to the namespace declarations
                foreach (var header in namespaceHeaders)
                {
                    distinctNamespaces[header.Item2.SymbolName].AddDocumentation(file.FileName, header.Item2.Documentation);
                }

                // add all type declarations
                var typesToCompile = AddItems(
                    file.GetTypeDeclarationHeaderItems(),
                    (pos, name, decl, att, doc) => ContainingParent(pos, namespaces).TryAddType(file.FileName, Location(pos, name.Item2), name, decl.Item2, att, decl.Item1, doc),
                    file.FileName.Value,
                    diagnostics);

                var tokensToCompile = new List<(QsQualifiedName, (QsComments, IEnumerable<CodeFragment.TokenIndex>))>();
                foreach (var headerItem in typesToCompile)
                {
                    var ns = ContainingParent(headerItem.Item2.GetPosition(), namespaces);
                    tokensToCompile.Add((new QsQualifiedName(ns.Name, headerItem.Item2.SymbolName), (headerItem.Item2.Comments, null)));
                }

                // add all callable declarations
                var callablesToCompile = AddItems(
                    file.GetCallableDeclarationHeaderItems(),
                    (pos, name, decl, att, doc) => ContainingParent(pos, namespaces).TryAddCallableDeclaration(file.FileName, Location(pos, name.Item2), name, Tuple.Create(decl.Item1, decl.Item3), att, decl.Item2, doc),
                    file.FileName.Value,
                    diagnostics);

                // add all callable specilizations -> TOOD: needs to be adapted for specializations outside the declaration body (not yet supported)
                foreach (var headerItem in callablesToCompile)
                {
                    var ns = ContainingParent(headerItem.Item2.GetPosition(), namespaces);
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
        /// Given the HeaderItem for a callable declaration, as well as the Namespace and the name of the source file it is declared in,
        /// adds all specializations defined within the declaration body to the given namespace.
        /// If the declaration body consists of only statements, adds the specialization corresponding to the default body.
        /// Adds the diagnostics generated during the process to the given list of diagnostics.
        /// If the given callable declaration contains specializations,
        /// returns a list of the token indices that contain the specializations to be included in the compilation.
        /// If the given callable does not contain any specializations,
        /// returns a list of token indices containing only the token of the callable declaration.
        /// Throws an ArgumentNullException if either the given namespace or diagnostics are null.
        /// </summary>
        private static List<CodeFragment.TokenIndex> AddSpecializationsToNamespace(
            FileContentManager file,
            Namespace ns,
            (CodeFragment.TokenIndex, HeaderEntry<Tuple<QsCallableKind, Modifiers, CallableSignature>>) parent,
            List<Diagnostic> diagnostics)
        {
            if (ns == null)
            {
                throw new ArgumentNullException(nameof(ns));
            }
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }
            var contentToCompile = new List<CodeFragment.TokenIndex>();
            var callableDecl = parent.Item2.Declaration;
            var parentName = parent.Item2.PositionedSymbol;

            // adding all specializations
            var directChildren = parent.Item1.GetChildren(deep: false);
            var specializationTokens = directChildren.Where(tIndex => FileHeader.IsCallableSpecialization(tIndex.GetFragment()));
            var specializations = file.GetHeaderItems(specializationTokens, frag => SpecializationDeclaration(parent.Item2, frag), null);
            foreach (var (tIndex, headerItem) in specializations)
            {
                var position = headerItem.GetPosition();
                var (specKind, generator, introRange) = headerItem.Declaration;
                var location = new QsLocation(DiagnosticTools.AsTuple(position), introRange);
                var messages = ns.TryAddCallableSpecialization(specKind, file.FileName, location, parentName, generator, headerItem.Attributes, headerItem.Documentation);
                if (!messages.Any(msg => msg.Diagnostic.IsError))
                {
                    contentToCompile.Add(tIndex);
                }
                foreach (var msg in messages)
                {
                    diagnostics.Add(Diagnostics.Generate(file.FileName.Value, msg, position));
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
                    var msg = QsCompilerDiagnostic.Error(ErrorCode.NotWithinSpecialization, Enumerable.Empty<string>(), msgRange.Item1, msgRange.Item2);
                    diagnostics.Add(Diagnostics.Generate(file.FileName.Value, msg, statement.GetRange().Start));
                }
            }

            // if the declaration directly contains statements and no specialization, auto-generate a default body for the callable
            if (!specializations.Any())
            {
                var location = new QsLocation(DiagnosticTools.AsTuple(parent.Item2.GetPosition()), parentName.Item2);
                var genRange = QsRangeInfo.NewValue(location.Range); // set to the range of the callable name
                var omittedSymbol = new QsSymbol(QsSymbolKind<QsSymbol>.OmittedSymbols, QsRangeInfo.Null);
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
        /// Resolves all type declarations, callable declarations and callable specializations in the given NamespacesManager,
        /// and verifies that the type declarations do not have any cyclic dependencies.
        /// If no fileName is given or the given fileName is null,
        /// adds all diagnostics generated during resolution and verification to the given list of diagnostics.
        /// If the given fileName is not null, adds only the diagnostics for the file with that name to the given list of diagnostics.
        /// Throws an ArgumentNullException if the given NamespaceManager or list of diagnostics is null.
        /// </summary>
        internal static void ResolveGlobalSymbols(NamespaceManager symbols, List<Diagnostic> diagnostics, string fileName = null)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            var declDiagnostics = symbols.ResolveAll(BuiltIn.NamespacesToAutoOpen);
            var cycleDiagnostics = SyntaxProcessing.SyntaxTree.CheckDefinedTypesForCycles(symbols.DefinedTypes());

            void AddDiagnostics(NonNullable<string> source, IEnumerable<Tuple<Tuple<int, int>, QsCompilerDiagnostic>> msgs) =>
                diagnostics.AddRange(msgs.Select(msg => Diagnostics.Generate(source.Value, msg.Item2, DiagnosticTools.AsPosition(msg.Item1))));

            if (fileName != null)
            {
                var key = NonNullable<string>.New(fileName);
                if (declDiagnostics.Contains(key))
                {
                    AddDiagnostics(key, declDiagnostics[key]);
                }
                if (cycleDiagnostics.Contains(key))
                {
                    AddDiagnostics(key, cycleDiagnostics[key]);
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
        /// Updates the symbol information in the given compilation unit with all (validly placed) open directives in the given file,
        /// and adds the generated diagnostics for the given file *only* to the given list of diagnostics.
        /// Throws an ArgumentNullException if the given compilation unit, the given file or the given diagnostics are null.
        /// Throws an InvalidOperationException if the lock for the given compilation cannot be set because a dependent lock is the gating lock ("outermost lock").
        /// </summary>
        internal static void ImportGlobalSymbols(this FileContentManager file, CompilationUnit compilation, List<Diagnostic> diagnostics)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            // While in principle the file does not need to be externally locked for this routine to evaluate correctly,
            // it is to be expected that the file is indeed locked, such that the information about open directives is consistent with the one on header items -
            // of course there is no way to verify that even if the file is locked, let's still call QsCompilerError.Verify on the file lock.
            QsCompilerError.Verify(file.SyncRoot.IsAtLeastReadLockHeld(), "file should be locked when calling ImportAndResolveGlobalSymbols");
            file.SyncRoot.EnterReadLock(); // there is no reason to also set a lock for the compilation - AddOpenDirective and ResolveAll will set a suitable lock
            try
            {
                var namespaces = file.GetNamespaceHeaderItems().Select(tuple => (tuple.Item2.GetPosition(), tuple.Item2.SymbolName)).ToList();
                AddItems(
                    file.GetOpenDirectivesHeaderItems(),
                    (pos, name, alias, _, __) => compilation.GlobalSymbols.AddOpenDirective(name.Item1, name.Item2, alias.Item1, alias.Item2, ContainingParent(pos, namespaces), file.FileName),
                    file.FileName.Value,
                    diagnostics);
            }
            finally
            {
                file.SyncRoot.ExitReadLock();
            }
        }

        /// <summary>
        /// Builds a FragmentTree containting the given grouping of token indices for a certain parent.
        /// Assumes that all given token indices are associated with the given file.
        /// Throws an ArgumentNullException if the given file or any of the given groupings is null.
        /// Throws an InvalidOperationException if the given file is not at least read-locked,
        /// since token indices are only ever valid until the next write operation to the file they are associated with.
        /// </summary>
        internal static ImmutableDictionary<QsQualifiedName, (QsComments, FragmentTree)> GetDeclarationTrees(
            this FileContentManager file,
            ImmutableDictionary<QsQualifiedName, (QsComments, IEnumerable<CodeFragment.TokenIndex>)> content)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            if (!file.SyncRoot.IsAtLeastReadLockHeld())
            {
                throw new InvalidOperationException("file needs to be locked in order to build the FragmentTrees from the given token indices");
            }

            IEnumerable<CodeFragment.TokenIndex> ChildrenToCompile(CodeFragment.TokenIndex token) =>
                token.GetChildren(deep: false).Where(child => child.GetFragment().IncludeInCompilation);

            IReadOnlyList<FragmentTree.TreeNode> BuildNodes(IEnumerable<CodeFragment.TokenIndex> tokens, Position rootPos) =>
                tokens?.Select(token =>
                {
                    var fragment = token.GetFragmentWithClosingComments();
                    var parentPos = rootPos ?? fragment.GetRange().Start;
                    return new FragmentTree.TreeNode(fragment, BuildNodes(ChildrenToCompile(token), parentPos), parentPos);
                })
                ?.ToList()?.AsReadOnly();

            var specRoots = content.Select(item => (item.Key, item.Value.Item1, BuildNodes(item.Value.Item2, null)));
            return specRoots.ToImmutableDictionary(
                spec => spec.Item1,
                spec => (spec.Item2, new FragmentTree(file.FileName, spec.Item1.Namespace, spec.Item1.Name, spec.Item3)));
        }

        /// <summary>
        /// Updates and resolves all global symbols in each given file,
        /// and modifies the given CompilationUnit accordingly in the process.
        /// Updates and pushes all HeaderDiagnostics in the given files,
        /// and clears all semantic diagnostics without pushing them.
        /// If the given Action for publishing diagnostics is not null,
        /// invokes it for the diagnostics of each given file after updating them.
        /// Throws an ArgumentNullException if the given compilation is null,
        /// or if the given files, or any file contained in files, is null.
        /// </summary>
        internal static ImmutableDictionary<QsQualifiedName, (QsComments, FragmentTree)> UpdateGlobalSymbolsFor(
            this CompilationUnit compilation, IEnumerable<FileContentManager> files)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }
            if (files == null || files.Contains(null))
            {
                throw new ArgumentNullException(nameof(files));
            }

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
                    file.ReplaceHeaderDiagnostics(messages[file.FileName.Value]);
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
        /// Builds the QsScope containing the given list of tree nodes,
        /// calling BuildStatement for each of them, and using the given scope context to verify and track all symbols.
        /// The declarations the scope inherits from its parents are assumed to be the current declarations in the given scope context.
        /// If a required set of functors are specified, then each operation called within the built scope needs to support these functors.
        /// If the set of required functors is unspecified or null, then the functors to support are determined by the parent scope.
        /// Throws an ArgumentNullException if the given list of tree nodes, scope context or diagnostics are null.
        /// </summary>
        private static QsScope BuildScope(
            IReadOnlyList<FragmentTree.TreeNode> nodeContent,
            ScopeContext<Position> context,
            List<Diagnostic> diagnostics,
            ImmutableHashSet<QsFunctor> requiredFunctorSupport = null)
        {
            if (nodeContent == null)
            {
                throw new ArgumentNullException(nameof(nodeContent));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            var inheritedSymbols = context.Symbols.CurrentDeclarations;
            context.Symbols.BeginScope(requiredFunctorSupport);
            var statements = BuildStatements(nodeContent.GetEnumerator(), context, diagnostics);
            context.Symbols.EndScope();
            return new QsScope(statements, inheritedSymbols);
        }

        /// <summary>
        /// Applies the given build function to the position relative to the tree root and the given scope context
        /// to get the desired object as well as a list of diagnostics.
        /// Adds the generated diagnostics to the given list of diagnostics, and returns the build object.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the given build function, scope context, or diagnostics are null.
        /// </exception>
        private static T BuildStatement<T>(
            FragmentTree.TreeNode node,
            Func<QsLocation, ScopeContext<Position>, Tuple<T, QsCompilerDiagnostic[]>> build,
            ScopeContext<Position> context,
            List<Diagnostic> diagnostics)
        {
            if (build == null)
            {
                throw new ArgumentNullException(nameof(build));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            var statementPos = node.Fragment.GetRange().Start;
            var location = new QsLocation(DiagnosticTools.AsTuple(node.GetPositionRelativeToRoot()), node.Fragment.HeaderRange);
            var (statement, messages) = build(location, context);
            diagnostics.AddRange(messages.Select(msg => Diagnostics.Generate(context.Symbols.SourceFile.Value, msg, statementPos)));
            return statement;
        }

        /// <summary>
        /// If the current tree node of the given iterator is a using-block intro,
        /// builds the corresponding using-statement updating the given scope context in the process,
        /// and moves the iterator to the next node.
        /// Adds the diagnostics generated during the building to the given list of diagnostics.
        /// Returns the built statement as out parameter, and returns true if the statement has been built.
        /// Sets the out parameter to null and returns false otherwise.
        /// Sets the boolean out parameter to true, if the iterator contains another node at the end of the routine -
        /// i.e. it is set to true if either the iterator has not been moved (no statement built),
        /// or if the last MoveNext() returned true, and is otherwise set to false.
        /// This routine will fail if accessing the current iterator item fails.
        /// Throws an ArgumentNullException if any of the given arguments is null.
        /// Throws an ArgumentException if the given scope context does not currently contain an open scope.
        /// </summary>
        private static bool TryBuildUsingStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext<Position> context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            out QsStatement statement)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }
            if (diagnostics == null)
            {
                throw new ArgumentException(nameof(diagnostics));
            }

            if (nodes.Current.Fragment.Kind is QsFragmentKind.UsingBlockIntro allocate)
            {
                context.Symbols.BeginScope(); // pushing a scope such that the declared variables are not available outside the body
                var allocationScope = BuildStatement(
                    nodes.Current,
                    (relPos, ctx) => Statements.NewAllocateScope(nodes.Current.Fragment.Comments, relPos, ctx, allocate.Item1, allocate.Item2),
                    context,
                    diagnostics);
                var body = BuildScope(nodes.Current.Children, context, diagnostics);
                statement = allocationScope(body);
                context.Symbols.EndScope();
                proceed = nodes.MoveNext();
                return true;
            }
            (statement, proceed) = (null, true);
            return false;
        }

        /// <summary>
        /// If the current tree node of the given iterator is a borrowing-block intro,
        /// builds the corresponding borrowing-statement updating the given scope context in the process,
        /// and moves the iterator to the next node.
        /// Adds the diagnostics generated during the building to the given list of diagnostics.
        /// Returns the built statement as out parameter, and returns true if the statement has been built.
        /// Sets the out parameter to null and returns false otherwise.
        /// Sets the boolean out parameter to true, if the iterator contains another node at the end of the routine -
        /// i.e. it is set to true if either the iterator has not been moved (no statement built),
        /// or if the last MoveNext() returned true, and is otherwise set to false.
        /// This routine will fail if accessing the current iterator item fails.
        /// Throws an ArgumentNullException if any of the given arguments is null.
        /// Throws an ArgumentException if the given scope context does not currently contain an open scope.
        /// </summary>
        private static bool TryBuildBorrowStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext<Position> context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            out QsStatement statement)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }
            if (diagnostics == null)
            {
                throw new ArgumentException(nameof(diagnostics));
            }

            if (nodes.Current.Fragment.Kind is QsFragmentKind.BorrowingBlockIntro borrow)
            {
                context.Symbols.BeginScope(); // pushing a scope such that the declared variables are not available outside the body
                var borrowingScope = BuildStatement(
                    nodes.Current,
                    (relPos, ctx) => Statements.NewBorrowScope(nodes.Current.Fragment.Comments, relPos, ctx, borrow.Item1, borrow.Item2),
                    context,
                    diagnostics);
                var body = BuildScope(nodes.Current.Children, context, diagnostics);
                statement = borrowingScope(body);
                context.Symbols.EndScope();
                proceed = nodes.MoveNext();
                return true;
            }
            (statement, proceed) = (null, true);
            return false;
        }

        /// <summary>
        /// If the current tree node of the given iterator is a repeat-until-success (RUS) intro,
        /// builds the corresponding RUS-statement updating the given scope context in the process,
        /// and moves the iterator to the next node.
        /// Adds the diagnostics generated during the building to the given list of diagnostics.
        /// Returns the built statement as out parameter, and returns true if the statement has been built.
        /// Sets the out parameter to null and returns false otherwise.
        /// Sets the boolean out parameter to true, if the iterator contains another node at the end of the routine -
        /// i.e. it is set to true if either the iterator has not been moved (no statement built),
        /// or if the last MoveNext() returned true, and is otherwise set to false.
        /// This routine will fail if accessing the current iterator item fails.
        /// Throws an ArgumentNullException if any of the given arguments is null.
        /// Throws an ArgumentException if the given scope context does not currently contain an open scope,
        /// or if the repeat header is not followed by a until-success clause.
        /// </summary>
        private static bool TryBuildRepeatStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext<Position> context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            out QsStatement statement)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }
            if (diagnostics == null)
            {
                throw new ArgumentException(nameof(diagnostics));
            }

            if (nodes.Current.Fragment.Kind.IsRepeatIntro)
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
        /// If the current tree node of the given iterator is a for-loop intro,
        /// builds the corresponding for-statement updating the given scope context in the process,
        /// and moves the iterator to the next node.
        /// Adds the diagnostics generated during the building to the given list of diagnostics.
        /// Returns the built statement as out parameter, and returns true if the statement has been built.
        /// Sets the out parameter to null and returns false otherwise.
        /// Sets the boolean out parameter to true, if the iterator contains another node at the end of the routine -
        /// i.e. it is set to true if either the iterator has not been moved (no statement built),
        /// or if the last MoveNext() returned true, and is otherwise set to false.
        /// This routine will fail if accessing the current iterator item fails.
        /// Throws an ArgumentNullException if any of the given arguments is null.
        /// Throws an ArgumentException if the given scope context does not currently contain an open scope.
        /// </summary>
        private static bool TryBuildForStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext<Position> context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            out QsStatement statement)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }
            if (diagnostics == null)
            {
                throw new ArgumentException(nameof(diagnostics));
            }

            if (nodes.Current.Fragment.Kind is QsFragmentKind.ForLoopIntro forStatement)
            {
                context.Symbols.BeginScope(); // pushing a scope such that the declared variables are not available outside the body
                var forLoop = BuildStatement(
                    nodes.Current,
                    (relPos, symbols) => Statements.NewForStatement(nodes.Current.Fragment.Comments, relPos, symbols, forStatement.Item1, forStatement.Item2),
                    context,
                    diagnostics);
                var body = BuildScope(nodes.Current.Children, context, diagnostics);
                statement = forLoop(body);
                context.Symbols.EndScope();
                proceed = nodes.MoveNext();
                return true;
            }
            (statement, proceed) = (null, true);
            return false;
        }

        /// <summary>
        /// If the current tree node of the given iterator is a while-loop intro,
        /// builds the corresponding while-statement updating the given scope context in the process,
        /// and moves the iterator to the next node.
        /// Adds the diagnostics generated during the building to the given list of diagnostics.
        /// Returns the built statement as out parameter, and returns true if the statement has been built.
        /// Sets the out parameter to null and returns false otherwise.
        /// Sets the boolean out parameter to true, if the iterator contains another node at the end of the routine -
        /// i.e. it is set to true if either the iterator has not been moved (no statement built),
        /// or if the last MoveNext() returned true, and is otherwise set to false.
        /// This routine will fail if accessing the current iterator item fails.
        /// Throws an ArgumentNullException if any of the given arguments is null.
        /// Throws an ArgumentException if the given scope context does not currently contain an open scope.
        /// </summary>
        private static bool TryBuildWhileStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext<Position> context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            out QsStatement statement)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }
            if (diagnostics == null)
            {
                throw new ArgumentException(nameof(diagnostics));
            }

            if (nodes.Current.Fragment.Kind is QsFragmentKind.WhileLoopIntro whileStatement)
            {
                context.Symbols.BeginScope(); // pushing a scope such that the declared variables are not available outside the body
                var whileLoop = BuildStatement(
                    nodes.Current,
                    (relPos, ctx) => Statements.NewWhileStatement(nodes.Current.Fragment.Comments, relPos, ctx, whileStatement.Item),
                    context,
                    diagnostics);
                var body = BuildScope(nodes.Current.Children, context, diagnostics);
                statement = whileLoop(body);
                context.Symbols.EndScope();
                proceed = nodes.MoveNext();
                return true;
            }
            (statement, proceed) = (null, true);
            return false;
        }

        /// <summary>
        /// If the current tree node of the given iterator is an if-statement into,
        /// builds the corresponding if-statement updating the given scope context in the process,
        /// and moves the iterator to the next node.
        /// Adds the diagnostics generated during the building to the given list of diagnostics.
        /// Returns the built statement as out parameter, and returns true if the statement has been built.
        /// Sets the out parameter to null and returns false otherwise.
        /// Sets the boolean out parameter to true, if the iterator contains another node at the end of the routine -
        /// i.e. it is set to true if either the iterator has not been moved (no statement built),
        /// or if the last MoveNext() returned true, and is otherwise set to false.
        /// This routine will fail if accessing the current iterator item fails.
        /// Throws an ArgumentNullException if any of the given arguments is null.
        /// Throws an ArgumentException if the given scope context does not currently contain an open scope.
        /// </summary>
        private static bool TryBuildIfStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext<Position> context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            out QsStatement statement)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }
            if (diagnostics == null)
            {
                throw new ArgumentException(nameof(diagnostics));
            }

            if (nodes.Current.Fragment.Kind is QsFragmentKind.IfClause ifCond)
            {
                var rootPosition = nodes.Current.GetRootPosition();

                // if block
                var buildClause = BuildStatement(
                    nodes.Current,
                    (relPos, ctx) => Statements.NewConditionalBlock(nodes.Current.Fragment.Comments, relPos, ctx, ifCond.Item),
                    context.WithinIfCondition,
                    diagnostics);
                var ifBlock = buildClause(BuildScope(nodes.Current.Children, context, diagnostics));

                // elif blocks
                proceed = nodes.MoveNext();
                var elifBlocks = new List<Tuple<TypedExpression, QsPositionedBlock>>();
                while (proceed && nodes.Current.Fragment.Kind is QsFragmentKind.ElifClause elifCond)
                {
                    buildClause = BuildStatement(
                        nodes.Current,
                        (relPos, ctx) => Statements.NewConditionalBlock(nodes.Current.Fragment.Comments, relPos, ctx, elifCond.Item),
                        context.WithinIfCondition,
                        diagnostics);
                    elifBlocks.Add(buildClause(BuildScope(nodes.Current.Children, context, diagnostics)));
                    proceed = nodes.MoveNext();
                }

                // else block
                var elseBlock = QsNullable<QsPositionedBlock>.Null;
                if (proceed && nodes.Current.Fragment.Kind.IsElseClause)
                {
                    var scope = BuildScope(nodes.Current.Children, context, diagnostics);
                    var elseLocation = new QsLocation(DiagnosticTools.AsTuple(nodes.Current.GetPositionRelativeToRoot()), nodes.Current.Fragment.HeaderRange);
                    elseBlock = QsNullable<QsPositionedBlock>.NewValue(
                        new QsPositionedBlock(scope, QsNullable<QsLocation>.NewValue(elseLocation), nodes.Current.Fragment.Comments));
                    proceed = nodes.MoveNext();
                }

                var (ifStatement, ifDiagnostics) =
                    Statements.NewIfStatement(context, ifBlock.Item1, ifBlock.Item2, elifBlocks, elseBlock);
                statement = ifStatement;
                diagnostics.AddRange(ifDiagnostics.Select(item =>
                {
                    var (relativeOffset, diagnostic) = item;
                    var offset = DiagnosticTools.GetAbsolutePosition(rootPosition, relativeOffset);
                    return Diagnostics.Generate(context.Symbols.SourceFile.Value, diagnostic, offset);
                }));
                return true;
            }
            (statement, proceed) = (null, true);
            return false;
        }

        /// <summary>
        /// If the current tree node of the given iterator is a within-block intro,
        /// builds the corresponding conjugation updating the given scope context in the process,
        /// and moves the iterator to the next node.
        /// Adds the diagnostics generated during the building to the given list of diagnostics.
        /// Returns the built statement as out parameter, and returns true if the statement has been built.
        /// Sets the out parameter to null and returns false otherwise.
        /// Sets the boolean out parameter to true, if the iterator contains another node at the end of the routine -
        /// i.e. it is set to true if either the iterator has not been moved (no statement built),
        /// or if the last MoveNext() returned true, and is otherwise set to false.
        /// This routine will fail if accessing the current iterator item fails.
        /// Throws an ArgumentNullException if any of the given arguments is null.
        /// Throws an ArgumentException if the given scope context does not currently contain an open scope.
        /// </summary>
        private static bool TryBuildConjugationStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext<Position> context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            out QsStatement statement)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }
            if (diagnostics == null)
            {
                throw new ArgumentException(nameof(diagnostics));
            }

            QsNullable<QsLocation> RelativeLocation(FragmentTree.TreeNode node) =>
                QsNullable<QsLocation>.NewValue(new QsLocation(DiagnosticTools.AsTuple(node.GetPositionRelativeToRoot()), node.Fragment.HeaderRange));

            if (nodes.Current.Fragment.Kind.IsWithinBlockIntro)
            {
                // The requirement for outer blocks in conjugations is always that an adjoint can be auto-generated for them,
                // independent on what functor specializations need to be auto-generated for the containing operation.
                var requiredFunctorSupport = ImmutableHashSet.Create(QsFunctor.Adjoint);
                var outerTranformation = BuildScope(nodes.Current.Children, context, diagnostics, requiredFunctorSupport);
                var outer = new QsPositionedBlock(outerTranformation, RelativeLocation(nodes.Current), nodes.Current.Fragment.Comments);

                if (nodes.MoveNext() && nodes.Current.Fragment.Kind.IsApplyBlockIntro)
                {
                    var innerTransformation = BuildScope(nodes.Current.Children, context, diagnostics);
                    var inner = new QsPositionedBlock(innerTransformation, RelativeLocation(nodes.Current), nodes.Current.Fragment.Comments);
                    var built = Statements.NewConjugation(outer, inner);
                    diagnostics.AddRange(built.Item2.Select(item =>
                    {
                        var (relativeOffset, diagnostic) = item;
                        var offset = DiagnosticTools.GetAbsolutePosition(nodes.Current.GetRootPosition(), relativeOffset);
                        return Diagnostics.Generate(context.Symbols.SourceFile.Value, diagnostic, offset);
                    }));

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
        /// If the current tree node of the given iterator is a let-statement,
        /// builds the corresponding let-statement updating the given scope context in the process,
        /// and moves the iterator to the next node.
        /// Adds the diagnostics generated during the building to the given list of diagnostics.
        /// Returns the built statement as out parameter, and returns true if the statement has been built.
        /// Sets the out parameter to null and returns false otherwise.
        /// Sets the boolean out parameter to true, if the iterator contains another node at the end of the routine -
        /// i.e. it is set to true if either the iterator has not been moved (no statement built),
        /// or if the last MoveNext() returned true, and is otherwise set to false.
        /// This routine will fail if accessing the current iterator item fails.
        /// Throws an ArgumentNullException if any of the given arguments is null.
        /// Throws an ArgumentException if the given scope context does not currently contain an open scope.
        /// </summary>
        private static bool TryBuildLetStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext<Position> context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            out QsStatement statement)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }
            if (diagnostics == null)
            {
                throw new ArgumentException(nameof(diagnostics));
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
        /// If the current tree node of the given iterator is a mutable-statement,
        /// builds the corresponding mutable-statement updating the given scope context in the process,
        /// and moves the iterator to the next node.
        /// Adds the diagnostics generated during the building to the given list of diagnostics.
        /// Returns the built statement as out parameter, and returns true if the statement has been built.
        /// Sets the out parameter to null and returns false otherwise.
        /// Sets the boolean out parameter to true, if the iterator contains another node at the end of the routine -
        /// i.e. it is set to true if either the iterator has not been moved (no statement built),
        /// or if the last MoveNext() returned true, and is otherwise set to false.
        /// This routine will fail if accessing the current iterator item fails.
        /// Throws an ArgumentNullException if any of the given arguments is null.
        /// Throws an ArgumentException if the given scope context does not currently contain an open scope.
        /// </summary>
        private static bool TryBuildMutableStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext<Position> context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            out QsStatement statement)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }
            if (diagnostics == null)
            {
                throw new ArgumentException(nameof(diagnostics));
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
        /// If the current tree node of the given iterator is a set-statement,
        /// builds the corresponding set-statement updating the given scope context in the process,
        /// and moves the iterator to the next node.
        /// Adds the diagnostics generated during the building to the given list of diagnostics.
        /// Returns the built statement as out parameter, and returns true if the statement has been built.
        /// Sets the out parameter to null and returns false otherwise.
        /// Sets the boolean out parameter to true, if the iterator contains another node at the end of the routine -
        /// i.e. it is set to true if either the iterator has not been moved (no statement built),
        /// or if the last MoveNext() returned true, and is otherwise set to false.
        /// This routine will fail if accessing the current iterator item fails.
        /// Throws an ArgumentNullException if any of the given arguments is null.
        /// Throws an ArgumentException if the given scope context does not currently contain an open scope.
        /// </summary>
        private static bool TryBuildSetStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext<Position> context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            out QsStatement statement)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }
            if (diagnostics == null)
            {
                throw new ArgumentException(nameof(diagnostics));
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
        /// If the current tree node of the given iterator is a fail-statement,
        /// builds the corresponding fail-statement updating the given scope context in the process,
        /// and moves the iterator to the next node.
        /// Adds the diagnostics generated during the building to the given list of diagnostics.
        /// Returns the built statement as out parameter, and returns true if the statement has been built.
        /// Sets the out parameter to null and returns false otherwise.
        /// Sets the boolean out parameter to true, if the iterator contains another node at the end of the routine -
        /// i.e. it is set to true if either the iterator has not been moved (no statement built),
        /// or if the last MoveNext() returned true, and is otherwise set to false.
        /// This routine will fail if accessing the current iterator item fails.
        /// Throws an ArgumentNullException if any of the given arguments is null.
        /// Throws an ArgumentException if the given scope context does not currently contain an open scope.
        /// </summary>
        private static bool TryBuildFailStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext<Position> context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            out QsStatement statement)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }
            if (diagnostics == null)
            {
                throw new ArgumentException(nameof(diagnostics));
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
        /// If the current tree node of the given iterator is a return-statement,
        /// builds the corresponding return-statement updating the given scope context in the process,
        /// and moves the iterator to the next node.
        /// Adds the diagnostics generated during the building to the given list of diagnostics.
        /// Returns the built statement as out parameter, and returns true if the statement has been built.
        /// Sets the out parameter to null and returns false otherwise.
        /// Sets the boolean out parameter to true, if the iterator contains another node at the end of the routine -
        /// i.e. it is set to true if either the iterator has not been moved (no statement built),
        /// or if the last MoveNext() returned true, and is otherwise set to false.
        /// This routine will fail if accessing the current iterator item fails.
        /// Throws an ArgumentNullException if any of the given arguments is null.
        /// Throws an ArgumentException if the given scope context does not currently contain an open scope.
        /// </summary>
        private static bool TryBuildReturnStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext<Position> context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            out QsStatement statement)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }
            if (diagnostics == null)
            {
                throw new ArgumentException(nameof(diagnostics));
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
        /// If the current tree node of the given iterator is an expression-statement,
        /// builds the corresponding expression-statement updating the given scope context in the process,
        /// and moves the iterator to the next node.
        /// Adds the diagnostics generated during the building to the given list of diagnostics.
        /// Returns the built statement as out parameter, and returns true if the statement has been built.
        /// Sets the out parameter to null and returns false otherwise.
        /// Sets the boolean out parameter to true, if the iterator contains another node at the end of the routine -
        /// i.e. it is set to true if either the iterator has not been moved (no statement built),
        /// or if the last MoveNext() returned true, and is otherwise set to false.
        /// This routine will fail if accessing the current iterator item fails.
        /// Throws an ArgumentNullException if any of the given arguments is null.
        /// Throws an ArgumentException if the given scope context does not currently contain an open scope.
        /// </summary>
        private static bool TryBuildExpressionStatement(
            IEnumerator<FragmentTree.TreeNode> nodes,
            ScopeContext<Position> context,
            List<Diagnostic> diagnostics,
            out bool proceed,
            out QsStatement statement)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }
            if (diagnostics == null)
            {
                throw new ArgumentException(nameof(diagnostics));
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
        /// Given a sequence of tree nodes, builds the corrsponding array of Q# statements (ignoring invalid fragments)
        /// using and updating the given scope context and adding the generated diagnostics to the given list of diagnostics,
        /// provided each statement consists of a suitable statement header followed by the required continuation(s), if any.
        /// Throws an ArgumentException if this is not the case,
        /// or if the given scope context does not currently contain an open scope.
        /// Throws an ArgumentNullException if any of the given arguments is null,
        /// or if any of the fragments contained in the given nodes is null.
        /// </summary>
        private static ImmutableArray<QsStatement> BuildStatements(
            IEnumerator<FragmentTree.TreeNode> nodes, ScopeContext<Position> context, List<Diagnostic> diagnostics)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (context.Symbols.AllScopesClosed)
            {
                throw new ArgumentException("invalid scope context state - statements may only occur within a scope");
            }
            if (diagnostics == null)
            {
                throw new ArgumentException(nameof(diagnostics));
            }

            var proceed = nodes.MoveNext();
            var statements = new List<QsStatement>();
            while (proceed)
            {
                if (nodes.Current.Fragment?.Kind == null)
                {
                    throw new ArgumentNullException(nameof(nodes.Current.Fragment), "fragment kind cannot be null");
                }
                else if (TryBuildExpressionStatement(nodes, context, diagnostics, out proceed, out QsStatement expressionStatement))
                {
                    statements.Add(expressionStatement);
                }
                else if (TryBuildLetStatement(nodes, context, diagnostics, out proceed, out QsStatement letStatement))
                {
                    statements.Add(letStatement);
                }
                else if (TryBuildMutableStatement(nodes, context, diagnostics, out proceed, out QsStatement mutableStatement))
                {
                    statements.Add(mutableStatement);
                }
                else if (TryBuildSetStatement(nodes, context, diagnostics, out proceed, out QsStatement setStatement))
                {
                    statements.Add(setStatement);
                }
                else if (TryBuildFailStatement(nodes, context, diagnostics, out proceed, out QsStatement failStatement))
                {
                    statements.Add(failStatement);
                }
                else if (TryBuildReturnStatement(nodes, context, diagnostics, out proceed, out QsStatement returnStatement))
                {
                    statements.Add(returnStatement);
                }
                else if (TryBuildForStatement(nodes, context, diagnostics, out proceed, out QsStatement forStatement))
                {
                    statements.Add(forStatement);
                }
                else if (TryBuildWhileStatement(nodes, context, diagnostics, out proceed, out QsStatement whileStatement))
                {
                    statements.Add(whileStatement);
                }
                else if (TryBuildIfStatement(nodes, context, diagnostics, out proceed, out QsStatement ifStatement))
                {
                    statements.Add(ifStatement);
                }
                else if (TryBuildRepeatStatement(nodes, context, diagnostics, out proceed, out QsStatement repeatStatement))
                {
                    statements.Add(repeatStatement);
                }
                else if (TryBuildConjugationStatement(nodes, context, diagnostics, out proceed, out QsStatement conjugationStatement))
                {
                    statements.Add(conjugationStatement);
                }
                else if (TryBuildBorrowStatement(nodes, context, diagnostics, out proceed, out QsStatement borrowingStatement))
                {
                    statements.Add(borrowingStatement);
                }
                else if (TryBuildUsingStatement(nodes, context, diagnostics, out proceed, out QsStatement usingStatement))
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
        /// Builds the user defined implementation based on the (children of the) given specialization root.
        /// The implementation takes the given argument tuple as argument and needs to support auto-generation for the specified set of functors.
        /// Uses the given scope context to resolve the symbols used within the implementation, and generates suitable diagnostics in the process.
        /// If necessary, generates suitable diagnostics for functor arguments (only!), which are discriminated by the missing position information
        /// for argument variables defined in the callable declaration).
        /// If the expected return type for the specialization is not Unit, verifies that all paths return a value or fail, generating suitable diagnostics.
        /// Adds the generated diagnostics to the given list of diagnostics.
        /// Throws an ArgumentNullException if the given argument, the scope context, or diagnostics are null.
        /// </summary>
        private static SpecializationImplementation BuildUserDefinedImplementation(
            FragmentTree.TreeNode root,
            NonNullable<string> sourceFile,
            QsTuple<LocalVariableDeclaration<QsLocalSymbol>> argTuple,
            ImmutableHashSet<QsFunctor> requiredFunctorSupport,
            ScopeContext<Position> context,
            List<Diagnostic> diagnostics)
        {
            if (argTuple == null)
            {
                throw new ArgumentNullException(nameof(argTuple));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (requiredFunctorSupport == null)
            {
                throw new ArgumentNullException(nameof(requiredFunctorSupport));
            }
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            // the variable defined on the declaration need to be verified upon building the callable (otherwise we get duplicate diagnostics),
            // but they need to be pushed *first* such that we get suitable re-declaration errors on those defined on the specialization
            // -> the position information is set to null (only) for variables defined in the declaration
            var (variablesOnDeclation, variablesOnSpecialization) = SyntaxGenerator.ExtractItems(argTuple).Partition(decl => decl.Position.IsNull);

            context.Symbols.BeginScope(requiredFunctorSupport);
            foreach (var decl in variablesOnDeclation)
            {
                context.Symbols.TryAddVariableDeclartion(decl);
            }

            var specPos = root.Fragment.GetRange().Start;
            foreach (var decl in variablesOnSpecialization)
            {
                var msgs = context.Symbols.TryAddVariableDeclartion(decl).Item2;
                var position = specPos.Add(DiagnosticTools.AsPosition(decl.Position.Item));
                diagnostics.AddRange(msgs.Select(msg => Diagnostics.Generate(sourceFile.Value, msg, position)));
            }

            var implementation = BuildScope(root.Children, context, diagnostics);
            context.Symbols.EndScope();

            // verify that all paths return a value if needed (or fail)
            var (allPathsReturn, messages) = SyntaxProcessing.SyntaxTree.AllPathsReturnValueOrFail(implementation);
            var rootPosition = root.Fragment.GetRange().Start;
            Position AsAbsolutePosition(Tuple<int, int> statementPos) => rootPosition.Add(DiagnosticTools.AsPosition(statementPos));
            diagnostics.AddRange(messages.Select(msg => Diagnostics.Generate(sourceFile.Value, msg.Item2, AsAbsolutePosition(msg.Item1))));
            if (!(context.ReturnType.Resolution.IsUnitType || context.ReturnType.Resolution.IsInvalidType) && !allPathsReturn)
            {
                var errRange = Parsing.HeaderDelimiters(root.Fragment.Kind.IsControlledAdjointDeclaration ? 2 : 1).Invoke(root.Fragment.Text);
                var missingReturn = new QsCompilerDiagnostic(DiagnosticItem.NewError(ErrorCode.MissingReturnOrFailStatement), Enumerable.Empty<string>(), errRange);
                diagnostics.Add(Diagnostics.Generate(sourceFile.Value, missingReturn, specPos));
            }
            return SpecializationImplementation.NewProvided(argTuple, implementation);
        }

        /// <summary>
        /// Given a function that returns the generator directive for a given specialization kind, or null if none has been defined for that kind,
        /// determines the necessary functor support required for each operation call within a user defined implementation of the specified specialization.
        /// Throws an ArgumentNullException if the given specialization for which to determine the required functor support is null,
        /// or if the given function to query the directives is.
        /// </summary>
        private static IEnumerable<QsFunctor> RequiredFunctorSupport(
            QsSpecializationKind spec,
            Func<QsSpecializationKind, QsGeneratorDirective> directives)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }
            if (directives == null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

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
        /// Given the root of a specialization declaration, the signature of the callable it belongs to,
        /// as well as the argument tuple of that callable, builds and returns the corresponding specialization.
        /// If the given root is a callable declaration, a default body specialization with its children as the implementation is returned -
        /// provided the children are exclusively valid statements. Fails with the corresponding exception otherwise.
        /// Adds the generated diagnostics to the given list of diagnostics.
        /// Throws an ArgumentNullException if the parent signature, its argument tuple,
        /// the compilation unit, or the given list of diagnostics is null.
        /// Throws an ArgumentException if the given root is neither a specialization declaration, nor a callable declaration,
        /// or if the callable the specialization belongs to does not support that specialization according to the given NamespaceManager.
        /// </summary>
        private static ImmutableArray<QsSpecialization> BuildSpecializations(
            FragmentTree specsRoot,
            ResolvedSignature parentSignature,
            QsTuple<LocalVariableDeclaration<QsLocalSymbol>> argTuple,
            CompilationUnit compilation,
            List<Diagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            if (parentSignature == null)
            {
                throw new ArgumentNullException(nameof(parentSignature));
            }
            if (argTuple == null)
            {
                throw new ArgumentNullException(nameof(argTuple));
            }
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }
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
                    QsComments comments = null) =>
                new QsSpecialization(
                    spec.Kind,
                    spec.Parent,
                    spec.Attributes,
                    spec.SourceFile,
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
                Func<QsSymbol, Tuple<QsTuple<LocalVariableDeclaration<QsLocalSymbol>>, QsCompilerDiagnostic[]>> buildArg,
                QsComments comments = null)
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
                    var specPos = root.Fragment.GetRange().Start;
                    var (arg, messages) = buildArg(userDefined.Item);
                    foreach (var msg in messages)
                    {
                        diagnostics.Add(Diagnostics.Generate(spec.SourceFile.Value, msg, specPos));
                    }

                    QsGeneratorDirective GetDirective(QsSpecializationKind k) => definedSpecs.TryGetValue(k, out defined) && defined.Item1.IsValue ? defined.Item1.Item : null;
                    var requiredFunctorSupport = RequiredFunctorSupport(kind, GetDirective).ToImmutableHashSet();
                    var context = ScopeContext<Position>.Create(
                        compilation.GlobalSymbols,
                        compilation.RuntimeCapabilities,
                        compilation.ProcessorArchitecture,
                        spec);
                    implementation = BuildUserDefinedImplementation(
                        root, spec.SourceFile, arg, requiredFunctorSupport, context, diagnostics);
                    QsCompilerError.Verify(context.Symbols.AllScopesClosed, "all scopes should be closed");
                }
                implementation = implementation ?? SpecializationImplementation.Intrinsic;
                return GetSpecialization(spec, signature, implementation, comments);
            }

            var parentCharacteristics = parentSignature.Information.Characteristics; // note that if this one is invalid, the corresponding specializations are still compiled
            var supportedFunctors = parentCharacteristics.SupportedFunctors.ValueOr(ImmutableHashSet<QsFunctor>.Empty);
            var ctlArgPos = QsNullable<Tuple<int, int>>.NewValue(DiagnosticTools.AsTuple(new Position())); // position relative to the start of the specialization, i.e. zero-position
            var controlledSignature = SyntaxGenerator.BuildControlled(parentSignature);

            QsSpecialization BuildSpec(FragmentTree.TreeNode root)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }
                bool InvalidCharacteristicsOrSupportedFunctors(params QsFunctor[] functors) =>
                    parentCharacteristics.AreInvalid || !functors.Any(f => !supportedFunctors.Contains(f));
                if (!definedSpecs.Values.Any(d => d.Item2.Position is DeclarationHeader.Offset.Defined pos && DiagnosticTools.AsPosition(pos.Item) == root.Fragment.GetRange().Start))
                {
                    return null; // only process specializations that are valid
                }

                if (FileHeader.IsCallableDeclaration(root.Fragment))
                {
                    // no specializations have been defined -> one default body
                    var genArg = new QsSymbol(QsSymbolKind<QsSymbol>.OmittedSymbols, QsRangeInfo.Null);
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
            existing.AddRange(specsRoot.Specializations.Select(BuildSpec).Where(s => s != null));
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
        /// Given access to a NamespaceManager containing all global declarations and their resolutions via a CompilationUnit,
        /// type checks each of the given FragmentTrees until the process is cancelled via the given cancellation token.
        /// For each namespace and callable name that occurs in the given FragmentTrees builds the corresponding QsCallable.
        /// Updates the given CompilationUnit with all built callables. Checks all types defined in the NamespaceManager for cycles.
        /// Returns a list with all accumulated diagnostics. If the request has been cancelled, returns null.
        /// Throws an ArgumentNullException if any of the arguments is null.
        /// </summary>
        internal static List<Diagnostic> RunTypeChecking(
            CompilationUnit compilation,
            ImmutableDictionary<QsQualifiedName, (QsComments, FragmentTree)> roots,
            CancellationToken cancellationToken)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }
            if (roots == null)
            {
                throw new ArgumentNullException(nameof(roots));
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

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

                QsCallable GetCallable((QsQualifiedName, ImmutableArray<QsSpecialization>) specItems)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return null;
                    }
                    var (parent, specs) = specItems;
                    var info = callableDeclarations[parent];
                    var declaredVariables = SyntaxGenerator.ExtractItems(info.ArgumentTuple);

                    // verify the variable declarations in the callable declaration
                    var symbolTracker = new SymbolTracker<Position>(compilation.GlobalSymbols, info.SourceFile, parent); // only ever used to verify declaration args
                    symbolTracker.BeginScope();
                    foreach (var decl in declaredVariables)
                    {
                        var offset = info.Position is DeclarationHeader.Offset.Defined pos ? pos.Item : null;
                        QsCompilerError.Verify(offset != null, "missing position information for built callable");
                        var msgs = symbolTracker.TryAddVariableDeclartion(decl).Item2
                            .Select(msg => Diagnostics.Generate(info.SourceFile.Value, msg, DiagnosticTools.AsPosition(offset)));
                        diagnostics.AddRange(msgs);
                    }
                    symbolTracker.EndScope();
                    QsCompilerError.Verify(symbolTracker.AllScopesClosed, "all scopes should be closed");
                    return new QsCallable(
                        info.Kind,
                        parent,
                        info.Attributes,
                        info.Modifiers,
                        info.SourceFile,
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
                    decl.Value.Modifiers,
                    decl.Value.SourceFile,
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
                return diagnostics;
            }
            finally
            {
                compilation.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// This method serves two entirely independent purposes since they require the exact same logic, despite what I would usually consider good practice.
        /// The first purpose is the following:
        /// Returns all locally declared symbols visible at the given relative position,
        /// assuming that the position corresponds to a piece of code within the given scope.
        /// If includeDeclaredAtPosition is set to true, then this includes the symbols declared within the statement at the specified position,
        /// even if those symbols are *not* visible after the statement ends (e.g. for-loops or qubit allocations).
        /// Note that if the given position does not correspond to a piece of code but rather to whitespace possibly after a scope ending,
        /// the returned declarations are not necessarily accurate - they are for any actual piece of code, though.
        /// The second purpose is the following:
        /// Returns the statements that follow a local declaration at the given relative position,
        /// i.e. the statements for which local variables declared at that position are defined.
        /// Whether the given relative position is indeed within a statement that declares local variables is not verified.
        /// It is important to note that the returned statements are *not* necessarily the set of statements
        /// for which the returned set of local variables are valid!
        /// The given relative position is expected to be relative to the beginning of the specialization declaration -
        /// or rather to be consistent with the position information saved for statements.
        /// Throws an ArgumentException if any of the statements contained in the given scope is not annotated with a valid position,
        /// or if the given relative position is not a valid position.
        /// </summary>
        private static (LocalDeclarations, IEnumerable<QsStatement>) StatementsAfterAndLocalDeclarationsAt(this QsScope scope, Position relativePosition, bool includeDeclaredAtPosition)
        {
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }
            if (!Utils.IsValidPosition(relativePosition))
            {
                throw new ArgumentException(nameof(relativePosition));
            }

            LocalDeclarations Concat(LocalDeclarations fst, LocalDeclarations snd)
                => new LocalDeclarations(fst.Variables.Concat(snd.Variables).ToImmutableArray());
            bool BeforePosition(QsNullable<QsLocation> location) =>
                location.IsValue && DiagnosticTools.AsPosition(location.Item.Offset).IsSmallerThan(relativePosition);
            bool StartsBeforePosition(QsScope body) => body.Statements.Any() && BeforePosition(body.Statements.First().Location);

            var precedingStatements = scope.Statements.TakeWhile(stm => BeforePosition(stm.Location)).ToImmutableArray();
            if (precedingStatements.Length == 0)
            {
                return (scope.KnownSymbols, scope.Statements);
            }
            var lastPreceding = precedingStatements[precedingStatements.Length - 1];

            QsScope relevantScope = null;
            if (lastPreceding.Statement is QsStatementKind.QsConditionalStatement condStatement)
            {
                var blocks = condStatement.Item.ConditionalBlocks.Select(block => block.Item2);
                var elseBlock = condStatement.Item.Default.ValueOr(null);
                if (elseBlock != null)
                {
                    blocks = blocks.Concat(new[] { elseBlock });
                }

                var preceding = blocks.TakeWhile(block => BeforePosition(block.Location));
                relevantScope = preceding.Any() ? preceding.Last().Body : null;
            }
            if (lastPreceding.Statement is QsStatementKind.QsForStatement forStatement)
            {
                relevantScope = forStatement.Item.Body;
            }
            if (lastPreceding.Statement is QsStatementKind.QsWhileStatement whileStatement)
            {
                relevantScope = whileStatement.Item.Body;
            }
            if (lastPreceding.Statement is QsStatementKind.QsRepeatStatement repeatStatement)
            {
                var allContainedStatements = repeatStatement.Item.RepeatBlock.Body.Statements.Concat(repeatStatement.Item.FixupBlock.Body.Statements).ToImmutableArray();
                relevantScope = new QsScope(allContainedStatements, repeatStatement.Item.RepeatBlock.Body.KnownSymbols);
            }
            if (lastPreceding.Statement is QsStatementKind.QsConjugation conjugation)
            {
                relevantScope = BeforePosition(conjugation.Item.InnerTransformation.Location)
                    ? conjugation.Item.InnerTransformation.Body
                    : conjugation.Item.OuterTransformation.Body;
            }
            if (lastPreceding.Statement is QsStatementKind.QsQubitScope allocationScope)
            {
                relevantScope = allocationScope.Item.Body;
            }

            if (relevantScope != null && StartsBeforePosition(relevantScope))
            {
                // the relative position is truly within the child scope
                return relevantScope.StatementsAfterAndLocalDeclarationsAt(relativePosition, includeDeclaredAtPosition);
            }

            LocalDeclarations AggregateLocalDeclarations(IEnumerable<QsStatement> stms) =>
                stms.Aggregate(scope.KnownSymbols, (decl, statement) => Concat(decl, statement.SymbolDeclarations));
            var symbols = includeDeclaredAtPosition
                ? relevantScope != null ? relevantScope.KnownSymbols : AggregateLocalDeclarations(precedingStatements)
                : AggregateLocalDeclarations(precedingStatements.Take(precedingStatements.Length - 1));
            var statementsAfterDecl = relevantScope == null
                ? scope.Statements.SkipWhile(stm => BeforePosition(stm.Location))
                : relevantScope.Statements;
            return (symbols, statementsAfterDecl);
        }

        /// <summary>
        /// Returns the statements that follow a local declaration at the given relative position,
        /// i.e. the statements for which local variables declared at that position are defined.
        /// Whether the given relative position is indeed within a statement that declares local variables is not verified.
        /// The given relative position is expected to be relative to the beginning of the specialization declaration -
        /// or rather to be consistent with the position information saved for statements.
        /// Throws an ArgumentException if any of the statements contained in the given scope is not annotated with a valid position,
        /// or if the given relative position is not a valid position.
        /// </summary>
        internal static IEnumerable<QsStatement> StatementsAfterDeclaration(this QsScope scope, Position relativePosition) =>
            StatementsAfterAndLocalDeclarationsAt(scope, relativePosition, false).Item2;

        /// <summary>
        /// Returns all locally declared symbols visible at the given relative position,
        /// assuming that the position corresponds to a piece of code within the given scope.
        /// If includeDeclaredAtPosition is set to true, then this includes the symbols declared within the statement at the specified position,
        /// even if those symbols are *not* visible after the statement ends (e.g. for-loops or qubit allocations).
        /// The given relative position is expected to be relative to the beginning of the specialization declaration -
        /// or rather to be consistent with the position information saved for statements.
        /// Note that if the given position does not correspond to a piece of code but rather to whitespace possibly after a scope ending,
        /// the returned declarations are not necessarily accurate - they are for any actual piece of code, though.
        /// Throws an ArgumentException if any of the statements contained in the given scope is not annotated with a valid position,
        /// or if the given relative position is not a valid position.
        /// </summary>
        internal static LocalDeclarations LocalDeclarationsAt(this QsScope scope, Position relativePosition, bool includeDeclaredAtPosition) =>
            StatementsAfterAndLocalDeclarationsAt(scope, relativePosition, includeDeclaredAtPosition).Item1;

        /// <summary>
        /// Returns all locally declared symbols visible at the given relative position,
        /// assuming that the position corresponds to a piece of code within the given scope.
        /// The given relative position is expected to be relative to the beginning of the specialization declaration -
        /// or rather to be consistent with the position information saved for statements.
        /// If the given position lays outside a piece of code e.g. after a scope ending the returned declarations may be inaccurate.
        /// Throws an ArgumentException if any of the statements contained in the given scope is not annotated with a valid position,
        /// or if the given relative position is not a valid position.
        /// </summary>
        public static LocalDeclarations LocalDeclarationsAt(this QsScope scope, Position relativePosition) =>
            StatementsAfterAndLocalDeclarationsAt(scope, relativePosition, false).Item1;

        /// <summary>
        /// Recomputes the globally defined symbols within the given file and updates the Symbols in the given compilation unit accordingly.
        /// Replaces all header diagnostics in the given file with the diagnostics generated during the symbol update.
        /// If neither the globally declared types/callables nor the imported namespaces have changed,
        /// directly proceeds to do the type checking for the content of the callable that has been modified only.
        /// If the globally declared types/callables have not changed, but the imported namespaces have,
        /// directly proceeds to do the type checking for the entire file content, independent on what parts have been changed.
        /// If the globally declared types/callables have changed, a global type checking event is triggered,
        /// since the type checking for the entire compilation unit and all compilation units depending on it needs to be recomputed.
        /// Throws an ArgumentNullException if the given file or compilation unit is null.
        /// </summary>
        internal static void UpdateTypeChecking(this FileContentManager file, CompilationUnit compilation)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

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
                ResolveGlobalSymbols(compilation.GlobalSymbols, diagnostics, file.FileName.Value);
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
                if (sameImports)
                {
                    file.AddAndFinalizeSemanticDiagnostics(diagnostics); // diagnostics have been cleared already for the edited callables (only)
                }
                else
                {
                    file.ReplaceSemanticDiagnostics(diagnostics);
                }
            }
            finally
            {
                file.SyncRoot.ExitUpgradeableReadLock();
                compilation.ExitWriteLock();
            }
        }
    }
}
