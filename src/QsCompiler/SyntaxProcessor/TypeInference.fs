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

type private Variable =
    {
        Substitution: ResolvedType option
        Constraints: Constraint list
        Source: Range
    }

module private Variable =
    let constrain typeConstraint variable =
        { variable with Constraints = typeConstraint :: variable.Constraints }

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

module private Inference =
    let characteristicsEqual info1 info2 =
        let chars1 = info1.Characteristics
        let chars2 = info2.Characteristics
        chars1.AreInvalid || chars2.AreInvalid || chars1.GetProperties().SetEquals(chars2.GetProperties())

    let isSubset info1 info2 =
        info1.Characteristics.GetProperties().IsSubsetOf(info2.Characteristics.GetProperties())

    let hasFunctor functor info =
        info.Characteristics.SupportedFunctors
        |> QsNullable.defaultValue ImmutableHashSet.Empty
        |> fun functors -> functors.Contains functor

    let rec (=~) (lhs: ResolvedType) (rhs: ResolvedType) =
        match lhs.Resolution, rhs.Resolution with
        | UserDefinedType udt1, UserDefinedType udt2 -> udt1.Namespace = udt2.Namespace && udt1.Name = udt2.Name
        | TypeParameter param1, TypeParameter param2 ->
            param1.Origin = param2.Origin && param1.TypeName = param2.TypeName
        | ArrayType item1, ArrayType item2 -> item1 =~ item2
        | TupleType items1, TupleType items2 -> items1.Length = items2.Length && Seq.forall2 (=~) items1 items2
        | QsTypeKind.Operation ((in1, out1), info1), QsTypeKind.Operation ((in2, out2), info2) ->
            in1 =~ in2 && out1 =~ out2 && characteristicsEqual info1 info2
        | QsTypeKind.Function (in1, out1), QsTypeKind.Function (in2, out2) -> in1 =~ in2 && out1 =~ out2
        | _ -> lhs = rhs

    let showType: ResolvedType -> _ = SyntaxTreeToQsharp.Default.ToCode

    let showFunctor =
        function
        | Adjoint -> Keywords.qsAdjointFunctor.id
        | Controlled -> Keywords.qsControlledFunctor.id

    let private combineCallableInfo ordering info1 info2 =
        match ordering with
        | Subtype ->
            let union = Union(info1.Characteristics, info2.Characteristics)
            CallableInformation.New(ResolvedCharacteristics.New union, InferredCallableInformation.NoInformation), []
        | Equal ->
            if characteristicsEqual info1 info2 then
                let characteristics =
                    if info1.Characteristics.AreInvalid then info2.Characteristics else info1.Characteristics

                let inferred =
                    [ info1.InferredInformation; info2.InferredInformation ] |> InferredCallableInformation.Common

                CallableInformation.New(characteristics, inferred), []
            else
                let error = ErrorCode.ArgumentMismatchInBinaryOp, [ sprintf "%A" info1; sprintf "%A" info2 ]

                CallableInformation.New
                    (ResolvedCharacteristics.New InvalidSetExpr, InferredCallableInformation.NoInformation),
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
        | _ when lhs =~ rhs -> lhs, []
        | _ ->
            let error = ErrorCode.ArgumentMismatchInBinaryOp, [ showType lhs; showType rhs ]
            ResolvedType.New InvalidType, [ QsCompilerDiagnostic.Error error Range.Zero ]

    let occursCheck param (resolvedType: ResolvedType) =
        let param = TypeParameter param

        if param <> resolvedType.Resolution && resolvedType.Exists((=) param)
        then failwithf
                 "Occurs check failed on types %s and %s."
                 (ResolvedType.New param |> showType)
                 (showType resolvedType)

    let letters =
        Seq.initInfinite (fun i -> i + 1)
        |> Seq.collect (fun length ->
            seq { 'a' .. 'z' }
            |> Seq.map string
            |> Seq.replicate length
            |> Seq.reduce (fun xs ys -> Seq.allPairs xs ys |> Seq.map (fun (x, y) -> x + y)))
        |> Seq.cache

open Inference

