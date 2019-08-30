// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.CompilerOptimization.Utils

open System
open System.Collections.Immutable
open System.Numerics
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree

open Types


/// Option default value operator
let internal (|?) = defaultArg


/// Flattens a pair of nested tuples by pairing the base items of each tuple.
/// Returns a seqence of pairs, one element from each side.
/// It is guaranteed that at most one element of a pair will be a Tuple.
/// Throws an ArgumentException if the lengths of the tuples do not match.
let rec internal jointFlatten (v1, v2) =
    match v1, v2 with
    | Tuple t1, Tuple t2 ->
        if t1.Length <> t2.Length then
            ArgumentException "The lengths of the given tuples do not match" |> raise
        Seq.zip t1 t2 |> Seq.collect jointFlatten
    | _ -> Seq.singleton (v1, v2)


/// Converts a range literal to a sequence of integers.
/// Throws an ArgumentException if the input isn't a valid range literal.
let internal rangeLiteralToSeq (r: Expr): seq<int64> =
    match r with
    | RangeLiteral (a, b) ->
        match a.Expression, b.Expression with
        | IntLiteral start, IntLiteral stop ->
            seq { start .. stop }
        | RangeLiteral ({Expression = IntLiteral start}, {Expression = IntLiteral step}), IntLiteral stop ->
            seq { start .. step .. stop }
        | _ -> ArgumentException "Invalid range literal" |> raise
    | _ -> ArgumentException "Not a range literal" |> raise


/// Returns None if any of the elements of the given list is None.
/// Otherwise, returns the given list, casting each option to its Some case.
let rec internal optionListToListOption l =
    match l with
    | [] -> Some []
    | None :: _ -> None
    | Some head :: tail -> Option.map (fun t2 -> head :: t2) (optionListToListOption tail)


/// Returns the given list without the elements at the given indices
let rec internal removeIndices idx l =
    List.indexed l |> List.filter (fun (i, _) -> not (List.contains i idx)) |> List.map snd


/// Converts a QsTuple to a SymbolTuple
let rec internal toSymbolTuple (x: QsTuple<LocalVariableDeclaration<QsLocalSymbol>>): SymbolTuple =
    match x with
    | QsTupleItem item ->
        match item.VariableName with
        | ValidName n -> VariableName n
        | InvalidName -> InvalidItem
    | QsTuple items when items.Length = 1 ->
        toSymbolTuple items.[0]
    | QsTuple items ->
        Seq.map toSymbolTuple items |> ImmutableArray.CreateRange |> VariableNameTuple

/// Matches a TypedExpression as a tuple of identifiers, represented as a SymbolTuple.
/// If the TypedExpression is not a tuple of identifiers, the pattern does not match.
let rec internal (|LocalVarTuple|_|) (expr: TypedExpression) =
    match expr.Expression with
    | Identifier (LocalVariable name, _) -> VariableName name |> Some
    | MissingExpr -> DiscardedItem |> Some
    | InvalidExpr -> InvalidItem |> Some
    | ValueTuple va ->
        va |> Seq.map (function LocalVarTuple t -> Some t | _ -> None)
        |> List.ofSeq |> optionListToListOption
        |> Option.map (ImmutableArray.CreateRange >> VariableNameTuple)
    | _ -> None


/// Wraps a QsExpressionType in a basic TypedExpression
/// The returned TypedExpression has no type param / inferred info / range information,
/// and it should not be used for any code step that requires this information.
let internal wrapExpr (bt: TypeKind) (expr: Expr): TypedExpression =
    let ii = {IsMutable=false; HasLocalQuantumDependency=false}
    TypedExpression.New (expr, ImmutableDictionary.Empty, ResolvedType.New bt, ii, Null)

/// Wraps a QsStatementKind in a basic QsStatement
let internal wrapStmt (stmt: QsStatementKind): QsStatement =
    let symbolDecl =
        match stmt with
        | QsVariableDeclaration x ->
            let isMutable = x.Kind = MutableBinding
            let posInfo = (Null, (QsPositionInfo.Zero, QsPositionInfo.Zero))
            seq {
                for lhs, rhs in jointFlatten (x.Lhs, x.Rhs) do
                    match lhs with
                    | VariableName name ->
                        yield LocalVariableDeclaration.New isMutable (posInfo, name, rhs.ResolvedType, false)
                    | _ -> () }
        | _ -> Seq.empty
    QsStatement.New QsComments.Empty Null (stmt, symbolDecl)


