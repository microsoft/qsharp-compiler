namespace Microsoft.Quantum.QsCompiler.CompilerOptimization

open System.Collections.Generic
open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open Microsoft.Quantum.QsCompiler.CompilerOptimization.Maybe
open Microsoft.Quantum.QsCompiler.CompilerOptimization.Utils
open Microsoft.Quantum.QsCompiler.CompilerOptimization.ExpressionEvaluation


/// The SyntaxTreeTransformation used to evaluate constants
type ConstantPropagator(compiledCallables: ImmutableDictionary<QsQualifiedName, QsCallable>) =
    inherit SyntaxTreeTransformation()
    let vars = VariablesDict()
    let callableDict = {compiledCallables = compiledCallables}

    // For determining if constant folding should be rerun
    let mutable changed = true

    /// Returns whether the syntax tree has been modified since this function was last called
    member this.checkChanged() =
          let x = changed
          changed <- false
          x

    /// Checks whether the syntax tree changed at all
    override this.Transform x =
        let newX = base.Transform x
        if x <> newX then changed <- true
        newX

    /// The ScopeTransformation used to evaluate constants
    override syntaxTree.Scope = { new ScopeTransformation() with

        override scope.Transform x =
            vars.enterScope()
            let result = base.Transform x
            vars.exitScope()
            result

        /// The ExpressionTransformation used to evaluate constant expressions
        override scope.Expression = upcast ExpressionEvaluator(vars, callableDict, 10)

        /// The StatementKindTransformation used to evaluate constants
        override scope.StatementKind = upcast { new StatementOptimizer(vars, callableDict) with 
            override so.ExpressionTransformation x = scope.Expression.Transform x
            override so.LocationTransformation x = scope.onLocation x
            override so.ScopeTransformation x = scope.Transform x
            override so.TypeTransformation x = scope.Expression.Type.Transform x

            override so.onVariableDeclaration stm =
                let lhs = so.onSymbolTuple stm.Lhs
                let rhs = so.ExpressionTransformation stm.Rhs
                if stm.Kind = ImmutableBinding then
                    if isLiteral rhs.Expression callableDict then
                        fillVars vars (StringTuple.fromSymbolTuple lhs, rhs.Expression)
                QsBinding<TypedExpression>.New stm.Kind (lhs, rhs) |> QsVariableDeclaration
        }
    }


/// The StatementKindTransformation used to simplify statements
and [<AbstractClass>] internal StatementOptimizer(vars: VariablesDict, callableDict: CallableDict) =
    inherit StatementKindTransformation()


    override this.onExpressionStatement ex =
        let ex = this.ExpressionTransformation ex
        maybe {
            let! qualName, newScope = tryInline ex callableDict
            let mySet = HashSet()
            findAllCalls newScope callableDict mySet
            let! _ = if not (mySet.Contains qualName) then Some() else None
            return this.ScopeTransformation newScope |> QsScopeStatement.New |> QsScopeStatement
        } |? QsExpressionStatement ex


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


    override this.onForStatement stm =
        let loopVar = fst stm.LoopItem |> this.onSymbolTuple
        let iterVals = this.ExpressionTransformation stm.IterationValues
        let iterValsAsSeq =
            match iterVals.Expression with
            | RangeLiteral _ -> 
                rangeLiteralToSeq iterVals.Expression |> Seq.map (fun x -> wrapExpr (IntLiteral x) Int) |> Some
            | ValueArray va -> va :> seq<_> |> Some
            | _ -> None

        match iterValsAsSeq with
        | Some s ->
            let iterRange = 
                s |> Seq.map (fun x ->
                    let variableDecl = QsBinding.New ImmutableBinding (loopVar, x) |> QsVariableDeclaration
                    let variableDeclStatement = {
                        Statement = variableDecl
                        SymbolDeclarations = stm.Body.KnownSymbols
                        Location = Null
                        Comments = QsComments.New ([], []) }
                    let innerScope =
                        { stm.Body with 
                            Statements = stm.Body.Statements.Insert(0, variableDeclStatement) }
                    {   Statement = innerScope |> QsScopeStatement.New |> QsScopeStatement
                        SymbolDeclarations = stm.Body.KnownSymbols
                        Location = Null
                        Comments = QsComments.New ([], []) })
            let outerScope = QsScope.New (iterRange, stm.Body.KnownSymbols)
            let result = outerScope |> QsScopeStatement.New |> QsScopeStatement |> this.Transform
            result
        | None ->
            let loopVarType = this.TypeTransformation (snd stm.LoopItem)
            let body = this.ScopeTransformation stm.Body
            QsForStatement.New ((loopVar, loopVarType), iterVals, body) |> QsForStatement

