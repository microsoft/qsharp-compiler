module Microsoft.Quantum.QsCompiler.Testing.CompletionParsingTests

open System
open System.Collections.Generic
open Xunit
open Microsoft.Quantum.QsCompiler.TextProcessing.CompletionParsing


let private matches env (text, expected) =
    match GetExpectedIdentifiers env text with
    | Success actual -> Assert.Equal<IEnumerable<IdentifierKind>>(Set.ofList expected, actual)
    | Failure message -> raise (Exception message)

let private fails env text =
    match GetExpectedIdentifiers env text with
    | Success _ -> raise <| Exception (String.Format("Input: {0}\nParser succeeded when it was expected to fail", text))
    | Failure _ -> ()

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

let private expression = [
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
    Keyword "_"
]

let private infix = [
    Keyword "and"
    Keyword "or"
]

let private statement =
    expression @ [
        Keyword "let"
        Keyword "mutable"
    ]

[<Fact>]
let ``Inside namespace parser tests`` () =
    let keywords = [
        Keyword "function"
        Keyword "operation"
        Keyword "newtype"
        Keyword "open"
    ]
    List.iter (matches NamespaceTopLevel) [
        ("", keywords)
        ("f", keywords)
        ("fun", keywords)
        ("function", keywords)
        ("o", keywords)
        ("ope", keywords)
        ("opera", keywords)
        ("operation", keywords)
        ("n", keywords)
        ("newt", keywords)
        ("newtype", keywords)
        ("open", keywords)
    ]

[<Fact>]
let ``Function declaration parser tests`` () =
    List.iter (matches NamespaceTopLevel) [
        ("function ", [Declaration])
        ("function Foo", [Declaration])
        ("function Foo ", [])
        ("function Foo (", [Declaration])
        ("function Foo (x", [Declaration])
        ("function Foo (x ", [])
        ("function Foo (x :", types)
        ("function Foo (x : Int", types)
        ("function Foo (x : Int ", [])
        ("function Foo (x : Int)", [])
        ("function Foo (x : Int) :", types)
        ("function Foo (x : Int) : ", types)
        ("function Foo (x : Int) : Unit", types)
        ("function Foo (x : Int) : Unit ", [])
        ("function Foo (x : Int) : (Int, MyT", types)
        ("function Foo (x : Int) : (Int,", types)
        ("function Foo (x : Int) : (Int, ", types)
        ("function Foo (x : Int) : (Int, MyT ", [])
        ("function Foo (x : (", types)
        ("function Foo (x : (Int", types)
        ("function Foo (x : (Int,", types)
        ("function Foo (x : (Int, ", types)
        ("function Foo (x : (Int, Int", types)
        ("function Foo (x : (Int, Int ", [])
        ("function Foo (x : (Int, Int), ", [Declaration])
        ("function Foo (f : (Int -> ", types)
        ("function Foo (f : (Int -> Int", types)
        ("function Foo (f : (Int -> Int ", [])
        ("function Foo (f : (Int -> Int)", [])
        ("function Foo (f : (Int -> Int)) : Unit", types)
        ("function Foo (f : (Int -> Int)) : Unit ", [])
        ("function Foo (q : ((Int -> Int) -> ", types)
        ("function Foo (q : ((Int -> Int) -> Int ", [])
        ("function Foo (q : ((Int -> Int) -> Int)) : Unit", types)
        ("function Foo (q : ((Int->Int)->Int)) : Unit", types)
        ("function Foo (q : ((Int -> Int) -> Int)) : Unit", types)
        ("function Foo<", [])
        ("function Foo<'", [Declaration])
        ("function Foo<> (q : 'T) : Unit", types)
        ("function Foo<'T> (q : '", [TypeParameter])
        ("function Foo<'T> (q : 'T) : Unit", types)
        ("function Foo<'T> (q : ('T -> Int)) : Unit", types)
        ("function Foo<'T> (q : ('T->Int)) : Unit", types)
        ("function Foo<'T> (q : (MyType -> Int)) : Unit", types)
    ]

