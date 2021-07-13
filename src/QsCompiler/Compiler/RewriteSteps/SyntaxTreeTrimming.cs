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
        private readonly bool isLibrary;
        private readonly IEnumerable<QsQualifiedName>? dependencies;

        public string Name => "Syntax Tree Trimming";

        public int Priority => RewriteStepPriorities.SyntaxTreeTrimming;

        public IDictionary<string, string?> AssemblyConstants { get; } = new Dictionary<string, string?>();

        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => Enumerable.Empty<IRewriteStep.Diagnostic>();

        public bool ImplementsPreconditionVerification => true;

        public bool ImplementsTransformation => true;

        public bool ImplementsPostconditionVerification => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxTreeTrimming"/> class.
        /// </summary>
        /// <param name="keepAllIntrinsics">When true, intrinsics will not be removed as part of the rewrite step.</param>
        /// <param name="isLibrary">When true, trimming will consider every public, non-generic callabe as an entry point.</param>
        public SyntaxTreeTrimming(bool keepAllIntrinsics = true, IEnumerable<QsQualifiedName>? dependencies = null, bool isLibrary = false)
        {
            this.keepAllIntrinsics = keepAllIntrinsics;
            this.dependencies = dependencies;
            this.isLibrary = isLibrary;
        }

        public bool PreconditionVerification(QsCompilation compilation) => compilation.EntryPoints.Any() || this.isLibrary;

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = TrimSyntaxTree.Apply(compilation, this.keepAllIntrinsics, this.dependencies, this.isLibrary);
            return true;
        }

        public bool PostconditionVerification(QsCompilation compilation)
        {
            throw new System.NotImplementedException();
        }
    }
}
