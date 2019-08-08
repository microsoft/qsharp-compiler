module Microsoft.Quantum.QsCompiler.CompilerOptimization.TransformationState

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Numerics
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree


/// Shorthand for a QsExpressionKind
type internal Expr = QsExpressionKind<TypedExpression, Identifier, ResolvedType>
/// Shorthand for a QsTypeKind
type internal TypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>
/// Shorthand for a QsInitializerKind
type internal InitKind = QsInitializerKind<ResolvedInitializer, TypedExpression>


/// Represents the current state of a constant propagation pass
type internal TransformationState = {
    compiledCallables: Map<QsQualifiedName, QsCallable>
    scopeStack: list<Map<string, Expr>>
    currentCallable: QsCallable option
}

let internal newState compiledCallables =
    {compiledCallables = compiledCallables; scopeStack = []; currentCallable = None}


let internal getCallable state qualName =
    state.compiledCallables.[qualName]


let internal enterScope state =
    {state with scopeStack = Map.empty :: state.scopeStack}

let internal exitScope state =
    match state.scopeStack with
    | _ :: tail -> {state with scopeStack = tail}
    | [] -> failwithf "No scope to exit"

let internal getVar state name =
    state.scopeStack |> List.tryPick (Map.tryFind name)
    

/// Returns whether a given expression is a literal (and thus a constant)
let rec internal isLiteral (state: TransformationState) (expr: Expr): bool =
    match expr with
    | UnitValue | IntLiteral _ | BigIntLiteral _ | DoubleLiteral _ | BoolLiteral _ | ResultLiteral _ | PauliLiteral _ -> true
    | ValueTuple a | StringLiteral (_, a) | ValueArray a -> Seq.forall (fun x -> isLiteral state x.Expression) a
    | RangeLiteral (a, b) -> isLiteral state a.Expression && isLiteral state b.Expression
    | NewArray (_, a) -> isLiteral state a.Expression
    | Identifier (GlobalCallable _, _) | MissingExpr -> true
    | CallLikeExpression ({Expression = Identifier (GlobalCallable qualName, _)}, b)
        when (getCallable state qualName).Kind = TypeConstructor -> isLiteral state b.Expression
    | CallLikeExpression (a, b) ->
        isLiteral state a.Expression && isLiteral state b.Expression &&
            TypedExpression.IsPartialApplication (CallLikeExpression (a, b))
    | _ -> false


let internal defineVar state (name, value) =
    if not (isLiteral state value) then state else
    // TODO: assert variable is undefined
    match state.scopeStack with
    | head :: tail ->
        let newHead = head.Add (name, value)
        {state with scopeStack = newHead :: tail}
    | [] -> failwithf "No scope to define variables in"
    
let internal setVar state (name, value) =
    if not (isLiteral state value) then state else
    // TODO: assert variable is defined, is same type
    match state.scopeStack |> List.tryFindIndex (Map.containsKey name) with
    | Some index ->
        let newScopeStack = state.scopeStack |> List.mapi (fun i -> if i = index then Map.add name value else id)
        {state with scopeStack = newScopeStack}
    | None -> failwithf "Variable %s is undefined" name

let rec private onTuple op state (names, values) =
    match names, values with
    | VariableName name, _ ->
        op state (name.Value, values)
    | VariableNameTuple namesTuple, ValueTuple valuesTuple ->
        // TODO: assert items and vt are same length
        Seq.zip namesTuple (Seq.map (fun x -> x.Expression) valuesTuple) |> Seq.fold (onTuple op) state
    | _ -> state

let internal defineVarTuple = onTuple defineVar

let internal setVarTuple = onTuple setVar

