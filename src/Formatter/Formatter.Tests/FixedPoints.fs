// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// <summary>
/// <see cref="QsFmt.Formatter.Tests.FixedPoint"/> test cases.
/// </summary>
module QsFmt.Formatter.Tests.FixedPoints

open QsFmt.Formatter.Tests

[<FixedPoint>]
let ``Namespace comments`` = """/// The Foo namespace.
namespace Foo {}

/// The Bar namespace.
namespace Bar {}

// End of file."""

[<FixedPoint>]
let ``Function with one parameter`` = """namespace Foo {
    function Bar(x : Int) : Int {
        return x;
    }
}"""

[<FixedPoint>]
let ``Function with two parameters`` = """namespace Foo {
    function Bar(x : Int, y : Int) : Int {
        return x + y;
    }
}"""

[<FixedPoint(Skip = "Not supported.")>]
let ``Entry point and using statement`` = """namespace Microsoft.Quantum.Foo {
    @EntryPoint()
    operation RunProgram (nQubits : Int) : Unit {
        using (register = Qubit[nQubits]) {
            H(register[0]);
        }
    }
}"""

[<FixedPoint>]
let ``Open directives and operation`` = """namespace Foo {
    open Bar;
    open Baz;

    operation Spam () : Unit {}
}"""

[<FixedPoint(Skip = "Not supported.")>]
let ``Open directive and entry point`` = """namespace Foo {
    open Test;

    @EntryPoint()
    operation Bar () : Unit {}
}"""

[<FixedPoint(Skip = "Not supported.")>]
let ``Operation with comments`` = """namespace Foo {
    open Test;

    operation Bar () : Unit {
        // This is a comment
        Message("Bar");
        // Lorem ipsum
        // Dolor sit amet
    }
}"""

[<FixedPoint>]
let ``Mutable variable`` = """namespace Foo {
    function Bar() : Unit {
        mutable x = 0;
    }
}"""

[<FixedPoint>]
let ``Mutable variable after comment`` = """namespace Foo {
    function Bar() : Unit {
        // Hello world
        mutable x = 0;
    }
}"""

[<FixedPoint(Skip = "Not supported.")>]
let ``Mutable variable before comment`` = """namespace Foo {
    function Bar() : Unit {
        mutable x = 0;
        // Hello world
    }
}"""

[<FixedPoint>]
let ``Array literal`` = """namespace Foo {
    function Bar() : Unit {
        let xs = [1, 2, 3];
    }
}"""
