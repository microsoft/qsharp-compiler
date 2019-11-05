// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

open System.Collections.Immutable
open System.Numerics
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree

//type private ExpressionKind = QsExpressionKind<TypedExpression,Identifier,ResolvedType>
//type private ExpressionType = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>


/// Convention: 
/// All methods starting with "on" implement the walk for an expression of a certain kind.
/// All methods starting with "before" group a set of statements, and are called before walking the set
/// even if the corresponding walk routine (starting with "on") is overridden.
/// 
/// These classes differ from the "*Transformation" classes in that these classes visit every node in the
/// syntax tree, but don't create a new syntax tree, while the Transformation classes generate a new (or
/// at least partially new) tree from the old one.
/// Effectively, the Transformation classes implement fold, while the Walker classes implement iter.
[<AbstractClass>]
type ExpressionKindWalker(?enable) =
    let enable = defaultArg enable true

    abstract member ExpressionWalker : TypedExpression -> unit
    abstract member TypeWalker : ResolvedType -> unit

    abstract member beforeCallLike : TypedExpression * TypedExpression -> unit
    default this.beforeCallLike (method, arg) = ()

    abstract member beforeFunctorApplication : TypedExpression -> unit
    default this.beforeFunctorApplication ex = ()

    abstract member beforeModifierApplication : TypedExpression -> unit
    default this.beforeModifierApplication ex = ()

    abstract member beforeBinaryOperatorExpression : TypedExpression * TypedExpression -> unit
    default this.beforeBinaryOperatorExpression (lhs, rhs) = ()

    abstract member beforeUnaryOperatorExpression : TypedExpression -> unit
    default this.beforeUnaryOperatorExpression ex = ()


    abstract member onIdentifier : Identifier * QsNullable<ImmutableArray<ResolvedType>> -> unit
    default this.onIdentifier (sym, tArgs) = tArgs |> QsNullable<_>.Iter (fun ts -> (ts |> Seq.iter this.TypeWalker))

    abstract member onOperationCall : TypedExpression * TypedExpression -> unit
    default this.onOperationCall (method, arg) = 
        this.ExpressionWalker method
        this.ExpressionWalker arg

    abstract member onFunctionCall : TypedExpression * TypedExpression -> unit
    default this.onFunctionCall (method, arg) = 
        this.ExpressionWalker method
        this.ExpressionWalker arg

    abstract member onPartialApplication : TypedExpression * TypedExpression -> unit
    default this.onPartialApplication (method, arg) = 
        this.ExpressionWalker method
        this.ExpressionWalker arg

    abstract member onAdjointApplication : TypedExpression -> unit
    default this.onAdjointApplication ex = this.ExpressionWalker ex

    abstract member onControlledApplication : TypedExpression -> unit
    default this.onControlledApplication ex = this.ExpressionWalker ex

    abstract member onUnwrapApplication : TypedExpression -> unit
    default this.onUnwrapApplication ex = this.ExpressionWalker ex

    abstract member onUnitValue : unit -> unit
    default this.onUnitValue () = ()

    abstract member onMissingExpression : unit -> unit
    default this.onMissingExpression () = ()

    abstract member onInvalidExpression : unit -> unit
    default this.onInvalidExpression () = ()

    abstract member onValueTuple : ImmutableArray<TypedExpression> -> unit
    default this.onValueTuple vs = vs |> Seq.iter this.ExpressionWalker

    abstract member onArrayItem : TypedExpression * TypedExpression -> unit
    default this.onArrayItem (arr, idx) = 
        this.ExpressionWalker arr
        this.ExpressionWalker idx

    abstract member onNamedItem : TypedExpression * Identifier -> unit
    default this.onNamedItem (ex, acc) = this.ExpressionWalker ex

    abstract member onValueArray : ImmutableArray<TypedExpression> -> unit
    default this.onValueArray vs = vs |> Seq.iter this.ExpressionWalker

    abstract member onNewArray : ResolvedType * TypedExpression -> unit
    default this.onNewArray (bt, idx) = 
        this.TypeWalker bt
        this.ExpressionWalker idx

    abstract member onIntLiteral : int64 -> unit
    default this.onIntLiteral i = ()

    abstract member onBigIntLiteral : BigInteger -> unit
    default this.onBigIntLiteral b = ()

    abstract member onDoubleLiteral : double -> unit
    default this.onDoubleLiteral d = ()

    abstract member onBoolLiteral : bool -> unit
    default this.onBoolLiteral b = ()

    abstract member onResultLiteral : QsResult -> unit
    default this.onResultLiteral r = ()

    abstract member onPauliLiteral : QsPauli -> unit
    default this.onPauliLiteral p = ()

    abstract member onStringLiteral : NonNullable<string> * ImmutableArray<TypedExpression> -> unit
    default this.onStringLiteral (s, exs) = exs |> Seq.iter this.ExpressionWalker

    abstract member onRangeLiteral : TypedExpression * TypedExpression -> unit
    default this.onRangeLiteral (lhs, rhs) = 
        this.ExpressionWalker lhs 
        this.ExpressionWalker rhs

    abstract member onCopyAndUpdateExpression : TypedExpression * TypedExpression * TypedExpression -> unit
    default this.onCopyAndUpdateExpression (lhs, accEx, rhs) = 
        this.ExpressionWalker lhs
        this.ExpressionWalker accEx
        this.ExpressionWalker rhs

    abstract member onConditionalExpression : TypedExpression * TypedExpression * TypedExpression -> unit
    default this.onConditionalExpression (cond, ifTrue, ifFalse) = 
        this.ExpressionWalker cond
        this.ExpressionWalker ifTrue
        this.ExpressionWalker ifFalse

    abstract member onEquality : TypedExpression * TypedExpression -> unit
    default this.onEquality (lhs, rhs) = 
        this.ExpressionWalker lhs
        this.ExpressionWalker rhs

    abstract member onInequality : TypedExpression * TypedExpression -> unit
    default this.onInequality (lhs, rhs) = 
        this.ExpressionWalker lhs
        this.ExpressionWalker rhs

    abstract member onLessThan : TypedExpression * TypedExpression -> unit
    default this.onLessThan (lhs, rhs) = 
        this.ExpressionWalker lhs
        this.ExpressionWalker rhs

    abstract member onLessThanOrEqual : TypedExpression * TypedExpression -> unit
    default this.onLessThanOrEqual (lhs, rhs) = 
        this.ExpressionWalker lhs
        this.ExpressionWalker rhs

    abstract member onGreaterThan : TypedExpression * TypedExpression -> unit
    default this.onGreaterThan (lhs, rhs) = 
        this.ExpressionWalker lhs
        this.ExpressionWalker rhs

    abstract member onGreaterThanOrEqual : TypedExpression * TypedExpression -> unit
    default this.onGreaterThanOrEqual (lhs, rhs) = 
        this.ExpressionWalker lhs
        this.ExpressionWalker rhs

    abstract member onLogicalAnd : TypedExpression * TypedExpression -> unit
    default this.onLogicalAnd (lhs, rhs) = 
        this.ExpressionWalker lhs
        this.ExpressionWalker rhs

    abstract member onLogicalOr : TypedExpression * TypedExpression -> unit
    default this.onLogicalOr (lhs, rhs) = 
        this.ExpressionWalker lhs
        this.ExpressionWalker rhs

    abstract member onAddition : TypedExpression * TypedExpression -> unit
    default this.onAddition (lhs, rhs) = 
        this.ExpressionWalker lhs
        this.ExpressionWalker rhs

    abstract member onSubtraction : TypedExpression * TypedExpression -> unit
    default this.onSubtraction (lhs, rhs) = 
        this.ExpressionWalker lhs
        this.ExpressionWalker rhs

    abstract member onMultiplication : TypedExpression * TypedExpression -> unit
    default this.onMultiplication (lhs, rhs) = 
        this.ExpressionWalker lhs
        this.ExpressionWalker rhs

    abstract member onDivision : TypedExpression * TypedExpression -> unit
    default this.onDivision (lhs, rhs) = 
        this.ExpressionWalker lhs
        this.ExpressionWalker rhs

    abstract member onExponentiate : TypedExpression * TypedExpression -> unit
    default this.onExponentiate (lhs, rhs) = 
        this.ExpressionWalker lhs
        this.ExpressionWalker rhs

    abstract member onModulo : TypedExpression * TypedExpression -> unit
    default this.onModulo (lhs, rhs) = 
        this.ExpressionWalker lhs
        this.ExpressionWalker rhs

    abstract member onLeftShift : TypedExpression * TypedExpression -> unit
    default this.onLeftShift (lhs, rhs) = 
        this.ExpressionWalker lhs
        this.ExpressionWalker rhs

    abstract member onRightShift : TypedExpression * TypedExpression -> unit
    default this.onRightShift (lhs, rhs) = 
        this.ExpressionWalker lhs
        this.ExpressionWalker rhs

    abstract member onBitwiseExclusiveOr : TypedExpression * TypedExpression -> unit
    default this.onBitwiseExclusiveOr (lhs, rhs) = 
        this.ExpressionWalker lhs
        this.ExpressionWalker rhs

    abstract member onBitwiseOr : TypedExpression * TypedExpression -> unit
    default this.onBitwiseOr (lhs, rhs) = 
        this.ExpressionWalker lhs
        this.ExpressionWalker rhs

    abstract member onBitwiseAnd : TypedExpression * TypedExpression -> unit
    default this.onBitwiseAnd (lhs, rhs) = 
        this.ExpressionWalker lhs
        this.ExpressionWalker rhs

    abstract member onLogicalNot : TypedExpression -> unit
    default this.onLogicalNot ex = this.ExpressionWalker ex

    abstract member onNegative : TypedExpression -> unit
    default this.onNegative ex = this.ExpressionWalker ex

    abstract member onBitwiseNot : TypedExpression -> unit
    default this.onBitwiseNot ex = this.ExpressionWalker ex


    member private this.dispatchCallLikeExpression (method, arg) = 
        match method.ResolvedType.Resolution with
            | _ when TypedExpression.IsPartialApplication (CallLikeExpression (method, arg)) -> this.onPartialApplication (method, arg) 
            | ExpressionType.Operation _                                                     -> this.onOperationCall (method, arg)
            | _                                                                              -> this.onFunctionCall (method, arg)

    abstract member Walk : ExpressionKind -> unit
    default this.Walk kind = 
        if not enable then () else 
        match kind with 
        | Identifier (sym, tArgs)                          -> this.onIdentifier                 (sym, tArgs)
        | CallLikeExpression (method,arg)                  -> this.beforeCallLike               (method, arg)
                                                              this.dispatchCallLikeExpression   (method, arg)
        | AdjointApplication ex                            -> this.beforeFunctorApplication     ex
                                                              this.beforeModifierApplication    ex
                                                              this.onAdjointApplication         ex
        | ControlledApplication ex                         -> this.beforeFunctorApplication     ex
                                                              this.beforeModifierApplication    ex
                                                              this.onControlledApplication      ex
        | UnwrapApplication ex                             -> this.beforeModifierApplication    ex
                                                              this.onUnwrapApplication          ex
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
        | EQ (lhs,rhs)                                     -> this.beforeBinaryOperatorExpression (lhs, rhs)
                                                              this.onEquality                   (lhs, rhs)
        | NEQ (lhs,rhs)                                    -> this.beforeBinaryOperatorExpression (lhs, rhs)
                                                              this.onInequality                 (lhs, rhs)
        | LT (lhs,rhs)                                     -> this.beforeBinaryOperatorExpression (lhs, rhs)
                                                              this.onLessThan                   (lhs, rhs)
        | LTE (lhs,rhs)                                    -> this.beforeBinaryOperatorExpression (lhs, rhs)
                                                              this.onLessThanOrEqual            (lhs, rhs)
        | GT (lhs,rhs)                                     -> this.beforeBinaryOperatorExpression (lhs, rhs)
                                                              this.onGreaterThan                (lhs, rhs)
        | GTE (lhs,rhs)                                    -> this.beforeBinaryOperatorExpression (lhs, rhs)
                                                              this.onGreaterThanOrEqual         (lhs, rhs)
        | AND (lhs,rhs)                                    -> this.beforeBinaryOperatorExpression (lhs, rhs)
                                                              this.onLogicalAnd                 (lhs, rhs)
        | OR (lhs,rhs)                                     -> this.beforeBinaryOperatorExpression (lhs, rhs)
                                                              this.onLogicalOr                  (lhs, rhs)
        | ADD (lhs,rhs)                                    -> this.beforeBinaryOperatorExpression (lhs, rhs)
                                                              this.onAddition                   (lhs, rhs)
        | SUB (lhs,rhs)                                    -> this.beforeBinaryOperatorExpression (lhs, rhs)
                                                              this.onSubtraction                (lhs, rhs)
        | MUL (lhs,rhs)                                    -> this.beforeBinaryOperatorExpression (lhs, rhs)
                                                              this.onMultiplication             (lhs, rhs)
        | DIV (lhs,rhs)                                    -> this.beforeBinaryOperatorExpression (lhs, rhs)
                                                              this.onDivision                   (lhs, rhs)
        | POW (lhs,rhs)                                    -> this.beforeBinaryOperatorExpression (lhs, rhs)
                                                              this.onExponentiate               (lhs, rhs)
        | MOD (lhs,rhs)                                    -> this.beforeBinaryOperatorExpression (lhs, rhs)
                                                              this.onModulo                     (lhs, rhs)
        | LSHIFT (lhs,rhs)                                 -> this.beforeBinaryOperatorExpression (lhs, rhs)
                                                              this.onLeftShift                  (lhs, rhs)
        | RSHIFT (lhs,rhs)                                 -> this.beforeBinaryOperatorExpression (lhs, rhs)
                                                              this.onRightShift                 (lhs, rhs)
        | BXOR (lhs,rhs)                                   -> this.beforeBinaryOperatorExpression (lhs, rhs)
                                                              this.onBitwiseExclusiveOr         (lhs, rhs)
        | BOR (lhs,rhs)                                    -> this.beforeBinaryOperatorExpression (lhs, rhs)
                                                              this.onBitwiseOr                  (lhs, rhs)
        | BAND (lhs,rhs)                                   -> this.beforeBinaryOperatorExpression (lhs, rhs)
                                                              this.onBitwiseAnd                 (lhs, rhs)
        | NOT ex                                           -> this.beforeUnaryOperatorExpression ex
                                                              this.onLogicalNot                 ex
        | NEG ex                                           -> this.beforeUnaryOperatorExpression ex
                                                              this.onNegative                   ex
        | BNOT ex                                          -> this.beforeUnaryOperatorExpression ex
                                                              this.onBitwiseNot                 ex


