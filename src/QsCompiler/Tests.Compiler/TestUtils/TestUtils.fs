// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.TestUtils

open System
open System.Collections.Immutable
open System.Text.RegularExpressions
open FParsec
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.TextProcessing
open Xunit


// utils for regex testing

let VerifyNoMatch input (m: Match) =
    Assert.False(m.Success, sprintf "matched \"%s\" for input \"%s\"" m.Value input)

let VerifyMatch expected (m: Match) =
    Assert.True(m.Success, sprintf "failed to match \"%s\"" expected)
    Assert.Equal(expected, m.Value)

let VerifyMatches (expected: _ list) (m: MatchCollection) =
    Assert.Equal(expected.Length, m.Count)

    for i = 0 to m.Count - 1 do
        Assert.Equal(expected.[i], m.[i].Value)


// utils for syntax testing

let isError diag =
    match diag.Diagnostic with
    | Diagnostics.Error (_) -> true
    | _ -> false

let getErrorCode diag =
    match diag.Diagnostic with
    | Diagnostics.Error (c) -> Some c
    | _ -> None

let simpleParseString parser string =
    match CharParsers.runParserOnString parser [] "" string with
    | Success (_) -> true
    | Failure (_) -> false

let parse_string parser str =
    let diags : QsCompilerDiagnostic list = []

    match CharParsers.runParserOnString parser diags "" str with
    | Success (_) -> true
    | Failure (_) -> false

let parse_string_diags parser str =
    let diags : QsCompilerDiagnostic list = []

    match CharParsers.runParserOnString parser diags "" str with
    | Success (_, ustate, _) -> true, ustate
    | Failure (_) -> false, []

let parse_string_diags_res parser str =
    let diags : QsCompilerDiagnostic list = []

    match CharParsers.runParserOnString parser diags "" str with
    | Success (res, ustate, _) -> true, ustate, Some res
    | Failure (_) -> false, [], None

let firstOfFour (a, _b, _c, _d) = a

let toExpr (ex: QsExpressionKind<QsExpression, QsSymbol, QsType>) = { Expression = ex; Range = Null }

let toInt n = IntLiteral(int64 n) |> toExpr

let toBigInt b =
    BigIntLiteral(System.Numerics.BigInteger.Parse b) |> toExpr

let toSymbol s = { Symbol = Symbol s; Range = Null }

let toIdentifier s =
    (Identifier(toSymbol s, Null)) |> toExpr

let toTuple (es: QsExpression seq) =
    ValueTuple(es.ToImmutableArray()) |> toExpr

let toArray (es: QsExpression seq) =
    ValueArray(es.ToImmutableArray()) |> toExpr

let toNewArray (b: QsType) (e: QsExpression) = NewArray(b, e) |> toExpr

let toType k = { Type = k; Range = Null }

let unitType = UnitType |> toType

let qubitType = Qubit |> toType

let internal toTupleType items =
    ImmutableArray.CreateRange items |> TupleType |> toType

let toOpType it ot s =
    QsTypeKind.Operation((it, ot), s) |> toType

let toCharacteristicsExpr k = { Characteristics = k; Range = Null }

let internal emptySet = toCharacteristicsExpr EmptySet
let adjSet = SimpleSet Adjointable |> toCharacteristicsExpr
let ctlSet = SimpleSet Controllable |> toCharacteristicsExpr
let adjCtlSet = Union(adjSet, ctlSet) |> toCharacteristicsExpr

let matchDiagnostics expected (actual: QsCompilerDiagnostic list) =
    let diags = actual |> List.map (fun d -> d.Diagnostic)

    ((diags |> List.length) = (expected |> List.length))
    && (diags |> List.forall (fun d -> expected |> List.contains d))

