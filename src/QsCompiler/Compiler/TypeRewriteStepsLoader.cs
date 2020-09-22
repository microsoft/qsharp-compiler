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
            foreach (var definedStep in config.RewriteStepTypes)
            {
                var loadedStep = this.CreateStep(definedStep.Item1, new Uri(definedStep.Item1.Assembly.Location), definedStep.Item2, onDiagnostic, onException);
                if (loadedStep != null)
                {
                    rewriteSteps.Add(loadedStep);
                }
            }

            return rewriteSteps.ToImmutable();
        }
    }
}
