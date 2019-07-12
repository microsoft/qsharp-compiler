module Utils

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Numerics
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree


type Expr = QsExpressionKind<TypedExpression, Identifier, ResolvedType>
type TypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>


let rec isLiteral (expr: Expr): bool =
    match expr with
    | UnitValue | IntLiteral _ | BigIntLiteral _ | DoubleLiteral _ | BoolLiteral _ | ResultLiteral _ | PauliLiteral _ -> true
    | ValueTuple a | StringLiteral (_, a) | ValueArray a -> Seq.forall (fun x -> isLiteral x.Expression) a
    | RangeLiteral (a, b) -> isLiteral a.Expression && isLiteral b.Expression
    | NewArray (_, a) -> isLiteral a.Expression
    | _ -> false


let rec prettyPrint (expr: Expr): string =
    match expr with
    | Identifier (LocalVariable a, _) -> a.Value
    | Identifier (GlobalCallable a, _) -> a.Name.Value
    | CallLikeExpression (f, x) -> (prettyPrint f.Expression) + "(" + (prettyPrint x.Expression) + ")"
    | ValueTuple a -> "(" + String.Join(", ", a |> Seq.map (fun x -> x.Expression) |> Seq.map prettyPrint) + ")"
    | a -> a.ToString()


let rangeLiteralToSeq (r: Expr): seq<int> =
    match r with
    | RangeLiteral (a, b) ->
        match a.Expression, b.Expression with
        | IntLiteral start, IntLiteral stop -> seq { int start .. int stop }
        | RangeLiteral (c, d), IntLiteral stop ->
            match c.Expression, d.Expression with
            | IntLiteral start, IntLiteral step -> seq { int start .. int step .. int stop }
            | _ -> failwithf "Invalid range literal: %O" r
        | _ -> failwithf "Invalid range literal: %O" r
    | _ -> failwithf "Not a range literal: %O" r


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
        
        
let rec fillVars (vars: VariablesDict) (argTuple: StringTuple, arg: Expr): unit =
    match argTuple with
    | SingleItem item -> vars.defineVar(item, arg)
    | MultipleItems items -> match arg with
        | ValueTuple vt -> vt |> Seq.map (fun x -> x.Expression) |>
            Seq.zip items |> Seq.map (fillVars vars) |> List.ofSeq |> ignore
        | _ -> Seq.zip items [arg] |> Seq.map (fillVars vars) |> List.ofSeq |> ignore



let rec constructNewArray (bt: TypeKind) (length: int): Expr option =
    match defaultValue bt with
    | Some x -> ImmutableArray.CreateRange (List.replicate length (wrapExpr x bt)) |> ValueArray |> Some
    | None -> None

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

and wrapExpr (expr: Expr) (bt: TypeKind): TypedExpression =
    let ii = {IsMutable=false; HasLocalQuantumDependency=false}
    TypedExpression.New (expr, ImmutableDictionary.Empty, ResolvedType.New bt, ii, Null)


