// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module internal Microsoft.Quantum.QsCompiler.Experimental.Utils

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Numerics
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.ComputationExpressions
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree


/// Shorthand for a QsExpressionKind
type internal ExprKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>
/// Shorthand for a QsTypeKind
type internal TypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>
/// Shorthand for a QsInitializerKind
type internal InitKind = QsInitializerKind<ResolvedInitializer, TypedExpression>


/// The maybe monad. Returns None if any of the lines are None.
let internal maybe = MaybeBuilder()

/// Returns Some () if x is true, and returns None otherwise.
/// Normally used after a do! in the Maybe monad, which makes this act as an assertion.
let internal check x = if x then Some () else None


/// Returns whether a given expression is a literal (and thus a constant)
let rec internal isLiteral (callables: IDictionary<QsQualifiedName, QsCallable>) (expr: TypedExpression): bool =
    let folder ex sub = 
        match ex.Expression with
        | IntLiteral _ | BigIntLiteral _ | DoubleLiteral _ | BoolLiteral _ | ResultLiteral _ | PauliLiteral _ | StringLiteral _
        | UnitValue | MissingExpr | Identifier (GlobalCallable _, _)
        | ValueTuple _ | ValueArray _ | RangeLiteral _ | NewArray _ -> true
        | Identifier _ when ex.ResolvedType.Resolution = Qubit -> true
        | CallLikeExpression ({Expression = Identifier (GlobalCallable qualName, _)}, _)
            when (callables.[qualName]).Kind = TypeConstructor -> true
        | a when TypedExpression.IsPartialApplication a -> true
        | _ -> false
        && Seq.forall id sub
    expr.Fold folder


/// If check(value) is true, returns a Constants with the given variable defined as the given value.
/// Otherwise, returns constants without any changes.
/// If the given variable is already defined, its name is shadowed in the current scope.
/// Throws an InvalidOperationException if there aren't any scopes on the stack.
let internal defineVar check (constants : IDictionary<_,_>) (name, value) =
    if check value then constants.[name] <- value

/// Applies the given function op on a SymbolTuple, ValueTuple pair
let rec private onTuple op constants (names, values) : unit =
    match names, values with
    | VariableName name, _ ->
        op constants (name, values)
    | VariableNameTuple namesTuple, Tuple valuesTuple ->
        if namesTuple.Length <> valuesTuple.Length then
            ArgumentException "names and values have different lengths" |> raise
        for sym, value in Seq.zip namesTuple valuesTuple do
            onTuple op constants (sym, value)
    | _ -> ()

/// Returns a Constants<Expr> with the given variables defined as the given values
let internal defineVarTuple check = onTuple (defineVar check)


/// Option default value operator
let internal (|?) = defaultArg


/// Flattens a nested tuple into the sequence of base items of the tuple.
let rec internal flatten = function
| Tuple t1 -> Seq.collect flatten t1
| v1 -> Seq.singleton v1

/// Flattens a pair of nested tuples by pairing the base items of each tuple.
/// Returns a seqence of pairs, one element from each side.
/// It is guaranteed that at most one element of a pair will be a Tuple.
/// Throws an ArgumentException if the lengths of the tuples do not match.
let rec internal jointFlatten = function
| Tuple t1, Tuple t2 ->
    if t1.Length <> t2.Length then
        ArgumentException "The lengths of the given tuples do not match" |> raise
    Seq.zip t1 t2 |> Seq.collect jointFlatten
| v1, v2 -> Seq.singleton (v1, v2)


/// Converts a range literal to a sequence of integers.
/// Throws an ArgumentException if the input isn't a valid range literal.
let internal rangeLiteralToSeq (r: ExprKind): seq<int64> =
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
let rec internal optionListToListOption = function
    | [] -> Some []
    | None :: _ -> None
    | Some head :: tail -> Option.map (fun t2 -> head :: t2) (optionListToListOption tail)


/// Returns the given list without the elements at the given indices
let rec internal removeIndices idx l =
    Seq.indexed l |> Seq.filter (fun (i, _) -> not (Seq.contains i idx)) |> Seq.map snd


/// Converts a QsTuple to a SymbolTuple
let rec internal toSymbolTuple (x: QsTuple<LocalVariableDeclaration<QsLocalSymbol>>): SymbolTuple =
    match x with
    | QsTupleItem item ->
        match item.VariableName with
        | ValidName n -> VariableName n
        | InvalidName -> InvalidItem
    | QsTuple items when items.Length = 0 ->
        DiscardedItem
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
let internal wrapExpr (bt: TypeKind) (expr: ExprKind): TypedExpression =
    let ii = {IsMutable=false; HasLocalQuantumDependency=false}
    TypedExpression.New (expr, ImmutableDictionary.Empty, ResolvedType.New bt, ii, Null)

/// Wraps a QsStatementKind in a basic QsStatement
let internal wrapStmt (stmt: QsStatementKind): QsStatement =
    let symbolDecl =
        match stmt with
        | QsVariableDeclaration x ->
            let isMutable = x.Kind = MutableBinding
            let posInfo = (Null, Range.Zero)
            seq {
                for lhs, rhs in jointFlatten (x.Lhs, x.Rhs) do
                    match lhs with
                    | VariableName name ->
                        yield LocalVariableDeclaration.New isMutable (posInfo, name, rhs.ResolvedType, false)
                    | _ -> () }
        | _ -> Seq.empty
    QsStatement.New QsComments.Empty Null (stmt, LocalDeclarations.New symbolDecl)


