// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.Transformations.LiftLambdas.Validation
{
    /// <summary>
    /// Validates that the lambda lifting transformation has removed all lambda expressions.
    /// </summary>
    public class ValidateLambdaLifting : SyntaxTreeTransformation
    {
        /// <summary>
        /// Applies the transformation that walks through the syntax tree, checking to ensure
        /// that all lambda expressions have been removed.
        /// </summary>
        public static void Apply(QsCompilation compilation)
        {
            new ValidateLambdaLifting().OnCompilation(compilation);
        }

        internal ValidateLambdaLifting()
            : base()
        {
            this.Namespaces = new NamespaceTransformation(this, TransformationOptions.NoRebuild);
            this.Statements = new StatementTransformation(this, TransformationOptions.NoRebuild);
            this.StatementKinds = new StatementKindTransformation(this, TransformationOptions.NoRebuild);
            this.Expressions = new ExpressionTransformation(this, TransformationOptions.NoRebuild);
            this.ExpressionKinds = new ExpressionKindTransformation(this);
            this.Types = new TypeTransformation(this, TransformationOptions.Disabled);
        }

        private class ExpressionKindTransformation : Core.ExpressionKindTransformation
        {
            public ExpressionKindTransformation(SyntaxTreeTransformation parent)
                : base(parent, TransformationOptions.NoRebuild)
            {
            }

            public override QsExpressionKind<TypedExpression, Identifier, ResolvedType> OnLambda(Lambda<TypedExpression> lambda)
            {
                throw new Exception("Lambda expressions must be removed.");
            }
        }
    }
}
