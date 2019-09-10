// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.CompilerOptimization.MinorTransformations

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

open Utils
open Printer


/// A SyntaxTreeTransformation that does nothing (for debugging purposes)
type internal DoNothingTransformation() =
    inherit SyntaxTreeTransformation()

    override __.Transform x =
        let newX = base.Transform x
        if x <> newX then
            // For some reason, even though we did literally nothing, they aren't equal
            ()
        newX


/// Represents a transformation meant to optimize a syntax tree
type internal OptimizingTransformation() =
    inherit SyntaxTreeTransformation()

    let mutable changed = false

    /// Returns whether the syntax tree has been modified since this function was last called
    member internal this.checkChanged() =
        let x = changed
        changed <- false
        x

    /// Checks whether the syntax tree changed at all
    override this.Transform x =
        let newX = base.Transform x
        if x <> newX then
            let s1 = printNamespace x
            let s2 = printNamespace newX
            if s1 <> s2 then
                printfn "%s made change! Size went from %d to %d" (this.GetType().Name) s1.Length s2.Length
                changed <- true
            else
                // This block shouldn't execute in theory, but it does in practice
                // TODO - figure out what causes this and fix it
                ()
        else
            // This block should execute in theory, but it doesn't in practice
            // TODO - figure out what causes this and fix it
            ()
        newX


/// A ScopeTransformation that counts how many times each variable is referenced
type internal ReferenceCounter() =
    inherit ScopeTransformation()

    let mutable numUses = Map.empty

    /// Returns the number of times the variable with the given name is referenced
    member __.getNumUses name = numUses.TryFind name |? 0

    override this.Expression = { new ExpressionTransformation() with
        override expr.Kind = { new ExpressionKindTransformation() with
            override __.ExpressionTransformation ex = expr.Transform ex
            override __.TypeTransformation t = expr.Type.Transform t

            override __.onIdentifier (sym, tArgs) =
                match sym with
                | LocalVariable name ->
                    numUses <- numUses.Add (name, this.getNumUses name + 1)
                | _ -> ()
                base.onIdentifier (sym, tArgs)
        }
    }


/// A ScopeTransformation that substitutes type parameters according to the given dictionary
type internal ReplaceTypeParams(typeParams: ImmutableDictionary<QsTypeParameter, ResolvedType>) =
    inherit ScopeTransformation()

    let typeMap = typeParams |> Seq.map (function KeyValue (a, b) -> (a.Origin, a.TypeName), b) |> Map

    override __.Expression = { new ExpressionTransformation() with
        override __.Type = { new ExpressionTypeTransformation() with
            override __.onTypeParameter tp =
                let key = tp.Origin, tp.TypeName
                if typeMap.ContainsKey key then
                    typeMap.[key].Resolution
                else
                    base.onTypeParameter tp
            }
    }


/// A ScopeTransformation that tracks what side effects the transformed code could cause
type internal SideEffectChecker() =
    inherit ScopeTransformation()

    let mutable anyQuantum = false
    let mutable anyMutation = false
    let mutable anyInterrupts = false
    let mutable anyOutput = false

    /// Whether the transformed code might have any quantum side effects (such as calling operations)
    member __.hasQuantum = anyQuantum
    /// Whether the transformed code might change the value of any mutable variable
    member __.hasMutation = anyMutation
    /// Whether the transformed code has any statements that interrupt normal control flow (such as returns)
    member __.hasInterrupts = anyInterrupts
    /// Whether the transformed code might output any messages to the console
    member __.hasOutput = anyOutput

    override __.Expression = { new ExpressionTransformation() with
        override expr.Kind = { new ExpressionKindTransformation() with
            override __.ExpressionTransformation ex = expr.Transform ex
            override __.TypeTransformation t = expr.Type.Transform t

            override __.onFunctionCall (method, arg) =
                anyOutput <- true
                base.onFunctionCall (method, arg)

            override __.onOperationCall (method, arg) =
                anyQuantum <- true
                anyOutput <- true
                base.onOperationCall (method, arg)
        }
    }

    override this.StatementKind = { new StatementKindTransformation() with
        override __.ScopeTransformation s = this.Transform s
        override __.ExpressionTransformation ex = this.Expression.Transform ex
        override __.TypeTransformation t = this.Expression.Type.Transform t
        override __.LocationTransformation l = this.onLocation l

        override __.onValueUpdate stm =
            let mutatesState = match stm.Rhs with LocalVarTuple x when isAllDiscarded x -> false | _ -> true
            anyMutation <- anyMutation || mutatesState
            base.onValueUpdate stm

        override __.onReturnStatement stm =
            anyInterrupts <- true
            base.onReturnStatement stm

        override __.onFailStatement stm =
            anyInterrupts <- true
            base.onFailStatement stm
    }


/// A ScopeTransformation that replaces one statement with zero or more statements
type [<AbstractClass>] internal StatementCollectorTransformation() =
    inherit ScopeTransformation()

    abstract member TransformStatement: QsStatementKind -> QsStatementKind seq

    override this.Transform scope =
        let parentSymbols = scope.KnownSymbols
        let statements =
            scope.Statements
            |> Seq.map this.onStatement
            |> Seq.map (fun x -> x.Statement)
            |> Seq.collect this.TransformStatement
            |> Seq.map wrapStmt
        QsScope.New (statements, parentSymbols)


/// A SyntaxTreeTransformation that removes all range information from anywhere in the AST
type internal StripAllRangeInformation() =
    inherit SyntaxTreeTransformation()

    override __.Scope = { new ScopeTransformation() with

        override __.onLocation _ = Null

        override __.Expression = { new ExpressionTransformation() with

            override __.onRangeInformation _ = Null

            override __.Type = { new ExpressionTypeTransformation() with
                override __.onRangeInformation _ = Null
            }
        }
    }
