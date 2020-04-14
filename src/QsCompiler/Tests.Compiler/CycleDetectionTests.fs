// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System
open System.IO
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations
open Xunit


type CycleDetectionTests () =

    let compilationManager = new CompilationUnitManager(new Action<Exception> (fun ex -> failwith ex.Message))

    let getTempFile () = new Uri(Path.GetFullPath(Path.GetRandomFileName()))
    let getManager uri content = CompilationUnitManager.InitializeFileManager(uri, content, compilationManager.PublishDiagnostics, compilationManager.LogException)

    let ReadAndChunkSourceFile fileName =
        let sourceInput = Path.Combine ("TestCases", fileName) |> File.ReadAllText
        sourceInput.Split ([|"==="|], StringSplitOptions.RemoveEmptyEntries)

    let BuildContent content =

        let fileId = getTempFile()
        let file = getManager fileId content

        compilationManager.AddOrUpdateSourceFileAsync(file) |> ignore
        let compilationDataStructures = compilationManager.Build()
        compilationManager.TryRemoveSourceFileAsync(fileId, false) |> ignore

        compilationDataStructures.Diagnostics() |> Seq.exists (fun d -> d.IsError()) |> Assert.False
        Assert.NotNull compilationDataStructures.BuiltCompilation

        compilationDataStructures

    let CompileCycleDetectionTest testNumber =
        let srcChunks = ReadAndChunkSourceFile "CycleDetection.qs"
        srcChunks.Length >= testNumber |> Assert.True
        let compilationDataStructures = BuildContent srcChunks.[testNumber-1]
        let callGraph = BuildCallGraph.Apply compilationDataStructures.BuiltCompilation
        Assert.NotNull callGraph
        callGraph.GetCallCycles ()

    let DecorateWithNamespace (ns : string) (input : string list list) =
        List.map (List.map (fun name -> { Namespace = NonNullable<_>.New ns; Name = NonNullable<_>.New name})) input

    let CyclicEquivalence lst1 lst2 =
        let size = List.length lst1
        if size <> List.length lst2 then
            false
        else
            let mutable i = 0
            let mutable j = 0
            let mutable k = 0
            let mutable rtrn = false
            while not rtrn && i < size && j < size do
                k <- 1
                while k <= size && lst1.[(i + k) % size] = lst2.[(j + k) % size] do
                    k <- k + 1
                if k > size then
                    rtrn <- true
                if lst1.[(i + k) % size] > lst2.[(j + k) % size] then
                    i <- i + k
                else
                    j <- j + k
            rtrn

    let CheckForExpectedCycles (actualCycles: seq<#seq<CallGraphNode>>) expectedCycles =
        let expected = expectedCycles |> DecorateWithNamespace Signatures.CycleDetection

        let actual = actualCycles |> (Seq.map ((Seq.map (fun x -> x.CallableName)) >> Seq.toList) >> Seq.toList)

        Assert.True(actual.Length = expected.Length,
            sprintf "Expected call graph to have %i cycle(s), but found %i cycle(s)" expected.Length actual.Length)

        for cycle in expected do
            Assert.True(List.exists (fun x -> CyclicEquivalence cycle x) actual,
                sprintf "Did not find one of the expected cycles") // ToDo: better error message here

    [<Fact>]
    member this.``No Cycles`` () =
        let cycles = CompileCycleDetectionTest 1
        Assert.Empty cycles

    [<Fact>]
    member this.``Simple Cycle`` () =
        let result = CompileCycleDetectionTest 2

        [
            [ "Foo"; "Bar" ]
        ]
        |> CheckForExpectedCycles result

    [<Fact>]
    member this.``Longer Cycle`` () =
        let result = CompileCycleDetectionTest 3

        [
            [ "Foo"; "Bar"; "Baz" ]
        ]
        |> CheckForExpectedCycles result

    [<Fact>]
    member this.``Direct Recursion Cycle`` () =
        let result = CompileCycleDetectionTest 4

        [
            [ "Foo" ]
        ]
        |> CheckForExpectedCycles result

    [<Fact>]
    member this.``Loop In Sequence`` () =
        let result = CompileCycleDetectionTest 5

        [
            [ "Bar" ]
        ]
        |> CheckForExpectedCycles result

    [<Fact>]
    member this.``Figure-Eight Cycles`` () =
        let result = CompileCycleDetectionTest 6

        [
            [ "Foo"; "Bar" ]
            [ "Foo"; "Baz" ]
        ]
        |> CheckForExpectedCycles result

    [<Fact(Skip="Cycle detection does not work here yet")>]
    member this.``Fully Connected Cycles`` () =
        let result = CompileCycleDetectionTest 7

        [
            [ "Foo" ]
            [ "Bar" ]
            [ "Baz" ]
            [ "Foo"; "Bar" ]
            [ "Foo"; "Baz" ]
            [ "Bar"; "Baz" ]
            [ "Foo"; "Bar"; "Baz" ]
            [ "Baz"; "Bar"; "Foo" ]
        ]
        |> CheckForExpectedCycles result