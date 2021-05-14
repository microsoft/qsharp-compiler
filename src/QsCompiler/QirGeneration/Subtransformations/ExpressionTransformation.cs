﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    internal class QirExpressionTransformation : ExpressionTransformation<GenerationContext>
    {
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
            this.SharedState.ExpressionTypeStack.Push(ex.ResolvedType);
            var result = base.OnTypedExpression(ex);
            this.SharedState.ExpressionTypeStack.Pop();
            return result;
        }
    }
}
