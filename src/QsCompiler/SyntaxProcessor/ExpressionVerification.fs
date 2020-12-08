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
open Microsoft.Quantum.QsCompiler.ReservedKeywords.AssemblyConstants
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxGenerator
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.VerificationTools
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.TextProcessing.Keywords
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput


// utils for verifying types in expressions

type private StripInferredInfoFromType() =
    inherit TypeTransformationBase()

    default this.OnCallableInformation opInfo =
        let characteristics =
            this.OnCharacteristicsExpression opInfo.Characteristics

        CallableInformation.New(characteristics, InferredCallableInformation.NoInformation)

    override this.OnRangeInformation _ = Null

let private StripInferredInfoFromType = (new StripInferredInfoFromType()).OnType

/// used for type matching arguments in call-like expressions
type private Variance =
    | Covariant
    | Contravariant
    | Invariant

let private invalid = InvalidType |> ResolvedType.New

let private ExprWithoutTypeArgs isMutable (ex, t, dep, range) =
    let inferred =
        InferredExpressionInformation.New(isMutable = isMutable, quantumDep = dep)

    TypedExpression.New(ex, ImmutableDictionary.Empty, t, inferred, range)

let private missingFunctors (target: ImmutableHashSet<_>, given) =
    let mapFunctors fs =
        fs
        |> Seq.map (function
            | Adjoint -> qsAdjointFunctor.id
            | Controlled -> qsControlledFunctor.id)
        |> Seq.toList

    match given with
    | Some fList -> target.Except(fList) |> mapFunctors
    | None -> if target.Any() then target |> mapFunctors else [ "(None)" ]

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
let private CommonBaseType addError
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
            variance
            |> function
            | Covariant -> Contravariant
            | Contravariant -> Covariant
            | Invariant -> Invariant

        let argType = matchTypes inputVariance (i1, i2) // variance changes for the argument type *only*
        let resType = matchTypes variance (o1, o2)
        argType, resType

    and commonOpType variance ((i1, o1), s1: CallableInformation) ((i2, o2), s2: CallableInformation) =
        let argType, resType =
            matchInAndOutputType variance (i1, o1) (i2, o2)

        let characteristics =
            variance
            |> function
            | Covariant -> CallableInformation.Common [ s1; s2 ]
            | Contravariant -> // no information can ever be inferred in this case, since contravariance only occurs within the type signatures of passed callables
                CallableInformation.New
                    (Union(s1.Characteristics, s2.Characteristics) |> ResolvedCharacteristics.New,
                     InferredCallableInformation.NoInformation)
            | Invariant when s1.Characteristics.AreInvalid
                             || s2.Characteristics.AreInvalid
                             || s1
                                 .Characteristics
                                 .GetProperties()
                                 .SetEquals(s2.Characteristics.GetProperties()) ->
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
            (Seq.zip ts1 ts2 |> Seq.map (matchTypes variance))
                .ToImmutableArray()
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

    matchTypes Covariant (lhsType, rhsType)

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
    | None when exType.isInvalid -> invalid
    | None when exType.isMissing ->
        range |> addError (ErrorCode.ExpressionOfUnknownType, [])

        invalid
    | None ->
        range |> addError errCode
        invalid

/// Verifies that the given resolved type is indeed of kind Unit,
/// adding an ExpectingUnitExpr error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
let internal VerifyIsUnit addError (exType, range) =
    let expectedUnit (t: ResolvedType) =
        if t.Resolution = UnitType then Some t else None

    VerifyIsOneOf expectedUnit (ErrorCode.ExpectingUnitExpr, []) addError (exType, range) |> ignore

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
        t.Resolution
        |> function
        | QsTypeKind.UserDefinedType udt -> Some(processUdt pushErr udt)
        | _ -> None

    VerifyIsOneOf isUdt (ErrorCode.ExpectingUserDefinedType, [ exType |> toString ]) addError (exType, range)

/// Verifies that the given resolved type is indeed of kind Bool,
/// adding an ExpectingBoolExpr error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
let internal VerifyIsBoolean addError (exType, range) =
    let expectedBool (t: ResolvedType) =
        if t.Resolution = Bool then Some t else None

    VerifyIsOneOf expectedBool (ErrorCode.ExpectingBoolExpr, [ exType |> toString ]) addError (exType, range)
    |> ignore

/// Verifies that both given resolved types are of kind Bool,
/// adding an ExpectingBoolExpr error with the corresponding range using addError otherwise.
/// If one of the given types is a missing type, also adds the corresponding ExpressionOfUnknownType error(s).
let private VerifyAreBooleans addError (lhsType, lhsRange) (rhsType, rhsRange) =
    VerifyIsBoolean addError (lhsType, lhsRange)
    VerifyIsBoolean addError (rhsType, rhsRange)

/// Verifies that the given resolved type is indeed of kind Int,
/// adding an ExpectingIntExpr error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
let internal VerifyIsInteger addError (exType, range) =
    let expectedInt (t: ResolvedType) =
        if t.Resolution = Int then Some t else None

    VerifyIsOneOf expectedInt (ErrorCode.ExpectingIntExpr, [ exType |> toString ]) addError (exType, range)
    |> ignore

/// Verifies that both given resolved types are of kind Int,
/// adding an ExpectingIntExpr error with the corresponding range using addError otherwise.
/// If one of the given types is a missing type, also adds the corresponding ExpressionOfUnknownType error(s).
let private VerifyAreIntegers addError (lhsType, lhsRange) (rhsType, rhsRange) =
    VerifyIsInteger addError (lhsType, lhsRange)
    VerifyIsInteger addError (rhsType, rhsRange)

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
let private VerifyIntegralOp parent addError ((lhsType: ResolvedType), lhsRange) (rhsType: ResolvedType, rhsRange) =
    let exType =
        CommonBaseType
            addError
            (ErrorCode.ArgumentMismatchInBinaryOp,
             [ lhsType |> toString
               rhsType |> toString ])
            parent
            (lhsType, lhsRange)
            (rhsType, rhsRange)

    VerifyIsIntegral addError (exType, rhsRange)

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
let private VerifyArithmeticOp parent addError (lhsType: ResolvedType, lhsRange) (rhsType: ResolvedType, rhsRange) =
    let exType =
        CommonBaseType
            addError
            (ErrorCode.ArgumentMismatchInBinaryOp,
             [ lhsType |> toString
               rhsType |> toString ])
            parent
            (lhsType, lhsRange)
            (rhsType, rhsRange)

    VerifySupportsArithmetic addError (exType, rhsRange)

