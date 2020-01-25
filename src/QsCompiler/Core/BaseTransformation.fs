// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

open System.Collections.Immutable
open System.Linq
open System.Numerics
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree

type QsArgumentTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>
type private ExpressionKind = QsExpressionKind<TypedExpression,Identifier,ResolvedType>
type private ExpressionType = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>

type MyEvent() =
    let mutable delegates = []

    member this.Add f =
        delegates <- delegates @ [f]

    member this.Sub f =
        let rec remove i l =
            match i, l with
            | 0, x::xs -> xs
            | i, x::xs -> x::remove (i - 1) xs
            | i, [] -> failwith "index out of range"
        delegates <- match Seq.tryFindIndexBack (fun x -> x = f) delegates with
                     | Some i -> remove i delegates
                     | None -> delegates


/// Convention: 
/// All methods starting with "on" implement the transformation syntax tree element.
/// All methods starting with "before" group a set of elements, and are called before applying the transformation
/// even if the corresponding transformation routine (starting with "on") is overridden.
type BaseTransformation() =

    (*Expression Kind*)

    //abstract member ExpressionTransformation : TypedExpression -> TypedExpression
    //abstract member TypeTransformation : ResolvedType -> ResolvedType

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
    default this.onIdentifier (sym, tArgs) = Identifier (sym, tArgs |> QsNullable<_>.Map (fun ts -> (ts |> Seq.map this.onResolvedType).ToImmutableArray()))

    abstract member onOperationCall : TypedExpression * TypedExpression -> ExpressionKind
    default this.onOperationCall (method, arg) = CallLikeExpression (this.onTypedExpression method, this.onTypedExpression arg)

    abstract member onFunctionCall : TypedExpression * TypedExpression -> ExpressionKind
    default this.onFunctionCall (method, arg) = CallLikeExpression (this.onTypedExpression method, this.onTypedExpression arg)

    abstract member onPartialApplication : TypedExpression * TypedExpression -> ExpressionKind
    default this.onPartialApplication (method, arg) = CallLikeExpression (this.onTypedExpression method, this.onTypedExpression arg)

    abstract member onAdjointApplication : TypedExpression -> ExpressionKind
    default this.onAdjointApplication ex = AdjointApplication (this.onTypedExpression ex)

    abstract member onControlledApplication : TypedExpression -> ExpressionKind
    default this.onControlledApplication ex = ControlledApplication (this.onTypedExpression ex)

    abstract member onUnwrapApplication : TypedExpression -> ExpressionKind
    default this.onUnwrapApplication ex = UnwrapApplication (this.onTypedExpression ex)

    abstract member onUnitValue : unit -> ExpressionKind
    default this.onUnitValue () = ExpressionKind.UnitValue

    abstract member onMissingExpression : unit -> ExpressionKind
    default this.onMissingExpression () = MissingExpr

    abstract member onInvalidExpression : unit -> ExpressionKind
    default this.onInvalidExpression () = InvalidExpr

    abstract member onValueTuple : ImmutableArray<TypedExpression> -> ExpressionKind
    default this.onValueTuple vs = ValueTuple ((vs |> Seq.map this.onTypedExpression).ToImmutableArray())

    abstract member onArrayItem : TypedExpression * TypedExpression -> ExpressionKind
    default this.onArrayItem (arr, idx) = ArrayItem (this.onTypedExpression arr, this.onTypedExpression idx)

    abstract member onNamedItem : TypedExpression * Identifier -> ExpressionKind
    default this.onNamedItem (ex, acc) = NamedItem (this.onTypedExpression ex, acc) 

    abstract member onValueArray : ImmutableArray<TypedExpression> -> ExpressionKind
    default this.onValueArray vs = ValueArray ((vs |> Seq.map this.onTypedExpression).ToImmutableArray())

    abstract member onNewArray : ResolvedType * TypedExpression -> ExpressionKind
    default this.onNewArray (bt, idx) = NewArray (this.onResolvedType bt, this.onTypedExpression idx)

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
    default this.onStringLiteral (s, exs) = StringLiteral (s, (exs |> Seq.map this.onTypedExpression).ToImmutableArray())

    abstract member onRangeLiteral : TypedExpression * TypedExpression -> ExpressionKind
    default this.onRangeLiteral (lhs, rhs) = RangeLiteral (this.onTypedExpression lhs, this.onTypedExpression rhs)

    abstract member onCopyAndUpdateExpression : TypedExpression * TypedExpression * TypedExpression -> ExpressionKind
    default this.onCopyAndUpdateExpression (lhs, accEx, rhs) = CopyAndUpdate (this.onTypedExpression lhs, this.onTypedExpression accEx, this.onTypedExpression rhs)

    abstract member onConditionalExpression : TypedExpression * TypedExpression * TypedExpression -> ExpressionKind
    default this.onConditionalExpression (cond, ifTrue, ifFalse) = CONDITIONAL (this.onTypedExpression cond, this.onTypedExpression ifTrue, this.onTypedExpression ifFalse)

    abstract member onEquality : TypedExpression * TypedExpression -> ExpressionKind
    default this.onEquality (lhs, rhs) = EQ (this.onTypedExpression lhs, this.onTypedExpression rhs)

    abstract member onInequality : TypedExpression * TypedExpression -> ExpressionKind
    default this.onInequality (lhs, rhs) = NEQ (this.onTypedExpression lhs, this.onTypedExpression rhs)

    abstract member onLessThan : TypedExpression * TypedExpression -> ExpressionKind
    default this.onLessThan (lhs, rhs) = LT (this.onTypedExpression lhs, this.onTypedExpression rhs)

    abstract member onLessThanOrEqual : TypedExpression * TypedExpression -> ExpressionKind
    default this.onLessThanOrEqual (lhs, rhs) = LTE (this.onTypedExpression lhs, this.onTypedExpression rhs)

    abstract member onGreaterThan : TypedExpression * TypedExpression -> ExpressionKind
    default this.onGreaterThan (lhs, rhs) = GT (this.onTypedExpression lhs, this.onTypedExpression rhs)

    abstract member onGreaterThanOrEqual : TypedExpression * TypedExpression -> ExpressionKind
    default this.onGreaterThanOrEqual (lhs, rhs) = GTE (this.onTypedExpression lhs, this.onTypedExpression rhs)

    abstract member onLogicalAnd : TypedExpression * TypedExpression -> ExpressionKind
    default this.onLogicalAnd (lhs, rhs) = AND (this.onTypedExpression lhs, this.onTypedExpression rhs)

    abstract member onLogicalOr : TypedExpression * TypedExpression -> ExpressionKind
    default this.onLogicalOr (lhs, rhs) = OR (this.onTypedExpression lhs, this.onTypedExpression rhs)

    abstract member onAddition : TypedExpression * TypedExpression -> ExpressionKind
    default this.onAddition (lhs, rhs) = ADD (this.onTypedExpression lhs, this.onTypedExpression rhs)

    abstract member onSubtraction : TypedExpression * TypedExpression -> ExpressionKind
    default this.onSubtraction (lhs, rhs) = SUB (this.onTypedExpression lhs, this.onTypedExpression rhs)

    abstract member onMultiplication : TypedExpression * TypedExpression -> ExpressionKind
    default this.onMultiplication (lhs, rhs) = MUL (this.onTypedExpression lhs, this.onTypedExpression rhs)

    abstract member onDivision : TypedExpression * TypedExpression -> ExpressionKind
    default this.onDivision (lhs, rhs) = DIV (this.onTypedExpression lhs, this.onTypedExpression rhs)

    abstract member onExponentiate : TypedExpression * TypedExpression -> ExpressionKind
    default this.onExponentiate (lhs, rhs) = POW (this.onTypedExpression lhs, this.onTypedExpression rhs)

    abstract member onModulo : TypedExpression * TypedExpression -> ExpressionKind
    default this.onModulo (lhs, rhs) = MOD (this.onTypedExpression lhs, this.onTypedExpression rhs)

    abstract member onLeftShift : TypedExpression * TypedExpression -> ExpressionKind
    default this.onLeftShift (lhs, rhs) = LSHIFT (this.onTypedExpression lhs, this.onTypedExpression rhs)

    abstract member onRightShift : TypedExpression * TypedExpression -> ExpressionKind
    default this.onRightShift (lhs, rhs) = RSHIFT (this.onTypedExpression lhs, this.onTypedExpression rhs)

    abstract member onBitwiseExclusiveOr : TypedExpression * TypedExpression -> ExpressionKind
    default this.onBitwiseExclusiveOr (lhs, rhs) = BXOR (this.onTypedExpression lhs, this.onTypedExpression rhs)

    abstract member onBitwiseOr : TypedExpression * TypedExpression -> ExpressionKind
    default this.onBitwiseOr (lhs, rhs) = BOR (this.onTypedExpression lhs, this.onTypedExpression rhs)

    abstract member onBitwiseAnd : TypedExpression * TypedExpression -> ExpressionKind
    default this.onBitwiseAnd (lhs, rhs) = BAND (this.onTypedExpression lhs, this.onTypedExpression rhs)

    abstract member onLogicalNot : TypedExpression -> ExpressionKind
    default this.onLogicalNot ex = NOT (this.onTypedExpression ex)

    abstract member onNegative : TypedExpression -> ExpressionKind
    default this.onNegative ex = NEG (this.onTypedExpression ex)

    abstract member onBitwiseNot : TypedExpression -> ExpressionKind
    default this.onBitwiseNot ex = BNOT (this.onTypedExpression ex)


    member private this.dispatchCallLikeExpression (method, arg) = 
        match method.ResolvedType.Resolution with
            | _ when TypedExpression.IsPartialApplication (CallLikeExpression (method, arg)) -> this.onPartialApplication (method, arg) 
            | ExpressionType.Operation _                                                     -> this.onOperationCall (method, arg)
            | _                                                                              -> this.onFunctionCall (method, arg)

    abstract member onExpressionKind : ExpressionKind -> ExpressionKind
    default this.onExpressionKind kind =
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

    (*Expression Type*)

    abstract member onRangeInformation : QsRangeInfo -> QsRangeInfo
    default this.onRangeInformation r = r

    abstract member onCharacteristicsExpression : ResolvedCharacteristics -> ResolvedCharacteristics
    default this.onCharacteristicsExpression fs = fs

    abstract member onCallableInformation : CallableInformation -> CallableInformation
    default this.onCallableInformation opInfo = 
        let characteristics = this.onCharacteristicsExpression opInfo.Characteristics
        let inferred = opInfo.InferredInformation
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
        QsTypeParameter.New (origin, name, range) |> ExpressionType.TypeParameter

    abstract member onUnitType : unit -> ExpressionType
    default this.onUnitType () = ExpressionType.UnitType

    abstract member onOperation : (ResolvedType * ResolvedType) * CallableInformation -> ExpressionType
    default this.onOperation ((it, ot), info) = ExpressionType.Operation ((this.onResolvedType it, this.onResolvedType ot), this.onCallableInformation info)

    abstract member onFunction : ResolvedType * ResolvedType -> ExpressionType
    default this.onFunction (it, ot) = ExpressionType.Function (this.onResolvedType it, this.onResolvedType ot)

    abstract member onTupleType : ImmutableArray<ResolvedType> -> ExpressionType
    default this.onTupleType ts = ExpressionType.TupleType ((ts |> Seq.map this.onResolvedType).ToImmutableArray())

    abstract member onArrayType : ResolvedType -> ExpressionType
    default this.onArrayType b = ExpressionType.ArrayType (this.onResolvedType b)
    
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
    
    abstract member onResolvedType : ResolvedType -> ResolvedType
    default this.onResolvedType (t : ResolvedType) =
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

    (*Expression*)

    abstract member onExpressionInformation : InferredExpressionInformation -> InferredExpressionInformation
    default this.onExpressionInformation info = info

    abstract member onTypeParamResolutions : ImmutableDictionary<(QsQualifiedName*NonNullable<string>), ResolvedType> -> ImmutableDictionary<(QsQualifiedName*NonNullable<string>), ResolvedType>
    default this.onTypeParamResolutions typeParams =
        let asTypeParameter (key) = QsTypeParameter.New (fst key, snd key, Null)
        let filteredTypeParams = 
            typeParams 
            |> Seq.map (fun kv -> this.onTypeParameter (kv.Key |> asTypeParameter), kv.Value)
            |> Seq.choose (function | TypeParameter tp, value -> Some ((tp.Origin, tp.TypeName), this.onResolvedType value) | _ -> None)
        filteredTypeParams.ToImmutableDictionary (fst,snd)

    abstract member onTypedExpression : TypedExpression -> TypedExpression
    default this.onTypedExpression (ex : TypedExpression) =
        let range                = this.onRangeInformation ex.Range
        let typeParamResolutions = this.onTypeParamResolutions ex.TypeParameterResolutions
        let kind                 = this.onExpressionKind ex.Expression
        let exType               = this.onResolvedType ex.ResolvedType
        let inferredInfo         = this.onExpressionInformation ex.InferredInformation
        TypedExpression.New (kind, typeParamResolutions, exType, inferredInfo, range)

    (*Statement Kind*)

    abstract member onQubitInitializer : ResolvedInitializer -> ResolvedInitializer 
    default this.onQubitInitializer init = 
        match init.Resolution with 
        | SingleQubitAllocation      -> SingleQubitAllocation
        | QubitRegisterAllocation ex -> QubitRegisterAllocation (this.onTypedExpression ex)
        | QubitTupleAllocation is    -> QubitTupleAllocation ((is |> Seq.map this.onQubitInitializer).ToImmutableArray())
        | InvalidInitializer         -> InvalidInitializer
        |> ResolvedInitializer.New 

    abstract member beforeVariableDeclaration : SymbolTuple -> SymbolTuple
    default this.beforeVariableDeclaration syms = syms

    abstract member onSymbolTuple : SymbolTuple -> SymbolTuple
    default this.onSymbolTuple syms = syms


    abstract member onExpressionStatement : TypedExpression -> QsStatementKind
    default this.onExpressionStatement ex = QsExpressionStatement (this.onTypedExpression ex)

    abstract member onReturnStatement : TypedExpression -> QsStatementKind
    default this.onReturnStatement ex = QsReturnStatement (this.onTypedExpression ex)

    abstract member onFailStatement : TypedExpression -> QsStatementKind
    default this.onFailStatement ex = QsFailStatement (this.onTypedExpression ex)

    abstract member onVariableDeclaration : QsBinding<TypedExpression> -> QsStatementKind
    default this.onVariableDeclaration stm = 
        let rhs = this.onTypedExpression stm.Rhs
        let lhs = this.onSymbolTuple stm.Lhs
        QsBinding<TypedExpression>.New stm.Kind (lhs, rhs) |> QsVariableDeclaration

    abstract member onValueUpdate : QsValueUpdate -> QsStatementKind
    default this.onValueUpdate stm = 
        let rhs = this.onTypedExpression stm.Rhs
        let lhs = this.onTypedExpression stm.Lhs
        QsValueUpdate.New (lhs, rhs) |> QsValueUpdate

    abstract member onPositionedBlock : TypedExpression option * QsPositionedBlock -> TypedExpression option * QsPositionedBlock
    default this.onPositionedBlock (intro : TypedExpression option, block : QsPositionedBlock) = 
        let location = this.onLocation block.Location
        let comments = block.Comments
        let expr = intro |> Option.map this.onTypedExpression
        let body = this.onScope block.Body
        expr, QsPositionedBlock.New comments location body

    abstract member onConditionalStatement : QsConditionalStatement -> QsStatementKind
    default this.onConditionalStatement stm = 
        let cases = stm.ConditionalBlocks |> Seq.map (fun (c, b) -> 
            let cond, block = this.onPositionedBlock (Some c, b)
            cond |> Option.get, block)
        let defaultCase = stm.Default |> QsNullable<_>.Map (fun b -> this.onPositionedBlock (None, b) |> snd)
        QsConditionalStatement.New (cases, defaultCase) |> QsConditionalStatement

    abstract member onForStatement : QsForStatement -> QsStatementKind
    default this.onForStatement stm = 
        let iterVals = this.onTypedExpression stm.IterationValues
        let loopVar = fst stm.LoopItem |> this.onSymbolTuple
        let loopVarType = this.onResolvedType (snd stm.LoopItem)
        let body = this.onScope stm.Body
        QsForStatement.New ((loopVar, loopVarType), iterVals, body) |> QsForStatement

    abstract member onWhileStatement : QsWhileStatement -> QsStatementKind
    default this.onWhileStatement stm = 
        let condition = this.onTypedExpression stm.Condition
        let body = this.onScope stm.Body
        QsWhileStatement.New (condition, body) |> QsWhileStatement

    abstract member onRepeatStatement : QsRepeatStatement -> QsStatementKind
    default this.onRepeatStatement stm = 
        let repeatBlock = this.onPositionedBlock (None, stm.RepeatBlock) |> snd
        let successCondition, fixupBlock = this.onPositionedBlock (Some stm.SuccessCondition, stm.FixupBlock)
        QsRepeatStatement.New (repeatBlock, successCondition |> Option.get, fixupBlock) |> QsRepeatStatement

    abstract member onConjugation : QsConjugation -> QsStatementKind
    default this.onConjugation stm = 
        let outer = this.onPositionedBlock (None, stm.OuterTransformation) |> snd
        let inner = this.onPositionedBlock (None, stm.InnerTransformation) |> snd
        QsConjugation.New (outer, inner) |> QsConjugation

    abstract member onQubitScope : QsQubitScope -> QsStatementKind
    default this.onQubitScope (stm : QsQubitScope) = 
        let kind = stm.Kind
        let rhs = this.onQubitInitializer stm.Binding.Rhs
        let lhs = this.onSymbolTuple stm.Binding.Lhs
        let body = this.onScope stm.Body
        QsQubitScope.New kind ((lhs, rhs), body) |> QsQubitScope

    abstract member onAllocateQubits : QsQubitScope -> QsStatementKind
    default this.onAllocateQubits stm = this.onQubitScope stm

    abstract member onBorrowQubits : QsQubitScope -> QsStatementKind
    default this.onBorrowQubits stm = this.onQubitScope stm


    member private this.dispatchQubitScope (stm : QsQubitScope) = 
        match stm.Kind with 
        | Allocate -> this.onAllocateQubits stm
        | Borrow   -> this.onBorrowQubits stm

    abstract member onStatementKind : QsStatementKind -> QsStatementKind
    default this.onStatementKind kind = 
        let beforeBinding (stm : QsBinding<TypedExpression>) = { stm with Lhs = this.beforeVariableDeclaration stm.Lhs }
        let beforeForStatement (stm : QsForStatement) = {stm with LoopItem = (this.beforeVariableDeclaration (fst stm.LoopItem), snd stm.LoopItem)} 
        let beforeQubitScope (stm : QsQubitScope) = {stm with Binding = {stm.Binding with Lhs = this.beforeVariableDeclaration stm.Binding.Lhs}}

        match kind with
        | QsExpressionStatement ex   -> this.onExpressionStatement  (ex)
        | QsReturnStatement ex       -> this.onReturnStatement      (ex)
        | QsFailStatement ex         -> this.onFailStatement        (ex)
        | QsVariableDeclaration stm  -> this.onVariableDeclaration  (stm  |> beforeBinding)
        | QsValueUpdate stm          -> this.onValueUpdate          (stm)
        | QsConditionalStatement stm -> this.onConditionalStatement (stm)
        | QsForStatement stm         -> this.onForStatement         (stm  |> beforeForStatement)
        | QsWhileStatement stm       -> this.onWhileStatement       (stm)
        | QsRepeatStatement stm      -> this.onRepeatStatement      (stm)
        | QsConjugation stm          -> this.onConjugation          (stm)
        | QsQubitScope stm           -> this.dispatchQubitScope     (stm  |> beforeQubitScope)

    (*Scope*)

    abstract member onLocalDeclarations : LocalDeclarations -> LocalDeclarations
    default this.onLocalDeclarations decl = 
        let onLocalVariableDeclaration (local : LocalVariableDeclaration<NonNullable<string>>) = 
            let loc = local.Position, local.Range
            let info = this.onExpressionInformation local.InferredInformation
            let varType = this.onResolvedType local.Type 
            LocalVariableDeclaration.New info.IsMutable (loc, local.VariableName, varType, info.HasLocalQuantumDependency)
        let variableDeclarations = decl.Variables |> Seq.map onLocalVariableDeclaration |> ImmutableArray.CreateRange
        LocalDeclarations.New variableDeclarations

    abstract member onStatement : QsStatement -> QsStatement
    default this.onStatement stm = 
        let location = this.onLocation stm.Location
        let comments = stm.Comments
        let kind = this.onStatementKind stm.Statement
        let varDecl = this.onLocalDeclarations stm.SymbolDeclarations
        QsStatement.New comments location (kind, varDecl)

    abstract member onScope : QsScope -> QsScope 
    default this.onScope scope = 
        let parentSymbols = this.onLocalDeclarations scope.KnownSymbols
        let statements = scope.Statements |> Seq.map this.onStatement
        QsScope.New (statements, parentSymbols)

    (*Syntax Tree*)

    abstract member beforeNamespaceElement : QsNamespaceElement -> QsNamespaceElement
    default this.beforeNamespaceElement e = e

    abstract member beforeCallable : QsCallable -> QsCallable
    default this.beforeCallable c = c

    abstract member beforeSpecialization : QsSpecialization -> QsSpecialization
    default this.beforeSpecialization spec = spec

    abstract member beforeSpecializationImplementation : SpecializationImplementation -> SpecializationImplementation
    default this.beforeSpecializationImplementation impl = impl

    abstract member beforeGeneratedImplementation : QsGeneratorDirective -> QsGeneratorDirective
    default this.beforeGeneratedImplementation dir = dir

    abstract member onLocation : QsNullable<QsLocation> -> QsNullable<QsLocation>
    default this.onLocation loc = loc

    abstract member onDocumentation : ImmutableArray<string> -> ImmutableArray<string>
    default this.onDocumentation doc = doc

    abstract member onSourceFile : NonNullable<string> -> NonNullable<string>
    default this.onSourceFile f = f

    abstract member onTypeItems : QsTuple<QsTypeItem> -> QsTuple<QsTypeItem>
    default this.onTypeItems tItem = 
        match tItem with 
        | QsTuple items -> (items |> Seq.map this.onTypeItems).ToImmutableArray() |> QsTuple
        | QsTupleItem (Anonymous itemType) -> 
            let t = this.onResolvedType itemType
            Anonymous t |> QsTupleItem
        | QsTupleItem (Named item) -> 
            let loc  = item.Position, item.Range
            let t    = this.onResolvedType item.Type
            let info = this.onExpressionInformation item.InferredInformation
            LocalVariableDeclaration<_>.New info.IsMutable (loc, item.VariableName, t, info.HasLocalQuantumDependency) |> Named |> QsTupleItem
            
    abstract member onArgumentTuple : QsArgumentTuple -> QsArgumentTuple
    default this.onArgumentTuple arg = 
        match arg with 
        | QsTuple items -> (items |> Seq.map this.onArgumentTuple).ToImmutableArray() |> QsTuple
        | QsTupleItem item -> 
            let loc  = item.Position, item.Range
            let t    = this.onResolvedType item.Type
            let info = this.onExpressionInformation item.InferredInformation
            LocalVariableDeclaration<_>.New info.IsMutable (loc, item.VariableName, t, info.HasLocalQuantumDependency) |> QsTupleItem

    abstract member onSignature : ResolvedSignature -> ResolvedSignature
    default this.onSignature (s : ResolvedSignature) = 
        let typeParams = s.TypeParameters 
        let argType = this.onResolvedType s.ArgumentType
        let returnType = this.onResolvedType s.ReturnType
        let info = this.onCallableInformation s.Information
        ResolvedSignature.New ((argType, returnType), info, typeParams)
    
    abstract member onExternalImplementation : unit -> unit
    default this.onExternalImplementation () = ()

    abstract member onIntrinsicImplementation : unit -> unit
    default this.onIntrinsicImplementation () = ()

    abstract member onProvidedImplementation : QsArgumentTuple * QsScope -> QsArgumentTuple * QsScope
    default this.onProvidedImplementation (argTuple, body) = 
        let argTuple = this.onArgumentTuple argTuple
        let body = this.onScope body
        argTuple, body

    abstract member onSelfInverseDirective : unit -> unit
    default this.onSelfInverseDirective () = ()

    abstract member onInvertDirective : unit -> unit
    default this.onInvertDirective () = ()

    abstract member onDistributeDirective : unit -> unit
    default this.onDistributeDirective () = ()

    abstract member onInvalidGeneratorDirective : unit -> unit
    default this.onInvalidGeneratorDirective () = ()

    member this.dispatchGeneratedImplementation (dir : QsGeneratorDirective) = 
        match this.beforeGeneratedImplementation dir with 
        | SelfInverse      -> this.onSelfInverseDirective ();     SelfInverse     
        | Invert           -> this.onInvertDirective();           Invert          
        | Distribute       -> this.onDistributeDirective();       Distribute      
        | InvalidGenerator -> this.onInvalidGeneratorDirective(); InvalidGenerator

    member this.dispatchSpecializationImplementation (impl : SpecializationImplementation) = 
        match this.beforeSpecializationImplementation impl with 
        | External                  -> this.onExternalImplementation();                  External
        | Intrinsic                 -> this.onIntrinsicImplementation();                 Intrinsic
        | Generated dir             -> this.dispatchGeneratedImplementation dir       |> Generated
        | Provided (argTuple, body) -> this.onProvidedImplementation (argTuple, body) |> Provided

    abstract member onSpecializationImplementation : QsSpecialization -> QsSpecialization
    default this.onSpecializationImplementation (spec : QsSpecialization) = 
        let source = this.onSourceFile spec.SourceFile
        let loc = this.onLocation spec.Location
        let attributes = spec.Attributes |> Seq.map this.onAttribute |> ImmutableArray.CreateRange
        let typeArgs = spec.TypeArguments |> QsNullable<_>.Map (fun args -> (args |> Seq.map this.onResolvedType).ToImmutableArray())
        let signature = this.onSignature spec.Signature
        let impl = this.dispatchSpecializationImplementation spec.Implementation 
        let doc = this.onDocumentation spec.Documentation
        let comments = spec.Comments
        QsSpecialization.New spec.Kind (source, loc) (spec.Parent, attributes, typeArgs, signature, impl, doc, comments)

    abstract member onBodySpecialization : QsSpecialization -> QsSpecialization
    default this.onBodySpecialization spec = this.onSpecializationImplementation spec
    
    abstract member onAdjointSpecialization : QsSpecialization -> QsSpecialization
    default this.onAdjointSpecialization spec = this.onSpecializationImplementation spec

    abstract member onControlledSpecialization : QsSpecialization -> QsSpecialization
    default this.onControlledSpecialization spec = this.onSpecializationImplementation spec

    abstract member onControlledAdjointSpecialization : QsSpecialization -> QsSpecialization
    default this.onControlledAdjointSpecialization spec = this.onSpecializationImplementation spec

    member this.dispatchSpecialization (spec : QsSpecialization) = 
        let spec = this.beforeSpecialization spec
        match spec.Kind with 
        | QsSpecializationKind.QsBody               -> this.onBodySpecialization spec
        | QsSpecializationKind.QsAdjoint            -> this.onAdjointSpecialization spec
        | QsSpecializationKind.QsControlled         -> this.onControlledSpecialization spec
        | QsSpecializationKind.QsControlledAdjoint  -> this.onControlledAdjointSpecialization spec

    abstract member onType : QsCustomType -> QsCustomType
    default this.onType t =
        let source = this.onSourceFile t.SourceFile 
        let loc = this.onLocation t.Location
        let attributes = t.Attributes |> Seq.map this.onAttribute |> ImmutableArray.CreateRange
        let underlyingType = this.onResolvedType t.Type
        let typeItems = this.onTypeItems t.TypeItems
        let doc = this.onDocumentation t.Documentation
        let comments = t.Comments
        QsCustomType.New (source, loc) (t.FullName, attributes, typeItems, underlyingType, doc, comments)

    abstract member onCallableImplementation : QsCallable -> QsCallable
    default this.onCallableImplementation (c : QsCallable) = 
        let source = this.onSourceFile c.SourceFile
        let loc = this.onLocation c.Location
        let attributes = c.Attributes |> Seq.map this.onAttribute |> ImmutableArray.CreateRange
        let signature = this.onSignature c.Signature
        let argTuple = this.onArgumentTuple c.ArgumentTuple
        let specializations = c.Specializations |> Seq.map this.dispatchSpecialization
        let doc = this.onDocumentation c.Documentation
        let comments = c.Comments
        QsCallable.New c.Kind (source, loc) (c.FullName, attributes, argTuple, signature, specializations, doc, comments)

    abstract member onOperation : QsCallable -> QsCallable
    default this.onOperation c = this.onCallableImplementation c

    abstract member onFunction : QsCallable -> QsCallable
    default this.onFunction c = this.onCallableImplementation c

    abstract member onTypeConstructor : QsCallable -> QsCallable
    default this.onTypeConstructor c = this.onCallableImplementation c

    member this.dispatchCallable (c : QsCallable) = 
        let c = this.beforeCallable c
        match c.Kind with 
        | QsCallableKind.Function           -> this.onFunction c
        | QsCallableKind.Operation          -> this.onOperation c
        | QsCallableKind.TypeConstructor    -> this.onTypeConstructor c

    abstract member onAttribute : QsDeclarationAttribute -> QsDeclarationAttribute
    default this.onAttribute att = att

    member this.dispatchNamespaceElement element =
        match this.beforeNamespaceElement element with
        | QsCustomType t    -> t |> this.onType           |> QsCustomType
        | QsCallable c      -> c |> this.dispatchCallable |> QsCallable

    abstract member onNamespace : QsNamespace -> QsNamespace
    default this.onNamespace ns =
        let name = ns.Name
        let doc = ns.Documentation.AsEnumerable().SelectMany(fun entry ->
            entry |> Seq.map (fun doc -> entry.Key, this.onDocumentation doc)).ToLookup(fst, snd)
        let elements = ns.Elements |> Seq.map this.dispatchNamespaceElement
        QsNamespace.New (name, elements, doc)