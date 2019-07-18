module Microsoft.Quantum.QsCompiler.CompilerOptimization.FunctionEvaluation

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree

open Utils


/// The current state of a function evaluation
type private FunctionState =
| Normal // The function is proceeding normally
| Returned of Expr // The function is returning a value
| Failed of Expr // The function is throwing an exception
| CouldNotEvaluate of string // I couldn't evaluate the function

/// The current state of an expression evaluation
type private ExpressionResult<'A> =
| ExprValue of 'A
| ExprError of string


/// Evaluates functions by stepping through their code
type [<AbstractClass>] internal FunctionEvaluator(cd: CallableDict) =

    /// Transforms a BoolLiteral into the corresponding bool
    let castToBool x =
        match x with
        | BoolLiteral b -> ExprValue b
        | _ -> "Not a BoolLiteral: " + (prettyPrint x) |> ExprError

    /// Returns whether a FunctionState is an interrupt
    let isInterrupt = function Normal -> false | _ -> true

    /// The callback for the subroutine that evaluates and simplifies an expression
    abstract member evaluateExpression: VariablesDict -> TypedExpression -> Expr

    /// Evaluates a single Q# statement
    member private this.evaluateStatement (vars: VariablesDict) (statement: QsStatement): FunctionState =
        match statement.Statement with
        | QsExpressionStatement expr ->
            this.evaluateExpression vars expr |> ignore; Normal
        | QsReturnStatement expr ->
            this.evaluateExpression vars expr |> Returned
        | QsFailStatement expr ->
            this.evaluateExpression vars expr |> Failed
        | QsVariableDeclaration s ->
            fillVars vars (StringTuple.fromSymbolTuple s.Lhs, this.evaluateExpression vars s.Rhs); Normal
        | QsValueUpdate s ->
            match s.Lhs.Expression with
                | Identifier (LocalVariable name, tArgs) -> vars.setVar(name.Value, this.evaluateExpression vars s.Rhs); Normal
                // TODO - allow any symbol tuple on the LHS
                | _ -> "Unknown LHS of value update statement: " + (prettyPrint s.Lhs.Expression) |> CouldNotEvaluate
        | QsConditionalStatement s ->
            match s.ConditionalBlocks |>
                Seq.map (fun (ts, block) -> this.evaluateExpression vars ts |> castToBool, block) |>
                Seq.tryFind (fst >> function ExprValue false -> false | _ -> true), s.Default with
            | Some (ExprError s, _), _ -> CouldNotEvaluate s
            | Some (ExprValue _, block), _ | None, Value block -> this.evaluateScope vars true block.Body
            | None, Null -> Normal
        | QsForStatement s ->
            let iterValues = this.evaluateExpression vars s.IterationValues
            match iterValues with
            | RangeLiteral _ ->
                rangeLiteralToSeq iterValues |> Seq.map (fun loopValue ->
                        vars.enterScope()
                        fillVars vars (StringTuple.fromSymbolTuple (fst s.LoopItem), IntLiteral loopValue)
                        let result = this.evaluateScope vars true s.Body
                        vars.exitScope()
                        result) |>
                    Seq.tryFind isInterrupt |? Normal
            | ValueArray va ->
                va |> Seq.map (fun loopValue ->
                        vars.enterScope()
                        fillVars vars (StringTuple.fromSymbolTuple (fst s.LoopItem), loopValue.Expression)
                        let result = this.evaluateScope vars true s.Body
                        vars.exitScope()
                        result) |>
                    Seq.tryFind isInterrupt |? Normal
            | _ ->
                "Unknown IterationValue in for loop: " + (prettyPrint iterValues) |> CouldNotEvaluate
        | QsWhileStatement s ->
            Seq.initInfinite (fun _ -> this.evaluateExpression vars s.Condition |> castToBool) |>
                Seq.map (function
                    | ExprValue true ->
                        match this.evaluateScope vars true s.Body with
                        | Normal -> Normal, false
                        | res -> res, true
                    | ExprValue false -> Normal, true
                    | ExprError s -> CouldNotEvaluate s, true) |>
                Seq.tryFind snd |? (Normal, true) |> fst
        | QsRepeatStatement s ->
            vars.enterScope()
            let res =
                Seq.initInfinite (fun _ -> this.evaluateScope vars false s.RepeatBlock.Body) |>
                Seq.map (function
                    | Normal ->
                        match this.evaluateExpression vars s.SuccessCondition |> castToBool with
                        | ExprValue true -> Normal, true
                        | ExprValue false ->
                            match this.evaluateScope vars false s.FixupBlock.Body with
                            | Normal -> Normal, false
                            | res -> res, true
                        | ExprError s -> CouldNotEvaluate s, true
                    | res -> res, true) |>
                Seq.tryFind snd |? (Normal, true) |> fst
            vars.exitScope()
            res
        | QsQubitScope s ->
            QsCompilerError.Raise "Cannot allocate qubits in function"
            "Cannot allocate qubits in function" |> CouldNotEvaluate

    /// Evaluates a list of Q# statements
    member private this.evaluateScope (vars: VariablesDict) (enterScope: bool) (scope: QsScope): FunctionState =
        if enterScope then vars.enterScope()
        let res =
            scope.Statements |>
            Seq.map (this.evaluateStatement vars) |>
            Seq.tryFind isInterrupt |? Normal
        if enterScope then vars.exitScope()
        res

    /// Evaluates a Q# function
    member this.evaluateFunction (name: QsQualifiedName) (arg: TypedExpression) (types: QsNullable<ImmutableArray<ResolvedType>>): Expr option =
        let callable = cd.getCallable name
        QsCompilerError.Verify (
            callable.Specializations.Length = 1,
            "Functions should only have one specialization")
        let impl = callable.Specializations.[0].Implementation
        match impl with
        | Provided (specArgs, scope) ->
            let vars = VariablesDict()
            vars.enterScope()
            fillVars vars (StringTuple.fromQsTuple callable.ArgumentTuple, arg.Expression)
            let res =
                match this.evaluateScope vars true scope with
                | Normal ->
                    // printfn "Function %O didn't return anything" name.Name.Value
                    None
                | Returned expr ->
                    // printfn "Function %O returned %O" name.Name.Value (prettyPrint expr)
                    Some expr
                | Failed expr ->
                    // printfn "Function %O failed with %O" name.Name.Value (prettyPrint expr)
                    None
                | CouldNotEvaluate reason ->
                    // printfn "Could not evaluate function %O on arg %O for reason %O" name.Name.Value (prettyPrint arg.Expression) reason
                    None
            vars.exitScope()
            res
        | _ ->
            // printfn "Implementation not provided for: %O" name
            None