[<Fact>]
let ``Operation declaration parser tests`` () =
    let characteristics = [
        Keyword "Adj"
        Keyword "Ctl"
    ]
    List.iter (matches NamespaceTopLevel) [
        ("operation ", [Declaration])
        ("operation Foo", [Declaration])
        ("operation Foo ", [])
        ("operation Foo (", [Declaration])
        ("operation Foo (q", [Declaration])
        ("operation Foo (q :", types)
        ("operation Foo (q : Qubit", types)
        ("operation Foo (q : Qubit)", [])
        ("operation Foo (q : Qubit) :", types)
        ("operation Foo (q : Qubit) : ", types)
        ("operation Foo (q : Qubit) : Unit", types)
        ("operation Foo (q : Qubit) : Unit ", [Keyword "is"])
        ("operation Foo (q : Qubit) : Unit i", [Keyword "is"])
        ("operation Foo (q : Qubit) : Unit is", [Keyword "is"])
        ("operation Foo (q : Qubit) : Unit is ", characteristics)
        ("operation Foo (q : Qubit) : Unit is A", characteristics)
        ("operation Foo (q : Qubit) : Unit is Adj", characteristics)
        ("operation Foo (q : Qubit) : Unit is Adj ", [])
        ("operation Foo (q : Qubit) : Unit is Adj +", characteristics)
        ("operation Foo (q : Qubit) : Unit is Adj + ", characteristics)
        ("operation Foo (q : Qubit) : Unit is Adj + Ctl", characteristics)
        ("operation Foo (q : Qubit) : Unit is (", characteristics)
        ("operation Foo (q : Qubit) : Unit is ( ", characteristics)
        ("operation Foo (q : Qubit) : Unit is (Adj", characteristics)
        ("operation Foo (q : Qubit) : Unit is (Adj ", [])
        ("operation Foo (q : Qubit) : Unit is (Adj + ", characteristics)
        ("operation Foo (q : Qubit) : Unit is (Adj + Ctl", characteristics)
        ("operation Foo (q : Qubit) : Unit is (Adj + Ctl)", [])
        ("operation Foo (q : Qubit) : Unit is Adj + Ctl ", [])
        ("operation Foo (q : Qubit) : (Int, MyT", types)
        ("operation Foo (q : Qubit) : (Int,", types)
        ("operation Foo (q : Qubit) : (Int, MyT ", [])
        ("operation Foo (q : (", types)
        ("operation Foo (q : (Qubit", types)
        ("operation Foo (q : (Qubit,", types)
        ("operation Foo (q : (Qubit, ", types)
        ("operation Foo (q : (Qubit, Qubit", types)
        ("operation Foo (q : (Qubit, Qubit ", [])
        ("operation Foo (q : (Qubit, Qubit), ", [Declaration])
        ("operation Foo (q : (Qubit => ", types)
        ("operation Foo (q : (Qubit => Unit", types)
        ("operation Foo (q : (Qubit => Unit ", [Keyword "is"])
        ("operation Foo (q : (Qubit => Unit is ", characteristics)
        ("operation Foo (q : (Qubit => Unit is Adj", characteristics)
        ("operation Foo (q : (Qubit => Unit is Adj ", [])
        ("operation Foo (q : (Qubit => Unit is Adj + ", characteristics)
        ("operation Foo (q : (Qubit => Unit is Adj)", [])
        ("operation Foo (q : (Qubit => Unit is Adj)) : Unit", types)
        ("operation Foo (q : ((Qubit => Unit ", [])
        ("operation Foo (q : ((Qubit => Unit) ", [Keyword "is"])
        ("operation Foo (q : ((Qubit => Unit) is ", characteristics)
        ("operation Foo (q : ((Qubit => Unit) is Adj", characteristics)
        ("operation Foo (q : ((Qubit => Unit) is Adj)", [])
        ("operation Foo (q : ((Qubit => Unit) is Adj))", [])
        ("operation Foo (q : ((Qubit => Unit) is Adj)) :", types)
        ("operation Foo (f : (Int -> ", types)
        ("operation Foo (f : (Int -> Int)) : Unit", types)
        ("operation Foo (f : (Int -> Int)) : Unit ", [Keyword "is"])
        ("operation Foo (q : (Int => Int)) : Unit", types)
        ("operation Foo (q : ((Int -> Int) => ", types)
        ("operation Foo (q : ((Int -> Int) => Int ", [Keyword "is"])
        ("operation Foo (q : ((Int -> Int) => Int)) : Unit", types)
        ("operation Foo (q : ((Int->Int)=>Int)) : Unit", types)
        ("operation Foo (q : ((Int -> Int) -> Int)) : Unit", types)
        ("operation Foo (q : ((Int => Int) => Int)) : Unit", types)
        ("operation Foo<'T> (q : 'T) : Unit", types)
        ("operation Foo<'T> (q : ('T => Int)) : Unit", types)
        ("operation Foo<'T> (q : ('T -> Int)) : Unit", types)
        ("operation Foo<'T> (q : ('T->Int)) : Unit", types)
        ("operation Foo<'T> (q : (MyType -> Int)) : Unit", types)
    ]
    List.iter (fails NamespaceTopLevel) [
        "operation Foo (q : Qubit) : Unit is Adj + Cat "
        "operation Foo (q : Qubit) : (Int, MyT is Adj"
        "operation Foo (q : Qubit) : (Int, MyT is Adj "
    ]

