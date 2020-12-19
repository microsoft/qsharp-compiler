// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.SyntaxTests

open FParsec
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.TextProcessing.ExpressionParsing
open Microsoft.Quantum.QsCompiler.TextProcessing.CodeFragments
open Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxBuilder
open System
open System.Collections.Immutable
open System.Globalization
open TestUtils
open Xunit

let private rawString = getStringContent (manyChars anyChar) |>> fst


[<Fact>]
let ``Reserved patterns`` () =
    [
        ("_mySymbol", true, Some "_mySymbol", [])
        ("mySymbol_", true, Some "mySymbol_", [])
        ("my_symbol", true, Some "my_symbol", [])
        ("my__symbol", true, None, [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("__mySymbol", true, None, [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("mySymbol__", true, None, [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("__my__sym", true, None, [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("my__sym__", true, None, [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("__mysym__", true, None, [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
    ]
    |> List.iter (testOne (symbolNameLike ErrorCode.InvalidIdentifierName))

    [
        ("a.b", true, ([ Some "a" ], Some "b"), [])
        ("_a.b", true, ([ Some "_a" ], Some "b"), [])
        ("a_.b", true, ([ None ], Some "b"), [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("a._b", true, ([ Some "a" ], Some "_b"), [])
        ("a.b_", true, ([ Some "a" ], Some "b_"), [])
        ("_a.b_", true, ([ Some "_a" ], Some "b_"), [])
        ("a_._b", true, ([ None ], Some "_b"), [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("__a.b", true, ([ None ], Some "b"), [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("a__a.b", true, ([ None ], Some "b"), [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("a__.b", true, ([ None ], Some "b"), [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("a.__b", true, ([ Some "a" ], None), [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("a.b__b", true, ([ Some "a" ], None), [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("a.b__", true, ([ Some "a" ], None), [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("__a.b__",
         true,
         ([ None ], None),
         [
             Error ErrorCode.InvalidUseOfUnderscorePattern
             Error ErrorCode.InvalidUseOfUnderscorePattern
         ])
    ]
    |> List.iter (testOne (multiSegmentSymbol ErrorCode.InvalidIdentifierName |>> fst))

    [
        ("a.b", true, Some "a.b", [])
        ("_a.b", true, Some "_a.b", [])
        ("a_.b", true, None, [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("a._b", true, Some "a._b", [])
        ("a.b_", true, None, [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("_a.b_", true, None, [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("a_._b", true, None, [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("__a.b", true, None, [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("a__a.b", true, None, [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("a__.b", true, None, [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("a.__b", true, None, [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("a.b__b", true, None, [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("a.b__", true, None, [ Error ErrorCode.InvalidUseOfUnderscorePattern ])
        ("__a.b__",
         true,
         None,
         [
             Error ErrorCode.InvalidUseOfUnderscorePattern
             Error ErrorCode.InvalidUseOfUnderscorePattern
         ])
    ]
    |> List.iter (testOne (namespaceName |>> fst))


[<Fact>]
let ``String parser tests`` () =
    [
        "\"This is a string\""
        "\"\""
        "$\"Interpolated string\""
        "$\"Interpolated string with {variable}\""
        "$\"Interpolated string with {expression+1}\""
        "$\"Interpolated string with {\"string argument\"}\""
        "$\"Interpolated string with {\"str1\" + \"str2\"}\""
        "$\"Interpolated string with {true ? \"str1\" | \"str2\"}\""
        "$\"Interpolated string with {true ? $\"{1}\" | $\"{2}\"}\""
    ]
    |> List.iter (fun s -> Assert.True(simpleParseString (stringLiteral .>> eof) s, "Failed to parse string " + s))

    // testing whether tabs etc in strings are processed correctly
    let testChar offset char string =
        match parse_string_diags_res rawString string with
        | true, _, Some parsed ->
            Assert.Equal(offset + 1, parsed.Length)
            Assert.Equal(char, parsed.[offset])
        | _ -> Assert.True(false, "failed to parse")

    testChar 0 '\t' "\"\\t\""
    testChar 0 '\r' "\"\\r\""
    testChar 0 '\n' "\"\\n\""
    testChar 0 '\"' "\"\\\"\""
    testChar 0 '\\' "\"\\\\\""
    testChar 3 '\t' "$\"{0}\\t\""
    testChar 3 '\r' "$\"{0}\\r\""
    testChar 3 '\n' "$\"{0}\\n\""
    testChar 3 '\"' "$\"{0}\\\"\""
    testChar 3 '\\' "$\"{0}\\\\\""
    testChar 3 '{' "$\"{0}\\{\""


[<Fact>]
let ``Optional tuple bracket tests`` () =
    let parser = (optTupleBrackets rawString) |>> fst

    [
        ("\"\";", true, "", [ Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket ])
        ("(\"\");", true, "", [])
        ("(\"\" ", true, "", [ Error ErrorCode.MissingRTupleBracket ])
        ("\"\");", true, "", [ Error ErrorCode.MissingLTupleBracket ])
    ]
    |> List.iter (testOne parser)


[<Fact>]
let ``Tuple bracket tests`` () =
    let parser = (tupleBrackets rawString) |>> fst

    [
        ("(\"\")", true, "", [])
        ("(\"\"", false, "", [ Error ErrorCode.MissingRTupleBracket ])
        ("\"\");", false, "", [ Error ErrorCode.MissingLTupleBracket ])
    ]
    |> List.iter (testOne parser)


[<Fact>]
let ``Array bracket tests`` () =
    let parser = (arrayBrackets rawString) |>> fst

    [
        ("[\"\"]", true, "", [])
        ("[\"\"", false, "", [ Error ErrorCode.MissingRArrayBracket ])
        ("\"\"];", false, "", [ Error ErrorCode.MissingLArrayBracket ])
    ]
    |> List.iter (testOne parser)


[<Fact>]
let ``Angle bracket tests`` () =
    let parser = (angleBrackets rawString) |>> fst

    [
        ("<\"\">", true, "", [])
        ("<\"\"", false, "", [ Error ErrorCode.MissingRAngleBracket ])
        ("\"\">;", false, "", [ Error ErrorCode.MissingLAngleBracket ])
    ]
    |> List.iter (testOne parser)


[<Fact>]
let ``Curly bracket tests`` () =
    let parser = (curlyBrackets rawString) |>> fst

    [
        ("{\"\"}", true, "", [])
        ("{\"\"", false, "", [ Error ErrorCode.MissingRCurlyBracket ])
        ("\"\"};", false, "", [ Error ErrorCode.MissingLCurlyBracket ])
    ]
    |> List.iter (testOne parser)


[<Fact>]
let ``Build tuple tests`` () =
    let parser =
        buildTuple rawString (fst >> String.concat ";") ErrorCode.InvalidValueTuple ErrorCode.MissingExpression ""

    [
        ("(\"a\")", true, "a", [])
        ("(\"a\",\"b\")", true, "a;b", [])
        ("()", false, "", [])
        ("(\"a\",)", true, "a", [ Warning WarningCode.ExcessComma ])
        ("(\"a\";)", true, "a", [ Error ErrorCode.ExcessContinuation ])
        ("(\"a\",5)", true, "a;", [ Error ErrorCode.InvalidValueTuple ])
        ("(\"a\",,\"b\")", true, "a;;b", [ Error ErrorCode.MissingExpression ])
        ("(\"a\"", false, "a", [ Error ErrorCode.MissingRCurlyBracket ])
        ("\"a\");", false, "a", [ Error ErrorCode.MissingLCurlyBracket ])
    ]
    |> List.iter (testOne parser)


[<Fact>]
let ``Symbol name tests`` () =
    let parser =
        symbolNameLike ErrorCode.ExpectingUnqualifiedSymbol
        >>= function
        | Some str -> preturn str
        | None -> fail ""

    [
        ("a", true, "a", [])
        ("a1", true, "a1", [])
        ("A", true, "A", [])
        ("if", false, "", [])
        ("IF", true, "IF", [])
        ("a_", true, "a_", [])
        ("_a", true, "_a", [])
        ("_", false, "", [])
        ("__", false, "", [])
        ("функция25", true, "функция25", []) // Russian word 'function' followed by '25'
        ("λ", true, "λ", []) // Greek small letter Lambda
        ("ℵ", true, "ℵ", []) // Hebrew capital letter Aleph
        ("𝑓", false, "", []) // Mathematical Italic Small F - not supported
        ("Q#", true, "Q", []) // 'Q' followed by '#' - only identifier 'Q' is parsed
        ("notЁ", true, "notЁ", []) // operation 'not' followed by Cyrillic 'Ё' - OK for identifier
        ("isЖ", true, "isЖ", []) // reserved word 'is' followed by Cyrillic 'Ж' - OK for identifier
    ]
    |> List.iter (testOne parser)


[<Fact>]
let ``Expression literal tests`` () =
    let intString (n: IFormattable) =
        n.ToString("G", CultureInfo.InvariantCulture)

    let doubleString (n: IFormattable) =
        n.ToString("R", CultureInfo.InvariantCulture)

    // constants that should be handled
    let minInt = Int64.MinValue // -9223372036854775808L
    let maxInt = Int64.MaxValue // 9223372036854775807L
    let minDouble = Double.MinValue // -1.7976931348623157E+308
    let maxDouble = Double.MaxValue // 1.7976931348623157E+308
    Assert.Equal(minInt, -(-minInt))

    // constants that should raise an error
    let absMinIntMinus1 = uint64 (-minInt) + 1UL
    let minIntMinus1Str = absMinIntMinus1 |> intString |> sprintf "-%s"
    let maxIntPlus1 = uint64 (maxInt) + 1UL
    let maxIntPlus2 = uint64 (maxInt) + 2UL
    let doublePrecBound = "1.79769313486232E+308" // what shows up as out of range in C#
    let minusDoublePrecBound = sprintf "-%s" doublePrecBound

    let noExprs = ImmutableArray.Empty

    [
        ("()", true, toExpr UnitValue, [])
        ("1", true, toInt 1, [])
        ("+1", true, toInt 1, [])
        ("-1", true, toExpr (NEG(toInt 1)), [])
        (intString minInt, true, toExpr (NEG(NEG(IntLiteral minInt |> toExpr) |> toExpr)), [])
        (minIntMinus1Str,
         true,
         toExpr (NEG(IntLiteral((int64) absMinIntMinus1) |> toExpr)),
         [ Error ErrorCode.IntOverflow ])
        (intString maxIntPlus1, true, toExpr (NEG(IntLiteral((int64) maxIntPlus1) |> toExpr)), []) // no error, will pop up at runtime
        (intString maxIntPlus2, true, toExpr (IntLiteral((int64) maxIntPlus2)), [ Error ErrorCode.IntOverflow ])
        (doublePrecBound,
         true,
         toExpr (DoubleLiteral System.Double.PositiveInfinity),
         [ Error ErrorCode.DoubleOverflow ])
        (minusDoublePrecBound,
         true,
         toExpr (NEG(DoubleLiteral System.Double.PositiveInfinity |> toExpr)),
         [ Error ErrorCode.DoubleOverflow ])
        (intString minInt, true, toExpr (NEG(NEG(IntLiteral minInt |> toExpr) |> toExpr)), [])
        (intString maxInt, true, toExpr (IntLiteral maxInt), [])
        (doubleString minDouble, true, toExpr (NEG(DoubleLiteral -minDouble |> toExpr)), [])
        (doubleString maxDouble, true, toExpr (DoubleLiteral maxDouble), [])
        ("0x1", true, toInt 1, [])
        ("+0x1", true, toInt 1, [])
        ("1L", true, toBigInt "1", [])
        ("+1L", true, toBigInt "1", [])
        ("-1L", true, toExpr (NEG(toBigInt "1")), [])
        ("10000000000000000L", true, toBigInt "10000000000000000", [])
        ("0b111L", true, toBigInt "7", [])
        ("0b1101L", true, toBigInt "13", [])
        ("0b1100101011111110L", true, toBigInt "51966", [])
        ("0o1L", true, toBigInt "1", [])
        ("0o105L", true, toBigInt "69", [])
        ("0o12345L", true, toBigInt "5349", [])
        ("0xfL", true, toBigInt "15", [])
        ("0xffL", true, toBigInt "255", [])
        ("1l", true, toBigInt "1", [])
        ("+1l", true, toBigInt "1", [])
        ("-1l", true, toExpr (NEG(toBigInt "1")), [])
        ("10000000000000000l", true, toBigInt "10000000000000000", [])
        ("0xfl", true, toBigInt "15", [])
        ("+0xfl", true, toBigInt "15", [])
        ("0xffl", true, toBigInt "255", [])
        ("+0xffl", true, toBigInt "255", [])
        ("0o1", true, toInt 1, [])
        ("+0o1", true, toInt 1, [])
        ("-0o1", true, toExpr (NEG(toInt 1)), [])
        ("0b1", true, toInt 1, [])
        ("0b100", true, toInt 4, [])
        ("+0b100", true, toInt 4, [])
        ("-0b100", true, toExpr (NEG(toInt 4)), [])
        ("0o1", true, toInt 1, [])
        ("0o100", true, toInt 64, [])
        ("+0o100", true, toInt 64, [])
        ("-0o100", true, toExpr (NEG(toInt 64)), [])
        (".1e-1", true, toExpr (DoubleLiteral 0.01), [])
        (".1", true, toExpr (DoubleLiteral 0.1), [])
        ("1.0", true, toExpr (DoubleLiteral 1.0), [])
        ("1.", true, toExpr (DoubleLiteral 1.0), [])
        ("+1.0", true, toExpr (DoubleLiteral 1.0), [])
        ("-1.0", true, toExpr (NEG(toExpr (DoubleLiteral 1.0))), [])
        ("-1.0e2", true, toExpr (NEG(toExpr (DoubleLiteral 100.0))), [])
        ("-1.0e-2", true, toExpr (NEG(toExpr (DoubleLiteral 0.01))), [])
        ("\"\"", true, toExpr (StringLiteral("", noExprs)), [])
        ("\"hello\"", true, toExpr (StringLiteral("hello", noExprs)), [])
        ("\"hello\\\\\"", true, toExpr (StringLiteral("hello\\", noExprs)), [])
        ("\"\\\"hello\\\"\"", true, toExpr (StringLiteral("\"hello\"", noExprs)), [])
        ("\"hello\\n\"", true, toExpr (StringLiteral("hello\n", noExprs)), [])
        ("\"hello\\r\\n\"", true, toExpr (StringLiteral("hello\r\n", noExprs)), [])
        ("\"hello\\t\"", true, toExpr (StringLiteral("hello\t", noExprs)), [])
        ("One", true, toExpr (ResultLiteral One), [])
        ("Zero", true, toExpr (ResultLiteral Zero), [])
        ("PauliI", true, toExpr (PauliLiteral PauliI), [])
        ("PauliX", true, toExpr (PauliLiteral PauliX), [])
        ("PauliY", true, toExpr (PauliLiteral PauliY), [])
        ("PauliZ", true, toExpr (PauliLiteral PauliZ), [])
        ("true", true, toExpr (BoolLiteral true), [])
        ("false", true, toExpr (BoolLiteral false), [])
    ]
    |> List.iter testExpr


[<Fact>]
let ``Simple arithmetic expression tests`` () =
    [
        ("-  1", true, toExpr (NEG(toExpr (IntLiteral 1L))), [])
        ("~~~1", true, toExpr (BNOT(toExpr (IntLiteral 1L))), [])
        ("1+1", true, toExpr (ADD(toInt 1, toInt 1)), [])
        ("1L+1L", true, toExpr (ADD(toBigInt "1", toBigInt "1")), [])
        ("1-1", true, toExpr (SUB(toInt 1, toInt 1)), [])
        ("1*1", true, toExpr (MUL(toInt 1, toInt 1)), [])
        ("1/1", true, toExpr (DIV(toInt 1, toInt 1)), [])
        ("1%1", true, toExpr (MOD(toInt 1, toInt 1)), [])
        ("1^1", true, toExpr (POW(toInt 1, toInt 1)), [])
        ("1|||1", true, toExpr (BOR(toInt 1, toInt 1)), [])
        ("1&&&1", true, toExpr (BAND(toInt 1, toInt 1)), [])
        ("1^^^1", true, toExpr (BXOR(toInt 1, toInt 1)), [])
        ("1>>>1", true, toExpr (RSHIFT(toInt 1, toInt 1)), [])
        ("1<<<1", true, toExpr (LSHIFT(toInt 1, toInt 1)), [])
    ]
    |> List.iter testExpr


[<Fact>]
let ``Simple Boolean expression tests`` () =
    [
        ("true && true",
         true,
         toExpr (AND(BoolLiteral true |> toExpr, BoolLiteral true |> toExpr)),
         [ Warning WarningCode.DeprecatedANDoperator ])
        ("true || true",
         true,
         toExpr (OR(BoolLiteral true |> toExpr, BoolLiteral true |> toExpr)),
         [ Warning WarningCode.DeprecatedORoperator ])
        ("!true", true, toExpr (NOT(BoolLiteral true |> toExpr)), [ Warning WarningCode.DeprecatedNOToperator ])
        ("true and true", true, toExpr (AND(BoolLiteral true |> toExpr, BoolLiteral true |> toExpr)), [])
        ("true or true", true, toExpr (OR(BoolLiteral true |> toExpr, BoolLiteral true |> toExpr)), [])
        ("not true", true, toExpr (NOT(BoolLiteral true |> toExpr)), [])
    ]
    |> List.iter testExpr


[<Fact>]
let ``Simple comparison expression tests`` () =
    [
        ("1<2", true, toExpr (LT(toInt 1, toInt 2)), [])
        ("1<=2", true, toExpr (LTE(toInt 1, toInt 2)), [])
        ("1>2", true, toExpr (GT(toInt 1, toInt 2)), [])
        ("1>=2", true, toExpr (GTE(toInt 1, toInt 2)), [])
        ("1==2", true, toExpr (EQ(toInt 1, toInt 2)), [])
        ("1!=2", true, toExpr (NEQ(toInt 1, toInt 2)), [])
        ("1<2 or 1>2", true, toExpr (OR(LT(toInt 1, toInt 2) |> toExpr, GT(toInt 1, toInt 2) |> toExpr)), [])
    ]
    |> List.iter testExpr


[<Fact>]
let ``Identifier tests`` () =
    [
        ("x", true, toIdentifier "x", [])
        ("a.b.c", true, toExpr (Identifier({ Symbol = QualifiedSymbol("a.b", "c"); Range = Null }, Null)), [])
    ]
    |> List.iter testExpr


[<Fact>]
let ``Complex literal tests`` () =
    [
        ("[1,2,3]", true, toArray [ toInt 1; toInt 2; toInt 3 ], [])
        ("[1,x,3]",
         true,
         toArray [ toInt 1
                   toIdentifier "x"
                   toInt 3 ],
         [])
        ("(1,2,3)", true, toTuple [ toInt 1; toInt 2; toInt 3 ], [])
        ("(x,2,3)",
         true,
         toTuple [ toIdentifier "x"
                   toInt 2
                   toInt 3 ],
         [])
        ("1..2", true, toExpr (RangeLiteral(toInt 1, toInt 2)), [])
        ("1..2..3", true, toExpr (RangeLiteral(RangeLiteral(toInt 1, toInt 2) |> toExpr, toInt 3)), [])
        ("1..2..x", true, toExpr (RangeLiteral(RangeLiteral(toInt 1, toInt 2) |> toExpr, toIdentifier "x")), [])
        ("1..x..3", true, toExpr (RangeLiteral(RangeLiteral(toInt 1, toIdentifier "x") |> toExpr, toInt 3)), [])
        ("x..2..3", true, toExpr (RangeLiteral(RangeLiteral(toIdentifier "x", toInt 2) |> toExpr, toInt 3)), [])
        ("1..x", true, toExpr (RangeLiteral(toInt 1, toIdentifier "x")), [])
        ("x..2", true, toExpr (RangeLiteral(toIdentifier "x", toInt 2)), [])
        ("x..y", true, toExpr (RangeLiteral(toIdentifier "x", toIdentifier "y")), [])
    ]
    |> List.iter testExpr


[<Fact>]
let ``Call tests`` () =
    [
        "x()", true, CallLikeExpression(toIdentifier "x", toExpr UnitValue) |> toExpr, []
        "x(1,2)", true, CallLikeExpression(toIdentifier "x", toTuple [ toInt 1; toInt 2 ]) |> toExpr, []
        "Adjoint x()",
        true,
        CallLikeExpression(toIdentifier "x" |> AdjointApplication |> toExpr, toExpr UnitValue) |> toExpr,
        []
        "Controlled x()",
        true,
        CallLikeExpression(toIdentifier "x" |> ControlledApplication |> toExpr, toExpr UnitValue) |> toExpr,
        []
        "f(1)(2)",
        true,
        (CallLikeExpression(toIdentifier "f", toTuple [ toInt 1 ]) |> toExpr, toTuple [ toInt 2 ])
        |> CallLikeExpression
        |> toExpr,
        []
        "f(1)(2)(3)",
        true,
        ((CallLikeExpression(toIdentifier "f", toTuple [ toInt 1 ]) |> toExpr, toTuple [ toInt 2 ])
         |> CallLikeExpression
         |> toExpr,
         toTuple [ toInt 3 ])
        |> CallLikeExpression
        |> toExpr,
        []
        "f(1)(2)[3]",
        true,
        (CallLikeExpression(toIdentifier "f", toTuple [ toInt 1 ]) |> toExpr, toTuple [ toInt 2 ])
        |> CallLikeExpression
        |> toExpr,
        []
        "(f(1)(2))[3]",
        true,
        ([
            (CallLikeExpression(toIdentifier "f", toTuple [ toInt 1 ]) |> toExpr, toTuple [ toInt 2 ])
            |> CallLikeExpression
            |> toExpr
         ]
         |> toTuple,
         toInt 3)
        |> ArrayItem
        |> toExpr,
        []
        "(f(1)(2))[3](4)",
        true,
        ([
            (CallLikeExpression(toIdentifier "f", toTuple [ toInt 1 ]) |> toExpr, toTuple [ toInt 2 ])
            |> CallLikeExpression
            |> toExpr
         ]
         |> toTuple,
         toInt 3)
        |> ArrayItem
        |> toExpr
        |> (fun left -> CallLikeExpression(left, toTuple [ toInt 4 ]) |> toExpr),
        []
        "f(1)(2)::X",
        true,
        (CallLikeExpression(toIdentifier "f", toTuple [ toInt 1 ]) |> toExpr, toTuple [ toInt 2 ])
        |> CallLikeExpression
        |> toExpr,
        []
        "(f(1)(2))::X",
        true,
        ([
            (CallLikeExpression(toIdentifier "f", toTuple [ toInt 1 ]) |> toExpr, toTuple [ toInt 2 ])
            |> CallLikeExpression
            |> toExpr
         ]
         |> toTuple,
         toSymbol "X")
        |> NamedItem
        |> toExpr,
        []
        "(f(1)(2))::X(4)",
        true,
        ([
            (CallLikeExpression(toIdentifier "f", toTuple [ toInt 1 ]) |> toExpr, toTuple [ toInt 2 ])
            |> CallLikeExpression
            |> toExpr
         ]
         |> toTuple,
         toSymbol "X")
        |> NamedItem
        |> toExpr
        |> (fun left -> CallLikeExpression(left, toTuple [ toInt 4 ]) |> toExpr),
        []
        "(x(_,1))(2)",
        true,
        (toTuple [ CallLikeExpression(toIdentifier "x", toTuple [ toExpr MissingExpr; toInt 1 ]) |> toExpr ],
         toTuple [ toInt 2 ])
        |> CallLikeExpression
        |> toExpr,
        []
        "x(_,1)(2)",
        true,
        (CallLikeExpression(toIdentifier "x", toTuple [ toExpr MissingExpr; toInt 1 ]) |> toExpr, toTuple [ toInt 2 ])
        |> CallLikeExpression
        |> toExpr,
        []
        "(x(_,1))(1,2)",
        true,
        (toTuple [ CallLikeExpression(toIdentifier "x", toTuple [ toExpr MissingExpr; toInt 1 ]) |> toExpr ],
         toTuple [ toInt 1; toInt 2 ])
        |> CallLikeExpression
        |> toExpr,
        []
        "x(_,1)(1,2)",
        true,
        (CallLikeExpression(toIdentifier "x", toTuple [ toExpr MissingExpr; toInt 1 ]) |> toExpr,
         toTuple [ toInt 1; toInt 2 ])
        |> CallLikeExpression
        |> toExpr,
        []
        "(x(1,(2, _)))(2)",
        true,
        ([
            CallLikeExpression
                (toIdentifier "x",
                 toTuple [ toInt 1
                           toTuple [ toInt 2; toExpr MissingExpr ] ])
            |> toExpr
         ]
         |> toTuple,
         toTuple [ toInt 2 ])
        |> CallLikeExpression
        |> toExpr,
        []
        "x(1,(2, _))(2)",
        true,
        (CallLikeExpression
            (toIdentifier "x",
             toTuple [ toInt 1
                       toTuple [ toInt 2; toExpr MissingExpr ] ])
         |> toExpr,
         toTuple [ toInt 2 ])
        |> CallLikeExpression
        |> toExpr,
        []
        "(x(_,(2, _)))(1,2)",
        true,
        ([
            (toIdentifier "x",
             toTuple [ toExpr MissingExpr
                       toTuple [ toInt 2; toExpr MissingExpr ] ])
            |> CallLikeExpression
            |> toExpr
         ]
         |> toTuple,
         toTuple [ toInt 1; toInt 2 ])
        |> CallLikeExpression
        |> toExpr,
        []
        "x(_,(2, _))(1,2)",
        true,
        ((toIdentifier "x",
          toTuple [ toExpr MissingExpr
                    toTuple [ toInt 2; toExpr MissingExpr ] ])
         |> CallLikeExpression
         |> toExpr,
         toTuple [ toInt 1; toInt 2 ])
        |> CallLikeExpression
        |> toExpr,
        []
    ]
    |> List.iter testExpr


[<Fact>]
let ``Modifier tests`` () = // modifiers can only be applied to identifiers, arity-1 tuples, and array item expressions
    [
        // modifiers on identifiers:
        ("ab!", true, toExpr (UnwrapApplication(toIdentifier "ab")), [])
        ("!ab!",
         true,
         toExpr (NOT(UnwrapApplication(toIdentifier "ab") |> toExpr)),
         [ Warning WarningCode.DeprecatedNOToperator ])
        ("ab!!", true, toExpr (UnwrapApplication(UnwrapApplication(toIdentifier "ab") |> toExpr)), [])
        ("Adjoint x", true, toExpr (AdjointApplication(toIdentifier "x")), [])
        ("Controlled Adjoint x",
         true,
         toExpr (ControlledApplication(AdjointApplication(toIdentifier "x") |> toExpr)),
         [])
        ("ab! ()",
         true,
         toExpr (CallLikeExpression(UnwrapApplication(toIdentifier "ab") |> toExpr, UnitValue |> toExpr)),
         [])
        ("ab!! ()",
         true,
         toExpr
             (CallLikeExpression
                 (UnwrapApplication(UnwrapApplication(toIdentifier "ab") |> toExpr) |> toExpr, UnitValue |> toExpr)),
         [])
        ("Adjoint x()",
         true,
         toExpr (CallLikeExpression(AdjointApplication(toIdentifier "x") |> toExpr, UnitValue |> toExpr)),
         [])
        ("Adjoint Controlled x()",
         true,
         toExpr
             (CallLikeExpression
                 (AdjointApplication(ControlledApplication(toIdentifier "x") |> toExpr) |> toExpr, UnitValue |> toExpr)),
         [])
        ("Adjoint x! ()",
         true,
         toExpr
             (CallLikeExpression
                 (AdjointApplication(UnwrapApplication(toIdentifier "x") |> toExpr) |> toExpr, UnitValue |> toExpr)),
         [])
        // modifiers on arity-1 tuples:
        ("(udt(x))!",
         true,
         toExpr
             (UnwrapApplication
                 (toTuple [ (CallLikeExpression(toIdentifier "udt", toTuple [ toIdentifier "x" ]) |> toExpr) ])),
         [])
        ("(udt(x))! ()",
         true,
         toExpr
             (CallLikeExpression
                 ((UnwrapApplication
                     (toTuple [ CallLikeExpression(toIdentifier "udt", toTuple [ toIdentifier "x" ]) |> toExpr ]))
                  |> toExpr,
                  UnitValue |> toExpr)),
         [])
        ("Controlled (udt(x))! ()",
         true,
         toExpr
             (CallLikeExpression
                 (ControlledApplication
                     ((UnwrapApplication
                         (toTuple [ CallLikeExpression(toIdentifier "udt", toTuple [ toIdentifier "x" ]) |> toExpr ]))
                      |> toExpr)
                  |> toExpr,
                  UnitValue |> toExpr)),
         [])
        ("(Controlled (udt(x))!)()",
         true,
         toExpr
             (CallLikeExpression
                 (toTuple [ ControlledApplication
                                ((UnwrapApplication
                                    (toTuple [ CallLikeExpression(toIdentifier "udt", toTuple [ toIdentifier "x" ])
                                               |> toExpr ]))
                                 |> toExpr)
                            |> toExpr ],
                  UnitValue |> toExpr)),
         [])
        // modifiers on array item expressions
        ("x[i]!", true, toExpr (UnwrapApplication(ArrayItem(toIdentifier "x", toIdentifier "i") |> toExpr)), [])
        ("Adjoint x[i]", true, toExpr (AdjointApplication(ArrayItem(toIdentifier "x", toIdentifier "i") |> toExpr)), [])
        ("x[i]! ()",
         true,
         toExpr
             (CallLikeExpression
                 (UnwrapApplication(ArrayItem(toIdentifier "x", toIdentifier "i") |> toExpr) |> toExpr,
                  UnitValue |> toExpr)),
         [])
        ("Adjoint x[i] ()",
         true,
         toExpr
             (CallLikeExpression
                 (AdjointApplication(ArrayItem(toIdentifier "x", toIdentifier "i") |> toExpr) |> toExpr,
                  UnitValue |> toExpr)),
         [])
        ("Controlled x[i]! ()",
         true,
         toExpr
             (CallLikeExpression
                 (ControlledApplication
                     (UnwrapApplication(ArrayItem(toIdentifier "x", toIdentifier "i") |> toExpr) |> toExpr)
                  |> toExpr,
                  UnitValue |> toExpr)),
         [])
        // modifiers combined with named and array item access
        ("x[i]::Re",
         true,
         toExpr (NamedItem(ArrayItem(toIdentifier "x", toIdentifier "i") |> toExpr, toSymbol "Re")),
         [])
        ("x[i]!::Re",
         true,
         toExpr
             (NamedItem
                 (UnwrapApplication(ArrayItem(toIdentifier "x", toIdentifier "i") |> toExpr) |> toExpr, toSymbol "Re")),
         [])
        ("x[i]::Re!",
         true,
         toExpr
             (UnwrapApplication
                 (NamedItem(ArrayItem(toIdentifier "x", toIdentifier "i") |> toExpr, toSymbol "Re") |> toExpr)),
         [])
        ("x[i]![j]",
         true,
         toExpr
             (ArrayItem
                 (UnwrapApplication(ArrayItem(toIdentifier "x", toIdentifier "i") |> toExpr) |> toExpr, toIdentifier "j")),
         [])
        ("x::Re![j]",
         true,
         toExpr
             (ArrayItem
                 (UnwrapApplication(NamedItem(toIdentifier "x", toSymbol "Re") |> toExpr) |> toExpr, toIdentifier "j")),
         [])
        ("x::Re!::Im",
         true,
         toExpr
             (NamedItem
                 (UnwrapApplication(NamedItem(toIdentifier "x", toSymbol "Re") |> toExpr) |> toExpr, toSymbol "Im")),
         [])
        ("x::Re!!::Im",
         true,
         toExpr
             (NamedItem
                 (UnwrapApplication(UnwrapApplication(NamedItem(toIdentifier "x", toSymbol "Re") |> toExpr) |> toExpr)
                  |> toExpr,
                  toSymbol "Im")),
         [])
    ]
    |> List.iter testExpr


[<Fact>]
let ``Function type tests`` () =
    [
        "(Int -> Int)", toTupleType [ Function(toType Int, toType Int) |> toType ], []

        "(Int -> (Int -> Int))",
        toTupleType [ Function(toType Int, toTupleType [ Function(toType Int, toType Int) |> toType ]) |> toType ],
        []

        "((Int -> Int) -> Int)",
        toTupleType [ Function(toTupleType [ Function(toType Int, toType Int) |> toType ], toType Int) |> toType ],
        []

        "Int -> Int", Function(toType Int, toType Int) |> toType, []
        "Int -> Int -> Int", Function(toType Int, Function(toType Int, toType Int) |> toType) |> toType, []

        "Int -> (Int -> Int)",
        Function(toType Int, toTupleType [ Function(toType Int, toType Int) |> toType ]) |> toType,
        []

        "(Int -> Int) -> Int",
        Function(toTupleType [ Function(toType Int, toType Int) |> toType ], toType Int) |> toType,
        []

        "Unit -> 'a", Function(unitType, TypeParameter(toSymbol "a") |> toType) |> toType, []
        "'a -> Unit", Function(TypeParameter(toSymbol "a") |> toType, unitType) |> toType, []
        "'a -> 'a", Function(TypeParameter(toSymbol "a") |> toType, TypeParameter(toSymbol "a") |> toType) |> toType, []

        "(Int -> Int, Int)",
        toTupleType [ Function(toType Int, toType Int) |> toType
                      toType Int ],
        []

        "(Int, Int -> Int)",
        toTupleType [ toType Int
                      Function(toType Int, toType Int) |> toType ],
        []

        "('T => Unit)[] -> Unit",
        Function
            (ArrayType(toTupleType [ toOpType (TypeParameter(toSymbol "T") |> toType) unitType emptySet ])
             |> toType,
             unitType)
        |> toType,
        []

        "('T => Unit is Adj)[] -> Unit",
        Function
            (ArrayType(toTupleType [ toOpType (TypeParameter(toSymbol "T") |> toType) unitType adjSet ])
             |> toType,
             unitType)
        |> toType,
        []

        "(('T => Unit)[] -> Unit)",
        toTupleType [ Function
                          (ArrayType(toTupleType [ toOpType (TypeParameter(toSymbol "T") |> toType) unitType emptySet ])
                           |> toType,
                           unitType)
                      |> toType ],
        []
    ]
    |> List.iter testType

    [
        "new (Int -> Int)[0]",
        true,
        toNewArray (toTupleType [ Function(toType Int, toType Int) |> toType ]) (toInt 0),
        []

        "new Int -> Int[0]", true, toNewArray (Function(toType Int, toType Int) |> toType) (toInt 0), []
    ]
    |> List.iter testExpr


[<Fact>]
let ``Operation type tests`` () =
    [
        "(Qubit => Unit)", toTupleType [ toOpType qubitType unitType emptySet ], []

        "(Qubit => (Qubit => Unit))",
        toTupleType [ toOpType qubitType (toTupleType [ toOpType qubitType unitType emptySet ]) emptySet ],
        []

        "((Qubit => Unit) => Unit)",
        toTupleType [ toOpType (toTupleType [ toOpType qubitType unitType emptySet ]) unitType emptySet ],
        []

        "Qubit => Unit", toOpType qubitType unitType emptySet, []
        "Qubit => Qubit => Unit", toOpType qubitType (toOpType qubitType unitType emptySet) emptySet, []

        "Qubit => (Qubit => Unit)",
        toOpType qubitType (toTupleType [ toOpType qubitType unitType emptySet ]) emptySet,
        []

        "(Qubit => Unit) => Unit", toOpType (toTupleType [ toOpType qubitType unitType emptySet ]) unitType emptySet, []
        "(Qubit => Unit is Adj)", toTupleType [ toOpType qubitType unitType adjSet ], []
        "(Qubit => Unit) is Adj", toTupleType [ toOpType qubitType unitType emptySet ], []

        "((Qubit => Unit) is Adj)",
        toTupleType [ toTupleType [ toOpType qubitType unitType emptySet ] ],
        [ Error ErrorCode.ExcessContinuation ]

        "(Qubit => (Qubit => Unit is Adj))",
        toTupleType [ toOpType qubitType (toTupleType [ toOpType qubitType unitType adjSet ]) emptySet ],
        []

        "(Qubit => (Qubit => Unit) is Adj)",
        toTupleType [ toOpType qubitType (toTupleType [ toOpType qubitType unitType emptySet ]) adjSet ],
        []

        "((Qubit => Unit) => Unit is Adj)",
        toTupleType [ toOpType (toTupleType [ toOpType qubitType unitType emptySet ]) unitType adjSet ],
        []

        "Qubit => Unit is Adj", toOpType qubitType unitType adjSet, []
        "Qubit => Qubit => Unit is Adj", toOpType qubitType (toOpType qubitType unitType adjSet) emptySet, []

        "Qubit => (Qubit => Unit) is Adj",
        toOpType qubitType (toTupleType [ toOpType qubitType unitType emptySet ]) adjSet,
        []

        "(Qubit => Unit) => Unit is Adj",
        toOpType (toTupleType [ toOpType qubitType unitType emptySet ]) unitType adjSet,
        []

        "Unit => 'a", toOpType unitType (TypeParameter(toSymbol "a") |> toType) emptySet, []
        "'a => Unit", toOpType (TypeParameter(toSymbol "a") |> toType) unitType emptySet, []

        "'a => 'a",
        toOpType (TypeParameter(toSymbol "a") |> toType) (TypeParameter(toSymbol "a") |> toType) emptySet,
        []

        "(Int => Int, Int)",
        toTupleType [ toOpType (toType Int) (toType Int) emptySet
                      toType Int ],
        []

        "(Int, Int => Int)",
        toTupleType [ toType Int
                      toOpType (toType Int) (toType Int) emptySet ],
        []

        "(Qubit => Unit is Adj)[]", ArrayType(toTupleType [ toOpType qubitType unitType adjSet ]) |> toType, []

        "Qubit => Unit is Adj[]",
        ArrayType(toOpType qubitType unitType adjSet) |> toType,
        [ Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket ]

        "(Qubit => Unit : Adjoint, Controlled)",
        toTupleType [ toOpType qubitType unitType emptySet
                      toType InvalidType ],
        [ Error ErrorCode.ExcessContinuation; Error ErrorCode.InvalidType ]

        "Qubit => Unit : Adjoint, Controlled", toOpType qubitType unitType emptySet, []
    ]
    |> List.iter testType

    [
        "new (Qubit => Unit is Adj)[0]",
        true,
        toNewArray (toTupleType [ toOpType qubitType unitType adjSet ]) (toInt 0),
        []

        "new Qubit => Unit is Adj[0]", true, toNewArray (toOpType qubitType unitType adjSet) (toInt 0), []
        "new (Qubit => Unit) is Adj[0]", true, toExpr InvalidExpr, [ Error ErrorCode.InvalidConstructorExpression ]

        "new (Qubit => Unit : Adjoint)[0]",
        true,
        toNewArray (toTupleType [ toOpType qubitType unitType emptySet ]) (toInt 0),
        [ Error ErrorCode.ExcessContinuation ]

        "new Qubit => Unit : Adjoint[0]", true, toExpr InvalidExpr, [ Error ErrorCode.InvalidConstructorExpression ]
        "new (Qubit => Unit) : Adjoint[0]", true, toExpr InvalidExpr, [ Error ErrorCode.InvalidConstructorExpression ]

        "new (Qubit => Unit : Adj)[0]",
        true,
        toNewArray (toTupleType [ toOpType qubitType unitType emptySet ]) (toInt 0),
        [ Error ErrorCode.ExcessContinuation ]

        "new Qubit => Unit : Adj[0]", true, toExpr InvalidExpr, [ Error ErrorCode.InvalidConstructorExpression ]
        "new (Qubit => Unit) : Adj[0]", true, toExpr InvalidExpr, [ Error ErrorCode.InvalidConstructorExpression ]

        "new (Qubit => Unit is Adj + Ctl)[0]",
        true,
        toNewArray (toTupleType [ toOpType qubitType unitType adjCtlSet ]) (toInt 0),
        []

        "new Qubit => Unit is Adj + Ctl[0]", true, toNewArray (toOpType qubitType unitType adjCtlSet) (toInt 0), []
        "new (Qubit => Unit) is Adj + Ctl[0]",
        true,
        toExpr InvalidExpr,
        [ Error ErrorCode.InvalidConstructorExpression ]

        "new (Qubit => Unit : Adjoint, Controlled)[0]",
        true,
        toNewArray (toTupleType [ toOpType qubitType unitType emptySet; toType InvalidType ]) (toInt 0),
        [ Error ErrorCode.ExcessContinuation; Error ErrorCode.InvalidType ]

        "new Qubit => Unit : Adjoint, Controlled[0]",
        true,
        toExpr InvalidExpr,
        [ Error ErrorCode.InvalidConstructorExpression ]

        "new (Qubit => Unit) : Adjoint, Controlled[0]",
        true,
        toExpr InvalidExpr,
        [ Error ErrorCode.InvalidConstructorExpression ]

        "new (Qubit => Unit : Adj + Ctl)[0]",
        true,
        toNewArray (toTupleType [ toOpType qubitType unitType emptySet ]) (toInt 0),
        [ Error ErrorCode.ExcessContinuation ]

        "new Qubit => Unit : Adj + Ctl[0]", true, toExpr InvalidExpr, [ Error ErrorCode.InvalidConstructorExpression ]
        "new (Qubit => Unit) : Adj + Ctl[0]", true, toExpr InvalidExpr, [ Error ErrorCode.InvalidConstructorExpression ]

        "new (Qubit => Unit : Adj, Ctl)[0]",
        true,
        toNewArray (toTupleType [ toOpType qubitType unitType emptySet; toType InvalidType ]) (toInt 0),
        [ Error ErrorCode.ExcessContinuation; Error ErrorCode.InvalidType ]

        "new Qubit => Unit : Adj, Ctl[0]", true, toExpr InvalidExpr, [ Error ErrorCode.InvalidConstructorExpression ]
        "new (Qubit => Unit) : Adj, Ctl[0]", true, toExpr InvalidExpr, [ Error ErrorCode.InvalidConstructorExpression ]

        "new (Qubit => Unit is Adj, Ctl)[0]",
        true,
        toNewArray (toTupleType [ toOpType qubitType unitType adjCtlSet ]) (toInt 0),
        [ Warning WarningCode.DeprecatedOpCharacteristics ]

        "new Qubit => Unit is Adj, Ctl[0]",
        true,
        toNewArray (toOpType qubitType unitType adjCtlSet) (toInt 0),
        [ Warning WarningCode.DeprecatedOpCharacteristics ]

        "new (Qubit => Unit) is Adj, Ctl[0]", true, toExpr InvalidExpr, [ Error ErrorCode.InvalidConstructorExpression ]

        "new (Qubit => Unit is (Adj + Ctl))[0]",
        true,
        toNewArray (toTupleType [ toOpType qubitType unitType adjCtlSet ]) (toInt 0),
        []

        "new (Qubit => Unit is ((Adj) + (Ctl)))[0]",
        true,
        toNewArray (toTupleType [ toOpType qubitType unitType adjCtlSet ]) (toInt 0),
        []

        "new (Qubit => Unit is (Adj + Ctl _))[0]",
        true,
        toNewArray (toTupleType [ toOpType qubitType unitType adjCtlSet ]) (toInt 0),
        [ Error ErrorCode.ExcessContinuation ]

        "new (Qubit => Unit is ())[0]",
        true,
        toNewArray (toTupleType [ toOpType qubitType unitType (toCharacteristicsExpr InvalidSetExpr) ]) (toInt 0),
        [ Error ErrorCode.MissingOperationCharacteristics ]

        "new (Qubit => Unit is (Adj + (Ctl + )))[0]",
        true,
        toNewArray (toTupleType [ toOpType qubitType unitType (toCharacteristicsExpr InvalidSetExpr) ]) (toInt 0),
        [ Error ErrorCode.InvalidOperationCharacteristics ]

        "new (Qubit => Unit is MySet)[0]",
        true,
        toNewArray (toTupleType [ toOpType qubitType unitType (toCharacteristicsExpr InvalidSetExpr) ]) (toInt 0),
        [ Error ErrorCode.UnknownSetName ]

        "new (Qubit => Unit is Adj + MySet)[0]",
        true,
        toNewArray (toTupleType [ toOpType qubitType unitType (toCharacteristicsExpr InvalidSetExpr) ]) (toInt 0),
        [ Error ErrorCode.UnknownSetName ]

        "new (Qubit => Unit is (Adj + MySet))[0]",
        true,
        toNewArray (toTupleType [ toOpType qubitType unitType (toCharacteristicsExpr InvalidSetExpr) ]) (toInt 0),
        [ Error ErrorCode.UnknownSetName ]
    ]
    |> List.iter testExpr


[<Fact>]
let ``Other expression tests`` () =
    [
        ("x?y|z", true, toExpr (CONDITIONAL(toIdentifier "x", toIdentifier "y", toIdentifier "z")), [])
        ("a[1]", true, toExpr (ArrayItem(toIdentifier "a", toInt 1)), [])
        ("a[0..2]", true, toExpr (ArrayItem(toIdentifier "a", RangeLiteral(toInt 0, toInt 2) |> toExpr)), [])
        ("a[]", true, toExpr (ArrayItem(toIdentifier "a", InvalidExpr |> toExpr)), [ Error ErrorCode.MissingExpression ])
        ("a[0...]", true, toExpr (ArrayItem(toIdentifier "a", RangeLiteral(toInt 0, toExpr MissingExpr) |> toExpr)), [])
        ("a[0 ... ]",
         true,
         toExpr (ArrayItem(toIdentifier "a", RangeLiteral(toInt 0, toExpr MissingExpr) |> toExpr)),
         [])
        ("a[0... ]", true, toExpr (ArrayItem(toIdentifier "a", RangeLiteral(toInt 0, toExpr MissingExpr) |> toExpr)), [])
        ("a[0..2...]",
         true,
         toExpr
             (ArrayItem
                 (toIdentifier "a", RangeLiteral(RangeLiteral(toInt 0, toInt 2) |> toExpr, toExpr MissingExpr) |> toExpr)),
         [])
        ("a[0 .. 2 ... ]",
         true,
         toExpr
             (ArrayItem
                 (toIdentifier "a", RangeLiteral(RangeLiteral(toInt 0, toInt 2) |> toExpr, toExpr MissingExpr) |> toExpr)),
         [])
        ("a[0..2 ...]",
         true,
         toExpr
             (ArrayItem
                 (toIdentifier "a", RangeLiteral(RangeLiteral(toInt 0, toInt 2) |> toExpr, toExpr MissingExpr) |> toExpr)),
         [])
        ("a[...0]", true, toExpr (ArrayItem(toIdentifier "a", RangeLiteral(toExpr MissingExpr, toInt 0) |> toExpr)), [])
        ("a[ ...0]", true, toExpr (ArrayItem(toIdentifier "a", RangeLiteral(toExpr MissingExpr, toInt 0) |> toExpr)), [])
        ("a[ ... 0]",
         true,
         toExpr (ArrayItem(toIdentifier "a", RangeLiteral(toExpr MissingExpr, toInt 0) |> toExpr)),
         [])
        ("a[...2..0]",
         true,
         toExpr
             (ArrayItem
                 (toIdentifier "a", RangeLiteral(RangeLiteral(toExpr MissingExpr, toInt 2) |> toExpr, toInt 0) |> toExpr)),
         [])
        ("a[ ...2 .. 0]",
         true,
         toExpr
             (ArrayItem
                 (toIdentifier "a", RangeLiteral(RangeLiteral(toExpr MissingExpr, toInt 2) |> toExpr, toInt 0) |> toExpr)),
         [])
        ("a[ ... 2 ..0]",
         true,
         toExpr
             (ArrayItem
                 (toIdentifier "a", RangeLiteral(RangeLiteral(toExpr MissingExpr, toInt 2) |> toExpr, toInt 0) |> toExpr)),
         [])
        ("a[...-1...]",
         true,
         toExpr
             (ArrayItem
                 (toIdentifier "a",
                  RangeLiteral(RangeLiteral(toExpr MissingExpr, NEG(toInt 1) |> toExpr) |> toExpr, toExpr MissingExpr)
                  |> toExpr)),
         [])
        ("a[ ... -1 ... ]",
         true,
         toExpr
             (ArrayItem
                 (toIdentifier "a",
                  RangeLiteral(RangeLiteral(toExpr MissingExpr, NEG(toInt 1) |> toExpr) |> toExpr, toExpr MissingExpr)
                  |> toExpr)),
         [])
        ("a[...]",
         true,
         toExpr (ArrayItem(toIdentifier "a", RangeLiteral(toExpr MissingExpr, toExpr MissingExpr) |> toExpr)),
         [])
        ("a[ ... ]",
         true,
         toExpr (ArrayItem(toIdentifier "a", RangeLiteral(toExpr MissingExpr, toExpr MissingExpr) |> toExpr)),
         [])
    ]
    |> List.iter testExpr


[<Fact>]
let ``Operator precendence tests`` () =
    [
        ("1+2*3", true, toExpr (ADD(toInt 1, (MUL(toInt 2, toInt 3) |> toExpr))), [])
        ("1*2-3", true, toExpr (SUB((MUL(toInt 1, toInt 2) |> toExpr), toInt 3)), [])
        ("1/2^3", true, toExpr (DIV(toInt 1, (POW(toInt 2, toInt 3) |> toExpr))), [])
        ("1/2/3", true, toExpr (DIV((DIV(toInt 1, toInt 2) |> toExpr), toInt 3)), [])
        ("1-2-3", true, toExpr (SUB((SUB(toInt 1, toInt 2) |> toExpr), toInt 3)), [])
        ("1==2/3", true, toExpr (EQ(toInt 1, (DIV(toInt 2, toInt 3) |> toExpr))), [])
        ("1+2>2*3", true, toExpr (GT(ADD(toInt 1, toInt 2) |> toExpr, (MUL(toInt 2, toInt 3) |> toExpr))), [])
        ("A(5+7)?2^3|B(3)/2",
         true,
         toExpr
             (CONDITIONAL
                 (CallLikeExpression(toIdentifier "A", [ ADD(toInt 5, toInt 7) |> toExpr ] |> toTuple) |> toExpr,
                  POW(toInt 2, toInt 3) |> toExpr,
                  DIV(CallLikeExpression(toIdentifier "B", [ toInt 3 ] |> toTuple) |> toExpr, toInt 2) |> toExpr)),
         [])
    ]
    |> List.iter testExpr
