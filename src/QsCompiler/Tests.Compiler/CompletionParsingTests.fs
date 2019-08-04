module Microsoft.Quantum.QsCompiler.Testing.CompletionParsingTests

open System.Collections.Generic
open Xunit
open Microsoft.Quantum.QsCompiler.TextProcessing.CompletionParsing


let private test env text expected =
    Assert.Equal<IEnumerable<IdentifierKind>>(Set.ofList expected, GetExpectedIdentifiers env text)

let private types =
    [
        Keyword "BigInt"
        Keyword "Bool"
        Keyword "Double"
        Keyword "Int"
        Keyword "Pauli"
        Keyword "Qubit"
        Keyword "Range"
        Keyword "Result"
        Keyword "String"
        Keyword "Unit"
        UserDefinedType
    ]

[<Fact>]
let ``Inside namespace parser tests`` () =
    test NamespaceTopLevel "" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test NamespaceTopLevel "f" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test NamespaceTopLevel "fun" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test NamespaceTopLevel "function" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test NamespaceTopLevel "o" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test NamespaceTopLevel "ope" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test NamespaceTopLevel "opera" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test NamespaceTopLevel "operation" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test NamespaceTopLevel "n" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test NamespaceTopLevel "newt" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test NamespaceTopLevel "newtype" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test NamespaceTopLevel "open" [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]

[<Fact>]
let ``Function declaration parser tests`` () =
    test NamespaceTopLevel "function " [Declaration]
    test NamespaceTopLevel "function Foo" [Declaration]
    test NamespaceTopLevel "function Foo " []
    test NamespaceTopLevel "function Foo (" [Declaration]
    test NamespaceTopLevel "function Foo (x" [Declaration]
    test NamespaceTopLevel "function Foo (x " []
    test NamespaceTopLevel "function Foo (x :" types
    test NamespaceTopLevel "function Foo (x : Int" types
    test NamespaceTopLevel "function Foo (x : Int " []
    test NamespaceTopLevel "function Foo (x : Int)" []
    test NamespaceTopLevel "function Foo (x : Int) :" types
    test NamespaceTopLevel "function Foo (x : Int) : " types
    test NamespaceTopLevel "function Foo (x : Int) : Unit" types
    test NamespaceTopLevel "function Foo (x : Int) : Unit " []
    test NamespaceTopLevel "function Foo (x : Int) : (Int, MyT" types
    test NamespaceTopLevel "function Foo (x : Int) : (Int," types
    test NamespaceTopLevel "function Foo (x : Int) : (Int, " types
    test NamespaceTopLevel "function Foo (x : Int) : (Int, MyT " []
    test NamespaceTopLevel "function Foo (x : (" types
    test NamespaceTopLevel "function Foo (x : (Int" types
    test NamespaceTopLevel "function Foo (x : (Int," types
    test NamespaceTopLevel "function Foo (x : (Int, " types
    test NamespaceTopLevel "function Foo (x : (Int, Int" types
    test NamespaceTopLevel "function Foo (x : (Int, Int " []
    test NamespaceTopLevel "function Foo (x : (Int, Int), " [Declaration]
    test NamespaceTopLevel "function Foo (f : (Int -> " types
    test NamespaceTopLevel "function Foo (f : (Int -> Int" types
    test NamespaceTopLevel "function Foo (f : (Int -> Int " []
    test NamespaceTopLevel "function Foo (f : (Int -> Int)" []
    test NamespaceTopLevel "function Foo (f : (Int -> Int)) : Unit" types
    test NamespaceTopLevel "function Foo (f : (Int -> Int)) : Unit " []
    test NamespaceTopLevel "function Foo (q : ((Int -> Int) -> " types
    test NamespaceTopLevel "function Foo (q : ((Int -> Int) -> Int " []
    test NamespaceTopLevel "function Foo (q : ((Int -> Int) -> Int)) : Unit" types
    test NamespaceTopLevel "function Foo (q : ((Int->Int)->Int)) : Unit" types
    test NamespaceTopLevel "function Foo (q : ((Int -> Int) -> Int)) : Unit" types
    test NamespaceTopLevel "function Foo<" []
    test NamespaceTopLevel "function Foo<'" [Declaration]
    test NamespaceTopLevel "function Foo<> (q : 'T) : Unit" types
    test NamespaceTopLevel "function Foo<'T> (q : '" [TypeParameter]
    test NamespaceTopLevel "function Foo<'T> (q : 'T) : Unit" types
    test NamespaceTopLevel "function Foo<'T> (q : ('T -> Int)) : Unit" types
    test NamespaceTopLevel "function Foo<'T> (q : ('T->Int)) : Unit" types
    test NamespaceTopLevel "function Foo<'T> (q : (MyType -> Int)) : Unit" types

