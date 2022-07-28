// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// Tests for the command-line interface.
module Microsoft.Quantum.QsFmt.App.Tests

open System
open System.IO
open Microsoft.Quantum.QsFmt.App.Arguments
open Microsoft.Quantum.QsFmt.App.Program
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

/// Represents a test file and all the expected versions of its contents.
type private TestFile =
    {
        /// The path to the test file.
        Path: string

        /// The original content of the file.
        Original: string

        /// The expected content after formatting.
        Formatted: string

        /// The expected content after updating.
        Updated: string

        /// The expected content after updating and formatting.
        UpdatedAndFormatted: String
    }

/// <summary>
/// Ensures that the new line characters will conform to the standard of the environment's new line character.
/// </summary>
let private standardizeNewLines (s: string) =
    s.Replace("\r", "").Replace("\n", Environment.NewLine)

let private cleanResult =
    {
        Code = ExitCode.Success |> int
        Out = ""
        Error = ""
    }

let private makeOutput verb files =
    (Seq.length files |> sprintf "%s %i files:" verb)
    :: (files |> Seq.map (fun file -> sprintf "\t%s %s" verb file) |> Seq.toList)
    |> fun lines -> String.Join(Environment.NewLine, lines)

let private updateOutput files =
    (makeOutput "Updated" files) + Environment.NewLine

let private formatOutput files =
    (makeOutput "Formatted" files) + Environment.NewLine

let private updateAndFormatOutput files =
    (makeOutput "Updated" files)
    + Environment.NewLine
    + (makeOutput "Formatted" files)
    + Environment.NewLine

let private makeTestFile (path: string) =
    let name = path.[(path.LastIndexOf "\\") + 1 .. (path.LastIndexOf ".qs") - 1]

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
        UpdatedAndFormatted =
            name
            |> sprintf
                "namespace %s {
    function Bar() : Int {
        for i in 0..1 {}
        return 0;
    }
}
"
            |> standardizeNewLines
    }

let private example1 = makeTestFile "Examples\\Example1.qs"
let private example2 = makeTestFile "Examples\\Example2.qs"
let private subExample1 = makeTestFile "Examples\\SubExamples1\\SubExample1.qs"
let private subExample2 = makeTestFile "Examples\\SubExamples1\\SubExample2.qs"
let private subExample3 = makeTestFile "Examples\\SubExamples2\\SubExample3.qs"
let private nestedExample1 = makeTestFile "Examples\\SubExamples2\\NestedExamples\\NestedExample1.qs"
let private nestedExample2 = makeTestFile "Examples\\SubExamples2\\NestedExamples\\NestedExample2.qs"

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
            Code = args |> List.toArray |> main
            Out = out.ToString() |> standardizeNewLines
            Error = error.ToString() |> standardizeNewLines
        }
    finally
        Console.SetIn previousInput
        Console.SetOut previousOutput
        Console.SetError previousError

let private runWithFiles (commandKind: CommandKind) files standardInput expectedOutput args =
    try
        Assert.Equal(expectedOutput, run args standardInput)

        for file in files do
            let after = File.ReadAllText file.Path |> standardizeNewLines

            let expected =
                match commandKind with
                | Format -> file.Formatted
                | Update -> file.Updated
                | UpdateAndFormat -> file.UpdatedAndFormatted

            Assert.Equal(expected, after)
    finally
        for file in files do
            File.WriteAllText(file.Path, file.Original)

[<Fact>]
let ``Format file`` () =
    runWithFiles
        Format
        [ example1 ]
        ""
        { cleanResult with Out = formatOutput [ example1.Path ] }
        [ "format"; "-i"; example1.Path ]

[<Fact>]
let ``Update file`` () =
    runWithFiles
        Update
        [ example1 ]
        ""
        { cleanResult with Out = updateOutput [ example1.Path ] }
        [ "update"; "-i"; example1.Path ]

[<Fact>]
let ``Update and format file`` () =
    runWithFiles
        UpdateAndFormat
        [ example1 ]
        ""
        { cleanResult with Out = updateAndFormatOutput [ example1.Path ] }
        [ "update-and-format"; "-i"; example1.Path ]

