// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core.Utils

type private ExpressionType = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>


type TypeTransformationBase(options: TransformationOptions) =

    let Node = if options.Rebuild then Fold else Walk
    new() = new TypeTransformationBase(TransformationOptions.Default)


    // supplementary type information

    abstract OnRangeInformation: QsNullable<Range> -> QsNullable<Range>
    default this.OnRangeInformation range = range

    abstract OnCharacteristicsExpression: ResolvedCharacteristics -> ResolvedCharacteristics
    default this.OnCharacteristicsExpression fs = fs

    abstract OnCallableInformation: CallableInformation -> CallableInformation

    default this.OnCallableInformation opInfo =
        let characteristics =
            this.OnCharacteristicsExpression opInfo.Characteristics

        let inferred = opInfo.InferredInformation

        CallableInformation.New |> Node.BuildOr opInfo (characteristics, inferred)


    // nodes containing subtypes

    abstract OnUserDefinedType: UserDefinedType -> ExpressionType

    default this.OnUserDefinedType udt =
        let ns, name = udt.Namespace, udt.Name
        let range = this.OnRangeInformation udt.Range

        ExpressionType.UserDefinedType << UserDefinedType.New |> Node.BuildOr InvalidType (ns, name, range)

    abstract OnTypeParameter: QsTypeParameter -> ExpressionType

    default this.OnTypeParameter tp =
        let origin = tp.Origin
        let name = tp.TypeName
        let range = this.OnRangeInformation tp.Range

        ExpressionType.TypeParameter << QsTypeParameter.New
        |> Node.BuildOr InvalidType (origin, name, range)

    abstract OnOperation: (ResolvedType * ResolvedType) * CallableInformation -> ExpressionType

    default this.OnOperation((it, ot), info) =
        let transformed =
            (this.OnType it, this.OnType ot), this.OnCallableInformation info

        ExpressionType.Operation |> Node.BuildOr InvalidType transformed

    abstract OnFunction: ResolvedType * ResolvedType -> ExpressionType

    default this.OnFunction(it, ot) =
        let transformed = this.OnType it, this.OnType ot

        ExpressionType.Function |> Node.BuildOr InvalidType transformed

    abstract OnTupleType: ImmutableArray<ResolvedType> -> ExpressionType

    default this.OnTupleType ts =
        let transformed =
            ts |> Seq.map this.OnType |> ImmutableArray.CreateRange

        ExpressionType.TupleType |> Node.BuildOr InvalidType transformed

    abstract OnArrayType: ResolvedType -> ExpressionType

    default this.OnArrayType b =
        ExpressionType.ArrayType |> Node.BuildOr InvalidType (this.OnType b)


    // leaf nodes

    abstract OnUnitType: unit -> ExpressionType
    default this.OnUnitType() = ExpressionType.UnitType

    abstract OnQubit: unit -> ExpressionType
    default this.OnQubit() = ExpressionType.Qubit

    abstract OnMissingType: unit -> ExpressionType
    default this.OnMissingType() = ExpressionType.MissingType

    abstract OnInvalidType: unit -> ExpressionType
    default this.OnInvalidType() = ExpressionType.InvalidType

    abstract OnInt: unit -> ExpressionType
    default this.OnInt() = ExpressionType.Int

    abstract OnBigInt: unit -> ExpressionType
    default this.OnBigInt() = ExpressionType.BigInt

    abstract OnDouble: unit -> ExpressionType
    default this.OnDouble() = ExpressionType.Double

    abstract OnBool: unit -> ExpressionType
    default this.OnBool() = ExpressionType.Bool

    abstract OnString: unit -> ExpressionType
    default this.OnString() = ExpressionType.String

    abstract OnResult: unit -> ExpressionType
    default this.OnResult() = ExpressionType.Result

    abstract OnPauli: unit -> ExpressionType
    default this.OnPauli() = ExpressionType.Pauli

    abstract OnRange: unit -> ExpressionType
    default this.OnRange() = ExpressionType.Range


    // transformation root called on each node

    member this.OnType(t: ResolvedType) =
        if not options.Enable then
            t
        else
            let transformed =
                t.Resolution
                |> function
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

            let ResolvedType t = ResolvedType.New(true, t)
            ResolvedType |> Node.BuildOr t transformed
