// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.CommandLineTests

open System
open System.IO
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CommandLineCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Xunit

let private pathRoot = 
    Path.GetPathRoot(Directory.GetCurrentDirectory())

let private parentDir = 
    Path.GetDirectoryName(Directory.GetCurrentDirectory())

let private testOne expected args = 
    let result = Program.Main args
    Assert.Equal(expected, result)

let private testInput expected args =
    [
        [|"diagnose"|]
        [|"build"; "-o"; "outputFolder"|]
    ] 
    |> List.iter (fun v -> Array.append v args |> testOne expected) 

let private testSnippet expected args = 
    [
        [|"diagnose"|]
        [|"build"|]
    ] 
    |> List.iter (fun v -> Array.append v args |> testOne expected) 

[<Fact>]
let ``valid snippet`` () =
    [|
        "-s" 
        "let a = 0;" 
        "-v"
    |] 
    |> testSnippet ReturnCode.SUCCESS

[<Fact>]
let ``invalid snippet`` () =
    [|
        "-s" 
        "let a = " 
    |]
    |> testSnippet ReturnCode.COMPILATION_ERRORS

    
[<Fact>]
let ``one valid file`` () =
    [|
        "-i"
        ("TestFiles","test-00.qs") |> Path.Combine
        "-v"
    |]
    |> testInput ReturnCode.SUCCESS
    
[<Fact>]
let ``multiple valid file`` () =
    [|
        "--input" 
        ("TestFiles","test-00.qs") |> Path.Combine
        ("TestFiles","test-01.qs") |> Path.Combine
    |]
    |> testInput ReturnCode.SUCCESS

    
[<Fact>]
let ``one invalid file`` () =
    [|
        "-i" 
        ("TestFiles","test-02.qs") |> Path.Combine
    |]
    |> testInput ReturnCode.COMPILATION_ERRORS


[<Fact>]
let ``mixed files`` () =
    [|
        "-i" 
        ("TestFiles","test-01.qs") |> Path.Combine
        ("TestFiles","test-02.qs") |> Path.Combine
    |]
    |> testInput ReturnCode.COMPILATION_ERRORS


[<Fact>]
let ``missing file`` () =
    [|
        "-i"
        ("TestFiles","foo-00.qs") |> Path.Combine
    |]
    |> testInput ReturnCode.UNRESOLVED_FILES
    
    [|
        "-i"
        ("TestFiles","test-01.qs") |> Path.Combine
        ("TestFiles","foo-00.qs") |> Path.Combine
    |]
    |> testInput ReturnCode.UNRESOLVED_FILES

    
[<Fact>]
let ``invalid argument`` () =
    [|
        "-i"
        ("TestFiles","test-00.qs") |> Path.Combine
        "--foo"
    |]
    |> testInput ReturnCode.INVALID_ARGUMENTS
        
        
[<Fact>]
let ``missing verb`` () =
    let args = 
        [|
            "-i"
            ("TestFiles","test-00.qs") |> Path.Combine
        |]        
    let result = Program.Main args
    Assert.Equal(ReturnCode.INVALID_ARGUMENTS, result)

    
[<Fact>]
let ``invalid verb`` () =
    let args = 
        [|
            "foo"
            "-i"
            ("TestFiles","test-00.qs") |> Path.Combine
        |]
    let result = Program.Main args
    Assert.Equal(ReturnCode.INVALID_ARGUMENTS, result)
    

[<Fact>]
let ``diagnose outputs`` () =
    let args = 
        [|
            "-i"
            ("TestFiles","test-00.qs") |> Path.Combine
            "--tree"
            "--tokenization"
            "--text"
            "--code"
        |]        
    let result = Program.Main args
    Assert.Equal(ReturnCode.INVALID_ARGUMENTS, result)


[<Fact>]
let ``generate docs`` () =
    let docsFolder = ("TestFiles", "docs.Out") |> Path.Combine
    for file in Directory.GetFiles docsFolder do
        File.Delete file

    let toc = Path.Combine (docsFolder, "toc.yml") 
    let nsDoc = Path.Combine (docsFolder, "Compiler.Tests.yml")
    let opDoc = Path.Combine (docsFolder, "compiler.tests.test01.yml")
    let existsAndNotEmpty fileName = fileName |> File.Exists && not (File.ReadAllText fileName |> String.IsNullOrWhiteSpace)
    let args = 
        [|
            "build"
            "--input" 
            ("TestFiles","test-01.qs") |> Path.Combine
            "--doc"
            docsFolder
        |]

    let result = Program.Main args
    Assert.Equal(ReturnCode.SUCCESS, result) 
    Assert.True (existsAndNotEmpty toc)
    Assert.True (existsAndNotEmpty nsDoc)
    Assert.True (existsAndNotEmpty opDoc)

    
[<Fact>]
let ``find path relative`` () =
    let fullPath = Path.Combine (Path.GetFullPath "alpha","beta","c","test-00.qs")
    let options = new BuildCompilation.BuildOptions()
    let id = CompilationUnitManager.TryGetFileId (new Uri(fullPath)) |> snd
    let expected = Path.Combine (Path.GetFullPath "alpha","beta","c","test-00.g.cs")
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)
    
