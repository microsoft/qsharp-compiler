// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace QsCompiler.AssemblyInfo

open System.Runtime.CompilerServices

[<assembly: InternalsVisibleTo("Tests.Microsoft.Quantum.QsCompiler" + SigningConstants.PUBLIC_KEY)>]
[<assembly: AutoOpenAttribute("Microsoft.Quantum.QsCompiler.SyntaxTreeExtensions")>]

do ()
