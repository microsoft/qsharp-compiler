// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using static Microsoft.Quantum.QsCompiler.ReservedKeywords.AssemblyConstants;
using static Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference;

namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    /// <summary>
    /// Infers the minimum runtime capabilities required by each callable.
    /// </summary>
    internal class CapabilityInference : IRewriteStep
    {
        public string Name => "Capability Inference";

        public int Priority => RewriteStepPriorities.CapabilityInference;

        public IDictionary<string, string?> AssemblyConstants { get; } = new Dictionary<string, string?>();

        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => Enumerable.Empty<IRewriteStep.Diagnostic>();

        public bool ImplementsPreconditionVerification => true;

        public bool ImplementsTransformation => true;

        public bool ImplementsPostconditionVerification => false;

        public bool PreconditionVerification(QsCompilation compilation)
        {
            var targetingNamespace = compilation.Namespaces.FirstOrDefault(ns =>
                ns.Name.Equals(BuiltIn.TargetingNamespace));
            var hasCapabilityAttribute = targetingNamespace?.Elements.Any(element =>
                element.GetFullName().Equals(BuiltIn.RequiresCapability.FullName)) ?? false;
            return hasCapabilityAttribute && !this.AssemblyConstants.ContainsKey(ExecutionTarget);
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = InferCapabilities(compilation);
            return true;
        }

        public bool PostconditionVerification(QsCompilation compilation) => throw new NotSupportedException();
    }
}
