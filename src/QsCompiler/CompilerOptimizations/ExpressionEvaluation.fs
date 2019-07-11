module ExpressionEvaluation

open System
open System.Collections.Immutable
open System.Numerics
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open Utils
open FunctionEvaluation


type ExpressionEvaluator(vars: VariablesDict, compiledCallables: ImmutableDictionary<QsQualifiedName, QsCallable>, reqFullEval: bool) =
    inherit ExpressionTransformation()

    member this.getFE() = { new FunctionEvaluator(compiledCallables) with 
        override f.evaluateExpression vars2 x =
            (ExpressionEvaluator(vars2, compiledCallables, true).Transform x).Expression }

    override this.Kind = upcast { new ExpressionKindEvaluator(vars, this.getFE(), reqFullEval) with 
        override kind.ExpressionTransformation x = this.Transform x 
        override kind.TypeTransformation x = this.Type.Transform x }


and [<AbstractClass>] ExpressionKindEvaluator(vars: VariablesDict, fe: FunctionEvaluator, reqFullEval: bool) =
    inherit ExpressionKindTransformation()
        
    member private this.simplify e1 = this.ExpressionTransformation e1

    member private this.simplify (e1, e2) =
        (this.ExpressionTransformation e1, this.ExpressionTransformation e2)

    member private this.simplify (e1, e2, e3) =
        (this.ExpressionTransformation e1, this.ExpressionTransformation e2, this.ExpressionTransformation e3)
        
    (*override this.Transform expr =
        let result = base.Transform expr
        if reqFullEval && not (isLiteral result)
        then failwithf "Failed to evaluate: %O" result
        result*)

    override this.onIdentifier (sym, tArgs) =
        match sym with
        | LocalVariable name ->
            match vars.getVar name.Value with
            | Some expr -> expr
            | None ->
                if reqFullEval then failwithf "Unknown variable: %O" name
                Identifier (sym, tArgs)
        | _ ->
            if reqFullEval then failwithf "Unknown identifier type: %O" sym
            Identifier (sym, tArgs)

    override this.onFunctionCall (method, arg) =
        let arg = this.simplify arg
        match isLiteral arg.Expression, method.Expression with
        | true, Identifier (GlobalCallable qualName, types) -> fe.evaluateFunction qualName arg types
        | _ -> CallLikeExpression (method, arg)

    override this.onArrayItem (arr, idx) =
        let arr, idx = this.simplify (arr, idx)
        match arr.Expression, idx.Expression with
        | ValueArray va, IntLiteral i -> va.[int i].Expression
        | _ -> ArrayItem (arr, idx)

    override this.onNewArray (bt, idx) =
        let idx = this.simplify idx
        match idx.Expression with
        | IntLiteral i -> match constructNewArray bt.Resolution (int i) with
            | Some result -> result
            | None -> NewArray (bt, idx)
        | _ -> NewArray (bt, idx)
        
    override this.onCopyAndUpdateExpression (lhs, accEx, rhs) =
        let lhs, accEx, rhs = this.simplify (lhs, accEx, rhs)
        match lhs.Expression, accEx.Expression with
        | ValueArray va, IntLiteral i -> ValueArray (va.SetItem(int i, rhs))
        | _ -> CopyAndUpdate (lhs, accEx, rhs)

    override this.onConditionalExpression (e1, e2, e3) =
        let e1, e2, e3 = this.simplify (e1, e2, e3)
        match e1.Expression with
        | BoolLiteral a -> if a then e2.Expression else e3.Expression
        | _ -> CONDITIONAL (e1, e2, e3)

    override this.onEquality (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match isLiteral lhs.Expression && isLiteral rhs.Expression with
        | true -> BoolLiteral (lhs.Expression = rhs.Expression)
        | false -> EQ (lhs, rhs)
        
    override this.onInequality (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match isLiteral lhs.Expression && isLiteral rhs.Expression with
        | true -> BoolLiteral (lhs.Expression <> rhs.Expression)
        | false -> NEQ (lhs, rhs)
        
    override this.onLessThan (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BoolLiteral (a < b)
        | DoubleLiteral a, DoubleLiteral b -> BoolLiteral (a < b)
        | IntLiteral a, IntLiteral b -> BoolLiteral (a < b)
        | _ -> LT (lhs, rhs)
        
    override this.onLessThanOrEqual (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BoolLiteral (a <= b)
        | DoubleLiteral a, DoubleLiteral b -> BoolLiteral (a <= b)
        | IntLiteral a, IntLiteral b -> BoolLiteral (a <= b)
        | _ -> LTE (lhs, rhs)
        
    override this.onGreaterThan (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BoolLiteral (a > b)
        | DoubleLiteral a, DoubleLiteral b -> BoolLiteral (a > b)
        | IntLiteral a, IntLiteral b -> BoolLiteral (a > b)
        | _ -> GT (lhs, rhs)
        
    override this.onGreaterThanOrEqual (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BoolLiteral (a >= b)
        | DoubleLiteral a, DoubleLiteral b -> BoolLiteral (a >= b)
        | IntLiteral a, IntLiteral b -> BoolLiteral (a >= b)
        | _ -> GTE (lhs, rhs)
        
    override this.onLogicalAnd (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BoolLiteral a, BoolLiteral b -> BoolLiteral (a && b)
        | _ -> AND (lhs, rhs)
        
    override this.onLogicalOr (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BoolLiteral a, BoolLiteral b -> BoolLiteral (a || b)
        | _ -> OR (lhs, rhs)
    
    override this.onAddition (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BigIntLiteral (a + b)
        | DoubleLiteral a, DoubleLiteral b -> DoubleLiteral (a + b)
        | IntLiteral a, IntLiteral b -> IntLiteral (a + b)
        | ValueArray a, ValueArray b -> ValueArray (a.AddRange b)
        | _ -> ADD (lhs, rhs)

    override this.onSubtraction (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BigIntLiteral (a - b)
        | DoubleLiteral a, DoubleLiteral b -> DoubleLiteral (a - b)
        | IntLiteral a, IntLiteral b -> IntLiteral (a - b)
        | _ -> SUB (lhs, rhs)
        
    override this.onMultiplication (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BigIntLiteral (a * b)
        | DoubleLiteral a, DoubleLiteral b -> DoubleLiteral (a * b)
        | IntLiteral a, IntLiteral b -> IntLiteral (a * b)
        | _ -> MUL (lhs, rhs)
        
    override this.onDivision (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BigIntLiteral (a / b)
        | DoubleLiteral a, DoubleLiteral b -> DoubleLiteral (a / b)
        | IntLiteral a, IntLiteral b -> IntLiteral (a / b)
        | _ -> DIV (lhs, rhs)
        
    override this.onExponentiate (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, IntLiteral b -> BigIntLiteral (BigInteger (Math.Pow(float a, float b)))
        | IntLiteral a, IntLiteral b -> IntLiteral (int64 (Math.Pow(float a, float b)))
        | _ -> POW (lhs, rhs)

    override this.onModulo (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BigIntLiteral (a % b)
        | IntLiteral a, IntLiteral b -> IntLiteral (a % b)
        | _ -> MOD (lhs, rhs)
        
    override this.onLeftShift (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BigIntLiteral (a <<< int b)
        | IntLiteral a, IntLiteral b -> IntLiteral (a <<< int b)
        | _ -> LSHIFT (lhs, rhs)
        
    override this.onRightShift (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BigIntLiteral (a <<< int b)
        | IntLiteral a, IntLiteral b -> IntLiteral (a <<< int b)
        | _ -> RSHIFT (lhs, rhs)
        
    override this.onBitwiseExclusiveOr (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BigIntLiteral (a ^^^ b)
        | IntLiteral a, IntLiteral b -> IntLiteral (a ^^^ b)
        | _ -> BXOR (lhs, rhs)
        
    override this.onBitwiseOr (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BigIntLiteral (a ||| b)
        | IntLiteral a, IntLiteral b -> IntLiteral (a ||| b)
        | _ -> BOR (lhs, rhs)
        
    override this.onBitwiseAnd (lhs, rhs) =
        let lhs, rhs = this.simplify (lhs, rhs)
        match lhs.Expression, rhs.Expression with
        | BigIntLiteral a, BigIntLiteral b -> BigIntLiteral (a &&& b)
        | IntLiteral a, IntLiteral b -> IntLiteral (a &&& b)
        | _ -> BAND (lhs, rhs)
        
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

