module Microsoft.Quantum.QsCompiler.CompilerOptimization.FunctionEvaluation

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree

open ComputationExpressions
open TransformationState
open Utils
open Printer



type FunctionInterrupt =
| Returned of Expr
| Failed of Expr
| CouldNotEvaluate of string
type FunctionState = Result<TransformationState, FunctionInterrupt>


/// Evaluates functions by stepping through their code
type [<AbstractClass>] FunctionEvaluator() =

    /// Transforms a BoolLiteral into the corresponding bool
    let castToBool x =
        match x with
        | BoolLiteral b -> Ok b
        | _ -> "Not a BoolLiteral: " + (printExpr x) |> CouldNotEvaluate |> Error

    /// The callback for the subroutine that evaluates and simplifies an expression
    abstract member evaluateExpression: TransformationState -> TypedExpression -> Expr

    /// Evaluates a single Q# statement
    member this.evaluateStatement (state: TransformationState) (statement: QsStatement): FunctionState =
        match statement.Statement with
        | QsExpressionStatement expr ->
            this.evaluateExpression state expr |> ignore; state |> Ok
        | QsReturnStatement expr ->
            this.evaluateExpression state expr |> Returned |> Error
        | QsFailStatement expr ->
            this.evaluateExpression state expr |> Failed |> Error
        | QsVariableDeclaration s ->
            defineVarTuple state (s.Lhs, this.evaluateExpression state s.Rhs) |> Ok
        | QsValueUpdate s ->
            match s.Lhs.Expression with
            | Identifier (LocalVariable name, tArgs) -> setVar state (name.Value, this.evaluateExpression state s.Rhs) |> Ok
            // TODO - allow any symbol tuple on the LHS
            | _ -> "Unknown LHS of value update statement: " + (printExpr s.Lhs.Expression) |> CouldNotEvaluate |> Error
        | QsConditionalStatement s ->
            let firstEval =
                s.ConditionalBlocks |>
                Seq.map (fun (ts, block) -> this.evaluateExpression state ts |> castToBool, block) |>
                Seq.tryFind (fst >> function Ok false -> false | _ -> true)
            match firstEval, s.Default with
            | Some (Error s, _), _ -> s |> Error
            | Some (Ok _, block), _ | None, Value block -> this.evaluateScope state true block.Body
            | None, Null -> Ok state
        | QsForStatement s ->
            let iterValues = this.evaluateExpression state s.IterationValues
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
                    | Ok state ->
                        let state = enterScope state
                        let state = defineVarTuple state (fst s.LoopItem, loopValue)
                        let result = this.evaluateScope state true s.Body
                        match result with
                        | Ok state -> exitScope state |> Ok
                        | x -> x
                    | _ -> result
                ) (Ok state)
        | QsWhileStatement stmt ->
            result {
                let stateRef = ref state
                while this.evaluateExpression !stateRef stmt.Condition |> castToBool do
                    let! s = this.evaluateScope !stateRef true stmt.Body
                    stateRef := s
                // set stateRef (this.evaluateScope !stateRef true stmt.Body)
                return stateRef.Value
            }
        | QsRepeatStatement stmt ->
            result {
                let stateRef = ref state
                stateRef := enterScope !stateRef
                let! s = this.evaluateScope !stateRef false stmt.RepeatBlock.Body
                stateRef := s
                while this.evaluateExpression !stateRef stmt.SuccessCondition |> castToBool do
                    let! s = this.evaluateScope !stateRef false stmt.FixupBlock.Body
                    stateRef := s
                    let! s = this.evaluateScope !stateRef false stmt.RepeatBlock.Body
                    stateRef := s
                stateRef := exitScope !stateRef
                return !stateRef
            }
        | QsQubitScope s ->
            QsCompilerError.Raise "Cannot allocate qubits in function"
            "Cannot allocate qubits in function" |> CouldNotEvaluate |> Error
        | QsScopeStatement s ->
            this.evaluateScope state true s.Body

    /// Evaluates a list of Q# statements
    member this.evaluateScope (state: TransformationState) (newScope: bool) (scope: QsScope): FunctionState =
        result {
            let stateRef = ref state
            if newScope then
                stateRef := enterScope stateRef.Value
            for stmt in scope.Statements do
                let! s = this.evaluateStatement stateRef.Value stmt
                stateRef := s
            if newScope then
                stateRef := exitScope stateRef.Value
            return stateRef.Value
        }

    /// Evaluates a Q# function
    member this.evaluateFunction (state: TransformationState) (name: QsQualifiedName) (arg: TypedExpression) (types: QsNullable<ImmutableArray<ResolvedType>>): Expr option =
        let callable = getCallable state name
        QsCompilerError.Verify (
            callable.Specializations.Length = 1,
            "Functions should only have one specialization")
        let impl = callable.Specializations.[0].Implementation
        match impl with
        | Provided (specArgs, scope) ->
            let state = enterScope state
            let state = defineVarTuple state (toSymbolTuple callable.ArgumentTuple, arg.Expression)
            match this.evaluateScope state true scope with
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

