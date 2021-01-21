// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.QsCompiler
{
    /// <summary>
    /// Loads and instantiates all rewrite steps passed into the configuration as types implementing IRewriteStep
    /// </summary>
    internal class TypeRewriteStepsLoader : AbstractRewriteStepsLoader
    {
        public TypeRewriteStepsLoader(Action<Diagnostic>? onDiagnostic = null, Action<Exception>? onException = null)
            : base(onDiagnostic, onException)
        {
        }

        public ImmutableArray<LoadedStep> GetLoadedSteps(IEnumerable<(Type, string?)>? rewriteStepTypes)
        {
            if (rewriteStepTypes == null)
            {
                return ImmutableArray<LoadedStep>.Empty;
            }

            var rewriteSteps = ImmutableArray.CreateBuilder<LoadedStep>();

            foreach (var definedStep in rewriteStepTypes)
            {
                var loadedStep = this.CreateStep(definedStep.Item1, new Uri(definedStep.Item1.Assembly.Location), definedStep.Item2);
                if (loadedStep != null)
                {
                    rewriteSteps.Add(loadedStep);
                }
            }

            return rewriteSteps.ToImmutable();
        }
    }
}
