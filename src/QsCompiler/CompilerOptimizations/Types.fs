module Microsoft.Quantum.QsCompiler.CompilerOptimization.Types

open System
open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree


/// Shorthand for a QsExpressionKind
type internal Expr = QsExpressionKind<TypedExpression, Identifier, ResolvedType>
/// Shorthand for a QsTypeKind
type internal TypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>
/// Shorthand for a QsInitializerKind
type internal InitKind = QsInitializerKind<ResolvedInitializer, TypedExpression>


/// Represents the dictionary of all callables in the program
type internal Callables = Callables of Map<QsQualifiedName, QsCallable>

/// Makes an instance of Callables from the given dictionary
let internal makeCallables (compiledCallables: ImmutableDictionary<QsQualifiedName, QsCallable>) =
    compiledCallables |> Seq.map (function KeyValue(a, b) -> a, b) |> Map.ofSeq |> Callables

/// Gets the QsCallable with the given qualified name.
/// Throws an KeyNotFoundException if no such callable exists.
let internal getCallable callables qualName =
    match callables with Callables c -> c.[qualName]


/// Represents a map whose keys are local variables, with support for scopes.
type internal Constants<'T> = Constants of list<Map<string, 'T>>

/// Returns a Constants inside of a new scope
let internal enterScope constants =
    match constants with Constants c -> Constants (Map.empty :: c)

/// Returns a Constants outside of the current scope.
/// Throws an InvalidOperationException if the scope stack of the given state is empty.
let internal exitScope constants =
    match constants with
    | Constants (_ :: tail) -> Constants tail
    | Constants [] -> InvalidOperationException "No scope to exit" |> raise

/// Gets the value associated with the given variable.
/// Returns None if the given variable is undefined.
let internal tryGetVar constants name =
    match constants with Constants c -> List.tryPick (Map.tryFind name) c


/// Returns whether a given expression is a literal (and thus a constant)
let rec internal isLiteral (callables: Callables) (expr: Expr): bool =
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


/// If check(value) is true, returns a Constants with the given variable defined as the given value.
/// Otherwise, returns constants without any changes.
/// If the given variable is already defined, its name is shadowed in the current scope.
/// Throws an InvalidOperationException if there aren't any scopes on the stack.
let internal defineVar check constants (name, value) =
    if not (check value) then constants else
    match constants with
    | Constants (head :: tail) -> Constants (head.Add (name, value) :: tail)
    | Constants [] -> InvalidOperationException "No scope to define variables in" |> raise

/// If check(value) is true, returns a Constants with the given variable set to the given value.
/// Otherwise, returns constants without any changes.
/// Throws an ArgumentException if trying to set an undefined variable.
let internal setVar check constants (name, value) =
    if not (check value) then constants else
    match constants with
    Constants c ->
        match List.tryFindIndex (Map.containsKey name) c with
        | Some index ->
            let updateFunc = fun i -> if i = index then Map.add name value else id
            Constants (List.mapi updateFunc c)
        | None -> sprintf "Variable %s is undefined" name |> ArgumentException |> raise

/// Applies the given function op on a SymbolTuple, ValueTuple pair
let rec private onTuple op constants (names, values) =
    match names, values with
    | VariableName name, _ ->
        op constants (name.Value, values)
    | VariableNameTuple namesTuple, ValueTuple valuesTuple ->
        // TODO: assert items and vt are same length
        if namesTuple.Length <> valuesTuple.Length then
            ArgumentException "names and values have different lengths" |> raise
        Seq.zip namesTuple (Seq.map (fun x -> x.Expression) valuesTuple) |> Seq.fold (onTuple op) constants
    | _ -> constants

/// Returns a Constants<Expr> with the given variables defined as the given values
let internal defineVarTuple check = onTuple (defineVar check)

/// Returns a Constants<Expr> with the given variables set to the given values
let internal setVarTuple check = onTuple (setVar check)
