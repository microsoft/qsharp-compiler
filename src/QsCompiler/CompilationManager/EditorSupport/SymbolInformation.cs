// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Position = Microsoft.Quantum.QsCompiler.DataTypes.Position;
using QsSymbolInfo = Microsoft.Quantum.QsCompiler.SyntaxProcessing.SyntaxExtensions.SymbolInformation;
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    /// <summary>
    /// This static class contains utils for getting the necessary information for editor commands.
    /// </summary>
    internal static class SymbolInfo
    {
        // utils for getting the necessary information for editor commands

        internal static Location AsLocation(string source, Position offset, Range relRange) =>
            new Location
            {
                Uri = CompilationUnitManager.TryGetUri(source, out var uri) ? uri : null,
                Range = (offset + relRange).ToLsp()
            };

        internal static Location AsLocation(IdentifierReferences.Location loc) =>
            AsLocation(loc.SourceFile, loc.DeclarationOffset + loc.RelativeStatementLocation.Offset, loc.SymbolRange);

        /// <summary>
        /// Returns the SymbolInformation for all namespace declarations in the file.
        /// </summary>
        public static IEnumerable<SymbolInformation> NamespaceDeclarationsSymbolInfo(this FileContentManager file) =>
            file.GetNamespaceDeclarations().Select(tuple => new SymbolInformation
            {
                Name = tuple.Item1,
                ContainerName = "Namespace Declarations",
                Kind = SymbolKind.Namespace,
                Location = new Location { Uri = file.Uri, Range = tuple.Item2.ToLsp() }
            });

        /// <summary>
        /// Returns the SymbolInformation for all type declarations in the file.
        /// </summary>
        public static IEnumerable<SymbolInformation> TypeDeclarationsSymbolInfo(this FileContentManager file) =>
            file.GetTypeDeclarations().Select(tuple => new SymbolInformation
            {
                Name = tuple.Item1,
                ContainerName = "Type Declarations",
                Kind = SymbolKind.Struct,
                Location = new Location { Uri = file.Uri, Range = tuple.Item2.ToLsp() }
            });

        /// <summary>
        /// Returns the SymbolInformation for all method declarations in the file.
        /// </summary>
        public static IEnumerable<SymbolInformation> CallableDeclarationsSymbolInfo(this FileContentManager file) =>
            file.GetCallableDeclarations().Select(tuple => new SymbolInformation
            {
                Name = tuple.Item1,
                ContainerName = "Operation and Function Declarations",
                Kind = SymbolKind.Method,
                Location = new Location { Uri = file.Uri, Range = tuple.Item2.ToLsp() }
            });

        /// <summary>
        /// Sets the out parameter to the code fragment that overlaps with the given position in the given file
        /// if such a fragment exists, or to null otherwise.
        /// If an overlapping code fragment exists, returns all symbol declarations, variable, Q# types, and Q# literals
        /// that *overlap* with the given position as Q# SymbolInformation.
        /// Returns null if no such fragment exists, or the given file and/or position is null, or the position is invalid.
        /// </summary>
        internal static QsSymbolInfo? TryGetQsSymbolInfo(
            this FileContentManager file,
            Position? position,
            bool includeEnd,
            out CodeFragment? fragment)
        {
            // getting the relevant token (if any)

            fragment = file?.TryGetFragmentAt(position, out _, includeEnd);
            if (fragment?.Kind == null)
            {
                return null;
            }
            var fragmentStart = fragment.Range.Start;

            // getting the symbol information (if any), and return the overlapping items only

            bool OverlapsWithPosition(Range symRange)
            {
                var absolute = fragmentStart + symRange;
                return includeEnd ? absolute.ContainsEnd(position) : absolute.Contains(position);
            }

            var symbolInfo = fragment.Kind.SymbolInformation();
            var overlappingDecl = symbolInfo.DeclaredSymbols.Where(sym => sym.Range.IsValue && OverlapsWithPosition(sym.Range.Item));
            QsCompilerError.Verify(overlappingDecl.Count() <= 1, "more than one declaration overlaps with the same position");
            var overlappingVariables = symbolInfo.UsedVariables.Where(sym => sym.Range.IsValue && OverlapsWithPosition(sym.Range.Item));
            QsCompilerError.Verify(overlappingVariables.Count() <= 1, "more than one variable overlaps with the same position");
            var overlappingTypes = symbolInfo.UsedTypes.Where(sym => sym.Range.IsValue && OverlapsWithPosition(sym.Range.Item));
            QsCompilerError.Verify(overlappingTypes.Count() <= 1, "more than one type overlaps with the same position");
            var overlappingLiterals = symbolInfo.UsedLiterals.Where(sym => sym.Range.IsValue && OverlapsWithPosition(sym.Range.Item));
            QsCompilerError.Verify(overlappingTypes.Count() <= 1, "more than one literal overlaps with the same position");

            return new QsSymbolInfo(
                declaredSymbols: overlappingDecl.ToImmutableHashSet(),
                usedVariables: overlappingVariables.ToImmutableHashSet(),
                usedTypes: overlappingTypes.ToImmutableHashSet(),
                usedLiterals: overlappingLiterals.ToImmutableHashSet());
        }

        /// <summary>
        /// Searches the given compilation for all references to a globally defined type or callable with the given name,
        /// and returns their locations as out parameter.
        /// If a set of source files is specified, then the search is limited to the specified files.
        /// Returns the location where that type or callable is defined as out parameter,
        /// or null if the declaration is not within this compilation unit and the files to which the search has been limited.
        /// Returns true if the search completed successfully, and false otherwise.
        /// If the given compilation unit or qualified name is null, returns false without raising an exception.
        /// </summary>
        internal static bool TryGetReferences(
            this CompilationUnit compilation,
            QsQualifiedName? fullName,
            out Location? declarationLocation,
            [NotNullWhen(true)] out IEnumerable<Location>? referenceLocations,
            IImmutableSet<string>? limitToSourceFiles = null)
        {
            (declarationLocation, referenceLocations) = (null, null);
            if (compilation == null || fullName == null)
            {
                return false;
            }

            var emptyDoc = Array.Empty<string>().ToLookup(i => i, _ => ImmutableArray<string>.Empty);
            var namespaces = compilation.GetCallables()
                .ToLookup(c => c.Key.Namespace, c => c.Value)
                .Select(ns => new QsNamespace(ns.Key, ns.Select(QsNamespaceElement.NewQsCallable).ToImmutableArray(), emptyDoc));

            Tuple<string, QsLocation>? declLoc = null;
            var defaultOffset = new QsLocation(Position.Zero, Range.Zero);
            referenceLocations = namespaces.SelectMany(ns =>
            {
                var locs = IdentifierReferences.Find(fullName, ns, defaultOffset, out var dLoc, limitToSourceFiles);
                declLoc ??= dLoc;
                return locs;
            })
            .Select(AsLocation).ToArray(); // ToArray is needed here to force the execution before checking declLoc
            declarationLocation = declLoc == null ? null : AsLocation(declLoc.Item1, declLoc.Item2.Offset, declLoc.Item2.Range);
            return true;
        }

        /// <summary>
        /// Searches the given compilation for all references to the identifier or type at the given position in the given file,
        /// and returns their locations as out parameter.
        /// If a set of source files is specified, then the search is limited to the specified files.
        /// Returns the location where that identifier or type is defined as out parameter,
        /// or null if the declaration is not within this compilation unit and the files to which the search has been limited.
        /// Returns true if the search completed successfully, and false otherwise.
        /// If the given file, compilation unit, or position is null, returns false without raising an exception.
        /// </summary>
        internal static bool TryGetReferences(
            this FileContentManager file,
            CompilationUnit compilation,
            Position position,
            out Location? declarationLocation,
            [NotNullWhen(true)] out IEnumerable<Location>? referenceLocations,
            IImmutableSet<string>? limitToSourceFiles = null)
        {
            (referenceLocations, declarationLocation) = (null, null);
            if (file == null || compilation == null)
            {
                return false;
            }
            var symbolInfo = file.TryGetQsSymbolInfo(position, true, out var fragment); // includes the end position
            if (symbolInfo == null || fragment?.Kind is QsFragmentKind.NamespaceDeclaration)
            {
                return false;
            }

            var sym = symbolInfo.UsedTypes.Any()
                && symbolInfo.UsedTypes.Single().Type is QsTypeKind<QsType, QsSymbol, QsSymbol, Characteristics>.UserDefinedType udt ? udt.Item
                : symbolInfo.UsedVariables.Any() ? symbolInfo.UsedVariables.Single()
                : symbolInfo.DeclaredSymbols.Any() ? symbolInfo.DeclaredSymbols.Single() : null;
            if (sym == null)
            {
                return false;
            }

            var implementation = compilation.TryGetSpecializationAt(file, position, out var parentName, out var callablePos, out var specPos);
            var declarations = implementation?.LocalDeclarationsAt(position - specPos, includeDeclaredAtPosition: true);
            var locals = compilation.PositionedDeclarations(parentName, callablePos, specPos, declarations);
            var definition = locals.LocalVariable(sym);

            if (definition.IsNull)
            {
                // the given position corresponds to an identifier of a global callable
                var nsName = parentName == null
                    ? file.TryGetNamespaceAt(position)
                    : parentName.Namespace;
                if (nsName == null)
                {
                    return false;
                }

                var result = ResolutionResult<CallableDeclarationHeader>.NotFound;
                if (sym.Symbol is QsSymbolKind<QsSymbol>.Symbol name)
                {
                    result = compilation.GlobalSymbols.TryResolveAndGetCallable(name.Item, nsName, file.FileName);
                }
                else if (sym.Symbol is QsSymbolKind<QsSymbol>.QualifiedSymbol qualifiedName)
                {
                    result = compilation.GlobalSymbols.TryGetCallable(
                        new QsQualifiedName(qualifiedName.Item1, qualifiedName.Item2),
                        nsName,
                        file.FileName);
                }
                var fullName = result is ResolutionResult<CallableDeclarationHeader>.Found header ? header.Item.QualifiedName : null;

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
                            ? IdentifierReferences.Find(definition.Item.Item1, impl.Item2, file.FileName, spec.Location.Item.Offset)
                            : ImmutableHashSet<IdentifierReferences.Location>.Empty)
                    .Select(AsLocation);
            }
            else if (implementation is null || specPos is null)
            {
                return false;
            }
            else
            {
                // the given position corresponds to a variable declared as part of a specialization declaration or implementation
                var defStart = defOffset + defRange.Start;
                var statements = implementation.StatementsAfterDeclaration(defStart - specPos);
                var scope = new QsScope(statements.ToImmutableArray(), locals);
                referenceLocations = IdentifierReferences.Find(definition.Item.Item1, scope, file.FileName, specPos).Select(AsLocation);
            }
            declarationLocation = AsLocation(file.FileName, definition.Item.Item2, defRange);
            return true;
        }
    }
}
