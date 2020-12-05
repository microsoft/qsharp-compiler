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
                    var contextValidScope = this.SharedState.IsValidScope;
                    var contextParams = this.SharedState.GeneratedOpParams;
                    this.SharedState.IsValidScope = true;

                    this.SharedState.GeneratedOpParams = statement.RepeatBlock.Body.KnownSymbols.Variables;
                    var (_, repeatBlock) = this.OnPositionedBlock(QsNullable<TypedExpression>.Null, statement.RepeatBlock);
                    this.SharedState.GeneratedOpParams = statement.FixupBlock.Body.KnownSymbols.Variables;
                    var (_, fixupBlock) = this.OnPositionedBlock(QsNullable<TypedExpression>.Null, statement.FixupBlock);

                    var newStatement = QsStatementKind.NewQsRepeatStatement(new QsRepeatStatement(
                        repeatBlock,
                        statement.SuccessCondition,
                        fixupBlock));

                    if (this.SharedState.IsValidScope && this.IsConditionedOnResult(statement.SuccessCondition))
                    {
                        var variables = new LocalDeclarations(repeatBlock.Body.KnownSymbols.Variables.Union(fixupBlock.Body.KnownSymbols.Variables).ToImmutableArray());
                        this.SharedState.GeneratedOpParams = variables.Variables;

                        var newScope = new QsScope(BuildStatements(repeatBlock, fixupBlock, statement.SuccessCondition), variables);
                        var newBlock = new QsPositionedBlock(newScope, repeatBlock.Location, repeatBlock.Comments);

                        if (this.SharedState.LiftBody(newBlock.Body, out var callable, out var call))
                        {
                            this.SharedState.GeneratedOperations?.Add(MakeRecursive(callable, call, variables));
                            newStatement = call.Statement;
                        }
                    }

                    this.SharedState.IsValidScope = this.SharedState.IsValidScope && contextValidScope;
                    this.SharedState.GeneratedOpParams = contextParams;
                    return newStatement;
                }

                private static ImmutableArray<QsStatement> BuildStatements(QsPositionedBlock repeatBlock, QsPositionedBlock fixupBlock, TypedExpression successCondition)
                {
                    var statements = new List<QsStatement>();
                    statements.AddRange(repeatBlock.Body.Statements);

                    var conditionalBlock = Tuple.Create(
                        new TypedExpression(
                            ExpressionKind.NewNOT(successCondition),
                            successCondition.TypeArguments,
                            successCondition.ResolvedType,
                            successCondition.InferredInformation,
                            successCondition.Range),
                        fixupBlock);
                    var conditionalStatement = new QsConditionalStatement(
                        ImmutableArray.Create(conditionalBlock),
                        QsNullable<QsPositionedBlock>.Null);
                    statements.Add(new QsStatement(
                        QsStatementKind.NewQsConditionalStatement(conditionalStatement),
                        LocalDeclarations.Empty,
                        QsNullable<QsLocation>.Null,
                        QsComments.Empty));

                    return statements.ToImmutableArray();
                }

                private static QsCallable MakeRecursive(QsCallable callable, QsStatement call, LocalDeclarations knownSymbols)
                {
                    var specialization = callable.Specializations.Single();
                    var providedImplementation = (SpecializationImplementation.Provided)specialization.Implementation;
                    var statements = new List<QsStatement>(providedImplementation.Item2.Statements);
                    var conditionalStatement = (QsStatementKind.QsConditionalStatement)statements.Last().Statement;
                    var conditionalBlock = conditionalStatement.Item.ConditionalBlocks.Single();
                    var conditionalScopeStatements = new List<QsStatement>(conditionalBlock.Item2.Body.Statements);

                    conditionalScopeStatements.Add(call);

                    var newConditionalBlock = Tuple.Create(
                        conditionalBlock.Item1,
                        new QsPositionedBlock(
                            new QsScope(
                                conditionalScopeStatements.ToImmutableArray(),
                                knownSymbols),
                            conditionalBlock.Item2.Location,
                            conditionalBlock.Item2.Comments));

                    var newConditionalStatement = new QsConditionalStatement(
                        ImmutableArray.Create(newConditionalBlock),
                        QsNullable<QsPositionedBlock>.Null);
                    statements.RemoveAt(statements.Count - 1);
                    statements.Add(new QsStatement(
                        QsStatementKind.NewQsConditionalStatement(newConditionalStatement),
                        LocalDeclarations.Empty,
                        QsNullable<QsLocation>.Null,
                        QsComments.Empty));

                    var newSpecialization = new QsSpecialization(
                        specialization.Kind,
                        specialization.Parent,
                        specialization.Attributes,
                        specialization.SourceFile,
                        specialization.Location,
                        specialization.TypeArguments,
                        specialization.Signature,
                        SpecializationImplementation.NewProvided(
                            providedImplementation.Item1,
                            new QsScope(
                                statements.ToImmutableArray(),
                                providedImplementation.Item2.KnownSymbols)),
                        specialization.Documentation,
                        specialization.Comments);

                    var newCallable = new QsCallable(
                        QsCallableKind.Operation,
                        callable.FullName,
                        callable.Attributes,
                        callable.Modifiers,
                        callable.SourceFile,
                        callable.Location,
                        callable.Signature,
                        callable.ArgumentTuple,
                        ImmutableArray.Create(newSpecialization),
                        callable.Documentation,
                        callable.Comments);

                    return newCallable;
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