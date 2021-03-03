// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.Expressions

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Linq

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxGenerator
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.VerificationTools
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open Microsoft.Quantum.QsCompiler.Utils

// utils for verifying types in expressions

type private StripInferredInfoFromType() =
    inherit TypeTransformationBase()

    default this.OnCallableInformation opInfo =
        let characteristics = this.OnCharacteristicsExpression opInfo.Characteristics
        CallableInformation.New(characteristics, InferredCallableInformation.NoInformation)

    override this.OnRangeInformation _ = Null

/// Return the string representation for a ResolveType. User defined types are represented by their full name.
let private toString (t: ResolvedType) = SyntaxTreeToQsharp.Default.ToCode t

let private StripInferredInfoFromType = (new StripInferredInfoFromType()).OnType

let private ExprWithoutTypeArgs isMutable (ex, t, dep, range) =
    let inferred = InferredExpressionInformation.New(isMutable = isMutable, quantumDep = dep)
    TypedExpression.New(ex, ImmutableDictionary.Empty, t, inferred, range)

let errorToDiagnostic f diagnose =
    f (fun (error, args) range -> QsCompilerDiagnostic.Error (error, args) range |> diagnose)

let private diagnoseWithRange range diagnose =
    List.iter (fun d -> diagnose { d with QsCompilerDiagnostic.Range = range })

/// Calls the given addWarning function with a suitable warning code and the given range
/// if the given expression contains an operation call.
let private VerifyConditionalExecution addWarning (ex: TypedExpression, range) =
    let isOperationCall (ex: TypedExpression) =
        match ex.Expression with
        | CallLikeExpression (method, _) when not (TypedExpression.IsPartialApplication ex.Expression) ->
            match method.ResolvedType.Resolution with
            | QsTypeKind.Operation (_, _) -> true
            | _ -> false
        | _ -> false

    if ex.Exists isOperationCall
    then range |> addWarning (WarningCode.ConditionalEvaluationOfOperationCall, [])

/// Given a function asExpected, returns the resolved type returned by that function if it returns Some,
/// and returns in invalid type otherwise, adding an ExpressionOfUnknownType error with the given range using addError
/// if the given type is a missing type.
let private VerifyIsOneOf asExpected errCode addError (exType: ResolvedType, range) =
    match asExpected exType with
    | Some exT -> exT
    | None when exType.isInvalid -> ResolvedType.New InvalidType
    | None when exType.isMissing ->
        range |> addError (ErrorCode.ExpressionOfUnknownType, [])
        ResolvedType.New InvalidType
    | None ->
        range |> addError errCode
        ResolvedType.New InvalidType

/// Verifies that the given resolved type is indeed of kind Unit,
/// adding an ExpectingUnitExpr error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
let internal VerifyIsUnit (inference: InferenceContext) diagnose (exType, range) =
    inference.Unify(exType, ResolvedType.New UnitType) |> diagnoseWithRange range diagnose

/// Verifies that the given resolved type is indeed of kind String,
/// adding an ExpectingStringExpr error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
let internal VerifyIsString addError (exType, range) =
    let expectedString (t: ResolvedType) =
        if t.Resolution = String then Some t else None

    VerifyIsOneOf expectedString (ErrorCode.ExpectingStringExpr, [ exType |> toString ]) addError (exType, range)
    |> ignore

/// Verifies that the given resolved type is indeed a user defined type,
/// adding an ExpectingUserDefinedType error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
/// Calls the given processing function on the user defined type, passing it the given function to add errors for the given range.
let internal VerifyUdtWith processUdt addError (exType, range) =
    let pushErr err = range |> addError err

    let isUdt (t: ResolvedType) =
        match t.Resolution with
        | QsTypeKind.UserDefinedType udt -> Some(processUdt pushErr udt)
        | _ -> None

    VerifyIsOneOf isUdt (ErrorCode.ExpectingUserDefinedType, [ exType |> toString ]) addError (exType, range)

/// Verifies that the given resolved type is indeed of kind Bool,
/// adding an ExpectingBoolExpr error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
let internal VerifyIsBoolean (inference: InferenceContext) diagnose (exType, range) =
    inference.Unify(exType, ResolvedType.New Bool) |> diagnoseWithRange range diagnose

/// Verifies that both given resolved types are of kind Bool,
/// adding an ExpectingBoolExpr error with the corresponding range using addError otherwise.
/// If one of the given types is a missing type, also adds the corresponding ExpressionOfUnknownType error(s).
let private VerifyAreBooleans inference diagnose (lhsType, lhsRange) (rhsType, rhsRange) =
    VerifyIsBoolean inference diagnose (lhsType, lhsRange)
    VerifyIsBoolean inference diagnose (rhsType, rhsRange)

/// Verifies that the given resolved type is indeed of kind Int,
/// adding an ExpectingIntExpr error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
let internal VerifyIsInteger (inference: InferenceContext) diagnose (exType, range) =
    inference.Unify(exType, ResolvedType.New Int) |> diagnoseWithRange range diagnose

/// Verifies that both given resolved types are of kind Int,
/// adding an ExpectingIntExpr error with the corresponding range using addError otherwise.
/// If one of the given types is a missing type, also adds the corresponding ExpressionOfUnknownType error(s).
let private VerifyAreIntegers inference diagnose (lhsType, lhsRange) (rhsType, rhsRange) =
    VerifyIsInteger inference diagnose (lhsType, lhsRange)
    VerifyIsInteger inference diagnose (rhsType, rhsRange)

/// Verifies that the given resolved type is indeed of kind Int or BigInt,
/// adding an ExpectingIntegralExpr error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
let internal VerifyIsIntegral addError (exType, range) =
    let expectedInt (t: ResolvedType) =
        if t.Resolution = Int || t.Resolution = BigInt
        then Some t
        else None

    VerifyIsOneOf expectedInt (ErrorCode.ExpectingIntegralExpr, [ exType |> toString ]) addError (exType, range)

/// Verifies that both given resolved types are of kind Int or BigInt, and that both are the same,
/// adding an ArgumentMismatchInBinaryOp or ExpectingIntegralExpr error with the corresponding range using addError otherwise.
/// If one of the given types is a missing type, also adds the corresponding ExpressionOfUnknownType error(s).
let private VerifyIntegralOp parent
                             (inference: InferenceContext)
                             diagnose
                             ((lhsType: ResolvedType), lhsRange)
                             (rhsType: ResolvedType, rhsRange)
                             =
    let exType, diagnostics = inference.Intersect(lhsType, rhsType, Supertype)
    diagnostics |> diagnoseWithRange (Range.Span lhsRange rhsRange) diagnose
    inference.Constrain(exType, Integral) |> diagnoseWithRange (Range.Span lhsRange rhsRange) diagnose
    exType

/// Verifies that the given resolved type indeed supports arithmetic operations,
/// adding an InvalidTypeInArithmeticExpr error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
/// Returns the type of the arithmetic expression.
let private VerifySupportsArithmetic addError (exType, range) =
    let expected (t: ResolvedType) = t.supportsArithmetic
    VerifyIsOneOf expected (ErrorCode.InvalidTypeInArithmeticExpr, [ exType |> toString ]) addError (exType, range)

