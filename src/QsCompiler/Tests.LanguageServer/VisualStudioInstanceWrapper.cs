// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Build.Locator;

namespace Microsoft.Quantum.QsLanguageServer.Testing
{
    internal static class VisualStudioInstanceWrapper
    {
        public static Lazy<VisualStudioInstance> LazyVisualStudioInstance { get; }
            = new Lazy<VisualStudioInstance>(MSBuildLocator.RegisterDefaults);
    }
}
