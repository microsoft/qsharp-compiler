﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// <summary>
/// <see cref="QsFmt.Formatter.Tests.Example"/> test cases.
/// </summary>
module Microsoft.Quantum.QsFmt.Formatter.Tests.Examples

[<Example(ExampleKind.Format)>]
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

[<Example(ExampleKind.Format)>]
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

[<Example(ExampleKind.Format)>]
let ``Adds newlines and indents`` =
    """namespace Foo { function Bar() : Int { let x = 5; return x; } }""",

    """namespace Foo {
    function Bar() : Int {
        let x = 5;
        return x;
    }
}"""

[<Example(ExampleKind.Format)>]
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

[<Example(ExampleKind.Update)>]
let ``Updates Using Blocks`` =
    """namespace Foo {
    operation Bar() : Unit {
        using ((qubits, q) = (Qubit[2], Qubit())) {
            let x = // Newlines are preserved.
                (7 * 1) // Comments too.
                + 4 / Fun<Bool, Double>();
            using (q2 = Qubit()) {
                let s = $"{1 + 2}";
            }
        }
    }
}""",

    """namespace Foo {
    operation Bar() : Unit {
        use (qubits, q) = (Qubit[2], Qubit()) {
            let x = // Newlines are preserved.
                (7 * 1) // Comments too.
                + 4 / Fun<Bool, Double>();
            use q2 = Qubit() {
                let s = $"{1 + 2}";
            }
        }
    }
}"""

[<Example(ExampleKind.Update)>]
let ``Updates Using Statements`` =
    """namespace Foo {
    operation Bar() : Unit {
        using ((qubits, q) = (Qubit[2], Qubit()));
        let x = // Newlines are preserved.
            (7 * 1) // Comments too.
            + 4 / Fun<Bool, Double>();
        using (q2 = Qubit());
        let s = $"{1 + 2}";
    }
}""",

    """namespace Foo {
    operation Bar() : Unit {
        use (qubits, q) = (Qubit[2], Qubit());
        let x = // Newlines are preserved.
            (7 * 1) // Comments too.
            + 4 / Fun<Bool, Double>();
        use q2 = Qubit();
        let s = $"{1 + 2}";
    }
}"""

[<Example(ExampleKind.Update)>]
let ``Updates Borrowing Blocks`` =
    """namespace Foo {
    operation Bar() : Unit {
        borrowing ((qubits, q) = (Qubit[2], Qubit())) {
            let x = // Newlines are preserved.
                (7 * 1) // Comments too.
                + 4 / Fun<Bool, Double>();
            borrowing (q2 = Qubit()) {
                let s = $"{1 + 2}";
            }
        }
    }
}""",

    """namespace Foo {
    operation Bar() : Unit {
        borrow (qubits, q) = (Qubit[2], Qubit()) {
            let x = // Newlines are preserved.
                (7 * 1) // Comments too.
                + 4 / Fun<Bool, Double>();
            borrow q2 = Qubit() {
                let s = $"{1 + 2}";
            }
        }
    }
}"""

[<Example(ExampleKind.Update)>]
let ``Updates Borrowing Statements`` =
    """namespace Foo {
    operation Bar() : Unit {
        borrowing ((qubits, q) = (Qubit[2], Qubit()));
        let x = // Newlines are preserved.
            (7 * 1) // Comments too.
            + 4 / Fun<Bool, Double>();
        borrowing (q2 = Qubit());
        let s = $"{1 + 2}";
    }
}""",

    """namespace Foo {
    operation Bar() : Unit {
        borrow (qubits, q) = (Qubit[2], Qubit());
        let x = // Newlines are preserved.
            (7 * 1) // Comments too.
            + 4 / Fun<Bool, Double>();
        borrow q2 = Qubit();
        let s = $"{1 + 2}";
    }
}"""

[<Example(ExampleKind.Update)>]
let ``Updates Using with Comments`` =
    """namespace Foo {
    operation Bar() : Unit {
        using (
            q = Qubit() // comment
        ) {
        }
    }
}""",

    """namespace Foo {
    operation Bar() : Unit {
        use"""
    + " "
    + """
            q = Qubit() // comment
         {
        }
    }
}"""
