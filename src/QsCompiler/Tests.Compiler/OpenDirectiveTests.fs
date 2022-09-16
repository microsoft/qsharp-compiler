// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.OpenDirectiveTests

open Microsoft.Quantum.QsCompiler.Diagnostics
open Xunit
open System.IO

let private folder = Path.Join("TestCases", "WholeFileTests")

[<Fact>]
let ``Conflicting aliases`` () =
    let compilation = TestUtils.buildFiles folder [ "ConflictingAliases.qs" ] [] None TestUtils.Library
    Diagnostics.assertMatches [ Error ErrorCode.InvalidNamespaceAliasName, None ] (compilation.Diagnostics())

[<Fact>]
let ``Duplicate open directives with conflicting aliases`` () =
    let compilation =
        TestUtils.buildFiles folder [ "DuplicateAndConflictingAliases.qs" ] [] None TestUtils.Library

    Diagnostics.assertMatches [ Error ErrorCode.InvalidNamespaceAliasName, None ] (compilation.Diagnostics())

[<Fact>]
let ``Open with and without alias`` () =
    let compilation = TestUtils.buildFiles folder [ "OpenWithAlias.qs" ] [] None TestUtils.Library
    Assert.Empty(compilation.Diagnostics())

[<Fact>]
let ``Duplicated open directives`` () =
    let compilation = TestUtils.buildFiles folder [ "DuplicateOpens.qs" ] [] None TestUtils.Library
    Assert.Empty(compilation.Diagnostics())
