// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

#nowarn "44" // TODO: RELEASE 2022-09, reenable after OnArrayItem and OnNamedItem are removed.

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

type ExpressionKindTransformationBase(expressionTransformation: _ -> ExpressionTransformationBase, options) =
    let node = if options.Rebuild then Fold else Walk

    member _.Types = expressionTransformation().Types

    member _.Expressions = expressionTransformation ()

    member internal this.Common = expressionTransformation().Types.Common

    // TODO: RELEASE 2022-09: Remove member.
    [<Obsolete("Please use ExpressionKindTransformationBase(unit -> ExpressionTransformationBase, TransformationOptions) instead.")>]
    new(expressionTransformation, typeTransformation: unit -> TypeTransformationBase, options: TransformationOptions) =
        ExpressionKindTransformationBase(expressionTransformation, options)

    new(options) as this =
        let types = TypeTransformationBase options
        let expressions = ExpressionTransformationBase((fun () -> this), (fun () -> types), options)
        ExpressionKindTransformationBase((fun () -> expressions), options)

    new(expressionTransformation) =
        ExpressionKindTransformationBase(expressionTransformation, TransformationOptions.Default)

    // TODO: RELEASE 2022-09: Remove member.
    [<Obsolete("Please use ExpressionKindTransformationBase(unit -> ExpressionTransformationBase) instead.")>]
    new(expressionTransformation, typeTransformation: unit -> TypeTransformationBase) =
        ExpressionKindTransformationBase(expressionTransformation, TransformationOptions.Default)

    new() = ExpressionKindTransformationBase TransformationOptions.Default

    // nodes containing subexpressions or subtypes

    abstract OnIdentifier: Identifier * QsNullable<ImmutableArray<ResolvedType>> -> ExpressionKind

    default this.OnIdentifier(sym, tArgs) =
        let tArgs =
            tArgs |> QsNullable<_>.Map (fun ts -> ts |> Seq.map this.Types.OnType |> ImmutableArray.CreateRange)

        let idName =
            match sym with
            | LocalVariable name -> this.Common.OnLocalName name |> LocalVariable
            | GlobalCallable _
            | InvalidIdentifier -> sym

        Identifier |> node.BuildOr InvalidExpr (idName, tArgs)

    abstract OnOperationCall: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnOperationCall(method, arg) =
        let method, arg = this.Expressions.OnTypedExpression method, this.Expressions.OnTypedExpression arg
        CallLikeExpression |> node.BuildOr InvalidExpr (method, arg)

    abstract OnFunctionCall: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnFunctionCall(method, arg) =
        let method, arg = this.Expressions.OnTypedExpression method, this.Expressions.OnTypedExpression arg
        CallLikeExpression |> node.BuildOr InvalidExpr (method, arg)

    abstract OnPartialApplication: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnPartialApplication(method, arg) =
        let method, arg = this.Expressions.OnTypedExpression method, this.Expressions.OnTypedExpression arg
        CallLikeExpression |> node.BuildOr InvalidExpr (method, arg)

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
        AdjointApplication |> node.BuildOr InvalidExpr ex

    abstract OnControlledApplication: TypedExpression -> ExpressionKind

    default this.OnControlledApplication ex =
        let ex = this.Expressions.OnTypedExpression ex
        ControlledApplication |> node.BuildOr InvalidExpr ex

    abstract OnUnwrapApplication: TypedExpression -> ExpressionKind

    default this.OnUnwrapApplication ex =
        let ex = this.Expressions.OnTypedExpression ex
        UnwrapApplication |> node.BuildOr InvalidExpr ex

    abstract OnValueTuple: ImmutableArray<TypedExpression> -> ExpressionKind

    default this.OnValueTuple vs =
        let values = vs |> Seq.map this.Expressions.OnTypedExpression |> ImmutableArray.CreateRange
        ValueTuple |> node.BuildOr InvalidExpr values

    // TODO: RELEASE 2022-09: Remove member.
    [<Obsolete "Use OnArrayItemAccess instead">]
    abstract OnArrayItem: TypedExpression * TypedExpression -> ExpressionKind

    // TODO: RELEASE 2022-09: Remove member.
    [<Obsolete "Use OnArrayItemAccess instead">]
    override this.OnArrayItem(arr, idx) =
        let arr, idx = this.Expressions.OnTypedExpression arr, this.Expressions.OnTypedExpression idx
        ArrayItem |> node.BuildOr InvalidExpr (arr, idx)

    abstract OnArrayItemAccess: TypedExpression * TypedExpression -> ExpressionKind
    default this.OnArrayItemAccess(arr, idx) = this.OnArrayItem(arr, idx) // replace with the implementation once the deprecated member is removed

    // TODO: RELEASE 2022-09: Remove member.
    [<Obsolete "Use OnNamedItemAccess instead">]
    abstract OnNamedItem: TypedExpression * Identifier -> ExpressionKind

    // TODO: RELEASE 2022-09: Remove member.
    [<Obsolete "Use OnNamedItemAccess instead">]
    override this.OnNamedItem(ex, acc) =
        let lhs = this.Expressions.OnTypedExpression ex

        let acc =
            match ex.ResolvedType.Resolution, acc with
            | UserDefinedType udt, LocalVariable itemName -> this.Common.OnItemName(udt, itemName) |> LocalVariable
            | _ -> acc

        NamedItem |> node.BuildOr InvalidExpr (lhs, acc)

    abstract OnNamedItemAccess: TypedExpression * Identifier -> ExpressionKind
    default this.OnNamedItemAccess(ex, acc) = this.OnNamedItem(ex, acc) // replace with the implementation once the deprecated member is removed

    abstract OnValueArray: ImmutableArray<TypedExpression> -> ExpressionKind

    default this.OnValueArray vs =
        let values = vs |> Seq.map this.Expressions.OnTypedExpression |> ImmutableArray.CreateRange
        ValueArray |> node.BuildOr InvalidExpr values

    abstract OnNewArray: ResolvedType * TypedExpression -> ExpressionKind

    default this.OnNewArray(bt, idx) =
        let bt, idx = this.Types.OnType bt, this.Expressions.OnTypedExpression idx
        NewArray |> node.BuildOr InvalidExpr (bt, idx)

    abstract OnSizedArray: value: TypedExpression * size: TypedExpression -> ExpressionKind

    default this.OnSizedArray(value, size) =
        let value = this.Expressions.OnTypedExpression value
        let size = this.Expressions.OnTypedExpression size
        SizedArray |> node.BuildOr InvalidExpr (value, size)

    abstract OnStringLiteral: string * ImmutableArray<TypedExpression> -> ExpressionKind

    default this.OnStringLiteral(s, exs) =
        let exs = exs |> Seq.map this.Expressions.OnTypedExpression |> ImmutableArray.CreateRange
        StringLiteral |> node.BuildOr InvalidExpr (s, exs)

    abstract OnRangeLiteral: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnRangeLiteral(lhs, rhs) =
        let lhs, rhs = this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs
        RangeLiteral |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnCopyAndUpdateExpression: TypedExpression * TypedExpression * TypedExpression -> ExpressionKind

    default this.OnCopyAndUpdateExpression(lhs, accEx, rhs) =
        let updated = this.Expressions.OnTypedExpression lhs

        let accEx =
            match lhs.ResolvedType.Resolution, accEx.Expression with
            | UserDefinedType udt, Identifier (LocalVariable itemName, Null) ->
                let range = this.Common.OnExpressionRange(accEx.Range)
                let itemName = this.Common.OnItemName(udt, itemName) |> LocalVariable
                let itemType = this.Types.OnType accEx.ResolvedType
                let info = this.Expressions.OnExpressionInformation accEx.InferredInformation
                TypedExpression.New(Identifier(itemName, Null), ImmutableDictionary.Empty, itemType, info, range)
            | _ -> this.Expressions.OnTypedExpression accEx

        let rhs = this.Expressions.OnTypedExpression rhs
        CopyAndUpdate |> node.BuildOr InvalidExpr (updated, accEx, rhs)

    abstract OnConditionalExpression: TypedExpression * TypedExpression * TypedExpression -> ExpressionKind

    default this.OnConditionalExpression(cond, ifTrue, ifFalse) =
        let cond, ifTrue, ifFalse =
            this.Expressions.OnTypedExpression cond,
            this.Expressions.OnTypedExpression ifTrue,
            this.Expressions.OnTypedExpression ifFalse

        CONDITIONAL |> node.BuildOr InvalidExpr (cond, ifTrue, ifFalse)

    abstract OnEquality: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnEquality(lhs, rhs) =
        let lhs, rhs = this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs
        EQ |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnInequality: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnInequality(lhs, rhs) =
        let lhs, rhs = this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs
        NEQ |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnLessThan: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnLessThan(lhs, rhs) =
        let lhs, rhs = this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs
        LT |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnLessThanOrEqual: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnLessThanOrEqual(lhs, rhs) =
        let lhs, rhs = this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs
        LTE |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnGreaterThan: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnGreaterThan(lhs, rhs) =
        let lhs, rhs = this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs
        GT |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnGreaterThanOrEqual: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnGreaterThanOrEqual(lhs, rhs) =
        let lhs, rhs = this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs
        GTE |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnLogicalAnd: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnLogicalAnd(lhs, rhs) =
        let lhs, rhs = this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs
        AND |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnLogicalOr: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnLogicalOr(lhs, rhs) =
        let lhs, rhs = this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs
        OR |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnAddition: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnAddition(lhs, rhs) =
        let lhs, rhs = this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs
        ADD |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnSubtraction: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnSubtraction(lhs, rhs) =
        let lhs, rhs = this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs
        SUB |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnMultiplication: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnMultiplication(lhs, rhs) =
        let lhs, rhs = this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs
        MUL |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnDivision: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnDivision(lhs, rhs) =
        let lhs, rhs = this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs
        DIV |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnExponentiate: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnExponentiate(lhs, rhs) =
        let lhs, rhs = this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs
        POW |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnModulo: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnModulo(lhs, rhs) =
        let lhs, rhs = this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs
        MOD |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnLeftShift: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnLeftShift(lhs, rhs) =
        let lhs, rhs = this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs
        LSHIFT |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnRightShift: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnRightShift(lhs, rhs) =
        let lhs, rhs = this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs
        RSHIFT |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnBitwiseExclusiveOr: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnBitwiseExclusiveOr(lhs, rhs) =
        let lhs, rhs = this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs
        BXOR |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnBitwiseOr: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnBitwiseOr(lhs, rhs) =
        let lhs, rhs = this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs
        BOR |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnBitwiseAnd: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnBitwiseAnd(lhs, rhs) =
        let lhs, rhs = this.Expressions.OnTypedExpression lhs, this.Expressions.OnTypedExpression rhs
        BAND |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnLogicalNot: TypedExpression -> ExpressionKind

    default this.OnLogicalNot ex =
        let ex = this.Expressions.OnTypedExpression ex
        NOT |> node.BuildOr InvalidExpr ex

    abstract OnNegative: TypedExpression -> ExpressionKind

    default this.OnNegative ex =
        let ex = this.Expressions.OnTypedExpression ex
        NEG |> node.BuildOr InvalidExpr ex

    abstract OnBitwiseNot: TypedExpression -> ExpressionKind

    default this.OnBitwiseNot ex =
        let ex = this.Expressions.OnTypedExpression ex
        BNOT |> node.BuildOr InvalidExpr ex

    abstract OnLambda: lambda: Lambda<TypedExpression, ResolvedType> -> ExpressionKind

    default this.OnLambda lambda =
        let syms = this.Common.OnArgumentTuple lambda.ArgumentTuple
        let body = this.Expressions.OnTypedExpression lambda.Body
        Lambda.create lambda.Kind syms >> Lambda |> node.BuildOr InvalidExpr body

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
                match kind with
                | Identifier (sym, tArgs) -> this.OnIdentifier(sym, tArgs)
                | CallLikeExpression (method, arg) -> this.OnCallLikeExpression(method, arg)
                | AdjointApplication ex -> this.OnAdjointApplication(ex)
                | ControlledApplication ex -> this.OnControlledApplication(ex)
                | UnwrapApplication ex -> this.OnUnwrapApplication(ex)
                | UnitValue -> this.OnUnitValue()
                | MissingExpr -> this.OnMissingExpression()
                | InvalidExpr -> this.OnInvalidExpression()
                | ValueTuple vs -> this.OnValueTuple vs
                | ArrayItem (arr, idx) -> this.OnArrayItemAccess(arr, idx)
                | NamedItem (ex, acc) -> this.OnNamedItemAccess(ex, acc)
                | ValueArray vs -> this.OnValueArray vs
                | NewArray (bt, idx) -> this.OnNewArray(bt, idx)
                | SizedArray (value, size) -> this.OnSizedArray(value, size)
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
                | Lambda lambda -> this.OnLambda lambda

            id |> node.BuildOr kind transformed

