// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

open System
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open System.Numerics
open System.Collections.Generic
open System.Linq

#nowarn "44" // RELEASE 2022-09: Re-enable after updating the ICommonTransformation implementations.

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Transformations.Core.Utils

type private ExpressionType = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>
type private ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>
type private QsArgumentTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>

// setup for syntax tree transformations with internal state

type MonoTransformation<'T>(state, options) =

    let node = if options.Rebuild then Fold else Walk

    member _.SharedState = state

    new(state) = MonoTransformation(state, TransformationOptions.Default)

    /// Invokes the transformation for all namespaces in the given compilation.
    member this.OnCompilation compilation =
        if options.Rebuild then
            let namespaces =
                compilation.Namespaces |> Seq.map this.OnNamespace |> ImmutableArray.CreateRange

            QsCompilation.New(namespaces, compilation.EntryPoints)
        else
            compilation.Namespaces |> Seq.iter (this.OnNamespace >> ignore)
            compilation

    abstract OnLocalNameDeclaration: name: string -> string
    default _.OnLocalNameDeclaration name = name

    abstract OnLocalName: name: string -> string
    default _.OnLocalName name = name

    abstract OnItemNameDeclaration: name: string -> string
    default _.OnItemNameDeclaration name = name

    abstract OnItemName: parentType: UserDefinedType * itemName: string -> string
    default _.OnItemName(_, itemName) = itemName

    abstract OnArgumentTuple: argTuple: QsArgumentTuple -> QsArgumentTuple

    default this.OnArgumentTuple argTuple =
        match argTuple with
        | QsTuple items as original ->
            let transformed = items |> Seq.map this.OnArgumentTuple |> ImmutableArray.CreateRange
            QsTuple |> node.BuildOr original transformed
        | QsTupleItem item as original ->
            let loc = this.OnSymbolLocation(item.Position, item.Range)
            let name = this.OnArgumentName item.VariableName // replace with the implementation once the deprecated member is removed
            let t = this.OnType item.Type
            let info = this.OnExpressionInformation item.InferredInformation
            let newDecl = LocalVariableDeclaration.New info.IsMutable (loc, name, t, info.HasLocalQuantumDependency)
            QsTupleItem |> node.BuildOr original newDecl

    abstract OnAbsoluteLocation: location: QsNullable<QsLocation> -> QsNullable<QsLocation>
    default _.OnAbsoluteLocation location = location

    abstract OnRelativeLocation: location: QsNullable<QsLocation> -> QsNullable<QsLocation>
    default _.OnRelativeLocation location = location

    abstract OnSymbolLocation: offset: QsNullable<Position> * range: Range -> QsNullable<Position> * Range
    default _.OnSymbolLocation(offset, range) = (offset, range)

    abstract OnExpressionRange: range: QsNullable<Range> -> QsNullable<Range>
    default _.OnExpressionRange range = range

    abstract OnTypeRange: range: TypeRange -> TypeRange
    default _.OnTypeRange range = range

    //interface ICommonTransformation with
    //    member this.OnLocalNameDeclaration name = this.OnLocalNameDeclaration name
    //    member this.OnLocalName name = this.OnLocalName name
    //    member this.OnItemNameDeclaration name = this.OnItemNameDeclaration name
    //    member this.OnItemName(parentType, itemName) = this.OnItemName(parentType, itemName)
    //    member this.OnArgumentTuple argTuple = this.OnArgumentTuple argTuple
    //    member this.OnAbsoluteLocation location = this.OnAbsoluteLocation location
    //    member this.OnRelativeLocation location = this.OnRelativeLocation location
    //    member this.OnSymbolLocation(offset, range) = this.OnSymbolLocation(offset, range)
    //    member this.OnExpressionRange range = this.OnExpressionRange range
    //    member this.OnTypeRange range = this.OnTypeRange range