[<Fact>]
let ``Type declaration parser tests`` () =
    List.iter (matches NamespaceTopLevel) [
        ("newtype ", [Declaration])
        ("newtype MyType", [Declaration])
        ("newtype MyType ", [])
        ("newtype MyType =", types)
        ("newtype MyType = ", types)
        ("newtype MyType = Int", types)
        ("newtype MyType = Int ", [])
        ("newtype MyType = (", Declaration :: types)
        ("newtype MyType = (In", Declaration :: types)
        ("newtype MyType = (Int", Declaration :: types)
        ("newtype MyType = (Int ", [])
        ("newtype MyType = (Int,", Declaration :: types)
        ("newtype MyType = (Int, Boo", Declaration :: types)
        ("newtype MyType = (Int, Bool", Declaration :: types)
        ("newtype MyType = (Int, Bool)", [])
        ("newtype MyType = (MyItem :", types)
        ("newtype MyType = (MyItem : ", types)
        ("newtype MyType = (MyItem : In", types)
        ("newtype MyType = (MyItem : Int", types)
        ("newtype MyType = (MyItem : Int,", Declaration :: types)
        ("newtype MyType = (MyItem : Int, ", Declaration :: types)
        ("newtype MyType = (MyItem : Int, Boo", Declaration :: types)
        ("newtype MyType = (MyItem : Int, Bool", Declaration :: types)
        ("newtype MyType = (MyItem : Int, Bool)", [])
        ("newtype MyType = (MyItem : Int, (", Declaration :: types)
        ("newtype MyType = (MyItem : Int, (Item2", Declaration :: types)
        ("newtype MyType = (MyItem : Int, (Item2 ", [])
        ("newtype MyType = (MyItem : Int, (Item2, ", Declaration :: types)
        ("newtype MyType = (MyItem : Int, (Item2, Item3 :", types)
        ("newtype MyType = (MyItem : Int, (Item2, Item3 : Int)", [])
        ("newtype MyType = (MyItem : Int, (Item2, Item3 : Int))", [])
    ]

[<Fact>]
let ``Open directive parser tests`` () =
    List.iter (matches NamespaceTopLevel) [
        ("open ", [Namespace])
        ("open Microsoft", [Namespace])
        ("open Microsoft.", [Member ("Microsoft", Namespace)])
        ("open Microsoft.Quantum", [Member ("Microsoft", Namespace)])
        ("open Microsoft.Quantum.", [Member ("Microsoft.Quantum", Namespace)])
        ("open Microsoft.Quantum.Math", [Member ("Microsoft.Quantum", Namespace)])
        ("open Microsoft.Quantum.Math ", [Keyword "as"])
        ("open Microsoft.Quantum.Math as", [Keyword "as"])
        ("open Microsoft.Quantum.Math as ", [Declaration])
        ("open Microsoft.Quantum.Math as Math", [Declaration])
        ("open Microsoft.Quantum.Math as Math ", [])
        ("open Microsoft.Quantum.Math as My", [Declaration])
        ("open Microsoft.Quantum.Math as My.", [Member ("My", Declaration)])
        ("open Microsoft.Quantum.Math as My.Math", [Member ("My", Declaration)])
        ("open Microsoft.Quantum.Math as My.Math ", [])
    ]

