// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Position = Microsoft.Quantum.QsCompiler.DataTypes.Position;
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder.EditorSupport
{
    using QsExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
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
            var declarationsBefore = parent.KnownSymbols.Variables
                .Concat(statementsBefore.SkipLast(1).SelectMany(s => s.SymbolDeclarations.Variables));

            if (!inclusive || currentStatement is null)
            {
                return new LocalDeclarations(declarationsBefore.ToImmutableArray());
            }

            return new LocalDeclarations(declarationsBefore
                .Concat(currentStatement.SymbolDeclarations.Variables)
                .Concat(ExpressionsInStatement(currentStatement).SelectMany(DeclarationsWithOffset))
                .ToImmutableArray());

            IEnumerable<LocalVariableDeclaration<string>> DeclarationsWithOffset(TypedExpression e)
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

            bool IsBefore(QsNullable<QsLocation> location) => location.IsValue && location.Item.Offset < position;
            bool ScopeStartsBefore(QsScope sc) => sc.Statements.FirstOrDefault() is { } st && IsBefore(st.Location);

            QsPositionedBlock? LastCondBlockBefore(QsStatementKind.QsConditionalStatement cond)
            {
                var condBlocks = cond.Item.ConditionalBlocks.Select(b => b.Item2);
                var blocks = cond.Item.Default.IsValue ? condBlocks.Append(cond.Item.Default.Item) : condBlocks;
                return blocks.TakeWhile(b => b.Location.IsValue && b.Location.Item.Offset < position).LastOrDefault();
            }

            static QsScope NextRepeatScope(QsStatementKind.QsRepeatStatement repeat)
            {
                var statements = repeat.Item.RepeatBlock.Body.Statements.Concat(repeat.Item.FixupBlock.Body.Statements);
                return new QsScope(statements.ToImmutableArray(), repeat.Item.RepeatBlock.Body.KnownSymbols);
            }
        }

        internal static TypedExpression? FindExpressionInStatementByPosition(QsStatement statement, Position position) =>
            ExpressionsInStatement(statement)
                .Select(e => FindExpressionByPosition(e, position))
                .FirstOrDefault(e => !(e is null));

        private static TypedExpression? FindExpressionByPosition(TypedExpression expression, Position position)
        {
            if (expression.Range.IsNull || !expression.Range.Item.Contains(position))
            {
                return null;
            }

            var child = expression.Expression switch
            {
                QsExpressionKind.ValueTuple tuple => Many(tuple.Item),
                QsExpressionKind.StringLiteral str => Many(str.Item2),
                QsExpressionKind.RangeLiteral range => Binary(range.Item1, range.Item2),
                QsExpressionKind.NewArray array => FindExpressionByPosition(array.Item2, position),
                QsExpressionKind.ValueArray array => Many(array.Item),
                QsExpressionKind.ArrayItem arrayItem => Binary(arrayItem.Item1, arrayItem.Item2),
                QsExpressionKind.NamedItem namedItem => FindExpressionByPosition(namedItem.Item1, position),
                QsExpressionKind.NEG neg => FindExpressionByPosition(neg.Item, position),
                QsExpressionKind.NOT not => FindExpressionByPosition(not.Item, position),
                QsExpressionKind.BNOT bNot => FindExpressionByPosition(bNot.Item, position),
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
                QsExpressionKind.UnwrapApplication unwrap => FindExpressionByPosition(unwrap.Item, position),
                QsExpressionKind.AdjointApplication adj => FindExpressionByPosition(adj.Item, position),
                QsExpressionKind.ControlledApplication ctl => FindExpressionByPosition(ctl.Item, position),
                QsExpressionKind.CallLikeExpression call => Binary(call.Item1, call.Item2),
                QsExpressionKind.SizedArray sizedArray => Binary(sizedArray.value, sizedArray.size),
                QsExpressionKind.Lambda lambda => FindExpressionByPosition(lambda.Item.Body, position),
                _ => null,
            };

            return child ?? expression;

            TypedExpression? Binary(TypedExpression e1, TypedExpression e2) =>
                FindExpressionByPosition(e1, position) ?? FindExpressionByPosition(e2, position);

            TypedExpression? Many(IEnumerable<TypedExpression> es) =>
                es.Select(e => FindExpressionByPosition(e, position)).FirstOrDefault(e => !(e is null));
        }

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
        }

        private static IEnumerable<LocalVariableDeclaration<string>> DeclarationsInExpressionByPosition(
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
                QsExpressionKind.NewArray array => DeclarationsInExpressionByPosition(array.Item2, position),
                QsExpressionKind.ValueArray array => Many(array.Item),
                QsExpressionKind.ArrayItem arrayItem => Binary(arrayItem.Item1, arrayItem.Item2),
                QsExpressionKind.NamedItem namedItem => DeclarationsInExpressionByPosition(namedItem.Item1, position),
                QsExpressionKind.NEG neg => DeclarationsInExpressionByPosition(neg.Item, position),
                QsExpressionKind.NOT not => DeclarationsInExpressionByPosition(not.Item, position),
                QsExpressionKind.BNOT bNot => DeclarationsInExpressionByPosition(bNot.Item, position),
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
                QsExpressionKind.UnwrapApplication unwrap => DeclarationsInExpressionByPosition(unwrap.Item, position),
                QsExpressionKind.AdjointApplication adj => DeclarationsInExpressionByPosition(adj.Item, position),
                QsExpressionKind.ControlledApplication ctl => DeclarationsInExpressionByPosition(ctl.Item, position),
                QsExpressionKind.CallLikeExpression call => Binary(call.Item1, call.Item2),
                QsExpressionKind.SizedArray sizedArray => Binary(sizedArray.value, sizedArray.size),
                QsExpressionKind.Lambda lambda => Lambda(lambda.Item),
                _ => Enumerable.Empty<LocalVariableDeclaration<string>>(),
            };

            IEnumerable<LocalVariableDeclaration<string>> Binary(TypedExpression e1, TypedExpression e2) =>
                DeclarationsInExpressionByPosition(e1, position)
                    .Concat(DeclarationsInExpressionByPosition(e2, position));

            IEnumerable<LocalVariableDeclaration<string>> Many(IEnumerable<TypedExpression> es) =>
                es.SelectMany(e => DeclarationsInExpressionByPosition(e, position));

            IEnumerable<LocalVariableDeclaration<string>> Lambda(Lambda<TypedExpression> lambda)
            {
                // Since lambda parameters are bound later to a value from any source, pessimistically assume it has a
                // local quantum dependency.
                var inferred = new InferredExpressionInformation(false, true);

                return DeclarationsInTypedSymbol(lambda.Param, CallableInputType(expression.ResolvedType), inferred)
                    .Concat(DeclarationsInExpressionByPosition(lambda.Body, position));
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
