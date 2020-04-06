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

    static member CheckResolution (parent, expected, [<ParamArray>] resolutions) = 
        let compareDict (d1 : ImmutableDictionary<_,_>) (d2 : ImmutableDictionary<_,_>) = 
            let keysMismatch = new HashSet<_>(d1.Keys)
            keysMismatch.SymmetricExceptWith d2.Keys 
            keysMismatch.Count = 0 && d1 |> Seq.exists (fun kv -> d2.[kv.Key] <> kv.Value) |> not
        let mutable combined = ImmutableDictionary.Empty
        let success = CallGraph.TryCombineTypeResolutions(parent, &combined, resolutions)
        Assert.True(compareDict expected combined, "combined resolutions did not match the expected ones")
        success

    static member AssertResolution (parent, expected, [<ParamArray>] resolutions) = 
        let success = CallGraphTests.CheckResolution (parent, expected, resolutions)
        Assert.True(success, "overall status indicated as not successful")

    static member AssertResolutionFailure (parent, expected, [<ParamArray>] resolutions) = 
        let success = CallGraphTests.CheckResolution (parent, expected, resolutions)
        Assert.False(success, "overall status indicated as success")


    [<Fact>]
    member this.``Type resolution`` () = 

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

        let res1 = resolution [
            (FooA, FooA |> TypeParameter)
        ]
        let expected = resolution [
            (FooA, FooA |> TypeParameter)
        ]
        CallGraphTests.AssertResolution(Foo, expected, res1)

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

        let res1 = resolution [
            (FooA, FooB |> TypeParameter)
        ]
        let expected = resolution [
            (FooA, FooB |> TypeParameter)
        ]
        CallGraphTests.AssertResolutionFailure(Foo, expected, res1)

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

        let res1 = resolution [
            (FooA, BarA |> TypeParameter)
        ]
        let res2 = resolution [
            (BarA, FooB |> TypeParameter)
        ]
        let expected = resolution [
            (FooA, FooB |> TypeParameter)
        ]
        CallGraphTests.AssertResolutionFailure(Foo, expected, res1, res2)

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
        CallGraphTests.AssertResolution(Foo, expected, res1, res2, res3)