// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;


namespace Microsoft.Quantum.QsCompiler.Transformations.MonomorphizationValidationTransformation
{
    public class MonomorphizationValidationTransformation : QsSyntaxTreeTransformation<MonomorphizationValidationTransformation.TransformationState>
    {
        public static void Apply(QsCompilation compilation)
        {
            var filter = new MonomorphizationValidationTransformation();

            foreach (var ns in compilation.Namespaces)
            {
                filter.Namespaces.Transform(ns);
            }
        }

        public class TransformationState { }

        public MonomorphizationValidationTransformation() : base(new TransformationState()) { }

        public override NamespaceTransformation<TransformationState> NewNamespaceTransformation() => new NamespaceTransformation(this);
        private class NamespaceTransformation : NamespaceTransformation<TransformationState>
        {
            public NamespaceTransformation(QsSyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

            public override ResolvedSignature onSignature(ResolvedSignature s)
            {
                if (s.TypeParameters.Any())
                {
                    throw new Exception("Signatures cannot contains type parameters");
                }

                return base.onSignature(s);
            }
        }

        public override Core.ExpressionTransformation<TransformationState> NewExpressionTransformation() => new ExpressionTransformation(this);
        private class ExpressionTransformation : Core.ExpressionTransformation<TransformationState>
        {
            public ExpressionTransformation(QsSyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

            public override ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> onTypeParamResolutions(ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> typeParams)
            {
                if (typeParams.Any())
                {
                    throw new Exception("Type Parameter Resolutions must be empty");
                }

                return typeParams;
            }
        }

        public override Core.ExpressionTypeTransformation<TransformationState> NewExpressionTypeTransformation() => new ExpressionTypeTransformation(this);
        private class ExpressionTypeTransformation : Core.ExpressionTypeTransformation<TransformationState>
        {
            public ExpressionTypeTransformation(QsSyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

            public override QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation> onTypeParameter(QsTypeParameter tp)
            {
                throw new Exception("Type Parameter types must be resolved");
            }
        }
    }
}
