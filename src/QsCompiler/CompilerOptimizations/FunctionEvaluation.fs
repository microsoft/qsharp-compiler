module FunctionEvaluation

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree

open Utils


exception Returned of Expr
exception Failed of Expr
exception CouldNotEvaluate of string


type [<AbstractClass>] FunctionEvaluator(compiledCallables: ImmutableDictionary<QsQualifiedName, QsCallable>) =

    let castToBool x =
        match x with
        | BoolLiteral b -> b
        | _ -> "Not a BoolLiteral: " + (prettyPrint x) |> CouldNotEvaluate |> raise

    member this.getCallable (name: QsQualifiedName): QsCallable =
        compiledCallables.[name]

    abstract member evaluateExpression: VariablesDict -> TypedExpression -> Expr

    member this.evaluateStatement (vars: VariablesDict) (statement: QsStatement): unit =
        match statement.Statement with
        | QsExpressionStatement expr ->
            this.evaluateExpression vars expr |> ignore
        | QsReturnStatement expr ->
            this.evaluateExpression vars expr |> Returned |> raise
        | QsFailStatement expr ->
            this.evaluateExpression vars expr |> Failed |> raise
        | QsVariableDeclaration s ->
            fillVars vars (StringTuple.fromSymbolTuple s.Lhs, this.evaluateExpression vars s.Rhs)
        | QsValueUpdate s ->
            match s.Lhs.Expression with
                | Identifier (LocalVariable name, tArgs) -> vars.setVar(name.Value, this.evaluateExpression vars s.Rhs)
                | _ -> "Unknown LHS of value update statement: " + (prettyPrint s.Lhs.Expression) |> CouldNotEvaluate |> raise
        | QsConditionalStatement s ->
            match Seq.tryFind (fun (ts, block) -> this.evaluateExpression vars ts |> castToBool) s.ConditionalBlocks with
            | Some (ts, block) -> this.evaluateScope vars block.Body
            | None -> match s.Default with
                | Value body -> this.evaluateScope vars body.Body
                | Null -> ()
        | QsForStatement s ->
            let iterValues = this.evaluateExpression vars s.IterationValues
            match iterValues with
            | RangeLiteral _ -> 
                for loopValue in rangeLiteralToSeq iterValues do
                    vars.enterScope()
                    fillVars vars (StringTuple.fromSymbolTuple (fst s.LoopItem), IntLiteral (int64 loopValue))
                    this.evaluateScope vars s.Body
                    vars.exitScope()
            | ValueArray va ->
                for loopValue in va do
                    vars.enterScope()
                    fillVars vars (StringTuple.fromSymbolTuple (fst s.LoopItem), loopValue.Expression)
                    this.evaluateScope vars s.Body
                    vars.exitScope()
            | _ ->
                "Unknown IterationValue in for loop: " + (prettyPrint iterValues) |> CouldNotEvaluate |> raise
        | QsWhileStatement s ->
            while this.evaluateExpression vars s.Condition |> castToBool do
                this.evaluateScope vars s.Body
        | QsRepeatStatement s ->
            let mutable success = false
            while not success do
                this.evaluateScope vars s.RepeatBlock.Body
                success <- this.evaluateExpression vars s.SuccessCondition |> castToBool
                if not success then
                    this.evaluateScope vars s.FixupBlock.Body
        | QsQubitScope s ->
            "Cannot allocate qubits in function" |> CouldNotEvaluate |> raise

    member this.evaluateScope (vars: VariablesDict) (scope: QsScope): unit =
        vars.enterScope()
        for statement in scope.Statements do
            this.evaluateStatement vars statement
        vars.exitScope()

    member this.evaluateFunction (name: QsQualifiedName) (arg: TypedExpression) (types: QsNullable<ImmutableArray<ResolvedType>>): Expr option =
        // TODO: assert compiledCallables contains name
        let callable = this.getCallable name
        // TODO: assert callable.Specializations.Length == 1
        let impl = callable.Specializations.[0].Implementation
        match impl with
        | Provided (specArgs, scope) ->
            let vars = VariablesDict()
            vars.enterScope()
            fillVars vars (StringTuple.fromQsTuple callable.ArgumentTuple, arg.Expression)
            try this.evaluateScope vars scope; None
            with
            | Returned expr ->
                // printfn "Function %O returned %O" name.Name.Value (prettyPrint expr)
                Some expr
            | Failed expr ->
                // printfn "Function %O failed with %O" name.Name.Value (prettyPrint expr)
                None
            | CouldNotEvaluate reason ->
                // printfn "Could not evaluate function %O on arg %O for reason %O" name.Name.Value (prettyPrint arg.Expression) reason
                None
        | _ ->
            // printfn "Implementation not provided for: %O" name
            None

