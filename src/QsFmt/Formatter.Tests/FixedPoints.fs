// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// <summary>
/// <see cref="QsFmt.Formatter.Tests.FixedPoint"/> test cases.
/// </summary>
module Microsoft.Quantum.QsFmt.Formatter.Tests.FixedPoints

open System.IO
open Antlr4.Runtime
open Microsoft.Quantum.QsFmt.Formatter
open Microsoft.Quantum.QsFmt.Formatter.Tests
open Microsoft.Quantum.QsFmt.Parser
open Xunit

/// <summary>
/// Returns true if <paramref name="source"/> is valid Q# syntax.
/// </summary>
let private isValidSyntax (source: string) =
    let parser = source |> AntlrInputStream |> QSharpLexer |> CommonTokenStream |> QSharpParser
    parser.document () |> ignore
    parser.NumberOfSyntaxErrors = 0

/// Test case files with valid syntax.
let private testCases () =
    Directory.GetFiles("TestCases", "*.qs", EnumerationOptions(RecurseSubdirectories = true))
    |> Seq.map File.ReadAllText
    |> Seq.filter isValidSyntax
    |> Seq.map (Array.create 1)

[<Theory>]
[<MemberData "testCases">]
let ``Identity preserves original source code`` source =
    let original = Ok source |> ShowResult
    let transformed = Formatter.identity source |> ShowResult
    Assert.Equal(original, transformed)

[<FixedPoint>]
let ``Namespace comments`` =
    """/// The Foo namespace.
namespace Foo {}

/// The Bar namespace.
namespace Bar {}

// End of file."""

[<FixedPoint>]
let ``Mixed newlines`` =
    "namespace Foo {}\n
namespace Bar {}\r\n"

[<FixedPoint>]
let ``Function with one parameter`` =
    """namespace Foo {
    function Bar(x : Int) : Int {
        return x;
    }
}"""

[<FixedPoint>]
let ``Function with two parameters`` =
    """namespace Foo {
    function Bar(x : Int, y : Int) : Int {
        return x + y;
    }
}"""

[<FixedPoint>]
let ``Operation with Adj characteristic`` =
    """namespace Foo {
    operation Bar () : Unit is Adj {}
}"""

[<FixedPoint>]
let ``Operation with Adj + Ctl characteristics`` =
    """namespace Foo {
    operation Bar () : Unit is Adj + Ctl {}
}"""

[<FixedPoint>]
let ``Entry point and using statement`` =
    """namespace Microsoft.Quantum.Foo {
    @EntryPoint()
    operation RunProgram (nQubits : Int) : Unit {
        using (register = Qubit[nQubits]) {
            H(register[0]);
        }
    }
}"""

[<FixedPoint>]
let ``Open directives and operation`` =
    """namespace Foo {
    open Bar;
    open Baz;

    operation Spam () : Unit {}
}"""

[<FixedPoint>]
let ``Open directive and entry point`` =
    """namespace Foo {
    open Test;

    @EntryPoint()
    operation Bar () : Unit {}
}"""

[<FixedPoint(Skip = "Not supported.")>]
let ``Operation with comments`` =
    """namespace Foo {
    open Test;

    operation Bar () : Unit {
        // This is a comment
        Message("Bar");
        // Lorem ipsum
        // Dolor sit amet
    }
}"""

[<FixedPoint>]
let ``Mutable variable`` =
    """namespace Foo {
    function Bar() : Unit {
        mutable x = 0;
    }
}"""

[<FixedPoint>]
let ``Mutable variable after comment`` =
    """namespace Foo {
    function Bar() : Unit {
        // Hello world
        mutable x = 0;
    }
}"""

[<FixedPoint(Skip = "Not supported.")>]
let ``Mutable variable before comment`` =
    """namespace Foo {
    function Bar() : Unit {
        mutable x = 0;
        // Hello world
    }
}"""

[<FixedPoint>]
let ``Array literal`` =
    """namespace Foo {
    function Bar() : Unit {
        let xs = [1, 2, 3];
    }
}"""

[<FixedPoint>]
let ``Declare type parameter`` =
    """namespace Foo {
    function Bar<'a>() : Unit {}
}
"""

