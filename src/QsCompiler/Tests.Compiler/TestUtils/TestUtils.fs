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

    for i in 0 .. m.Count - 1 do
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
    let diags: QsCompilerDiagnostic list = []

    match CharParsers.runParserOnString parser diags "" str with
    | Success (_) -> true
    | Failure (_) -> false

let parse_string_diags parser str =
    let diags: QsCompilerDiagnostic list = []

    match CharParsers.runParserOnString parser diags "" str with
    | Success (_, ustate, _) -> true, ustate
    | Failure (_) -> false, []

let parse_string_diags_res parser str =
    let diags: QsCompilerDiagnostic list = []

    match CharParsers.runParserOnString parser diags "" str with
    | Success (res, ustate, _) -> true, ustate, Some res
    | Failure (_) -> false, [], None

let firstOfFour (a, _b, _c, _d) = a

let toExpr (ex: QsExpressionKind<QsExpression, QsSymbol, QsType>) = { Expression = ex; Range = Null }

let toInt n = IntLiteral(int64 n) |> toExpr

let toBigInt b =
    BigIntLiteral(System.Numerics.BigInteger.Parse b) |> toExpr

let toSymbol s = { Symbol = Symbol s; Range = Null }

let toIdentifier s = (Identifier(toSymbol s, Null)) |> toExpr

let toTuple (es: QsExpression seq) =
    ValueTuple(es.ToImmutableArray()) |> toExpr

let toArray (es: QsExpression seq) =
    ValueArray(es.ToImmutableArray()) |> toExpr

let toNewArray (b: QsType) (e: QsExpression) = NewArray(b, e) |> toExpr

let toType k = { Type = k; Range = Null }

let unitType = UnitType |> toType

let qubitType = Qubit |> toType

let toOpType it ot s = Operation((it, ot), s) |> toType

let toCharacteristicsExpr k = { Characteristics = k; Range = Null }

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
    | Operation ((it1, ot1), s1) ->
        match t2.Type with
        | Operation ((it2, ot2), s2) -> (matchType it1 it2) && (matchType ot1 ot2) && (matchSetExpr s1 s2)
        | _ -> false
    | Function (it1, ot1) ->
        match t2.Type with
        | Function (it2, ot2) -> (matchType it1 it2) && (matchType ot1 ot2)
        | _ -> false

