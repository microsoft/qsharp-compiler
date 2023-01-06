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

        private static bool ValidateAndEmit(QsCompilation compilation, TargetCapability? targetCapability, Func<Generator, Action<string>, bool> emit, List<IRewriteStep.Diagnostic>? diagnostics = null)
        {
            diagnostics ??= new List<IRewriteStep.Diagnostic>();
            var preconditionSatisfied =
                VerifyTargetInstructionInferencePrecondition(compilation) &&
                VerifyGenerationPrecondition(compilation, diagnostics);

            void GenerateError(string msg) =>
                diagnostics.Add(new IRewriteStep.Diagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Message = $"Failed to create QIR output.\n{msg}",
                    Stage = IRewriteStep.Stage.PostconditionVerification,
                });

            if (preconditionSatisfied)
            {
                compilation = RunTargetInstructionInference(compilation, diagnostics);
                using var generator = CreateAndPopulateGenerator(compilation, targetCapability);
                return emit(generator, GenerateError);
            }
            else
            {
                return false;
            }
        }
    }
}
