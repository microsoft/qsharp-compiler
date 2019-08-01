﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.SyntaxTests

open FParsec
open TestUtils
open Xunit
open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.TextProcessing.ExpressionParsing
open Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxBuilder
open Microsoft.Quantum.QsCompiler.SyntaxTokens

let private rawString = getStringContent (manyChars anyChar) |>> fst

// Component parsers
[<Fact>]
let ``String parser tests`` () =
    [
        "\"This is a string\"";
        "\"\"";
        "$\"Interpolated string\"";
        "$\"Interpolated string with {variable}\"";
        "$\"Interpolated string with {expression+1}\"";
        "$\"Interpolated string with {\"string argument\"}\"";
        "$\"Interpolated string with {\"str1\" + \"str2\"}\"";
        "$\"Interpolated string with {true ? \"str1\" | \"str2\"}\"";
        "$\"Interpolated string with {true ? $\"{1}\" | $\"{2}\"}\"";
    ]
    |> List.iter (fun s -> Assert.True(simpleParseString (stringLiteral .>> eof) s, "Failed to parse string " + s))

    // testing whether tabs etc in strings are processed correctly
    let testChar offset char string = 
        match parse_string_diags_res rawString string with 
        | true, _, Some parsed -> 
            Assert.Equal(offset + 1, parsed.Length)
            Assert.Equal(char, parsed.[offset])
        | _ -> Assert.True(false, "failed to parse")
    testChar 0 '\t' "\"\t\""
    testChar 0 '\n' "\"\r\"" // carriage returns will get replace by \n
    testChar 0 '\n' "\"\n\""
    testChar 3 '\t' "$\"{0}\t\""
    testChar 3 '\n' "$\"{0}\r\"" 
    testChar 3 '\n' "$\"{0}\n\""


[<Fact>]
let ``Optional tuple bracket tests`` () =
    let parser = (optTupleBrackets rawString) |>> fst
    [
        ("\"\";",   true,   "", [Error ErrorCode.MissingLTupleBracket;Error ErrorCode.MissingRTupleBracket]);
        ("(\"\");", true,   "", []);
        ("(\"\" ",  true,   "", [Error ErrorCode.MissingRTupleBracket]);
        ("\"\");",  true,   "", [Error ErrorCode.MissingLTupleBracket])
    ]
    |> List.iter (testOne parser)


[<Fact>]
let ``Tuple bracket tests`` () =
    let parser = (tupleBrackets rawString) |>> fst
    [
        ("(\"\")",   true,  "", []);
        ("(\"\"",    false, "", [Error ErrorCode.MissingRTupleBracket]);
        ("\"\");",   false, "", [Error ErrorCode.MissingLTupleBracket])
    ]
    |> List.iter (testOne parser)


[<Fact>]
let ``Array bracket tests`` () =
    let parser = (arrayBrackets rawString) |>> fst
    [
        ("[\"\"]",   true,  "", []);
        ("[\"\"",    false, "", [Error ErrorCode.MissingRArrayBracket]);
        ("\"\"];",   false, "", [Error ErrorCode.MissingLArrayBracket])
    ]
    |> List.iter (testOne parser)


[<Fact>]
let ``Angle bracket tests`` () =
    let parser = (angleBrackets rawString) |>> fst
    [
        ("<\"\">",   true,  "", []);
        ("<\"\"",    false, "", [Error ErrorCode.MissingRAngleBracket]);
        ("\"\">;",   false, "", [Error ErrorCode.MissingLAngleBracket])
    ]
    |> List.iter (testOne parser)


[<Fact>]
let ``Curly bracket tests`` () =
    let parser = (curlyBrackets rawString) |>> fst
    [
        ("{\"\"}",   true,  "", []);
        ("{\"\"",    false, "", [Error ErrorCode.MissingRCurlyBracket]);
        ("\"\"};",   false, "", [Error ErrorCode.MissingLCurlyBracket])
    ]
    |> List.iter (testOne parser)


[<Fact>]
let ``Build tuple tests`` () =
    let parser = buildTuple rawString (fst >> String.concat ";") ErrorCode.InvalidValueTuple ErrorCode.MissingExpression ""
    [
        ("(\"a\")",             true,    "a",          []);
        ("(\"a\",\"b\")",       true,    "a;b",        []);
        ("()",                  false,   "",           []);
        ("(\"a\",)",            true,    "a",          [Warning WarningCode.ExcessComma]);
        ("(\"a\";)",            true,    "a",          [Error ErrorCode.ExcessContinuation]);
        ("(\"a\",5)",           true,    "a;",         [Error ErrorCode.InvalidValueTuple]);
        ("(\"a\",,\"b\")",      true,    "a;;b",       [Error ErrorCode.MissingExpression]);
        ("(\"a\"",              false,   "a",          [Error ErrorCode.MissingRCurlyBracket]);
        ("\"a\");",             false,   "a",          [Error ErrorCode.MissingLCurlyBracket]);
    ]
    |> List.iter (testOne parser)