[<Fact>]
let ``Statement parser tests`` () =
    List.iter (matches Statement) [
        ("", statement)
        ("let ", [Declaration])
        ("let x ", [])
        ("let x =", expression)
        ("let x = ", expression)
        ("let x = y", expression)
        ("let x = y ", infix)
        ("let (", [Declaration])
        ("let (x", [Declaration])
        ("let (x ", [])
        ("let (x,", [Declaration])
        ("let (x, _", [Declaration])
        ("let (x, _, ", [Declaration])
        ("let (x, _, (", [Declaration])
        ("let (x, _, (y,", [Declaration])
        ("let (x, _, (y, z)", [])
        ("let (x, _, (y, z))", [])
        ("let (x, _, (y, z)) =", expression)
        ("let (x, _, (y, z)) = Foo", expression)
        ("let (x, _, (y, z)) = Foo(", expression)
        ("let (x, _, (y, z)) = Foo()", infix)
        ("mutable ", [Declaration])
        ("mutable x ", [])
        ("mutable x =", expression)
        ("mutable x = ", expression)
        ("mutable x = y", expression)
        ("mutable x = y ", infix)
        ("mutable (", [Declaration])
        ("mutable (x", [Declaration])
        ("mutable (x ", [])
        ("mutable (x,", [Declaration])
        ("mutable (x, _", [Declaration])
        ("mutable (x, _, ", [Declaration])
        ("mutable (x, _, (", [Declaration])
        ("mutable (x, _, (y,", [Declaration])
        ("mutable (x, _, (y, z)", [])
        ("mutable (x, _, (y, z))", [])
        ("mutable (x, _, (y, z)) =", expression)
        ("mutable (x, _, (y, z)) = Foo", expression)
        ("mutable (x, _, (y, z)) = Foo(", expression)
        ("mutable (x, _, (y, z)) = Foo()", infix)
    ]