[<Fact>]
let ``Operation declaration parser tests`` () =
    test NamespaceTopLevel "operation " [Declaration]
    test NamespaceTopLevel "operation Foo" [Declaration]
    test NamespaceTopLevel "operation Foo " []
    test NamespaceTopLevel "operation Foo (" [Declaration]
    test NamespaceTopLevel "operation Foo (q" [Declaration]
    test NamespaceTopLevel "operation Foo (q :" types
    test NamespaceTopLevel "operation Foo (q : Qubit" types
    test NamespaceTopLevel "operation Foo (q : Qubit)" []
    test NamespaceTopLevel "operation Foo (q : Qubit) :" types
    test NamespaceTopLevel "operation Foo (q : Qubit) : " types
    test NamespaceTopLevel "operation Foo (q : Qubit) : Unit" types
    test NamespaceTopLevel "operation Foo (q : Qubit) : Unit " [Keyword "is"]
    test NamespaceTopLevel "operation Foo (q : Qubit) : Unit i" [Keyword "is"]
    test NamespaceTopLevel "operation Foo (q : Qubit) : Unit is" [Keyword "is"]
    test NamespaceTopLevel "operation Foo (q : Qubit) : Unit is " [Characteristic]
    test NamespaceTopLevel "operation Foo (q : Qubit) : Unit is A" [Characteristic]
    test NamespaceTopLevel "operation Foo (q : Qubit) : Unit is Adj" [Characteristic]
    test NamespaceTopLevel "operation Foo (q : Qubit) : Unit is Adj " []
    test NamespaceTopLevel "operation Foo (q : Qubit) : Unit is Adj +" [Characteristic]
    test NamespaceTopLevel "operation Foo (q : Qubit) : Unit is Adj + " [Characteristic]
    test NamespaceTopLevel "operation Foo (q : Qubit) : Unit is Adj + Ctl" [Characteristic]
    test NamespaceTopLevel "operation Foo (q : Qubit) : Unit is (" [Characteristic]
    test NamespaceTopLevel "operation Foo (q : Qubit) : Unit is ( " [Characteristic]
    test NamespaceTopLevel "operation Foo (q : Qubit) : Unit is (Adj" [Characteristic]
    // TODO: test NamespaceTopLevel "operation Foo (q : Qubit) : Unit is (Adj " []
    test NamespaceTopLevel "operation Foo (q : Qubit) : Unit is (Adj + " [Characteristic]
    test NamespaceTopLevel "operation Foo (q : Qubit) : Unit is (Adj + Ctl" [Characteristic]
    // TODO: test NamespaceTopLevel "operation Foo (q : Qubit) : Unit is (Adj + Ctl)" []    
    test NamespaceTopLevel "operation Foo (q : Qubit) : Unit is Adj + Ctl " []
    test NamespaceTopLevel "operation Foo (q : Qubit) : Unit is Adj + Cat " []
    test NamespaceTopLevel "operation Foo (q : Qubit) : (Int, MyT" types
    test NamespaceTopLevel "operation Foo (q : Qubit) : (Int," types
    test NamespaceTopLevel "operation Foo (q : Qubit) : (Int, MyT " []
    test NamespaceTopLevel "operation Foo (q : (" types
    test NamespaceTopLevel "operation Foo (q : (Qubit" types
    test NamespaceTopLevel "operation Foo (q : (Qubit," types
    test NamespaceTopLevel "operation Foo (q : (Qubit, " types
    test NamespaceTopLevel "operation Foo (q : (Qubit, Qubit" types
    test NamespaceTopLevel "operation Foo (q : (Qubit, Qubit " []
    test NamespaceTopLevel "operation Foo (q : (Qubit, Qubit), " [Declaration]
    test NamespaceTopLevel "operation Foo (q : (Qubit => " types
    test NamespaceTopLevel "operation Foo (q : (Qubit => Unit" types
    test NamespaceTopLevel "operation Foo (q : (Qubit => Unit " [Keyword "is"]
    test NamespaceTopLevel "operation Foo (q : (Qubit => Unit is " [Characteristic]
    test NamespaceTopLevel "operation Foo (q : (Qubit => Unit is Adj" [Characteristic]
    test NamespaceTopLevel "operation Foo (q : (Qubit => Unit is Adj " []
    test NamespaceTopLevel "operation Foo (q : (Qubit => Unit is Adj + " [Characteristic]
    test NamespaceTopLevel "operation Foo (q : (Qubit => Unit is Adj)" []
    test NamespaceTopLevel "operation Foo (q : (Qubit => Unit is Adj)) : Unit" types
    test NamespaceTopLevel "operation Foo (q : ((Qubit => Unit " []
    test NamespaceTopLevel "operation Foo (q : ((Qubit => Unit) " [Keyword "is"]
    test NamespaceTopLevel "operation Foo (q : ((Qubit => Unit) is " [Characteristic]
    test NamespaceTopLevel "operation Foo (q : ((Qubit => Unit) is Adj" [Characteristic]
    test NamespaceTopLevel "operation Foo (q : ((Qubit => Unit) is Adj)" []
    test NamespaceTopLevel "operation Foo (q : ((Qubit => Unit) is Adj))" []
    test NamespaceTopLevel "operation Foo (q : ((Qubit => Unit) is Adj)) :" types
    test NamespaceTopLevel "operation Foo (f : (Int -> " types
    test NamespaceTopLevel "operation Foo (f : (Int -> Int)) : Unit" types
    test NamespaceTopLevel "operation Foo (f : (Int -> Int)) : Unit " [Keyword "is"]
    test NamespaceTopLevel "operation Foo (q : (Int => Int)) : Unit" types
    test NamespaceTopLevel "operation Foo (q : ((Int -> Int) => " types
    test NamespaceTopLevel "operation Foo (q : ((Int -> Int) => Int " [Keyword "is"]
    test NamespaceTopLevel "operation Foo (q : ((Int -> Int) => Int)) : Unit" types
    test NamespaceTopLevel "operation Foo (q : ((Int->Int)=>Int)) : Unit" types
    test NamespaceTopLevel "operation Foo (q : ((Int -> Int) -> Int)) : Unit" types
    test NamespaceTopLevel "operation Foo (q : ((Int => Int) => Int)) : Unit" types
    test NamespaceTopLevel "operation Foo<'T> (q : 'T) : Unit" types
    test NamespaceTopLevel "operation Foo<'T> (q : ('T => Int)) : Unit" types
    test NamespaceTopLevel "operation Foo<'T> (q : ('T -> Int)) : Unit" types
    test NamespaceTopLevel "operation Foo<'T> (q : ('T->Int)) : Unit" types
    test NamespaceTopLevel "operation Foo<'T> (q : (MyType -> Int)) : Unit" types

