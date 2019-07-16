module Microsoft.Quantum.QsCompiler.CompilerOptimization.Utils

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Numerics
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree


/// Option default value operator
let (|?) = defaultArg


/// Shorthand for a QsExpressionKind
type Expr = QsExpressionKind<TypedExpression, Identifier, ResolvedType>
/// Shorthand for a QsTypeKind
type TypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>


/// Represents the global dictionary that maps names to callables
type CallableDict = {
        compiledCallables: ImmutableDictionary<QsQualifiedName, QsCallable>
} with
    member this.getCallable (name: QsQualifiedName): QsCallable =
        this.compiledCallables.[name]


/// Returns whether a given expression is a literal (and thus a constant)
let rec isLiteral (expr: Expr) (cd: CallableDict): bool =
    match expr with
    | UnitValue | IntLiteral _ | BigIntLiteral _ | DoubleLiteral _ | BoolLiteral _ | ResultLiteral _ | PauliLiteral _ -> true
    | ValueTuple a | StringLiteral (_, a) | ValueArray a -> Seq.forall (fun x -> isLiteral x.Expression cd) a
    | RangeLiteral (a, b) -> isLiteral a.Expression cd && isLiteral b.Expression cd
    | NewArray (_, a) -> isLiteral a.Expression cd
    | Identifier (GlobalCallable _, _) | MissingExpr -> true
    | CallLikeExpression ({Expression = Identifier (GlobalCallable qualName, _)}, b)
        when (cd.getCallable qualName).Kind = TypeConstructor -> isLiteral b.Expression cd
    | CallLikeExpression (a, b) ->
        isLiteral a.Expression cd && isLiteral b.Expression cd &&
            TypedExpression.IsPartialApplication (CallLikeExpression (a, b))
    | _ -> false


/// Helper method to improve expression readability
let rec prettyPrint (expr: Expr): string =
    match expr with
    | Identifier (LocalVariable a, _) -> "LocalVariable " + a.Value
    | Identifier (GlobalCallable a, _) -> "GlobalCallable " + a.Name.Value
    | CallLikeExpression (f, x) -> "(" + (prettyPrint f.Expression) + " of " + (prettyPrint x.Expression) + ")"
    | ValueTuple a -> "(" + String.Join(", ", a |> Seq.map (fun x -> x.Expression) |> Seq.map prettyPrint) + ")"
    | ADD (a, b) -> "(" + (prettyPrint a.Expression) + " + " + (prettyPrint b.Expression) + ")"
    | a -> a.ToString()


/// Converts a range literal to a sequence of integers
let rangeLiteralToSeq (r: Expr): seq<int> =
    match r with
    | RangeLiteral (a, b) ->
        match a.Expression, b.Expression with
        | IntLiteral start, IntLiteral stop ->
            seq { int start .. int stop }
        | RangeLiteral ({Expression = IntLiteral start}, {Expression = IntLiteral step}), IntLiteral stop ->
            seq { int start .. int step .. int stop }
        | _ -> failwithf "Invalid range literal: %O" r
    | _ -> failwithf "Not a range literal: %O" r


/// Represents the current state of all the local variables in a function
type VariablesDict() =
    let scopeStack = new Stack<Dictionary<string, Expr>>()

    member this.enterScope(): unit =
        scopeStack.Push (new Dictionary<string, Expr>()) |> ignore

    member this.exitScope(): unit =
        // TODO: assert scopeStack is nonempty
        scopeStack.Pop () |> ignore

    member this.defineVar(name: string, value: Expr): unit =
        // TODO: assert variable is undefined
        scopeStack.Peek().[name] <- value

    member this.setVar(name: string, value: Expr): unit =
        // TODO: assert variable is defined, is same type
        (scopeStack |> Seq.find (fun x -> x.ContainsKey name)).[name] <- value

    member this.getVar(name: string): Expr option =
        match scopeStack |> Seq.tryFind (fun x -> x.ContainsKey name) with
        | Some dict -> Some dict.[name]
        | None -> None

    override this.ToString(): string =
        "[" + String.Join("\n", scopeStack |> Seq.map (fun x ->
            "{" + String.Join(", ", x |> Seq.map (fun y -> y.Key + "=" + y.Value.ToString())) + "}"
        )) + "]"

        
