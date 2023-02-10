// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations;
using Microsoft.Quantum.QsCompiler.Transformations.Monomorphization.Validation;
using Microsoft.Quantum.QsCompiler.Transformations.SyntaxTreeTrimming;
using Microsoft.Quantum.QsCompiler.Transformations.Targeting;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    public static class CompilationSteps
    {
        internal static bool VerifyTargetInstructionInferencePrecondition(QsCompilation compilation)
        {
            var attributes = compilation.Namespaces.Attributes().Select(att => att.FullName).ToImmutableHashSet();
            return attributes.Contains(BuiltIn.TargetInstruction.FullName)
                && attributes.Contains(BuiltIn.Inline.FullName);
        }

        internal static bool VerifyGenerationPrecondition(QsCompilation compilation, List<IRewriteStep.Diagnostic> diagnostics)
        {
            try
            {
                ValidateMonomorphization.Apply(compilation, allowTypeParametersForIntrinsics: true);
                return true;
            }
            catch
            {
                diagnostics.Add(new IRewriteStep.Diagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Stage = IRewriteStep.Stage.PreconditionVerification,
                    Message = DiagnosticItem.Message(ErrorCode.SyntaxTreeNotMonomorphized, Array.Empty<string>()),
                    Source = Assembly.GetExecutingAssembly().Location,
                });
                return false;
            }
        }

        internal static Generator CreateAndPopulateGenerator(QsCompilation compilation, TargetCapability? targetCapability)
        {
            compilation =
                targetCapability == TargetCapabilityModule.BasicExecution ||
                targetCapability == TargetCapabilityModule.AdaptiveExecution
                ? AddOutputRecording.Apply(compilation, useRuntimeAPI: true, mainSuffix: NameGeneration.MainSuffix, alwaysCreateWrapper: true)
                : compilation;
            var generator = new Generator(compilation, capability: targetCapability);
            generator.Apply();
            return generator;
        }

        internal static QsCompilation RunTargetInstructionInference(QsCompilation compilation, List<IRewriteStep.Diagnostic> diagnostics)
        {
            var transformed = TrimSyntaxTree.Apply(compilation, keepAllIntrinsics: false);
            transformed = InferTargetInstructions.ReplaceSelfAdjointSpecializations(transformed);
            transformed = InferTargetInstructions.LiftIntrinsicSpecializations(transformed);
            var allAttributesAdded = InferTargetInstructions.TryAddMissingTargetInstructionAttributes(transformed, out transformed);
            if (!allAttributesAdded)
            {
                diagnostics.Add(new IRewriteStep.Diagnostic
                {
                    Severity = DiagnosticSeverity.Warning,
                    Message = DiagnosticItem.Message(WarningCode.MissingTargetInstructionName, Array.Empty<string>()),
                    Stage = IRewriteStep.Stage.Transformation,
                });
            }

            return transformed;
        }

        private static bool ValidateAndEmit(QsCompilation compilation, TargetCapability? targetCapability, string fileName, bool emitBitcode, bool overwrite = true, List<IRewriteStep.Diagnostic>? diagnostics = null)
        {
            diagnostics ??= new List<IRewriteStep.Diagnostic>();
            var preconditionSatisfied =
                VerifyTargetInstructionInferencePrecondition(compilation) &&
                VerifyGenerationPrecondition(compilation, diagnostics);

            if (preconditionSatisfied)
            {
                compilation = RunTargetInstructionInference(compilation, diagnostics);
                using var generator = CreateAndPopulateGenerator(compilation, targetCapability);

                var fileCreated = generator.Emit(fileName, emitBitcode, overwrite);
                var isValid = generator.Verify(out string validationErrors);

                if (!isValid)
                {
                    diagnostics.Add(new IRewriteStep.Diagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        Message = $"Failed to create QIR output.\n{validationErrors}",
                        Stage = IRewriteStep.Stage.PostconditionVerification,
                    });
                }

                return fileCreated && isValid;
            }

            return false;
        }

        /// <summary>
        /// Creates a file with the given name that contains the QIR bitcode for the given compilation.
        /// </summary>
        /// <param name="compilation">The Q# compilation to compile to bitcode.</param>
        /// <param name="targetCapability">The set of features that are supported by the targeted backend. This is relevant for certain kinds of optimizations.</param>
        /// <param name="fileName">The name of the file where the bitcode should be written to.</param>
        /// <param name="overwrite">Whether or not to overwrite a file if it already exists.</param>
        /// <param name="diagnostics">A list to store any diagnostics generated in the process.</param>
        /// <returns>True if the preconditions for compilation to QIR were satisfied and the file has been created successfully, and false otherwise.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if a file with the given name already exists, but <paramref name="overwrite"/> has been set to false.
        /// </exception>
        public static bool GenerateBitcode(QsCompilation compilation, TargetCapability? targetCapability, string fileName, bool overwrite = true, List<IRewriteStep.Diagnostic>? diagnostics = null)
        {
            return ValidateAndEmit(compilation, targetCapability, fileName, emitBitcode: true, overwrite, diagnostics);
        }

        /// <summary>
        /// Creates a file with the given name that contains the human readable QIR for the given compilation.
        /// </summary>
        /// <param name="compilation">The Q# compilation to compile to bitcode.</param>
        /// <param name="targetCapability">The set of features that are supported by the targeted backend. This is relevant for certain kinds of optimizations.</param>
        /// <param name="fileName">The name of the file where the bitcode should be written to.</param>
        /// <param name="overwrite">Whether or not to overwrite a file if it already exists.</param>
        /// <param name="diagnostics">A list to store any diagnostics generated in the process.</param>
        /// <returns>True if the preconditions for compilation to QIR were satisfied and the file has been created successfully, and false otherwise.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if a file with the given name already exists, but <paramref name="overwrite"/> has been set to false.
        /// </exception>
        public static bool GenerateLlvmIR(QsCompilation compilation, TargetCapability? targetCapability, string fileName, bool overwrite = true, List<IRewriteStep.Diagnostic>? diagnostics = null)
        {
            return ValidateAndEmit(compilation, targetCapability, fileName, emitBitcode: false, overwrite, diagnostics);
        }
    }
}
