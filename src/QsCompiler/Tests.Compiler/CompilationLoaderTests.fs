// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open System.IO
open Xunit
open Xunit.Abstractions
open Microsoft.VisualStudio.LanguageServer.Protocol

type CompilationLoaderTests(output: ITestOutputHelper) =

    /// The path to the Q# file that contains the test cases.
    let testFile = Path.Combine("TestCases", "CompilationLoader.qs")

    /// <summary>
    /// A map where each key is a test case name, and each value is the source code of the test case.
    /// </summary>
    /// <remarks>
    /// Each test case corresponds to a section from <see cref="testFile"/>, separated by "// ---". The text immediately
    /// after "// ---" until the end of the line is the name of the test case.
    /// </remarks>
    let testCases =
        File.ReadAllText testFile
        |> fun text -> Environment.NewLine + "// ---" |> text.Split
        |> Seq.map (fun case ->
            let parts = case.Split(Environment.NewLine, 2)
            parts.[0].Trim(), parts.[1])
        |> Map.ofSeq

    /// <summary>
    /// Compiles a snippet of Q# source code.
    /// </summary>
    let compileQSharp source =
        use compilationManager = new CompilationUnitManager()

        let fileManager uri content =
            CompilationUnitManager.InitializeFileManager(uri, content)

        // Create and add files to the compilation.
        let corePath = Path.GetFullPath(Path.Combine("TestCases", "LinkingTests", "Core.qs"))

        fileManager (new Uri(corePath)) (File.ReadAllText corePath)
        |> compilationManager.AddOrUpdateSourceFileAsync
        |> ignore

        let sourceUri = new Uri(Path.GetFullPath(Path.GetRandomFileName()))
        fileManager sourceUri source |> compilationManager.AddOrUpdateSourceFileAsync |> ignore

        let compilation = compilationManager.Build()

        let errors =
            compilation.Diagnostics()
            |> Seq.filter (fun diagnostic -> diagnostic.Severity = DiagnosticSeverity.Error)

        Assert.Empty errors
        compilation.BuiltCompilation

    /// <summary>
    /// Attempts to write a Q# compilation to its binary representation and then to create a Q# compilation from the written binary represenation.
    /// </summary>
    let verifyBinaryWriteRead compilation =
        use stream = new MemoryStream()
        let writeSuccessful = CompilationLoader.WriteBinary(compilation, stream)
        let mutable readSuccessful = false

        if writeSuccessful then
            stream.Position <- 0L
            let mutable qsCompilation = Unchecked.defaultof<SyntaxTree.QsCompilation>
            readSuccessful <- CompilationLoader.ReadBinary(stream, &qsCompilation)

        (writeSuccessful, readSuccessful)

    [<Theory>]
    [<InlineData("Control Flow")>]
    [<InlineData("Declarations")>]
    [<InlineData("Functions")>]
    [<InlineData("Loops")>]
    [<InlineData("Mutable")>]
    [<InlineData("Operations")>]
    [<InlineData("Qubit Usage")>]
    [<InlineData("Tuple Deconstruction")>]
    [<InlineData("Update And Reassign")>]
    [<InlineData("User Defined Types")>]
    member this.``Write Read Binary`` testCaseName =
        let testSource = testCases |> Map.find testCaseName
        let compilation = compileQSharp testSource
        let writeSuccessful, readSuccessful = verifyBinaryWriteRead compilation
        Assert.True(writeSuccessful)
        Assert.True(readSuccessful)
