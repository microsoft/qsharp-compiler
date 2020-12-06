// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.IntrinsicResolution;

namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    /// <summary>
    /// Replaces any syntax tree element in the compilation with the one in the environment tree given upon construction.
    /// </summary>
    internal class IntrinsicResolution : IRewriteStep
    {
        public string Name => "Intrinsic Resolution";

        public int Priority => 1200; // currently not used

        public IDictionary<string, string?> AssemblyConstants { get; }

        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => Enumerable.Empty<IRewriteStep.Diagnostic>();

        public bool ImplementsPreconditionVerification => false;

        public bool ImplementsTransformation => true;

        public bool ImplementsPostconditionVerification => false;

        private QsCompilation Environment { get; }

        public IntrinsicResolution(QsCompilation environment)
        {
            this.AssemblyConstants = new Dictionary<string, string?>();
            this.Environment = environment;
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
