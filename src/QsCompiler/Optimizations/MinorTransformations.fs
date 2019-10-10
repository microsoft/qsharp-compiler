// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Optimizations.MinorTransformations

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.Optimizations.Utils
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core


/// A SyntaxTreeTransformation that finds identifiers in each implementation that represent distict values.
/// Should be called at the QsCallable level, not as the QsNamespace level, as it's meant to operate on a single callable.
type internal FindDistinctQubits() =
    inherit SyntaxTreeTransformation()

    let mutable _distinctNames = Set.empty

    /// A set of identifier names that we expect to represent distinct values
    member __.distinctNames = _distinctNames

    override __.onProvidedImplementation (argTuple, body) =
        argTuple |> toSymbolTuple |> flatten |> Seq.iter (function
        | VariableName name -> _distinctNames <- _distinctNames.Add name
        | _ -> ())
        base.onProvidedImplementation (argTuple, body)

    override __.Scope = { new ScopeTransformation() with
        override this.StatementKind = { new StatementKindTransformation() with
            override __.ScopeTransformation s = this.Transform s
            override __.ExpressionTransformation ex = ex
            override __.TypeTransformation t = t
            override __.LocationTransformation l = l

            override __.onQubitScope stm =
                stm.Binding.Lhs |> flatten |> Seq.iter (function
                | VariableName name -> _distinctNames <- _distinctNames.Add name
                | _ -> ())
                base.onQubitScope stm
        }
    }


/// A ScopeTransformation that tracks what variables the transformed code could mutate
type internal MutationChecker() =
    inherit ScopeTransformation()

    let mutable declaredVars = Set.empty
    let mutable mutatedVars = Set.empty

    /// The set of variables that this code doesn't declare but does mutate
    member __.externalMutations = mutatedVars - declaredVars

    override this.StatementKind = { new StatementKindTransformation() with
        override __.ScopeTransformation s = this.Transform s
        override __.ExpressionTransformation ex = ex
        override __.TypeTransformation t = t
        override __.LocationTransformation l = l

        override __.onVariableDeclaration stm =
            flatten stm.Lhs |> Seq.iter (function
            | VariableName name -> declaredVars <- declaredVars.Add name
            | _ -> ())
            base.onVariableDeclaration stm

        override __.onValueUpdate stm =
            match stm.Lhs with
            | LocalVarTuple v ->
                flatten v |> Seq.iter (function
                | VariableName name -> mutatedVars <- mutatedVars.Add name
                | _ -> ())
            | _ -> ()
            base.onValueUpdate stm
    }


/// Represents a transformation meant to optimize a syntax tree
type internal OptimizingTransformation() =
    inherit SyntaxTreeTransformation()

    let mutable changed = false

    /// Returns whether the syntax tree has been modified since this function was last called
    member internal __.checkChanged() =
        let x = changed
        changed <- false
        x

    /// Checks whether the syntax tree changed at all
    override __.Transform x =
        let newX = base.Transform x
        if (x.Elements, x.Name) <> (newX.Elements, newX.Name) then
            changed <- true
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
            override __.TypeTransformation t = t

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
            override __.TypeTransformation t = t

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
        override __.TypeTransformation t = t
        override __.LocationTransformation l = l

        override __.onValueUpdate stm =
            let mutatesState = match stm.Lhs with LocalVarTuple x when isAllDiscarded x -> false | _ -> true
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


/// A SyntaxTreeTransformation that removes all known symbols from anywhere in the AST
type internal StripAllKnownSymbols() =
    inherit SyntaxTreeTransformation()

    override __.Scope = { new ScopeTransformation() with
        override this.Transform scope =
            QsScope.New (scope.Statements |> Seq.map this.onStatement, LocalDeclarations.Empty)
    }
