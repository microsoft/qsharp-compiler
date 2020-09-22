// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.QsCompiler
{
    internal class TypeRewriteStepsLoader : AbstractRewriteStepsLoader
    {
        public override ImmutableArray<LoadedStep> GetLoadedSteps(CompilationLoader.Configuration config, Action<Diagnostic> onDiagnostic = null, Action<Exception> onException = null)
        {
            if (config.RewriteStepTypes == null)
            {
                return ImmutableArray<LoadedStep>.Empty;
            }

            var rewriteSteps = ImmutableArray.CreateBuilder<LoadedStep>();

            // add steps specified in the config as IRewriteStep types
            foreach (var step in config.RewriteStepTypes)
            {
                rewriteSteps.Add(this.CreateStep(step.Item1, new Uri(step.Item1.Assembly.Location), step.Item2, onDiagnostic, onException));
            }

            return rewriteSteps.ToImmutable();
        }
    }
}
