// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System
open System.IO
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.DependencyAnalysis
open Microsoft.Quantum.QsCompiler.ReservedKeywords
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Xunit
open Xunit.Abstractions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open System.Collections.Immutable

type CallGraphTests (output:ITestOutputHelper) =

    let compilationManager = new CompilationUnitManager(new Action<Exception> (fun ex -> failwith ex.Message))
    let compilationManagerExe =
        new CompilationUnitManager(new Action<Exception> (fun ex -> failwith ex.Message),
                                   null,
                                   false,
                                   AssemblyConstants.RuntimeCapabilities.Unknown,
                                   true) // The isExecutable is true

    let getTempFile () = new Uri(Path.GetFullPath(Path.GetRandomFileName()))
    let getManager uri content = CompilationUnitManager.InitializeFileManager(uri, content, compilationManager.PublishDiagnostics, compilationManager.LogException)

    // Adds Core to the compilation
    do  let addOrUpdateSourceFile filePath =
            getManager (new Uri(filePath)) (File.ReadAllText filePath) |> compilationManager.AddOrUpdateSourceFileAsync |> ignore
            getManager (new Uri(filePath)) (File.ReadAllText filePath) |> compilationManagerExe.AddOrUpdateSourceFileAsync |> ignore
        Path.Combine ("TestCases", "LinkingTests", "Core.qs") |> Path.GetFullPath |> addOrUpdateSourceFile

    let DecorateWithNamespace (ns : string) (input : string list list) =
        List.map (List.map (fun name -> { Namespace = NonNullable<_>.New ns; Name = NonNullable<_>.New name })) input

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

    let CompileTest testNumber fileName =
        let srcChunks = ReadAndChunkSourceFile fileName
        srcChunks.Length >= testNumber |> Assert.True
        let compilationDataStructures = BuildContent srcChunks.[testNumber-1]
        Assert.NotNull compilationDataStructures.BuiltCompilation
        compilationDataStructures.BuiltCompilation

    let BuildTrimmedGraph (compilation : QsCompilation) = SimpleCallGraph(compilation, true)

    let CompileCycleDetectionTest testNumber =
        let SimpleCallGraph = CompileTest testNumber "CycleDetection.qs" |> SimpleCallGraph
        SimpleCallGraph.GetCallCycles ()

    let CompileTypeParameterResolutionTest testNumber =
        CompileTest testNumber "TypeParameterResolution.qs"

    let CompileTypeParameterResolutionTestWithExe testNumber =
        let srcChunks = ReadAndChunkSourceFile "TypeParameterResolution.qs"
        srcChunks.Length >= testNumber |> Assert.True

        let fileId = getTempFile()
        let file = getManager fileId srcChunks.[testNumber-1]
        compilationManagerExe.AddOrUpdateSourceFileAsync(file) |> ignore
        let compilationDataStructures = compilationManagerExe.Build()
        compilationManagerExe.TryRemoveSourceFileAsync(fileId, false) |> ignore
        compilationDataStructures.Diagnostics() |> Seq.exists (fun d -> d.IsError()) |> Assert.False
        Assert.NotNull compilationDataStructures.BuiltCompilation
        compilationDataStructures.BuiltCompilation

    /// Checks if one of the given lists can be rotated into the other given list
    let CyclicEquivalence lst1 lst2 =
        let size1 = List.length lst1
        if size1 <> List.length lst2 then
            false
        else
            let rotate n = lst1 |> List.permute (fun index -> (index + n) % size1)
            let rotations = [0 .. size1 - 1] |> List.map rotate
            List.contains lst2 rotations

    let CheckForExpectedCycles (actualCycles: seq<#seq<SimpleCallGraphNode>>) expectedCycles =
        let expected = expectedCycles |> DecorateWithNamespace Signatures.CycleDetectionNS

        let actual = actualCycles |> (Seq.map ((Seq.map (fun x -> x.CallableName)) >> Seq.toList) >> Seq.toList)

        Assert.True(actual.Length = expected.Length,
            sprintf "Expected call graph to have %i cycle(s), but found %i cycle(s)" expected.Length actual.Length)

        let cycleToString (cycle : QsQualifiedName list) =
            String.Join(" -> ", List.map (fun node -> node.ToString()) cycle)

        for cycle in expected do
            Assert.True(List.exists (CyclicEquivalence cycle) actual,
                sprintf "Did not find expected cycle: %s" (cycleToString cycle))

    let AssertExpectedDirectDependencies nameFrom nameToList (givenGraph : SimpleCallGraph) =
        let strToNode name =
            let nodeName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New name }
            SimpleCallGraphNode(nodeName)
        let dependencies = givenGraph.GetDirectDependencies (strToNode nameFrom)
        for nameTo in nameToList do
            let expectedNode = strToNode nameTo
            Assert.True(dependencies.Contains(expectedNode),
                sprintf "Expected %s to take dependency on %s." nameFrom nameTo)

    let AssertInGraph (givenGraph : SimpleCallGraph) name =
        let nodeName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New name }
        let found = givenGraph.Nodes |> Seq.exists (fun x -> x.CallableName = nodeName)
        Assert.True(found, sprintf "Expected %s to be in the call graph." name)

    let AssertNotInGraph (givenGraph : SimpleCallGraph) name =
        let nodeName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New name }
        let found = givenGraph.Nodes |> Seq.exists (fun x -> x.CallableName = nodeName)
        Assert.False(found, sprintf "Expected %s to not be in the call graph." name)

    let AssertInConcreteGraph (givenGraph : ConcreteCallGraph) node =
        Assert.True(givenGraph.Nodes.Contains(node),
            sprintf "Expected %A (%A) to be in the call graph with the following type parameter resolutions:\n%A" node.CallableName node.Kind node.ParamResolutions)

    let AssertNotInConcreteGraph (givenGraph : ConcreteCallGraph) node =
        Assert.False(givenGraph.Nodes.Contains(node),
            sprintf "Expected %A (%A) to not be in the call graph with the following type parameter resolutions:\n%A" node.CallableName node.Kind node.ParamResolutions)

    // ToDo: Add tests for cycle validation once that is implemented.
    // ToDo: Add tests for concrete call graph once it is finalized.
    
    [<Fact>]
    [<Trait("Category","Populate Call Graph")>]
    member this.``Basic Entry Point`` () =
        let graph = CompileTypeParameterResolutionTestWithExe 1 |> BuildTrimmedGraph

        [
            "Main", [
                "Foo"
                "Bar"
            ]
            "Foo", [ ]
            "Bar", [
                "Baz"
            ]
            "Baz", [ ]
        ]
        |> List.map (fun x -> AssertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

    [<Fact>]
    [<Trait("Category","Populate Call Graph")>]
    member this.``Unrelated To Entry Point`` () =
        let graph = CompileTypeParameterResolutionTestWithExe 2 |> BuildTrimmedGraph

        [
            "Main", [
                "Foo"
            ]
            "Foo", [ ]
        ]
        |> List.map (fun x -> AssertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

        [
            "Bar"
            "Baz"
        ]
        |> List.map (AssertNotInGraph graph)
        |> ignore

    [<Fact>]
    [<Trait("Category","Populate Call Graph")>]
    member this.``Not Called With Entry Point`` () =
        let graph = CompileTypeParameterResolutionTestWithExe 3 |> BuildTrimmedGraph

        [
            "Main", [
                "Foo"
            ]
            "Foo", [ ]
        ]
        |> List.map (fun x -> AssertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

        AssertNotInGraph graph "NotCalled"

    [<Fact>]
    [<Trait("Category","Populate Call Graph")>]
    member this.``Not Called Without Entry Point`` () =
        let graph = CompileTypeParameterResolutionTest 4 |> SimpleCallGraph

        [
            "Main", [
                "Foo"
            ]
            "Foo", [ ]
        ]
        |> List.map (fun x -> AssertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

        AssertInGraph graph "NotCalled"

    [<Fact>]
    [<Trait("Category","Populate Call Graph")>]
    member this.``Unrelated Without Entry Point`` () =
        let graph = CompileTypeParameterResolutionTest 5 |> SimpleCallGraph

        [
            "Main", [
                "Foo"
            ]
            "Foo", [ ]
            "Bar", [
                "Baz"
            ]
            "Baz", [ ]
        ]
        |> List.map (fun x -> AssertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

    [<Fact>]
    [<Trait("Category","Populate Call Graph")>]
    member this.``Entry Point No Descendants`` () =
        let graph = CompileTypeParameterResolutionTestWithExe 6 |> BuildTrimmedGraph
        AssertInGraph graph "Main"

    [<Fact>]
    [<Trait("Category","Populate Call Graph")>]
    member this.``Calls Entry Point From Entry Point`` () =
        let graph = CompileTypeParameterResolutionTestWithExe 7 |> BuildTrimmedGraph

        [
            "Main", [
                "Foo"
            ]
            "Foo", [
                "Main"
            ]
        ]
        |> List.map (fun x -> AssertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

    [<Fact>]
    [<Trait("Category","Populate Call Graph")>]
    member this.``Entry Point Ancestor And Descendant`` () =
        let graph = CompileTypeParameterResolutionTestWithExe 8 |> BuildTrimmedGraph

        [
            "Main", [
                "Foo"
            ]
            "Foo", [ ]
        ]
        |> List.map (fun x -> AssertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

        AssertNotInGraph graph "Bar"

    [<Fact>]
    [<Trait("Category","Populate Call Graph")>]
    member this.``Concrete Graph has Concretizations`` () = 
        let graph = CompileTypeParameterResolutionTestWithExe 9 |> ConcreteCallGraph

        let makeNode name resType =
            let qalifiedName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New name }
            let res = dict[(qalifiedName, NonNullable<_>.New "A"), ResolvedType.New resType].ToImmutableDictionary()
            ConcreteCallGraphNode(qalifiedName, QsSpecializationKind.QsBody, res)

        let makeNodeNoRes name =
            let qalifiedName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New name }
            let res = ImmutableDictionary.Empty
            ConcreteCallGraphNode(qalifiedName, QsSpecializationKind.QsBody, res)

        let FooDouble = makeNode "Foo" Double
        let FooString = makeNode "Foo" String
        let FooEmpty = makeNodeNoRes "Foo"
        let BarString = makeNode "Bar" String
        let BarEmpty = makeNodeNoRes "Bar"

        AssertInConcreteGraph graph FooDouble
        AssertInConcreteGraph graph FooString
        AssertInConcreteGraph graph BarString

        AssertNotInConcreteGraph graph FooEmpty
        AssertNotInConcreteGraph graph BarEmpty

    [<Fact>]
    [<Trait("Category","Populate Call Graph")>]
    member this.``Concrete Graph Trims Specializations`` () = 
        let graph = CompileTypeParameterResolutionTestWithExe 10 |> ConcreteCallGraph

        let makeNode name spec =
            let qalifiedName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New name }
            let res = ImmutableDictionary.Empty
            ConcreteCallGraphNode(qalifiedName, spec, res)

        let FooAdj = makeNode "FooAdj" QsAdjoint
        let FooCtl = makeNode "FooCtl" QsControlled
        let FooCtlAdj = makeNode "FooCtlAdj" QsControlledAdjoint
        let BarAdj = makeNode "BarAdj" QsBody
        let BarCtl = makeNode "BarCtl" QsBody
        let BarCtlAdj = makeNode "BarCtlAdj" QsBody
        let Unused = makeNode "Unused" QsBody

        AssertInConcreteGraph graph FooAdj
        AssertInConcreteGraph graph FooCtl
        AssertInConcreteGraph graph FooCtlAdj
        AssertInConcreteGraph graph BarAdj
        AssertInConcreteGraph graph BarCtl
        AssertInConcreteGraph graph BarCtlAdj

        AssertNotInConcreteGraph graph Unused

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

    [<Fact>]
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

    [<Fact>]
    [<Trait("Category","Cycle Detection")>]
    member this.``Sausage Link Graph Cycles`` () =
        let result = CompileCycleDetectionTest 8

        [
            [ "_1"; "_2"; ]
            [ "_2"; "_3"; ]
            [ "_3"; "_4"; ]
        ]
        |> CheckForExpectedCycles result

    [<Fact>]
    [<Trait("Category","Cycle Detection")>]
    member this.``Double Link Graph Cycles`` () =
        let result = CompileCycleDetectionTest 9

        [
            [ "_1"; "_2"; "_6"; "_5" ]
            [ "_1"; "_4"; "_6"; "_3" ]
            [ "_1"; "_2"; "_6"; "_3" ]
            [ "_1"; "_4"; "_6"; "_5" ]
        ]
        |> CheckForExpectedCycles result

    [<Fact>]
    [<Trait("Category","Cycle Detection")>]
    member this.``Multiple SCC Cycles`` () =
        let result = CompileCycleDetectionTest 10

        [
            [ "SCC1_1" ]
            [ "SCC2_1"; "SCC2_2" ]
            [ "SCC3_2"; "SCC3_3"; ]
            [ "SCC3_1"; "SCC3_2"; "SCC3_3" ]
        ]
        |> CheckForExpectedCycles result

    [<Fact>]
    [<Trait("Category","Cycle Detection")>]
    member this.``Johnson's Graph Cycles`` () =
        let result = CompileCycleDetectionTest 11

        [
            [ "_1"; "_2"; "_k2"; "_k3"; "_2k"; "_2k1" ]
            [ "_1"; "_3"; "_k2"; "_k3"; "_2k"; "_2k1" ]
            [ "_1"; "_k1"; "_k2"; "_k3"; "_2k"; "_2k1" ]

            [ "_2k2"; "_2k3"; "_k2" ]
            [ "_2k2"; "_2k3"; "_k2"; "_k3" ]
            [ "_2k2"; "_2k3"; "_k2"; "_k3"; "_2k" ]
            [ "_2k2"; "_2k3"; "_k2"; "_k3"; "_2k"; "_2k1" ]

            [ "_2k2"; "_2k3"; "_3k3" ]
            [ "_2k2"; "_2k4"; "_3k3" ]
            [ "_2k2"; "_3k2"; "_3k3" ]
        ]
        |> CheckForExpectedCycles result
