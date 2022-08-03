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

    let typeParameterNS = "Microsoft.Quantum.Testing.TypeParameter"

    let qualifiedName name =
        (typeParameterNS, name) |> QsQualifiedName.New

    let typeParameter (id: string) =
        let pieces = id.Split(".")
        Assert.True(pieces.Length = 2)
        let parent = qualifiedName pieces.[0]
        let name = pieces.[1]
        QsTypeParameter.New(parent, name)

    let fooA = typeParameter "Foo.A"
    let fooB = typeParameter "Foo.B"
    let fooC = typeParameter "Foo.C"
    let barA = typeParameter "Bar.A"
    let barB = typeParameter "Bar.B"
    let bazA = typeParameter "Baz.A"

    let makeTupleType types =
        types |> Seq.map ResolvedType.New |> ImmutableArray.CreateRange |> TupleType

    let makeArrayType ``type`` = ResolvedType.New ``type`` |> ArrayType

    let resolutionFromParam (res: (QsTypeParameter * QsTypeKind<_, _, _, _>) list) =
        res.ToImmutableDictionary((fun (tp, _) -> tp.Origin, tp.TypeName), snd >> ResolvedType.New)

    let checkResolutionMatch (res1: ImmutableDictionary<_, _>) (res2: ImmutableDictionary<_, _>) =
        let keysMismatch = ImmutableHashSet.CreateRange(res1.Keys).SymmetricExcept res2.Keys
        keysMismatch.Count = 0 && res1 |> Seq.exists (fun kv -> res2.[kv.Key] <> kv.Value) |> not

    let assertExpectedResolution expected given =
        Assert.True(checkResolutionMatch expected given, "Given resolutions did not match the expected resolutions.")

    let checkCombinedResolution
        expected
        (resolutions: ImmutableDictionary<(QsQualifiedName * string), ResolvedType> [])
        =
        let combination = TypeResolutionCombination(resolutions)
        assertExpectedResolution expected combination.CombinedResolutionDictionary
        combination.IsValid

    let assertCombinedResolution expected resolutions =
        let success = checkCombinedResolution expected resolutions
        Assert.True(success, "Combining type resolutions was not successful.")

    let assertCombinedResolutionFailure expected resolutions =
        let success = checkCombinedResolution expected resolutions
        Assert.False(success, "Combining type resolutions should have failed.")

    let compilationManager =
        new CompilationUnitManager(ProjectProperties.Empty, (fun ex -> failwith ex.Message))

    let getTempFile () =
        Uri(Path.GetFullPath(Path.GetRandomFileName()))

    let getManager uri content =
        CompilationUnitManager.InitializeFileManager(
            uri,
            content,
            compilationManager.PublishDiagnostics,
            compilationManager.LogException
        )

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

    let compileTypeParameterTest testNumber =
        let srcChunks = readAndChunkSourceFile "TypeParameter.qs"
        srcChunks.Length >= testNumber |> Assert.True
        let compilationDataStructures = buildContent <| srcChunks.[testNumber - 1]
        let processedCompilation = compilationDataStructures.BuiltCompilation
        Assert.NotNull processedCompilation
        processedCompilation

    let getCallableWithName compilation ns name =
        compilation.Namespaces
        |> Seq.filter (fun x -> x.Name = ns)
        |> GlobalCallableResolutions
        |> Seq.find (fun x -> x.Key.Name = name)
        |> (fun x -> x.Value)

    let getMainExpression (compilation: QsCompilation) =
        let mainCallable = getCallableWithName compilation typeParameterNS "Main"

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
            [|
                resolutionFromParam [ (fooA, barA |> TypeParameter)
                                      (fooB, Int) ]
                resolutionFromParam [ (barA, Int) ]
            |]

        let expected =
            resolutionFromParam [ (fooA, Int)
                                  (fooB, Int)
                                  (barA, Int) ]

        assertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Resolution to Type Parameter``() =

        let given =
            [|
                resolutionFromParam [ (fooA, barA |> TypeParameter) ]
                resolutionFromParam [ (barA, bazA |> TypeParameter) ]
            |]

        let expected =
            resolutionFromParam [ (fooA, bazA |> TypeParameter)
                                  (barA, bazA |> TypeParameter) ]

        assertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Resolution via Identity Mapping``() =

        let given =
            [|
                resolutionFromParam [ (fooA, fooA |> TypeParameter) ]
                resolutionFromParam [ (fooA, Int) ]
            |]

        let expected = resolutionFromParam [ (fooA, Int) ]

        assertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Multi-Stage Resolution``() =

        let given =
            [|
                resolutionFromParam [ (fooA, barA |> TypeParameter) ]
                resolutionFromParam [ (barA, Int)
                                      (fooB, Int) ]
            |]

        let expected =
            resolutionFromParam [ (fooA, Int)
                                  (fooB, Int)
                                  (barA, Int) ]

        assertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Multiple Resolutions to Concrete``() =

        let given =
            [|
                resolutionFromParam [ (fooA, barA |> TypeParameter)
                                      (fooB, barA |> TypeParameter) ]
                resolutionFromParam [ (barA, Int) ]
            |]

        let expected =
            resolutionFromParam [ (fooA, Int)
                                  (fooB, Int)
                                  (barA, Int) ]

        assertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Multiple Resolutions to Type Parameter``() =

        let given =
            [|
                resolutionFromParam [ (fooA, barA |> TypeParameter)
                                      (fooB, barA |> TypeParameter) ]
                resolutionFromParam [ (barA, bazA |> TypeParameter) ]
                resolutionFromParam [ (bazA, Double) ]
            |]

        let expected =
            resolutionFromParam [ (fooA, Double)
                                  (fooB, Double)
                                  (barA, Double)
                                  (bazA, Double) ]

        assertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Multi-Stage Resolution of Multiple Resolutions to Concrete``() =

        let given =
            [|
                resolutionFromParam [ (fooA, barA |> TypeParameter) ]
                resolutionFromParam [ (fooB, barA |> TypeParameter) ]
                resolutionFromParam [ (barA, Int) ]
            |]

        let expected =
            resolutionFromParam [ (fooA, Int)
                                  (fooB, Int)
                                  (barA, Int) ]

        assertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Multi-Stage Resolution of Multiple Resolutions to Type Parameter``() =

        let given =
            [|
                resolutionFromParam [ (fooA, barA |> TypeParameter) ]
                resolutionFromParam [ (barA, bazA |> TypeParameter)
                                      (fooB, barA |> TypeParameter) ]
                resolutionFromParam [ (bazA, Int) ]
            |]

        let expected =
            resolutionFromParam [ (fooA, Int)
                                  (fooB, Int)
                                  (barA, Int)
                                  (bazA, Int) ]

        assertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Redundant Resolution to Concrete``() =

        let given =
            [|
                resolutionFromParam [ (fooA, barA |> TypeParameter) ]
                resolutionFromParam [ (barA, Int)
                                      (fooA, Int) ]
            |]

        let expected =
            resolutionFromParam [ (fooA, Int)
                                  (barA, Int) ]

        assertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Redundant Resolution to Type Parameter``() =

        let given =
            [|
                resolutionFromParam [ (fooA, barA |> TypeParameter) ]
                resolutionFromParam [ (barA, bazA |> TypeParameter) ]
                resolutionFromParam [ (fooA, bazA |> TypeParameter) ]
            |]

        let expected =
            resolutionFromParam [ (fooA, bazA |> TypeParameter)
                                  (barA, bazA |> TypeParameter) ]

        assertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Conflicting Resolution to Concrete``() =

        let given =
            [|
                resolutionFromParam [ (fooA, barA |> TypeParameter) ]
                resolutionFromParam [ (barA, Int)
                                      (fooA, Double) ]
            |]

        let expected =
            resolutionFromParam [ (fooA, Double)
                                  (barA, Int) ]

        assertCombinedResolutionFailure expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Conflicting Resolution to Type Parameter``() =

        let given =
            [|
                resolutionFromParam [ (fooA, barA |> TypeParameter) ]
                resolutionFromParam [ (barA, bazA |> TypeParameter)
                                      (fooA, barB |> TypeParameter) ]
            |]

        let expected =
            resolutionFromParam [ (fooA, barB |> TypeParameter)
                                  (barA, bazA |> TypeParameter) ]

        assertCombinedResolutionFailure expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Direct Resolution to Native``() =

        let given = [| resolutionFromParam [ (fooA, fooA |> TypeParameter) ] |]
        let expected = resolutionFromParam [ (fooA, fooA |> TypeParameter) ]
        assertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Indirect Resolution to Native``() =

        let given =
            [|
                resolutionFromParam [ (fooA, barA |> TypeParameter) ]
                resolutionFromParam [ (barA, bazA |> TypeParameter) ]
                resolutionFromParam [ (bazA, fooA |> TypeParameter) ]
            |]

        let expected =
            resolutionFromParam [ (fooA, fooA |> TypeParameter)
                                  (barA, fooA |> TypeParameter)
                                  (bazA, fooA |> TypeParameter) ]

        assertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Direct Resolution Constrains Native``() =

        let given = [| resolutionFromParam [ (fooA, fooB |> TypeParameter) ] |]
        let expected = resolutionFromParam [ (fooA, fooB |> TypeParameter) ]
        assertCombinedResolutionFailure expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Indirect Resolution Constrains Native``() =

        let given =
            [|
                resolutionFromParam [ (fooA, barA |> TypeParameter) ]
                resolutionFromParam [ (barA, bazA |> TypeParameter) ]
                resolutionFromParam [ (bazA, fooB |> TypeParameter) ]
            |]

        let expected =
            resolutionFromParam [ (fooA, fooB |> TypeParameter)
                                  (barA, fooB |> TypeParameter)
                                  (bazA, fooB |> TypeParameter) ]

        assertCombinedResolutionFailure expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Inner Cycle Constrains Type Parameter``() =

        let given =
            [|
                resolutionFromParam [ (fooA, barA |> TypeParameter) ]
                resolutionFromParam [ (barA, bazA |> TypeParameter) ]
                resolutionFromParam [ (bazA, barB |> TypeParameter) ]
            |]

        let expected =
            resolutionFromParam [ (fooA, barB |> TypeParameter)
                                  (barA, barB |> TypeParameter)
                                  (bazA, barB |> TypeParameter) ]

        assertCombinedResolutionFailure expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Nested Type Paramter Resolution``() =
        let given =
            [|
                resolutionFromParam [ (fooA, [ barA |> TypeParameter; Int ] |> makeTupleType) ]
                resolutionFromParam [ (barA, [ String; Double ] |> makeTupleType) ]
            |]

        let expected =
            resolutionFromParam [ (fooA, [ [ String; Double ] |> makeTupleType; Int ] |> makeTupleType)
                                  (barA, [ String; Double ] |> makeTupleType) ]

        assertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Nested Constricted Resolution``() =
        let given =
            [|
                resolutionFromParam [ (fooA, [ fooB |> TypeParameter; Int ] |> makeTupleType) ]
            |]

        let expected = resolutionFromParam [ (fooA, [ fooB |> TypeParameter; Int ] |> makeTupleType) ]

        assertCombinedResolutionFailure expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Constricting Nested Self Resolution``() =
        let given =
            [|
                resolutionFromParam [ (fooA, [ fooA |> TypeParameter; Int ] |> makeTupleType) ]
            |]

        let expected = resolutionFromParam [ (fooA, [ fooA |> TypeParameter; Int ] |> makeTupleType) ]

        assertCombinedResolutionFailure expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Constricting Array Nested Self Resolution``() =
        let given = [| resolutionFromParam [ (fooA, fooA |> TypeParameter |> makeArrayType) ] |]
        let expected = resolutionFromParam [ (fooA, fooA |> TypeParameter |> makeArrayType) ]
        assertCombinedResolutionFailure expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Constricting Indirect Nested Self Resolution``() =
        let given =
            [|
                resolutionFromParam [ (fooA, barA |> TypeParameter) ]
                resolutionFromParam [ (barA, [ Int; fooA |> TypeParameter ] |> makeTupleType) ]
            |]

        let expected =
            resolutionFromParam [ (barA, [ Int; [ Int; fooA |> TypeParameter ] |> makeTupleType ] |> makeTupleType)
                                  (fooA, [ Int; fooA |> TypeParameter ] |> makeTupleType) ]

        assertCombinedResolutionFailure expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Constricting Array Indirect Nested Self Resolution``() =
        let given =
            [|
                resolutionFromParam [ (fooA, barA |> TypeParameter) ]
                resolutionFromParam [ (barA, fooA |> TypeParameter |> makeArrayType) ]
            |]

        let expected =
            resolutionFromParam [ (barA, fooA |> TypeParameter |> makeArrayType |> makeArrayType)
                                  (fooA, fooA |> TypeParameter |> makeArrayType) ]

        assertCombinedResolutionFailure expected given

    [<Fact>]
    [<Trait("Category", "Type Resolution")>]
    member this.``Single Dictonary Resolution``() =
        let given =
            [|
                resolutionFromParam [ (fooA, barA |> TypeParameter)
                                      (barA, Int) ]
            |]

        let expected =
            resolutionFromParam [ (fooA, Int)
                                  (barA, Int) ]

        assertCombinedResolution expected given

    [<Fact>]
    [<Trait("Category", "Parsing Expressions")>]
    member this.``Identifier Resolution``() =
        let expression = compileTypeParameterTest 1 |> getMainExpression

        let combination = TypeResolutionCombination(expression)
        let given = combination.CombinedResolutionDictionary

        let expected =
            resolutionFromParam [ (fooA, Double)
                                  (fooB, Int)
                                  (fooC, String) ]

        assertExpectedResolution expected given

    [<Fact>]
    [<Trait("Category", "Parsing Expressions")>]
    member this.``Adjoint Application Resolution``() =
        let expression = compileTypeParameterTest 2 |> getMainExpression

        let combination = TypeResolutionCombination(expression)
        let given = combination.CombinedResolutionDictionary

        let expected =
            resolutionFromParam [ (fooA, Double)
                                  (fooB, Int)
                                  (fooC, String) ]

        assertExpectedResolution expected given

    [<Fact>]
    [<Trait("Category", "Parsing Expressions")>]
    member this.``Controlled Application Resolution``() =
        let expression = compileTypeParameterTest 3 |> getMainExpression

        let combination = TypeResolutionCombination(expression)
        let given = combination.CombinedResolutionDictionary

        let expected =
            resolutionFromParam [ (fooA, Double)
                                  (fooB, Int)
                                  (fooC, String) ]

        assertExpectedResolution expected given

    [<Fact>]
    [<Trait("Category", "Parsing Expressions")>]
    member this.``Partial Application Resolution``() =
        let expression = compileTypeParameterTest 4 |> getMainExpression

        let combination = TypeResolutionCombination(expression)
        let given = combination.CombinedResolutionDictionary

        let expected =
            resolutionFromParam [ (fooA, Double)
                                  (fooB, Int)
                                  (fooC, String) ]

        assertExpectedResolution expected given

    [<Fact>]
    [<Trait("Category", "Parsing Expressions")>]
    member this.``Sub-call Resolution``() =
        let expression = compileTypeParameterTest 5 |> getMainExpression
        let combination = TypeResolutionCombination(expression)
        let given = combination.CombinedResolutionDictionary
        let expected = [ fooA, Int; fooB, Int; fooC, Int ] |> resolutionFromParam
        assertExpectedResolution expected given

    [<Fact>]
    [<Trait("Category", "Parsing Expressions")>]
    member this.``Argument Sub-call Resolution``() =
        let expression = compileTypeParameterTest 6 |> getMainExpression

        let combination = TypeResolutionCombination(expression)
        let given = combination.CombinedResolutionDictionary

        let expected =
            resolutionFromParam [ (fooA, Double)
                                  (fooB, Int)
                                  (fooC, String) ]

        assertExpectedResolution expected given