[<Fact>]
let ``Symbol name tests`` () =
    let parser = 
        symbolNameLike ErrorCode.ExpectingUnqualifiedSymbol 
        >>= function | Some str -> preturn str | None -> fail ""
    [
        ("a",                   true,    "a",              []);
        ("a1",                  true,    "a1",             []);
        ("A",                   true,    "A",              []);
        ("if",                  false,   "",               []);
        ("IF",                  true,    "IF",             []);
        ("a_",                  true,    "a_",             []);
        ("_a",                  true,    "_a",             []);
        ("_",                   false,   "",               []);
        ("__",                  false,   "",               []);
        ("__a",                 true,    "__a",            []);
    ]
    |> List.iter (testOne parser)


[<Fact>]
let ``Expression literal tests`` () =

    // constants that should be handled
    let minInt = System.Int64.MinValue // -9223372036854775808L
    let maxInt = System.Int64.MaxValue // 9223372036854775807L
    let minDouble = System.Double.MinValue // -1.7976931348623157E+308
    let maxDouble = System.Double.MaxValue // 1.7976931348623157E+308
    Assert.Equal(minInt, -(-minInt))

    // constants that should raise an error
    let absMinIntMinus1 = uint64(-minInt) + 1UL
    let minIntMinus1Str = absMinIntMinus1.ToString() |> sprintf "-%s"
    let maxIntPlus1 = uint64(maxInt) + 1UL
    let maxIntPlus2 = uint64(maxInt) + 2UL
    let doublePrecBound = "1.79769313486232E+308" // what shows up as out of range in C#
    let minusDoublePrecBound = sprintf "-%s" doublePrecBound

    let noExprs = ([| |] : QsExpression[]).ToImmutableArray()
    [
        ("()",                    true,    toExpr UnitValue,                                                      []); 
        ("1",                     true,    toInt 1,                                                               []); 
        ("+1",                    true,    toInt 1,                                                               []); 
        ("-1",                    true,    toExpr (NEG (toInt 1)),                                                []); 
        (minInt.ToString(),       true,    toExpr (NEG (NEG (IntLiteral minInt |> toExpr) |> toExpr)),            []); 
        (minIntMinus1Str,         true,    toExpr (NEG (IntLiteral ((int64)absMinIntMinus1) |> toExpr)),          [Error ErrorCode.IntOverflow]);
        (maxIntPlus1.ToString(),  true,    toExpr (NEG (IntLiteral ((int64)maxIntPlus1) |> toExpr)),              []); // no error, will pop up at runtime
        (maxIntPlus2.ToString(),  true,    toExpr (IntLiteral ((int64)maxIntPlus2)),                              [Error ErrorCode.IntOverflow]);
        (doublePrecBound,         true,    toExpr (DoubleLiteral System.Double.NaN),                              [Error ErrorCode.DoubleOverflow]);
        (minusDoublePrecBound,    true,    toExpr (NEG (DoubleLiteral System.Double.NaN |> toExpr)),              [Error ErrorCode.DoubleOverflow]);
        (minInt.ToString(),       true,    toExpr (NEG (NEG (IntLiteral minInt |> toExpr) |> toExpr)),            []); 
        (maxInt.ToString(),       true,    toExpr (IntLiteral maxInt),                                            []); 
        (minDouble.ToString("R"), true,    toExpr (NEG (DoubleLiteral -minDouble |> toExpr)),                     []); 
        (maxDouble.ToString("R"), true,    toExpr (DoubleLiteral maxDouble),                                      []); 
        ("0x1",                   true,    toInt 1,                                                               []); 
        ("+0x1",                  true,    toInt 1,                                                               []); 
        ("1L",                    true,    toBigInt "1",                                                          []); 
        ("+1L",                   true,    toBigInt "1",                                                          []); 
        ("-1L",                   true,    toExpr (NEG (toBigInt "1")),                                           []); 
        ("10000000000000000L",    true,    toBigInt "10000000000000000",                                          []); 
        ("0xfL",                  true,    toBigInt "15",                                                         []); 
        ("0xffL",                 true,    toBigInt "255",                                                        []); 
        ("1l",                    true,    toBigInt "1",                                                          []); 
        ("+1l",                   true,    toBigInt "1",                                                          []); 
        ("-1l",                   true,    toExpr (NEG (toBigInt "1")),                                           []); 
        ("10000000000000000l",    true,    toBigInt "10000000000000000",                                          []); 
        ("0xfl",                  true,    toBigInt "15",                                                         []); 
        ("+0xfl",                 true,    toBigInt "15",                                                         []); 
        ("0xffl",                 true,    toBigInt "255",                                                        []); 
        ("+0xffl",                true,    toBigInt "255",                                                        []); 
        ("0b1",                   true,    toInt 1,                                                               []); 
        ("0b100",                 true,    toInt 4,                                                               []); 
        ("+0b100",                true,    toInt 4,                                                               []); 
        ("-0b100",                true,    toExpr (NEG (toInt 4)),                                                []);
        (".1",                    true,    toExpr (DoubleLiteral 0.1),                                            []); 
        ("1.0",                   true,    toExpr (DoubleLiteral 1.0),                                            []); 
        ("1.",                    true,    toExpr (DoubleLiteral 1.0),                                            []); 
        ("+1.0",                  true,    toExpr (DoubleLiteral 1.0),                                            []); 
        ("-1.0",                  true,    toExpr (NEG (toExpr (DoubleLiteral 1.0))),                             []); 
        ("-1.0e2",                true,    toExpr (NEG (toExpr (DoubleLiteral 100.0))),                           []);
        ("-1.0e-2",               true,    toExpr (NEG (toExpr (DoubleLiteral 0.01))),                            []);
        ("\"\"",                  true,    toExpr (StringLiteral (NonNullable<string>.New "", noExprs)),          []);
        ("\"hello\"",             true,    toExpr (StringLiteral (NonNullable<string>.New "hello", noExprs)),     []);
        ("One",                   true,    toExpr (ResultLiteral One),                                            []);
        ("Zero",                  true,    toExpr (ResultLiteral Zero),                                           []);
        ("PauliI",                true,    toExpr (PauliLiteral PauliI),                                          []);
        ("PauliX",                true,    toExpr (PauliLiteral PauliX),                                          []);
        ("PauliY",                true,    toExpr (PauliLiteral PauliY),                                          []);
        ("PauliZ",                true,    toExpr (PauliLiteral PauliZ),                                          []);
        ("true",                  true,    toExpr (BoolLiteral true),                                             []);
        ("false",                 true,    toExpr (BoolLiteral false),                                            []);
    ]
    |> List.iter testExpr


