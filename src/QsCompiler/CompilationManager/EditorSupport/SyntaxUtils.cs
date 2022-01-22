// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Position = Microsoft.Quantum.QsCompiler.DataTypes.Position;
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder.EditorSupport
{
    using QsExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using QsInitializerKind = QsInitializerKind<ResolvedInitializer, TypedExpression>;
    using QsSymbolKind = QsSymbolKind<QsSymbol>;
    using QsTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    internal static class SyntaxUtils
    {
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
            var currentStatement = statementsBefore.LastOrDefault();
            var varsBefore = parent.KnownSymbols.Variables
                .Concat(statementsBefore.SkipLast(1).SelectMany(s => s.SymbolDeclarations.Variables))
                .ToImmutableArray();

            if (!inclusive || currentStatement is null)
            {
                return new LocalDeclarations(varsBefore);
            }

            // This is null if the position does not occur in an expression of the current statement.
            var currentExpression = ExpressionsInStatement(currentStatement).FirstOrDefault(IsSelectedExpression);
            var expressionVars = currentExpression?.Apply(VarsWithOffset).Concat(varsBefore);

            var statementVars = currentStatement.Statement switch
            {
                QsStatementKind.QsForStatement f => f.Item.Body.KnownSymbols.Variables,
                QsStatementKind.QsQubitScope q => q.Item.Body.KnownSymbols.Variables,
                _ => currentStatement.SymbolDeclarations.Variables.Concat(varsBefore),
            };

            return new LocalDeclarations((expressionVars ?? statementVars).ToImmutableArray());

            bool IsSelectedExpression(TypedExpression e) =>
                e.Range.Any(r => r.Contains(position - currentStatement.Location.Item.Offset));

            IEnumerable<LocalVariableDeclaration<string>> VarsWithOffset(TypedExpression e)
            {
                if (currentStatement.Location.IsNull)
                {
                    return Enumerable.Empty<LocalVariableDeclaration<string>>();
                }

                e = new ExpressionOffsetTransformation(currentStatement.Location.Item.Offset).OnTypedExpression(e);
                return DeclarationsInExpressionByPosition(e, position);
            }
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
        internal static (QsScope, ImmutableList<QsStatement>, ImmutableList<QsStatement>) SplitStatementsByPosition(
            QsScope scope, Position position)
        {
            var statementsBefore = scope.Statements.TakeWhile(s => IsBefore(s.Location)).ToImmutableList();
            var statementsAfter = scope.Statements.SkipWhile(s => IsBefore(s.Location)).ToImmutableList();
            var nextScope = statementsBefore.LastOrDefault()?.Statement switch
            {
                QsStatementKind.QsConditionalStatement cond => LastCondBlockBefore(cond)?.Body,
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

            bool IsBefore(QsNullable<QsLocation> location) => location.Any(l => l.Offset < position);
            bool ScopeStartsBefore(QsScope s) => s.Statements.FirstOrDefault()?.Location is { } l && IsBefore(l);

            QsPositionedBlock? LastCondBlockBefore(QsStatementKind.QsConditionalStatement cond)
            {
                var condBlocks = cond.Item.ConditionalBlocks.Select(b => b.Item2);
                var blocks = cond.Item.Default.IsValue ? condBlocks.Append(cond.Item.Default.Item) : condBlocks;
                return blocks.TakeWhile(b => b.Location.Any(l => l.Offset < position)).LastOrDefault();
            }

            static QsScope NextRepeatScope(QsStatementKind.QsRepeatStatement repeat)
            {
                var statements = repeat.Item.RepeatBlock.Body.Statements
                    .Concat(repeat.Item.FixupBlock.Body.Statements)
                    .ToImmutableArray();

                return new QsScope(statements, repeat.Item.RepeatBlock.Body.KnownSymbols);
            }
        }

        internal static TypedExpression? FindExpressionInStatementByPosition(QsStatement statement, Position position) =>
            ExpressionsInStatement(statement)
                .Select(e => FindExpressionByPosition(e, position))
                .FirstOrDefault(e => !(e is null));

        private static TypedExpression? FindExpressionByPosition(TypedExpression expression, Position position) =>
            expression.Fold<TypedExpression?>((e, children) =>
                children.FirstOrDefault(child => !(child is null))
                ?? (e.Range.IsValue && e.Range.Item.Contains(position) ? e : null));

        private static IEnumerable<TypedExpression> ExpressionsInStatement(QsStatement statement)
        {
            return statement.Statement switch
            {
                QsStatementKind.QsExpressionStatement expression => new[] { expression.Item },
                QsStatementKind.QsReturnStatement @return => new[] { @return.Item },
                QsStatementKind.QsFailStatement fail => new[] { fail.Item },
                QsStatementKind.QsVariableDeclaration declaration => new[] { declaration.Item.Rhs },
                QsStatementKind.QsValueUpdate update => new[] { update.Item.Rhs },
                QsStatementKind.QsConditionalStatement cond => cond.Item.ConditionalBlocks.Select(CondBlockExpression),
                QsStatementKind.QsForStatement @for => new[] { @for.Item.IterationValues },
                QsStatementKind.QsWhileStatement @while => new[] { @while.Item.Condition },
                QsStatementKind.QsRepeatStatement repeat => new[] { repeat.Item.SuccessCondition },
                QsStatementKind.QsQubitScope qubit => InitializerExpressions(qubit.Item.Binding.Rhs),
                _ => Enumerable.Empty<TypedExpression>(),
            };

            TypedExpression CondBlockExpression(Tuple<TypedExpression, QsPositionedBlock> b)
            {
                if (statement.Location.IsNull || b.Item2.Location.IsNull)
                {
                    return b.Item1;
                }

                var offset = b.Item2.Location.Item.Offset - statement.Location.Item.Offset;
                return new ExpressionOffsetTransformation(offset).OnTypedExpression(b.Item1);
            }

            static IEnumerable<TypedExpression> InitializerExpressions(ResolvedInitializer i) => i.Resolution switch
            {
                QsInitializerKind.QubitRegisterAllocation r => new[] { r.Item },
                QsInitializerKind.QubitTupleAllocation t => t.Item.SelectMany(InitializerExpressions),
                _ => Enumerable.Empty<TypedExpression>(),
            };
        }

        private static IEnumerable<LocalVariableDeclaration<string>> DeclarationsInExpressionByPosition(
            TypedExpression expression, Position position)
        {
            return expression.ExtractAll(Declarations);

            IEnumerable<LocalVariableDeclaration<string>> Declarations(TypedExpression e)
            {
                if (!e.Range.Any(r => r.Contains(position)))
                {
                    return Enumerable.Empty<LocalVariableDeclaration<string>>();
                }

                if (e.Expression is QsExpressionKind.Lambda lambda)
                {
                    // Since lambda parameters are bound later to a value from any source, pessimistically assume it has
                    // a local quantum dependency.
                    var inferred = new InferredExpressionInformation(false, true);

                    return DeclarationsInTypedSymbol(lambda.Item.Param, CallableInputType(e.ResolvedType), inferred)
                        .Concat(DeclarationsInExpressionByPosition(lambda.Item.Body, position));
                }

                return Enumerable.Empty<LocalVariableDeclaration<string>>();
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

        private class ExpressionOffsetTransformation : ExpressionTransformation
        {
            private readonly Position offset;

            internal ExpressionOffsetTransformation(Position offset) => this.offset = offset;

            public override QsNullable<Range> OnRangeInformation(QsNullable<Range> range) =>
                range.Map(r => this.offset + r);
        }
    }
}
