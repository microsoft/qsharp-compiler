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

/// Creates a new TransformationState with the given compiledCallables
let internal newState compiledCallables =
    {compiledCallables = compiledCallables; scopeStack = []; currentCallable = None}


/// Gets the QsCallable with the given qualified name
let internal getCallable state qualName =
    state.compiledCallables.[qualName]


/// Returns a TransformationState inside of a new scope
let internal enterScope state =
    {state with scopeStack = Map.empty :: state.scopeStack}

/// Returns a TransformationState outside of the current scope.
/// Throws an InvalidOperationException if the scope stack of the given state is empty. 
let internal exitScope state =
    match state.scopeStack with
    | _ :: tail -> {state with scopeStack = tail}
    | [] -> InvalidOperationException "No scope to exit" |> raise

/// Gets the current value of the given variable
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
        when (getCallable state qualName).Kind = TypeConstructor -> 
        QsCompilerError.Verify (
            (cd.getCallable qualName).Specializations.Length = 1,
            "Type constructors should have exactly one specialization")
        QsCompilerError.Verify (
            (cd.getCallable qualName).Specializations.[0].Implementation = Intrinsic,
            "Type constructors should be implicit")        
        isLiteral state b.Expression
    | CallLikeExpression (a, b) ->
        isLiteral state a.Expression && isLiteral state b.Expression &&
            TypedExpression.IsPartialApplication (CallLikeExpression (a, b))
    | _ -> false


/// Returns a TransformationState with the given variable defined as the given value
let internal defineVar state (name, value) =
    if not (isLiteral state value) then state else
    // TODO: assert variable is undefined
    match state.scopeStack with
    | head :: tail ->
        let newHead = head.Add (name, value)
        {state with scopeStack = newHead :: tail}
    | [] -> InvalidOperationException "No scope to define variables in" |> raise

/// Returns a TransformationState with the given variable set to the given value.
/// Throws an ArgumentException if no variable with the given name is defined in the given state. 
let internal setVar state (name, value) =
    if not (isLiteral state value) then state else
    // TODO: assert variable is defined, is same type
    match state.scopeStack |> List.tryFindIndex (Map.containsKey name) with
    | Some index ->
        let newScopeStack = state.scopeStack |> List.mapi (fun i -> if i = index then Map.add name value else id)
        {state with scopeStack = newScopeStack}
    | None -> failwithf "Variable %s is undefined" name

/// Applies the given function op on a SymbolTuple, ValueTuple pair
let rec private onTuple op state (names, values) =
    match names, values with
    | VariableName name, _ ->
        op state (name.Value, values)
    | VariableNameTuple namesTuple, ValueTuple valuesTuple ->
        // TODO: assert items and vt are same length
        Seq.zip namesTuple (Seq.map (fun x -> x.Expression) valuesTuple) |> Seq.fold (onTuple op) state
    | _ -> state

/// Returns a TransformationState with the given variables defined as the given values
let internal defineVarTuple = onTuple defineVar

/// Returns a TransformationState with the given variables set to the given values
let internal setVarTuple = onTuple setVar

