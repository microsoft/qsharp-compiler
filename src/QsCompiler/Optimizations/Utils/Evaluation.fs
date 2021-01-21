// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module internal Microsoft.Quantum.QsCompiler.Experimental.Evaluation

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Numerics
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.ComputationExpressions
open Microsoft.Quantum.QsCompiler.Experimental.Utils
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core


/// Represents the internal state of a function evaluation.
/// The first element is a map that stores the current values of all the local variables.
/// The second element is a counter that stores the remaining number of statements we evaluate.
type private EvalState = Dictionary<string, TypedExpression> * int

/// Represents any interrupt to the normal control flow of a function evaluation.
/// Includes return statements, errors, and (if they were added) break/continue statements.
type private FunctionInterrupt =
    /// Represents the function invoking a return statement
    | Returned of TypedExpression
    /// Represents the function invoking a fail statement
    | Failed of TypedExpression
    /// We reached the limit of statement executions
    | TooManyStatements
    /// We were unable to evaluate the function for some reason
    | CouldNotEvaluate of string

/// A shorthand for the specific Imperative type used by several functions in this file
type private Imp<'t> = Imperative<EvalState, 't, FunctionInterrupt>


/// Evaluates functions by stepping through their code
type internal FunctionEvaluator(callables: IDictionary<QsQualifiedName, QsCallable>) =

    /// Represents a computation that decreases the remaining statements counter by 1.
    /// Yields an OutOfStatements interrupt if this decreases the remaining statements below 0.
    let incrementState: Imp<Unit> =
        imperative {
            let! vars, counter = getState
            if counter < 1 then yield TooManyStatements
            do! putState (vars, counter - 1)
        }

    /// Represents a computation that sets the given variables to the given values
    let setVars callables entry: Imp<Unit> =
        imperative {
            let! vars, counter = getState
            defineVarTuple (isLiteral callables) vars entry
            do! putState (vars, counter)
        }

    /// Casts a BoolLiteral to the corresponding bool
    let castToBool x: bool =
        match x.Expression with
        | BoolLiteral b -> b
        | _ -> ArgumentException("Not a BoolLiteral: " + x.Expression.ToString()) |> raise

    /// Evaluates and simplifies a single Q# expression
    member private this.EvaluateExpression expr: Imp<TypedExpression> =
        imperative {
            let! vars, counter = getState
            let result = ExpressionEvaluator(callables, vars, counter / 2).Expressions.OnTypedExpression expr

            if isLiteral callables result
            then return result
            else yield CouldNotEvaluate("Not a literal: " + result.Expression.ToString())
        }

    /// Evaluates a single Q# statement
    member private this.EvaluateStatement(statement: QsStatement) =
        imperative {
            do! incrementState

            match statement.Statement with
            | QsExpressionStatement _ ->
                // We do nothing in this case because we're evaluating a function, and expression
                // statements inside functions never have side effects, so we can skip evaluating them.
                ()
            | QsReturnStatement expr ->
                let! value = this.EvaluateExpression expr
                yield Returned value
            | QsFailStatement expr ->
                let! value = this.EvaluateExpression expr
                yield Failed value
            | QsVariableDeclaration s ->
                let! value = this.EvaluateExpression s.Rhs
                do! setVars callables (s.Lhs, value)
            | QsValueUpdate s ->
                match s.Lhs with
                | LocalVarTuple vt ->
                    let! value = this.EvaluateExpression s.Rhs
                    do! setVars callables (vt, value)
                | _ -> yield CouldNotEvaluate("Unknown LHS of value update statement: " + s.Lhs.Expression.ToString())
            | QsConditionalStatement s ->
                let mutable evalElseCase = true

                for cond, block in s.ConditionalBlocks do
                    let! value = this.EvaluateExpression cond <&> castToBool

                    if value then
                        do! this.EvaluateScope block.Body
                        evalElseCase <- false
                        do! Break

                if evalElseCase then
                    match s.Default with
                    | Value block -> do! this.EvaluateScope block.Body
                    | _ -> ()
            | QsForStatement stmt ->
                let! iterExpr = this.EvaluateExpression stmt.IterationValues

                let! iterSeq =
                    imperative {
                        match iterExpr.Expression with
                        | RangeLiteral _ when isLiteral callables iterExpr ->
                            return rangeLiteralToSeq iterExpr.Expression |> Seq.map (IntLiteral >> wrapExpr Int)
                        | ValueArray va -> return va :> seq<_>
                        | _ ->
                            yield
                                CouldNotEvaluate
                                    ("Unknown IterationValue in for loop: " + iterExpr.Expression.ToString())
                    }

                for loopValue in iterSeq do
                    do! setVars callables (fst stmt.LoopItem, loopValue)
                    do! this.EvaluateScope stmt.Body
            | QsWhileStatement stmt ->
                while this.EvaluateExpression stmt.Condition <&> castToBool do
                    do! this.EvaluateScope stmt.Body
            | QsRepeatStatement stmt ->
                while true do
                    do! this.EvaluateScope stmt.RepeatBlock.Body
                    let! value = this.EvaluateExpression stmt.SuccessCondition <&> castToBool
                    if value then do! Break
                    do! this.EvaluateScope stmt.FixupBlock.Body
            | QsQubitScope _ -> yield CouldNotEvaluate "Cannot allocate qubits in function"
            | QsConjugation _ -> yield CouldNotEvaluate "Cannot conjugate in function"
            | EmptyStatement -> ()
        }

    /// Evaluates a list of Q# statements
    member private this.EvaluateScope(scope: QsScope) =
        imperative {
            for stmt in scope.Statements do
                do! this.EvaluateStatement stmt
        }

    /// <summary>
    /// Evaluates the given Q# function on the given argument.
    /// Returns Some ([expr]) if we successfully evaluate the function as [expr].
    /// Returns None if we were unable to evaluate the function.
    /// </summary>
    /// <exception cref="ArgumentException">The input is not a function, or the function is invalid.</exception>
    member internal this.EvaluateFunction (name: QsQualifiedName) (arg: TypedExpression) (stmtsLeft: int) =
        let callable = callables.[name]

        if callable.Kind = Operation
        then ArgumentException "Input is not a function" |> raise

        if callable.Specializations.Length <> 1
        then ArgumentException "Functions must have exactly one specialization" |> raise

        let impl = (Seq.exactlyOne callable.Specializations).Implementation

        match impl with
        | Provided (specArgs, scope) ->
            let vars = new Dictionary<_, _>()
            defineVarTuple (isLiteral callables) vars (toSymbolTuple specArgs, arg)

            match this.EvaluateScope scope (vars, stmtsLeft) with
            | Normal _ -> None
            | Break _ -> None
            | Interrupt (Returned expr) -> Some expr
            | Interrupt (Failed _) -> None
            | Interrupt TooManyStatements -> None
            | Interrupt (CouldNotEvaluate _) -> None
        | _ -> None


