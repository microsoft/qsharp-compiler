﻿module Microsoft.Quantum.QsCompiler.Testing.CompletionParsingTests

open System.Collections.Generic
open Xunit
open Microsoft.Quantum.QsCompiler.TextProcessing
open Microsoft.Quantum.QsCompiler.TextProcessing.CompletionParsing


let private test text expected =
    Assert.Equal<IEnumerable<IdentifierKind>>(Set.ofList expected, GetExpectedIdentifiers NamespaceTopLevel text)

[<Fact>]
let ``Inside namespace parser tests`` () =
    test "" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test "f" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test "fun" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test "function" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test "o" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test "ope" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test "opera" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test "operation" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test "n" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test "newt" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test "newtype" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test "open" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]

[<Fact>]
let ``Function declaration parser tests`` () =
    test "function " [Declaration]
    test "function Foo" [Declaration]
    test "function Foo " []
    test "function Foo (" [Declaration]
    test "function Foo (x" [Declaration]
    test "function Foo (x " []
    test "function Foo (x :" [Type]
    test "function Foo (x : Int" [Type]
    test "function Foo (x : Int " []
    test "function Foo (x : Int)" []
    test "function Foo (x : Int) :" [Type]
    test "function Foo (x : Int) : " [Type]
    test "function Foo (x : Int) : Unit" [Type]
    test "function Foo (x : Int) : Unit " []
    test "function Foo (x : Int) : (Int, MyT" [Type]
    test "function Foo (x : Int) : (Int," [Type]
    test "function Foo (x : Int) : (Int, " [Type]
    test "function Foo (x : Int) : (Int, MyT " []
    test "function Foo (x : (" [Type]
    test "function Foo (x : (Int" [Type]
    test "function Foo (x : (Int," [Type]
    test "function Foo (x : (Int, " [Type]
    test "function Foo (x : (Int, Int" [Type]
    test "function Foo (x : (Int, Int " []
    test "function Foo (x : (Int, Int), " [Declaration]
    test "function Foo (f : (Int -> " [Type]
    test "function Foo (f : (Int -> Int" [Type]
    test "function Foo (f : (Int -> Int " []
    test "function Foo (f : (Int -> Int)" []
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
    test "operation Foo (q : Qubit) : Unit is (" [Characteristic]
    test "operation Foo (q : Qubit) : Unit is ( " [Characteristic]
    test "operation Foo (q : Qubit) : Unit is (Adj" [Characteristic]
    test "operation Foo (q : Qubit) : Unit is (Adj " []
    test "operation Foo (q : Qubit) : Unit is (Adj + " [Characteristic]
    test "operation Foo (q : Qubit) : Unit is (Adj + Ctl" [Characteristic]
    test "operation Foo (q : Qubit) : Unit is (Adj + Ctl)" []    
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
    test "operation Foo (q : (Qubit => Unit is Adj" [Characteristic]
    test "operation Foo (q : (Qubit => Unit is Adj " []
    test "operation Foo (q : (Qubit => Unit is Adj + " [Characteristic]
    test "operation Foo (q : (Qubit => Unit is Adj)" []
    test "operation Foo (q : (Qubit => Unit is Adj)) : Unit" [Type]
    test "operation Foo (q : ((Qubit => Unit " []
    test "operation Foo (q : ((Qubit => Unit) " [Keyword "is"]
    test "operation Foo (q : ((Qubit => Unit) is " [Characteristic]
    test "operation Foo (q : ((Qubit => Unit) is Adj" [Characteristic]
    test "operation Foo (q : ((Qubit => Unit) is Adj)" []
    test "operation Foo (q : ((Qubit => Unit) is Adj))" []
    test "operation Foo (q : ((Qubit => Unit) is Adj)) :" [Type]
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
    test "newtype " [Declaration]
    test "newtype MyType" [Declaration]
    test "newtype MyType " []
    test "newtype MyType =" [Type]
    test "newtype MyType = " [Type]
    test "newtype MyType = Int" [Type]
    test "newtype MyType = (" [Declaration; Type]
    test "newtype MyType = (In" [Declaration; Type]
    test "newtype MyType = (Int" [Declaration; Type]
    test "newtype MyType = (Int " []
    test "newtype MyType = (Int," [Declaration; Type]
    test "newtype MyType = (Int, Boo" [Declaration; Type]
    test "newtype MyType = (Int, Bool" [Declaration; Type]
    test "newtype MyType = (Int, Bool)" []
    test "newtype MyType = (MyItem :" [Type]
    test "newtype MyType = (MyItem : " [Type]
    test "newtype MyType = (MyItem : In" [Type]
    test "newtype MyType = (MyItem : Int" [Type]
    test "newtype MyType = (MyItem : Int," [Declaration; Type]
    test "newtype MyType = (MyItem : Int, " [Declaration; Type]
    test "newtype MyType = (MyItem : Int, Boo" [Declaration; Type]
    test "newtype MyType = (MyItem : Int, Bool" [Declaration; Type]
    test "newtype MyType = (MyItem : Int, Bool)" []
    test "newtype MyType = (MyItem : Int, (" [Declaration; Type]
    test "newtype MyType = (MyItem : Int, (Item2" [Declaration; Type]
    test "newtype MyType = (MyItem : Int, (Item2 " []
    test "newtype MyType = (MyItem : Int, (Item2, " [Declaration; Type]
    test "newtype MyType = (MyItem : Int, (Item2, Item3 :" [Type]
    test "newtype MyType = (MyItem : Int, (Item2, Item3 : Int)" []
    test "newtype MyType = (MyItem : Int, (Item2, Item3 : Int))" []

[<Fact>]
let ``Open directive parser tests`` () =
    test "open " [Namespace]
    test "open Microsoft" [Namespace]
    test "open Microsoft." [Member ("Microsoft", Namespace)]
    test "open Microsoft.Quantum" [Member ("Microsoft", Namespace)]
    test "open Microsoft.Quantum." [Member ("Microsoft.Quantum", Namespace)]
    test "open Microsoft.Quantum.Math" [Member ("Microsoft.Quantum", Namespace)]
    test "open Microsoft.Quantum.Math " [Keyword "as"]
    test "open Microsoft.Quantum.Math as" [Keyword "as"]
    test "open Microsoft.Quantum.Math as " [Declaration]
    test "open Microsoft.Quantum.Math as Math" [Declaration]
    test "open Microsoft.Quantum.Math as Math " []
    test "open Microsoft.Quantum.Math as My" [Declaration]
    test "open Microsoft.Quantum.Math as My." [Member ("My", Declaration)]
    test "open Microsoft.Quantum.Math as My.Math" [Member ("My", Declaration)]
    test "open Microsoft.Quantum.Math as My.Math " []
