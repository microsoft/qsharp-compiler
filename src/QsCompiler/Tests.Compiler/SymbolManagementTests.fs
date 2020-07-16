// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.SymbolManagementTests

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SymbolManagement
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Xunit


let private rnd = System.Random(1); 
let getRange () =
    Range.Create (Position.Create (rnd.Next()) (rnd.Next()))
                 (Position.Create (rnd.Next()) (rnd.Next()))
    |> Value

let toQualName (ns : string, name : string) =
    {Namespace = NonNullable<_>.New ns; Name = NonNullable<_>.New name}

let toTypeParam (origin : string * string) (name : string) = 
    let ns, originName = origin
    TypeParameter ({Origin = toQualName (ns, originName); TypeName = NonNullable<_>.New name; Range = getRange()})

let toUdt (qualName : string * string) =
    let fullName = qualName |> toQualName
    UserDefinedType {Namespace = fullName.Namespace; Name = fullName.Name; Range = getRange()}

let toTuple (types : _ seq) =
    TupleType ((types |> Seq.map ResolvedType.New).ToImmutableArray())

let thash (rt) = NamespaceManager.TypeHash (ResolvedType.New(rt))

[<Fact>]
let ``type hash tests`` () =
    [
        (UnitType, UnitType, true);
        (UnitType, Int, false);
        (toTypeParam ("A", "B") "C", UnitType, false);
        (toTypeParam ("A", "B") "C", toTypeParam ("A", "B") "C", true);
        (toTypeParam ("A", "B") "C", toTypeParam ("A", "B") "Z", false); 
        (toUdt ("A", "B"), toUdt ("A", "B"), true);
        (toUdt ("A", "B"), toUdt ("A", "X"), false);
        (toTuple [Int; Bool], toTuple [Bool; String], false)
        (toTuple [Int; Bool], toTuple [Int; Bool], true)
    ]
    |> Seq.iter (
        fun (left, right, expected) ->
            let ok = (expected = ((thash left) = (thash right)))
            Assert.True ok
    )