and ExpressionTransformationBase(exkindTransformation, typeTransformation, options) =
    let node = if options.Rebuild then Fold else Walk

    member _.Types: TypeTransformationBase = typeTransformation ()

    member _.ExpressionKinds = exkindTransformation ()

    member internal _.Common = typeTransformation().Common

    new(options) as this =
        let types = TypeTransformationBase options
        let kinds = ExpressionKindTransformationBase((fun () -> this), options)
        ExpressionTransformationBase((fun () -> kinds), (fun () -> types), options)

    new(exkindTransformation, typeTransformation) =
        ExpressionTransformationBase(exkindTransformation, typeTransformation, TransformationOptions.Default)

    new() = ExpressionTransformationBase TransformationOptions.Default

    // supplementary expression information

    // TODO: RELEASE 2022-09: Remove member.
    [<Obsolete "Use SyntaxTreeTransformation.OnExpressionRange instead.">]
    abstract OnRangeInformation: QsNullable<Range> -> QsNullable<Range>

    // TODO: RELEASE 2022-09: Remove member.
    [<Obsolete "Use SyntaxTreeTransformation.OnExpressionRange instead.">]
    override this.OnRangeInformation range = range

    abstract OnExpressionInformation: InferredExpressionInformation -> InferredExpressionInformation
    default this.OnExpressionInformation info = info

    // nodes containing subexpressions or subtypes

    abstract OnTypeParamResolutions:
        ImmutableDictionary<(QsQualifiedName * string), ResolvedType> ->
            ImmutableDictionary<(QsQualifiedName * string), ResolvedType>

    default this.OnTypeParamResolutions typeParams =
        let filteredTypeParams =
            typeParams
            |> Seq.choose (fun item ->
                let origin, name = item.Key

                match QsTypeParameter.New(origin, name) |> this.Types.OnTypeParameter with
                | TypeParameter p -> Some(KeyValuePair.Create((p.Origin, p.TypeName), this.Types.OnType item.Value))
                | _ -> None)

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
            let range = this.Common.OnExpressionRange ex.Range
            let typeParamResolutions = this.OnTypeParamResolutions ex.TypeParameterResolutions
            let kind = this.ExpressionKinds.OnExpressionKind ex.Expression
            let exType = this.Types.OnType ex.ResolvedType
            let inferredInfo = this.OnExpressionInformation ex.InferredInformation
            TypedExpression.New |> node.BuildOr ex (kind, typeParamResolutions, exType, inferredInfo, range)
