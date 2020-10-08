// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    /// <summary>
    /// Evaluates classical computations as much as possible.
    /// </summary>
    internal class FullPreEvaluation : IRewriteStep
    {
        public string Name => "Full Pre-Evaluation";

        public int Priority => RewriteStepPriorities.EvaluationOfClassicalComputations;

        public IDictionary<string, string?> AssemblyConstants { get; }

        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => Enumerable.Empty<IRewriteStep.Diagnostic>();

        public bool ImplementsPreconditionVerification => false;

        public bool ImplementsTransformation => true;

        public bool ImplementsPostconditionVerification => false;

        public FullPreEvaluation()
        {
            this.AssemblyConstants = new Dictionary<string, string?>();
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
