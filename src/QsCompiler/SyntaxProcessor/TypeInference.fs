// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing

open Microsoft.Quantum.QsCompiler
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
open System.Collections.Generic
open System.Collections.Immutable

type private Ordering =
    | Subtype
    | Equal
    | Supertype

module private Ordering =
    let not =
        function
        | Subtype -> Supertype
        | Equal -> Equal
        | Supertype -> Subtype

type internal Constraint =
    | Adjointable
    | Callable of input: ResolvedType * output: ResolvedType
    | CanGenerateFunctors of functors: QsFunctor Set
    | Controllable of controlled: ResolvedType
    | Equatable
    | HasPartialApplication of missing: ResolvedType * result: ResolvedType
    | Indexed of index: ResolvedType * item: ResolvedType
    | Integral
    | Iterable of item: ResolvedType
    | Numeric
    | Semigroup
    | Wrapped of item: ResolvedType

module private Inference =
    let printType: ResolvedType -> _ = SyntaxTreeToQsharp.Default.ToCode

    let private combineCallableInfo ordering info1 info2 =
        let isInvalid info = info.Characteristics.AreInvalid
        let inferredNone = InferredCallableInformation.NoInformation
        let inferredCommon = InferredCallableInformation.Common
        let properties1 = lazy (info1.Characteristics.GetProperties())
        let properties2 = lazy (info2.Characteristics.GetProperties())

        match ordering with
        | Subtype ->
            let union = Union(info1.Characteristics, info2.Characteristics)
            CallableInformation.New(ResolvedCharacteristics.New union, inferredNone), []
        | Equal ->
            if isInvalid info1 || isInvalid info2 || properties1.Value.SetEquals properties2.Value then
                let characteristics = if isInvalid info1 then info2.Characteristics else info1.Characteristics
                let inferred = [ info1.InferredInformation; info2.InferredInformation ] |> inferredCommon
                CallableInformation.New(characteristics, inferred), []
            else
                let error = ErrorCode.ArgumentMismatchInBinaryOp, [ sprintf "%A" info1; sprintf "%A" info2 ]

                CallableInformation.New(ResolvedCharacteristics.New InvalidSetExpr, inferredNone),
                [ QsCompilerDiagnostic.Error error Range.Zero ]
        | Supertype -> [ info1; info2 ] |> CallableInformation.Common, []

    let rec combine ordering (lhs: ResolvedType) (rhs: ResolvedType) =
        match lhs.Resolution, rhs.Resolution with
        | ArrayType item1, ArrayType item2 ->
            let combinedType, diagnostics = combine Equal item1 item2
            ArrayType combinedType |> ResolvedType.New, diagnostics
        | TupleType items1, TupleType items2 when items1.Length = items2.Length ->
            let combinedTypes, diagnostics = Seq.map2 (combine ordering) items1 items2 |> Seq.toList |> List.unzip
            ImmutableArray.CreateRange combinedTypes |> TupleType |> ResolvedType.New, List.concat diagnostics
        | QsTypeKind.Operation ((in1, out1), info1), QsTypeKind.Operation ((in2, out2), info2) ->
            let input, inDiagnostics = combine (Ordering.not ordering) in1 in2
            let output, outDiagnostics = combine ordering out1 out2
            let info, infoDiagnostics = combineCallableInfo ordering info1 info2

            QsTypeKind.Operation((input, output), info) |> ResolvedType.New,
            inDiagnostics @ outDiagnostics @ infoDiagnostics
        | QsTypeKind.Function (in1, out1), QsTypeKind.Function (in2, out2) ->
            let input, inDiagnostics = combine (Ordering.not ordering) in1 in2
            let output, outDiagnostics = combine ordering out1 out2
            QsTypeKind.Function(input, output) |> ResolvedType.New, inDiagnostics @ outDiagnostics
        | InvalidType, _
        | _, InvalidType -> ResolvedType.New InvalidType, []
        | _ when lhs = rhs -> lhs, []
        | _ ->
            let error = ErrorCode.ArgumentMismatchInBinaryOp, [ printType lhs; printType rhs ]
            ResolvedType.New InvalidType, [ QsCompilerDiagnostic.Error error Range.Zero ]

    let isSubsetOf info1 info2 =
        info1.Characteristics.GetProperties().IsSubsetOf(info2.Characteristics.GetProperties())

    let hasFunctor functor info =
        info.Characteristics.SupportedFunctors
        |> QsNullable.defaultValue ImmutableHashSet.Empty
        |> fun functors -> functors.Contains functor

    let occursCheck param (resolvedType: ResolvedType) =
        if TypeParameter param |> (=) |> resolvedType.Exists
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

open Inference

