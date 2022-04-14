// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference

open System
open System.Collections.Generic
open System.Collections.Immutable
open Microsoft.Collections.Extensions
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxProcessing
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference.RelationOps
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.VerificationTools
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open Microsoft.Quantum.QsCompiler.Utils

/// A placeholder type variable.
type Variable =
    {
        /// The substituted type.
        Substitution: ResolvedType option
        /// Whether this variable encountered a type inference error.
        HasError: bool
        /// The source range that this variable originated from.
        Source: Range
    }

type ConstraintSource =
    | Here
    | There

module Utils =
    let mapFst f (x, y) = (f x, y)

    let mapSnd f (x, y) = (x, f y)

open Utils

/// Tools to help with type inference.
module Inference =
    /// <summary>
    /// True if <paramref name="info1"/> and <paramref name="info2"/> have the same set of characteristics, or if either
    /// <paramref name="info1"/> or <paramref name="info2"/> have invalid characteristics.
    /// </summary>
    let charsEqual info1 info2 =
        let chars1 = info1.Characteristics
        let chars2 = info2.Characteristics
        chars1.AreInvalid || chars2.AreInvalid || chars1.GetProperties().SetEquals(chars2.GetProperties())

    /// <summary>
    /// True if the characteristics of <paramref name="info1"/> are a subset of the characteristics of
    /// <paramref name="info2"/>, or if either <paramref name="info1"/> or <paramref name="info2"/> have invalid
    /// characteristics.
    /// </summary>
    let charsSubset info1 info2 =
        let chars1 = info1.Characteristics
        let chars2 = info2.Characteristics
        chars1.AreInvalid || chars2.AreInvalid || chars1.GetProperties().IsSubsetOf(chars2.GetProperties())

    /// <summary>
    /// True if the characteristics of <paramref name="info"/> contain the given <paramref name="functor"/>.
    /// </summary>
    let hasFunctor functor info =
        info.Characteristics.SupportedFunctors
        |> QsNullable.defaultValue ImmutableHashSet.Empty
        |> fun functors -> functors.Contains functor

    /// <summary>
    /// Combines information from two callables such that the resulting callable information satisfies the given
    /// <paramref name="ordering"/> with respect to both <paramref name="info1"/> and <paramref name="info2"/>.
    /// </summary>
    /// <returns><see cref="Some"/> information if a combination is possible; otherwise <see cref="None"/>.</returns>
    let private combineCallableInfo ordering info1 info2 =
        match ordering with
        | Subtype ->
            let chars = Union(info1.Characteristics, info2.Characteristics) |> ResolvedCharacteristics.New
            CallableInformation.New(chars, InferredCallableInformation.NoInformation) |> Some
        | Equal when charsEqual info1 info2 ->
            let inferred =
                [ info1.InferredInformation; info2.InferredInformation ] |> InferredCallableInformation.Common

            let chars = if info1.Characteristics.AreInvalid then info2.Characteristics else info1.Characteristics
            CallableInformation.New(chars, inferred) |> Some
        | Equal -> None
        | Supertype -> [ info1; info2 ] |> CallableInformation.Common |> Some

    /// <summary>
    /// Combines <paramref name="type1"/> and <paramref name="type2"/> such that the resulting type satisfies the given
    /// <paramref name="ordering"/> with respect to both original types.
    /// </summary>
    /// <returns>
    /// The combined type with a <see cref="Generated"/> range.
    /// </returns>
    let rec combine ordering (type1: ResolvedType) (type2: ResolvedType) =
        let error = TypeIntersectionMismatch(ordering, TypeContext.createOrphan type1 type2)

        match type1.Resolution, type2.Resolution with
        | ArrayType item1, ArrayType item2 -> combine Equal item1 item2 |> mapFst (ArrayType >> ResolvedType.New)
        | TupleType items1, TupleType items2 when items1.Length = items2.Length ->
            let combinedTypes, diagnostics = Seq.map2 (combine ordering) items1 items2 |> Seq.toList |> List.unzip
            ImmutableArray.CreateRange combinedTypes |> TupleType |> ResolvedType.New, List.concat diagnostics
        | QsTypeKind.Operation ((in1, out1), info1), QsTypeKind.Operation ((in2, out2), info2) ->
            let input, inDiagnostics = combine (Ordering.reverse ordering) in1 in2
            let output, outDiagnostics = combine ordering out1 out2

            let info, infoDiagnostics =
                match combineCallableInfo ordering info1 info2 with
                | Some info -> info, []
                | None ->
                    CallableInformation.New(
                        ResolvedCharacteristics.New InvalidSetExpr,
                        InferredCallableInformation.NoInformation
                    ),
                    [ error ]

            QsTypeKind.Operation((input, output), info) |> ResolvedType.New,
            inDiagnostics @ outDiagnostics @ infoDiagnostics
        | QsTypeKind.Function (in1, out1), QsTypeKind.Function (in2, out2) ->
            let input, inDiagnostics = combine (Ordering.reverse ordering) in1 in2
            let output, outDiagnostics = combine ordering out1 out2
            QsTypeKind.Function(input, output) |> ResolvedType.New, inDiagnostics @ outDiagnostics
        | InvalidType, _
        | _, InvalidType -> ResolvedType.New InvalidType, []
        | _ when type1 = type2 -> type1 |> ResolvedType.withAllRanges TypeRange.Generated, []
        | _ -> ResolvedType.New InvalidType, [ error ]
        |> mapSnd (Diagnostic.withParents type1 type2 |> List.map)

    /// <summary>
    /// True if <paramref name="param"/> does not occur in <paramref name="resolvedType"/>.
    /// </summary>
    let occursCheck param (resolvedType: ResolvedType) =
        TypeParameter param |> (=) |> resolvedType.Exists |> not

    /// An infinite sequence of alphabetic strings of increasing length in alphabetical order.
    let letters =
        Seq.initInfinite ((+) 1)
        |> Seq.collect (fun length ->
            seq { 'a' .. 'z' }
            |> Seq.map string
            |> Seq.replicate length
            |> Seq.reduce (fun strings -> Seq.allPairs strings >> Seq.map String.Concat))

    /// <summary>
    /// The set of type parameters contained in the given <paramref name="resolvedType"/>.
    /// </summary>
    let typeParameters resolvedType =
        let mutable parameters = Set.empty

        let transformation =
            { new TypeTransformation() with
                member this.OnTypeParameter param =
                    parameters <- parameters |> Set.add param
                    TypeParameter param
            }

        transformation.OnType resolvedType |> ignore
        parameters

    /// <summary>
    /// The types in a class constraint that need to be solved to at least depth one before the constraint can be
    /// applied. In other words, if the type is a fresh variable, then it needs to have a substitution, but the
    /// substituted type may be unsolved.
    /// </summary>
    let classDependencies =
        function
        | ClassConstraint.Adjointable operation -> [ operation ]
        | Callable (callable, _, _) -> [ callable ]
        | ClassConstraint.Controllable (operation, _) -> [ operation ]
        | Eq ty -> [ ty ]
        | HasFunctorsIfOperation (callable, _) -> [ callable ]
        | HasPartialApplication (callable, _, _) -> [ callable ]
        | Index (container, index, _) -> [ container; index ]
        | Integral ty -> [ ty ]
        | Iterable (container, _) -> [ container ]
        | Num ty -> [ ty ]
        | Semigroup ty -> [ ty ]
        | Unwrap (container, _) -> [ container ]