[<FixedPoint>]
let ``Declare 2 type parameters`` =
    """namespace Foo {
    function Bar<'a, 'b>() : Unit {}
}
"""

[<FixedPoint>]
let ``Declare 3 type parameters`` =
    """namespace Foo {
    function Bar<'a, 'b, 'c>() : Unit {}
}
"""

[<FixedPoint>]
let ``Intrinsic body specialization`` =
    """namespace Foo {
    function Bar() : Unit {
        body intrinsic;
    }
}
"""

[<FixedPoint>]
let ``Intrinsic body and controlled specializations`` =
    """namespace Foo {
    function Bar() : Unit {
        body intrinsic;
        controlled intrinsic;
    }
}
"""

[<FixedPoint>]
let ``Explicit body and controlled specializations`` =
    """namespace Foo {
    function Bar() : Unit {
        body (...) {
            Message("body");
        }

        controlled (cs, ...) {
            Message("controlled");
        }
    }
}
"""

[<FixedPoint>]
let ``Discard symbol`` =
    """namespace Foo {
    function Bar() : Unit {
        let _ = 0;
    }
}
"""

[<FixedPoint>]
let ``Missing type`` =
    """namespace Foo {
    function Bar (arg : _) : Unit {}
}"""

[<FixedPoint>]
let ``Type parameter`` =
    """namespace Foo {
    function Bar (arg : 't) : Unit {}
}"""

[<FixedPoint>]
let ``Tuple type`` =
    """namespace Foo {
    function Bar (arg : (BigInt, Bool, (Double, Int))) : Pauli {}
}"""

[<FixedPoint>]
let ``Array type`` =
    """namespace Foo {
    function Bar (arg : Qubit[]) : Range {}
}"""

[<FixedPoint>]
let ``Function type`` =
    """namespace Foo {
    function Bar (arg : Result => String is Adj) : Unit {}
}"""

[<FixedPoint>]
let Literals =
    """namespace Foo {
    function Bar () : Unit {
        let int = 1;
        let big_int = 1234567890L;
        let double = 1.23;
        let double_no_frac = 1.;
        let double_no_whole = .1;
        let exponent1 = 1e2;
        let exponent2 = 1.2e3;
        let exponent3 = 3.e4;
        let exponent4 = .4e5;
        let exponent5 = 5e-6;
        let exponent6 = 5.6e+7;
        let string = "abc";
        let interp_string = $"abc";
        let bool = False;
        let result = One;
        let pauli = PauliI;
    }
}"""

[<FixedPoint>]
let ``Array expression`` =
    """namespace Foo {
    function Bar () : Int[] {
        return [1, 2, 3];
    }
}"""

[<FixedPoint>]
let ``New array expression`` =
    """namespace Foo {
    function Bar () : Int[] {
        return new Int[3];
    }
}"""

[<FixedPoint>]
let ``Named item access`` =
    """namespace Foo {
    function Bar () : Double {
        let complex = Complex(1.,0.);
        return complex::Imaginary;
    }
}"""

[<FixedPoint>]
let ``Array item access`` =
    """namespace Foo {
    function Bar (arr: Int[]) : Int {
        return arr[0];
    }
}"""

[<FixedPoint>]
let ``Unwrap operator`` =
    """namespace Foo {
    function FetchFirst (tup: (Int, String)) : Int {
        let (first, second) = tup!;
        return first;
    }
}"""

[<FixedPoint>]
let ``Factor application`` =
    """namespace Foo {
    function Bar () : Unit {
        Controlled Adjoint Op([], ());
    }
}"""

[<FixedPoint>]
let ``Prefix operator`` =
    """namespace Foo {
    function Bar () : Int {
        return +2 + -2;
    }
}"""

[<FixedPoint>]
let ``Conditional expression`` =
    """namespace Foo {
    function Bar () : Int {
        return true? 1 | 2;
    }
}"""

[<FixedPoint>]
let ``Range expressions`` =
    """namespace Foo {
    function Bar () : Unit {
        let arr = [1,2,3,4,5,6];
        let slice1 = arr[3...];
        let slice2 = arr[...2..3];
        let slice3 = arr[...];
    }
}"""