[<Fact>]
let ``find path relative to outputfolder`` () =
    let fullPath = Path.Combine(Path.GetFullPath "alpha","beta","c","test-00.qs")
    let options = new BuildCompilation.BuildOptions()
    options.OutputFolder <- Path.Combine (pathRoot,"foo","bar")
    let id = CompilationUnitManager.TryGetFileId (new Uri(fullPath)) |> snd
    let expected = Path.Combine (pathRoot, "foo", "bar", "alpha", "beta", "c", "test-00.g.cs")
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)
    
[<Fact>]
let ``find path relative to relative outputfolder`` () =
    let fullPath = Path.Combine (Path.GetFullPath "alpha","beta","c","test-00.qs")
    let options = new BuildCompilation.BuildOptions()
    options.OutputFolder <- Path.Combine("..","foo","bar")
    let id = CompilationUnitManager.TryGetFileId (new Uri(fullPath)) |> snd
    let expected = Path.Combine (parentDir,"foo","bar","alpha","beta","c","test-00.g.cs")
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)
    
[<Fact>]
let ``find path absolute`` () =
    let fileName = Path.Combine (pathRoot,"alpha","beta","c","test-02.qs")
    let fullPath = Path.GetFullPath fileName
    let options = new BuildCompilation.BuildOptions()
    let id = CompilationUnitManager.TryGetFileId (new Uri(fullPath)) |> snd
    let expected = Path.GetFullPath "test-02.g.cs"
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)
   
[<Fact>]
let ``find path absolute to outputfolder`` () =
    let fullPath = Path.Combine (pathRoot, "alpha","beta","c","test-02.qs")
    let options = new BuildCompilation.BuildOptions()
    options.OutputFolder <- Path.Combine (pathRoot, "foo","bar")
    let id = CompilationUnitManager.TryGetFileId (new Uri(fullPath)) |> snd
    let expected = Path.Combine (pathRoot, "foo","bar", "test-02.g.cs")
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)

[<Fact>]
let ``find path relative to here`` () =
    let fullPath = Path.Combine (Path.GetFullPath "alpha","beta","c","test-03.qs")
    let options = new BuildCompilation.BuildOptions()
    let id = CompilationUnitManager.TryGetFileId (new Uri(fullPath)) |> snd
    let expected = Path.Combine (Path.GetFullPath "alpha","beta","c","test-03.g.cs")
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)

[<Fact>]
let ``find path relative with spaces`` () =
    let fullPath = Path.Combine (Path.GetFullPath "alpha", "some beta", "c", "test 00.qs")
    let options = new BuildCompilation.BuildOptions()
    let id = CompilationUnitManager.TryGetFileId (new Uri(fullPath)) |> snd
    let expected = Path.Combine (Path.GetFullPath "alpha","some beta","c","test 00.g.cs")
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)
    
[<Fact>]
let ``find path relative to outputfolder with spaces`` () =
    let fullPath = Path.Combine (Path.GetFullPath "alpha","some beta","c","test 00.qs")
    let options = new BuildCompilation.BuildOptions()
    options.OutputFolder <- Path.Combine (pathRoot, "foo", "some bar")
    let id = CompilationUnitManager.TryGetFileId (new Uri(fullPath)) |> snd
    let expected = Path.GetFullPath (Path.Combine (pathRoot, "foo","some bar","alpha","some beta","c","test 00.g.cs"))
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)
    
[<Fact>]
let ``find path relative to relative outputfolder with spaces`` () =
    let fullPath = Path.Combine (Path.GetFullPath "alpha","some beta","c","test 00.qs")
    let options = new BuildCompilation.BuildOptions()
    options.OutputFolder <- Path.Combine ("..","some foo","bar")
    let id = CompilationUnitManager.TryGetFileId (new Uri(fullPath)) |> snd
    let expected = Path.Combine (parentDir,"some foo","bar","alpha","some beta","c","test 00.g.cs")
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)
    
[<Fact>]
let ``find path absolute with spaces`` () =
    let fullPath = Path.Combine (pathRoot, "alpha","some beta","c","test 02.qs")
    let options = new BuildCompilation.BuildOptions()
    let id = CompilationUnitManager.TryGetFileId (new Uri(fullPath)) |> snd
    let expected = Path.GetFullPath "test 02.g.cs"
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)
   
[<Fact>]
let ``find path absolute to outputfolder with spaces`` () =
    let fullPath = Path.Combine (pathRoot, "alpha", "some beta", "c", "test 02.qs")
    let options = new BuildCompilation.BuildOptions()
    options.OutputFolder <- Path.Combine(pathRoot, "foo", "some bar")
    let id = CompilationUnitManager.TryGetFileId (new Uri(fullPath)) |> snd
    let expected = Path.Combine (pathRoot, "foo", "some bar", "test 02.g.cs")
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)

[<Fact>]
let ``find path relative to here with spaces`` () =
    let fullPath = Path.Combine (Path.GetFullPath "alpha","some beta","c","test 03.qs")
    let options = new BuildCompilation.BuildOptions()
    let id = CompilationUnitManager.TryGetFileId (new Uri(fullPath)) |> snd
    let expected = Path.Combine (Path.GetFullPath "alpha","some beta","c","test 03.g.cs")
    let actual = CompilationLoader.GeneratedFile(id, options.OutputFolder, ".g.cs")
    Assert.Equal(expected, actual)
