// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference

open System
open System.Collections.Generic
open System.Collections.Immutable

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxProcessing
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
        /// The list of constraints on the type of this variable.
        Constraints: Constraint list
        /// Whether this variable encountered a type inference error.
        HasError: bool
        /// The source range that this variable originated from.
        Source: Range
    }

module Variable =
    /// Adds a type constraint to the type of the variable.
    let constrain typeConstraint variable =
        { variable with Constraints = typeConstraint :: variable.Constraints }

/// An ordering comparison between types.
type Ordering =
    /// The type is a subtype of another type.
    | Subtype
    /// The types are equal.
    | Equal
    /// The type is a supertype of another type.
    | Supertype

module Ordering =
    /// Reverses the direction of the ordering.
    let reverse =
        function
        | Subtype -> Supertype
        | Equal -> Equal
        | Supertype -> Subtype

type 'a Relation = Relation of lhs: 'a * ordering: Ordering * rhs: 'a

module Relation =
    let map f (Relation (x, ordering, y)) = Relation(f x, ordering, f y)

module RelationOps =
    let (<.) lhs rhs = Relation(lhs, Subtype, rhs)

    let (.=.) lhs rhs = Relation(lhs, Equal, rhs)

    let (.>) lhs rhs = Relation(lhs, Supertype, rhs)

/// <summary>A type context stores information needed for error reporting of mismatched types.</summary>
/// <example>
/// <para>
/// While matching type <c>(A, B)</c> with type <c>(C, B)</c>, an error is encountered when <c>A</c> is recursively
/// matched with <c>C</c>. The error message might look something like: "Mismatched types <c>A</c> and <c>C</c>.
/// Expected: <c>(A, B)</c>. Actual: <c>(C, B)</c>."
/// </para>
/// <para>
/// In this example, <c>A</c> is <see cref="Expected" />, <c>(A, B)</c> is <see cref="ExpectedParent" />, <c>C</c> is
/// <see cref="Actual" />, and <c>(C, B)</c> is <see cref="ActualParent" />.
/// </para>
/// </example>
type TypeContext =
    {
        Expected: ResolvedType
        ExpectedParent: ResolvedType option
        Actual: ResolvedType
        ActualParent: ResolvedType option
    }

module TypeContext =
    let createOrphan expected actual =
        {
            Expected = expected
            ExpectedParent = None
            Actual = actual
            ActualParent = None
        }

    let withParents expected actual mismatch =
        { mismatch with ExpectedParent = Some expected; ActualParent = Some actual }

type Diagnostic =
    | TypeMismatch of TypeContext
    | TypeIntersectionMismatch of ordering: Ordering * context: TypeContext
    | InfiniteType of TypeContext
    | CompilerDiagnostic of QsCompilerDiagnostic