/// Verifies that the given resolved type indeed supports iteration,
/// adding an ExpectingIterableExpr error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
/// NOTE: returns the type of the iteration *item*.
let internal VerifyIsIterable addError (exType, range) =
    let expected (t: ResolvedType) = t.supportsIteration
    VerifyIsOneOf expected (ErrorCode.ExpectingIterableExpr, [ exType |> toString ]) addError (exType, range)

/// Verifies that given resolved types can be used within a concatenation operator.
/// First tries to find a common base type for the two types,
/// adding an ArgumentMismatchInBinaryOp error for the corresponding range(s) using addError if no common base type can be found.
/// If a common base type exists, verifies that this base type supports concatenation,
/// adding the corresponding error otherwise.
/// If one of the given types is a missing type, also adds the corresponding ExpressionOfUnknownType error(s).
/// Returns the type of the concatenation expression (i.e. the found base type).
let private VerifyConcatenation parent addError (lhsType: ResolvedType, lhsRange) (rhsType: ResolvedType, rhsRange) =
    let exType =
        CommonBaseType
            addError
            (ErrorCode.ArgumentMismatchInBinaryOp,
             [ lhsType |> toString
               rhsType |> toString ])
            parent
            (lhsType, lhsRange)
            (rhsType, rhsRange)

    let expected (t: ResolvedType) = t.supportsConcatenation
    VerifyIsOneOf expected (ErrorCode.InvalidTypeForConcatenation, [ exType |> toString ]) addError (exType, rhsRange)

/// Verifies that given resolved types can be used within an equality comparison expression.
/// First tries to find a common base type for the two types,
/// adding an ArgumentMismatchInBinaryOp error for the corresponding range(s) using addError if no common base type can be found.
/// If a common base type exists, verifies that this base type supports equality comparison,
/// adding the corresponding error otherwise.
/// If one of the given types is a missing type, also adds the corresponding ExpressionOfUnknownType error(s).
let private VerifyEqualityComparison context addError (lhsType, lhsRange) (rhsType, rhsRange) =
    // NOTE: this may not be the behavior that we want (right now it does not matter, since we don't support equality
    // comparison for any derived type).
    let argumentError =
        ErrorCode.ArgumentMismatchInBinaryOp, [ toString lhsType; toString rhsType ]

    let baseType =
        CommonBaseType addError argumentError context.Symbols.Parent (lhsType, lhsRange) (rhsType, rhsRange)

    let unsupportedError =
        ErrorCode.InvalidTypeInEqualityComparison, [ toString baseType ]

    VerifyIsOneOf (fun t -> t.supportsEqualityComparison) unsupportedError addError (baseType, rhsRange)
    |> ignore

/// Given a list of all item types and there corresponding ranges, verifies that a value array literal can be built from them.
/// Adds a MissingExprInArray error with the corresponding range using addError if one of the given types is missing.
/// Filtering all missing or invalid types, tries to find a common base type for the remaining item types,
/// and adds a MultipleTypesInArray error for the entire array if this fails.
/// Returns the inferred type of the array.
/// Returns an array with missing base type if the given list of item types is empty.
let private VerifyValueArray parent addError (content, range) =
    content
    |> List.iter (fun (t: ResolvedType, r) ->
        if t.isMissing
        then r |> addError (ErrorCode.MissingExprInArray, []))

    let arrayType = ArrayType >> ResolvedType.New
    let invalidOrMissing (t: ResolvedType) = t.isInvalid || t.isMissing

    let rec findCommonBaseType (errs: List<_>) current =
        function
        | [] -> current
        | next :: tail ->
            let accumulateErrs code _ = errs.Add code

            let common =
                CommonBaseType accumulateErrs (ErrorCode.MultipleTypesInArray, []) parent (current, range) (next, range)

            findCommonBaseType errs common tail

    match content |> List.unzip |> fst |> List.filter (not << invalidOrMissing) |> List.distinct with
    | [] when content.Length = 0 -> MissingType |> ResolvedType.New |> arrayType
    | [] -> InvalidType |> ResolvedType.New |> arrayType
    | first :: itemTs ->
        let commonBaseTerrs = new List<ErrorCode * string list>()

        let common =
            findCommonBaseType commonBaseTerrs first itemTs

        if commonBaseTerrs.Count = 0 then
            common |> arrayType
        else
            range |> addError (ErrorCode.MultipleTypesInArray, [])

            invalid |> arrayType

/// Verifies that the given resolved type supports numbered item access,
/// adding an ItemAccessForNonArray error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
let internal VerifyNumberedItemAccess addError (exType, range) =
    let expectedArray (t: ResolvedType) =
        t.Resolution
        |> function
        | ArrayType _ -> Some t
        | _ -> None

    VerifyIsOneOf expectedArray (ErrorCode.ItemAccessForNonArray, [ exType |> toString ]) addError (exType, range)

/// Verifies that the given type of the left hand side of an array item expression is indeed an array type (or invalid),
/// adding an ItemAccessForNonArray error with the corresponding range using addError otherwise.
/// Verifies that the given type of the expression within the item access is either of type Int or Range,
/// adding an InvalidArrayItemIndex error with the corresponding range using addError otherwise.
/// Returns the type of the array item expression.
let private VerifyArrayItem addError (arrType: ResolvedType, arrRange) (indexType: ResolvedType, indexRange) =
    let indexIsInt = indexType.Resolution = Int
    let indexIsRange = indexType.Resolution = Range

    if (not indexType.isInvalid) && (not indexIsInt) && (not indexIsRange)
    then indexRange |> addError (ErrorCode.InvalidArrayItemIndex, [ indexType |> toString ])

    let ressArrType =
        VerifyNumberedItemAccess addError (arrType, arrRange)

    match ressArrType.Resolution with
    | ArrayType baseType when indexIsInt -> baseType
    | ArrayType baseType when indexIsRange -> baseType |> ArrayType |> ResolvedType.New
    | ArrayType _ -> invalid
    | _ when indexIsRange -> invalid |> ArrayType |> ResolvedType.New
    | _ -> invalid

/// Verifies that the given functor can be applied to an expression of the given type,
/// adding an error with the given error code and range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
/// Returns the type of the functor application expression.
let private VerifyFunctorApplication functor errCode addError (ex: ResolvedType, range) =
    let opSupportingFunctor (t: ResolvedType) =
        t.Resolution
        |> function
        | QsTypeKind.Operation (_, info) when info.Characteristics.AreInvalid -> Some t
        | QsTypeKind.Operation (_, info) ->
            info.Characteristics.SupportedFunctors
            |> function
            | Value functors when functors.Contains functor -> Some t
            | _ -> None
        | _ -> None

    VerifyIsOneOf opSupportingFunctor errCode addError (ex, range)

