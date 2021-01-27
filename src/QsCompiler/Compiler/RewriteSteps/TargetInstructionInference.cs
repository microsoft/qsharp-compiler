// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Targeting;

namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    /// <summary>
    /// Creates a separate callable for each intrinsic specialization.
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
    internal class TargetInstructionInference : IRewriteStep
    {
        private readonly List<IRewriteStep.Diagnostic> diagnostics = new List<IRewriteStep.Diagnostic>();

        public TargetInstructionInference()
        {
            this.diagnostics = new List<IRewriteStep.Diagnostic>();
            this.AssemblyConstants = new Dictionary<string, string?>();
        }

        /// <inheritdoc/>
        public string Name => "Target Instruction Separation";

        /// <inheritdoc/>
        public int Priority => RewriteStepPriorities.TargetInstructionSeparation;

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
            var attributes = compilation.Namespaces.Attributes().Select(att => att.FullName).ToImmutableHashSet();
            return attributes.Contains(BuiltIn.TargetInstruction.FullName)
                && attributes.Contains(BuiltIn.Inline.FullName);
        }

        /// <inheritdoc/>
        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = InferTargetInstructions.ReplaceSelfAdjointSpecializations(compilation);
            transformed = InferTargetInstructions.LiftIntrinsicSpecializations(transformed);
            var allAttributesAdded = InferTargetInstructions.TryAddMissingTargetInstructionAttributes(transformed, out transformed);
            if (!allAttributesAdded)
            {
                this.diagnostics.Add(new IRewriteStep.Diagnostic
                {
                    Severity = DiagnosticSeverity.Warning,
                    Message = DiagnosticItem.Message(WarningCode.MissingTargetInstructionName, Array.Empty<string>()),
                    Stage = IRewriteStep.Stage.Transformation,
                });
            }
            return true;
        }

        /// <inheritdoc/>
        public bool PostconditionVerification(QsCompilation compilation) =>
            throw new NotImplementedException();
    }
}