let ``Expression statement parser tests`` () =
    List.iter (matches Statement) [
        ("", expression)
        ("x", expression)
        ("x ", infix)
        ("2 ", infix)
        ("x or", infix)
        ("x or ", expression)
        ("x or y", expression)
        ("x or y ", infix)
        ("x or y and", infix)
        ("x or y and ", expression)
        ("x or y and not", expression)
        ("x or y and not ", expression)
        ("x or y and not z", expression)
        ("x or y and not not ", expression)
        ("x or y and not not z", expression)
        ("x +", expression)
        ("x + ", expression)
        ("x + 2", [])
        ("x >", expression)
        ("x > ", expression)
        ("x > y", expression)
        ("x > y ", infix)
        ("x <", expression @ types)
        ("x < ", expression @ types)
        ("x < y", expression @ types)
        ("x < y ", infix)
        ("x &&&", expression)
        ("x &&& ", expression)
        ("x &&& y", expression)
        ("~~~", expression)
        ("~~~x", expression)
        ("~~~x ", infix)
        ("foo!", infix)
        ("foo! ", infix)
        ("foo! and", infix)
        ("foo! and ", expression)
        ("foo! and bar", expression)
        ("foo! and bar ", infix)
        ("Foo(", expression)
        ("Foo()", infix)
        ("Foo.", [Member ("Foo", Variable)])
        ("Foo.Bar", [Member ("Foo", Variable)])
        ("Foo.Bar(", expression)
        ("Foo.Bar()", infix)
        ("Foo(x", expression)
        ("Foo(x,", expression)
        ("Foo(x, ", expression)
        ("Foo(x, y", expression)
        ("Foo(x, y)", infix)
        ("Foo(true, Zero)", infix)
        ("Foo(2)", infix)
        ("Foo(1.2)", infix)
        ("Foo(1.", [])
        ("Foo(-0.111)", infix)
        ("Foo(2, 1.2, -0.111)", infix)
        ("Foo(_", expression)
        ("Foo(_)", infix)
        ("Foo(_,", expression)
        ("Foo(_, ", expression)
        ("Foo(_, 2", [])
        ("Foo(_, 2)", infix)
        ("Foo<", expression @ types)
        ("Foo<>", infix)
        ("Foo<>(", expression)
        ("Foo<>(2", [])
        ("Foo<>(2,", expression)
        ("Foo<>(2, ", expression)
        ("Foo<>(2, f", expression)
        ("Foo<>(2, false", expression)
        ("Foo<>(2, false)", infix)
        ("Foo<I", expression @ types)
        ("Foo<Int", expression @ types)
        ("Foo<Int>", infix)
        ("Foo<Int,", types)
        ("Foo<Int, ", types)
        ("Foo<Int, B", types)
        ("Foo<Int, Bool>", infix)
        ("Foo<Int, Bool>(", expression)
        ("Foo<Int, Bool>(2", [])
        ("Foo<Int, Bool>(2,", expression)
        ("Foo<Int, Bool>(2, ", expression)
        ("Foo<Int, Bool>(2, f", expression)
        ("Foo<Int, Bool>(2, false", expression)
        ("Foo<Int, Bool>(2, false)", infix)
        ("Adjoint", expression)
        ("Adjoint ", [Keyword "Adjoint"; Keyword "Controlled"; Variable])
        ("Adjoint Foo", [Keyword "Adjoint"; Keyword "Controlled"; Variable])
        ("Adjoint Foo ", infix)
        ("Adjoint Foo(q)", infix)
        ("[", expression)
        ("[x", expression)
        ("[x]", infix)
        ("[x ", infix)
        ("[x a", infix)
        ("[x and", infix)
        ("[x and ", expression)
        ("[x and y,", expression)
        ("[x and y, z", expression)
        ("[x and y, z]", infix)
        ("(", expression)
        ("(x", expression)
        ("(x)", infix)
        ("(x ", infix)
        ("(x a", infix)
        ("(x and", infix)
        ("(x and ", expression)
        ("(x and y,", expression)
        ("(x and y, z", expression)
        ("(x and y, z)", infix)
        ("x[", expression)
        ("x[y ", infix)
        ("x[2 + ", expression)
        ("x[2]", infix)
        ("x[y]", infix)
        ("x[Length", expression)
        ("x[Length(", expression)
        ("x[Length(x", expression)
        ("x[Length(x)", infix)
        ("x[Length(x) -", expression)
        ("x[Length(x) - ", expression)
        ("x[Length(x) - 1", [])
        ("x[Length(x) - 1]", infix)
        ("x[1 .. ", expression)
        ("x[1 ...", [])
        ("x[1...", [])
        ("x[...", expression)
        ("x[...2", [])
        ("x[...2]", infix)
        ("x[...2...", [])
        ("x[...2...]", infix)
        ("x[1 ...]", infix)
        ("x[...]", infix)
        ("(Foo())[", expression)
        ("(Foo())[2]", infix)
        ("(Foo())[y]", infix)
        ("(Foo())[Length", expression)
        ("(Foo())[Length(", expression)
        ("(Foo())[Length(x", expression)
        ("(Foo())[Length(x)", infix)
        ("(Foo())[Length(x) -", expression)
        ("(Foo())[Length(x) - ", expression)
        ("(Foo())[Length(x) - 1", [])
        ("(Foo())[Length(x) - 1]", infix)
        ("(Foo())[1 .. ", expression)
        ("(Foo())[1 ...", [])
        ("(Foo())[1 ...]", infix)
        ("new", expression)
        ("new ", types)
        ("new Int", types)
        ("new Int[", expression)
        ("new Int[x", expression)
        ("new Int[x ", infix)
        ("new Int[x +", expression)
        ("new Int[x + 1", [])
        ("new Int[x + 1]", infix)
        ("new Int[][", expression)
        ("new Int[][2", [])
        ("new Int[][2]", infix)
        ("x::", [NamedItem])
        ("x::Foo", [NamedItem])
        ("x::Foo ", infix)
        ("(Foo())::", [NamedItem])
        ("(Foo())::Foo", [NamedItem])
        ("(Foo())::Foo ", infix)
        ("arr w", infix)
        ("arr w/", NamedItem :: expression)
        ("arr w/ 2", [])
        ("arr w/ 2 ", infix)
        ("arr w/ 2..", expression)
        ("arr w/ 2..4", [])
        ("arr w/ 2..4 <-", expression)
        ("arr w/ 2..4 <- true", expression)
        ("x w/ Foo", NamedItem :: expression)
        ("x w/ Foo ", infix)
        ("x w/ Foo <-", expression)
        ("x w/ Foo <- false", expression)
        ("Foo() ?", expression)
        ("Foo() ? 1 + 2 |", expression)
        ("Foo() ? 1 + 2 | 4", [])
        ("Foo() ? 1 + 2 | 4 ", infix)
        ("\"", [])
        ("\"hello", [])
        ("\"hello\"", infix)
        ("\"hello\\\"", [])
        ("\"hello\\\"world", [])
        ("\"hello\\\"world\"", infix)
        ("$\"hello", [])
        ("$\"hello {", expression)
        ("$\"hello {x ", infix)
        ("$\"hello {x +", expression)
        ("$\"hello {x + ", expression)
        ("$\"hello {x + y", expression)
        ("$\"hello {x + y}", [])
        ("$\"hello {x + y}\"", infix)
        ("$\"hello \\{", [])
        ("$\"hello \\{x ", [])
        ("$\"hello \\{x +", [])
        ("$\"hello \\{x + ", [])
        ("$\"hello \\{x + y", [])
        ("$\"hello \\{x + y}", [])
        ("$\"hello \\{x + y}\"", infix)
        ("$\"hello {$\"", [])
        ("$\"hello {$\"hi\"", infix)
        ("$\"hello {$\"hi\"}", [])
        ("$\"hello {$\"hi\"}\"", infix)
    ]