/// Verifies that given resolved types can be used within a binary arithmetic operator.
/// First tries to find a common base type for the two types,
/// adding an ArgumentMismatchInBinaryOp error for the corresponding range(s) using addError if no common base type can be found.
/// If a common base type exists, verifies that this base type supports arithmetic operations,
/// adding the corresponding error otherwise.
/// If one of the given types is a missing type, also adds the corresponding ExpressionOfUnknownType error(s).
/// Returns the type of the arithmetic expression (i.e. the found base type).
let private VerifyArithmeticOp parent
                               (inference: InferenceContext)
                               diagnose
                               (lhsType: ResolvedType, lhsRange)
                               (rhsType: ResolvedType, rhsRange)
                               =
    let exType, diagnostics = inference.Intersect(lhsType, rhsType, Supertype)
    diagnostics |> diagnoseWithRange (Range.Span lhsRange rhsRange) diagnose
    inference.Constrain(exType, Numeric) |> diagnoseWithRange (Range.Span lhsRange rhsRange) diagnose
    exType

/// Verifies that the given resolved type indeed supports iteration,
/// adding an ExpectingIterableExpr error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
/// NOTE: returns the type of the iteration *item*.
let internal VerifyIsIterable (inference: InferenceContext) diagnose (exType, range) =
    // let expected (t: ResolvedType) = t.supportsIteration
    // VerifyIsOneOf expected (ErrorCode.ExpectingIterableExpr, [ exType |> toString ]) addError (exType, range)

    let item = inference.Fresh()
    inference.Constrain(exType, Iterable item) |> diagnoseWithRange range diagnose
    item

/// Verifies that given resolved types can be used within a concatenation operator.
/// First tries to find a common base type for the two types,
/// adding an ArgumentMismatchInBinaryOp error for the corresponding range(s) using addError if no common base type can be found.
/// If a common base type exists, verifies that this base type supports concatenation,
/// adding the corresponding error otherwise.
/// If one of the given types is a missing type, also adds the corresponding ExpressionOfUnknownType error(s).
/// Returns the type of the concatenation expression (i.e. the found base type).
let private VerifyConcatenation parent
                                (inference: InferenceContext)
                                diagnose
                                (lhsType: ResolvedType, lhsRange)
                                (rhsType: ResolvedType, rhsRange)
                                =
    let exType, diagnostics = inference.Intersect(lhsType, rhsType, Supertype)
    diagnostics |> diagnoseWithRange (Range.Span lhsRange rhsRange) diagnose
    inference.Constrain(exType, Semigroup) |> diagnoseWithRange (Range.Span lhsRange rhsRange) diagnose

    // let expected (t: ResolvedType) = t.supportsConcatenation
    // VerifyIsOneOf expected (ErrorCode.InvalidTypeForConcatenation, [ exType |> toString ]) addError (exType, rhsRange)
    exType

/// Verifies that given resolved types can be used within an equality comparison expression.
/// First tries to find a common base type for the two types,
/// adding an ArgumentMismatchInBinaryOp error for the corresponding range(s) using addError if no common base type can be found.
/// If a common base type exists, verifies that this base type supports equality comparison,
/// adding the corresponding error otherwise.
/// If one of the given types is a missing type, also adds the corresponding ExpressionOfUnknownType error(s).
let private VerifyEqualityComparison context diagnose (lhsType, lhsRange) (rhsType, rhsRange) =
    let exType, diagnostics = context.Inference.Intersect(lhsType, rhsType, Supertype)
    diagnostics |> diagnoseWithRange (Range.Span lhsRange rhsRange) diagnose

    context.Inference.Constrain(exType, Equatable)
    |> diagnoseWithRange (Range.Span lhsRange rhsRange) diagnose

/// Given a list of all item types and there corresponding ranges, verifies that a value array literal can be built from them.
/// Adds a MissingExprInArray error with the corresponding range using addError if one of the given types is missing.
/// Filtering all missing or invalid types, tries to find a common base type for the remaining item types,
/// and adds a MultipleTypesInArray error for the entire array if this fails.
/// Returns the inferred type of the array.
/// Returns an array with missing base type if the given list of item types is empty.
let private VerifyValueArray parent (inference: InferenceContext) diagnose (content, range) =
    content
    |> List.iter (fun (t: ResolvedType, r) ->
        if t.isMissing
        then QsCompilerDiagnostic.Error (ErrorCode.MissingExprInArray, []) r |> diagnose)

    let invalidOrMissing (t: ResolvedType) = t.isInvalid || t.isMissing

    match content |> List.map fst |> List.filter (invalidOrMissing >> not) |> List.distinct with
    | [] when List.isEmpty content -> inference.Fresh()
    | [] -> ResolvedType.New InvalidType |> ArrayType |> ResolvedType.New
    | types ->
        let commonType =
            types
            |> List.reduce (fun left right ->
                let t, ds = inference.Intersect(left, right, Supertype)
                ds |> List.iter diagnose
                t)

        ArrayType commonType |> ResolvedType.New

/// Verifies that the given resolved type supports numbered item access,
/// adding an ItemAccessForNonArray error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
let internal VerifyNumberedItemAccess addError (exType, range) =
    let expectedArray (t: ResolvedType) =
        match t.Resolution with
        | ArrayType _ -> Some t
        | _ -> None

    VerifyIsOneOf expectedArray (ErrorCode.ItemAccessForNonArray, [ exType |> toString ]) addError (exType, range)

/// Verifies that the given type of the left hand side of an array item expression is indeed an array type (or invalid),
/// adding an ItemAccessForNonArray error with the corresponding range using addError otherwise.
/// Verifies that the given type of the expression within the item access is either of type Int or Range,
/// adding an InvalidArrayItemIndex error with the corresponding range using addError otherwise.
/// Returns the type of the array item expression.
let private VerifyArrayItem (inference: InferenceContext)
                            diagnose
                            (arrType: ResolvedType, arrRange)
                            (indexType: ResolvedType, indexRange)
                            =
    let itemType = inference.Fresh()
    inference.Constrain(arrType, Indexed(indexType, itemType)) |> diagnoseWithRange arrRange diagnose
    itemType

/// Verifies that the given functor can be applied to an expression of the given type,
/// adding an error with the given error code and range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
/// Returns the type of the functor application expression.
let private VerifyFunctorApplication functor errCode addError (ex: ResolvedType, range) =
    let opSupportingFunctor (t: ResolvedType) =
        match t.Resolution with
        | QsTypeKind.Operation (_, info) when info.Characteristics.AreInvalid -> Some t
        | QsTypeKind.Operation (_, info) ->
            match info.Characteristics.SupportedFunctors with
            | Value functors when functors.Contains functor -> Some t
            | _ -> None
        | _ -> None

    VerifyIsOneOf opSupportingFunctor errCode addError (ex, range)

/// Verifies that the Adjoint functor can be applied to an expression of the given type,
/// adding an InvalidAdjointApplication error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
/// Returns the type of the functor application expression.
let private VerifyAdjointApplication (inference: InferenceContext) diagnose (resolvedType, range) =
    inference.Constrain(resolvedType, Constraint.Adjointable) |> diagnoseWithRange range diagnose
    resolvedType

/// Verifies that the Controlled functor can be applied to an expression of the given type,
/// adding an InvalidControlledApplication error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
/// Returns the type of the functor application expression.
let private VerifyControlledApplication (inference: InferenceContext) diagnose (resolvedType, range) =
    let controlled = inference.Fresh()

    inference.Constrain(resolvedType, Constraint.Controllable controlled)
    |> diagnoseWithRange range diagnose

    controlled