type InferenceContext(symbolTracker: SymbolTracker) =
    let variables = Dictionary()
    let classConstraints = MultiValueDictionary()

    let mutable rootNodePos = Null
    let mutable relativePos = Null
    let mutable statementPosition = Position.Zero

    let bind param substitution =
        if Inference.occursCheck param substitution then
            let variable = variables.[param]
            if Option.isSome variable.Substitution then failwith "Type parameter is already bound."
            variables.[param] <- { variable with Substitution = Some substitution }
            Ok()
        else
            Result.Error()

    let rememberErrorsFor types diagnostics =
        if types |> Seq.contains (ResolvedType.New InvalidType) || List.isEmpty diagnostics |> not then
            for param in types |> Seq.fold (fun params' -> Inference.typeParameters >> Set.union params') Set.empty do
                match variables.TryGetValue param |> tryOption with
                | Some variable -> variables.[param] <- { variable with HasError = true }
                | None -> ()

        diagnostics

    let unsolvedVariable (ty: ResolvedType) =
        match ty.Resolution with
        | TypeParameter p ->
            variables.TryGetValue p
            |> tryOption
            |> Option.bind (fun v -> if Option.isNone v.Substitution then Some p else None)
        | _ -> None

    member context.AmbiguousDiagnostics =
        let diagnostic param variable =
            let name = TypeParameter param |> ResolvedType.New |> SyntaxTreeToQsharp.Default.ToCode
            let constraints = classConstraints.GetValueOrDefault(param, []) |> Seq.map string |> String.concat ", "

            QsCompilerDiagnostic.Error
                (ErrorCode.AmbiguousTypeParameterResolution, [ name; constraints ])
                variable.Source

        variables
        |> Seq.filter (fun item -> not item.Value.HasError && Option.isNone item.Value.Substitution)
        |> Seq.map (fun item -> diagnostic item.Key item.Value)
        |> Seq.toList

    member context.UseStatementPosition position =
        rootNodePos <- Null
        relativePos <- Null
        statementPosition <- position

    member context.UseSyntaxTreeNodeLocation(rootNodePosition, relativePosition) =
        rootNodePos <- Value rootNodePosition
        relativePos <- Value relativePosition
        statementPosition <- rootNodePosition + relativePosition

    member internal context.GetRelativeStatementPosition() =
        match relativePos with
        | Value pos -> pos
        | Null -> InvalidOperationException "location information is unspecified" |> raise

    member internal context.Fresh source =
        let name = Inference.letters |> Seq.item variables.Count

        let param =
            Seq.initInfinite (fun i -> if i = 0 then name else name + string (i - 1))
            |> Seq.map (fun name -> QsTypeParameter.New(symbolTracker.Parent, name))
            |> Seq.skipWhile (fun param ->
                variables.ContainsKey param || symbolTracker.DefinedTypeParameters.Contains param.TypeName)
            |> Seq.head

        let variable =
            {
                Substitution = None
                HasError = false
                Source = statementPosition + source
            }

        variables.Add(param, variable)
        TypeParameter param |> ResolvedType.create (Inferred source)

    member internal context.Intersect(type1, type2) =
        context.ConstrainImpl(type1 .= type2) |> ignore
        let left = context.Resolve type1
        let right = context.Resolve type2

        Inference.combine Supertype left right
        |> mapSnd (List.map Diagnostic.toCompilerDiagnostic >> rememberErrorsFor [ left; right ])

    member internal context.Constrain con =
        context.ConstrainImpl con
        |> List.map Diagnostic.toCompilerDiagnostic
        |> rememberErrorsFor (Constraint.types con)

    member private context.ConstrainImpl con =
        match con with
        | Class cls ->
            match Inference.classDependencies cls |> List.choose unsolvedVariable with
            | [] -> context.ApplyClassConstraint(cls, Here)
            | tyParams ->
                for param in tyParams do
                    classConstraints.Add(param, cls)

                []
        | Relation (expected, ordering, actual) -> context.Relate(expected, ordering, actual)

    member internal context.Resolve type_ =
        let resolveWithRange type_ =
            let type_' = context.Resolve type_

            // Prefer the original type range since it may be closer to the source of an error, but otherwise fall back
            // to the newly resolved type range.
            type_' |> ResolvedType.withRange (type_.Range |> TypeRange.orElse type_'.Range)

        match type_.Resolution with
        | TypeParameter param ->
            tryOption (variables.TryGetValue param)
            |> Option.bind (fun variable -> variable.Substitution)
            |> Option.map resolveWithRange
            |> Option.defaultValue type_
        | ArrayType array -> type_ |> ResolvedType.withKind (context.Resolve array |> ArrayType)
        | TupleType tuple ->
            type_
            |> ResolvedType.withKind (tuple |> Seq.map context.Resolve |> ImmutableArray.CreateRange |> TupleType)
        | QsTypeKind.Operation ((inType, outType), info) ->
            type_
            |> ResolvedType.withKind (QsTypeKind.Operation((context.Resolve inType, context.Resolve outType), info))
        | QsTypeKind.Function (inType, outType) ->
            type_
            |> ResolvedType.withKind (QsTypeKind.Function(context.Resolve inType, context.Resolve outType))
        | _ -> type_

    member private context.Relate(expected, ordering, actual) =
        let expected = context.Resolve expected
        let actual = context.Resolve actual
        let typeContext = TypeContext.createOrphan expected actual
        let updateParents = Diagnostic.withParents expected actual

        let tryBind param substitution =
            match bind param substitution with
            | Ok () -> context.ApplyClassConstraints param
            | Result.Error () -> [ InfiniteType typeContext ]

        match expected.Resolution, actual.Resolution with
        | _ when expected = actual -> []
        | TypeParameter param, _ when variables.ContainsKey param -> tryBind param actual
        | _, TypeParameter param when variables.ContainsKey param -> tryBind param expected
        | ArrayType item1, ArrayType item2 -> context.ConstrainImpl(item1 .= item2) |> List.map updateParents
        | TupleType items1, TupleType items2 ->
            [
                if items1.Length <> items2.Length then TypeMismatch typeContext
                for item1, item2 in Seq.zip items1 items2 do
                    yield! context.Relate(item1, ordering, item2) |> List.map updateParents
            ]
        | QsTypeKind.Operation ((in1, out1), info1), QsTypeKind.Operation ((in2, out2), info2) ->
            let charsOk =
                match ordering with
                | Subtype -> Inference.charsSubset info2 info1
                | Equal -> Inference.charsEqual info1 info2
                | Supertype -> Inference.charsSubset info1 info2

            [
                if not charsOk then TypeMismatch typeContext
                yield! context.Relate(in1, Ordering.reverse ordering, in2) |> List.map updateParents
                yield! context.Relate(out1, ordering, out2) |> List.map updateParents
            ]
        | QsTypeKind.Function (in1, out1), QsTypeKind.Function (in2, out2) ->
            context.Relate(in1, Ordering.reverse ordering, in2) @ context.Relate(out1, ordering, out2)
            |> List.map updateParents
        | QsTypeKind.Operation ((in1, out1), _), QsTypeKind.Function (in2, out2)
        | QsTypeKind.Function (in1, out1), QsTypeKind.Operation ((in2, out2), _) ->
            [
                TypeMismatch typeContext
                yield! context.Relate(in1, Ordering.reverse ordering, in2) |> List.map updateParents
                yield! context.Relate(out1, ordering, out2) |> List.map updateParents
            ]
        | InvalidType, _
        | MissingType, _
        | _, InvalidType
        | _, MissingType -> []
        | _ -> [ TypeMismatch typeContext ]

    member private context.ApplyClassConstraint(cls, source) =
        let error code args range =
            let range = TypeRange.tryRange range |> QsNullable.defaultValue Range.Zero
            QsCompilerDiagnostic.Error(code, args) range |> CompilerDiagnostic

        let isInvalidType ty =
            context.Resolve(ty).Resolution = InvalidType

        match cls with
        | _ when Inference.classDependencies cls |> List.exists isInvalidType -> []
        | ClassConstraint.Adjointable operation ->
            let operation = context.Resolve operation

            match operation.Resolution with
            | QsTypeKind.Operation (_, info) when Inference.hasFunctor Adjoint info -> []
            | _ -> [ error ErrorCode.InvalidAdjointApplication [] operation.Range ]
        | Callable (callable, input, output) ->
            let callable = context.Resolve callable

            let requiredCallable =
                match callable.Resolution with
                | QsTypeKind.Operation _ ->
                    QsTypeKind.Operation((input, output), CallableInformation.NoInformation) |> ResolvedType.New |> Some
                | QsTypeKind.Function _ -> QsTypeKind.Function(input, output) |> ResolvedType.New |> Some
                | _ -> None

            // The callable constraint needs special handling for diagnostics:
            //
            // - If the source is here, it is assumed to be a call expression. The callable is the "expected" type. A
            //   type mismatch is the argument's fault.
            // - If the source is somewhere else, the callable is being provided as a value to a higher-order function.
            //   The callable is the "actual" value. A type mismatch is the callable's fault.
            //
            // The constraints are equivalent. Only the diagnostic messages are different.
            match requiredCallable, source with
            | Some requiredCallable, Here -> context.ConstrainImpl(callable <. requiredCallable)
            | Some requiredCallable, There -> context.ConstrainImpl(requiredCallable .> callable)
            | None, _ ->
                [
                    error ErrorCode.ExpectingCallableExpr [ SyntaxTreeToQsharp.Default.ToCode callable ] callable.Range
                ]
        | ClassConstraint.Controllable (operation, controlled) ->
            let operation = context.Resolve operation
            let invalidControlled = error ErrorCode.InvalidControlledApplication [] operation.Range

            match operation.Resolution with
            | QsTypeKind.Operation ((input, output), info) ->
                let actualControlled =
                    QsTypeKind.Operation((SyntaxGenerator.AddControlQubits input, output), info) |> ResolvedType.New

                [
                    if info |> Inference.hasFunctor Controlled |> not then invalidControlled
                    yield! context.ConstrainImpl(controlled .> actualControlled)
                ]
            | QsTypeKind.Function (input, output) ->
                let actualControlled =
                    QsTypeKind.Operation((SyntaxGenerator.AddControlQubits input, output), CallableInformation.Invalid)
                    |> ResolvedType.New

                invalidControlled :: context.ConstrainImpl(controlled .> actualControlled)
            | _ -> [ invalidControlled ]
        | Eq ty ->
            let ty = context.Resolve ty

            [
                if Option.isNone ty.supportsEqualityComparison then
                    error ErrorCode.InvalidTypeInEqualityComparison [ SyntaxTreeToQsharp.Default.ToCode ty ] ty.Range
            ]
        | HasFunctorsIfOperation (callable, functors) ->
            let callable = context.Resolve callable

            match callable.Resolution with
            | QsTypeKind.Operation (_, info) ->
                let supported = info.Characteristics.SupportedFunctors.ValueOr ImmutableHashSet.Empty
                let missing = Set.difference functors (Set.ofSeq supported)

                [
                    if not info.Characteristics.AreInvalid && Set.isEmpty missing |> not then
                        error
                            ErrorCode.MissingFunctorForAutoGeneration
                            [ Seq.map string missing |> String.concat "," ]
                            callable.Range
                ]
            | _ -> []
        | HasPartialApplication (callable, missing, callable') ->
            let callable = context.Resolve callable

            match callable.Resolution with
            | QsTypeKind.Function (_, output) ->
                context.ConstrainImpl(ResolvedType.New(QsTypeKind.Function(missing, output)) <. callable')
            | QsTypeKind.Operation ((_, output), info) ->
                context.ConstrainImpl(ResolvedType.New(QsTypeKind.Operation((missing, output), info)) <. callable')
            | _ ->
                [
                    error ErrorCode.ExpectingCallableExpr [ SyntaxTreeToQsharp.Default.ToCode callable ] callable.Range
                ]
        | Index (container, index, item) ->
            let container = context.Resolve container
            let index = context.Resolve index

            match container.Resolution, index.Resolution with
            | ArrayType actualItem, Int -> context.ConstrainImpl(item .> actualItem)
            | ArrayType _, Range -> context.ConstrainImpl(item .> container)
            | ArrayType _, InvalidType -> []
            | ArrayType _, _ ->
                [
                    error ErrorCode.InvalidArrayItemIndex [ SyntaxTreeToQsharp.Default.ToCode index ] index.Range
                ]
            | _ ->
                [
                    error
                        ErrorCode.ItemAccessForNonArray
                        [ SyntaxTreeToQsharp.Default.ToCode container ]
                        container.Range
                ]
        | Integral ty ->
            let ty = context.Resolve ty

            [
                if ty.Resolution <> Int && ty.Resolution <> BigInt then
                    error ErrorCode.ExpectingIntegralExpr [ SyntaxTreeToQsharp.Default.ToCode ty ] ty.Range
            ]
        | Iterable (container, item) ->
            let container = context.Resolve container

            match container.supportsIteration with
            | Some actualItem -> context.ConstrainImpl(item .> actualItem)
            | None ->
                [
                    error
                        ErrorCode.ExpectingIterableExpr
                        [ SyntaxTreeToQsharp.Default.ToCode container ]
                        container.Range
                ]
        | Num ty ->
            let ty = context.Resolve ty

            [
                if Option.isNone ty.supportsArithmetic then
                    error ErrorCode.InvalidTypeInArithmeticExpr [ SyntaxTreeToQsharp.Default.ToCode ty ] ty.Range
            ]
        | Semigroup ty ->
            let ty = context.Resolve ty

            [
                if Option.isNone ty.supportsConcatenation && Option.isNone ty.supportsArithmetic then
                    error ErrorCode.InvalidTypeForConcatenation [ SyntaxTreeToQsharp.Default.ToCode ty ] ty.Range
            ]
        | Unwrap (container, item) ->
            let container = context.Resolve container

            match container.Resolution with
            | UserDefinedType udt ->
                let actualItem = symbolTracker.GetUnderlyingType(fun _ -> ()) udt
                context.ConstrainImpl(item .> actualItem)
            | _ ->
                [
                    error
                        ErrorCode.ExpectingUserDefinedType
                        [ SyntaxTreeToQsharp.Default.ToCode container ]
                        container.Range
                ]

    /// <summary>
    /// Applies all of the ready class constraints that depend on <paramref name="param"/>.
    /// </summary>
    member private context.ApplyClassConstraints param =
        match classConstraints.TryGetValue param |> tryOption with
        | Some classes ->
            if classConstraints.Remove param |> not then failwith "Couldn't remove class constraints."
            let isReady = Inference.classDependencies >> List.choose unsolvedVariable >> List.isEmpty

            Seq.filter isReady classes
            |> Seq.collect (fun cls -> context.ApplyClassConstraint(cls, There))
            |> Seq.toList
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
