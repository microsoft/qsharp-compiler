// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace QsCompiler.AssemblyInfo

open System.Runtime.CompilerServices

[<assembly:InternalsVisibleTo("Microsoft.Quantum.QsCore"
                              + SigningConstants.PUBLIC_KEY)>]
[<assembly:InternalsVisibleTo("Tests.Microsoft.Quantum.QsCompiler"
                              + SigningConstants.PUBLIC_KEY)>]

do ()
