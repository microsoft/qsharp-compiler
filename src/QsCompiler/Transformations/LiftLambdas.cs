// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
            }

            public LiftContent()
                : base(new TransformationState())
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.ExpressionKinds = new ExpressionKindTransformation(this);
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

            private class ExpressionKindTransformation : ExpressionKindTransformation<TransformationState>
            {
                public ExpressionKindTransformation(SyntaxTreeTransformation<TransformationState> parent)
                    : base(parent)
                {
                }

                /// <inheritdoc/>
                public override ExpressionKind OnLambda(Lambda<TypedExpression> lambda)
                {
                    return base.OnLambda(lambda);
                }
            }
        }
    }
}
