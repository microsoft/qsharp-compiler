// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.Formatter.Tests.Tests

open System
open System.IO
open Microsoft.Quantum.QsFmt.Formatter
open Xunit

/// <summary>
/// Ensures that the new line characters will conform to the standard of the environment's new line character.
/// </summary>
let standardizeNewLines (s: string) =
    s.Replace("\r", "").Replace("\n", Environment.NewLine)

let private run input expectedOutput expectedWarnings =
    let previousError = Console.Error
    use error = new StringWriter()
    let expectedOutput = expectedOutput |> standardizeNewLines |> Ok
    let expectedWarnings = expectedWarnings |> standardizeNewLines

    try
        Console.SetError error
        let output = Formatter.update "" None input |> Result.map standardizeNewLines
        let warnings = error.ToString() |> standardizeNewLines
        Assert.Equal(expectedOutput, output)
        Assert.Equal(expectedWarnings, warnings)
    finally
        Console.SetError previousError

[<Fact>]
let ``Array Syntax Qubit Types`` () =
    let input =
        """namespace Foo {
    operation Bar() : Unit {
        let t1 = new Int[3];
        let t2 = new Qubit[3];
        let t3 = new Qubit[][3];
        let t4 = new (Int, (Double, Qubit), (String), Unit)[3];
        let t5 = new Double[3];
    }
}"""

    let expectedOutput =
        """namespace Foo {
    operation Bar() : Unit {
        let t1 = [0, size = 3];
        let t2 = new Qubit[3];
        let t3 = new Qubit[][3];
        let t4 = new (Int, (Double, Qubit), (String), Unit)[3];
        let t5 = [0.0, size = 3];
    }
}"""

    let expectedWarnings =
        """Warning: Unable to updated deprecated new array syntax in input from line 4, character 18 to line 4, character 30.
Warning: Unable to updated deprecated new array syntax in input from line 5, character 18 to line 5, character 32.
Warning: Unable to updated deprecated new array syntax in input from line 6, character 18 to line 6, character 63.
"""

    run input expectedOutput expectedWarnings

[<Fact>]
let ``Array Syntax UDT Types`` () =
    let input =
        """namespace Foo {
    newtype MyType = (First: Int, Second: Double);
    operation Bar() : Unit {
        let t1 = new Int[3];
        let t2 = new MyType[3];
        let t3 = new MyType[][3];
        let t4 = new (Int, (Double, MyType), (String), Unit)[3];
        let t5 = new Double[3];
    }
}"""

    let expectedOutput =
        """namespace Foo {
    newtype MyType = (First: Int, Second: Double);
    operation Bar() : Unit {
        let t1 = [0, size = 3];
        let t2 = new MyType[3];
        let t3 = new MyType[][3];
        let t4 = new (Int, (Double, MyType), (String), Unit)[3];
        let t5 = [0.0, size = 3];
    }
}"""

    let expectedWarnings =
        """Warning: Unable to updated deprecated new array syntax in input from line 5, character 18 to line 5, character 31.
Warning: Unable to updated deprecated new array syntax in input from line 6, character 18 to line 6, character 33.
Warning: Unable to updated deprecated new array syntax in input from line 7, character 18 to line 7, character 64.
"""

    run input expectedOutput expectedWarnings

[<Fact>]
let ``Array Syntax Type Parameter Types`` () =
    let input =
        """namespace Foo {
    operation Bar<'T>() : Unit {
        let t1 = new Int[3];
        let t2 = new 'T[3];
        let t3 = new 'T[][3];
        let t4 = new (Int, (Double, 'T), (String), Unit)[3];
        let t5 = new Double[3];
    }
}"""

    let expectedOutput =
        """namespace Foo {
    operation Bar<'T>() : Unit {
        let t1 = [0, size = 3];
        let t2 = new 'T[3];
        let t3 = new 'T[][3];
        let t4 = new (Int, (Double, 'T), (String), Unit)[3];
        let t5 = [0.0, size = 3];
    }
}"""

    let expectedWarnings =
        """Warning: Unable to updated deprecated new array syntax in input from line 4, character 18 to line 4, character 27.
Warning: Unable to updated deprecated new array syntax in input from line 5, character 18 to line 5, character 29.
Warning: Unable to updated deprecated new array syntax in input from line 6, character 18 to line 6, character 60.
"""

    run input expectedOutput expectedWarnings

[<Fact>]
let ``Array Syntax Function Types`` () =
    let input =
        """namespace Foo {
    operation Bar() : Unit {
        let t1 = new Int[3];
        let t2 = new (Int -> Unit)[3];
        let t3 = new (Int -> Unit)[][3];
        let t4 = new (Int, (Double, (Int -> Unit)), (String), Unit)[3];
        let t5 = new Double[3];
    }
}"""

    let expectedOutput =
        """namespace Foo {
    operation Bar() : Unit {
        let t1 = [0, size = 3];
        let t2 = new (Int -> Unit)[3];
        let t3 = new (Int -> Unit)[][3];
        let t4 = new (Int, (Double, (Int -> Unit)), (String), Unit)[3];
        let t5 = [0.0, size = 3];
    }
}"""

    let expectedWarnings =
        """Warning: Unable to updated deprecated new array syntax in input from line 4, character 18 to line 4, character 38.
Warning: Unable to updated deprecated new array syntax in input from line 5, character 18 to line 5, character 40.
Warning: Unable to updated deprecated new array syntax in input from line 6, character 18 to line 6, character 71.
"""

    run input expectedOutput expectedWarnings

[<Fact>]
let ``Array Syntax Operation Types`` () =
    let input =
        """namespace Foo {
    operation Bar() : Unit {
        let t1 = new Int[3];
        let t2 = new (Int => Unit)[3];
        let t3 = new (Int => Unit)[][3];
        let t4 = new (Int, (Double, (Int => Unit)), (String), Unit)[3];
        let t5 = new Double[3];
    }
}"""

    let expectedOutput =
        """namespace Foo {
    operation Bar() : Unit {
        let t1 = [0, size = 3];
        let t2 = new (Int => Unit)[3];
        let t3 = new (Int => Unit)[][3];
        let t4 = new (Int, (Double, (Int => Unit)), (String), Unit)[3];
        let t5 = [0.0, size = 3];
    }
}"""

    let expectedWarnings =
        """Warning: Unable to updated deprecated new array syntax in input from line 4, character 18 to line 4, character 38.
Warning: Unable to updated deprecated new array syntax in input from line 5, character 18 to line 5, character 40.
Warning: Unable to updated deprecated new array syntax in input from line 6, character 18 to line 6, character 71.
"""

    run input expectedOutput expectedWarnings

[<Fact>]
[<Trait("Category", "Statement Kinds Support")>]
let ``Expression Statement Support`` () =
    let input =
        """namespace Foo {
    operation Bar1(x : Int) : Unit {}
    operation Bar2() : Unit {
        Bar1((new Int[3])[2]);
    }
}"""

    let expectedOutput =
        """namespace Foo {
    operation Bar1(x : Int) : Unit {}
    operation Bar2() : Unit {
        Bar1(([0, size = 3])[2]);
    }
}"""

    run input expectedOutput String.Empty
