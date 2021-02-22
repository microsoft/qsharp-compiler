// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing

open System.Collections.Generic
open System.Collections.Immutable

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.VerificationTools
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open Microsoft.Quantum.QsCompiler.Utils

/// Describes the direction of the subtyping relationship between components of compound types.
type internal Variance =
    | Covariant
    | Contravariant
    | Invariant

type internal Substitution = private { Variance: Variance; Type: ResolvedType }

type internal Constraint =
    | Semigroup
    | Equatable
    | Numeric
    | Iterable of item: ResolvedType

module internal TypeInference =
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
            ResolvedType.New InvalidType

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
                    let characteristics =
                        if s1.Characteristics.AreInvalid then s2.Characteristics else s1.Characteristics

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
                raiseError (ErrorCode.ConstrainsTypeParameter, [ SyntaxTreeToQsharp.Default.ToCode t1 ]) (true, false)
            | _, QsTypeKind.TypeParameter _ ->
                raiseError (ErrorCode.ConstrainsTypeParameter, [ SyntaxTreeToQsharp.Default.ToCode t2 ]) (false, true)
            | _ when t1.isInvalid || t2.isInvalid -> if t1.isInvalid then t2 else t1
            | _ when t1 = t2 -> t1
            | _ -> raiseError mismatchErr (true, true)

        matchTypes variance (lhsType, rhsType)

    let CommonBaseType addError = commonType Covariant addError

    let printType: ResolvedType -> string = SyntaxTreeToQsharp.Default.ToCode

    let merge substitutions =
        let mutable diagnostics = []

        let common variance left right =
            let varianceName =
                match variance with
                | Covariant -> "subtype"
                | Contravariant -> "supertype"
                | Invariant -> "type"

            commonType variance (fun error range ->
                diagnostics <- QsCompilerDiagnostic.Error error range :: diagnostics)
                (ErrorCode.NoIntersectingType, [ varianceName; printType left; printType right ])
                { Name = ""; Namespace = "" } (left, Range.Zero) (right, Range.Zero)

        let substitute variance typ =
            Option.map (common variance typ) >> Option.defaultValue typ >> Some

        let folder (lower, upper) substitution =
            match substitution.Variance with
            | Covariant -> lower, upper |> substitute Covariant substitution.Type
            | Contravariant -> lower |> substitute Contravariant substitution.Type, upper
            | Invariant ->
                lower |> substitute Invariant substitution.Type, upper |> substitute Invariant substitution.Type

        match substitutions |> List.fold folder (None, None) with
        | Some lower, Some upper ->
            if lower.Resolution = InvalidType || upper.Resolution = InvalidType then
                ResolvedType.New InvalidType, diagnostics
            elif lower = upper then
                lower, diagnostics
            else
                let error = ErrorCode.NoIntersectingType, [ "type"; printType lower; printType upper ]
                ResolvedType.New InvalidType, [ QsCompilerDiagnostic.Error error Range.Zero ]
        | Some t, None
        | None, Some t -> t, diagnostics
        | None, None -> failwith "Merge failed: empty substitution."

    let isSubsetOf info1 info2 =
        info1.Characteristics.GetProperties().IsSubsetOf(info2.Characteristics.GetProperties())

    let occursCheck param (resolvedType: ResolvedType) =
        if resolvedType.Exists((=) (TypeParameter param))
        then failwithf "Occurs check: cannot construct the infinite type %A ~ %A." param resolvedType

open TypeInference

