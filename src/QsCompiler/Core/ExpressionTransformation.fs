// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Numerics
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core.Utils

type private ExpressionKind = 
    QsExpressionKind<TypedExpression,Identifier,ResolvedType>


type ExpressionKindTransformationBase internal (options : TransformationOptions, _internal_) =
    
    let missingTransformation name _ = new InvalidOperationException(sprintf "No %s transformation has been specified." name) |> raise 
    let Node = if options.DisableRebuild then Walk else Fold

    member val internal TypeTransformationHandle = missingTransformation "type" with get, set
    member val internal ExpressionTransformationHandle = missingTransformation "expression" with get, set

    member this.Types = this.TypeTransformationHandle()
    member this.Expressions = this.ExpressionTransformationHandle()

    new (expressionTransformation : unit -> ExpressionTransformationBase, typeTransformation : unit -> TypeTransformationBase, options) as this = 
        new ExpressionKindTransformationBase(options, "_internal_") then 
            this.TypeTransformationHandle <- typeTransformation
            this.ExpressionTransformationHandle <- expressionTransformation

    new (options : TransformationOptions) as this = 
        new ExpressionKindTransformationBase(options, "_internal_") then 
            let typeTransformation = new TypeTransformationBase(options)
            let expressionTransformation = new ExpressionTransformationBase((fun _ -> this), (fun _ -> this.Types), options)
            this.TypeTransformationHandle <- fun _ -> typeTransformation
            this.ExpressionTransformationHandle <- fun _ -> expressionTransformation

    new (expressionTransformation : unit -> ExpressionTransformationBase, typeTransformation : unit -> TypeTransformationBase) = 
        new ExpressionKindTransformationBase(expressionTransformation, typeTransformation, TransformationOptions.Default) 

    new () = new ExpressionKindTransformationBase (TransformationOptions.Default)


    // methods invoked before selective expressions

    abstract member beforeCallLike : TypedExpression * TypedExpression -> TypedExpression * TypedExpression
    default this.beforeCallLike (method, arg) = (method, arg)

    abstract member beforeFunctorApplication : TypedExpression -> TypedExpression
    default this.beforeFunctorApplication ex = ex

    abstract member beforeModifierApplication : TypedExpression -> TypedExpression
    default this.beforeModifierApplication ex = ex

    abstract member beforeBinaryOperatorExpression : TypedExpression * TypedExpression -> TypedExpression * TypedExpression
    default this.beforeBinaryOperatorExpression (lhs, rhs) = (lhs, rhs)

    abstract member beforeUnaryOperatorExpression : TypedExpression -> TypedExpression
    default this.beforeUnaryOperatorExpression ex = ex


    // nodes containing subexpressions or subtypes

    abstract member onIdentifier : Identifier * QsNullable<ImmutableArray<ResolvedType>> -> ExpressionKind
    default this.onIdentifier (sym, tArgs) = 
        let tArgs =  tArgs |> QsNullable<_>.Map (fun ts -> ts |> Seq.map this.Types.Transform |> ImmutableArray.CreateRange)
        Identifier |> Node.BuildOr InvalidExpr (sym, tArgs)

    abstract member onOperationCall : TypedExpression * TypedExpression -> ExpressionKind
    default this.onOperationCall (method, arg) = 
        let method, arg = this.Expressions.Transform method, this.Expressions.Transform arg
        CallLikeExpression |> Node.BuildOr InvalidExpr (method, arg)

    abstract member onFunctionCall : TypedExpression * TypedExpression -> ExpressionKind
    default this.onFunctionCall (method, arg) = 
        let method, arg = this.Expressions.Transform method, this.Expressions.Transform arg
        CallLikeExpression |> Node.BuildOr InvalidExpr (method, arg)

    abstract member onPartialApplication : TypedExpression * TypedExpression -> ExpressionKind
    default this.onPartialApplication (method, arg) = 
        let method, arg = this.Expressions.Transform method, this.Expressions.Transform arg
        CallLikeExpression |> Node.BuildOr InvalidExpr (method, arg)

    abstract member onAdjointApplication : TypedExpression -> ExpressionKind
    default this.onAdjointApplication ex = 
        let ex = this.Expressions.Transform ex
        AdjointApplication |> Node.BuildOr InvalidExpr ex

    abstract member onControlledApplication : TypedExpression -> ExpressionKind
    default this.onControlledApplication ex = 
        let ex = this.Expressions.Transform ex
        ControlledApplication |> Node.BuildOr InvalidExpr ex

    abstract member onUnwrapApplication : TypedExpression -> ExpressionKind
    default this.onUnwrapApplication ex = 
        let ex = this.Expressions.Transform ex
        UnwrapApplication |> Node.BuildOr InvalidExpr ex

    abstract member onValueTuple : ImmutableArray<TypedExpression> -> ExpressionKind
    default this.onValueTuple vs = 
        let values = vs |> Seq.map this.Expressions.Transform |> ImmutableArray.CreateRange
        ValueTuple |> Node.BuildOr InvalidExpr values

    abstract member onArrayItem : TypedExpression * TypedExpression -> ExpressionKind
    default this.onArrayItem (arr, idx) = 
        let arr, idx = this.Expressions.Transform arr, this.Expressions.Transform idx
        ArrayItem |> Node.BuildOr InvalidExpr (arr, idx)

    abstract member onNamedItem : TypedExpression * Identifier -> ExpressionKind
    default this.onNamedItem (ex, acc) = 
        let ex = this.Expressions.Transform ex
        NamedItem |> Node.BuildOr InvalidExpr (ex, acc) 

    abstract member onValueArray : ImmutableArray<TypedExpression> -> ExpressionKind
    default this.onValueArray vs = 
        let values = vs |> Seq.map this.Expressions.Transform |> ImmutableArray.CreateRange
        ValueArray |> Node.BuildOr InvalidExpr values

    abstract member onNewArray : ResolvedType * TypedExpression -> ExpressionKind
    default this.onNewArray (bt, idx) = 
        let bt, idx = this.Types.Transform bt, this.Expressions.Transform idx
        NewArray |> Node.BuildOr InvalidExpr (bt, idx)

    abstract member onStringLiteral : NonNullable<string> * ImmutableArray<TypedExpression> -> ExpressionKind
    default this.onStringLiteral (s, exs) = 
        let exs = exs |> Seq.map this.Expressions.Transform |> ImmutableArray.CreateRange
        StringLiteral |> Node.BuildOr InvalidExpr (s, exs)

    abstract member onRangeLiteral : TypedExpression * TypedExpression -> ExpressionKind
    default this.onRangeLiteral (lhs, rhs) = 
        let lhs, rhs = this.Expressions.Transform lhs, this.Expressions.Transform rhs
        RangeLiteral |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract member onCopyAndUpdateExpression : TypedExpression * TypedExpression * TypedExpression -> ExpressionKind
    default this.onCopyAndUpdateExpression (lhs, accEx, rhs) = 
        let lhs, accEx, rhs = this.Expressions.Transform lhs, this.Expressions.Transform accEx, this.Expressions.Transform rhs
        CopyAndUpdate |> Node.BuildOr InvalidExpr (lhs, accEx, rhs)

    abstract member onConditionalExpression : TypedExpression * TypedExpression * TypedExpression -> ExpressionKind
    default this.onConditionalExpression (cond, ifTrue, ifFalse) = 
        let cond, ifTrue, ifFalse = this.Expressions.Transform cond, this.Expressions.Transform ifTrue, this.Expressions.Transform ifFalse
        CONDITIONAL |> Node.BuildOr InvalidExpr (cond, ifTrue, ifFalse)

    abstract member onEquality : TypedExpression * TypedExpression -> ExpressionKind
    default this.onEquality (lhs, rhs) = 
        let lhs, rhs = this.Expressions.Transform lhs, this.Expressions.Transform rhs
        EQ |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract member onInequality : TypedExpression * TypedExpression -> ExpressionKind
    default this.onInequality (lhs, rhs) = 
        let lhs, rhs = this.Expressions.Transform lhs, this.Expressions.Transform rhs
        NEQ |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract member onLessThan : TypedExpression * TypedExpression -> ExpressionKind
    default this.onLessThan (lhs, rhs) = 
        let lhs, rhs = this.Expressions.Transform lhs, this.Expressions.Transform rhs
        LT |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract member onLessThanOrEqual : TypedExpression * TypedExpression -> ExpressionKind
    default this.onLessThanOrEqual (lhs, rhs) = 
        let lhs, rhs = this.Expressions.Transform lhs, this.Expressions.Transform rhs
        LTE |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract member onGreaterThan : TypedExpression * TypedExpression -> ExpressionKind
    default this.onGreaterThan (lhs, rhs) = 
        let lhs, rhs = this.Expressions.Transform lhs, this.Expressions.Transform rhs
        GT |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract member onGreaterThanOrEqual : TypedExpression * TypedExpression -> ExpressionKind
    default this.onGreaterThanOrEqual (lhs, rhs) =
        let lhs, rhs = this.Expressions.Transform lhs, this.Expressions.Transform rhs    
        GTE |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract member onLogicalAnd : TypedExpression * TypedExpression -> ExpressionKind
    default this.onLogicalAnd (lhs, rhs) = 
        let lhs, rhs = this.Expressions.Transform lhs, this.Expressions.Transform rhs
        AND |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract member onLogicalOr : TypedExpression * TypedExpression -> ExpressionKind
    default this.onLogicalOr (lhs, rhs) = 
        let lhs, rhs = this.Expressions.Transform lhs, this.Expressions.Transform rhs
        OR |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract member onAddition : TypedExpression * TypedExpression -> ExpressionKind
    default this.onAddition (lhs, rhs) = 
        let lhs, rhs = this.Expressions.Transform lhs, this.Expressions.Transform rhs
        ADD |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract member onSubtraction : TypedExpression * TypedExpression -> ExpressionKind
    default this.onSubtraction (lhs, rhs) = 
        let lhs, rhs = this.Expressions.Transform lhs, this.Expressions.Transform rhs
        SUB |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract member onMultiplication : TypedExpression * TypedExpression -> ExpressionKind
    default this.onMultiplication (lhs, rhs) = 
        let lhs, rhs = this.Expressions.Transform lhs, this.Expressions.Transform rhs
        MUL |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract member onDivision : TypedExpression * TypedExpression -> ExpressionKind
    default this.onDivision (lhs, rhs) = 
        let lhs, rhs = this.Expressions.Transform lhs, this.Expressions.Transform rhs
        DIV |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract member onExponentiate : TypedExpression * TypedExpression -> ExpressionKind
    default this.onExponentiate (lhs, rhs) = 
        let lhs, rhs = this.Expressions.Transform lhs, this.Expressions.Transform rhs
        POW |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract member onModulo : TypedExpression * TypedExpression -> ExpressionKind
    default this.onModulo (lhs, rhs) = 
        let lhs, rhs = this.Expressions.Transform lhs, this.Expressions.Transform rhs
        MOD |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract member onLeftShift : TypedExpression * TypedExpression -> ExpressionKind
    default this.onLeftShift (lhs, rhs) = 
        let lhs, rhs = this.Expressions.Transform lhs, this.Expressions.Transform rhs
        LSHIFT |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract member onRightShift : TypedExpression * TypedExpression -> ExpressionKind
    default this.onRightShift (lhs, rhs) = 
        let lhs, rhs = this.Expressions.Transform lhs, this.Expressions.Transform rhs
        RSHIFT |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract member onBitwiseExclusiveOr : TypedExpression * TypedExpression -> ExpressionKind
    default this.onBitwiseExclusiveOr (lhs, rhs) = 
        let lhs, rhs = this.Expressions.Transform lhs, this.Expressions.Transform rhs
        BXOR |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract member onBitwiseOr : TypedExpression * TypedExpression -> ExpressionKind
    default this.onBitwiseOr (lhs, rhs) = 
        let lhs, rhs = this.Expressions.Transform lhs, this.Expressions.Transform rhs
        BOR |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract member onBitwiseAnd : TypedExpression * TypedExpression -> ExpressionKind
    default this.onBitwiseAnd (lhs, rhs) = 
        let lhs, rhs = this.Expressions.Transform lhs, this.Expressions.Transform rhs
        BAND |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract member onLogicalNot : TypedExpression -> ExpressionKind
    default this.onLogicalNot ex = 
        let ex = this.Expressions.Transform ex
        NOT |> Node.BuildOr InvalidExpr ex

    abstract member onNegative : TypedExpression -> ExpressionKind
    default this.onNegative ex = 
        let ex = this.Expressions.Transform ex
        NEG |> Node.BuildOr InvalidExpr ex

    abstract member onBitwiseNot : TypedExpression -> ExpressionKind
    default this.onBitwiseNot ex = 
        let ex = this.Expressions.Transform ex
        BNOT |> Node.BuildOr InvalidExpr ex


    // leaf nodes

    abstract member onUnitValue : unit -> ExpressionKind
    default this.onUnitValue () = ExpressionKind.UnitValue

    abstract member onMissingExpression : unit -> ExpressionKind
    default this.onMissingExpression () = MissingExpr

    abstract member onInvalidExpression : unit -> ExpressionKind
    default this.onInvalidExpression () = InvalidExpr

    abstract member onIntLiteral : int64 -> ExpressionKind
    default this.onIntLiteral i = IntLiteral i

    abstract member onBigIntLiteral : BigInteger -> ExpressionKind
    default this.onBigIntLiteral b = BigIntLiteral b

    abstract member onDoubleLiteral : double -> ExpressionKind
    default this.onDoubleLiteral d = DoubleLiteral d

    abstract member onBoolLiteral : bool -> ExpressionKind
    default this.onBoolLiteral b = BoolLiteral b

    abstract member onResultLiteral : QsResult -> ExpressionKind
    default this.onResultLiteral r = ResultLiteral r

    abstract member onPauliLiteral : QsPauli -> ExpressionKind
    default this.onPauliLiteral p = PauliLiteral p


    // transformation root called on each node

    member private this.dispatchCallLikeExpression (method, arg) = 
        match method.ResolvedType.Resolution with
            | _ when TypedExpression.IsPartialApplication (CallLikeExpression (method, arg)) -> this.onPartialApplication (method, arg) 
            | ExpressionType.Operation _                                                     -> this.onOperationCall (method, arg)
            | _                                                                              -> this.onFunctionCall (method, arg)

    abstract member Transform : ExpressionKind -> ExpressionKind
    default this.Transform kind = 
        if options.Disable then kind else
        let transformed = kind |> function
            | Identifier (sym, tArgs)                          -> this.onIdentifier                 (sym, tArgs)
            | CallLikeExpression (method,arg)                  -> this.dispatchCallLikeExpression   ((method, arg)        |> this.beforeCallLike)
            | AdjointApplication ex                            -> this.onAdjointApplication         (ex                   |> (this.beforeFunctorApplication >> this.beforeModifierApplication))
            | ControlledApplication ex                         -> this.onControlledApplication      (ex                   |> (this.beforeFunctorApplication >> this.beforeModifierApplication))
            | UnwrapApplication ex                             -> this.onUnwrapApplication          (ex                   |> this.beforeModifierApplication)
            | UnitValue                                        -> this.onUnitValue                  ()
            | MissingExpr                                      -> this.onMissingExpression          ()
            | InvalidExpr                                      -> this.onInvalidExpression          () 
            | ValueTuple vs                                    -> this.onValueTuple                 vs
            | ArrayItem (arr, idx)                             -> this.onArrayItem                  (arr, idx)
            | NamedItem (ex, acc)                              -> this.onNamedItem                  (ex, acc)
            | ValueArray vs                                    -> this.onValueArray                 vs
            | NewArray (bt, idx)                               -> this.onNewArray                   (bt, idx)
            | IntLiteral i                                     -> this.onIntLiteral                 i
            | BigIntLiteral b                                  -> this.onBigIntLiteral              b
            | DoubleLiteral d                                  -> this.onDoubleLiteral              d
            | BoolLiteral b                                    -> this.onBoolLiteral                b
            | ResultLiteral r                                  -> this.onResultLiteral              r
            | PauliLiteral p                                   -> this.onPauliLiteral               p
            | StringLiteral (s, exs)                           -> this.onStringLiteral              (s, exs)
            | RangeLiteral (lhs, rhs)                          -> this.onRangeLiteral               (lhs, rhs)
            | CopyAndUpdate (lhs, accEx, rhs)                  -> this.onCopyAndUpdateExpression    (lhs, accEx, rhs)
            | CONDITIONAL (cond, ifTrue, ifFalse)              -> this.onConditionalExpression      (cond, ifTrue, ifFalse)
            | EQ (lhs,rhs)                                     -> this.onEquality                   ((lhs, rhs)          |> this.beforeBinaryOperatorExpression)
            | NEQ (lhs,rhs)                                    -> this.onInequality                 ((lhs, rhs)          |> this.beforeBinaryOperatorExpression)
            | LT (lhs,rhs)                                     -> this.onLessThan                   ((lhs, rhs)          |> this.beforeBinaryOperatorExpression)
            | LTE (lhs,rhs)                                    -> this.onLessThanOrEqual            ((lhs, rhs)          |> this.beforeBinaryOperatorExpression)
            | GT (lhs,rhs)                                     -> this.onGreaterThan                ((lhs, rhs)          |> this.beforeBinaryOperatorExpression)
            | GTE (lhs,rhs)                                    -> this.onGreaterThanOrEqual         ((lhs, rhs)          |> this.beforeBinaryOperatorExpression)
            | AND (lhs,rhs)                                    -> this.onLogicalAnd                 ((lhs, rhs)          |> this.beforeBinaryOperatorExpression)
            | OR (lhs,rhs)                                     -> this.onLogicalOr                  ((lhs, rhs)          |> this.beforeBinaryOperatorExpression)
            | ADD (lhs,rhs)                                    -> this.onAddition                   ((lhs, rhs)          |> this.beforeBinaryOperatorExpression)
            | SUB (lhs,rhs)                                    -> this.onSubtraction                ((lhs, rhs)          |> this.beforeBinaryOperatorExpression)
            | MUL (lhs,rhs)                                    -> this.onMultiplication             ((lhs, rhs)          |> this.beforeBinaryOperatorExpression)
            | DIV (lhs,rhs)                                    -> this.onDivision                   ((lhs, rhs)          |> this.beforeBinaryOperatorExpression)
            | POW (lhs,rhs)                                    -> this.onExponentiate               ((lhs, rhs)          |> this.beforeBinaryOperatorExpression)
            | MOD (lhs,rhs)                                    -> this.onModulo                     ((lhs, rhs)          |> this.beforeBinaryOperatorExpression)
            | LSHIFT (lhs,rhs)                                 -> this.onLeftShift                  ((lhs, rhs)          |> this.beforeBinaryOperatorExpression)
            | RSHIFT (lhs,rhs)                                 -> this.onRightShift                 ((lhs, rhs)          |> this.beforeBinaryOperatorExpression)
            | BXOR (lhs,rhs)                                   -> this.onBitwiseExclusiveOr         ((lhs, rhs)          |> this.beforeBinaryOperatorExpression)
            | BOR (lhs,rhs)                                    -> this.onBitwiseOr                  ((lhs, rhs)          |> this.beforeBinaryOperatorExpression)
            | BAND (lhs,rhs)                                   -> this.onBitwiseAnd                 ((lhs, rhs)          |> this.beforeBinaryOperatorExpression)
            | NOT ex                                           -> this.onLogicalNot                 (ex                  |> this.beforeUnaryOperatorExpression)
            | NEG ex                                           -> this.onNegative                   (ex                  |> this.beforeUnaryOperatorExpression)
            | BNOT ex                                          -> this.onBitwiseNot                 (ex                  |> this.beforeUnaryOperatorExpression)
        id |> Node.BuildOr kind transformed


