// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.MonomorphizationTransformation;
using Microsoft.Quantum.QsCompiler.Transformations.MonomorphizationValidationTransformation;


namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    internal class Monomorphization : IRewriteStep
    {
        public string Name { get; }
        public int Priority { get; }
        public IDictionary<string, string> AssemblyConstants { get; }
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics { get; }

        public bool ImplementsTransformation { get; }
        public bool ImplementsPreconditionVerification { get; }
        public bool ImplementsPostconditionVerification { get; }

        public Monomorphization()
        {
            Name = "Monomorphization";
            Priority = 10; // Not used for built-in transformations like this
            AssemblyConstants = new Dictionary<string, string>();
            ImplementsTransformation = true;
            ImplementsPreconditionVerification = true;
            ImplementsPostconditionVerification = true;
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = MonomorphizationTransformation.Apply(compilation);
            return true;
        }

        public bool PreconditionVerification(QsCompilation compilation) =>
            compilation != null && compilation.EntryPoints.Any();

        public bool PostconditionVerification(QsCompilation compilation)
        {
            try { MonomorphizationValidationTransformation.Apply(compilation); }
            catch { return false; }
            return true;
        }
    }
}
