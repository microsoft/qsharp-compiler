// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.Transformations.MonomorphizationValidation
{
    public class old_MonomorphizationValidationTransformation
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
            ExpressionTransformation<Core.ExpressionKindTransformation, MonomorphizationValidationExpressionType>
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

    public class MonomorphizationValidationTransformation
    {
        private readonly BaseTransformation transformation;
        
        public static void Apply(QsCompilation compilation)
        {
            var filter = new MonomorphizationValidationTransformation();

            foreach (var ns in compilation.Namespaces)
            {
                filter.transformation.onNamespace.Call(ns);
            }
        }

        public MonomorphizationValidationTransformation()
        {
            transformation = new BaseTransformation();

            transformation.onSignature.Add(x => this.onSignature(x));
            transformation.onTypeParamResolutions.Add(x => this.onTypeParamResolutions(x));
            transformation.onTypeParameter.Add(x => this.onTypeParameter(x));
        }

        private ResolvedSignature onSignature(ResolvedSignature s)
        {
            if (s.TypeParameters.Any())
            {
                throw new Exception("Signatures cannot contains type parameters");
            }

            return transformation.onSignature.CallDefault(s);
        }

        public ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> onTypeParamResolutions(ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> typeParams)
        {
            if (typeParams.Any())
            {
                throw new Exception("Type Parameter Resolutions must be empty");
            }

            return typeParams;
        }

        public QsTypeParameter onTypeParameter(QsTypeParameter tp)
        {
            throw new Exception("Type Parameter types must be resolved");
        }
    }
}