/// The ExpressionTransformation used to evaluate constant expressions
and internal ExpressionEvaluator private (_private_) =
    inherit SyntaxTreeTransformation()

    internal new(callables: IDictionary<QsQualifiedName, QsCallable>,
                 constants: IDictionary<string, TypedExpression>,
                 stmtsLeft: int) as this =
        new ExpressionEvaluator("_private_")
        then
            this.ExpressionKinds <- new ExpressionKindEvaluator(this, callables, constants, stmtsLeft)
            this.Types <- new TypeTransformation(this, TransformationOptions.Disabled)


/// The ExpressionKindTransformation used to evaluate constant expressions
and private ExpressionKindEvaluator(parent,
                                    callables: IDictionary<QsQualifiedName, QsCallable>,
                                    constants: IDictionary<string, TypedExpression>,
                                    stmtsLeft: int) =
    inherit ExpressionKindTransformation(parent)

    member private this.simplify e1 = this.Expressions.OnTypedExpression e1

    member private this.simplify(e1, e2) =
        (this.Expressions.OnTypedExpression e1, this.Expressions.OnTypedExpression e2)

    member private this.simplify(e1, e2, e3) =
        (this.Expressions.OnTypedExpression e1,
         this.Expressions.OnTypedExpression e2,
         this.Expressions.OnTypedExpression e3)

    member private this.arithBoolBinaryOp qop bigIntOp doubleOp intOp lhs rhs =
        let lhs, rhs = this.simplify (lhs, rhs)

        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BoolLiteral(bigIntOp a b)
        | DoubleLiteral a, DoubleLiteral b -> BoolLiteral(doubleOp a b)
        | IntLiteral a, IntLiteral b -> BoolLiteral(intOp a b)
        | _ -> qop (lhs, rhs)

    member private this.arithNumBinaryOp qop bigIntOp doubleOp intOp lhs rhs =
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BigIntLiteral(bigIntOp a b)
        | DoubleLiteral a, DoubleLiteral b -> DoubleLiteral(doubleOp a b)
        | IntLiteral a, IntLiteral b -> IntLiteral(intOp a b)
        | _ -> qop (lhs, rhs)

    member private this.intBinaryOp qop bigIntOp intOp lhs rhs =
        let lhs, rhs = this.simplify (lhs, rhs)

        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BigIntLiteral(bigIntOp a b)
        | IntLiteral a, IntLiteral b -> IntLiteral(intOp a b)
        | _ -> qop (lhs, rhs)

    override this.OnIdentifier(sym, tArgs) =
        match sym with
        | LocalVariable name ->
            match constants.TryGetValue name with
            | true, ex -> ex.Expression
            | _ -> Identifier(sym, tArgs)
        | _ -> Identifier(sym, tArgs)

    override this.OnFunctionCall(method, arg) =
        let method, arg = this.simplify (method, arg)

        maybe {
            match method.Expression with
            | Identifier (GlobalCallable qualName, _) ->
                do! check (stmtsLeft > 0 && isLiteral callables arg)
                let fe = FunctionEvaluator(callables)
                return! fe.EvaluateFunction qualName arg stmtsLeft |> Option.map (fun x -> x.Expression)
            | CallLikeExpression (baseMethod, partialArg) ->
                do! check (TypedExpression.IsPartialApplication method.Expression)
                return this.OnExpressionKind(CallLikeExpression(baseMethod, fillPartialArg (partialArg, arg)))
            | _ -> return! None
        }
        |? CallLikeExpression(method, arg)

    override this.OnOperationCall(method, arg) =
        let method, arg = this.simplify (method, arg)

        maybe {
            match method.Expression with
            | CallLikeExpression (baseMethod, partialArg) ->
                do! check (TypedExpression.IsPartialApplication method.Expression)
                return this.OnExpressionKind(CallLikeExpression(baseMethod, fillPartialArg (partialArg, arg)))
            | _ -> return! None
        }
        |? CallLikeExpression(method, arg)

    override this.OnPartialApplication(method, arg) =
        let method, arg = this.simplify (method, arg)

        maybe {
            match method.Expression with
            | CallLikeExpression (baseMethod, partialArg) ->
                do! check (TypedExpression.IsPartialApplication method.Expression)
                return this.OnExpressionKind(CallLikeExpression(baseMethod, fillPartialArg (partialArg, arg)))
            | _ -> return! None
        }
        |? CallLikeExpression(method, arg)

    override this.OnUnwrapApplication ex =
        let ex = this.simplify ex

        match ex.Expression with
        | CallLikeExpression ({ Expression = Identifier (GlobalCallable qualName, types) }, arg) when (callables.[qualName])
            .Kind = TypeConstructor ->
              // TODO - must be adapted if we want to support user-defined type constructors
              QsCompilerError.Verify
                  ((callables.[qualName]).Specializations.Length = 1,
                   "Type constructors should have exactly one specialization")

              QsCompilerError.Verify
                  ((callables.[qualName]).Specializations.[0].Implementation = Intrinsic,
                   "Type constructors should be implicit")

              arg.Expression
        | _ -> UnwrapApplication ex

    override this.OnArrayItem(arr, idx) =
        let arr, idx = this.simplify (arr, idx)

        match arr.Expression, idx.Expression with
        | ValueArray va, IntLiteral i -> va.[safeCastInt64 i].Expression
        | ValueArray va, RangeLiteral _ when isLiteral callables idx ->
            rangeLiteralToSeq idx.Expression
            |> Seq.map (fun i -> va.[safeCastInt64 i])
            |> ImmutableArray.CreateRange
            |> ValueArray
        | _ -> ArrayItem(arr, idx)

    override this.OnNewArray(bt, idx) =
        let idx = this.simplify idx

        match idx.Expression with
        | IntLiteral i -> constructNewArray bt.Resolution (safeCastInt64 i) |? NewArray(bt, idx)
        | _ -> NewArray(bt, idx)

    override this.OnCopyAndUpdateExpression(lhs, accEx, rhs) =
        let lhs, accEx, rhs = this.simplify (lhs, accEx, rhs)

        match lhs.Expression, accEx.Expression, rhs.Expression with
        | ValueArray va, IntLiteral i, _ -> ValueArray(va.SetItem(safeCastInt64 i, rhs))
        | ValueArray va, RangeLiteral _, ValueArray vb when isLiteral callables accEx ->
            rangeLiteralToSeq accEx.Expression
            |> Seq.map safeCastInt64
            |> Seq.indexed
            |> (va |> Seq.fold (fun st (i1, i2) -> st.SetItem(i2, vb.[i1])))
            |> ValueArray
        // TODO - handle named items in user-defined types
        | _ -> CopyAndUpdate(lhs, accEx, rhs)

    override this.OnConditionalExpression(e1, e2, e3) =
        let e1 = this.simplify e1

        match e1.Expression with
        | BoolLiteral a -> if a then (this.simplify e2).Expression else (this.simplify e3).Expression
        | _ -> CONDITIONAL(e1, this.simplify e2, this.simplify e3)

    override this.OnEquality(lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)

        match isLiteral callables lhs && isLiteral callables rhs with
        | true -> BoolLiteral(lhs.Expression = rhs.Expression)
        | false -> EQ(lhs, rhs)

    override this.OnInequality(lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)

        match isLiteral callables lhs && isLiteral callables rhs with
        | true -> BoolLiteral(lhs.Expression <> rhs.Expression)
        | false -> NEQ(lhs, rhs)

    override this.OnLessThan(lhs, rhs) =
        this.arithBoolBinaryOp LT (<) (<) (<) lhs rhs

    override this.OnLessThanOrEqual(lhs, rhs) =
        this.arithBoolBinaryOp LTE (<=) (<=) (<=) lhs rhs

    override this.OnGreaterThan(lhs, rhs) =
        this.arithBoolBinaryOp GT (>) (>) (>) lhs rhs

    override this.OnGreaterThanOrEqual(lhs, rhs) =
        this.arithBoolBinaryOp GTE (>=) (>=) (>=) lhs rhs

    override this.OnLogicalAnd(lhs, rhs) =
        let lhs = this.simplify lhs

        match lhs.Expression with
        | BoolLiteral true -> (this.simplify rhs).Expression
        | BoolLiteral false -> BoolLiteral false
        | _ -> AND(lhs, this.simplify rhs)

    override this.OnLogicalOr(lhs, rhs) =
        let lhs = this.simplify lhs

        match lhs.Expression with
        | BoolLiteral true -> BoolLiteral true
        | BoolLiteral false -> (this.simplify rhs).Expression
        | _ -> OR(lhs, this.simplify rhs)

    // - simplifies addition of two constants (integers, big integers,
    //   doubles, arrays, and strings) into single constant
    // - rewrites (integers, big integers, and doubles):
    //     0 + x = x
    //     x + 0 = x
    override this.OnAddition(lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)

        match lhs.Expression, rhs.Expression with
        | ValueArray a, ValueArray b -> ValueArray(a.AddRange b)
        | StringLiteral (a, a2), StringLiteral (b, b2) when a2.Length = 0 || b2.Length = 0 ->
            StringLiteral(a + b, a2.AddRange b2)
        | BigIntLiteral zero, op
        | op, BigIntLiteral zero when zero.IsZero -> op
        | DoubleLiteral 0.0, op
        | op, DoubleLiteral 0.0
        | IntLiteral 0L, op
        | op, IntLiteral 0L -> op
        | _ -> this.arithNumBinaryOp ADD (+) (+) (+) lhs rhs

    // - simplifies subtraction of two constants into single constant
    // - rewrites (integers, big integers, and doubles)
    //     x - 0 = x
    //     0 - x = -x
    //     x - x = 0
    override this.OnSubtraction(lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)

        match lhs.Expression, rhs.Expression with
        | op, BigIntLiteral zero when zero.IsZero -> op
        | op, DoubleLiteral 0.0
        | op, IntLiteral 0L -> op
        | (BigIntLiteral zero), _ when zero.IsZero -> NEG rhs
        | (DoubleLiteral 0.0), _
        | (IntLiteral 0L), _ -> NEG rhs
        | op1, op2 when op1 = op2 ->
            match lhs.ResolvedType.Resolution with
            | BigInt -> BigIntLiteral BigInteger.Zero
            | Double -> DoubleLiteral 0.0
            | Int -> IntLiteral 0L
            | _ -> this.arithNumBinaryOp SUB (-) (-) (-) lhs rhs
        | _ -> this.arithNumBinaryOp SUB (-) (-) (-) lhs rhs

    // - simplifies multiplication of two constants into single constant
    // - rewrites (integers, big integers, and doubles)
    //     x * 0 = 0
    //     0 * x = 0
    //     x * 1 = x
    //     1 * x = x
    override this.OnMultiplication(lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)

        match lhs.Expression, rhs.Expression with
        | _, (BigIntLiteral zero)
        | (BigIntLiteral zero), _ when zero.IsZero -> BigIntLiteral BigInteger.Zero
        | _, (DoubleLiteral 0.0)
        | (DoubleLiteral 0.0), _ -> DoubleLiteral 0.0
        | _, (IntLiteral 0L)
        | (IntLiteral 0L), _ -> IntLiteral 0L
        | op, (BigIntLiteral one)
        | (BigIntLiteral one), op when one.IsOne -> op
        | op, (DoubleLiteral 1.0)
        | (DoubleLiteral 1.0), op
        | op, (IntLiteral 1L)
        | (IntLiteral 1L), op -> op
        | _ -> this.arithNumBinaryOp MUL (*) (*) (*) lhs rhs

    // - simplifies multiplication of two constants into single constant
    override this.OnDivision(lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)

        match lhs.Expression, rhs.Expression with
        | op, (BigIntLiteral one) when one.IsOne -> op
        | op, (DoubleLiteral 1.0)
        | op, (IntLiteral 1L) -> op
        | _ -> this.arithNumBinaryOp DIV (/) (/) (/) lhs rhs

    override this.OnExponentiate(lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)

        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, IntLiteral b -> BigIntLiteral(BigInteger.Pow(a, safeCastInt64 b))
        | DoubleLiteral a, DoubleLiteral b -> DoubleLiteral(Math.Pow(a, b))
        | IntLiteral a, IntLiteral b -> IntLiteral(longPow a b)
        | _ -> POW(lhs, rhs)

    override this.OnModulo(lhs, rhs) = this.intBinaryOp MOD (%) (%) lhs rhs

    override this.OnLeftShift(lhs, rhs) =
        this.intBinaryOp LSHIFT (fun l r -> l <<< safeCastBigInt r) (fun l r -> l <<< safeCastInt64 r) lhs rhs

    override this.OnRightShift(lhs, rhs) =
        this.intBinaryOp RSHIFT (fun l r -> l >>> safeCastBigInt r) (fun l r -> l >>> safeCastInt64 r) lhs rhs

    override this.OnBitwiseExclusiveOr(lhs, rhs) =
        this.intBinaryOp BXOR (^^^) (^^^) lhs rhs

    override this.OnBitwiseOr(lhs, rhs) = this.intBinaryOp BOR (|||) (|||) lhs rhs

    override this.OnBitwiseAnd(lhs, rhs) =
        this.intBinaryOp BAND (&&&) (&&&) lhs rhs

    override this.OnLogicalNot expr =
        let expr = this.simplify expr

        match expr.Expression with
        | BoolLiteral a -> BoolLiteral(not a)
        | _ -> NOT expr

    override this.OnNegative expr =
        let expr = this.simplify expr

        match expr.Expression with
        | BigIntLiteral a -> BigIntLiteral(-a)
        | DoubleLiteral a -> DoubleLiteral(-a)
        | IntLiteral a -> IntLiteral(-a)
        | _ -> NEG expr

    override this.OnBitwiseNot expr =
        let expr = this.simplify expr

        match expr.Expression with
        | IntLiteral a -> IntLiteral(~~~a)
        | _ -> BNOT expr