[<Fact>]
let ``Simple arithmetic expression tests`` () =
    [
        ("-  1",                true,    toExpr (NEG (toExpr (IntLiteral 1L))),                                 []); 
        ("~~~1",                true,    toExpr (BNOT (toExpr (IntLiteral 1L))),                                []); 
        ("1+1",                 true,    toExpr (ADD (toInt 1, toInt 1)),                                       []); 
        ("1L+1L",               true,    toExpr (ADD (toBigInt "1", toBigInt "1")),                             []); 
        ("1-1",                 true,    toExpr (SUB (toInt 1, toInt 1)),                                       []); 
        ("1*1",                 true,    toExpr (MUL (toInt 1, toInt 1)),                                       []); 
        ("1/1",                 true,    toExpr (DIV (toInt 1, toInt 1)),                                       []); 
        ("1%1",                 true,    toExpr (MOD (toInt 1, toInt 1)),                                       []); 
        ("1^1",                 true,    toExpr (POW (toInt 1, toInt 1)),                                       []); 
        ("1|||1",               true,    toExpr (BOR (toInt 1, toInt 1)),                                       []); 
        ("1&&&1",               true,    toExpr (BAND (toInt 1, toInt 1)),                                      []); 
        ("1^^^1",               true,    toExpr (BXOR (toInt 1, toInt 1)),                                      []); 
        ("1>>>1",               true,    toExpr (RSHIFT (toInt 1, toInt 1)),                                    []); 
        ("1<<<1",               true,    toExpr (LSHIFT (toInt 1, toInt 1)),                                    []); 
    ]
    |> List.iter testExpr


[<Fact>]
let ``Simple Boolean expression tests`` () =
    [
        ("true && true",        true,    toExpr (AND (BoolLiteral true |> toExpr, BoolLiteral true |> toExpr)), [Warning WarningCode.DeprecatedANDoperator]); 
        ("true || true",        true,    toExpr (OR (BoolLiteral true |> toExpr, BoolLiteral true |> toExpr)),  [Warning WarningCode.DeprecatedORoperator]); 
        ("!true",               true,    toExpr (NOT (BoolLiteral true |> toExpr)),                             [Warning WarningCode.DeprecatedNOToperator]); 
        ("true and true",       true,    toExpr (AND (BoolLiteral true |> toExpr, BoolLiteral true |> toExpr)), []); 
        ("true or true",        true,    toExpr (OR (BoolLiteral true |> toExpr, BoolLiteral true |> toExpr)),  []); 
        ("not true",            true,    toExpr (NOT (BoolLiteral true |> toExpr)),                             []); 
    ]
    |> List.iter testExpr


