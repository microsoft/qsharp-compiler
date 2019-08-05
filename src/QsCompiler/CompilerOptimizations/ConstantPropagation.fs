namespace Microsoft.Quantum.QsCompiler.CompilerOptimization

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open Microsoft.Quantum.QsCompiler.CompilerOptimization.Utils
open Microsoft.Quantum.QsCompiler.CompilerOptimization.ExpressionEvaluation
open Microsoft.Quantum.QsCompiler.CompilerOptimization.Printer


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

    /// Marks the syntax tree as having changed
    member this.markChanged() =
        changed <- true

    /// The ScopeTransformation used to evaluate constants
    override syntaxTree.Scope = { new ScopeTransformation() with

        override scope.Transform x =
            vars.enterScope()
            let result = base.Transform x
            vars.exitScope()
            result
            
        /// The ExpressionTransformation used to evaluate constant expressions
        override scope.Expression = upcast { new ExpressionEvaluator(vars, callableDict, 10) with
            override ee.Transform x =
                let newX = base.Transform x
                if x <> newX then changed <- true
                newX }
                
        /// The StatementKindTransformation used to evaluate constants
        override scope.StatementKind = { new StatementKindTransformation() with 
            override statementKind.ExpressionTransformation x = scope.Expression.Transform x
            override statementKind.LocationTransformation x = scope.onLocation x
            override statementKind.ScopeTransformation x = scope.Transform x
            override statementKind.TypeTransformation x = scope.Expression.Type.Transform x

            override statementKind.onVariableDeclaration stm =
                let lhs = statementKind.onSymbolTuple stm.Lhs
                let rhs = statementKind.ExpressionTransformation stm.Rhs
                if stm.Kind = ImmutableBinding then
                    if isLiteral rhs.Expression callableDict then
                        fillVars vars (StringTuple.fromSymbolTuple lhs, rhs.Expression)
                        // printfn "Found constant declaration: %O = %O" (StringTuple.fromSymbolTuple lhs) (prettyPrint rhs.Expression)
                QsBinding<TypedExpression>.New stm.Kind (lhs, rhs) |> QsVariableDeclaration

            override statementKind.onConditionalStatement stm = 
                let cbList, cbListEnd =
                    stm.ConditionalBlocks |> Seq.fold (fun s (c, b) ->
                        let cond, block = statementKind.onPositionedBlock (Some c, b)
                        match cond.Value.Expression with
                        | BoolLiteral true -> s @ [None, block]
                        | BoolLiteral false -> s
                        | _ -> s @ [cond, block]
                    ) [] |> List.ofSeq |> takeWhilePlus1 (fun (c, b) -> c <> None)
                let defaultCase = stm.Default |> QsNullable<_>.Map (fun b -> statementKind.onPositionedBlock (None, b) |> snd)

                let newDefault =
                    match cbListEnd, defaultCase with
                    | Some (_, a), _ -> Value a
                    | _, Value a -> Value a
                    | _ -> Null
                match cbList, newDefault with
                | [], Value x ->
                    x.Body |> QsScopeStatement.New |> QsScopeStatement
                | [], Null ->
                    QsScope.New ([], LocalDeclarations.New []) |> QsScopeStatement.New |> QsScopeStatement
                | _ ->
                    let cases = cbList |> Seq.map (fun (c, b) -> (Option.get c, b))
                    QsConditionalStatement.New (cases, defaultCase) |> QsConditionalStatement

            override statementKind.onForStatement stm =
                let loopVar = fst stm.LoopItem |> statementKind.onSymbolTuple
                let iterVals = statementKind.ExpressionTransformation stm.IterationValues
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
                    let result = outerScope |> QsScopeStatement.New |> QsScopeStatement |> statementKind.Transform
                    // printfn "Unrolled for loop!\nOld statement was:\n%O\nNew statement is:\n%O" (printStm 0 (QsForStatement stm)) (printStm 0 result)
                    result
                | None ->
                    let loopVarType = statementKind.TypeTransformation (snd stm.LoopItem)
                    let body = statementKind.ScopeTransformation stm.Body
                    QsForStatement.New ((loopVar, loopVarType), iterVals, body) |> QsForStatement
        }
    }