//and TypeTransformation<'T>(parentTransformation, options) =

    // supplementary type information

    abstract OnCharacteristicsExpression: ResolvedCharacteristics -> ResolvedCharacteristics
    default _.OnCharacteristicsExpression fs = fs

    abstract OnCallableInformation: CallableInformation -> CallableInformation

    default this.OnCallableInformation opInfo =
        let characteristics = this.OnCharacteristicsExpression opInfo.Characteristics
        let inferred = opInfo.InferredInformation
        CallableInformation.New |> node.BuildOr opInfo (characteristics, inferred)


    // nodes containing subtypes

    abstract OnUserDefinedType: UserDefinedType -> ExpressionType

    default this.OnUserDefinedType udt =
        let ns, name = udt.Namespace, udt.Name
        let range = this.OnExpressionRange udt.Range // udt.Range should be removed along with OnRangeInformation
        node.BuildOr InvalidType (ns, name, range) (UserDefinedType.New >> UserDefinedType)

    abstract OnTypeParameter: QsTypeParameter -> ExpressionType

    default this.OnTypeParameter tp =
        let origin = tp.Origin
        let name = tp.TypeName
        let range = this.OnExpressionRange tp.Range // tp.Range should be removed along with OnRangeInformation
        node.BuildOr InvalidType (origin, name, range) (QsTypeParameter.New >> TypeParameter)

    abstract OnOperation: (ResolvedType * ResolvedType) * CallableInformation -> ExpressionType

    default this.OnOperation((it, ot), info) =
        let transformed = (this.OnType it, this.OnType ot), this.OnCallableInformation info
        ExpressionType.Operation |> node.BuildOr InvalidType transformed

    abstract OnFunction: ResolvedType * ResolvedType -> ExpressionType

    default this.OnFunction(it, ot) =
        let transformed = this.OnType it, this.OnType ot
        ExpressionType.Function |> node.BuildOr InvalidType transformed

    abstract OnTupleType: ImmutableArray<ResolvedType> -> ExpressionType

    default this.OnTupleType ts =
        let transformed = ts |> Seq.map this.OnType |> ImmutableArray.CreateRange
        ExpressionType.TupleType |> node.BuildOr InvalidType transformed

    abstract OnArrayType: ResolvedType -> ExpressionType

    default this.OnArrayType b =
        ExpressionType.ArrayType |> node.BuildOr InvalidType (this.OnType b)


    // leaf nodes

    abstract OnUnitType: unit -> ExpressionType
    default _.OnUnitType() = ExpressionType.UnitType

    abstract OnQubit: unit -> ExpressionType
    default _.OnQubit() = ExpressionType.Qubit

    abstract OnMissingType: unit -> ExpressionType
    default _.OnMissingType() = ExpressionType.MissingType

    abstract OnInvalidType: unit -> ExpressionType
    default _.OnInvalidType() = ExpressionType.InvalidType

    abstract OnInt: unit -> ExpressionType
    default _.OnInt() = ExpressionType.Int

    abstract OnBigInt: unit -> ExpressionType
    default _.OnBigInt() = ExpressionType.BigInt

    abstract OnDouble: unit -> ExpressionType
    default _.OnDouble() = ExpressionType.Double

    abstract OnBool: unit -> ExpressionType
    default _.OnBool() = ExpressionType.Bool

    abstract OnString: unit -> ExpressionType
    default _.OnString() = ExpressionType.String

    abstract OnResult: unit -> ExpressionType
    default _.OnResult() = ExpressionType.Result

    abstract OnPauli: unit -> ExpressionType
    default _.OnPauli() = ExpressionType.Pauli

    abstract OnRange: unit -> ExpressionType
    default _.OnRange() = ExpressionType.Range


    // transformation root called on each node

    abstract OnType: ResolvedType -> ResolvedType

    default this.OnType(t: ResolvedType) =
        if not options.Enable then
            t
        else
            let range = this.OnTypeRange t.Range

            let transformed =
                match t.Resolution with
                | ExpressionType.UnitType -> this.OnUnitType()
                | ExpressionType.Operation ((it, ot), fs) -> this.OnOperation((it, ot), fs)
                | ExpressionType.Function (it, ot) -> this.OnFunction(it, ot)
                | ExpressionType.TupleType ts -> this.OnTupleType ts
                | ExpressionType.ArrayType b -> this.OnArrayType b
                | ExpressionType.UserDefinedType udt -> this.OnUserDefinedType udt
                | ExpressionType.TypeParameter tp -> this.OnTypeParameter tp
                | ExpressionType.Qubit -> this.OnQubit()
                | ExpressionType.MissingType -> this.OnMissingType()
                | ExpressionType.InvalidType -> this.OnInvalidType()
                | ExpressionType.Int -> this.OnInt()
                | ExpressionType.BigInt -> this.OnBigInt()
                | ExpressionType.Double -> this.OnDouble()
                | ExpressionType.Bool -> this.OnBool()
                | ExpressionType.String -> this.OnString()
                | ExpressionType.Result -> this.OnResult()
                | ExpressionType.Pauli -> this.OnPauli()
                | ExpressionType.Range -> this.OnRange()

            ResolvedType.create range |> node.BuildOr t transformed

//and ExpressionKindTransformation<'T>(parentTransformation, options) as this =

