// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.EditorSupport;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Position = Microsoft.Quantum.QsCompiler.DataTypes.Position;
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    using QsTypeKind = QsTypeKind<QsType, QsSymbol, QsSymbol, Characteristics>;

    /// <summary>
    /// This static class contains utils for getting the necessary information for editor commands.
    /// </summary>
    internal static class SymbolInfo
    {
        /* utils for getting the necessary information for editor commands */

        internal static Location AsLocation(string source, Position offset, Range relRange) =>
            new Location
            {
                Uri = CompilationUnitManager.TryGetUri(source, out var uri) ? uri : throw new Exception($"Source location {source} could not be converted to a valid URI."),
                Range = (offset + relRange).ToLsp(),
            };

        internal static Location AsLocation(IdentifierReferences.Location loc) =>
            AsLocation(loc.SourceFile, loc.DeclarationOffset + loc.RelativeStatementLocation.Offset, loc.SymbolRange);

        /// <summary>
        /// Returns the <see cref="SymbolInformation"/> for all namespace declarations in <paramref name="file"/>.
        /// </summary>
        public static IEnumerable<SymbolInformation> NamespaceDeclarationsSymbolInfo(this FileContentManager file) =>
            file.GetNamespaceDeclarations().Select(tuple => new SymbolInformation
            {
                Name = tuple.Item1,
                ContainerName = "Namespace Declarations",
                Kind = SymbolKind.Namespace,
                Location = new Location { Uri = file.Uri, Range = tuple.Item2.ToLsp() },
            });

        /// <summary>
        /// Returns the <see cref="SymbolInformation"/> for all type declarations in <paramref name="file"/>.
        /// </summary>
        public static IEnumerable<SymbolInformation> TypeDeclarationsSymbolInfo(this FileContentManager file) =>
            file.GetTypeDeclarations().Select(tuple => new SymbolInformation
            {
                Name = tuple.Item1,
                ContainerName = "Type Declarations",
                Kind = SymbolKind.Struct,
                Location = new Location { Uri = file.Uri, Range = tuple.Item2.ToLsp() },
            });

        /// <summary>
        /// Returns the <see cref="SymbolInformation"/> for all method declarations in <paramref name="file"/>.
        /// </summary>
        public static IEnumerable<SymbolInformation> CallableDeclarationsSymbolInfo(this FileContentManager file) =>
            file.GetCallableDeclarations().Select(tuple => new SymbolInformation
            {
                Name = tuple.Item1,
                ContainerName = "Operation and Function Declarations",
                Kind = SymbolKind.Method,
                Location = new Location { Uri = file.Uri, Range = tuple.Item2.ToLsp() },
            });

        /// <summary>
        /// Returns the symbol occurrence that overlaps with <paramref name="position"/> in <paramref name="fragment"/>.
        /// </summary>
        /// <param name="fragment">The fragment to look in.</param>
        /// <param name="position">The position to look at.</param>
        /// <param name="includeEnd">
        /// True if an overlapping symbol's end position can be equal to <paramref name="position"/>.
        /// </param>
        /// <returns>The overlapping occurrence.</returns>
        internal static SymbolOccurrence? SymbolOccurrence(CodeFragment fragment, Position position, bool includeEnd)
        {
            return fragment.Kind is null
                ? null
                : SymbolOccurrenceModule.InFragment(fragment.Kind).SingleOrDefault(OccurrenceOverlaps);

            bool OccurrenceOverlaps(SymbolOccurrence occurrence) => occurrence.Match(
                declaration: s => RangeOverlaps(s.Range),
                usedType: t => RangeOverlaps(t.Range),
                usedVariable: s => RangeOverlaps(s.Range),
                usedLiteral: e => RangeOverlaps(e.Range));

            bool RangeOverlaps(QsNullable<Range> range)
            {
                if (range.IsNull)
                {
                    return false;
                }

                var absolute = fragment.Range.Start + range.Item;
                return includeEnd ? absolute.ContainsEnd(position) : absolute.Contains(position);
            }
        }

        /// <summary>
        /// Searches <paramref name="compilation"/> for all references to a globally defined type or callable with <paramref name="fullName"/>.
        /// </summary>
        /// <param name="referenceLocations">
        /// The reference locations where type or callable <paramref name="fullName"/> is defined,
        /// or null if the declaration is not within <paramref name="compilation"/>, limited by <paramref name="limitToSourceFiles"/>.
        /// </param>
        /// <param name="limitToSourceFiles">Limit the search to these files, only.</param>
        /// <returns>
        /// True if the search completed successfully, and false otherwise.
        /// </returns>
        /// <remarks>
        /// If <paramref name="compilation"/> or <paramref name="fullName"/> is null, returns false without raising an exception.
        /// </remarks>
        private static bool TryGetReferences(
            this CompilationUnit compilation,
            QsQualifiedName? fullName,
            out Location? declarationLocation,
            [NotNullWhen(true)] out IEnumerable<Location>? referenceLocations,
            IImmutableSet<string>? limitToSourceFiles = null)
        {
            (declarationLocation, referenceLocations) = (null, null);
            if (fullName is null)
            {
                return false;
            }

            var emptyDoc = Array.Empty<string>().ToLookup(i => i, _ => ImmutableArray<string>.Empty);
            var namespaces = compilation.GetCallables()
                .ToLookup(c => c.Key.Namespace, c => c.Value)
                .Select(ns => new QsNamespace(ns.Key, ns.Select(QsNamespaceElement.NewQsCallable).ToImmutableArray(), emptyDoc));

            Tuple<string, QsLocation>? declLoc = null;
            var defaultOffset = new QsLocation(Position.Zero, Range.Zero);

            referenceLocations = namespaces
                .SelectMany(ns =>
                {
                    var references = new IdentifierReferences(fullName, defaultOffset, limitToSourceFiles);
                    references.Namespaces.OnNamespace(ns);
                    declLoc ??= references.SharedState.DeclarationLocation;
                    return references.SharedState.Locations;
                })
                .Select(AsLocation)
                .ToArray(); // ToArray is needed here to force the execution before checking declLoc

            if (!(declLoc is null))
            {
                declarationLocation = AsLocation(declLoc.Item1, declLoc.Item2.Offset, declLoc.Item2.Range);
            }

            return true;
        }

        /// <summary>
        /// Searches <paramref name="compilation"/> for all references to the identifier or type at <paramref name="position"/> in <paramref name="file"/>,
        /// and returns their locations as out parameter.
        /// </summary>
        /// <param name="referenceLocations">
        /// The reference locations where the identifier or type at <paramref name="position"/> is defined
        /// or null if the declaration is not within <paramref name="compilation"/>, limited by <paramref name="limitToSourceFiles"/>.
        /// </param>
        /// <param name="limitToSourceFiles">Limit the search to these files, only.</param>
        /// <returns>
        /// True if the search completed successfully, and false otherwise.
        /// </returns>
        /// <remarks>
        /// If <paramref name="file"/>, <paramref name="compilation"/>, or <paramref name="position"/> is null, returns false without raising an exception.
        /// </remarks>
        internal static bool TryGetReferences(
            this FileContentManager file,
            CompilationUnit compilation,
            Position position,
            out Location? declarationLocation,
            [NotNullWhen(true)] out IEnumerable<Location>? referenceLocations,
            IImmutableSet<string>? limitToSourceFiles = null)
        {
            (referenceLocations, declarationLocation) = (null, null);

            var fragment = file.TryGetFragmentAt(position, out _, true);
            if (fragment is null || fragment.Kind is QsFragmentKind.NamespaceDeclaration)
            {
                return false;
            }

            var occurrence = SymbolOccurrence(fragment, position, true);
            var sym = occurrence?.Match(
                declaration: s => s,
                usedType: t => (t.Type as QsTypeKind.UserDefinedType)?.Item,
                usedVariable: s => s,
                usedLiteral: e => null);

            if (sym == null)
            {
                return false;
            }

            var implementation = compilation.TryGetSpecializationAt(file, position, out var parentName, out var callablePos, out var specPos);
            var declarations = implementation is null
                ? null
                : SyntaxUtils.LocalsInScope(implementation, position - specPos, true);
            var locals = compilation.PositionedDeclarations(parentName, callablePos, specPos, declarations);
            var definition = locals.LocalVariable(sym);

            if (definition.IsNull)
            {
                // the given position corresponds to an identifier of a global callable
                var nsName = parentName == null ? file.TryGetNamespaceAt(position) : parentName.Namespace;
                if (nsName == null)
                {
                    return false;
                }

                var result = sym.Symbol switch
                {
                    QsSymbolKind<QsSymbol>.Symbol s => compilation.GlobalSymbols.TryResolveAndGetCallable(
                        s.Item, nsName, file.FileName),
                    QsSymbolKind<QsSymbol>.QualifiedSymbol q => compilation.GlobalSymbols.TryGetCallable(
                        new QsQualifiedName(q.Item1, q.Item2), nsName, file.FileName),
                    _ => ResolutionResult<CallableDeclarationHeader>.NotFound,
                };

                var fullName = (result as ResolutionResult<CallableDeclarationHeader>.Found)?.Item.QualifiedName;
                return compilation.TryGetReferences(fullName, out declarationLocation, out referenceLocations, limitToSourceFiles);
            }

            referenceLocations = Enumerable.Empty<Location>();
            if (limitToSourceFiles != null && !limitToSourceFiles.Contains(file.FileName))
            {
                return true;
            }

            var (defOffset, defRange) = (definition.Item.Item2, definition.Item.Item3);
            if (defOffset == callablePos)
            {
                // the given position corresponds to a variable declared as part of a callable declaration
                if (parentName is null || !compilation.GetCallables().TryGetValue(parentName, out var parent))
                {
                    return false;
                }

                referenceLocations = parent.Specializations
                    .Where(spec => spec.Source.AssemblyOrCodeFile == file.FileName)
                    .SelectMany(spec =>
                        spec.Implementation is SpecializationImplementation.Provided impl && spec.Location.IsValue
                            ? IdentifierReferences.FindInScope(file.FileName, spec.Location.Item.Offset, impl.Item2, definition.Item.Item1)
                            : ImmutableHashSet<IdentifierReferences.Location>.Empty)
                    .Select(AsLocation);
            }
            else if (implementation is null || specPos is null)
            {
                return false;
            }
            else
            {
                // The position refers to a variable declared in a specialization declaration or implementation.
                var defPosition = defOffset + defRange.Start - specPos;
                referenceLocations = StatementOrExpressionReferences(
                    file.FileName, specPos, implementation, defPosition, definition.Item.Item1, locals);
            }

            declarationLocation = AsLocation(file.FileName, definition.Item.Item2, defRange);
            return true;
        }

        private static IEnumerable<Location> StatementOrExpressionReferences(
            string file,
            Position specPosition,
            QsScope scope,
            Position identPosition,
            string ident,
            LocalDeclarations locals)
        {
            var (_, statementsBefore, statementsAfter) = SyntaxUtils.SplitStatementsByPosition(scope, identPosition);
            if (!(statementsBefore.LastOrDefault() is { Location: { IsValue: true } } statement))
            {
                return StatementReferences();
            }

            var defPositionInStatement = identPosition - statement.Location.Item.Offset;
            if (!(SyntaxUtils.FindExpressionInStatementByPosition(statement, defPositionInStatement) is { } expr))
            {
                return StatementReferences();
            }

            var location = statement.Location.IsValue ? statement.Location.Item : null;
            return IdentifierReferences.FindInExpression(file, specPosition, location, expr, ident)
                .Select(AsLocation);

            IEnumerable<Location> StatementReferences()
            {
                var scopeAfter = new QsScope(statementsAfter.ToImmutableArray(), locals);
                return IdentifierReferences.FindInScope(file, specPosition, scopeAfter, ident).Select(AsLocation);
            }
        }
    }
}