// utils for verifying identifiers, call expressions, and resolving type parameters

let private replaceTypes (resolutions: ImmutableDictionary<_, ResolvedType>) =
    { new TypeTransformation() with
        member this.OnTypeParameter param =
            resolutions.TryGetValue((param.Origin, param.TypeName))
            |> tryOption
            |> Option.map (fun resolvedType -> resolvedType.Resolution)
            |> Option.defaultValue (TypeParameter param)
    }

/// Given a Q# symbol and  optionally its type arguments, builds the corresponding Identifier and its type arguments,
/// calling ResolveIdentifer and ResolveType on the given SymbolTracker respectively.
/// Upon construction of the typed expression, all type parameters in the identifier type are resolved to the non-missing type arguments,
/// leaving those for which the type argument is missing unchanged.
/// Calls addDiagnostics on all diagnostics generated during the resolution.
/// If the given type arguments are not null (even if it's empty), but the identifier is not type parametrized,
/// adds a IdentifierCannotHaveTypeArguments error via addDiagnostic.
/// If the Identifier could potentially be type parameterized (even if the number of type parameters is null),
/// but the number of type arguments does not match the number of type parameters, adds a WrongNumberOfTypeArguments error via addDiagnostic.
/// Returns the resolved Identifer after type parameter resolution as typed expression.
let private VerifyIdentifier (inference: InferenceContext) diagnose (symbols: SymbolTracker) (sym, typeArgs) =
    let resolvedTargs =
        typeArgs
        |> QsNullable<_>
            .Map(fun (args: ImmutableArray<QsType>) ->
                args.Select(fun tArg ->
                    match tArg.Type with
                    | MissingType -> ResolvedType.New MissingType
                    | _ -> symbols.ResolveType diagnose tArg))
        |> QsNullable<_>.Map(fun args -> args.ToImmutableArray())

    let resId, typeParams = symbols.ResolveIdentifier diagnose sym
    let identifier, info = Identifier(resId.VariableName, resolvedTargs), resId.InferredInformation

    // resolve type parameters (if any) with the given type arguments
    // Note: type parameterized objects are never mutable - remember they are not the same as an identifier containing a template...!
    let invalidWithoutTargs mut =
        (identifier, ResolvedType.New InvalidType, info.HasLocalQuantumDependency, sym.Range)
        |> ExprWithoutTypeArgs mut

    match resId.VariableName, resolvedTargs with
    | InvalidIdentifier, Null -> invalidWithoutTargs true
    | InvalidIdentifier, Value _ -> invalidWithoutTargs false
    | LocalVariable _, Null ->
        (identifier, resId.Type, info.HasLocalQuantumDependency, sym.Range)
        |> ExprWithoutTypeArgs info.IsMutable
    | LocalVariable _, Value _ ->
        sym.RangeOrDefault
        |> QsCompilerDiagnostic.Error(ErrorCode.IdentifierCannotHaveTypeArguments, [])
        |> diagnose

        invalidWithoutTargs false
    | GlobalCallable _, Value res when res.Length <> typeParams.Length ->
        sym.RangeOrDefault
        |> QsCompilerDiagnostic.Error(ErrorCode.WrongNumberOfTypeArguments, [ typeParams.Length.ToString() ])
        |> diagnose

        invalidWithoutTargs false
    | GlobalCallable name, _ ->
        let typeParams =
            typeParams
            |> Seq.choose (function
                | ValidName param -> Some(name, param)
                | InvalidName -> None)

        let typeArgs = resolvedTargs |> QsNullable.defaultValue ImmutableArray.Empty

        let resolutions =
            typeParams
            |> Seq.mapi (fun i param ->
                if i < typeArgs.Length && typeArgs.[i].Resolution <> MissingType
                then KeyValuePair(param, typeArgs.[i])
                else KeyValuePair(param, inference.Fresh()))
            |> ImmutableDictionary.CreateRange

        let resolvedType =
            ((replaceTypes resolutions).OnType resId.Type).Resolution |> inference.Resolve |> ResolvedType.New

        let identifier =
            if QsNullable.isValue resolvedTargs
            then Identifier(GlobalCallable name, ImmutableArray.CreateRange resolutions.Values |> Value)
            else identifier

        let exInfo = InferredExpressionInformation.New(isMutable = false, quantumDep = info.HasLocalQuantumDependency)
        TypedExpression.New(identifier, resolutions, resolvedType, exInfo, sym.Range)

/// Verifies whether an expression of the given argument type can be used as argument to a method (function, operation, or setter)
/// that expects an argument of the given target type. The given target type may contain a missing type (valid for a setter).
/// Accumulates and returns an array with error codes for the cases where this is not the case, and returns an empty array otherwise.
/// Note that MissingTypes in the argument type should not occur aside from possibly as array base type of the expression.
/// A missing type in the given argument type will cause a verification failure in QsCompilerError.
/// For each type parameter in the target type, calls addTypeParameterResolution with a tuple of the type parameter and the type that is substituted for it.
/// IMPORTANT: The consistent (i.e. non-ambiguous and non-constraining) resolution of type parameters is *not* verified by this routine
/// and needs to be verified in a separate step!
let internal TypeMatchArgument (inference: InferenceContext) addTypeParameterResolution targetType argType =
    inference.Unify(argType, targetType)

/// Returns the type of the expression that completes the argument
/// (i.e. the expected type for the expression that completes all missing pieces in the argument) as option,
/// as well a a look-up for type parameters that are resolved by the given argument.
/// Returning None for the completing expression type indicates that no expressions are missing for the call.
/// Returning an invalid type as Some indicates that either the type of the given argument is
/// incompatible with the targetType, or that the targetType itself is invalid,
/// and no conclusion can be reached on the type for the unresolved part of the argument.
let private IsValidArgument (inference: InferenceContext) diagnose targetType (arg, resolveInner) =
    let buildType (tItems: ResolvedType option list) =
        let remaining = tItems |> List.choose id
        let containsInvalid = remaining |> List.exists (fun x -> x.isInvalid)
        let containsMissing = remaining |> List.exists (fun x -> x.isMissing)
        QsCompilerError.Verify(not containsMissing, "missing type in remaining input type")

        if containsInvalid then
            ResolvedType.New InvalidType |> Some
        else
            match remaining with
            | [] -> None
            | [ t ] -> Some t
            | _ -> TupleType(remaining.ToImmutableArray()) |> ResolvedType.New |> Some

    let lookUp = new List<(QsQualifiedName * string) * (ResolvedType * Range)>()
    let addTpResolution range (tp, exT) = lookUp.Add(tp, (exT, range))

    let rec recur (targetT: ResolvedType, argEx: QsExpression) =
        let pushErrs errCodes =
            for code in errCodes do
                QsCompilerDiagnostic.Error code argEx.RangeOrDefault |> diagnose

        QsCompilerError.Verify(not targetT.isMissing, "target type is missing")

        match targetT, argEx with
        | _, _ when targetT.isInvalid || targetT.isMissing -> ResolvedType.New InvalidType |> Some
        | _, Missing -> targetT |> Some
        | Tuple ts, Tuple exs when ts.Length <> exs.Length ->
            [|
                (ErrorCode.ArgumentTupleShapeMismatch, [ resolveInner argEx |> toString; targetT |> toString ])
            |]
            |> pushErrs

            ResolvedType.New InvalidType |> Some
        | Tuple ts, Tuple exs when ts.Length = exs.Length -> List.zip ts exs |> List.map recur |> buildType
        | Item t, Tuple _ when not (t: ResolvedType).isTypeParameter ->
            [| (ErrorCode.UnexpectedTupleArgument, [ targetT |> toString ]) |] |> pushErrs
            ResolvedType.New InvalidType |> Some
        | _, _ ->
            TypeMatchArgument inference (addTpResolution argEx.RangeOrDefault) targetT (resolveInner argEx)
            |> diagnoseWithRange argEx.RangeOrDefault diagnose

            None

    recur (targetType, arg), lookUp.ToLookup(fst, snd)

