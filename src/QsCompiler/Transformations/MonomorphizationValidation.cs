// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;


namespace Microsoft.Quantum.QsCompiler.Transformations.Monomorphization.Validation
{
    public class ValidateMonomorphization : SyntaxTreeTransformation<ValidateMonomorphization.TransformationState>
    {
        public static void Apply(QsCompilation compilation)
        {
            var filter = new ValidateMonomorphization();

            foreach (var ns in compilation.Namespaces)
            {
                filter.Namespaces.OnNamespace(ns);
            }
        }

        public class TransformationState { }

        internal ValidateMonomorphization() : base(new TransformationState()) 
        { 
            this.Namespaces = new NamespaceTransformation(this);
            this.Expressions = new ExpressionTransformation(this);
            this.Types = new TypeTransformation(this);
        }

        private class NamespaceTransformation : NamespaceTransformation<TransformationState>
        {
            public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

            public override ResolvedSignature OnSignature(ResolvedSignature s)
            {
                if (s.TypeParameters.Any())
                {
                    throw new Exception("Signatures cannot contains type parameters");
                }

                return base.OnSignature(s);
            }
        }

        private class ExpressionTransformation : ExpressionTransformation<TransformationState>
        {
            public ExpressionTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

            public override ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> OnTypeParamResolutions(ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> typeParams)
            {
                if (typeParams.Any())
                {
                    throw new Exception("Type Parameter Resolutions must be empty");
                }

                return typeParams;
            }
        }

        private class TypeTransformation : TypeTransformation<TransformationState>
        {
            public TypeTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

            public override QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation> OnTypeParameter(QsTypeParameter tp)
            {
                throw new Exception("Type Parameter types must be resolved");
            }
        }
    }
}
