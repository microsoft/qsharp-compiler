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
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;


namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    internal static class SuggestedEdits
    {
        /// <summary>
        /// Returns the given edit for the specified file as WorkspaceEdit.
        /// Throws an ArgumentNullException if the given file or any of the given edits is null.
        /// </summary>
        private static WorkspaceEdit GetWorkspaceEdit(this FileContentManager file, params TextEdit[] edits)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (edits == null || edits.Any(edit => edit == null)) throw new ArgumentNullException(nameof(edits));

            var versionedFileId = new VersionedTextDocumentIdentifier { Uri = file.Uri, Version = 1 }; // setting version to null here won't work in VS Code ...
            return new WorkspaceEdit
            {
                DocumentChanges = new[] { new TextDocumentEdit { TextDocument = versionedFileId, Edits = edits } },
                Changes = new Dictionary<string, TextEdit[]> { { file.FileName.Value, edits } }
            };
        }

        /// <summary>
        /// Returns all namespaces in which a callable with the name of the symbol at the given position in the given
        /// file belongs to.
        ///
        /// Returns an empty collection if any of the arguments is null, if no unqualified symbol exists at that
        /// location, or if the position is not part of a namespace.
        ///
        /// Returns the name of the identifier as an out parameter if an unqualified symbol exists at that location.
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
        /// Returns all namespaces in which a type with the name of the symbol at the given position in the given file
        /// belongs to.
        ///
        /// Returns an empty collection if any of the arguments is null, if no unqualified symbol exists at that
        /// location, or if the position is not part of a namespace.
        ///
        /// Returns the name of the type as an out parameter if an unqualified symbol exists at that location.
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

        private static IEnumerable<NonNullable<string>> TypesThatDifferByCapitilization
            (CompilationUnit compilation, string typeName)
        {
            return compilation.GetTypes().Keys
                .Select(tn => tn.Name.Value)
                .Where(tn => !tn.Equals(typeName, StringComparison.Ordinal) && tn.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                .Select(tn => NonNullable<string>.New(tn));
        }

        /// <summary>
        /// Returns all Types that match an alternative capitilization of this type.
        /// Returns an empty collection if any of the arguments is null or if no unqualified symbol exists at that location. 
        /// Returns the name of the type as out parameter if an unqualified symbol exists at that location.
        /// </summary>
        private static IEnumerable<NonNullable<string>> CapitalizationSuggestionsForIdAtPosition
            (this FileContentManager file, Position pos, CompilationUnit compilation, out string typeName)
        {
            var variables = file?.TryGetQsSymbolInfo(pos, true, out CodeFragment _)?.UsedVariables;
            typeName = variables != null && variables.Any() ? variables.Single().Symbol.AsDeclarationName(null) : null;

            if (typeName == null || compilation == null) { return ImmutableArray<NonNullable<string>>.Empty; }
            return TypesThatDifferByCapitilization(compilation, typeName);
        }

        /// <summary>
        /// Returns all Types in which a type with the name of the symbol at the given position in the given file belongs to.
        /// Returns an empty collection if any of the arguments is null or if no unqualified symbol exists at that location. 
        /// Returns the name of the type as out parameter if an unqualified symbol exists at that location.
        /// </summary>
        private static IEnumerable<NonNullable<string>> CapitalizationSuggestionsForTypeAtPosition
            (this FileContentManager file, Position pos, CompilationUnit compilation, out string typeName)
        {
            var types = file?.TryGetQsSymbolInfo(pos, true, out CodeFragment _)?.UsedTypes;
            typeName = types != null && types.Any() &&
                types.Single().Type is QsTypeKind<QsType, QsSymbol, QsSymbol, Characteristics>.UserDefinedType udt
                ? udt.Item.Symbol.AsDeclarationName(null) : null;

            if (typeName == null || compilation == null) { return ImmutableArray<NonNullable<string>>.Empty; }
            return TypesThatDifferByCapitilization(compilation, typeName);
        }

        /// <summary>
        /// Returns all code fragments in the specified file that overlap with the given range.
        /// Returns an empty sequence if any of the given arguments is null.
        /// </summary>
        private static IEnumerable<CodeFragment> FragmentsOverlappingWithRange(this FileContentManager file, LSP.Range range)
        {
            if (file == null || range?.Start == null || range.End == null) return Enumerable.Empty<CodeFragment>();
            var (start, end) = (range.Start.Line, range.End.Line);

            var fragAtStart = file.TryGetFragmentAt(range.Start, out var _, includeEnd: true);
            var inRange = file.GetTokenizedLine(start).Select(t => t.WithUpdatedLineNumber(start)).Where(ContextBuilder.TokensAfter(range.Start)); // does not include fragAtStart
            inRange = start == end
                ? inRange.Where(ContextBuilder.TokensStartingBefore(range.End))
                : inRange.Concat(file.GetTokenizedLines(start + 1, end - start - 1).SelectMany((x, i) => x.Select(t => t.WithUpdatedLineNumber(start + 1 + i))))
                    .Concat(file.GetTokenizedLine(end).Select(t => t.WithUpdatedLineNumber(end)).Where(ContextBuilder.TokensStartingBefore(range.End)));

            var fragments = ImmutableArray.CreateBuilder<CodeFragment>();
            if (fragAtStart != null) fragments.Add(fragAtStart);
            fragments.AddRange(inRange);
            return fragments.ToImmutableArray();
        }

        /// <summary>
        /// Return an enumerable of suitable edits to add open directives for all given namespaces for which no open directive already exists.
        /// Returns an edit for opening a given namespace even if an alias is already defined for that namespace.
        /// Returns an empty enumerable if suitable edits could not be determined.
        /// </summary>
        private static IEnumerable<TextEdit> OpenDirectiveSuggestions(this FileContentManager file, int lineNr, params NonNullable<string>[] namespaces)
        {
            // determine the first fragment in the containing namespace
            var nsElements = file?.NamespaceDeclarationTokens()
                .TakeWhile(t => t.Line <= lineNr).LastOrDefault() // going by line here is fine - inaccuracies if someone has multiple namespace and callable declarations on the same line seem acceptable...
                ?.GetChildren(deep: false);
            var firstInNs = nsElements?.FirstOrDefault()?.GetFragment();
            if (firstInNs?.Kind == null) return Enumerable.Empty<TextEdit>();

            // determine what open directives already exist
            var insertOpenDirAt = firstInNs.GetRange().Start;
            var openDirs = nsElements.Select(t => t.GetFragment().Kind?.OpenedNamespace())
                .TakeWhile(opened => opened?.IsValue ?? false)
                .Select(opened => (
                    opened.Value.Item.Item1.Symbol.AsDeclarationName(null),
                    opened.Value.Item.Item2.IsValue ? opened.Value.Item.Item2.Item.Symbol.AsDeclarationName("") : null))
                .Where(opened => opened.Item1 != null)
                .GroupBy(opened => opened.Item1, opened => opened.Item2) // in case there are duplicate open directives...
                .ToImmutableDictionary(opened => opened.Key, opened => opened.First());

            // range and whitespace info for inserting open directives
            var openDirEditRange = new LSP.Range { Start = insertOpenDirAt, End = insertOpenDirAt };
            var additionalLinesAfterOpenDir = firstInNs.Kind.OpenedNamespace().IsNull ? $"{Environment.NewLine}{Environment.NewLine}" : "";
            var indentationAfterOpenDir = file.GetLine(insertOpenDirAt.Line).Text.Substring(0, insertOpenDirAt.Character);
            var whitespaceAfterOpenDir = $"{Environment.NewLine}{additionalLinesAfterOpenDir}{(String.IsNullOrWhiteSpace(indentationAfterOpenDir) ? indentationAfterOpenDir : "    ")}";

            // construct a suitable edit
            return namespaces.Distinct().Where(ns => !openDirs.Contains(ns.Value, null)).Select(suggestedNS =>  // filter all namespaces that are already open
            {
                var directive = $"{Keywords.importDirectiveHeader.id} {suggestedNS.Value}";
                return new TextEdit { Range = openDirEditRange, NewText = $"{directive};{whitespaceAfterOpenDir}" };
            });
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
                var edit = new TextEdit { Range = new LSP.Range { Start = pos, End = pos }, NewText = $"{suggestedNS.Value}." };
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
        /// Returns a sequence of namespace suggestions for how errors for unknown types and callable in the given diagnostics can be fixed,
        /// given the file for which those diagnostics were generated and the corresponding compilation.
        /// The given line number is used to determine the containing namespace.
        /// Returns an empty enumerable if any of the given arguments is null.
        /// </summary>
        internal static IEnumerable<(string, WorkspaceEdit)> NamespaceSuggestionsForUnknownIdentifiers
            (this FileContentManager file, CompilationUnit compilation, int lineNr, IEnumerable<Diagnostic> diagnostics)
        {
            if (file == null || diagnostics == null) return Enumerable.Empty<(string, WorkspaceEdit)>();
            var unknownCallables = diagnostics.Where(DiagnosticTools.ErrorType(ErrorCode.UnknownIdentifier));
            var unknownTypes = diagnostics.Where(DiagnosticTools.ErrorType(ErrorCode.UnknownType));
            if (!unknownCallables.Any() && !unknownTypes.Any()) return Enumerable.Empty<(string, WorkspaceEdit)>();

            var suggestionsForIds = unknownCallables.Select(d => d.Range.Start)
                .SelectMany(pos => file.NamespaceSuggestionsForIdAtPosition(pos, compilation, out var _));
            var suggestionsForTypes = unknownTypes.Select(d => d.Range.Start)
                .SelectMany(pos => file.NamespaceSuggestionsForTypeAtPosition(pos, compilation, out var _));
            return file.OpenDirectiveSuggestions(lineNr, suggestionsForIds.Concat(suggestionsForTypes).ToArray())
                .Select(edit => (edit.NewText.Trim().Trim(';'), file.GetWorkspaceEdit(edit)));
        }

        /// <summary>
        /// Returns a sequence of replacement Type suggestions for how errors for unknown types and callable in the given diagnostics can be fixed,
        /// given the file for which those diagnostics were generated and the corresponding compilation.
        /// Returns an empty enumerable if any of the given arguments is null.
        /// </summary>
        internal static IEnumerable<(string, WorkspaceEdit)> CapitalizationSuggestionsForUnknownIdentifiers
            (this FileContentManager file, CompilationUnit compilation, IEnumerable<Diagnostic> diagnostics)
        {
            if (file == null || diagnostics == null) return Enumerable.Empty<(string, WorkspaceEdit)>();
            var unknownCallables = diagnostics.Where(DiagnosticTools.ErrorType(ErrorCode.UnknownIdentifier));
            var unknownTypes = diagnostics.Where(DiagnosticTools.ErrorType(ErrorCode.UnknownType));
            if (!unknownCallables.Any() && !unknownTypes.Any()) return Enumerable.Empty<(string, WorkspaceEdit)>();

            (string, WorkspaceEdit) SuggestedIdEdit(NonNullable<string> suggestedId, LSP.Range range)
            {
                var edit = new TextEdit { Range = range.Copy(), NewText = $"{suggestedId.Value}" };
                return ($"Replace with \"{suggestedId.Value}\".", file.GetWorkspaceEdit(edit));
            }

            var suggestionsForIds = unknownCallables
                .SelectMany(d => file.CapitalizationSuggestionsForIdAtPosition(d.Range.Start, compilation, out var _).Select(id => SuggestedIdEdit(id, d.Range)));
            var suggestionsForTypes = unknownTypes
                .SelectMany(d => file.CapitalizationSuggestionsForTypeAtPosition(d.Range.Start, compilation, out var _).Select(id => SuggestedIdEdit(id, d.Range)));
            return suggestionsForIds.Concat(suggestionsForTypes);
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
            var suggestions = NamespaceSuggestionsForUnknownIdentifiers(file, compilation, lineNr, diagnostics);
            if (!suggestions.Any())
            {
                suggestions = CapitalizationSuggestionsForUnknownIdentifiers(file, compilation, diagnostics);
            }

            return suggestions;
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

            (string, WorkspaceEdit) ReplaceWith(string text, LSP.Range range)
            {
                static bool NeedsWs(Char ch) => Char.IsLetterOrDigit(ch) || ch == '_';
                if (range?.Start != null && range.End != null)
                {
                    var beforeEdit = file.GetLine(range.Start.Line).Text.Substring(0, range.Start.Character);
                    var afterEdit = file.GetLine(range.End.Line).Text.Substring(range.End.Character);
                    if (beforeEdit.Any() && NeedsWs(beforeEdit.Last())) text = $" {text}";
                    if (afterEdit.Any() && NeedsWs(afterEdit.First())) text = $"{text} ";
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

            static string CharacteristicsAnnotation(Characteristics c)
            {
                var charEx = SyntaxTreeToQsharp.CharacteristicsExpression(SymbolResolution.ResolveCharacteristics(c));
                return charEx == null ? "" : $"{Keywords.qsCharacteristics.id} {charEx}";
            }

            var suggestionsForOpCharacteristics = deprecatedOpCharacteristics.SelectMany(d =>
            {
                // TODO: TryGetQsSymbolInfo currently only returns information about the inner most leafs rather than all types etc.
                // Once it returns indeed all types in the fragment, the following code block should be replaced by the commented out code below.
                var fragment = file.TryGetFragmentAt(d.Range.Start, out var _);

                static IEnumerable<Characteristics> GetCharacteristics(QsTuple<Tuple<QsSymbol, QsType>> argTuple) =>
                    SyntaxGenerator.ExtractItems(argTuple).SelectMany(item => item.Item2.ExtractCharacteristics()).Distinct();
                var characteristicsInFragment =
                    fragment?.Kind is QsFragmentKind.FunctionDeclaration function ? GetCharacteristics(function.Item3.Argument) :
                    fragment?.Kind is QsFragmentKind.OperationDeclaration operation ? GetCharacteristics(operation.Item3.Argument) :
                    fragment?.Kind is QsFragmentKind.TypeDefinition type ? GetCharacteristics(type.Item3) :
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

        /// <summary>
        /// Returns a sequence of suggestions for update-and-reassign statements based on the generated diagnostics,
        /// and given the file for which those diagnostics were generated.
        /// Returns an empty enumerable if any of the given arguments is null.
        /// </summary>
        internal static IEnumerable<(string, WorkspaceEdit)> SuggestionsForUpdateAndReassignStatements
            (this FileContentManager file, IEnumerable<Diagnostic> diagnostics)
        {
            if (file == null || diagnostics == null) return Enumerable.Empty<(string, WorkspaceEdit)>();
            var updateOfArrayItemExprs = diagnostics.Where(DiagnosticTools.ErrorType(ErrorCode.UpdateOfArrayItemExpr));

            (string, WorkspaceEdit) SuggestedCopyAndUpdateExpr(CodeFragment fragment)
            {
                var exprInfo = Parsing.ProcessUpdateOfArrayItemExpr.Invoke(fragment.Text);
                // Skip if the statement did not match a pattern for which we can give a code action
                if (exprInfo == null || (exprInfo.Item1.Line == 1 && exprInfo.Item1.Column == 1)) return ("", null);

                // Convert set <identifier>[<index>] = <rhs> to set <identifier> w/= <index> <- <rhs>
                var rhs = $"{exprInfo.Item3} {Keywords.qsCopyAndUpdateOp.cont} {exprInfo.Item4}";
                var outputStr = $"{Keywords.qsValueUpdate.id} {exprInfo.Item2} {Keywords.qsCopyAndUpdateOp.op}= {rhs}";
                var fragmentRange = fragment.GetRange();
                var edit = new TextEdit { Range = fragmentRange.Copy(), NewText = outputStr };
                return ("Replace with an update-and-reassign statement.", file.GetWorkspaceEdit(edit));
            }

            return updateOfArrayItemExprs
                .Select(d => file?.TryGetFragmentAt(d.Range.Start, out var _, includeEnd: true))
                .Where(frag => frag != null)
                .Select(frag => SuggestedCopyAndUpdateExpr(frag))
                .Where(s => s.Item2 != null);
        }

        /// <summary>
        /// Returns a sequence of suggestions for replacing ranges over array indices with the corresponding library call,
        /// provided the corresponding library is referenced.
        /// Returns an empty enumerable if this is not the case or any of the given arguments is null.
        /// </summary>
        internal static IEnumerable<(string, WorkspaceEdit)> SuggestionsForIndexRange
            (this FileContentManager file, CompilationUnit compilation, LSP.Range range)
        {
            if (file == null || compilation == null || range?.Start == null)
            {
                return Enumerable.Empty<(string, WorkspaceEdit)>();
            }

            // Ensure that the IndexRange library function exists in this compilation unit.
            var nsName = file.TryGetNamespaceAt(range.Start);
            if (nsName == null)
            {
                return Enumerable.Empty<(string, WorkspaceEdit)>();
            }
            var indexRange = compilation.GlobalSymbols.TryGetCallable(
                BuiltIn.IndexRange.FullName,
                NonNullable<string>.New(nsName),
                file.FileName);
            if (!indexRange.IsFound)
            {
                return Enumerable.Empty<(string, WorkspaceEdit)>();
            }

            /// Returns true the given expression is of the form "0 .. Length(args) - 1",
            /// as well as the range of the entire expression and the argument tuple "(args)" as out parameters.
            static bool IsIndexRange(QsExpression iterExpr, Position offset, out LSP.Range exprRange, out LSP.Range argRange)
            {
                if (iterExpr.Expression is QsExpressionKind<QsExpression, QsSymbol, QsType>.RangeLiteral rangeExpression && iterExpr.Range.IsValue &&                               // iterable expression is a valid range literal
                    rangeExpression.Item1.Expression is QsExpressionKind<QsExpression, QsSymbol, QsType>.IntLiteral intLiteralExpression && intLiteralExpression.Item == 0L &&      // .. starting at 0 ..
                    rangeExpression.Item2.Expression is QsExpressionKind<QsExpression, QsSymbol, QsType>.SUB SUBExpression &&                                                       // .. and ending in subtracting ..
                    SUBExpression.Item2.Expression is QsExpressionKind<QsExpression, QsSymbol, QsType>.IntLiteral subIntLiteralExpression && subIntLiteralExpression.Item == 1L &&  // .. 1 from ..
                    SUBExpression.Item1.Expression is QsExpressionKind<QsExpression, QsSymbol, QsType>.CallLikeExpression callLikeExression &&                                      // .. a call ..
                    callLikeExression.Item1.Expression is QsExpressionKind<QsExpression, QsSymbol, QsType>.Identifier identifier &&                                                 // .. to and identifier ..
                    identifier.Item1.Symbol is QsSymbolKind<QsSymbol>.Symbol symName && symName.Item.Value == BuiltIn.Length.FullName.Name.Value &&                                          // .. "Length" called with ..
                    callLikeExression.Item2.Expression is QsExpressionKind<QsExpression, QsSymbol, QsType>.ValueTuple valueTuple && callLikeExression.Item2.Range.IsValue)          // .. a valid argument tuple
                {
                    exprRange = DiagnosticTools.GetAbsoluteRange(offset, iterExpr.Range.Item);
                    argRange = DiagnosticTools.GetAbsoluteRange(offset, callLikeExression.Item2.Range.Item);
                    return true;
                }

                (exprRange, argRange) = (null, null);
                return false;
            }

            /// Returns the text edits for replacing an range over the indices with the corresponding library call if the given code fragment is a suitable for-loop intro.
            /// The returned edits do *not* include an edit for adding the corresponding open-directive if necessary.
            static IEnumerable<TextEdit> IndexRangeEdits(CodeFragment fragment)
            {
                if (fragment.Kind is QsFragmentKind.ForLoopIntro forLoopIntro && // todo: in principle we could give these suggestions for any index range
                    IsIndexRange(forLoopIntro.Item2, fragment.GetRange().Start, out var iterExprRange, out var argTupleRange))
                {
                    yield return new TextEdit()
                    {
                        Range = new LSP.Range() { Start = iterExprRange.Start, End = argTupleRange.Start },
                        NewText = BuiltIn.IndexRange.FullName.Name.Value
                    };
                    yield return new TextEdit()
                    {
                        Range = new LSP.Range() { Start = argTupleRange.End, End = iterExprRange.End },
                        NewText = ""
                    };
                }
            }

            var fragments = file.FragmentsOverlappingWithRange(range);
            var edits = fragments.SelectMany(IndexRangeEdits);
            var suggestedOpenDir = file.OpenDirectiveSuggestions(range.Start.Line, BuiltIn.IndexRange.FullName.Namespace);
            return edits.Any()
                ? new[] { ("Use IndexRange to iterate over indices.", file.GetWorkspaceEdit(suggestedOpenDir.Concat(edits).ToArray())) }
                : Enumerable.Empty<(string, WorkspaceEdit)>();
        }

        /// <summary>
        /// Returns a sequence of suggestions for removing code that is never executed based on the generated diagnostics,
        /// and given the file for which those diagnostics were generated.
        /// Returns an empty enumerable if any of the given arguments is null.
        /// </summary>
        internal static IEnumerable<(string, WorkspaceEdit)> SuggestionsForUnreachableCode
            (this FileContentManager file, IEnumerable<Diagnostic> diagnostics)
        {
            if (file == null || diagnostics == null) return Enumerable.Empty<(string, WorkspaceEdit)>();
            var unreachableCode = diagnostics.Where(DiagnosticTools.WarningType(WarningCode.UnreachableCode));

            WorkspaceEdit SuggestedRemoval(Position pos)
            {
                var fragment = file.TryGetFragmentAt(pos, out var currentFragToken);
                var lastFragToken = new CodeFragment.TokenIndex(currentFragToken);
                if (fragment == null || --lastFragToken == null) return null;

                // work off of the last reachable fragment, if there is one
                var lastBeforeErase = lastFragToken.GetFragment();
                var eraseStart = lastBeforeErase.GetRange().End;

                // find the last fragment in the scope
                while (currentFragToken != null)
                {
                    lastFragToken = currentFragToken;
                    currentFragToken = currentFragToken.NextOnScope(true);
                }
                var lastInScope = lastFragToken.GetFragment();
                var eraseEnd = lastInScope.GetRange().End;

                // determine the whitespace for the replacement string
                var lastLine = file.GetLine(lastFragToken.Line).Text.Substring(0, lastInScope.GetRange().Start.Character);
                var trimmedLastLine = lastLine.TrimEnd();
                var whitespace = lastLine[trimmedLastLine.Length..];

                // build the replacement string
                var replaceString = lastBeforeErase.FollowedBy == CodeFragment.MissingDelimiter ? "" : $"{lastBeforeErase.FollowedBy}";
                replaceString += eraseStart.Line == eraseEnd.Line ? " " : $"{Environment.NewLine}{whitespace}";

                // create and return a suitable edit
                var edit = new TextEdit { Range = new LSP.Range { Start = eraseStart, End = eraseEnd }, NewText = replaceString };
                return file.GetWorkspaceEdit(edit);
            }

            return unreachableCode
                .Select(d => SuggestedRemoval(d.Range?.Start))
                .Where(edit => edit != null)
                .Select(edit => ("Remove unreachable code.", edit));
        }

        /// <summary>
        /// Returns a sequence of suggestions to insert doc comments for an undocumented declaration that overlap with the given range in the given file.
        /// Returns an empty enumerable if more than one code fragment overlaps with the given range,
        /// or the overlapping fragment does not contain a declaration,
        /// or the overlapping fragment contains a declaration that is already documented,
        /// or if any of the given arguments is null.
        /// </summary>
        internal static IEnumerable<(string, WorkspaceEdit)> DocCommentSuggestions(this FileContentManager file, LSP.Range range)
        {
            var overlapping = file?.FragmentsOverlappingWithRange(range);
            var fragment = overlapping?.FirstOrDefault();
            if (fragment?.Kind == null || overlapping.Count() != 1) return Enumerable.Empty<(string, WorkspaceEdit)>(); // only suggest doc comment directly on the declaration

            var (nsDecl, callableDecl, typeDecl) = (fragment.Kind.DeclaredNamespace(), fragment.Kind.DeclaredCallable(), fragment.Kind.DeclaredType());
            var declSymbol = nsDecl.IsValue ? nsDecl.Item.Item1.Symbol
                : callableDecl.IsValue ? callableDecl.Item.Item1.Symbol
                : typeDecl.IsValue ? typeDecl.Item.Item1.Symbol : null;
            var declStart = fragment.GetRange().Start;
            if (declSymbol == null || file.DocumentingComments(declStart).Any()) return Enumerable.Empty<(string, WorkspaceEdit)>();

            // set declStart to the position of the first attribute attached to the declaration
            static bool EmptyOrFirstAttribute(IEnumerable<CodeFragment> line, out CodeFragment att)
            {
                att = line?.Reverse().TakeWhile(t => t.Kind is QsFragmentKind.DeclarationAttribute).LastOrDefault();
                return att != null || (line != null && !line.Any());
            }
            var preceding = file.GetTokenizedLine(declStart.Line).TakeWhile(ContextBuilder.TokensUpTo(new Position(0, declStart.Character)));
            for (var lineNr = declStart.Line; EmptyOrFirstAttribute(preceding, out var precedingAttribute); )
            {
                if (precedingAttribute != null)
                { declStart = precedingAttribute.GetRange().Start.WithUpdatedLineNumber(lineNr); }
                preceding = lineNr-- > 0 ? file.GetTokenizedLine(lineNr) : (IEnumerable<CodeFragment>)null;
            }

            var docPrefix = "/// ";
            var endLine = $"{Environment.NewLine}{file.GetLine(declStart.Line).Text.Substring(0, declStart.Character)}";
            var docString = $"{docPrefix}# Summary{endLine}{docPrefix}{endLine}";

            var (argTuple, typeParams) =
                callableDecl.IsValue ? (callableDecl.Item.Item2.Item3.Argument,
                                        callableDecl.Item.Item2.Item3.TypeParameters)
                : typeDecl.IsValue ? (typeDecl.Item.Item2.Item2, ImmutableArray<QsSymbol>.Empty)
                : (null, ImmutableArray<QsSymbol>.Empty);
            var hasOutput = callableDecl.IsValue && !callableDecl.Item.Item2.Item3.ReturnType.Type.IsUnitType;

            var args = argTuple == null ? ImmutableArray<Tuple<QsSymbol, QsType>>.Empty : SyntaxGenerator.ExtractItems(argTuple);
            docString = String.Concat(
                docString,
                // Document Input Parameters
                args.Any() ? $"{docPrefix}# Input{endLine}" : String.Empty,
                String.Concat(args.Select(x => $"{docPrefix}## {x.Item1.Symbol.AsDeclarationName(null)}{endLine}{docPrefix}{endLine}")),
                // Document Output
                hasOutput ? $"{docPrefix}# Output{endLine}{docPrefix}{endLine}" : String.Empty,
                // Document Type Parameters
                typeParams.Any() ? $"{docPrefix}# Type Parameters{endLine}" : String.Empty,
                String.Concat(typeParams.Select(x => $"{docPrefix}## '{x.Symbol.AsDeclarationName(null)}{endLine}{docPrefix}{endLine}"))
            );

            var whichDecl = $" for {declSymbol.AsDeclarationName(null)}";
            var suggestedEdit = file.GetWorkspaceEdit(new TextEdit { Range = new LSP.Range { Start = declStart, End = declStart }, NewText = docString });
            return new[] { ($"Add documentation{whichDecl}.", suggestedEdit) };
        }
    }
}