// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.IntrinsicResolution;


namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    internal class IntrinsicResolution : IRewriteStep
    {
        public string Name => "Intrinsic Resolution";
        public int Priority => 10; // Not used for built-in transformations like this
        public IDictionary<string, string> AssemblyConstants { get; }
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => null;

        public bool ImplementsPreconditionVerification => false;
        public bool ImplementsTransformation => true;
        public bool ImplementsPostconditionVerification => false;

        private QsCompilation Environment { get; }

        public IntrinsicResolution(QsCompilation environment)
        {
            AssemblyConstants = new Dictionary<string, string>();
            Environment = environment;
        }

        public bool PreconditionVerification(QsCompilation compilation)
        {
            throw new System.NotImplementedException();
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = ReplaceWithTargetIntrinsics.Apply(this.Environment, compilation);
            return true;
        }

        public bool PostconditionVerification(QsCompilation compilation)
        {
            throw new System.NotImplementedException();
        }
    }
}
