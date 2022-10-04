// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using static Microsoft.Quantum.QsCompiler.ReservedKeywords.AssemblyConstants;

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
            var targeting = compilation.Namespaces.FirstOrDefault(ns => ns.Name == BuiltIn.TargetingNamespace);
            var requiresCapability = targeting?.Elements.FirstOrDefault(e =>
                e.GetFullName().Equals(BuiltIn.RequiresCapability.FullName));

            return requiresCapability is not null && !this.AssemblyConstants.ContainsKey(ExecutionTarget);
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = Capabilities.Infer(compilation);
            return true;
        }

        public bool PostconditionVerification(QsCompilation compilation) => true;
    }
}
