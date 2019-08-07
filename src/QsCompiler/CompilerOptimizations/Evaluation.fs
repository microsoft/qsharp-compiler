﻿module Microsoft.Quantum.QsCompiler.CompilerOptimization.Evaluation


open System
open System.Collections.Immutable
open System.Numerics
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open ComputationExpressions
open Types
open Utils
open Printer


type FunctionInterrupt =
| Returned of Expr
| Failed of Expr
| CouldNotEvaluate of string

type FunctionState = Result<Constants<Expr>, FunctionInterrupt>


/// Evaluates functions by stepping through their code
type FunctionEvaluator(callables: Callables, maxRecursiveDepth: int) =

    /// Transforms a BoolLiteral into the corresponding bool
    let castToBool x =
        match x with
        | BoolLiteral b -> Ok b
        | _ -> "Not a BoolLiteral: " + (printExpr x) |> CouldNotEvaluate |> Error

    /// The callback for the subroutine that evaluates and simplifies an expression
    member this.evaluateExpression constants expr =
        (ExpressionEvaluator(callables, constants, maxRecursiveDepth - 1).Transform expr).Expression

    /// Evaluates a single Q# statement
    member this.evaluateStatement (constants: Constants<Expr>) (statement: QsStatement): FunctionState =
        match statement.Statement with
        | QsExpressionStatement expr ->
            this.evaluateExpression constants expr |> ignore; constants |> Ok
        | QsReturnStatement expr ->
            this.evaluateExpression constants expr |> Returned |> Error
        | QsFailStatement expr ->
            this.evaluateExpression constants expr |> Failed |> Error
        | QsVariableDeclaration s ->
            defineVarTuple (isLiteral callables) constants (s.Lhs, this.evaluateExpression constants s.Rhs) |> Ok
        | QsValueUpdate s ->
            match s.Lhs.Expression with
            | Identifier (LocalVariable name, tArgs) -> setVar (isLiteral callables) constants (name.Value, this.evaluateExpression constants s.Rhs) |> Ok
            | _ -> "Unknown LHS of value update statement: " + (printExpr s.Lhs.Expression) |> CouldNotEvaluate |> Error
        | QsConditionalStatement s ->
            let firstEval =
                s.ConditionalBlocks |>
                Seq.map (fun (ts, block) -> this.evaluateExpression constants ts |> castToBool, block) |>
                Seq.tryFind (fst >> function Ok false -> false | _ -> true)
            match firstEval, s.Default with
            | Some (Error s, _), _ -> s |> Error
            | Some (Ok _, block), _ | None, Value block -> this.evaluateScope constants true block.Body
            | None, Null -> Ok constants
        | QsForStatement s ->
            let iterValues = this.evaluateExpression constants s.IterationValues
            let iterSeqOpt =
                match iterValues with
                | RangeLiteral _ -> rangeLiteralToSeq iterValues |> Seq.map IntLiteral |> Some
                | ValueArray va -> va |> Seq.map (fun x -> x.Expression) |> Some
                | _ -> None
            match iterSeqOpt with
            | None -> 
                "Unknown IterationValue in for loop: " + (printExpr iterValues) |> CouldNotEvaluate |> Error
            | Some iterSeq ->
                iterSeq |> Seq.fold (fun result loopValue ->
                    match result with
                    | Ok constants ->
                        let constants = enterScope constants
                        let constants = defineVarTuple (isLiteral callables) constants (fst s.LoopItem, loopValue)
                        let result = this.evaluateScope constants true s.Body
                        match result with
                        | Ok constants -> exitScope constants |> Ok
                        | x -> x
                    | _ -> result
                ) (Ok constants)
        | QsWhileStatement stmt ->
            result {
                let constantsRef = ref constants
                while this.evaluateExpression !constantsRef stmt.Condition |> castToBool do
                    let! s = this.evaluateScope !constantsRef true stmt.Body
                    constantsRef := s
                // set constantsRef (this.evaluateScope !constantsRef true stmt.Body)
                return constantsRef.Value
            }
        | QsRepeatStatement stmt ->
            result {
                let constantsRef = ref constants
                constantsRef := enterScope !constantsRef
                let! s = this.evaluateScope !constantsRef false stmt.RepeatBlock.Body
                constantsRef := s
                while this.evaluateExpression !constantsRef stmt.SuccessCondition |> castToBool do
                    let! s = this.evaluateScope !constantsRef false stmt.FixupBlock.Body
                    constantsRef := s
                    let! s = this.evaluateScope !constantsRef false stmt.RepeatBlock.Body
                    constantsRef := s
                constantsRef := exitScope !constantsRef
                return !constantsRef
            }
        | QsQubitScope s ->
            "Cannot allocate qubits in function" |> CouldNotEvaluate |> Error
        | QsScopeStatement s ->
            this.evaluateScope constants true s.Body

    /// Evaluates a list of Q# statements
    member this.evaluateScope (constants: Constants<Expr>) (newScope: bool) (scope: QsScope): FunctionState =
        result {
            let constantsRef = ref constants
            if newScope then
                constantsRef := enterScope constantsRef.Value
            for stmt in scope.Statements do
                let! s = this.evaluateStatement constantsRef.Value stmt
                constantsRef := s
            if newScope then
                constantsRef := exitScope constantsRef.Value
            return !constantsRef
        }

    /// Evaluates a Q# function
    member this.evaluateFunction (constants: Constants<Expr>) (name: QsQualifiedName) (arg: TypedExpression) (types: QsNullable<ImmutableArray<ResolvedType>>): Expr option =
        // TODO: assert compiledCallables contains name
        let callable = getCallable callables name
        // TODO: assert callable.Specializations.Length == 1
        let impl = callable.Specializations.[0].Implementation
        match impl with
        | Provided (specArgs, scope) ->
            let constants = enterScope constants
            let constants = defineVarTuple (isLiteral callables) constants (toSymbolTuple callable.ArgumentTuple, arg.Expression)
            match this.evaluateScope constants true scope with
            | Ok _ ->
                // printfn "Function %O didn't return anything" name.Name.Value
                None
            | Error (Returned expr) ->
                // printfn "Function %O returned %O" name.Name.Value (prettyPrint expr)
                Some expr
            | Error (Failed expr) ->
                // printfn "Function %O failed with %O" name.Name.Value (prettyPrint expr)
                None
            | Error (CouldNotEvaluate reason) ->
                // printfn "Could not evaluate function %O on arg %O for reason %O" name.Name.Value (prettyPrint arg.Expression) reason
                None
        | _ ->
            // printfn "Implementation not provided for: %O" name
            None


