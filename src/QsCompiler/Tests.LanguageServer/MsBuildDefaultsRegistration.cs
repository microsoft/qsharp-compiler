// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Build.Locator;

namespace Microsoft.Quantum.QsLanguageServer.Testing
{
    internal static class MsBuildDefaults
    {
        public static Lazy<VisualStudioInstance> LazyRegistration { get; }
            = new Lazy<VisualStudioInstance>(MSBuildLocator.RegisterDefaults);
    }
}
