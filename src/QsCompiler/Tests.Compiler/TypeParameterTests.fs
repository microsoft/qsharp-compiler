// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System
open System.Collections.Immutable
open System.IO
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Xunit


type TypeParameterTests() =

    let TypeParameterNS = "Microsoft.Quantum.Testing.TypeParameter"

    let qualifiedName name =
        (TypeParameterNS, name) |> QsQualifiedName.New

    let typeParameter (id: string) =
        let pieces = id.Split(".")
        Assert.True(pieces.Length = 2)
        let parent = qualifiedName pieces.[0]
        let name = pieces.[1]
        QsTypeParameter.New(parent, name, Null)

    let FooA = typeParameter "Foo.A"
    let FooB = typeParameter "Foo.B"
    let FooC = typeParameter "Foo.C"
    let BarA = typeParameter "Bar.A"
    let BarB = typeParameter "Bar.B"
    let BazA = typeParameter "Baz.A"

    let MakeTupleType types =
        types |> Seq.map ResolvedType.New |> ImmutableArray.CreateRange |> TupleType

    let MakeArrayType ``type`` = ResolvedType.New ``type`` |> ArrayType

    let ResolutionFromParam (res: (QsTypeParameter * QsTypeKind<_, _, _, _>) list) =
        res.ToImmutableDictionary((fun (tp, _) -> tp.Origin, tp.TypeName), snd >> ResolvedType.New)

    let CheckResolutionMatch (res1: ImmutableDictionary<_, _>) (res2: ImmutableDictionary<_, _>) =
        let keysMismatch =
            ImmutableHashSet
                .CreateRange(res1.Keys).SymmetricExcept res2.Keys

        keysMismatch.Count = 0 && res1 |> Seq.exists (fun kv -> res2.[kv.Key] <> kv.Value) |> not

    let AssertExpectedResolution expected given =
        Assert.True(CheckResolutionMatch expected given, "Given resolutions did not match the expected resolutions.")

    let CheckCombinedResolution expected (resolutions: ImmutableDictionary<(QsQualifiedName * string), ResolvedType> []) =
        let combination = TypeResolutionCombination(resolutions)
        AssertExpectedResolution expected combination.CombinedResolutionDictionary
        combination.IsValid

    let AssertCombinedResolution expected resolutions =
        let success = CheckCombinedResolution expected resolutions
        Assert.True(success, "Combining type resolutions was not successful.")

    let AssertCombinedResolutionFailure expected resolutions =
        let success = CheckCombinedResolution expected resolutions
        Assert.False(success, "Combining type resolutions should have failed.")

    let compilationManager = new CompilationUnitManager(new Action<Exception>(fun ex -> failwith ex.Message))

    let getTempFile () =
        new Uri(Path.GetFullPath(Path.GetRandomFileName()))

    let getManager uri content =
        CompilationUnitManager.InitializeFileManager
            (uri, content, compilationManager.PublishDiagnostics, compilationManager.LogException)

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

    let CompileTypeParameterTest testNumber =
        let srcChunks = ReadAndChunkSourceFile "TypeParameter.qs"
        srcChunks.Length >= testNumber |> Assert.True
        let compilationDataStructures = BuildContent <| srcChunks.[testNumber - 1]
        let processedCompilation = compilationDataStructures.BuiltCompilation
        Assert.NotNull processedCompilation
        processedCompilation

    let GetCallableWithName compilation ns name =
        compilation.Namespaces
        |> Seq.filter (fun x -> x.Name = ns)
        |> GlobalCallableResolutions
        |> Seq.find (fun x -> x.Key.Name = name)
        |> (fun x -> x.Value)

    let GetMainExpression (compilation: QsCompilation) =
        let mainCallable = GetCallableWithName compilation TypeParameterNS "Main"

        let body =
            mainCallable.Specializations
            |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsBody)
            |> fun x ->
                match x.Implementation with
                | Provided (_, body) -> body
                | _ -> failwith "Expected but did not find Provided Implementation"

        Assert.True(body.Statements.Length = 1)

        match body.Statements.[0].Statement with
        | QsExpressionStatement expression -> expression
        | _ -> failwith "Expected but did not find an Expression Statement"


    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Resolution to Concrete``() =

        let given =
            [| ResolutionFromParam [ (FooA, BarA |> TypeParameter)
                                     (FooB, Int) ]
               ResolutionFromParam [ (BarA, Int) ] |]

        let expected =
            ResolutionFromParam [ (FooA, Int)
                                  (FooB, Int)
                                  (BarA, Int) ]

        AssertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Resolution to Type Parameter``() =

        let given =
            [| ResolutionFromParam [ (FooA, BarA |> TypeParameter) ]
               ResolutionFromParam [ (BarA, BazA |> TypeParameter) ] |]

        let expected =
            ResolutionFromParam [ (FooA, BazA |> TypeParameter)
                                  (BarA, BazA |> TypeParameter) ]

        AssertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Resolution via Identity Mapping``() =

        let given =
            [| ResolutionFromParam [ (FooA, FooA |> TypeParameter) ]
               ResolutionFromParam [ (FooA, Int) ] |]

        let expected = ResolutionFromParam [ (FooA, Int) ]

        AssertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Multi-Stage Resolution``() =

        let given =
            [| ResolutionFromParam [ (FooA, BarA |> TypeParameter) ]
               ResolutionFromParam [ (BarA, Int)
                                     (FooB, Int) ] |]

        let expected =
            ResolutionFromParam [ (FooA, Int)
                                  (FooB, Int)
                                  (BarA, Int) ]

        AssertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Multiple Resolutions to Concrete``() =

        let given =
            [| ResolutionFromParam [ (FooA, BarA |> TypeParameter)
                                     (FooB, BarA |> TypeParameter) ]
               ResolutionFromParam [ (BarA, Int) ] |]

        let expected =
            ResolutionFromParam [ (FooA, Int)
                                  (FooB, Int)
                                  (BarA, Int) ]

        AssertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Multiple Resolutions to Type Parameter``() =

        let given =
            [| ResolutionFromParam [ (FooA, BarA |> TypeParameter)
                                     (FooB, BarA |> TypeParameter) ]
               ResolutionFromParam [ (BarA, BazA |> TypeParameter) ]
               ResolutionFromParam [ (BazA, Double) ] |]

        let expected =
            ResolutionFromParam [ (FooA, Double)
                                  (FooB, Double)
                                  (BarA, Double)
                                  (BazA, Double) ]

        AssertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Multi-Stage Resolution of Multiple Resolutions to Concrete``() =

        let given =
            [| ResolutionFromParam [ (FooA, BarA |> TypeParameter) ]
               ResolutionFromParam [ (FooB, BarA |> TypeParameter) ]
               ResolutionFromParam [ (BarA, Int) ] |]

        let expected =
            ResolutionFromParam [ (FooA, Int)
                                  (FooB, Int)
                                  (BarA, Int) ]

        AssertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Multi-Stage Resolution of Multiple Resolutions to Type Parameter``() =

        let given =
            [| ResolutionFromParam [ (FooA, BarA |> TypeParameter) ]
               ResolutionFromParam [ (BarA, BazA |> TypeParameter)
                                     (FooB, BarA |> TypeParameter) ]
               ResolutionFromParam [ (BazA, Int) ] |]

        let expected =
            ResolutionFromParam [ (FooA, Int)
                                  (FooB, Int)
                                  (BarA, Int)
                                  (BazA, Int) ]

        AssertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Redundant Resolution to Concrete``() =

        let given =
            [| ResolutionFromParam [ (FooA, BarA |> TypeParameter) ]
               ResolutionFromParam [ (BarA, Int)
                                     (FooA, Int) ] |]

        let expected =
            ResolutionFromParam [ (FooA, Int)
                                  (BarA, Int) ]

        AssertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Redundant Resolution to Type Parameter``() =

        let given =
            [| ResolutionFromParam [ (FooA, BarA |> TypeParameter) ]
               ResolutionFromParam [ (BarA, BazA |> TypeParameter) ]
               ResolutionFromParam [ (FooA, BazA |> TypeParameter) ] |]

        let expected =
            ResolutionFromParam [ (FooA, BazA |> TypeParameter)
                                  (BarA, BazA |> TypeParameter) ]

        AssertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Conflicting Resolution to Concrete``() =

        let given =
            [| ResolutionFromParam [ (FooA, BarA |> TypeParameter) ]
               ResolutionFromParam [ (BarA, Int)
                                     (FooA, Double) ] |]

        let expected =
            ResolutionFromParam [ (FooA, Double)
                                  (BarA, Int) ]

        AssertCombinedResolutionFailure expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Conflicting Resolution to Type Parameter``() =

        let given =
            [| ResolutionFromParam [ (FooA, BarA |> TypeParameter) ]
               ResolutionFromParam [ (BarA, BazA |> TypeParameter)
                                     (FooA, BarB |> TypeParameter) ] |]

        let expected =
            ResolutionFromParam [ (FooA, BarB |> TypeParameter)
                                  (BarA, BazA |> TypeParameter) ]

        AssertCombinedResolutionFailure expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Direct Resolution to Native``() =
        let given = [| ResolutionFromParam [ (FooA, FooA |> TypeParameter) ] |]
        let expected = ResolutionFromParam [ (FooA, FooA |> TypeParameter) ]
        AssertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Indirect Resolution to Native``() =

        let given =
            [| ResolutionFromParam [ (FooA, BarA |> TypeParameter) ]
               ResolutionFromParam [ (BarA, BazA |> TypeParameter) ]
               ResolutionFromParam [ (BazA, FooA |> TypeParameter) ] |]

        let expected =
            ResolutionFromParam [ (FooA, FooA |> TypeParameter)
                                  (BarA, FooA |> TypeParameter)
                                  (BazA, FooA |> TypeParameter) ]

        AssertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Direct Resolution Constrains Native``() =

        let given = [| ResolutionFromParam [ (FooA, FooB |> TypeParameter) ] |]
        let expected = ResolutionFromParam [ (FooA, FooB |> TypeParameter) ]
        AssertCombinedResolutionFailure expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Indirect Resolution Constrains Native``() =

        let given =
            [| ResolutionFromParam [ (FooA, BarA |> TypeParameter) ]
               ResolutionFromParam [ (BarA, BazA |> TypeParameter) ]
               ResolutionFromParam [ (BazA, FooB |> TypeParameter) ] |]

        let expected =
            ResolutionFromParam [ (FooA, FooB |> TypeParameter)
                                  (BarA, FooB |> TypeParameter)
                                  (BazA, FooB |> TypeParameter) ]

        AssertCombinedResolutionFailure expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Inner Cycle Constrains Type Parameter``() =

        let given =
            [| ResolutionFromParam [ (FooA, BarA |> TypeParameter) ]
               ResolutionFromParam [ (BarA, BazA |> TypeParameter) ]
               ResolutionFromParam [ (BazA, BarB |> TypeParameter) ] |]

        let expected =
            ResolutionFromParam [ (FooA, BarB |> TypeParameter)
                                  (BarA, BarB |> TypeParameter)
                                  (BazA, BarB |> TypeParameter) ]

        AssertCombinedResolutionFailure expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Nested Type Paramter Resolution``() =
        let given =
            [| ResolutionFromParam [ (FooA, [ BarA |> TypeParameter; Int ] |> MakeTupleType) ]
               ResolutionFromParam [ (BarA, [ String; Double ] |> MakeTupleType) ] |]

        let expected =
            ResolutionFromParam [ (FooA,
                                   [ [ String; Double ] |> MakeTupleType
                                     Int ]
                                   |> MakeTupleType)
                                  (BarA, [ String; Double ] |> MakeTupleType) ]

        AssertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Nested Constricted Resolution``() =
        let given =
            [| ResolutionFromParam [ (FooA, [ FooB |> TypeParameter; Int ] |> MakeTupleType) ] |]

        let expected = ResolutionFromParam [ (FooA, [ FooB |> TypeParameter; Int ] |> MakeTupleType) ]

        AssertCombinedResolutionFailure expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Constricting Nested Self Resolution``() =
        let given =
            [| ResolutionFromParam [ (FooA, [ FooA |> TypeParameter; Int ] |> MakeTupleType) ] |]

        let expected = ResolutionFromParam [ (FooA, [ FooA |> TypeParameter; Int ] |> MakeTupleType) ]

        AssertCombinedResolutionFailure expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Constricting Array Nested Self Resolution``() =
        let given = [| ResolutionFromParam [ (FooA, FooA |> TypeParameter |> MakeArrayType) ] |]
        let expected = ResolutionFromParam [ (FooA, FooA |> TypeParameter |> MakeArrayType) ]
        AssertCombinedResolutionFailure expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Constricting Indirect Nested Self Resolution``() =
        let given =
            [| ResolutionFromParam [ (FooA, BarA |> TypeParameter) ]
               ResolutionFromParam [ (BarA, [ Int; FooA |> TypeParameter ] |> MakeTupleType) ] |]

        let expected =
            ResolutionFromParam [ (BarA,
                                   [ Int
                                     [ Int; FooA |> TypeParameter ] |> MakeTupleType ]
                                   |> MakeTupleType)
                                  (FooA, [ Int; FooA |> TypeParameter ] |> MakeTupleType) ]

        AssertCombinedResolutionFailure expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Constricting Array Indirect Nested Self Resolution``() =
        let given =
            [| ResolutionFromParam [ (FooA, BarA |> TypeParameter) ]
               ResolutionFromParam [ (BarA, FooA |> TypeParameter |> MakeArrayType) ] |]

        let expected =
            ResolutionFromParam [ (BarA, FooA |> TypeParameter |> MakeArrayType |> MakeArrayType)
                                  (FooA, FooA |> TypeParameter |> MakeArrayType) ]

        AssertCombinedResolutionFailure expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Single Dictonary Resolution``() =
        let given =
            [| ResolutionFromParam [ (FooA, BarA |> TypeParameter)
                                     (BarA, Int) ] |]

        let expected =
            ResolutionFromParam [ (FooA, Int)
                                  (BarA, Int) ]

        AssertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Parsing Expressions")>]
    member this.``Identifier Resolution``() =
        let expression = CompileTypeParameterTest 1 |> GetMainExpression
        let combination = TypeResolutionCombination(expression)
        let given = combination.CombinedResolutionDictionary

        let expected =
            ResolutionFromParam [ (FooA, Double)
                                  (FooB, Int)
                                  (FooC, String) ]

        AssertExpectedResolution expected given

    [<Fact>]
    [<Trait("Category", "Parsing Expressions")>]
    member this.``Adjoint Application Resolution``() =
        let expression = CompileTypeParameterTest 2 |> GetMainExpression
        let combination = TypeResolutionCombination(expression)
        let given = combination.CombinedResolutionDictionary

        let expected =
            ResolutionFromParam [ (FooA, Double)
                                  (FooB, Int)
                                  (FooC, String) ]

        AssertExpectedResolution expected given

    [<Fact>]
    [<Trait("Category", "Parsing Expressions")>]
    member this.``Controlled Application Resolution``() =
        let expression = CompileTypeParameterTest 3 |> GetMainExpression
        let combination = TypeResolutionCombination(expression)
        let given = combination.CombinedResolutionDictionary

        let expected =
            ResolutionFromParam [ (FooA, Double)
                                  (FooB, Int)
                                  (FooC, String) ]

        AssertExpectedResolution expected given

    [<Fact>]
    [<Trait("Category", "Parsing Expressions")>]
    member this.``Partial Application Resolution``() =
        let expression = CompileTypeParameterTest 4 |> GetMainExpression
        let combination = TypeResolutionCombination(expression)
        let given = combination.CombinedResolutionDictionary

        let expected =
            ResolutionFromParam [ (FooA, Double)
                                  (FooB, Int)
                                  (FooC, String) ]

        AssertExpectedResolution expected given

    [<Fact>]
    [<Trait("Category", "Parsing Expressions")>]
    member this.``Sub-call Resolution``() =
        let expression = CompileTypeParameterTest 5 |> GetMainExpression
        let combination = TypeResolutionCombination(expression)
        let given = combination.CombinedResolutionDictionary
        let expected = ResolutionFromParam []

        AssertExpectedResolution expected given

    [<Fact>]
    [<Trait("Category", "Parsing Expressions")>]
    member this.``Argument Sub-call Resolution``() =
        let expression = CompileTypeParameterTest 6 |> GetMainExpression
        let combination = TypeResolutionCombination(expression)
        let given = combination.CombinedResolutionDictionary

        let expected =
            ResolutionFromParam [ (FooA, Double)
                                  (FooB, Int)
                                  (FooC, String) ]

        AssertExpectedResolution expected given
