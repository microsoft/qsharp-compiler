// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Quantum.QsCompiler.SyntaxTree;


namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    internal class FullPreEvaluation : IRewriteStep
    {
        public string Name { get; }
        public int Priority { get; }
        public IDictionary<string, string> AssemblyConstants { get; }
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics { get; }

        public bool ImplementsTransformation { get; }
        public bool ImplementsPreconditionVerification { get; }
        public bool ImplementsPostconditionVerification { get; }

        public FullPreEvaluation()
        {
            Name = "Full Pre-Evaluation";
            Priority = 10; // Not used for built-in transformations like this
            AssemblyConstants = new Dictionary<string, string>();
            ImplementsTransformation = true;
            ImplementsPreconditionVerification = false;
            ImplementsPostconditionVerification = false;
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            return compilation.PreEvaluateAll(out transformed);
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
