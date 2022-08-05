// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System
open System.Collections.Immutable
open System.IO
open System.Linq
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.DependencyAnalysis
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.ReservedKeywords
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.VisualStudio.LanguageServer.Protocol
open Xunit
open Xunit.Abstractions

type CallGraphTests(output: ITestOutputHelper) =

    let compilationManager =
        new CompilationUnitManager(ProjectProperties.Empty, (fun ex -> failwith ex.Message))

    let compilationManagerExe =
        let props = ImmutableDictionary.CreateBuilder()
        props.Add(MSBuildProperties.ResolvedQsharpOutputType, AssemblyConstants.QsharpExe)
        new CompilationUnitManager(ProjectProperties(props), (fun ex -> failwith ex.Message))

    let getTempFile () =
        Uri(Path.GetFullPath(Path.GetRandomFileName()))

    let getManager uri content =
        CompilationUnitManager.InitializeFileManager(
            uri,
            content,
            compilationManager.PublishDiagnostics,
            compilationManager.LogException
        )

    // Adds Core to the compilation
    do
        let addOrUpdateSourceFile filePath =
            getManager (Uri(filePath)) (File.ReadAllText filePath)
            |> compilationManager.AddOrUpdateSourceFileAsync
            |> ignore

            getManager (Uri(filePath)) (File.ReadAllText filePath)
            |> compilationManagerExe.AddOrUpdateSourceFileAsync
            |> ignore

        Path.Combine("TestCases", "LinkingTests", "Core.qs") |> Path.GetFullPath |> addOrUpdateSourceFile

    let makeGraphNode name specKind (paramRes: _ list) =
        let qualifiedName = { Namespace = Signatures.PopulateCallGraphNS; Name = name }
        let res = paramRes.ToImmutableDictionary((fun kvp -> qualifiedName, fst kvp), snd >> ResolvedType.New)
        ConcreteCallGraphNode(qualifiedName, specKind, res)

    let decorateWithNamespace (ns: string) (input: string list list) =
        List.map (List.map (fun name -> { Namespace = ns; Name = name })) input

    let readAndChunkSourceFile fileName =
        let sourceInput = Path.Combine("TestCases", fileName) |> File.ReadAllText
        sourceInput.Split([| "===" |], StringSplitOptions.RemoveEmptyEntries)

    let buildContent content =

        let fileId = getTempFile ()
        let file = getManager fileId content

        compilationManager.AddOrUpdateSourceFileAsync(file) |> ignore
        let compilationDataStructures = compilationManager.Build()
        compilationManager.TryRemoveSourceFileAsync(fileId, false) |> ignore

        compilationDataStructures.Diagnostics() |> Seq.exists (fun d -> d.IsError()) |> Assert.False
        Assert.NotNull compilationDataStructures.BuiltCompilation

        compilationDataStructures

    let buildContentWithErrors content (expectedErrors: seq<_>) =

        let fileId = getTempFile ()
        let file = getManager fileId content

        compilationManager.AddOrUpdateSourceFileAsync(file) |> ignore
        let compilationDataStructures = compilationManager.Build()
        compilationManager.TryRemoveSourceFileAsync(fileId, false) |> ignore

        let expected = Seq.map int expectedErrors

        let got =
            compilationDataStructures.Diagnostics()
            |> Seq.filter (fun d -> d.Severity = Nullable DiagnosticSeverity.Error)
            |> Seq.choose (fun d ->
                match Diagnostics.TryGetCode d.Code.Value.Second with
                | true, code -> Some code
                | false, _ -> None)

        let codeMismatch = expected.ToImmutableHashSet().SymmetricExcept got
        let gotLookup = got.ToLookup(new Func<_, _>(id))
        let expectedLookup = expected.ToLookup(new Func<_, _>(id))
        let nrMismatch = gotLookup.Where(fun g -> g.Count() <> expectedLookup.[g.Key].Count())

        Assert.False(
            codeMismatch.Any() || nrMismatch.Any(),
            sprintf
                "%A code mismatch\nexpected: %s\ngot: %s"
                DiagnosticSeverity.Error
                (String.Join(", ", expected))
                (String.Join(", ", got))
        )

    let compileTest testNumber fileName =
        let srcChunks = readAndChunkSourceFile fileName
        srcChunks.Length >= testNumber |> Assert.True
        let compilationDataStructures = buildContent srcChunks.[testNumber - 1]
        Assert.NotNull compilationDataStructures.BuiltCompilation
        compilationDataStructures.BuiltCompilation

    let buildTrimmedGraph (compilation: QsCompilation) = CallGraph(compilation, true)

    let compileTestExpectingErrors testNumber fileName expect =
        let srcChunks = readAndChunkSourceFile fileName
        srcChunks.Length >= testNumber |> Assert.True
        buildContentWithErrors srcChunks.[testNumber - 1] expect

    let compileCycleDetectionTest testNumber =
        let callGraph = compileTest testNumber "CycleDetection.qs" |> CallGraph
        callGraph.GetCallCycles()

    let compileCycleValidationTest testNumber =
        compileTest testNumber "CycleValidation.qs" |> ignore

    let compileInvalidCycleTest testNumber expected =
        let errors =
            expected
            |> Seq.choose (function
                | Error error -> Some error
                | _ -> None)

        compileTestExpectingErrors testNumber "CycleValidation.qs" errors

    let populateCallGraph testNumber =
        compileTest testNumber "PopulateCallGraph.qs"

    let populateCallGraphWithExe testNumber =
        let srcChunks = readAndChunkSourceFile "PopulateCallGraph.qs"
        srcChunks.Length >= testNumber |> Assert.True

        let fileId = getTempFile ()
        let file = getManager fileId srcChunks.[testNumber - 1]
        compilationManagerExe.AddOrUpdateSourceFileAsync(file) |> ignore
        let compilationDataStructures = compilationManagerExe.Build()
        compilationManagerExe.TryRemoveSourceFileAsync(fileId, false) |> ignore
        compilationDataStructures.Diagnostics() |> Seq.exists (fun d -> d.IsError()) |> Assert.False
        Assert.NotNull compilationDataStructures.BuiltCompilation
        compilationDataStructures.BuiltCompilation

    /// Checks if one of the given lists can be rotated into the other given list
    let cyclicEquivalence lst1 lst2 =
        let size1 = List.length lst1

        if size1 <> List.length lst2 then
            false
        else
            let rotate n =
                lst1 |> List.permute (fun index -> (index + n) % size1)

            let rotations = [ 0 .. size1 - 1 ] |> List.map rotate
            List.contains lst2 rotations

    let checkForExpectedCycles (actualCycles: seq<#seq<CallGraphNode>>) expectedCycles =
        let expected = expectedCycles |> decorateWithNamespace Signatures.CycleDetectionNS
        let actual = actualCycles |> (Seq.map ((Seq.map (fun x -> x.CallableName)) >> Seq.toList) >> Seq.toList)

        Assert.True(
            actual.Length = expected.Length,
            sprintf "Expected call graph to have %i cycle(s), but found %i cycle(s)" expected.Length actual.Length
        )

        let cycleToString (cycle: QsQualifiedName list) =
            String.Join(" -> ", List.map (fun node -> node.ToString()) cycle)

        for cycle in expected do
            Assert.True(
                List.exists (cyclicEquivalence cycle) actual,
                sprintf "Did not find expected cycle: %s" (cycleToString cycle)
            )

    let assertExpectedDirectDependencies nameFrom nameToList (givenGraph: CallGraph) =
        let strToNode name =
            let nodeName = { Namespace = Signatures.PopulateCallGraphNS; Name = name }
            CallGraphNode(nodeName)

        let dependencies = givenGraph.GetDirectDependencies(strToNode nameFrom)

        for nameTo in nameToList do
            let expectedNode = strToNode nameTo

            Assert.True(
                dependencies.Contains(expectedNode),
                sprintf "Expected %s to take dependency on %s." nameFrom nameTo
            )

    let assertInGraph (givenGraph: CallGraph) name =
        let nodeName = { Namespace = Signatures.PopulateCallGraphNS; Name = name }
        let found = givenGraph.Nodes |> Seq.exists (fun x -> x.CallableName = nodeName)
        Assert.True(found, sprintf "Expected %s to be in the call graph." name)

    let assertNotInGraph (givenGraph: CallGraph) name =
        let nodeName = { Namespace = Signatures.PopulateCallGraphNS; Name = name }
        let found = givenGraph.Nodes |> Seq.exists (fun x -> x.CallableName = nodeName)
        Assert.False(found, sprintf "Expected %s to not be in the call graph." name)

    let assertInConcreteGraph (givenGraph: ConcreteCallGraph) node =
        Assert.True(
            givenGraph.Nodes.Contains(node),
            sprintf
                "Expected %A (%A) to be in the call graph with the following type parameter resolutions:\n%A"
                node.CallableName
                node.Kind
                node.ParamResolutions
        )

    let assertNotInConcreteGraph (givenGraph: ConcreteCallGraph) node =
        Assert.False(
            givenGraph.Nodes.Contains(node),
            sprintf
                "Expected %A (%A) to not be in the call graph with the following type parameter resolutions:\n%A"
                node.CallableName
                node.Kind
                node.ParamResolutions
        )

    // ToDo: Add tests for cycle validation once that is implemented.

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Basic Entry Point``() =
        let graph = populateCallGraphWithExe 1 |> buildTrimmedGraph

        [ "Main", [ "Foo"; "Bar" ]; "Foo", []; "Bar", [ "Baz" ]; "Baz", [] ]
        |> List.map (fun x -> assertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Unrelated To Entry Point``() =
        let graph = populateCallGraphWithExe 2 |> buildTrimmedGraph

        [ "Main", [ "Foo" ]; "Foo", [] ]
        |> List.map (fun x -> assertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

        [ "Bar"; "Baz" ] |> List.map (assertNotInGraph graph) |> ignore

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Not Called With Entry Point``() =
        let graph = populateCallGraphWithExe 3 |> buildTrimmedGraph

        [ "Main", [ "Foo" ]; "Foo", [] ]
        |> List.map (fun x -> assertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

        assertNotInGraph graph "NotCalled"

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Not Called Without Entry Point``() =
        let graph = populateCallGraph 4 |> CallGraph

        [ "Main", [ "Foo" ]; "Foo", [] ]
        |> List.map (fun x -> assertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

        assertInGraph graph "NotCalled"

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Unrelated Without Entry Point``() =
        let graph = populateCallGraph 5 |> CallGraph

        [ "Main", [ "Foo" ]; "Foo", []; "Bar", [ "Baz" ]; "Baz", [] ]
        |> List.map (fun x -> assertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Entry Point No Descendants``() =
        let graph = populateCallGraphWithExe 6 |> buildTrimmedGraph
        assertInGraph graph "Main"

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Calls Entry Point From Entry Point``() =
        let graph = populateCallGraphWithExe 7 |> buildTrimmedGraph

        [ "Main", [ "Foo" ]; "Foo", [ "Main" ] ]
        |> List.map (fun x -> assertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Entry Point Ancestor And Descendant``() =
        let graph = populateCallGraphWithExe 8 |> buildTrimmedGraph

        [ "Main", [ "Foo" ]; "Foo", [] ]
        |> List.map (fun x -> assertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

        assertNotInGraph graph "Bar"

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Multiple Entry Points``() =
        let graph = populateCallGraphWithExe 9 |> buildTrimmedGraph

        [ "Main1", [ "Foo" ]; "Main2", [ "Bar" ]; "Foo", []; "Bar", [] ]
        |> List.map (fun x -> assertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Unit Test as Starting Point``() =
        let graph = populateCallGraphWithExe 10 |> buildTrimmedGraph

        [ "Test", [ "Foo" ]; "Foo", [] ]
        |> List.map (fun x -> assertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

        assertNotInGraph graph "Bar"

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Entry Points and Unit Tests``() =
        let graph = populateCallGraphWithExe 11 |> buildTrimmedGraph

        [
            "Main1", [ "Foo" ]
            "Main2", [ "Bar" ]
            "Test1", [ "Zip" ]
            "Test2", [ "Zap" ]
        ]
        |> List.map (fun x -> assertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

        assertNotInGraph graph "Unused"

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Concrete Graph has Concretizations``() =
        let graph = populateCallGraphWithExe 12 |> ConcreteCallGraph

        let makeNode name resType =
            makeGraphNode name QsSpecializationKind.QsBody [ ("A", resType) ]

        let makeNodeNoRes name =
            makeGraphNode name QsSpecializationKind.QsBody []

        let fooDouble = makeNode "Foo" Double
        let fooString = makeNode "Foo" String
        let fooEmpty = makeNodeNoRes "Foo"
        let barString = makeNode "Bar" String
        let barEmpty = makeNodeNoRes "Bar"

        assertInConcreteGraph graph fooDouble
        assertInConcreteGraph graph fooString
        assertInConcreteGraph graph barString

        assertNotInConcreteGraph graph fooEmpty
        assertNotInConcreteGraph graph barEmpty

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Concrete Graph Contains All Specializations``() =
        let graph = populateCallGraphWithExe 13 |> ConcreteCallGraph

        let makeNode name spec = makeGraphNode name spec []

        let fooAdj = makeNode "FooAdj" QsAdjoint
        let fooCtl = makeNode "FooCtl" QsControlled
        let fooCtlAdj = makeNode "FooCtlAdj" QsControlledAdjoint
        let barAdj = makeNode "BarAdj" QsBody
        let barCtl = makeNode "BarCtl" QsBody
        let barCtlAdj = makeNode "BarCtlAdj" QsBody
        let unused = makeNode "Unused" QsBody

        assertInConcreteGraph graph fooAdj
        assertInConcreteGraph graph fooCtl
        assertInConcreteGraph graph fooCtlAdj
        assertInConcreteGraph graph barAdj
        assertInConcreteGraph graph barCtl
        assertInConcreteGraph graph barCtlAdj
        assertInConcreteGraph graph unused

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Concrete Graph Double Reference Resolution``() =
        let graph = populateCallGraphWithExe 14 |> ConcreteCallGraph

        let makeNode resType =
            makeGraphNode "Foo" QsSpecializationKind.QsBody [ "A", resType ]

        let fooInt = makeNode Int
        let fooFunc = makeNode (QsTypeKind.Function(ResolvedType.New Int, ResolvedType.New Int))

        assertInConcreteGraph graph fooInt
        assertInConcreteGraph graph fooFunc

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Concrete Graph Non-Call Reference Only Body``() =
        let graph = populateCallGraphWithExe 15 |> ConcreteCallGraph

        let makeNode spec = makeGraphNode "Foo" spec []

        let foo = makeNode QsBody
        let fooAdj = makeNode QsAdjoint
        let fooCtl = makeNode QsControlled
        let fooCtlAdj = makeNode QsControlledAdjoint

        assertInConcreteGraph graph foo

        assertNotInConcreteGraph graph fooAdj
        assertNotInConcreteGraph graph fooCtl
        assertNotInConcreteGraph graph fooCtlAdj

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Concrete Graph Non-Call Reference With Adjoint``() =
        let graph = populateCallGraphWithExe 16 |> ConcreteCallGraph

        let makeNode spec = makeGraphNode "Foo" spec []

        let foo = makeNode QsBody
        let fooAdj = makeNode QsAdjoint
        let fooCtl = makeNode QsControlled
        let fooCtlAdj = makeNode QsControlledAdjoint

        assertInConcreteGraph graph foo
        assertInConcreteGraph graph fooAdj

        assertNotInConcreteGraph graph fooCtl
        assertNotInConcreteGraph graph fooCtlAdj

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Concrete Graph Non-Call Reference With All``() =
        let graph = populateCallGraphWithExe 17 |> ConcreteCallGraph

        let makeNode spec = makeGraphNode "Foo" spec []

        let foo = makeNode QsBody
        let fooAdj = makeNode QsAdjoint
        let fooCtl = makeNode QsControlled
        let fooCtlAdj = makeNode QsControlledAdjoint

        assertInConcreteGraph graph foo
        assertInConcreteGraph graph fooAdj
        assertInConcreteGraph graph fooCtl
        assertInConcreteGraph graph fooCtlAdj

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Concrete Graph Call Self-Adjoint Reference``() =
        let compilation = populateCallGraphWithExe 18
        let mutable transformed = { Namespaces = ImmutableArray.Empty; EntryPoints = ImmutableArray.Empty }
        Assert.True(CodeGeneration.GenerateFunctorSpecializations(compilation, &transformed))
        let graph = transformed |> ConcreteCallGraph

        let makeNode spec = makeGraphNode "Foo" spec []

        let foo = makeNode QsBody
        let fooAdj = makeNode QsAdjoint
        let fooCtl = makeNode QsControlled
        let fooCtlAdj = makeNode QsControlledAdjoint

        assertInConcreteGraph graph foo
        assertInConcreteGraph graph fooAdj
        assertInConcreteGraph graph fooCtl
        assertInConcreteGraph graph fooCtlAdj

        let dependencies = graph.GetDirectDependencies fooAdj

        Assert.True(
            dependencies.Contains(foo),
            "Expected adjoint specialization to take dependency on body specialization."
        )

        let dependencies = graph.GetDirectDependencies fooCtlAdj

        Assert.True(
            dependencies.Contains(fooCtl),
            "Expected controlled-adjoint specialization to take dependency on controlled specialization."
        )

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Concrete Graph Clears Type Param Resolutions After Statements``() =
        let compilation = populateCallGraphWithExe 19
        let mutable transformed = { Namespaces = ImmutableArray.Empty; EntryPoints = ImmutableArray.Empty }
        Assert.True(CodeGeneration.GenerateFunctorSpecializations(compilation, &transformed))
        let graph = transformed |> ConcreteCallGraph

        for node in graph.Nodes do
            let unresolvedTypeParameters =
                node.ParamResolutions
                |> Seq.choose (fun res ->
                    match res.Value.Resolution with
                    | TypeParameter _ -> Some(res.Key)
                    | _ -> None)

            Assert.Empty unresolvedTypeParameters

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Concrete Graph Multiple Entry Points``() =
        let graph = populateCallGraphWithExe 20 |> ConcreteCallGraph

        let makeNode name resType =
            makeGraphNode name QsSpecializationKind.QsBody [ ("A", resType) ]

        let makeNodeNoRes name =
            makeGraphNode name QsSpecializationKind.QsBody []

        let main1 = makeNodeNoRes "Main1"
        let main2 = makeNodeNoRes "Main2"
        let fooDouble = makeNode "Foo" Double
        let fooString = makeNode "Foo" String
        let fooEmpty = makeNodeNoRes "Foo"

        assertInConcreteGraph graph main1
        assertInConcreteGraph graph main2
        assertInConcreteGraph graph fooDouble
        assertInConcreteGraph graph fooString

        assertNotInConcreteGraph graph fooEmpty

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Concrete Graph Unit Tests``() =
        let graph = populateCallGraphWithExe 21 |> ConcreteCallGraph

        let makeNode name resType =
            makeGraphNode name QsSpecializationKind.QsBody [ ("A", resType) ]

        let makeNodeNoRes name =
            makeGraphNode name QsSpecializationKind.QsBody []

        let test1 = makeNodeNoRes "Test1"
        let test2 = makeNodeNoRes "Test2"
        let fooDouble = makeNode "Foo" Double
        let fooString = makeNode "Foo" String
        let fooEmpty = makeNodeNoRes "Foo"

        assertInConcreteGraph graph test1
        assertInConcreteGraph graph test2
        assertInConcreteGraph graph fooDouble
        assertInConcreteGraph graph fooString

        assertNotInConcreteGraph graph fooEmpty

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Concrete Graph Entry Points and Unit Tests``() =
        let graph = populateCallGraphWithExe 22 |> ConcreteCallGraph

        let makeNode name resType =
            makeGraphNode name QsSpecializationKind.QsBody [ ("A", resType) ]

        let makeNodeNoRes name =
            makeGraphNode name QsSpecializationKind.QsBody []

        let main1 = makeNodeNoRes "Main1"
        let main2 = makeNodeNoRes "Main2"
        let test1 = makeNodeNoRes "Test1"
        let test2 = makeNodeNoRes "Test2"
        let fooDouble = makeNode "Foo" Double
        let fooString = makeNode "Foo" String
        let fooEmpty = makeNodeNoRes "Foo"
        let barDouble = makeNode "Bar" Double
        let barString = makeNode "Bar" String
        let barEmpty = makeNodeNoRes "Bar"
        let unused = makeNodeNoRes "Unused"

        assertInConcreteGraph graph main1
        assertInConcreteGraph graph main2
        assertInConcreteGraph graph test1
        assertInConcreteGraph graph test2
        assertInConcreteGraph graph fooDouble
        assertInConcreteGraph graph fooString
        assertInConcreteGraph graph barDouble
        assertInConcreteGraph graph barString

        assertNotInConcreteGraph graph fooEmpty
        assertNotInConcreteGraph graph barEmpty
        assertNotInConcreteGraph graph unused

    [<Fact>]
    [<Trait("Category", "Cycle Detection")>]
    member this.``No Cycles``() =
        let cycles = compileCycleDetectionTest 1
        Assert.Empty cycles

    [<Fact>]
    [<Trait("Category", "Cycle Detection")>]
    member this.``Simple Cycle``() =
        let result = compileCycleDetectionTest 2

        [ [ "Foo"; "Bar" ] ] |> checkForExpectedCycles result

    [<Fact>]
    [<Trait("Category", "Cycle Detection")>]
    member this.``Longer Cycle``() =
        let result = compileCycleDetectionTest 3

        [ [ "Foo"; "Bar"; "Baz" ] ] |> checkForExpectedCycles result

    [<Fact>]
    [<Trait("Category", "Cycle Detection")>]
    member this.``Direct Recursion Cycle``() =
        let result = compileCycleDetectionTest 4

        [ [ "Foo" ] ] |> checkForExpectedCycles result

    [<Fact>]
    [<Trait("Category", "Cycle Detection")>]
    member this.``Loop In Sequence``() =
        let result = compileCycleDetectionTest 5

        [ [ "Bar" ] ] |> checkForExpectedCycles result

    [<Fact>]
    [<Trait("Category", "Cycle Detection")>]
    member this.``Figure-Eight Cycles``() =
        let result = compileCycleDetectionTest 6

        [ [ "Foo"; "Bar" ]; [ "Foo"; "Baz" ] ] |> checkForExpectedCycles result

    [<Fact>]
    [<Trait("Category", "Cycle Detection")>]
    member this.``Fully Connected Cycles``() =
        let result = compileCycleDetectionTest 7

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
        |> checkForExpectedCycles result

    [<Fact>]
    [<Trait("Category", "Cycle Detection")>]
    member this.``Sausage Link Graph Cycles``() =
        let result = compileCycleDetectionTest 8

        [ [ "_1"; "_2" ]; [ "_2"; "_3" ]; [ "_3"; "_4" ] ] |> checkForExpectedCycles result

    [<Fact>]
    [<Trait("Category", "Cycle Detection")>]
    member this.``Double Link Graph Cycles``() =
        let result = compileCycleDetectionTest 9

        [
            [ "_1"; "_2"; "_6"; "_5" ]
            [ "_1"; "_4"; "_6"; "_3" ]
            [ "_1"; "_2"; "_6"; "_3" ]
            [ "_1"; "_4"; "_6"; "_5" ]
        ]
        |> checkForExpectedCycles result

    [<Fact>]
    [<Trait("Category", "Cycle Detection")>]
    member this.``Multiple SCC Cycles``() =
        let result = compileCycleDetectionTest 10

        [
            [ "SCC1_1" ]
            [ "SCC2_1"; "SCC2_2" ]
            [ "SCC3_2"; "SCC3_3" ]
            [ "SCC3_1"; "SCC3_2"; "SCC3_3" ]
        ]
        |> checkForExpectedCycles result

    [<Fact>]
    [<Trait("Category", "Cycle Detection")>]
    member this.``Johnson's Graph Cycles``() =
        let result = compileCycleDetectionTest 11

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
        |> checkForExpectedCycles result

    [<Fact>]
    [<Trait("Category", "Cycle Validation")>]
    member this.``Cycle with Generic Resolution``() = compileCycleValidationTest 1

    [<Fact>]
    [<Trait("Category", "Cycle Validation")>]
    member this.``Cycle with Concrete Resolution``() = compileCycleValidationTest 2

    [<Fact>]
    [<Trait("Category", "Cycle Validation")>]
    member this.``Constricting Cycle``() =
        Error ErrorCode.InvalidCyclicTypeParameterResolution
        |> List.replicate 3
        |> compileInvalidCycleTest 3

    [<Fact>]
    [<Trait("Category", "Cycle Validation")>]
    member this.``Cycle with Rotating Parameters``() = compileCycleValidationTest 4

    [<Fact>]
    [<Trait("Category", "Cycle Validation")>]
    member this.``Cycle with Mutated Forwarding``() =
        Error ErrorCode.InvalidCyclicTypeParameterResolution
        |> List.replicate 3
        |> compileInvalidCycleTest 5

    [<Fact>]
    [<Trait("Category", "Cycle Validation")>]
    member this.``Cycle with Multiple Concrete Resolutions``() = compileCycleValidationTest 6

    [<Fact>]
    [<Trait("Category", "Cycle Validation")>]
    member this.``Cycle with Rotating Constriction``() =
        Error ErrorCode.InvalidCyclicTypeParameterResolution
        |> List.replicate 18
        |> compileInvalidCycleTest 7
