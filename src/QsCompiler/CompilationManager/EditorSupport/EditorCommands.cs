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
using Microsoft.Quantum.QsCompiler.TextProcessing;
using Microsoft.VisualStudio.LanguageServer.Protocol;


namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    internal static class EditorCommands
    {
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

            var locals = compilation.TryGetLocalDeclarations(file, position, out var cName, includeDeclaredAtPosition: true);
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
                ? SymbolInfo.AsLocation(found.Item.Item1, found.Item.Item2, found.Item.Item3)
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
        /// Returns a look-up of workspace edits suggested by the compiler for the given location and context.
        /// The key of the look-up is a suitable title for the corresponding edits that can be presented to the user. 
        /// Returns null if any of the given arguments is null or if suitable edits cannot be determined.
        /// </summary>
        public static ILookup<string, WorkspaceEdit> CodeActions(this FileContentManager file, CompilationUnit compilation, Range range, CodeActionContext context)
        {
            if (range?.Start == null || range.End == null || file == null || !Utils.IsValidRange(range, file)) return null;
            var suggestionsForUnknownIds = file.SuggestionsForUnknownIdentifiers(compilation, range.Start.Line, context?.Diagnostics);
            var suggestionsForAmbiguousIds = file.SuggestionsForAmbiguousIdentifiers(compilation, context?.Diagnostics);
            var suggestionsForDeprecatedSyntax = file.SuggestionsForDeprecatedSyntax(context?.Diagnostics);
            var suggestionsForUpdateAndReassign = file.SuggestionsForUpdateAndReassignStatements(context?.Diagnostics);
            var suggestionsForIndexRange = file.SuggestionsForIndexRange(compilation, range);
            var suggestionsForUnreachableCode = file.SuggestionsForUnreachableCode(context?.Diagnostics);
            var suggestionsForDocComments = file.DocCommentSuggestions(range);
            return suggestionsForUnknownIds
                .Concat(suggestionsForAmbiguousIds)
                .Concat(suggestionsForDeprecatedSyntax)
                .Concat(suggestionsForUpdateAndReassign)
                .Concat(suggestionsForIndexRange)
                .Concat(suggestionsForUnreachableCode)
                .Concat(suggestionsForDocComments)
                .ToLookup(s => s.Item1, s => s.Item2);
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

            var locals = compilation.TryGetLocalDeclarations(file, position, out var cName, includeDeclaredAtPosition: true);
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

            var fragment = file?.TryGetFragmentAt(position, out var _, includeEnd: true);
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
                
            var doc = documentation.PrintSummary(format == MarkupKind.Markdown).Trim();
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