/// Returns true if the given expression is Some and contains an identifier
/// that is not part of a call-like expression and refers to the given parent.
/// Returns false otherwise.
let internal IsTypeParamRecursion (parent, definedTypeParams: ImmutableArray<_>) ex =
    let paramSelf =
        function
        | Identifier (GlobalCallable id, Null) -> id = parent && definedTypeParams.Length <> 0
        | Identifier (GlobalCallable id, Value tArgs) -> id = parent && tArgs.Contains(MissingType |> ResolvedType.New)
        | _ -> false

    ex
    |> Option.exists (fun kind ->
        let typedEx = AutoGeneratedExpression kind InvalidType false // the expression type is irrelevant

        let nonCallSubexpressions =
            typedEx.Extract(function
                | CallLikeExpression _ -> InvalidExpr
                | exKind -> exKind)

        nonCallSubexpressions |> Seq.exists (fun ex -> ex.Expression |> paramSelf))

/// Verifies that an expression of the given rhsType, used within the given parent (i.e. specialization declaration),
/// can be used when an expression of expectedType is expected by callaing TypeMatchArgument.
/// Generates an error with the given error code mismatchErr for the given range if this is not the case.
/// Verifies that any internal type parameters are "matched" only with themselves (or with an invalid type),
/// and generates a ConstrainsTypeParameter error if this is not the case.
/// If the given rhsEx is Some value, verifies whether it contains an identifier referring to the parent
/// that is not part of a call-like expression but does not specify all needed type arguments.
/// Calls the given function addError on all generated errors.
/// IMPORTANT: ignores any external type parameter occuring in expectedType without raising an error!
let internal VerifyAssignment (inference: InferenceContext)
                              expectedType
                              (parent, definedTypeParams)
                              mismatchErr
                              addError
                              (rhsType, rhsEx, rhsRange)
                              =
    // we need to check if the right hand side contains a type parametrized version of the parent callable
//    let directRecursion = IsTypeParamRecursion (parent, definedTypeParams) rhsEx

    //    if directRecursion
//    then rhsRange |> addError (ErrorCode.InvalidUseOfTypeParameterizedObject, [])
    // we need to check if all type parameters are consistently resolved
    let tpResolutions = new List<(QsQualifiedName * string) * ResolvedType>()

    let addTpResolution (key, exT) =
        // we can ignoring external type parameters,
        // since for a set-statement these can only occur if either the lhs can either not be set or has been assigned previously
        // and for a return statement the expected return type cannot contain external type parameters by construction
        if fst key = parent then tpResolutions.Add(key, exT)

    let errCodes = TypeMatchArgument inference addTpResolution expectedType rhsType

    if errCodes.Length <> 0 then
        let resolvedRhs = inference.Resolve rhsType.Resolution |> ResolvedType.New
        addError (mismatchErr, [ toString resolvedRhs; toString expectedType ]) rhsRange

    let containsNonTrivialResolution (tp: IGrouping<_, ResolvedType>) =
        let notResolvedToItself (x: ResolvedType) =
            match x.Resolution with
            | TypeParameter p -> p.Origin <> fst tp.Key || p.TypeName <> snd tp.Key
            | _ -> not x.isInvalid

        tp |> Seq.exists notResolvedToItself

    let nonTrivialResolutions =
        tpResolutions.ToLookup(fst, snd).Where containsNonTrivialResolution
        |> Seq.map (fun g ->
            QsTypeParameter.New(fst g.Key, snd g.Key, Null) |> TypeParameter |> ResolvedType.New |> toString)
        |> Seq.toList

    if nonTrivialResolutions.Any() then
        rhsRange
        |> addError (ErrorCode.ConstrainsTypeParameter, [ String.Join(", ", nonTrivialResolutions) ])


// utils for building TypedExpressions from QsExpressions

