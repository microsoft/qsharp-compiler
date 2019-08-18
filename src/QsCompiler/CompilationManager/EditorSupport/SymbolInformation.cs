// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using QsSymbolInfo = Microsoft.Quantum.QsCompiler.SyntaxProcessing.SyntaxExtensions.SymbolInformation;


namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    /// <summary>
    /// This static class contains utils for getting the necessary information for editor commands.
    /// </summary>
    internal static class SymbolInfo
    {
        // utils for getting the necessary information for editor commands

        /// <summary>
        /// Throws an ArgumentNullException if the given offset or relative range is null.
        /// </summary>
        internal static Location AsLocation(NonNullable<string> source,
            Tuple<int, int> offset, Tuple<QsPositionInfo, QsPositionInfo> relRange) =>
            new Location
            {
                Uri = CompilationUnitManager.TryGetUri(source, out var uri) ? uri : null,
                Range = DiagnosticTools.GetAbsoluteRange(DiagnosticTools.AsPosition(offset), relRange)
            };

        /// <summary>
        /// Throws an ArgumentNullException if the given reference location is null.
        /// </summary>
        internal static Location AsLocation(IdentifierReferences.Location loc) =>
            AsLocation(loc.SourceFile, DiagnosticTools.StatementPosition(loc.RootNode, loc.StatementOffset), loc.SymbolRange);

        /// <summary>
        /// Returns the SymbolInformation for all namespace declarations in the file.
        /// </summary>
        public static IEnumerable<SymbolInformation> NamespaceDeclarationsSymbolInfo(this FileContentManager file) =>
            file.GetNamespaceDeclarations().Select(tuple => new SymbolInformation
            {
                Name = tuple.Item1.Value,
                ContainerName = "Namespace Declarations",
                Kind = SymbolKind.Namespace,
                Location = new Location { Uri = file.Uri, Range = tuple.Item2 }
            });

        /// <summary>
        /// Returns the SymbolInformation for all type declarations in the file.
        /// </summary>
        public static IEnumerable<SymbolInformation> TypeDeclarationsSymbolInfo(this FileContentManager file) =>
            file.GetTypeDeclarations().Select(tuple => new SymbolInformation
            {
                Name = tuple.Item1.Value,
                ContainerName = "Type Declarations",
                Kind = SymbolKind.Struct,
                Location = new Location { Uri = file.Uri, Range = tuple.Item2 }
            });

        /// <summary>
        /// Returns the SymbolInformation for all method declarations in the file.
        /// </summary>
        public static IEnumerable<SymbolInformation> CallableDeclarationsSymbolInfo(this FileContentManager file) =>
            file.GetCallableDeclarations().Select(tuple => new SymbolInformation
            {
                Name = tuple.Item1.Value,
                ContainerName = "Operation and Function Declarations",
                Kind = SymbolKind.Method,
                Location = new Location { Uri = file.Uri, Range = tuple.Item2 }
            });

        /// <summary>
        /// Sets the out parameter to the code fragment that overlaps with the given position in the given file
        /// if such a fragment exists, or to null otherwise. 
        /// If an overlapping code fragment exists, returns all symbol declarations, variable, Q# types, and Q# literals 
        /// that *overlap* with the given position as Q# SymbolInformation.
        /// Returns null if no such fragment exists, or the given file and/or position is null, or the position is invalid. 
        /// </summary>
        internal static QsSymbolInfo TryGetQsSymbolInfo(this FileContentManager file,
            Position position, bool includeEnd, out CodeFragment fragment)
        {
            // getting the relevant token (if any)

            fragment = file?.TryGetFragmentAt(position, includeEnd);
            if (fragment?.Kind == null) return null;
            var fragmentStart = fragment.GetRange().Start;

            // getting the symbol information (if any), and return the overlapping items only

            bool OverlapsWithPosition(Tuple<QsPositionInfo, QsPositionInfo> symRange) =>
                position.IsWithinRange(DiagnosticTools.GetAbsoluteRange(fragmentStart, symRange), includeEnd);

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
        internal static bool TryGetReferences(this CompilationUnit compilation, QsQualifiedName fullName,
            out Location declarationLocation, out IEnumerable<Location> referenceLocations,
            IImmutableSet<NonNullable<string>> limitToSourceFiles = null)
        {
            (declarationLocation, referenceLocations) = (null, null);
            if (compilation == null || fullName == null) return false;

            var emptyDoc = new NonNullable<string>[0].ToLookup(i => i, _ => ImmutableArray<string>.Empty);
            var namespaces = compilation.GetCallables()
                .ToLookup(c => c.Key.Namespace, c => c.Value)
                .Select(ns => new QsNamespace(ns.Key, ns.Select(QsNamespaceElement.NewQsCallable).ToImmutableArray(), emptyDoc));

            Tuple<NonNullable<string>, QsLocation> declLoc = null;
            var defaultOffset = new QsLocation(DiagnosticTools.AsTuple(new Position(0, 0)), QsCompilerDiagnostic.DefaultRange);
            referenceLocations = namespaces.SelectMany(ns =>
            {
                var locs = IdentifierReferences.Find(fullName, ns, defaultOffset, out var dLoc, limitToSourceFiles);
                declLoc = declLoc ?? dLoc;
                return locs;
            })
            .Distinct().Select(AsLocation).ToArray(); // ToArray is needed here to force the execution before checking declLoc
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
            this FileContentManager file, CompilationUnit compilation, Position position,
            out Location declarationLocation, out IEnumerable<Location> referenceLocations,
            IImmutableSet<NonNullable<string>> limitToSourceFiles = null)
        {
            (referenceLocations, declarationLocation) = (null, null);
            var symbolInfo = file?.TryGetQsSymbolInfo(position, true, out var _); // includes the end position 
            if (symbolInfo == null || compilation == null) return false;

            var sym = symbolInfo.UsedTypes.Any()
                && symbolInfo.UsedTypes.Single().Type is QsTypeKind<QsType, QsSymbol, QsSymbol, Characteristics>.UserDefinedType udt ? udt.Item
                : symbolInfo.UsedVariables.Any() ? symbolInfo.UsedVariables.Single()
                : symbolInfo.DeclaredSymbols.Any() ? symbolInfo.DeclaredSymbols.Single() : null;
            if (sym == null) return false;

            var implementation = compilation.TryGetSpecializationAt(file, position, out var parentName, out var callablePos, out var specPos);
            var declarations = implementation?.LocalDeclarationsAt(position.Subtract(specPos)).Item1;
            var locals = compilation.PositionedDeclarations(parentName, callablePos, specPos, declarations);
            var definition = locals.LocalVariable(sym);

            if (definition.IsNull) // the given position corresponds to an identifier of a global callable
            {
                var nsName = parentName == null
                    ? file.TryGetNamespaceAt(position)
                    : parentName.Namespace.Value;
                if (nsName == null) return false;
                var ns = NonNullable<string>.New(nsName);

                QsQualifiedName fullName = null;
                if (sym.Symbol is QsSymbolKind<QsSymbol>.Symbol name)
                {
                    var header = compilation.GlobalSymbols.TryResolveAndGetCallable(name.Item, ns, file.FileName).Item1;
                    if (header.IsValue) fullName = header.Item.QualifiedName;
                }
                if (sym.Symbol is QsSymbolKind<QsSymbol>.QualifiedSymbol qualName)
                {
                    var header = compilation.GlobalSymbols.TryGetCallable(new QsQualifiedName(qualName.Item1, qualName.Item2), ns, file.FileName);
                    if (header.IsValue) fullName = header.Item.QualifiedName;
                }
                return compilation.TryGetReferences(fullName, out declarationLocation, out referenceLocations, limitToSourceFiles);
            }

            referenceLocations = Enumerable.Empty<Location>();
            if (limitToSourceFiles != null && !limitToSourceFiles.Contains(file.FileName)) return true;
            var (defOffset, defRange) = (DiagnosticTools.AsPosition(definition.Item.Item2), definition.Item.Item3);

            if (defOffset.Equals(callablePos)) // the given position corresponds to a variable declared as part of a callable declaration
            {
                if (!compilation.GetCallables().TryGetValue(parentName, out var parent)) return false;
                referenceLocations = parent.Specializations
                    .Where(spec => spec.SourceFile.Value == file.FileName.Value)
                    .SelectMany(spec =>
                        spec.Implementation is SpecializationImplementation.Provided impl
                            ? IdentifierLocation.Find(definition.Item.Item1, impl.Item2, file.FileName, spec.Location)
                            : ImmutableArray<IdentifierReferences.Location>.Empty)
                    .Distinct().Select(AsLocation);
            }
            else // the given position corresponds to a variable declared as part of a specialization declaration or implementation
            {
                var defStart = DiagnosticTools.GetAbsolutePosition(defOffset, defRange.Item1);
                var statements = implementation.LocalDeclarationsAt(defStart.Subtract(specPos)).Item2;
                var scope = new QsScope(statements.ToImmutableArray(), locals);
                var rootLoc = new QsLocation(DiagnosticTools.AsTuple(specPos), null); // null is fine here since it won't be used
                referenceLocations = IdentifierLocation.Find(definition.Item.Item1, scope, file.FileName, rootLoc).Distinct().Select(AsLocation);
            }
            declarationLocation = AsLocation(file.FileName, definition.Item.Item2, defRange);
            return true;
        }
    }
}
