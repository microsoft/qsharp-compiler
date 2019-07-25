﻿module Microsoft.Quantum.QsCompiler.Testing.CompletionParsingTests

open System.Collections.Generic
open Xunit
open Microsoft.Quantum.QsCompiler.TextProcessing.CompletionParsing


let private test text expected =
    Assert.Equal<IEnumerable<IdentifierKind>>(Set.ofList expected, GetExpectedIdentifiers text)

[<Fact>]
let ``Inside namespace parser tests`` () =
    test "" [Keyword "function"; Keyword "operation"; Keyword "newtype"]
    test "f" [Keyword "function"; Keyword "operation"; Keyword "newtype"]
    test "fun" [Keyword "function"; Keyword "operation"; Keyword "newtype"]
    // TODO: test "function" [Keyword "function"; Keyword "operation"; Keyword "newtype"]
    test "o" [Keyword "function"; Keyword "operation"; Keyword "newtype"]
    test "opera" [Keyword "function"; Keyword "operation"; Keyword "newtype"]
    // TODO: test "operation" [Keyword "function"; Keyword "operation"; Keyword "newtype"]
    test "n" [Keyword "function"; Keyword "operation"; Keyword "newtype"]
    test "newt" [Keyword "function"; Keyword "operation"; Keyword "newtype"]
    // TODO: test "newtype" [Keyword "function"; Keyword "operation"; Keyword "newtype"]

[<Fact>]
let ``Function declaration parser tests`` () =
    test "function" [Keyword "function"]
    test "function " [Declaration]
    test "function Foo" [Declaration]
    test "function Foo " []
    test "function Foo (" [Declaration]
    test "function Foo (x" [Declaration]
    test "function Foo (x :" [Type]
    test "function Foo (x : Int" [Type]
    test "function Foo (x : Int)" []
    test "function Foo (x : Int) :" [Type]
    test "function Foo (x : Int) : " [Type]
    test "function Foo (x : Int) : Unit" [Type]
    test "function Foo (x : Int) : Unit " []
    test "function Foo (x : Int) : (Int, MyT" [Type]
    test "function Foo (x : Int) : (Int," [Type]
    test "function Foo (x : Int) : (Int, MyT " []
    test "function Foo (x : (" [Type]
    test "function Foo (x : (Int" [Type]
    test "function Foo (x : (Int," [Type]
    test "function Foo (x : (Int, " [Type]
    test "function Foo (x : (Int, Int" [Type]
    test "function Foo (x : (Int, Int " []
    test "function Foo (x : (Int, Int), " [Declaration]
    test "function Foo (f : (Int -> " [Type]
    test "function Foo (f : (Int -> Int)) : Unit" [Type]
    test "function Foo (f : (Int -> Int)) : Unit " []
    test "function Foo (q : ((Int -> Int) -> " [Type]
    test "function Foo (q : ((Int -> Int) -> Int " []
    test "function Foo (q : ((Int -> Int) -> Int)) : Unit" [Type]
    test "function Foo (q : ((Int->Int)->Int)) : Unit" [Type]
    test "function Foo (q : ((Int -> Int) -> Int)) : Unit" [Type]
    test "function Foo<" [Declaration]
    test "function Foo<'" [Declaration]
    test "function Foo<> (q : 'T) : Unit" [Type]
    test "function Foo<'T> (q : 'T) : Unit" [Type]
    test "function Foo<'T> (q : ('T -> Int)) : Unit" [Type]
    test "function Foo<'T> (q : ('T->Int)) : Unit" [Type]
    test "function Foo<'T> (q : (MyType -> Int)) : Unit" [Type]

