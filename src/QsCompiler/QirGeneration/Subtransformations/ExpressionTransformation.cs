// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    internal class QirExpressionTransformation : ExpressionTransformation<GenerationContext>
    {
        internal class Metadata
        {
            public ResolvedType Type { get; }

            public bool MayEscapeItsScope { get; } = true;

            internal Metadata(ResolvedType type, bool mayEscapeScope = true)
            {
                this.Type = type;
                this.MayEscapeItsScope = mayEscapeScope;
            }
        }

        public QirExpressionTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation)
            : base(parentTransformation)
        {
        }

        public QirExpressionTransformation(GenerationContext sharedState)
            : base(sharedState)
        {
        }

        public QirExpressionTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation, TransformationOptions options)
            : base(parentTransformation, options)
        {
        }

        public QirExpressionTransformation(GenerationContext sharedState, TransformationOptions options)
            : base(sharedState, options)
        {
        }

        /* public overrides */

        public override TypedExpression OnTypedExpression(TypedExpression ex)
        {
            // Todo: write a proper analysis to determine whether a value may escape its scope.
            var metadata = new Metadata(
                ex.ResolvedType,
                mayEscapeScope: !this.SharedState.TargetQirProfile);

            this.SharedState.ExpressionMetadataStack.Push(metadata);
            var result = base.OnTypedExpression(ex);
            this.SharedState.ExpressionMetadataStack.Pop();
            return result;
        }
    }
}
