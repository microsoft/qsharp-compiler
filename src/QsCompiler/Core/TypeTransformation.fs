// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core.Utils

type private ExpressionType = 
    QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>


type TypeTransformationBase(options : TransformationOptions) = 

    let Node = if options.DisableRebuild then Walk else Fold
    new () = new TypeTransformationBase(TransformationOptions.Default)


    // supplementary type information 

    abstract member onRangeInformation : QsRangeInfo -> QsRangeInfo
    default this.onRangeInformation r = r

    abstract member onCharacteristicsExpression : ResolvedCharacteristics -> ResolvedCharacteristics
    default this.onCharacteristicsExpression fs = fs

    abstract member onCallableInformation : CallableInformation -> CallableInformation
    default this.onCallableInformation opInfo = 
        let characteristics = this.onCharacteristicsExpression opInfo.Characteristics
        let inferred = opInfo.InferredInformation
        CallableInformation.New |> Node.BuildOr opInfo (characteristics, inferred)


    // nodes containing subtypes

    abstract member onUserDefinedType : UserDefinedType -> ExpressionType
    default this.onUserDefinedType udt = 
        let ns, name = udt.Namespace, udt.Name
        let range = this.onRangeInformation udt.Range
        ExpressionType.UserDefinedType << UserDefinedType.New |> Node.BuildOr InvalidType (ns, name, range)

    abstract member onTypeParameter : QsTypeParameter -> ExpressionType
    default this.onTypeParameter tp = 
        let origin = tp.Origin
        let name = tp.TypeName
        let range = this.onRangeInformation tp.Range
        ExpressionType.TypeParameter << QsTypeParameter.New |> Node.BuildOr InvalidType (origin, name, range)

    abstract member onOperation : (ResolvedType * ResolvedType) * CallableInformation -> ExpressionType
    default this.onOperation ((it, ot), info) = 
        ExpressionType.Operation |> Node.BuildOr InvalidType ((this.Transform it, this.Transform ot), this.onCallableInformation info)

    abstract member onFunction : ResolvedType * ResolvedType -> ExpressionType
    default this.onFunction (it, ot) = 
        ExpressionType.Function |> Node.BuildOr InvalidType (this.Transform it, this.Transform ot)

    abstract member onTupleType : ImmutableArray<ResolvedType> -> ExpressionType
    default this.onTupleType ts = 
        ExpressionType.TupleType |> Node.BuildOr InvalidType ((ts |> Seq.map this.Transform).ToImmutableArray())

    abstract member onArrayType : ResolvedType -> ExpressionType
    default this.onArrayType b = 
        ExpressionType.ArrayType |> Node.BuildOr InvalidType (this.Transform b)


    // leaf nodes
    
    abstract member onUnitType : unit -> ExpressionType
    default this.onUnitType () = ExpressionType.UnitType

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


    // transformation root called on each node

    member this.Transform (t : ResolvedType) =
        if options.Disable then t else
        let transformed = t.Resolution |> function
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
        ResolvedType.New |> Node.BuildOr t transformed