let rec matchType (t1: QsType) (t2: QsType) =
    let matchAll (a1: ImmutableArray<QsType>) (a2: ImmutableArray<QsType>) =
        a1.Length = a2.Length && Seq.forall2 matchType a1 a2

    let rec matchSetExpr (e1: Characteristics) (e2: Characteristics) =
        match e1.Characteristics with
        | InvalidSetExpr
        | EmptySet
        | SimpleSet _ -> e1.Characteristics = e2.Characteristics
        | Union (fu1, su1) ->
            match e2.Characteristics with
            | Union (fu2, su2) -> matchSetExpr fu1 fu2 && matchSetExpr su1 su2
            | _ -> false
        | Intersection (fi1, si1) ->
            match e2.Characteristics with
            | Intersection (fi2, si2) -> matchSetExpr fi1 fi2 && matchSetExpr si1 si2
            | _ -> false

    match t1.Type with
    | UnitType
    | Int
    | BigInt
    | Double
    | Bool
    | String
    | Qubit
    | Result
    | Pauli
    | Range
    | MissingType
    | InvalidType -> t1.Type = t2.Type
    | ArrayType bt1 ->
        match t2.Type with
        | ArrayType bt2 -> matchType bt1 bt2
        | _ -> false
    | TupleType ts1 ->
        match t2.Type with
        | TupleType ts2 -> matchAll ts1 ts2
        | _ -> false
    | UserDefinedType name1 ->
        match t2.Type with
        | UserDefinedType name2 -> name1.Symbol = name2.Symbol
        | _ -> false
    | TypeParameter tp1 ->
        match t2.Type with
        | TypeParameter tp2 -> tp1.Symbol = tp2.Symbol
        | _ -> false
    | QsTypeKind.Operation ((it1, ot1), s1) ->
        match t2.Type with
        | QsTypeKind.Operation ((it2, ot2), s2) -> (matchType it1 it2) && (matchType ot1 ot2) && (matchSetExpr s1 s2)
        | _ -> false
    | QsTypeKind.Function (it1, ot1) ->
        match t2.Type with
        | QsTypeKind.Function (it2, ot2) -> (matchType it1 it2) && (matchType ot1 ot2)
        | _ -> false

