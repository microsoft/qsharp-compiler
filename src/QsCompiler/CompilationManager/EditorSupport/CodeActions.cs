// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.TextProcessing;
using Microsoft.Quantum.QsCompiler.Transformations;
using Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Lsp = Microsoft.VisualStudio.LanguageServer.Protocol;
using Position = Microsoft.Quantum.QsCompiler.DataTypes.Position;
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    using QsTypeKind = QsTypeKind<QsType, QsSymbol, QsSymbol, Characteristics>;

    internal static class SuggestedEdits
    {
        /// <summary>
        /// Returns the given edit for the specified file as WorkspaceEdit.
        /// </summary>
        private static WorkspaceEdit GetWorkspaceEdit(this FileContentManager file, params TextEdit[] edits)
        {
            var versionedFileId = new VersionedTextDocumentIdentifier { Uri = file.Uri, Version = 1 }; // setting version to null here won't work in VS Code ...
            return new WorkspaceEdit
            {
                DocumentChanges = new[] { new TextDocumentEdit { TextDocument = versionedFileId, Edits = edits } },
                Changes = new Dictionary<string, TextEdit[]> { { file.FileName, edits } }
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
        private static IEnumerable<string> IdNamespaceSuggestions(
            this FileContentManager file, Position pos, CompilationUnit compilation, out string? idName)
        {
            var variables = file?.TryGetQsSymbolInfo(pos, true, out CodeFragment _)?.UsedVariables;
            idName = variables != null && variables.Any() ? variables.Single().Symbol.AsDeclarationName(null) : null;
            return idName != null && compilation != null
                ? compilation.GlobalSymbols.NamespacesContainingCallable(idName)
                : ImmutableArray<string>.Empty;
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
        private static IEnumerable<string> TypeNamespaceSuggestions(
            this FileContentManager file, Position pos, CompilationUnit compilation, out string? typeName)
        {
            var types = file?.TryGetQsSymbolInfo(pos, true, out CodeFragment _)?.UsedTypes;
            typeName = types != null && types.Any() &&
                types.Single().Type is QsTypeKind<QsType, QsSymbol, QsSymbol, Characteristics>.UserDefinedType udt
                ? udt.Item.Symbol.AsDeclarationName(null) : null;
            return typeName != null && compilation != null
                ? compilation.GlobalSymbols.NamespacesContainingType(typeName)
                : ImmutableArray<string>.Empty;
        }

        /// <summary>
        /// Returns names that match an alternative casing of the identifier at the given position in the file, or the
        /// empty enumerable if there is no valid identifier at the given position.
        /// </summary>
        private static IEnumerable<string> IdCaseSuggestions(
            this FileContentManager file, Position pos, CompilationUnit compilation)
        {
            IEnumerable<string> AlternateNames(string name) =>
                from callable in compilation.GlobalSymbols.AccessibleCallables()
                let otherName = callable.QualifiedName.Name
                where otherName != name && otherName.Equals(name, StringComparison.OrdinalIgnoreCase)
                select otherName;

            var symbolKind = file.TryGetQsSymbolInfo(pos, true, out _)?.UsedVariables?.SingleOrDefault()?.Symbol;
            return symbolKind.AsDeclarationName(null)?.Apply(AlternateNames) ?? Enumerable.Empty<string>();
        }

        /// <summary>
        /// Returns names that match an alternative casing of the type name at the given position in the file, or the
        /// empty enumerable if there is no valid type name at the given position.
        /// </summary>
        private static IEnumerable<string> TypeCaseSuggestions(
            this FileContentManager file, Position pos, CompilationUnit compilation)
        {
            IEnumerable<string> AlternateNames(string name) =>
                from type in compilation.GlobalSymbols.AccessibleTypes()
                let otherName = type.QualifiedName.Name
                where otherName != name && otherName.Equals(name, StringComparison.OrdinalIgnoreCase)
                select otherName;

            var typeKind = file.TryGetQsSymbolInfo(pos, true, out _)?.UsedTypes?.SingleOrDefault()?.Type;
            var udt = typeKind as QsTypeKind.UserDefinedType;
            return udt?.Item.Symbol.AsDeclarationName(null)?.Apply(AlternateNames) ?? Enumerable.Empty<string>();
        }

        /// <summary>
        /// Returns all code fragments in the specified file that overlap with the given range.
        /// Returns an empty sequence if any of the given arguments is null.
        /// </summary>
        private static IEnumerable<CodeFragment> FragmentsOverlappingWithRange(this FileContentManager file, Range range)
        {
            if (file is null || range is null)
            {
                return Enumerable.Empty<CodeFragment>();
            }
            var (start, end) = (range.Start.Line, range.End.Line);

            var fragAtStart = file.TryGetFragmentAt(range.Start, out _, includeEnd: true);
            var inRange = file.GetTokenizedLine(start).Select(t => t.WithLineNumOffset(start)).Where(ContextBuilder.TokensAfter(range.Start)); // does not include fragAtStart
            inRange = start == end
                ? inRange.Where(ContextBuilder.TokensStartingBefore(range.End))
                : inRange.Concat(file.GetTokenizedLines(start + 1, end - start - 1).SelectMany((x, i) => x.Select(t => t.WithLineNumOffset(start + 1 + i))))
                    .Concat(file.GetTokenizedLine(end).Select(t => t.WithLineNumOffset(end)).Where(ContextBuilder.TokensStartingBefore(range.End)));

            var fragments = ImmutableArray.CreateBuilder<CodeFragment>();
            if (fragAtStart != null)
            {
                fragments.Add(fragAtStart);
            }
            fragments.AddRange(inRange);
            return fragments.ToImmutableArray();
        }

        /// <summary>
        /// Return an enumerable of suitable edits to add open directives for all given namespaces for which no open directive already exists.
        /// Returns an edit for opening a given namespace even if an alias is already defined for that namespace.
        /// Returns an empty enumerable if suitable edits could not be determined.
        /// </summary>
        private static IEnumerable<TextEdit> OpenDirectiveSuggestions(this FileContentManager file, int lineNr, IEnumerable<string> namespaces)
        {
            // determine the first fragment in the containing namespace
            var nsElements = file?.NamespaceDeclarationTokens()
                .TakeWhile(t => t.Line <= lineNr).LastOrDefault() // going by line here is fine - inaccuracies if someone has multiple namespace and callable declarations on the same line seem acceptable...
                ?.GetChildren(deep: false);
            var firstInNs = nsElements?.FirstOrDefault()?.GetFragment();
            if (file is null || nsElements is null || firstInNs?.Kind == null)
            {
                return Enumerable.Empty<TextEdit>();
            }

            // determine what open directives already exist
            var insertOpenDirAt = firstInNs.Range.Start;
            var openDirs = nsElements.SelectNotNull(t => t.GetFragment().Kind?.OpenedNamespace())
                .TakeWhile(opened => opened.IsValue)
                .Select(opened => (
                    opened.Item.Item1.Symbol.AsDeclarationName(null),
                    opened.Item.Item2.IsValue ? opened.Item.Item2.Item.Symbol.AsDeclarationName("") : null))
                .Where(opened => opened.Item1 != null)
                .GroupBy(opened => opened.Item1, opened => opened.Item2) // in case there are duplicate open directives...
                .ToImmutableDictionary(opened => opened.Key, opened => opened.First());

            // range and whitespace info for inserting open directives
            var additionalLinesAfterOpenDir = firstInNs.Kind.OpenedNamespace().IsNull ? $"{Environment.NewLine}{Environment.NewLine}" : "";
            var indentationAfterOpenDir = file.GetLine(insertOpenDirAt.Line).Text.Substring(0, insertOpenDirAt.Column);
            var whitespaceAfterOpenDir = $"{Environment.NewLine}{additionalLinesAfterOpenDir}{(string.IsNullOrWhiteSpace(indentationAfterOpenDir) ? indentationAfterOpenDir : "    ")}";

            // construct a suitable edit
            return namespaces.Distinct().Where(ns => !openDirs.Contains(ns, null)).Select(suggestedNS => // filter all namespaces that are already open
            {
                var directive = $"{Keywords.importDirectiveHeader.id} {suggestedNS}";
                return new TextEdit
                {
                    Range = new Lsp.Range { Start = insertOpenDirAt.ToLsp(), End = insertOpenDirAt.ToLsp() },
                    NewText = $"{directive};{whitespaceAfterOpenDir}"
                };
            });
        }

        /// <summary>
        /// Returns a sequence of suggestions on how errors for ambiguous types and callable in the given diagnostics can be fixed,
        /// given the file for which those diagnostics were generated and the corresponding compilation.
        /// Returns an empty enumerable if any of the given arguments is null.
        /// </summary>
        internal static IEnumerable<(string, WorkspaceEdit)> AmbiguousIdSuggestions(
            this FileContentManager file, CompilationUnit compilation, IEnumerable<Diagnostic> diagnostics)
        {
            if (file == null || diagnostics == null)
            {
                return Enumerable.Empty<(string, WorkspaceEdit)>();
            }
            var ambiguousCallables = diagnostics.Where(DiagnosticTools.ErrorType(ErrorCode.AmbiguousCallable));
            var ambiguousTypes = diagnostics.Where(DiagnosticTools.ErrorType(ErrorCode.AmbiguousType));
            if (!ambiguousCallables.Any() && !ambiguousTypes.Any())
            {
                return Enumerable.Empty<(string, WorkspaceEdit)>();
            }

            (string, WorkspaceEdit)? SuggestedNameQualification(string suggestedNS, string? id, Position pos)
            {
                var edit = new TextEdit
                {
                    Range = new Lsp.Range { Start = pos.ToLsp(), End = pos.ToLsp() },
                    NewText = $"{suggestedNS}."
                };
                return id is null
                    ? null as (string, WorkspaceEdit)?
                    : ($"{suggestedNS}.{id}", file.GetWorkspaceEdit(edit));
            }

            var suggestedIdQualifications = ambiguousCallables
                .Select(d => d.Range.Start.ToQSharp())
                .SelectMany(pos => file
                    .IdNamespaceSuggestions(pos, compilation, out var id)
                    .SelectNotNull(ns => SuggestedNameQualification(ns, id, pos)));
            var suggestedTypeQualifications = ambiguousTypes
                .Select(d => d.Range.Start.ToQSharp())
                .SelectMany(pos => file
                    .TypeNamespaceSuggestions(pos, compilation, out var id)
                    .SelectNotNull(ns => SuggestedNameQualification(ns, id, pos)));
            return suggestedIdQualifications.Concat(suggestedTypeQualifications);
        }

        /// <summary>
        /// Returns workspace edits for opening namespaces of unknown identifiers.
        /// </summary>
        private static IEnumerable<(string, WorkspaceEdit)> UnknownIdNamespaceSuggestions(
            this FileContentManager file,
            CompilationUnit compilation,
            int lineNr,
            IReadOnlyCollection<Diagnostic> diagnostics)
        {
            var idSuggestions = diagnostics
                .Where(DiagnosticTools.ErrorType(ErrorCode.UnknownIdentifier))
                .SelectMany(d => file.IdNamespaceSuggestions(d.Range.Start.ToQSharp(), compilation, out _));
            var typeSuggestions = diagnostics
                .Where(DiagnosticTools.ErrorType(ErrorCode.UnknownType))
                .SelectMany(d => file.TypeNamespaceSuggestions(d.Range.Start.ToQSharp(), compilation, out _));
            return file
                .OpenDirectiveSuggestions(lineNr, idSuggestions.Concat(typeSuggestions))
                .Select(edit => (edit.NewText.Trim().Trim(';'), file.GetWorkspaceEdit(edit)));
        }

        /// <summary>
        /// Returns workspace edits for correcting the casing or capitalization of unknown identifiers.
        /// </summary>
        private static IEnumerable<(string, WorkspaceEdit)> UnknownIdCaseSuggestions(
            this FileContentManager file, CompilationUnit compilation, IReadOnlyCollection<Diagnostic> diagnostics)
        {
            (string, WorkspaceEdit) SuggestedIdEdit(string suggestedId, Lsp.Range range)
            {
                var edit = new TextEdit { Range = range, NewText = suggestedId };
                return ($"Replace with \"{suggestedId}\".", file.GetWorkspaceEdit(edit));
            }

            var idSuggestions =
                from diagnostic in diagnostics
                where DiagnosticTools.ErrorType(ErrorCode.UnknownIdentifier)(diagnostic)
                from id in file.IdCaseSuggestions(diagnostic.Range.Start.ToQSharp(), compilation)
                select SuggestedIdEdit(id, diagnostic.Range);
            var typeSuggestions =
                from diagnostic in diagnostics
                where DiagnosticTools.ErrorType(ErrorCode.UnknownType)(diagnostic)
                from type in file.TypeCaseSuggestions(diagnostic.Range.Start.ToQSharp(), compilation)
                select SuggestedIdEdit(type, diagnostic.Range);
            return idSuggestions.Concat(typeSuggestions);
        }

        /// <summary>
        /// Returns a sequence of suggestions on how errors for unknown types and callable in the given diagnostics can be fixed,
        /// given the file for which those diagnostics were generated and the corresponding compilation.
        /// The given line number is used to determine the containing namespace.
        /// Returns an empty enumerable if any of the given arguments is null.
        /// </summary>
        internal static IEnumerable<(string, WorkspaceEdit)> UnknownIdSuggestions(
            this FileContentManager file,
            CompilationUnit compilation,
            int lineNr,
            IReadOnlyCollection<Diagnostic> diagnostics) =>
            UnknownIdNamespaceSuggestions(file, compilation, lineNr, diagnostics)
                .Concat(UnknownIdCaseSuggestions(file, compilation, diagnostics));

        /// <summary>
        /// Returns a sequence of suggestions on how deprecated syntax can be updated based on the generated diagnostics,
        /// and given the file for which those diagnostics were generated.
        /// Returns an empty enumerable if any of the given arguments is null.
        /// </summary>
        internal static IEnumerable<(string, WorkspaceEdit)> DeprecatedSyntaxSuggestions(
            this FileContentManager file, IReadOnlyCollection<Diagnostic> diagnostics)
        {
            (string, WorkspaceEdit) ReplaceWith(string text, Lsp.Range range)
            {
                static bool NeedsWs(char ch) => char.IsLetterOrDigit(ch) || ch == '_';
                if (range.Start != null && range.End != null)
                {
                    var beforeEdit = file.GetLine(range.Start.Line).Text.Substring(0, range.Start.Character);
                    var afterEdit = file.GetLine(range.End.Line).Text.Substring(range.End.Character);
                    if (beforeEdit.Any() && NeedsWs(beforeEdit.Last()))
                    {
                        text = $" {text}";
                    }
                    if (afterEdit.Any() && NeedsWs(afterEdit.First()))
                    {
                        text = $"{text} ";
                    }
                }
                var edit = new TextEdit { Range = range, NewText = text };
                return ($"Replace with \"{text.Trim()}\".", file.GetWorkspaceEdit(edit));
            }

            // update deprecated keywords and operators
            var unitSuggestions = diagnostics.Where(DiagnosticTools.WarningType(WarningCode.DeprecatedUnitType))
                .Select(d => ReplaceWith(Keywords.qsUnit.id, d.Range));
            var notSuggestions = diagnostics.Where(DiagnosticTools.WarningType(WarningCode.DeprecatedNOToperator))
                .Select(d => ReplaceWith(Keywords.qsNOTop.op, d.Range));
            var andSuggestions = diagnostics.Where(DiagnosticTools.WarningType(WarningCode.DeprecatedANDoperator))
                .Select(d => ReplaceWith(Keywords.qsANDop.op, d.Range));
            var orSuggestions = diagnostics.Where(DiagnosticTools.WarningType(WarningCode.DeprecatedORoperator))
                .Select(d => ReplaceWith(Keywords.qsORop.op, d.Range));

            // update deprecated operation characteristics syntax

            static IEnumerable<Characteristics> GetCharacteristics(QsTuple<Tuple<QsSymbol, QsType>> argTuple) =>
                SyntaxGenerator.ExtractItems(argTuple)
                    .SelectMany(item => item.Item2.ExtractCharacteristics())
                    .Distinct();

            static string CharacteristicsAnnotation(Characteristics c)
            {
                var charEx = SyntaxTreeToQsharp.CharacteristicsExpression(SymbolResolution.ResolveCharacteristics(c));
                return charEx == null ? "" : $"{Keywords.qsCharacteristics.id} {charEx}";
            }

            var characteristicsSuggestions = diagnostics
                .Where(DiagnosticTools.WarningType(WarningCode.DeprecatedOpCharacteristics))
                .SelectMany(diagnostic =>
                {
                    // TODO: TryGetQsSymbolInfo currently only returns information about the inner most leafs rather
                    // than all types etc.
                    var fragment = file.TryGetFragmentAt(diagnostic.Range.Start.ToQSharp(), out _);
                    if (fragment is null)
                    {
                        return Enumerable.Empty<(string, WorkspaceEdit)>();
                    }
                    var characteristics = fragment.Kind switch
                    {
                        QsFragmentKind.FunctionDeclaration function => GetCharacteristics(function.Item3.Argument),
                        QsFragmentKind.OperationDeclaration operation => GetCharacteristics(operation.Item3.Argument),
                        QsFragmentKind.TypeDefinition type => GetCharacteristics(type.Item3),
                        _ => Enumerable.Empty<Characteristics>()
                    };
                    return
                        from characteristic in characteristics
                        let range = fragment.Range.Start + characteristic.Range.Item
                        where characteristic.Range.IsValue && Range.Overlaps(range, diagnostic.Range.ToQSharp())
                        select ReplaceWith(CharacteristicsAnnotation(characteristic), diagnostic.Range);
                });

            return characteristicsSuggestions
                .Concat(unitSuggestions)
                .Concat(notSuggestions)
                .Concat(andSuggestions)
                .Concat(orSuggestions);
        }

        /// <summary>
        /// Returns a sequence of suggestions for update-and-reassign statements based on the generated diagnostics,
        /// and given the file for which those diagnostics were generated.
        /// Returns an empty enumerable if any of the given arguments is null.
        /// </summary>
        internal static IEnumerable<(string, WorkspaceEdit)> UpdateReassignStatementSuggestions(
            this FileContentManager file, IEnumerable<Diagnostic> diagnostics)
        {
            var updateOfArrayItemExprs = diagnostics.Where(DiagnosticTools.ErrorType(ErrorCode.UpdateOfArrayItemExpr));

            (string, WorkspaceEdit?) SuggestedCopyAndUpdateExpr(CodeFragment fragment)
            {
                var exprInfo = Parsing.ProcessUpdateOfArrayItemExpr.Invoke(fragment.Text);
                // Skip if the statement did not match a pattern for which we can give a code action
                if (exprInfo == null || (exprInfo.Item1.Line == 1 && exprInfo.Item1.Column == 1))
                {
                    return ("", null);
                }

                // Convert set <identifier>[<index>] = <rhs> to set <identifier> w/= <index> <- <rhs>
                var rhs = $"{exprInfo.Item3} {Keywords.qsCopyAndUpdateOp.cont} {exprInfo.Item4}";
                var outputStr = $"{Keywords.qsValueUpdate.id} {exprInfo.Item2} {Keywords.qsCopyAndUpdateOp.op}= {rhs}";
                var edit = new TextEdit { Range = fragment.Range.ToLsp(), NewText = outputStr };
                return ("Replace with an update-and-reassign statement.", file.GetWorkspaceEdit(edit));
            }

            return updateOfArrayItemExprs
                .SelectNotNull(d => file?.TryGetFragmentAt(d.Range.Start.ToQSharp(), out var _, includeEnd: true))
                .Select(frag => SuggestedCopyAndUpdateExpr(frag))
                .SelectNotNull(s => s.Item2 is null ? null as (string, WorkspaceEdit)? : (s.Item1, s.Item2));
        }

        /// <summary>
        /// Returns true the given expression is of the form "0 .. Length(args) - 1",
        /// as well as the range of the entire expression and the argument tuple "(args)" as out parameters.
        /// </summary>
        private static bool IsIndexRange(
            QsExpression iterExpr,
            Position offset,
            [NotNullWhen(true)] out Range? exprRange,
            [NotNullWhen(true)] out Range? argRange)
        {
            if (
                // iterable expression is a valid range literal
                iterExpr.Expression is QsExpressionKind<QsExpression, QsSymbol, QsType>.RangeLiteral rangeExpression && iterExpr.Range.IsValue &&
                // .. starting at 0 ..
                rangeExpression.Item1.Expression is QsExpressionKind<QsExpression, QsSymbol, QsType>.IntLiteral intLiteralExpression && intLiteralExpression.Item == 0L &&
                // .. and ending in subtracting ..
                rangeExpression.Item2.Expression is QsExpressionKind<QsExpression, QsSymbol, QsType>.SUB sUBExpression &&
                // .. 1 from ..
                sUBExpression.Item2.Expression is QsExpressionKind<QsExpression, QsSymbol, QsType>.IntLiteral subIntLiteralExpression && subIntLiteralExpression.Item == 1L &&
                // .. a call ..
                sUBExpression.Item1.Expression is QsExpressionKind<QsExpression, QsSymbol, QsType>.CallLikeExpression callLikeExression &&
                // .. to and identifier ..
                callLikeExression.Item1.Expression is QsExpressionKind<QsExpression, QsSymbol, QsType>.Identifier identifier &&
                // .. "Length" called with ..
                identifier.Item1.Symbol is QsSymbolKind<QsSymbol>.Symbol symName && symName.Item == BuiltIn.Length.FullName.Name &&
                // .. a valid argument tuple
                callLikeExression.Item2.Expression is QsExpressionKind<QsExpression, QsSymbol, QsType>.ValueTuple valueTuple && callLikeExression.Item2.Range.IsValue)
            {
                exprRange = offset + iterExpr.Range.Item;
                argRange = offset + callLikeExression.Item2.Range.Item;
                return true;
            }

            (exprRange, argRange) = (null, null);
            return false;
        }

        /// <summary>
        /// Returns a sequence of suggestions for replacing ranges over array indices with the corresponding library call,
        /// provided the corresponding library is referenced.
        /// Returns an empty enumerable if this is not the case or any of the given arguments is null.
        /// </summary>
        internal static IEnumerable<(string, WorkspaceEdit)> IndexRangeSuggestions(
            this FileContentManager file, CompilationUnit compilation, Range range)
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
                nsName,
                file.FileName);
            if (!indexRange.IsFound)
            {
                return Enumerable.Empty<(string, WorkspaceEdit)>();
            }

            // Returns the text edits for replacing an range over the indices with the corresponding library call if the given code fragment is a suitable for-loop intro.
            // The returned edits do *not* include an edit for adding the corresponding open-directive if necessary.
            static IEnumerable<TextEdit> IndexRangeEdits(CodeFragment fragment)
            {
                if (fragment.Kind is QsFragmentKind.ForLoopIntro forLoopIntro && // todo: in principle we could give these suggestions for any index range
                    IsIndexRange(forLoopIntro.Item2, fragment.Range.Start, out var iterExprRange, out var argTupleRange))
                {
                    yield return new TextEdit
                    {
                        Range = new Lsp.Range { Start = iterExprRange.Start.ToLsp(), End = argTupleRange.Start.ToLsp() },
                        NewText = BuiltIn.IndexRange.FullName.Name
                    };
                    yield return new TextEdit
                    {
                        Range = new Lsp.Range { Start = argTupleRange.End.ToLsp(), End = iterExprRange.End.ToLsp() },
                        NewText = ""
                    };
                }
            }

            var fragments = file.FragmentsOverlappingWithRange(range);
            var edits = fragments.SelectMany(IndexRangeEdits);
            var suggestedOpenDir =
                file.OpenDirectiveSuggestions(range.Start.Line, new[] { BuiltIn.IndexRange.FullName.Namespace });
            return edits.Any()
                ? new[] { ("Use IndexRange to iterate over indices.", file.GetWorkspaceEdit(suggestedOpenDir.Concat(edits).ToArray())) }
                : Enumerable.Empty<(string, WorkspaceEdit)>();
        }

        /// <summary>
        /// Returns a sequence of suggestions for removing code that is never executed based on the generated diagnostics,
        /// and given the file for which those diagnostics were generated.
        /// Returns an empty enumerable if any of the given arguments is null.
        /// </summary>
        internal static IEnumerable<(string, WorkspaceEdit)> UnreachableCodeSuggestions(
            this FileContentManager file, IEnumerable<Diagnostic> diagnostics)
        {
            if (file == null || diagnostics == null)
            {
                return Enumerable.Empty<(string, WorkspaceEdit)>();
            }
            var unreachableCode = diagnostics.Where(DiagnosticTools.WarningType(WarningCode.UnreachableCode));

            WorkspaceEdit? SuggestedRemoval(Position? pos)
            {
                var fragment = file.TryGetFragmentAt(pos, out var currentFragToken);
                var lastFragToken = currentFragToken?.Apply(token => new CodeFragment.TokenIndex(token).Previous());
                if (fragment == null || lastFragToken == null)
                {
                    return null;
                }

                // work off of the last reachable fragment, if there is one
                var lastBeforeErase = lastFragToken.GetFragment();
                var eraseStart = lastBeforeErase.Range.End;

                // find the last fragment in the scope
                while (currentFragToken != null)
                {
                    lastFragToken = currentFragToken;
                    currentFragToken = currentFragToken.NextOnScope(true);
                }
                var lastInScope = lastFragToken.GetFragment();
                var eraseEnd = lastInScope.Range.End;

                // determine the whitespace for the replacement string
                var lastLine = file.GetLine(lastFragToken.Line).Text.Substring(0, lastInScope.Range.Start.Column);
                var trimmedLastLine = lastLine.TrimEnd();
                var whitespace = lastLine[trimmedLastLine.Length..];

                // build the replacement string
                var replaceString = lastBeforeErase.FollowedBy == CodeFragment.MissingDelimiter ? "" : $"{lastBeforeErase.FollowedBy}";
                replaceString += eraseStart.Line == eraseEnd.Line ? " " : $"{Environment.NewLine}{whitespace}";

                // create and return a suitable edit
                var edit = new TextEdit { Range = new Lsp.Range { Start = eraseStart.ToLsp(), End = eraseEnd.ToLsp() }, NewText = replaceString };
                return file.GetWorkspaceEdit(edit);
            }

            return unreachableCode
                .SelectNotNull(d => SuggestedRemoval(d.Range?.Start.ToQSharp()))
                .Select(edit => ("Remove unreachable code.", edit));
        }

        /// <summary>
        /// Returns a sequence of suggestions to insert doc comments for an undocumented declaration that overlap with the given range in the given file.
        /// Returns an empty enumerable if more than one code fragment overlaps with the given range,
        /// or the overlapping fragment does not contain a declaration,
        /// or the overlapping fragment contains a declaration that is already documented,
        /// or if any of the given arguments is null.
        /// </summary>
        /// <exception cref="FileContentException">The file content changed while processing.</exception>
        internal static IEnumerable<(string, WorkspaceEdit)> DocCommentSuggestions(
            this FileContentManager file, Range range)
        {
            var overlapping = file.FragmentsOverlappingWithRange(range);
            var fragment = overlapping.FirstOrDefault();
            if (fragment?.Kind == null || overlapping.Count() != 1)
            {
                return Enumerable.Empty<(string, WorkspaceEdit)>(); // only suggest doc comment directly on the declaration
            }

            var (nsDecl, callableDecl, typeDecl) = (fragment.Kind.DeclaredNamespace(), fragment.Kind.DeclaredCallable(), fragment.Kind.DeclaredType());
            var declSymbol = nsDecl.IsValue ? nsDecl.Item.Item1.Symbol
                : callableDecl.IsValue ? callableDecl.Item.Item1.Symbol
                : typeDecl.IsValue ? typeDecl.Item.Item1.Symbol : null;
            var declStart = fragment.Range.Start;
            if (declSymbol == null || file.DocumentingComments(declStart).Any())
            {
                return Enumerable.Empty<(string, WorkspaceEdit)>();
            }

            // set declStart to the position of the first attribute attached to the declaration
            static bool EmptyOrFirstAttribute(IEnumerable<CodeFragment>? line, out CodeFragment? att)
            {
                att = line?.Reverse().TakeWhile(t => t.Kind is QsFragmentKind.DeclarationAttribute).LastOrDefault();
                return att != null || (line != null && !line.Any());
            }
            var preceding = file.GetTokenizedLine(declStart.Line).TakeWhile(ContextBuilder.TokensUpTo(Position.Create(0, declStart.Column)));
            for (var lineNr = declStart.Line; EmptyOrFirstAttribute(preceding, out var precedingAttribute);)
            {
                if (precedingAttribute != null)
                {
                    declStart = Position.Create(declStart.Line + lineNr, declStart.Column);
                }
                preceding = lineNr-- > 0 ? file.GetTokenizedLine(lineNr) : (IEnumerable<CodeFragment>?)null;
            }

            const string docPrefix = "/// ";
            var lineText = file.GetLine(declStart.Line).Text;
            var indent = declStart.Column <= lineText.Length
                ? lineText.Substring(0, declStart.Column)
                : throw new FileContentException("Fragment start position exceeds the bounds of the line.");
            var endLine = Environment.NewLine + indent;
            var docString = $"{docPrefix}# Summary{endLine}{docPrefix}{endLine}";

            var (argTuple, typeParams) =
                callableDecl.IsValue ? (callableDecl.Item.Item2.Item3.Argument,
                                        callableDecl.Item.Item2.Item3.TypeParameters)
                : typeDecl.IsValue ? (typeDecl.Item.Item2.Item2, ImmutableArray<QsSymbol>.Empty)
                : (null, ImmutableArray<QsSymbol>.Empty);
            var hasOutput = callableDecl.IsValue && !callableDecl.Item.Item2.Item3.ReturnType.Type.IsUnitType;

            var args = argTuple == null ? ImmutableArray<Tuple<QsSymbol, QsType>>.Empty : SyntaxGenerator.ExtractItems(argTuple);
            docString = string.Concat(
                docString,
                // Document Input Parameters
                args.Any() ? $"{docPrefix}# Input{endLine}" : string.Empty,
                string.Concat(args.Select(x => $"{docPrefix}## {x.Item1.Symbol.AsDeclarationName(null)}{endLine}{docPrefix}{endLine}")),
                // Document Output
                hasOutput ? $"{docPrefix}# Output{endLine}{docPrefix}{endLine}" : string.Empty,
                // Document Type Parameters
                typeParams.Any() ? $"{docPrefix}# Type Parameters{endLine}" : string.Empty,
                string.Concat(typeParams.Select(x => $"{docPrefix}## '{x.Symbol.AsDeclarationName(null)}{endLine}{docPrefix}{endLine}")));

            var whichDecl = $" for {declSymbol.AsDeclarationName(null)}";
            var suggestedEdit = file.GetWorkspaceEdit(new TextEdit
            {
                Range = new Lsp.Range { Start = declStart.ToLsp(), End = declStart.ToLsp() },
                NewText = docString
            });
            return new[] { ($"Add documentation{whichDecl}.", suggestedEdit) };
        }
    }
}
