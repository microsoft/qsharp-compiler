// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.Experimental;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Conjugations;
using Microsoft.Quantum.QsCompiler.Transformations.FunctorGeneration;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;

namespace Microsoft.Quantum.QsCompiler
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
        /// </summary>
        public static QsScope GenerateAdjoint(this QsScope scope)
        {
            // Since we are pulling purely classical statements up, we are potentially changing the order of declarations.
            // We therefore need to generate unique variable names before reordering the statements.
            scope = new UniqueVariableNames().Statements.OnScope(scope);
            scope = ApplyFunctorToOperationCalls.ApplyAdjoint(scope);
            scope = new ExtractNestedOperationCalls().Statements.OnScope(scope);
            scope = new ReverseOrderOfOperationCalls().Statements.OnScope(scope);
            return StripPositionInfo.Apply(scope);
        }

        /// <summary>
        /// Given the body of an operation, auto-generates the (content of the) controlled specialization using the default name
        /// for control qubits. Adds the control qubits names to the list of defined variables for the scope and each subscope.
        /// </summary>
        public static QsScope GenerateControlled(this QsScope scope)
        {
            scope = ApplyFunctorToOperationCalls.ApplyControlled(scope);
            return StripPositionInfo.Apply(scope);
        }

        /// <summary>
        /// Eliminates all conjugations from the given compilation by replacing them with the corresponding implementations (i.e. inlining them).
        /// The generation of the adjoint for the outer block is subject to the same limitation as any adjoint auto-generation.
        /// In particular, it is only guaranteed to be valid if operation calls only occur within expression statements, and
        /// throws an InvalidOperationException if the outer block contains while-loops.
        /// Any thrown exception is logged using the given onException action and silently ignored if onException is not specified or null.
        /// Returns true if the transformation succeeded without throwing an exception, and false otherwise.
        /// </summary>
        public static bool InlineConjugations(this QsCompilation compilation, out QsCompilation inlined, Action<Exception>? onException = null)
        {
            var inline = new InlineConjugations(onException);
            var namespaces = compilation.Namespaces.Select(inline.Namespaces.OnNamespace).ToImmutableArray();
            inlined = new QsCompilation(namespaces, compilation.EntryPoints);
            return inline.SharedState.Success;
        }

        /// <summary>
        /// Pre-evaluates as much of the classical computations as possible in the given compilation.
        /// Any thrown exception is logged using the given onException action and silently ignored if onException is not specified or null.
        /// Returns true if the transformation succeeded without throwing an exception, and false otherwise.
        /// </summary>
        public static bool PreEvaluateAll(this QsCompilation compilation, out QsCompilation evaluated, Action<Exception>? onException = null)
        {
            try
            {
                evaluated = PreEvaluation.All(compilation);
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
                evaluated = compilation;
                return false;
            }
            return true;
        }
    }
}