// nodes containing subexpressions or subtypes

    abstract OnIdentifier: Identifier * QsNullable<ImmutableArray<ResolvedType>> -> ExpressionKind

    default this.OnIdentifier(sym, tArgs) =
        let tArgs =
            tArgs |> QsNullable<_>.Map (fun ts -> ts |> Seq.map this.OnType |> ImmutableArray.CreateRange)

        let idName =
            match sym with
            | LocalVariable name -> this.OnLocalName name |> LocalVariable
            | GlobalCallable _
            | InvalidIdentifier -> sym

        Identifier |> node.BuildOr InvalidExpr (idName, tArgs)

    abstract OnOperationCall: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnOperationCall(method, arg) =
        let method, arg = this.OnTypedExpression method, this.OnTypedExpression arg
        CallLikeExpression |> node.BuildOr InvalidExpr (method, arg)

    abstract OnFunctionCall: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnFunctionCall(method, arg) =
        let method, arg = this.OnTypedExpression method, this.OnTypedExpression arg
        CallLikeExpression |> node.BuildOr InvalidExpr (method, arg)

    abstract OnPartialApplication: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnPartialApplication(method, arg) =
        let method, arg = this.OnTypedExpression method, this.OnTypedExpression arg
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
        let ex = this.OnTypedExpression ex
        AdjointApplication |> node.BuildOr InvalidExpr ex

    abstract OnControlledApplication: TypedExpression -> ExpressionKind

    default this.OnControlledApplication ex =
        let ex = this.OnTypedExpression ex
        ControlledApplication |> node.BuildOr InvalidExpr ex

    abstract OnUnwrapApplication: TypedExpression -> ExpressionKind

    default this.OnUnwrapApplication ex =
        let ex = this.OnTypedExpression ex
        UnwrapApplication |> node.BuildOr InvalidExpr ex

    abstract OnValueTuple: ImmutableArray<TypedExpression> -> ExpressionKind

    default this.OnValueTuple vs =
        let values = vs |> Seq.map this.OnTypedExpression |> ImmutableArray.CreateRange
        ValueTuple |> node.BuildOr InvalidExpr values

    abstract OnArrayItemAccess: TypedExpression * TypedExpression -> ExpressionKind
    default this.OnArrayItemAccess(arr, idx) =
        let arr, idx = this.OnTypedExpression arr, this.OnTypedExpression idx
        ArrayItem |> node.BuildOr InvalidExpr (arr, idx)

    abstract OnNamedItemAccess: TypedExpression * Identifier -> ExpressionKind
    default this.OnNamedItemAccess(ex, acc) =
        let lhs = this.OnTypedExpression ex
        let acc =
            match ex.ResolvedType.Resolution, acc with
            | UserDefinedType udt, LocalVariable itemName -> this.OnItemName(udt, itemName) |> LocalVariable
            | _ -> acc
        
        NamedItem |> node.BuildOr InvalidExpr (lhs, acc)

    abstract OnValueArray: ImmutableArray<TypedExpression> -> ExpressionKind

    default this.OnValueArray vs =
        let values = vs |> Seq.map this.OnTypedExpression |> ImmutableArray.CreateRange
        ValueArray |> node.BuildOr InvalidExpr values

    abstract OnNewArray: ResolvedType * TypedExpression -> ExpressionKind

    default this.OnNewArray(bt, idx) =
        let bt, idx = this.OnType bt, this.OnTypedExpression idx
        NewArray |> node.BuildOr InvalidExpr (bt, idx)

    abstract OnSizedArray: value: TypedExpression * size: TypedExpression -> ExpressionKind

    default this.OnSizedArray(value, size) =
        let value = this.OnTypedExpression value
        let size = this.OnTypedExpression size
        SizedArray |> node.BuildOr InvalidExpr (value, size)

    abstract OnStringLiteral: string * ImmutableArray<TypedExpression> -> ExpressionKind

    default this.OnStringLiteral(s, exs) =
        let exs = exs |> Seq.map this.OnTypedExpression |> ImmutableArray.CreateRange
        StringLiteral |> node.BuildOr InvalidExpr (s, exs)

    abstract OnRangeLiteral: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnRangeLiteral(lhs, rhs) =
        let lhs, rhs = this.OnTypedExpression lhs, this.OnTypedExpression rhs
        RangeLiteral |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnCopyAndUpdateExpression: TypedExpression * TypedExpression * TypedExpression -> ExpressionKind

    default this.OnCopyAndUpdateExpression(lhs, accEx, rhs) =
        let updated = this.OnTypedExpression lhs

        let accEx =
            match lhs.ResolvedType.Resolution, accEx.Expression with
            | UserDefinedType udt, Identifier (LocalVariable itemName, Null) ->
                let range = this.OnExpressionRange(accEx.Range)
                let itemName = this.OnItemName(udt, itemName) |> LocalVariable
                let itemType = this.OnType accEx.ResolvedType
                let info = this.OnExpressionInformation accEx.InferredInformation
                TypedExpression.New(Identifier(itemName, Null), ImmutableDictionary.Empty, itemType, info, range)
            | _ -> this.OnTypedExpression accEx

        let rhs = this.OnTypedExpression rhs
        CopyAndUpdate |> node.BuildOr InvalidExpr (updated, accEx, rhs)

    abstract OnConditionalExpression: TypedExpression * TypedExpression * TypedExpression -> ExpressionKind

    default this.OnConditionalExpression(cond, ifTrue, ifFalse) =
        let cond, ifTrue, ifFalse =
            this.OnTypedExpression cond,
            this.OnTypedExpression ifTrue,
            this.OnTypedExpression ifFalse

        CONDITIONAL |> node.BuildOr InvalidExpr (cond, ifTrue, ifFalse)

    abstract OnEquality: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnEquality(lhs, rhs) =
        let lhs, rhs = this.OnTypedExpression lhs, this.OnTypedExpression rhs
        EQ |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnInequality: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnInequality(lhs, rhs) =
        let lhs, rhs = this.OnTypedExpression lhs, this.OnTypedExpression rhs
        NEQ |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnLessThan: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnLessThan(lhs, rhs) =
        let lhs, rhs = this.OnTypedExpression lhs, this.OnTypedExpression rhs
        LT |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnLessThanOrEqual: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnLessThanOrEqual(lhs, rhs) =
        let lhs, rhs = this.OnTypedExpression lhs, this.OnTypedExpression rhs
        LTE |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnGreaterThan: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnGreaterThan(lhs, rhs) =
        let lhs, rhs = this.OnTypedExpression lhs, this.OnTypedExpression rhs
        GT |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnGreaterThanOrEqual: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnGreaterThanOrEqual(lhs, rhs) =
        let lhs, rhs = this.OnTypedExpression lhs, this.OnTypedExpression rhs
        GTE |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnLogicalAnd: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnLogicalAnd(lhs, rhs) =
        let lhs, rhs = this.OnTypedExpression lhs, this.OnTypedExpression rhs
        AND |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnLogicalOr: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnLogicalOr(lhs, rhs) =
        let lhs, rhs = this.OnTypedExpression lhs, this.OnTypedExpression rhs
        OR |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnAddition: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnAddition(lhs, rhs) =
        let lhs, rhs = this.OnTypedExpression lhs, this.OnTypedExpression rhs
        ADD |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnSubtraction: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnSubtraction(lhs, rhs) =
        let lhs, rhs = this.OnTypedExpression lhs, this.OnTypedExpression rhs
        SUB |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnMultiplication: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnMultiplication(lhs, rhs) =
        let lhs, rhs = this.OnTypedExpression lhs, this.OnTypedExpression rhs
        MUL |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnDivision: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnDivision(lhs, rhs) =
        let lhs, rhs = this.OnTypedExpression lhs, this.OnTypedExpression rhs
        DIV |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnExponentiate: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnExponentiate(lhs, rhs) =
        let lhs, rhs = this.OnTypedExpression lhs, this.OnTypedExpression rhs
        POW |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnModulo: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnModulo(lhs, rhs) =
        let lhs, rhs = this.OnTypedExpression lhs, this.OnTypedExpression rhs
        MOD |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnLeftShift: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnLeftShift(lhs, rhs) =
        let lhs, rhs = this.OnTypedExpression lhs, this.OnTypedExpression rhs
        LSHIFT |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnRightShift: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnRightShift(lhs, rhs) =
        let lhs, rhs = this.OnTypedExpression lhs, this.OnTypedExpression rhs
        RSHIFT |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnBitwiseExclusiveOr: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnBitwiseExclusiveOr(lhs, rhs) =
        let lhs, rhs = this.OnTypedExpression lhs, this.OnTypedExpression rhs
        BXOR |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnBitwiseOr: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnBitwiseOr(lhs, rhs) =
        let lhs, rhs = this.OnTypedExpression lhs, this.OnTypedExpression rhs
        BOR |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnBitwiseAnd: TypedExpression * TypedExpression -> ExpressionKind

    default this.OnBitwiseAnd(lhs, rhs) =
        let lhs, rhs = this.OnTypedExpression lhs, this.OnTypedExpression rhs
        BAND |> node.BuildOr InvalidExpr (lhs, rhs)

    abstract OnLogicalNot: TypedExpression -> ExpressionKind

    default this.OnLogicalNot ex =
        let ex = this.OnTypedExpression ex
        NOT |> node.BuildOr InvalidExpr ex

    abstract OnNegative: TypedExpression -> ExpressionKind

    default this.OnNegative ex =
        let ex = this.OnTypedExpression ex
        NEG |> node.BuildOr InvalidExpr ex

    abstract OnBitwiseNot: TypedExpression -> ExpressionKind

    default this.OnBitwiseNot ex =
        let ex = this.OnTypedExpression ex
        BNOT |> node.BuildOr InvalidExpr ex

    abstract OnLambda: lambda: Lambda<TypedExpression, ResolvedType> -> ExpressionKind

    default this.OnLambda lambda =
        let syms = this.OnArgumentTuple lambda.ArgumentTuple
        let body = this.OnTypedExpression lambda.Body
        Lambda.create lambda.Kind syms >> Lambda |> node.BuildOr InvalidExpr body

    // leaf nodes

    abstract OnUnitValue: unit -> ExpressionKind
    default _.OnUnitValue() = ExpressionKind.UnitValue

    abstract OnMissingExpression: unit -> ExpressionKind
    default _.OnMissingExpression() = MissingExpr

    abstract OnInvalidExpression: unit -> ExpressionKind
    default _.OnInvalidExpression() = InvalidExpr

    abstract OnIntLiteral: int64 -> ExpressionKind
    default _.OnIntLiteral i = IntLiteral i

    abstract OnBigIntLiteral: BigInteger -> ExpressionKind
    default _.OnBigIntLiteral b = BigIntLiteral b

    abstract OnDoubleLiteral: double -> ExpressionKind
    default _.OnDoubleLiteral d = DoubleLiteral d

    abstract OnBoolLiteral: bool -> ExpressionKind
    default _.OnBoolLiteral b = BoolLiteral b

    abstract OnResultLiteral: QsResult -> ExpressionKind
    default _.OnResultLiteral r = ResultLiteral r

    abstract OnPauliLiteral: QsPauli -> ExpressionKind
    default _.OnPauliLiteral p = PauliLiteral p

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

