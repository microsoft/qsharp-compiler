// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.QsCompiler
{
    internal class InstanceRewriteStepsLoader : AbstractRewriteStepsLoader
    {
        public override ImmutableArray<LoadedStep> GetLoadedSteps(CompilationLoader.Configuration config, Action<Diagnostic> onDiagnostic = null, Action<Exception> onException = null)
        {
            if (config.RewriteStepInstances == null)
            {
                return ImmutableArray<LoadedStep>.Empty;
            }

            var rewriteSteps = ImmutableArray.CreateBuilder<LoadedStep>();

            // add steps specified in the config as IRewriteStep instances
            foreach (var step in config.RewriteStepInstances)
            {
                rewriteSteps.Add(new LoadedStep(step.Item1, step.Item2));
            }

            return rewriteSteps.ToImmutable();
        }
    }
}