[<Fact>]
let ``Type declaration parser tests`` () =
    test NamespaceTopLevel "newtype " [Declaration]
    test NamespaceTopLevel "newtype MyType" [Declaration]
    test NamespaceTopLevel "newtype MyType " []
    test NamespaceTopLevel "newtype MyType =" types
    test NamespaceTopLevel "newtype MyType = " types
    test NamespaceTopLevel "newtype MyType = Int" types
    test NamespaceTopLevel "newtype MyType = (" (Declaration :: types)
    test NamespaceTopLevel "newtype MyType = (In" (Declaration :: types)
    test NamespaceTopLevel "newtype MyType = (Int" (Declaration :: types)
    test NamespaceTopLevel "newtype MyType = (Int " []
    test NamespaceTopLevel "newtype MyType = (Int," (Declaration :: types)
    test NamespaceTopLevel "newtype MyType = (Int, Boo" (Declaration :: types)
    test NamespaceTopLevel "newtype MyType = (Int, Bool" (Declaration :: types)
    test NamespaceTopLevel "newtype MyType = (Int, Bool)" []
    test NamespaceTopLevel "newtype MyType = (MyItem :" types
    test NamespaceTopLevel "newtype MyType = (MyItem : " types
    test NamespaceTopLevel "newtype MyType = (MyItem : In" types
    test NamespaceTopLevel "newtype MyType = (MyItem : Int" types
    test NamespaceTopLevel "newtype MyType = (MyItem : Int," (Declaration :: types)
    test NamespaceTopLevel "newtype MyType = (MyItem : Int, " (Declaration :: types)
    test NamespaceTopLevel "newtype MyType = (MyItem : Int, Boo" (Declaration :: types)
    test NamespaceTopLevel "newtype MyType = (MyItem : Int, Bool" (Declaration :: types)
    test NamespaceTopLevel "newtype MyType = (MyItem : Int, Bool)" []
    test NamespaceTopLevel "newtype MyType = (MyItem : Int, (" (Declaration :: types)
    test NamespaceTopLevel "newtype MyType = (MyItem : Int, (Item2" (Declaration :: types)
    test NamespaceTopLevel "newtype MyType = (MyItem : Int, (Item2 " []
    test NamespaceTopLevel "newtype MyType = (MyItem : Int, (Item2, " (Declaration :: types)
    test NamespaceTopLevel "newtype MyType = (MyItem : Int, (Item2, Item3 :" types
    test NamespaceTopLevel "newtype MyType = (MyItem : Int, (Item2, Item3 : Int)" []
    test NamespaceTopLevel "newtype MyType = (MyItem : Int, (Item2, Item3 : Int))" []