/// Verifies that the Adjoint functor can be applied to an expression of the given type,
/// adding an InvalidAdjointApplication error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
/// Returns the type of the functor application expression.
let private VerifyAdjointApplication =
    VerifyFunctorApplication Adjoint (ErrorCode.InvalidAdjointApplication, [])

/// Verifies that the Controlled functor can be applied to an expression of the given type,
/// adding an InvalidControlledApplication error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
/// Returns the type of the functor application expression.
let private VerifyControlledApplication addError (ex: ResolvedType, range) =
    let origType =
        VerifyFunctorApplication Controlled (ErrorCode.InvalidControlledApplication, []) addError (ex, range)

    match origType.Resolution with
    | QsTypeKind.Operation ((arg, res), characteristics) ->
        QsTypeKind.Operation((arg |> SyntaxGenerator.AddControlQubits, res), characteristics)
        |> ResolvedType.New
    | _ -> origType // is invalid type


// utils for verifying identifiers, call expressions, and resolving type parameters

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
let private VerifyIdentifier addDiagnostic (symbols: SymbolTracker) (sym, tArgs) =
    let resolvedTargs =
        tArgs
        |> QsNullable<_>
            .Map(fun (args: ImmutableArray<QsType>) ->
                args.Select(fun tArg ->
                    tArg.Type
                    |> function
                    | MissingType -> ResolvedType.New MissingType
                    | _ -> symbols.ResolveType addDiagnostic tArg))
        |> QsNullable<_>
            .Map(fun args -> args.ToImmutableArray())

    let resId, typeParams =
        symbols.ResolveIdentifier addDiagnostic sym

    let identifier, info =
        Identifier(resId.VariableName, resolvedTargs), resId.InferredInformation

    // resolve type parameters (if any) with the given type arguments
    // Note: type parameterized objects are never mutable - remember they are not the same as an identifier containing a template...!
    let invalidWithoutTargs mut =
        (identifier, invalid, info.HasLocalQuantumDependency, sym.Range) |> ExprWithoutTypeArgs mut

    match resId.VariableName, resolvedTargs with
    | InvalidIdentifier, Null -> invalidWithoutTargs true
    | InvalidIdentifier, Value _ -> invalidWithoutTargs false
    | LocalVariable _, Null ->
        (identifier, resId.Type, info.HasLocalQuantumDependency, sym.Range)
        |> ExprWithoutTypeArgs info.IsMutable
    | LocalVariable _, Value _ ->
        sym.RangeOrDefault
        |> QsCompilerDiagnostic.Error(ErrorCode.IdentifierCannotHaveTypeArguments, [])
        |> addDiagnostic

        invalidWithoutTargs false
    | GlobalCallable _, Null ->
        (identifier, resId.Type, info.HasLocalQuantumDependency, sym.Range) |> ExprWithoutTypeArgs false
    | GlobalCallable _, Value res when res.Length <> typeParams.Length ->
        sym.RangeOrDefault
        |> QsCompilerDiagnostic.Error(ErrorCode.WrongNumberOfTypeArguments, [ typeParams.Length.ToString() ])
        |> addDiagnostic

        invalidWithoutTargs false
    | GlobalCallable id, Value res ->
        let resolutions =
            [ for (tp, ta) in res |> Seq.zip typeParams do
                if not ta.isMissing
                then yield (tp, ta |> StripPositionInfo.Apply) ]
            |> List.choose (fun (tp, ta) ->
                tp
                |> function
                | InvalidName -> None // invalid type parameters cannot possibly turn up in the identifier type ... (they don't parse)
                | ValidName tpName -> Some((QsQualifiedName.New(id.Namespace, id.Name), tpName), ta))

        let typeParamLookUp =
            resolutions.ToImmutableDictionary(fst, snd)

        let exInfo =
            InferredExpressionInformation.New(isMutable = false, quantumDep = info.HasLocalQuantumDependency)

        TypedExpression.New(identifier, typeParamLookUp, resId.Type, exInfo, sym.Range)

