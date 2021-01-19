// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System
open System.Collections.Immutable
open System.IO
open System.Linq
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.DependencyAnalysis
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.ReservedKeywords
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.VisualStudio.LanguageServer.Protocol
open Xunit
open Xunit.Abstractions

type CallGraphTests(output: ITestOutputHelper) =

    let compilationManager = new CompilationUnitManager(new Action<Exception>(fun ex -> failwith ex.Message))

    let compilationManagerExe =
        new CompilationUnitManager(Action<_>(fun ex -> failwith ex.Message),
                                   null,
                                   false,
                                   FullComputation,
                                   isExecutable = true)

    let getTempFile () =
        new Uri(Path.GetFullPath(Path.GetRandomFileName()))

    let getManager uri content =
        CompilationUnitManager.InitializeFileManager
            (uri, content, compilationManager.PublishDiagnostics, compilationManager.LogException)

    // Adds Core to the compilation
    do
        let addOrUpdateSourceFile filePath =
            getManager (new Uri(filePath)) (File.ReadAllText filePath)
            |> compilationManager.AddOrUpdateSourceFileAsync
            |> ignore

            getManager (new Uri(filePath)) (File.ReadAllText filePath)
            |> compilationManagerExe.AddOrUpdateSourceFileAsync
            |> ignore

        Path.Combine("TestCases", "LinkingTests", "Core.qs") |> Path.GetFullPath |> addOrUpdateSourceFile

    let MakeNode name specKind (paramRes: _ list) =
        let qualifiedName = { Namespace = Signatures.PopulateCallGraphNS; Name = name }

        let res =
            paramRes.ToImmutableDictionary
                ((fun kvp -> (qualifiedName, fst kvp)), (fun kvp -> ResolvedType.New(snd kvp)))

        ConcreteCallGraphNode(qualifiedName, specKind, res)

    let DecorateWithNamespace (ns: string) (input: string list list) =
        List.map (List.map (fun name -> { Namespace = ns; Name = name })) input

    let ReadAndChunkSourceFile fileName =
        let sourceInput = Path.Combine("TestCases", fileName) |> File.ReadAllText
        sourceInput.Split([| "===" |], StringSplitOptions.RemoveEmptyEntries)

    let BuildContent content =

        let fileId = getTempFile ()
        let file = getManager fileId content

        compilationManager.AddOrUpdateSourceFileAsync(file) |> ignore
        let compilationDataStructures = compilationManager.Build()
        compilationManager.TryRemoveSourceFileAsync(fileId, false) |> ignore

        compilationDataStructures.Diagnostics() |> Seq.exists (fun d -> d.IsError()) |> Assert.False
        Assert.NotNull compilationDataStructures.BuiltCompilation

        compilationDataStructures

    let BuildContentWithErrors content (expectedErrors: seq<_>) =

        let fileId = getTempFile ()
        let file = getManager fileId content

        compilationManager.AddOrUpdateSourceFileAsync(file) |> ignore
        let compilationDataStructures = compilationManager.Build()
        compilationManager.TryRemoveSourceFileAsync(fileId, false) |> ignore

        let expected = Seq.map (fun code -> int code) expectedErrors

        let got =
            compilationDataStructures.Diagnostics()
            |> Seq.filter (fun d -> d.Severity = DiagnosticSeverity.Error)
            |> Seq.choose (fun d ->
                match Diagnostics.TryGetCode d.Code with
                | true, code -> Some code
                | false, _ -> None)

        let codeMismatch = expected.ToImmutableHashSet().SymmetricExcept got
        let gotLookup = got.ToLookup(new Func<_, _>(id))
        let expectedLookup = expected.ToLookup(new Func<_, _>(id))
        let nrMismatch = gotLookup.Where(fun g -> g.Count() <> expectedLookup.[g.Key].Count())

        Assert.False
            (codeMismatch.Any() || nrMismatch.Any(),
             sprintf "%A code mismatch\nexpected: %s\ngot: %s" DiagnosticSeverity.Error (String.Join(", ", expected))
                 (String.Join(", ", got)))

    let CompileTest testNumber fileName =
        let srcChunks = ReadAndChunkSourceFile fileName
        srcChunks.Length >= testNumber |> Assert.True
        let compilationDataStructures = BuildContent srcChunks.[testNumber - 1]
        Assert.NotNull compilationDataStructures.BuiltCompilation
        compilationDataStructures.BuiltCompilation

    let BuildTrimmedGraph (compilation: QsCompilation) = CallGraph(compilation, true)

    let CompileTestExpectingErrors testNumber fileName expect =
        let srcChunks = ReadAndChunkSourceFile fileName
        srcChunks.Length >= testNumber |> Assert.True
        BuildContentWithErrors srcChunks.[testNumber - 1] expect

    let CompileCycleDetectionTest testNumber =
        let CallGraph = CompileTest testNumber "CycleDetection.qs" |> CallGraph
        CallGraph.GetCallCycles()

    let CompileCycleValidationTest testNumber =
        CompileTest testNumber "CycleValidation.qs" |> ignore

    let CompileInvalidCycleTest testNumber expected =
        let errors =
            expected
            |> Seq.choose (function
                | Error error -> Some error
                | _ -> None)

        CompileTestExpectingErrors testNumber "CycleValidation.qs" errors

    let PopulateCallGraph testNumber =
        CompileTest testNumber "PopulateCallGraph.qs"

    let PopulateCallGraphWithExe testNumber =
        let srcChunks = ReadAndChunkSourceFile "PopulateCallGraph.qs"
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
    let CyclicEquivalence lst1 lst2 =
        let size1 = List.length lst1

        if size1 <> List.length lst2 then
            false
        else
            let rotate n =
                lst1 |> List.permute (fun index -> (index + n) % size1)

            let rotations = [ 0 .. size1 - 1 ] |> List.map rotate
            List.contains lst2 rotations

    let CheckForExpectedCycles (actualCycles: seq<#seq<CallGraphNode>>) expectedCycles =
        let expected = expectedCycles |> DecorateWithNamespace Signatures.CycleDetectionNS
        let actual = actualCycles |> (Seq.map ((Seq.map (fun x -> x.CallableName)) >> Seq.toList) >> Seq.toList)

        Assert.True
            (actual.Length = expected.Length,
             sprintf "Expected call graph to have %i cycle(s), but found %i cycle(s)" expected.Length actual.Length)

        let cycleToString (cycle: QsQualifiedName list) =
            String.Join(" -> ", List.map (fun node -> node.ToString()) cycle)

        for cycle in expected do
            Assert.True
                (List.exists (CyclicEquivalence cycle) actual,
                 sprintf "Did not find expected cycle: %s" (cycleToString cycle))

    let AssertExpectedDirectDependencies nameFrom nameToList (givenGraph: CallGraph) =
        let strToNode name =
            let nodeName = { Namespace = Signatures.PopulateCallGraphNS; Name = name }
            CallGraphNode(nodeName)

        let dependencies = givenGraph.GetDirectDependencies(strToNode nameFrom)

        for nameTo in nameToList do
            let expectedNode = strToNode nameTo

            Assert.True
                (dependencies.Contains(expectedNode), sprintf "Expected %s to take dependency on %s." nameFrom nameTo)

    let AssertInGraph (givenGraph: CallGraph) name =
        let nodeName = { Namespace = Signatures.PopulateCallGraphNS; Name = name }
        let found = givenGraph.Nodes |> Seq.exists (fun x -> x.CallableName = nodeName)
        Assert.True(found, sprintf "Expected %s to be in the call graph." name)

    let AssertNotInGraph (givenGraph: CallGraph) name =
        let nodeName = { Namespace = Signatures.PopulateCallGraphNS; Name = name }
        let found = givenGraph.Nodes |> Seq.exists (fun x -> x.CallableName = nodeName)
        Assert.False(found, sprintf "Expected %s to not be in the call graph." name)

    let AssertInConcreteGraph (givenGraph: ConcreteCallGraph) node =
        Assert.True
            (givenGraph.Nodes.Contains(node),
             sprintf "Expected %A (%A) to be in the call graph with the following type parameter resolutions:\n%A"
                 node.CallableName node.Kind node.ParamResolutions)

    let AssertNotInConcreteGraph (givenGraph: ConcreteCallGraph) node =
        Assert.False
            (givenGraph.Nodes.Contains(node),
             sprintf "Expected %A (%A) to not be in the call graph with the following type parameter resolutions:\n%A"
                 node.CallableName node.Kind node.ParamResolutions)

    // ToDo: Add tests for cycle validation once that is implemented.

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Basic Entry Point``() =
        let graph = PopulateCallGraphWithExe 1 |> BuildTrimmedGraph

        [ "Main", [ "Foo"; "Bar" ]; "Foo", []; "Bar", [ "Baz" ]; "Baz", [] ]
        |> List.map (fun x -> AssertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Unrelated To Entry Point``() =
        let graph = PopulateCallGraphWithExe 2 |> BuildTrimmedGraph

        [ "Main", [ "Foo" ]; "Foo", [] ]
        |> List.map (fun x -> AssertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

        [ "Bar"; "Baz" ] |> List.map (AssertNotInGraph graph) |> ignore

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Not Called With Entry Point``() =
        let graph = PopulateCallGraphWithExe 3 |> BuildTrimmedGraph

        [ "Main", [ "Foo" ]; "Foo", [] ]
        |> List.map (fun x -> AssertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

        AssertNotInGraph graph "NotCalled"

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Not Called Without Entry Point``() =
        let graph = PopulateCallGraph 4 |> CallGraph

        [ "Main", [ "Foo" ]; "Foo", [] ]
        |> List.map (fun x -> AssertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

        AssertInGraph graph "NotCalled"

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Unrelated Without Entry Point``() =
        let graph = PopulateCallGraph 5 |> CallGraph

        [ "Main", [ "Foo" ]; "Foo", []; "Bar", [ "Baz" ]; "Baz", [] ]
        |> List.map (fun x -> AssertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Entry Point No Descendants``() =
        let graph = PopulateCallGraphWithExe 6 |> BuildTrimmedGraph
        AssertInGraph graph "Main"

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Calls Entry Point From Entry Point``() =
        let graph = PopulateCallGraphWithExe 7 |> BuildTrimmedGraph

        [ "Main", [ "Foo" ]; "Foo", [ "Main" ] ]
        |> List.map (fun x -> AssertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Entry Point Ancestor And Descendant``() =
        let graph = PopulateCallGraphWithExe 8 |> BuildTrimmedGraph

        [ "Main", [ "Foo" ]; "Foo", [] ]
        |> List.map (fun x -> AssertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

        AssertNotInGraph graph "Bar"

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Concrete Graph has Concretizations``() =
        let graph = PopulateCallGraphWithExe 9 |> ConcreteCallGraph

        let makeNode name resType =
            MakeNode name QsSpecializationKind.QsBody [ ("A", resType) ]

        let makeNodeNoRes name =
            MakeNode name QsSpecializationKind.QsBody []

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
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Concrete Graph Contains All Specializations``() =
        let graph = PopulateCallGraphWithExe 10 |> ConcreteCallGraph

        let makeNode name spec = MakeNode name spec []

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
        AssertInConcreteGraph graph Unused

    [<Fact(Skip = "Double reference resolution is not yet supported")>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Concrete Graph Double Reference Resolution``() =
        let graph = PopulateCallGraphWithExe 11 |> ConcreteCallGraph

        let makeNode resType =
            MakeNode "Foo" QsSpecializationKind.QsBody [ ("A", resType) ]

        let FooInt = makeNode Int
        let FooFunc = makeNode ((ResolvedType.New Int, ResolvedType.New Int) |> QsTypeKind.Function)

        AssertInConcreteGraph graph FooInt
        AssertInConcreteGraph graph FooFunc

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Concrete Graph Non-Call Reference Only Body``() =
        let graph = PopulateCallGraphWithExe 12 |> ConcreteCallGraph

        let makeNode spec = MakeNode "Foo" spec []

        let Foo = makeNode QsBody
        let FooAdj = makeNode QsAdjoint
        let FooCtl = makeNode QsControlled
        let FooCtlAdj = makeNode QsControlledAdjoint

        AssertInConcreteGraph graph Foo

        AssertNotInConcreteGraph graph FooAdj
        AssertNotInConcreteGraph graph FooCtl
        AssertNotInConcreteGraph graph FooCtlAdj

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Concrete Graph Non-Call Reference With Adjoint``() =
        let graph = PopulateCallGraphWithExe 13 |> ConcreteCallGraph

        let makeNode spec = MakeNode "Foo" spec []

        let Foo = makeNode QsBody
        let FooAdj = makeNode QsAdjoint
        let FooCtl = makeNode QsControlled
        let FooCtlAdj = makeNode QsControlledAdjoint

        AssertInConcreteGraph graph Foo
        AssertInConcreteGraph graph FooAdj

        AssertNotInConcreteGraph graph FooCtl
        AssertNotInConcreteGraph graph FooCtlAdj

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Concrete Graph Non-Call Reference With All``() =
        let graph = PopulateCallGraphWithExe 14 |> ConcreteCallGraph

        let makeNode spec = MakeNode "Foo" spec []

        let Foo = makeNode QsBody
        let FooAdj = makeNode QsAdjoint
        let FooCtl = makeNode QsControlled
        let FooCtlAdj = makeNode QsControlledAdjoint

        AssertInConcreteGraph graph Foo
        AssertInConcreteGraph graph FooAdj
        AssertInConcreteGraph graph FooCtl
        AssertInConcreteGraph graph FooCtlAdj

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Concrete Graph Call Self-Adjoint Reference``() =
        let compilation = PopulateCallGraphWithExe 15
        let mutable transformed = { Namespaces = ImmutableArray.Empty; EntryPoints = ImmutableArray.Empty }
        Assert.True(CodeGeneration.GenerateFunctorSpecializations(compilation, &transformed))
        let graph = transformed |> ConcreteCallGraph

        let makeNode spec = MakeNode "Foo" spec []

        let Foo = makeNode QsBody
        let FooAdj = makeNode QsAdjoint
        let FooCtl = makeNode QsControlled
        let FooCtlAdj = makeNode QsControlledAdjoint

        AssertInConcreteGraph graph Foo
        AssertInConcreteGraph graph FooAdj
        AssertInConcreteGraph graph FooCtl
        AssertInConcreteGraph graph FooCtlAdj

        let dependencies = graph.GetDirectDependencies FooAdj

        Assert.True
            (dependencies.Contains(Foo), "Expected adjoint specialization to take dependency on body specialization.")

        let dependencies = graph.GetDirectDependencies FooCtlAdj

        Assert.True
            (dependencies.Contains(FooCtl),
             "Expected controlled-adjoint specialization to take dependency on controlled specialization.")

    [<Fact>]
    [<Trait("Category", "Populate Call Graph")>]
    member this.``Concrete Graph Clears Type Param Resolutions After Statements``() =
        let compilation = PopulateCallGraphWithExe 16
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
    [<Trait("Category", "Cycle Detection")>]
    member this.``No Cycles``() =
        let cycles = CompileCycleDetectionTest 1
        Assert.Empty cycles

    [<Fact>]
    [<Trait("Category", "Cycle Detection")>]
    member this.``Simple Cycle``() =
        let result = CompileCycleDetectionTest 2

        [ [ "Foo"; "Bar" ] ] |> CheckForExpectedCycles result

    [<Fact>]
    [<Trait("Category", "Cycle Detection")>]
    member this.``Longer Cycle``() =
        let result = CompileCycleDetectionTest 3

        [ [ "Foo"; "Bar"; "Baz" ] ] |> CheckForExpectedCycles result

    [<Fact>]
    [<Trait("Category", "Cycle Detection")>]
    member this.``Direct Recursion Cycle``() =
        let result = CompileCycleDetectionTest 4

        [ [ "Foo" ] ] |> CheckForExpectedCycles result

    [<Fact>]
    [<Trait("Category", "Cycle Detection")>]
    member this.``Loop In Sequence``() =
        let result = CompileCycleDetectionTest 5

        [ [ "Bar" ] ] |> CheckForExpectedCycles result

    [<Fact>]
    [<Trait("Category", "Cycle Detection")>]
    member this.``Figure-Eight Cycles``() =
        let result = CompileCycleDetectionTest 6

        [ [ "Foo"; "Bar" ]; [ "Foo"; "Baz" ] ] |> CheckForExpectedCycles result

    [<Fact>]
    [<Trait("Category", "Cycle Detection")>]
    member this.``Fully Connected Cycles``() =
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
    [<Trait("Category", "Cycle Detection")>]
    member this.``Sausage Link Graph Cycles``() =
        let result = CompileCycleDetectionTest 8

        [ [ "_1"; "_2" ]; [ "_2"; "_3" ]; [ "_3"; "_4" ] ] |> CheckForExpectedCycles result

    [<Fact>]
    [<Trait("Category", "Cycle Detection")>]
    member this.``Double Link Graph Cycles``() =
        let result = CompileCycleDetectionTest 9

        [
            [ "_1"; "_2"; "_6"; "_5" ]
            [ "_1"; "_4"; "_6"; "_3" ]
            [ "_1"; "_2"; "_6"; "_3" ]
            [ "_1"; "_4"; "_6"; "_5" ]
        ]
        |> CheckForExpectedCycles result

    [<Fact>]
    [<Trait("Category", "Cycle Detection")>]
    member this.``Multiple SCC Cycles``() =
        let result = CompileCycleDetectionTest 10

        [
            [ "SCC1_1" ]
            [ "SCC2_1"; "SCC2_2" ]
            [ "SCC3_2"; "SCC3_3" ]
            [ "SCC3_1"; "SCC3_2"; "SCC3_3" ]
        ]
        |> CheckForExpectedCycles result

    [<Fact>]
    [<Trait("Category", "Cycle Detection")>]
    member this.``Johnson's Graph Cycles``() =
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

    [<Fact>]
    [<Trait("Category", "Cycle Validation")>]
    member this.``Cycle with Generic Resolution``() = CompileCycleValidationTest 1

    [<Fact>]
    [<Trait("Category", "Cycle Validation")>]
    member this.``Cycle with Concrete Resolution``() = CompileCycleValidationTest 2

    [<Fact>]
    [<Trait("Category", "Cycle Validation")>]
    member this.``Constricting Cycle``() =
        Error ErrorCode.InvalidCyclicTypeParameterResolution
        |> List.replicate 3
        |> CompileInvalidCycleTest 3

    [<Fact>]
    [<Trait("Category", "Cycle Validation")>]
    member this.``Cycle with Rotating Parameters``() = CompileCycleValidationTest 4

    [<Fact>]
    [<Trait("Category", "Cycle Validation")>]
    member this.``Cycle with Mutated Forwarding``() =
        Error ErrorCode.InvalidCyclicTypeParameterResolution
        |> List.replicate 3
        |> CompileInvalidCycleTest 5

    [<Fact>]
    [<Trait("Category", "Cycle Validation")>]
    member this.``Cycle with Multiple Concrete Resolutions``() = CompileCycleValidationTest 6

    [<Fact>]
    [<Trait("Category", "Cycle Validation")>]
    member this.``Cycle with Rotating Constriction``() =
        Error ErrorCode.InvalidCyclicTypeParameterResolution
        |> List.replicate 18
        |> CompileInvalidCycleTest 7