and ExpressionTransformationBase internal (options : TransformationOptions, _internal_) = 
    
    let missingTransformation name _ = new InvalidOperationException(sprintf "No %s transformation has been specified." name) |> raise 
    let Node = if options.DisableRebuild then Walk else Fold

    member val internal TypeTransformationHandle = missingTransformation "type" with get, set
    member val internal ExpressionKindTransformationHandle = missingTransformation "expression kind" with get, set

    member this.Types = this.TypeTransformationHandle()
    member this.ExpressionKinds = this.ExpressionKindTransformationHandle()

    new (exkindTransformation : unit -> ExpressionKindTransformationBase, typeTransformation : unit -> TypeTransformationBase, options : TransformationOptions) as this = 
        new ExpressionTransformationBase(options, "_internal_") then 
            this.TypeTransformationHandle <- typeTransformation
            this.ExpressionKindTransformationHandle <- exkindTransformation

    new (options : TransformationOptions) as this = 
        new ExpressionTransformationBase(options, "_internal_") then
            let typeTransformation = new TypeTransformationBase(options)
            let exprKindTransformation = new ExpressionKindTransformationBase((fun _ -> this), (fun _ -> this.Types), options)
            this.TypeTransformationHandle <- fun _ -> typeTransformation
            this.ExpressionKindTransformationHandle <- fun _ -> exprKindTransformation

    new (exkindTransformation : unit -> ExpressionKindTransformationBase, typeTransformation : unit -> TypeTransformationBase) =
        new ExpressionTransformationBase(exkindTransformation, typeTransformation, TransformationOptions.Default)

    new () = new ExpressionTransformationBase(TransformationOptions.Default)


    // supplementary expression information 

    abstract member onRangeInformation : QsNullable<QsPositionInfo*QsPositionInfo> -> QsNullable<QsPositionInfo*QsPositionInfo>
    default this.onRangeInformation r = r

    abstract member onExpressionInformation : InferredExpressionInformation -> InferredExpressionInformation
    default this.onExpressionInformation info = info


    // nodes containing subexpressions or subtypes

    /// If DisableRebuild is set to true, this method won't walk the types in the dictionary.
    abstract member onTypeParamResolutions : ImmutableDictionary<(QsQualifiedName*NonNullable<string>), ResolvedType> -> ImmutableDictionary<(QsQualifiedName*NonNullable<string>), ResolvedType>
    default this.onTypeParamResolutions typeParams =
        let asTypeParameter (key) = QsTypeParameter.New (fst key, snd key, Null)
        let filteredTypeParams = 
            typeParams 
            |> Seq.map (fun kv -> this.Types.onTypeParameter (kv.Key |> asTypeParameter), kv.Value)
            |> Seq.choose (function | TypeParameter tp, value -> Some ((tp.Origin, tp.TypeName), this.Types.Transform value) | _ -> None)
            |> Seq.map (fun (key, value) -> new KeyValuePair<_,_>(key, value))
        ImmutableDictionary.CreateRange |> Node.BuildOr typeParams filteredTypeParams


    // transformation root called on each node

    abstract member Transform : TypedExpression -> TypedExpression
    default this.Transform (ex : TypedExpression) =
        if options.Disable then ex else
        let range                = this.onRangeInformation ex.Range
        let typeParamResolutions = this.onTypeParamResolutions ex.TypeParameterResolutions
        let kind                 = this.ExpressionKinds.Transform ex.Expression
        let exType               = this.Types.Transform ex.ResolvedType
        let inferredInfo         = this.onExpressionInformation ex.InferredInformation
        TypedExpression.New |> Node.BuildOr ex (kind, typeParamResolutions, exType, inferredInfo, range)
