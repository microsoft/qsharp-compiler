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

type private ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>


type ExpressionKindTransformationBase internal (options: TransformationOptions, _internal_) =

    let missingTransformation name _ =
        new InvalidOperationException(sprintf "No %s transformation has been specified." name) |> raise

    let Node = if options.Rebuild then Fold else Walk

    member val internal TypeTransformationHandle = missingTransformation "type" with get, set
    member val internal ExpressionTransformationHandle = missingTransformation "expression" with get, set

    member this.Types = this.TypeTransformationHandle()
    member this.Expressions = this.ExpressionTransformationHandle()

    new(expressionTransformation: unit -> ExpressionTransformationBase,
        typeTransformation: unit -> TypeTransformationBase,
        options) as this =
        new ExpressionKindTransformationBase(options, "_internal_")
        then
            this.TypeTransformationHandle <- typeTransformation
            this.ExpressionTransformationHandle <- expressionTransformation

    new(options: TransformationOptions) as this =
        new ExpressionKindTransformationBase(options, "_internal_")
        then
            let typeTransformation = new TypeTransformationBase(options)

            let expressionTransformation =
                new ExpressionTransformationBase((fun _ -> this), (fun _ -> this.Types), options)

            this.TypeTransformationHandle <- fun _ -> typeTransformation
            this.ExpressionTransformationHandle <- fun _ -> expressionTransformation

    new(expressionTransformation: unit -> ExpressionTransformationBase,
        typeTransformation: unit -> TypeTransformationBase) =
        new ExpressionKindTransformationBase(expressionTransformation, typeTransformation, TransformationOptions.Default)

    new() = new ExpressionKindTransformationBase(TransformationOptions.Default)


    // nodes containing subexpressions or subtypes

    abstract OnIdentifier: Identifier * QsNullable<ImmutableArray<ResolvedType>> -> ExpressionKind

    default this.OnIdentifier(sym, tArgs) =
        let tArgs =
            tArgs
            |> QsNullable<_>
                .Map(fun ts -> ts |> Seq.map this.Types.OnType |> ImmutableArray.CreateRange)

        Identifier |> Node.BuildOr InvalidExpr (sym, tArgs)

    abstract OnOperationCall: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnOperationCall(method, arg) =
        let method, arg =
            this.Expressions.OnTypedExpression method, this.Expressions.OnTypedExpression arg

        CallLikeExpression |> Node.BuildOr InvalidExpr (method, arg)

    abstract OnFunctionCall: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnFunctionCall(method, arg) =
        let method, arg =
            this.Expressions.OnTypedExpression method, this.Expressions.OnTypedExpression arg

        CallLikeExpression |> Node.BuildOr InvalidExpr (method, arg)

    abstract OnPartialApplication: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnPartialApplication(method, arg) =
        let method, arg =
            this.Expressions.OnTypedExpression method, this.Expressions.OnTypedExpression arg

        CallLikeExpression |> Node.BuildOr InvalidExpr (method, arg)

    abstract OnCallLikeExpression: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnCallLikeExpression(method, arg) =
        match method.ResolvedType.Resolution with
        | _ when TypedExpression.IsPartialApplication(CallLikeExpression(method, arg)) ->
            this.OnPartialApplication(method, arg)
        | ExpressionType.Operation _ -> this.OnOperationCall(method, arg)
        | _ -> this.OnFunctionCall(method, arg)

    abstract OnAdjointApplication: TypedExpression -> ExpressionKind

    default this.OnAdjointApplication ex =
        let ex = this.Expressions.OnTypedExpression ex
        AdjointApplication |> Node.BuildOr InvalidExpr ex

    abstract OnControlledApplication: TypedExpression -> ExpressionKind

    default this.OnControlledApplication ex =
        let ex = this.Expressions.OnTypedExpression ex

        ControlledApplication |> Node.BuildOr InvalidExpr ex

    abstract OnUnwrapApplication: TypedExpression -> ExpressionKind

    default this.OnUnwrapApplication ex =
        let ex = this.Expressions.OnTypedExpression ex
        UnwrapApplication |> Node.BuildOr InvalidExpr ex

    abstract OnValueTuple: ImmutableArray<TypedExpression> -> ExpressionKind

    default this.OnValueTuple vs =
        let values =
            vs |> Seq.map this.Expressions.OnTypedExpression |> ImmutableArray.CreateRange

        ValueTuple |> Node.BuildOr InvalidExpr values

    abstract OnArrayItem: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnArrayItem(arr, idx) =
        let arr, idx =
            this.Expressions.OnTypedExpression arr, this.Expressions.OnTypedExpression idx

        ArrayItem |> Node.BuildOr InvalidExpr (arr, idx)

    abstract OnNamedItem: TypedExpression * Identifier -> ExpressionKind

    default this.OnNamedItem(ex, acc) =
        let ex = this.Expressions.OnTypedExpression ex
        NamedItem |> Node.BuildOr InvalidExpr (ex, acc)

    abstract OnValueArray: ImmutableArray<TypedExpression> -> ExpressionKind

    default this.OnValueArray vs =
        let values =
            vs |> Seq.map this.Expressions.OnTypedExpression |> ImmutableArray.CreateRange

        ValueArray |> Node.BuildOr InvalidExpr values

    abstract OnNewArray: ResolvedType * TypedExpression -> ExpressionKind

    default this.OnNewArray(bt, idx) =
        let bt, idx =
            this.Types.OnType bt, this.Expressions.OnTypedExpression idx

        NewArray |> Node.BuildOr InvalidExpr (bt, idx)

    abstract OnStringLiteral: string * ImmutableArray<TypedExpression> -> ExpressionKind

    default this.OnStringLiteral(s, exs) =
        let exs =
            exs |> Seq.map this.Expressions.OnTypedExpression |> ImmutableArray.CreateRange

        StringLiteral |> Node.BuildOr InvalidExpr (s, exs)

    abstract OnRangeLiteral: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnRangeLiteral(lhs, rhs) =
        let lhs, rhs =
            this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs

        RangeLiteral |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnCopyAndUpdateExpression: TypedExpression * TypedExpression * TypedExpression -> ExpressionKind

    default this.OnCopyAndUpdateExpression(lhs, accEx, rhs) =
        let lhs, accEx, rhs =
            this.Expressions.OnTypedExpression lhs,
            this.Expressions.OnTypedExpression accEx,
            this.Expressions.OnTypedExpression rhs

        CopyAndUpdate |> Node.BuildOr InvalidExpr (lhs, accEx, rhs)

    abstract OnConditionalExpression: TypedExpression * TypedExpression * TypedExpression -> ExpressionKind

    default this.OnConditionalExpression(cond, ifTrue, ifFalse) =
        let cond, ifTrue, ifFalse =
            this.Expressions.OnTypedExpression cond,
            this.Expressions.OnTypedExpression ifTrue,
            this.Expressions.OnTypedExpression ifFalse

        CONDITIONAL |> Node.BuildOr InvalidExpr (cond, ifTrue, ifFalse)

    abstract OnEquality: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnEquality(lhs, rhs) =
        let lhs, rhs =
            this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs

        EQ |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnInequality: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnInequality(lhs, rhs) =
        let lhs, rhs =
            this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs

        NEQ |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnLessThan: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnLessThan(lhs, rhs) =
        let lhs, rhs =
            this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs

        LT |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnLessThanOrEqual: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnLessThanOrEqual(lhs, rhs) =
        let lhs, rhs =
            this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs

        LTE |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnGreaterThan: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnGreaterThan(lhs, rhs) =
        let lhs, rhs =
            this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs

        GT |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnGreaterThanOrEqual: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnGreaterThanOrEqual(lhs, rhs) =
        let lhs, rhs =
            this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs

        GTE |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnLogicalAnd: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnLogicalAnd(lhs, rhs) =
        let lhs, rhs =
            this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs

        AND |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnLogicalOr: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnLogicalOr(lhs, rhs) =
        let lhs, rhs =
            this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs

        OR |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnAddition: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnAddition(lhs, rhs) =
        let lhs, rhs =
            this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs

        ADD |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnSubtraction: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnSubtraction(lhs, rhs) =
        let lhs, rhs =
            this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs

        SUB |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnMultiplication: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnMultiplication(lhs, rhs) =
        let lhs, rhs =
            this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs

        MUL |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnDivision: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnDivision(lhs, rhs) =
        let lhs, rhs =
            this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs

        DIV |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnExponentiate: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnExponentiate(lhs, rhs) =
        let lhs, rhs =
            this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs

        POW |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnModulo: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnModulo(lhs, rhs) =
        let lhs, rhs =
            this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs

        MOD |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnLeftShift: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnLeftShift(lhs, rhs) =
        let lhs, rhs =
            this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs

        LSHIFT |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnRightShift: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnRightShift(lhs, rhs) =
        let lhs, rhs =
            this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs

        RSHIFT |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnBitwiseExclusiveOr: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnBitwiseExclusiveOr(lhs, rhs) =
        let lhs, rhs =
            this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs

        BXOR |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnBitwiseOr: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnBitwiseOr(lhs, rhs) =
        let lhs, rhs =
            this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs

        BOR |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnBitwiseAnd: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnBitwiseAnd(lhs, rhs) =
        let lhs, rhs =
            this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs

        BAND |> Node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnLogicalNot: TypedExpression -> ExpressionKind

    default this.OnLogicalNot ex =
        let ex = this.Expressions.OnTypedExpression ex
        NOT |> Node.BuildOr InvalidExpr ex

    abstract OnNegative: TypedExpression -> ExpressionKind

    default this.OnNegative ex =
        let ex = this.Expressions.OnTypedExpression ex
        NEG |> Node.BuildOr InvalidExpr ex

    abstract OnBitwiseNot: TypedExpression -> ExpressionKind

    default this.OnBitwiseNot ex =
        let ex = this.Expressions.OnTypedExpression ex
        BNOT |> Node.BuildOr InvalidExpr ex


    // leaf nodes

    abstract OnUnitValue: unit -> ExpressionKind
    default this.OnUnitValue() = ExpressionKind.UnitValue

    abstract OnMissingExpression: unit -> ExpressionKind
    default this.OnMissingExpression() = MissingExpr

    abstract OnInvalidExpression: unit -> ExpressionKind
    default this.OnInvalidExpression() = InvalidExpr

    abstract OnIntLiteral: int64 -> ExpressionKind
    default this.OnIntLiteral i = IntLiteral i

    abstract OnBigIntLiteral: BigInteger -> ExpressionKind
    default this.OnBigIntLiteral b = BigIntLiteral b

    abstract OnDoubleLiteral: double -> ExpressionKind
    default this.OnDoubleLiteral d = DoubleLiteral d

    abstract OnBoolLiteral: bool -> ExpressionKind
    default this.OnBoolLiteral b = BoolLiteral b

    abstract OnResultLiteral: QsResult -> ExpressionKind
    default this.OnResultLiteral r = ResultLiteral r

    abstract OnPauliLiteral: QsPauli -> ExpressionKind
    default this.OnPauliLiteral p = PauliLiteral p


    // transformation root called on each node

    abstract OnExpressionKind: ExpressionKind -> ExpressionKind

    default this.OnExpressionKind kind =
        if not options.Enable then
            kind
        else
            let transformed =
                kind
                |> function
                | Identifier (sym, tArgs) -> this.OnIdentifier(sym, tArgs)
                | CallLikeExpression (method, arg) -> this.OnCallLikeExpression(method, arg)
                | AdjointApplication ex -> this.OnAdjointApplication(ex)
                | ControlledApplication ex -> this.OnControlledApplication(ex)
                | UnwrapApplication ex -> this.OnUnwrapApplication(ex)
                | UnitValue -> this.OnUnitValue()
                | MissingExpr -> this.OnMissingExpression()
                | InvalidExpr -> this.OnInvalidExpression()
                | ValueTuple vs -> this.OnValueTuple vs
                | ArrayItem (arr, idx) -> this.OnArrayItem(arr, idx)
                | NamedItem (ex, acc) -> this.OnNamedItem(ex, acc)
                | ValueArray vs -> this.OnValueArray vs
                | NewArray (bt, idx) -> this.OnNewArray(bt, idx)
                | IntLiteral i -> this.OnIntLiteral i
                | BigIntLiteral b -> this.OnBigIntLiteral b
                | DoubleLiteral d -> this.OnDoubleLiteral d
                | BoolLiteral b -> this.OnBoolLiteral b
                | ResultLiteral r -> this.OnResultLiteral r
                | PauliLiteral p -> this.OnPauliLiteral p
                | StringLiteral (s, exs) -> this.OnStringLiteral(s, exs)
                | RangeLiteral (lhs, rhs) -> this.OnRangeLiteral(lhs, rhs)
                | CopyAndUpdate (lhs, accEx, rhs) -> this.OnCopyAndUpdateExpression(lhs, accEx, rhs)
                | CONDITIONAL (cond, ifTrue, ifFalse) -> this.OnConditionalExpression(cond, ifTrue, ifFalse)
                | EQ (lhs, rhs) -> this.OnEquality(lhs, rhs)
                | NEQ (lhs, rhs) -> this.OnInequality(lhs, rhs)
                | LT (lhs, rhs) -> this.OnLessThan(lhs, rhs)
                | LTE (lhs, rhs) -> this.OnLessThanOrEqual(lhs, rhs)
                | GT (lhs, rhs) -> this.OnGreaterThan(lhs, rhs)
                | GTE (lhs, rhs) -> this.OnGreaterThanOrEqual(lhs, rhs)
                | AND (lhs, rhs) -> this.OnLogicalAnd(lhs, rhs)
                | OR (lhs, rhs) -> this.OnLogicalOr(lhs, rhs)
                | ADD (lhs, rhs) -> this.OnAddition(lhs, rhs)
                | SUB (lhs, rhs) -> this.OnSubtraction(lhs, rhs)
                | MUL (lhs, rhs) -> this.OnMultiplication(lhs, rhs)
                | DIV (lhs, rhs) -> this.OnDivision(lhs, rhs)
                | POW (lhs, rhs) -> this.OnExponentiate(lhs, rhs)
                | MOD (lhs, rhs) -> this.OnModulo(lhs, rhs)
                | LSHIFT (lhs, rhs) -> this.OnLeftShift(lhs, rhs)
                | RSHIFT (lhs, rhs) -> this.OnRightShift(lhs, rhs)
                | BXOR (lhs, rhs) -> this.OnBitwiseExclusiveOr(lhs, rhs)
                | BOR (lhs, rhs) -> this.OnBitwiseOr(lhs, rhs)
                | BAND (lhs, rhs) -> this.OnBitwiseAnd(lhs, rhs)
                | NOT ex -> this.OnLogicalNot(ex)
                | NEG ex -> this.OnNegative(ex)
                | BNOT ex -> this.OnBitwiseNot(ex)

            id |> Node.BuildOr kind transformed


