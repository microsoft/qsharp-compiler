// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Quantum.QsCompiler.SyntaxTree;


namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    internal class FullPreEvaluation : IRewriteStep
    {
        public string Name => "Full Pre-Evaluation";
        public int Priority => 10; // Not used for built-in transformations like this
        public IDictionary<string, string> AssemblyConstants { get; }
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => null;

        public bool ImplementsPreconditionVerification => false;
        public bool ImplementsTransformation => true;
        public bool ImplementsPostconditionVerification => false;

        public FullPreEvaluation()
        {
            AssemblyConstants = new Dictionary<string, string>();
        }

        public bool PreconditionVerification(QsCompilation compilation)
        {
            throw new System.NotImplementedException();
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            return compilation.PreEvaluateAll(out transformed);
        }

        public bool PostconditionVerification(QsCompilation compilation)
        {
            throw new System.NotImplementedException();
        }
    }
}
