/// Tests for the command-line interface.
module QsFmt.App.Tests

open QsFmt.App.Program
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
             Code = 1
             Out = ""
             Error = "ERROR: missing argument '<string>'.

INPUT:

    <string>              File to format or \"-\" to read from standard input.

OPTIONS:

    --help                Display this list of options.
"
         },
         run [||] "")

[<Fact>]
let ``Formats file`` () =
    Assert.Equal
        ({
             Code = 0
             Out = "namespace Foo {
    function Bar() : Int {
        return 0;
    }
}
"
             Error = ""
         },
         run [| "Example.qs" |] "")

[<Fact>]
let ``Formats standard input`` () =
    Assert.Equal
        ({
             Code = 0
             Out = "namespace Foo {
    function Bar() : Int {
        return 0;
    }
}
"
             Error = ""
         },
         run [| "-" |] "namespace Foo { function Bar() : Int { return 0; } }\n")
