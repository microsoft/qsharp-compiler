module Microsoft.Quantum.QsCompiler.CompilerOptimization.VariableRenaming

open System.Collections.Immutable
open System.Text.RegularExpressions
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open ComputationExpressions
open Types
open Utils


type VariableRenamer(argTuple: QsArgumentTuple) =
    inherit ScopeTransformation()

    let mutable numUses = Map.empty
    let mutable constants = enterScope (Constants [])

    let demangle varName =
        let m = Regex.Match (varName, "^__qsVar\d+__(.+)__$")
        if m.Success then m.Groups.[1].Value else varName

    let generateUniqueName varName =
        let baseName = demangle varName
        let mutable num, newName = 0, baseName
        while numUses.ContainsKey newName do
            num <- num + 1
            newName <- sprintf "__qsVar%d__%s__" num baseName
        numUses <- Map.add newName 0 numUses
        constants <- defineVar (fun _ -> true) constants (varName, newName)
        newName

    let rec processArgTuple args =
        match args with
        | QsTupleItem item ->
            match item.VariableName with
            | ValidName name -> generateUniqueName name.Value |> ignore
            | InvalidName -> ()
        | QsTuple items -> Seq.iter processArgTuple items

    do processArgTuple argTuple

    member this.getNumUses name =
        Map.tryFind name numUses


    override scope.Transform x =
        constants <- enterScope constants
        let result = base.Transform x
        constants <- exitScope constants
        result

    override scope.Expression = { new ExpressionTransformation() with 
        override expr.Kind = { new ExpressionKindTransformation() with
            override exprKind.ExpressionTransformation ex = expr.Transform ex
            override exprKind.TypeTransformation t = expr.Type.Transform t

            override exprKind.onIdentifier (sym, tArgs) =
                maybe {
                    let! name =
                        match sym with
                        | LocalVariable name -> Some name.Value
                        | _ -> None
                    let! newName = getVar constants name
                    numUses <- Map.add newName (Map.find newName numUses + 1) numUses
                    return Identifier (LocalVariable (NonNullable<_>.New newName), tArgs)
                } |? Identifier (sym, tArgs)
        }
    }

    override scope.StatementKind = { new StatementKindTransformation() with
        override stmtKind.ExpressionTransformation x = scope.Expression.Transform x
        override stmtKind.LocationTransformation x = scope.onLocation x
        override stmtKind.ScopeTransformation x = scope.Transform x
        override stmtKind.TypeTransformation x = scope.Expression.Type.Transform x

        override this.onVariableDeclaration stm = 
            let rhs = this.ExpressionTransformation stm.Rhs
            let lhs = this.onSymbolTuple stm.Lhs
            QsBinding<TypedExpression>.New stm.Kind (lhs, rhs) |> QsVariableDeclaration

        override this.onSymbolTuple syms =
            match syms with
            | VariableName item -> VariableName (NonNullable<_>.New (generateUniqueName item.Value))
            | VariableNameTuple items -> Seq.map this.onSymbolTuple items |> ImmutableArray.CreateRange |> VariableNameTuple
            | InvalidItem | DiscardedItem -> syms
    }
