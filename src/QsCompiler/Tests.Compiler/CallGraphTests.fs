// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.IO
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations
open Xunit
open Xunit.Abstractions


type CallGraphTests (output:ITestOutputHelper) =

    let qualifiedName name =
        ("NS" |> NonNullable<string>.New, name |> NonNullable<string>.New) |> QsQualifiedName.New

    let typeParameter (id : string) =
        let pieces = id.Split(".")
        Assert.True(pieces.Length = 2)
        let parent = qualifiedName pieces.[0]
        let name = pieces.[1] |> NonNullable<string>.New
        QsTypeParameter.New (parent, name, Null)

    let Foo  = qualifiedName "Foo"
    let Bar  = qualifiedName "Bar"

    let FooA = typeParameter "Foo.A"
    let FooB = typeParameter "Foo.B"
    let BarA = typeParameter "Bar.A"
    let BarC = typeParameter "Bar.C"
    let BazA = typeParameter "Baz.A"

    let compilationManager = new CompilationUnitManager(new Action<Exception> (fun ex -> failwith ex.Message))

    let getTempFile () = new Uri(Path.GetFullPath(Path.GetRandomFileName()))
    let getManager uri content = CompilationUnitManager.InitializeFileManager(uri, content, compilationManager.PublishDiagnostics, compilationManager.LogException)

    let DecorateWithNamespace (ns : string) (input : string list list) =
        List.map (List.map (fun name -> { Namespace = NonNullable<_>.New ns; Name = NonNullable<_>.New name })) input

    let resolution (res : (QsTypeParameter * QsTypeKind<_,_,_,_>) list) =
        res.ToImmutableDictionary((fun (tp,_) -> tp.Origin, tp.TypeName), snd >> ResolvedType.New)

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

    /// Checks if one of the given lists can be rotated into the other given list
    let CyclicEquivalence lst1 lst2 =
        let size1 = List.length lst1
        if size1 <> List.length lst2 then
            false
        else
            let rotate n = lst1 |> List.permute (fun index -> (index + n) % size1)
            let rotations = [0 .. size1 - 1] |> List.map rotate
            List.contains lst2 rotations

    let CheckForExpectedCycles (actualCycles: seq<#seq<CallGraphNode>>) expectedCycles =
        let expected = expectedCycles |> DecorateWithNamespace Signatures.CycleDetectionNS

        let actual = actualCycles |> (Seq.map ((Seq.map (fun x -> x.CallableName)) >> Seq.toList) >> Seq.toList)

        Assert.True(actual.Length = expected.Length,
            sprintf "Expected call graph to have %i cycle(s), but found %i cycle(s)" expected.Length actual.Length)

        let cycleToString (cycle : QsQualifiedName list) =
            String.Join(" -> ", List.map (fun node -> node.ToString()) cycle)

        for cycle in expected do
            Assert.True(List.exists (CyclicEquivalence cycle) actual,
                sprintf "Did not find expected cycle: %s" (cycleToString cycle))

    static member CheckResolution (parent, expected : IDictionary<_,_>, [<ParamArray>] resolutions) =
        let expectedKeys = ImmutableHashSet.CreateRange(expected.Keys)
        let compareWithExpected (d : ImmutableDictionary<_,_>) =
            let keysMismatch = expectedKeys.SymmetricExcept d.Keys
            keysMismatch.Count = 0 && expected |> Seq.exists (fun kv -> d.[kv.Key] <> kv.Value) |> not
        let mutable combined = ImmutableDictionary.Empty
        let success = CallGraph.TryCombineTypeResolutions(parent, &combined, resolutions)
        Assert.True(compareWithExpected combined, "combined resolutions did not match the expected ones")
        success

    static member AssertResolution (parent, expected, [<ParamArray>] resolutions) =
        let success = CallGraphTests.CheckResolution (parent, expected, resolutions)
        Assert.True(success, "Combining type resolutions was not successful.")

    static member AssertResolutionFailure (parent, expected, [<ParamArray>] resolutions) =
        let success = CallGraphTests.CheckResolution (parent, expected, resolutions)
        Assert.False(success, "Combining type resolutions should have failed.")


    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Resolution to Concrete`` () =

        let res1 = resolution [
            (FooA, BarA |> TypeParameter)
            (FooB, Int)
        ]
        let res2 = resolution [
            (BarA, Int)
        ]
        let expected = resolution [
            (FooA, Int)
            (FooB, Int)
        ]
        CallGraphTests.AssertResolution(Foo, expected, res1, res2)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Resolution to Type Parameter`` () =

        let res1 = resolution [
            (FooA, BarA |> TypeParameter)
        ]
        let res2 = resolution [
            (BarA, BazA |> TypeParameter)
        ]
        let expected = resolution [
            (FooA, BazA |> TypeParameter)
        ]
        CallGraphTests.AssertResolution(Foo, expected, res1, res2)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Resolution via Identity Mapping`` () =

        let res1 = resolution [
            (FooA, FooA |> TypeParameter)
        ]
        let res2 = resolution [
            (FooA, Int)
        ]
        let expected = resolution [
            (FooA, Int)
        ]
        CallGraphTests.AssertResolution(Foo, expected, res1, res2)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Multi-Stage Resolution`` () =

        let res1 = resolution [
            (FooA, BarA |> TypeParameter)
        ]
        let res2 = resolution [
            (BarA, Int)
            (FooB, Int)
        ]
        let expected = resolution [
            (FooA, Int)
            (FooB, Int)
        ]
        CallGraphTests.AssertResolution(Foo, expected, res1, res2)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Multiple Resolutions to Concrete`` () =

        let res1 = resolution [
            (FooA, BarA |> TypeParameter)
            (FooB, BarA |> TypeParameter)
        ]
        let res2 = resolution [
            (BarA, Int)
        ]
        let expected = resolution [
            (FooA, Int)
            (FooB, Int)
        ]
        CallGraphTests.AssertResolution(Foo, expected, res1, res2)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Multiple Resolutions to Type Parameter`` () =

        let res1 = resolution [
            (FooA, BarA |> TypeParameter)
            (FooB, BarA |> TypeParameter)
        ]
        let res2 = resolution [
            (BarA, BazA |> TypeParameter)
        ]
        let res3 = resolution [
            (BazA, Double)
        ]
        let expected = resolution [
            (FooA, Double)
            (FooB, Double)
        ]
        CallGraphTests.AssertResolution(Foo, expected, res1, res2, res3)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Multi-Stage Resolution of Multiple Resolutions to Concrete`` () =

        let res1 = resolution [
            (FooA, BarA |> TypeParameter)
        ]
        let res2 = resolution [
            (FooB, BarA |> TypeParameter)
        ]
        let res3 = resolution [
            (BarA, Int)
        ]
        let expected = resolution [
            (FooA, Int)
            (FooB, Int)
        ]
        CallGraphTests.AssertResolution(Foo, expected, res1, res2, res3)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Redundant Resolution to Concrete`` () =

        let res1 = resolution [
            (FooA, BarA |> TypeParameter)
        ]
        let res2 = resolution [
            (BarA, Int)
            (FooA, Int)
        ]
        let expected = resolution [
            (FooA, Int)
        ]
        CallGraphTests.AssertResolution(Foo, expected, res1, res2)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Redundant Resolution to Type Parameter`` () =

        let res1 = resolution [
            (FooA, BarA |> TypeParameter)
        ]
        let res2 = resolution [
            (BarA, BazA |> TypeParameter)
        ]
        let res3 = resolution [
            (FooA, BazA |> TypeParameter)
        ]
        let expected = resolution [
            (FooA, BazA |> TypeParameter)
        ]
        CallGraphTests.AssertResolution(Foo, expected, res1, res2, res3)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Conflicting Resolution to Concrete`` () =

        let res1 = resolution [
            (FooA, BarA |> TypeParameter)
        ]
        let res2 = resolution [
            (BarA, Int)
            (FooA, Double)
        ]
        let expected = resolution [
            (FooA, Double)
        ]
        CallGraphTests.AssertResolutionFailure(Foo, expected, res1, res2)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Conflicting Resolution to Type Parameter`` () =

        let res1 = resolution [
            (FooA, BarA |> TypeParameter)
        ]
        let res2 = resolution [
            (BarA, BazA |> TypeParameter)
            (FooA, BarC |> TypeParameter)
        ]
        let expected = resolution [
            (FooA, BarC |> TypeParameter)
        ]
        CallGraphTests.AssertResolutionFailure(Foo, expected, res1, res2)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Direct Resolution to Native`` () =

        let res1 = resolution [
            (FooA, FooA |> TypeParameter)
        ]
        let expected = resolution [
            (FooA, FooA |> TypeParameter)
        ]
        CallGraphTests.AssertResolution(Foo, expected, res1)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Indirect Resolution to Native`` () =

        let res1 = resolution [
            (FooA, BarA |> TypeParameter)
        ]
        let res2 = resolution [
            (BarA, BazA |> TypeParameter)
        ]
        let res3 = resolution [
            (BazA, FooA |> TypeParameter)
        ]
        let expected = resolution [
            (FooA, FooA |> TypeParameter)
        ]
        CallGraphTests.AssertResolution(Foo, expected, res1, res2, res3)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Direct Resolution Constrains Native`` () =

        let res1 = resolution [
            (FooA, FooB |> TypeParameter)
        ]
        let expected = resolution [
            (FooA, FooB |> TypeParameter)
        ]
        CallGraphTests.AssertResolutionFailure(Foo, expected, res1)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Indirect Resolution Constrains Native`` () =

        let res1 = resolution [
            (FooA, BarA |> TypeParameter)
        ]
        let res2 = resolution [
            (BarA, BazA |> TypeParameter)
        ]
        let res3 = resolution [
            (BazA, FooB |> TypeParameter)
        ]
        let expected = resolution [
            (FooA, FooB |> TypeParameter)
        ]
        CallGraphTests.AssertResolutionFailure(Foo, expected, res1, res2, res3)

    [<Fact>]
    [<Trait("Category","Cycle Detection")>]
    member this.``No Cycles`` () =
        let cycles = CompileCycleDetectionTest 1
        Assert.Empty cycles

    [<Fact>]
    [<Trait("Category","Cycle Detection")>]
    member this.``Simple Cycle`` () =
        let result = CompileCycleDetectionTest 2

        [
            [ "Foo"; "Bar" ]
        ]
        |> CheckForExpectedCycles result

    [<Fact>]
    [<Trait("Category","Cycle Detection")>]
    member this.``Longer Cycle`` () =
        let result = CompileCycleDetectionTest 3

        [
            [ "Foo"; "Bar"; "Baz" ]
        ]
        |> CheckForExpectedCycles result

    [<Fact>]
    [<Trait("Category","Cycle Detection")>]
    member this.``Direct Recursion Cycle`` () =
        let result = CompileCycleDetectionTest 4

        [
            [ "Foo" ]
        ]
        |> CheckForExpectedCycles result

    [<Fact>]
    [<Trait("Category","Cycle Detection")>]
    member this.``Loop In Sequence`` () =
        let result = CompileCycleDetectionTest 5

        [
            [ "Bar" ]
        ]
        |> CheckForExpectedCycles result

    [<Fact>]
    [<Trait("Category","Cycle Detection")>]
    member this.``Figure-Eight Cycles`` () =
        let result = CompileCycleDetectionTest 6

        [
            [ "Foo"; "Bar" ]
            [ "Foo"; "Baz" ]
        ]
        |> CheckForExpectedCycles result

    [<Fact(Skip="Cycle detection does not work here yet")>]
    [<Trait("Category","Cycle Detection")>]
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