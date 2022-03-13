// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.Transformations.Monomorphization2.Validation
{
    /// <summary>
    /// Validates that the monomorphization transformation has removed all references to
    /// generic objects.
    /// </summary>
    public class ValidateMonomorphization : MonoTransformation
    {
        /// <summary>
        /// Applies the transformation that walks through the syntax tree, checking to ensure
        /// that all generic data has been removed. If allowTypeParametersForIntrinsics is true,
        /// then generic data is allowed for type parameters of callables that have an intrinsic body.
        /// </summary>
        public static void Apply(QsCompilation compilation, bool allowTypeParametersForIntrinsics = true)
        {
            var intrinsicCallableSet = allowTypeParametersForIntrinsics
                ? compilation.Namespaces.GlobalCallableResolutions()
                    .Where(kvp => kvp.Value.Specializations.Any(spec => spec.Implementation.IsIntrinsic))
                    .Select(kvp => kvp.Key)
                    .ToImmutableHashSet()
                : ImmutableHashSet<QsQualifiedName>.Empty;

            new ValidateMonomorphization(intrinsicCallableSet).OnCompilation(compilation);
        }

        public ImmutableHashSet<QsQualifiedName> IntrinsicCallableSet { get; }

        public ValidateMonomorphization(ImmutableHashSet<QsQualifiedName> intrinsicCallableSet)
        {
            this.IntrinsicCallableSet = intrinsicCallableSet;
        }

        public override QsCallable OnCallableDeclaration(QsCallable c)
        {
            // Don't validate intrinsics
            if (this.IntrinsicCallableSet.Contains(c.FullName))
            {
                return c;
            }
            else
            {
                return base.OnCallableDeclaration(c);
            }
        }

        public override ResolvedSignature OnSignature(ResolvedSignature s)
        {
            if (s.TypeParameters.Any())
            {
                throw new Exception("Signatures cannot contains type parameters");
            }

            return base.OnSignature(s);
        }

        public override ImmutableDictionary<Tuple<QsQualifiedName, string>, ResolvedType> OnTypeParamResolutions(ImmutableDictionary<Tuple<QsQualifiedName, string>, ResolvedType> typeParams)
        {
            // Type resolutions for intrinsics are allowed
            if (typeParams.Any(kvp => !this.IntrinsicCallableSet.Contains(kvp.Key.Item1)))
            {
                throw new Exception("Type Parameter Resolutions must be empty");
            }

            return typeParams;
        }

        // If an intrinsic callable is generic, then its type parameters can occur within expressions;
        // when generic intrinsics are called, the type of that call contains type parameter types.
        public override QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation> OnTypeParameter(QsTypeParameter tp) =>
            this.IntrinsicCallableSet.Contains(tp.Origin)
            ? QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>.NewTypeParameter(tp)
            : throw new Exception("Type Parameter types must be resolved");
    }
}
