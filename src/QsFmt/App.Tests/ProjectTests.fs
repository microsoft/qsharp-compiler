// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.ProjectTests

open System.IO
open Microsoft.Quantum.QsFmt.App
open Xunit

[<Fact>]
let SimpleApplication () =
    DesignTimeBuild.assemblyLoadContextSetup ()

    let files, version =
        DesignTimeBuild.getSourceFiles "Examples\TestProjects\SimpleApplication\QSharpApplication1.csproj"

    let files = files |> List.map Path.GetFullPath

    let expectedFiles =
        [
            "Examples\TestProjects\SimpleApplication\Program.qs"
            "Examples\TestProjects\SimpleApplication\Included1.qs"
            "Examples\TestProjects\SimpleApplication\SubFolder1\Included2.qs"
            "Examples\TestProjects\SimpleApplication\SubFolder2\Included3.qs"
            "Examples\TestProjects\SimpleApplication\SubFolder1\SubSubFolder\Included4.qs"
            "Examples\Example1.qs"
            "Examples\Example2.qs"
        ]
        |> List.map Path.GetFullPath

    Assert.True(expectedFiles.Length = files.Length, "Didn't get the expected number of files.")

    for expected in expectedFiles do
        Assert.True(List.contains expected files, sprintf "Expected but did not find file %s" expected)

[<Fact>]
let SimpleLibrary () =
    DesignTimeBuild.assemblyLoadContextSetup ()

    let files, version =
        DesignTimeBuild.getSourceFiles "Examples\TestProjects\SimpleLibrary\QSharpLibrary1.csproj"

    let files = files |> List.map Path.GetFullPath

    let expectedFiles =
        [
            "Examples\TestProjects\SimpleLibrary\Library.qs"
            "Examples\TestProjects\SimpleLibrary\Included1.qs"
            "Examples\TestProjects\SimpleLibrary\SubFolder1\Included2.qs"
            "Examples\TestProjects\SimpleLibrary\SubFolder2\Included3.qs"
            "Examples\TestProjects\SimpleLibrary\SubFolder1\SubSubFolder\Included4.qs"
            "Examples\Example1.qs"
            "Examples\Example2.qs"
        ]
        |> List.map Path.GetFullPath

    Assert.True(expectedFiles.Length = files.Length, "Didn't get the expected number of files.")

    for expected in expectedFiles do
        Assert.True(List.contains expected files, sprintf "Expected but did not find file %s" expected)

[<Fact>]
let SimpleTestProject () =
    DesignTimeBuild.assemblyLoadContextSetup ()

    let files, version =
        DesignTimeBuild.getSourceFiles "Examples\TestProjects\SimpleTestProject\QSharpTestProject1.csproj"

    let files = files |> List.map Path.GetFullPath

    let expectedFiles =
        [
            "Examples\TestProjects\SimpleTestProject\Tests.qs"
            "Examples\TestProjects\SimpleTestProject\Included1.qs"
            "Examples\TestProjects\SimpleTestProject\SubFolder1\Included2.qs"
            "Examples\TestProjects\SimpleTestProject\SubFolder2\Included3.qs"
            "Examples\TestProjects\SimpleTestProject\SubFolder1\SubSubFolder\Included4.qs"
            "Examples\Example1.qs"
            "Examples\Example2.qs"
        ]
        |> List.map Path.GetFullPath

    Assert.True(expectedFiles.Length = files.Length, "Didn't get the expected number of files.")

    for expected in expectedFiles do
        Assert.True(List.contains expected files, sprintf "Expected but did not find file %s" expected)
