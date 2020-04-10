// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System
open System.Collections.Generic
open System.Collections.Immutable
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
    
    let resolution (res : (QsTypeParameter * QsTypeKind<_,_,_,_>) list) =
        res.ToImmutableDictionary((fun (tp,_) -> tp.Origin, tp.TypeName), snd >> ResolvedType.New)

    let Foo  = qualifiedName "Foo"
    let Bar  = qualifiedName "Bar"

    let FooA = typeParameter "Foo.A"
    let FooB = typeParameter "Foo.B"
    let BarA = typeParameter "Bar.A"
    let BarC = typeParameter "Bar.C"
    let BazA = typeParameter "Baz.A"

    static member CheckResolution (parent, expected : IDictionary<_,_>, [<ParamArray>] resolutions : _ []) = 
        let expectedKeys = ImmutableHashSet.CreateRange(expected.Keys) 
        let compareWithExpected (d : ImmutableDictionary<_,_>) = 
            let keysMismatch = expectedKeys.SymmetricExcept d.Keys 
            keysMismatch.Count = 0 && expected |> Seq.exists (fun kv -> d.[kv.Key] <> kv.Value) |> not
        let mutable combined, success = ImmutableDictionary.Empty, true

        let VerifyResolution dicts = 
            success <- success && CallGraph.TryCombineTypeResolutions(&combined, dicts)
            combined <- combined 
                |> Seq.choose (fun kv -> if (fst kv.Key).Equals parent then Some kv else None)
                |> ImmutableDictionary.CreateRange
            Assert.True(compareWithExpected combined, "combined resolutions did not match the expected ones")

        for startIndex = 1 to resolutions.Length do
            Array.concat [resolutions.[startIndex ..]; resolutions.[..startIndex - 1]] |> VerifyResolution
        success

    static member AssertResolution (parent, expected, [<ParamArray>] resolutions) = 
        let success = CallGraphTests.CheckResolution (parent, expected, resolutions)
        Assert.True(success, "Combining type resolutions was not successful.")

    static member AssertResolutionFailure (parent, expected, [<ParamArray>] resolutions) = 
        let success = CallGraphTests.CheckResolution (parent, expected, resolutions)
        Assert.False(success, "Combining type resolutions should have failed.")


    [<Fact>]
    [<Trait("Category","Type resolution")>]
    member this.``Type resolution: resolution to concrete`` () = 

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
    [<Trait("Category","Type resolution")>]
    member this.``Type resolution: resolution to type parameter`` () = 

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
    [<Trait("Category","Type resolution")>]
    member this.``Type resolution: resolution via identity mapping`` () = 

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
    [<Trait("Category","Type resolution")>]
    member this.``Type resolution: multi-stage resolution`` () = 

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
    [<Trait("Category","Type resolution")>]
    member this.``Type resolution: multiple resolutions to concrete`` () = 

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
    [<Trait("Category","Type resolution")>]
    member this.``Type resolution: multiple resolutions to type parameter`` () = 

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
    [<Trait("Category","Type resolution")>]
    member this.``Type resolution: multi-stage resolution of multiple resolutions to concrete`` () = 

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
    [<Trait("Category","Type resolution")>]
    member this.``Type resolution: multi-stage resolution of multiple resolutions to type parameter`` () = 

        let res1 = resolution [
            (FooA, BarA |> TypeParameter)
        ]
        let res2 = resolution [
            (BarA, BazA |> TypeParameter)
            (FooB, BarA |> TypeParameter)
        ]
        let res3 = resolution [
            (BazA, Int)
        ]
        let expected = resolution [
            (FooA, Int)
            (FooB, Int)
        ]
        CallGraphTests.AssertResolution(Foo, expected, res1, res2, res3)

    [<Fact>]
    [<Trait("Category","Type resolution")>]
    member this.``Type resolution: redundant resolution to concrete`` () = 

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
    [<Trait("Category","Type resolution")>]
    member this.``Type resolution: redundant resolution to type parameter`` () = 

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
    [<Trait("Category","Type resolution")>]
    member this.``Type resolution: conflicting resolution to concrete`` () = 

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
    [<Trait("Category","Type resolution")>]
    member this.``Type resolution: conflicting resolution to type parameter`` () = 

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
    [<Trait("Category","Type resolution")>]
    member this.``Type resolution: direct resolution to native`` () = 

        let res1 = resolution [
            (FooA, FooA |> TypeParameter)
        ]
        let expected = resolution [
            (FooA, FooA |> TypeParameter)
        ]
        CallGraphTests.AssertResolution(Foo, expected, res1)

    [<Fact>]
    [<Trait("Category","Type resolution")>]
    member this.``Type resolution: indirect resolution to native`` () = 

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
    [<Trait("Category","Type resolution")>]
    member this.``Type resolution: direct resolution constrains native`` () = 

        let res1 = resolution [
            (FooA, FooB |> TypeParameter)
        ]
        let expected = resolution [
            (FooA, FooB |> TypeParameter)
        ]
        CallGraphTests.AssertResolutionFailure(Foo, expected, res1)

    [<Fact>]
    [<Trait("Category","Type resolution")>]
    member this.``Type resolution: indirect resolution constrains native`` () = 

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
    [<Trait("Category","Type resolution")>]
    member this.``Type resolution: inner cycle constrains type parameter`` () = 

        let res1 = resolution [
            (FooA, BarA |> TypeParameter)
        ]
        let res2 = resolution [
            (BarA, BazA |> TypeParameter)
        ]
        let res3 = resolution [
            (BazA, BarC |> TypeParameter) // TODO: for performance reasons it would be nice to detect this case as well and error here
        ]
        let expected = resolution [
            (FooA, BarC |> TypeParameter)
        ]
        CallGraphTests.AssertResolutionFailure(Foo, expected, res1, res2, res3)