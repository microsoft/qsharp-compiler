// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

open System.Collections.Immutable
open System.Numerics
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree

type private ExpressionKind = QsExpressionKind<TypedExpression,Identifier,ResolvedType>
type private ExpressionType = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>


/// Convention: 
/// All methods starting with "on" implement the transformation for an expression of a certain kind.
/// All methods starting with "before" group a set of statements, and are called before applying the transformation
/// even if the corresponding transformation routine (starting with "on") is overridden.
[<AbstractClass>]
type ExpressionKindTransformation(?enable) =
    let enable = defaultArg enable true

    abstract member ExpressionTransformation : TypedExpression -> TypedExpression
    abstract member TypeTransformation : ResolvedType -> ResolvedType

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


    abstract member onIdentifier : Identifier * QsNullable<ImmutableArray<ResolvedType>> -> ExpressionKind
    default this.onIdentifier (sym, tArgs) = Identifier (sym, tArgs |> QsNullable<_>.Map (fun ts -> (ts |> Seq.map this.TypeTransformation).ToImmutableArray()))

    abstract member onOperationCall : TypedExpression * TypedExpression -> ExpressionKind
    default this.onOperationCall (method, arg) = CallLikeExpression (this.ExpressionTransformation method, this.ExpressionTransformation arg)

    abstract member onFunctionCall : TypedExpression * TypedExpression -> ExpressionKind
    default this.onFunctionCall (method, arg) = CallLikeExpression (this.ExpressionTransformation method, this.ExpressionTransformation arg)

    abstract member onPartialApplication : TypedExpression * TypedExpression -> ExpressionKind
    default this.onPartialApplication (method, arg) = CallLikeExpression (this.ExpressionTransformation method, this.ExpressionTransformation arg)

    abstract member onAdjointApplication : TypedExpression -> ExpressionKind
    default this.onAdjointApplication ex = AdjointApplication (this.ExpressionTransformation ex)

    abstract member onControlledApplication : TypedExpression -> ExpressionKind
    default this.onControlledApplication ex = ControlledApplication (this.ExpressionTransformation ex)

    abstract member onUnwrapApplication : TypedExpression -> ExpressionKind
    default this.onUnwrapApplication ex = UnwrapApplication (this.ExpressionTransformation ex)

    abstract member onUnitValue : unit -> ExpressionKind
    default this.onUnitValue () = ExpressionKind.UnitValue

    abstract member onMissingExpression : unit -> ExpressionKind
    default this.onMissingExpression () = MissingExpr

    abstract member onInvalidExpression : unit -> ExpressionKind
    default this.onInvalidExpression () = InvalidExpr

    abstract member onValueTuple : ImmutableArray<TypedExpression> -> ExpressionKind
    default this.onValueTuple vs = ValueTuple ((vs |> Seq.map this.ExpressionTransformation).ToImmutableArray())

    abstract member onArrayItem : TypedExpression * TypedExpression -> ExpressionKind
    default this.onArrayItem (arr, idx) = ArrayItem (this.ExpressionTransformation arr, this.ExpressionTransformation idx)

    abstract member onNamedItem : TypedExpression * Identifier -> ExpressionKind
    default this.onNamedItem (ex, acc) = NamedItem (this.ExpressionTransformation ex, acc) 

    abstract member onValueArray : ImmutableArray<TypedExpression> -> ExpressionKind
    default this.onValueArray vs = ValueArray ((vs |> Seq.map this.ExpressionTransformation).ToImmutableArray())

    abstract member onNewArray : ResolvedType * TypedExpression -> ExpressionKind
    default this.onNewArray (bt, idx) = NewArray (this.TypeTransformation bt, this.ExpressionTransformation idx)

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

    abstract member onStringLiteral : NonNullable<string> * ImmutableArray<TypedExpression> -> ExpressionKind
    default this.onStringLiteral (s, exs) = StringLiteral (s, (exs |> Seq.map this.ExpressionTransformation).ToImmutableArray())

    abstract member onRangeLiteral : TypedExpression * TypedExpression -> ExpressionKind
    default this.onRangeLiteral (lhs, rhs) = RangeLiteral (this.ExpressionTransformation lhs, this.ExpressionTransformation rhs)

    abstract member onCopyAndUpdateExpression : TypedExpression * TypedExpression * TypedExpression -> ExpressionKind
    default this.onCopyAndUpdateExpression (lhs, accEx, rhs) = CopyAndUpdate (this.ExpressionTransformation lhs, this.ExpressionTransformation accEx, this.ExpressionTransformation rhs)

    abstract member onConditionalExpression : TypedExpression * TypedExpression * TypedExpression -> ExpressionKind
    default this.onConditionalExpression (cond, ifTrue, ifFalse) = CONDITIONAL (this.ExpressionTransformation cond, this.ExpressionTransformation ifTrue, this.ExpressionTransformation ifFalse)

    abstract member onEquality : TypedExpression * TypedExpression -> ExpressionKind
    default this.onEquality (lhs, rhs) = EQ (this.ExpressionTransformation lhs, this.ExpressionTransformation rhs)

    abstract member onInequality : TypedExpression * TypedExpression -> ExpressionKind
    default this.onInequality (lhs, rhs) = NEQ (this.ExpressionTransformation lhs, this.ExpressionTransformation rhs)

    abstract member onLessThan : TypedExpression * TypedExpression -> ExpressionKind
    default this.onLessThan (lhs, rhs) = LT (this.ExpressionTransformation lhs, this.ExpressionTransformation rhs)

    abstract member onLessThanOrEqual : TypedExpression * TypedExpression -> ExpressionKind
    default this.onLessThanOrEqual (lhs, rhs) = LTE (this.ExpressionTransformation lhs, this.ExpressionTransformation rhs)

    abstract member onGreaterThan : TypedExpression * TypedExpression -> ExpressionKind
    default this.onGreaterThan (lhs, rhs) = GT (this.ExpressionTransformation lhs, this.ExpressionTransformation rhs)

    abstract member onGreaterThanOrEqual : TypedExpression * TypedExpression -> ExpressionKind
    default this.onGreaterThanOrEqual (lhs, rhs) = GTE (this.ExpressionTransformation lhs, this.ExpressionTransformation rhs)

    abstract member onLogicalAnd : TypedExpression * TypedExpression -> ExpressionKind
    default this.onLogicalAnd (lhs, rhs) = AND (this.ExpressionTransformation lhs, this.ExpressionTransformation rhs)

    abstract member onLogicalOr : TypedExpression * TypedExpression -> ExpressionKind
    default this.onLogicalOr (lhs, rhs) = OR (this.ExpressionTransformation lhs, this.ExpressionTransformation rhs)

    abstract member onAddition : TypedExpression * TypedExpression -> ExpressionKind
    default this.onAddition (lhs, rhs) = ADD (this.ExpressionTransformation lhs, this.ExpressionTransformation rhs)

    abstract member onSubtraction : TypedExpression * TypedExpression -> ExpressionKind
    default this.onSubtraction (lhs, rhs) = SUB (this.ExpressionTransformation lhs, this.ExpressionTransformation rhs)

    abstract member onMultiplication : TypedExpression * TypedExpression -> ExpressionKind
    default this.onMultiplication (lhs, rhs) = MUL (this.ExpressionTransformation lhs, this.ExpressionTransformation rhs)

    abstract member onDivision : TypedExpression * TypedExpression -> ExpressionKind
    default this.onDivision (lhs, rhs) = DIV (this.ExpressionTransformation lhs, this.ExpressionTransformation rhs)

    abstract member onExponentiate : TypedExpression * TypedExpression -> ExpressionKind
    default this.onExponentiate (lhs, rhs) = POW (this.ExpressionTransformation lhs, this.ExpressionTransformation rhs)

    abstract member onModulo : TypedExpression * TypedExpression -> ExpressionKind
    default this.onModulo (lhs, rhs) = MOD (this.ExpressionTransformation lhs, this.ExpressionTransformation rhs)

    abstract member onLeftShift : TypedExpression * TypedExpression -> ExpressionKind
    default this.onLeftShift (lhs, rhs) = LSHIFT (this.ExpressionTransformation lhs, this.ExpressionTransformation rhs)

    abstract member onRightShift : TypedExpression * TypedExpression -> ExpressionKind
    default this.onRightShift (lhs, rhs) = RSHIFT (this.ExpressionTransformation lhs, this.ExpressionTransformation rhs)

    abstract member onBitwiseExclusiveOr : TypedExpression * TypedExpression -> ExpressionKind
    default this.onBitwiseExclusiveOr (lhs, rhs) = BXOR (this.ExpressionTransformation lhs, this.ExpressionTransformation rhs)

    abstract member onBitwiseOr : TypedExpression * TypedExpression -> ExpressionKind
    default this.onBitwiseOr (lhs, rhs) = BOR (this.ExpressionTransformation lhs, this.ExpressionTransformation rhs)

    abstract member onBitwiseAnd : TypedExpression * TypedExpression -> ExpressionKind
    default this.onBitwiseAnd (lhs, rhs) = BAND (this.ExpressionTransformation lhs, this.ExpressionTransformation rhs)

    abstract member onLogicalNot : TypedExpression -> ExpressionKind
    default this.onLogicalNot ex = NOT (this.ExpressionTransformation ex)

    abstract member onNegative : TypedExpression -> ExpressionKind
    default this.onNegative ex = NEG (this.ExpressionTransformation ex)

    abstract member onBitwiseNot : TypedExpression -> ExpressionKind
    default this.onBitwiseNot ex = BNOT (this.ExpressionTransformation ex)


    member private this.dispatchCallLikeExpression (method, arg) = 
        match method.ResolvedType.Resolution with
            | _ when TypedExpression.IsPartialApplication (CallLikeExpression (method, arg)) -> this.onPartialApplication (method, arg) 
            | ExpressionType.Operation _                                                     -> this.onOperationCall (method, arg)
            | _                                                                              -> this.onFunctionCall (method, arg)

    member this.Transform kind = 
        if not enable then kind else 
        match kind with 
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


