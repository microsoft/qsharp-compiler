// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing

open System
open System.Collections.Generic
open System.Collections.Immutable

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.VerificationTools
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.TextProcessing
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
    | AppliesPartial of missing: ResolvedType * result: ResolvedType
    | Callable of input: ResolvedType * output: ResolvedType
    | CanGenerateFunctors of QsFunctor Set
    | Equatable
    | Indexed of index: ResolvedType * item: ResolvedType
    | Integral
    | Iterable of item: ResolvedType
    | Numeric
    | Semigroup
    | Wrapped of item: ResolvedType

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
            | QsTypeKind.TypeParameter tp1, QsTypeKind.TypeParameter tp2 when tp1 = tp2 -> t1
            | QsTypeKind.TypeParameter tp, QsTypeKind.InvalidType when tp.Origin = parent -> t1
            | QsTypeKind.InvalidType, QsTypeKind.TypeParameter tp when tp.Origin = parent -> t2
            | QsTypeKind.TypeParameter _, _ ->
                raiseError (ErrorCode.ConstrainsTypeParameter, [ SyntaxTreeToQsharp.Default.ToCode t1 ]) (true, false)
            | _, QsTypeKind.TypeParameter _ ->
                raiseError (ErrorCode.ConstrainsTypeParameter, [ SyntaxTreeToQsharp.Default.ToCode t2 ]) (false, true)
            | _ when t1.isInvalid || t2.isInvalid -> if t1.isInvalid then t2 else t1
            | _ when t1 = t2 -> t1
            | _ -> raiseError mismatchErr (true, false) // (true, true)

        matchTypes variance (lhsType, rhsType)

    let CommonBaseType addError = commonType Covariant addError

    let printType: ResolvedType -> string = SyntaxTreeToQsharp.Default.ToCode

    let intersect left right =
        let varianceName =
            match left.Variance, right.Variance with
            | Covariant, Covariant -> "any subtype of "
            | Contravariant, Contravariant -> "any supertype of "
            | _ -> ""

        let error = ErrorCode.TypeUnificationFailed, [ printType left.Type; varianceName + printType right.Type ]

        if left.Variance <> Invariant && left.Variance = right.Variance then
            let mutable diagnostics = []

            let newType =
                commonType left.Variance (fun error range ->
                    diagnostics <- QsCompilerDiagnostic.Error error range :: diagnostics) error
                    { Name = ""; Namespace = "" } (left.Type, Range.Zero) (right.Type, Range.Zero)

            { Variance = left.Variance; Type = newType }, diagnostics
        elif left.Type = right.Type then
            { left with Variance = Invariant }, []
        elif left.Type.Resolution = InvalidType || right.Type.Resolution = InvalidType then
            { Variance = Invariant; Type = ResolvedType.New InvalidType }, []
        else
            { Variance = Invariant; Type = ResolvedType.New InvalidType },
            [ QsCompilerDiagnostic.Error error Range.Zero ]

    let isSubsetOf info1 info2 =
        info1.Characteristics.GetProperties().IsSubsetOf(info2.Characteristics.GetProperties())

    let occursCheck param (resolvedType: ResolvedType) =
        if resolvedType.Exists((=) (TypeParameter param))
        then failwithf "Occurs check: cannot construct the infinite type %A ~ %A." param resolvedType

    // TODO
    let isFresh (param: QsTypeParameter) =
        param.TypeName.StartsWith "__a" && param.TypeName.EndsWith "__"

    let missingFunctors target given =
        let mapFunctors =
            Seq.map (function
                | Adjoint -> Keywords.qsAdjointFunctor.id
                | Controlled -> Keywords.qsControlledFunctor.id)
            >> Seq.toList

        match given with
        | Some fs -> Set.difference target fs |> mapFunctors
        | None -> if Set.isEmpty target then [ "(None)" ] else mapFunctors target

open TypeInference

