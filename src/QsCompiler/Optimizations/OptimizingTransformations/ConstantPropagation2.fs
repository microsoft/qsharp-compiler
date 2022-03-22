// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Experimental

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Numerics
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Experimental.Evaluation
open Microsoft.Quantum.QsCompiler.Experimental.Utils
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree


/// The SyntaxTreeTransformation used to evaluate constants
type internal ConstantPropagation (callables) as this =
    inherit TransformationBase()

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
                | SizedArray _
                | NewArray _ -> true
                | CallLikeExpression ({ Expression = Identifier (GlobalCallable name, _) }, _) when
                    callables.[name].Kind = TypeConstructor
                    ->
                    true
                | _ when TypedExpression.IsPartialApplication ex.Expression -> true
                | UnitValue
                | IntLiteral _
                | BigIntLiteral _
                | DoubleLiteral _
                | BoolLiteral _
                | StringLiteral _
                | ResultLiteral _
                | PauliLiteral _
                | NEG _
                | NOT _
                | BNOT _
                | ADD _
                | SUB _
                | MUL _
                | DIV _
                | MOD _
                | POW _
                | EQ _
                | NEQ _
                | LT _
                | LTE _
                | GT _
                | GTE _
                | AND _
                | OR _
                | BOR _
                | BAND _
                | BXOR _
                | LSHIFT _
                | RSHIFT _
                | CONDITIONAL _
                | CopyAndUpdate _
                | AdjointApplication _
                | ControlledApplication _
                | CallLikeExpression _
                | Lambda _
                | MissingExpr _
                | InvalidExpr _ -> false
                && Seq.forall id sub)

        expr.Fold folder

    /// The current dictionary that maps variables to the values we substitute for them
    member val Constants = new Dictionary<string, TypedExpression>()

    override __.OnProvidedImplementation(argTuple, body) =
        this.Constants.Clear()
        ``base``.OnProvidedImplementation(argTuple, body)

    override __.OnVariableDeclaration stm =
        let lhs = this.OnSymbolTuple stm.Lhs
        let rhs = this.OnTypedExpression stm.Rhs

        if stm.Kind = ImmutableBinding then
            defineVarTuple (shouldPropagate callables) this.Constants (lhs, rhs)

        QsBinding<TypedExpression>.New stm.Kind (lhs, rhs) |> QsVariableDeclaration

    override __.OnConditionalStatement stm =
        let cbList, cbListEnd =
            stm.ConditionalBlocks
            |> Seq.fold
                (fun s (cond, block) ->
                    let newCond = this.OnTypedExpression cond

                    match newCond.Expression with
                    | BoolLiteral true -> s @ [ Null, block ]
                    | BoolLiteral false -> s
                    | _ -> s @ [ Value cond, block ])
                []
            |> List.ofSeq
            |> takeWhilePlus1 (fun (c, _) -> c <> Null)

        let newDefault = cbListEnd |> Option.map (snd >> Value) |? stm.Default

        let cbList = cbList |> List.map (fun (c, b) -> this.OnPositionedBlock(c, b))

        let newDefault =
            match newDefault with
            | Value x -> this.OnPositionedBlock(Null, x) |> snd |> Value
            | Null -> Null

        match cbList, newDefault with
        | [], Value x -> x.Body |> newScopeStatement
        | [], Null -> QsScope.New([], LocalDeclarations.New []) |> newScopeStatement
        | _ ->
            let invalidCondition () = failwith "missing condition"
            let cases = cbList |> Seq.map (fun (c, b) -> (c.ValueOrApply invalidCondition, b))
            QsConditionalStatement.New(cases, newDefault) |> QsConditionalStatement

    override __.OnQubitScope(stm: QsQubitScope) =
        let kind = stm.Kind
        let lhs = this.OnSymbolTuple stm.Binding.Lhs
        let rhs = this.OnQubitInitializer stm.Binding.Rhs

        jointFlatten (lhs, rhs)
        |> Seq.iter (fun (l, r) ->
            match l, r.Resolution with
            | VariableName name, QubitRegisterAllocation { Expression = IntLiteral num } ->
                let arrayIden = Identifier(LocalVariable name, Null) |> wrapExpr (ArrayType(ResolvedType.New Qubit))
                let elemI = fun i -> ArrayItem(arrayIden, IntLiteral(int64 i) |> wrapExpr Int)

                let expr =
                    Seq.init (safeCastInt64 num) (elemI >> wrapExpr Qubit)
                    |> ImmutableArray.CreateRange
                    |> ValueArray
                    |> wrapExpr (ArrayType(ResolvedType.New Qubit))

                defineVar (fun _ -> true) this.Constants (name, expr)
            | _ -> ())

        let body = this.OnScope stm.Body
        QsQubitScope.New kind ((lhs, rhs), body) |> QsQubitScope

    // Copied from ExpressionEvaluator

    member private this.simplify e1 = this.OnTypedExpression e1

    member private this.simplify(e1, e2) =
        (this.OnTypedExpression e1, this.OnTypedExpression e2)

    member private this.simplify(e1, e2, e3) =
        (this.OnTypedExpression e1,
         this.OnTypedExpression e2,
         this.OnTypedExpression e3)

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
            match this.Constants.TryGetValue name with
            | true, ex -> ex.Expression
            | _ -> Identifier(sym, tArgs)
        | _ -> Identifier(sym, tArgs)

    override this.OnFunctionCall(method, arg) =
        let method, arg = this.simplify (method, arg)

        maybe {
            match method.Expression with
            | Identifier (GlobalCallable qualName, _) ->
                do! check (isLiteral callables arg)
                let fe = FunctionEvaluator(callables)
                return! fe.EvaluateFunction qualName arg 1000 |> Option.map (fun x -> x.Expression)
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
        | CallLikeExpression ({ Expression = Identifier (GlobalCallable qualName, types) }, arg) when
            (callables.[qualName]).Kind = TypeConstructor
            ->
            // TODO - must be adapted if we want to support user-defined type constructors
            QsCompilerError.Verify(
                (callables.[qualName]).Specializations.Length = 1,
                "Type constructors should have exactly one specialization"
            )

            QsCompilerError.Verify(
                (callables.[qualName]).Specializations.[0].Implementation = Intrinsic,
                "Type constructors should be implicit"
            )

            arg.Expression
        | _ -> UnwrapApplication ex

    override this.OnArrayItemAccess(arr, idx) =
        let arr, idx = this.simplify (arr, idx)

        match arr.Expression, idx.Expression with
        | ValueArray va, IntLiteral i -> va.[safeCastInt64 i].Expression
        | ValueArray va, RangeLiteral _ when isLiteral callables idx ->
            rangeLiteralToSeq idx.Expression
            |> Seq.map (fun i -> va.[safeCastInt64 i])
            |> ImmutableArray.CreateRange
            |> ValueArray
        | _ -> ArrayItem(arr, idx)

    override this.OnSizedArray(value, size) =
        let value = this.simplify value
        let size = this.simplify size

        match size.Expression with
        | IntLiteral i when isLiteral callables value -> constructArray (safeCastInt64 i) value
        | _ -> SizedArray(value, size)

    override this.OnNewArray(itemType, length) =
        let length = this.simplify length

        match length.Expression with
        | IntLiteral i -> defaultValue itemType.Resolution |> Option.map (safeCastInt64 i |> constructArray)
        | _ -> None
        |> Option.defaultValue (NewArray(itemType, length))

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

    override this.OnBitwiseOr(lhs, rhs) =
        this.intBinaryOp BOR (|||) (|||) lhs rhs

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
