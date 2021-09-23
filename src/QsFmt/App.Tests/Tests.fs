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

type private TestFile =
    {
        Name: string
        Content: string
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

let private runWithFiles files standardInput expectedOutput args =
    let originals = files |> List.map (fun file -> { file with Content = File.ReadAllText file.Name })
    try
        Assert.Equal(
            expectedOutput,
            run args standardInput
        )
        for file in files do
            let after = File.ReadAllText file.Name |> standardizeNewLines
            Assert.Equal(file.Content |> standardizeNewLines, after)
    finally
        for file in originals do
            File.WriteAllText(file.Name, file.Content)

[<Fact>]
let ``Shows help with no arguments`` () =
    Assert.Equal(
        {
            Code = 2
            Out = ""
            Error =
                standardizeNewLines
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
[<InlineData("Examples\\Example1.qs",
             "namespace Example1 {
    function Bar() : Int {
        return 0;
    }
}
")>]
let ``Formats file`` path output =
    runWithFiles [{ Name = path; Content = output }] "" { Code = 0; Out = ""; Error = "" } [| "format"; path |]

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
             "<Standard Input>, Line 1, Character 16: mismatched input 'invalid' expecting {'function', 'internal', 'newtype', 'open', 'operation', '@', '}'}
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
[<InlineData "Examples\\NotFound.qs">]
let ``Shows file not found error`` path =
    let result = run [| path |] ""
    Assert.Equal(3, result.Code)
    Assert.Empty result.Out
    Assert.NotEmpty result.Error

[<Fact>]
let ``Input multiple files`` () =
    let files = [
        {
            Name = "Examples\\Example1.qs"
            Content = "namespace Example1 {
    function Bar() : Int {
        return 0;
    }
}
"       }
        {
            Name = "Examples\\Example2.qs"
            Content = "namespace Example2 {
    function Bar() : Int {
        return 0;
    }
}
"       }
    ]

    runWithFiles files "" { Code = 0; Out = ""; Error = "" } [| "format"; "Examples\\Example1.qs"; "Examples\\Example2.qs" |]

[<Fact>]
let ``Input directories`` () =
    let files = [
        {
            Name = "Examples\\SubExamples1\\SubExample1.qs"
            Content = "namespace SubExample1 {
    function Bar() : Int {
        return 0;
    }
}
"       }
        {
            Name = "Examples\\SubExamples1\\SubExample2.qs"
            Content = "namespace SubExample2 {
    function Bar() : Int {
        return 0;
    }
}
"       }
        {
            Name = "Examples\\SubExamples2\\SubExample3.qs"
            Content = "namespace SubExample3 {
    function Bar() : Int {
        return 0;
    }
}
"       }
    ]
    runWithFiles files "" { Code = 0; Out = ""; Error = "" } [| "format"; "Examples\\SubExamples1"; "Examples\\SubExamples2" |]

[<Fact>]
let ``Input directories with files and stdin`` () =
    let files = [
        {
            Name = "Examples\\Example1.qs"
            Content = "namespace Example1 {
    function Bar() : Int {
        return 0;
    }
}
"       }
        {
            Name = "Examples\\SubExamples1\\SubExample1.qs"
            Content = "namespace SubExample1 {
    function Bar() : Int {
        return 0;
    }
}
"       }
        {
            Name = "Examples\\SubExamples1\\SubExample2.qs"
            Content = "namespace SubExample2 {
    function Bar() : Int {
        return 0;
    }
}
"       }
    ]
    let input =
        "namespace StandardIn { function Bar() : Int { return 0; } }
"
    let output =
        "namespace StandardIn {
    function Bar() : Int {
        return 0;
    }
}
"
    runWithFiles files input { Code = 0; Out = output |> standardizeNewLines; Error = "" } [| "format"; "Examples\\Example1.qs"; "-"; "Examples\\SubExamples1" |]

[<Fact>]
let ``Input directories with recursive flag`` () =
    let files = [
        {
            Name = "Examples\\Example1.qs"
            Content = "namespace Example1 {
    function Bar() : Int {
        return 0;
    }
}
"       }
        {
            Name = "Examples\\SubExamples1\\SubExample1.qs"
            Content = "namespace SubExample1 {
    function Bar() : Int {
        return 0;
    }
}
"       }
        {
            Name = "Examples\\SubExamples1\\SubExample2.qs"
            Content = "namespace SubExample2 {
    function Bar() : Int {
        return 0;
    }
}
"       }
        {
            Name = "Examples\\SubExamples2\\SubExample3.qs"
            Content = "namespace SubExample3 {
    function Bar() : Int {
        return 0;
    }
}
"       }
        {
            Name = "Examples\\SubExamples2\\NestedExamples\\NestedExample1.qs"
            Content = "namespace NestedExample1 {
    function Bar() : Int {
        return 0;
    }
}
"       }
        {
            Name = "Examples\\SubExamples2\\NestedExamples\\NestedExample2.qs"
            Content = "namespace NestedExample2 {
    function Bar() : Int {
        return 0;
    }
}
"       }
    ]
    let input =
        "namespace StandardIn { function Bar() : Int { return 0; } }
"
    let output =
        "namespace StandardIn {
    function Bar() : Int {
        return 0;
    }
}
"
    runWithFiles files input { Code = 0; Out = output |> standardizeNewLines; Error = "" } [| "format"; "-r"; "Examples\\Example1.qs"; "-"; "Examples\\SubExamples1"; "Examples\\SubExamples2" |]

[<Fact>]
let ``Process correct files while erroring on incorrect`` () =
    let files = [
        {
            Name = "Examples\\Example1.qs"
            Content = "namespace Example1 {
    function Bar() : Int {
        return 0;
    }
}
"       }
        {
            Name = "Examples\\Example2.qs"
            Content = "namespace Example2 {
    function Bar() : Int {
        return 0;
    }
}
"       }
    ]
    let input =
        "namespace StandardIn { function Bar() : Int { return 0; } }
"
    let output =
        "namespace StandardIn {
    function Bar() : Int {
        return 0;
    }
}
"
    let originals = files |> List.map (fun file -> { file with Content = File.ReadAllText file.Name })
    try
        let result = run [| "format"; "Examples\\Example1.qs"; "-"; "Examples\\NotFound.qs"; "Examples\\Example2.qs" |] (input |> standardizeNewLines)
        Assert.Equal(3, result.Code)
        Assert.Equal(output |> standardizeNewLines, result.Out)
        Assert.NotEmpty(result.Error)
        for file in files do
            let after = File.ReadAllText file.Name |> standardizeNewLines
            Assert.Equal(file.Content |> standardizeNewLines, after)
    finally
        for file in originals do
            File.WriteAllText(file.Name, file.Content)