type InferenceContext(symbolTracker: SymbolTracker) =
    let mutable count = 0

    let mutable unbound = Set.empty

    let substitutions = Dictionary()

    let mutable constraints = []

    let bind param substitution =
        occursCheck param substitution.Type
        unbound <- Set.remove param unbound

        match substitutions.TryGetValue param |> tryOption with
        | Some v ->
            let sub, diagnostics = intersect substitution v
            substitutions.[param] <- sub
            diagnostics
        | None ->
            substitutions.[param] <- substitution
            []

    member internal context.Fresh() =
        let name = sprintf "__a%d__" count
        count <- count + 1

        let param =
            {
                Origin = symbolTracker.Parent
                TypeName = name
                Range = Null
            }

        unbound <- Set.add param unbound
        TypeParameter param |> ResolvedType.New

    member internal context.Unify(left: ResolvedType, right: ResolvedType) =
        let left = context.Resolve left.Resolution |> ResolvedType.New
        let right = context.Resolve right.Resolution |> ResolvedType.New

        match left.Resolution, right.Resolution with
        | _ when left = right -> []
        | TypeParameter param, _ when isFresh param -> bind param { Variance = Contravariant; Type = right }
        | _, TypeParameter param when isFresh param -> bind param { Variance = Covariant; Type = left }
        | ArrayType item1, ArrayType item2 -> context.Unify(item1, item2) // TODO: Invariant.
        | TupleType items1, TupleType items2 when items1.Length = items2.Length ->
            Seq.zip items1 items2 |> Seq.collect context.Unify |> Seq.toList
        | QsTypeKind.Operation ((in1, out1), info1), QsTypeKind.Operation ((in2, out2), info2) when isSubsetOf
                                                                                                        info2
                                                                                                        info1 ->
            context.Unify(in2, in1) @ context.Unify(out1, out2)
        | QsTypeKind.Function (in1, out1), QsTypeKind.Function (in2, out2) ->
            context.Unify(in2, in1) @ context.Unify(out1, out2)
        | QsTypeKind.Operation ((in1, out1), _), QsTypeKind.Function (in2, out2)
        | QsTypeKind.Function (in1, out1), QsTypeKind.Operation ((in2, out2), _) ->
            // Function and operation types aren't compatible, but we can still try to unify their input and output
            // types for more accurate error messages.
            let error = ErrorCode.TypeUnificationFailed, [ printType left; "any subtype of " + printType right ]

            [ QsCompilerDiagnostic.Error error Range.Zero ]
            @ context.Unify(in2, in1) @ context.Unify(out1, out2)
        | InvalidType, _
        | MissingType, _
        | _, InvalidType
        | _, MissingType -> []
        | _ ->
            let error = ErrorCode.TypeUnificationFailed, [ printType left; "any subtype of " + printType right ]
            [ QsCompilerDiagnostic.Error error Range.Zero ]

    member private context.CheckConstraint(typeConstraint, resolvedType: ResolvedType) =
        match typeConstraint with
        | _ when resolvedType.Resolution = InvalidType -> []
        | AppliesPartial (missing, result) ->
            match resolvedType.Resolution with
            | QsTypeKind.Function (_, output) ->
                context.Unify(QsTypeKind.Function(missing, output) |> ResolvedType.New, result)
            | QsTypeKind.Operation ((_, output), info) ->
                context.Unify(QsTypeKind.Operation((missing, output), info) |> ResolvedType.New, result)
            | _ ->
                let error = ErrorCode.ConstraintNotSatisfied, [ printType resolvedType; "AppliesPartial" ]
                [ QsCompilerDiagnostic.Error error Range.Zero ]
        | Callable (input, output) ->
            match resolvedType.Resolution with
            | QsTypeKind.Operation _ ->
                let operationType = QsTypeKind.Operation((input, output), CallableInformation.NoInformation)
                context.Unify(resolvedType, ResolvedType.New operationType)
            | QsTypeKind.Function _ ->
                context.Unify(resolvedType, QsTypeKind.Function(input, output) |> ResolvedType.New)
            | _ ->
                let error = ErrorCode.ConstraintNotSatisfied, [ printType resolvedType; "Callable" ]
                [ QsCompilerDiagnostic.Error error Range.Zero ]
        | CanGenerateFunctors functors ->
            match resolvedType.Resolution with
            | QsTypeKind.Operation (_, info) ->
                let supported = info.Characteristics.SupportedFunctors.ValueOr ImmutableHashSet.Empty

                match Set.ofSeq supported |> Some |> missingFunctors functors with
                | _ when info.Characteristics.AreInvalid -> []
                | [] -> []
                | missing ->
                    let error = ErrorCode.MissingFunctorForAutoGeneration, [ String.Join(", ", missing) ]
                    [ QsCompilerDiagnostic.Error error Range.Zero ]
            | _ -> []
        | Equatable ->
            if resolvedType.supportsEqualityComparison |> Option.isSome
            then []
            else failwithf "Equatable constraint not satisfied for %A." resolvedType
        | Indexed (index, item) ->
            match resolvedType.Resolution, context.Resolve index.Resolution with
            | ArrayType actualItem, Int -> context.Unify(actualItem, item)
            | ArrayType _, Range -> context.Unify(resolvedType, item)
            | _ ->
                let error = ErrorCode.ConstraintNotSatisfied, [ printType resolvedType; "Indexed" ]
                [ QsCompilerDiagnostic.Error error Range.Zero ]
        | Integral ->
            if resolvedType.Resolution = Int || resolvedType.Resolution = BigInt
            then []
            else failwithf "Integral constraint not satisfied for %A." resolvedType
        | Iterable item ->
            match resolvedType.supportsIteration with
            | Some actualItem -> context.Unify(actualItem, item)
            | None -> failwithf "Iterable %A constraint not satisfied for %A." item resolvedType
        | Numeric ->
            if resolvedType.supportsArithmetic |> Option.isSome
            then []
            else failwithf "Numeric constraint not satisfied for %A." resolvedType
        | Semigroup ->
            if resolvedType.supportsConcatenation |> Option.isSome
               || resolvedType.supportsArithmetic |> Option.isSome then
                []
            else
                failwithf "Semigroup constraint not satisfied for %A." resolvedType
        | Wrapped item ->
            match resolvedType.Resolution with
            | UserDefinedType udt ->
                let actualItem = symbolTracker.GetUnderlyingType (fun _ -> ()) udt
                context.Unify(actualItem, item)
            | _ -> failwithf "Wrapped %A constraint not satisfied for %A." item resolvedType

    member internal context.Constrain(resolvedType: ResolvedType, typeConstraint) =
        let resolvedType = context.Resolve resolvedType.Resolution |> ResolvedType.New

        match resolvedType.Resolution with
        | TypeParameter param ->
            constraints <- (param, typeConstraint) :: constraints
            []
        | _ -> context.CheckConstraint(typeConstraint, resolvedType)

    member context.Satisfy() =
        [
            for param, typeConstraint in constraints do
                let typeKind = TypeParameter param |> context.Resolve
                yield! context.CheckConstraint(typeConstraint, ResolvedType.New typeKind)

            for param in unbound ->
                QsCompilerDiagnostic.Error (ErrorCode.AmbiguousTypeVariable, [ param.TypeName ]) Range.Zero
        ]

    member internal context.Resolve typeKind =
        match typeKind with
        | TypeParameter param ->
            substitutions.TryGetValue param
            |> tryOption
            |> Option.map (fun substitution -> context.Resolve substitution.Type.Resolution)
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
                member this.OnTypeParameter param =
                    match TypeParameter param |> context.Resolve with
                    | TypeParameter param' when isFresh param' -> InvalidType
                    | resolvedType -> resolvedType
            }

        SyntaxTreeTransformation(Types = types)