[<Fact>]
let ``Operation declaration parser tests`` () =
    test "operation" [Keyword "operation"]
    test "operation " [Declaration]
    test "operation Foo" [Declaration]
    test "operation Foo " []
    test "operation Foo (" [Declaration]
    test "operation Foo (q" [Declaration]
    test "operation Foo (q :" [Type]
    test "operation Foo (q : Qubit" [Type]
    test "operation Foo (q : Qubit)" []
    test "operation Foo (q : Qubit) :" [Type]
    test "operation Foo (q : Qubit) : " [Type]
    test "operation Foo (q : Qubit) : Unit" [Type]
    test "operation Foo (q : Qubit) : Unit " [Keyword "is"]
    test "operation Foo (q : Qubit) : Unit i" [Keyword "is"]
    test "operation Foo (q : Qubit) : Unit is" [Keyword "is"]
    test "operation Foo (q : Qubit) : Unit is " [Characteristic]
    test "operation Foo (q : Qubit) : Unit is A" [Characteristic]
    test "operation Foo (q : Qubit) : Unit is Adj" [Characteristic]
    test "operation Foo (q : Qubit) : Unit is Adj " []
    test "operation Foo (q : Qubit) : Unit is Adj +" [Characteristic]
    test "operation Foo (q : Qubit) : Unit is Adj + " [Characteristic]
    test "operation Foo (q : Qubit) : Unit is Adj + Ctl" [Characteristic]
    test "operation Foo (q : Qubit) : Unit is Adj + Ctl " []
    test "operation Foo (q : Qubit) : Unit is Adj + Cat " []
    test "operation Foo (q : Qubit) : (Int, MyT" [Type]
    test "operation Foo (q : Qubit) : (Int," [Type]
    test "operation Foo (q : Qubit) : (Int, MyT " []
    test "operation Foo (q : (" [Type]
    test "operation Foo (q : (Qubit" [Type]
    test "operation Foo (q : (Qubit," [Type]
    test "operation Foo (q : (Qubit, " [Type]
    test "operation Foo (q : (Qubit, Qubit" [Type]
    test "operation Foo (q : (Qubit, Qubit " []
    test "operation Foo (q : (Qubit, Qubit), " [Declaration]
    test "operation Foo (q : (Qubit => " [Type]
    test "operation Foo (q : (Qubit => Unit" [Type]
    test "operation Foo (q : (Qubit => Unit " [Keyword "is"]
    test "operation Foo (q : (Qubit => Unit is " [Characteristic]
    test "operation Foo (q : (Qubit => Unit is Adj)) : Unit" [Type]
    test "operation Foo (f : (Int -> " [Type]
    test "operation Foo (f : (Int -> Int)) : Unit" [Type]
    test "operation Foo (f : (Int -> Int)) : Unit " [Keyword "is"]
    test "operation Foo (q : (Int => Int)) : Unit" [Type]
    test "operation Foo (q : ((Int -> Int) => " [Type]
    test "operation Foo (q : ((Int -> Int) => Int " [Keyword "is"]
    test "operation Foo (q : ((Int -> Int) => Int)) : Unit" [Type]
    test "operation Foo (q : ((Int->Int)=>Int)) : Unit" [Type]
    test "operation Foo (q : ((Int -> Int) -> Int)) : Unit" [Type]
    test "operation Foo (q : ((Int => Int) => Int)) : Unit" [Type]
    test "operation Foo<'T> (q : 'T) : Unit" [Type]
    test "operation Foo<'T> (q : ('T => Int)) : Unit" [Type]
    test "operation Foo<'T> (q : ('T -> Int)) : Unit" [Type]
    test "operation Foo<'T> (q : ('T->Int)) : Unit" [Type]
    test "operation Foo<'T> (q : (MyType -> Int)) : Unit" [Type]
    test "operation Foo (q : Qubit) : (Int, MyT is Adj" []
    test "operation Foo (q : Qubit) : (Int, MyT is Adj " []

[<Fact>]
let ``Type declaration parser tests`` () =
    test "newtype" [Keyword "newtype"]
    test "newtype " [Declaration]
    test "newtype MyType" [Declaration]
    test "newtype MyType " []
    test "newtype MyType =" [Type]
    test "newtype MyType = " [Type]
    test "newtype MyType = Int" [Type]
    test "newtype MyType = (" [Declaration; Type]
    test "newtype MyType = (In" [Declaration; Type]
    // TODO: test "newtype MyType = (Int" [Declaration; Type]
    test "newtype MyType = (Int," [Declaration; Type]
    test "newtype MyType = (Int, Boo" [Declaration; Type]
    // TODO: test "newtype MyType = (Int, Bool" [Declaration; Type]
    test "newtype MyType = (Int, Bool)" []
    test "newtype MyType = (MyItem :" [Type]
    test "newtype MyType = (MyItem : " [Type]
    test "newtype MyType = (MyItem : In" [Type]
    test "newtype MyType = (MyItem : Int" [Type]
    test "newtype MyType = (MyItem : Int," [Declaration; Type]
    test "newtype MyType = (MyItem : Int, " [Declaration; Type]
    test "newtype MyType = (MyItem : Int, Boo" [Declaration; Type]
    // TODO: test "newtype MyType = (MyItem : Int, Bool" [Declaration; Type]
    test "newtype MyType = (MyItem : Int, Bool)" []
