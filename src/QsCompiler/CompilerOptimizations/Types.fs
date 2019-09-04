// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
type internal Callables (m: ImmutableDictionary<QsQualifiedName, QsCallable>) =

    /// Gets the QsCallable with the given qualified name.
    /// Throws an KeyNotFoundException if no such callable exists.
    member __.get qualName =
        m.[qualName]


/// Returns whether a given expression is a literal (and thus a constant)
let rec internal isLiteral (callables: Callables) (expr: TypedExpression): bool =
    expr |> TypedExpression.MapFold (fun ex -> ex.Expression) (fun sub ex ->
        match ex.Expression with
        | IntLiteral _ | BigIntLiteral _ | DoubleLiteral _ | BoolLiteral _ | ResultLiteral _ | PauliLiteral _ | StringLiteral _
        | UnitValue | MissingExpr | Identifier (GlobalCallable _, _)
        | ValueTuple _ | ValueArray _ | RangeLiteral _ | NewArray _ -> true
        | Identifier _ when ex.ResolvedType.Resolution = Qubit -> true
        | CallLikeExpression ({Expression = Identifier (GlobalCallable qualName, _)}, _)
            when (callables.get qualName).Kind = TypeConstructor -> true
        | a when TypedExpression.IsPartialApplication a -> true
        | _ -> false
        && Seq.forall id sub)


/// If check(value) is true, returns a Constants with the given variable defined as the given value.
/// Otherwise, returns constants without any changes.
/// If the given variable is already defined, its name is shadowed in the current scope.
/// Throws an InvalidOperationException if there aren't any scopes on the stack.
let internal defineVar check constants (name, value) =
    if not (check value) then constants else constants |> Map.add name value

/// Applies the given function op on a SymbolTuple, ValueTuple pair
let rec private onTuple op constants (names, values) =
    match names, values with
    | VariableName name, _ ->
        op constants (name.Value, values)
    | VariableNameTuple namesTuple, Tuple valuesTuple ->
        if namesTuple.Length <> valuesTuple.Length then
            ArgumentException "names and values have different lengths" |> raise
        Seq.zip namesTuple valuesTuple |> Seq.fold (onTuple op) constants
    | _ -> constants

/// Returns a Constants<Expr> with the given variables defined as the given values
let internal defineVarTuple check = onTuple (defineVar check)
