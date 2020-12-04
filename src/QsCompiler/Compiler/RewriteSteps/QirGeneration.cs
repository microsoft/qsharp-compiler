// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Quantum.QsCompiler.QIR;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    public class QirGeneration : IRewriteStep
    {
        private readonly string outputFile;

        private readonly List<IRewriteStep.Diagnostic> diagnostics = new List<IRewriteStep.Diagnostic>();

        public QirGeneration(string outputFileName)
        {
            this.outputFile = outputFileName;
        }

        /// <inheritdoc/>
        public string Name => "QIR Generation";

        /// <inheritdoc/>
        public int Priority => 0;

        /// <inheritdoc/>
        public IDictionary<string, string?> AssemblyConstants { get; } = new Dictionary<string, string?>();

        /// <inheritdoc/>
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => this.diagnostics;

        /// <inheritdoc/>
        public bool ImplementsPreconditionVerification => false;

        /// <inheritdoc/>
        public bool ImplementsTransformation => false;

        /// <inheritdoc/>
        public bool ImplementsPostconditionVerification => false;

        /// <inheritdoc/>
        public bool PostconditionVerification(QsCompilation compilation)
        {
            return true;
        }

        /// <inheritdoc/>
        public bool PreconditionVerification(QsCompilation compilation)
        {
            return true;
        }

        /// <inheritdoc/>
        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            var generator = new Generator(compilation, new Configuration());
            generator.Apply();
            generator.Emit(this.outputFile);
            transformed = compilation;
            return true;
        }
    }
}
