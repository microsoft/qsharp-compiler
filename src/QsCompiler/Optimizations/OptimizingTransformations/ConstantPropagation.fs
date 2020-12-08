// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Experimental

open System.Collections.Generic
open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Experimental.Evaluation
open Microsoft.Quantum.QsCompiler.Experimental.Utils
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations


/// The SyntaxTreeTransformation used to evaluate constants
type ConstantPropagation private (_private_: string) =
    inherit TransformationBase()

    /// The current dictionary that maps variables to the values we substitute for them
    member val Constants = new Dictionary<string, TypedExpression>()

    new(callables) as this =
        new ConstantPropagation("_private_")
        then
            this.Namespaces <- new ConstantPropagationNamespaces(this)
            this.StatementKinds <- new ConstantPropagationStatementKinds(this, callables)

            this.Expressions <-
                (new ExpressionEvaluator(callables, this.Constants, 1000))
                    .Expressions

            this.Types <- new Core.TypeTransformation(this, Core.TransformationOptions.Disabled)

/// private helper class for ConstantPropagation
and private ConstantPropagationNamespaces(parent: ConstantPropagation) =
    inherit NamespaceTransformationBase(parent)

    override __.OnProvidedImplementation(argTuple, body) =
        parent.Constants.Clear()
        base.OnProvidedImplementation(argTuple, body)

/// private helper class for ConstantPropagation
and private ConstantPropagationStatementKinds(parent: ConstantPropagation, callables) =
    inherit Core.StatementKindTransformation(parent)

    /// Returns whether the given expression should be propagated as a constant.
    /// For a statement of the form "let x = [expr];", if shouldPropagate(expr) is true,
    /// then we should substitute x with [expr] wherever x occurs in future code.
    let rec shouldPropagate callables (expr: TypedExpression) =
        let folder ex sub =
            isLiteral callables ex
            || (match ex.Expression with
                | Identifier _
                | ArrayItem _
                | UnwrapApplication _
                | NamedItem _
                | ValueTuple _
                | ValueArray _
                | RangeLiteral _
                | NewArray _ -> true
                | CallLikeExpression ({ Expression = Identifier (GlobalCallable qualName, _) }, _) when (callables.[qualName])
                    .Kind = TypeConstructor -> true
                | a when TypedExpression.IsPartialApplication a -> true
                | _ -> false
                && Seq.forall id sub)

        expr.Fold folder

    override so.OnVariableDeclaration stm =
        let lhs = so.OnSymbolTuple stm.Lhs
        let rhs = so.Expressions.OnTypedExpression stm.Rhs

        if stm.Kind = ImmutableBinding
        then defineVarTuple (shouldPropagate callables) parent.Constants (lhs, rhs)

        QsBinding<TypedExpression>.New stm.Kind (lhs, rhs) |> QsVariableDeclaration

    override this.OnConditionalStatement stm =
        let cbList, cbListEnd =
            stm.ConditionalBlocks
            |> Seq.fold (fun s (cond, block) ->
                let newCond = this.Expressions.OnTypedExpression cond

                match newCond.Expression with
                | BoolLiteral true -> s @ [ Null, block ]
                | BoolLiteral false -> s
                | _ -> s @ [ Value cond, block ]) []
            |> List.ofSeq
            |> takeWhilePlus1 (fun (c, _) -> c <> Null)

        let newDefault =
            cbListEnd |> Option.map (snd >> Value) |? stm.Default

        let cbList =
            cbList |> List.map (fun (c, b) -> this.OnPositionedBlock(c, b))

        let newDefault =
            match newDefault with
            | Value x -> this.OnPositionedBlock(Null, x) |> snd |> Value
            | Null -> Null

        match cbList, newDefault with
        | [], Value x -> x.Body |> newScopeStatement
        | [], Null -> QsScope.New([], LocalDeclarations.New []) |> newScopeStatement
        | _ ->
            let invalidCondition () = failwith "missing condition"

            let cases =
                cbList |> Seq.map (fun (c, b) -> (c.ValueOrApply invalidCondition, b))

            QsConditionalStatement.New(cases, newDefault) |> QsConditionalStatement

    override this.OnQubitScope(stm: QsQubitScope) =
        let kind = stm.Kind
        let lhs = this.OnSymbolTuple stm.Binding.Lhs
        let rhs = this.OnQubitInitializer stm.Binding.Rhs

        jointFlatten (lhs, rhs)
        |> Seq.iter (fun (l, r) ->
            match l, r.Resolution with
            | VariableName name, QubitRegisterAllocation { Expression = IntLiteral num } ->
                let arrayIden =
                    Identifier(LocalVariable name, Null) |> wrapExpr (ArrayType(ResolvedType.New Qubit))

                let elemI =
                    fun i -> ArrayItem(arrayIden, IntLiteral(int64 i) |> wrapExpr Int)

                let expr =
                    Seq.init (safeCastInt64 num) (elemI >> wrapExpr Qubit)
                    |> ImmutableArray.CreateRange
                    |> ValueArray
                    |> wrapExpr (ArrayType(ResolvedType.New Qubit))

                defineVar (fun _ -> true) parent.Constants (name, expr)
            | _ -> ())

        let body = this.Statements.OnScope stm.Body

        QsQubitScope.New kind ((lhs, rhs), body) |> QsQubitScope
