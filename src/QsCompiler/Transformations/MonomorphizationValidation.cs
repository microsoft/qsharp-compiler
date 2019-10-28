using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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

        internal class MonomorphizationValidationSyntax : SyntaxTreeTransformation<ScopeTransformation<MonomorphizationValidationExpression>>
        {
            public MonomorphizationValidationSyntax(ScopeTransformation<MonomorphizationValidationExpression> scope = null) :
                base(scope ?? new ScopeTransformation<MonomorphizationValidationExpression>(new MonomorphizationValidationExpression())) { }

            public override ResolvedSignature onSignature(ResolvedSignature s)
            {
                if (s.TypeParameters.Any())
                {
                    // TODO: throw error
                    throw new Exception("Signatures cannot contains type parameters");
                }

                return base.onSignature(s);
            }
        }

        internal class MonomorphizationValidationExpression :
            ExpressionTransformation<Core.ExpressionKindTransformation, MonomorphizationValidationExpressionType>
        {
            public MonomorphizationValidationExpression() :
                base(null, expr => new MonomorphizationValidationExpressionType(expr as MonomorphizationValidationExpression))
            { }

            public override ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> onTypeParamResolutions(ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> typeParams)
            {
                if (typeParams.Any())
                {
                    // TODO: throw error
                    throw new Exception("Type Parameter Resolutions must be empty");
                }

                return typeParams;
            }
        }

        internal class MonomorphizationValidationExpressionType : ExpressionTypeTransformation<MonomorphizationValidationExpression>
        {
            public MonomorphizationValidationExpressionType(MonomorphizationValidationExpression expr) : base(expr) { }

            public override QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation> onTypeParameter(QsTypeParameter tp)
            {
                // TODO: throw error
                throw new Exception("Type Parameter types must be resolved");
            }
        }
    }
}