and ExpressionTypeWalker(?enable) = 
    let enable = defaultArg enable true

    abstract member onRangeInformation : QsRangeInfo -> unit
    default this.onRangeInformation r =()

    abstract member onCharacteristicsExpression : ResolvedCharacteristics -> unit
    default this.onCharacteristicsExpression fs = ()

    abstract member onCallableInformation : CallableInformation -> unit
    default this.onCallableInformation opInfo = 
        this.onCharacteristicsExpression opInfo.Characteristics

    abstract member onUserDefinedType : UserDefinedType -> unit
    default this.onUserDefinedType udt = 
        this.onRangeInformation udt.Range

    abstract member onTypeParameter : QsTypeParameter -> unit
    default this.onTypeParameter tp = 
        this.onRangeInformation tp.Range

    abstract member onUnitType : unit -> unit
    default this.onUnitType () = ()

    abstract member onOperation : (ResolvedType * ResolvedType) * CallableInformation -> unit
    default this.onOperation ((it, ot), info) = 
        this.Walk it
        this.Walk ot
        this.onCallableInformation info

    abstract member onFunction : ResolvedType * ResolvedType -> unit
    default this.onFunction (it, ot) = 
        this.Walk it
        this.Walk ot

    abstract member onTupleType : ImmutableArray<ResolvedType> -> unit
    default this.onTupleType ts = ts |> Seq.iter this.Walk

    abstract member onArrayType : ResolvedType -> unit
    default this.onArrayType b = this.Walk b

    abstract member onQubit : unit -> unit
    default this.onQubit () = ()

    abstract member onMissingType : unit -> unit
    default this.onMissingType () = ()

    abstract member onInvalidType : unit -> unit
    default this.onInvalidType () = ()

    abstract member onInt : unit -> unit
    default this.onInt () = ()

    abstract member onBigInt : unit -> unit
    default this.onBigInt () = ()

    abstract member onDouble : unit -> unit
    default this.onDouble () = ()

    abstract member onBool : unit -> unit
    default this.onBool () = ()

    abstract member onString : unit -> unit
    default this.onString () = ()

    abstract member onResult : unit -> unit
    default this.onResult () = ()

    abstract member onPauli : unit -> unit
    default this.onPauli () = ()

    abstract member onRange : unit -> unit
    default this.onRange () = ()

    member this.Walk (t : ResolvedType) =
        if not enable then () else
        match t.Resolution with
        | ExpressionType.UnitType                    -> this.onUnitType ()
        | ExpressionType.Operation ((it, ot), fs)    -> this.onOperation ((it, ot), fs)
        | ExpressionType.Function (it, ot)           -> this.onFunction (it, ot)
        | ExpressionType.TupleType ts                -> this.onTupleType ts
        | ExpressionType.ArrayType b                 -> this.onArrayType b
        | ExpressionType.UserDefinedType udt         -> this.onUserDefinedType udt
        | ExpressionType.TypeParameter tp            -> this.onTypeParameter tp
        | ExpressionType.Qubit                       -> this.onQubit ()
        | ExpressionType.MissingType                 -> this.onMissingType ()
        | ExpressionType.InvalidType                 -> this.onInvalidType ()
        | ExpressionType.Int                         -> this.onInt ()
        | ExpressionType.BigInt                      -> this.onBigInt ()
        | ExpressionType.Double                      -> this.onDouble ()
        | ExpressionType.Bool                        -> this.onBool ()
        | ExpressionType.String                      -> this.onString ()
        | ExpressionType.Result                      -> this.onResult ()
        | ExpressionType.Pauli                       -> this.onPauli ()
        | ExpressionType.Range                       -> this.onRange ()


and ExpressionWalker(?enableKindWalkers) = 
    let enableKind = defaultArg enableKindWalkers true
    let typeWalker = new ExpressionTypeWalker()

    abstract member Kind : ExpressionKindWalker
    default this.Kind = {
        new ExpressionKindWalker (enableKind) with 
            override x.ExpressionWalker ex = this.Walk ex
            override x.TypeWalker t = this.Type.Walk t
        }

    abstract member Type : ExpressionTypeWalker
    default this.Type = typeWalker

    abstract member onRangeInformation : QsNullable<QsPositionInfo*QsPositionInfo> -> unit
    default this.onRangeInformation r = ()

    abstract member onExpressionInformation : InferredExpressionInformation -> unit
    default this.onExpressionInformation info = ()

    abstract member Walk : TypedExpression -> unit
    default this.Walk (ex : TypedExpression) =
        this.onRangeInformation ex.Range
        this.Kind.Walk ex.Expression
        this.Type.Walk ex.ResolvedType
        this.onExpressionInformation ex.InferredInformation