module Diagnostic =
    /// <summary>
    /// Updates the parents in the diagnostic's type context if the type range of the new parents is the same as the old
    /// parents.
    /// </summary>
    /// <remarks>
    /// When updating diagnostic parents "inside out" (from the innermost nested types that caused the error to the
    /// outermost original types), the range checking behavior has the effect of finding the full type of the expression
    /// that is underlined by the diagnostic.
    /// </remarks>
    let withParents expected actual =
        let hasSameRange (type1: ResolvedType) : ResolvedType option -> _ =
            Option.forall (fun type2 -> type1.Range = type2.Range)

        let checkActualRange context =
            if hasSameRange actual context.ActualParent then
                context |> TypeContext.withParents expected actual
            else
                context

        let checkBothRanges context =
            if hasSameRange expected context.ExpectedParent && hasSameRange actual context.ActualParent then
                context |> TypeContext.withParents expected actual
            else
                context

        // For diagnostics whose range corresponds to the actual type of the provided expression, only check the actual
        // type's range. For diagnostics whose range spans both the expected and actual types, check both ranges. See
        // `toCompilerDiagnostic` for which range is used by each diagnostic case.
        function
        | TypeMismatch context -> checkActualRange context |> TypeMismatch
        | TypeIntersectionMismatch (ordering, context) -> TypeIntersectionMismatch(ordering, checkBothRanges context)
        | InfiniteType context -> checkActualRange context |> InfiniteType
        | CompilerDiagnostic diagnostic -> CompilerDiagnostic diagnostic

    let private describeType (resolvedType: ResolvedType) =
        match resolvedType.Resolution with
        | TypeParameter param ->
            sprintf "parameter %s (bound by %s)" (SyntaxTreeToQsharp.Default.ToCode resolvedType) param.Origin.Name
        | _ -> SyntaxTreeToQsharp.Default.ToCode resolvedType

    let private typeContextArgs context =
        let expectedParent = context.ExpectedParent |> Option.defaultValue context.Expected
        let actualParent = context.ActualParent |> Option.defaultValue context.Actual

        [
            describeType context.Expected
            SyntaxTreeToQsharp.Default.ToCode expectedParent
            describeType context.Actual
            SyntaxTreeToQsharp.Default.ToCode actualParent
        ]

    let toCompilerDiagnostic =
        function
        | TypeMismatch context ->
            let range = TypeRange.tryRange context.Actual.Range |> QsNullable.defaultValue Range.Zero
            QsCompilerDiagnostic.Error(ErrorCode.TypeMismatch, typeContextArgs context) range
        | TypeIntersectionMismatch (ordering, context) ->
            let orderingString =
                match ordering with
                | Subtype -> "share a subtype with"
                | Equal -> "equal"
                | Supertype -> "share a supertype with"

            let range =
                (TypeRange.tryRange context.Expected.Range, TypeRange.tryRange context.Actual.Range)
                ||> QsNullable.Map2 Range.Span
                |> QsNullable.defaultValue Range.Zero

            let args = orderingString :: typeContextArgs context
            QsCompilerDiagnostic.Error(ErrorCode.TypeIntersectionMismatch, args) range
        | InfiniteType context ->
            let range = TypeRange.tryRange context.Actual.Range |> QsNullable.defaultValue Range.Zero
            QsCompilerDiagnostic.Error(ErrorCode.InfiniteType, typeContextArgs context) range
        | CompilerDiagnostic diagnostic -> diagnostic

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
        |> Seq.collect
            (fun length ->
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

open RelationOps

type InferenceContext(symbolTracker: SymbolTracker) =
    let variables = Dictionary()

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

    member context.AmbiguousDiagnostics =
        let diagnostic param variable =
            let args =
                [
                    TypeParameter param |> ResolvedType.New |> SyntaxTreeToQsharp.Default.ToCode
                    variable.Constraints |> List.map Constraint.pretty |> String.concat ", "
                ]

            QsCompilerDiagnostic.Error(ErrorCode.AmbiguousTypeParameterResolution, args) variable.Source

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
            |> Seq.skipWhile
                (fun param ->
                    variables.ContainsKey param || symbolTracker.DefinedTypeParameters.Contains param.TypeName)
            |> Seq.head

        let variable =
            {
                Substitution = None
                Constraints = []
                HasError = false
                Source = statementPosition + source
            }

        variables.Add(param, variable)
        TypeParameter param |> ResolvedType.create (Inferred source)

    member internal context.Match(Relation (expected, _, actual) as relation) =
        context.MatchImpl relation
        |> List.map Diagnostic.toCompilerDiagnostic
        |> rememberErrorsFor [ expected; actual ]

    member internal context.Intersect(type1, type2) =
        context.MatchImpl(type1 .=. type2) |> ignore
        let left = context.Resolve type1
        let right = context.Resolve type2

        Inference.combine Supertype left right
        |> mapSnd (List.map Diagnostic.toCompilerDiagnostic >> rememberErrorsFor [ left; right ])

    member internal context.Constrain(type_, constraint_) =
        let type_ = context.Resolve type_

        match type_.Resolution with
        | TypeParameter param ->
            match variables.TryGetValue param |> tryOption with
            | Some variable ->
                variables.[param] <- variable |> Variable.constrain constraint_
                []
            | None -> context.ApplyConstraint(constraint_, type_)
        | _ -> context.ApplyConstraint(constraint_, type_)
        |> List.map Diagnostic.toCompilerDiagnostic
        |> rememberErrorsFor (type_ :: Constraint.types constraint_)

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

    member private context.MatchImpl(Relation (expected, ordering, actual)) =
        let expected = context.Resolve expected
        let actual = context.Resolve actual
        let typeContext = TypeContext.createOrphan expected actual

        let tryBind param substitution =
            match bind param substitution with
            | Ok () -> context.ApplyConstraints(param, substitution)
            | Result.Error () -> [ InfiniteType typeContext ]

        match expected.Resolution, actual.Resolution with
        | _ when expected = actual -> []
        | TypeParameter param, _ when variables.ContainsKey param -> tryBind param actual
        | _, TypeParameter param when variables.ContainsKey param -> tryBind param expected
        | ArrayType item1, ArrayType item2 -> context.MatchImpl(item1 .=. item2)
        | TupleType items1, TupleType items2 ->
            [
                if items1.Length <> items2.Length then TypeMismatch typeContext
                for item1, item2 in Seq.zip items1 items2 do
                    yield! Relation(item1, ordering, item2) |> context.MatchImpl
            ]
        | QsTypeKind.Operation ((in1, out1), info1), QsTypeKind.Operation ((in2, out2), info2) ->
            let charsErrors =
                match ordering with
                | Subtype when Inference.charsSubset info2 info1 |> not -> [ TypeMismatch typeContext ]
                | Equal when Inference.charsEqual info1 info2 |> not -> [ TypeMismatch typeContext ]
                | Supertype when Inference.charsSubset info1 info2 |> not -> [ TypeMismatch typeContext ]
                | _ -> []

            context.MatchImpl(Relation(in1, Ordering.reverse ordering, in2))
            @ context.MatchImpl(Relation(out1, ordering, out2)) @ charsErrors
        | QsTypeKind.Function (in1, out1), QsTypeKind.Function (in2, out2) ->
            context.MatchImpl(Relation(in1, Ordering.reverse ordering, in2))
            @ context.MatchImpl(Relation(out1, ordering, out2))
        | QsTypeKind.Operation ((in1, out1), _), QsTypeKind.Function (in2, out2)
        | QsTypeKind.Function (in1, out1), QsTypeKind.Operation ((in2, out2), _) ->
            TypeMismatch typeContext :: context.MatchImpl(Relation(in1, Ordering.reverse ordering, in2))
            @ context.MatchImpl(Relation(out1, ordering, out2))
        | InvalidType, _
        | MissingType, _
        | _, InvalidType
        | _, MissingType -> []
        | _ -> [ TypeMismatch typeContext ]
        |> List.map (Diagnostic.withParents expected actual)

    member private context.ApplyConstraint(constraint_, type_) =
        let error code args range =
            let range = TypeRange.tryRange range |> QsNullable.defaultValue Range.Zero
            QsCompilerDiagnostic.Error(code, args) range |> CompilerDiagnostic

        let typeString = SyntaxTreeToQsharp.Default.ToCode type_

        match constraint_ with
        | _ when type_.Resolution = InvalidType -> []
        | Constraint.Adjointable ->
            match type_.Resolution with
            | QsTypeKind.Operation (_, info) when Inference.hasFunctor Adjoint info -> []
            | _ -> [ error ErrorCode.InvalidAdjointApplication [] type_.Range ]
        | Callable (input, output) ->
            match type_.Resolution with
            | QsTypeKind.Operation _ ->
                let operationType = QsTypeKind.Operation((input, output), CallableInformation.NoInformation)
                context.MatchImpl(type_ <. ResolvedType.New operationType)
            | QsTypeKind.Function _ -> context.MatchImpl(type_ <. ResolvedType.New(QsTypeKind.Function(input, output)))
            | _ -> [ error ErrorCode.ExpectingCallableExpr [ typeString ] type_.Range ]
        | CanGenerateFunctors functors ->
            match type_.Resolution with
            | QsTypeKind.Operation (_, info) ->
                let supported = info.Characteristics.SupportedFunctors.ValueOr ImmutableHashSet.Empty
                let missing = Set.difference functors (Set.ofSeq supported)

                [
                    if not info.Characteristics.AreInvalid && Set.isEmpty missing |> not then
                        error
                            ErrorCode.MissingFunctorForAutoGeneration
                            [ Seq.map string missing |> String.concat "," ]
                            type_.Range
                ]
            | _ -> []
        | Constraint.Controllable controlled ->
            let invalidControlled = error ErrorCode.InvalidControlledApplication [] type_.Range

            match type_.Resolution with
            | QsTypeKind.Operation ((input, output), info) ->
                let actualControlled =
                    QsTypeKind.Operation((SyntaxGenerator.AddControlQubits input, output), info) |> ResolvedType.New

                [
                    if info |> Inference.hasFunctor Controlled |> not then invalidControlled
                    yield! context.MatchImpl(controlled .> actualControlled)
                ]
            | QsTypeKind.Function (input, output) ->
                let actualControlled =
                    QsTypeKind.Operation((SyntaxGenerator.AddControlQubits input, output), CallableInformation.Invalid)
                    |> ResolvedType.New

                invalidControlled :: context.MatchImpl(controlled .> actualControlled)
            | _ -> [ invalidControlled ]
        | Equatable ->
            [
                if Option.isNone type_.supportsEqualityComparison then
                    error ErrorCode.InvalidTypeInEqualityComparison [ typeString ] type_.Range
            ]
        | HasPartialApplication (missing, result) ->
            match type_.Resolution with
            | QsTypeKind.Function (_, output) ->
                context.MatchImpl(ResolvedType.New(QsTypeKind.Function(missing, output)) <. result)
            | QsTypeKind.Operation ((_, output), info) ->
                context.MatchImpl(ResolvedType.New(QsTypeKind.Operation((missing, output), info)) <. result)
            | _ -> [ error ErrorCode.ExpectingCallableExpr [ typeString ] type_.Range ]
        | Indexed (index, item) ->
            let index = context.Resolve index

            match type_.Resolution, index.Resolution with
            | ArrayType actualItem, Int -> context.MatchImpl(item .> actualItem)
            | ArrayType _, Range -> context.MatchImpl(item .> type_)
            | ArrayType _, InvalidType -> []
            | ArrayType _, _ ->
                [
                    error ErrorCode.InvalidArrayItemIndex [ SyntaxTreeToQsharp.Default.ToCode index ] index.Range
                ]
            | _ -> [ error ErrorCode.ItemAccessForNonArray [ typeString ] type_.Range ]
        | Integral ->
            [
                if type_.Resolution <> Int && type_.Resolution <> BigInt then
                    error ErrorCode.ExpectingIntegralExpr [ typeString ] type_.Range
            ]
        | Iterable item ->
            match type_.supportsIteration with
            | Some actualItem -> context.MatchImpl(item .> actualItem)
            | None -> [ error ErrorCode.ExpectingIterableExpr [ typeString ] type_.Range ]
        | Numeric ->
            [
                if Option.isNone type_.supportsArithmetic then
                    error ErrorCode.InvalidTypeInArithmeticExpr [ typeString ] type_.Range
            ]
        | Semigroup ->
            [
                if Option.isNone type_.supportsConcatenation && Option.isNone type_.supportsArithmetic then
                    error ErrorCode.InvalidTypeForConcatenation [ typeString ] type_.Range
            ]
        | Wrapped item ->
            match type_.Resolution with
            | UserDefinedType udt ->
                let actualItem = symbolTracker.GetUnderlyingType(fun _ -> ()) udt
                context.MatchImpl(item .> actualItem)
            | _ -> [ error ErrorCode.ExpectingUserDefinedType [ typeString ] type_.Range ]

    /// <summary>
    /// Applies all of the constraints for <paramref name="param"/>, given that it has just been bound to
    /// <paramref name="resolvedType"/>.
    /// </summary>
    member private context.ApplyConstraints(param, resolvedType) =
        match variables.TryGetValue param |> tryOption with
        | Some variable ->
            let diagnostics = variable.Constraints |> List.collect (fun c -> context.ApplyConstraint(c, resolvedType))
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
