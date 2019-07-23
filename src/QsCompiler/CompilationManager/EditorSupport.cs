// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.TextProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.TextProcessing;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using QsSymbolInfo = Microsoft.Quantum.QsCompiler.SyntaxProcessing.SyntaxExtensions.SymbolInformation;


namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    internal static class EditorSupport
    {
        // utils for getting the necessary information for editor commands

        /// <summary>
        /// Throws an ArgumentNullException if the given offset or relative range is null.
        /// </summary>
        private static Location AsLocation(NonNullable<string> source,
            Tuple<int, int> offset, Tuple<QsPositionInfo, QsPositionInfo> relRange) =>
            new Location
            {
                Uri = CompilationUnitManager.TryGetUri(source, out var uri) ? uri : null,
                Range = DiagnosticTools.GetAbsoluteRange(DiagnosticTools.AsPosition(offset), relRange)
            };

        /// <summary>
        /// Throws an ArgumentNullException if the given reference location is null.
        /// </summary>
        private static Location AsLocation(IdentifierReferences.Location loc) =>
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
        /// Returns all namespaces in which a callable with the name of the symbol at the given position in the given file belongs to.
        /// Returns an empty collection if any of the arguments is null or if no unqualified symbol exists at that location. 
        /// Returns the name of the identifier as out parameter if an unqualified symbol exists at that location.
        /// </summary>
        private static IEnumerable<NonNullable<string>> NamespaceSuggestionsForIdAtPosition
            (this FileContentManager file, Position pos, CompilationUnit compilation, out string idName)
        {
            var variables = file?.TryGetQsSymbolInfo(pos, true, out CodeFragment _)?.UsedVariables;
            idName = variables != null && variables.Any() ? variables.Single().Symbol.AsDeclarationName(null) : null;
            return idName != null && compilation != null
                ? compilation.GlobalSymbols.NamespacesContainingCallable(NonNullable<string>.New(idName))
                : ImmutableArray<NonNullable<string>>.Empty;
        }

        /// <summary>
        /// Returns all namespaces in which a type with the name of the symbol at the given position in the given file belongs to.
        /// Returns an empty collection if any of the arguments is null or if no unqualified symbol exists at that location. 
        /// Returns the name of the type as out parameter if an unqualified symbol exists at that location.
        /// </summary>
        private static IEnumerable<NonNullable<string>> NamespaceSuggestionsForTypeAtPosition
            (this FileContentManager file, Position pos, CompilationUnit compilation, out string typeName)
        {
            var types = file?.TryGetQsSymbolInfo(pos, true, out CodeFragment _)?.UsedTypes;
            typeName = types != null && types.Any() &&
                types.Single().Type is QsTypeKind<QsType, QsSymbol, QsSymbol, Characteristics>.UserDefinedType udt
                ? udt.Item.Symbol.AsDeclarationName(null) : null;
            return typeName != null && compilation != null
                ? compilation.GlobalSymbols.NamespacesContainingType(NonNullable<string>.New(typeName))
                : ImmutableArray<NonNullable<string>>.Empty;
        }

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
                :  symbolInfo.UsedVariables.Any() ? symbolInfo.UsedVariables.Single()
                :  symbolInfo.DeclaredSymbols.Any() ? symbolInfo.DeclaredSymbols.Single() : null;
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


        // editor commands

        /// <summary>
        /// Returns an array with completion suggestions for the given file and position.
        /// Returns null if the given uri is null or if the specified file is not listed as source file.
        /// </summary>
        public static SymbolInformation[] DocumentSymbols(this FileContentManager file)
        {
            if (file == null) return null;
            var namespaceDeclarations = file.NamespaceDeclarationsSymbolInfo();
            var typeDeclarations = file.TypeDeclarationsSymbolInfo();
            var callableDeclarations = file.CallableDeclarationsSymbolInfo();
            return namespaceDeclarations.Concat(typeDeclarations).Concat(callableDeclarations).ToArray();
        }

        /// <summary>
        /// Returns the source file and position where the item at the given position is declared at,
        /// if such a declaration exists, and returns null otherwise.
        /// </summary>
        public static Location DefinitionLocation(this FileContentManager file, CompilationUnit compilation, Position position)
        {
            var symbolInfo = file?.TryGetQsSymbolInfo(position, true, out CodeFragment _); // includes the end position 
            if (symbolInfo == null || compilation == null) return null;

            var locals = compilation.TryGetLocalDeclarations(file, position, out var cName);
            if (cName == null) return null;

            var found =
                symbolInfo.UsedVariables.Any()
                ? compilation.GlobalSymbols.VariableDeclaration(locals, cName.Namespace, file.FileName, symbolInfo.UsedVariables.Single())
                : symbolInfo.UsedTypes.Any()
                ? compilation.GlobalSymbols.TypeDeclaration(cName.Namespace, file.FileName, symbolInfo.UsedTypes.Single())
                : symbolInfo.DeclaredSymbols.Any()
                ? compilation.GlobalSymbols.SymbolDeclaration(locals, cName.Namespace, file.FileName, symbolInfo.DeclaredSymbols.Single())
                : QsNullable<Tuple<NonNullable<string>, Tuple<int, int>, Tuple<QsPositionInfo, QsPositionInfo>>>.Null;

            return found.IsValue
                ? AsLocation(found.Item.Item1, found.Item.Item2, found.Item.Item3)
                : null;
        }

        /// <summary>
        /// Returns an array with all locations where the symbol at the given position - if any - is referenced. 
        /// Returns null if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the currently processed file content,
        /// or if no symbol exists at the specified position at this time.
        /// </summary>
        public static Location[] SymbolReferences(this FileContentManager file, CompilationUnit compilation, Position position, ReferenceContext context)
        {
            if (file == null) return null;
            if (!file.TryGetReferences(compilation, position, out var declLocation, out var locations)) return null;
            return context?.IncludeDeclaration ?? true && declLocation != null
                ? new[] { declLocation }.Concat(locations).ToArray()
                : locations.ToArray();
        }

        /// <summary>
        /// Returns the workspace edit that describes the changes to be done if the symbol at the given position - if any - is renamed to the given name. 
        /// Returns null if no symbol exists at the specified position,
        /// or if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the file.
        /// </summary>
        public static WorkspaceEdit Rename(this FileContentManager file, CompilationUnit compilation, Position position, string newName)
        {
            if (newName == null || file == null) return null; 
            var found = file.TryGetReferences(compilation, position, out var declLocation, out var locations);
            if (!found) return null;
            if (declLocation != null) locations = new[] { declLocation }.Concat(locations);

            var changes = locations.ToLookup(loc => loc.Uri, loc => new TextEdit { Range = loc.Range, NewText = newName}); 
            return new WorkspaceEdit
            {
                DocumentChanges = changes
                    .Select(change => new TextDocumentEdit
                    {
                        TextDocument = new VersionedTextDocumentIdentifier { Uri = change.Key, Version = 1 }, // setting version to null here won't work in VS Code ...
                        Edits = change.ToArray()
                    })
                    .ToArray(),

                Changes = changes.ToDictionary(
                    items => CompilationUnitManager.TryGetFileId(items.Key, out var name) ? name.Value : null,
                    items => items.ToArray())
            };
        }

        /// <summary>
        /// Returns a dictionary of workspace edits suggested by the compiler for the given location and context.
        /// The keys of the dictionary are suitable titles for each edit that can be presented to the user. 
        /// Returns null if any of the given arguments is null or if suitable edits cannot be determined.
        /// </summary>
        public static ImmutableDictionary<string, WorkspaceEdit> CodeActions(this FileContentManager file, CompilationUnit compilation, Range range, CodeActionContext context)
        {
            if (range?.Start == null || range.End == null || file == null || !Utils.IsValidRange(range, file)) return null;
            if (compilation == null || context?.Diagnostics == null) return null;
            var versionedFileId = new VersionedTextDocumentIdentifier { Uri = file.Uri, Version = 1 }; // setting version to null here won't work in VS Code ...

            WorkspaceEdit GetWorkspaceEdit(TextEdit edit) => new WorkspaceEdit
            {
                DocumentChanges = new[] { new TextDocumentEdit { TextDocument = versionedFileId, Edits = new[] { edit } } },
                Changes = new Dictionary<string, TextEdit[]> { { file.FileName.Value, new[] { edit } } }
            };

            // diagnostics based on which suggestions are given
            var ambiguousCallables = context.Diagnostics.Where(DiagnosticTools.ErrorType(ErrorCode.AmbiguousCallable));
            var unknownCallables = context.Diagnostics.Where(DiagnosticTools.ErrorType(ErrorCode.UnknownIdentifier));
            var ambiguousTypes = context.Diagnostics.Where(DiagnosticTools.ErrorType(ErrorCode.AmbiguousType));
            var unknownTypes = context.Diagnostics.Where(DiagnosticTools.ErrorType(ErrorCode.UnknownType));
            var deprecatedUnitTypes = context.Diagnostics.Where(DiagnosticTools.WarningType(WarningCode.DeprecatedUnitType));

            // Suggestion to replace depricated unit types
            (string, WorkspaceEdit) editAtPosition(string text, Position start, Position end)
            {
                var edit = new TextEdit { Range = new Range { Start = start, End = end}, NewText = text };
                return ("Replacing with \"" + $"{text}" + "\"", GetWorkspaceEdit(edit));
            }

            if (deprecatedUnitTypes.Any())
            { return deprecatedUnitTypes.Select(d => editAtPosition(Keywords.qsUnit.id, d.Range.Start, d.Range.End)).ToImmutableDictionary(s => s.Item1, s => s.Item2); }

            // suggestions for ambiguous ids and types

            (string, WorkspaceEdit) SuggestedNameQualification(NonNullable<string> suggestedNS, string id, Position pos)
            {
                var edit = new TextEdit { Range = new Range { Start = pos, End = pos }, NewText = $"{suggestedNS.Value}." };
                return ($"{suggestedNS.Value}.{id}", GetWorkspaceEdit(edit));
            }

            var suggestedIdQualifications = ambiguousCallables.Select(d => d.Range.Start)
                .SelectMany(pos => file.NamespaceSuggestionsForIdAtPosition(pos, compilation, out var id)
                .Select(ns => SuggestedNameQualification(ns, id, pos)));
            var suggestedTypeQualifications = ambiguousTypes.Select(d => d.Range.Start)
                .SelectMany(pos => file.NamespaceSuggestionsForTypeAtPosition(pos, compilation, out var id)
                .Select(ns => SuggestedNameQualification(ns, id, pos)));

            if (!unknownCallables.Any() && !unknownTypes.Any())
            { return suggestedIdQualifications.Concat(suggestedTypeQualifications).ToImmutableDictionary(s => s.Item1, s => s.Item2); }

            // suggestions for unknown ids and types

            // determine the first fragment in the containing namespace
            var firstInNs = file.NamespaceDeclarationTokens()
                .TakeWhile(t => t.Line <= range.Start.Line).LastOrDefault() // going by line here is fine - I am ok with a failure if someone has muliple namespace and callable declarations on the same line...
                ?.GetChildren(deep: false)?.FirstOrDefault()?.GetFragment();
            if (firstInNs == null) return null;
            var insertOpenDirAt = firstInNs.GetRange().Start;

            // range and whitespace info for inserting open directives
            var openDirEditRange = new Range { Start = insertOpenDirAt, End = insertOpenDirAt };
            var indentationAfterOpenDir = file.GetLine(insertOpenDirAt.Line).Text.Substring(0, insertOpenDirAt.Character);
            var additionalLinesAfterOpenDir = firstInNs.Kind.OpenedNamespace().IsNull ? $"{Environment.NewLine}{Environment.NewLine}" : "";
            var whitespaceAfterOpenDir = $"{Environment.NewLine}{additionalLinesAfterOpenDir}{indentationAfterOpenDir}";

            (string, WorkspaceEdit) SuggestedOpenDirective(NonNullable<string> suggestedNS)
            {
                var directive = $"{Keywords.importDirectiveHeader.id} {suggestedNS.Value}";
                var edit = new TextEdit { Range = openDirEditRange, NewText = $"{directive};{whitespaceAfterOpenDir}" };
                return (directive, GetWorkspaceEdit(edit));
            }

            var suggestionsForIds = unknownCallables.Select(d => d.Range.Start)
                .SelectMany(pos => file.NamespaceSuggestionsForIdAtPosition(pos, compilation, out var _))
                .Select(SuggestedOpenDirective);
            var suggestionsForTypes = unknownTypes.Select(d => d.Range.Start)
                .SelectMany(pos => file.NamespaceSuggestionsForTypeAtPosition(pos, compilation, out var _))
                .Select(SuggestedOpenDirective);

            return suggestionsForIds.Concat(suggestionsForTypes)
                .Concat(suggestedIdQualifications).Concat(suggestedTypeQualifications)
                .ToImmutableDictionary(s => s.Item1, s => s.Item2);
        }

        /// <summary>
        /// Returns an array with all usages of the identifier at the given position (if any) as DocumentHighlights.
        /// Returns null if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the currently processed file content,
        /// or if no identifier exists at the specified position at this time.
        /// </summary>
        public static DocumentHighlight[] DocumentHighlights(this FileContentManager file, CompilationUnit compilation, Position position)
        {
            DocumentHighlight AsHighlight(Range range) =>
                new DocumentHighlight { Range = range, Kind = DocumentHighlightKind.Read };

            if (file == null) return null;
            var found = file.TryGetReferences(compilation, position,
                out var declLocation, out var locations,
                limitToSourceFiles: ImmutableHashSet.Create(file.FileName));
            if (!found) return null;

            QsCompilerError.Verify(declLocation == null || declLocation.Uri == file.Uri, "location outside current file");
            var highlights = locations.Select(loc =>
            {
                QsCompilerError.Verify(loc.Uri == file.Uri, "location outside current file");
                return AsHighlight(loc.Range);
            });

            return declLocation != null
                ? new[] { AsHighlight(declLocation.Range) }.Concat(highlights).ToArray()
                : highlights.ToArray();
        }

        /// <summary>
        /// Returns information about the item at the specified position as Hover information.
        /// Returns null if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the currently processed file content,
        /// or if no token exists at the specified position.
        /// </summary>
        public static Hover HoverInformation(this FileContentManager file, CompilationUnit compilation, Position position,
            MarkupKind format = MarkupKind.PlainText)
        {
            Hover GetHover(string info) => info == null ? null : new Hover
            {
                Contents = new MarkupContent { Kind = format, Value = info },
                Range = new Range { Start = position, End = position }
            };

            var markdown = format == MarkupKind.Markdown;
            var symbolInfo = file?.TryGetQsSymbolInfo(position, false, out var _);
            if (symbolInfo == null || compilation == null) return null;
            if (symbolInfo.UsedLiterals.Any()) return GetHover(symbolInfo.UsedLiterals.Single().LiteralInfo(markdown).Value);

            var locals = compilation.TryGetLocalDeclarations(file, position, out var cName);
            var nsName = cName?.Namespace.Value ?? file.TryGetNamespaceAt(position);
            if (nsName == null) return null;

            // TODO: add hover for functor generators and functor applications
            // TOOD: add hover for new array expr ?
            // TODO: add nested types - requires dropping the .Single and actually resolving to the closest match!
            var ns = NonNullable<string>.New(nsName);
            return GetHover(symbolInfo.UsedVariables.Any()
                ? compilation.GlobalSymbols.VariableInfo(locals, ns, file.FileName, symbolInfo.UsedVariables.Single(), markdown).Value
                : symbolInfo.UsedTypes.Any()
                ? compilation.GlobalSymbols.TypeInfo(ns, file.FileName, symbolInfo.UsedTypes.Single(), markdown).Value
                : symbolInfo.DeclaredSymbols.Any()
                ? compilation.GlobalSymbols.DeclarationInfo(locals, ns, file.FileName, symbolInfo.DeclaredSymbols.Single(), markdown).Value
                : null);
        }

        /// <summary>
        /// Returns the signature help information for a call expression if there is such an expression at the specified position.
        /// Returns null if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the currently processed file content,
        /// or if no call expression exists at the specified position at this time,
        /// or if no signature help information can be provided for the call expression at the specified position.
        /// </summary>
        public static SignatureHelp SignatureHelp(this FileContentManager file, CompilationUnit compilation, Position position,
            MarkupKind format = MarkupKind.PlainText)
        {
            // getting the relevant token (if any)

            var fragment = file?.TryGetFragmentAt(position, true);
            if (fragment?.Kind == null || compilation == null) return null;
            var fragmentStart = fragment.GetRange().Start;

            // getting the overlapping call expressions (if any), and determine the header of the called callable 

            bool OverlapsWithPosition(Tuple<QsPositionInfo, QsPositionInfo> symRange) =>
                position.IsWithinRange(DiagnosticTools.GetAbsoluteRange(fragmentStart, symRange), true);

            var overlappingEx = fragment.Kind.CallExpressions().Where(ex => ex.Range.IsValue && OverlapsWithPosition(ex.Range.Item)).ToList();
            if (!overlappingEx.Any()) return null;
            overlappingEx.Sort((ex1, ex2) => // for nested call expressions, the last expressions (by range) is always the closest one
            {
                var (x, y) = (ex1.Range.Item, ex2.Range.Item);
                int result = x.Item1.CompareTo(y.Item1);
                return result == 0 ? x.Item2.CompareTo(y.Item2) : result;
            });

            var nsName = file.TryGetNamespaceAt(position);
            var (method, args) = overlappingEx.Last().Expression is QsExpressionKind<QsExpression, QsSymbol, QsType>.CallLikeExpression c ? (c.Item1, c.Item2) : (null, null);
            if (nsName == null || method == null || args == null) return null;

            // getting the called identifier as well as what functors have been applied to it

            List<QsFunctor> FunctorApplications(ref QsExpression ex)
            {
                var (next, inner) =
                    ex.Expression is QsExpressionKind<QsExpression, QsSymbol, QsType>.AdjointApplication adj ? (QsFunctor.Adjoint, adj.Item) :
                    ex.Expression is QsExpressionKind<QsExpression, QsSymbol, QsType>.ControlledApplication ctl ? (QsFunctor.Controlled, ctl.Item) :
                    (null, null);
                var fs = inner == null ? new List<QsFunctor>() : FunctorApplications(ref inner);
                if (next != null) fs.Add(next);
                ex = inner ?? ex;
                return fs;
            }

            var functors = FunctorApplications(ref method);
            var id = method.Expression as QsExpressionKind<QsExpression, QsSymbol, QsType>.Identifier;
            if (id == null) return null;

            // extracting and adapting the relevant information for the called callable

            var ns = NonNullable<string>.New(nsName);
            var methodDecl = id.Item1.Symbol is QsSymbolKind<QsSymbol>.Symbol sym
                ? compilation.GlobalSymbols.TryResolveAndGetCallable(sym.Item, ns, file.FileName).Item1
                : id.Item1.Symbol is QsSymbolKind<QsSymbol>.QualifiedSymbol qualSym
                ? compilation.GlobalSymbols.TryGetCallable(new QsQualifiedName(qualSym.Item1, qualSym.Item2), ns, file.FileName)
                : QsNullable<CallableDeclarationHeader>.Null;
            if (methodDecl.IsNull) return null;

            var (documentation, argTuple) = (methodDecl.Item.Documentation, methodDecl.Item.ArgumentTuple);
            var nrCtlApplications = functors.Where(f => f.Equals(QsFunctor.Controlled)).Count();
            while (nrCtlApplications-- > 0)
            {
                var ctlQsName = QsLocalSymbol.NewValidName(NonNullable<string>.New(nrCtlApplications == 0 ? "cs" : $"cs{nrCtlApplications}"));
                argTuple = SyntaxGenerator.WithControlQubits(argTuple, QsNullable<Tuple<int, int>>.Null, ctlQsName, QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null);
            }
            
            // now that we now what callable is called we need to check which argument should come next

            bool BeforePosition(Tuple<QsPositionInfo, QsPositionInfo> symRange) =>
                DiagnosticTools.GetAbsolutePosition(fragmentStart, symRange.Item2).IsSmallerThan(position);

            IEnumerable<(Tuple<QsPositionInfo, QsPositionInfo>, string)> ExtractParameterRanges
                (QsExpression ex, QsTuple<LocalVariableDeclaration<QsLocalSymbol>> decl)
            {
                var Null = ((Tuple<QsPositionInfo, QsPositionInfo>)null, (string)null);
                IEnumerable<(Tuple<QsPositionInfo, QsPositionInfo>, string)> SingleItem(string paramName)
                {
                    var arg = ex?.Range == null ? ((Tuple<QsPositionInfo, QsPositionInfo>)null, paramName)
                        : ex.Range.IsValue ? (ex.Range.Item, paramName)
                        : Null; // no signature help if there are invalid expressions
                    return new[] { arg };
                }

                if (decl is QsTuple<LocalVariableDeclaration<QsLocalSymbol>>.QsTupleItem dItem)
                { return SingleItem(dItem.Item.VariableName is QsLocalSymbol.ValidName n ? n.Item.Value : "__argName__"); }

                var declItems = decl as QsTuple<LocalVariableDeclaration<QsLocalSymbol>>.QsTuple;
                var exItems = ex?.Expression as QsExpressionKind<QsExpression, QsSymbol, QsType>.ValueTuple;
                if (declItems == null) return new[] { Null };
                if (exItems == null && declItems.Item.Length > 1) return SingleItem(decl.PrintArgumentTuple()); 

                var argItems = exItems != null ? exItems.Item : (ex == null ? ImmutableArray<QsExpression>.Empty : ImmutableArray.Create(ex));
                return argItems.AddRange(Enumerable.Repeat<QsExpression>(null, declItems.Item.Length - argItems.Length))
                    .Zip(declItems.Item, (e, d) => (e, d))
                    .SelectMany(arg => ExtractParameterRanges(arg.Item1, arg.Item2));
            }

            var callArgs = ExtractParameterRanges(args, argTuple).ToArray();
            if (id == null || callArgs == null || callArgs.Any(item => item.Item2 == null)) return null; // no signature help if there are invalid expressions

            // finally we can build the signature help information

            MarkupContent AsMarkupContent(string str) => new MarkupContent { Kind = format, Value = str };
            ParameterInformation AsParameterInfo(NonNullable<string> paramName) => new ParameterInformation
            {
                Label = paramName.Value,
                Documentation = AsMarkupContent(documentation.ParameterDescription(paramName.Value))
            };

            var signatureLabel = $"{methodDecl.Item.QualifiedName.Name.Value} {argTuple.PrintArgumentTuple()}";
            foreach (var f in functors)
            {
                if (f.IsAdjoint) signatureLabel = $"{Keywords.qsAdjointFunctor.id} {signatureLabel}";
                if (f.IsControlled) signatureLabel = $"{Keywords.qsControlledFunctor.id} {signatureLabel}";
            }
                
            var doc = documentation.PrintSummary(format == MarkupKind.Markdown).TrimStart();
            var info = new SignatureInformation
            {
                Documentation = AsMarkupContent(doc),
                Label = signatureLabel, // Note: the label needs to be expressed in a way that the active parameter is detectable
                Parameters = callArgs.Select(d => NonNullable<string>.New(d.Item2)).Select(AsParameterInfo).ToArray()
            };
            var precedingArgs = callArgs
                .TakeWhile(item => item.Item1 == null || BeforePosition(item.Item1)) // skip args that have already been typed or - in the case of inner items - are missing
                .Reverse().SkipWhile(item => item.Item1 == null); // don't count missing, i.e. not yet typed items, of the relevant inner argument tuple 
            return new SignatureHelp
            {
                Signatures = new[] { info }, // since we don't support overloading there is just one signature here
                ActiveSignature = 0, 
                ActiveParameter = precedingArgs.Count() 
            };
        }
    }
}