[<Fact>]
let ``Simple comparison expression tests`` () =
    [
        ("1<2",                 true,    toExpr (LT (toInt 1, toInt 2)),                                        []); 
        ("1<=2",                true,    toExpr (LTE (toInt 1, toInt 2)),                                       []); 
        ("1>2",                 true,    toExpr (GT (toInt 1, toInt 2)),                                        []); 
        ("1>=2",                true,    toExpr (GTE (toInt 1, toInt 2)),                                       []); 
        ("1==2",                true,    toExpr (EQ (toInt 1, toInt 2)),                                        []); 
        ("1!=2",                true,    toExpr (NEQ (toInt 1, toInt 2)),                                       []); 
        ("1<2 or 1>2",          true,    toExpr (OR (LT (toInt 1, toInt 2) |> toExpr, 
                                                     GT (toInt 1, toInt 2) |> toExpr)),                         []); 
    ]
    |> List.iter testExpr


[<Fact>]
let ``Identifier tests`` () =
    [
        ("x",                   true,    toSymbol "x",                                                          []); 
        ("a.b.c",               true,    toExpr (Identifier ({Symbol=QualifiedSymbol("a.b" |> NonNullable<string>.New,
                                                                                     "c" |> NonNullable<string>.New);
                                                              Range=Null}, Null)),                              []); 
    ]
    |> List.iter testExpr


[<Fact>]
let ``Complex literal tests`` () =
    [
        ("[1,2,3]",             true,    toArray [ toInt 1; toInt 2; toInt 3 ],                                 []); 
        ("[1,x,3]",             true,    toArray [ toInt 1; toSymbol "x"; toInt 3 ],                            []); 
        ("(1,2,3)",             true,    toTuple [ toInt 1; toInt 2; toInt 3 ],                                 []); 
        ("(x,2,3)",             true,    toTuple [ toSymbol "x"; toInt 2; toInt 3 ],                            []); 
        ("1..2",                true,    toExpr (RangeLiteral (toInt 1,
                                                        toInt 2)),                                              []); 
        ("1..2..3",             true,    toExpr (RangeLiteral (RangeLiteral (toInt 1, toInt 2) |> toExpr,
                                                        toInt 3)),                                              []); 
        ("1..2..x",             true,    toExpr (RangeLiteral (RangeLiteral (toInt 1, toInt 2) |> toExpr,
                                                        toSymbol "x")),                                         []); 
        ("1..x..3",             true,    toExpr (RangeLiteral (RangeLiteral (toInt 1, toSymbol "x") |> toExpr, toInt 3)),     []); 
        ("x..2..3",             true,    toExpr (RangeLiteral (RangeLiteral (toSymbol "x", toInt 2) |> toExpr,
                                                        toInt 3)),                                              []); 
        ("1..x",                true,    toExpr (RangeLiteral (toInt 1, toSymbol "x")),                                []); 
        ("x..2",                true,    toExpr (RangeLiteral (toSymbol "x", toInt 2)),                                []); 
        ("x..y",                true,    toExpr (RangeLiteral (toSymbol "x", toSymbol "y")),                           []); 
    ]
    |> List.iter testExpr


[<Fact>]
let ``Call tests`` () =
    [
        ("x()",                 true,    toExpr (CallLikeExpression (toSymbol "x", UnitValue |> toExpr)),       []);
        ("x(1,2)",              true,    toExpr (CallLikeExpression (toSymbol "x", 
                                                                     toTuple [ toInt 1; 
                                                                               toInt 2])),      []);
        ("Adjoint x()",         true,    toExpr (CallLikeExpression (AdjointApplication (toSymbol "x") |> toExpr, 
                                                                     UnitValue |> toExpr)),                     []);
        ("Controlled x()",      true,    toExpr (CallLikeExpression (ControlledApplication (toSymbol "x") |> toExpr, 
                                                                     UnitValue |> toExpr)),                     []);

        ("(x(_,1))(2)",         true,    toExpr (CallLikeExpression (toTuple [toExpr (CallLikeExpression (toSymbol "x", 
                                                                                                 toTuple [ MissingExpr |> toExpr;
                                                                                                           toInt 1])
                                                                    )], toTuple [ toInt 2 ])),                   []);
        ("(x(_,1))(1,2)",       true,    toExpr (CallLikeExpression (toTuple [toExpr (CallLikeExpression (toSymbol "x", 
                                                                                                 toTuple [ MissingExpr |> toExpr;
                                                                                                           toInt 1])
                                                                    )], toTuple [ toInt 1;
                                                                                 toInt 2 ])),                   []);
        ("(x(1,(2, _)))(2)",    true,    toExpr (CallLikeExpression (toTuple [toExpr (CallLikeExpression (toSymbol "x", 
                                                                                                 toTuple [ toInt 1;
                                                                                                           toTuple [ toInt 2;
                                                                                                                     MissingExpr |> toExpr];
                                                                                                         ])
                                                                    )], toTuple [ toInt 2 ])),                   []);
        ("(x(_,(2, _)))(1,2)",  true,    toExpr (CallLikeExpression (toTuple [toExpr (CallLikeExpression (toSymbol "x", 
                                                                                                 toTuple [ MissingExpr |> toExpr;
                                                                                                           toTuple [ toInt 2;
                                                                                                                     MissingExpr |> toExpr];
                                                                                                         ])
                                                                    )], toTuple [ toInt 1;
                                                                                 toInt 2 ])),                   []);
    ]
    |> List.iter testExpr


