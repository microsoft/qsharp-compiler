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
    using ParameterTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol, ResolvedType>>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    internal static class LiftLambdaExpressions
    {
        public static QsCompilation Apply(QsCompilation compilation)
        {
            var filter = new LiftContent();
            var transformed = filter.OnCompilation(compilation);
            return transformed;
        }

        private class LiftContent : SyntaxTreeTransformation<LiftContent.TransformationState>
        {
            internal class TransformationState
            {
                internal List<QsCallable>? GeneratedCallables { get; set; } = null;

                internal ImmutableArray<LocalVariableDeclaration<string, ResolvedType>> KnownVariables { get; set; } =
                    ImmutableArray<LocalVariableDeclaration<string, ResolvedType>>.Empty;

                internal QsCallable? CurrentCallable { get; set; } = null;
            }

            public LiftContent()
                : base(new TransformationState())
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.Statements = new StatementTransformation(this);
                this.Expressions = new ExpressionTransformation(this);
            }

            private class NamespaceTransformation : NamespaceTransformation<TransformationState>
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

                /// <inheritdoc/>
                public override QsCallable OnCallableDeclaration(QsCallable c)
                {
                    this.SharedState.CurrentCallable = c;
                    return base.OnCallableDeclaration(c);
                }
            }

            private class StatementTransformation : StatementTransformation<TransformationState>
            {
                public StatementTransformation(SyntaxTreeTransformation<TransformationState> parent)
                    : base(parent)
                {
                }

                /// <inheritdoc/>
                public override QsScope OnScope(QsScope scope)
                {
                    var saved = this.SharedState.KnownVariables;
                    this.SharedState.KnownVariables = scope.KnownSymbols.Variables;
                    var result = base.OnScope(scope);
                    this.SharedState.KnownVariables = saved;
                    return result;
                }

                /// <inheritdoc/>
                public override QsStatement OnStatement(QsStatement stm)
                {
                    var result = base.OnStatement(stm);
                    this.SharedState.KnownVariables = this.SharedState.KnownVariables
                        .Concat(stm.SymbolDeclarations.Variables)
                        .ToImmutableArray();
                    return result;
                }
            }

            private class ExpressionTransformation : ExpressionTransformation<TransformationState>
            {
                public ExpressionTransformation(SyntaxTreeTransformation<TransformationState> parent)
                    : base(parent)
                {
                }

                /// <inheritdoc/>
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

                private ParameterTuple SanitizeLambdaParams(ParameterTuple paramNames)
                {
                    var missingSymbolCount = 0;

                    ParameterTuple Sanitize(ParameterTuple paramName)
                    {
                        if (paramName is ParameterTuple.QsTupleItem param)
                        {
                            return param.Item.VariableName.IsInvalidName
                                ? ParameterTuple.NewQsTupleItem(
                                    param.Item.WithName(QsLocalSymbol.NewValidName($"__missingLambdaParam_{missingSymbolCount++}__")))
                                : param;
                        }
                        else if (paramName is ParameterTuple.QsTuple tup)
                        {
                            if (tup.Item.Length == 0)
                            {
                                // Need an artificial Unit parameter here
                                var localVar = new LocalVariableDeclaration<QsLocalSymbol, ResolvedType>(
                                    QsLocalSymbol.NewValidName("__lambdaUnitParam__"),
                                    ResolvedType.New(ResolvedTypeKind.UnitType),
                                    InferredExpressionInformation.ParameterDeclaration,
                                    QsNullable<Position>.Null,
                                    DataTypes.Range.Zero);
                                return ParameterTuple.NewQsTupleItem(localVar);
                            }
                            else if (tup.Item.Length == 1)
                            {
                                return Sanitize(tup.Item.First());
                            }
                            else
                            {
                                return ParameterTuple.NewQsTuple(tup.Item
                                    .Select(symbol => Sanitize(symbol))
                                    .ToImmutableArray());
                            }
                        }
                        else
                        {
                            throw new ArgumentException("Unsupported lambda parameter. Only `QsTupleItem`, and `QsTuple` are supported.");
                        }
                    }

                    return Sanitize(paramNames);
                }

                private TypedExpression HandleLambdas(TypedExpression ex, Lambda<TypedExpression, ResolvedType> lambda)
                {
                    var processedLambdaExpressionKind = this.ExpressionKinds.OnLambda(lambda);
                    var processedLambda = (processedLambdaExpressionKind as ExpressionKind.Lambda)!.Item;
                    var lambdaBody = processedLambda.Body;
                    var lambdaParams = this.SanitizeLambdaParams(processedLambda.ArgumentTuple);
                    var callableInfo =
                        ex.ResolvedType.Resolution is ResolvedTypeKind.Operation op
                        ? op.Item2
                        : ex.ResolvedType.Resolution is ResolvedTypeKind.Function
                        ? new CallableInformation(ResolvedCharacteristics.Empty, InferredCallableInformation.NoInformation)
                        : throw new ArgumentException("Lambda with non-callable type");

                    // Returns are allowed only if we are not doing an adjoint specialization.
                    var isReturnStatement =
                       !lambdaBody.Expression.IsUnitValue // There will be no statements in this case.
                       && (callableInfo.Characteristics.SupportedFunctors.IsNull
                       || !callableInfo.Characteristics.SupportedFunctors.Item.Contains(QsFunctor.Adjoint));
                    var bodyStatment = new QsStatement(
                        isReturnStatement
                            ? QsStatementKind.NewQsReturnStatement(lambdaBody)
                            : QsStatementKind.NewQsExpressionStatement(lambdaBody),
                        LocalDeclarations.Empty,
                        QsNullable<QsLocation>.Null,
                        QsComments.Empty);
                    var generatedContent = new QsScope(
                        lambdaBody.Expression.IsUnitValue
                            ? ImmutableArray<QsStatement>.Empty // if it is just a single unit literal, there should be an empty body
                            : new[] { bodyStatment }.ToImmutableArray(),
                        new LocalDeclarations(this.SharedState.KnownVariables));

                    var success = processedLambda.Kind.IsFunction
                        ? ContentLifting.LiftContent.LiftFunctionBody(this.SharedState.CurrentCallable!, generatedContent, lambdaParams, isReturnStatement, out var call, out var callable)
                        : ContentLifting.LiftContent.LiftOperationBody(this.SharedState.CurrentCallable!, generatedContent, lambdaParams, callableInfo, isReturnStatement, out call, out callable);

                    if (!success)
                    {
                        throw new ArgumentException("Lambda failed to be lifted.");
                    }

                    this.SharedState.GeneratedCallables!.Add(callable!);
                    return call!;
                }
            }
        }
    }
}
