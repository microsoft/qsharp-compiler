// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.QIR;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Monomorphization.Validation;

namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    internal class QirGeneration : IRewriteStep
    {
        private Generator? generator;
        private readonly List<IRewriteStep.Diagnostic> diagnostics;

        public QirGeneration()
        {
            this.diagnostics = new List<IRewriteStep.Diagnostic>();
            this.AssemblyConstants = new Dictionary<string, string?>();
        }

        /// <inheritdoc/>
        public string Name => "QIR Generation";

        /// <inheritdoc/>
        public int Priority => -10; // currently not used

        /// <inheritdoc/>
        public IDictionary<string, string?> AssemblyConstants { get; }

        /// <inheritdoc/>
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => this.diagnostics;

        /// <inheritdoc/>
        public bool ImplementsPreconditionVerification => true;

        /// <inheritdoc/>
        public bool ImplementsTransformation => true;

        /// <inheritdoc/>
        public bool ImplementsPostconditionVerification => false;

        /// <inheritdoc/>
        public bool PreconditionVerification(QsCompilation compilation)
        {
            try
            {
                ValidateMonomorphization.Apply(compilation);
                return true;
            }
            catch
            {
                this.diagnostics.Add(new IRewriteStep.Diagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Stage = IRewriteStep.Stage.PreconditionVerification,
                    Message = DiagnosticItem.Message(ErrorCode.SyntaxTreeNotMonomorphized, Array.Empty<string>())
                });
                return false;
            }
        }

        /// <inheritdoc/>
        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = compilation;
            this.generator = new Generator(transformed);
            this.generator.Apply();
            return true;
        }

        /// <inheritdoc/>
        public bool PostconditionVerification(QsCompilation compilation) =>
            throw new NotImplementedException();

        /// <summary>
        /// Writes the generated QIR to an output file.
        /// Does nothing if the transformation has not yet run.
        /// </summary>
        /// <param name="fileName">The file to which the output is written.</param>
        /// <param name="emitBitcode">False if the file should be human readable, true if the file should contain bitcode.</param>
        public void Emit(string fileName, bool emitBitcode) =>
            this.generator?.Emit(fileName, emitBitcode: emitBitcode);
    }
}