[<Fact>]
let ``Modifier tests`` () = // modifiers can only be applied to identifiers, arity-1 tuples, and array item expressions
    [
        // modifiers on identifiers:
        ("ab!",                      true,    toExpr (UnwrapApplication (toSymbol "ab")),                                        []); 
        ("!ab!",                     true,    toExpr (NOT (UnwrapApplication (toSymbol "ab") |> toExpr)),                        [Warning WarningCode.DeprecatedNOToperator]); 
        ("ab!!",                     true,    toExpr (UnwrapApplication (UnwrapApplication (toSymbol "ab") |> toExpr)),          []); 
        ("Adjoint x",                true,    toExpr (AdjointApplication (toSymbol "x")),                                        []);
        ("Controlled Adjoint x",     true,    toExpr (ControlledApplication (AdjointApplication (toSymbol "x") |> toExpr)),      []);
        ("ab! ()",                   true,    toExpr (CallLikeExpression (UnwrapApplication (toSymbol "ab") |> toExpr, 
                                                                          UnitValue |> toExpr)),                                 []); 
        ("ab!! ()",                  true,    toExpr (CallLikeExpression (UnwrapApplication (UnwrapApplication (toSymbol "ab") |> toExpr) |> toExpr, 
                                                                          UnitValue |> toExpr)),                                 []); 
        ("Adjoint x()",              true,    toExpr (CallLikeExpression (AdjointApplication (toSymbol "x") |> toExpr,
                                                                          UnitValue |> toExpr)),                                 []);
        ("Adjoint Controlled x()",   true,    toExpr (CallLikeExpression (AdjointApplication (ControlledApplication (toSymbol "x") |> toExpr) |> toExpr,
                                                                          UnitValue |> toExpr)),                                 []);
        ("Adjoint x! ()",            true,    toExpr (CallLikeExpression (AdjointApplication (UnwrapApplication (toSymbol "x") |> toExpr) |> toExpr,
                                                                          UnitValue |> toExpr)),                                 []);
        // modifiers on arity-1 tuples:
        ("(udt(x))!",                true,    toExpr (UnwrapApplication (toTuple [(CallLikeExpression (toSymbol "udt",
                                                                                    toTuple [toSymbol "x"]) |> toExpr)])),       []);
        ("(udt(x))! ()",             true,    toExpr (CallLikeExpression ((UnwrapApplication (toTuple [CallLikeExpression (
                                                                                                        toSymbol "udt",
                                                                                                        toTuple [toSymbol "x"]) |> toExpr])) |> toExpr, 
                                                                          UnitValue |> toExpr)),                                 []);
        ("Controlled (udt(x))! ()",  true,    toExpr (CallLikeExpression (ControlledApplication ((UnwrapApplication (toTuple [CallLikeExpression (
                                                                                                                                toSymbol "udt",
                                                                                                                                toTuple [toSymbol "x"]) |> toExpr])) |> toExpr) |> toExpr, 
                                                                          UnitValue |> toExpr)),                                 []);
        ("(Controlled (udt(x))!)()", true,    toExpr (CallLikeExpression (toTuple [ControlledApplication ((UnwrapApplication (toTuple [CallLikeExpression (
                                                                                                                                        toSymbol "udt",
                                                                                                                                        toTuple [toSymbol "x"]) |> toExpr])) |> toExpr) |> toExpr], 
                                                                          UnitValue |> toExpr)),                                 []);
        // modifiers on array item expressions
        ("x[i]!",                    true,    toExpr (UnwrapApplication (ArrayItem(toSymbol "x", 
                                                                                   toSymbol "i") |> toExpr)),                               []); 
        ("Adjoint x[i]",             true,    toExpr (AdjointApplication (ArrayItem(toSymbol "x", 
                                                                                    toSymbol "i") |> toExpr)),                   []);
        ("x[i]! ()",                 true,    toExpr (CallLikeExpression (UnwrapApplication (ArrayItem(toSymbol "x", 
                                                                                                       toSymbol "i") |> toExpr) |> toExpr, 
                                                                          UnitValue |> toExpr)),                                 []); 
        ("Adjoint x[i] ()",          true,    toExpr (CallLikeExpression (AdjointApplication (ArrayItem(toSymbol "x", 
                                                                                                        toSymbol "i") |> toExpr) |> toExpr,
                                                                          UnitValue |> toExpr)),                                 []);
        ("Controlled x[i]! ()",      true,    toExpr (CallLikeExpression (ControlledApplication (UnwrapApplication (ArrayItem(toSymbol "x", 
                                                                                                                              toSymbol "i") |> toExpr) |> toExpr) |> toExpr,
                                                                          UnitValue |> toExpr)),                                 []);

        // Note that since we do not currently have the means to support a left-recursive grammar (would require a different approach to parsing),
        // we have to prioritize in order to stick with a non-left-recursive grammar (there are algorithms that translate a left-recursive grammar into a non-left-recursive one, 
        // but for practical purposes would require reconstructing the syntax tree of the old grammar from the one built by the new grammar). 
        // We prioritize call like expressions over array item expressions, 
        // and hence similar syntax patterns for array item expressions as tested above for call-like expressions are currently not processed / do not exist in the language. 
    ]
    |> List.iter testExpr


