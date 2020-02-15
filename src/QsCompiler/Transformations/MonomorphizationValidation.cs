// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;


namespace Microsoft.Quantum.QsCompiler.Transformations.MonomorphizationValidation
{
    public class MonomorphizationValidationTransformation
    {
        public static void Apply(QsCompilation compilation)
        {
            var filter = new MonomorphizationValidationSyntax();

            foreach (var ns in compilation.Namespaces)
            {
                filter.Transform(ns);
            }
        }

        private class MonomorphizationValidationSyntax : SyntaxTreeTransformation<ScopeTransformation<MonomorphizationValidationExpression>>
        {
            public MonomorphizationValidationSyntax(ScopeTransformation<MonomorphizationValidationExpression> scope = null) :
                base(scope ?? new ScopeTransformation<MonomorphizationValidationExpression>(new MonomorphizationValidationExpression())) { }

            public override ResolvedSignature onSignature(ResolvedSignature s)
            {
                if (s.TypeParameters.Any())
                {
                    throw new Exception("Signatures cannot contains type parameters");
                }

                return base.onSignature(s);
            }
        }

        private class MonomorphizationValidationExpression :
            ExpressionTransformation<Core.ExpressionKindTransformationBase, MonomorphizationValidationExpressionType>
        {
            public MonomorphizationValidationExpression() :
                base(expr => new ExpressionKindTransformation<MonomorphizationValidationExpression>(expr as MonomorphizationValidationExpression),
                     expr => new MonomorphizationValidationExpressionType(expr as MonomorphizationValidationExpression))
            { }

            public override ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> onTypeParamResolutions(ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> typeParams)
            {
                if (typeParams.Any())
                {
                    throw new Exception("Type Parameter Resolutions must be empty");
                }

                return typeParams;
            }
        }

        private class MonomorphizationValidationExpressionType : ExpressionTypeTransformation<MonomorphizationValidationExpression>
        {
            public MonomorphizationValidationExpressionType(MonomorphizationValidationExpression expr) : base(expr) { }

            public override QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation> onTypeParameter(QsTypeParameter tp)
            {
                throw new Exception("Type Parameter types must be resolved");
            }
        }
    }
}
