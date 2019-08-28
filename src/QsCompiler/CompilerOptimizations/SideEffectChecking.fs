// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.CompilerOptimization.SideEffectChecking

open Microsoft.Quantum.QsCompiler.Transformations.Core

open Utils


type internal SideEffectChecker() =
    inherit ScopeTransformation()

    let mutable anyQuantum = false
    let mutable anyMutation = false
    let mutable anyInterrupts = false

    member __.hasQuantum = anyQuantum
    member __.hasMutation = anyMutation
    member __.hasInterrupts = anyInterrupts

    override __.Expression = { new ExpressionTransformation() with
        override expr.Kind = { new ExpressionKindTransformation() with
            override __.ExpressionTransformation ex = expr.Transform ex
            override __.TypeTransformation t = expr.Type.Transform t

            override __.onOperationCall (method, arg) =
                anyQuantum <- true
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