[<Fact>]
let ``Operation type tests`` () = 
    [
        ("new (Qubit => Unit is Adj)[0]",  true,    toNewArray (toOpType qubitType unitType adjSet) (toInt 0),  []); 
        ("new Qubit => Unit is Adj[0]"  ,  true,    toNewArray (toOpType qubitType unitType adjSet) (toInt 0),  [Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket]); 
        ("new (Qubit => Unit) is Adj[0]",  true,    toNewArray (toOpType qubitType unitType adjSet) (toInt 0),  [Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket]); 
    
        ("new (Qubit => Unit : Adjoint)[0]",  true,    toNewArray (toOpType qubitType unitType adjSet) (toInt 0),  [Warning WarningCode.DeprecatedOpCharacteristics]); 
        ("new Qubit => Unit : Adjoint[0]"  ,  true,    toNewArray (toOpType qubitType unitType adjSet) (toInt 0),  [Warning WarningCode.DeprecatedOpCharacteristics; Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket]); 
        ("new (Qubit => Unit) : Adjoint[0]",  true,    toNewArray (toOpType qubitType unitType adjSet) (toInt 0),  [Warning WarningCode.DeprecatedOpCharacteristics; Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket]); 
        
        ("new (Qubit => Unit : Adj)[0]",  true,    toNewArray (toOpType qubitType unitType adjSet) (toInt 0),  [Warning WarningCode.DeprecatedOpCharacteristicsIntro]); 
        ("new Qubit => Unit : Adj[0]"  ,  true,    toNewArray (toOpType qubitType unitType adjSet) (toInt 0),  [Warning WarningCode.DeprecatedOpCharacteristicsIntro; Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket]); 
        ("new (Qubit => Unit) : Adj[0]",  true,    toNewArray (toOpType qubitType unitType adjSet) (toInt 0),  [Warning WarningCode.DeprecatedOpCharacteristicsIntro; Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket]); 

        ("new (Qubit => Unit is Adj + Ctl)[0]",  true,    toNewArray (toOpType qubitType unitType adjCtlSet) (toInt 0),  []); 
        ("new Qubit => Unit is Adj + Ctl[0]"  ,  true,    toNewArray (toOpType qubitType unitType adjCtlSet) (toInt 0),  [Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket]); 
        ("new (Qubit => Unit) is Adj + Ctl[0]",  true,    toNewArray (toOpType qubitType unitType adjCtlSet) (toInt 0),  [Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket]); 

        ("new (Qubit => Unit : Adjoint, Controlled)[0]",  true,    toNewArray (toOpType qubitType unitType adjCtlSet) (toInt 0),  [Warning WarningCode.DeprecatedOpCharacteristics]); 
        ("new Qubit => Unit : Adjoint, Controlled[0]"  ,  true,    toNewArray (toOpType qubitType unitType adjCtlSet) (toInt 0),  [Warning WarningCode.DeprecatedOpCharacteristics; Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket]); 
        ("new (Qubit => Unit) : Adjoint, Controlled[0]",  true,    toNewArray (toOpType qubitType unitType adjCtlSet) (toInt 0),  [Warning WarningCode.DeprecatedOpCharacteristics; Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket]); 
        
        ("new (Qubit => Unit : Adj + Ctl)[0]",  true,    toNewArray (toOpType qubitType unitType adjCtlSet) (toInt 0),  [Warning WarningCode.DeprecatedOpCharacteristicsIntro]); 
        ("new Qubit => Unit : Adj + Ctl[0]"  ,  true,    toNewArray (toOpType qubitType unitType adjCtlSet) (toInt 0),  [Warning WarningCode.DeprecatedOpCharacteristicsIntro; Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket]); 
        ("new (Qubit => Unit) : Adj + Ctl[0]",  true,    toNewArray (toOpType qubitType unitType adjCtlSet) (toInt 0),  [Warning WarningCode.DeprecatedOpCharacteristicsIntro; Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket]); 
        
        ("new (Qubit => Unit : Adj, Ctl)[0]",  true,    toNewArray (toOpType qubitType unitType adjCtlSet) (toInt 0),  [Warning WarningCode.DeprecatedOpCharacteristics]); 
        ("new Qubit => Unit : Adj, Ctl[0]"  ,  true,    toNewArray (toOpType qubitType unitType adjCtlSet) (toInt 0),  [Warning WarningCode.DeprecatedOpCharacteristics; Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket]); 
        ("new (Qubit => Unit) : Adj, Ctl[0]",  true,    toNewArray (toOpType qubitType unitType adjCtlSet) (toInt 0),  [Warning WarningCode.DeprecatedOpCharacteristics; Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket]); 
        
        ("new (Qubit => Unit is Adj, Ctl)[0]",  true,    toNewArray (toOpType qubitType unitType adjCtlSet) (toInt 0),  [Warning WarningCode.DeprecatedOpCharacteristics]); 
        ("new Qubit => Unit is Adj, Ctl[0]"  ,  true,    toNewArray (toOpType qubitType unitType adjCtlSet) (toInt 0),  [Warning WarningCode.DeprecatedOpCharacteristics; Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket]); 
        ("new (Qubit => Unit) is Adj, Ctl[0]",  true,    toNewArray (toOpType qubitType unitType adjCtlSet) (toInt 0),  [Warning WarningCode.DeprecatedOpCharacteristics; Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket]); 

        ("new (Qubit => Unit is (Adj + Ctl))[0]"     ,  true,    toNewArray (toOpType qubitType unitType adjCtlSet) (toInt 0),  []); 
        ("new (Qubit => Unit is ((Adj) + (Ctl)))[0]" ,  true,    toNewArray (toOpType qubitType unitType adjCtlSet) (toInt 0),  []); 
        ("new (Qubit => Unit is (Adj + Ctl _))[0]"   ,  true,    toNewArray (toOpType qubitType unitType adjCtlSet) (toInt 0),  [Error ErrorCode.ExcessContinuation]); 
        ("new (Qubit => Unit is ())[0]"              ,  true,    toNewArray (toOpType qubitType unitType (toCharacteristicsExpr InvalidSetExpr)) (toInt 0), [Error ErrorCode.MissingOperationCharacteristics]); 
        ("new (Qubit => Unit is (Adj + (Ctl + )))[0]",  true,    toNewArray (toOpType qubitType unitType (toCharacteristicsExpr InvalidSetExpr)) (toInt 0), [Error ErrorCode.InvalidOperationCharacteristics]); 

        ("new (Qubit => Unit is MySet)[0]"        ,  true,    toNewArray (toOpType qubitType unitType (toCharacteristicsExpr InvalidSetExpr)) (toInt 0),  [Error ErrorCode.UnknownSetName]); 
        ("new (Qubit => Unit is Adj + MySet)[0]"  ,  true,    toNewArray (toOpType qubitType unitType (toCharacteristicsExpr InvalidSetExpr)) (toInt 0),  [Error ErrorCode.UnknownSetName]); 
        ("new (Qubit => Unit is (Adj + MySet))[0]",  true,    toNewArray (toOpType qubitType unitType (toCharacteristicsExpr InvalidSetExpr)) (toInt 0),  [Error ErrorCode.UnknownSetName]); 

    ]
    |> List.iter testExpr


