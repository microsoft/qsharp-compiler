// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System
open System.Collections.Immutable
open System.IO
open System.Linq
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.DependencyAnalysis
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.CallGraphWalker
open Xunit
open Xunit.Abstractions
open Microsoft.Quantum.QsCompiler.ReservedKeywords


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

    let ResolutionFromParam (res : (QsTypeParameter * QsTypeKind<_,_,_,_>) list) =
        res.ToImmutableDictionary((fun (tp,_) -> tp.Origin, tp.TypeName), snd >> ResolvedType.New)

    let ResolutionFromParamName (res : (QsQualifiedName * NonNullable<string> * QsTypeKind<_,_,_,_>) list) =
        res.ToImmutableDictionary((fun (op, param, _) -> op, param), (fun (_, _, resolution) -> ResolvedType.New resolution))

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
        let callGraph = BuildCallGraph.Apply compilationDataStructures.BuiltCompilation
        Assert.NotNull callGraph
        callGraph

    let CompileCycleDetectionTest testNumber =
        let callGraph = CompileTest testNumber "CycleDetection.qs"
        callGraph.GetCallCycles ()

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

        let callGraph = BuildCallGraph.Apply compilationDataStructures.BuiltCompilation
        Assert.NotNull callGraph
        callGraph

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

    let CheckResolutionMatch (res1 : ImmutableDictionary<_,_>) (res2 : ImmutableDictionary<_,_> ) =
        let keysMismatch = ImmutableHashSet.CreateRange(res1.Keys).SymmetricExcept res2.Keys
        keysMismatch.Count = 0 && res1 |> Seq.exists (fun kv -> res2.[kv.Key] <> kv.Value) |> not

    let AssertExpectedResolution (expected : ImmutableDictionary<_,_>) (given : ImmutableDictionary<_,_> ) =
        Assert.True(CheckResolutionMatch expected given, "Given resolutions did not match the expected resolutions.")

    let AssertExpectedResolutionList (expected : ImmutableDictionary<_,_> list) (given : ImmutableDictionary<_,_> list) =
        let sameLength = expected.Length = given.Length
        let isMatch = sameLength && expected |> List.forall (fun res1 -> given |> List.exists (fun res2 -> CheckResolutionMatch res1 res2))
        Assert.True(isMatch, "Given resolutions did not match the expected resolutions.")

    let AssertExpectedDepencency nameFrom nameTo (given : ILookup<_,_>) expected =
        let opName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New nameTo }
        let opNode = new CallGraphNode(opName, QsSpecializationKind.QsBody, QsNullable<ImmutableArray<ResolvedType>>.Null)
        let expected = expected |> List.map (fun x -> x |> List.map (fun y -> (opName, fst y, snd y)) |> ResolutionFromParamName)
        Assert.True(given.Contains(opNode), sprintf "Expected %s to take dependency on %s." nameFrom nameTo)
        let edges = given.[opNode]
        Assert.True(edges.Count() = expected.Length, sprintf "Expected exactly %i edge(s) from %s to %s." expected.Length nameFrom nameTo)
        let given = List.map (fun (x : CallGraphEdge) -> x.ParamResolutions) (Seq.toList edges)
        AssertExpectedResolutionList expected given

    let AssertExpectedDirectDependencies nameFrom nameToList (givenGraph : CallGraph) =
        let strToNode name =
            let nodeName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New name }
            CallGraphNode(nodeName, QsSpecializationKind.QsBody, QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = givenGraph.GetDirectDependencies (strToNode nameFrom)
        for nameTo in nameToList do
            let expectedNode = strToNode nameTo
            Assert.True(dependencies.Contains(expectedNode),
                sprintf "Expected %s to take dependency on %s." nameFrom nameTo)

    let AssertNotInGraph (givenGraph : CallGraph) name =
        let nodeName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New name }
        let node = CallGraphNode(nodeName, QsSpecializationKind.QsBody, QsNullable<ImmutableArray<ResolvedType>>.Null)
        Assert.False(givenGraph.ContainsNode(node), sprintf "Expected %s to not be in the call graph." name)

    let CheckCombinedResolution (parent, expected : ImmutableDictionary<_,_>, [<ParamArray>] resolutions) =
        let mutable combined = ImmutableDictionary.Empty
        let success = TypeParamUtils.TryCombineTypeResolutionsForTarget(parent, &combined, resolutions)
        AssertExpectedResolution expected combined
        success

    let AssertCombinedResolution (parent, expected, [<ParamArray>] resolutions) =
        let success = CheckCombinedResolution (parent, expected, resolutions)
        Assert.True(success, "Combining type resolutions was not successful.")

    let AssertCombinedResolutionFailure (parent, expected, [<ParamArray>] resolutions) =
        let success = CheckCombinedResolution (parent, expected, resolutions)
        Assert.False(success, "Combining type resolutions should have failed.")

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Resolution to Concrete`` () =

        let given = [|
            ResolutionFromParam [
                (FooA, BarA |> TypeParameter)
                (FooB, Int)
            ]
            ResolutionFromParam [
                (BarA, Int)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, Int)
            (FooB, Int)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Resolution to Type Parameter`` () =

        let given = [|
            ResolutionFromParam [
                (FooA, BarA |> TypeParameter)
            ]
            ResolutionFromParam [
                (BarA, BazA |> TypeParameter)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, BazA |> TypeParameter)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Resolution via Identity Mapping`` () =

        let given = [|
            ResolutionFromParam [
                (FooA, FooA |> TypeParameter)
            ]
            ResolutionFromParam [
                (FooA, Int)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, Int)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Multi-Stage Resolution`` () =

        let given = [|
            ResolutionFromParam [
                (FooA, BarA |> TypeParameter)
            ]
            ResolutionFromParam [
                (BarA, Int)
                (FooB, Int)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, Int)
            (FooB, Int)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Multiple Resolutions to Concrete`` () =

        let given = [|
            ResolutionFromParam [
                (FooA, BarA |> TypeParameter)
                (FooB, BarA |> TypeParameter)
            ]
            ResolutionFromParam [
                (BarA, Int)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, Int)
            (FooB, Int)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Multiple Resolutions to Type Parameter`` () =

        let given = [|
            ResolutionFromParam [
                (FooA, BarA |> TypeParameter)
                (FooB, BarA |> TypeParameter)
            ]
            ResolutionFromParam [
                (BarA, BazA |> TypeParameter)
            ]
            ResolutionFromParam [
                (BazA, Double)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, Double)
            (FooB, Double)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Multi-Stage Resolution of Multiple Resolutions to Concrete`` () =

        let given = [|
            ResolutionFromParam [
                (FooA, BarA |> TypeParameter)
            ]
            ResolutionFromParam [
                (FooB, BarA |> TypeParameter)
            ]
            ResolutionFromParam [
                (BarA, Int)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, Int)
            (FooB, Int)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact(Skip="Need to rework resolution algorithm.")>]
    [<Trait("Category","Type resolution")>]
    member this.``Multi-Stage Resolution of Multiple Resolutions to Type Parameter`` () =

        let given = [|
            ResolutionFromParam [
                (FooA, BarA |> TypeParameter)
            ]
            ResolutionFromParam [
                (BarA, BazA |> TypeParameter)
                (FooB, BarA |> TypeParameter)
            ]
            ResolutionFromParam [
                (BazA, Int)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, Int)
            (FooB, Int)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Redundant Resolution to Concrete`` () =

        let given = [|
            ResolutionFromParam [
                (FooA, BarA |> TypeParameter)
            ]
            ResolutionFromParam [
                (BarA, Int)
                (FooA, Int)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, Int)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Redundant Resolution to Type Parameter`` () =

        let given = [|
            ResolutionFromParam [
                (FooA, BarA |> TypeParameter)
            ]
            ResolutionFromParam [
                (BarA, BazA |> TypeParameter)
            ]
            ResolutionFromParam [
                (FooA, BazA |> TypeParameter)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, BazA |> TypeParameter)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Conflicting Resolution to Concrete`` () =

        let given = [|
            ResolutionFromParam [
                (FooA, BarA |> TypeParameter)
            ]
            ResolutionFromParam [
                (BarA, Int)
                (FooA, Double)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, Double)
        ]

        AssertCombinedResolutionFailure(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Conflicting Resolution to Type Parameter`` () =

        let given = [|
            ResolutionFromParam [
                (FooA, BarA |> TypeParameter)
            ]
            ResolutionFromParam [
                (BarA, BazA |> TypeParameter)
                (FooA, BarC |> TypeParameter)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, BarC |> TypeParameter)
        ]

        AssertCombinedResolutionFailure(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Direct Resolution to Native`` () =

        let given = [|
            ResolutionFromParam [
                (FooA, FooA |> TypeParameter)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, FooA |> TypeParameter)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Indirect Resolution to Native`` () =

        let given = [|
            ResolutionFromParam [
                (FooA, BarA |> TypeParameter)
            ]
            ResolutionFromParam [
                (BarA, BazA |> TypeParameter)
            ]
            ResolutionFromParam [
                (BazA, FooA |> TypeParameter)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, FooA |> TypeParameter)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Direct Resolution Constrains Native`` () =

        let given = [|
            ResolutionFromParam [
                (FooA, FooB |> TypeParameter)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, FooB |> TypeParameter)
        ]

        AssertCombinedResolutionFailure(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Indirect Resolution Constrains Native`` () =

        let given = [|
            ResolutionFromParam [
                (FooA, BarA |> TypeParameter)
            ]
            ResolutionFromParam [
                (BarA, BazA |> TypeParameter)
            ]
            ResolutionFromParam [
                (BazA, FooB |> TypeParameter)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, FooB |> TypeParameter)
        ]

        AssertCombinedResolutionFailure(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type resolution")>]
    member this.``Inner Cycle Constrains Type Parameter`` () =

        let given = [|
            ResolutionFromParam [
                (FooA, BarA |> TypeParameter)
            ]
            ResolutionFromParam [
                (BarA, BazA |> TypeParameter)
            ]
            ResolutionFromParam [
                (BazA, BarC |> TypeParameter) // TODO: for performance reasons it would be nice to detect this case as well and error here
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, BarC |> TypeParameter)
        ]

        AssertCombinedResolutionFailure(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Get Dependencies")>]
    member this.``Get All Dependencies`` () =
        let graph = CompileTypeParameterResolutionTest 1

        let mainName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" }
        let mainNode = new CallGraphNode(mainName, QsSpecializationKind.QsBody, QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = graph.GetAllDependencies mainNode

        [
            [
                (NonNullable<_>.New "A", Int)
                (NonNullable<_>.New "B", String)
            ]
        ]
        |> AssertExpectedDepencency "Main" "Foo" dependencies

        [
            [
                (NonNullable<_>.New "X", Int)
            ]
            [
                (NonNullable<_>.New "X", String)
            ]
        ]
        |> AssertExpectedDepencency "Main" "Bar" dependencies

        [
            [
                (NonNullable<_>.New "Y", String)
            ]
        ]
        |> AssertExpectedDepencency "Main" "Baz" dependencies

    [<Fact>]
    [<Trait("Category","Get Dependencies")>]
    member this.``Argument Resolution`` () =
        let graph = CompileTypeParameterResolutionTest 2

        let mainName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" }
        let mainNode = new CallGraphNode(mainName, QsSpecializationKind.QsBody, QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = graph.GetAllDependencies mainNode

        [
            [
                (NonNullable<_>.New "A", Int)
            ]
        ]
        |> AssertExpectedDepencency "Main" "Foo" dependencies

    [<Fact>]
    [<Trait("Category","Get Dependencies")>]
    member this.``Type List Resolution`` () =
        let graph = CompileTypeParameterResolutionTest 3

        let mainName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" }
        let mainNode = new CallGraphNode(mainName, QsSpecializationKind.QsBody, QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = graph.GetAllDependencies mainNode

        [
            [
                (NonNullable<_>.New "A", Int)
            ]
        ]
        |> AssertExpectedDepencency "Main" "Foo" dependencies

    [<Fact>]
    [<Trait("Category","Get Dependencies")>]
    member this.``Argument and Type List Resolution`` () =
        let graph = CompileTypeParameterResolutionTest 4

        let mainName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" }
        let mainNode = new CallGraphNode(mainName, QsSpecializationKind.QsBody, QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = graph.GetAllDependencies mainNode

        [
            [
                (NonNullable<_>.New "A", Int)
            ]
        ]
        |> AssertExpectedDepencency "Main" "Foo" dependencies

    [<Fact>]
    [<Trait("Category","Get Dependencies")>]
    member this.``Partial Application One Argument`` () =
        let graph = CompileTypeParameterResolutionTest 5

        let mainName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" }
        let mainNode = new CallGraphNode(mainName, QsSpecializationKind.QsBody, QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = graph.GetAllDependencies mainNode

        [
            [
                (NonNullable<_>.New "A", Int)
            ]
        ]
        |> AssertExpectedDepencency "Main" "Foo" dependencies

    [<Fact>]
    [<Trait("Category","Get Dependencies")>]
    member this.``Partial Application Two Arguments`` () =
        let graph = CompileTypeParameterResolutionTest 6

        let mainName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" }
        let mainNode = new CallGraphNode(mainName, QsSpecializationKind.QsBody, QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = graph.GetAllDependencies mainNode

        [
            [
                (NonNullable<_>.New "A", Double)
                (NonNullable<_>.New "B", Int)
            ]
        ]
        |> AssertExpectedDepencency "Main" "Foo" dependencies

    [<Fact>]
    [<Trait("Category","Get Dependencies")>]
    member this.``Complex Partial Application`` () =
        let graph = CompileTypeParameterResolutionTest 7

        let mainName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" }
        let mainNode = new CallGraphNode(mainName, QsSpecializationKind.QsBody, QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = graph.GetAllDependencies mainNode

        [
            [
                (NonNullable<_>.New "A", String)
                (NonNullable<_>.New "B", Double)
                (NonNullable<_>.New "C", Int)
            ]
        ]
        |> AssertExpectedDepencency "Main" "Foo" dependencies

    [<Fact>]
    [<Trait("Category","Get Dependencies")>]
    member this.``Nested Partial Application`` () =
        let graph = CompileTypeParameterResolutionTest 8

        let mainName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" }
        let mainNode = new CallGraphNode(mainName, QsSpecializationKind.QsBody, QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = graph.GetAllDependencies mainNode

        [
            [
                (NonNullable<_>.New "A", Int)
            ]
        ]
        |> AssertExpectedDepencency "Main" "Foo" dependencies

        [
            [
                (NonNullable<_>.New "B", Int)
            ]
        ]
        |> AssertExpectedDepencency "Main" "Bar" dependencies

    [<Fact>]
    [<Trait("Category","Get Dependencies")>]
    member this.``Operation Returns Operation`` () =
        let graph = CompileTypeParameterResolutionTest 9

        let mainName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" }
        let mainNode = new CallGraphNode(mainName, QsSpecializationKind.QsBody, QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = graph.GetAllDependencies mainNode

        [
            [
                (NonNullable<_>.New "A", Int)
            ]
        ]
        |> AssertExpectedDepencency "Main" "Foo" dependencies

        [
            [
                (NonNullable<_>.New "B", Int)
            ]
        ]
        |> AssertExpectedDepencency "Main" "Bar" dependencies

    [<Fact>]
    [<Trait("Category","Get Dependencies")>]
    member this.``Operation Takes Operation`` () =
        let graph = CompileTypeParameterResolutionTestWithExe 10

        let mainName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" }
        let mainNode = new CallGraphNode(mainName, QsSpecializationKind.QsBody, QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = graph.GetAllDependencies mainNode

        [
            [
                (NonNullable<_>.New "A", Int)
            ]
        ]
        |> AssertExpectedDepencency "Main" "Foo" dependencies

        [
            [
                (NonNullable<_>.New "B", Int)
            ]
        ]
        |> AssertExpectedDepencency "Main" "Bar" dependencies

    [<Fact>]
    [<Trait("Category","Populate Call Graph")>]
    member this.``Basic Entry Point`` () =
        let graph = CompileTypeParameterResolutionTestWithExe 11

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
        let graph = CompileTypeParameterResolutionTestWithExe 12

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
    member this.``Separated From Entry Point By Specialization`` () =
        let graph = CompileTypeParameterResolutionTestWithExe 13

        // The generalized methods of asserting dependencies assumes Body nodes, but
        // this relationship is between a Body and an Adjoint specializations, so we
        // will check manually.
        let mainNode =
            CallGraphNode({ Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" },
                QsSpecializationKind.QsBody, QsNullable<ImmutableArray<ResolvedType>>.Null)

        let adjFooNode =
            CallGraphNode({ Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Foo" },
                QsSpecializationKind.QsAdjoint, QsNullable<ImmutableArray<ResolvedType>>.Null)

        let mainDependencies = graph.GetDirectDependencies mainNode
        Assert.True(mainDependencies.Contains(adjFooNode),
            sprintf "Expected %s to take dependency on %s." "Main" "Adjoint Foo")

        [
            "Foo", [
                "Bar"
            ]
        ]
        |> List.map (fun x -> AssertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

    [<Fact>]
    [<Trait("Category","Populate Call Graph")>]
    member this.``Not Called With Entry Point`` () =
        let graph = CompileTypeParameterResolutionTestWithExe 14

        [
            "Main", [
                "Foo"
            ]
            "Foo", [ ]
        ]
        |> List.map (fun x -> AssertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

        [
            "NotCalled"
        ]
        |> List.map (AssertNotInGraph graph)
        |> ignore

    [<Fact>]
    [<Trait("Category","Populate Call Graph")>]
    member this.``Not Called Without Entry Point`` () =
        let graph = CompileTypeParameterResolutionTest 15

        [
            "Main", [
                "Foo"
            ]
            "Foo", [ ]
        ]
        |> List.map (fun x -> AssertExpectedDirectDependencies (fst x) (snd x) graph)
        |> ignore

        [
            "NotCalled"
        ]
        |> List.map (AssertNotInGraph graph)
        |> ignore

    [<Fact>]
    [<Trait("Category","Populate Call Graph")>]
    member this.``Unrelated Without Entry Point`` () =
        let graph = CompileTypeParameterResolutionTest 16

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
        let graph = CompileTypeParameterResolutionTestWithExe 17
        Assert.True(graph.Count = 0, "Expected call graph to be empty.")

    [<Fact>]
    [<Trait("Category","Populate Call Graph")>]
    member this.``Calls Entry Point From Entry Point`` () =
        let graph = CompileTypeParameterResolutionTestWithExe 18

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
        let graph = CompileTypeParameterResolutionTestWithExe 19

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