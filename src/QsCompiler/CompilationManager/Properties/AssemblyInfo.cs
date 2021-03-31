// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

// Allow the test assembly to use our internal methods
[assembly: InternalsVisibleTo("Tests.Microsoft.Quantum.QsCompiler" + SigningConstants.PublicKey)]
[assembly: InternalsVisibleTo("Tests.Microsoft.Quantum.QsLanguageServer" + SigningConstants.PublicKey)]