[<Fact>]
let ``Other expression tests`` () =
    [
        ("x?y|z",               true,    toExpr (CONDITIONAL (toSymbol "x", toSymbol "y", toSymbol "z")), []); 
        ("a[1]",                true,    toExpr (ArrayItem (toSymbol "a", toInt 1)), []);
        ("a[0..2]",             true,    toExpr (ArrayItem (toSymbol "a", RangeLiteral (toInt 0, toInt 2) |> toExpr)), []);
        ("a[]",                 true,    toExpr (ArrayItem (toSymbol "a", InvalidExpr |> toExpr)), [Error ErrorCode.MissingExpression]);
        ("a[0...]",             true,    toExpr (ArrayItem (toSymbol "a", RangeLiteral (toInt 0, toExpr MissingExpr) |> toExpr)), []);
        ("a[0 ... ]",           true,    toExpr (ArrayItem (toSymbol "a", RangeLiteral (toInt 0, toExpr MissingExpr) |> toExpr)), []);
        ("a[0... ]",            true,    toExpr (ArrayItem (toSymbol "a", RangeLiteral (toInt 0, toExpr MissingExpr) |> toExpr)), []);
        ("a[0..2...]",          true,    toExpr (ArrayItem (toSymbol "a", RangeLiteral (RangeLiteral (toInt 0, toInt 2) |> toExpr, toExpr MissingExpr) |> toExpr)), []);
        ("a[0 .. 2 ... ]",      true,    toExpr (ArrayItem (toSymbol "a", RangeLiteral (RangeLiteral (toInt 0, toInt 2) |> toExpr, toExpr MissingExpr) |> toExpr)), []);
        ("a[0..2 ...]",         true,    toExpr (ArrayItem (toSymbol "a", RangeLiteral (RangeLiteral (toInt 0, toInt 2) |> toExpr, toExpr MissingExpr) |> toExpr)), []);
        ("a[...0]",             true,    toExpr (ArrayItem (toSymbol "a", RangeLiteral (toExpr MissingExpr, toInt 0) |> toExpr)), []);
        ("a[ ...0]",            true,    toExpr (ArrayItem (toSymbol "a", RangeLiteral (toExpr MissingExpr, toInt 0) |> toExpr)), []);
        ("a[ ... 0]",           true,    toExpr (ArrayItem (toSymbol "a", RangeLiteral (toExpr MissingExpr, toInt 0) |> toExpr)), []);
        ("a[...2..0]",          true,    toExpr (ArrayItem (toSymbol "a", RangeLiteral (RangeLiteral (toExpr MissingExpr, toInt 2) |> toExpr, toInt 0) |> toExpr)), []);
        ("a[ ...2 .. 0]",       true,    toExpr (ArrayItem (toSymbol "a", RangeLiteral (RangeLiteral (toExpr MissingExpr, toInt 2) |> toExpr, toInt 0) |> toExpr)), []);
        ("a[ ... 2 ..0]",       true,    toExpr (ArrayItem (toSymbol "a", RangeLiteral (RangeLiteral (toExpr MissingExpr, toInt 2) |> toExpr, toInt 0) |> toExpr)), []);
        ("a[...-1...]",         true,    toExpr (ArrayItem (toSymbol "a", RangeLiteral (RangeLiteral (toExpr MissingExpr, NEG (toInt 1) |> toExpr) |> toExpr, toExpr MissingExpr) |> toExpr)), []);
        ("a[ ... -1 ... ]",     true,    toExpr (ArrayItem (toSymbol "a", RangeLiteral (RangeLiteral (toExpr MissingExpr, NEG (toInt 1) |> toExpr) |> toExpr, toExpr MissingExpr) |> toExpr)), []);
        ("a[...]",              true,    toExpr (ArrayItem (toSymbol "a", RangeLiteral (toExpr MissingExpr, toExpr MissingExpr) |> toExpr)), []);
        ("a[ ... ]",            true,    toExpr (ArrayItem (toSymbol "a", RangeLiteral (toExpr MissingExpr, toExpr MissingExpr) |> toExpr)), []);
    ]
    |> List.iter testExpr


