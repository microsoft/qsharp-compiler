// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Microsoft.Quantum.QsCompilationManager" + SigningConstants.PublicKey)]
[assembly: InternalsVisibleTo("Microsoft.Quantum.QsCompiler" + SigningConstants.PublicKey)]
[assembly: InternalsVisibleTo("Microsoft.Quantum.QsLanguageServer" + SigningConstants.PublicKey)]
[assembly: InternalsVisibleTo("Tests.Microsoft.Quantum.QsCompiler" + SigningConstants.PublicKey)]