type InferenceContext(symbolTracker: SymbolTracker) =
    let variables = Dictionary()

    let mutable statementPosition = Position.Zero

    let bind param substitution =
        occursCheck param substitution
        let variable = variables.[param]

        match variable.Substitution with
        | Some substitution' when substitution <> substitution' ->
            failwith "The type parameter is already bound to a different type."
        | _ -> variables.[param] <- { variable with Substitution = Some substitution }

    member context.AmbiguousDiagnostics =
        [
            for variable in variables do
                if Option.isNone variable.Value.Substitution then
                    QsCompilerDiagnostic.Error
                        (ErrorCode.AmbiguousTypeVariable, [ TypeParameter variable.Key |> ResolvedType.New |> showType ])
                        variable.Value.Source
        ]

    member context.SetStatementPosition position = statementPosition <- position

    member internal context.Fresh(source: Range) =
        let name = letters |> Seq.item variables.Count

        let param =
            Seq.initInfinite (fun i -> if i = 0 then name else name + string (i - 1))
            |> Seq.map (fun name -> QsTypeParameter.New(symbolTracker.Parent, name, Null))
            |> Seq.skipWhile (fun param ->
                variables.ContainsKey param || symbolTracker.DefinedTypeParameters.Contains param.TypeName)
            |> Seq.head

        let variable =
            {
                Substitution = None
                Constraints = []
                Source = statementPosition + source
            }

        variables.Add(param, variable)
        TypeParameter param |> ResolvedType.New

    member internal context.Unify(expected: ResolvedType, actual: ResolvedType) =
        context.UnifyByOrdering(context.Resolve expected, Supertype, context.Resolve actual)

    member internal context.Intersect(left: ResolvedType, right: ResolvedType) =
        context.UnifyByOrdering(context.Resolve left, Equal, context.Resolve right) |> ignore
        combine Supertype (context.Resolve left) (context.Resolve right)

    member internal context.Constrain(resolvedType: ResolvedType, typeConstraint) =
        let resolvedType = context.Resolve resolvedType

        match resolvedType.Resolution with
        | TypeParameter param ->
            match variables.TryGetValue param |> tryOption with
            | Some variable ->
                variables.[param] <- variable |> Variable.constrain typeConstraint
                []
            | None -> context.ApplyConstraint(typeConstraint, resolvedType)
        | _ -> context.ApplyConstraint(typeConstraint, resolvedType)

    member internal context.Resolve(resolvedType: ResolvedType) =
        match resolvedType.Resolution with
        | TypeParameter param ->
            variables.TryGetValue param
            |> tryOption
            |> Option.bind (fun variable -> variable.Substitution)
            |> Option.map context.Resolve
            |> Option.defaultValue resolvedType
        | ArrayType array -> context.Resolve array |> ArrayType |> ResolvedType.New
        | TupleType tuple ->
            tuple |> Seq.map context.Resolve |> ImmutableArray.CreateRange |> TupleType |> ResolvedType.New
        | QsTypeKind.Operation ((inType, outType), info) ->
            QsTypeKind.Operation((context.Resolve inType, context.Resolve outType), info) |> ResolvedType.New
        | QsTypeKind.Function (inType, outType) ->
            QsTypeKind.Function(context.Resolve inType, context.Resolve outType) |> ResolvedType.New
        | _ -> resolvedType

    member private context.UnifyByOrdering(expected: ResolvedType, ordering, actual: ResolvedType) =
        let error =
            QsCompilerDiagnostic.Error
                (ErrorCode.TypeUnificationFailed, [ showType expected; showType actual ])
                Range.Zero

        match expected.Resolution, actual.Resolution with
        | _ when ordering = Subtype -> context.UnifyByOrdering(expected, Supertype, actual)
        | _ when expected =~ actual -> []
        | TypeParameter param, _ when variables.ContainsKey param ->
            bind param actual
            context.ApplyConstraints(param, actual)
        | _, TypeParameter param when variables.ContainsKey param ->
            bind param expected
            context.ApplyConstraints(param, expected)
        | ArrayType item1, ArrayType item2 -> context.UnifyByOrdering(item1, Equal, item2)
        | TupleType items1, TupleType items2 when items1.Length = items2.Length ->
            Seq.zip items1 items2
            |> Seq.collect (fun (item1, item2) ->
                context.UnifyByOrdering(context.Resolve item1, ordering, context.Resolve item2))
            |> Seq.toList
        | QsTypeKind.Operation ((in1, out1), info1), QsTypeKind.Operation ((in2, out2), info2) ->
            let errors =
                if ordering = Equal && not (characteristicsEqual info1 info2) || not (isSubset info1 info2)
                then [ error ]
                else []

            errors
            @ context.UnifyByOrdering(in2, ordering, in1)
              @ context.UnifyByOrdering(context.Resolve out1, ordering, context.Resolve out2)
        | QsTypeKind.Function (in1, out1), QsTypeKind.Function (in2, out2) ->
            context.UnifyByOrdering(in2, ordering, in1)
            @ context.UnifyByOrdering(context.Resolve out1, ordering, context.Resolve out2)
        | QsTypeKind.Operation ((in1, out1), _), QsTypeKind.Function (in2, out2)
        | QsTypeKind.Function (in1, out1), QsTypeKind.Operation ((in2, out2), _) ->
            error :: context.UnifyByOrdering(in2, ordering, in1)
            @ context.UnifyByOrdering(context.Resolve out1, ordering, context.Resolve out2)
        | InvalidType, _
        | MissingType, _
        | _, InvalidType
        | _, MissingType -> []
        | _ -> [ error ]

    member private context.ApplyConstraint(typeConstraint, resolvedType: ResolvedType) =
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
                let error = ErrorCode.ConstraintNotSatisfied, [ showType resolvedType; "Callable" ]
                [ QsCompilerDiagnostic.Error error Range.Zero ]
        | CanGenerateFunctors functors ->
            match resolvedType.Resolution with
            | QsTypeKind.Operation (_, info) ->
                let supported = info.Characteristics.SupportedFunctors.ValueOr ImmutableHashSet.Empty
                let missing = Set.difference functors (Set.ofSeq supported)

                if info.Characteristics.AreInvalid || Set.isEmpty missing then
                    []
                else
                    let error =
                        ErrorCode.MissingFunctorForAutoGeneration,
                        [ missing |> Seq.map showFunctor |> String.concat "," ]

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
                let error = ErrorCode.InvalidTypeInEqualityComparison, [ showType resolvedType ]
                [ QsCompilerDiagnostic.Error error Range.Zero ]
        | HasPartialApplication (missing, result) ->
            match resolvedType.Resolution with
            | QsTypeKind.Function (_, output) ->
                context.Unify(result, QsTypeKind.Function(missing, output) |> ResolvedType.New)
            | QsTypeKind.Operation ((_, output), info) ->
                context.Unify(result, QsTypeKind.Operation((missing, output), info) |> ResolvedType.New)
            | _ ->
                let error = ErrorCode.ConstraintNotSatisfied, [ showType resolvedType; "HasPartialApplication" ]
                [ QsCompilerDiagnostic.Error error Range.Zero ]
        | Indexed (index, item) ->
            match resolvedType.Resolution, (context.Resolve index).Resolution with
            | ArrayType actualItem, Int -> context.Unify(item, actualItem)
            | ArrayType _, Range -> context.Unify(item, resolvedType)
            | _ ->
                let error = ErrorCode.ConstraintNotSatisfied, [ showType resolvedType; "Indexed" ]
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

    member private context.ApplyConstraints(param, resolvedType) =
        match variables.TryGetValue param |> tryOption with
        | Some variable ->
            let diagnostics =
                variable.Constraints
                |> List.collect (fun typeConstraint -> context.ApplyConstraint(typeConstraint, resolvedType))

            variables.[param] <- { variable with Constraints = [] }
            diagnostics
        | None -> []

module InferenceContext =
    [<CompiledName "Resolver">]
    let resolver (context: InferenceContext) =
        let types =
            { new TypeTransformation() with
                member this.OnTypeParameter param =
                    (TypeParameter param |> ResolvedType.New |> context.Resolve).Resolution
            }

        SyntaxTreeTransformation(Types = types)
