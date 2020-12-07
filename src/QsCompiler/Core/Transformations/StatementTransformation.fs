// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

open System
open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core.Utils


type StatementKindTransformationBase internal (options : TransformationOptions, _internal_) =

    let missingTransformation name _ = new InvalidOperationException(sprintf "No %s transformation has been specified." name) |> raise 
    let Node = if options.Rebuild then Fold else Walk

    member val internal ExpressionTransformationHandle = missingTransformation "expression" with get, set
    member val internal StatementTransformationHandle = missingTransformation "statement" with get, set

    member this.Expressions = this.ExpressionTransformationHandle()
    member this.Statements = this.StatementTransformationHandle()

    new (statementTransformation : unit -> StatementTransformationBase, expressionTransformation : unit -> ExpressionTransformationBase, options : TransformationOptions) as this = 
        new StatementKindTransformationBase(options, "_internal_") then 
            this.ExpressionTransformationHandle <- expressionTransformation
            this.StatementTransformationHandle <- statementTransformation

    new (options : TransformationOptions) as this = 
        new StatementKindTransformationBase(options, "_internal_") then
            let expressionTransformation = new ExpressionTransformationBase(options)
            let statementTransformation = new StatementTransformationBase((fun _ -> this), (fun _ -> this.Expressions), options)
            this.ExpressionTransformationHandle <- fun _ -> expressionTransformation
            this.StatementTransformationHandle <- fun _ -> statementTransformation

    new (statementTransformation : unit -> StatementTransformationBase, expressionTransformation : unit -> ExpressionTransformationBase) =
        new StatementKindTransformationBase(statementTransformation, expressionTransformation, TransformationOptions.Default)

    new () = new StatementKindTransformationBase(TransformationOptions.Default)


    // subconstructs used within statements 

    abstract member OnSymbolTuple : SymbolTuple -> SymbolTuple
    default this.OnSymbolTuple syms = syms

    abstract member OnQubitInitializer : ResolvedInitializer -> ResolvedInitializer 
    default this.OnQubitInitializer init = 
        let transformed = init.Resolution |> function 
            | SingleQubitAllocation              -> SingleQubitAllocation
            | QubitRegisterAllocation ex as orig -> QubitRegisterAllocation |> Node.BuildOr orig (this.Expressions.OnTypedExpression ex)
            | QubitTupleAllocation is as orig    -> QubitTupleAllocation |> Node.BuildOr orig (is |> Seq.map this.OnQubitInitializer |> ImmutableArray.CreateRange)
            | InvalidInitializer                 -> InvalidInitializer
        ResolvedInitializer.New |> Node.BuildOr init transformed

    abstract member OnPositionedBlock : QsNullable<TypedExpression> * QsPositionedBlock -> QsNullable<TypedExpression> * QsPositionedBlock
    default this.OnPositionedBlock (intro : QsNullable<TypedExpression>, block : QsPositionedBlock) = 
        let location = this.Statements.OnLocation block.Location
        let comments = block.Comments
        let expr = intro |> QsNullable<_>.Map this.Expressions.OnTypedExpression
        let body = this.Statements.OnScope block.Body
        let PositionedBlock (expr,  body, location, comments) = expr, QsPositionedBlock.New comments location body
        PositionedBlock |> Node.BuildOr (intro, block) (expr, body, location, comments)


    // statements containing subconstructs or expressions

    abstract member OnVariableDeclaration : QsBinding<TypedExpression> -> QsStatementKind
    default this.OnVariableDeclaration stm = 
        let rhs = this.Expressions.OnTypedExpression stm.Rhs
        let lhs = this.OnSymbolTuple stm.Lhs
        QsVariableDeclaration << QsBinding<TypedExpression>.New stm.Kind |> Node.BuildOr EmptyStatement (lhs, rhs) 

    abstract member OnValueUpdate : QsValueUpdate -> QsStatementKind
    default this.OnValueUpdate stm = 
        let rhs = this.Expressions.OnTypedExpression stm.Rhs
        let lhs = this.Expressions.OnTypedExpression stm.Lhs
        QsValueUpdate << QsValueUpdate.New |> Node.BuildOr EmptyStatement (lhs, rhs) 

    abstract member OnConditionalStatement : QsConditionalStatement -> QsStatementKind
    default this.OnConditionalStatement stm = 
        let cases = stm.ConditionalBlocks |> Seq.map (fun (c, b) -> 
            let cond, block = this.OnPositionedBlock (Value c, b)
            let invalidCondition () = failwith "missing condition in if-statement"
            cond.ValueOrApply invalidCondition, block) |> ImmutableArray.CreateRange
        let defaultCase = stm.Default |> QsNullable<_>.Map (fun b -> this.OnPositionedBlock (Null, b) |> snd)
        QsConditionalStatement << QsConditionalStatement.New |> Node.BuildOr EmptyStatement (cases, defaultCase)

    abstract member OnForStatement : QsForStatement -> QsStatementKind
    default this.OnForStatement stm = 
        let iterVals = this.Expressions.OnTypedExpression stm.IterationValues
        let loopVar = fst stm.LoopItem |> this.OnSymbolTuple
        let loopVarType = this.Expressions.Types.OnType (snd stm.LoopItem)
        let body = this.Statements.OnScope stm.Body
        QsForStatement << QsForStatement.New |> Node.BuildOr EmptyStatement ((loopVar, loopVarType), iterVals, body)

    abstract member OnWhileStatement : QsWhileStatement -> QsStatementKind
    default this.OnWhileStatement stm = 
        let condition = this.Expressions.OnTypedExpression stm.Condition
        let body = this.Statements.OnScope stm.Body
        QsWhileStatement << QsWhileStatement.New |> Node.BuildOr EmptyStatement (condition, body)

    abstract member OnRepeatStatement : QsRepeatStatement -> QsStatementKind
    default this.OnRepeatStatement stm = 
        let repeatBlock = this.OnPositionedBlock (Null, stm.RepeatBlock) |> snd
        let successCondition, fixupBlock = this.OnPositionedBlock (Value stm.SuccessCondition, stm.FixupBlock)
        let invalidCondition () = failwith "missing success condition in repeat-statement"
        QsRepeatStatement << QsRepeatStatement.New |> Node.BuildOr EmptyStatement (repeatBlock, successCondition.ValueOrApply invalidCondition, fixupBlock)

    abstract member OnConjugation : QsConjugation -> QsStatementKind
    default this.OnConjugation stm = 
        let outer = this.OnPositionedBlock (Null, stm.OuterTransformation) |> snd
        let inner = this.OnPositionedBlock (Null, stm.InnerTransformation) |> snd
        QsConjugation << QsConjugation.New |> Node.BuildOr EmptyStatement (outer, inner) 

    abstract member OnExpressionStatement : TypedExpression -> QsStatementKind
    default this.OnExpressionStatement ex = 
        let transformed = this.Expressions.OnTypedExpression ex 
        QsExpressionStatement |> Node.BuildOr EmptyStatement transformed

    abstract member OnReturnStatement : TypedExpression -> QsStatementKind
    default this.OnReturnStatement ex = 
        let transformed = this.Expressions.OnTypedExpression ex 
        QsReturnStatement |> Node.BuildOr EmptyStatement transformed

    abstract member OnFailStatement : TypedExpression -> QsStatementKind
    default this.OnFailStatement ex = 
        let transformed = this.Expressions.OnTypedExpression ex
        QsFailStatement |> Node.BuildOr EmptyStatement transformed

    /// This method is defined for the sole purpose of eliminating code duplication for each of the specialization kinds. 
    /// It is hence not intended and should never be needed for public use. 
    member private this.OnQubitScopeKind (stm : QsQubitScope) = 
        let kind = stm.Kind
        let rhs = this.OnQubitInitializer stm.Binding.Rhs
        let lhs = this.OnSymbolTuple stm.Binding.Lhs
        let body = this.Statements.OnScope stm.Body
        QsQubitScope << QsQubitScope.New kind |> Node.BuildOr EmptyStatement ((lhs, rhs), body)

    abstract member OnAllocateQubits : QsQubitScope -> QsStatementKind
    default this.OnAllocateQubits stm = this.OnQubitScopeKind stm

    abstract member OnBorrowQubits : QsQubitScope -> QsStatementKind
    default this.OnBorrowQubits stm = this.OnQubitScopeKind stm

    abstract member OnQubitScope : QsQubitScope -> QsStatementKind
    default this.OnQubitScope (stm : QsQubitScope) = 
        match stm.Kind with 
        | Allocate -> this.OnAllocateQubits stm
        | Borrow   -> this.OnBorrowQubits stm


    // leaf nodes
    
    abstract member OnEmptyStatement : unit -> QsStatementKind
    default this.OnEmptyStatement () = EmptyStatement


    // transformation root called on each statement

    abstract member OnStatementKind : QsStatementKind -> QsStatementKind
    default this.OnStatementKind kind = 
        if not options.Enable then kind else
        let transformed = kind |> function
            | QsExpressionStatement ex   -> this.OnExpressionStatement  ex
            | QsReturnStatement ex       -> this.OnReturnStatement      ex
            | QsFailStatement ex         -> this.OnFailStatement        ex
            | QsVariableDeclaration stm  -> this.OnVariableDeclaration  stm
            | QsValueUpdate stm          -> this.OnValueUpdate          stm
            | QsConditionalStatement stm -> this.OnConditionalStatement stm
            | QsForStatement stm         -> this.OnForStatement         stm
            | QsWhileStatement stm       -> this.OnWhileStatement       stm
            | QsRepeatStatement stm      -> this.OnRepeatStatement      stm
            | QsConjugation stm          -> this.OnConjugation          stm
            | QsQubitScope stm           -> this.OnQubitScope           stm
            | EmptyStatement             -> this.OnEmptyStatement       ()
        id |> Node.BuildOr kind transformed


