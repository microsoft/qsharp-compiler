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

type MyHandler<'T> (handler : 'T -> 'T) = member this.Call = handler

type MyEvent<'T,'F> (defaultHandler : 'T -> 'T, final : 'T -> 'F) =
    
    let removeAt i lst =
        List.append
        <| List.take i lst
        <| List.skip (i+1) lst

    member val private Handlers : MyHandler<'T> list = [] with get, set
    member val private CombinedHandleValid = true with get, set
    member val private CombinedHandle = defaultHandler with get, set

    member private this.UpdateCombinedHandle () =
        this.CombinedHandle <- match this.Handlers with
                               | [f] -> f.Call
                               | f1 :: fn -> List.fold (>>) f1.Call <| List.map (fun (f : MyHandler<'T>) -> f.Call) fn
                               | [] -> defaultHandler
        this.CombinedHandleValid <- true

    member this.Add handler =
        match this.Handlers with
        | [] -> this.Handlers <- [handler]
                this.CombinedHandle <- handler.Call
                this.CombinedHandleValid <- true
        | _ -> this.Handlers <- this.Handlers @ [handler]
               if this.CombinedHandleValid then
                   this.CombinedHandle <- this.CombinedHandle >> handler.Call
               else
                   this.UpdateCombinedHandle()
    
    member this.Add (handler : System.Func<'T, 'T>) =
        let rtrn = MyHandler handler.Invoke
        this.Add rtrn
        rtrn

    member this.Remove handler =
        this.Handlers <- match Seq.tryFindIndexBack (fun x -> x = handler) this.Handlers with
                         | Some i -> removeAt i this.Handlers
                         | None -> this.Handlers
        this.CombinedHandleValid <- false

    member this.Call arg =
        if not this.CombinedHandleValid then this.UpdateCombinedHandle()
        this.CombinedHandle arg |> final

    member this.CallDefault arg = defaultHandler arg

type MyEvent<'T> (defaultHandler : 'T -> 'T) = inherit MyEvent<'T,'T>(defaultHandler, id)

/// Convention:
/// All methods starting with "on" implement the transformation syntax tree element.
/// All methods starting with "before" group a set of elements, and are called before applying the transformation
/// even if the corresponding transformation routine (starting with "on") is overridden.
type BaseTransformation() as this =

    (*Expression Kind*)

    member val beforeCallLike : MyEvent<TypedExpression * TypedExpression> = MyEvent<TypedExpression * TypedExpression> id

    member val beforeFunctorApplication : MyEvent<TypedExpression> = MyEvent<TypedExpression> id

    member val beforeModifierApplication : MyEvent<TypedExpression> = MyEvent<TypedExpression> id

    member val beforeBinaryOperatorExpression : MyEvent<TypedExpression * TypedExpression> = MyEvent<TypedExpression * TypedExpression> id

    member val beforeUnaryOperatorExpression : MyEvent<TypedExpression> = MyEvent<TypedExpression> id

    member val onIdentifier : MyEvent<Identifier * QsNullable<ImmutableArray<ResolvedType>>, ExpressionKind> =
        MyEvent<Identifier * QsNullable<ImmutableArray<ResolvedType>>, ExpressionKind>
            ((fun (sym, tArgs) ->
                sym, tArgs |> QsNullable<_>.Map (fun ts -> (ts |> Seq.map this.onResolvedType.Call).ToImmutableArray())),
            Identifier)

    member val onOperationCall : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (method, arg) -> (this.onTypedExpression.Call method, this.onTypedExpression.Call arg)),
            CallLikeExpression)

    member val onFunctionCall : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (method, arg) -> (this.onTypedExpression.Call method, this.onTypedExpression.Call arg)),
            CallLikeExpression)

    member val onPartialApplication : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (method, arg) -> (this.onTypedExpression.Call method, this.onTypedExpression.Call arg)),
            CallLikeExpression)

    member val onAdjointApplication : MyEvent<TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression, ExpressionKind>
            ((fun ex -> this.onTypedExpression.Call ex),
            AdjointApplication)

    member val onControlledApplication : MyEvent<TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression, ExpressionKind>
            ((fun ex -> this.onTypedExpression.Call ex),
            ControlledApplication)

    member val onUnwrapApplication : MyEvent<TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression, ExpressionKind>
            ((fun ex -> this.onTypedExpression.Call ex),
            UnwrapApplication)

    member val onUnitValue : MyEvent<unit, ExpressionKind> =
        MyEvent<unit, ExpressionKind>
            (id,
            (fun () -> ExpressionKind.UnitValue))

    member val onMissingExpression : MyEvent<unit, ExpressionKind> =
        MyEvent<unit, ExpressionKind>
            (id,
            (fun () -> MissingExpr))

    member val onInvalidExpression : MyEvent<unit, ExpressionKind> =
        MyEvent<unit, ExpressionKind>
            (id,
            (fun () -> InvalidExpr))

    member val onValueTuple : MyEvent<ImmutableArray<TypedExpression>, ExpressionKind> =
        MyEvent<ImmutableArray<TypedExpression>, ExpressionKind>
            ((fun vs -> (vs |> Seq.map this.onTypedExpression.Call).ToImmutableArray()),
            ValueTuple)

    member val onArrayItem : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (arr, idx) -> (this.onTypedExpression.Call arr, this.onTypedExpression.Call idx)),
            ArrayItem)

    member val onNamedItem : MyEvent<TypedExpression * Identifier, ExpressionKind> =
        MyEvent<TypedExpression * Identifier, ExpressionKind>
            ((fun (ex, acc) -> (this.onTypedExpression.Call ex, acc)),
            NamedItem)

    member val onValueArray : MyEvent<ImmutableArray<TypedExpression>, ExpressionKind> =
        MyEvent<ImmutableArray<TypedExpression>, ExpressionKind>
            ((fun vs -> (vs |> Seq.map this.onTypedExpression.Call).ToImmutableArray()),
            ValueArray)

    member val onNewArray : MyEvent<ResolvedType * TypedExpression, ExpressionKind> =
        MyEvent<ResolvedType * TypedExpression, ExpressionKind>
            ((fun (bt, idx) -> (this.onResolvedType.Call bt, this.onTypedExpression.Call idx)),
            NewArray)

    member val onIntLiteral : MyEvent<int64, ExpressionKind> =
        MyEvent<int64, ExpressionKind>
            (id,
            IntLiteral)

    member val onBigIntLiteral : MyEvent<BigInteger, ExpressionKind> =
        MyEvent<BigInteger, ExpressionKind>
            (id,
            BigIntLiteral)

    member val onDoubleLiteral : MyEvent<double, ExpressionKind> =
        MyEvent<double, ExpressionKind>
            (id,
            DoubleLiteral)

    member val onBoolLiteral : MyEvent<bool, ExpressionKind> =
        MyEvent<bool, ExpressionKind>
            (id,
            BoolLiteral)

    member val onResultLiteral : MyEvent<QsResult, ExpressionKind> =
        MyEvent<QsResult, ExpressionKind>
            (id,
            ResultLiteral)

    member val onPauliLiteral : MyEvent<QsPauli, ExpressionKind> =
        MyEvent<QsPauli, ExpressionKind>
            (id,
            PauliLiteral)

    member val onStringLiteral : MyEvent<NonNullable<string> * ImmutableArray<TypedExpression>, ExpressionKind> =
        MyEvent<NonNullable<string> * ImmutableArray<TypedExpression>, ExpressionKind>
            ((fun (s, exs) -> (s, (exs |> Seq.map this.onTypedExpression.Call).ToImmutableArray())),
            StringLiteral)

    member val onRangeLiteral : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call rhs)),
            RangeLiteral)

    member val onCopyAndUpdateExpression : MyEvent<TypedExpression * TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, accEx, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call accEx, this.onTypedExpression.Call rhs)),
            CopyAndUpdate)

    member val onConditionalExpression : MyEvent<TypedExpression * TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression * TypedExpression, ExpressionKind>
            ((fun (cond, ifTrue, ifFalse) -> (this.onTypedExpression.Call cond, this.onTypedExpression.Call ifTrue, this.onTypedExpression.Call ifFalse)),
            CONDITIONAL)

    member val onEquality : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call rhs)),
            EQ)

    member val onInequality : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call rhs)),
            NEQ)

    member val onLessThan : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call rhs)),
            LT)

    member val onLessThanOrEqual : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call rhs)),
            LTE)

    member val onGreaterThan : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call rhs)),
            GT)

    member val onGreaterThanOrEqual : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call rhs)),
            GTE)

    member val onLogicalAnd : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call rhs)),
            AND)

    member val onLogicalOr : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call rhs)),
            OR)

    member val onAddition : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call rhs)),
            ADD)

    member val onSubtraction : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call rhs)),
            SUB)

    member val onMultiplication : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call rhs)),
            MUL)

    member val onDivision : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call rhs)),
            DIV)

    member val onExponentiate : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call rhs)),
            POW)

    member val onModulo : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call rhs)),
            MOD)

    member val onLeftShift : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call rhs)),
            LSHIFT)

    member val onRightShift : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call rhs)),
            RSHIFT)

    member val onBitwiseExclusiveOr : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call rhs)),
            BXOR)

    member val onBitwiseOr : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call rhs)),
            BOR)

    member val onBitwiseAnd : MyEvent<TypedExpression * TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression * TypedExpression, ExpressionKind>
            ((fun (lhs, rhs) -> (this.onTypedExpression.Call lhs, this.onTypedExpression.Call rhs)),
            BAND)

    member val onLogicalNot : MyEvent<TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression, ExpressionKind>
            ((fun ex -> this.onTypedExpression.Call ex),
            NOT)

    member val onNegative : MyEvent<TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression, ExpressionKind>
            ((fun ex -> this.onTypedExpression.Call ex),
            NEG)

    member val onBitwiseNot : MyEvent<TypedExpression, ExpressionKind> =
        MyEvent<TypedExpression, ExpressionKind>
            ((fun ex -> this.onTypedExpression.Call ex),
            BNOT)


    member private this.dispatchCallLikeExpression (method, arg) =
        match method.ResolvedType.Resolution with
            | _ when TypedExpression.IsPartialApplication (CallLikeExpression (method, arg)) -> this.onPartialApplication.Call (method, arg)
            | ExpressionType.Operation _                                                     -> this.onOperationCall.Call (method, arg)
            | _                                                                              -> this.onFunctionCall.Call (method, arg)

    member val onExpressionKind : MyEvent<ExpressionKind> = MyEvent<ExpressionKind> (fun kind ->
        match kind with
        | Identifier (sym, tArgs)                          -> this.onIdentifier.Call                 (sym, tArgs)
        | CallLikeExpression (method,arg)                  -> this.dispatchCallLikeExpression        ((method, arg)        |> this.beforeCallLike.Call)
        | AdjointApplication ex                            -> this.onAdjointApplication.Call         (ex                   |> (this.beforeFunctorApplication.Call >> this.beforeModifierApplication.Call))
        | ControlledApplication ex                         -> this.onControlledApplication.Call      (ex                   |> (this.beforeFunctorApplication.Call >> this.beforeModifierApplication.Call))
        | UnwrapApplication ex                             -> this.onUnwrapApplication.Call          (ex                   |> this.beforeModifierApplication.Call)
        | UnitValue                                        -> this.onUnitValue.Call                  ()
        | MissingExpr                                      -> this.onMissingExpression.Call          ()
        | InvalidExpr                                      -> this.onInvalidExpression.Call          ()
        | ValueTuple vs                                    -> this.onValueTuple.Call                 vs
        | ArrayItem (arr, idx)                             -> this.onArrayItem.Call                  (arr, idx)
        | NamedItem (ex, acc)                              -> this.onNamedItem.Call                  (ex, acc)
        | ValueArray vs                                    -> this.onValueArray.Call                 vs
        | NewArray (bt, idx)                               -> this.onNewArray.Call                   (bt, idx)
        | IntLiteral i                                     -> this.onIntLiteral.Call                 i
        | BigIntLiteral b                                  -> this.onBigIntLiteral.Call              b
        | DoubleLiteral d                                  -> this.onDoubleLiteral.Call              d
        | BoolLiteral b                                    -> this.onBoolLiteral.Call                b
        | ResultLiteral r                                  -> this.onResultLiteral.Call              r
        | PauliLiteral p                                   -> this.onPauliLiteral.Call               p
        | StringLiteral (s, exs)                           -> this.onStringLiteral.Call              (s, exs)
        | RangeLiteral (lhs, rhs)                          -> this.onRangeLiteral.Call               (lhs, rhs)
        | CopyAndUpdate (lhs, accEx, rhs)                  -> this.onCopyAndUpdateExpression.Call    (lhs, accEx, rhs)
        | CONDITIONAL (cond, ifTrue, ifFalse)              -> this.onConditionalExpression.Call      (cond, ifTrue, ifFalse)
        | EQ (lhs,rhs)                                     -> this.onEquality.Call                   ((lhs, rhs)          |> this.beforeBinaryOperatorExpression.Call)
        | NEQ (lhs,rhs)                                    -> this.onInequality.Call                 ((lhs, rhs)          |> this.beforeBinaryOperatorExpression.Call)
        | LT (lhs,rhs)                                     -> this.onLessThan.Call                   ((lhs, rhs)          |> this.beforeBinaryOperatorExpression.Call)
        | LTE (lhs,rhs)                                    -> this.onLessThanOrEqual.Call            ((lhs, rhs)          |> this.beforeBinaryOperatorExpression.Call)
        | GT (lhs,rhs)                                     -> this.onGreaterThan.Call                ((lhs, rhs)          |> this.beforeBinaryOperatorExpression.Call)
        | GTE (lhs,rhs)                                    -> this.onGreaterThanOrEqual.Call         ((lhs, rhs)          |> this.beforeBinaryOperatorExpression.Call)
        | AND (lhs,rhs)                                    -> this.onLogicalAnd.Call                 ((lhs, rhs)          |> this.beforeBinaryOperatorExpression.Call)
        | OR (lhs,rhs)                                     -> this.onLogicalOr.Call                  ((lhs, rhs)          |> this.beforeBinaryOperatorExpression.Call)
        | ADD (lhs,rhs)                                    -> this.onAddition.Call                   ((lhs, rhs)          |> this.beforeBinaryOperatorExpression.Call)
        | SUB (lhs,rhs)                                    -> this.onSubtraction.Call                ((lhs, rhs)          |> this.beforeBinaryOperatorExpression.Call)
        | MUL (lhs,rhs)                                    -> this.onMultiplication.Call             ((lhs, rhs)          |> this.beforeBinaryOperatorExpression.Call)
        | DIV (lhs,rhs)                                    -> this.onDivision.Call                   ((lhs, rhs)          |> this.beforeBinaryOperatorExpression.Call)
        | POW (lhs,rhs)                                    -> this.onExponentiate.Call               ((lhs, rhs)          |> this.beforeBinaryOperatorExpression.Call)
        | MOD (lhs,rhs)                                    -> this.onModulo.Call                     ((lhs, rhs)          |> this.beforeBinaryOperatorExpression.Call)
        | LSHIFT (lhs,rhs)                                 -> this.onLeftShift.Call                  ((lhs, rhs)          |> this.beforeBinaryOperatorExpression.Call)
        | RSHIFT (lhs,rhs)                                 -> this.onRightShift.Call                 ((lhs, rhs)          |> this.beforeBinaryOperatorExpression.Call)
        | BXOR (lhs,rhs)                                   -> this.onBitwiseExclusiveOr.Call         ((lhs, rhs)          |> this.beforeBinaryOperatorExpression.Call)
        | BOR (lhs,rhs)                                    -> this.onBitwiseOr.Call                  ((lhs, rhs)          |> this.beforeBinaryOperatorExpression.Call)
        | BAND (lhs,rhs)                                   -> this.onBitwiseAnd.Call                 ((lhs, rhs)          |> this.beforeBinaryOperatorExpression.Call)
        | NOT ex                                           -> this.onLogicalNot.Call                 (ex                  |> this.beforeUnaryOperatorExpression.Call)
        | NEG ex                                           -> this.onNegative.Call                   (ex                  |> this.beforeUnaryOperatorExpression.Call)
        | BNOT ex                                          -> this.onBitwiseNot.Call                 (ex                  |> this.beforeUnaryOperatorExpression.Call))

    (*Expression Type*)

    member val onRangeInformation : MyEvent<QsRangeInfo> = MyEvent<QsRangeInfo> id

    member val onCharacteristicsExpression : MyEvent<ResolvedCharacteristics> = MyEvent<ResolvedCharacteristics> id

    member val onCallableInformation : MyEvent<CallableInformation> = MyEvent<CallableInformation> (fun opInfo ->
        let characteristics = this.onCharacteristicsExpression.Call opInfo.Characteristics
        let inferred = opInfo.InferredInformation
        CallableInformation.New (characteristics, inferred))

    member val onUserDefinedType : MyEvent<UserDefinedType, ExpressionType> =
        MyEvent<UserDefinedType, ExpressionType>
            ((fun udt ->
                let ns, name = udt.Namespace, udt.Name
                let range = this.onRangeInformation.Call udt.Range
                UserDefinedType.New (ns, name, range)),
            ExpressionType.UserDefinedType)

    member val onTypeParameter : MyEvent<QsTypeParameter, ExpressionType> =
        MyEvent<QsTypeParameter, ExpressionType>
            ((fun tp ->
                let origin = tp.Origin
                let name = tp.TypeName
                let range = this.onRangeInformation.Call tp.Range
                QsTypeParameter.New (origin, name, range)),
            ExpressionType.TypeParameter)

    member val onUnitType : MyEvent<unit, ExpressionType> =
        MyEvent<unit, ExpressionType>
            (id,
            (fun () -> ExpressionType.UnitType))

    member val onOperationType : MyEvent<(ResolvedType * ResolvedType) * CallableInformation, ExpressionType> =
        MyEvent<(ResolvedType * ResolvedType) * CallableInformation, ExpressionType>
            ((fun ((it, ot), info) -> ((this.onResolvedType.Call it, this.onResolvedType.Call ot), this.onCallableInformation.Call info)),
            ExpressionType.Operation)

    member val onFunctionType : MyEvent<ResolvedType * ResolvedType, ExpressionType> =
        MyEvent<ResolvedType * ResolvedType, ExpressionType>
            ((fun (it, ot) -> (this.onResolvedType.Call it, this.onResolvedType.Call ot)),
            ExpressionType.Function)

    member val onTupleType : MyEvent<ImmutableArray<ResolvedType>, ExpressionType> =
        MyEvent<ImmutableArray<ResolvedType>, ExpressionType>
            ((fun ts -> (ts |> Seq.map this.onResolvedType.Call).ToImmutableArray()),
            ExpressionType.TupleType)

    member val onArrayType : MyEvent<ResolvedType, ExpressionType> =
        MyEvent<ResolvedType, ExpressionType>
            ((fun b -> this.onResolvedType.Call b),
            ExpressionType.ArrayType)

    member val onQubit : MyEvent<unit, ExpressionType> =
        MyEvent<unit, ExpressionType>
            (id,
            (fun () -> ExpressionType.Qubit))

    member val onMissingType : MyEvent<unit, ExpressionType> =
        MyEvent<unit, ExpressionType>
            (id,
            (fun () -> ExpressionType.MissingType))

    member val onInvalidType : MyEvent<unit, ExpressionType> =
        MyEvent<unit, ExpressionType>
            (id,
            (fun () -> ExpressionType.InvalidType))

    member val onInt : MyEvent<unit, ExpressionType> =
        MyEvent<unit, ExpressionType>
            (id,
            (fun () -> ExpressionType.Int))

    member val onBigInt : MyEvent<unit, ExpressionType> =
        MyEvent<unit, ExpressionType>
            (id,
            (fun () -> ExpressionType.BigInt))

    member val onDouble : MyEvent<unit, ExpressionType> =
        MyEvent<unit, ExpressionType>
            (id,
            (fun () -> ExpressionType.Double))

    member val onBool : MyEvent<unit, ExpressionType> =
        MyEvent<unit, ExpressionType>
            (id,
            (fun () -> ExpressionType.Bool))

    member val onString : MyEvent<unit, ExpressionType> =
        MyEvent<unit, ExpressionType>
            (id,
            (fun () -> ExpressionType.String))

    member val onResult : MyEvent<unit, ExpressionType> =
        MyEvent<unit, ExpressionType>
            (id,
            (fun () -> ExpressionType.Result))

    member val onPauli : MyEvent<unit, ExpressionType> =
        MyEvent<unit, ExpressionType>
            (id,
            (fun () -> ExpressionType.Pauli))

    member val onRange : MyEvent<unit, ExpressionType> =
        MyEvent<unit, ExpressionType>
            (id,
            (fun () -> ExpressionType.Range))

    member val onResolvedType : MyEvent<ResolvedType> = MyEvent<ResolvedType> (fun t ->
        match t.Resolution with
        | ExpressionType.UnitType                    -> this.onUnitType.Call ()
        | ExpressionType.Operation ((it, ot), fs)    -> this.onOperationType.Call ((it, ot), fs)
        | ExpressionType.Function (it, ot)           -> this.onFunctionType.Call (it, ot)
        | ExpressionType.TupleType ts                -> this.onTupleType.Call ts
        | ExpressionType.ArrayType b                 -> this.onArrayType.Call b
        | ExpressionType.UserDefinedType udt         -> this.onUserDefinedType.Call udt
        | ExpressionType.TypeParameter tp            -> this.onTypeParameter.Call tp
        | ExpressionType.Qubit                       -> this.onQubit.Call ()
        | ExpressionType.MissingType                 -> this.onMissingType.Call ()
        | ExpressionType.InvalidType                 -> this.onInvalidType.Call ()
        | ExpressionType.Int                         -> this.onInt.Call ()
        | ExpressionType.BigInt                      -> this.onBigInt.Call ()
        | ExpressionType.Double                      -> this.onDouble.Call ()
        | ExpressionType.Bool                        -> this.onBool.Call ()
        | ExpressionType.String                      -> this.onString.Call ()
        | ExpressionType.Result                      -> this.onResult.Call ()
        | ExpressionType.Pauli                       -> this.onPauli.Call ()
        | ExpressionType.Range                       -> this.onRange.Call ()
        |> ResolvedType.New)

    (*Expression*)

    member val onLocation : MyEvent<QsNullable<QsLocation>> = MyEvent<QsNullable<QsLocation>> id

    member val onExpressionInformation : MyEvent<InferredExpressionInformation> = MyEvent<InferredExpressionInformation> id

    member val onTypeParamResolutions : MyEvent<ImmutableDictionary<(QsQualifiedName*NonNullable<string>), ResolvedType>> =
        MyEvent<ImmutableDictionary<(QsQualifiedName*NonNullable<string>), ResolvedType>> (fun typeParams ->
        let asTypeParameter (key) = QsTypeParameter.New (fst key, snd key, Null)
        let filteredTypeParams =
            typeParams
            |> Seq.map (fun kv -> this.onTypeParameter.Call (kv.Key |> asTypeParameter), kv.Value)
            |> Seq.choose (function | TypeParameter tp, value -> Some ((tp.Origin, tp.TypeName), this.onResolvedType.Call value) | _ -> None)
        filteredTypeParams.ToImmutableDictionary (fst,snd))

    member val onTypedExpression : MyEvent<TypedExpression> = MyEvent<TypedExpression> (fun ex ->
        let range                = this.onRangeInformation.Call ex.Range
        let typeParamResolutions = this.onTypeParamResolutions.Call ex.TypeParameterResolutions
        let kind                 = this.onExpressionKind.Call ex.Expression
        let exType               = this.onResolvedType.Call ex.ResolvedType
        let inferredInfo         = this.onExpressionInformation.Call ex.InferredInformation
        TypedExpression.New (kind, typeParamResolutions, exType, inferredInfo, range))

    (*Statement Kind*)

    member val onQubitInitializer : MyEvent<ResolvedInitializer> = MyEvent<ResolvedInitializer> (fun init ->
        match init.Resolution with
        | SingleQubitAllocation      -> SingleQubitAllocation
        | QubitRegisterAllocation ex -> QubitRegisterAllocation (this.onTypedExpression.Call ex)
        | QubitTupleAllocation is    -> QubitTupleAllocation ((is |> Seq.map this.onQubitInitializer.Call).ToImmutableArray())
        | InvalidInitializer         -> InvalidInitializer
        |> ResolvedInitializer.New)

    member val beforeVariableDeclaration : MyEvent<SymbolTuple> = MyEvent<SymbolTuple> id

    member val onSymbolTuple : MyEvent<SymbolTuple> = MyEvent<SymbolTuple> id


    member val onExpressionStatement : MyEvent<TypedExpression, QsStatementKind> =
        MyEvent<TypedExpression, QsStatementKind>
            ((fun ex -> this.onTypedExpression.Call ex),
            QsExpressionStatement)

    member val onReturnStatement : MyEvent<TypedExpression, QsStatementKind> =
        MyEvent<TypedExpression, QsStatementKind>
            ((fun ex -> this.onTypedExpression.Call ex),
            QsReturnStatement)

    member val onFailStatement : MyEvent<TypedExpression, QsStatementKind> =
        MyEvent<TypedExpression, QsStatementKind>
            ((fun ex -> this.onTypedExpression.Call ex),
            QsFailStatement)

    member val onVariableDeclaration : MyEvent<QsBinding<TypedExpression>, QsStatementKind> =
        MyEvent<QsBinding<TypedExpression>, QsStatementKind>
            ((fun stm ->
                let rhs = this.onTypedExpression.Call stm.Rhs
                let lhs = this.onSymbolTuple.Call stm.Lhs
                QsBinding<TypedExpression>.New stm.Kind (lhs, rhs)),
            QsVariableDeclaration)

    member val onValueUpdate : MyEvent<QsValueUpdate, QsStatementKind> =
        MyEvent<QsValueUpdate, QsStatementKind>
            ((fun stm ->
                let rhs = this.onTypedExpression.Call stm.Rhs
                let lhs = this.onTypedExpression.Call stm.Lhs
                QsValueUpdate.New (lhs, rhs)),
            QsValueUpdate)

    member val onPositionedBlock : MyEvent<TypedExpression option * QsPositionedBlock> =
        MyEvent<TypedExpression option * QsPositionedBlock> (fun (intro : TypedExpression option, block : QsPositionedBlock) ->
            let location = this.onLocation.Call block.Location
            let comments = block.Comments
            let expr = intro |> Option.map this.onTypedExpression.Call
            let body = this.onScope.Call block.Body
            expr, QsPositionedBlock.New comments location body)

    member val onConditionalStatement : MyEvent<QsConditionalStatement, QsStatementKind> =
        MyEvent<QsConditionalStatement, QsStatementKind>
            ((fun stm ->
                let cases = stm.ConditionalBlocks |> Seq.map (fun (c, b) ->
                    let cond, block = this.onPositionedBlock.Call(Some c, b)
                    cond |> Option.get, block)
                let defaultCase = stm.Default |> QsNullable<_>.Map (fun b -> this.onPositionedBlock.Call(None, b) |> snd)
                QsConditionalStatement.New (cases, defaultCase)),
            QsConditionalStatement)

    member val onForStatement : MyEvent<QsForStatement, QsStatementKind> =
        MyEvent<QsForStatement, QsStatementKind>
            ((fun stm ->
                let iterVals = this.onTypedExpression.Call stm.IterationValues
                let loopVar = fst stm.LoopItem |> this.onSymbolTuple.Call
                let loopVarType = this.onResolvedType.Call (snd stm.LoopItem)
                let body = this.onScope.Call stm.Body
                QsForStatement.New ((loopVar, loopVarType), iterVals, body)),
            QsForStatement)

    member val onWhileStatement : MyEvent<QsWhileStatement, QsStatementKind> =
        MyEvent<QsWhileStatement, QsStatementKind>
            ((fun stm ->
                let condition = this.onTypedExpression.Call stm.Condition
                let body = this.onScope.Call stm.Body
                QsWhileStatement.New (condition, body)),
            QsWhileStatement)

    member val onRepeatStatement : MyEvent<QsRepeatStatement, QsStatementKind> =
        MyEvent<QsRepeatStatement, QsStatementKind>
            ((fun stm ->
                let repeatBlock = this.onPositionedBlock.Call(None, stm.RepeatBlock) |> snd
                let successCondition, fixupBlock = this.onPositionedBlock.Call(Some stm.SuccessCondition, stm.FixupBlock)
                QsRepeatStatement.New (repeatBlock, successCondition |> Option.get, fixupBlock)),
            QsRepeatStatement)

    member val onConjugation : MyEvent<QsConjugation, QsStatementKind> =
        MyEvent<QsConjugation, QsStatementKind>
            ((fun stm ->
                let outer = this.onPositionedBlock.Call (None, stm.OuterTransformation) |> snd
                let inner = this.onPositionedBlock.Call (None, stm.InnerTransformation) |> snd
                QsConjugation.New (outer, inner)),
            QsConjugation)

    member val onQubitScope : MyEvent<QsQubitScope, QsStatementKind> =
        MyEvent<QsQubitScope, QsStatementKind>
            ((fun stm ->
                let kind = stm.Kind
                let rhs = this.onQubitInitializer.Call stm.Binding.Rhs
                let lhs = this.onSymbolTuple.Call stm.Binding.Lhs
                let body = this.onScope.Call stm.Body
                QsQubitScope.New kind ((lhs, rhs), body)),
            QsQubitScope)

    member val onAllocateQubits : MyEvent<QsQubitScope, QsStatementKind> =
        MyEvent<QsQubitScope, QsStatementKind>
            (id,
            (fun x -> this.onQubitScope.Call x))

    member val onBorrowQubits : MyEvent<QsQubitScope, QsStatementKind> =
        MyEvent<QsQubitScope, QsStatementKind>
            (id,
            (fun x -> this.onQubitScope.Call x))


    member private this.dispatchQubitScope (stm : QsQubitScope) =
        match stm.Kind with
        | Allocate -> this.onAllocateQubits.Call stm
        | Borrow   -> this.onBorrowQubits.Call stm

    member val onStatementKind : MyEvent<QsStatementKind> = MyEvent<QsStatementKind> (fun kind ->
        let beforeBinding (stm : QsBinding<TypedExpression>) = { stm with Lhs = this.beforeVariableDeclaration.Call stm.Lhs }
        let beforeForStatement (stm : QsForStatement) = {stm with LoopItem = (this.beforeVariableDeclaration.Call (fst stm.LoopItem), snd stm.LoopItem)}
        let beforeQubitScope (stm : QsQubitScope) = {stm with Binding = {stm.Binding with Lhs = this.beforeVariableDeclaration.Call stm.Binding.Lhs}}

        match kind with
        | QsExpressionStatement ex   -> this.onExpressionStatement.Call  (ex)
        | QsReturnStatement ex       -> this.onReturnStatement.Call      (ex)
        | QsFailStatement ex         -> this.onFailStatement.Call        (ex)
        | QsVariableDeclaration stm  -> this.onVariableDeclaration.Call  (stm  |> beforeBinding)
        | QsValueUpdate stm          -> this.onValueUpdate.Call          (stm)
        | QsConditionalStatement stm -> this.onConditionalStatement.Call (stm)
        | QsForStatement stm         -> this.onForStatement.Call         (stm  |> beforeForStatement)
        | QsWhileStatement stm       -> this.onWhileStatement.Call       (stm)
        | QsRepeatStatement stm      -> this.onRepeatStatement.Call      (stm)
        | QsConjugation stm          -> this.onConjugation.Call          (stm)
        | QsQubitScope stm           -> this.dispatchQubitScope          (stm  |> beforeQubitScope)
    )

    (*Scope*)

    member val onLocalDeclarations : MyEvent<LocalDeclarations> = MyEvent<LocalDeclarations> (fun decl ->
        let onLocalVariableDeclaration (local : LocalVariableDeclaration<NonNullable<string>>) =
            let loc = local.Position, local.Range
            let info = this.onExpressionInformation.Call local.InferredInformation
            let varType = this.onResolvedType.Call local.Type
            LocalVariableDeclaration.New info.IsMutable (loc, local.VariableName, varType, info.HasLocalQuantumDependency)
        let variableDeclarations = decl.Variables |> Seq.map onLocalVariableDeclaration |> ImmutableArray.CreateRange
        LocalDeclarations.New variableDeclarations)

    member val onStatement : MyEvent<QsStatement> = MyEvent<QsStatement> (fun stm ->
        let location = this.onLocation.Call stm.Location
        let comments = stm.Comments
        let kind = this.onStatementKind.Call stm.Statement
        let varDecl = this.onLocalDeclarations.Call stm.SymbolDeclarations
        QsStatement.New comments location (kind, varDecl))

    member val onScope : MyEvent<QsScope> = MyEvent<QsScope> (fun scope ->
        let parentSymbols = this.onLocalDeclarations.Call scope.KnownSymbols
        let statements = scope.Statements |> Seq.map this.onStatement.Call
        QsScope.New (statements, parentSymbols))

    (*Syntax Tree*)

    member val beforeNamespaceElement : MyEvent<QsNamespaceElement> = MyEvent<QsNamespaceElement> id

    member val beforeCallable : MyEvent<QsCallable> = MyEvent<QsCallable> id

    member val beforeSpecialization : MyEvent<QsSpecialization> = MyEvent<QsSpecialization> id

    member val beforeSpecializationImplementation : MyEvent<SpecializationImplementation> = MyEvent<SpecializationImplementation> id

    member val beforeGeneratedImplementation : MyEvent<QsGeneratorDirective> = MyEvent<QsGeneratorDirective> id

    member val onDocumentation : MyEvent<ImmutableArray<string>> = MyEvent<ImmutableArray<string>> id

    member val onSourceFile : MyEvent<NonNullable<string>> = MyEvent<NonNullable<string>> id

    member val onTypeItems : MyEvent<QsTuple<QsTypeItem>> = MyEvent<QsTuple<QsTypeItem>> (fun tItem ->
        match tItem with
        | QsTuple items -> (items |> Seq.map this.onTypeItems.Call).ToImmutableArray() |> QsTuple
        | QsTupleItem (Anonymous itemType) ->
            let t = this.onResolvedType.Call itemType
            Anonymous t |> QsTupleItem
        | QsTupleItem (Named item) ->
            let loc  = item.Position, item.Range
            let t    = this.onResolvedType.Call item.Type
            let info = this.onExpressionInformation.Call item.InferredInformation
            LocalVariableDeclaration<_>.New info.IsMutable (loc, item.VariableName, t, info.HasLocalQuantumDependency) |> Named |> QsTupleItem)

    member val onArgumentTuple : MyEvent<QsArgumentTuple> = MyEvent<QsArgumentTuple> (fun arg ->
        match arg with
        | QsTuple items -> (items |> Seq.map this.onArgumentTuple.Call).ToImmutableArray() |> QsTuple
        | QsTupleItem item ->
            let loc  = item.Position, item.Range
            let t    = this.onResolvedType.Call item.Type
            let info = this.onExpressionInformation.Call item.InferredInformation
            LocalVariableDeclaration<_>.New info.IsMutable (loc, item.VariableName, t, info.HasLocalQuantumDependency) |> QsTupleItem)

    member val onSignature : MyEvent<ResolvedSignature> = MyEvent<ResolvedSignature> (fun s ->
        let typeParams = s.TypeParameters
        let argType = this.onResolvedType.Call s.ArgumentType
        let returnType = this.onResolvedType.Call s.ReturnType
        let info = this.onCallableInformation.Call s.Information
        ResolvedSignature.New ((argType, returnType), info, typeParams))

    member val onExternalImplementation : MyEvent<unit> = MyEvent<unit> id

    member val onIntrinsicImplementation : MyEvent<unit> = MyEvent<unit> id

    member val onProvidedImplementation : MyEvent<QsArgumentTuple * QsScope> = MyEvent<QsArgumentTuple * QsScope> (fun (argTuple, body) ->
        let argTuple = this.onArgumentTuple.Call argTuple
        let body = this.onScope.Call body
        argTuple, body)

    member val onSelfInverseDirective : MyEvent<unit> = MyEvent<unit> id

    member val onInvertDirective : MyEvent<unit> = MyEvent<unit> id

    member val onDistributeDirective : MyEvent<unit> = MyEvent<unit> id

    member val onInvalidGeneratorDirective : MyEvent<unit> = MyEvent<unit> id

    member this.dispatchGeneratedImplementation dir =
        match this.beforeGeneratedImplementation.Call dir with
        | SelfInverse      -> this.onSelfInverseDirective.Call();     SelfInverse
        | Invert           -> this.onInvertDirective.Call();           Invert
        | Distribute       -> this.onDistributeDirective.Call();       Distribute
        | InvalidGenerator -> this.onInvalidGeneratorDirective.Call(); InvalidGenerator

    member this.dispatchSpecializationImplementation impl =
        match this.beforeSpecializationImplementation.Call impl with
        | External                  -> this.onExternalImplementation.Call();                  External
        | Intrinsic                 -> this.onIntrinsicImplementation.Call();                 Intrinsic
        | Generated dir             -> this.dispatchGeneratedImplementation dir       |> Generated
        | Provided (argTuple, body) -> this.onProvidedImplementation.Call (argTuple, body) |> Provided

    member val onSpecializationImplementation : MyEvent<QsSpecialization> = MyEvent<QsSpecialization> (fun spec ->
        let source = this.onSourceFile.Call spec.SourceFile
        let loc = this.onLocation.Call spec.Location
        let attributes = spec.Attributes |> Seq.map this.onAttribute.Call |> ImmutableArray.CreateRange
        let typeArgs = spec.TypeArguments |> QsNullable<_>.Map (fun args -> (args |> Seq.map this.onResolvedType.Call).ToImmutableArray())
        let signature = this.onSignature.Call spec.Signature
        let impl = this.dispatchSpecializationImplementation spec.Implementation
        let doc = this.onDocumentation.Call spec.Documentation
        let comments = spec.Comments
        QsSpecialization.New spec.Kind (source, loc) (spec.Parent, attributes, typeArgs, signature, impl, doc, comments))

    member val onBodySpecialization : MyEvent<QsSpecialization> = MyEvent<QsSpecialization> (fun spec -> this.onSpecializationImplementation.Call spec)

    member val onAdjointSpecialization : MyEvent<QsSpecialization> = MyEvent<QsSpecialization> (fun spec -> this.onSpecializationImplementation.Call spec)

    member val onControlledSpecialization : MyEvent<QsSpecialization> = MyEvent<QsSpecialization> (fun spec -> this.onSpecializationImplementation.Call spec)

    member val onControlledAdjointSpecialization : MyEvent<QsSpecialization> = MyEvent<QsSpecialization> (fun spec -> this.onSpecializationImplementation.Call spec)

    member this.dispatchSpecialization spec =
        let spec = this.beforeSpecialization.Call spec
        match spec.Kind with
        | QsSpecializationKind.QsBody               -> this.onBodySpecialization.Call spec
        | QsSpecializationKind.QsAdjoint            -> this.onAdjointSpecialization.Call spec
        | QsSpecializationKind.QsControlled         -> this.onControlledSpecialization.Call spec
        | QsSpecializationKind.QsControlledAdjoint  -> this.onControlledAdjointSpecialization.Call spec

    member val onType : MyEvent<QsCustomType> = MyEvent<QsCustomType> (fun t ->
        let source = this.onSourceFile.Call t.SourceFile
        let loc = this.onLocation.Call t.Location
        let attributes = t.Attributes |> Seq.map this.onAttribute.Call |> ImmutableArray.CreateRange
        let underlyingType = this.onResolvedType.Call t.Type
        let typeItems = this.onTypeItems.Call t.TypeItems
        let doc = this.onDocumentation.Call t.Documentation
        let comments = t.Comments
        QsCustomType.New (source, loc) (t.FullName, attributes, typeItems, underlyingType, doc, comments))

    member val onCallableImplementation : MyEvent<QsCallable> = MyEvent<QsCallable> (fun c ->
        let source = this.onSourceFile.Call c.SourceFile
        let loc = this.onLocation.Call c.Location
        let attributes = c.Attributes |> Seq.map this.onAttribute.Call |> ImmutableArray.CreateRange
        let signature = this.onSignature.Call c.Signature
        let argTuple = this.onArgumentTuple.Call c.ArgumentTuple
        let specializations = c.Specializations |> Seq.map this.dispatchSpecialization
        let doc = this.onDocumentation.Call c.Documentation
        let comments = c.Comments
        QsCallable.New c.Kind (source, loc) (c.FullName, attributes, argTuple, signature, specializations, doc, comments))

    member val onOperation : MyEvent<QsCallable> = MyEvent<QsCallable> (fun c -> this.onCallableImplementation.Call c)

    member val onFunction : MyEvent<QsCallable> = MyEvent<QsCallable> (fun c -> this.onCallableImplementation.Call c)

    member val onTypeConstructor : MyEvent<QsCallable> = MyEvent<QsCallable> (fun c -> this.onCallableImplementation.Call c)

    member this.dispatchCallable c =
        let c = this.beforeCallable.Call c
        match c.Kind with
        | QsCallableKind.Function           -> this.onFunction.Call c
        | QsCallableKind.Operation          -> this.onOperation.Call c
        | QsCallableKind.TypeConstructor    -> this.onTypeConstructor.Call c

    member val onAttribute : MyEvent<QsDeclarationAttribute> = MyEvent<QsDeclarationAttribute> id

    member this.dispatchNamespaceElement element =
        match this.beforeNamespaceElement.Call element with
        | QsCustomType t    -> t |> this.onType.Call      |> QsCustomType
        | QsCallable c      -> c |> this.dispatchCallable |> QsCallable

    member val onNamespace : MyEvent<QsNamespace> = MyEvent<QsNamespace> (fun ns ->
        let name = ns.Name
        let doc = ns.Documentation.AsEnumerable().SelectMany(fun entry ->
            entry |> Seq.map (fun doc -> entry.Key, this.onDocumentation.Call doc)).ToLookup(fst, snd)
        let elements = ns.Elements |> Seq.map this.dispatchNamespaceElement
        QsNamespace.New (name, elements, doc))