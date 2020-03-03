// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.IntrinsicResolution;


namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    internal class IntrinsicResolution : IRewriteStep
    {
        public string Name { get; }
        public int Priority { get; }
        public IDictionary<string, string> AssemblyConstants { get; }
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics { get; }

        public bool ImplementsTransformation { get; }
        public bool ImplementsPreconditionVerification { get; }
        public bool ImplementsPostconditionVerification { get; }

        private QsCompilation Environment { get; }

        public IntrinsicResolution(QsCompilation environment)
        {
            Name = "IntrinsicResolution";
            Priority = 10; // Not used for built-in transformations like this
            AssemblyConstants = new Dictionary<string, string>();
            ImplementsTransformation = true;
            ImplementsPreconditionVerification = false;
            ImplementsPostconditionVerification = false;

            Environment = environment;
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = ReplaceWithTargetIntrinsics.Apply(this.Environment, compilation);
            return true;
        }

        public bool PreconditionVerification(QsCompilation compilation)
        {
            throw new System.NotImplementedException();
        }

        public bool PostconditionVerification(QsCompilation compilation)
        {
            throw new System.NotImplementedException();
        }
    }
}