type InferenceContext(origin) =
    let mutable count = 0

    let substitutions = Dictionary()

    let mutable constraints = []

    let bind param substitution =
        occursCheck param substitution.Type

        match substitutions.TryGetValue param |> tryOption with
        | Some v -> substitutions.[param] <- substitution :: v
        | None -> substitutions.[param] <- [ substitution ]

    member internal context.Fresh() =
        let name = sprintf "__a%d__" count
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
        | TypeParameter param1, TypeParameter param2 when param1 = param2 -> []
        | TypeParameter param, _ ->
            bind param { Variance = Contravariant; Type = right }
            []
        | _, TypeParameter param ->
            bind param { Variance = Covariant; Type = left }
            []
        | ArrayType item1, ArrayType item2 -> context.Unify(item1, item2) // TODO: Invariant.
        | TupleType items1, TupleType items2 -> Seq.zip items1 items2 |> Seq.collect context.Unify |> Seq.toList
        | QsTypeKind.Operation ((in1, out1), info1), QsTypeKind.Operation ((in2, out2), info2) when isSubsetOf
                                                                                                        info1
                                                                                                        info2 ->
            // TODO: Variance.
            [ in1, in2; out1, out2 ] |> List.collect context.Unify
        | QsTypeKind.Function (in1, out1), QsTypeKind.Function (in2, out2) ->
            // TODO: Variance.
            [ in1, in2; out1, out2 ] |> List.collect context.Unify
        | InvalidType, _
        | _, InvalidType -> []
        | _ when left = right -> []
        | _ ->
            let error = ErrorCode.TypeUnificationFailed, [ printType left; printType right ]
            [ QsCompilerDiagnostic.Error error Range.Zero ]

    member private context.CheckConstraint(typeConstraint, resolvedType: ResolvedType) =
        if resolvedType.Resolution = InvalidType then
            []
        else
            match typeConstraint with
            | Semigroup ->
                if resolvedType.supportsConcatenation |> Option.isNone
                   && resolvedType.supportsArithmetic |> Option.isNone then
                    failwithf "Semigroup constraint not satisfied for %A." resolvedType
                else
                    []
            | Equatable ->
                if resolvedType.supportsEqualityComparison |> Option.isNone
                then failwithf "Equatable constraint not satisfied for %A." resolvedType
                else []
            | Numeric ->
                if resolvedType.supportsArithmetic |> Option.isNone
                then failwithf "Numeric constraint not satisfied for %A." resolvedType
                else []
            | Iterable item ->
                match resolvedType.supportsIteration with
                | Some actualItem -> context.Unify(actualItem, item)
                | None -> failwithf "Iterable %A constraint not satisfied for %A." item resolvedType

    member internal context.Constrain(resolvedType: ResolvedType, typeConstraint) =
        match resolvedType.Resolution with
        | TypeParameter param ->
            constraints <- (param, typeConstraint) :: constraints
            []
        | _ -> context.CheckConstraint(typeConstraint, resolvedType)

    member context.Satisfy() =
        [
            for param, typeConstraint in constraints do
                let typeKind, diagnostics = TypeParameter param |> context.Resolve
                yield! diagnostics
                yield! context.CheckConstraint(typeConstraint, ResolvedType.New typeKind)
        ]

    member internal context.Resolve typeKind: QsTypeKind<_, _, _, _> * QsCompilerDiagnostic list =
        match typeKind with
        | TypeParameter param ->
            substitutions.TryGetValue param
            |> tryOption
            |> Option.map (fun substitutions ->
                let results = substitutions |> List.map (fun s -> s, context.Resolve s.Type.Resolution)
                let resolvedDiagnostics = results |> List.collect (fun (_, (_, diagnostic)) -> diagnostic)

                let merged, mergedDiagnostics =
                    results
                    |> List.map (fun (s, (resolvedType, _)) -> { s with Type = ResolvedType.New resolvedType })
                    |> merge

                merged.Resolution, resolvedDiagnostics @ mergedDiagnostics)
            |> Option.defaultValue (typeKind, [])
        | ArrayType array ->
            let resolvedType, diagnostics = context.Resolve array.Resolution
            ResolvedType.New resolvedType |> ArrayType, diagnostics
        | TupleType tuple ->
            let results = tuple |> Seq.map (fun item -> context.Resolve item.Resolution)
            let types = results |> Seq.map (fst >> ResolvedType.New)
            let diagnostics = results |> Seq.collect snd
            ImmutableArray.CreateRange types |> TupleType, diagnostics |> Seq.toList
        | QsTypeKind.Operation ((inType, outType), info) ->
            let inType, inDiagnostics = context.Resolve inType.Resolution
            let outType, outDiagnostics = context.Resolve outType.Resolution

            QsTypeKind.Operation((ResolvedType.New inType, ResolvedType.New outType), info),
            inDiagnostics @ outDiagnostics
        | QsTypeKind.Function (inType, outType) ->
            let inType, inDiagnostics = context.Resolve inType.Resolution
            let outType, outDiagnostics = context.Resolve outType.Resolution
            QsTypeKind.Function(ResolvedType.New inType, ResolvedType.New outType), inDiagnostics @ outDiagnostics
        | _ -> typeKind, []

module InferenceContext =
    [<CompiledName "Resolver">]
    let resolver (context: InferenceContext) =
        let diagnostics = ResizeArray()

        let types =
            { new TypeTransformation() with
                member this.OnTypeParameter param =
                    let typeKind, resolvedDiagnostics = TypeParameter param |> context.Resolve
                    diagnostics.AddRange resolvedDiagnostics
                    typeKind
            }

        SyntaxTreeTransformation(Types = types), diagnostics
