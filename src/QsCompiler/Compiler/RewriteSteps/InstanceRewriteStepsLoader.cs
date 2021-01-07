// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.QsCompiler
{
    /// <summary>
    /// Loads all rewrite steps passed into the configuration as instances of objects implementing IRewriteStep
    /// </summary>
    internal class InstanceRewriteStepsLoader : AbstractRewriteStepsLoader
    {
        public InstanceRewriteStepsLoader(Action<Diagnostic>? onDiagnostic = null, Action<Exception>? onException = null)
            : base(onDiagnostic, onException)
        {
        }

        public ImmutableArray<LoadedStep> GetLoadedSteps(IEnumerable<(IRewriteStep, string?)>? rewriteStepInstances)
        {
            if (rewriteStepInstances == null)
            {
                return ImmutableArray<LoadedStep>.Empty;
            }

            var rewriteSteps = ImmutableArray.CreateBuilder<LoadedStep>();

            // add steps specified in the config as IRewriteStep instances
            foreach (var step in rewriteStepInstances)
            {
                rewriteSteps.Add(new LoadedStep(step.Item1, step.Item2));
            }

            return rewriteSteps.ToImmutable();
        }
    }
}
