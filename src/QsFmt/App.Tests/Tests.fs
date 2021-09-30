// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// Tests for the command-line interface.
module Microsoft.Quantum.QsFmt.App.Tests

open Microsoft.Quantum.QsFmt.App.Program
open System
open System.IO
open Xunit

/// The result of running the application.
type Result =
    {
        /// The exit status code.
        Code: int

        /// The standard output.
        Out: string

        /// The standard error.
        Error: string
    }

type TestFile =
    {
        Path: string
        Original: string
        Formatted: string
        Updated: string
    }

/// <summary>
/// Ensures that the new line characters will conform to the standard of the environment's new line character.
/// </summary>
let standardizeNewLines (s: string) =
    s.Replace("\r", "").Replace("\n", Environment.NewLine)

let CleanResult = { Code = 0; Out = ""; Error = "" }

let makeTestFile (path: string) =
    let name = path.[(path.LastIndexOf "\\") + 1 .. (path.LastIndexOf ".qs") - 1]
    {
        Path = path
        Original = File.ReadAllText path
        Formatted = name |> sprintf "namespace %s {
    function Bar() : Int {
        for (i in 0..1) {}
        return 0;
    }
}
"       |> standardizeNewLines
        Updated = name |> sprintf "namespace %s { function Bar() : Int { for i in 0..1 {} return 0; } }
"       |> standardizeNewLines
    }

let Example1 = makeTestFile "Examples\\Example1.qs"
let Example2 = makeTestFile "Examples\\Example2.qs"
let SubExample1 = makeTestFile "Examples\\SubExamples1\\SubExample1.qs"
let SubExample2 = makeTestFile "Examples\\SubExamples1\\SubExample2.qs"
let SubExample3 = makeTestFile "Examples\\SubExamples2\\SubExample3.qs"
let NestedExample1 = makeTestFile "Examples\\SubExamples2\\NestedExamples\\NestedExample1.qs"
let NestedExample2 = makeTestFile "Examples\\SubExamples2\\NestedExamples\\NestedExample2.qs"

let StandardInputTest =
    {
        Path = "-"
        Original = "namespace StandardIn { function Bar() : Int { for (i in 0..1) {} return 0; } }
"
        Formatted = "namespace StandardIn {
    function Bar() : Int {
        for (i in 0..1) {}
        return 0;
    }
}
"       |> standardizeNewLines
        Updated = "namespace StandardIn { function Bar() : Int { for i in 0..1 {} return 0; } }
"       |> standardizeNewLines
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
            Out = out.ToString() |> standardizeNewLines
            Error = error.ToString() |> standardizeNewLines
        }
    finally
        Console.SetIn previousInput
        Console.SetOut previousOutput
        Console.SetError previousError

let private runWithFiles isUpdate files standardInput expectedOutput args =
    try
        Assert.Equal(
            expectedOutput,
            run args standardInput
        )
        for file in files do
            let after = File.ReadAllText file.Path |> standardizeNewLines
            let expected = (if isUpdate then file.Updated else file.Formatted)
            Assert.Equal(expected, after)
    finally
        for file in files do
            File.WriteAllText(file.Path, file.Original)

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

[<Fact>]
let ``Updates file`` () =
    runWithFiles true [Example1] "" CleanResult [| "update"; Example1.Path |]

[<Fact>]
let ``Updates standard input`` () =
    Assert.Equal(
        {
            Code = 0
            Out = StandardInputTest.Updated
            Error = ""
        },
        run [| "update"; "-" |] StandardInputTest.Original
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
        Example1
        Example2
    ]

    runWithFiles true files "" CleanResult [| "update"; Example1.Path; Example2.Path |]

[<Fact>]
let ``Input directories`` () =
    let files = [
        SubExample1
        SubExample2
        SubExample3
    ]
    runWithFiles true files "" CleanResult [| "update"; "Examples\\SubExamples1"; "Examples\\SubExamples2" |]

[<Fact>]
let ``Input directories with files and stdin`` () =
    let files = [
        Example1
        SubExample1
        SubExample2
    ]
    [| "update"; Example1.Path; "-"; "Examples\\SubExamples1" |]
    |> runWithFiles true files StandardInputTest.Original { Code = 0; Out = StandardInputTest.Updated; Error = "" }

[<Fact>]
let ``Input directories with recursive flag`` () =
    let files = [
        Example1
        SubExample1
        SubExample2
        SubExample3
        NestedExample1
        NestedExample2
    ]
    [| "update"; "-r"; Example1.Path; "-"; "Examples\\SubExamples1"; "Examples\\SubExamples2" |]
    |> runWithFiles true files StandardInputTest.Original { Code = 0; Out = StandardInputTest.Updated; Error = "" }

[<Fact>]
let ``Process correct files while erroring on incorrect`` () =
    let files = [
        Example1
        Example2
    ]
    try
        let result = run [| "update"; Example1.Path; "-"; "Examples\\NotFound.qs"; Example2.Path |] StandardInputTest.Original
        Assert.Equal(3, result.Code)
        Assert.Equal(StandardInputTest.Updated, result.Out)
        Assert.NotEmpty(result.Error)
        for file in files do
            let after = File.ReadAllText file.Path |> standardizeNewLines
            Assert.Equal(file.Updated, after)
    finally
        for file in files do
            File.WriteAllText(file.Path, file.Original)

[<Fact>]
let ``Backup flag`` () =
    let files = [
        Example1
        SubExample3
    ]
    try
        let result = run [| "update"; "-b"; "-"; Example1.Path; "Examples\\SubExamples2"; |] StandardInputTest.Original

        Assert.Equal(
            {
                Code = 0;
                Out = StandardInputTest.Updated
                Error = ""
            },
            result
        )

        for file in files do
            Assert.True(File.Exists(file.Path + "~"))
            let backup = File.ReadAllText(file.Path + "~") |> standardizeNewLines
            Assert.Equal(file.Original |> standardizeNewLines, backup)
            let after = File.ReadAllText file.Path |> standardizeNewLines
            Assert.Equal(file.Updated, after)
    finally
        for file in files do
            File.WriteAllText(file.Path, file.Original)
            if File.Exists(file.Path + "~") then
                File.Delete(file.Path + "~")

[<Fact>]
let ``Error when same input given multiple times`` () =
    let files = [
        Example1
        SubExample1
        SubExample2
    ]
    let outputResult =
        {
            Code = 5
            Out = ""
            Error = "This input has already been processed: Examples\SubExamples1\SubExample1.qs
This input has already been processed: Examples\Example1.qs
" |> standardizeNewLines
        }
    [| "update"; Example1.Path; "Examples\\SubExamples1"; SubExample1.Path; Example1.Path |]
    |> runWithFiles true files "" outputResult
