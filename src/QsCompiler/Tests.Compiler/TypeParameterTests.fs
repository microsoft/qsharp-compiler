// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System
open System.IO
open System.Text.RegularExpressions
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.ClassicallyControlled
open Microsoft.Quantum.QsCompiler.Transformations.GetTypeParameterResolutions;
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open Xunit
open System.Collections.Immutable


type TypeParameterTests () =

    let qualifiedName name =
        ("NS" |> NonNullable<string>.New, name |> NonNullable<string>.New) |> QsQualifiedName.New

    let typeParameter (id : string) =
        let pieces = id.Split(".")
        Assert.True(pieces.Length = 2)
        let parent = qualifiedName pieces.[0]
        let name = pieces.[1] |> NonNullable<string>.New
        QsTypeParameter.New (parent, name, Null)

    let FooA = typeParameter "Foo.A"
    let FooB = typeParameter "Foo.B"
    let BarA = typeParameter "Bar.A"
    let BarB = typeParameter "Bar.B"
    let BazA = typeParameter "Baz.A"

    let MakeTupleType types =
        types |> Seq.map ResolvedType.New |> ImmutableArray.CreateRange |> TupleType

    let ResolutionFromParam (res : (QsTypeParameter * QsTypeKind<_,_,_,_>) list) =
        res.ToImmutableDictionary((fun (tp,_) -> tp.Origin, tp.TypeName), snd >> ResolvedType.New)

    let CheckResolutionMatch (res1 : ImmutableDictionary<_,_>) (res2 : ImmutableDictionary<_,_> ) =
        let keysMismatch = ImmutableHashSet.CreateRange(res1.Keys).SymmetricExcept res2.Keys
        keysMismatch.Count = 0 && res1 |> Seq.exists (fun kv -> res2.[kv.Key] <> kv.Value) |> not

    let AssertExpectedResolution (expected : ImmutableDictionary<_,_>) (given : ImmutableDictionary<_,_> ) =
        Assert.True(CheckResolutionMatch expected given, "Given resolutions did not match the expected resolutions.")

    let CheckCombinedResolution (expected : ImmutableDictionary<_,_>, [<ParamArray>] resolutions) =
        let mutable combined = ImmutableDictionary.Empty
        let success = TypeParamUtils.TryCombineTypeResolutions(&combined, resolutions)
        AssertExpectedResolution expected combined
        success

    let AssertCombinedResolution (expected, [<ParamArray>] resolutions) =
        let success = CheckCombinedResolution (expected, resolutions)
        Assert.True(success, "Combining type resolutions was not successful.")

    let AssertCombinedResolutionFailure (expected, [<ParamArray>] resolutions) =
        let success = CheckCombinedResolution (expected, resolutions)
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
            (BarA, Int)
        ]

        AssertCombinedResolution(expected, given)

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
            (BarA, BazA |> TypeParameter)
        ]

        AssertCombinedResolution(expected, given)

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

        AssertCombinedResolution(expected, given)

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
            (BarA, Int)
        ]

        AssertCombinedResolution(expected, given)

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
            (BarA, Int)
        ]

        AssertCombinedResolution(expected, given)

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
            (BarA, Double)
            (BazA, Double)
        ]

        AssertCombinedResolution(expected, given)

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
            (BarA, Int)
        ]

        AssertCombinedResolution(expected, given)

    [<Fact>]
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
            (BarA, Int)
            (BazA, Int)
        ]

        AssertCombinedResolution(expected, given)

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
            (BarA, Int)
        ]

        AssertCombinedResolution(expected, given)

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
            (BarA, BazA |> TypeParameter)
        ]

        AssertCombinedResolution(expected, given)

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
            (BarA, Int)
        ]

        AssertCombinedResolutionFailure(expected, given)

    [<Fact>]
    [<Trait("Category","Type Resolution")>]
    member this.``Conflicting Resolution to Type Parameter`` () =

        let given = [|
            ResolutionFromParam [
                (FooA, BarA |> TypeParameter)
            ]
            ResolutionFromParam [
                (BarA, BazA |> TypeParameter)
                (FooA, BarB |> TypeParameter)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, BarB |> TypeParameter)
            (BarA, BazA |> TypeParameter)
        ]

        AssertCombinedResolutionFailure(expected, given)

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

        AssertCombinedResolution(expected, given)

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
            (BarA, FooA |> TypeParameter)
            (BazA, FooA |> TypeParameter)
        ]

        AssertCombinedResolution(expected, given)

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

        AssertCombinedResolutionFailure(expected, given)

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
            (BarA, FooB |> TypeParameter)
            (BazA, FooB |> TypeParameter)
        ]

        AssertCombinedResolutionFailure(expected, given)

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
                (BazA, BarB |> TypeParameter)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, BarB |> TypeParameter)
            (BarA, BarB |> TypeParameter)
            (BazA, BarB |> TypeParameter)
        ]

        AssertCombinedResolutionFailure(expected, given)

    [<Fact>]
    [<Trait("Category","Type resolution")>]
    member this.``Nested Type Paramter Resolution`` () =
        let given = [|
            ResolutionFromParam [
                (FooA, [BarA |> TypeParameter; Int] |> MakeTupleType)
            ]
            ResolutionFromParam [
                (BarA, [String; Double] |> MakeTupleType)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, [[String; Double] |> MakeTupleType; Int] |> MakeTupleType)
            (BarA, [String; Double] |> MakeTupleType)
        ]

        AssertCombinedResolution(expected, given)

    [<Fact>]
    [<Trait("Category","Type resolution")>]
    member this.``Nested Constricted Resolution`` () =
        let given = [|
            ResolutionFromParam [
                (FooA, [FooB |> TypeParameter; Int] |> MakeTupleType)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, [FooB |> TypeParameter; Int] |> MakeTupleType)
        ]

        AssertCombinedResolutionFailure(expected, given)

    [<Fact>]
    [<Trait("Category","Type resolution")>]
    member this.``Nested Self Resolution`` () =
        let given = [|
            ResolutionFromParam [
                (FooA, [FooA |> TypeParameter; BarA |> TypeParameter] |> MakeTupleType)
            ]
            ResolutionFromParam [
                (BarA, Int)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, [FooA |> TypeParameter; Int] |> MakeTupleType)
            (BarA, Int)
        ]

        AssertCombinedResolution(expected, given)

    [<Fact>]
    [<Trait("Category","Type resolution")>]
    member this.``Single Dictonary Resolution`` () =
        let given = [|
            ResolutionFromParam [
                (FooA, BarA |> TypeParameter)
                (BarA, Int)
            ]
        |]
        let expected = ResolutionFromParam [
            (FooA, Int)
            (BarA, Int)
        ]

        AssertCombinedResolution(expected, given)
