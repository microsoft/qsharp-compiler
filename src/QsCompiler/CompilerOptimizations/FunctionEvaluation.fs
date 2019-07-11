module FunctionEvaluation

open System
open System.Collections.Generic
open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree

open Utils


type [<AbstractClass>] FunctionEvaluator(compiledCallables: ImmutableDictionary<QsQualifiedName, QsCallable>) =

    abstract member evaluateExpression: VariablesDict -> TypedExpression -> Expr

    member this.evaluateStatement (vars: VariablesDict) (statement: QsStatement) =
        match statement.Statement with
        | QsExpressionStatement expr -> this.evaluateExpression vars expr |> ignore
        | QsReturnStatement expr ->
            vars.defineVar("__return__", this.evaluateExpression vars expr)
        | QsFailStatement expr -> vars.defineVar("__fail__", this.evaluateExpression vars expr)
        | QsVariableDeclaration s -> fillVars vars (StringTuple.fromSymbolTuple s.Lhs, this.evaluateExpression vars s.Rhs)
        | QsValueUpdate s -> match s.Lhs.Expression with
            | Identifier (LocalVariable name, tArgs) -> vars.setVar(name.Value, this.evaluateExpression vars s.Rhs)
            | _ -> Console.WriteLine("Unknown LHS of value update statement: " + s.Lhs.Expression.ToString())
        | QsConditionalStatement s ->
            match Seq.tryFind (fun (ts, block) -> this.evaluateExpression vars ts = BoolLiteral true) s.ConditionalBlocks with
            | Some (ts, block) -> this.evaluateScope vars block.Body
            | None -> match s.Default with
                | Value body -> this.evaluateScope vars body.Body
                | Null -> ()
        | QsForStatement s ->
            let n, t = s.LoopItem
            for loopValue in rangeLiteralToSeq s.IterationValues.Expression do
                vars.enterScope()
                fillVars vars (StringTuple.fromSymbolTuple n, IntLiteral (int64 loopValue))
                this.evaluateScope vars s.Body
                vars.exitScope()
        | QsWhileStatement s ->
            while this.evaluateExpression vars s.Condition = BoolLiteral true do
                this.evaluateScope vars s.Body
        | QsRepeatStatement s ->
            let mutable cont = true
            while cont do
                this.evaluateScope vars s.RepeatBlock.Body
                cont <- this.evaluateExpression vars s.SuccessCondition = BoolLiteral false
                if not cont then
                    this.evaluateScope vars s.FixupBlock.Body
        | QsQubitScope s ->
            failwith "Allocating qubits not allowed in functions"


    member this.evaluateScope (vars: VariablesDict) (scope: QsScope) =
        vars.enterScope()
        for statement in scope.Statements do
            if vars.getVar("__return__") = None then
                this.evaluateStatement vars statement
        if vars.getVar("__return__") = None then
            vars.exitScope()

    member this.evaluateFunction (name: QsQualifiedName) (arg: TypedExpression) (types: QsNullable<ImmutableArray<ResolvedType>>) =
        // TODO: assert compiledCallables contains name
        let callable = compiledCallables.[name]
        // TODO: assert callable.Specializations.Length == 1
        let impl = callable.Specializations.[0].Implementation
        // TODO: assert impl is Provided
        match impl with Provided (specArgs, scope) ->
            let vars = VariablesDict()
            vars.enterScope()
            fillVars vars (StringTuple.fromQsTuple callable.ArgumentTuple, arg.Expression)
            this.evaluateScope vars scope
            // TODO: assert vars.getVar is not None
            match vars.getVar "__return__" with Some value -> value

