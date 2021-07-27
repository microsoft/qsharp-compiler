// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// <summary>
/// <see cref="QsFmt.Formatter.Tests.Example"/> test cases.
/// </summary>
module Microsoft.Quantum.QsFmt.Formatter.Tests.Examples

[<FormatExample>]
let ``Indents function`` =
    """namespace Foo {
function Bar() : Int {
let x = 5;
return x;
}
}""",

    """namespace Foo {
    function Bar() : Int {
        let x = 5;
        return x;
    }
}"""

[<FormatExample>]
let ``Indents if-else statement`` =
    """namespace Foo {
function Bar() : Int {
let x = 5;
if (x == 5) {
return x;
} else {
return x + 1;
}
}
}""",

    """namespace Foo {
    function Bar() : Int {
        let x = 5;
        if (x == 5) {
            return x;
        } else {
            return x + 1;
        }
    }
}"""

[<FormatExample>]
let ``Adds newlines and indents`` =
    """namespace Foo { function Bar() : Int { let x = 5; return x; } }""",

    """namespace Foo {
    function Bar() : Int {
        let x = 5;
        return x;
    }
}"""

[<FormatExample>]
let ``Removes extraneous spaces`` =
    """namespace     Foo {
    function Bar() : Int [     ] {
        let x= // Newlines are preserved.
            (7 *   1) // Comments too.
            + 4 / Fun<Bool,     Double>();
        let s = $"{ 1 +    2}";
        return  x w/ Foo <- (7, y);
    }
}""",

    """namespace Foo {
    function Bar() : Int [ ] {
        let x = // Newlines are preserved.
            (7 * 1) // Comments too.
            + 4 / Fun<Bool, Double>();
        let s = $"{ 1 + 2}";
        return x w/ Foo <- (7, y);
    }
}"""

[<UpdateExample>]
let ``Updates Using Statements`` =
    """namespace Foo {
    operation Bar() : Unit {
        using ((qubits, q) = (Qubit[2], Qubit())) {
            using (q2 = Qubit());
        }
    }
}""",

    """namespace Foo {
    operation Bar() : Unit {
        use (qubits, q) = (Qubit[2], Qubit()) {
            use q2 = Qubit();
        }
    }
}"""

[<UpdateExample>]
let ``Updates Borrowing Statements`` =
    """namespace Foo {
    operation Bar() : Unit {
        borrowing ((qubits, q) = (Qubit[2], Qubit())) {
            borrowing (q2 = Qubit());
        }
    }
}""",

    """namespace Foo {
    operation Bar() : Unit {
        borrow (qubits, q) = (Qubit[2], Qubit()) {
            borrow q2 = Qubit();
        }
    }
}"""