and internal ExpressionEvaluator(callables: Callables, constants: Constants<Expr>, maxRecursiveDepth: int) =
    inherit ExpressionTransformation()

    override this.Kind = upcast { new ExpressionKindEvaluator(callables, constants, maxRecursiveDepth) with 
        override exprKind.ExpressionTransformation x = this.Transform x 
        override exprKind.TypeTransformation x = this.Type.Transform x }


/// The ExpressionKindTransformation used to evaluate constant expressions
and [<AbstractClass>] private ExpressionKindEvaluator(callables: Callables, constants: Constants<Expr>, maxRecursiveDepth: int) =
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
        | LocalVariable name -> getVar constants name.Value |? Identifier (sym, tArgs)
        | _ -> Identifier (sym, tArgs)

    override this.onFunctionCall (method, arg) =
        let method, arg = this.simplify (method, arg)
        maybe {
            match method.Expression with
            | Identifier (GlobalCallable qualName, types) ->
                do! check (maxRecursiveDepth > 0 && isLiteral callables arg.Expression)
                let fe = FunctionEvaluator (callables, maxRecursiveDepth)
                return! fe.evaluateFunction constants qualName arg types
            | CallLikeExpression (baseMethod, partialArg) ->
                do! hasMissingExprs partialArg |> check
                return this.Transform (CallLikeExpression (baseMethod, fillPartialArg (partialArg, arg)))
            | _ -> ()
        } |? CallLikeExpression (method, arg)

    override this.onPartialApplication (method, arg) =
        let method, arg = this.simplify (method, arg)
        maybe {
            match method.Expression with
            | CallLikeExpression (baseMethod, partialArg) ->
                do! hasMissingExprs partialArg |> check
                return this.Transform (CallLikeExpression (baseMethod, fillPartialArg (partialArg, arg)))
            | _ -> ()
        } |? CallLikeExpression (method, arg)

    override this.onUnwrapApplication ex =
        let ex = this.simplify ex
        match ex.Expression with
        | CallLikeExpression ({Expression = Identifier (GlobalCallable qualName, types)}, arg)
            when (getCallable callables qualName).Kind = TypeConstructor ->
                // TODO - must be adapted if we want to support user-defined type constructors
                QsCompilerError.Verify (
                    (getCallable callables qualName).Specializations.Length = 1,
                    "Type constructors should have exactly one specialization")
                QsCompilerError.Verify (
                    (getCallable callables qualName).Specializations.[0].Implementation = Intrinsic,
                    "Type constructors should be implicit")
                arg.Expression
        | _ -> UnwrapApplication ex

    override this.onArrayItem (arr, idx) =
        let arr, idx = this.simplify (arr, idx)
        match arr.Expression, idx.Expression with
        | ValueArray va, IntLiteral i -> va.[int i].Expression
        | _ -> ArrayItem (arr, idx)

    override this.onNewArray (bt, idx) =
        let idx = this.simplify idx
        match idx.Expression with
        | IntLiteral i -> constructNewArray bt.Resolution (int i) |? NewArray (bt, idx)
        // TODO - handle array slicing
        | _ -> NewArray (bt, idx)
 
    override this.onCopyAndUpdateExpression (lhs, accEx, rhs) =
        let lhs, accEx, rhs = this.simplify (lhs, accEx, rhs)
        match lhs.Expression, accEx.Expression with
        | ValueArray va, IntLiteral i -> ValueArray (va.SetItem(int i, rhs))
        // TODO - handle array slicing
        // TODO - handle named items in user-defined types
        | _ -> CopyAndUpdate (lhs, accEx, rhs)

    override this.onConditionalExpression (e1, e2, e3) =
        let e1 = this.simplify e1
        match e1.Expression with
        | BoolLiteral a -> if a then (this.simplify e2).Expression else (this.simplify e3).Expression
        | _ -> CONDITIONAL (e1, this.simplify e2, this.simplify e3)

    override this.onEquality (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match isLiteral callables lhs.Expression && isLiteral callables rhs.Expression with
        | true -> BoolLiteral (lhs.Expression = rhs.Expression)
        | false -> EQ (lhs, rhs)

    override this.onInequality (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match isLiteral callables lhs.Expression && isLiteral callables rhs.Expression with
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
        | StringLiteral (a, a2), StringLiteral (b, b2) -> StringLiteral (NonNullable<_>.New (a.Value + b.Value), a2.AddRange b2)
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
        | BigIntLiteral a, IntLiteral b -> BigIntLiteral (BigInteger.Pow(a, Convert.ToInt32(b)))
        | DoubleLiteral a, DoubleLiteral b -> DoubleLiteral (Math.Pow(a, b))
        | IntLiteral a, IntLiteral b -> IntLiteral (longPow a b)
        | _ -> POW (lhs, rhs)

    override this.onModulo (lhs, rhs) =
        this.intBinaryOp MOD (%) (%) lhs rhs

    override this.onLeftShift (lhs, rhs) =
        this.intBinaryOp LSHIFT (fun l r -> l <<< int r) (fun l r -> l <<< int r) lhs rhs

    override this.onRightShift (lhs, rhs) =
        this.intBinaryOp RSHIFT (fun l r -> l >>> int r) (fun l r -> l >>> int r) lhs rhs

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

