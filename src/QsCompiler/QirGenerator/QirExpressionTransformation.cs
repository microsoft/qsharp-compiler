using Llvm.NET.Types;
using Llvm.NET.Values;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Microsoft.Quantum.QsCompiler.QirGenerator
{
    public class QirExpressionTransformation : ExpressionTransformation<GenerationContext>
    {
        public QirExpressionTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation) : base(parentTransformation)
        {
        }

        public QirExpressionTransformation(GenerationContext sharedState) : base(sharedState)
        {
        }

        public QirExpressionTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation, TransformationOptions options) : base(parentTransformation, options)
        {
        }

        public QirExpressionTransformation(GenerationContext sharedState, TransformationOptions options) : base(sharedState, options)
        {
        }

        //public override InferredExpressionInformation OnExpressionInformation(InferredExpressionInformation info)
        //{
        //    return base.OnExpressionInformation(info);
        //}

        //public override QsNullable<Tuple<QsPositionInfo, QsPositionInfo>> OnRangeInformation(QsNullable<Tuple<QsPositionInfo, QsPositionInfo>> r)
        //{
        //    return base.OnRangeInformation(r);
        //}

        public override TypedExpression OnTypedExpression(TypedExpression ex)
        {
            this.SharedState.ExpressionTypeStack.Push(ex.ResolvedType);

            var result = base.OnTypedExpression(ex);

            this.SharedState.ExpressionTypeStack.Pop();

            return result;
        }

        //public override ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> OnTypeParamResolutions(ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> typeParams)
        //{
        //    return base.OnTypeParamResolutions(typeParams);
        //}
    }
}