type QsExpression with

    /// Given a SymbolTracker containing all the symbols which are currently defined,
    /// recursively computes the corresponding typed expression for a Q# expression.
    /// Calls addDiagnostic on each diagnostic generated during the resolution.
    /// Returns the computed typed expression.
    member this.Resolve ({ Symbols = symbols } as context) addDiagnostic: TypedExpression =

        /// Calls Resolve on the given Q# expression.
        let InnerExpression (item: QsExpression) = item.Resolve context addDiagnostic
        /// Builds a QsCompilerDiagnostic with the given error code and range.
        let addError code range =
            range |> QsCompilerDiagnostic.Error code |> addDiagnostic
        /// Builds a QsCompilerDiagnostic with the given warning code and range.
        let addWarning code range =
            range |> QsCompilerDiagnostic.Warning code |> addDiagnostic

        /// Given and expression used for array slicing, as well as the type of the sliced expression,
        /// generates suitable boundaries for open ended ranges and returns the resolved slicing expression as Some.
        /// Returns None if the slicing expression is trivial, i.e. if the sliced array does not deviate from the orginal one.
        /// NOTE: Does *not* generated any diagnostics related to the given type for the array to slice.
        let resolveSlicing (resolvedArr: TypedExpression) (idx: QsExpression) =
            let invalidRangeDelimiter =
                (InvalidExpr,
                 ResolvedType.New InvalidType,
                 resolvedArr.InferredInformation.HasLocalQuantumDependency,
                 Null)
                |> ExprWithoutTypeArgs false

            let validSlicing (step: TypedExpression option) =
                match resolvedArr.ResolvedType.Resolution with
                | ArrayType _ -> step.IsNone || step.Value.ResolvedType.Resolution = Int
                | _ -> false

            let ConditionalIntExpr (cond: TypedExpression, ifTrue: TypedExpression, ifFalse: TypedExpression) =
                let quantumDep =
                    [ cond; ifTrue; ifFalse ]
                    |> List.exists (fun ex -> ex.InferredInformation.HasLocalQuantumDependency)

                (CONDITIONAL(cond, ifTrue, ifFalse), Int |> ResolvedType.New, quantumDep, Null)
                |> ExprWithoutTypeArgs false

            let OpenStartInSlicing =
                function
                | Some step when validSlicing (Some step) ->
                    ConditionalIntExpr(IsNegative step, LengthMinusOne resolvedArr, SyntaxGenerator.IntLiteral 0L)
                | _ -> SyntaxGenerator.IntLiteral 0L

            let OpenEndInSlicing =
                function
                | Some step when validSlicing (Some step) ->
                    ConditionalIntExpr(IsNegative step, SyntaxGenerator.IntLiteral 0L, LengthMinusOne resolvedArr)
                | ex -> if validSlicing ex then LengthMinusOne resolvedArr else invalidRangeDelimiter

            let resolveSlicingRange (rstart, rstep, rend) =
                let integerExpr ex =
                    let resolved = InnerExpression ex
                    VerifyIsInteger context.Inference addDiagnostic (resolved.ResolvedType, ex.RangeOrDefault)
                    resolved

                let resolvedStep = rstep |> Option.map integerExpr

                let resolveWith build (ex: QsExpression) =
                    if ex.isMissing then build resolvedStep else integerExpr ex

                let resolvedStart, resolvedEnd =
                    rstart |> resolveWith OpenStartInSlicing, rend |> resolveWith OpenEndInSlicing

                match resolvedStep with
                | Some resolvedStep ->
                    SyntaxGenerator.RangeLiteral(SyntaxGenerator.RangeLiteral(resolvedStart, resolvedStep), resolvedEnd)
                | None -> SyntaxGenerator.RangeLiteral(resolvedStart, resolvedEnd)

            match idx.Expression with
            | RangeLiteral (lhs, rhs) when lhs.isMissing && rhs.isMissing -> None // case arr[...]
            | RangeLiteral (lhs, rend) ->
                lhs.Expression
                |> (Some
                    << function
                    | RangeLiteral (rstart, rstep) -> resolveSlicingRange (rstart, Some rstep, rend) // cases arr[...step..ex], arr[ex..step...], arr[ex1..step..ex2], and arr[...ex...]
                    | _ -> resolveSlicingRange (lhs, None, rend)) // case arr[...ex], arr[ex...] and arr[ex1..ex2]
            | _ -> InnerExpression idx |> Some // case arr[ex]


        /// Resolves and verifies the interpolated expressions, and returns the StringLiteral as typed expression.
        let buildStringLiteral (literal, interpolated: IEnumerable<_>) =
            let resInterpol = (interpolated.Select InnerExpression).ToImmutableArray()
            let localQdependency = resInterpol |> Seq.exists (fun r -> r.InferredInformation.HasLocalQuantumDependency)

            (StringLiteral(literal, resInterpol), String |> ResolvedType.New, localQdependency, this.Range)
            |> ExprWithoutTypeArgs false

        /// <summary>
        /// Resolves and verifies all given items, and returns the corresponding ValueTuple as typed expression.
        /// If the ValueTuple contains only one item, the item is returned instead (i.e. arity-1 tuple expressions are stripped).
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="items"/> is empty.</exception>
        let buildTuple (items: ImmutableArray<_>) =
            let resolvedItems = (items.Select InnerExpression).ToImmutableArray()
            let resolvedTypes = (resolvedItems |> Seq.map (fun x -> x.ResolvedType)).ToImmutableArray()

            let localQdependency =
                resolvedItems |> Seq.exists (fun item -> item.InferredInformation.HasLocalQuantumDependency)

            if resolvedItems.Length = 0 then
                ArgumentException "tuple expression requires at least one tuple item" |> raise
            elif resolvedItems.Length = 1 then
                resolvedItems.[0]
            else
                (ValueTuple resolvedItems, TupleType resolvedTypes |> ResolvedType.New, localQdependency, this.Range)
                |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given array base type and the expression denoting the length of the array,
        /// and returns the corrsponding NewArray expression as typed expression
        let buildNewArray (bType, ex: QsExpression) =
            let resolvedEx = InnerExpression ex
            VerifyIsInteger context.Inference addDiagnostic (resolvedEx.ResolvedType, ex.RangeOrDefault)
            let resolvedBase = symbols.ResolveType addDiagnostic bType
            let arrType = resolvedBase |> StripPositionInfo.Apply |> ArrayType |> ResolvedType.New
            let quantumDep = resolvedEx.InferredInformation.HasLocalQuantumDependency
            (NewArray(resolvedBase, resolvedEx), arrType, quantumDep, this.Range) |> ExprWithoutTypeArgs false

        /// Resolves and verifies all given items of a value array literal, and returns the corresponding ValueArray as typed expression.
        let buildValueArray (values: ImmutableArray<_>) =
            let positioned =
                values.Select(fun ex -> InnerExpression ex, ex.RangeOrDefault)
                |> Seq.toList
                |> List.map (fun (ex, r) -> ex, (ex.ResolvedType, r))

            let resolvedType =
                positioned
                |> List.map snd
                |> fun vals ->
                    VerifyValueArray symbols.Parent context.Inference addDiagnostic (vals, this.RangeOrDefault)

            let resolvedValues = (positioned |> List.map fst).ToImmutableArray()

            let localQdependency =
                resolvedValues |> Seq.exists (fun item -> item.InferredInformation.HasLocalQuantumDependency)

            (ValueArray resolvedValues, resolvedType, localQdependency, this.Range) |> ExprWithoutTypeArgs false

        /// Resolves and verifies the sized array constructor expression and returns it as a typed expression.
        let buildSizedArray value (size: QsExpression) =
            let value = InnerExpression value
            let arrayType = ArrayType value.ResolvedType |> ResolvedType.New

            let sizeRange = size.RangeOrDefault
            let size = InnerExpression size
            VerifyIsInteger context.Inference addDiagnostic (size.ResolvedType, sizeRange)

            let quantumDependency =
                value.InferredInformation.HasLocalQuantumDependency
                || size.InferredInformation.HasLocalQuantumDependency

            (SizedArray(value, size), arrayType, quantumDependency, this.Range) |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given array expression and index expression of an array item access expression,
        /// and returns the corresponding ArrayItem expression as typed expression.
        let buildArrayItem (arr, idx: QsExpression) =
            let resolvedArr = InnerExpression arr

            match resolveSlicing resolvedArr idx with
            | None ->
                { resolvedArr with
                    ResolvedType = VerifyNumberedItemAccess addError (resolvedArr.ResolvedType, arr.RangeOrDefault)
                }
            | Some resolvedIdx ->
                let resolvedType =
                    VerifyArrayItem
                        context.Inference
                        addDiagnostic
                        (resolvedArr.ResolvedType, arr.RangeOrDefault)
                        (resolvedIdx.ResolvedType, idx.RangeOrDefault)

                let localQdependency =
                    resolvedArr.InferredInformation.HasLocalQuantumDependency
                    || resolvedIdx.InferredInformation.HasLocalQuantumDependency

                (ArrayItem(resolvedArr, resolvedIdx), resolvedType, localQdependency, this.Range)
                |> ExprWithoutTypeArgs false

        /// Given a symbol used to represent an item name in an item access or update expression,
        /// returns the an identifier that can be used to represent the corresponding item name.
        /// Adds an error if the given symbol is not either invalid or an unqualified symbol.
        let buildItemName (sym: QsSymbol) =
            match sym.Symbol with
            | InvalidSymbol -> InvalidIdentifier
            | Symbol name -> LocalVariable name
            | _ ->
                sym.RangeOrDefault |> addError (ErrorCode.ExpectingItemName, [])
                InvalidIdentifier

        /// Resolves and verifies the given expression and item name of a named item access expression,
        /// and returns the corresponding NamedItem expression as typed expression.
        let buildNamedItem (ex, acc: QsSymbol) =
            let resolvedEx = InnerExpression ex
            let itemName = acc |> buildItemName
            // TODO: Eager resolution.
            let udtType = context.Inference.Resolve resolvedEx.ResolvedType.Resolution |> ResolvedType.New
            let exType = VerifyUdtWith (symbols.GetItemType itemName) addError (udtType, ex.RangeOrDefault)
            let localQdependency = resolvedEx.InferredInformation.HasLocalQuantumDependency
            (NamedItem(resolvedEx, itemName), exType, localQdependency, this.Range) |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given left hand side, access expression, and right hand side of a copy-and-update expression,
        /// and returns the corresponding copy-and-update expression as typed expression.
        let buildCopyAndUpdate (lhs: QsExpression, accEx: QsExpression, rhs: QsExpression) =
            let resLhs, resRhs = InnerExpression lhs, InnerExpression rhs
            // TODO: Eager resolution.
            let lhsType = context.Inference.Resolve resLhs.ResolvedType.Resolution |> ResolvedType.New
            let resLhs = { resLhs with ResolvedType = lhsType }

            let resolvedCopyAndUpdateExpr resAccEx =
                let localQdependency =
                    [ resLhs; resAccEx; resRhs ]
                    |> Seq.map (fun ex -> ex.InferredInformation.HasLocalQuantumDependency)
                    |> Seq.contains true

                (CopyAndUpdate(resLhs, resAccEx, resRhs), resLhs.ResolvedType, localQdependency, this.Range)
                |> ExprWithoutTypeArgs false

            let parent = symbols.Parent, symbols.DefinedTypeParameters

            match (resLhs.ResolvedType.Resolution, accEx.Expression) with
            | UserDefinedType _, Identifier (sym, Null) ->
                let itemName = sym |> buildItemName

                let itemType =
                    VerifyUdtWith (symbols.GetItemType itemName) addError (resLhs.ResolvedType, lhs.RangeOrDefault)

                VerifyAssignment
                    context.Inference
                    itemType
                    parent
                    ErrorCode.TypeMismatchInCopyAndUpdateExpr
                    addError
                    (resRhs.ResolvedType, None, rhs.RangeOrDefault)

                let resAccEx =
                    (Identifier(itemName, Null),
                     itemType,
                     resLhs.InferredInformation.HasLocalQuantumDependency,
                     sym.Range)
                    |> ExprWithoutTypeArgs false

                resAccEx |> resolvedCopyAndUpdateExpr
            | _ -> // by default, assume that the update expression is supposed to be for an array
                match resolveSlicing resLhs accEx with
                | None -> // indicates a trivial slicing of the form "..." resulting in a complete replacement
                    let expectedRhs = VerifyNumberedItemAccess addError (resLhs.ResolvedType, lhs.RangeOrDefault)

                    VerifyAssignment
                        context.Inference
                        expectedRhs
                        parent
                        ErrorCode.TypeMismatchInCopyAndUpdateExpr
                        addError
                        (resRhs.ResolvedType, None, rhs.RangeOrDefault)

                    { resRhs with ResolvedType = expectedRhs }
                | Some resAccEx -> // indicates either a index or index range to update
                    let expectedRhs =
                        VerifyArrayItem
                            context.Inference
                            addDiagnostic
                            (resLhs.ResolvedType, lhs.RangeOrDefault)
                            (resAccEx.ResolvedType, accEx.RangeOrDefault)

                    VerifyAssignment
                        context.Inference
                        expectedRhs
                        parent
                        ErrorCode.TypeMismatchInCopyAndUpdateExpr
                        addError
                        (resRhs.ResolvedType, None, rhs.RangeOrDefault)

                    resAccEx |> resolvedCopyAndUpdateExpr

        /// Resolves and verifies the given left hand side and right hand side of a range operator,
        /// and returns the corresponding RANGE expression as typed expression.
        /// NOTE: handles both the case of a range with and without explicitly specified step size
        /// *under the assumption* that the range operator is left associative.
        let buildRange (lhs: QsExpression, rEnd: QsExpression) =
            let resRhs = InnerExpression rEnd
            VerifyIsInteger context.Inference addDiagnostic (resRhs.ResolvedType, rEnd.RangeOrDefault)

            let resLhs =
                match lhs.Expression with
                | RangeLiteral (rStart, rStep) ->
                    let (resStart, resStep) = (InnerExpression rStart, InnerExpression rStep)

                    VerifyAreIntegers
                        context.Inference
                        addDiagnostic
                        (resStart.ResolvedType, rStart.RangeOrDefault)
                        (resStep.ResolvedType, rStep.RangeOrDefault)

                    let localQdependency =
                        resStart.InferredInformation.HasLocalQuantumDependency
                        || resStep.InferredInformation.HasLocalQuantumDependency

                    (RangeLiteral(resStart, resStep), Range |> ResolvedType.New, localQdependency, this.Range)
                    |> ExprWithoutTypeArgs false
                | _ ->
                    InnerExpression lhs
                    |> (fun resStart ->
                        VerifyIsInteger context.Inference addDiagnostic (resStart.ResolvedType, lhs.RangeOrDefault)
                        resStart)

            let localQdependency =
                resLhs.InferredInformation.HasLocalQuantumDependency
                || resRhs.InferredInformation.HasLocalQuantumDependency

            (RangeLiteral(resLhs, resRhs), Range |> ResolvedType.New, localQdependency, this.Range)
            |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given expression with the given verification function,
        /// and returns the corresponding expression built with buildExprKind as typed expression.
        let verifyAndBuildWith buildExprKind verify (ex: QsExpression) =
            let resolvedEx = InnerExpression ex
            let exType = verify addDiagnostic (resolvedEx.ResolvedType, ex.RangeOrDefault)

            (buildExprKind resolvedEx, exType, resolvedEx.InferredInformation.HasLocalQuantumDependency, this.Range)
            |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given left hand side and right hand side of an arithmetic operator,
        /// and returns the corresponding expression built with buildExprKind as typed expression.
        let buildArithmeticOp buildExprKind (lhs, rhs) =
            let (resolvedLhs, resolvedRhs) = (InnerExpression lhs, InnerExpression rhs)

            let resolvedType =
                VerifyArithmeticOp
                    symbols.Parent
                    context.Inference
                    addDiagnostic
                    (resolvedLhs.ResolvedType, lhs.RangeOrDefault)
                    (resolvedRhs.ResolvedType, rhs.RangeOrDefault)

            let localQdependency =
                resolvedLhs.InferredInformation.HasLocalQuantumDependency
                || resolvedRhs.InferredInformation.HasLocalQuantumDependency

            (buildExprKind (resolvedLhs, resolvedRhs), resolvedType, localQdependency, this.Range)
            |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given left hand side and right hand side of an addition operator,
        /// and returns the corresponding ADD expression as typed expression.
        /// Note: ADD is used for both arithmetic expressions as well as concatenation expressions.
        /// If the resolved type of the given lhs supports concatenation, then the verification is done for a concatenation expression,
        /// and otherwise it is done for an arithmetic expression.
        let buildAddition (lhs, rhs) =
            let (resolvedLhs, resolvedRhs) = (InnerExpression lhs, InnerExpression rhs)

            let resolvedType =
                // Note: this relies on the lhs supporting concatenation if and only if all of its base types do,
                // and there being no type that supports both arithmetic and concatenation
                // if resolvedLhs.ResolvedType.supportsConcatenation.IsSome then
                VerifyConcatenation
                    symbols.Parent
                    context.Inference
                    addDiagnostic
                    (resolvedLhs.ResolvedType, lhs.RangeOrDefault)
                    (resolvedRhs.ResolvedType, rhs.RangeOrDefault)
            // else
            //    VerifyArithmeticOp
            //        symbols.Parent
            //        addError
            //        (resolvedLhs.ResolvedType, lhs.RangeOrDefault)
            //        (resolvedRhs.ResolvedType, rhs.RangeOrDefault)

            let localQdependency =
                resolvedLhs.InferredInformation.HasLocalQuantumDependency
                || resolvedRhs.InferredInformation.HasLocalQuantumDependency

            (ADD(resolvedLhs, resolvedRhs), resolvedType, localQdependency, this.Range)
            |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given left hand side and right hand side of a power operator,
        /// and returns the corresponding POW expression as typed expression.
        /// Note: POW can take two integers or two doubles, in which case the result is a double, or it can take a big
        /// integer and an integer, in which case the result is a big integer.
        let buildPower (lhs, rhs) =
            let (resolvedLhs, resolvedRhs) = (InnerExpression lhs, InnerExpression rhs)

            let resolvedType =
                if resolvedLhs.ResolvedType.Resolution = BigInt then
                    VerifyIsInteger context.Inference addDiagnostic (resolvedRhs.ResolvedType, rhs.RangeOrDefault)
                    resolvedLhs.ResolvedType
                else
                    VerifyArithmeticOp
                        symbols.Parent
                        context.Inference
                        addDiagnostic
                        (resolvedLhs.ResolvedType, lhs.RangeOrDefault)
                        (resolvedRhs.ResolvedType, rhs.RangeOrDefault)

            let localQdependency =
                resolvedLhs.InferredInformation.HasLocalQuantumDependency
                || resolvedRhs.InferredInformation.HasLocalQuantumDependency

            (POW(resolvedLhs, resolvedRhs), resolvedType, localQdependency, this.Range)
            |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given left hand side and right hand side of a binary integral operator,
        /// and returns the corresponding expression built with buildExprKind as typed expression of type Int or BigInt, as appropriate.
        let buildIntegralOp buildExprKind (lhs, rhs) =
            let (resolvedLhs, resolvedRhs) = (InnerExpression lhs, InnerExpression rhs)

            let resolvedType =
                VerifyIntegralOp
                    symbols.Parent
                    context.Inference
                    addDiagnostic
                    (resolvedLhs.ResolvedType, lhs.RangeOrDefault)
                    (resolvedRhs.ResolvedType, rhs.RangeOrDefault)

            let localQdependency =
                resolvedLhs.InferredInformation.HasLocalQuantumDependency
                || resolvedRhs.InferredInformation.HasLocalQuantumDependency

            (buildExprKind (resolvedLhs, resolvedRhs), resolvedType, localQdependency, this.Range)
            |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given left hand side and right hand side of a shift operator,
        /// and returns the corresponding expression built with buildExprKind as typed expression of type Int or BigInt, as appropriate.
        let buildShiftOp buildExprKind (lhs, rhs) =
            let (resolvedLhs, resolvedRhs) = (InnerExpression lhs, InnerExpression rhs)
            let resolvedType = VerifyIsIntegral addError (resolvedLhs.ResolvedType, lhs.RangeOrDefault)
            VerifyIsInteger context.Inference addDiagnostic (resolvedRhs.ResolvedType, rhs.RangeOrDefault)

            let localQdependency =
                resolvedLhs.InferredInformation.HasLocalQuantumDependency
                || resolvedRhs.InferredInformation.HasLocalQuantumDependency

            (buildExprKind (resolvedLhs, resolvedRhs), resolvedType, localQdependency, this.Range)
            |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given left hand side and right hand side of a binary boolean operator,
        /// and returns the corresponding expression built with buildExprKind as typed expression of type Bool.
        let buildBooleanOpWith verify shortCircuits buildExprKind (lhs, rhs) =
            let (resolvedLhs, resolvedRhs) = (InnerExpression lhs, InnerExpression rhs)

            if shortCircuits
            then VerifyConditionalExecution addWarning (resolvedRhs, rhs.RangeOrDefault)

            verify
                addDiagnostic
                (resolvedLhs.ResolvedType, lhs.RangeOrDefault)
                (resolvedRhs.ResolvedType, rhs.RangeOrDefault)

            let localQdependency =
                resolvedLhs.InferredInformation.HasLocalQuantumDependency
                || resolvedRhs.InferredInformation.HasLocalQuantumDependency

            (buildExprKind (resolvedLhs, resolvedRhs), Bool |> ResolvedType.New, localQdependency, this.Range)
            |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given condition, left hand side, and right hand side of a conditional expression (if-else-shorthand),
        /// and returns the corresponding conditional expression as typed expression.
        let buildConditional (cond: QsExpression, ifTrue: QsExpression, ifFalse: QsExpression) =
            let resCond, resIsTrue, resIsFalse = InnerExpression cond, InnerExpression ifTrue, InnerExpression ifFalse
            VerifyConditionalExecution addWarning (resIsTrue, ifTrue.RangeOrDefault)
            VerifyConditionalExecution addWarning (resIsFalse, ifFalse.RangeOrDefault)
            VerifyIsBoolean context.Inference addDiagnostic (resCond.ResolvedType, cond.RangeOrDefault)

            let exType = context.Inference.Fresh()

            context.Inference.Unify(resIsTrue.ResolvedType, exType)
            |> diagnoseWithRange ifTrue.RangeOrDefault addDiagnostic

            context.Inference.Unify(resIsFalse.ResolvedType, exType)
            |> diagnoseWithRange ifFalse.RangeOrDefault addDiagnostic

            let localQdependency =
                [ resCond; resIsTrue; resIsFalse ]
                |> Seq.map (fun ex -> ex.InferredInformation.HasLocalQuantumDependency)
                |> Seq.contains true

            (CONDITIONAL(resCond, resIsTrue, resIsFalse), exType, localQdependency, this.Range)
            |> ExprWithoutTypeArgs false

        /// Resolves the given expression and verifies that its type is indeed a user defined type.
        /// Determines the underlying type of the user defined type and returns the corresponding UNWRAP expression as typed expression of that type.
        let buildUnwrap (ex: QsExpression) =
            let resolvedEx = InnerExpression ex
            let exType = context.Inference.Fresh()

            context.Inference.Constrain(resolvedEx.ResolvedType, Wrapped exType)
            |> diagnoseWithRange ex.RangeOrDefault addDiagnostic

            (UnwrapApplication resolvedEx, exType, resolvedEx.InferredInformation.HasLocalQuantumDependency, this.Range)
            |> ExprWithoutTypeArgs false

        let rec partialArgType (argType: ResolvedType) =
            match argType.Resolution with
            | MissingType ->
                let param = context.Inference.Fresh()
                param, Some param
            | TupleType items ->
                let items, missing =
                    (items |> Seq.map partialArgType, ([], []))
                    ||> Seq.foldBack (fun (item, params1) (items, params2) ->
                            item :: items, Option.toList params1 @ params2)

                let missing =
                    if List.isEmpty missing
                    then None
                    else ImmutableArray.CreateRange missing |> TupleType |> ResolvedType.New |> Some

                ImmutableArray.CreateRange items |> TupleType |> ResolvedType.New, missing
            | _ -> argType, None

        /// Resolves and verifies the given left hand side and right hand side of a call expression,
        /// and returns the corresponding expression as typed expression.
        let buildCall (callable: QsExpression, arg: QsExpression) =
            let resolvedCallable = InnerExpression callable
            let resolvedArg = InnerExpression arg
            let callExpression = CallLikeExpression(resolvedCallable, resolvedArg)
            let argType, partialType = partialArgType resolvedArg.ResolvedType
            let isPartial = Option.isSome partialType

            if not isPartial then
                context.Inference.Constrain
                    (resolvedCallable.ResolvedType, Set.ofSeq symbols.RequiredFunctorSupport |> CanGenerateFunctors)
                |> diagnoseWithRange callable.RangeOrDefault addDiagnostic

            let output = context.Inference.Fresh()

            if isPartial || context.IsInOperation then
                context.Inference.Constrain(resolvedCallable.ResolvedType, Callable(argType, output))
                |> diagnoseWithRange callable.RangeOrDefault addDiagnostic
            else
                // TODO: Better error message.
                context.Inference.Unify
                    (resolvedCallable.ResolvedType, QsTypeKind.Function(argType, output) |> ResolvedType.New)
                |> diagnoseWithRange callable.RangeOrDefault addDiagnostic

            let resultType =
                match partialType with
                | Some partial ->
                    let result = context.Inference.Fresh()

                    context.Inference.Constrain(resolvedCallable.ResolvedType, AppliesPartial(partial, result))
                    |> diagnoseWithRange arg.RangeOrDefault addDiagnostic

                    result
                | None -> output

            // Be pessimistic: if we don't know that the callable is a function at this point, assume it's an
            // operation.
            let isFunction =
                match context.Inference.Resolve resolvedCallable.ResolvedType.Resolution with
                | QsTypeKind.Function _ -> true
                | _ -> false

            let hasQuantumDependency =
                if isPartial || isFunction then
                    resolvedCallable.InferredInformation.HasLocalQuantumDependency
                    || resolvedArg.InferredInformation.HasLocalQuantumDependency
                else
                    true

            let info = InferredExpressionInformation.New(isMutable = false, quantumDep = hasQuantumDependency)
            TypedExpression.New(callExpression, resolvedCallable.TypeParameterResolutions, resultType, info, this.Range)

        match this.Expression with
        | InvalidExpr -> (InvalidExpr, InvalidType |> ResolvedType.New, false, this.Range) |> ExprWithoutTypeArgs true // choosing the more permissive option here
        | MissingExpr -> (MissingExpr, MissingType |> ResolvedType.New, false, this.Range) |> ExprWithoutTypeArgs false
        | UnitValue -> (UnitValue, UnitType |> ResolvedType.New, false, this.Range) |> ExprWithoutTypeArgs false
        | Identifier (sym, tArgs) -> VerifyIdentifier context.Inference addDiagnostic symbols (sym, tArgs)
        | CallLikeExpression (method, arg) -> buildCall (method, arg)
        | AdjointApplication ex -> verifyAndBuildWith AdjointApplication (VerifyAdjointApplication context.Inference) ex
        | ControlledApplication ex ->
            verifyAndBuildWith ControlledApplication (VerifyControlledApplication context.Inference) ex
        | UnwrapApplication ex -> buildUnwrap ex
        | ValueTuple items -> buildTuple items
        | ArrayItem (arr, idx) -> buildArrayItem (arr, idx)
        | NamedItem (ex, acc) -> buildNamedItem (ex, acc)
        | ValueArray values -> buildValueArray values
        | NewArray (baseType, ex) -> buildNewArray (baseType, ex)
        | SizedArray (value, size) -> buildSizedArray value size
        | IntLiteral i -> (IntLiteral i, Int |> ResolvedType.New, false, this.Range) |> ExprWithoutTypeArgs false
        | BigIntLiteral b ->
            (BigIntLiteral b, BigInt |> ResolvedType.New, false, this.Range) |> ExprWithoutTypeArgs false
        | DoubleLiteral d ->
            (DoubleLiteral d, Double |> ResolvedType.New, false, this.Range) |> ExprWithoutTypeArgs false
        | BoolLiteral b -> (BoolLiteral b, Bool |> ResolvedType.New, false, this.Range) |> ExprWithoutTypeArgs false
        | ResultLiteral r ->
            (ResultLiteral r, Result |> ResolvedType.New, false, this.Range) |> ExprWithoutTypeArgs false
        | PauliLiteral p -> (PauliLiteral p, Pauli |> ResolvedType.New, false, this.Range) |> ExprWithoutTypeArgs false
        | StringLiteral (s, exs) -> buildStringLiteral (s, exs)
        | RangeLiteral (lhs, rEnd) -> buildRange (lhs, rEnd)
        | CopyAndUpdate (lhs, accEx, rhs) -> buildCopyAndUpdate (lhs, accEx, rhs)
        | CONDITIONAL (cond, ifTrue, ifFalse) -> buildConditional (cond, ifTrue, ifFalse)
        | ADD (lhs, rhs) -> buildAddition (lhs, rhs) // addition takes a special role since it is used for both arithmetic and concatenation expressions
        | SUB (lhs, rhs) -> buildArithmeticOp SUB (lhs, rhs)
        | MUL (lhs, rhs) -> buildArithmeticOp MUL (lhs, rhs)
        | DIV (lhs, rhs) -> buildArithmeticOp DIV (lhs, rhs)
        | LT (lhs, rhs) ->
            buildBooleanOpWith (fun log l r -> VerifyArithmeticOp symbols.Parent context.Inference log l r |> ignore)
                false LT (lhs, rhs)
        | LTE (lhs, rhs) ->
            buildBooleanOpWith (fun log l r -> VerifyArithmeticOp symbols.Parent context.Inference log l r |> ignore)
                false LTE (lhs, rhs)
        | GT (lhs, rhs) ->
            buildBooleanOpWith (fun log l r -> VerifyArithmeticOp symbols.Parent context.Inference log l r |> ignore)
                false GT (lhs, rhs)
        | GTE (lhs, rhs) ->
            buildBooleanOpWith (fun log l r -> VerifyArithmeticOp symbols.Parent context.Inference log l r |> ignore)
                false GTE (lhs, rhs)
        | POW (lhs, rhs) -> buildPower (lhs, rhs) // power takes a special role because you can raise integers and doubles to integer and double powers, but bigint only to integer powers
        | MOD (lhs, rhs) -> buildIntegralOp MOD (lhs, rhs)
        | LSHIFT (lhs, rhs) -> buildShiftOp LSHIFT (lhs, rhs)
        | RSHIFT (lhs, rhs) -> buildShiftOp RSHIFT (lhs, rhs)
        | BOR (lhs, rhs) -> buildIntegralOp BOR (lhs, rhs)
        | BAND (lhs, rhs) -> buildIntegralOp BAND (lhs, rhs)
        | BXOR (lhs, rhs) -> buildIntegralOp BXOR (lhs, rhs)
        | AND (lhs, rhs) -> buildBooleanOpWith (VerifyAreBooleans context.Inference) true AND (lhs, rhs)
        | OR (lhs, rhs) -> buildBooleanOpWith (VerifyAreBooleans context.Inference) true OR (lhs, rhs)
        | EQ (lhs, rhs) -> buildBooleanOpWith (VerifyEqualityComparison context) false EQ (lhs, rhs)
        | NEQ (lhs, rhs) -> buildBooleanOpWith (VerifyEqualityComparison context) false NEQ (lhs, rhs)
        | NEG ex -> verifyAndBuildWith NEG (errorToDiagnostic VerifySupportsArithmetic) ex
        | BNOT ex -> verifyAndBuildWith BNOT (errorToDiagnostic VerifyIsIntegral) ex
        | NOT ex ->
            ex
            |> verifyAndBuildWith NOT (fun log arg ->
                   VerifyIsBoolean context.Inference log arg
                   ResolvedType.New Bool)
