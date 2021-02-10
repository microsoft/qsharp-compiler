// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.CommandLineTests

open System
open System.IO
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CommandLineCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.ReservedKeywords
open Xunit


let private pathRoot = Path.GetPathRoot(Directory.GetCurrentDirectory())

let private parentDir = Path.GetDirectoryName(Directory.GetCurrentDirectory())

let private testOne expected args =
    let result = Program.Main args
    Assert.Equal(expected, result)

let private testInput expected args =
    [ [| "diagnose" |]; [| "build"; "-o"; "outputFolder" |] ]
    |> List.iter (fun v -> Array.append v args |> testOne expected)

let private testSnippet expected args =
    [ [| "diagnose" |]; [| "build" |] ] |> List.iter (fun v -> Array.append v args |> testOne expected)

[<Fact>]
let ``valid snippet`` () =
    [| "-s"; "let a = 0;"; "-v n" |] |> testSnippet ReturnCode.Success

[<Fact>]
let ``invalid snippet`` () =
    [| "-s"; "let a = " |] |> testSnippet ReturnCode.CompilationErrors


[<Fact>]
let ``one valid file`` () =
    [|
        "-i"
        ("TestCases", "General.qs") |> Path.Combine
        "--verbosity"
        "Diagnostic"
    |]
    |> testInput ReturnCode.Success

[<Fact>]
let ``multiple valid file`` () =
    [|
        "--input"
        ("TestCases", "General.qs") |> Path.Combine
        ("TestCases", "LinkingTests", "Core.qs") |> Path.Combine
    |]
    |> testInput ReturnCode.Success


[<Fact>]
let ``one invalid file`` () =
    [| "-i"; ("TestCases", "TypeChecking.qs") |> Path.Combine |]
    |> testInput ReturnCode.CompilationErrors


[<Fact>]
let ``mixed files`` () =
    [|
        "-i"
        ("TestCases", "LinkingTests", "Core.qs") |> Path.Combine
        ("TestCases", "TypeChecking.qs") |> Path.Combine
    |]
    |> testInput ReturnCode.CompilationErrors


[<Fact>]
let ``missing file`` () =
    [| "-i"; ("TestCases", "NonExistent.qs") |> Path.Combine |] |> testInput ReturnCode.UnresolvedFiles

    [|
        "-i"
        ("TestCases", "LinkingTests", "Core.qs") |> Path.Combine
        ("TestCases", "NonExistent.qs") |> Path.Combine
    |]
    |> testInput ReturnCode.UnresolvedFiles


[<Fact>]
let ``invalid argument`` () =
    [| "-i"; ("TestCases", "General.qs") |> Path.Combine; "--foo" |]
    |> testInput ReturnCode.InvalidArguments


[<Fact>]
let ``missing verb`` () =
    let args = [| "-i"; ("TestCases", "General.qs") |> Path.Combine |]
    let result = Program.Main args
    Assert.Equal(ReturnCode.InvalidArguments, result)


[<Fact>]
let ``invalid verb`` () =
    let args = [| "foo"; "-i"; ("TestCases", "General.qs") |> Path.Combine |]
    let result = Program.Main args
    Assert.Equal(ReturnCode.InvalidArguments, result)


[<Fact>]
let ``diagnose outputs`` () =
    let args =
        [|
            "-i"
            ("TestCases", "General.qs") |> Path.Combine
            "--tree"
            "--tokenization"
            "--text"
            "--code"
        |]

    let result = Program.Main args
    Assert.Equal(ReturnCode.InvalidArguments, result)


[<Fact>]
let ``options from response files`` () =
    let configFile = ("TestCases", "qsc-config.txt") |> Path.Combine
    let configArgs = [| "-i"; ("TestCases", "LinkingTests", "Core.qs") |> Path.Combine |]
    File.WriteAllText(configFile, String.Join(" ", configArgs))

    let commandLineArgs =
        [|
            "build"
            "-v"
            "Detailed"
            "--format"
            "MsBuild"
            "--response-files"
            configFile
        |]

    let result = Program.Main commandLineArgs
    Assert.Equal(ReturnCode.Success, result)


[<Fact>]
let ``execute rewrite steps only if validation passes`` () =
    let source1 = ("TestCases", "LinkingTests", "Core.qs") |> Path.Combine
    let source2 = ("TestCases", "AttributeGeneration.qs") |> Path.Combine

    let config =
        CompilationLoader.Configuration
            (GenerateFunctorSupport = true, BuildOutputFolder = null, RuntimeCapability = BasicQuantumFunctionality)

    let loadSources (loader: Func<_ seq, _>) = loader.Invoke([ source1; source2 ])

    let loaded =
        new CompilationLoader(new CompilationLoader.SourceLoader(loadSources), Seq.empty, new Nullable<_>(config))

    Assert.Equal(CompilationLoader.Status.Succeeded, loaded.SourceFileLoading)
    Assert.Equal(CompilationLoader.Status.Succeeded, loaded.ReferenceLoading)
    Assert.Equal(CompilationLoader.Status.Succeeded, loaded.Validation)
    Assert.Equal(CompilationLoader.Status.Succeeded, loaded.FunctorSupport)
    Assert.Equal(CompilationLoader.Status.NotRun, loaded.Monomorphization) // no entry point

    let loadSources (loader: Func<_ seq, _>) = loader.Invoke([ source2 ])

    let loaded =
        new CompilationLoader(new CompilationLoader.SourceLoader(loadSources), Seq.empty, new Nullable<_>(config))

    Assert.Equal(CompilationLoader.Status.Succeeded, loaded.SourceFileLoading)
    Assert.Equal(CompilationLoader.Status.Succeeded, loaded.ReferenceLoading)
    Assert.Equal(CompilationLoader.Status.Failed, loaded.Validation)
    Assert.Equal(CompilationLoader.Status.NotRun, loaded.FunctorSupport)
    Assert.Equal(CompilationLoader.Status.NotRun, loaded.Monomorphization)

