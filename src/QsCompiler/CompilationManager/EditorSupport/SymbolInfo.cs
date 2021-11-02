// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.DataTypes;
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
    using QsExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using QsSymbolKind = QsSymbolKind<QsSymbol>;
    using QsTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

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
        /// If an overlapping code fragment exists, returns all symbol declarations, variable, Q# types, and Q# literals
        /// that *overlap* with <paramref name="position"/> as a <see cref="QsSymbolInfo"/>.
        /// </summary>
        /// <param name="fragment">
        /// The code fragment that overlaps with <paramref name="position"/> in <paramref name="file"/>,
        /// or null if no such fragment exists.
        /// </param>
        /// <remarks>
        /// Returns null if no such fragment exists, or <paramref name="file"/> and/or <paramref name="position"/>
        /// is null, or <paramref name="position"/> is invalid.
        /// </remarks>
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
            QsCompilerError.Verify(overlappingLiterals.Count() <= 1, "more than one literal overlaps with the same position");

            return new QsSymbolInfo(
                declaredSymbols: overlappingDecl.ToImmutableHashSet(),
                usedVariables: overlappingVariables.ToImmutableHashSet(),
                usedTypes: overlappingTypes.ToImmutableHashSet(),
                usedLiterals: overlappingLiterals.ToImmutableHashSet());
        }

        /// <summary>
        /// Returns the local declarations that are in scope at <paramref name="position"/>. Assumes that
        /// <paramref name="scope"/> contains <paramref name="position"/>.
        /// </summary>
        /// <param name="scope">The scope to look in.</param>
        /// <param name="position">The position in the scope to look at.</param>
        /// <param name="inclusive">
        /// True if locals declared by the statement or expression that contains <paramref name="position"/> should be
        /// included.
        /// </param>
        /// <returns>The local declarations.</returns>
        internal static LocalDeclarations LocalsInScope(QsScope scope, Position position, bool inclusive)
        {
            var (parent, statementsBefore, _) = SplitStatementsByPosition(scope, position);
            var declarationsBefore = parent.KnownSymbols.Variables
                .Concat(statementsBefore.SkipLast(1).SelectMany(s => s.SymbolDeclarations.Variables));
            var currentStatement = statementsBefore.LastOrDefault();

            if (!inclusive || currentStatement is null)
            {
                return new LocalDeclarations(declarationsBefore.ToImmutableArray());
            }

            var expressionDeclarations = currentStatement.Statement switch
            {
                QsStatementKind.QsExpressionStatement expression => ExpressionDeclarations(expression.Item),
                QsStatementKind.QsReturnStatement @return => ExpressionDeclarations(@return.Item),
                QsStatementKind.QsFailStatement fail => ExpressionDeclarations(fail.Item),
                QsStatementKind.QsVariableDeclaration declaration => ExpressionDeclarations(declaration.Item.Rhs),
                QsStatementKind.QsValueUpdate update => ExpressionDeclarations(update.Item.Rhs),
                QsStatementKind.QsConditionalStatement cond => CondExpressionDeclarations(cond),
                QsStatementKind.QsForStatement @for => ExpressionDeclarations(@for.Item.IterationValues),
                QsStatementKind.QsWhileStatement @while => ExpressionDeclarations(@while.Item.Condition),
                QsStatementKind.QsRepeatStatement repeat => ExpressionDeclarations(repeat.Item.SuccessCondition),
                _ => Enumerable.Empty<LocalVariableDeclaration<string>>(),
            };

            return new LocalDeclarations(
                declarationsBefore
                    .Concat(currentStatement.SymbolDeclarations.Variables)
                    .Concat(expressionDeclarations.Select(AddStatementOffset))
                    .ToImmutableArray());

            IEnumerable<LocalVariableDeclaration<string>> ExpressionDeclarations(TypedExpression expr) =>
                DeclarationsInExpression(expr, position - currentStatement.Location.Item.Offset);

            IEnumerable<LocalVariableDeclaration<string>> CondExpressionDeclarations(
                QsStatementKind.QsConditionalStatement cond) =>
                LastConditionBlockBefore(cond, position) is ({ } lastCond, var lastBlock)
                    ? DeclarationsInExpression(lastCond, position - lastBlock.Location.Item.Offset)
                    : Enumerable.Empty<LocalVariableDeclaration<string>>();

            LocalVariableDeclaration<string> AddStatementOffset(LocalVariableDeclaration<string> decl)
            {
                var specPosition = decl.Position.Map(p => currentStatement.Location.Item.Offset + p);
                return new LocalVariableDeclaration<string>(
                    decl.VariableName, decl.Type, decl.InferredInformation, specPosition, decl.Range);
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
            var declarations = implementation is null ? null : LocalsInScope(implementation, position - specPos, true);
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
                var statements = SplitStatementsByPosition(implementation, defStart - specPos).Item3;
                var scope = new QsScope(statements.ToImmutableArray(), locals);
                referenceLocations = IdentifierReferences.Find(definition.Item.Item1, scope, file.FileName, specPos).Select(AsLocation);
            }

            declarationLocation = AsLocation(file.FileName, definition.Item.Item2, defRange);
            return true;
        }

        /// <summary>
        /// Finds the innermost scope that contains <paramref name="position"/> and returns the scope, statements in the
        /// innermost scope before and including <paramref name="position"/>, and statements after
        /// <paramref name="position"/>. If <paramref name="position"/> points to a statement that declares variables
        /// and contains a child scope, the statements after <paramref name="position"/> are from the child scope.
        /// </summary>
        /// <param name="scope">A scope that contains <paramref name="position"/>.</param>
        /// <param name="position">The position to split by.</param>
        /// <returns>The innermost scope and statements before and after <paramref name="position"/>.</returns>
        /// <remarks>
        /// Statements that do not have a location are treated as if they occur after <paramref name="position"/>.
        /// </remarks>
        private static (QsScope, ImmutableList<QsStatement>, ImmutableList<QsStatement>) SplitStatementsByPosition(
            QsScope scope, Position position)
        {
            var statementsBefore = scope.Statements.TakeWhile(s => IsBefore(s.Location)).ToImmutableList();
            var statementsAfter = scope.Statements.SkipWhile(s => IsBefore(s.Location)).ToImmutableList();
            var nextScope = statementsBefore.LastOrDefault()?.Statement switch
            {
                QsStatementKind.QsConditionalStatement cond => LastConditionBlockBefore(cond, position)?.Item2.Body,
                QsStatementKind.QsForStatement @for => @for.Item.Body,
                QsStatementKind.QsWhileStatement @while => @while.Item.Body,
                QsStatementKind.QsRepeatStatement repeat => NextRepeatScope(repeat),
                QsStatementKind.QsConjugation conjugation => IsBefore(conjugation.Item.InnerTransformation.Location)
                    ? conjugation.Item.InnerTransformation.Body
                    : conjugation.Item.OuterTransformation.Body,
                QsStatementKind.QsQubitScope qubit => qubit.Item.Body,
                _ => null,
            };

            return nextScope is null ? (scope, statementsBefore, statementsAfter)
                : ScopeStartsBefore(nextScope) ? SplitStatementsByPosition(nextScope, position)
                : (scope, statementsBefore, nextScope.Statements.ToImmutableList());

            bool IsBefore(QsNullable<QsLocation> location) => location.IsValue && location.Item.Offset < position;
            bool ScopeStartsBefore(QsScope sc) => sc.Statements.FirstOrDefault() is { } st && IsBefore(st.Location);

            static QsScope NextRepeatScope(QsStatementKind.QsRepeatStatement repeat)
            {
                var statements = repeat.Item.RepeatBlock.Body.Statements
                    .Concat(repeat.Item.FixupBlock.Body.Statements)
                    .ToImmutableArray();

                return new QsScope(statements, repeat.Item.RepeatBlock.Body.KnownSymbols);
            }
        }

        private static (TypedExpression?, QsPositionedBlock)? LastConditionBlockBefore(
            QsStatementKind.QsConditionalStatement conditional, Position position)
        {
            var condBlocks = conditional.Item.ConditionalBlocks.Select(b => b.ToValueTuple());
            var blocks = conditional.Item.Default.IsValue
                ? condBlocks.Append((null, conditional.Item.Default.Item))
                : condBlocks;

            return blocks
                .TakeWhile(b => b.Item2.Location.IsValue && b.Item2.Location.Item.Offset < position)
                .LastOrDefault();
        }

        private static IEnumerable<LocalVariableDeclaration<string>> DeclarationsInExpression(
            TypedExpression expression, Position position)
        {
            if (expression.Range.IsNull || !expression.Range.Item.Contains(position))
            {
                return Enumerable.Empty<LocalVariableDeclaration<string>>();
            }

            return expression.Expression switch
            {
                QsExpressionKind.ValueTuple tuple => Many(tuple.Item),
                QsExpressionKind.StringLiteral str => Many(str.Item2),
                QsExpressionKind.RangeLiteral range => Binary(range.Item1, range.Item2),
                QsExpressionKind.NewArray array => DeclarationsInExpression(array.Item2, position),
                QsExpressionKind.ValueArray array => Many(array.Item),
                QsExpressionKind.ArrayItem arrayItem => Binary(arrayItem.Item1, arrayItem.Item2),
                QsExpressionKind.NamedItem namedItem => DeclarationsInExpression(namedItem.Item1, position),
                QsExpressionKind.NEG neg => DeclarationsInExpression(neg.Item, position),
                QsExpressionKind.NOT not => DeclarationsInExpression(not.Item, position),
                QsExpressionKind.BNOT bNot => DeclarationsInExpression(bNot.Item, position),
                QsExpressionKind.ADD add => Binary(add.Item1, add.Item2),
                QsExpressionKind.SUB sub => Binary(sub.Item1, sub.Item2),
                QsExpressionKind.MUL mul => Binary(mul.Item1, mul.Item2),
                QsExpressionKind.DIV div => Binary(div.Item1, div.Item2),
                QsExpressionKind.MOD mod => Binary(mod.Item1, mod.Item2),
                QsExpressionKind.POW pow => Binary(pow.Item1, pow.Item2),
                QsExpressionKind.EQ eq => Binary(eq.Item1, eq.Item2),
                QsExpressionKind.NEQ neq => Binary(neq.Item1, neq.Item2),
                QsExpressionKind.LT lt => Binary(lt.Item1, lt.Item2),
                QsExpressionKind.LTE lte => Binary(lte.Item1, lte.Item2),
                QsExpressionKind.GT gt => Binary(gt.Item1, gt.Item2),
                QsExpressionKind.GTE gte => Binary(gte.Item1, gte.Item2),
                QsExpressionKind.AND and => Binary(and.Item1, and.Item2),
                QsExpressionKind.OR or => Binary(or.Item1, or.Item2),
                QsExpressionKind.BOR bOr => Binary(bOr.Item1, bOr.Item2),
                QsExpressionKind.BAND bAnd => Binary(bAnd.Item1, bAnd.Item2),
                QsExpressionKind.BXOR bXor => Binary(bXor.Item1, bXor.Item2),
                QsExpressionKind.LSHIFT lShift => Binary(lShift.Item1, lShift.Item2),
                QsExpressionKind.RSHIFT rShift => Binary(rShift.Item1, rShift.Item2),
                QsExpressionKind.CONDITIONAL cond => Many(new[] { cond.Item1, cond.Item2, cond.Item3 }),
                QsExpressionKind.CopyAndUpdate update => Binary(update.Item1, update.Item3),
                QsExpressionKind.UnwrapApplication unwrap => DeclarationsInExpression(unwrap.Item, position),
                QsExpressionKind.AdjointApplication adj => DeclarationsInExpression(adj.Item, position),
                QsExpressionKind.ControlledApplication ctl => DeclarationsInExpression(ctl.Item, position),
                QsExpressionKind.CallLikeExpression call => Binary(call.Item1, call.Item2),
                QsExpressionKind.SizedArray sizedArray => Binary(sizedArray.value, sizedArray.size),
                QsExpressionKind.Lambda lambda => Lambda(lambda.Item),
                _ => Enumerable.Empty<LocalVariableDeclaration<string>>(),
            };

            IEnumerable<LocalVariableDeclaration<string>> Binary(TypedExpression e1, TypedExpression e2) =>
                DeclarationsInExpression(e1, position).Concat(DeclarationsInExpression(e2, position));

            IEnumerable<LocalVariableDeclaration<string>> Many(IEnumerable<TypedExpression> es) =>
                es.SelectMany(e => DeclarationsInExpression(e, position));

            IEnumerable<LocalVariableDeclaration<string>> Lambda(Lambda<TypedExpression> lambda)
            {
                // Since lambda parameters are bound later to a value from any source, pessimistically assume it has a
                // local quantum dependency.
                var inferred = new InferredExpressionInformation(false, true);

                return DeclarationsInTypedSymbol(lambda.Param, CallableInputType(expression.ResolvedType), inferred)
                    .Concat(DeclarationsInExpression(lambda.Body, position));
            }

            static ResolvedType CallableInputType(ResolvedType type) => type.Resolution switch
            {
                QsTypeKind.Function function => function.Item1,
                QsTypeKind.Operation operation => operation.Item1.Item1,
                _ => throw new Exception("Type is not a callable type."),
            };
        }

        private static IEnumerable<LocalVariableDeclaration<string>> DeclarationsInTypedSymbol(
            QsSymbol symbol, ResolvedType type, InferredExpressionInformation inferred)
        {
            switch (symbol.Symbol, type.Resolution)
            {
                case (QsSymbolKind.Symbol name, _):
                    var range = symbol.Range.IsValue
                        ? symbol.Range.Item
                        : throw new ArgumentException("Range is null.", nameof(symbol));

                    var position = QsNullable<Position>.NewValue(range.Start);
                    return new[]
                    {
                        new LocalVariableDeclaration<string>(name.Item, type, inferred, position, range - range.Start),
                    };
                case (QsSymbolKind.SymbolTuple symbols, QsTypeKind.TupleType types):
                    return
                        from typedSymbol in symbols.Item.Zip(types.Item, ValueTuple.Create)
                        from declaration in DeclarationsInTypedSymbol(typedSymbol.Item1, typedSymbol.Item2, inferred)
                        select declaration;
                case (QsSymbolKind.SymbolTuple symbols, _):
                    return symbols.Item.SingleOrDefault() is { } single
                        ? DeclarationsInTypedSymbol(single, type, inferred)
                        : Enumerable.Empty<LocalVariableDeclaration<string>>();
                default:
                    return Enumerable.Empty<LocalVariableDeclaration<string>>();
            }
        }
    }
}