and ExpressionTypeTransformation(?enable) = 
    let enable = defaultArg enable true

    abstract member onRangeInformation : QsRangeInfo -> QsRangeInfo
    default this.onRangeInformation r = r

    abstract member onCharacteristicsExpression : ResolvedCharacteristics -> ResolvedCharacteristics
    default this.onCharacteristicsExpression fs = fs

    abstract member onCallableInformation : CallableInformation -> CallableInformation
    default this.onCallableInformation opInfo = 
        let inferred = opInfo.InferredInformation
        let characteristics = this.onCharacteristicsExpression opInfo.Characteristics
        CallableInformation.New (characteristics, inferred)

    abstract member onUserDefinedType : UserDefinedType -> ExpressionType
    default this.onUserDefinedType udt = 
        let ns, name = udt.Namespace, udt.Name
        let range = this.onRangeInformation udt.Range
        UserDefinedType.New (ns, name, range) |> ExpressionType.UserDefinedType

    abstract member onTypeParameter : QsTypeParameter -> ExpressionType
    default this.onTypeParameter tp = 
        let origin = tp.Origin
        let name = tp.TypeName
        let range = this.onRangeInformation tp.Range
        QsTypeParameter.New (origin.Namespace, origin.Name, name, range) |> ExpressionType.TypeParameter

    abstract member onUnitType : unit -> ExpressionType
    default this.onUnitType () = ExpressionType.UnitType

    abstract member onOperation : (ResolvedType * ResolvedType) * CallableInformation -> ExpressionType
    default this.onOperation ((it, ot), info) = ExpressionType.Operation ((this.Transform it, this.Transform ot), this.onCallableInformation info)

    abstract member onFunction : ResolvedType * ResolvedType -> ExpressionType
    default this.onFunction (it, ot) = ExpressionType.Function (this.Transform it, this.Transform ot)

    abstract member onTupleType : ImmutableArray<ResolvedType> -> ExpressionType
    default this.onTupleType ts = ExpressionType.TupleType ((ts |> Seq.map this.Transform).ToImmutableArray())

    abstract member onArrayType : ResolvedType -> ExpressionType
    default this.onArrayType b = ExpressionType.ArrayType (this.Transform b)

    abstract member onQubit : unit -> ExpressionType
    default this.onQubit () = ExpressionType.Qubit

    abstract member onMissingType : unit -> ExpressionType
    default this.onMissingType () = ExpressionType.MissingType

    abstract member onInvalidType : unit -> ExpressionType
    default this.onInvalidType () = ExpressionType.InvalidType

    abstract member onInt : unit -> ExpressionType
    default this.onInt () = ExpressionType.Int

    abstract member onBigInt : unit -> ExpressionType
    default this.onBigInt () = ExpressionType.BigInt

    abstract member onDouble : unit -> ExpressionType
    default this.onDouble () = ExpressionType.Double

    abstract member onBool : unit -> ExpressionType
    default this.onBool () = ExpressionType.Bool

    abstract member onString : unit -> ExpressionType
    default this.onString () = ExpressionType.String

    abstract member onResult : unit -> ExpressionType
    default this.onResult () = ExpressionType.Result

    abstract member onPauli : unit -> ExpressionType
    default this.onPauli () = ExpressionType.Pauli

    abstract member onRange : unit -> ExpressionType
    default this.onRange () = ExpressionType.Range

    member this.Transform (t : ResolvedType) =
        if not enable then t else
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
        |> ResolvedType.New


