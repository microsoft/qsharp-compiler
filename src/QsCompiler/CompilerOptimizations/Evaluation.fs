// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.CompilerOptimization.Evaluation

open System
open System.Collections.Immutable
open System.Numerics
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open ComputationExpressions
open Utils
open Printer


/// Represents the internal state of a function evaluation.
/// The first element is a map that stores the current values of all the local variables.
/// The second element is a counter that stores the remaining number of statements we evaluate.
type private EvalState = Map<string, TypedExpression> * int

/// Represents any interrupt to the normal control flow of a function evaluation.
/// Includes return statements, errors, and (if they were added) break/continue statements.
type private FunctionInterrupt =
/// Represents the function invoking a return statement
| Returned of TypedExpression
/// Represents the function invoking a fail statement
| Failed of TypedExpression
/// We reached the limit of statement executions
| OutOfStatements
/// We were unable to evaluate the function for some reason
| CouldNotEvaluate of string

/// A shorthand for the specific Imperative type used by several functions in this file
type private Imp<'t> = Imperative<EvalState, 't, FunctionInterrupt>

/// Represents a computation that decreases the remaining statements counter by 1.
/// Yields an OutOfStatements interrupt if this decreases the remaining statements below 0.
let private incrementState: Imp<Unit> = imperative {
    let! vars, counter = getState
    if counter < 1 then yield OutOfStatements
    do! putState (vars, counter - 1)
}

/// Represents a computation that sets the given variables to the given values
let private setVars callables entry: Imp<Unit> = imperative {
    let! vars, counter = getState
    do! putState (defineVarTuple (isLiteral callables) vars entry, counter)
}


/// Evaluates functions by stepping through their code
type internal FunctionEvaluator(callables: Callables) =

    /// Casts a BoolLiteral to the corresponding bool
    let castToBool x: Imp<bool> = imperative {
        match x.Expression with
        | BoolLiteral b -> return b
        | _ -> yield CouldNotEvaluate ("Not a BoolLiteral: " + (printExpr x.Expression))
    }

    /// Evaluates and simplifies a single Q# expression
    member internal __.evaluateExpression expr: Imp<TypedExpression> = imperative {
        let! vars, counter = getState
        let result = ExpressionEvaluator(callables, vars, counter / 2).Transform expr
        if isLiteral callables result then return result
        else yield CouldNotEvaluate ("Not a literal: " + (printExpr result.Expression))
    }

    /// Evaluates a single Q# statement
    member private this.evaluateStatement (statement: QsStatement): Imp<Unit> = imperative {
        do! incrementState

        match statement.Statement with
        | QsExpressionStatement _ ->
            ()
        | QsReturnStatement expr ->
            let! value = this.evaluateExpression expr
            yield Returned value
        | QsFailStatement expr ->
            let! value = this.evaluateExpression expr
            yield Failed value
        | QsVariableDeclaration s ->
            let! value = this.evaluateExpression s.Rhs
            do! setVars callables (s.Lhs, value)
        | QsValueUpdate s ->
            match s.Lhs with
            | LocalVarTuple vt ->
                let! value = this.evaluateExpression s.Rhs
                do! setVars callables (vt, value)
            | _ -> yield CouldNotEvaluate ("Unknown LHS of value update statement: " + (printExpr s.Lhs.Expression))
        | QsConditionalStatement s ->
            let mutable evalElseCase = true
            for cond, block in s.ConditionalBlocks do
                let! value = this.evaluateExpression cond >>= castToBool
                if value then
                    do! this.evaluateScope block.Body
                    evalElseCase <- false
                    do! Break
            if evalElseCase then
                match s.Default with
                | Value block -> do! this.evaluateScope block.Body
                | _ -> ()
        | QsForStatement stmt ->
            let! iterExpr = this.evaluateExpression stmt.IterationValues
            let! iterSeq = imperative {
                match iterExpr.Expression with
                | RangeLiteral _ when isLiteral callables iterExpr ->
                    return rangeLiteralToSeq iterExpr.Expression |> Seq.map (IntLiteral >> wrapExpr Int)
                | ValueArray va ->
                    return va :> seq<_>
                | _ ->
                    yield CouldNotEvaluate ("Unknown IterationValue in for loop: " + (printExpr iterExpr.Expression))
            }
            for loopValue in iterSeq do
                do! setVars callables (fst stmt.LoopItem, loopValue)
                do! this.evaluateScope stmt.Body
        | QsWhileStatement stmt ->
            while this.evaluateExpression stmt.Condition >>= castToBool do
                do! this.evaluateScope stmt.Body
        | QsRepeatStatement stmt ->
            while true do
                do! this.evaluateScope stmt.RepeatBlock.Body
                let! value = this.evaluateExpression stmt.SuccessCondition >>= castToBool
                if value then do! Break
                do! this.evaluateScope stmt.FixupBlock.Body
        | QsQubitScope _ ->
            yield CouldNotEvaluate "Cannot allocate qubits in function"
        | QsConjugation _ ->
            yield CouldNotEvaluate "Cannot conjugate in function"
        | QsScopeStatement s ->
            do! this.evaluateScope s.Body
    }

    /// Evaluates a list of Q# statements
    member private this.evaluateScope (scope: QsScope): Imp<Unit> = imperative {
        for stmt in scope.Statements do
            do! this.evaluateStatement stmt
    }

    /// Evaluates the given Q# function on the given argument.
    /// Returns Some ([expr]) if we successfully evaluate the function as [expr].
    /// Returns None if we were unable to evaluate the function.
    /// Throws an ArgumentException if the input is not a function, or if the function is invalid.
    member internal this.evaluateFunction (name: QsQualifiedName) (arg: TypedExpression) (types: QsNullable<ImmutableArray<ResolvedType>>) (stmtsLeft: int): TypedExpression option =
        let callable = callables.get name
        if callable.Kind = Operation then
            ArgumentException "Input is not a function" |> raise
        if callable.Specializations.Length <> 1 then
            ArgumentException "Functions must have exactly one specialization" |> raise
        let impl = (Seq.exactlyOne callable.Specializations).Implementation
        match impl with
        | Provided (specArgs, scope) ->
            let vars = defineVarTuple (isLiteral callables) Map.empty (toSymbolTuple specArgs, arg)
            match this.evaluateScope scope (vars, stmtsLeft) with
            | Normal _ ->
                // printfn "Function %O didn't return anything" name.Name.Value
                None
            | Break _ ->
                // printfn "Function %O ended with a break statement" name.Name.Value
                None
            | Interrupt (Returned expr) ->
                // printfn "Function %O returned %O" name.Name.Value (prettyPrint expr)
                Some expr
            | Interrupt (Failed expr) ->
                // printfn "Function %O failed with %O" name.Name.Value (prettyPrint expr)
                None
            | Interrupt OutOfStatements ->
                // printfn "Function %O ran out of statements" name.Name.Value
                None
            | Interrupt (CouldNotEvaluate reason) ->
                // printfn "Could not evaluate function %O on arg %O for reason %O" name.Name.Value (prettyPrint arg.Expression) reason
                None
        | _ ->
            // printfn "Implementation not provided for: %O" name
            None


