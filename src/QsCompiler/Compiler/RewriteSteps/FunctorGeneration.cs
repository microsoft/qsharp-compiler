// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    /// <summary>
    /// Replaces all functor generation directives with the corresponding implementation.
    /// </summary>
    internal class FunctorGeneration : IRewriteStep
    {
        public string Name => "Functor Generation";

        public int Priority => RewriteStepPriorities.GenerationOfFunctorSupport;

        public IDictionary<string, string?> AssemblyConstants { get; }

        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => Enumerable.Empty<IRewriteStep.Diagnostic>();

        public bool ImplementsPreconditionVerification => true;

        public bool ImplementsTransformation => true;

        public bool ImplementsPostconditionVerification => false;

        public FunctorGeneration()
        {
            this.AssemblyConstants = new Dictionary<string, string?>();
        }

        public bool PreconditionVerification(QsCompilation compilation)
        {
            var requiredNamespace = compilation.Namespaces
                .FirstOrDefault(ns => ns.Name.Equals(BuiltIn.CoreNamespace));

            if (requiredNamespace == null)
            {
                return false;
            }

            var providedOperations = new QsNamespace[] { requiredNamespace }
                .Callables()
                .Select(c => c.FullName)
                .ToHashSet();
            var requiredBuiltIns = new HashSet<QsQualifiedName>()
            {
                BuiltIn.Length.FullName,
                BuiltIn.RangeReverse.FullName
            };

            return requiredBuiltIns.IsSubsetOf(providedOperations);
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            return CodeGeneration.GenerateFunctorSpecializations(compilation, out transformed);
        }

        public bool PostconditionVerification(QsCompilation compilation)
        {
            throw new System.NotImplementedException();
        }
    }
}