[<Fact>]
let ``Shows syntax errors`` () =
    Assert.Equal(
        {
            Code = ExitCode.SyntaxErrors |> int
            Out = ""
            Error =
                standardizeNewLines
                    "Examples\SyntaxError.qs, Line 1, Character 16: mismatched input 'invalid' expecting {'function', 'internal', 'newtype', 'open', 'operation', '@', '}'}
"
        },
        run [ "update"; "-i"; "Examples\\SyntaxError.qs" ] ""
    )

[<Fact>]
let ``Shows file not found error`` () =
    let result = run [ "update"; "-i"; "Examples\\NotFound.qs" ] ""
    Assert.Equal(ExitCode.IOError |> int, result.Code)
    Assert.Empty result.Out
    Assert.NotEmpty result.Error

[<Fact>]
let ``Input multiple files`` () =
    let files = [ example1; example2 ]
    let paths = files |> List.map (fun f -> f.Path)

    runWithFiles Format files "" { cleanResult with Out = formatOutput paths } ([ "format"; "-i" ] @ paths)
    runWithFiles Update files "" { cleanResult with Out = updateOutput paths } ([ "update"; "-i" ] @ paths)

    runWithFiles
        UpdateAndFormat
        files
        ""
        { cleanResult with Out = updateAndFormatOutput paths }
        ([ "update-and-format"; "-i" ] @ paths)

[<Fact>]
let ``Input directories`` () =
    let files = [ subExample1; subExample2; subExample3 ]
    let paths = files |> List.map (fun f -> f.Path)

    runWithFiles
        Format
        files
        ""
        { cleanResult with Out = formatOutput paths }
        [ "format"; "-i"; "Examples\\SubExamples1"; "Examples\\SubExamples2" ]

    runWithFiles
        Update
        files
        ""
        { cleanResult with Out = updateOutput paths }
        [ "update"; "-i"; "Examples\\SubExamples1"; "Examples\\SubExamples2" ]

    runWithFiles
        UpdateAndFormat
        files
        ""
        { cleanResult with Out = updateAndFormatOutput paths }
        [
            "update-and-format"
            "-i"
            "Examples\\SubExamples1"
            "Examples\\SubExamples2"
        ]

[<Fact>]
let ``Input directories with files`` () =
    let files = [ example1; subExample1; subExample2 ]
    let paths = files |> List.map (fun f -> f.Path)

    runWithFiles
        Format
        files
        ""
        { cleanResult with Out = formatOutput paths }
        [ "format"; "-i"; example1.Path; "Examples\\SubExamples1" ]

    runWithFiles
        Update
        files
        ""
        { cleanResult with Out = updateOutput paths }
        [ "update"; "-i"; example1.Path; "Examples\\SubExamples1" ]

    runWithFiles
        UpdateAndFormat
        files
        ""
        { cleanResult with Out = updateAndFormatOutput paths }
        [ "update-and-format"; "-i"; example1.Path; "Examples\\SubExamples1" ]

[<Fact>]
let ``Input directories with recursive flag`` () =
    let files =
        [
            example1
            subExample1
            subExample2
            nestedExample1
            nestedExample2
            subExample3
        ]

    let paths = files |> List.map (fun f -> f.Path)

    let args verb =
        [
            verb
            "-r"
            "-i"
            example1.Path
            "Examples\\SubExamples1"
            "Examples\\SubExamples2"
        ]

    args "format" |> runWithFiles Format files "" { cleanResult with Out = formatOutput paths }
    args "update" |> runWithFiles Update files "" { cleanResult with Out = updateOutput paths }

    args "update-and-format"
    |> runWithFiles UpdateAndFormat files "" { cleanResult with Out = updateAndFormatOutput paths }

[<Fact>]
let ``Process correct files while erroring on incorrect`` () =
    let files = [ example1; example2 ]

    try
        let result = run [ "update"; "-i"; example1.Path; "Examples\\NotFound.qs"; example2.Path ] ""

        Assert.Equal(ExitCode.IOError |> int, result.Code)
        Assert.NotEmpty(result.Error)

        for file in files do
            let after = File.ReadAllText file.Path |> standardizeNewLines
            Assert.Equal(file.Updated, after)
    finally
        for file in files do
            File.WriteAllText(file.Path, file.Original)

