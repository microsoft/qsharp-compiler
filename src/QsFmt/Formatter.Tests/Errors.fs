// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// Tests for error handling.
module Microsoft.Quantum.QsFmt.Formatter.Tests.Errors

open Microsoft.Quantum.QsFmt.Formatter
open Xunit

[<Fact>]
let ``Returns error result with syntax errors`` () =
    let result =
        Formatter.format None "namespace Foo { invalid syntax; }" |> Result.mapError (List.map string)

    let error =
        "Line 1, Character 16: mismatched input 'invalid' expecting {'function', 'internal', 'newtype', 'open', 'operation', '@', '}'}"

    Assert.Equal(Error [ error ], result)
