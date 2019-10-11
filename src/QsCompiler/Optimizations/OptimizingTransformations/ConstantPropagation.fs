// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Optimizations.ConstantPropagation

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Optimizations.Evaluation
open Microsoft.Quantum.QsCompiler.Optimizations.MinorTransformations
open Microsoft.Quantum.QsCompiler.Optimizations.Utils
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core


/// Returns whether the given expression should be propagated as a constant.
/// For a statement of the form "let x = [expr];", if shouldPropagate(expr) is true,
/// then we should substitute [expr] for x wherever x occurs in future code.
let rec private shouldPropagate callables (expr : TypedExpression) =
    let folder ex sub = 
        isLiteral callables ex ||
        (match ex.Expression with
        | Identifier _ | ArrayItem _ | UnwrapApplication _ | NamedItem _
        | ValueTuple _ | ValueArray _ | RangeLiteral _ | NewArray _ -> true
        | CallLikeExpression ({Expression = Identifier (GlobalCallable qualName, _)}, _)
            when (callables.get qualName).Kind = TypeConstructor -> true
        | a when TypedExpression.IsPartialApplication a -> true
        | _ -> false
        && Seq.forall id sub)
    expr.Fold folder


/// The SyntaxTreeTransformation used to evaluate constants
type internal ConstantPropagator(callables) =
    inherit OptimizingTransformation()

    /// The current dictionary that maps variables to the values we substitute for them
    let mutable constants = Map.empty


    override __.onProvidedImplementation (argTuple, body) =
        constants <- Map.empty
        base.onProvidedImplementation (argTuple, body)

    /// The ScopeTransformation used to evaluate constants
    override __.Scope = { new ScopeTransformation() with

        /// The ExpressionTransformation used to evaluate constant expressions
        override __.Expression = upcast ExpressionEvaluator(callables, constants, 1000)

        /// The StatementKindTransformation used to evaluate constants
        override scope.StatementKind = { new StatementKindTransformation() with
            override __.ExpressionTransformation x = scope.Expression.Transform x
            override __.LocationTransformation x = x
            override __.ScopeTransformation x = scope.Transform x
            override __.TypeTransformation x = x

            override so.onVariableDeclaration stm =
                let lhs = so.onSymbolTuple stm.Lhs
                let rhs = so.ExpressionTransformation stm.Rhs
                if stm.Kind = ImmutableBinding then
                    constants <- defineVarTuple (shouldPropagate callables) constants (lhs, rhs)
                QsBinding<TypedExpression>.New stm.Kind (lhs, rhs) |> QsVariableDeclaration

            override this.onConditionalStatement stm =
                let cbList, cbListEnd =
                    stm.ConditionalBlocks |> Seq.fold (fun s (cond, block) ->
                        let newCond = this.ExpressionTransformation cond
                        match newCond.Expression with
                        | BoolLiteral true -> s @ [None, block]
                        | BoolLiteral false -> s
                        | _ -> s @ [Some cond, block]
                    ) [] |> List.ofSeq |> takeWhilePlus1 (fun (c, _) -> c <> None)
                let newDefault = cbListEnd |> Option.map (snd >> Value) |? stm.Default

                let cbList = cbList |> List.map (fun (c, b) -> this.onPositionedBlock (c, b))
                let newDefault = match newDefault with Value x -> this.onPositionedBlock (None, x) |> snd |> Value | Null -> Null

                match cbList, newDefault with
                | [], Value x ->
                    x.Body |> newScopeStatement
                | [], Null ->
                    QsScope.New ([], LocalDeclarations.New []) |> newScopeStatement
                | _ ->
                    let cases = cbList |> Seq.map (fun (c, b) -> (Option.get c, b))
                    QsConditionalStatement.New (cases, newDefault) |> QsConditionalStatement

            override this.onQubitScope (stm : QsQubitScope) =
                let kind = stm.Kind
                let lhs = this.onSymbolTuple stm.Binding.Lhs
                let rhs = this.onQubitInitializer stm.Binding.Rhs

                jointFlatten (lhs, rhs) |> Seq.iter (fun (l, r) ->
                    match l, r.Resolution with
                    | VariableName name, QubitRegisterAllocation {Expression = IntLiteral num} ->
                        let arrayIden = Identifier (LocalVariable name, Null) |> wrapExpr (ArrayType (ResolvedType.New Qubit))
                        let elemI = fun i -> ArrayItem (arrayIden, IntLiteral (int64 i) |> wrapExpr Int)
                        let expr = Seq.init (safeCastInt64 num) (elemI >> wrapExpr Qubit) |> ImmutableArray.CreateRange |> ValueArray |> wrapExpr (ArrayType (ResolvedType.New Qubit))
                        constants <- defineVar (fun _ -> true) constants (name.Value, expr)
                    | _ -> ())

                let body = this.ScopeTransformation stm.Body
                QsQubitScope.New kind ((lhs, rhs), body) |> QsQubitScope
        }
    }
