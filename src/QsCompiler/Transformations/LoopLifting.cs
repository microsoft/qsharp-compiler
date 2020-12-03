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
        public static QsCompilation Apply(QsCompilation compilation)
        {
            return new LiftRepeatBodies().OnCompilation(compilation);
        }

        private class LiftRepeatBodies : ContentLifting.LiftContent<ContentLifting.LiftContent.TransformationState>
        {
            public LiftRepeatBodies() : base(new ContentLifting.LiftContent.TransformationState())
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
                    var contextValidScope = this.SharedState.IsValidScope;
                    var contextParams = this.SharedState.GeneratedOpParams;
                    this.SharedState.IsValidScope = IsConditionedOnResult(statement.SuccessCondition);
                    this.SharedState.GeneratedOpParams = statement.RepeatBlock.Body.KnownSymbols.Variables;

                    var (_, block) = this.OnPositionedBlock(QsNullable<TypedExpression>.Null, statement.RepeatBlock);

                    if (this.SharedState.LiftBody(block.Body, out var callable, out var call))
                    {
                        this.SharedState.GeneratedOperations?.Add(callable);
                        block = new QsPositionedBlock(
                            new QsScope(ImmutableArray.Create(call), block.Body.KnownSymbols),
                            block.Location,
                            block.Comments);
                    }

                    this.SharedState.IsValidScope = contextValidScope;
                    this.SharedState.GeneratedOpParams = contextParams;

                    return QsStatementKind.NewQsRepeatStatement(new QsRepeatStatement(
                        block,
                        statement.SuccessCondition,
                        this.OnPositionedBlock(QsNullable<TypedExpression>.Null, statement.FixupBlock).Item2));
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
                        return IsConditionedOnResult(andEx.Item1) || IsConditionedOnResult(andEx.Item2);
                    }
                    else if (expression.Expression is ExpressionKind.OR orEx)
                    {
                        return IsConditionedOnResult(orEx.Item1) || IsConditionedOnResult(orEx.Item2);
                    }
                    else if (expression.Expression is ExpressionKind.NOT notEx)
                    {
                        return IsConditionedOnResult(notEx.Item);
                    }

                    return false;
                }
            }
        }
    }
}