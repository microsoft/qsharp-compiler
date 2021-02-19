// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference

open System.Collections.Immutable
open System.Collections.Generic

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.VerificationTools
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open Microsoft.Quantum.QsCompiler.Utils
open Microsoft.Quantum.QsCompiler.Diagnostics

/// used for type matching arguments in call-like expressions
type internal Variance =
    | Covariant
    | Contravariant
    | Invariant

let internal invalid = InvalidType |> ResolvedType.New

/// Return the string representation for a ResolveType.
/// User defined types are represented by their full name.
let internal toString (t: ResolvedType) = SyntaxTreeToQsharp.Default.ToCode t

/// Given two resolve types, determines and returns a common base type if such a type exists,
/// or pushes adds a suitable error using addError and returns invalid type if a common base type does not exist.
/// Adds an ExpressionOfUnknownType error if either of the types contains a missing type.
/// Adds an InvalidUseOfTypeParameterizedObject error if the types contain external type parameters,
/// i.e. type parameters that do not belong to the given parent (callable specialization).
/// Adds a ConstrainsTypeParameter error if an internal type parameter (i.e. one that belongs to the given parent) in one type
/// does not correspond to the same type parameter in the other type (or an invalid type).
/// Note: the only subtyping that occurs is due to operation supporting only a proper subset of the functors supported by their derived type.
/// This subtyping carries over to tuple types containing operations, and callable types containing operations as within their in- and/or output type.
/// However, arrays in particular are treated as invariant;
/// i.e. an array of operations of type t1 are *not* a subtype of arrays of operations of type t2 even if t1 is a subtype of t2.
let private commonType variance
                       addError
                       mismatchErr
                       parent
                       (lhsType: ResolvedType, lhsRange)
                       (rhsType: ResolvedType, rhsRange)
                       : ResolvedType =
    let raiseError errCode (lhsCond, rhsCond) =
        if lhsCond then lhsRange |> addError errCode
        if rhsCond then rhsRange |> addError errCode
        invalid

    let rec matchInAndOutputType variance (i1, o1) (i2, o2) =
        let inputVariance =
            match variance with
            | Covariant -> Contravariant
            | Contravariant -> Covariant
            | Invariant -> Invariant

        let argType = matchTypes inputVariance (i1, i2) // variance changes for the argument type *only*
        let resType = matchTypes variance (o1, o2)
        argType, resType

    and commonOpType variance ((i1, o1), s1: CallableInformation) ((i2, o2), s2: CallableInformation) =
        let argType, resType = matchInAndOutputType variance (i1, o1) (i2, o2)

        let characteristics =
            match variance with
            | Covariant -> CallableInformation.Common [ s1; s2 ]
            | Contravariant -> // no information can ever be inferred in this case, since contravariance only occurs within the type signatures of passed callables
                CallableInformation.New
                    (Union(s1.Characteristics, s2.Characteristics) |> ResolvedCharacteristics.New,
                     InferredCallableInformation.NoInformation)
            | Invariant when s1.Characteristics.AreInvalid
                             || s2.Characteristics.AreInvalid
                             || s1.Characteristics.GetProperties().SetEquals(s2.Characteristics.GetProperties()) ->
                let characteristics = if s1.Characteristics.AreInvalid then s2.Characteristics else s1.Characteristics

                let inferred =
                    InferredCallableInformation.Common [ s1.InferredInformation
                                                         s2.InferredInformation ]

                CallableInformation.New(characteristics, inferred)
            | Invariant ->
                raiseError mismatchErr (true, true) |> ignore

                CallableInformation.New
                    (ResolvedCharacteristics.New InvalidSetExpr, InferredCallableInformation.NoInformation)

        QsTypeKind.Operation((argType, resType), characteristics) |> ResolvedType.New

    and matchTypes variance (t1: ResolvedType, t2: ResolvedType) =
        match t1.Resolution, t2.Resolution with
        | _ when t1.isMissing || t2.isMissing ->
            raiseError (ErrorCode.ExpressionOfUnknownType, []) (t1.isMissing, t2.isMissing)
        | QsTypeKind.ArrayType b1, QsTypeKind.ArrayType b2 when b1.isMissing || b2.isMissing ->
            if b1.isMissing then t2 else t1
        | QsTypeKind.ArrayType b1, QsTypeKind.ArrayType b2 ->
            matchTypes Invariant (b1, b2) |> ArrayType |> ResolvedType.New
        | QsTypeKind.TupleType ts1, QsTypeKind.TupleType ts2 when ts1.Length = ts2.Length ->
            (Seq.zip ts1 ts2 |> Seq.map (matchTypes variance)).ToImmutableArray()
            |> TupleType
            |> ResolvedType.New
        | QsTypeKind.UserDefinedType udt1, QsTypeKind.UserDefinedType udt2 when udt1 = udt2 -> t1
        | QsTypeKind.Operation ((i1, o1), l1), QsTypeKind.Operation ((i2, o2), l2) ->
            commonOpType variance ((i1, o1), l1) ((i2, o2), l2)
        | QsTypeKind.Function (i1, o1), QsTypeKind.Function (i2, o2) ->
            matchInAndOutputType variance (i1, o1) (i2, o2) |> QsTypeKind.Function |> ResolvedType.New
        | QsTypeKind.TypeParameter tp1, QsTypeKind.TypeParameter tp2 when tp1 = tp2 && tp1.Origin = parent -> t1
        | QsTypeKind.TypeParameter tp1, QsTypeKind.TypeParameter tp2 when tp1 = tp2 ->
            raiseError (ErrorCode.InvalidUseOfTypeParameterizedObject, []) (true, true)
        | QsTypeKind.TypeParameter tp, QsTypeKind.InvalidType when tp.Origin = parent -> t1
        | QsTypeKind.InvalidType, QsTypeKind.TypeParameter tp when tp.Origin = parent -> t2
        | QsTypeKind.TypeParameter _, _ ->
            raiseError (ErrorCode.ConstrainsTypeParameter, [ t1 |> toString ]) (true, false)
        | _, QsTypeKind.TypeParameter _ ->
            raiseError (ErrorCode.ConstrainsTypeParameter, [ t2 |> toString ]) (false, true)
        | _ when t1.isInvalid || t2.isInvalid -> if t1.isInvalid then t2 else t1
        | _ when t1 = t2 -> t1
        | _ -> raiseError mismatchErr (true, true)

    matchTypes variance (lhsType, rhsType)

