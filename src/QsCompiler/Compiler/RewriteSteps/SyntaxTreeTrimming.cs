// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.SyntaxTreeTrimming;

namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    /// <summary>
    /// Removes unused callables from the syntax tree.
    /// </summary>
    internal class SyntaxTreeTrimming : IRewriteStep
    {
        private readonly bool keepAllIntrinsics;

        public string Name => "Syntax Tree Trimming";

        public int Priority => RewriteStepPriorities.SyntaxTreeTrimming;

        public IDictionary<string, string?> AssemblyConstants { get; } = new Dictionary<string, string?>();

        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => Enumerable.Empty<IRewriteStep.Diagnostic>();

        public bool ImplementsPreconditionVerification => true;

        public bool ImplementsTransformation => true;

        public bool ImplementsPostconditionVerification => false;

        /// <summary>
        /// Constructor for the SyntaxTreeTrimming Rewrite Step.
        /// </summary>
        /// <param name="keepAllIntrinsics">When true, intrinsics will not be removed as part of the rewrite step.</param>
        public SyntaxTreeTrimming(bool keepAllIntrinsics = true)
        {
            this.keepAllIntrinsics = keepAllIntrinsics;
        }

        public bool PreconditionVerification(QsCompilation compilation) => compilation.EntryPoints.Any();

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = TrimSyntaxTree.Apply(compilation, this.keepAllIntrinsics);
            return true;
        }

        public bool PostconditionVerification(QsCompilation compilation)
        {
            throw new System.NotImplementedException();
        }
    }
}
