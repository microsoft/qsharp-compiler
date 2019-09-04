﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.CompletionParsingTests

open System
open Xunit
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.TextProcessing.CodeCompletion
open Microsoft.Quantum.QsCompiler.TextProcessing.CodeCompletion.FragmentParsing


let private matches scope previous (text, expected) =
    match GetCompletionKinds scope previous text with
    | Success actual ->
        Assert.True(Set.ofList expected = Set.ofSeq actual,
                    String.Format("Input:    {0}\n" +
                                  "Expected: {1}\n" +
                                  "Actual:   {2}",
                                  text, Set.ofList expected, actual))
    | Failure message -> raise (Exception message)

let private fails scope previous text =
    match GetCompletionKinds scope previous text with
    | Success _ -> raise <| Exception (String.Format("Input: {0}\nParser succeeded when it was expected to fail", text))
    | Failure _ -> ()

let private types = [
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
    Callable
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

let private callableStatement =
    expression @ [
        Keyword "let"
        Keyword "mutable"
        Keyword "return"
        Keyword "set"
        Keyword "fail"
        Keyword "if"
        Keyword "for"
    ]

let private functionStatement =
    callableStatement @ [
        Keyword "while"
    ]

let private operationStatement =
    callableStatement @ [
        Keyword "repeat"
        Keyword "using"
        Keyword "borrowing"
        Keyword "within"
    ]

let private operationTopLevelStatement =
    operationStatement @ [
        Keyword "body"
        Keyword "adjoint"
        Keyword "controlled"
    ]

let testElifElse scope previous =
    let statement =
        match scope with
        | Operation | OperationTopLevel -> operationStatement
        | Function -> functionStatement
        | _ -> raise (Exception "Scope is not in a function or operation")
    List.iter (matches scope previous) [
        ("", [Keyword "elif"; Keyword "else"] @ statement)
        ("elif ", [])
        ("elif (", expression)
        ("elif (true", expression)
        ("elif (true)", [])
        ("else ", [])
    ]

[<Fact>]
let ``Top-level parser tests`` () =
    List.iter (matches TopLevel Null) [
        ("", [Keyword "namespace"])
        ("namespace ", [Namespace])
        ("namespace Foo", [Namespace])
        ("namespace Foo.", [Member ("Foo", Namespace)])
        ("namespace Foo.Bar", [Member ("Foo", Namespace)])
        ("namespace Foo.Bar.", [Member ("Foo.Bar", Namespace)])
        ("namespace Foo.Bar.Baz", [Member ("Foo.Bar", Namespace)])
        ("namespace Foo.Bar.Baz ", [])
    ]

[<Fact>]
let ``Namespace top-level parser tests`` () =
    let keywords = [
        Keyword "function"
        Keyword "operation"
        Keyword "newtype"
        Keyword "open"
    ]
    List.iter (matches NamespaceTopLevel Null) [
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
    List.iter (matches NamespaceTopLevel Null) [
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
    List.iter (matches NamespaceTopLevel Null) [
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
        ("operation Foo (q : Qubit) : Unit is Adj +C", characteristics)
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
    List.iter (fails NamespaceTopLevel Null) [
        "operation Foo (q : Qubit) : Unit is Adj + Cat "
        "operation Foo (q : Qubit) : (Int, MyT is Adj"
        "operation Foo (q : Qubit) : (Int, MyT is Adj "
    ]

[<Fact>]
let ``Type declaration parser tests`` () =
    List.iter (matches NamespaceTopLevel Null) [
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
    List.iter (matches NamespaceTopLevel Null) [
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
let ``Function statement parser tests`` () =
    List.iter (matches Function Null) [
        ("", functionStatement)
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
        ("set ", [MutableVariable])
        ("set x", [MutableVariable])
        ("set x ", infix)
        ("set x =", expression)
        ("set x = ", expression)
        ("set x = y", expression)
        ("set x = y ", infix)
        ("set x &", [])
        ("set x &&", [])
        ("set x &&&", [])
        ("set x &&&=", expression)
        ("set x &&&= ", expression)
        ("set x &&&= 2", [])
        ("set x &&&= 2 ", infix)
        ("set x and", infix)
        ("set x and=", expression)
        ("set x and= ", expression)
        ("set x and= y", expression)
        ("set x and= y ", infix)
        ("set (", [MutableVariable])
        ("set (x", [MutableVariable])
        ("set (x,", [MutableVariable])
        ("set (x, ", [MutableVariable])
        ("set (x, y", [MutableVariable])
        ("set (x, y)", infix)
        ("set (x, y) ", infix)
        ("set (x, y) =", expression)
        ("set (x, y) = ", expression)
        ("set (x, y) = (", expression)
        ("set (x, y) = (1", [])
        ("set (x, y) = (1,", expression)
        ("set (x, y) = (1, ", expression)
        ("set (x, y) = (1, 2", [])
        ("set (x, y) = (1, 2)", infix)
        ("set x w", infix)
        ("set x w/", [])
        ("set x w/=", NamedItem :: expression)
        ("set x w/= ", NamedItem :: expression)
        ("set x w/= Foo", NamedItem :: expression)
        ("set x w/= Foo ", infix)
        ("set x w/= Foo <", types @ expression)
        ("set x w/= Foo <-", expression)
        ("set x w/= Foo <- ", expression)
        ("set x w/= Foo <- y", expression)
        ("set x w/= 0", [])
        ("set x w/= 0 ", infix)
        ("set x w/= 0 <-", expression)
        ("set x w/= 0 <", types @ expression)
        ("set x w/= 0 <- ", expression)
        ("set x w/= 0 <- One", expression)
        ("set x w/= 0..", expression)
        ("set x w/= 0..2", [])
        ("set x w/= 0..2 ", infix)
        ("set x w/= 0..2 <-", expression)
        ("set x w/= 0..2 <- ", expression)
        ("set x w/= 0..2 <- [", expression)
        ("set x w/= 0..2 <- [One", expression)
        ("set x w/= 0..2 <- [One,", expression)
        ("set x w/= 0..2 <- [One, Zero,", expression)
        ("set x w/= 0..2 <- [One, Zero, One", expression)
        ("set x w/= 0..2 <- [One, Zero, One]", infix)
        ("return ", expression)
        ("return x", expression)
        ("return x ", infix)
        ("return x +", expression)
        ("return x + ", expression)
        ("return x + 1", [])
        ("fail ", expression)
        ("fail \"", [])
        ("fail \"foo", [])
        ("fail \"foo\"", infix)
        ("if ", [])
        ("if (", expression)
        ("if (x ", infix)
        ("if (x and ", expression)
        ("if (x and y)", [])
        ("for ", [])
        ("for (", [Declaration])
        ("for (x ", [Keyword "in"])
        ("for (x in ", expression)
        ("for (x in 0 ", infix)
        ("for (x in 0 ..", expression)
        ("for (x in 0 .. ", expression)
        ("for (x in 0 .. 10", [])
        ("for (x in 0 .. 10)", [])
        ("for ((", [Declaration])
        ("for ((x,", [Declaration])
        ("for ((x, y", [Declaration])
        ("for ((x, y)", [Keyword "in"])
        ("for ((x, y) ", [Keyword "in"])
        ("for ((x, y) in ", expression)
        ("for ((x, y) in Foo()", infix)
        ("for ((x, y) in Foo())", [])
        ("while ", [])
        ("while (", expression)
        ("while (Foo()", infix)
        ("while (Foo() and ", expression)
        ("while (Foo() and not ", expression)
        ("while (Foo() and not Bar()", infix)
        ("while (Foo() and not Bar() or ", expression)
        ("while (Foo() and not Bar() or false)", [])
    ]
    testElifElse Function (Value (IfClause { Expression = InvalidExpr; Range = Null }))
    testElifElse Function (Value (ElifClause { Expression = InvalidExpr; Range = Null }))
    matches Function (Value ElseClause) ("", functionStatement)

[<Fact>]
let ``Operation statement parser tests`` () =
    let functorGen = [
        Keyword "intrinsic"
        Keyword "auto"
        Keyword "self"
        Keyword "invert"
        Keyword "distribute"
    ]
    List.iter (matches Operation Null) [
        ("repeat ", [])
        ("within ", [])
        ("using ", [])
        ("using (", [Declaration])
        ("using (q ", [])
        ("using (q =", [Keyword "Qubit"])
        ("using (q = Qubit", [Keyword "Qubit"])
        ("using (q = Qubit(", [])
        ("using (q = Qubit()", [])
        ("using (q = Qubit())", [])
        ("using (q = Qubit[", expression)
        ("using (q = Qubit[5", [])
        ("using (q = Qubit[5]", [])
        ("using (q = Qubit[5])", [])
        ("using (q = Qubit[n", expression)
        ("using (q = Qubit[n ", infix)
        ("using (q = Qubit[n +", expression)
        ("using (q = Qubit[n +1", [])
        ("using (q = Qubit[n +x", expression)
        ("using (q = Qubit[n + ", expression)
        ("using (q = Qubit[n + 1", [])
        ("using (q = Qubit[n + 1]", [])
        ("using (q = Qubit[n + 1])", [])
        ("using ((", [Declaration])
        ("using ((q,", [Declaration])
        ("using ((q, r)", [])
        ("using ((q, r) =", [Keyword "Qubit"])
        ("using ((q, r) = (", [Keyword "Qubit"])
        ("using ((q, r) = (Qubit(", [])
        ("using ((q, r) = (Qubit()", [])
        ("using ((q, r) = (Qubit(),", [Keyword "Qubit"])
        ("using ((q, r) = (Qubit(), Qubit()", [])
        ("using ((q, r) = (Qubit(), Qubit())", [])
        ("borrowing (", [Declaration])
        ("borrowing (q ", [])
        ("borrowing (q =", [Keyword "Qubit"])
        ("borrowing (q = Qubit", [Keyword "Qubit"])
        ("borrowing (q = Qubit(", [])
        ("borrowing (q = Qubit()", [])
        ("borrowing (q = Qubit())", [])
        ("borrowing (q = Qubit[", expression)
        ("borrowing (q = Qubit[5", [])
        ("borrowing (q = Qubit[5]", [])
        ("borrowing (q = Qubit[5])", [])
        ("borrowing (q = Qubit[n", expression)
        ("borrowing (q = Qubit[n ", infix)
        ("borrowing (q = Qubit[n +", expression)
        ("borrowing (q = Qubit[n +1", [])
        ("borrowing (q = Qubit[n +x", expression)
        ("borrowing (q = Qubit[n + ", expression)
        ("borrowing (q = Qubit[n + 1]", [])
        ("borrowing (q = Qubit[n + 1])", [])
        ("borrowing ((", [Declaration])
        ("borrowing ((q,", [Declaration])
        ("borrowing ((q, r)", [])
        ("borrowing ((q, r) =", [Keyword "Qubit"])
        ("borrowing ((q, r) = (", [Keyword "Qubit"])
        ("borrowing ((q, r) = (Qubit(", [])
        ("borrowing ((q, r) = (Qubit()", [])
        ("borrowing ((q, r) = (Qubit(),", [Keyword "Qubit"])
        ("borrowing ((q, r) = (Qubit(), Qubit()", [])
        ("borrowing ((q, r) = (Qubit(), Qubit())", [])
    ]
    testElifElse Operation (Value (IfClause { Expression = InvalidExpr; Range = Null }))
    testElifElse Operation (Value (ElifClause { Expression = InvalidExpr; Range = Null }))
    matches Operation (Value ElseClause) ("", operationStatement)
    List.iter (matches Operation (Value WithinBlockIntro)) [
        ("", [Keyword "apply"])
        ("apply ", [])
    ]
    List.iter (matches OperationTopLevel (Value RepeatIntro)) [
        ("", [Keyword "until"])
        ("until ", [])
        ("until (", expression)
        ("until (false", expression)
        ("until (false)", [Keyword "fixup"])
        ("until (false) ", [Keyword "fixup"])
        ("until (false) fixup", [Keyword "fixup"])
        ("until (false) fixup ", [])
    ]
    List.iter (matches Operation (Value RepeatIntro)) [
        ("", [Keyword "until"])
        ("until ", [])
        ("until (", expression)
        ("until (false", expression)
        ("until (false)", [Keyword "fixup"])
        ("until (false) ", [Keyword "fixup"])
        ("until (false) fixup", [Keyword "fixup"])
        ("until (false) fixup ", [])
    ]
    List.iter (matches OperationTopLevel Null) [
        ("", operationTopLevelStatement)
        ("body ", functorGen)
        ("body intrinsic ", [])
        ("body (", [Declaration])
        ("body (.", [])
        ("body (..", [])
        ("body (...", [])
        ("body (...)", [])
        ("adjoint ", Keyword "controlled" :: functorGen)
        ("adjoint self ", [])
        ("adjoint invert ", [])
        ("adjoint auto ", [])
        ("controlled ", Keyword "adjoint" :: functorGen)
        ("controlled auto ", [])
        ("controlled distribute ", [])
        ("controlled (", [Declaration])
        ("controlled (cs,", [Declaration])
        ("controlled (cs, .", [])
        ("controlled (cs, ..", [])
        ("controlled (cs, ...", [])
        ("controlled (cs, ...)", [])
        ("controlled adjoint ", functorGen)
    ]

[<Fact>]
let ``Expression parser tests`` () =
    List.iter (matches Operation Null) [
        ("", operationStatement)
        ("x", operationStatement)
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
        ("x or y and not not", expression)
        ("x or y and not not ", expression)
        ("x or y and not not z", expression)
        ("x +", expression)
        ("x +y", expression)
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
        ("x <=", expression)
        ("x <= ", expression)
        ("x &", expression)
        ("x &&", expression)
        ("x &&&", expression)
        ("x &&& ", expression)
        ("x &&& y", expression)
        ("x |", expression)
        ("x ||", expression)
        ("x |||", expression)
        ("x ||| ", expression)
        ("x ||| y", expression)
        ("~", [])
        ("~~", [])
        ("~~~", expression)
        ("~~~x", expression)
        ("~~~x ", infix)
        ("foo!", infix)
        ("foo! ", infix)
        ("foo! and", infix)
        ("foo! and ", expression)
        ("foo! and bar", expression)
        ("foo! and bar ", infix)
        ("foo!=", expression)
        ("foo!=b", expression)
        ("foo!= ", expression)
        ("Foo(", expression)
        ("Foo()", infix)
        ("Foo.", [Member ("Foo", Callable)])
        ("Foo.Bar", [Member ("Foo", Callable)])
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
        ("Adjoint", operationStatement)
        ("Adjoint ", [Keyword "Adjoint"; Keyword "Controlled"; Callable])
        ("Adjoint Foo", [Keyword "Adjoint"; Keyword "Controlled"; Callable])
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
        ("x[Length(x) -y", expression)
        ("x[Length(x) - ", expression)
        ("x[Length(x) - 1", [])
        ("x[Length(x) - 1]", infix)
        ("x[1 .. ", expression)
        ("x[1 ...", [])
        ("x[1...", [])
        ("x[...", expression)
        ("x[...x", expression)
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
        ("(Foo())[Length(x) -y", expression)
        ("(Foo())[Length(x) - ", expression)
        ("(Foo())[Length(x) - 1", [])
        ("(Foo())[Length(x) - 1]", infix)
        ("(Foo())[1 .. ", expression)
        ("(Foo())[1 ...", [])
        ("(Foo())[1 ...]", infix)
        ("new", operationStatement)
        ("new ", types)
        ("new Int", types)
        ("new Int[", expression)
        ("new Int[x", expression)
        ("new Int[x ", infix)
        ("new Int[x +", expression)
        ("new Int[x +y", expression)
        ("new Int[x + 1", [])
        ("new Int[x + 1]", infix)
        ("new Int[][", expression)
        ("new Int[][2", [])
        ("new Int[][2]", infix)
        ("x:", [])
        ("x::", [NamedItem])
        ("x::Foo", [NamedItem])
        ("x::Foo ", infix)
        ("(Foo())::", [NamedItem])
        ("(Foo())::Foo", [NamedItem])
        ("(Foo())::Foo ", infix)
        ("arr w", infix)
        ("arr w/", NamedItem :: expression)
        ("arr w/ ", NamedItem :: expression)
        ("arr w/ 2", [])
        ("arr w/ 2 ", infix)
        ("arr w/ 2 ..", expression)
        ("arr w/ 2 .. ", expression)
        ("arr w/ 2..", expression)
        ("arr w/ 2..x", expression)
        ("arr w/ 2..4", [])
        ("arr w/ 2..4 <-", expression)
        ("arr w/ 2..4 <- ", expression)
        ("arr w/ 2..4 <- true", expression)
        ("x w/ Foo", NamedItem :: expression)
        ("x w/ Foo ", infix)
        ("x w/ Foo <", types @ expression)
        ("x w/ Foo <-", expression)
        ("x w/ Foo <- ", expression)
        ("x w/ Foo <- false", expression)
        ("Foo() ?", expression)
        ("Foo() ? ", expression)
        ("Foo() ?x", expression)
        ("Foo() ? 1 + 2 |", expression)
        ("Foo() ? 1 + 2 |x", expression)
        ("Foo() ? 1 + 2 | ", expression)
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
        ("$\"hello {x +y", expression)
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
