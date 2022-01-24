// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.TestUtils

open System
open System.Collections.Immutable
open System.IO
open System.Text.RegularExpressions
open FParsec
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.TextProcessing
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
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


// utils for building test cases

let ReadAndChunkSourceFile fileName =
    let sourceInput = Path.Combine("TestCases", fileName) |> File.ReadAllText
    sourceInput.Split([| "===" |], StringSplitOptions.RemoveEmptyEntries)

let BuildContent content =
    let compilationManager = new CompilationUnitManager(ProjectProperties.Empty, (fun ex -> failwith ex.Message))
    let fileId = new Uri(Path.GetFullPath(Path.GetRandomFileName()))
    let file =
        CompilationUnitManager.InitializeFileManager(
            fileId,
            content,
            compilationManager.PublishDiagnostics,
            compilationManager.LogException)

    compilationManager.AddOrUpdateSourceFileAsync(file) |> ignore
    let compilationDataStructures = compilationManager.Build()
    compilationManager.TryRemoveSourceFileAsync(fileId, false) |> ignore

    compilationDataStructures.Diagnostics() |> Seq.exists (fun d -> d.IsError()) |> Assert.False
    Assert.NotNull compilationDataStructures.BuiltCompilation

    compilationDataStructures


// utils for getting components from test materials

let GetBodyFromCallable call =
    call.Specializations |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsBody)

let GetAdjFromCallable call =
    call.Specializations |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsAdjoint)

let GetCtlFromCallable call =
    call.Specializations |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsControlled)

let GetCtlAdjFromCallable call =
    call.Specializations |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsControlledAdjoint)

let GetLinesFromSpecialization specialization =
    let writer = new SyntaxTreeToQsharp()

    specialization
    |> fun x ->
        match x.Implementation with
        | Provided (_, body) -> Some body
        | _ -> None
    |> Option.get
    |> writer.Statements.OnScope
    |> ignore

    writer.SharedState.StatementOutputHandle
    |> Seq.filter (not << String.IsNullOrWhiteSpace)
    |> Seq.toArray

let GetCallableWithName compilation ns name =
    compilation.Namespaces
    |> Seq.filter (fun x -> x.Name = ns)
    |> GlobalCallableResolutions
    |> Seq.find (fun x -> x.Key.Name = name)
    |> (fun x -> x.Value)

let GetCallablesWithSuffix compilation ns (suffix: string) =
    compilation.Namespaces
    |> Seq.filter (fun x -> x.Name = ns)
    |> GlobalCallableResolutions
    |> Seq.filter (fun x -> x.Key.Name.EndsWith suffix)
    |> Seq.map (fun x -> x.Value)

let private _checkIfLineIsCall ``namespace`` name input =
    let call = sprintf @"(%s\.)?%s" <| Regex.Escape ``namespace`` <| Regex.Escape name
    let typeArgs = @"(<\s*([^<]*[^<\s])\s*>)?" // Does not support nested type args
    let args = @"\(\s*(.*[^\s])?\s*\)"
    let regex = sprintf @"^\s*%s\s*%s\s*%s;$" call typeArgs args

    let regexMatch = Regex.Match(input, regex)

    if regexMatch.Success then
        (true, regexMatch.Groups.[3].Value, regexMatch.Groups.[4].Value)
    else
        (false, "", "")

let IdentifyGeneratedByCalls generatedCallables calls =
    let mutable callables =
        generatedCallables |> Seq.map (fun x -> x, x |> (GetBodyFromCallable >> GetLinesFromSpecialization))

    let hasCall callable (call: seq<int * string * string>) =
        let (_, lines: string []) = callable
        Seq.forall (fun (i, ns, name) -> _checkIfLineIsCall ns name lines.[i] |> (fun (x, _, _) -> x)) call

    Assert.True(Seq.length callables = Seq.length calls) // This should be true if this method is called correctly

    let mutable rtrn = Seq.empty

    let removeAt i lst =
        Seq.append <| Seq.take i lst <| Seq.skip (i + 1) lst

    for call in calls do
        callables
        |> Seq.tryFindIndex (fun callSig -> hasCall callSig call)
        |> (fun x ->
            Assert.True(x <> None, "Did not find expected generated content")
            rtrn <- Seq.append rtrn [ Seq.item x.Value callables ]
            callables <- removeAt x.Value callables)

    rtrn |> Seq.map (fun (x, y) -> x)


// utils for testing checks and assertions

let CheckIfLineIsCall = _checkIfLineIsCall

let CheckIfSpecializationHasCalls specialization (calls: seq<int * string * string>) =
    let lines = GetLinesFromSpecialization specialization
    Seq.forall (fun (i, ns, name) -> CheckIfLineIsCall ns name lines.[i] |> (fun (x, _, _) -> x)) calls

let AssertSpecializationHasCalls specialization calls =
    Assert.True(
        CheckIfSpecializationHasCalls specialization calls,
        sprintf "Callable %O(%A) did not have expected content" specialization.Parent specialization.Kind
    )

let DoesCallSupportFunctors expectedFunctors call =
    let hasAdjoint = expectedFunctors |> Seq.contains QsFunctor.Adjoint
    let hasControlled = expectedFunctors |> Seq.contains QsFunctor.Controlled

    // Checks the characteristics match
    let charMatch =
        lazy
            (match call.Signature.Information.Characteristics.SupportedFunctors with
             | Value x -> x.SetEquals(expectedFunctors)
             | Null -> 0 = Seq.length expectedFunctors)

    // Checks that the target specializations are present
    let adjMatch =
        lazy
            (if hasAdjoint then
                 match call.Specializations |> Seq.tryFind (fun x -> x.Kind = QsSpecializationKind.QsAdjoint) with
                 | None -> false
                 | Some x ->
                     match x.Implementation with
                     | SpecializationImplementation.Generated gen ->
                         gen = QsGeneratorDirective.Invert || gen = QsGeneratorDirective.SelfInverse
                     | SpecializationImplementation.Provided _ -> true
                     | _ -> false
             else
                 true)

    let ctlMatch =
        lazy
            (if hasControlled then
                 match call.Specializations |> Seq.tryFind (fun x -> x.Kind = QsSpecializationKind.QsControlled) with
                 | None -> false
                 | Some x ->
                     match x.Implementation with
                     | SpecializationImplementation.Generated gen -> gen = QsGeneratorDirective.Distribute
                     | SpecializationImplementation.Provided _ -> true
                     | _ -> false
             else
                 true)

    charMatch.Value && adjMatch.Value && ctlMatch.Value

let AssertCallSupportsFunctors expectedFunctors call =
    Assert.True(
        DoesCallSupportFunctors expectedFunctors call,
        sprintf "Callable %O did not support the expected functors" call.FullName
    )
