module Microsoft.Quantum.QsCompiler.CompilerOptimization.Utils

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Numerics
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree

open ComputationExpressions
open TransformationState


/// Option default value operator
let internal (|?) = defaultArg


/// Converts a range literal to a sequence of integers
let internal rangeLiteralToSeq (r: Expr): seq<int64> =
    match r with
    | RangeLiteral (a, b) ->
        match a.Expression, b.Expression with
        | IntLiteral start, IntLiteral stop ->
            seq { start .. stop }
        | RangeLiteral ({Expression = IntLiteral start}, {Expression = IntLiteral step}), IntLiteral stop ->
            seq { start .. step .. stop }
        | _ -> failwithf "Invalid range literal: %O" r
    | _ -> failwithf "Not a range literal: %O" r


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
        VariableNameTuple ((Seq.map toSymbolTuple items).ToImmutableArray())

/// Wraps a QsExpressionType in a basic TypedExpression
/// The returned TypedExpression has no type param / inferred info / range information,
/// and it should not be used for any code step that requires this information.
let internal wrapExpr (expr: Expr) (bt: TypeKind): TypedExpression =
    let ii = {IsMutable=false; HasLocalQuantumDependency=false}
    TypedExpression.New (expr, ImmutableDictionary.Empty, ResolvedType.New bt, ii, Null)

/// Wraps a QsStatementKind in a basic QsStatement
let internal wrapStmt (stmt: QsStatementKind): QsStatement =
    QsStatement.New QsComments.Empty Null (stmt, [])


/// Returns a new array of the given type and length.
/// Returns None if the type doesn't have a default value.
let rec internal constructNewArray (bt: TypeKind) (length: int): Expr option =
    defaultValue bt |> Option.map (fun x -> ImmutableArray.CreateRange (List.replicate length (wrapExpr x bt)) |> ValueArray)

/// Returns the default value for a given type (from Q# documentation)
and private defaultValue (bt: TypeKind): Expr option =
    match bt with
    | Int -> IntLiteral 0L |> Some
    | BigInt -> BigIntLiteral BigInteger.Zero |> Some
    | Double -> DoubleLiteral 0.0 |> Some
    | Bool -> BoolLiteral false |> Some
    | String -> StringLiteral (NonNullable<_>.New "", ImmutableArray.Empty) |> Some
    | Pauli -> PauliLiteral PauliI |> Some
    | Result -> ResultLiteral Zero |> Some
    | Range -> RangeLiteral (wrapExpr (IntLiteral 1L) Int, wrapExpr (IntLiteral 0L) Int) |> Some
    | ArrayType t -> constructNewArray t.Resolution 0
    | _ -> None


/// Returns true if the given expression contains any MissingExprs
let rec internal hasMissingExprs (expr: TypedExpression): bool =
    match expr.Expression with
    | MissingExpr -> true
    | ValueTuple vt -> Seq.exists hasMissingExprs vt
    | _ -> false

/// Fills a partial argument by replacing MissingExprs with the corresponding values of a tuple
let rec internal fillPartialArg (partialArg: TypedExpression, arg: TypedExpression): TypedExpression =
    match partialArg with
    | Missing -> arg
    | Tuple items ->
        let argsList =
            match List.filter hasMissingExprs items, arg with
            | [_], _ -> [arg]
            | _, Tuple args -> args
            | _ -> failwithf "args must be a tuple"
        // assert items2.Length = items3.Length
        items |> List.mapFold (fun args t1 ->
            if hasMissingExprs t1 then
                match args with
                | [] -> failwithf "ran out of args"
                | head :: tail -> fillPartialArg (t1, head), tail
            else t1, args
        ) argsList |> fst |> ImmutableArray.CreateRange
        |> ValueTuple |> wrapExpr <| partialArg.ResolvedType.Resolution
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


