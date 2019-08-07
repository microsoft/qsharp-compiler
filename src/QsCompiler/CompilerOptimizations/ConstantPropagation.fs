module Microsoft.Quantum.QsCompiler.CompilerOptimization.ConstantPropagation

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open Types
open Utils
open Evaluation
open OptimizingTransformation


let rec shouldPropagate callables expr =
    isLiteral callables expr ||
    match expr with
    | ValueTuple a | StringLiteral (_, a) | ValueArray a -> Seq.forall (fun x -> shouldPropagate callables x.Expression) a
    | RangeLiteral (a, b) | ArrayItem (a, b) -> shouldPropagate callables a.Expression && shouldPropagate callables b.Expression
    | NewArray (_, a) | UnwrapApplication a -> shouldPropagate callables a.Expression
    | Identifier _ -> true
    | CallLikeExpression ({Expression = Identifier (GlobalCallable qualName, _)}, b)
        when (getCallable callables qualName).Kind = TypeConstructor -> shouldPropagate callables b.Expression
    | CallLikeExpression (a, b) ->
        shouldPropagate callables a.Expression && shouldPropagate callables b.Expression &&
            TypedExpression.IsPartialApplication (CallLikeExpression (a, b))
    | _ -> false


/// The SyntaxTreeTransformation used to evaluate constants
type ConstantPropagator(compiledCallables) =
    inherit OptimizingTransformation()

    let callables = makeCallables compiledCallables
    
    let mutable constants = Constants []
    let mutable skipScope = false


    /// The ScopeTransformation used to evaluate constants
    override syntaxTree.Scope = { new ScopeTransformation() with

        override scope.Transform x =
            if skipScope then
                skipScope <- false
                base.Transform x
            else
                constants <- enterScope constants
                let result = base.Transform x
                constants <- exitScope constants
                result

        /// The ExpressionTransformation used to evaluate constant expressions
        override scope.Expression = upcast ExpressionEvaluator(callables, constants, 10)

        /// The StatementKindTransformation used to evaluate constants
        override scope.StatementKind = { new StatementKindTransformation() with 
            override so.ExpressionTransformation x = scope.Expression.Transform x
            override so.LocationTransformation x = scope.onLocation x
            override so.ScopeTransformation x = scope.Transform x
            override so.TypeTransformation x = scope.Expression.Type.Transform x

            override so.onVariableDeclaration stm =
                let lhs = so.onSymbolTuple stm.Lhs
                let rhs = so.ExpressionTransformation stm.Rhs
                if stm.Kind = ImmutableBinding then
                    constants <- defineVarTuple (shouldPropagate callables) constants (lhs, rhs.Expression)
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
                    x.Body |> QsScopeStatement.New |> QsScopeStatement
                | [], Null ->
                    QsScope.New ([], LocalDeclarations.New []) |> QsScopeStatement.New |> QsScopeStatement
                | _ ->
                    let cases = cbList |> Seq.map (fun (c, b) -> (Option.get c, b))
                    QsConditionalStatement.New (cases, newDefault) |> QsConditionalStatement

            override this.onRepeatStatement stm =
                constants <- enterScope constants
                skipScope <- true
                let result = base.onRepeatStatement stm
                constants <- exitScope constants
                result
        }
    }
