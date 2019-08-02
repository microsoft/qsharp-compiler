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
let rangeLiteralToSeq (r: Expr): seq<int64> =
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
let rec toSymbolTuple (x: QsTuple<LocalVariableDeclaration<QsLocalSymbol>>): SymbolTuple =
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
let wrapExpr (expr: Expr) (bt: TypeKind): TypedExpression =
    let ii = {IsMutable=false; HasLocalQuantumDependency=false}
    TypedExpression.New (expr, ImmutableDictionary.Empty, ResolvedType.New bt, ii, Null)

/// Wraps a QsStatementKind in a basic QsStatement
let wrapStmt (stmt: QsStatementKind): QsStatement =
    QsStatement.New QsComments.Empty Null (stmt, [])


/// Returns a new array of the given type and length.
/// Returns None if the type doesn't have a default value.
let rec constructNewArray (bt: TypeKind) (length: int): Expr option =
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


/// Counts the number of MissingExprs in the given tuple
let rec private countMissingExprs (expr: TypedExpression): int =
    match expr.Expression with
    | MissingExpr -> 1
    | ValueTuple vt -> Seq.sumBy countMissingExprs vt
    | _ -> 0
    
/// Replaces any MissingExprs in the given tuple with the first elements of the given list.
/// Returns the new values of the partial-argument tuple and the argument list.
let rec private fillPartialArg (partialArg: TypedExpression, arg: list<TypedExpression>): TypedExpression * list<TypedExpression> =
    match partialArg.Expression with
    | MissingExpr ->
        match arg with
        | first::rest -> first, rest
        | _ -> failwithf "Not enough remaining arguments"
    | ValueTuple vt ->
        let newList, newArg =
            Seq.fold (fun (cpa, ca) v ->
                let newPa, newCa = fillPartialArg (v, ca)
                cpa @ [newPa], newCa
            ) ([], arg) vt
        let newPa = wrapExpr (ValueTuple (ImmutableArray.CreateRange newList)) partialArg.ResolvedType.Resolution
        newPa, newArg
    | _ -> partialArg, arg

/// Transforms a partially-application call by replacing missing values with the new arguments
let internal partialApplyFunction (baseMethod: TypedExpression) (partialArg: TypedExpression) (arg: TypedExpression): Expr =
    let argsList =
        if countMissingExprs partialArg = 1 then [arg] else
        match arg.Expression with
        | ValueTuple vt ->
            if countMissingExprs partialArg <> vt.Length
            then failwithf "Invalid number of arguments: %O doesn't match %O" arg.Expression partialArg.Expression
            else List.ofSeq vt
        | _ -> failwithf "Invalid arg: %O" arg.Expression
    CallLikeExpression (baseMethod, fst (fillPartialArg (partialArg, argsList)))


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

let rec hasReturnStatement (scope: QsScope): bool =
    scope |> findAllBaseStatements |> Seq.exists (function QsReturnStatement _ -> true | _ -> false)

let rec scopeLength (scope: QsScope): int =
    scope |> findAllBaseStatements |> Seq.length


let tryInline (state: TransformationState) (ex: TypedExpression) =
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
        let! _ = if not (hasReturnStatement scope) then Some () else None
        let newBinding = QsBinding.New ImmutableBinding (toSymbolTuple callable.ArgumentTuple, arg)
        let newStatements = scope.Statements.Insert (0, newBinding |> QsVariableDeclaration |> wrapStmt)
        return qualName, {scope with Statements = newStatements}
    }

let rec findAllCalls (state: TransformationState) (scope: QsScope) (found: HashSet<QsQualifiedName>): unit =
    scope |> findAllBaseStatements |> Seq.map (function
        | QsExpressionStatement ex ->
            match tryInline state ex with
            | Some (qualName, newScope) ->
                if found.Add qualName then
                    findAllCalls state newScope found
            | None -> ()
        | _ -> ()
    ) |> List.ofSeq |> ignore