[<Fact>]
let ``find path relative`` () =
    let fullPath = Path.Combine(Path.GetFullPath "alpha", "beta", "c", "test-path.qs")
    let options = new BuildCompilation.BuildOptions()
    let id = CompilationUnitManager.GetFileId(new Uri(fullPath))
    let expected = Path.Combine(Path.GetFullPath "alpha", "beta", "c", "test-path.g.cs")
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)

[<Fact>]
let ``find path relative to outputfolder`` () =
    let fullPath = Path.Combine(Path.GetFullPath "alpha", "beta", "c", "test-path.qs")
    let options = new BuildCompilation.BuildOptions()
    options.OutputFolder <- Path.Combine(pathRoot, "foo", "bar")
    let id = CompilationUnitManager.GetFileId(new Uri(fullPath))
    let expected = Path.Combine(pathRoot, "foo", "bar", "alpha", "beta", "c", "test-path.g.cs")
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)

[<Fact>]
let ``find path relative to relative outputfolder`` () =
    let fullPath = Path.Combine(Path.GetFullPath "alpha", "beta", "c", "test-path.qs")
    let options = new BuildCompilation.BuildOptions()
    options.OutputFolder <- Path.Combine("..", "foo", "bar")
    let id = CompilationUnitManager.GetFileId(new Uri(fullPath))
    let expected = Path.Combine(parentDir, "foo", "bar", "alpha", "beta", "c", "test-path.g.cs")
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)

[<Fact>]
let ``find path absolute`` () =
    let fileName = Path.Combine(pathRoot, "alpha", "beta", "c", "test-path.qs")
    let fullPath = Path.GetFullPath fileName
    let options = new BuildCompilation.BuildOptions()
    let id = CompilationUnitManager.GetFileId(new Uri(fullPath))
    let expected = Path.GetFullPath "test-path.g.cs"
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)

[<Fact>]
let ``find path absolute to outputfolder`` () =
    let fullPath = Path.Combine(pathRoot, "alpha", "beta", "c", "test-path.qs")
    let options = new BuildCompilation.BuildOptions()
    options.OutputFolder <- Path.Combine(pathRoot, "foo", "bar")
    let id = CompilationUnitManager.GetFileId(new Uri(fullPath))
    let expected = Path.Combine(pathRoot, "foo", "bar", "test-path.g.cs")
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)

[<Fact>]
let ``find path relative to here`` () =
    let fullPath = Path.Combine(Path.GetFullPath "alpha", "beta", "c", "test-path.qs")
    let options = new BuildCompilation.BuildOptions()
    let id = CompilationUnitManager.GetFileId(new Uri(fullPath))
    let expected = Path.Combine(Path.GetFullPath "alpha", "beta", "c", "test-path.g.cs")
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)

[<Fact>]
let ``find path relative with spaces`` () =
    let fullPath = Path.Combine(Path.GetFullPath "alpha", "some beta", "c", "test 00.qs")
    let options = new BuildCompilation.BuildOptions()
    let id = CompilationUnitManager.GetFileId(new Uri(fullPath))
    let expected = Path.Combine(Path.GetFullPath "alpha", "some beta", "c", "test 00.g.cs")
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)

[<Fact>]
let ``find path relative to outputfolder with spaces`` () =
    let fullPath = Path.Combine(Path.GetFullPath "alpha", "some beta", "c", "test 00.qs")
    let options = new BuildCompilation.BuildOptions()
    options.OutputFolder <- Path.Combine(pathRoot, "foo", "some bar")
    let id = CompilationUnitManager.GetFileId(new Uri(fullPath))

    let expected =
        Path.GetFullPath(Path.Combine(pathRoot, "foo", "some bar", "alpha", "some beta", "c", "test 00.g.cs"))

    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)

[<Fact>]
let ``find path relative to relative outputfolder with spaces`` () =
    let fullPath = Path.Combine(Path.GetFullPath "alpha", "some beta", "c", "test 00.qs")
    let options = new BuildCompilation.BuildOptions()
    options.OutputFolder <- Path.Combine("..", "some foo", "bar")
    let id = CompilationUnitManager.GetFileId(new Uri(fullPath))
    let expected = Path.Combine(parentDir, "some foo", "bar", "alpha", "some beta", "c", "test 00.g.cs")
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)

[<Fact>]
let ``find path absolute with spaces`` () =
    let fullPath = Path.Combine(pathRoot, "alpha", "some beta", "c", "test 02.qs")
    let options = new BuildCompilation.BuildOptions()
    let id = CompilationUnitManager.GetFileId(new Uri(fullPath))
    let expected = Path.GetFullPath "test 02.g.cs"
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)

[<Fact>]
let ``find path absolute to outputfolder with spaces`` () =
    let fullPath = Path.Combine(pathRoot, "alpha", "some beta", "c", "test 02.qs")
    let options = new BuildCompilation.BuildOptions()
    options.OutputFolder <- Path.Combine(pathRoot, "foo", "some bar")
    let id = CompilationUnitManager.GetFileId(new Uri(fullPath))
    let expected = Path.Combine(pathRoot, "foo", "some bar", "test 02.g.cs")
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)

[<Fact>]
let ``find path relative to here with spaces`` () =
    let fullPath = Path.Combine(Path.GetFullPath "alpha", "some beta", "c", "test 03.qs")
    let options = new BuildCompilation.BuildOptions()
    let id = CompilationUnitManager.GetFileId(new Uri(fullPath))
    let expected = Path.Combine(Path.GetFullPath "alpha", "some beta", "c", "test 03.g.cs")
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)
