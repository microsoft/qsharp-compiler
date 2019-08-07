module Microsoft.Quantum.QsCompiler.CompilerOptimization.Types

open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree


/// Shorthand for a QsExpressionKind
type Expr = QsExpressionKind<TypedExpression, Identifier, ResolvedType>
/// Shorthand for a QsTypeKind
type TypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>
/// Shorthand for a QsInitializerKind
type InitKind = QsInitializerKind<ResolvedInitializer, TypedExpression>


/// Represents the dictionary of all callables in the program
type Callables = Callables of Map<QsQualifiedName, QsCallable>

let makeCallables compiledCallables =
    compiledCallables |> Seq.map (function KeyValue(a, b) -> a, b) |> Map.ofSeq |> Callables
    
let getCallable callables qualName =
    match callables with Callables c -> c.[qualName]


/// Represents the current state of a constant propagation pass
type Constants<'T> = Constants of list<Map<string, 'T>>

let enterScope constants =
    match constants with Constants c -> Constants (Map.empty :: c)

let exitScope constants =
    match constants with
    | Constants (_ :: tail) -> Constants tail
    | Constants [] -> failwithf "No scope to exit"

let getVar constants name =
    match constants with Constants c -> List.tryPick (Map.tryFind name) c


/// Returns whether a given expression is a literal (and thus a constant)
let rec isLiteral (callables: Callables) (expr: Expr): bool =
    match expr with
    | UnitValue | IntLiteral _ | BigIntLiteral _ | DoubleLiteral _ | BoolLiteral _ | ResultLiteral _ | PauliLiteral _ -> true
    | ValueTuple a | StringLiteral (_, a) | ValueArray a -> Seq.forall (fun x -> isLiteral callables x.Expression) a
    | RangeLiteral (a, b) -> isLiteral callables a.Expression && isLiteral callables b.Expression
    | NewArray (_, a) -> isLiteral callables a.Expression
    | Identifier (GlobalCallable _, _) | MissingExpr -> true
    | CallLikeExpression ({Expression = Identifier (GlobalCallable qualName, _)}, b)
        when (getCallable callables qualName).Kind = TypeConstructor -> isLiteral callables b.Expression
    | CallLikeExpression (a, b) ->
        isLiteral callables a.Expression && isLiteral callables b.Expression &&
            TypedExpression.IsPartialApplication (CallLikeExpression (a, b))
    | _ -> false


let defineVar check constants (name, value) =
    if not (check value) then constants else
    // TODO: assert variable is undefined
    match constants with
    | Constants (head :: tail) -> Constants (head.Add (name, value) :: tail)
    | Constants [] -> failwithf "No scope to define variables in"
    
let setVar check constants (name, value) =
    if not (check value) then constants else
    // TODO: assert variable is defined, is same type
    match constants with
    Constants c ->
        match List.tryFindIndex (Map.containsKey name) c with
        | Some index ->
            let updateFunc = fun i -> if i = index then Map.add name value else id
            Constants (List.mapi updateFunc c)
        | None -> failwithf "Variable %s is undefined" name

let rec private onTuple op check constants (names, values) =
    match names, values with
    | VariableName name, _ ->
        op check constants (name.Value, values)
    | VariableNameTuple namesTuple, ValueTuple valuesTuple ->
        // TODO: assert items and vt are same length
        Seq.zip namesTuple (Seq.map (fun x -> x.Expression) valuesTuple) |> Seq.fold (onTuple op check) constants
    | _ -> constants

let defineVarTuple = onTuple defineVar

let setVarTuple = onTuple setVar

