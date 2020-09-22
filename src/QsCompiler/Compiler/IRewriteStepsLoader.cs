// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.QsCompiler
{
    internal interface IRewriteStepsLoader
    {
        ImmutableArray<LoadedStep> GetLoadedSteps(CompilationLoader.Configuration config,
            Action<Diagnostic> onDiagnostic = null,
            Action<Exception> onException = null);
    }
}
