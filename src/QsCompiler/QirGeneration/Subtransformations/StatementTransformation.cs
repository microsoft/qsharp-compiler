// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    internal class QirStatementTransformation : StatementTransformation<GenerationContext>
    {
        public QirStatementTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation)
            : base(parentTransformation)
        {
        }

        public QirStatementTransformation(GenerationContext sharedState)
            : base(sharedState)
        {
        }

        public QirStatementTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation, TransformationOptions options)
            : base(parentTransformation, options)
        {
        }

        public QirStatementTransformation(GenerationContext sharedState, TransformationOptions options)
            : base(sharedState, options)
        {
        }

        /* public overrides */

        public override QsStatement OnStatement(QsStatement stm)
        {
            this.SharedState.DIManager.StatementLocationStack.Push(stm.Location.Item);
            this.SharedState.DIManager.EmitLocation();
            QsStatement result = base.OnStatement(stm);
            this.SharedState.DIManager.StatementLocationStack.Pop();
            return result;
        }
    }
}
