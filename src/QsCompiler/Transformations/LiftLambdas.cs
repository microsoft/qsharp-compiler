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

namespace Microsoft.Quantum.QsCompiler.Transformations.LiftLambdas
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using TypeArgsResolution = ImmutableArray<Tuple<QsQualifiedName, string, ResolvedType>>;

    internal static class LiftLambdaExpressions
    {
        public static QsCompilation Apply(QsCompilation compilation)
        {
            return compilation;
        }

        private class LiftContent : ContentLifting.LiftContent<LiftContent.TransformationState>
        {
            internal class TransformationState : ContentLifting.LiftContent.TransformationState
            {
                internal List<QsCallable>? GeneratedCallables { get; set; } = null;

                internal ImmutableArray<LocalVariableDeclaration<string>> KnownVariables { get; set; } = ImmutableArray<LocalVariableDeclaration<string>>.Empty;
        }

            public LiftContent()
                : base(new TransformationState())
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.Statements = new StatementTransformation(this);
                this.Expressions = new ExpressionTransformation(this);
            }

            private new class NamespaceTransformation : ContentLifting.LiftContent<TransformationState>.NamespaceTransformation
            {
                public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent)
                    : base(parent)
                {
                }

                /// <inheritdoc/>
                public override QsNamespace OnNamespace(QsNamespace ns)
                {
                    // Generated callables list will be populated in the transform
                    this.SharedState.GeneratedCallables = new List<QsCallable>();
                    return base.OnNamespace(ns)
                        .WithElements(elems => elems.AddRange(this.SharedState.GeneratedCallables.Select(callable => QsNamespaceElement.NewQsCallable(callable))));
                }
            }

            private class StatementTransformation : StatementTransformation<TransformationState>
            {
                public StatementTransformation(SyntaxTreeTransformation<TransformationState> parent)
                    : base(parent)
                {
                }

                public override QsScope OnScope(QsScope scope)
                {
                    var saved = this.SharedState.KnownVariables;
                    this.SharedState.KnownVariables = this.SharedState.KnownVariables
                        .Concat(scope.KnownSymbols.Variables)
                        .ToImmutableArray();
                    var returnValue = base.OnScope(scope);
                    this.SharedState.KnownVariables = saved;
                    return returnValue;
                }

                public override QsStatement OnStatement(QsStatement stm)
                {
                    this.SharedState.KnownVariables = this.SharedState.KnownVariables
                        .Concat(stm.SymbolDeclarations.Variables)
                        .ToImmutableArray();
                    return base.OnStatement(stm);
                }
            }

            private class ExpressionTransformation : ExpressionTransformation<TransformationState>
            {
                public ExpressionTransformation(SyntaxTreeTransformation<TransformationState> parent)
                    : base(parent)
                {
                }

                public override TypedExpression OnTypedExpression(TypedExpression ex)
                {
                    if (ex.Expression is ExpressionKind.Lambda lambda)
                    {
                        return this.HandleLambdas(ex, lambda.Item);
                    }
                    else
                    {
                        return base.OnTypedExpression(ex);
                    }
                }

                private TypedExpression HandleLambdas(TypedExpression ex, Lambda<TypedExpression> lambda)
                {
                    var processedLambdaExpressionKind = this.ExpressionKinds.OnLambda(lambda);
                    var processedLambda = (processedLambdaExpressionKind as ExpressionKind.Lambda)!.Item;

                    var lambdaBody = processedLambda.Body;
                    var returnStatment = new QsStatement(
                        QsStatementKind.NewQsReturnStatement(lambdaBody),
                        LocalDeclarations.Empty,
                        QsNullable<QsLocation>.Null,
                        QsComments.Empty);

                    // ToDo: local declarations should include all local variables in scope as well as lambda parameters.
                    var lambdaParams = new List<LocalVariableDeclaration<string>>();
                    var generatedParams = this.SharedState.KnownVariables.Concat(lambdaParams);

                    var generatedContent = new QsScope(new[] { returnStatment }.ToImmutableArray(), new LocalDeclarations(generatedParams.ToImmutableArray()));

                    // The LiftBody determines which callable kind to generate based on the callable kind of the parent callable.
                    // We need to override that behavior with the lambda kind. So we just change the callable in the shared state
                    // to have the callable kind that we want.
                    var originalCallable = this.SharedState.CurrentCallable!.Callable;
                    var modifiedCallabe = new QsCallable(
                        processedLambda.Kind.IsFunction ? QsCallableKind.Function : QsCallableKind.Operation,
                        originalCallable.FullName,
                        originalCallable.Attributes,
                        originalCallable.Access,
                        originalCallable.Source,
                        originalCallable.Location,
                        originalCallable.Signature,
                        originalCallable.ArgumentTuple,
                        originalCallable.Specializations,
                        originalCallable.Documentation,
                        originalCallable.Comments);

                    this.SharedState.CurrentCallable = new ContentLifting.LiftContent.CallableDetails(modifiedCallabe);
                    if (this.SharedState.LiftBody(generatedContent, true, out var call, out var callable))
                    {
                        // ToDo: ensure the lambda parameters are present and last in the parameter list for the generated callable,
                        // and not present in the call expression.

                        this.SharedState.GeneratedCallables!.Add(callable);

                        return new TypedExpression(
                            call.Expression,
                            call.TypeArguments,
                            ex.ResolvedType,
                            ex.InferredInformation,
                            ex.Range);
                    }
                    else
                    {
                        return new TypedExpression(
                            processedLambdaExpressionKind,
                            ex.TypeArguments,
                            ex.ResolvedType,
                            ex.InferredInformation,
                            ex.Range);
                    }
                }
            }
        }
    }
}