//and ExpressionTransformation<'T>(parentTransformation, options) as this =
    // supplementary expression information

    abstract OnExpressionInformation: InferredExpressionInformation -> InferredExpressionInformation
    default _.OnExpressionInformation info = info

    // nodes containing subexpressions or subtypes

    abstract OnTypeParamResolutions:
        ImmutableDictionary<(QsQualifiedName * string), ResolvedType> ->
            ImmutableDictionary<(QsQualifiedName * string), ResolvedType>

    default this.OnTypeParamResolutions typeParams =
        let filteredTypeParams =
            typeParams
            |> Seq.map (fun kv -> QsTypeParameter.New(fst kv.Key, snd kv.Key) |> this.OnTypeParameter, kv.Value)
            |> Seq.choose (function
                | TypeParameter tp, value -> Some((tp.Origin, tp.TypeName), this.OnType value)
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
            let range = this.OnExpressionRange ex.Range
            let typeParamResolutions = this.OnTypeParamResolutions ex.TypeParameterResolutions
            let kind = this.OnExpressionKind ex.Expression
            let exType = this.OnType ex.ResolvedType
            let inferredInfo = this.OnExpressionInformation ex.InferredInformation
            TypedExpression.New |> node.BuildOr ex (kind, typeParamResolutions, exType, inferredInfo, range)

//and StatementKindTransformation<'T>(parentTransformation, options: TransformationOptions) as this =
    // subconstructs used within statements

    abstract OnSymbolTuple: SymbolTuple -> SymbolTuple

    default this.OnSymbolTuple syms =
        match syms with
        | VariableNameTuple tuple ->
            tuple |> Seq.map this.OnSymbolTuple |> ImmutableArray.CreateRange |> VariableNameTuple
        | VariableName name -> this.OnLocalNameDeclaration name |> VariableName
        | DiscardedItem
        | InvalidItem -> syms

    abstract OnQubitInitializer: ResolvedInitializer -> ResolvedInitializer

    default this.OnQubitInitializer init =
        let transformed =
            match init.Resolution with
            | SingleQubitAllocation -> SingleQubitAllocation
            | QubitRegisterAllocation ex as orig ->
                QubitRegisterAllocation |> node.BuildOr orig (this.OnTypedExpression ex)
            | QubitTupleAllocation is as orig ->
                QubitTupleAllocation
                |> node.BuildOr orig (is |> Seq.map this.OnQubitInitializer |> ImmutableArray.CreateRange)
            | InvalidInitializer -> InvalidInitializer

        ResolvedInitializer.New |> node.BuildOr init transformed

    abstract OnPositionedBlock:
        QsNullable<TypedExpression> * QsPositionedBlock -> QsNullable<TypedExpression> * QsPositionedBlock

    default this.OnPositionedBlock(intro: QsNullable<TypedExpression>, block: QsPositionedBlock) =
        let location = this.OnRelativeLocation block.Location
        let comments = block.Comments
        let expr = intro |> QsNullable<_>.Map this.OnTypedExpression
        let body = this.OnScope block.Body

        let PositionedBlock (expr, body, location, comments) =
            expr, QsPositionedBlock.New comments location body

        PositionedBlock |> node.BuildOr(intro, block) (expr, body, location, comments)

    // statements containing subconstructs or expressions

    abstract OnVariableDeclaration: QsBinding<TypedExpression> -> QsStatementKind

    default this.OnVariableDeclaration stm =
        let rhs = this.OnTypedExpression stm.Rhs
        let lhs = this.OnSymbolTuple stm.Lhs

        QsVariableDeclaration << QsBinding<TypedExpression>.New stm.Kind
        |> node.BuildOr EmptyStatement (lhs, rhs)

    abstract OnValueUpdate: QsValueUpdate -> QsStatementKind

    default this.OnValueUpdate stm =
        let rhs = this.OnTypedExpression stm.Rhs
        let lhs = this.OnTypedExpression stm.Lhs
        QsValueUpdate << QsValueUpdate.New |> node.BuildOr EmptyStatement (lhs, rhs)

    abstract OnConditionalStatement: QsConditionalStatement -> QsStatementKind

    default this.OnConditionalStatement stm =
        let cases =
            stm.ConditionalBlocks
            |> Seq.map (fun (c, b) ->
                let cond, block = this.OnPositionedBlock(Value c, b)

                let invalidCondition () =
                    failwith "missing condition in if-statement"

                cond.ValueOrApply invalidCondition, block)
            |> ImmutableArray.CreateRange

        let defaultCase = stm.Default |> QsNullable<_>.Map (fun b -> this.OnPositionedBlock(Null, b) |> snd)

        QsConditionalStatement << QsConditionalStatement.New
        |> node.BuildOr EmptyStatement (cases, defaultCase)

    abstract OnForStatement: QsForStatement -> QsStatementKind

    default this.OnForStatement stm =
        let iterVals = this.OnTypedExpression stm.IterationValues
        let loopVar = fst stm.LoopItem |> this.OnSymbolTuple
        let loopVarType = this.OnType(snd stm.LoopItem)
        let body = this.OnScope stm.Body

        QsForStatement << QsForStatement.New
        |> node.BuildOr EmptyStatement ((loopVar, loopVarType), iterVals, body)

    abstract OnWhileStatement: QsWhileStatement -> QsStatementKind

    default this.OnWhileStatement stm =
        let condition = this.OnTypedExpression stm.Condition
        let body = this.OnScope stm.Body
        QsWhileStatement << QsWhileStatement.New |> node.BuildOr EmptyStatement (condition, body)

    abstract OnRepeatStatement: QsRepeatStatement -> QsStatementKind

    default this.OnRepeatStatement stm =
        let repeatBlock = this.OnPositionedBlock(Null, stm.RepeatBlock) |> snd
        let successCondition, fixupBlock = this.OnPositionedBlock(Value stm.SuccessCondition, stm.FixupBlock)

        let invalidCondition () =
            failwith "missing success condition in repeat-statement"

        QsRepeatStatement << QsRepeatStatement.New
        |> node.BuildOr EmptyStatement (repeatBlock, successCondition.ValueOrApply invalidCondition, fixupBlock)

    abstract OnConjugation: QsConjugation -> QsStatementKind

    default this.OnConjugation stm =
        let outer = this.OnPositionedBlock(Null, stm.OuterTransformation) |> snd
        let inner = this.OnPositionedBlock(Null, stm.InnerTransformation) |> snd
        QsConjugation << QsConjugation.New |> node.BuildOr EmptyStatement (outer, inner)

    abstract OnExpressionStatement: TypedExpression -> QsStatementKind

    default this.OnExpressionStatement ex =
        let transformed = this.OnTypedExpression ex
        QsExpressionStatement |> node.BuildOr EmptyStatement transformed

    abstract OnReturnStatement: TypedExpression -> QsStatementKind

    default this.OnReturnStatement ex =
        let transformed = this.OnTypedExpression ex
        QsReturnStatement |> node.BuildOr EmptyStatement transformed

    abstract OnFailStatement: TypedExpression -> QsStatementKind

    default this.OnFailStatement ex =
        let transformed = this.OnTypedExpression ex
        QsFailStatement |> node.BuildOr EmptyStatement transformed

    /// This method is defined for the sole purpose of eliminating code duplication for each of the specialization kinds.
    /// It is hence not intended and should never be needed for public use.
    member private this.OnQubitScopeKind(stm: QsQubitScope) =
        let kind = stm.Kind
        let rhs = this.OnQubitInitializer stm.Binding.Rhs
        let lhs = this.OnSymbolTuple stm.Binding.Lhs
        let body = this.OnScope stm.Body
        QsQubitScope << QsQubitScope.New kind |> node.BuildOr EmptyStatement ((lhs, rhs), body)

    abstract OnAllocateQubits: QsQubitScope -> QsStatementKind
    default this.OnAllocateQubits stm = this.OnQubitScopeKind stm

    abstract OnBorrowQubits: QsQubitScope -> QsStatementKind
    default this.OnBorrowQubits stm = this.OnQubitScopeKind stm

    abstract OnQubitScope: QsQubitScope -> QsStatementKind

    default this.OnQubitScope(stm: QsQubitScope) =
        match stm.Kind with
        | Allocate -> this.OnAllocateQubits stm
        | Borrow -> this.OnBorrowQubits stm

    // leaf nodes

    abstract OnEmptyStatement: unit -> QsStatementKind
    default _.OnEmptyStatement() = EmptyStatement

    // transformation root called on each statement

    abstract OnStatementKind: QsStatementKind -> QsStatementKind

    default this.OnStatementKind kind =
        if not options.Enable then
            kind
        else
            let transformed =
                match kind with
                | QsExpressionStatement ex -> this.OnExpressionStatement ex
                | QsReturnStatement ex -> this.OnReturnStatement ex
                | QsFailStatement ex -> this.OnFailStatement ex
                | QsVariableDeclaration stm -> this.OnVariableDeclaration stm
                | QsValueUpdate stm -> this.OnValueUpdate stm
                | QsConditionalStatement stm -> this.OnConditionalStatement stm
                | QsForStatement stm -> this.OnForStatement stm
                | QsWhileStatement stm -> this.OnWhileStatement stm
                | QsRepeatStatement stm -> this.OnRepeatStatement stm
                | QsConjugation stm -> this.OnConjugation stm
                | QsQubitScope stm -> this.OnQubitScope stm
                | EmptyStatement -> this.OnEmptyStatement()

            id |> node.BuildOr kind transformed

//and StatementTransformation<'T>(parentTransformation, options) as this =
    // supplementary statement information

    abstract OnLocalDeclarations: LocalDeclarations -> LocalDeclarations

    default this.OnLocalDeclarations decl =
        let onLocalVariableDeclaration (local: LocalVariableDeclaration<string>) =
            let loc = this.OnSymbolLocation(local.Position, local.Range)
            let name = this.OnLocalName local.VariableName
            let varType = this.OnType local.Type
            let info = this.OnExpressionInformation local.InferredInformation
            LocalVariableDeclaration.New info.IsMutable (loc, name, varType, info.HasLocalQuantumDependency)

        let variableDeclarations = decl.Variables |> Seq.map onLocalVariableDeclaration

        if options.Rebuild then
            LocalDeclarations.New(variableDeclarations |> ImmutableArray.CreateRange)
        else
            variableDeclarations |> Seq.iter ignore
            decl

    // transformation roots called on each statement or statement block

    abstract OnStatement: QsStatement -> QsStatement

    default this.OnStatement stm =
        if not options.Enable then
            stm
        else
            let location = this.OnRelativeLocation stm.Location
            let comments = stm.Comments
            let kind = this.OnStatementKind stm.Statement
            let varDecl = this.OnLocalDeclarations stm.SymbolDeclarations
            QsStatement.New comments location |> node.BuildOr stm (kind, varDecl)

    abstract OnScope: QsScope -> QsScope

    default this.OnScope scope =
        if not options.Enable then
            scope
        else
            let parentSymbols = this.OnLocalDeclarations scope.KnownSymbols
            let statements = scope.Statements |> Seq.map this.OnStatement

            if options.Rebuild then
                QsScope.New(statements |> ImmutableArray.CreateRange, parentSymbols)
            else
                statements |> Seq.iter ignore
                scope

//and NamespaceTransformation<'T>(parentTransformation, options) as this =
    // subconstructs used within declarations

    abstract OnDocumentation: ImmutableArray<string> -> ImmutableArray<string>
    default _.OnDocumentation doc = doc

    abstract OnSource: Source -> Source
    default _.OnSource source = source

    abstract OnAttribute: QsDeclarationAttribute -> QsDeclarationAttribute
    default _.OnAttribute att = att

    abstract OnTypeItems: QsTuple<QsTypeItem> -> QsTuple<QsTypeItem>

    default this.OnTypeItems tItem =
        match tItem with
        | QsTuple items as original ->
            let transformed = items |> Seq.map this.OnTypeItems |> ImmutableArray.CreateRange
            QsTuple |> node.BuildOr original transformed
        | QsTupleItem (Anonymous itemType) as original ->
            let t = this.OnType itemType
            QsTupleItem << Anonymous |> node.BuildOr original t
        | QsTupleItem (Named item) as original ->
            let loc = this.OnSymbolLocation(item.Position, item.Range)
            let name = this.OnItemNameDeclaration item.VariableName
            let t = this.OnType item.Type
            let info = this.OnExpressionInformation item.InferredInformation

            QsTupleItem << Named << LocalVariableDeclaration.New info.IsMutable
            |> node.BuildOr original (loc, name, t, info.HasLocalQuantumDependency)

    // TODO: RELEASE 2022-09: Remove.
    [<Obsolete "Use SyntaxTreeTransformation.OnLocalNameDeclaration or override OnArgumentTuple instead.">]
    abstract OnArgumentName: QsLocalSymbol -> QsLocalSymbol

    // TODO: RELEASE 2022-09: Remove.
    [<Obsolete "Use SyntaxTreeTransformation.OnLocalNameDeclaration or override OnArgumentTuple instead.">]
    override this.OnArgumentName arg =
        match arg with
        | ValidName name -> this.OnLocalNameDeclaration name |> ValidName
        | InvalidName -> arg

    abstract OnSignature: ResolvedSignature -> ResolvedSignature

    default this.OnSignature(s: ResolvedSignature) =
        let typeParams = s.TypeParameters // if this had a range is should be handled by the corresponding Common nodes
        let argType = this.OnType s.ArgumentType
        let returnType = this.OnType s.ReturnType
        let info = this.OnCallableInformation s.Information
        ResolvedSignature.New |> node.BuildOr s ((argType, returnType), info, typeParams)

    // specialization declarations and implementations

    abstract OnProvidedImplementation: QsArgumentTuple * QsScope -> QsArgumentTuple * QsScope

    default this.OnProvidedImplementation(argTuple, body) =
        let argTuple = this.OnArgumentTuple argTuple
        let body = this.OnScope body
        argTuple, body

    abstract OnSelfInverseDirective: unit -> unit
    default _.OnSelfInverseDirective() = ()

    abstract OnInvertDirective: unit -> unit
    default _.OnInvertDirective() = ()

    abstract OnDistributeDirective: unit -> unit
    default _.OnDistributeDirective() = ()

    abstract OnInvalidGeneratorDirective: unit -> unit
    default _.OnInvalidGeneratorDirective() = ()

    abstract OnExternalImplementation: unit -> unit
    default _.OnExternalImplementation() = ()

    abstract OnIntrinsicImplementation: unit -> unit
    default _.OnIntrinsicImplementation() = ()

    abstract OnGeneratedImplementation: QsGeneratorDirective -> QsGeneratorDirective

    default this.OnGeneratedImplementation(directive: QsGeneratorDirective) =
        match directive with
        | SelfInverse ->
            this.OnSelfInverseDirective()
            SelfInverse
        | Invert ->
            this.OnInvertDirective()
            Invert
        | Distribute ->
            this.OnDistributeDirective()
            Distribute
        | InvalidGenerator ->
            this.OnInvalidGeneratorDirective()
            InvalidGenerator

    abstract OnSpecializationImplementation: SpecializationImplementation -> SpecializationImplementation

    default this.OnSpecializationImplementation(implementation: SpecializationImplementation) =
        let Build kind transformed =
            kind |> node.BuildOr implementation transformed

        match implementation with
        | External ->
            this.OnExternalImplementation()
            External
        | Intrinsic ->
            this.OnIntrinsicImplementation()
            Intrinsic
        | Generated dir -> this.OnGeneratedImplementation dir |> Build Generated
        | Provided (argTuple, body) -> this.OnProvidedImplementation(argTuple, body) |> Build Provided

    /// This method is defined for the sole purpose of eliminating code duplication for each of the specialization kinds.
    /// It is hence not intended and should never be needed for public use.
    member private this.OnSpecializationKind(spec: QsSpecialization) =
        let source = this.OnSource spec.Source
        let loc = this.OnAbsoluteLocation spec.Location
        let attributes = spec.Attributes |> Seq.map this.OnAttribute |> ImmutableArray.CreateRange

        let typeArgs =
            spec.TypeArguments
            |> QsNullable<_>.Map (fun args -> args |> Seq.map this.OnType |> ImmutableArray.CreateRange)

        let signature = this.OnSignature spec.Signature
        let impl = this.OnSpecializationImplementation spec.Implementation
        let doc = this.OnDocumentation spec.Documentation
        let comments = spec.Comments

        QsSpecialization.New spec.Kind (source, loc)
        |> node.BuildOr spec (spec.Parent, attributes, typeArgs, signature, impl, doc, comments)

    abstract OnBodySpecialization: QsSpecialization -> QsSpecialization
    default this.OnBodySpecialization spec = this.OnSpecializationKind spec

    abstract OnAdjointSpecialization: QsSpecialization -> QsSpecialization
    default this.OnAdjointSpecialization spec = this.OnSpecializationKind spec

    abstract OnControlledSpecialization: QsSpecialization -> QsSpecialization
    default this.OnControlledSpecialization spec = this.OnSpecializationKind spec

    abstract OnControlledAdjointSpecialization: QsSpecialization -> QsSpecialization
    default this.OnControlledAdjointSpecialization spec = this.OnSpecializationKind spec

    abstract OnSpecializationDeclaration: QsSpecialization -> QsSpecialization

    default this.OnSpecializationDeclaration(spec: QsSpecialization) =
        match spec.Kind with
        | QsSpecializationKind.QsBody -> this.OnBodySpecialization spec
        | QsSpecializationKind.QsAdjoint -> this.OnAdjointSpecialization spec
        | QsSpecializationKind.QsControlled -> this.OnControlledSpecialization spec
        | QsSpecializationKind.QsControlledAdjoint -> this.OnControlledAdjointSpecialization spec

    // type and callable declarations

    /// This method is defined for the sole purpose of eliminating code duplication for each of the callable kinds.
    /// It is hence not intended and should never be needed for public use.
    member private this.OnCallableKind(c: QsCallable) =
        let source = this.OnSource c.Source
        let loc = this.OnAbsoluteLocation c.Location
        let attributes = c.Attributes |> Seq.map this.OnAttribute |> ImmutableArray.CreateRange
        let signature = this.OnSignature c.Signature
        let argTuple = this.OnArgumentTuple c.ArgumentTuple

        let specializations =
            c.Specializations
            |> Seq.sortBy (fun c -> c.Kind)
            |> Seq.map this.OnSpecializationDeclaration
            |> ImmutableArray.CreateRange

        let doc = this.OnDocumentation c.Documentation
        let comments = c.Comments

        QsCallable.New c.Kind (source, loc)
        |> node.BuildOr c (c.FullName, attributes, c.Access, argTuple, signature, specializations, doc, comments)

    abstract OnOperation: QsCallable -> QsCallable
    default this.OnOperation c = this.OnCallableKind c

    abstract OnFunction: QsCallable -> QsCallable
    default this.OnFunction c = this.OnCallableKind c

    abstract OnTypeConstructor: QsCallable -> QsCallable
    default this.OnTypeConstructor c = this.OnCallableKind c

    abstract OnCallableDeclaration: QsCallable -> QsCallable

    default this.OnCallableDeclaration(c: QsCallable) =
        match c.Kind with
        | QsCallableKind.Function -> this.OnFunction c
        | QsCallableKind.Operation -> this.OnOperation c
        | QsCallableKind.TypeConstructor -> this.OnTypeConstructor c

    abstract OnTypeDeclaration: QsCustomType -> QsCustomType

    default this.OnTypeDeclaration t =
        let source = this.OnSource t.Source
        let loc = this.OnAbsoluteLocation t.Location
        let attributes = t.Attributes |> Seq.map this.OnAttribute |> ImmutableArray.CreateRange
        let underlyingType = this.OnType t.Type
        let typeItems = this.OnTypeItems t.TypeItems
        let doc = this.OnDocumentation t.Documentation
        let comments = t.Comments

        QsCustomType.New(source, loc)
        |> node.BuildOr t (t.FullName, attributes, t.Access, typeItems, underlyingType, doc, comments)

    // transformation roots called on each namespace or namespace element

    abstract OnNamespaceElement: QsNamespaceElement -> QsNamespaceElement

    default this.OnNamespaceElement element =
        if not options.Enable then
            element
        else
            match element with
            | QsCustomType t -> t |> this.OnTypeDeclaration |> QsCustomType
            | QsCallable c -> c |> this.OnCallableDeclaration |> QsCallable

    abstract OnNamespace: QsNamespace -> QsNamespace

    default this.OnNamespace ns =
        if not options.Enable then
            ns
        else
            let name = ns.Name

            let doc =
                ns
                    .Documentation
                    .AsEnumerable()
                    .SelectMany(fun entry -> entry |> Seq.map (fun doc -> entry.Key, this.OnDocumentation doc))
                    .ToLookup(fst, snd)

            let elements = ns.Elements |> Seq.map this.OnNamespaceElement

            if options.Rebuild then
                QsNamespace.New(name, elements |> ImmutableArray.CreateRange, doc)
            else
                elements |> Seq.iter ignore
                ns