/// Returns a new array of the given type and length.
/// Returns None if the type doesn't have a default value as an expression.
let rec internal constructNewArray (bt: TypeKind) (length: int): ExprKind option =
    defaultValue bt |> Option.map (fun x -> ImmutableArray.CreateRange (List.replicate length (wrapExpr bt x)) |> ValueArray)

/// Returns the default value for a given type (from Q# documentation).
/// Returns None for types whose default values are not representable as expressions.
and internal defaultValue (bt: TypeKind): ExprKind option =
    match bt with
    | UnitType -> UnitValue |> Some
    | Int -> IntLiteral 0L |> Some
    | BigInt -> BigIntLiteral BigInteger.Zero |> Some
    | Double -> DoubleLiteral 0.0 |> Some
    | Bool -> BoolLiteral false |> Some
    | String -> StringLiteral ("", ImmutableArray.Empty) |> Some
    | Pauli -> PauliLiteral PauliI |> Some
    | Result -> ResultLiteral Zero |> Some
    | Range -> RangeLiteral (wrapExpr Int (IntLiteral 1L), wrapExpr Int (IntLiteral 0L)) |> Some
    | ArrayType t -> constructNewArray t.Resolution 0
    | _ -> None


/// Returns true if the expression contains missing expressions.
/// Returns false otherwise.
let rec private containsMissing (ex : TypedExpression) =
    match ex.Expression with 
    | MissingExpr -> true
    | ValueTuple items -> items |> Seq.exists containsMissing
    | _ -> false

/// Fills a partial argument by replacing MissingExprs with the corresponding values of a tuple
let rec internal fillPartialArg (partialArg: TypedExpression, arg: TypedExpression): TypedExpression =
    match partialArg with
    | Missing -> arg
    | Tuple items ->
        let argsList =
            match List.filter containsMissing items, arg with
            | [_], _ -> [arg]
            | _, Tuple args -> args
            | _ -> failwithf "args must be a tuple"
        items |> List.mapFold (fun args t1 ->
            if containsMissing t1 then
                match args with
                | [] -> failwithf "ran out of args"
                | head :: tail -> fillPartialArg (t1, head), tail
            else t1, args
        ) argsList |> fst |> ImmutableArray.CreateRange
        |> ValueTuple |> wrapExpr partialArg.ResolvedType.Resolution
    | _ -> failwithf "unknown partialArgs"


/// Computes exponentiation for 64-bit integers
let internal longPow (a: int64) (b: int64): int64 =
    if b < 0L then failwithf "Negative power %d not supported for integer exponentiation." b
    let mutable x = a
    let mutable power = b
    let mutable returnValue = 1L;
    while power <> 0L do
        if (power &&& 1L) = 1L then returnValue <- returnValue * x
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
let internal findAllSubStatements (scope: QsScope) =
    let statementKind (s : QsStatement) = s.Statement
    scope.Statements |> Seq.collect (fun stm -> stm.ExtractAll (statementKind >> Seq.singleton))

/// Returns the number of return statements in this scope
let internal countReturnStatements (scope: QsScope): int =
    scope |> findAllSubStatements |> Seq.sumBy (function QsReturnStatement _ -> 1 | _ -> 0)

/// Returns the number of statements in this scope
let internal scopeLength (scope: QsScope): int =
    scope |> findAllSubStatements |> Seq.length


/// Returns whether all variables in a symbol tuple are discarded
let rec internal isAllDiscarded = function
    | DiscardedItem -> true
    | VariableNameTuple items -> Seq.forall isAllDiscarded items
    | _ -> false


/// Casts an int64 to an int, throwing an ArgumentException if outside the allowed range
let internal safeCastInt64 (i: int64): int =
    if i > int64 (1 <<< 30) || i < -int64 (1 <<< 30) then
        ArgumentException "Integer is too large for 32 bits" |> raise
    else int i

/// Casts a BigInteger to an int, throwing an ArgumentException if outside the allowed range
let internal safeCastBigInt (i: BigInteger): int =
    if BigInteger.Abs(i) > BigInteger (1 <<< 30) then
        ArgumentException "Integer is too large for 32 bits" |> raise
    else int i


/// Creates a new scope statement wrapping the given block
let internal newScopeStatement (block: QsScope): QsStatementKind =
    let posBlock = QsPositionedBlock.New QsComments.Empty Null block
    QsConditionalStatement.New (Seq.singleton (wrapExpr Bool (BoolLiteral true), posBlock), Null) |> QsConditionalStatement

/// Matches a QsStatementKind as a scope statement
let internal (|ScopeStatement|_|) (stmt: QsStatementKind) =
    maybe {
        let! condStmt = match stmt with QsConditionalStatement x -> Some x | _ -> None
        do! check (condStmt.ConditionalBlocks.Length >= 1)
        let cond, block = condStmt.ConditionalBlocks.[0]
        do! check (cond.Expression = BoolLiteral true)
        return block
    }
