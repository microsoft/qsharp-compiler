// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

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

    let compareDict (d1 : ImmutableDictionary<_,_>) (d2 : ImmutableDictionary<_,_>) = 
        let keysMismatch = new HashSet<_>(d1.Keys)
        keysMismatch.SymmetricExceptWith d2.Keys 
        keysMismatch.Count = 0 && d1 |> Seq.exists (fun kv -> d2.[kv.Key] <> kv.Value) |> not

    let Foo  = qualifiedName "Foo"
    let Bar  = qualifiedName "Bar"

    let FooA = typeParameter "Foo.A"
    let FooB = typeParameter "Foo.B"
    let BarA = typeParameter "Bar.A"
    let BarC = typeParameter "Bar.C"
    let BazA = typeParameter "Baz.A"


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

        let mutable combined = ImmutableDictionary.Empty
        let success = CallGraph.TryCombineTypeResolutions(Foo, &combined, res1, res2)
        Assert.True(success)
        Assert.True(compareDict expected combined)
