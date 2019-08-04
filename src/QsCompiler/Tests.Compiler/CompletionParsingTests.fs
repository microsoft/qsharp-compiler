module Microsoft.Quantum.QsCompiler.Testing.CompletionParsingTests

open System.Collections.Generic
open Xunit
open Microsoft.Quantum.QsCompiler.TextProcessing.CompletionParsing


let private testAs env text expected =
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
    let test = testAs NamespaceTopLevel
    let keywords = [Keyword "function"; Keyword "operation"; Keyword "newtype"; Keyword "open"]
    test "" keywords
    test "f" keywords
    test "fun" keywords
    test "function" keywords
    test "o" keywords
    test "ope" keywords
    test "opera" keywords
    test "operation" keywords
    test "n" keywords
    test "newt" keywords
    test "newtype" keywords
    test "open" keywords

[<Fact>]
let ``Function declaration parser tests`` () =
    let test = testAs NamespaceTopLevel
    test "function " [Declaration]
    test "function Foo" [Declaration]
    test "function Foo " []
    test "function Foo (" [Declaration]
    test "function Foo (x" [Declaration]
    test "function Foo (x " []
    test "function Foo (x :" types
    test "function Foo (x : Int" types
    test "function Foo (x : Int " []
    test "function Foo (x : Int)" []
    test "function Foo (x : Int) :" types
    test "function Foo (x : Int) : " types
    test "function Foo (x : Int) : Unit" types
    test "function Foo (x : Int) : Unit " []
    test "function Foo (x : Int) : (Int, MyT" types
    test "function Foo (x : Int) : (Int," types
    test "function Foo (x : Int) : (Int, " types
    test "function Foo (x : Int) : (Int, MyT " []
    test "function Foo (x : (" types
    test "function Foo (x : (Int" types
    test "function Foo (x : (Int," types
    test "function Foo (x : (Int, " types
    test "function Foo (x : (Int, Int" types
    test "function Foo (x : (Int, Int " []
    test "function Foo (x : (Int, Int), " [Declaration]
    test "function Foo (f : (Int -> " types
    test "function Foo (f : (Int -> Int" types
    test "function Foo (f : (Int -> Int " []
    test "function Foo (f : (Int -> Int)" []
    test "function Foo (f : (Int -> Int)) : Unit" types
    test "function Foo (f : (Int -> Int)) : Unit " []
    test "function Foo (q : ((Int -> Int) -> " types
    test "function Foo (q : ((Int -> Int) -> Int " []
    test "function Foo (q : ((Int -> Int) -> Int)) : Unit" types
    test "function Foo (q : ((Int->Int)->Int)) : Unit" types
    test "function Foo (q : ((Int -> Int) -> Int)) : Unit" types
    test "function Foo<" []
    test "function Foo<'" [Declaration]
    test "function Foo<> (q : 'T) : Unit" types
    test "function Foo<'T> (q : '" [TypeParameter]
    test "function Foo<'T> (q : 'T) : Unit" types
    test "function Foo<'T> (q : ('T -> Int)) : Unit" types
    test "function Foo<'T> (q : ('T->Int)) : Unit" types
    test "function Foo<'T> (q : (MyType -> Int)) : Unit" types

