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
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.TextProcessing;
using Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput;
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

        /// <summary>
        /// Returns a sequence of suggestions on how deprecated syntax can be updated based on the generated diagnostics, 
        /// and given the file for which those diagnostics were generated. 
        /// Returns an empty enumerable if any of the given arguments is null. 
        /// </summary>
        internal static IEnumerable<(string, WorkspaceEdit)> SuggestionsForDeprecatedSyntax
            (this FileContentManager file, IEnumerable<Diagnostic> diagnostics)
        {
            if (file == null || diagnostics == null) return Enumerable.Empty<(string, WorkspaceEdit)>();
            var deprecatedUnitTypes = diagnostics.Where(DiagnosticTools.WarningType(WarningCode.DeprecatedUnitType));
            var deprecatedNOToperators = diagnostics.Where(DiagnosticTools.WarningType(WarningCode.DeprecatedNOToperator));
            var deprecatedANDoperators = diagnostics.Where(DiagnosticTools.WarningType(WarningCode.DeprecatedANDoperator));
            var deprecatedORoperators = diagnostics.Where(DiagnosticTools.WarningType(WarningCode.DeprecatedORoperator));
            var deprecatedOpCharacteristics = diagnostics.Where(DiagnosticTools.WarningType(WarningCode.DeprecatedOpCharacteristics));

            (string, WorkspaceEdit) ReplaceWith(string text, Range range)
            {
                bool NeedsWsBefore(Char ch) => Char.IsLetterOrDigit(ch) || ch == '_';
                if (range?.Start != null && range.End != null)
                {
                    var beforeEdit = file.GetLine(range.Start.Line).Text.Substring(0, range.Start.Character);
                    var afterEdit = file.GetLine(range.End.Line).Text.Substring(range.End.Character);
                    if (beforeEdit.Any() && !Char.IsWhiteSpace(beforeEdit.Last())) text = $" {text}";
                    if (afterEdit.Any() && NeedsWsBefore(afterEdit.First())) text = $"{text} ";
                }
                var edit = new TextEdit { Range = range?.Copy(), NewText = text };
                return ($"Replace with \"{text.Trim()}\".", file.GetWorkspaceEdit(edit));
            }

            // update deprecated keywords and operators

            var suggestionsForUnitType = deprecatedUnitTypes.Select(d => ReplaceWith(Keywords.qsUnit.id, d.Range));
            var suggestionsForNOT = deprecatedNOToperators.Select(d => ReplaceWith(Keywords.qsNOTop.op, d.Range));
            var suggestionsForAND = deprecatedANDoperators.Select(d => ReplaceWith(Keywords.qsANDop.op, d.Range));
            var suggestionsForOR = deprecatedORoperators.Select(d => ReplaceWith(Keywords.qsORop.op, d.Range));

            // update deprecated operation characteristics syntax

            var typeToQs = new ExpressionTypeToQs(new ExpressionToQs());
            string CharacteristicsAnnotation(Characteristics c)
            {
                typeToQs.onCharacteristicsExpression(SymbolResolution.ResolveCharacteristics(c));
                return $"{Keywords.qsCharacteristics.id} {typeToQs.Output}";
            }

            var suggestionsForOpCharacteristics = deprecatedOpCharacteristics.SelectMany(d =>
            {
                // TODO: TryGetQsSymbolInfo currently only returns information about the inner most leafs rather than all types etc. 
                // Once it returns indeed all types in the fragment, the following code block should be replaced by the commented out code below. 
                var fragment = file.TryGetFragmentAt(d.Range.Start);
                IEnumerable<Characteristics> GetCharacteristics(QsTuple<Tuple<QsSymbol, QsType>> argTuple) =>
                    SyntaxGenerator.ExtractItems(argTuple).SelectMany(item => item.Item2.ExtractCharacteristics()).Distinct();
                var characteristicsInFragment =
                    fragment?.Kind is QsFragmentKind.FunctionDeclaration function ? GetCharacteristics(function.Item2.Argument) :
                    fragment?.Kind is QsFragmentKind.OperationDeclaration operation ? GetCharacteristics(operation.Item2.Argument) :
                    fragment?.Kind is QsFragmentKind.TypeDefinition type ? GetCharacteristics(type.Item2) :
                    Enumerable.Empty<Characteristics>();

                //var symbolInfo = file.TryGetQsSymbolInfo(d.Range.Start, false, out var fragment);
                //var characteristicsInFragment = (symbolInfo?.UsedTypes ?? Enumerable.Empty<QsType>())
                //    .SelectMany(t => t.ExtractCharacteristics()).Distinct();
                var fragmentStart = fragment?.GetRange()?.Start;
                return characteristicsInFragment
                    .Where(c => c.Range.IsValue && DiagnosticTools.GetAbsoluteRange(fragmentStart, c.Range.Item).Overlaps(d.Range))
                    .Select(c => ReplaceWith(CharacteristicsAnnotation(c), d.Range));
            });

            return suggestionsForOpCharacteristics.ToArray()
                .Concat(suggestionsForUnitType)
                .Concat(suggestionsForNOT)
                .Concat(suggestionsForAND)
                .Concat(suggestionsForOR);
        }

        public static IEnumerable<(string, WorkspaceEdit)> DocCommentSuggestions(this FileContentManager file, int lineNr)
        {
            IEnumerable<(string, WorkspaceEdit)> SuggestedDocStrings(params List<CodeFragment.TokenIndex>[] declLists)
            {
                CallableSignature getSignature(QsNullable<Tuple<QsSymbol, Tuple<QsCallableKind, CallableSignature>>> callableDecl) =>
                    callableDecl.Item.Item2.Item2;
                ImmutableArray<Tuple<QsSymbol, QsType>> getArguments(CallableSignature sig) =>
                    ((QsTuple<Tuple<QsSymbol, QsType>>.QsTuple)sig.Argument).Item.Select(x => ((QsTuple<Tuple<QsSymbol, QsType>>.QsTupleItem)x).Item).ToImmutableArray();
                // Consider making TypeChecking.DocumentingComments public and replacing hasDocumentingComment with TypeChecking.DocumentingComments(...).Any()
                System.Reflection.MethodInfo DocumentingComments =
                            typeof(TypeChecking).GetMethod("DocumentingComments", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                bool hasDocumentingComment(Position pos) => ((ImmutableArray<string>)DocumentingComments.Invoke(null, new object[] { file, pos })).Any();
                return declLists.Select((List<CodeFragment.TokenIndex> decls) =>
                {
                    // Get last un-documented declaration
                    var declTokenInd = decls?.TakeWhile(t => t.Line <= lineNr)
                                             .Where(t => !hasDocumentingComment(t.GetFragment().GetRange().Start));

                    if (declTokenInd is null || !declTokenInd.Any())
                        return (null, null);

                    var declFrag = declTokenInd.Last().GetFragment();
                    var declRange = declFrag.GetRange();
                    string indentation = file.GetLine(declRange.Start.Line).Text.Substring(0, declRange.Start.Character);
                    string docPrefix = "/// ", endl = $"{Environment.NewLine}{indentation}";
                    var docString = $"{docPrefix}# Summary{endl}{docPrefix}{endl}";

                    if (FileHeader.IsCallableDeclaration(declFrag))
                    {
                        var args = getArguments(getSignature(declFrag.Kind.DeclaredCallable()));
                        var typeParameters = getSignature(declFrag.Kind.DeclaredCallable()).TypeParameters;

                        docString = string.Concat(docString,
                                                    // Document Input Parameters
                                                    args.Any() ? $"{docPrefix}# Input{endl}" : string.Empty,
                                                    string.Concat(args.Select(x => $"{docPrefix}## {x.Item1.Symbol.AsDeclarationName(null)}{endl}{docPrefix}{endl}")),
                                                    // Document Type Parameters
                                                    typeParameters.Any() ? $"{docPrefix}# Type Parameters{endl}" : string.Empty,
                                                    string.Concat(typeParameters.Select(x => $"{docPrefix}## {x.Symbol.AsDeclarationName(null)}{endl}{docPrefix}{endl}")));
                    }

                    return ($"Document {declFrag.Text}",
                            file.GetWorkspaceEdit(new TextEdit { Range = new Range { Start = declRange.Start, End = declRange.Start }, NewText = docString }));
                }).Where(x => !x.Equals((null, null)));
            }

            return SuggestedDocStrings(file.CallableDeclarationTokens(), file.NamespaceDeclarationTokens(), file.TypeDeclarationTokens());
        }
    }
}