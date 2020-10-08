// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    /// <summary>
    /// Inlines all conjugations, eliminating that construct from the syntax tree.
    /// </summary>
    internal class ConjugationInlining : IRewriteStep
    {
        public string Name => "Conjugation Inlining";

        public int Priority => RewriteStepPriorities.InliningOfConjugations;

        public IDictionary<string, string?> AssemblyConstants { get; }

        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => Enumerable.Empty<IRewriteStep.Diagnostic>();

        public bool ImplementsPreconditionVerification => false;

        public bool ImplementsTransformation => true;

        public bool ImplementsPostconditionVerification => false;

        public ConjugationInlining()
        {
            this.AssemblyConstants = new Dictionary<string, string?>();
        }

        public bool PreconditionVerification(QsCompilation compilation)
        {
            throw new System.NotImplementedException();
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            return compilation.InlineConjugations(out transformed);
        }

        public bool PostconditionVerification(QsCompilation compilation)
        {
            throw new System.NotImplementedException();
        }
    }
}
