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
/// Ensures that the new line characters will conform to the standard of the environment's new line character.
/// </summary>
let standardizeNewLines (s: string) =
    s.Replace("\r", "").Replace("\n", Environment.NewLine)

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
            Out = out.ToString() |> standardizeNewLines
            Error = error.ToString() |> standardizeNewLines
        }
    finally
        Console.SetIn previousInput
        Console.SetOut previousOutput
        Console.SetError previousError

[<Fact>]
let ``Shows help with no arguments`` () =
    Assert.Equal(
        {
            Code = 2
            Out = ""
            Error = standardizeNewLines
                "ERROR: missing argument '<string>...'.

INPUT:

    <string>...           File to format or \"-\" to read from standard input.

SUBCOMMANDS:

    update                Update depreciated syntax in the input files.
    format                Format the source code in input files.

    Use 'testhost.exe <subcommand> --help' for additional information.

OPTIONS:

    --backup, -b          Create backup files of input files.
    --recurse, -r         Process the input folder recursively.
    --help                Display this list of options.
"
        },
        run [||] ""
    )

[<Theory>]
[<InlineData("Example1.qs",
             "namespace Example1 {
    function Bar() : Int {
        return 0;
    }
}
")>]
let ``Formats file`` path output =
    Assert.Equal(
        {
            Code = 0
            Out = output |> standardizeNewLines
            Error = ""
        },
        run [| "format"; path |] ""
    )

[<Theory>]
[<InlineData("namespace Foo { function Bar() : Int { return 0; } }
",
             "namespace Foo {
    function Bar() : Int {
        return 0;
    }
}
")>]
let ``Formats standard input`` input output =
    Assert.Equal(
        {
            Code = 0
            Out = output |> standardizeNewLines
            Error = ""
        },
        run [| "format"; "-" |] input
    )

[<Theory>]
[<InlineData("namespace Foo { invalid syntax; } ",
             "Line 1, column 16: mismatched input 'invalid' expecting {'function', 'internal', 'newtype', 'open', 'operation', '@', '}'}
")>]
let ``Shows syntax errors`` input errors =
    Assert.Equal(
        {
            Code = 1
            Out = ""
            Error = errors |> standardizeNewLines
        },
        run [| "-" |] input
    )

[<Theory>]
[<InlineData "NotFound.qs">]
let ``Shows file not found error`` path =
    let result = run [| path |] ""
    Assert.Equal(3, result.Code)
    Assert.Empty result.Out
    Assert.NotEmpty result.Error

[<Fact>]
let ``Input multiple files`` () =
    let output = "namespace Example1 {
    function Bar() : Int {
        return 0;
    }
}
namespace Example2 {
    function Bar() : Int {
        return 0;
    }
}
"
    Assert.Equal(
        {
            Code = 0
            Out = output |> standardizeNewLines
            Error = ""
        },
        run [| "format"; "Example1.qs"; "Example2.qs" |] ""
    )

[<Fact>]
let ``Input directories`` () =
    let output = "namespace SubExample1 {
    function Bar() : Int {
        return 0;
    }
}
namespace SubExample2 {
    function Bar() : Int {
        return 0;
    }
}
namespace SubExample3 {
    function Bar() : Int {
        return 0;
    }
}
"
    Assert.Equal(
        {
            Code = 0
            Out = output |> standardizeNewLines
            Error = ""
        },
        run [| "format"; "SubExamples1"; "SubExamples2" |] ""
    )

[<Fact>]
let ``Input directories with files and stdin`` () =
    let input = "namespace StandardIn { function Bar() : Int { return 0; } }
"
    let output = "namespace Example1 {
    function Bar() : Int {
        return 0;
    }
}
namespace StandardIn {
    function Bar() : Int {
        return 0;
    }
}
namespace SubExample1 {
    function Bar() : Int {
        return 0;
    }
}
namespace SubExample2 {
    function Bar() : Int {
        return 0;
    }
}
"
    Assert.Equal(
        {
            Code = 0
            Out = output |> standardizeNewLines
            Error = ""
        },
        run [| "format"; "Example1.qs"; "-"; "SubExamples1" |] (input |> standardizeNewLines)
    )

[<Fact>]
let ``Input directories with recursive flag`` () =
    let input = "namespace StandardIn { function Bar() : Int { return 0; } }
"
    let output = "namespace Example1 {
    function Bar() : Int {
        return 0;
    }
}
namespace StandardIn {
    function Bar() : Int {
        return 0;
    }
}
namespace SubExample1 {
    function Bar() : Int {
        return 0;
    }
}
namespace SubExample2 {
    function Bar() : Int {
        return 0;
    }
}
namespace SubExample3 {
    function Bar() : Int {
        return 0;
    }
}
namespace NestedExample1 {
    function Bar() : Int {
        return 0;
    }
}
namespace NestedExample2 {
    function Bar() : Int {
        return 0;
    }
}
"
    Assert.Equal(
        {
            Code = 0
            Out = output |> standardizeNewLines
            Error = ""
        },
        run [| "format"; "-r"; "Example1.qs"; "-"; "SubExamples1"; "SubExamples2" |] (input |> standardizeNewLines)
    )