[<Fact>]
let ``Operation declaration parser tests`` () =
    let test = testAs NamespaceTopLevel
    test "operation " [Declaration]
    test "operation Foo" [Declaration]
    test "operation Foo " []
    test "operation Foo (" [Declaration]
    test "operation Foo (q" [Declaration]
    test "operation Foo (q :" types
    test "operation Foo (q : Qubit" types
    test "operation Foo (q : Qubit)" []
    test "operation Foo (q : Qubit) :" types
    test "operation Foo (q : Qubit) : " types
    test "operation Foo (q : Qubit) : Unit" types
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
    // TODO: test "operation Foo (q : Qubit) : Unit is (Adj " []
    test "operation Foo (q : Qubit) : Unit is (Adj + " [Characteristic]
    test "operation Foo (q : Qubit) : Unit is (Adj + Ctl" [Characteristic]
    // TODO: test "operation Foo (q : Qubit) : Unit is (Adj + Ctl)" []    
    test "operation Foo (q : Qubit) : Unit is Adj + Ctl " []
    test "operation Foo (q : Qubit) : Unit is Adj + Cat " []
    test "operation Foo (q : Qubit) : (Int, MyT" types
    test "operation Foo (q : Qubit) : (Int," types
    test "operation Foo (q : Qubit) : (Int, MyT " []
    test "operation Foo (q : (" types
    test "operation Foo (q : (Qubit" types
    test "operation Foo (q : (Qubit," types
    test "operation Foo (q : (Qubit, " types
    test "operation Foo (q : (Qubit, Qubit" types
    test "operation Foo (q : (Qubit, Qubit " []
    test "operation Foo (q : (Qubit, Qubit), " [Declaration]
    test "operation Foo (q : (Qubit => " types
    test "operation Foo (q : (Qubit => Unit" types
    test "operation Foo (q : (Qubit => Unit " [Keyword "is"]
    test "operation Foo (q : (Qubit => Unit is " [Characteristic]
    test "operation Foo (q : (Qubit => Unit is Adj" [Characteristic]
    test "operation Foo (q : (Qubit => Unit is Adj " []
    test "operation Foo (q : (Qubit => Unit is Adj + " [Characteristic]
    test "operation Foo (q : (Qubit => Unit is Adj)" []
    test "operation Foo (q : (Qubit => Unit is Adj)) : Unit" types
    test "operation Foo (q : ((Qubit => Unit " []
    test "operation Foo (q : ((Qubit => Unit) " [Keyword "is"]
    test "operation Foo (q : ((Qubit => Unit) is " [Characteristic]
    test "operation Foo (q : ((Qubit => Unit) is Adj" [Characteristic]
    test "operation Foo (q : ((Qubit => Unit) is Adj)" []
    test "operation Foo (q : ((Qubit => Unit) is Adj))" []
    test "operation Foo (q : ((Qubit => Unit) is Adj)) :" types
    test "operation Foo (f : (Int -> " types
    test "operation Foo (f : (Int -> Int)) : Unit" types
    test "operation Foo (f : (Int -> Int)) : Unit " [Keyword "is"]
    test "operation Foo (q : (Int => Int)) : Unit" types
    test "operation Foo (q : ((Int -> Int) => " types
    test "operation Foo (q : ((Int -> Int) => Int " [Keyword "is"]
    test "operation Foo (q : ((Int -> Int) => Int)) : Unit" types
    test "operation Foo (q : ((Int->Int)=>Int)) : Unit" types
    test "operation Foo (q : ((Int -> Int) -> Int)) : Unit" types
    test "operation Foo (q : ((Int => Int) => Int)) : Unit" types
    test "operation Foo<'T> (q : 'T) : Unit" types
    test "operation Foo<'T> (q : ('T => Int)) : Unit" types
    test "operation Foo<'T> (q : ('T -> Int)) : Unit" types
    test "operation Foo<'T> (q : ('T->Int)) : Unit" types
    test "operation Foo<'T> (q : (MyType -> Int)) : Unit" types

[<Fact>]
let ``Type declaration parser tests`` () =
    let test = testAs NamespaceTopLevel
    test "newtype " [Declaration]
    test "newtype MyType" [Declaration]
    test "newtype MyType " []
    test "newtype MyType =" types
    test "newtype MyType = " types
    test "newtype MyType = Int" types
    test "newtype MyType = (" (Declaration :: types)
    test "newtype MyType = (In" (Declaration :: types)
    test "newtype MyType = (Int" (Declaration :: types)
    test "newtype MyType = (Int " []
    test "newtype MyType = (Int," (Declaration :: types)
    test "newtype MyType = (Int, Boo" (Declaration :: types)
    test "newtype MyType = (Int, Bool" (Declaration :: types)
    test "newtype MyType = (Int, Bool)" []
    test "newtype MyType = (MyItem :" types
    test "newtype MyType = (MyItem : " types
    test "newtype MyType = (MyItem : In" types
    test "newtype MyType = (MyItem : Int" types
    test "newtype MyType = (MyItem : Int," (Declaration :: types)
    test "newtype MyType = (MyItem : Int, " (Declaration :: types)
    test "newtype MyType = (MyItem : Int, Boo" (Declaration :: types)
    test "newtype MyType = (MyItem : Int, Bool" (Declaration :: types)
    test "newtype MyType = (MyItem : Int, Bool)" []
    test "newtype MyType = (MyItem : Int, (" (Declaration :: types)
    test "newtype MyType = (MyItem : Int, (Item2" (Declaration :: types)
    test "newtype MyType = (MyItem : Int, (Item2 " []
    test "newtype MyType = (MyItem : Int, (Item2, " (Declaration :: types)
    test "newtype MyType = (MyItem : Int, (Item2, Item3 :" types
    test "newtype MyType = (MyItem : Int, (Item2, Item3 : Int)" []
    test "newtype MyType = (MyItem : Int, (Item2, Item3 : Int))" []

