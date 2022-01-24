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
    using ParameterTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>;
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

                internal ImmutableArray<LocalVariableDeclaration<string>> KnownVariables { get; set; } = ImmutableArray<LocalVariableDeclaration<string>>.Empty;

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

                private ParameterTuple MakeLambdaParams(ResolvedType expressionType, QsSymbol paramNames)
                {
                    var missingSymbolCount = 0;

                    ParameterTuple MatchNameWithType(ResolvedType paramType, QsSymbol paramName)
                    {
                        if (paramName.Symbol.IsMissingSymbol)
                        {
                            var localVar = new LocalVariableDeclaration<QsLocalSymbol>(
                                QsLocalSymbol.NewValidName($"__missingLambdaParam_{missingSymbolCount}__"),
                                paramType,
                                new InferredExpressionInformation(false, false),
                                QsNullable<Position>.Null,
                                paramName.Range.IsNull
                                    ? DataTypes.Range.Zero
                                    : paramName.Range.Item);
                            missingSymbolCount++;
                            return ParameterTuple.NewQsTupleItem(localVar);
                        }
                        else if (paramName.Symbol is QsSymbolKind<QsSymbol>.Symbol sym)
                        {
                            var localVar = new LocalVariableDeclaration<QsLocalSymbol>(
                                QsLocalSymbol.NewValidName(sym.Item),
                                paramType,
                                new InferredExpressionInformation(false, false),
                                QsNullable<Position>.Null,
                                paramName.Range.IsNull
                                    ? DataTypes.Range.Zero
                                    : paramName.Range.Item);
                            return ParameterTuple.NewQsTupleItem(localVar);
                        }
                        else if (paramName.Symbol is QsSymbolKind<QsSymbol>.SymbolTuple tup)
                        {
                            if (tup.Item.Length == 0 && paramType.Resolution.IsUnitType)
                            {
                                // Need an artificial Unit parameter here
                                var localVar = new LocalVariableDeclaration<QsLocalSymbol>(
                                    QsLocalSymbol.NewValidName("__lambdaUnitParam__"),
                                    paramType,
                                    new InferredExpressionInformation(false, false),
                                    QsNullable<Position>.Null,
                                    paramName.Range.IsNull
                                        ? DataTypes.Range.Zero
                                        : paramName.Range.Item);
                                return ParameterTuple.NewQsTupleItem(localVar);
                            }
                            else if (tup.Item.Length == 1)
                            {
                                return MatchNameWithType(paramType, tup.Item.First());
                            }
                            else if (paramType.Resolution is ResolvedTypeKind.TupleType tupType)
                            {
                                var subSymbols = tup.Item;
                                var subSybmolTypes = tupType.Item;

                                if (subSymbols.Length != subSybmolTypes.Length)
                                {
                                    throw new ArgumentException("Lambda parameter type length mismatch");
                                }

                                return ParameterTuple.NewQsTuple(subSymbols
                                    .Select((symbol, i) => MatchNameWithType(subSybmolTypes[i], symbol))
                                    .ToImmutableArray());
                            }
                            else
                            {
                                throw new ArgumentException("Lambda parameter type mismatch");
                            }
                        }
                        else
                        {
                            throw new ArgumentException("Lambda parameter type mismatch");
                        }
                    }

                    var paramTypes =
                        expressionType.Resolution is ResolvedTypeKind.Operation op
                        ? op.Item1.Item1
                        : expressionType.Resolution is ResolvedTypeKind.Function func
                        ? func.Item1
                        : throw new ArgumentException("Lambda with non-callable type");

                    return MatchNameWithType(paramTypes, paramNames);
                }

                private TypedExpression HandleLambdas(TypedExpression ex, Lambda<TypedExpression> lambda)
                {
                    var processedLambdaExpressionKind = this.ExpressionKinds.OnLambda(lambda);
                    var processedLambda = (processedLambdaExpressionKind as ExpressionKind.Lambda)!.Item;
                    var lambdaBody = processedLambda.Body;
                    var lambdaParams = this.MakeLambdaParams(ex.ResolvedType, processedLambda.Param);
                    var callableInfo =
                        ex.ResolvedType.Resolution is ResolvedTypeKind.Operation op
                        ? op.Item2
                        : ex.ResolvedType.Resolution is ResolvedTypeKind.Function
                        ? new CallableInformation(ResolvedCharacteristics.Empty, InferredCallableInformation.NoInformation)
                        : throw new ArgumentException("Lambda with non-callable type");

                    // Returns are allowed only if we are not returning Unit from an operation.
                    var isReturnStatement =
                        !(ex.ResolvedType.Resolution is ResolvedTypeKind.Operation opType) || !opType.Item1.Item2.Resolution.IsUnitType;

                    // Alternatively:
                    // Returns are allowed only if we are not doing an adjoint specialization.
                    // var isReturnStatement =
                    //    callableInfo.Characteristics.SupportedFunctors.IsNull
                    //    || !callableInfo.Characteristics.SupportedFunctors.Item.Contains(QsFunctor.Adjoint);
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

                    if (success)
                    {
                        this.SharedState.GeneratedCallables!.Add(callable!);
                        return call!;
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
