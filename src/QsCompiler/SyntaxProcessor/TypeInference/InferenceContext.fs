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
open Microsoft.Quantum.QsCompiler.TextProcessing
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open Microsoft.Quantum.QsCompiler.Utils

/// A placeholder type variable.
type private Variable =
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

module private Variable =
    /// Adds a type constraint to the type of the variable.
    let constrain typeConstraint variable =
        { variable with Constraints = typeConstraint :: variable.Constraints }

/// An ordering comparison of two types.
type private Ordering =
    /// The type is a subtype of the other type.
    | Subtype

    /// The types are equal.
    | Equal

    /// The type is a supertype of the other type.
    | Supertype

module private Ordering =
    /// Negates the direction of the ordering.
    let not =
        function
        | Subtype -> Supertype
        | Equal -> Equal
        | Supertype -> Subtype

/// Remembers context when recursively comparing two types side-by-side.
type private TypeContext =
    {
        /// The left-hand type.
        Left: ResolvedType

        /// The right-hand type.
        Right: ResolvedType

        /// The most recent relevant ancestor of the left-hand type.
        OriginalLeft: ResolvedType

        /// The most recent relevant ancestor of the right-hand type.
        OriginalRight: ResolvedType
    }

module private TypeContext =
    /// <summary>
    /// Creates a type context originating with <paramref name="left"/> and <paramref name="right"/>.
    /// </summary>
    let create left right =
        {
            Left = left
            Right = right
            OriginalLeft = left
            OriginalRight = right
        }

    /// Descends into the respective children of each type, preserving the original types if the range of both the left
    /// and the right types do not change.
    let into (leftChild: ResolvedType) (rightChild: ResolvedType) context =
        if leftChild.Range = context.Left.Range || rightChild.Range = context.Right.Range
        then { context with Left = leftChild; Right = rightChild }
        else create leftChild rightChild

    /// Descends into the respective children of each type, preserving the original types if the range of the right type
    /// does not change.
    let intoRight leftChild (rightChild: ResolvedType) context =
        if rightChild.Range = context.Right.Range
        then { context with Left = leftChild; Right = rightChild }
        else create leftChild rightChild

    /// Swaps the left and right types.
    let swap context =
        {
            Left = context.Right
            Right = context.Left
            OriginalLeft = context.OriginalRight
            OriginalRight = context.OriginalLeft
        }

    /// The list of types in the context, in order: left, right, original left, original right.
    let toList context =
        [ context.Left; context.Right; context.OriginalLeft; context.OriginalRight ]

