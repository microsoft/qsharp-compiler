// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.TextTests

open Microsoft.Quantum.QsCompiler.Testing.TestUtils
open Microsoft.Quantum.QsCompiler.TextProcessing
open Microsoft.Quantum.QsCompiler.TextProcessing.ParsingPrimitives
open Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxBuilder
open Xunit


// Punctuation tests
[<Fact>]
let ``Simple punctuation tests`` () =
    Assert.True(simpleParseString comma ",", "Comma positive")
    Assert.False(simpleParseString comma ";", "Comma negative")

    Assert.True(simpleParseString colon ":", "Colon positive")
    Assert.False(simpleParseString colon ",", "Colon negative")

    Assert.True(simpleParseString equal "=", "Equal positive")
    Assert.False(simpleParseString equal "+", "Equal negative")

    Assert.True(simpleParseString opArrow "=>", "Operation arrow positive")
    Assert.False(simpleParseString opArrow "->", "Operation arrow negative")

    Assert.True(simpleParseString fctArrow "->", "Function arrow positive")
    Assert.False(simpleParseString fctArrow "=>", "Function arrow negative")

    Assert.True(simpleParseString unitValue "()", "Unit value positive")
    Assert.True(simpleParseString unitValue "(   )", "Unit value positive 2")

    Assert.True
        (simpleParseString unitValue @"(
    )",
         "Unit value positive 3")

    Assert.False(simpleParseString unitValue "unit", "Unit value negative")

    Assert.True(simpleParseString missingExpr "_", "Missing expr positive")
    Assert.False(simpleParseString missingExpr "-", "Missing expr negative")

    Assert.True(simpleParseString discardedSymbol "_", "Discarded symbol positive")
    Assert.False(simpleParseString discardedSymbol "-", "Discarded symbol negative")

    Assert.True(simpleParseString omittedSymbols "...", "Omitted symbols positive")
    Assert.False(simpleParseString omittedSymbols "..", "Omitted symbols negative")

[<Fact>]
let ``Bracket tests`` () =
    Assert.True(simpleParseString lTuple "(", "Left tuple")
    Assert.True(simpleParseString rTuple ")", "Right tuple")
    Assert.True(simpleParseString lArray "[", "Left array")
    Assert.True(simpleParseString rArray "]", "Right array")
    Assert.True(simpleParseString lAngle "<", "Left angle")
    Assert.True(simpleParseString rAngle ">", "Right angle")
    Assert.True(simpleParseString lCurly "{", "Left curly")
    Assert.True(simpleParseString rCurly "}", "Right curly")

// Keyword tests
// These are used for a variety of tests around keywords
let languageKeywords =
    [
        "Unit"
        "Int"
        "BigInt"
        "Double"
        "Qubit"
        "Pauli"
        "Result"
        "Range"
        "Bool"
        "String"
        "auto"
        "self"
        "intrinsic"
        "invert"
        "distribute"
        "fixup"
        "in"
    ]

let fragmentKeywords =
    [
        "let"
        "mutable"
        "set"
        "for"
        "while"
        "repeat"
        "until"
        "within"
        "apply"
        "if"
        "elif"
        "else"
        "return"
        "fail"
        "using"
        "borrowing"
        "namespace"
        "open"
        "operation"
        "function"
        "newtype"
        "body"
        "adjoint"
        "controlled"
    ]

let reservedWords =
    [
        "Adjoint"
        "Controlled"
        "new"
        "not"
        "and"
        "or"
        "PauliI"
        "PauliX"
        "PauliY"
        "PauliZ"
        "One"
        "Zero"
    ]

let keywords = languageKeywords @ fragmentKeywords @ reservedWords

let nonkeywords =
    [
        "allocate"
        "import"
        "class"
        "member"
        "IPauli"
        "XPauli"
        "YPauli"
        "ZPauli"
        "one"
        "zero"
        "int"
        "double"
        "qubit"
        "pauli"
        "result"
        "range"
        "bool"
        "string"
        "var"
        "type"
        "Length"
        "Power"
    ]

[<Fact>]
let ``Keyword tests`` () =
    keywords
    |> List.iter (fun k -> Assert.True(Keywords.ReservedKeywords.Contains k, "Keyword " + k + " missing"))

[<Fact>]
let ``Non-keyword tests`` () =
    nonkeywords
    |> List.iter (fun k -> Assert.False(Keywords.ReservedKeywords.Contains k, "Incorrect keyword " + k))

[<Fact>]
let ``Reserved keyword parser tests`` () =
    reservedWords
    |> List.iter (fun k -> Assert.True(parse_string qsReservedKeyword k, "Failed to identify keyword " + k))

[<Fact>]
let ``Language keyword parser tests`` () =
    languageKeywords
    |> List.iter (fun k -> Assert.True(parse_string qsLanguageKeyword k, "Failed to identify keyword " + k))

[<Fact>]
let ``Fragment keyword parser tests`` () =
    fragmentKeywords
    |> List.iter (fun k -> Assert.True(parse_string qsFragmentHeader k, "Failed to identify keyword " + k))
