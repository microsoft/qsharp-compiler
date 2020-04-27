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
open Microsoft.Quantum.QsCompiler.Transformations.CallGraphNS
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

    let otherResolution (res : (QsQualifiedName * NonNullable<string> * QsTypeKind<_,_,_,_>) list) =
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

    let CheckResolutionListMatch (resList1 : ImmutableDictionary<_,_> list) (resList2 : ImmutableDictionary<_,_> list) =
        let sameLength = resList1.Length = resList2.Length
        sameLength && resList1 |> List.forall (fun res1 -> resList2 |> List.exists (fun res2 -> CheckResolutionMatch res1 res2))

    let AssertExpectedResolutionList (expected : ImmutableDictionary<_,_> list) (given : ImmutableDictionary<_,_> list) =
        Assert.True(CheckResolutionListMatch expected given, "Given resolutions did not match the expected resolutions.")

    let OtherAssertExpectedResolutions name (given : Dictionary<_,ImmutableArray<_>>) expected =
        let opName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New name }
        let opNode = new CallGraphNode(CallableName = opName, Kind = QsSpecializationKind.QsBody, TypeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null)
        let expected = expected |> List.map (fun x -> x |> List.map (fun y -> (opName, fst y, snd y)) |> otherResolution)
        Assert.True(given.ContainsKey(opNode), sprintf "Expected Main to take dependency on %s." name)
        let edges = given.[opNode]
        Assert.True(edges.Length = expected.Length, sprintf "Expected exactly %i edge(s) from Main to %s." expected.Length name);
        let given = List.map (fun (x : CallGraphEdge) -> x.ParamResolutions) (Seq.toList edges)
        AssertExpectedResolutionList expected given

    let CheckCombinedResolution (parent, expected : ImmutableDictionary<_,_>, [<ParamArray>] resolutions) =
        let mutable combined = ImmutableDictionary.Empty
        let success = TypeParamStuff.TryCombineTypeResolutionsWithTarget(parent, &combined, resolutions)
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
            resolution [
                (FooA, BarA |> TypeParameter)
                (FooB, Int)
            ]
            resolution [
                (BarA, Int)
            ]
        |]
        let expected = resolution [
            (FooA, Int)
            (FooB, Int)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Resolution to Type Parameter`` () =

        let given = [|
            resolution [
                (FooA, BarA |> TypeParameter)
            ]
            resolution [
                (BarA, BazA |> TypeParameter)
            ]
        |]
        let expected = resolution [
            (FooA, BazA |> TypeParameter)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Resolution via Identity Mapping`` () =

        let given = [|
            resolution [
                (FooA, FooA |> TypeParameter)
            ]
            resolution [
                (FooA, Int)
            ]
        |]
        let expected = resolution [
            (FooA, Int)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Multi-Stage Resolution`` () =

        let given = [|
            resolution [
                (FooA, BarA |> TypeParameter)
            ]
            resolution [
                (BarA, Int)
                (FooB, Int)
            ]
        |]
        let expected = resolution [
            (FooA, Int)
            (FooB, Int)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Multiple Resolutions to Concrete`` () =

        let given = [|
            resolution [
                (FooA, BarA |> TypeParameter)
                (FooB, BarA |> TypeParameter)
            ]
            resolution [
                (BarA, Int)
            ]
        |]
        let expected = resolution [
            (FooA, Int)
            (FooB, Int)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Multiple Resolutions to Type Parameter`` () =

        let given = [|
            resolution [
                (FooA, BarA |> TypeParameter)
                (FooB, BarA |> TypeParameter)
            ]
            resolution [
                (BarA, BazA |> TypeParameter)
            ]
            resolution [
                (BazA, Double)
            ]
        |]
        let expected = resolution [
            (FooA, Double)
            (FooB, Double)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Multi-Stage Resolution of Multiple Resolutions to Concrete`` () =

        let given = [|
            resolution [
                (FooA, BarA |> TypeParameter)
            ]
            resolution [
                (FooB, BarA |> TypeParameter)
            ]
            resolution [
                (BarA, Int)
            ]
        |]
        let expected = resolution [
            (FooA, Int)
            (FooB, Int)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Redundant Resolution to Concrete`` () =

        let given = [|
            resolution [
                (FooA, BarA |> TypeParameter)
            ]
            resolution [
                (BarA, Int)
                (FooA, Int)
            ]
        |]
        let expected = resolution [
            (FooA, Int)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Redundant Resolution to Type Parameter`` () =

        let given = [|
            resolution [
                (FooA, BarA |> TypeParameter)
            ]
            resolution [
                (BarA, BazA |> TypeParameter)
            ]
            resolution [
                (FooA, BazA |> TypeParameter)
            ]
        |]
        let expected = resolution [
            (FooA, BazA |> TypeParameter)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Conflicting Resolution to Concrete`` () =

        let given = [|
            resolution [
                (FooA, BarA |> TypeParameter)
            ]
            resolution [
                (BarA, Int)
                (FooA, Double)
            ]
        |]
        let expected = resolution [
            (FooA, Double)
        ]

        AssertCombinedResolutionFailure(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Conflicting Resolution to Type Parameter`` () =

        let given = [|
            resolution [
                (FooA, BarA |> TypeParameter)
            ]
            resolution [
                (BarA, BazA |> TypeParameter)
                (FooA, BarC |> TypeParameter)
            ]
        |]
        let expected = resolution [
            (FooA, BarC |> TypeParameter)
        ]

        AssertCombinedResolutionFailure(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Direct Resolution to Native`` () =

        let given = [|
            resolution [
                (FooA, FooA |> TypeParameter)
            ]
        |]
        let expected = resolution [
            (FooA, FooA |> TypeParameter)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Indirect Resolution to Native`` () =

        let given = [|
            resolution [
                (FooA, BarA |> TypeParameter)
            ]
            resolution [
                (BarA, BazA |> TypeParameter)
            ]
            resolution [
                (BazA, FooA |> TypeParameter)
            ]
        |]
        let expected = resolution [
            (FooA, FooA |> TypeParameter)
        ]

        AssertCombinedResolution(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Direct Resolution Constrains Native`` () =

        let given = [|
            resolution [
                (FooA, FooB |> TypeParameter)
            ]
        |]
        let expected = resolution [
            (FooA, FooB |> TypeParameter)
        ]

        AssertCombinedResolutionFailure(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Indirect Resolution Constrains Native`` () =

        let given = [|
            resolution [
                (FooA, BarA |> TypeParameter)
            ]
            resolution [
                (BarA, BazA |> TypeParameter)
            ]
            resolution [
                (BazA, FooB |> TypeParameter)
            ]
        |]
        let expected = resolution [
            (FooA, FooB |> TypeParameter)
        ]

        AssertCombinedResolutionFailure(Foo, expected, given)

    [<Fact>]
    [<Trait("Category","Get Dependencies")>]
    member this.``Get All Dependencies`` () =
        let graph = CompileTypeParameterResolutionTest 1

        let mainName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" }
        let mainNode = new CallGraphNode(CallableName = mainName, Kind = QsSpecializationKind.QsBody, TypeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = graph.GetAllDependencies mainNode

        [
            [
                (NonNullable<_>.New "A", Int)
                (NonNullable<_>.New "B", String)
            ]
        ]
        |> OtherAssertExpectedResolutions "Foo" dependencies

        [
            [
                (NonNullable<_>.New "X", Int)
            ]
            [
                (NonNullable<_>.New "X", String)
            ]
        ]
        |> OtherAssertExpectedResolutions "Bar" dependencies

        [
            [
                (NonNullable<_>.New "Y", String)
            ]
        ]
        |> OtherAssertExpectedResolutions "Baz" dependencies

    [<Fact>]
    [<Trait("Category","Get Dependencies")>]
    member this.``Argument Resolution`` () =
        let graph = CompileTypeParameterResolutionTest 2

        let mainName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" }
        let mainNode = new CallGraphNode(CallableName = mainName, Kind = QsSpecializationKind.QsBody, TypeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = graph.GetAllDependencies mainNode

        [
            [
                (NonNullable<_>.New "A", Int)
            ]
        ]
        |> OtherAssertExpectedResolutions "Foo" dependencies

    [<Fact>]
    [<Trait("Category","Get Dependencies")>]
    member this.``Type List Resolution`` () =
        let graph = CompileTypeParameterResolutionTest 3

        let mainName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" }
        let mainNode = new CallGraphNode(CallableName = mainName, Kind = QsSpecializationKind.QsBody, TypeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = graph.GetAllDependencies mainNode

        [
            [
                (NonNullable<_>.New "A", Int)
            ]
        ]
        |> OtherAssertExpectedResolutions "Foo" dependencies

    [<Fact>]
    [<Trait("Category","Get Dependencies")>]
    member this.``Argument and Type List Resolution`` () =
        let graph = CompileTypeParameterResolutionTest 4

        let mainName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" }
        let mainNode = new CallGraphNode(CallableName = mainName, Kind = QsSpecializationKind.QsBody, TypeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = graph.GetAllDependencies mainNode

        [
            [
                (NonNullable<_>.New "A", Int)
            ]
        ]
        |> OtherAssertExpectedResolutions "Foo" dependencies

    [<Fact>]
    [<Trait("Category","Get Dependencies")>]
    member this.``Partial Application One Argument`` () =
        let graph = CompileTypeParameterResolutionTest 5

        let mainName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" }
        let mainNode = new CallGraphNode(CallableName = mainName, Kind = QsSpecializationKind.QsBody, TypeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = graph.GetAllDependencies mainNode

        [
            [
                (NonNullable<_>.New "A", Int)
            ]
        ]
        |> OtherAssertExpectedResolutions "Foo" dependencies

    [<Fact>]
    [<Trait("Category","Get Dependencies")>]
    member this.``Partial Application Two Arguments`` () =
        let graph = CompileTypeParameterResolutionTest 6

        let mainName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" }
        let mainNode = new CallGraphNode(CallableName = mainName, Kind = QsSpecializationKind.QsBody, TypeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = graph.GetAllDependencies mainNode

        [
            [
                (NonNullable<_>.New "A", Double)
                (NonNullable<_>.New "B", Int)
            ]
        ]
        |> OtherAssertExpectedResolutions "Foo" dependencies

    [<Fact>]
    [<Trait("Category","Get Dependencies")>]
    member this.``Complex Partial Application`` () =
        let graph = CompileTypeParameterResolutionTest 7

        let mainName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" }
        let mainNode = new CallGraphNode(CallableName = mainName, Kind = QsSpecializationKind.QsBody, TypeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = graph.GetAllDependencies mainNode

        [
            [
                (NonNullable<_>.New "A", String)
                (NonNullable<_>.New "B", Double)
                (NonNullable<_>.New "C", Int)
            ]
        ]
        |> OtherAssertExpectedResolutions "Foo" dependencies

    [<Fact>]
    [<Trait("Category","Get Dependencies")>]
    member this.``Nested Partial Application`` () =
        let graph = CompileTypeParameterResolutionTest 8

        let mainName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" }
        let mainNode = new CallGraphNode(CallableName = mainName, Kind = QsSpecializationKind.QsBody, TypeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = graph.GetAllDependencies mainNode

        [
            [
                (NonNullable<_>.New "A", Int)
            ]
        ]
        |> OtherAssertExpectedResolutions "Foo" dependencies

        [
            [
                (NonNullable<_>.New "B", Int)
            ]
        ]
        |> OtherAssertExpectedResolutions "Bar" dependencies

    [<Fact>]
    [<Trait("Category","Get Dependencies")>]
    member this.``Operation Returns Operation`` () =
        let graph = CompileTypeParameterResolutionTest 9

        let mainName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" }
        let mainNode = new CallGraphNode(CallableName = mainName, Kind = QsSpecializationKind.QsBody, TypeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = graph.GetAllDependencies mainNode

        [
            [
                (NonNullable<_>.New "A", Int)
            ]
        ]
        |> OtherAssertExpectedResolutions "Foo" dependencies

        [
            [
                (NonNullable<_>.New "B", Int)
            ]
        ]
        |> OtherAssertExpectedResolutions "Bar" dependencies

    [<Fact>]
    [<Trait("Category","Get Dependencies")>]
    member this.``Operation Takes Operation`` () =
        let graph = CompileTypeParameterResolutionTest 10

        let mainName = { Namespace = NonNullable<_>.New Signatures.TypeParameterResolutionNS; Name = NonNullable<_>.New "Main" }
        let mainNode = new CallGraphNode(CallableName = mainName, Kind = QsSpecializationKind.QsBody, TypeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null)
        let dependencies = graph.GetAllDependencies mainNode

        [
            [
                (NonNullable<_>.New "A", Int)
            ]
        ]
        |> OtherAssertExpectedResolutions "Foo" dependencies

        [
            [
                (NonNullable<_>.New "B", Int)
            ]
        ]
        |> OtherAssertExpectedResolutions "Bar" dependencies

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