/// Tools to help with type inference.
module private Inference =
    /// <summary>
    /// True if <paramref name="info1"/> and <paramref name="info2"/> contain the same set of characteristics.
    /// </summary>
    let characteristicsEqual info1 info2 =
        let chars1 = info1.Characteristics
        let chars2 = info2.Characteristics
        chars1.AreInvalid || chars2.AreInvalid || chars1.GetProperties().SetEquals(chars2.GetProperties())

    /// <summary>
    /// True if the characteristics of <paramref name="info1"/> are a subset of the characteristics of
    /// <paramref name="info2"/>.
    /// </summary>
    let isSubset info1 info2 =
        info1.Characteristics.GetProperties().IsSubsetOf(info2.Characteristics.GetProperties())

    /// <summary>
    /// True if the characteristics of <paramref name="info"/> contain the given <paramref name="functor"/>.
    /// </summary>
    let hasFunctor functor info =
        info.Characteristics.SupportedFunctors
        |> QsNullable.defaultValue ImmutableHashSet.Empty
        |> fun functors -> functors.Contains functor

    /// Shows the type as a string.
    let showType: ResolvedType -> _ = SyntaxTreeToQsharp.Default.ToCode

    /// Shows the functor as a string.
    let showFunctor =
        function
        | Adjoint -> Keywords.qsAdjointFunctor.id
        | Controlled -> Keywords.qsControlledFunctor.id

    /// <summary>
    /// Combines information from two callables such that the resulting callable information satisfies the given
    /// <paramref name="ordering"/> with respect to both <paramref name="info1"/> and <paramref name="info2"/>.
    /// </summary>
    /// <returns><see cref="Some"/> information if a combination is possible; otherwise <see cref="None"/>.</returns>
    let private combineCallableInfo ordering info1 info2 =
        match ordering with
        | Subtype ->
            let chars = Union(info1.Characteristics, info2.Characteristics)

            CallableInformation.New(ResolvedCharacteristics.New chars, InferredCallableInformation.NoInformation)
            |> Some
        | Equal when characteristicsEqual info1 info2 ->
            let characteristics =
                if info1.Characteristics.AreInvalid then info2.Characteristics else info1.Characteristics

            let inferred =
                [ info1.InferredInformation; info2.InferredInformation ] |> InferredCallableInformation.Common

            CallableInformation.New(characteristics, inferred) |> Some
        | Equal -> None
        | Supertype -> [ info1; info2 ] |> CallableInformation.Common |> Some

    /// <summary>
    /// Combines two types such that the resulting type satisfies the given <paramref name="ordering"/> with respect to
    /// both original types.
    /// </summary>
    let rec combine ordering types =
        let range = QsNullable.Map2 Range.Span types.Left.Range types.Right.Range

        let relation =
            match ordering with
            | Subtype -> "share a subtype with"
            | Equal -> "equal"
            | Supertype -> "share a base type with"

        let error =
            QsCompilerDiagnostic.Error
                (ErrorCode.MissingBaseType, relation :: (TypeContext.toList types |> List.map showType))
                (range |> QsNullable.defaultValue Range.Zero)

        match types.Left.Resolution, types.Right.Resolution with
        | ArrayType item1, ArrayType item2 ->
            let combinedType, diagnostics = types |> TypeContext.into item1 item2 |> combine Equal
            ArrayType combinedType |> ResolvedType.create range, diagnostics
        | TupleType items1, TupleType items2 when items1.Length = items2.Length ->
            let combinedTypes, diagnostics =
                (items1, items2)
                ||> Seq.map2 (fun item1 item2 -> types |> TypeContext.into item1 item2 |> combine ordering)
                |> Seq.toList
                |> List.unzip

            ImmutableArray.CreateRange combinedTypes |> TupleType |> ResolvedType.create range, List.concat diagnostics
        | QsTypeKind.Operation ((in1, out1), info1), QsTypeKind.Operation ((in2, out2), info2) ->
            let input, inDiagnostics = types |> TypeContext.into in1 in2 |> combine (Ordering.not ordering)
            let output, outDiagnostics = types |> TypeContext.into out1 out2 |> combine ordering

            let info, infoDiagnostics =
                match combineCallableInfo ordering info1 info2 with
                | Some info -> info, []
                | None ->
                    CallableInformation.New
                        (ResolvedCharacteristics.New InvalidSetExpr, InferredCallableInformation.NoInformation),
                    [ error ]

            QsTypeKind.Operation((input, output), info) |> ResolvedType.create range,
            inDiagnostics @ outDiagnostics @ infoDiagnostics
        | QsTypeKind.Function (in1, out1), QsTypeKind.Function (in2, out2) ->
            let input, inDiagnostics = types |> TypeContext.into in1 in2 |> combine (Ordering.not ordering)
            let output, outDiagnostics = types |> TypeContext.into out1 out2 |> combine ordering
            QsTypeKind.Function(input, output) |> ResolvedType.create range, inDiagnostics @ outDiagnostics
        | InvalidType, _
        | _, InvalidType -> ResolvedType.create range InvalidType, []
        | _ when types.Left = types.Right -> types.Left |> ResolvedType.withAllRanges range, []
        | _ -> ResolvedType.create range InvalidType, [ error ]

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

        { new TypeTransformation() with
            member this.OnTypeParameter param =
                parameters <- parameters |> Set.add param
                TypeParameter param
        }.OnType resolvedType
        |> ignore

        parameters