/// Represents a (possibly-nested) tuple of strings.
/// Used as a way to compare QsTuples and SymbolTuples.
type StringTuple =
| SingleItem of string
| MultipleItems of seq<StringTuple>
        
    static member fromQsTuple (x: QsTuple<LocalVariableDeclaration<QsLocalSymbol>>): StringTuple =
        match x with
        | QsTupleItem item -> match item.VariableName with
            | ValidName n -> SingleItem n.Value
            | InvalidName -> SingleItem "__invalid__"
        | QsTuple items -> MultipleItems (Seq.map StringTuple.fromQsTuple items)
            
    static member fromSymbolTuple (x: SymbolTuple): StringTuple =
        match x with
        | InvalidItem -> SingleItem "__invalid__"
        | VariableName n -> SingleItem n.Value
        | VariableNameTuple items -> MultipleItems (Seq.map StringTuple.fromSymbolTuple items)
        | DiscardedItem -> SingleItem "__discarded__"

    override this.ToString() =
        match this with
        | SingleItem item -> item
        | MultipleItems items -> "(" + String.Join(", ", items) + ")"
        
        
/// Modifies the VariablesDict by setting the given argument tuple to the given values
let rec fillVars (vars: VariablesDict) (argTuple: StringTuple, arg: Expr): unit =
    match argTuple with
    | SingleItem item -> vars.defineVar(item, arg)
    | MultipleItems items -> match arg with
        | ValueTuple vt -> vt |> Seq.map (fun x -> x.Expression) |>
            Seq.zip items |> Seq.map (fillVars vars) |> List.ofSeq |> ignore
        | _ -> Seq.zip items [arg] |> Seq.map (fillVars vars) |> List.ofSeq |> ignore


/// Returns a new array of the given type and length.
/// Returns None if the type doesn't have a default value.
let rec constructNewArray (bt: TypeKind) (length: int): Expr option =
    match defaultValue bt with
    | Some x -> ImmutableArray.CreateRange (List.replicate length (wrapExpr x bt)) |> ValueArray |> Some
    | None -> None

/// Returns the default value for a given type (from Q# documentation)
and defaultValue (bt: TypeKind): Expr option =
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

/// Wraps a QsExpressionType in a basic TypedExpression
and wrapExpr (expr: Expr) (bt: TypeKind): TypedExpression =
    let ii = {IsMutable=false; HasLocalQuantumDependency=false}
    TypedExpression.New (expr, ImmutableDictionary.Empty, ResolvedType.New bt, ii, Null)


/// Counts the number of MissingExprs in the given tuple
let rec countMissingExprs (expr: TypedExpression): int =
    match expr.Expression with
    | MissingExpr -> 1
    | ValueTuple vt -> Seq.map countMissingExprs vt |> Seq.sum
    | _ -> 0
    
/// Replaces any MissingExprs in the given tuple with the first elements of the given list.
/// Returns the new values of the partial-argument tuple and the argument list.
let rec fillPartialArg (partialArg: TypedExpression, arg: list<TypedExpression>): TypedExpression * list<TypedExpression> =
    match partialArg.Expression with
    | MissingExpr ->
        let first :: rest = arg
        first, rest
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
let partialApplyFunction (baseMethod: TypedExpression) (partialArg: TypedExpression) (arg: TypedExpression): Expr =
    let argsList =
        if countMissingExprs partialArg = 1 then [arg]
        else match arg.Expression with
        | ValueTuple vt ->
            if countMissingExprs partialArg <> vt.Length
            then failwithf "Invalid number of arguments: %O doesn't match %O" (prettyPrint arg.Expression) (prettyPrint partialArg.Expression)
            else List.ofSeq vt
        | _ -> failwithf "Invalid arg: %O" (prettyPrint arg.Expression)
    CallLikeExpression (baseMethod, fst (fillPartialArg (partialArg, argsList)))