[<Fact>]
let ``Open directive parser tests`` () =
    let test = testAs NamespaceTopLevel
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

[<Fact>]
let ``Expression statement parser tests`` () =
    let test = testAs Statement
    let expression = [
        Variable
        Keyword "new"
        Keyword "not"
        Keyword "Adjoint"
        Keyword "Controlled"
        Keyword "PauliX"
        Keyword "PauliY"
        Keyword "PauliZ"
        Keyword "PauliI"
        Keyword "Zero"
        Keyword "One"
        Keyword "true"
        Keyword "false"
    ]
    let infix = [Keyword "and"; Keyword "or"]
    test "" expression
    test "x" expression
    test "x " infix
    test "x or" infix
    test "x or " expression
    test "x or y" expression
    test "x or y " infix
    test "x or y and" infix
    test "x or y and " expression
    test "x or y and not" expression
    test "x or y and not " expression
    test "x or y and not z" expression
    test "x or y and not not " expression
    test "x or y and not not z" expression
    test "foo!" infix
    test "foo! " infix
    test "foo! and" infix
    test "foo! and " expression
    test "foo! and bar" expression
    test "foo! and bar " infix
    test "Foo(" expression
    test "Foo()" infix
    test "Foo(x" expression
    test "Foo(x," expression
    test "Foo(x, " expression
    test "Foo(x, y" expression
    test "Foo(x, y)" infix
    test "Foo(true, Zero)" infix
    test "Foo(2)" infix
    test "Foo(1.2)" infix
    test "Foo(-0.111)" infix
    test "Foo(2, 1.2, -0.111)" infix
    test "Adjoint" expression
    test "Adjoint " [Keyword "Adjoint"; Keyword "Controlled"; Variable]
    test "Adjoint Foo" [Keyword "Adjoint"; Keyword "Controlled"; Variable]
    test "Adjoint Foo " infix
    test "Adjoint Foo(q)" infix
    test "[" expression
    test "[x" expression
    test "[x]" infix
    test "[x " infix
    test "[x a" infix
    test "[x and" infix
    test "[x and " expression
    test "[x and y," expression
    test "[x and y, z" expression
    test "[x and y, z]" infix
    test "(" expression
    test "(x" expression
    test "(x)" infix
    test "(x " infix
    test "(x a" infix
    test "(x and" infix
    test "(x and " expression
    test "(x and y," expression
    test "(x and y, z" expression
    test "(x and y, z)" infix
    test "x[" expression
    test "x[y " infix
    test "x[2 + " expression
    test "x[2]" infix
    test "x[y]" infix
    test "x[Length" expression
    test "x[Length(" expression
    test "x[Length(x)" infix
    test "x[Length(x) -" expression
    test "x[Length(x) - " expression
    test "x[Length(x) - 1" []
    test "x[Length(x) - 1]" infix
    test "x[1 .. " expression
    test "x[1 ..." []
    test "x[1 ...]" infix
    test "(Foo())[" expression
    test "(Foo())[2]" infix
    test "(Foo())[y]" infix
    test "(Foo())[Length" expression
    test "(Foo())[Length(" expression
    test "(Foo())[Length(x)" infix
    test "(Foo())[Length(x) -" expression
    test "(Foo())[Length(x) - " expression
    test "(Foo())[Length(x) - 1" []
    test "(Foo())[Length(x) - 1]" infix
    test "(Foo())[1 .. " expression
    test "(Foo())[1 ..." []
    test "(Foo())[1 ...]" infix
    test "new" expression
    test "new " types
    test "new Int" types
    test "new Int[" expression
    test "new Int[x" expression
    test "new Int[x " infix
    test "new Int[x +" expression
    test "new Int[x + 1" []
    test "new Int[x + 1]" infix
    test "new Int[][" expression
    test "new Int[][2" []
    test "new Int[][2]" infix
