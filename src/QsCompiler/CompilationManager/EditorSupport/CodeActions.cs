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
                var fragment = file.TryGetFragmentAt(d.Range.Start, out var _);
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
        /// Returns a sequence of suggestions for removing code that is never executed based on the generated diagnostics, 
        /// and given the file for which those diagnostics were generated. 
        /// Returns an empty enumerable if any of the given arguments is null. 
        /// </summary>
        internal static IEnumerable<(string, WorkspaceEdit)> SuggestionsForUnreachableCode
            (this FileContentManager file, IEnumerable<Diagnostic> diagnostics)
        {
            if (file == null || diagnostics == null) return Enumerable.Empty<(string, WorkspaceEdit)>();
            var unreachableCode = diagnostics.Where(DiagnosticTools.WarningType(WarningCode.UnreachableCode));

            WorkspaceEdit SuggestedRemoveText(Position pos)
            {
                var fragment = file.TryGetFragmentAt(pos, out var currentFragToken);
                if (fragment == null) return null;

                var eraseStart = fragment.GetRange().Start;
                var lastFragToken = new CodeFragment.TokenIndex(currentFragToken);
                --lastFragToken;
                CodeFragment tempFrag = null;
                string lastFollowedBy = "";

                // Work off of the last reachable fragment, if there is one
                if (lastFragToken != null)
                {
                    tempFrag = lastFragToken.GetFragment();
                    lastFollowedBy = $"{tempFrag.FollowedBy}";
                    eraseStart = tempFrag.GetRange().End;
                }

                // Find the last fragment in the scope
                while (currentFragToken != null)
                {
                    lastFragToken = currentFragToken;
                    currentFragToken = currentFragToken.NextOnScope(true);
                }
                tempFrag = lastFragToken.GetFragment();
                Position eraseEnd = tempFrag.GetRange().End;

                // Build replace string
                string replaceString = lastFollowedBy;
                if (eraseStart.Line != eraseEnd.Line)
                {
                    replaceString += $"{Environment.NewLine}";
                    // Give it the indentation of the parent scope
                    // ToDo: Support the tab style specified by the editor
                    replaceString += String.Concat(Enumerable.Repeat("\t", tempFrag.Indentation - 1));
                }
                else
                {
                    replaceString += " ";
                }

                // Create and Return Edit
                var edit = new TextEdit { Range = new Range { Start = eraseStart, End = eraseEnd }, NewText = replaceString };
                return file.GetWorkspaceEdit(edit);
            }

            return unreachableCode
                .Select(d => SuggestedRemoveText(d.Range?.Start))
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
        internal static IEnumerable<(string, WorkspaceEdit)> DocCommentSuggestions(this FileContentManager file, Range range)
        {
            if (file == null || range?.Start == null || range.End == null) return Enumerable.Empty<(string, WorkspaceEdit)>();
            var (start, end) = (range.Start.Line, range.End.Line);

            var fragAtStart = file.TryGetFragmentAt(range?.Start, out var _, includeEnd: true);
            var inRange = file.GetTokenizedLine(start).Select(t => t.WithUpdatedLineNumber(start)).Where(ContextBuilder.TokensAfter(range.Start)); // does not include fragAtStart
            inRange = start == end
                ? inRange.Where(ContextBuilder.TokensStartingBefore(range.End))
                : inRange.Concat(file.GetTokenizedLines(start + 1, end - start - 1).SelectMany((x, i) => x.Select(t => t.WithUpdatedLineNumber(start + 1 + i))))
                    .Concat(file.GetTokenizedLine(end).Select(t => t.WithUpdatedLineNumber(end)).Where(ContextBuilder.TokensStartingBefore(range.End)));
            var fragment = 
                fragAtStart != null && !inRange.Any() ? fragAtStart :
                fragAtStart == null ? inRange.FirstOrDefault() : null;
            var declRange = fragment?.GetRange();

            if (fragment == null) return Enumerable.Empty<(string, WorkspaceEdit)>(); // only suggest doc comment directly on the declaration
            var (nsDecl, callableDecl, typeDecl) = (fragment.Kind.DeclaredNamespace(), fragment.Kind.DeclaredCallable(), fragment.Kind.DeclaredType());
            var declSymbol = nsDecl.IsValue ? nsDecl.Item.Item1.Symbol 
                : callableDecl.IsValue ? callableDecl.Item.Item1.Symbol
                : typeDecl.IsValue ? typeDecl.Item.Item1.Symbol : null;
            if (declSymbol == null || file.DocumentingComments(declRange.Start).Any()) return Enumerable.Empty<(string, WorkspaceEdit)>();

            var docPrefix = "///";
            var endLine = $"{Environment.NewLine}{file.GetLine(declRange.Start.Line).Text.Substring(0, declRange.Start.Character)}";
            var docString = $"{docPrefix}# Summary{endLine}{docPrefix}{endLine}";

            var (argTuple, typeParams) =
                callableDecl.IsValue ? (callableDecl.Item.Item2.Item2.Argument, callableDecl.Item.Item2.Item2.TypeParameters) :
                typeDecl.IsValue ? (typeDecl.Item.Item2, ImmutableArray<QsSymbol>.Empty) :
                (null, ImmutableArray<QsSymbol>.Empty);

            var args = argTuple == null ? ImmutableArray<Tuple<QsSymbol, QsType>>.Empty : SyntaxGenerator.ExtractItems(argTuple);
            docString = String.Concat(
                docString,
                // Document Input Parameters
                args.Any() ? $"{docPrefix}# Input{endLine}" : String.Empty,
                String.Concat(args.Select(x => $"{docPrefix}## {x.Item1.Symbol.AsDeclarationName(null)}{endLine}{docPrefix}{endLine}")),
                // Document Type Parameters
                typeParams.Any() ? $"{docPrefix}# Type Parameters{endLine}" : String.Empty,
                String.Concat(typeParams.Select(x => $"{docPrefix}## {x.Symbol.AsDeclarationName(null)}{endLine}{docPrefix}{endLine}"))
            );

            var whichDecl = $" for {declSymbol.AsDeclarationName(null)}";
            var suggestedEdit = file.GetWorkspaceEdit(new TextEdit { Range = new Range { Start = declRange.Start, End = declRange.Start }, NewText = docString });
            return new[] { ($"Add documentation{whichDecl}.", suggestedEdit) };
        }
    }
}