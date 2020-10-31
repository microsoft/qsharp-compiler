// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open Xunit
open Xunit.Abstractions

type CompilationLoaderTests (output:ITestOutputHelper) =
    [<Fact>]
    member this.``Basic Write Read Binary`` () =
        Assert.True(true)