/// Verifies whether an expression of the given argument type can be used as argument to a method (function, operation, or setter)
/// that expects an argument of the given target type. The given target type may contain a missing type (valid for a setter).
/// Accumulates and returns an array with error codes for the cases where this is not the case, and returns an empty array otherwise.
/// Note that MissingTypes in the argument type should not occur aside from possibly as array base type of the expression.
/// A missing type in the given argument type will cause a verification failure in QsCompilerError.
/// For each type parameter in the target type, calls addTypeParameterResolution with a tuple of the type parameter and the type that is substituted for it.
/// IMPORTANT: The consistent (i.e. non-ambiguous and non-constraining) resolution of type parameters is *not* verified by this routine
/// and needs to be verified in a separate step!
let internal TypeMatchArgument addTypeParameterResolution targetType argType =
    let givenAndExpectedType =
        [ argType |> toString
          targetType |> toString ]

    let onErrorRaiseInstead errCode (diag: IEnumerable<_>) =
        if diag.Any() then [| errCode |] else [||]

    let rec compareTuple (variance: Variance) (ts1: IEnumerable<_>) (ts2: IEnumerable<_>) =
        if ts1.Count() <> ts2.Count() then
            [| (ErrorCode.ArgumentTupleShapeMismatch, givenAndExpectedType) |]
        else
            (ts1.Zip(ts2, (fun i1 i2 -> (i1, i2))))
                .SelectMany(new Func<_, _>(matchTypes variance >> Array.toSeq))
            |> onErrorRaiseInstead (ErrorCode.ArgumentTupleMismatch, givenAndExpectedType)

    and compareSignature variance ((i1, o1), s1: ResolvedCharacteristics) ((i2, o2), s2: ResolvedCharacteristics) =
        let l1, l2 =
            let compilerError () =
                QsCompilerError.Raise "supported functors could not be determined"
                ImmutableHashSet.Empty

            if s1.AreInvalid || s2.AreInvalid
            then ImmutableHashSet.Empty, ImmutableHashSet.Empty
            else s1.SupportedFunctors.ValueOrApply compilerError, s2.SupportedFunctors.ValueOrApply compilerError

        let argVariance, ferrCode, expected =
            variance
            |> function
            | Covariant -> Contravariant, ErrorCode.MissingFunctorSupport, missingFunctors (l1, Some l2)
            | Contravariant -> Covariant, ErrorCode.ExcessFunctorSupport, missingFunctors (l2, Some l1)
            | Invariant ->
                Invariant,
                ErrorCode.FunctorSupportMismatch,
                (if (l1.SymmetricExcept l2).Any() then missingFunctors (l1, None) else [])

        let fErrs =
            if expected.Length = 0
            then [||]
            else [| (ferrCode, [ String.Join(", ", expected) ]) |]

        (matchTypes argVariance (i1, i2)
         |> onErrorRaiseInstead (ErrorCode.CallableTypeInputTypeMismatch, [ i2 |> toString; i1 |> toString ]))
            .Concat((matchTypes  // variance changes for the argument type *only*
                         variance
                         (o1, o2)
                     |> onErrorRaiseInstead
                         (ErrorCode.CallableTypeOutputTypeMismatch, [ o2 |> toString; o1 |> toString ])).Concat fErrs)
        |> Seq.toArray

    and compareArrayBaseTypes (bt: ResolvedType) (ba: ResolvedType) =
        if ba.isMissing then
            // empty array on the right hand side is always ok, otherwise arrays are invariant
            Array.empty
        else
            matchTypes Invariant (bt, ba)
            |> onErrorRaiseInstead (ErrorCode.ArrayBaseTypeMismatch, [ ba |> toString; bt |> toString ])

    and matchTypes variance (targetT: ResolvedType, exType: ResolvedType) =
        QsCompilerError.Verify(not exType.isMissing, "expression type is missing")

        match targetT.Resolution, exType.Resolution with
        | QsTypeKind.MissingType, _ ->
            // the lhs of a set-statement may contain underscores
            Array.empty
        | QsTypeKind.TypeParameter tp, _ ->
            // lhs is a type parameter of the *called* callable!
            addTypeParameterResolution ((tp.Origin, tp.TypeName), exType)
            [||]
        | QsTypeKind.ArrayType b1, QsTypeKind.ArrayType b2 -> compareArrayBaseTypes b1 b2
        | QsTypeKind.TupleType ts1, QsTypeKind.TupleType ts2 -> compareTuple variance ts1 ts2
        | QsTypeKind.UserDefinedType udt1, QsTypeKind.UserDefinedType udt2 when udt1 = udt2 -> [||]
        | QsTypeKind.UserDefinedType _, QsTypeKind.UserDefinedType _ ->
            [| (ErrorCode.UserDefinedTypeMismatch,
                [ exType |> toString
                  targetT |> toString ]) |]
        | QsTypeKind.Operation ((i1, o1), l1), QsTypeKind.Operation ((i2, o2), l2) ->
            compareSignature variance ((i1, o1), l1.Characteristics) ((i2, o2), l2.Characteristics)
        | QsTypeKind.Function (i1, o1), QsTypeKind.Function (i2, o2) ->
            compareSignature
                variance
                ((i1, o1), ResolvedCharacteristics.Empty)
                ((i2, o2), ResolvedCharacteristics.Empty)
        | QsTypeKind.InvalidType, _
        | _, QsTypeKind.InvalidType -> [||]
        | resT, resA when resT = resA -> [||]
        | _ -> [| (ErrorCode.ArgumentTypeMismatch, givenAndExpectedType) |]

    matchTypes Covariant (targetType, argType)

/// Returns the type of the expression that completes the argument
/// (i.e. the expected type for the expression that completes all missing pieces in the argument) as option,
/// as well a a look-up for type parameters that are resolved by the given argument.
/// Returning None for the completing expression type indicates that no expressions are missing for the call.
/// Returning an invalid type as Some indicates that either the type of the given argument is
/// incompatible with the targetType, or that the targetType itself is invalid,
/// and no conclusion can be reached on the type for the unresolved part of the argument.
let private IsValidArgument addError targetType (arg, resolveInner) =
    let invalid = invalid |> Some

    let buildType (tItems: ResolvedType option list) =
        let remaining = tItems |> List.choose id

        let containsInvalid =
            remaining |> List.exists (fun x -> x.isInvalid)

        let containsMissing =
            remaining |> List.exists (fun x -> x.isMissing)

        QsCompilerError.Verify(not containsMissing, "missing type in remaining input type")

        if containsInvalid then
            invalid
        else
            remaining
            |> function
            | [] -> None
            | [ t ] -> Some t
            | _ -> TupleType(remaining.ToImmutableArray()) |> ResolvedType.New |> Some

    let lookUp =
        new List<(QsQualifiedName * string) * (ResolvedType * Range)>()

    let addTpResolution range (tp, exT) = lookUp.Add(tp, (exT, range))

    let rec recur (targetT: ResolvedType, argEx: QsExpression) =
        let pushErrs errCodes =
            for code in errCodes do
                argEx.RangeOrDefault |> addError code

        QsCompilerError.Verify(not targetT.isMissing, "target type is missing")

        match targetT, argEx with
        | _, _ when targetT.isInvalid || targetT.isMissing -> invalid
        | _, Missing -> targetT |> Some
        | Tuple ts, Tuple exs when ts.Length <> exs.Length ->
            [| (ErrorCode.ArgumentTupleShapeMismatch,
                [ resolveInner argEx |> toString
                  targetT |> toString ]) |]
            |> pushErrs

            invalid
        | Tuple ts, Tuple exs when ts.Length = exs.Length -> List.zip ts exs |> List.map recur |> buildType
        | Item t, Tuple _ when not (t: ResolvedType).isTypeParameter ->
            [| (ErrorCode.UnexpectedTupleArgument, [ targetT |> toString ]) |] |> pushErrs

            invalid
        | _, _ ->
            TypeMatchArgument (addTpResolution argEx.RangeOrDefault) targetT (resolveInner argEx) |> pushErrs

            None

    recur (targetType, arg), lookUp.ToLookup(fst, snd)