open Inference

type InferenceContext(symbolTracker: SymbolTracker) =
    let variables = Dictionary()

    let mutable statementPosition = Position.Zero

    let bind param substitution =
        if occursCheck param substitution |> not then failwith "Occurs check failed."
        let variable = variables.[param]

        match variable.Substitution with
        | Some substitution' when substitution <> substitution' ->
            failwith "The type parameter is already bound to a different type."
        | _ -> variables.[param] <- { variable with Substitution = Some substitution }

    let rememberErrors types diagnostics =
        if types |> Seq.contains (ResolvedType.New InvalidType) || List.isEmpty diagnostics |> not then
            for param in types |> Seq.fold (fun params' -> typeParameters >> Set.union params') Set.empty do
                match variables.TryGetValue param |> tryOption with
                | Some variable -> variables.[param] <- { variable with HasError = true }
                | None -> ()

        diagnostics

    member context.AmbiguousDiagnostics =
        [
            for variable in variables do
                if not variable.Value.HasError && Option.isNone variable.Value.Substitution
                then QsCompilerDiagnostic.Error (ErrorCode.AmbiguousTypeParameterResolution, []) variable.Value.Source
        ]

    member context.UseStatementPosition position = statementPosition <- position

    member internal context.Fresh source =
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
                HasError = false
                Source = statementPosition + source
            }

        variables.Add(param, variable)
        TypeParameter param |> ResolvedType.create (Value source)

    member internal context.Unify(expected, actual) =
        context.UnifyByOrdering(Supertype, TypeContext.create (context.Resolve expected) (context.Resolve actual))
        |> rememberErrors [ expected; actual ]

    member internal context.Intersect(left, right) =
        context.UnifyByOrdering(Equal, TypeContext.create (context.Resolve left) (context.Resolve right))
        |> ignore

        let left = context.Resolve left
        let right = context.Resolve right
        let intersection, diagnostics = TypeContext.create left right |> combine Supertype
        intersection, diagnostics |> rememberErrors [ left; right ]

    member internal context.Constrain(resolvedType, typeConstraint) =
        let resolvedType = context.Resolve resolvedType

        match resolvedType.Resolution with
        | TypeParameter param ->
            match variables.TryGetValue param |> tryOption with
            | Some variable ->
                variables.[param] <- variable |> Variable.constrain typeConstraint
                []
            | None -> context.ApplyConstraint(typeConstraint, resolvedType)
        | _ -> context.ApplyConstraint(typeConstraint, resolvedType)
        |> rememberErrors (resolvedType :: Constraint.types typeConstraint)

    member internal context.Resolve resolvedType =
        let resolveWithRange type' =
            let type' = context.Resolve type'
            type' |> ResolvedType.withRange (type'.Range |> QsNullable.orElse resolvedType.Range)

        match resolvedType.Resolution with
        | TypeParameter param ->
            tryOption (variables.TryGetValue param)
            |> Option.bind (fun variable -> variable.Substitution)
            |> Option.map resolveWithRange
            |> Option.defaultValue resolvedType
        | ArrayType array -> resolvedType |> ResolvedType.withKind (context.Resolve array |> ArrayType)
        | TupleType tuple ->
            resolvedType
            |> ResolvedType.withKind (tuple |> Seq.map context.Resolve |> ImmutableArray.CreateRange |> TupleType)
        | QsTypeKind.Operation ((inType, outType), info) ->
            resolvedType
            |> ResolvedType.withKind (QsTypeKind.Operation((context.Resolve inType, context.Resolve outType), info))
        | QsTypeKind.Function (inType, outType) ->
            resolvedType
            |> ResolvedType.withKind (QsTypeKind.Function(context.Resolve inType, context.Resolve outType))
        | _ -> resolvedType

    /// <summary>
    /// Unifies two types given that the left-hand type must satisfy the <paramref name="ordering"/> relation relative
    /// to the right-hand type.
    /// </summary>
    member private context.UnifyByOrdering(ordering, types) =
        let error =
            QsCompilerDiagnostic.Error
                (ErrorCode.TypeMismatch, TypeContext.toList types |> List.map showType)
                (types.Right.Range |> QsNullable.defaultValue Range.Zero)

        match types.Left.Resolution, types.Right.Resolution with
        | _ when types.Left = types.Right -> []
        | TypeParameter param, _ when variables.ContainsKey param ->
            bind param types.Right
            context.ApplyConstraints(param, types.Right)
        | _, TypeParameter param when variables.ContainsKey param ->
            bind param types.Left
            context.ApplyConstraints(param, types.Left)
        | ArrayType item1, ArrayType item2 -> context.UnifyByOrdering(Equal, types |> TypeContext.intoRight item1 item2)
        | TupleType items1, TupleType items2 ->
            [
                if items1.Length <> items2.Length then error
                for item1, item2 in Seq.zip items1 items2 do
                    let types = types |> TypeContext.intoRight (context.Resolve item1) (context.Resolve item2)
                    yield! context.UnifyByOrdering(ordering, types)
            ]
        | QsTypeKind.Operation ((in1, out1), info1), QsTypeKind.Operation ((in2, out2), info2) ->
            let errors =
                if ordering = Equal && not (characteristicsEqual info1 info2)
                   || ordering = Supertype && not (isSubset info1 info2)
                   || ordering = Subtype && not (isSubset info2 info1) then
                    [ error ]
                else
                    []

            context.UnifyByOrdering(ordering, types |> TypeContext.swap |> TypeContext.intoRight in2 in1)
            @ context.UnifyByOrdering
                (ordering, types |> TypeContext.intoRight (context.Resolve out1) (context.Resolve out2))
              @ errors
        | QsTypeKind.Function (in1, out1), QsTypeKind.Function (in2, out2) ->
            context.UnifyByOrdering(ordering, types |> TypeContext.swap |> TypeContext.intoRight in2 in1)
            @ context.UnifyByOrdering
                (ordering, types |> TypeContext.intoRight (context.Resolve out1) (context.Resolve out2))
        | QsTypeKind.Operation ((in1, out1), _), QsTypeKind.Function (in2, out2)
        | QsTypeKind.Function (in1, out1), QsTypeKind.Operation ((in2, out2), _) ->
            error
            :: context.UnifyByOrdering(ordering, types |> TypeContext.swap |> TypeContext.intoRight in2 in1)
            @ context.UnifyByOrdering
                (ordering, types |> TypeContext.intoRight (context.Resolve out1) (context.Resolve out2))
        | InvalidType, _
        | MissingType, _
        | _, InvalidType
        | _, MissingType -> []
        | _ -> [ error ]

    /// <summary>
    /// Applies the <paramref name="typeConstraint"/> to the given <paramref name="resolvedType"/>.
    /// </summary>
    member private context.ApplyConstraint(typeConstraint, resolvedType) =
        let range = resolvedType.Range |> QsNullable.defaultValue Range.Zero

        match typeConstraint with
        | _ when resolvedType.Resolution = InvalidType -> []
        | Constraint.Adjointable ->
            match resolvedType.Resolution with
            | QsTypeKind.Operation (_, info) when hasFunctor Adjoint info -> []
            | _ -> [ QsCompilerDiagnostic.Error (ErrorCode.InvalidAdjointApplication, []) range ]
        | Callable (input, output) ->
            match resolvedType.Resolution with
            | QsTypeKind.Operation _ ->
                let operationType = QsTypeKind.Operation((input, output), CallableInformation.NoInformation)
                context.Unify(ResolvedType.New operationType, resolvedType)
            | QsTypeKind.Function _ ->
                context.Unify(QsTypeKind.Function(input, output) |> ResolvedType.New, resolvedType)
            | _ ->
                [
                    QsCompilerDiagnostic.Error (ErrorCode.ExpectingCallableExpr, [ showType resolvedType ]) range
                ]
        | CanGenerateFunctors functors ->
            match resolvedType.Resolution with
            | QsTypeKind.Operation (_, info) ->
                let supported = info.Characteristics.SupportedFunctors.ValueOr ImmutableHashSet.Empty
                let missing = Set.difference functors (Set.ofSeq supported)

                let error =
                    ErrorCode.MissingFunctorForAutoGeneration, [ missing |> Seq.map showFunctor |> String.concat "," ]

                [
                    if not info.Characteristics.AreInvalid && Set.isEmpty missing |> not
                    then QsCompilerDiagnostic.Error error range
                ]
            | _ -> []
        | Constraint.Controllable controlled ->
            let error = QsCompilerDiagnostic.Error (ErrorCode.InvalidControlledApplication, []) range

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
            [
                if Option.isNone resolvedType.supportsEqualityComparison
                then QsCompilerDiagnostic.Error
                         (ErrorCode.InvalidTypeInEqualityComparison, [ showType resolvedType ])
                         range
            ]
        | HasPartialApplication (missing, result) ->
            match resolvedType.Resolution with
            | QsTypeKind.Function (_, output) ->
                context.Unify(result, QsTypeKind.Function(missing, output) |> ResolvedType.New)
            | QsTypeKind.Operation ((_, output), info) ->
                context.Unify(result, QsTypeKind.Operation((missing, output), info) |> ResolvedType.New)
            | _ ->
                [
                    QsCompilerDiagnostic.Error (ErrorCode.ExpectingCallableExpr, [ showType resolvedType ]) range
                ]
        | Indexed (index, item) ->
            let index = context.Resolve index

            match resolvedType.Resolution, index.Resolution with
            | ArrayType actualItem, Int -> context.Unify(item, actualItem)
            | ArrayType _, Range -> context.Unify(item, resolvedType)
            | ArrayType _, _ ->
                [
                    QsCompilerDiagnostic.Error
                        (ErrorCode.InvalidArrayItemIndex, [ showType index ])
                        (index.Range |> QsNullable.defaultValue Range.Zero)
                ]
            | _ ->
                [
                    QsCompilerDiagnostic.Error (ErrorCode.ItemAccessForNonArray, [ showType resolvedType ]) range
                ]
        | Integral ->
            [
                if resolvedType.Resolution <> Int && resolvedType.Resolution <> BigInt
                then QsCompilerDiagnostic.Error (ErrorCode.ExpectingIntegralExpr, [ showType resolvedType ]) range
            ]
        | Iterable item ->
            match resolvedType.supportsIteration with
            | Some actualItem -> context.Unify(item, actualItem)
            | None ->
                [
                    QsCompilerDiagnostic.Error (ErrorCode.ExpectingIterableExpr, [ showType resolvedType ]) range
                ]
        | Numeric ->
            [
                if Option.isNone resolvedType.supportsArithmetic
                then QsCompilerDiagnostic.Error (ErrorCode.InvalidTypeInArithmeticExpr, [ showType resolvedType ]) range
            ]
        | Semigroup ->
            [
                if Option.isNone resolvedType.supportsConcatenation && Option.isNone resolvedType.supportsArithmetic
                then QsCompilerDiagnostic.Error (ErrorCode.InvalidTypeForConcatenation, [ showType resolvedType ]) range
            ]
        | Wrapped item ->
            match resolvedType.Resolution with
            | UserDefinedType udt ->
                let actualItem = symbolTracker.GetUnderlyingType (fun _ -> ()) udt
                context.Unify(item, actualItem)
            | _ ->
                [
                    QsCompilerDiagnostic.Error (ErrorCode.ExpectingUserDefinedType, [ showType resolvedType ]) range
                ]

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