and ExpressionTransformation(?enableKindTransformations) = 
    let enableKind = defaultArg enableKindTransformations true
    let typeTransformation = new ExpressionTypeTransformation()

    abstract member Kind : ExpressionKindTransformation
    default this.Kind = {
        new ExpressionKindTransformation (enableKind) with 
            override x.ExpressionTransformation ex = this.Transform ex
            override x.TypeTransformation t = this.Type.Transform t
        }

    abstract member Type : ExpressionTypeTransformation
    default this.Type = typeTransformation

    abstract member onRange : QsNullable<QsPositionInfo*QsPositionInfo> -> QsNullable<QsPositionInfo*QsPositionInfo>
    default this.onRange r = r

    abstract member onExpressionInformation : InferredExpressionInformation -> InferredExpressionInformation
    default this.onExpressionInformation info = info

    abstract member Transform : TypedExpression -> TypedExpression
    default this.Transform (ex : TypedExpression) =
        let range                = this.onRange ex.Range
        let kind                 = this.Kind.Transform ex.Expression
        let typeParamResolutions = ex.TypeParameterResolutions
        let exType               = this.Type.Transform ex.ResolvedType
        let inferredInfo         = this.onExpressionInformation ex.InferredInformation
        TypedExpression.New (kind, typeParamResolutions, exType, inferredInfo, range)


