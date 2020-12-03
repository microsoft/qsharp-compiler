// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

namespace Microsoft.Quantum.QsCompiler.Transformations.LoopLifting
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    /// <summary>
    /// Performs the transformation steps needed to lift any repeat-until loops
    /// into generated recursive operations.
    /// </summary>
    public static class LiftLoops
    {
        /// <summary>
        /// Applies the repeat loop lifting transformation to the given compilation.
        /// </summary>
        /// <returns>
        /// The transformed compilation.
        /// </returns>
        public static QsCompilation Apply(QsCompilation compilation)
        {
            return new LiftRepeatBodies().OnCompilation(compilation);
        }

        private class LiftRepeatBodies : ContentLifting.LiftContent<ContentLifting.LiftContent.TransformationState>
        {
            public LiftRepeatBodies()
                : base(new ContentLifting.LiftContent.TransformationState())
            {
                this.StatementKinds = new StatementKindTransformation(this);
            }

            private new class StatementKindTransformation
                : ContentLifting.LiftContent<ContentLifting.LiftContent.TransformationState>.StatementKindTransformation
            {
                public StatementKindTransformation(SyntaxTreeTransformation<ContentLifting.LiftContent.TransformationState> parent)
                    : base(parent)
                {
                }

                public override QsStatementKind OnRepeatStatement(QsRepeatStatement statement)
                {
                    var (_, repeatBlock) = this.OnPositionedBlock(QsNullable<TypedExpression>.Null, statement.RepeatBlock);
                    var (_, fixupBlock) = this.OnPositionedBlock(QsNullable<TypedExpression>.Null, statement.FixupBlock);

                    if (this.IsConditionedOnResult(statement.SuccessCondition))
                    {
                        var contextValidScope = this.SharedState.IsValidScope;
                        var contextParams = this.SharedState.GeneratedOpParams;
                        this.SharedState.IsValidScope = true;
                        var variables = repeatBlock.Body.KnownSymbols.Variables.Union(fixupBlock.Body.KnownSymbols.Variables).ToImmutableArray();
                        this.SharedState.GeneratedOpParams = variables;

                        var newScope = new QsScope(BuildStatements(repeatBlock, fixupBlock, statement.SuccessCondition), new LocalDeclarations(variables));
                        var newBlock = new QsPositionedBlock(newScope, repeatBlock.Location, repeatBlock.Comments);

                        var canLift = this.SharedState.LiftBody(newBlock.Body, out var callable, out var call);
                        this.SharedState.IsValidScope = contextValidScope;
                        this.SharedState.GeneratedOpParams = contextParams;
                        if (canLift && callable != null && call != null)
                        {
                            this.SharedState.GeneratedOperations?.Add(callable);
                            return call.Statement;
                        }
                    }

                    // This is not a repeat based on a result, so we can assume it is classical and return
                    // it without any transformation.
                    return QsStatementKind.NewQsRepeatStatement(new QsRepeatStatement(
                        repeatBlock,
                        statement.SuccessCondition,
                        fixupBlock));
                }

                private static ImmutableArray<QsStatement> BuildStatements(QsPositionedBlock repeatBlock, QsPositionedBlock fixupBlock, TypedExpression successCondition)
                {
                    var statements = new List<QsStatement>();
                    statements.AddRange(repeatBlock.Body.Statements);

                    var emptyScope = new QsScope(
                        ImmutableArray<QsStatement>.Empty,
                        LocalDeclarations.Empty);
                    var conditionalBlock = Tuple.Create(
                        successCondition,
                        new QsPositionedBlock(
                            emptyScope,
                            QsNullable<QsLocation>.Null,
                            QsComments.Empty));
                    var conditionalStatement = new QsConditionalStatement(
                        new List<Tuple<TypedExpression, QsPositionedBlock>> { conditionalBlock }.ToImmutableArray(),
                        QsNullable<QsPositionedBlock>.NewValue(fixupBlock));
                    statements.Add(new QsStatement(
                        QsStatementKind.NewQsConditionalStatement(conditionalStatement),
                        LocalDeclarations.Empty,
                        QsNullable<QsLocation>.Null,
                        QsComments.Empty));

                    return statements.ToImmutableArray();
                }

                private bool IsConditionedOnResult(
                    TypedExpression expression)
                {
                    if (expression.Expression is ExpressionKind.EQ eq)
                    {
                        return eq.Item1.ResolvedType.Resolution == ResolvedTypeKind.Result ||
                            eq.Item2.ResolvedType.Resolution == ResolvedTypeKind.Result;
                    }
                    else if (expression.Expression is ExpressionKind.NEQ neq)
                    {
                        return neq.Item1.ResolvedType.Resolution == ResolvedTypeKind.Result ||
                            neq.Item2.ResolvedType.Resolution == ResolvedTypeKind.Result;
                    }
                    else if (expression.Expression is ExpressionKind.AND andEx)
                    {
                        return this.IsConditionedOnResult(andEx.Item1) || this.IsConditionedOnResult(andEx.Item2);
                    }
                    else if (expression.Expression is ExpressionKind.OR orEx)
                    {
                        return this.IsConditionedOnResult(orEx.Item1) || this.IsConditionedOnResult(orEx.Item2);
                    }
                    else if (expression.Expression is ExpressionKind.NOT notEx)
                    {
                        return this.IsConditionedOnResult(notEx.Item);
                    }

                    return false;
                }
            }
        }
    }
}