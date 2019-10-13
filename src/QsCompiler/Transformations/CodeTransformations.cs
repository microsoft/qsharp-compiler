// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Quantum.QsCompiler.Optimizations;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Conjugations;
using Microsoft.Quantum.QsCompiler.Transformations.FunctorGeneration;
using Microsoft.Quantum.QsCompiler.Transformations.Monomorphization;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;


namespace Microsoft.Quantum.QsCompiler.Transformations
{
    /// <summary>
    /// Static base class to accumulate the handles to individual syntax tree rewrite steps. 
    /// </summary>
    public static class CodeTransformations
    {
        /// <summary>
        /// Given the body of an operation, auto-generates the (content of the) adjoint specialization, 
        /// under the assumption that operation calls may only ever occur within expression statements, 
        /// and while-loops cannot occur within operations. 
        /// Throws an ArgumentNullException if the given scope is null.
        /// </summary>
        public static QsScope GenerateAdjoint(this QsScope scope)
        {
            // Since we are pulling purely classical statements up, we are potentially changing the order of declarations. 
            // We therefore need to generate unique variable names before reordering the statements. 
            scope = new UniqueVariableNames().Transform(scope); 
            scope = ApplyFunctorToOperationCalls.ApplyAdjoint(scope);
            scope = new ReverseOrderOfOperationCalls().Transform(scope);
            return StripPositionInfo.Apply(scope);
        }

        /// <summary>
        /// Given the body of an operation, auto-generates the (content of the) controlled specialization 
        /// using the default name for control qubits.
        /// Throws an ArgumentNullException if the given scope is null.
        /// </summary>
        public static QsScope GenerateControlled(this QsScope scope)
        {
            scope = ApplyFunctorToOperationCalls.ApplyControlled(scope);
            return StripPositionInfo.Apply(scope);
        }

        /// <summary>
        /// Eliminates all conjugations from the given scope by replacing them with the corresponding implementations (i.e. inlining them). 
        /// The generation of the adjoint for the outer block is subject to the same limitation as any adjoint auto-generation. 
        /// In particular, it is only guaranteed to be valid if operation calls only occur within expression statements, and 
        /// throws an InvalidOperationException if the outer block contains while-loops. 
        /// Throws an ArgumentNullException if the given scope is null.
        /// </summary>
        public static QsScope InlineConjugations(this QsScope scope) =>
            new InlineConjugationStatements().Transform(scope);

        /// <summary>
        /// 
        /// </summary>
        public static bool PreEvaluateAll(IEnumerable<QsNamespace> syntaxTree, 
            out IEnumerable<QsNamespace> evaluated, Action<Exception> onException = null)
        {
            try { evaluated = PreEvalution.All(syntaxTree); }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
                evaluated = syntaxTree;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Eliminates all type parameterized callables from the scope by replacing their definitions and references to concrete
        /// versions of the callable.
        /// Throws an ArgumentNullException if the given syntaxTree is null.
        /// </summary>
        public static bool Monomorphisize(IEnumerable<QsNamespace> syntaxTree, out IEnumerable<QsNamespace> result, 
            Action<Exception> onException = null) 
        {
            if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
            result = syntaxTree;
            try
            {
                result = ResolveGenericsSyntax.Apply(syntaxTree);
                return true;
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
                return false;
            }
        }
    }
}