let internal CommonBaseType addError = commonType Covariant addError

type private Substitution = { Variance: Variance; Type: ResolvedType }

let private merge substitutions =
    let common variance left right =
        commonType variance (fun _ _ -> ()) (ErrorCode.InvalidType, []) { Name = ""; Namespace = "" } (left, Range.Zero)
            (right, Range.Zero)

    let substitute variance typ =
        Option.map (common variance typ) >> Option.defaultValue typ >> Some

    let folder (lower, upper) substitution =
        match substitution.Variance with
        | Covariant -> lower, upper |> substitute Covariant substitution.Type
        | Contravariant -> lower |> substitute Contravariant substitution.Type, upper
        | Invariant -> lower |> substitute Invariant substitution.Type, upper |> substitute Invariant substitution.Type

    match substitutions |> List.fold folder (None, None) with
    | Some lower, Some upper ->
        if lower = upper
        then lower
        else failwithf "Merge failed: conflicting bounds %A and %A." lower upper
    | Some t, None
    | None, Some t -> t
    | None, None -> failwith "Merge failed: empty substitution."

let private isSubsetOf info1 info2 =
    info1.Characteristics.GetProperties().IsSubsetOf(info2.Characteristics.GetProperties())

// TODO
let private greatestSubtype = List.head

type internal Constraint =
    | Semigroup
    | Iterable of item: ResolvedType

