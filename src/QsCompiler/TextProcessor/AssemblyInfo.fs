// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace QsLanguageProcessor.AssemblyInfo

open System.Runtime.CompilerServices

// Allow the test assembly to use our internal methods
[<assembly: InternalsVisibleTo("Tests.Microsoft.Quantum.QsCompiler" + SigningConstants.PUBLIC_KEY)>]

do ()