let rec matchExpression e1 e2 =
    let matchAll (a1: ImmutableArray<QsExpression>) (a2: ImmutableArray<QsExpression>) =
        a1.Length = a2.Length && Seq.forall2 matchExpression a1 a2

    let matchTypeArray (t1: QsNullable<ImmutableArray<QsType>>) (t2: QsNullable<ImmutableArray<QsType>>) =
        if t1 <> Null && t2 <> Null
        then Seq.forall2 matchType (t1.ValueOr ImmutableArray.Empty) (t2.ValueOr ImmutableArray.Empty)
        elif t1 = Null && t2 = Null
        then true
        else false

    let ex1 = e1.Expression
    let ex2 = e2.Expression

    match ex1 with
    | UnitValue
    | IntLiteral _
    | BigIntLiteral _
    | BoolLiteral _
    | ResultLiteral _
    | PauliLiteral _
    | MissingExpr
    | InvalidExpr -> ex1 = ex2
    | DoubleLiteral d1 ->
        match ex2 with
        | DoubleLiteral d2 -> d1 = d2 || (Double.IsNaN d1 && Double.IsNaN d2)
        | _ -> false
    | Identifier (i1, t1) ->
        match ex2 with
        | Identifier (i2, t2) -> i1.Symbol = i2.Symbol && matchTypeArray t1 t2
        | _ -> false
    | StringLiteral (s1, a1) ->
        match ex2 with
        | StringLiteral (s2, a2) -> (s1 = s2) && (matchAll a1 a2)
        | _ -> false
    | ValueTuple a1 ->
        match ex2 with
        | ValueTuple a2 -> matchAll a1 a2
        | _ -> false
    | NewArray (t1, s1) ->
        match ex2 with
        | NewArray (t2, s2) -> matchType t1 t2 && matchExpression s1 s2
        | _ -> false
    | ValueArray a1 ->
        match ex2 with
        | ValueArray a2 -> matchAll a1 a2
        | _ -> false
    | ArrayItem (s1a, s1b) ->
        match ex2 with
        | ArrayItem (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | NamedItem (u1, a1) ->
        match ex2 with
        | NamedItem (u2, a2) -> (matchExpression u1 u2) && (a1.Symbol = a2.Symbol)
        | _ -> false
    | NEG s1 ->
        match ex2 with
        | NEG s2 -> matchExpression s1 s2
        | _ -> false
    | NOT s1 ->
        match ex2 with
        | NOT s2 -> matchExpression s1 s2
        | _ -> false
    | BNOT s1 ->
        match ex2 with
        | BNOT s2 -> matchExpression s1 s2
        | _ -> false
    | ADD (s1a, s1b) ->
        match ex2 with
        | ADD (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | SUB (s1a, s1b) ->
        match ex2 with
        | SUB (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | MUL (s1a, s1b) ->
        match ex2 with
        | MUL (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | DIV (s1a, s1b) ->
        match ex2 with
        | DIV (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | MOD (s1a, s1b) ->
        match ex2 with
        | MOD (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | POW (s1a, s1b) ->
        match ex2 with
        | POW (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | EQ (s1a, s1b) ->
        match ex2 with
        | EQ (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | NEQ (s1a, s1b) ->
        match ex2 with
        | NEQ (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | LT (s1a, s1b) ->
        match ex2 with
        | LT (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | LTE (s1a, s1b) ->
        match ex2 with
        | LTE (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | GT (s1a, s1b) ->
        match ex2 with
        | GT (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | GTE (s1a, s1b) ->
        match ex2 with
        | GTE (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | AND (s1a, s1b) ->
        match ex2 with
        | AND (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | OR (s1a, s1b) ->
        match ex2 with
        | OR (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | BOR (s1a, s1b) ->
        match ex2 with
        | BOR (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | BAND (s1a, s1b) ->
        match ex2 with
        | BAND (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | BXOR (s1a, s1b) ->
        match ex2 with
        | BXOR (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | LSHIFT (s1a, s1b) ->
        match ex2 with
        | LSHIFT (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | RSHIFT (s1a, s1b) ->
        match ex2 with
        | RSHIFT (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | RangeLiteral (s1a, s1b) ->
        match ex2 with
        | RangeLiteral (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false
    | CopyAndUpdate (s1a, s1b, s1c) ->
        match ex2 with
        | CopyAndUpdate (s2a, s2b, s2c) ->
            (matchExpression s1a s2a) && (matchExpression s1b s2b) && (matchExpression s1c s2c)
        | _ -> false
    | CONDITIONAL (s1a, s1b, s1c) ->
        match ex2 with
        | CONDITIONAL (s2a, s2b, s2c) ->
            (matchExpression s1a s2a) && (matchExpression s1b s2b) && (matchExpression s1c s2c)
        | _ -> false
    | UnwrapApplication s1 ->
        match ex2 with
        | UnwrapApplication s2 -> matchExpression s1 s2
        | _ -> false
    | AdjointApplication s1 ->
        match ex2 with
        | AdjointApplication s2 -> matchExpression s1 s2
        | _ -> false
    | ControlledApplication s1 ->
        match ex2 with
        | ControlledApplication s2 -> matchExpression s1 s2
        | _ -> false
    | CallLikeExpression (s1a, s1b) ->
        match ex2 with
        | CallLikeExpression (s2a, s2b) -> (matchExpression s1a s2a) && (matchExpression s1b s2b)
        | _ -> false

let testOne parser (str, succExp, resExp, diagsExp) =
    let succ, diags, res = parse_string_diags_res parser str
    let succOk = succ = succExp
    let resOk = (not succ) || (res |> Option.contains resExp)
    let errsOk = (not succ) || (matchDiagnostics diagsExp diags)

    Assert.True
        (succOk && resOk && errsOk,
         sprintf
             "String %s: %s"
             str
             (if not succOk
              then sprintf "%s unexpectedly" (if succExp then "failed" else "passed")
              elif not resOk
              then sprintf "expected result %A but received %A" resExp res.Value
              else sprintf "expected errors %A but received %A" diagsExp diags))

let testExpr (str, succExp, resExp, diagsExp) =
    let succ, diags, res = parse_string_diags_res ExpressionParsing.expr str
    let succOk = succ = succExp
    let resOk = (not succ) || (res |> Option.exists (matchExpression resExp))
    let errsOk = (not succ) || (matchDiagnostics diagsExp diags)

    Assert.True
        (succOk && resOk && errsOk,
         sprintf
             "Expression %s: %s"
             str
             (if not succOk
              then sprintf "%s unexpectedly" (if succExp then "failed" else "passed")
              elif not resOk
              then sprintf "expected result %A but received %A" resExp res.Value
              else sprintf "expected errors %A but received %A" diagsExp diags))