type InferenceContext(origin) =
    let mutable count = 0

    let substitutions = Dictionary()

    let mutable constraints = []

    let bind param substitution =
        match substitutions.TryGetValue param |> tryOption with
        | Some v -> substitutions.[param] <- substitution :: v
        | None -> substitutions.[param] <- [ substitution ]

    member internal context.Fresh() =
        let name = sprintf "__t%d__" count
        count <- count + 1

        {
            Origin = origin
            TypeName = name
            Range = Null
        }
        |> TypeParameter
        |> ResolvedType.New

    member internal context.Unify(left: ResolvedType, right: ResolvedType) =
        // TODO: Make sure type parameters are actually placeholders created by this context and not foralls.
        match left.Resolution, right.Resolution with
        | TypeParameter param, _ -> bind param { Variance = Contravariant; Type = right }
        | _, TypeParameter param -> bind param { Variance = Covariant; Type = left }
        | ArrayType item1, ArrayType item2 -> context.Unify(item1, item2) // TODO: Invariant.
        | TupleType items1, TupleType items2 -> Seq.zip items1 items2 |> Seq.iter context.Unify
        | QsTypeKind.Operation ((in1, out1), info1), QsTypeKind.Operation ((in2, out2), info2) when isSubsetOf
                                                                                                        info1
                                                                                                        info2 ->
            // TODO: Variance.
            [ in1, in2; out1, out2 ] |> List.iter context.Unify
        | QsTypeKind.Function (in1, out1), QsTypeKind.Function (in2, out2) ->
            // TODO: Variance.
            [ in1, in2; out1, out2 ] |> List.iter context.Unify
        | _ ->
            if left <> right
            then failwithf "Cannot unify %A <: %A" left.Resolution right.Resolution

    member internal context.Constrain(resolvedType: ResolvedType, ``constraint``) =
        match resolvedType.Resolution with
        | TypeParameter param -> constraints <- (param, ``constraint``) :: constraints
        | _ ->
            match ``constraint`` with
            | Semigroup ->
                if resolvedType.supportsConcatenation |> Option.isNone
                   && resolvedType.supportsArithmetic |> Option.isNone then
                    failwithf "%A is not a semigroup" resolvedType.Resolution
            | Iterable item ->
                match resolvedType.supportsIteration with
                | Some t when item = t -> () // TODO: Is subtype of.
                | _ -> failwithf "%A cannot iterate %A" resolvedType.Resolution item

    member context.Satisfy() =
        for param, ``constraint`` in constraints do
            let t = TypeParameter param |> context.Resolve |> ResolvedType.New

            match ``constraint`` with
            | Semigroup ->
                if t.supportsConcatenation |> Option.isNone && t.supportsArithmetic |> Option.isNone
                then failwithf "%A is not a semigroup" t.Resolution
            | Iterable expectedItem ->
                match t.supportsIteration with
                | Some actualItem -> context.Unify(actualItem, expectedItem)
                | None -> failwithf "%A cannot iterate" t.Resolution

    member internal context.Resolve typeKind =
        match typeKind with
        | TypeParameter param ->
            substitutions.TryGetValue param
            |> tryOption
            |> Option.map (fun substitutions ->
                (substitutions
                 |> List.map (fun s -> { s with Type = context.Resolve s.Type.Resolution |> ResolvedType.New })
                 |> merge)
                    .Resolution)
            |> Option.defaultValue typeKind
        | ArrayType array -> context.Resolve array.Resolution |> ResolvedType.New |> ArrayType
        | TupleType tuple ->
            tuple
            |> Seq.map (fun item -> context.Resolve item.Resolution |> ResolvedType.New)
            |> ImmutableArray.CreateRange
            |> TupleType
        | QsTypeKind.Operation ((inType, outType), info) ->
            let inType = context.Resolve inType.Resolution |> ResolvedType.New
            let outType = context.Resolve outType.Resolution |> ResolvedType.New
            QsTypeKind.Operation((inType, outType), info)
        | QsTypeKind.Function (inType, outType) ->
            let inType = context.Resolve inType.Resolution |> ResolvedType.New
            let outType = context.Resolve outType.Resolution |> ResolvedType.New
            QsTypeKind.Function(inType, outType)
        | _ -> typeKind

module InferenceContext =
    [<CompiledName "Resolver">]
    let resolver (context: InferenceContext) =
        let types =
            { new TypeTransformation() with
                member this.OnTypeParameter param = TypeParameter param |> context.Resolve
            }

        SyntaxTreeTransformation(Types = types)