/// Returns a new array of the given type and length.
/// Returns None if the type doesn't have a default value as an expression.
let rec internal constructNewArray (bt: TypeKind) (length: int): Expr option =
    defaultValue bt |> Option.map (fun x -> ImmutableArray.CreateRange (List.replicate length (wrapExpr bt x)) |> ValueArray)

/// Returns the default value for a given type (from Q# documentation).
/// Returns None for types whose default values are not representable as expressions.
and private defaultValue (bt: TypeKind): Expr option =
    match bt with
    | Int -> IntLiteral 0L |> Some
    | BigInt -> BigIntLiteral BigInteger.Zero |> Some
    | Double -> DoubleLiteral 0.0 |> Some
    | Bool -> BoolLiteral false |> Some
    | String -> StringLiteral (NonNullable<_>.New "", ImmutableArray.Empty) |> Some
    | Pauli -> PauliLiteral PauliI |> Some
    | Result -> ResultLiteral Zero |> Some
    | Range -> RangeLiteral (wrapExpr Int (IntLiteral 1L), wrapExpr Int (IntLiteral 0L)) |> Some
    | ArrayType t -> constructNewArray t.Resolution 0
    | _ -> None


/// Fills a partial argument by replacing MissingExprs with the corresponding values of a tuple
let rec internal fillPartialArg (partialArg: TypedExpression, arg: TypedExpression): TypedExpression =
    match partialArg with
    | Missing -> arg
    | Tuple items ->
        let argsList =
            match List.filter TypedExpression.ContainsMissing items, arg with
            | [_], _ -> [arg]
            | _, Tuple args -> args
            | _ -> failwithf "args must be a tuple"
        // assert items2.Length = items3.Length
        items |> List.mapFold (fun args t1 ->
            if TypedExpression.ContainsMissing t1 then
                match args with
                | [] -> failwithf "ran out of args"
                | head :: tail -> fillPartialArg (t1, head), tail
            else t1, args
        ) argsList |> fst |> ImmutableArray.CreateRange
        |> ValueTuple |> wrapExpr partialArg.ResolvedType.Resolution
    | _ -> failwithf "unknown partialArgs"


/// Computes exponentiation for 64-bit integers
let internal longPow (a: int64) (b: int64): int64 =
    if b < 0L then
        failwithf "Negative power %d not supported for integer exponentiation." b
    let mutable x = a
    let mutable power = b
    let mutable returnValue = 1L;
    while power <> 0L do
        if (power &&& 1L) = 1L then
            returnValue <- returnValue * x
        x <- x * x
        power <- power >>> 1
    returnValue


/// Returns the intial part of the list that satisfies the given condition, just as List.takeWhile().
/// Also returns the first item that doesn't satisfy the given condition, or if all items satisfy the condition, returns None.
let rec internal takeWhilePlus1 (f: 'A -> bool) (l : list<'A>) =
    match l with
    | first :: rest ->
        if f first then
            let a, b = takeWhilePlus1 f rest
            first :: a, b
        else [], Some first
    | [] -> [], None


/// Returns a sequence of all statements contained directly or indirectly in this scope
let internal findAllBaseStatements (scope: QsScope) =
    scope.Statements |> Seq.collect (QsStatement.ExtractAll (fun s -> [s.Statement]))

/// Returns the number of return statements in this scope
let internal countReturnStatements (scope: QsScope): int =
    scope |> findAllBaseStatements |> Seq.sumBy (function QsReturnStatement _ -> 1 | _ -> 0)

/// Returns the number of statements in this scope
let internal scopeLength (scope: QsScope): int =
    scope |> findAllBaseStatements |> Seq.length


/// Returns whether all variables in a symbol tuple are discarded
let rec internal isAllDiscarded = function
| DiscardedItem -> true
| VariableNameTuple items -> Seq.forall isAllDiscarded items
| _ -> false


let internal safeCastInt64 (i: int64): int =
    if Math.Abs(i) > int64 (1 <<< 30) then
        ArgumentException "Integer is too large for 32 bits" |> raise
    int i

let internal safeCastBigInt (i: BigInteger): int =
    if BigInteger.Abs(i) > BigInteger (1 <<< 30) then
        ArgumentException "Integer is too large for 32 bits" |> raise
    int i