/// Returns all the "bottom-level" statements in the current scope.
/// Bottom-level statements are defined as those that don't contain their own scopes.
/// This function recurses through all the subscopes of the current scope.
let rec private findAllBaseStatements (scope: QsScope): seq<QsStatementKind> =
    scope.Statements |> Seq.collect (fun s ->
        match s.Statement with
        | QsConditionalStatement cond ->
            seq {
                for _, b in cond.ConditionalBlocks do yield b.Body
                match cond.Default with
                | Value b -> yield b.Body
                | _ -> ()
            } |> Seq.collect findAllBaseStatements
        | QsForStatement {Body = scope}
        | QsWhileStatement {Body = scope}
        | QsQubitScope {Body = scope}
        | QsScopeStatement {Body = scope} -> findAllBaseStatements scope
        | QsRepeatStatement {RepeatBlock = scope1; FixupBlock = scope2} ->
            Seq.concat [findAllBaseStatements scope1.Body; findAllBaseStatements scope2.Body]
        | x -> Seq.singleton x
    )

/// Returns whether this scope contains any return statement
let rec internal hasReturnStatement (scope: QsScope): bool =
    scope |> findAllBaseStatements |> Seq.exists (function QsReturnStatement _ -> true | _ -> false)

/// Returns the number of "bottom-level" statements in this scope
let rec internal scopeLength (scope: QsScope): int =
    scope |> findAllBaseStatements |> Seq.length


/// Tries to inline the given expression, checking whether it's valid to do so.
/// Returns the name of the callable and the modified scope after inlining.
let internal tryInline (state: TransformationState) (ex: TypedExpression) =
    maybe {
        let! expr, arg =
            match ex.Expression with
            | CallLikeExpression ({Expression = expr}, arg) -> Some (expr, arg)
            | _ -> None
        let! qualName, specKind =
            match expr with
            | Identifier (GlobalCallable qualName, _) -> Some (qualName, QsBody)
            | AdjointApplication {Expression = Identifier (GlobalCallable qualName, _)} -> Some (qualName, QsAdjoint)
            | ControlledApplication {Expression = Identifier (GlobalCallable qualName, _)} -> Some (qualName, QsControlled)
            | AdjointApplication {Expression = ControlledApplication {Expression = Identifier (GlobalCallable qualName, _)}} -> Some (qualName, QsControlledAdjoint)
            | ControlledApplication {Expression = AdjointApplication {Expression = Identifier (GlobalCallable qualName, _)}} -> Some (qualName, QsControlledAdjoint)
            | _ -> None
        let callable = getCallable state qualName
        let! impl = callable.Specializations |> Seq.tryFind (fun s -> s.Kind = specKind)
        let! specArgs, scope =
            match impl with
            | {Implementation = Provided (specArgs, scope)} -> Some (specArgs, scope)
            | {Implementation = Generated SelfInverse} ->
                let newKind = match specKind with QsAdjoint -> Some QsBody | QsControlledAdjoint -> Some QsControlled | _ -> None
                match callable.Specializations |> Seq.tryFind (fun s -> Some s.Kind = newKind) with
                | Some {Implementation = Provided (specArgs, scope)} -> Some (specArgs, scope)
                | _ -> None
            | _ -> None
        do! not (hasReturnStatement scope) |> check
        let newBinding = QsBinding.New ImmutableBinding (toSymbolTuple callable.ArgumentTuple, arg)
        let newStatements = scope.Statements.Insert (0, newBinding |> QsVariableDeclaration |> wrapStmt)
        return qualName, {scope with Statements = newStatements}
    }

/// Finds all the callables that could appear below this scope in the call stack.
/// Mutates the given HashSet by adding all the found callables to the set.
/// Is used to prevent inlining recursive functions into themselves forever.
let rec internal findAllCalls (state: TransformationState) (scope: QsScope) (found: HashSet<QsQualifiedName>): unit =
    scope |> findAllBaseStatements |> Seq.map (function
        | QsExpressionStatement ex ->
            match tryInline state ex with
            | Some (qualName, newScope) ->
                if found.Add qualName then
                    findAllCalls state newScope found
            | None -> ()
        | _ -> ()
    ) |> List.ofSeq |> ignore