let rec matchExpression e1 e2 =
    let matchAll (a1: ImmutableArray<QsExpression>) (a2: ImmutableArray<QsExpression>) =
        a1.Length = a2.Length && Seq.forall2 matchExpression a1 a2

    let matchTypeArray (t1: QsNullable<ImmutableArray<QsType>>) (t2: QsNullable<ImmutableArray<QsType>>) =
        if t1 <> Null && t2 <> Null then
            Seq.forall2 matchType (t1.ValueOr ImmutableArray.Empty) (t2.ValueOr ImmutableArray.Empty)
        elif t1 = Null && t2 = Null then
            true
        else
            false

    match e1.Expression, e2.Expression with
    | DoubleLiteral d1, DoubleLiteral d2 -> d1 = d2 || (Double.IsNaN d1 && Double.IsNaN d2)
    | Identifier (i1, t1), Identifier (i2, t2) -> i1.Symbol = i2.Symbol && matchTypeArray t1 t2
    | StringLiteral (s1, a1), StringLiteral (s2, a2) -> s1 = s2 && matchAll a1 a2
    | ValueTuple a1, ValueTuple a2 -> matchAll a1 a2
    | NewArray (t1, s1), NewArray (t2, s2) -> matchType t1 t2 && matchExpression s1 s2
    | ValueArray a1, ValueArray a2 -> matchAll a1 a2
    | SizedArray (value1, size1), SizedArray (value2, size2) ->
        matchExpression value1 value2 && matchExpression size1 size2
    | ArrayItem (s1a, s1b), ArrayItem (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | NamedItem (u1, a1), NamedItem (u2, a2) -> matchExpression u1 u2 && a1.Symbol = a2.Symbol
    | NEG s1, NEG s2 -> matchExpression s1 s2
    | NOT s1, NOT s2 -> matchExpression s1 s2
    | BNOT s1, BNOT s2 -> matchExpression s1 s2
    | ADD (s1a, s1b), ADD (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | SUB (s1a, s1b), SUB (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | MUL (s1a, s1b), MUL (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | DIV (s1a, s1b), DIV (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | MOD (s1a, s1b), MOD (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | POW (s1a, s1b), POW (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | EQ (s1a, s1b), EQ (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | NEQ (s1a, s1b), NEQ (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | LT (s1a, s1b), LT (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | LTE (s1a, s1b), LTE (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | GT (s1a, s1b), GT (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | GTE (s1a, s1b), GTE (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | AND (s1a, s1b), AND (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | OR (s1a, s1b), OR (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | BOR (s1a, s1b), BOR (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | BAND (s1a, s1b), BAND (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | BXOR (s1a, s1b), BXOR (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | LSHIFT (s1a, s1b), LSHIFT (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | RSHIFT (s1a, s1b), RSHIFT (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | RangeLiteral (s1a, s1b), RangeLiteral (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | CopyAndUpdate (s1a, s1b, s1c), CopyAndUpdate (s2a, s2b, s2c) ->
        matchExpression s1a s2a && matchExpression s1b s2b && matchExpression s1c s2c
    | CONDITIONAL (s1a, s1b, s1c), CONDITIONAL (s2a, s2b, s2c) ->
        matchExpression s1a s2a && matchExpression s1b s2b && matchExpression s1c s2c
    | UnwrapApplication s1, UnwrapApplication s2 -> matchExpression s1 s2
    | AdjointApplication s1, AdjointApplication s2 -> matchExpression s1 s2
    | ControlledApplication s1, ControlledApplication s2 -> matchExpression s1 s2
    | CallLikeExpression (s1a, s1b), CallLikeExpression (s2a, s2b) -> matchExpression s1a s2a && matchExpression s1b s2b
    | Lambda lambda1, Lambda lambda2 ->
        lambda1.Kind = lambda2.Kind
        && lambda1.Param = lambda2.Param
        && matchExpression lambda1.Body lambda2.Body
    | expr1, expr2 -> expr1 = expr2

let testOne parser (str, succExp, resExp, diagsExp) =
    let succ, diags, res = parse_string_diags_res parser str
    let succOk = succ = succExp
    let resOk = (not succ) || (res |> Option.contains resExp)
    let errsOk = (not succ) || (matchDiagnostics diagsExp diags)

    Assert.True(
        succOk && resOk && errsOk,
        sprintf
            "String %s: %s"
            str
            (if not succOk then sprintf "%s unexpectedly" (if succExp then "failed" else "passed")
             elif not resOk then sprintf "expected result %A but received %A" resExp res.Value
             else sprintf "expected errors %A but received %A" diagsExp diags)
    )

let internal testType (str, result, diagnostics) =
    let success, diagnostics', result' = parse_string_diags_res TypeParsing.qsType str
    Assert.True(success, sprintf "Failed to parse: %s" str)

    Assert.True(
        result' |> Option.exists (matchType result),
        sprintf "Type: %s\n\nExpected result:\n%A\n\nActual result:\n%A" str result result'
    )

    Assert.True(
        matchDiagnostics diagnostics diagnostics',
        sprintf "Type: %s\n\nExpected diagnostics:\n%A\n\nActual diagnostics:\n%A" str diagnostics diagnostics'
    )

let testExpr (str, succExp, resExp, diagsExp) =
    let succ, diags, res = parse_string_diags_res ExpressionParsing.expr str
    let succOk = succ = succExp
    let resOk = (not succ) || (res |> Option.exists (matchExpression resExp))
    let errsOk = (not succ) || (matchDiagnostics diagsExp diags)

    Assert.True(
        succOk && resOk && errsOk,
        sprintf
            "Expression %s: %s"
            str
            (if not succOk then sprintf "%s unexpectedly" (if succExp then "failed" else "passed")
             elif not resOk then sprintf "expected result %A but received %A" resExp res.Value
             else sprintf "expected errors %A but received %A" diagsExp diags)
    )