[<Fact>]
let ``Open directive parser tests`` () =
    test NamespaceTopLevel "open " [Namespace]
    test NamespaceTopLevel "open Microsoft" [Namespace]
    test NamespaceTopLevel "open Microsoft." [Member ("Microsoft", Namespace)]
    test NamespaceTopLevel "open Microsoft.Quantum" [Member ("Microsoft", Namespace)]
    test NamespaceTopLevel "open Microsoft.Quantum." [Member ("Microsoft.Quantum", Namespace)]
    test NamespaceTopLevel "open Microsoft.Quantum.Math" [Member ("Microsoft.Quantum", Namespace)]
    test NamespaceTopLevel "open Microsoft.Quantum.Math " [Keyword "as"]
    test NamespaceTopLevel "open Microsoft.Quantum.Math as" [Keyword "as"]
    test NamespaceTopLevel "open Microsoft.Quantum.Math as " [Declaration]
    test NamespaceTopLevel "open Microsoft.Quantum.Math as Math" [Declaration]
    test NamespaceTopLevel "open Microsoft.Quantum.Math as Math " []
    test NamespaceTopLevel "open Microsoft.Quantum.Math as My" [Declaration]
    test NamespaceTopLevel "open Microsoft.Quantum.Math as My." [Member ("My", Declaration)]
    test NamespaceTopLevel "open Microsoft.Quantum.Math as My.Math" [Member ("My", Declaration)]
    test NamespaceTopLevel "open Microsoft.Quantum.Math as My.Math " []

[<Fact>]
let ``Statement parser tests`` () =
    let expression = [Variable; Keyword "not"; Keyword "Adjoint"; Keyword "Controlled"]
    let infix = [Keyword "and"; Keyword "or"]
    test Statement "" expression
    test Statement "x" expression
    test Statement "x " infix
    test Statement "x or" infix
    test Statement "x or " expression
    test Statement "x or y" expression
    test Statement "x or y " infix
    test Statement "x or y and" infix
    test Statement "x or y and " expression
    test Statement "x or y and not" expression
    test Statement "x or y and not " expression
    test Statement "x or y and not z" expression
    test Statement "x or y and not not " expression
    test Statement "x or y and not not z" expression
    test Statement "foo!" infix
    test Statement "foo! " infix
    test Statement "foo! and" infix
    test Statement "foo! and " expression
    test Statement "foo! and bar" expression
    test Statement "foo! and bar " infix
    test Statement "Foo(" expression
    test Statement "Foo()" infix
    test Statement "Foo(x" expression
    test Statement "Foo(x," expression
    test Statement "Foo(x, " expression
    test Statement "Foo(x, y" expression
    test Statement "Foo(x, y)" infix
    test Statement "Adjoint" expression
    test Statement "Adjoint " expression
    test Statement "Adjoint Foo" expression
    test Statement "Adjoint Foo " infix
    test Statement "Adjoint Foo(q)" infix
