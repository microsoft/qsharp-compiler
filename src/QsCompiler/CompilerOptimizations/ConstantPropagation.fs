namespace Microsoft.Quantum.QsCompiler.CompilerOptimization

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open Microsoft.Quantum.QsCompiler.CompilerOptimization.Utils
open Microsoft.Quantum.QsCompiler.CompilerOptimization.ExpressionEvaluation


/// The SyntaxTreeTransformation used to evaluate constants
type ConstantPropagator(compiledCallables: ImmutableDictionary<QsQualifiedName, QsCallable>) =
    inherit SyntaxTreeTransformation()
    let vars = VariablesDict()
    let callableDict = {compiledCallables = compiledCallables}

    // For determining if constant folding should be rerun
    let mutable changed = true 
    let mutable prevChanged = false
    
    // For logging the constants we found
    let mutable declarations = []

    /// Returns whether the syntax tree has been modified since this function was last called
    member this.checkChanged() =
          let x = changed
          changed <- false
          x

    /// Marks the syntax tree as having changed
    member this.markChanged() =
        prevChanged <- changed
        changed <- true

    /// Ignores the most recent marking of the syntax tree changing
    member this.undoMarkChanged() =
        changed <- prevChanged

    /// Gets a sorted list of the names of all the constant local variables.
    /// Used for temporary logging/testing purposes, will be removed.
    member this.getDeclarations =
        declarations |> List.sort |> Seq.ofList
        
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
                        declarations <- declarations @ [sprintf "%O = %O" (StringTuple.fromSymbolTuple lhs) (prettyPrint rhs.Expression)]
                        // printfn "Found constant declaration: %O = %O" (StringTuple.fromSymbolTuple lhs) (prettyPrint rhs.Expression)
                QsBinding<TypedExpression>.New stm.Kind (lhs, rhs) |> QsVariableDeclaration
        }
    }
