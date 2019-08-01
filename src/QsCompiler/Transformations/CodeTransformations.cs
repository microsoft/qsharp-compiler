// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Conjugations;
using Microsoft.Quantum.QsCompiler.Transformations.FunctorGeneration;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;


namespace Microsoft.Quantum.QsCompiler.Transformations
{
    /// <summary>
    /// Static base class to accumulate the handles to individual syntax tree rewrite steps. 
    /// </summary>
    public static class CodeTransformations
    {
        /// <summary>
        /// Applying the transformation sets all location information to Null.
        /// </summary>
        private class StripLocationInformation :
            ScopeTransformation<StatementKindTransformation<StripLocationInformation>, NoExpressionTransformations>
        {
            public StripLocationInformation() :
                base(s => new StatementKindTransformation<StripLocationInformation>(s as StripLocationInformation), new NoExpressionTransformations())
            { }

            public override QsNullable<QsLocation> onLocation(QsNullable<QsLocation> loc) =>
                QsNullable<QsLocation>.Null;

            public static readonly Func<QsScope, QsScope> Apply =
                new StripLocationInformation().Transform;
        }

        /// <summary>
        /// Given the body of an operation, auto-generates the (content of the) adjoint specialization, 
        /// under the assumption that operation calls may only ever occur within expression statements, 
        /// and while-loops cannot occur within operations. 
        /// </summary>
        public static QsScope GenerateAdjoint(this QsScope scope)
        {
            scope = new UniqueVariableNames().Transform(scope);
            scope = ApplyFunctorToOperationCalls.ApplyAdjoint(scope);
            scope = new ReverseOrderOfOperationCalls().Transform(scope);
            return StripLocationInformation.Apply(scope);
        }

        /// <summary>
        /// Given the body of an operation, auto-generates the (content of the) controlled specialization 
        /// using the default name for control qubits.
        /// </summary>
        public static QsScope GenerateControlled(this QsScope scope)
        {
            scope = ApplyFunctorToOperationCalls.ApplyControlled(scope);
            return StripLocationInformation.Apply(scope);
        }

        /// <summary>
        /// Eliminates all conjugate-statements from the given scope by replacing them with the corresponding implementations (i.e. inlining them). 
        /// The generation of the adjoint for the outer block needed for conjugation is subject to the same limitation as any adjoint auto-generation. 
        /// In particular, it is only guaranteed to be valid if operation calls only occur within expression statements, and 
        /// throws an InvalidOperationException if the outer block contains while-loops. 
        /// </summary>
        public static QsScope InlineConjugations(this QsScope scope) =>
            new InlineConjugateStatements().Transform(scope); 
    }
}


