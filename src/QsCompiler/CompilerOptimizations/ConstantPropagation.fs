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
    let cd = {compiledCallables = compiledCallables}

    let mutable changed = true
    let mutable prevChanged = false
    let mutable foundConstants = []

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

    /// Marks the given local variables as constants
    member this.addFoundConstants(lhs) =
        match lhs with
        | VariableName n -> foundConstants <- foundConstants @ [n.Value]
        | VariableNameTuple t -> t |> Seq.map (fun x -> this.addFoundConstants x) |> List.ofSeq |> ignore
        | _ -> ()

    /// Gets a sorted list of the names of all the constant local variables
    member this.getFoundConstants =
        foundConstants |> List.sort |> Seq.ofList
        
    /// The ScopeTransformation used to evaluate constants
    override syntaxTree.Scope = { new ScopeTransformation() with

        override scope.Transform x =
            vars.enterScope()
            let result = base.Transform x
            vars.exitScope()
            result
            
        /// The ExpressionTransformation used to evaluate constant expressions
        override scope.Expression = upcast { new ExpressionEvaluator(vars, cd, 10) with
            override ee.Transform x =
                let newX = base.Transform x
                if x.ToString() <> newX.ToString() then changed <- true
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
                    if isLiteral rhs.Expression cd then
                        fillVars vars (StringTuple.fromSymbolTuple lhs, rhs.Expression)
                        syntaxTree.addFoundConstants lhs
                        // printfn "Found constant declaration: %O = %O" (StringTuple.fromSymbolTuple lhs) (prettyPrint rhs.Expression)
                QsBinding<TypedExpression>.New stm.Kind (lhs, rhs) |> QsVariableDeclaration
        }
    }