and ExpressionTransformationBase internal (options: TransformationOptions, _internal_) =

    let missingTransformation name _ =
        new InvalidOperationException(sprintf "No %s transformation has been specified." name) |> raise

    let Node = if options.Rebuild then Fold else Walk

    member val internal TypeTransformationHandle = missingTransformation "type" with get, set
    member val internal ExpressionKindTransformationHandle = missingTransformation "expression kind" with get, set

    member this.Types = this.TypeTransformationHandle()

    member this.ExpressionKinds =
        this.ExpressionKindTransformationHandle()

    new(exkindTransformation: unit -> ExpressionKindTransformationBase,
        typeTransformation: unit -> TypeTransformationBase,
        options: TransformationOptions) as this =
        new ExpressionTransformationBase(options, "_internal_")
        then
            this.TypeTransformationHandle <- typeTransformation
            this.ExpressionKindTransformationHandle <- exkindTransformation

    new(options: TransformationOptions) as this =
        new ExpressionTransformationBase(options, "_internal_")
        then
            let typeTransformation = new TypeTransformationBase(options)

            let exprKindTransformation =
                new ExpressionKindTransformationBase((fun _ -> this), (fun _ -> this.Types), options)

            this.TypeTransformationHandle <- fun _ -> typeTransformation
            this.ExpressionKindTransformationHandle <- fun _ -> exprKindTransformation

    new(exkindTransformation: unit -> ExpressionKindTransformationBase,
        typeTransformation: unit -> TypeTransformationBase) =
        new ExpressionTransformationBase(exkindTransformation, typeTransformation, TransformationOptions.Default)

    new() = new ExpressionTransformationBase(TransformationOptions.Default)


    // supplementary expression information

    abstract OnRangeInformation: QsNullable<Range> -> QsNullable<Range>
    default this.OnRangeInformation range = range

    abstract OnExpressionInformation: InferredExpressionInformation -> InferredExpressionInformation
    default this.OnExpressionInformation info = info


    // nodes containing subexpressions or subtypes

    abstract OnTypeParamResolutions: ImmutableDictionary<(QsQualifiedName * string), ResolvedType>
     -> ImmutableDictionary<(QsQualifiedName * string), ResolvedType>

    default this.OnTypeParamResolutions typeParams =
        let asTypeParameter (key) =
            QsTypeParameter.New(fst key, snd key, Null)

        let filteredTypeParams =
            typeParams
            |> Seq.map (fun kv -> this.Types.OnTypeParameter(kv.Key |> asTypeParameter), kv.Value)
            |> Seq.choose (function
                | TypeParameter tp, value -> Some((tp.Origin, tp.TypeName), this.Types.OnType value)
                | _ -> None)
            |> Seq.map (fun (key, value) -> new KeyValuePair<_, _>(key, value))

        if options.Rebuild then
            ImmutableDictionary.CreateRange filteredTypeParams
        else
            filteredTypeParams |> Seq.iter ignore
            typeParams

    // transformation root called on each node

    abstract OnTypedExpression: TypedExpression -> TypedExpression

    default this.OnTypedExpression(ex: TypedExpression) =
        if not options.Enable then
            ex
        else
            let range = this.OnRangeInformation ex.Range

            let typeParamResolutions =
                this.OnTypeParamResolutions ex.TypeParameterResolutions

            let kind =
                this.ExpressionKinds.OnExpressionKind ex.Expression

            let exType = this.Types.OnType ex.ResolvedType

            let inferredInfo =
                this.OnExpressionInformation ex.InferredInformation

            TypedExpression.New |> Node.BuildOr ex (kind, typeParamResolutions, exType, inferredInfo, range)
