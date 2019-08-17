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
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.TextProcessing;
using Microsoft.VisualStudio.LanguageServer.Protocol;


namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    internal static class SuggestedEdits
    {
        /// <summary>
        /// Returns the given edit for the specified file as WorkspaceEdit.
        /// </summary>
        private static WorkspaceEdit GetWorkspaceEdit(this FileContentManager file, TextEdit edit) 
        {
            var versionedFileId = new VersionedTextDocumentIdentifier { Uri = file.Uri, Version = 1 }; // setting version to null here won't work in VS Code ...
            return new WorkspaceEdit
            {
                DocumentChanges = new[] { new TextDocumentEdit { TextDocument = versionedFileId, Edits = new[] { edit } } },
                Changes = new Dictionary<string, TextEdit[]> { { file.FileName.Value, new[] { edit } } }
            };
        }

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
        /// Returns a sequence of suggestions on how errors for ambiguous types and callable in the given diagnostics can be fixed, 
        /// given the file for which those diagnostics were generated and the corresponding compilation. 
        /// Returns an empty enumerable if any of the given arguments is null. 
        /// </summary>
        internal static IEnumerable<(string, WorkspaceEdit)> SuggestionsForAmbiguousIdentifiers
            (this FileContentManager file, CompilationUnit compilation, IEnumerable<Diagnostic> diagnostics)
        {
            if (file == null || diagnostics == null) return Enumerable.Empty<(string, WorkspaceEdit)>();
            var ambiguousCallables = diagnostics.Where(DiagnosticTools.ErrorType(ErrorCode.AmbiguousCallable));
            var ambiguousTypes = diagnostics.Where(DiagnosticTools.ErrorType(ErrorCode.AmbiguousType));
            if (!ambiguousCallables.Any() && !ambiguousTypes.Any()) return Enumerable.Empty<(string, WorkspaceEdit)>();

            (string, WorkspaceEdit) SuggestedNameQualification(NonNullable<string> suggestedNS, string id, Position pos)
            {
                var edit = new TextEdit { Range = new Range { Start = pos, End = pos }, NewText = $"{suggestedNS.Value}." };
                return ($"{suggestedNS.Value}.{id}", file.GetWorkspaceEdit(edit));
            }

            var suggestedIdQualifications = ambiguousCallables.Select(d => d.Range.Start)
                .SelectMany(pos => file.NamespaceSuggestionsForIdAtPosition(pos, compilation, out var id)
                .Select(ns => SuggestedNameQualification(ns, id, pos)));
            var suggestedTypeQualifications = ambiguousTypes.Select(d => d.Range.Start)
                .SelectMany(pos => file.NamespaceSuggestionsForTypeAtPosition(pos, compilation, out var id)
                .Select(ns => SuggestedNameQualification(ns, id, pos)));
            return suggestedIdQualifications.Concat(suggestedTypeQualifications);
        }

        /// <summary>
        /// Returns a sequence of suggestions on how errors for unknown types and callable in the given diagnostics can be fixed, 
        /// given the file for which those diagnostics were generated and the corresponding compilation. 
        /// The given line number is used to determine the containing namespace. 
        /// Returns an empty enumerable if any of the given arguments is null. 
        /// </summary>
        internal static IEnumerable<(string, WorkspaceEdit)> SuggestionsForUnknownIdentifiers
            (this FileContentManager file, CompilationUnit compilation, int lineNr, IEnumerable<Diagnostic> diagnostics)
        {
            if (file == null || diagnostics == null) return Enumerable.Empty<(string, WorkspaceEdit)>();
            var unknownCallables = diagnostics.Where(DiagnosticTools.ErrorType(ErrorCode.UnknownIdentifier));
            var unknownTypes = diagnostics.Where(DiagnosticTools.ErrorType(ErrorCode.UnknownType));
            if (!unknownCallables.Any() && !unknownTypes.Any()) return Enumerable.Empty<(string, WorkspaceEdit)>();

            // determine the first fragment in the containing namespace
            var firstInNs = file.NamespaceDeclarationTokens()
                .TakeWhile(t => t.Line <= lineNr).LastOrDefault() // going by line here is fine - inaccuracies if someone has multiple namespace and callable declarations on the same line seem acceptable...
                ?.GetChildren(deep: false)?.FirstOrDefault()?.GetFragment();
            if (firstInNs == null) return Enumerable.Empty<(string, WorkspaceEdit)>();
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
                return (directive, file.GetWorkspaceEdit(edit));
            }

            var suggestionsForIds = unknownCallables.Select(d => d.Range.Start)
                .SelectMany(pos => file.NamespaceSuggestionsForIdAtPosition(pos, compilation, out var _))
                .Select(SuggestedOpenDirective);
            var suggestionsForTypes = unknownTypes.Select(d => d.Range.Start)
                .SelectMany(pos => file.NamespaceSuggestionsForTypeAtPosition(pos, compilation, out var _))
                .Select(SuggestedOpenDirective);
            return suggestionsForIds.Concat(suggestionsForTypes);
        }
    }
}