[<Fact>]
let ``Operator precendence tests`` () =
    [
        ("1+2*3",               true,    toExpr (ADD (toInt 1, 
                                                      (MUL (toInt 2, 
                                                            toInt 3) |> toExpr))),              []); 
        ("1*2-3",               true,    toExpr (SUB ((MUL (toInt 1, 
                                                            toInt 2) |> toExpr),
                                                      toInt 3)),                                []); 
        ("1/2^3",               true,    toExpr (DIV (toInt 1, 
                                                      (POW (toInt 2, 
                                                            toInt 3) |> toExpr))),              []); 
        ("1/2/3",               true,    toExpr (DIV ((DIV (toInt 1, 
                                                            toInt 2) |> toExpr),
                                                      toInt 3)),                                []); 
        ("1-2-3",               true,    toExpr (SUB ((SUB (toInt 1, 
                                                            toInt 2) |> toExpr),
                                                      toInt 3)),                                []); 
        ("1==2/3",              true,    toExpr (EQ (toInt 1, 
                                                    (DIV (toInt 2,
                                                          toInt 3) |> toExpr))),                []); 
        ("1+2>2*3",             true,    toExpr (GT (ADD (toInt 1, 
                                                          toInt 2) |> toExpr, 
                                                    (MUL (toInt 2,
                                                          toInt 3) |> toExpr))),                []); 
        ("A(5+7)?2^3|B(3)/2",   true,    toExpr (CONDITIONAL (CallLikeExpression (toSymbol "A", 
                                                                                  [ADD(toInt 5, toInt 7) |> toExpr] |> toTuple) |> toExpr,
                                                              POW(toInt 2, toInt 3) |> toExpr,
                                                              DIV(CallLikeExpression (toSymbol "B", [toInt 3] |> toTuple) |> toExpr,
                                                                  toInt 2) |> toExpr)),         []);
    ]
    |> List.iter testExpr
