// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Monomorphization;
using Microsoft.Quantum.QsCompiler.Transformations.Monomorphization.Validation;


namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    internal class Monomorphization : IRewriteStep
    {
        public string Name => "Monomorphization";
        public int Priority => 10; // Not used for built-in transformations like this
        public IDictionary<string, string> AssemblyConstants { get; }
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => null;

        public bool ImplementsPreconditionVerification => true;
        public bool ImplementsTransformation => true;
        public bool ImplementsPostconditionVerification => true;

        public Monomorphization()
        {
            AssemblyConstants = new Dictionary<string, string>();
        }

        public bool PreconditionVerification(QsCompilation compilation) =>
            compilation != null && compilation.EntryPoints.Any();

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = Monomorphize.Apply(compilation);
            return true;
        }

        public bool PostconditionVerification(QsCompilation compilation)
        {
            try { ValidateMonomorphization.Apply(compilation); }
            catch { return false; }
            return true;
        }
    }
}