type InferenceContext(symbolTracker: SymbolTracker) =
    let mutable count = 0

    let unbound = HashSet()

    let substitutions = Dictionary()

    let constraints = Dictionary()

    let bind param substitution =
        occursCheck param substitution
        unbound.Remove param |> ignore
        substitutions.Add(param, substitution)

    member internal context.Fresh() =
        let name = sprintf "__a%d__" count
        count <- count + 1

        let param =
            {
                Origin = symbolTracker.Parent
                TypeName = name
                Range = Null
            }

        unbound.Add param |> ignore
        TypeParameter param |> ResolvedType.New

    member internal context.Unify(expected: ResolvedType, actual) =
        context.UnifyRelation(expected, Supertype, actual)

    member private context.UnifyRelation(lhs: ResolvedType, compares, rhs: ResolvedType) =
        let expected = context.Resolve lhs.Resolution |> ResolvedType.New
        let actual = context.Resolve rhs.Resolution |> ResolvedType.New

        let unificationError =
            lazy
                (QsCompilerDiagnostic.Error
                    (ErrorCode.TypeUnificationFailed,
                     [
                         printType expected
                         if compares = Equal then printType actual else "any subtype of " + printType actual
                     ])
                     Range.Zero)

        match expected.Resolution, actual.Resolution with
        | _ when compares = Supertype -> context.UnifyRelation(rhs, Subtype, lhs)
        | _ when expected = actual -> []
        | TypeParameter param, _ when isFresh param ->
            bind param actual
            context.PopConstraints(param, actual)
        | _, TypeParameter param when isFresh param ->
            bind param expected
            context.PopConstraints(param, expected)
        | ArrayType item1, ArrayType item2 -> context.UnifyRelation(item1, Equal, item2)
        | TupleType items1, TupleType items2 when items1.Length = items2.Length ->
            Seq.zip items1 items2
            |> Seq.collect (fun (item1, item2) -> context.UnifyRelation(item1, compares, item2))
            |> Seq.toList
        | QsTypeKind.Operation ((in1, out1), info1), QsTypeKind.Operation ((in2, out2), info2) ->
            let sameChars =
                info1.Characteristics.AreInvalid
                || info2.Characteristics.AreInvalid
                || info1.Characteristics.GetProperties().SetEquals(info2.Characteristics.GetProperties())

            let errors =
                [
                    if compares = Equal && not sameChars || compares <> Equal && not (isSubsetOf info2 info1)
                    then unificationError.Value
                ]

            errors @ context.UnifyRelation(in2, compares, in1) @ context.UnifyRelation(out1, compares, out2)
        | QsTypeKind.Function (in1, out1), QsTypeKind.Function (in2, out2) ->
            context.UnifyRelation(in2, compares, in1) @ context.UnifyRelation(out1, compares, out2)
        | QsTypeKind.Operation ((in1, out1), _), QsTypeKind.Function (in2, out2)
        | QsTypeKind.Function (in1, out1), QsTypeKind.Operation ((in2, out2), _) ->
            // Function and operation types aren't compatible, but we can still try to unify their input and output
            // types for more accurate error messages.
            unificationError.Value :: context.UnifyRelation(in2, compares, in1)
            @ context.UnifyRelation(out1, compares, out2)
        | InvalidType, _
        | MissingType, _
        | _, InvalidType
        | _, MissingType -> []
        | _ -> [ unificationError.Value ]

    member internal context.Intersect(left: ResolvedType, right) =
        context.UnifyRelation(left, Equal, right) |> ignore
        let left = context.Resolve left.Resolution |> ResolvedType.New
        let right = context.Resolve right.Resolution |> ResolvedType.New
        combine Supertype left right

    member private context.CheckConstraint(typeConstraint, resolvedType: ResolvedType) =
        match typeConstraint with
        | _ when resolvedType.Resolution = InvalidType -> []
        | Adjointable ->
            match resolvedType.Resolution with
            | QsTypeKind.Operation (_, info) when hasFunctor Adjoint info -> []
            | _ ->
                [
                    QsCompilerDiagnostic.Error (ErrorCode.InvalidAdjointApplication, []) Range.Zero
                ]
        | Callable (input, output) ->
            match resolvedType.Resolution with
            | QsTypeKind.Operation _ ->
                let operationType = QsTypeKind.Operation((input, output), CallableInformation.NoInformation)
                context.Unify(ResolvedType.New operationType, resolvedType)
            | QsTypeKind.Function _ ->
                context.Unify(QsTypeKind.Function(input, output) |> ResolvedType.New, resolvedType)
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
                    let error = ErrorCode.MissingFunctorForAutoGeneration, [ String.concat "," missing ]
                    [ QsCompilerDiagnostic.Error error Range.Zero ]
            | _ -> []
        | Controllable controlled ->
            let error = QsCompilerDiagnostic.Error (ErrorCode.InvalidControlledApplication, []) Range.Zero

            match resolvedType.Resolution with
            | QsTypeKind.Operation ((input, output), info) ->
                let actualControlled =
                    QsTypeKind.Operation((SyntaxGenerator.AddControlQubits input, output), info) |> ResolvedType.New

                [
                    if info |> hasFunctor Controlled |> not then error
                    yield! context.Unify(controlled, actualControlled)
                ]
            | QsTypeKind.Function (input, output) ->
                let actualControlled =
                    QsTypeKind.Operation((SyntaxGenerator.AddControlQubits input, output), CallableInformation.Invalid)
                    |> ResolvedType.New

                error :: context.Unify(controlled, actualControlled)
            | _ -> [ error ]
        | Equatable ->
            if Option.isSome resolvedType.supportsEqualityComparison then
                []
            else
                let error = ErrorCode.InvalidTypeInEqualityComparison, [ printType resolvedType ]
                [ QsCompilerDiagnostic.Error error Range.Zero ]
        | HasPartialApplication (missing, result) ->
            match resolvedType.Resolution with
            | QsTypeKind.Function (_, output) ->
                context.Unify(result, QsTypeKind.Function(missing, output) |> ResolvedType.New)
            | QsTypeKind.Operation ((_, output), info) ->
                context.Unify(result, QsTypeKind.Operation((missing, output), info) |> ResolvedType.New)
            | _ ->
                let error = ErrorCode.ConstraintNotSatisfied, [ printType resolvedType; "HasPartialApplication" ]
                [ QsCompilerDiagnostic.Error error Range.Zero ]
        | Indexed (index, item) ->
            match resolvedType.Resolution, context.Resolve index.Resolution with
            | ArrayType actualItem, Int -> context.Unify(item, actualItem)
            | ArrayType _, Range -> context.Unify(item, resolvedType)
            | _ ->
                let error = ErrorCode.ConstraintNotSatisfied, [ printType resolvedType; "Indexed" ]
                [ QsCompilerDiagnostic.Error error Range.Zero ]
        | Integral ->
            if resolvedType.Resolution = Int || resolvedType.Resolution = BigInt
            then []
            else failwithf "Integral constraint not satisfied for %A." resolvedType
        | Iterable item ->
            match resolvedType.supportsIteration with
            | Some actualItem -> context.Unify(item, actualItem)
            | None -> failwithf "Iterable %A constraint not satisfied for %A." item resolvedType
        | Numeric ->
            if Option.isSome resolvedType.supportsArithmetic
            then []
            else failwithf "Numeric constraint not satisfied for %A." resolvedType
        | Semigroup ->
            if Option.isSome resolvedType.supportsConcatenation || Option.isSome resolvedType.supportsArithmetic
            then []
            else failwithf "Semigroup constraint not satisfied for %A." resolvedType
        | Wrapped item ->
            match resolvedType.Resolution with
            | UserDefinedType udt ->
                let actualItem = symbolTracker.GetUnderlyingType (fun _ -> ()) udt
                context.Unify(item, actualItem)
            | _ -> failwithf "Wrapped %A constraint not satisfied for %A." item resolvedType

    member internal context.Constrain(resolvedType: ResolvedType, typeConstraint) =
        let resolvedType = context.Resolve resolvedType.Resolution |> ResolvedType.New

        match resolvedType.Resolution with
        | TypeParameter param when isFresh param ->
            match constraints.TryGetValue param |> tryOption with
            | Some xs -> constraints.[param] <- typeConstraint :: xs
            | None -> constraints.Add(param, [ typeConstraint ])

            []
        | _ -> context.CheckConstraint(typeConstraint, resolvedType)

    member private context.PopConstraints(param, resolvedType) =
        let diagnostics =
            constraints.TryGetValue param
            |> tryOption
            |> Option.defaultValue []
            |> List.collect (fun typeConstraint -> context.CheckConstraint(typeConstraint, resolvedType))

        constraints.Remove param |> ignore
        diagnostics

    member internal context.Ambiguous =
        [
            for param in unbound ->
                QsCompilerDiagnostic.Error (ErrorCode.AmbiguousTypeVariable, [ param.TypeName ]) Range.Zero
        ]

    member internal context.Resolve typeKind =
        match typeKind with
        | TypeParameter param ->
            substitutions.TryGetValue param
            |> tryOption
            |> Option.map (fun substitution -> context.Resolve substitution.Resolution)
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

        SyntaxTreeTransformation(Types = types), context.Ambiguous
