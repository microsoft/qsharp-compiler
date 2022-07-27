// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.QIR;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.SyntaxTreeTrimming;

namespace Microsoft.Quantum.QsCompiler
{
    public class QirGeneration : IRewriteStep
    {
        internal const int EmissionPriority = -10;

        private readonly List<IRewriteStep.Diagnostic> diagnostics = new();

        public QirGeneration() =>
            this.AssemblyConstants = new Dictionary<string, string?>();

        /// <inheritdoc/>
        public string Name => "QIR Generation";

        /// <inheritdoc/>
        public int Priority => EmissionPriority;

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
        public bool PreconditionVerification(QsCompilation compilation) =>
            CompilationSteps.VerifyGenerationPrecondition(compilation, this.diagnostics);

        /// <inheritdoc/>
        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            using var generator = CompilationSteps.CreateAndPopulateGenerator(compilation, this.AssemblyConstants);

            // write generated QIR to disk
            var assemblyName = this.AssemblyConstants.TryGetValue(ReservedKeywords.AssemblyConstants.AssemblyName, out var asmName) ? asmName : null;
            var targetFile = Path.GetFullPath(string.IsNullOrWhiteSpace(assemblyName) ? "main.txt" : $"{Path.GetFileName(assemblyName)}.txt");

            PerformanceTracking.TaskStart(PerformanceTracking.Task.BitcodeGeneration);
            var bcOutputFolder = this.AssemblyConstants.TryGetValue(ReservedKeywords.AssemblyConstants.OutputPath, out var path) && !string.IsNullOrWhiteSpace(path) ? path : "qir";
            var bcFile = CompilationLoader.GeneratedFile(targetFile, Path.GetFullPath(bcOutputFolder), ".bc", "");
            generator.Emit(bcFile, emitBitcode: true);
            PerformanceTracking.TaskEnd(PerformanceTracking.Task.BitcodeGeneration);

            if (this.AssemblyConstants.TryGetValue(ReservedKeywords.AssemblyConstants.QirOutputPath, out path) && !string.IsNullOrWhiteSpace(path))
            {
                // create the human readable version as well
                var llvmSourceFile = CompilationLoader.GeneratedFile(targetFile, Path.GetFullPath(path), ".ll", "");
                generator.Emit(llvmSourceFile, emitBitcode: false);
            }

            transformed = compilation;
            return true;
        }

        /// <inheritdoc/>
        public bool PostconditionVerification(QsCompilation compilation) =>
            throw new NotImplementedException();
    }

    /// <summary>
    /// First prunes unused intrinsics using <see cref="TrimSyntaxTree" /> and then
    /// creates a separate callable for each intrinsic specialization.
    /// Adds a TargetInstruction attribute to each intrinsic callable that doesn't have one,
    /// unless the automatically determined target instruction name conflicts with another target instruction name.
    /// The automatically determined name of the target instruction is the lower case version of the unqualified callable name.
    /// Generates a warning without failing the transformation if some attributes could not be added.
    /// Leaves any type parameterized callables and type constructors unmodified.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// An intrinsic callable contains non-intrinsic specializations
    /// or a non-intrinsic callable contains intrinsic specializations,
    /// or the a callable doesn't have a body specialization.
    /// </exception>
    public class TargetInstructionInference : IRewriteStep
    {
        private readonly List<IRewriteStep.Diagnostic> diagnostics = new();

        public TargetInstructionInference() =>
            this.AssemblyConstants = new Dictionary<string, string?>();

        /// <inheritdoc/>
        public string Name => "Target Instruction Separation";

        /// <inheritdoc/>
        public int Priority => QirGeneration.EmissionPriority + 1;

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
        public bool PreconditionVerification(QsCompilation compilation) =>
            CompilationSteps.VerifyTargetInstructionInferencePrecondition(compilation);

        /// <inheritdoc/>
        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = CompilationSteps.RunTargetInstructionInference(compilation, this.diagnostics);
            return true;
        }

        /// <inheritdoc/>
        public bool PostconditionVerification(QsCompilation compilation) =>
            throw new NotImplementedException();
    }
}