and StatementTransformationBase internal (options : TransformationOptions, _internal_) =

    let missingTransformation name _ = new InvalidOperationException(sprintf "No %s transformation has been specified." name) |> raise 
    let Node = if options.Rebuild then Fold else Walk

    member val internal ExpressionTransformationHandle = missingTransformation "expression" with get, set
    member val internal StatementKindTransformationHandle = missingTransformation "statement kind" with get, set

    member this.Expressions = this.ExpressionTransformationHandle()
    member this.StatementKinds = this.StatementKindTransformationHandle()

    new (statementKindTransformation : unit -> StatementKindTransformationBase, expressionTransformation : unit -> ExpressionTransformationBase, options : TransformationOptions) as this = 
        new StatementTransformationBase(options, "_internal_") then 
            this.ExpressionTransformationHandle <- expressionTransformation
            this.StatementKindTransformationHandle <- statementKindTransformation

    new (options : TransformationOptions) as this = 
        new StatementTransformationBase(options, "_internal_") then
            let expressionTransformation = new ExpressionTransformationBase(options)
            let statementTransformation = new StatementKindTransformationBase((fun _ -> this), (fun _ -> this.Expressions), options)
            this.ExpressionTransformationHandle <- fun _ -> expressionTransformation
            this.StatementKindTransformationHandle <- fun _ -> statementTransformation

    new (statementKindTransformation : unit -> StatementKindTransformationBase, expressionTransformation : unit -> ExpressionTransformationBase) = 
        new StatementTransformationBase(statementKindTransformation, expressionTransformation, TransformationOptions.Default)

    new () = new StatementTransformationBase(TransformationOptions.Default)


    // supplementary statement information 

    abstract member OnLocation : QsNullable<QsLocation> -> QsNullable<QsLocation>
    default this.OnLocation loc = loc

    abstract member OnVariableName : string -> string
    default this.OnVariableName name = name

    abstract member OnLocalDeclarations : LocalDeclarations -> LocalDeclarations
    default this.OnLocalDeclarations decl = 
        let onLocalVariableDeclaration (local : LocalVariableDeclaration<string>) =
            let loc = local.Position, local.Range
            let name = this.OnVariableName local.VariableName
            let varType = this.Expressions.Types.OnType local.Type 
            let info = this.Expressions.OnExpressionInformation local.InferredInformation
            LocalVariableDeclaration.New info.IsMutable (loc, name, varType, info.HasLocalQuantumDependency)
        let variableDeclarations = decl.Variables |> Seq.map onLocalVariableDeclaration
        if options.Rebuild then LocalDeclarations.New (variableDeclarations |> ImmutableArray.CreateRange)
        else variableDeclarations |> Seq.iter ignore; decl

    // transformation roots called on each statement or statement block

    abstract member OnStatement : QsStatement -> QsStatement
    default this.OnStatement stm = 
        if not options.Enable then stm else
        let location = this.OnLocation stm.Location
        let comments = stm.Comments
        let kind = this.StatementKinds.OnStatementKind stm.Statement
        let varDecl = this.OnLocalDeclarations stm.SymbolDeclarations
        QsStatement.New comments location |> Node.BuildOr stm (kind, varDecl)

    abstract member OnScope : QsScope -> QsScope 
    default this.OnScope scope = 
        if not options.Enable then scope else
        let parentSymbols = this.OnLocalDeclarations scope.KnownSymbols
        let statements = scope.Statements |> Seq.map this.OnStatement
        if options.Rebuild then QsScope.New (statements |> ImmutableArray.CreateRange, parentSymbols)
        else statements |> Seq.iter ignore; scope