/// Given the expected argument type and the expected result type of a callable,
/// verifies the given argument using the given function getType to to resolve the type of the argument items.
/// Calls IsValidArgument to obtain the type of the expression that would complete the given argument (which may contain missing expressions),
/// as well as a lookup for the type parameters that are defined by the non-missing argument items.
/// Verifies that there is no ambiguity in that lookup,
/// and adds a AmbiguousTypeParameterResolution error for the corresponding range using addError otherwise.
/// Enforces that direct recursions respect any explicitly specified type arguments (tArgs), and adds a ConstrainsTypeParameter otherwise.
/// Adds a ConstrainsTypeParameter error if when the call would restrict what the type parameters of the parent callable can resolve to.
/// Builds the type of the call expression using buildCallableType.
/// Returns the built and verified look-up for the type paramters as well as the type of the call expression.
let private VerifyCallExpr buildCallableType
                           addError
                           (parent, isDirectRecursion, tArgs)
                           (expectedArgType, expectedResultType)
                           (arg, getType)
                           =
    let requireIdResolution =
        tArgs
        |> function
        | Some (tArgs: ImmutableArray<ResolvedType>) ->
            new Set<_>(tArgs
                       |> Seq.choose (fun tArg ->
                           tArg.Resolution
                           |> function
                           | TypeParameter tp when tp.Origin = parent -> Some tp.TypeName
                           | _ -> None))
        | None -> Set.empty

    let getTypeParameterResolutions (lookUp: ILookup<_, _>) =
        // IMPORTANT: Note that it is *not* possible to determine something like a "common base type"
        // without knowing the context in which the type parameters given in the lookUp occur!! ("covariant vs contravariant resolution" of the type parameter)
        let containsMissing (t: ResolvedType) =
            t.Exists(function
                | MissingType -> true
                | _ -> false)

        let findResolution (entry: IGrouping<_, ResolvedType * _>) =

            let uniqueResolution (res, r) =
                if res |> containsMissing then
                    r |> addError (ErrorCode.PartialApplicationOfTypeParameter, [])

                    invalid
                elif fst entry.Key = parent then // resolution of an internal type parameter
                    // Internal type parameters may occur on the lhs
                    // 1.) due to explicitly provided type arguments to the called expression
                    // 2.) because the call is a direct recursion
                    // In the first case, they always need to be "resolved" to exactly themselves.
                    // In the second case, they can be resolve to anything, just like any other (i.e. external) type parameter.
                    // Any combination of one and two may occur. For a recursive partial applications that does not resolve all type parameters,
                    // we need to to distinguish the type parameters of the returned partial application expression from the ones in the parent function.
                    // We hence treat self-recursions separately and require that for direct self-recursions all type parameters need to be resolved;
                    // i.e. we prevent type parametrized partial applications of the parent callable, and all type arguments are attached to the identifier.
                    let typeParam =
                        QsTypeParameter.New(fst entry.Key, snd entry.Key, Null) |> TypeParameter |> ResolvedType.New

                    match res.Resolution with
                    | TypeParameter tp when tp.Origin = fst entry.Key && tp.TypeName = snd entry.Key -> typeParam
                    | _ when isDirectRecursion ->
                        // In the case where we have a direct recursion we need to know what explicit type arguments were specified.
                        // In particular, we need to know when a type argument was a type parameter of the parent callable.
                        // In that case, the type parameter needs to resolve to itself.
                        // For any other type parameters, we can resolve them to whatever they resolve to, unless it is a type parameter of another callable.
                        // Then we need to attach these resolutions as type arguments to the identifier (done by the calling method, which builds the expression).
                        // At the end, all type parameters for the parent need to be resolved, and otherwise we need to generate a suitable error.
                        if (requireIdResolution.Contains(snd entry.Key)) then // explicitly specified type argument that needs to resolve to itself (which it doesn't)
                            r |> addError (ErrorCode.ConstrainsTypeParameter, [ typeParam |> toString ])

                            typeParam
                        else // this could be incorporated into the outer matching, but keeping the logic for the direct recursion case together may be more readable
                            match res.Resolution with
                            | TypeParameter tp when tp.Origin <> parent ->
                                r |> addError (ErrorCode.AmbiguousTypeParameterResolution, [])

                                invalid // todo: it would be nice to eventually lift this restriction
                            | _ -> res |> StripPositionInfo.Apply
                    | _ ->
                        r |> addError (ErrorCode.ConstrainsTypeParameter, [ typeParam |> toString ])

                        typeParam
                else
                    res |> StripPositionInfo.Apply

            match entry |> Seq.distinctBy fst |> Seq.toList with
            | [ (res, r) ] -> uniqueResolution (res, r)
            | _ ->
                entry
                |> Seq.distinctBy (fst >> StripInferredInfoFromType)
                |> Seq.toList
                |> function
                | [ (res, r) ] -> uniqueResolution (res, r)
                | _ -> // either conflicting with internal type parameter or ambiguous
                    if fst entry.Key <> parent || not <| requireIdResolution.Contains(snd entry.Key) then
                        for (_, r) in entry do
                            r |> addError (ErrorCode.AmbiguousTypeParameterResolution, [])

                        invalid
                    else // explicitly specified by type argument
                        let typeParam =
                            QsTypeParameter.New(fst entry.Key, snd entry.Key, Null) |> TypeParameter |> ResolvedType.New

                        let conflicting =
                            entry |> Seq.filter (fun (t, _) -> typeParam = StripPositionInfo.Apply t)

                        for (_, r) in conflicting do
                            r
                            |> addError (ErrorCode.TypeParameterResConflictWithTypeArgument, [ typeParam |> toString ])

                        typeParam

        let tpResolutions =
            lookUp |> Seq.map (fun entry -> entry.Key, findResolution entry)

        tpResolutions.ToImmutableDictionary(fst, snd)

    let remaining, lookUp =
        (arg, getType) |> IsValidArgument addError expectedArgType

    getTypeParameterResolutions lookUp,
    remaining
    |> function
    | None -> expectedResultType
    | Some remainingArgT when remainingArgT.isInvalid -> invalid
    | Some remainingArgT -> buildCallableType (remainingArgT, expectedResultType) |> ResolvedType.New

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
        let typedEx =
            AutoGeneratedExpression kind InvalidType false // the expression type is irrelevant

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
let internal VerifyAssignment expectedType (parent, definedTypeParams) mismatchErr addError (rhsType, rhsEx, rhsRange) =
    // we need to check if the right hand side contains a type parametrized version of the parent callable
    let directRecursion =
        IsTypeParamRecursion (parent, definedTypeParams) rhsEx

    if directRecursion
    then rhsRange |> addError (ErrorCode.InvalidUseOfTypeParameterizedObject, [])
    // we need to check if all type parameters are consistently resolved
    let tpResolutions =
        new List<(QsQualifiedName * string) * ResolvedType>()

    let addTpResolution (key, exT) =
        // we can ignoring external type parameters,
        // since for a set-statement these can only occur if either the lhs can either not be set or has been assigned previously
        // and for a return statement the expected return type cannot contain external type parameters by construction
        if fst key = parent then tpResolutions.Add(key, exT)

    let errCodes =
        TypeMatchArgument addTpResolution expectedType rhsType

    if errCodes.Length <> 0 then
        rhsRange
        |> addError
            (mismatchErr,
             [ rhsType |> toString
               expectedType |> toString ])

    let containsNonTrivialResolution (tp: IGrouping<_, ResolvedType>) =
        let notResolvedToItself (x: ResolvedType) =
            match x.Resolution with
            | TypeParameter p -> p.Origin <> fst tp.Key || p.TypeName <> snd tp.Key
            | _ -> not x.isInvalid

        tp |> Seq.exists notResolvedToItself

    let nonTrivialResolutions =
        tpResolutions
            .ToLookup(fst, snd).Where containsNonTrivialResolution
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
                (InvalidExpr, invalid, resolvedArr.InferredInformation.HasLocalQuantumDependency, Null)
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
                    VerifyIsInteger addError (resolved.ResolvedType, ex.RangeOrDefault)
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
            let resInterpol =
                (interpolated.Select InnerExpression)
                    .ToImmutableArray()

            let localQdependency =
                resInterpol |> Seq.exists (fun r -> r.InferredInformation.HasLocalQuantumDependency)

            (StringLiteral(literal, resInterpol), String |> ResolvedType.New, localQdependency, this.Range)
            |> ExprWithoutTypeArgs false

        /// Resolves and verifies all given items, and returns the corresponding ValueTuple as typed expression.
        /// If the ValueTuple contains only one item, the item is returned instead (i.e. arity-1 tuple expressions are stripped).
        /// Throws an ArgumentException if the given items do not at least contain one element.
        let buildTuple (items: ImmutableArray<_>) =
            let resolvedItems =
                (items.Select InnerExpression).ToImmutableArray()

            let resolvedTypes =
                (resolvedItems |> Seq.map (fun x -> x.ResolvedType))
                    .ToImmutableArray()

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
            VerifyIsInteger addError (resolvedEx.ResolvedType, ex.RangeOrDefault)
            let resolvedBase = symbols.ResolveType addDiagnostic bType

            let arrType =
                resolvedBase |> StripPositionInfo.Apply |> ArrayType |> ResolvedType.New

            let quantumDep =
                resolvedEx.InferredInformation.HasLocalQuantumDependency

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
                |> fun vals -> VerifyValueArray symbols.Parent addError (vals, this.RangeOrDefault)

            let resolvedValues =
                (positioned |> List.map fst).ToImmutableArray()

            let localQdependency =
                resolvedValues |> Seq.exists (fun item -> item.InferredInformation.HasLocalQuantumDependency)

            (ValueArray resolvedValues, resolvedType, localQdependency, this.Range) |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given array expression and index expression of an array item access expression,
        /// and returns the corresponding ArrayItem expression as typed expression.
        let buildArrayItem (arr, idx: QsExpression) =
            let resolvedArr = InnerExpression arr

            match resolveSlicing resolvedArr idx with
            | None ->
                { resolvedArr with
                      ResolvedType = VerifyNumberedItemAccess addError (resolvedArr.ResolvedType, arr.RangeOrDefault) }
            | Some resolvedIdx ->
                let resolvedType =
                    VerifyArrayItem
                        addError
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
            sym.Symbol
            |> function
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

            let exType =
                VerifyUdtWith (symbols.GetItemType itemName) addError (resolvedEx.ResolvedType, ex.RangeOrDefault)

            let localQdependency =
                resolvedEx.InferredInformation.HasLocalQuantumDependency

            (NamedItem(resolvedEx, itemName), exType, localQdependency, this.Range) |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given left hand side, access expression, and right hand side of a copy-and-update expression,
        /// and returns the corresponding copy-and-update expression as typed expression.
        let buildCopyAndUpdate (lhs: QsExpression, accEx: QsExpression, rhs: QsExpression) =
            let resLhs, resRhs = InnerExpression lhs, InnerExpression rhs

            let resolvedCopyAndUpdateExpr resAccEx =
                let localQdependency =
                    [ resLhs; resAccEx; resRhs ]
                    |> Seq.map (fun ex -> ex.InferredInformation.HasLocalQuantumDependency)
                    |> Seq.contains true

                (CopyAndUpdate(resLhs, resAccEx, resRhs), resLhs.ResolvedType, localQdependency, this.Range)
                |> ExprWithoutTypeArgs false

            let parent =
                symbols.Parent, symbols.DefinedTypeParameters

            match (resLhs.ResolvedType.Resolution, accEx.Expression) with
            | UserDefinedType _, Identifier (sym, Null) ->
                let itemName = sym |> buildItemName

                let itemType =
                    VerifyUdtWith (symbols.GetItemType itemName) addError (resLhs.ResolvedType, lhs.RangeOrDefault)

                VerifyAssignment
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
                    let expectedRhs =
                        VerifyNumberedItemAccess addError (resLhs.ResolvedType, lhs.RangeOrDefault)

                    VerifyAssignment
                        expectedRhs
                        parent
                        ErrorCode.TypeMismatchInCopyAndUpdateExpr
                        addError
                        (resRhs.ResolvedType, None, rhs.RangeOrDefault)

                    { resRhs with
                          ResolvedType = expectedRhs }
                | Some resAccEx -> // indicates either a index or index range to update
                    let expectedRhs =
                        VerifyArrayItem
                            addError
                            (resLhs.ResolvedType, lhs.RangeOrDefault)
                            (resAccEx.ResolvedType, accEx.RangeOrDefault)

                    VerifyAssignment
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
            VerifyIsInteger addError (resRhs.ResolvedType, rEnd.RangeOrDefault)

            let resLhs =
                lhs.Expression
                |> function
                | RangeLiteral (rStart, rStep) ->
                    let (resStart, resStep) =
                        (InnerExpression rStart, InnerExpression rStep)

                    VerifyAreIntegers
                        addError
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
                        VerifyIsInteger addError (resStart.ResolvedType, lhs.RangeOrDefault)
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

            let exType =
                verify addError (resolvedEx.ResolvedType, ex.RangeOrDefault)

            (buildExprKind resolvedEx, exType, resolvedEx.InferredInformation.HasLocalQuantumDependency, this.Range)
            |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given left hand side and right hand side of an arithmetic operator,
        /// and returns the corresponding expression built with buildExprKind as typed expression.
        let buildArithmeticOp buildExprKind (lhs, rhs) =
            let (resolvedLhs, resolvedRhs) =
                (InnerExpression lhs, InnerExpression rhs)

            let resolvedType =
                VerifyArithmeticOp
                    symbols.Parent
                    addError
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
            let (resolvedLhs, resolvedRhs) =
                (InnerExpression lhs, InnerExpression rhs)

            let resolvedType =
                // Note: this relies on the lhs supporting concatenation if and only if all of its base types do,
                // and there being no type that supports both arithmetic and concatenation
                if resolvedLhs.ResolvedType.supportsConcatenation.IsSome then
                    VerifyConcatenation
                        symbols.Parent
                        addError
                        (resolvedLhs.ResolvedType, lhs.RangeOrDefault)
                        (resolvedRhs.ResolvedType, rhs.RangeOrDefault)
                else
                    VerifyArithmeticOp
                        symbols.Parent
                        addError
                        (resolvedLhs.ResolvedType, lhs.RangeOrDefault)
                        (resolvedRhs.ResolvedType, rhs.RangeOrDefault)

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
            let (resolvedLhs, resolvedRhs) =
                (InnerExpression lhs, InnerExpression rhs)

            let resolvedType =
                if resolvedLhs.ResolvedType.Resolution = BigInt then
                    VerifyIsInteger addError (resolvedRhs.ResolvedType, rhs.RangeOrDefault)
                    resolvedLhs.ResolvedType
                else
                    VerifyArithmeticOp
                        symbols.Parent
                        addError
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
            let (resolvedLhs, resolvedRhs) =
                (InnerExpression lhs, InnerExpression rhs)

            let resolvedType =
                VerifyIntegralOp
                    symbols.Parent
                    addError
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
            let (resolvedLhs, resolvedRhs) =
                (InnerExpression lhs, InnerExpression rhs)

            let resolvedType =
                VerifyIsIntegral addError (resolvedLhs.ResolvedType, lhs.RangeOrDefault)

            VerifyIsInteger addError (resolvedRhs.ResolvedType, rhs.RangeOrDefault)

            let localQdependency =
                resolvedLhs.InferredInformation.HasLocalQuantumDependency
                || resolvedRhs.InferredInformation.HasLocalQuantumDependency

            (buildExprKind (resolvedLhs, resolvedRhs), resolvedType, localQdependency, this.Range)
            |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given left hand side and right hand side of a binary boolean operator,
        /// and returns the corresponding expression built with buildExprKind as typed expression of type Bool.
        let buildBooleanOpWith verify shortCircuits buildExprKind (lhs, rhs) =
            let (resolvedLhs, resolvedRhs) =
                (InnerExpression lhs, InnerExpression rhs)

            if shortCircuits
            then VerifyConditionalExecution addWarning (resolvedRhs, rhs.RangeOrDefault)

            verify
                addError
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
            let resCond, resIsTrue, resIsFalse =
                InnerExpression cond, InnerExpression ifTrue, InnerExpression ifFalse

            VerifyConditionalExecution addWarning (resIsTrue, ifTrue.RangeOrDefault)
            VerifyConditionalExecution addWarning (resIsFalse, ifFalse.RangeOrDefault)
            VerifyIsBoolean addError (resCond.ResolvedType, cond.RangeOrDefault)

            let lhs, rhs =
                (resIsTrue.ResolvedType, ifTrue.RangeOrDefault), (resIsFalse.ResolvedType, ifFalse.RangeOrDefault)

            let exType =
                CommonBaseType
                    addError
                    (ErrorCode.TypeMismatchInConditional,
                     [ resIsTrue.ResolvedType |> toString
                       resIsFalse.ResolvedType |> toString ])
                    symbols.Parent
                    lhs
                    rhs

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

            let exType =
                VerifyUdtWith symbols.GetUnderlyingType addError (resolvedEx.ResolvedType, ex.RangeOrDefault)

            (UnwrapApplication resolvedEx, exType, resolvedEx.InferredInformation.HasLocalQuantumDependency, this.Range)
            |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given left hand side and right hand side of a call expression,
        /// and returns the corresponding expression as typed expression.
        let buildCall (method, arg) =
            let getType (ex: QsExpression) =
                (ex.Resolve context (fun _ -> ())).ResolvedType

            let (resolvedMethod, resolvedArg) =
                (InnerExpression method, InnerExpression arg)

            let locQdepClassicalEx =
                resolvedMethod.InferredInformation.HasLocalQuantumDependency
                || resolvedArg.InferredInformation.HasLocalQuantumDependency

            let originalExKind =
                CallLikeExpression(resolvedMethod, resolvedArg)

            let invalidEx =
                (originalExKind, invalid, false, this.Range) |> ExprWithoutTypeArgs false

            let isDirectRecursion, tArgs =
                resolvedMethod.Expression
                |> function
                | Identifier (GlobalCallable id, tArgs) ->
                    id = symbols.Parent, tArgs |> QsNullable<_>.Fold (fun _ v -> Some v) None
                | _ -> false, None

            let callTypeOrPartial build (expectedArgT, expectedResT) =
                let (typeParamResolutions, exType) =
                    VerifyCallExpr
                        build
                        addError
                        (symbols.Parent, isDirectRecursion, tArgs)
                        (expectedArgT, expectedResT)
                        (arg, getType)

                let requiresTypeArgumentResolution =
                    isDirectRecursion && symbols.DefinedTypeParameters.Length <> 0

                if not requiresTypeArgumentResolution then
                    typeParamResolutions, exType, originalExKind
                else // we need to attach the resolved type arguments to the identifier

                    // combine the explicitly given type arguments with the inferred ones to get all type arguments for the called method
                    let tArgs =
                        tArgs
                        |> Option.defaultValue
                            (symbols.DefinedTypeParameters
                             |> Seq.map (fun _ -> ResolvedType.New MissingType)
                             |> ImmutableArray.CreateRange)

                    QsCompilerError.Verify
                        (tArgs.Length = symbols.DefinedTypeParameters.Length, "length mismatch for type arguments")

                    let tArgs =
                        tArgs
                        |> Seq.zip symbols.DefinedTypeParameters
                        |> Seq.map (function
                            | tpName, tArg when tArg.isMissing ->
                                typeParamResolutions.TryGetValue((symbols.Parent, tpName))
                                |> function
                                | true, res -> (symbols.Parent, tpName), res
                                | false, _ -> (symbols.Parent, tpName), ResolvedType.New MissingType // important: these need to be filtered out when building the resolutions dictionary
                            | tpName, tArg -> (symbols.Parent, tpName), tArg)

                    if tArgs |> Seq.map snd |> Seq.contains (ResolvedType.New MissingType)
                    then method.RangeOrDefault |> addError (ErrorCode.UnresolvedTypeParameterForRecursiveCall, [])

                    // attach the new fully populated type arguments to the identifier of the callable
                    let methodTypeParamResolutions =
                        tArgs
                        |> Seq.filter (snd >> (<>) (ResolvedType.New MissingType))
                        |> Seq.map KeyValuePair.Create
                        |> ImmutableDictionary.CreateRange

                    let methodTypeArgs =
                        tArgs |> Seq.map snd |> ImmutableArray.CreateRange |> Value

                    let typeParamRes =
                        typeParamResolutions
                        |> Seq.filter (fun kv -> fst kv.Key <> symbols.Parent)
                        |> ImmutableDictionary.CreateRange

                    let methodExprKind =
                        Identifier(GlobalCallable symbols.Parent, methodTypeArgs)

                    let methodExpr =
                        TypedExpression.New
                            (methodExprKind,
                             methodTypeParamResolutions,
                             resolvedMethod.ResolvedType,
                             resolvedMethod.InferredInformation,
                             resolvedMethod.Range)

                    typeParamRes, exType, CallLikeExpression(methodExpr, resolvedArg)

            match resolvedMethod.ResolvedType.Resolution with
            | QsTypeKind.InvalidType -> invalidEx
            | QsTypeKind.Function (argT, resT) ->
                let typeParamResolutions, exType, exprKind =
                    (argT, resT) |> callTypeOrPartial QsTypeKind.Function

                let exInfo =
                    InferredExpressionInformation.New(isMutable = false, quantumDep = locQdepClassicalEx)

                TypedExpression.New(exprKind, typeParamResolutions, exType, exInfo, this.Range)
            | QsTypeKind.Operation ((argT, resT), characteristics) ->
                let typeParamResolutions, exType, exprKind =
                    (argT, resT) |> callTypeOrPartial (fun (i, o) -> QsTypeKind.Operation((i, o), characteristics))

                let isPartialApplication =
                    TypedExpression.IsPartialApplication exprKind

                if not (isPartialApplication || characteristics.Characteristics.AreInvalid) then // check that the functors necessary for auto-generation are supported
                    let functors =
                        characteristics.Characteristics.SupportedFunctors.ValueOr ImmutableHashSet.Empty

                    let missing =
                        missingFunctors (symbols.RequiredFunctorSupport, Some functors)

                    if missing.Length <> 0 then
                        method.RangeOrDefault
                        |> addError (ErrorCode.MissingFunctorForAutoGeneration, [ String.Join(", ", missing) ])

                let localQDependency =
                    if isPartialApplication then locQdepClassicalEx else true

                let exInfo =
                    InferredExpressionInformation.New(isMutable = false, quantumDep = localQDependency)

                if not (context.IsInOperation || isPartialApplication) then
                    method.RangeOrDefault |> addError (ErrorCode.OperationCallOutsideOfOperation, [])

                    invalidEx
                else
                    TypedExpression.New(exprKind, typeParamResolutions, exType, exInfo, this.Range)
            | _ ->
                method.RangeOrDefault
                |> addError (ErrorCode.ExpectingCallableExpr, [ resolvedMethod.ResolvedType |> toString ])

                invalidEx

        match this.Expression with
        | InvalidExpr -> (InvalidExpr, InvalidType |> ResolvedType.New, false, this.Range) |> ExprWithoutTypeArgs true // choosing the more permissive option here
        | MissingExpr -> (MissingExpr, MissingType |> ResolvedType.New, false, this.Range) |> ExprWithoutTypeArgs false
        | UnitValue -> (UnitValue, UnitType |> ResolvedType.New, false, this.Range) |> ExprWithoutTypeArgs false
        | Identifier (sym, tArgs) -> VerifyIdentifier addDiagnostic symbols (sym, tArgs)
        | CallLikeExpression (method, arg) -> buildCall (method, arg)
        | AdjointApplication ex -> verifyAndBuildWith AdjointApplication VerifyAdjointApplication ex
        | ControlledApplication ex -> verifyAndBuildWith ControlledApplication VerifyControlledApplication ex
        | UnwrapApplication ex -> buildUnwrap ex
        | ValueTuple items -> buildTuple items
        | ArrayItem (arr, idx) -> buildArrayItem (arr, idx)
        | NamedItem (ex, acc) -> buildNamedItem (ex, acc)
        | ValueArray values -> buildValueArray values
        | NewArray (baseType, ex) -> buildNewArray (baseType, ex)
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
            buildBooleanOpWith (fun log l r -> VerifyArithmeticOp symbols.Parent log l r |> ignore) false LT (lhs, rhs)
        | LTE (lhs, rhs) ->
            buildBooleanOpWith (fun log l r -> VerifyArithmeticOp symbols.Parent log l r |> ignore) false LTE (lhs, rhs)
        | GT (lhs, rhs) ->
            buildBooleanOpWith (fun log l r -> VerifyArithmeticOp symbols.Parent log l r |> ignore) false GT (lhs, rhs)
        | GTE (lhs, rhs) ->
            buildBooleanOpWith (fun log l r -> VerifyArithmeticOp symbols.Parent log l r |> ignore) false GTE (lhs, rhs)
        | POW (lhs, rhs) -> buildPower (lhs, rhs) // power takes a special role because you can raise integers and doubles to integer and double powers, but bigint only to integer powers
        | MOD (lhs, rhs) -> buildIntegralOp MOD (lhs, rhs)
        | LSHIFT (lhs, rhs) -> buildShiftOp LSHIFT (lhs, rhs)
        | RSHIFT (lhs, rhs) -> buildShiftOp RSHIFT (lhs, rhs)
        | BOR (lhs, rhs) -> buildIntegralOp BOR (lhs, rhs)
        | BAND (lhs, rhs) -> buildIntegralOp BAND (lhs, rhs)
        | BXOR (lhs, rhs) -> buildIntegralOp BXOR (lhs, rhs)
        | AND (lhs, rhs) -> buildBooleanOpWith VerifyAreBooleans true AND (lhs, rhs)
        | OR (lhs, rhs) -> buildBooleanOpWith VerifyAreBooleans true OR (lhs, rhs)
        | EQ (lhs, rhs) -> buildBooleanOpWith (VerifyEqualityComparison context) false EQ (lhs, rhs)
        | NEQ (lhs, rhs) -> buildBooleanOpWith (VerifyEqualityComparison context) false NEQ (lhs, rhs)
        | NEG ex -> verifyAndBuildWith NEG VerifySupportsArithmetic ex
        | BNOT ex -> verifyAndBuildWith BNOT VerifyIsIntegral ex
        | NOT ex ->
            verifyAndBuildWith NOT (fun log arg ->
                VerifyIsBoolean log arg
                ResolvedType.New Bool) ex