/// The ExpressionTransformation used to evaluate constant expressions
and internal ExpressionEvaluator(callables: Callables, constants: Map<string, TypedExpression>, stmtsLeft: int) =
    inherit ExpressionTransformation()

    override this.Kind = upcast { new ExpressionKindEvaluator(callables, constants, stmtsLeft) with
        override __.ExpressionTransformation x = this.Transform x
        override __.TypeTransformation x = this.Type.Transform x }


/// The ExpressionKindTransformation used to evaluate constant expressions
and [<AbstractClass>] private ExpressionKindEvaluator(callables: Callables, constants: Map<string, TypedExpression>, stmtsLeft: int) =
    inherit ExpressionKindTransformation()

    member private this.simplify e1 = this.ExpressionTransformation e1

    member private this.simplify (e1, e2) =
        (this.ExpressionTransformation e1, this.ExpressionTransformation e2)

    member private this.simplify (e1, e2, e3) =
        (this.ExpressionTransformation e1, this.ExpressionTransformation e2, this.ExpressionTransformation e3)

    member private this.arithBoolBinaryOp qop bigIntOp doubleOp intOp lhs rhs =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BoolLiteral (bigIntOp a b)
        | DoubleLiteral a, DoubleLiteral b -> BoolLiteral (doubleOp a b)
        | IntLiteral a, IntLiteral b -> BoolLiteral (intOp a b)
        | _ -> qop (lhs, rhs)

    member private this.arithNumBinaryOp qop bigIntOp doubleOp intOp lhs rhs =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BigIntLiteral (bigIntOp a b)
        | DoubleLiteral a, DoubleLiteral b -> DoubleLiteral (doubleOp a b)
        | IntLiteral a, IntLiteral b -> IntLiteral (intOp a b)
        | _ -> qop (lhs, rhs)

    member private this.intBinaryOp qop bigIntOp intOp lhs rhs =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BigIntLiteral (bigIntOp a b)
        | IntLiteral a, IntLiteral b -> IntLiteral (intOp a b)
        | _ -> qop (lhs, rhs)

    override this.onIdentifier (sym, tArgs) =
        match sym with
        | LocalVariable name -> Map.tryFind name.Value constants |> Option.map (fun x -> x.Expression) |? Identifier (sym, tArgs)
        | _ -> Identifier (sym, tArgs)

    override this.onFunctionCall (method, arg) =
        let method, arg = this.simplify (method, arg)
        maybe {
            match method.Expression with
            | Identifier (GlobalCallable qualName, types) ->
                do! check (stmtsLeft > 0 && isLiteral callables arg)
                let fe = FunctionEvaluator (callables)
                return! fe.evaluateFunction qualName arg types stmtsLeft |> Option.map (fun x -> x.Expression)
            | CallLikeExpression (baseMethod, partialArg) ->
                do! check (TypedExpression.ContainsMissing partialArg)
                return this.Transform (CallLikeExpression (baseMethod, fillPartialArg (partialArg, arg)))
            | _ -> return! None
        } |? CallLikeExpression (method, arg)

    override this.onOperationCall (method, arg) =
        let method, arg = this.simplify (method, arg)
        maybe {
            match method.Expression with
            | CallLikeExpression (baseMethod, partialArg) ->
                do! check (TypedExpression.ContainsMissing partialArg)
                return this.Transform (CallLikeExpression (baseMethod, fillPartialArg (partialArg, arg)))
            | _ -> return! None
        } |? CallLikeExpression (method, arg)

    override this.onPartialApplication (method, arg) =
        let method, arg = this.simplify (method, arg)
        maybe {
            match method.Expression with
            | CallLikeExpression (baseMethod, partialArg) ->
                do! check (TypedExpression.ContainsMissing partialArg)
                return this.Transform (CallLikeExpression (baseMethod, fillPartialArg (partialArg, arg)))
            | _ -> return! None
        } |? CallLikeExpression (method, arg)

    override this.onUnwrapApplication ex =
        let ex = this.simplify ex
        match ex.Expression with
        | CallLikeExpression ({Expression = Identifier (GlobalCallable qualName, types)}, arg)
            when (callables.get qualName).Kind = TypeConstructor ->
                // TODO - must be adapted if we want to support user-defined type constructors
                QsCompilerError.Verify (
                    (callables.get qualName).Specializations.Length = 1,
                    "Type constructors should have exactly one specialization")
                QsCompilerError.Verify (
                    (callables.get qualName).Specializations.[0].Implementation = Intrinsic,
                    "Type constructors should be implicit")
                arg.Expression
        | _ -> UnwrapApplication ex

    override this.onArrayItem (arr, idx) =
        let arr, idx = this.simplify (arr, idx)
        match arr.Expression, idx.Expression with
        | ValueArray va, IntLiteral i -> va.[safeCastInt64 i].Expression
        | ValueArray va, RangeLiteral _ when isLiteral callables idx ->
            rangeLiteralToSeq idx.Expression |> Seq.map (fun i -> va.[safeCastInt64 i]) |> ImmutableArray.CreateRange |> ValueArray
        | _ -> ArrayItem (arr, idx)

    override this.onNewArray (bt, idx) =
        let idx = this.simplify idx
        match idx.Expression with
        | IntLiteral i -> constructNewArray bt.Resolution (safeCastInt64 i) |? NewArray (bt, idx)
        | _ -> NewArray (bt, idx)

    override this.onCopyAndUpdateExpression (lhs, accEx, rhs) =
        let lhs, accEx, rhs = this.simplify (lhs, accEx, rhs)
        match lhs.Expression, accEx.Expression, rhs.Expression with
        | ValueArray va, IntLiteral i, _ -> ValueArray (va.SetItem(safeCastInt64 i, rhs))
        | ValueArray va, RangeLiteral _, ValueArray vb when isLiteral callables accEx ->
            rangeLiteralToSeq accEx.Expression |> Seq.map safeCastInt64 |> Seq.indexed
            |> (va |> Seq.fold (fun st (i1, i2) -> st.SetItem(i2, vb.[i1]))) |> ValueArray
        // TODO - handle named items in user-defined types
        | _ -> CopyAndUpdate (lhs, accEx, rhs)

    override this.onConditionalExpression (e1, e2, e3) =
        let e1 = this.simplify e1
        match e1.Expression with
        | BoolLiteral a -> if a then (this.simplify e2).Expression else (this.simplify e3).Expression
        | _ -> CONDITIONAL (e1, this.simplify e2, this.simplify e3)

    override this.onEquality (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match isLiteral callables lhs && isLiteral callables rhs with
        | true -> BoolLiteral (lhs.Expression = rhs.Expression)
        | false -> EQ (lhs, rhs)

    override this.onInequality (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match isLiteral callables lhs && isLiteral callables rhs with
        | true -> BoolLiteral (lhs.Expression <> rhs.Expression)
        | false -> NEQ (lhs, rhs)

    override this.onLessThan (lhs, rhs) =
        this.arithBoolBinaryOp LT (<) (<) (<) lhs rhs

    override this.onLessThanOrEqual (lhs, rhs) =
        this.arithBoolBinaryOp LTE (<=) (<=) (<=) lhs rhs

    override this.onGreaterThan (lhs, rhs) =
        this.arithBoolBinaryOp GT (>) (>) (>) lhs rhs

    override this.onGreaterThanOrEqual (lhs, rhs) =
        this.arithBoolBinaryOp GTE (>=) (>=) (>=) lhs rhs

    override this.onLogicalAnd (lhs, rhs) =
        let lhs = this.simplify lhs
        match lhs.Expression with
        | BoolLiteral true -> (this.simplify rhs).Expression
        | BoolLiteral false -> BoolLiteral false
        | _ -> AND (lhs, this.simplify rhs)

    override this.onLogicalOr (lhs, rhs) =
        let lhs = this.simplify lhs
        match lhs.Expression with
        | BoolLiteral true -> BoolLiteral true
        | BoolLiteral false -> (this.simplify rhs).Expression
        | _ -> OR (lhs, this.simplify rhs)

    override this.onAddition (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BigIntLiteral (a + b)
        | DoubleLiteral a, DoubleLiteral b -> DoubleLiteral (a + b)
        | IntLiteral a, IntLiteral b -> IntLiteral (a + b)
        | ValueArray a, ValueArray b -> ValueArray (a.AddRange b)
        | StringLiteral (a, a2), StringLiteral (b, b2) when a2.Length = 0 || b2.Length = 0 ->
            StringLiteral (NonNullable<_>.New (a.Value + b.Value), a2.AddRange b2)
        | _ -> ADD (lhs, rhs)

    override this.onSubtraction (lhs, rhs) =
        this.arithNumBinaryOp SUB (-) (-) (-) lhs rhs

    override this.onMultiplication (lhs, rhs) =
        this.arithNumBinaryOp MUL (*) (*) (*) lhs rhs

    override this.onDivision (lhs, rhs) =
        this.arithNumBinaryOp DIV (/) (/) (/) lhs rhs

    override this.onExponentiate (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, IntLiteral b -> BigIntLiteral (BigInteger.Pow(a, safeCastInt64 b))
        | DoubleLiteral a, DoubleLiteral b -> DoubleLiteral (Math.Pow(a, b))
        | IntLiteral a, IntLiteral b -> IntLiteral (longPow a b)
        | _ -> POW (lhs, rhs)

    override this.onModulo (lhs, rhs) =
        this.intBinaryOp MOD (%) (%) lhs rhs

    override this.onLeftShift (lhs, rhs) =
        this.intBinaryOp LSHIFT (fun l r -> l <<< safeCastBigInt r) (fun l r -> l <<< safeCastInt64 r) lhs rhs

    override this.onRightShift (lhs, rhs) =
        this.intBinaryOp RSHIFT (fun l r -> l >>> safeCastBigInt r) (fun l r -> l >>> safeCastInt64 r) lhs rhs

    override this.onBitwiseExclusiveOr (lhs, rhs) =
        this.intBinaryOp BXOR (^^^) (^^^) lhs rhs

    override this.onBitwiseOr (lhs, rhs) =
        this.intBinaryOp BOR (|||) (|||) lhs rhs

    override this.onBitwiseAnd (lhs, rhs) =
        this.intBinaryOp BAND (&&&) (&&&) lhs rhs

    override this.onLogicalNot expr =
        let expr = this.simplify expr
        match expr.Expression with
        | BoolLiteral a -> BoolLiteral (not a)
        | _ -> NOT expr

    override this.onNegative expr =
        let expr = this.simplify expr
        match expr.Expression with
        | BigIntLiteral a -> BigIntLiteral (-a)
        | DoubleLiteral a -> DoubleLiteral (-a)
        | IntLiteral a -> IntLiteral (-a)
        | _ -> NEG expr

    override this.onBitwiseNot expr =
        let expr = this.simplify expr
        match expr.Expression with
        | IntLiteral a -> IntLiteral (~~~a)
        | _ -> BNOT expr
