// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Monomorphization;
using Microsoft.Quantum.QsCompiler.Transformations.Monomorphization.Validation;


namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    internal class FunctorGeneration : IRewriteStep
    {
        public string Name { get; }
        public int Priority { get; }
        public IDictionary<string, string> AssemblyConstants { get; }
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics { get; }

        public bool ImplementsTransformation { get; }
        public bool ImplementsPreconditionVerification { get; }
        public bool ImplementsPostconditionVerification { get; }

        public FunctorGeneration()
        {
            Name = "Functor Generation";
            Priority = 10; // Not used for built-in transformations like this
            AssemblyConstants = new Dictionary<string, string>();
            ImplementsTransformation = true;
            ImplementsPreconditionVerification = true;
            ImplementsPostconditionVerification = false;
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            return CodeGeneration.GenerateFunctorSpecializations(compilation, out transformed);
        }

        public bool PreconditionVerification(QsCompilation compilation)
        {
            var requiredNamespace = compilation.Namespaces
                .FirstOrDefault(ns => ns.Name.Equals(BuiltIn.CoreNamespace));

            if (requiredNamespace == null)
            {
                return false;
            }

            var providedOperations = new QsNamespace[] { requiredNamespace }.Callables().Select(c => c.FullName);
            var requiredBuiltIns = new List<QsQualifiedName>()
            {
                BuiltIn.Length.FullName,
                BuiltIn.RangeReverse.FullName
            };

            return requiredBuiltIns.All(builtIn => providedOperations.Any(provided => provided.Equals(builtIn)));
        }

        public bool PostconditionVerification(QsCompilation compilation)
        {
            throw new System.NotImplementedException();
        }
    }
}
