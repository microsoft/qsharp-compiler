using Llvm.NET.Types;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Quantum.QsCompiler.QirGenerator
{
    public class QirTransformation : SyntaxTreeTransformation<GenerationContext>
    {
        public QirTransformation(GenerationContext state) : base(state, TransformationOptions.NoRebuild)
        {
            state.Transformation = this;

            this.Namespaces = new QirNamespaceTransformation(this, TransformationOptions.NoRebuild);
            this.StatementKinds = new QirStatementKindTransformation(this, TransformationOptions.NoRebuild);
            this.Expressions = new QirExpressionTransformation(this, TransformationOptions.NoRebuild);
            this.ExpressionKinds = new QirExpressionKindTransformation(this, TransformationOptions.NoRebuild);
            this.Types = new QirTypeTransformation(this, TransformationOptions.NoRebuild);
        }
    }
}
