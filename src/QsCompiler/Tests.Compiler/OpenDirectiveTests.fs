// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open Microsoft.Quantum.QsCompiler.Diagnostics
open Xunit

type OpenDirectiveTests() =
    inherit WholeFileTests()

    [<Fact>]
    member this.``Conflicting aliases``() =
        this.Expect "ConflictingAliases.qs" [ Error ErrorCode.InvalidNamespaceAliasName ]

    [<Fact>]
    member this.``Duplicate open directives with conflicting aliases``() =
        this.Expect "DuplicateAndConflictingAliases.qs" [ Error ErrorCode.InvalidNamespaceAliasName ]

    [<Fact>]
    member this.``Open with and without alias``() = this.Expect "OpenWithAlias.qs" []

    [<Fact>]
    member this.``Duplicated open directives``() = this.Expect "DuplicateOpens.qs" []