[<Fact>]
let ``Backup flag`` () =
    let files = [ example1; subExample3 ]
    let paths = files |> List.map (fun f -> f.Path)

    try
        let result = run [ "update"; "-b"; "-i"; example1.Path; "Examples\\SubExamples2" ] ""

        Assert.Equal({ cleanResult with Out = updateOutput paths }, result)

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
    let files = [ example1; subExample1; subExample2 ]

    let outputResult =
        {
            Code = ExitCode.FileAlreadyProcessed |> int
            Out = ""
            Error =
                "This input has already been processed: Examples\SubExamples1\SubExample1.qs
This input has already been processed: Examples\Example1.qs
"
                |> standardizeNewLines
        }

    let args verb =
        [
            verb
            "-i"
            example1.Path
            "Examples\\SubExamples1"
            subExample1.Path
            example1.Path
        ]

    args "format" |> runWithFiles Format files "" outputResult
    args "update" |> runWithFiles Update files "" outputResult
    args "update-and-format" |> runWithFiles UpdateAndFormat files "" outputResult

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
            UpdatedAndFormatted =
                index
                |> sprintf
                    "namespace TestTarget {
    function Bar%i() : Int {
        for i in 0..1 {}
        return 0;
    }
}
"
                |> standardizeNewLines

        }

    let testTargetProgram =
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
            UpdatedAndFormatted =
                "namespace TestTarget {
    @EntryPoint()
    operation Bar() : Unit {
        for i in 0..1 {}
    }
}
"
                |> standardizeNewLines
        }

    let testTargetIncluded = makeTestFile "Examples\\TestTarget\\Included.qs" 1
    let testTargetExcluded1 = makeTestFile "Examples\\TestTarget\\Excluded1.qs" 2
    let testTargetExcluded2 = makeTestFile "Examples\\TestTarget\\Excluded2.qs" 3


    let files = [ testTargetIncluded; testTargetProgram ]
    let paths = files |> List.map (fun f -> Path.GetFullPath f.Path)

    let testCommand commandKind =
        let verb, getOutput =
            match commandKind with
            | Format -> "format", formatOutput
            | Update -> "update", updateOutput
            | UpdateAndFormat -> "update-and-format", updateAndFormatOutput

        [ verb; "-p"; "Examples\\TestTarget\\TestTarget.csproj" ]
        |> runWithFiles commandKind files "" { cleanResult with Out = getOutput paths }

        let excluded1 = File.ReadAllText testTargetExcluded1.Path
        Assert.Equal(excluded1, testTargetExcluded1.Original)

        let excluded2 = File.ReadAllText testTargetExcluded2.Path
        Assert.Equal(excluded2, testTargetExcluded2.Original)

    testCommand Format
    testCommand Update
    testCommand UpdateAndFormat

[<Fact>]
let ``Outdated version project file as input`` () =
    Assert.Equal(
        {
            Code = ExitCode.QdkOutOfDate |> int
            Out = ""
            Error =
                standardizeNewLines
                    "Error: Qdk Version is out of date. Only Qdk version 0.16.2104.138035 or later is supported.
"
        },
        run [ "update"; "-p"; "Examples\\TestProjects\\OldVersion\\OldVersion.csproj" ] ""
    )

[<Fact>]
let ``Outdated system project file as input`` () =
    let project = "Examples\\TestProjects\\OldApplication\\OldApplication.csproj"
    let result = run [ "update"; "-p"; project ] ""
    Assert.Equal(ExitCode.UnhandledException |> int, result.Code)
    Assert.Equal("", result.Out)

    Assert.True(
        result.Error.StartsWith(
            sprintf
                "Unexpected Error: System.Exception: The given project file, %s is not a Q# project file. Please ensure your project file uses the Microsoft.Quantum.Sdk."
                project
        )
    )
