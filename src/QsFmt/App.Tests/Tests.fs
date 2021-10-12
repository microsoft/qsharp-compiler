// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// Tests for the command-line interface.
module Microsoft.Quantum.QsFmt.App.Tests

open System
open System.IO
open Microsoft.Quantum.QsFmt.App.Program
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

let CleanResult =
    {
        Code = 0
        Out = ""
        Error = ""
    }

let makeTestFile (path: string) =
    let name = path.[(path.LastIndexOf "\\") + 1..(path.LastIndexOf ".qs") - 1]

    {
        Path = path
        Original = File.ReadAllText path
        Formatted =
            name
            |> sprintf
                "namespace %s {
    function Bar() : Int {
        for (i in 0..1) {}
        return 0;
    }
}
"
            |> standardizeNewLines
        Updated =
            name
            |> sprintf
                "namespace %s { function Bar() : Int { for i in 0..1 {} return 0; } }
"
            |> standardizeNewLines
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
        Original =
            "namespace StandardIn { function Bar() : Int { for (i in 0..1) {} return 0; } }
"
        Formatted =
            "namespace StandardIn {
    function Bar() : Int {
        for (i in 0..1) {}
        return 0;
    }
}
"
            |> standardizeNewLines
        Updated =
            "namespace StandardIn { function Bar() : Int { for i in 0..1 {} return 0; } }
"
            |> standardizeNewLines
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
        Assert.Equal(expectedOutput, run args standardInput)

        for file in files do
            let after = File.ReadAllText file.Path |> standardizeNewLines
            let expected = (if isUpdate then file.Updated else file.Formatted)
            Assert.Equal(expected, after)
    finally
        for file in files do
            File.WriteAllText(file.Path, file.Original)

[<Fact>]
let ``Updates file`` () =
    runWithFiles true [ Example1 ] "" CleanResult [| "update"; "-i"; Example1.Path |]

[<Fact(Skip = "Standard Input is not currently supported.")>]
let ``Updates standard input`` () =
    Assert.Equal(
        {
            Code = 0
            Out = StandardInputTest.Updated
            Error = ""
        },
        run [| "update"; "-" |] StandardInputTest.Original
    )

[<Fact>]
let ``Shows file not found error`` () =
    let result = run [| "update"; "-i"; "Examples\\NotFound.qs" |] ""
    Assert.Equal(3, result.Code)
    Assert.Empty result.Out
    Assert.NotEmpty result.Error

[<Fact>]
let ``Input multiple files`` () =
    let files = [ Example1; Example2 ]

    runWithFiles true files "" CleanResult [| "update"; "-i"; Example1.Path; Example2.Path |]

[<Fact>]
let ``Input directories`` () =
    let files = [ SubExample1; SubExample2; SubExample3 ]

    runWithFiles true files "" CleanResult [| "update"; "-i"; "Examples\\SubExamples1"; "Examples\\SubExamples2" |]

[<Fact>]
let ``Input directories with files`` () =
    let files = [ Example1; SubExample1; SubExample2 ]

    runWithFiles true files "" CleanResult [| "update"; "-i"; Example1.Path; "Examples\\SubExamples1" |]

[<Fact>]
let ``Input directories with recursive flag`` () =
    let files =
        [
            Example1
            SubExample1
            SubExample2
            SubExample3
            NestedExample1
            NestedExample2
        ]

    let args =
        [|
            "update"
            "-r"
            "-i"
            Example1.Path
            "Examples\\SubExamples1"
            "Examples\\SubExamples2"
        |]

    runWithFiles true files "" CleanResult args

[<Fact>]
let ``Process correct files while erroring on incorrect`` () =
    let files = [ Example1; Example2 ]

    try
        let result = run [| "update"; "-i"; Example1.Path; "Examples\\NotFound.qs"; Example2.Path |] ""

        Assert.Equal(3, result.Code)
        Assert.NotEmpty(result.Error)

        for file in files do
            let after = File.ReadAllText file.Path |> standardizeNewLines
            Assert.Equal(file.Updated, after)
    finally
        for file in files do
            File.WriteAllText(file.Path, file.Original)

[<Fact>]
let ``Backup flag`` () =
    let files = [ Example1; SubExample3 ]

    try
        let result = run [| "update"; "-b"; "-i"; Example1.Path; "Examples\\SubExamples2" |] ""

        Assert.Equal(CleanResult, result)

        for file in files do
            let backup = file.Path + "~"
            Assert.True(File.Exists(backup))
            let backup = File.ReadAllText(backup) |> standardizeNewLines
            Assert.Equal(file.Original |> standardizeNewLines, backup)
            let after = File.ReadAllText file.Path |> standardizeNewLines
            Assert.Equal(file.Updated, after)
    finally
        for file in files do
            File.WriteAllText(file.Path, file.Original)
            if File.Exists(file.Path + "~") then File.Delete(file.Path + "~")

[<Fact>]
let ``Error when same input given multiple times`` () =
    let files = [ Example1; SubExample1; SubExample2 ]

    let outputResult =
        {
            Code = 5
            Out = ""
            Error =
                "This input has already been processed: Examples\SubExamples1\SubExample1.qs
This input has already been processed: Examples\Example1.qs
"
                |> standardizeNewLines
        }

    [|
        "update"
        "-i"
        Example1.Path
        "Examples\\SubExamples1"
        SubExample1.Path
        Example1.Path
    |]
    |> runWithFiles true files "" outputResult

[<Fact>]
let ``Project file as input`` () =

    let makeTestFile (path: string) (index: int) =
        {
            Path = path
            Original = File.ReadAllText path
            Formatted =
                index
                |> sprintf
                    "namespace TestTarget {
    function Bar%i() : Int {
        for (i in 0..1) {}
        return 0;
    }
}
"
                |> standardizeNewLines
            Updated =
                index
                |> sprintf
                    "namespace TestTarget { function Bar%i() : Int { for i in 0..1 {} return 0; } }
"
                |> standardizeNewLines
        }

    let TestTargetProgram =
        let path = "Examples\\TestTarget\\Program.qs"

        {
            Path = path
            Original = File.ReadAllText path
            Formatted =
                "namespace TestTarget {
    @EntryPoint()
    operation Bar() : Unit {
        for (i in 0..1) {}
    }
}
"
                |> standardizeNewLines
            Updated =
                "namespace TestTarget { @EntryPoint() operation Bar() : Unit { for i in 0..1 {} } }
"
                |> standardizeNewLines
        }

    let TestTargetIncluded = makeTestFile "Examples\\TestTarget\\Included.qs" 1
    let TestTargetExcluded1 = makeTestFile "Examples\\TestTarget\\Excluded1.qs" 2
    let TestTargetExcluded2 = makeTestFile "Examples\\TestTarget\\Excluded2.qs" 3


    let files = [ TestTargetProgram; TestTargetIncluded ]

    [| "update"; "-p"; "Examples\\TestTarget\\TestTarget.csproj" |]
    |> runWithFiles true files "" CleanResult

    let excluded1 = File.ReadAllText TestTargetExcluded1.Path
    Assert.Equal(excluded1, TestTargetExcluded1.Original)

    let excluded2 = File.ReadAllText TestTargetExcluded2.Path
    Assert.Equal(excluded2, TestTargetExcluded2.Original)
