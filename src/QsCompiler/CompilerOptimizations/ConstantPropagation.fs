module CompilerOptimizations

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open Utils
open ExpressionEvaluation


type CP_SyntaxTree(compiledCallables: ImmutableDictionary<QsQualifiedName, QsCallable>) =
    inherit SyntaxTreeTransformation()
    let vars = VariablesDict()

    let mutable changed = true
    let mutable prevChanged = false

    member this.checkChanged() =
          let x = changed
          changed <- false
          x

    member this.markChanged() =
        prevChanged <- changed
        changed <- true

    member this.undoMarkChanged() =
        changed <- prevChanged

    override syntaxTree.Scope = { new ScopeTransformation() with

        override scope.Transform x =
            vars.enterScope()
            let result = base.Transform x
            vars.exitScope()
            result

        override scope.Expression = upcast { new ExpressionEvaluator(vars, compiledCallables, 2) with
            override ee.Transform x =
                let newX = base.Transform x
                if x.ToString() <> newX.ToString() then changed <- true
                newX }

        override scope.StatementKind = { new StatementKindTransformation() with 
            override statementKind.ExpressionTransformation x = scope.Expression.Transform x
            override statementKind.LocationTransformation x = scope.onLocation x
            override statementKind.ScopeTransformation x = scope.Transform x
            override statementKind.TypeTransformation x = scope.Expression.Type.Transform x

            override statementKind.onVariableDeclaration stm =
                let lhs = statementKind.onSymbolTuple stm.Lhs
                let rhs = statementKind.ExpressionTransformation stm.Rhs
                if stm.Kind = ImmutableBinding then
                    if isLiteral rhs.Expression then
                        fillVars vars (StringTuple.fromSymbolTuple lhs, rhs.Expression)
                        printfn "Found constant declaration: %O = %O" (StringTuple.fromSymbolTuple lhs) (prettyPrint rhs.Expression)
                QsBinding<TypedExpression>.New stm.Kind (lhs, rhs) |> QsVariableDeclaration
        }
    }
