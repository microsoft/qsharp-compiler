// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// Tests for the command-line interface.
module Microsoft.Quantum.QsFmt.App.Tests

open Microsoft.Quantum.QsFmt.App.Program
open System
open System.IO
open Xunit

/// The result of running the application.
type private Result =
    {
        /// The exit status code.
        Code: int

        /// The standard output.
        Out: string

        /// The standard error.
        Error: string
    }

/// <summary>
/// Runs the application with the command-line arguments, <paramref name="args"/>, and the standard input,
/// <paramref name="input"/>.
/// </summary>
let private run args input =
    let previousInput = Console.In
    let previousOutput = Console.Out
    let previousError = Console.Error

    use input = new StringReader(input)
    use out = new StringWriter()
    use error = new StringWriter()

    try
        Console.SetIn input
        Console.SetOut out
        Console.SetError error

        {
            Code = main args
            Out = out.ToString()
            Error = error.ToString()
        }
    finally
        Console.SetIn previousInput
        Console.SetOut previousOutput
        Console.SetError previousError

[<Fact>]
let ``Shows help with no arguments`` () =
    Assert.Equal
        ({
             Code = 2
             Out = ""
             Error = "ERROR: missing argument '<string>'.

INPUT:

    <string>              File to format or \"-\" to read from standard input.

OPTIONS:

    --help                Display this list of options.
"
         },
         run [||] "")

[<Theory>]
[<InlineData("Example.qs",
             "namespace Foo {
    function Bar() : Int {
        return 0;
    }
}
")>]
let ``Formats file`` path output =
    Assert.Equal
        ({
             Code = 0
             Out = output
             Error = ""
         },
         run [| path |] "")

[<Theory>]
[<InlineData("namespace Foo { function Bar() : Int { return 0; } }\n",
             "namespace Foo {
    function Bar() : Int {
        return 0;
    }
}
")>]
let ``Formats standard input`` input output =
    Assert.Equal
        ({
             Code = 0
             Out = output
             Error = ""
         },
         run [| "-" |] input)

[<Theory>]
[<InlineData("namespace Foo { invalid syntax; } ",
             "Line 1, column 16: mismatched input 'invalid' expecting {'function', 'internal', 'newtype', 'open', 'operation', '@', '}'}
")>]
let ``Shows syntax errors`` input errors =
    Assert.Equal
        ({
             Code = 1
             Out = ""
             Error = errors
         },
         run [| "-" |] input)

[<Theory>]
[<InlineData "NotFound.qs">]
let ``Shows file not found error`` path =
    let result = run [| path |] ""
    Assert.Equal(3, result.Code)
    Assert.Empty result.Out
    Assert.NotEmpty result.Error
