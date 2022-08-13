// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open Microsoft.Quantum.QsCompiler.Diagnostics
open Xunit

type OpenDirectiveTests() =

    [<Fact>]
    member this.``Conflicting aliases``() =
        WholeFileTests.Expect "ConflictingAliases.qs" [ Error ErrorCode.InvalidNamespaceAliasName ]

    [<Fact>]
    member this.``Duplicate open directives with conflicting aliases``() =
        WholeFileTests.Expect "DuplicateAndConflictingAliases.qs" [ Error ErrorCode.InvalidNamespaceAliasName ]

    [<Fact>]
    member this.``Open with and without alias``() =
        WholeFileTests.Expect "OpenWithAlias.qs" []

    [<Fact>]
    member this.``Duplicated open directives``() =
        WholeFileTests.Expect "DuplicateOpens.qs" []
