module Microsoft.Quantum.QsCompiler.Testing.CompletionParsingTests

open FParsec
open Xunit
open Microsoft.Quantum.QsCompiler.TextProcessing.CompletionParsing

let private testContext text expected =
    match getContext text with
    | Success (context, _, _) -> Assert.Equal(expected, context)
    | Failure (_) -> Assert.True(false, "failed to parse")

let private testInvalid text =
    let parses =
        match getContext text with
        | Success (_) -> true
        | Failure (_) -> false
    Assert.False(parses, "unexpectedly parsed successfully")

[<Fact>]
let ``Operation declaration parser tests`` () =
    testContext "" (Keyword "operation")
    // TODO: testContext "o" (Keyword "operation")
    testContext "operation" (Keyword "operation")
    testContext "operation " DeclaredSymbol
    testContext "operation Foo" DeclaredSymbol
    testContext "operation Foo " Nothing
    testContext "operation Foo (" DeclaredSymbol
    testContext "operation Foo (q" DeclaredSymbol
    testContext "operation Foo (q :" Type
    testContext "operation Foo (q : Qubit" Type
    testContext "operation Foo (q : Qubit)" Nothing
    testContext "operation Foo (q : Qubit) :" Type
    testContext "operation Foo (q : Qubit) : " Type
    testContext "operation Foo (q : Qubit) : Unit" Type
    testContext "operation Foo (q : Qubit) : Unit " (Keyword "is")
    // TODO: testContext "operation Foo (q : Qubit) : Unit i" (Keyword "is")
    testContext "operation Foo (q : Qubit) : Unit is" (Keyword "is")
    testContext "operation Foo (q : Qubit) : Unit is " Characteristic
    testContext "operation Foo (q : Qubit) : Unit is A" Characteristic
    testContext "operation Foo (q : Qubit) : Unit is Adj" Characteristic
    testContext "operation Foo (q : Qubit) : Unit is Adj " Nothing
    testContext "operation Foo (q : Qubit) : Unit is Adj +" Characteristic
    testContext "operation Foo (q : Qubit) : Unit is Adj + " Characteristic
    testContext "operation Foo (q : Qubit) : Unit is Adj + Ctl" Characteristic
    testContext "operation Foo (q : Qubit) : Unit is Adj + Ctl " Nothing
    testContext "operation Foo (q : Qubit) : Unit is Adj + Cat " Nothing
    testContext "operation Foo (q : Qubit) : (Int, MyT" Type
    testContext "operation Foo (q : Qubit) : (Int," Type
    testContext "operation Foo (q : Qubit) : (Int, MyT " Nothing
    testContext "operation Foo (q : (" Type
    testContext "operation Foo (q : (Qubit" Type
    testContext "operation Foo (q : (Qubit," Type
    testContext "operation Foo (q : (Qubit, " Type
    testContext "operation Foo (q : (Qubit, Qubit" Type
    testContext "operation Foo (q : (Qubit, Qubit " Nothing
    testContext "operation Foo (q : (Qubit, Qubit), " DeclaredSymbol
    testContext "operation Foo (q : (Qubit => " Type
    testContext "operation Foo (q : (Qubit => Unit" Type
    testContext "operation Foo (q : (Qubit => Unit " (Keyword "is")
    testContext "operation Foo (q : (Qubit => Unit is " Characteristic
    testContext "operation Foo (q : (Qubit => Unit is Adj)) : Unit" Type
    testContext "operation Foo (f : (Int -> " Type
    testContext "operation Foo (f : (Int -> Int)) : Unit" Type
    testContext "operation Foo (f : (Int -> Int)) : Unit " (Keyword "is")
    testContext "operation Foo (q : (Int => Int)) : Unit" Type
    testContext "operation Foo (q : ((Int -> Int) => " Type
    testContext "operation Foo (q : ((Int -> Int) => Int " (Keyword "is")
    testContext "operation Foo (q : ((Int -> Int) => Int)) : Unit" Type
    testContext "operation Foo (q : ((Int->Int)=>Int)) : Unit" Type
    testContext "operation Foo (q : ((Int -> Int) -> Int)) : Unit" Type
    testContext "operation Foo (q : ((Int => Int) => Int)) : Unit" Type
    testContext "operation Foo<'T> (q : 'T) : Unit" Type
    testContext "operation Foo<'T> (q : ('T => Int)) : Unit" Type
    testContext "operation Foo<'T> (q : ('T -> Int)) : Unit" Type
    testContext "operation Foo<'T> (q : ('T->Int)) : Unit" Type
    testContext "operation Foo<'T> (q : (MyType -> Int)) : Unit" Type
    testInvalid "operation Foo (q : Qubit) : (Int, MyT is Adj"
    testInvalid "operation Foo (q : Qubit) : (Int, MyT is Adj "
