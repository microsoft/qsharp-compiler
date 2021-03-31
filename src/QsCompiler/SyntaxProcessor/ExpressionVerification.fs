// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.Expressions

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxGenerator
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.VerificationTools
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open Microsoft.Quantum.QsCompiler.Utils
open System.Collections.Generic
open System.Collections.Immutable

// utils for verifying types in expressions

/// Returns the string representation of a type.
let private showType: ResolvedType -> _ = SyntaxTreeToQsharp.Default.ToCode

let private ExprWithoutTypeArgs isMutable (ex, t, dep, range) =
    let inferred = InferredExpressionInformation.New(isMutable = isMutable, quantumDep = dep)
    TypedExpression.New(ex, ImmutableDictionary.Empty, t, inferred, range)

let internal rangeOrDefault expr =
    match expr.Expression, expr.Range with
    | _, Value range -> range
    | InvalidExpr, Null -> Range.Zero
    | _, Null -> failwith "valid expression without a range"

/// Calls the given addWarning function with a suitable warning code and the given range
/// if the given expression contains an operation call.
let private VerifyConditionalExecution (expr: TypedExpression) =
    let isOperationCall ex =
        match ex.Expression with
        | CallLikeExpression (callable, _) when not (TypedExpression.IsPartialApplication ex.Expression) ->
            match callable.ResolvedType.Resolution with
            | QsTypeKind.Operation _ -> true
            | _ -> false
        | _ -> false

    [
        if expr.Exists isOperationCall
        then QsCompilerDiagnostic.Warning (WarningCode.ConditionalEvaluationOfOperationCall, []) (rangeOrDefault expr)
    ]

/// Verifies that the given resolved type is indeed a user defined type,
/// adding an ExpectingUserDefinedType error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
/// Calls the given processing function on the user defined type, passing it the given function to add errors for the given range.
let private VerifyUdtWith processUdt (resolvedType: ResolvedType, range) =
    match resolvedType.Resolution with
    | QsTypeKind.UserDefinedType udt ->
        let diagnostics = ResizeArray()
        let resultType = udt |> processUdt (fun error -> QsCompilerDiagnostic.Error error range |> diagnostics.Add)
        resultType, Seq.toList diagnostics
    | _ ->
        ResolvedType.New InvalidType,
        [
            QsCompilerDiagnostic.Error (ErrorCode.ExpectingUserDefinedType, [ showType resolvedType ]) range
        ]

/// Verifies that both given resolved types are of kind Bool,
/// adding an ExpectingBoolExpr error with the corresponding range using addError otherwise.
/// If one of the given types is a missing type, also adds the corresponding ExpressionOfUnknownType error(s).
let private VerifyAreBooleans (inference: InferenceContext) lhs rhs =
    inference.Unify(ResolvedType.New Bool, lhs.ResolvedType)
    @ inference.Unify(ResolvedType.New Bool, rhs.ResolvedType)

/// Verifies that both given resolved types are of kind Int,
/// adding an ExpectingIntExpr error with the corresponding range using addError otherwise.
/// If one of the given types is a missing type, also adds the corresponding ExpressionOfUnknownType error(s).
let private VerifyAreIntegers (inference: InferenceContext) lhs rhs =
    inference.Unify(ResolvedType.New Int, lhs.ResolvedType)
    @ inference.Unify(ResolvedType.New Int, rhs.ResolvedType)

/// Verifies that the given resolved type is indeed of kind Int or BigInt,
/// adding an ExpectingIntegralExpr error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
let private VerifyIsIntegral (inference: InferenceContext) expr =
    expr.ResolvedType, inference.Constrain(expr.ResolvedType, Integral)

/// Verifies that both given resolved types are of kind Int or BigInt, and that both are the same,
/// adding an ArgumentMismatchInBinaryOp or ExpectingIntegralExpr error with the corresponding range using addError otherwise.
/// If one of the given types is a missing type, also adds the corresponding ExpressionOfUnknownType error(s).
let private VerifyIntegralOp (inference: InferenceContext) range lhs rhs =
    let exType, intersectDiagnostics = inference.Intersect(lhs.ResolvedType, rhs.ResolvedType)
    let exType = exType |> ResolvedType.withAllRanges (TypeRange.inferred range)
    let constrainDiagnostics = inference.Constrain(exType, Integral)
    exType, intersectDiagnostics @ constrainDiagnostics

/// Verifies that the given resolved type indeed supports arithmetic operations,
/// adding an InvalidTypeInArithmeticExpr error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
/// Returns the type of the arithmetic expression.
let private VerifySupportsArithmetic (inference: InferenceContext) expr =
    expr.ResolvedType, inference.Constrain(expr.ResolvedType, Numeric)

/// Verifies that given resolved types can be used within a binary arithmetic operator.
/// First tries to find a common base type for the two types,
/// adding an ArgumentMismatchInBinaryOp error for the corresponding range(s) using addError if no common base type can be found.
/// If a common base type exists, verifies that this base type supports arithmetic operations,
/// adding the corresponding error otherwise.
/// If one of the given types is a missing type, also adds the corresponding ExpressionOfUnknownType error(s).
/// Returns the type of the arithmetic expression (i.e. the found base type).
let private VerifyArithmeticOp (inference: InferenceContext) range lhs rhs =
    let exType, intersectDiagnostics = inference.Intersect(lhs.ResolvedType, rhs.ResolvedType)
    let exType = exType |> ResolvedType.withAllRanges (TypeRange.inferred range)
    let constrainDiagnostics = inference.Constrain(exType, Numeric)
    exType, intersectDiagnostics @ constrainDiagnostics

/// Verifies that the given resolved type indeed supports iteration,
/// adding an ExpectingIterableExpr error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
/// NOTE: returns the type of the iteration *item*.
let internal VerifyIsIterable (inference: InferenceContext) expr =
    let range = rangeOrDefault expr
    let item = inference.Fresh range
    item, inference.Constrain(expr.ResolvedType, Iterable item)

/// Verifies that given resolved types can be used within a concatenation operator.
/// First tries to find a common base type for the two types,
/// adding an ArgumentMismatchInBinaryOp error for the corresponding range(s) using addError if no common base type can be found.
/// If a common base type exists, verifies that this base type supports concatenation,
/// adding the corresponding error otherwise.
/// If one of the given types is a missing type, also adds the corresponding ExpressionOfUnknownType error(s).
/// Returns the type of the concatenation expression (i.e. the found base type).
let private VerifyConcatenation (inference: InferenceContext) range lhs rhs =
    let exType, intersectDiagnostics = inference.Intersect(lhs.ResolvedType, rhs.ResolvedType)
    let exType = exType |> ResolvedType.withAllRanges (TypeRange.inferred range)
    let constrainDiagnostics = inference.Constrain(exType, Semigroup)
    exType, intersectDiagnostics @ constrainDiagnostics

/// Verifies that given resolved types can be used within an equality comparison expression.
/// First tries to find a common base type for the two types,
/// adding an ArgumentMismatchInBinaryOp error for the corresponding range(s) using addError if no common base type can be found.
/// If a common base type exists, verifies that this base type supports equality comparison,
/// adding the corresponding error otherwise.
/// If one of the given types is a missing type, also adds the corresponding ExpressionOfUnknownType error(s).
let private VerifyEqualityComparison (inference: InferenceContext) range lhs rhs =
    let exType, intersectDiagnostics = inference.Intersect(lhs.ResolvedType, rhs.ResolvedType)
    let exType = exType |> ResolvedType.withAllRanges (TypeRange.inferred range)
    let constrainDiagnostics = inference.Constrain(exType, Equatable)
    intersectDiagnostics @ constrainDiagnostics

/// Given a list of all item types and there corresponding ranges, verifies that a value array literal can be built from them.
/// Adds a MissingExprInArray error with the corresponding range using addError if one of the given types is missing.
/// Filtering all missing or invalid types, tries to find a common base type for the remaining item types,
/// and adds a MultipleTypesInArray error for the entire array if this fails.
/// Returns the inferred type of the array.
/// Returns an array with missing base type if the given list of item types is empty.
let private VerifyValueArray (inference: InferenceContext) range exprs =
    let diagnostics = ResizeArray()

    for expr in exprs do
        if expr.ResolvedType.isMissing then
            QsCompilerDiagnostic.Error (ErrorCode.MissingExprInArray, []) (rangeOrDefault expr)
            |> diagnostics.Add

    let types =
        exprs
        |> Seq.map (fun e -> e.ResolvedType)
        |> Seq.filter (fun t -> not t.isInvalid && not t.isMissing)

    if Seq.isEmpty types && Seq.isEmpty exprs then
        inference.Fresh range |> ArrayType |> ResolvedType.create (Inferred range), Seq.toList diagnostics
    elif Seq.isEmpty types then
        ResolvedType.New InvalidType |> ArrayType |> ResolvedType.create (Inferred range), Seq.toList diagnostics
    else
        types
        |> Seq.reduce (fun left right ->
            let intersectionType, intersectionDiagnostics = inference.Intersect(left, right)
            List.iter diagnostics.Add intersectionDiagnostics
            intersectionType |> ResolvedType.withAllRanges right.Range)
        |> ResolvedType.withAllRanges (Inferred range)
        |> ArrayType
        |> ResolvedType.create (Inferred range),
        Seq.toList diagnostics

/// Verifies that the given resolved type supports numbered item access,
/// adding an ItemAccessForNonArray error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
let internal VerifyNumberedItemAccess (inference: InferenceContext) expr =
    let range = rangeOrDefault expr
    let resultType = inference.Fresh range

    resultType, inference.Constrain(expr.ResolvedType, Indexed(ResolvedType.New Range, resultType))

/// Verifies that the given type of the left hand side of an array item expression is indeed an array type (or invalid),
/// adding an ItemAccessForNonArray error with the corresponding range using addError otherwise.
/// Verifies that the given type of the expression within the item access is either of type Int or Range,
/// adding an InvalidArrayItemIndex error with the corresponding range using addError otherwise.
/// Returns the type of the array item expression.
let private VerifyArrayItem (inference: InferenceContext) array index =
    let range = rangeOrDefault array
    let itemType = inference.Fresh range
    itemType, inference.Constrain(array.ResolvedType, Indexed(index.ResolvedType, itemType))

/// Verifies that the Adjoint functor can be applied to an expression of the given type,
/// adding an InvalidAdjointApplication error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
/// Returns the type of the functor application expression.
let private VerifyAdjointApplication (inference: InferenceContext) expr =
    expr.ResolvedType, inference.Constrain(expr.ResolvedType, Constraint.Adjointable)

/// Verifies that the Controlled functor can be applied to an expression of the given type,
/// adding an InvalidControlledApplication error with the given range using addError otherwise.
/// If the given type is a missing type, also adds the corresponding ExpressionOfUnknownType error.
/// Returns the type of the functor application expression.
let private VerifyControlledApplication (inference: InferenceContext) expr =
    let range = rangeOrDefault expr
    let controlled = inference.Fresh range
    controlled, inference.Constrain(expr.ResolvedType, Constraint.Controllable controlled)

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
let private VerifyIdentifier (inference: InferenceContext) (symbols: SymbolTracker) (sym, typeArgs) =
    let diagnostics = ResizeArray()

    let resolvedTargs =
        typeArgs
        |> QsNullable<_>
            .Map(fun (args: ImmutableArray<QsType>) ->
                args
                |> Seq.map (fun tArg ->
                    match tArg.Type with
                    | MissingType -> ResolvedType.New MissingType
                    | _ -> symbols.ResolveType diagnostics.Add tArg))
        |> QsNullable<_>.Map(fun args -> args.ToImmutableArray())

    let resId, typeParams = symbols.ResolveIdentifier diagnostics.Add sym
    let identifier, info = Identifier(resId.VariableName, resolvedTargs), resId.InferredInformation

    // resolve type parameters (if any) with the given type arguments
    // Note: type parameterized objects are never mutable - remember they are not the same as an identifier containing a template...!
    let invalidWithoutTargs mut =
        (identifier, ResolvedType.New InvalidType, info.HasLocalQuantumDependency, sym.Range)
        |> ExprWithoutTypeArgs mut

    match resId.VariableName, resolvedTargs with
    | InvalidIdentifier, Null -> invalidWithoutTargs true, Seq.toList diagnostics
    | InvalidIdentifier, Value _ -> invalidWithoutTargs false, Seq.toList diagnostics
    | LocalVariable _, Null ->
        ExprWithoutTypeArgs info.IsMutable (identifier, resId.Type, info.HasLocalQuantumDependency, sym.Range),
        Seq.toList diagnostics
    | LocalVariable _, Value _ ->
        invalidWithoutTargs false,
        QsCompilerDiagnostic.Error (ErrorCode.IdentifierCannotHaveTypeArguments, []) sym.RangeOrDefault
        :: Seq.toList diagnostics
    | GlobalCallable _, Value res when res.Length <> typeParams.Length ->
        invalidWithoutTargs false,
        QsCompilerDiagnostic.Error
            (ErrorCode.WrongNumberOfTypeArguments, [ string typeParams.Length ])
            sym.RangeOrDefault
        :: Seq.toList diagnostics
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
                else KeyValuePair(param, inference.Fresh sym.RangeOrDefault))
            |> ImmutableDictionary.CreateRange

        let identifier =
            if resolutions.IsEmpty
            then identifier
            else Identifier(GlobalCallable name, ImmutableArray.CreateRange resolutions.Values |> Value)

        let resolvedType = (replaceTypes resolutions).OnType resId.Type |> inference.Resolve
        let exInfo = InferredExpressionInformation.New(isMutable = false, quantumDep = info.HasLocalQuantumDependency)
        TypedExpression.New(identifier, resolutions, resolvedType, exInfo, sym.Range), Seq.toList diagnostics

/// Verifies that an expression of the given rhsType, used within the given parent (i.e. specialization declaration),
/// can be used when an expression of expectedType is expected by callaing TypeMatchArgument.
/// Generates an error with the given error code mismatchErr for the given range if this is not the case.
/// Verifies that any internal type parameters are "matched" only with themselves (or with an invalid type),
/// and generates a ConstrainsTypeParameter error if this is not the case.
/// If the given rhsEx is Some value, verifies whether it contains an identifier referring to the parent
/// that is not part of a call-like expression but does not specify all needed type arguments.
/// Calls the given function addError on all generated errors.
/// IMPORTANT: ignores any external type parameter occuring in expectedType without raising an error!
let internal VerifyAssignment (inference: InferenceContext) expectedType mismatchErr rhs =
    [
        if inference.Unify(expectedType, rhs.ResolvedType) |> List.isEmpty |> not then
            QsCompilerDiagnostic.Error
                (mismatchErr, [ showType rhs.ResolvedType; showType expectedType ])
                (rangeOrDefault rhs)
    ]

// utils for building TypedExpressions from QsExpressions

type QsExpression with
    /// Given a SymbolTracker containing all the symbols which are currently defined,
    /// recursively computes the corresponding typed expression for a Q# expression.
    /// Calls addDiagnostic on each diagnostic generated during the resolution.
    /// Returns the computed typed expression.
    member this.Resolve ({ Symbols = symbols; Inference = inference } as context) diagnose =
        let resolve (item: QsExpression) = item.Resolve context diagnose

        let takeDiagnostics (value, diagnostics) =
            List.iter diagnose diagnostics
            value

        /// Given and expression used for array slicing, as well as the type of the sliced expression,
        /// generates suitable boundaries for open ended ranges and returns the resolved slicing expression as Some.
        /// Returns None if the slicing expression is trivial, i.e. if the sliced array does not deviate from the orginal one.
        /// NOTE: Does *not* generated any diagnostics related to the given type for the array to slice.
        let resolveSlicing array (index: QsExpression) =
            let array = { array with ResolvedType = inference.Resolve array.ResolvedType }

            let invalidRangeDelimiter =
                ExprWithoutTypeArgs
                    false
                    (InvalidExpr,
                     ResolvedType.New InvalidType,
                     array.InferredInformation.HasLocalQuantumDependency,
                     Null)

            let validSlicing step =
                match array.ResolvedType.Resolution with
                | ArrayType _ ->
                    step |> Option.forall (fun expr -> Int = (inference.Resolve expr.ResolvedType).Resolution)
                | _ -> false

            let conditionalIntExpr (cond: TypedExpression) ifTrue ifFalse =
                let quantumDep =
                    [ cond; ifTrue; ifFalse ]
                    |> List.exists (fun ex -> ex.InferredInformation.HasLocalQuantumDependency)

                ExprWithoutTypeArgs false (CONDITIONAL(cond, ifTrue, ifFalse), ResolvedType.New Int, quantumDep, Null)

            let openStartInSlicing =
                function
                | Some step when Some step |> validSlicing ->
                    conditionalIntExpr (IsNegative step) (LengthMinusOne array) (SyntaxGenerator.IntLiteral 0L)
                | _ -> SyntaxGenerator.IntLiteral 0L

            let openEndInSlicing =
                function
                | Some step when Some step |> validSlicing ->
                    conditionalIntExpr (IsNegative step) (SyntaxGenerator.IntLiteral 0L) (LengthMinusOne array)
                | ex -> if validSlicing ex then LengthMinusOne array else invalidRangeDelimiter

            let resolveSlicingRange start step end' =
                let integerExpr ex =
                    let ex = resolve ex
                    inference.Unify(ResolvedType.New Int, ex.ResolvedType) |> List.iter diagnose
                    ex

                let resolvedStep = step |> Option.map integerExpr

                let resolveWith build (ex: QsExpression) =
                    if ex.isMissing then build resolvedStep else integerExpr ex

                let resolvedStart, resolvedEnd =
                    start |> resolveWith openStartInSlicing, end' |> resolveWith openEndInSlicing

                match resolvedStep with
                | Some resolvedStep ->
                    SyntaxGenerator.RangeLiteral(SyntaxGenerator.RangeLiteral(resolvedStart, resolvedStep), resolvedEnd)
                | None -> SyntaxGenerator.RangeLiteral(resolvedStart, resolvedEnd)

            match index.Expression with
            | RangeLiteral (lhs, rhs) when lhs.isMissing && rhs.isMissing ->
                // case arr[...]
                None
            | RangeLiteral (lhs, end') ->
                match lhs.Expression with
                | RangeLiteral (start, step) ->
                    // cases arr[...step..ex], arr[ex..step...], arr[ex1..step..ex2], and arr[...ex...]
                    resolveSlicingRange start (Some step) end'
                | _ ->
                    // case arr[...ex], arr[ex...] and arr[ex1..ex2]
                    resolveSlicingRange lhs None end'
                |> Some
            | _ ->
                // case arr[ex]
                resolve index |> Some

        /// Resolves and verifies the interpolated expressions, and returns the StringLiteral as typed expression.
        let buildStringLiteral (literal, interpolated) =
            let resInterpol = interpolated |> Seq.map resolve |> ImmutableArray.CreateRange
            let localQdependency = resInterpol |> Seq.exists (fun r -> r.InferredInformation.HasLocalQuantumDependency)

            (StringLiteral(literal, resInterpol),
             String |> ResolvedType.create (TypeRange.inferred this.Range),
             localQdependency,
             this.Range)
            |> ExprWithoutTypeArgs false

        /// <summary>
        /// Resolves and verifies all given items, and returns the corresponding ValueTuple as typed expression.
        /// If the ValueTuple contains only one item, the item is returned instead (i.e. arity-1 tuple expressions are stripped).
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="items"/> is empty.</exception>
        let buildTuple items =
            let items = items |> Seq.map resolve |> ImmutableArray.CreateRange
            let types = items |> Seq.map (fun x -> x.ResolvedType) |> ImmutableArray.CreateRange
            let localQdependency = items |> Seq.exists (fun item -> item.InferredInformation.HasLocalQuantumDependency)

            if items.IsEmpty then
                failwith "tuple expression requires at least one tuple item"
            elif items.Length = 1 then
                items.[0]
            else
                (ValueTuple items,
                 TupleType types |> ResolvedType.create (TypeRange.inferred this.Range),
                 localQdependency,
                 this.Range)
                |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given array base type and the expression denoting the length of the array,
        /// and returns the corrsponding NewArray expression as typed expression
        let buildNewArray (bType, ex: QsExpression) =
            let ex = resolve ex
            inference.Unify(ResolvedType.New Int, ex.ResolvedType) |> List.iter diagnose

            let resolvedBase = symbols.ResolveType diagnose bType
            let arrType = resolvedBase |> ArrayType |> ResolvedType.create (TypeRange.inferred this.Range)
            let quantumDep = ex.InferredInformation.HasLocalQuantumDependency
            (NewArray(resolvedBase, ex), arrType, quantumDep, this.Range) |> ExprWithoutTypeArgs false

        /// Resolves and verifies all given items of a value array literal, and returns the corresponding ValueArray as typed expression.
        let buildValueArray values =
            let values = values |> Seq.map resolve |> ImmutableArray.CreateRange
            let resolvedType = values |> VerifyValueArray inference this.RangeOrDefault |> takeDiagnostics
            let localQdependency = values |> Seq.exists (fun item -> item.InferredInformation.HasLocalQuantumDependency)
            (ValueArray values, resolvedType, localQdependency, this.Range) |> ExprWithoutTypeArgs false

        /// Resolves and verifies the sized array constructor expression and returns it as a typed expression.
        let buildSizedArray value (size: QsExpression) =
            let value = resolve value
            let arrayType = ArrayType value.ResolvedType |> ResolvedType.create (TypeRange.inferred this.Range)
            let size = resolve size
            inference.Unify(ResolvedType.New Int, size.ResolvedType) |> List.iter diagnose

            let quantumDependency =
                value.InferredInformation.HasLocalQuantumDependency
                || size.InferredInformation.HasLocalQuantumDependency

            (SizedArray(value, size), arrayType, quantumDependency, this.Range) |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given array expression and index expression of an array item access expression,
        /// and returns the corresponding ArrayItem expression as typed expression.
        let buildArrayItem (arr, idx: QsExpression) =
            let arr = resolve arr

            match resolveSlicing arr idx with
            | None -> { arr with ResolvedType = VerifyNumberedItemAccess inference arr |> takeDiagnostics }
            | Some resolvedIdx ->
                let resolvedType = VerifyArrayItem inference arr resolvedIdx |> takeDiagnostics

                let localQdependency =
                    arr.InferredInformation.HasLocalQuantumDependency
                    || resolvedIdx.InferredInformation.HasLocalQuantumDependency

                (ArrayItem(arr, resolvedIdx), resolvedType, localQdependency, this.Range)
                |> ExprWithoutTypeArgs false

        /// Given a symbol used to represent an item name in an item access or update expression,
        /// returns the an identifier that can be used to represent the corresponding item name.
        /// Adds an error if the given symbol is not either invalid or an unqualified symbol.
        let buildItemName (sym: QsSymbol) =
            match sym.Symbol with
            | InvalidSymbol -> InvalidIdentifier
            | Symbol name -> LocalVariable name
            | _ ->
                QsCompilerDiagnostic.Error (ErrorCode.ExpectingItemName, []) sym.RangeOrDefault |> diagnose
                InvalidIdentifier

        /// Resolves and verifies the given expression and item name of a named item access expression,
        /// and returns the corresponding NamedItem expression as typed expression.
        let buildNamedItem (ex, acc: QsSymbol) =
            let ex = resolve ex
            let itemName = acc |> buildItemName
            let udtType = inference.Resolve ex.ResolvedType
            let exType = VerifyUdtWith (symbols.GetItemType itemName) (udtType, rangeOrDefault ex) |> takeDiagnostics
            let localQdependency = ex.InferredInformation.HasLocalQuantumDependency
            (NamedItem(ex, itemName), exType, localQdependency, this.Range) |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given left hand side, access expression, and right hand side of a copy-and-update expression,
        /// and returns the corresponding copy-and-update expression as typed expression.
        let buildCopyAndUpdate (lhs, accEx: QsExpression, rhs) =
            let lhs = resolve lhs
            let lhs = { lhs with ResolvedType = inference.Resolve lhs.ResolvedType }
            let rhs = resolve rhs

            let resolvedCopyAndUpdateExpr accEx =
                let localQdependency =
                    [ lhs; accEx; rhs ]
                    |> Seq.map (fun ex -> ex.InferredInformation.HasLocalQuantumDependency)
                    |> Seq.contains true

                (CopyAndUpdate(lhs, accEx, rhs), lhs.ResolvedType, localQdependency, this.Range)
                |> ExprWithoutTypeArgs false

            match (lhs.ResolvedType.Resolution, accEx.Expression) with
            | UserDefinedType _, Identifier (sym, Null) ->
                let itemName = buildItemName sym

                let itemType =
                    VerifyUdtWith (symbols.GetItemType itemName) (lhs.ResolvedType, rangeOrDefault lhs)
                    |> takeDiagnostics

                VerifyAssignment inference itemType ErrorCode.TypeMismatchInCopyAndUpdateExpr rhs
                |> List.iter diagnose

                let resAccEx =
                    (Identifier(itemName, Null), itemType, lhs.InferredInformation.HasLocalQuantumDependency, sym.Range)
                    |> ExprWithoutTypeArgs false

                resAccEx |> resolvedCopyAndUpdateExpr
            | _ -> // by default, assume that the update expression is supposed to be for an array
                match resolveSlicing lhs accEx with
                | None -> // indicates a trivial slicing of the form "..." resulting in a complete replacement
                    let expectedRhs = VerifyNumberedItemAccess inference lhs |> takeDiagnostics

                    VerifyAssignment inference expectedRhs ErrorCode.TypeMismatchInCopyAndUpdateExpr rhs
                    |> List.iter diagnose

                    { rhs with ResolvedType = expectedRhs }
                | Some resAccEx -> // indicates either a index or index range to update
                    let expectedRhs = VerifyArrayItem inference lhs resAccEx |> takeDiagnostics

                    VerifyAssignment inference expectedRhs ErrorCode.TypeMismatchInCopyAndUpdateExpr rhs
                    |> List.iter diagnose

                    resAccEx |> resolvedCopyAndUpdateExpr

        /// Resolves and verifies the given left hand side and right hand side of a range operator,
        /// and returns the corresponding RANGE expression as typed expression.
        /// NOTE: handles both the case of a range with and without explicitly specified step size
        /// *under the assumption* that the range operator is left associative.
        let buildRange (lhs: QsExpression, rhs: QsExpression) =
            let rhs = resolve rhs
            inference.Unify(ResolvedType.New Int, rhs.ResolvedType) |> List.iter diagnose

            let lhs =
                match lhs.Expression with
                | RangeLiteral (start, step) ->
                    let start = resolve start
                    let step = resolve step
                    VerifyAreIntegers inference start step |> List.iter diagnose

                    let localQdependency =
                        start.InferredInformation.HasLocalQuantumDependency
                        || step.InferredInformation.HasLocalQuantumDependency

                    (RangeLiteral(start, step),
                     Range |> ResolvedType.create (TypeRange.inferred this.Range),
                     localQdependency,
                     this.Range)
                    |> ExprWithoutTypeArgs false
                | _ ->
                    resolve lhs
                    |> (fun resStart ->
                        inference.Unify(ResolvedType.New Int, resStart.ResolvedType) |> List.iter diagnose
                        resStart)

            let localQdependency =
                lhs.InferredInformation.HasLocalQuantumDependency
                || rhs.InferredInformation.HasLocalQuantumDependency

            (RangeLiteral(lhs, rhs),
             Range |> ResolvedType.create (TypeRange.inferred this.Range),
             localQdependency,
             this.Range)
            |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given expression with the given verification function,
        /// and returns the corresponding expression built with buildExprKind as typed expression.
        let verifyAndBuildWith buildExprKind verify (ex: QsExpression) =
            let ex = resolve ex
            let exType = verify ex |> takeDiagnostics

            (buildExprKind ex, exType, ex.InferredInformation.HasLocalQuantumDependency, this.Range)
            |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given left hand side and right hand side of an arithmetic operator,
        /// and returns the corresponding expression built with buildExprKind as typed expression.
        let buildArithmeticOp buildExprKind (lhs, rhs) =
            let lhs = resolve lhs
            let rhs = resolve rhs
            let resolvedType = VerifyArithmeticOp inference this.Range lhs rhs |> takeDiagnostics

            let localQdependency =
                lhs.InferredInformation.HasLocalQuantumDependency
                || rhs.InferredInformation.HasLocalQuantumDependency

            (buildExprKind (lhs, rhs), resolvedType, localQdependency, this.Range) |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given left hand side and right hand side of an addition operator,
        /// and returns the corresponding ADD expression as typed expression.
        /// Note: ADD is used for both arithmetic expressions as well as concatenation expressions.
        /// If the resolved type of the given lhs supports concatenation, then the verification is done for a concatenation expression,
        /// and otherwise it is done for an arithmetic expression.
        let buildAddition (lhs, rhs) =
            let lhs = resolve lhs
            let rhs = resolve rhs
            let resolvedType = VerifyConcatenation inference this.Range lhs rhs |> takeDiagnostics

            let localQdependency =
                lhs.InferredInformation.HasLocalQuantumDependency
                || rhs.InferredInformation.HasLocalQuantumDependency

            (ADD(lhs, rhs), resolvedType, localQdependency, this.Range) |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given left hand side and right hand side of a power operator,
        /// and returns the corresponding POW expression as typed expression.
        /// Note: POW can take two integers or two doubles, in which case the result is a double, or it can take a big
        /// integer and an integer, in which case the result is a big integer.
        let buildPower (lhs, rhs) =
            let lhs = resolve lhs
            let rhs = resolve rhs

            let resolvedType =
                if lhs.ResolvedType.Resolution = BigInt then
                    inference.Unify(ResolvedType.New Int, rhs.ResolvedType) |> List.iter diagnose
                    lhs.ResolvedType
                else
                    VerifyArithmeticOp inference this.Range lhs rhs |> takeDiagnostics

            let localQdependency =
                lhs.InferredInformation.HasLocalQuantumDependency
                || rhs.InferredInformation.HasLocalQuantumDependency

            (POW(lhs, rhs), resolvedType, localQdependency, this.Range) |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given left hand side and right hand side of a binary integral operator,
        /// and returns the corresponding expression built with buildExprKind as typed expression of type Int or BigInt, as appropriate.
        let buildIntegralOp buildExprKind (lhs, rhs) =
            let lhs = resolve lhs
            let rhs = resolve rhs
            let resolvedType = VerifyIntegralOp inference this.Range lhs rhs |> takeDiagnostics

            let localQdependency =
                lhs.InferredInformation.HasLocalQuantumDependency
                || rhs.InferredInformation.HasLocalQuantumDependency

            (buildExprKind (lhs, rhs), resolvedType, localQdependency, this.Range) |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given left hand side and right hand side of a shift operator,
        /// and returns the corresponding expression built with buildExprKind as typed expression of type Int or BigInt, as appropriate.
        let buildShiftOp buildExprKind (lhs, rhs) =
            let lhs = resolve lhs
            let rhs = resolve rhs
            let resolvedType = VerifyIsIntegral inference lhs |> takeDiagnostics
            inference.Unify(ResolvedType.New Int, rhs.ResolvedType) |> List.iter diagnose

            let localQdependency =
                lhs.InferredInformation.HasLocalQuantumDependency
                || rhs.InferredInformation.HasLocalQuantumDependency

            (buildExprKind (lhs, rhs), resolvedType, localQdependency, this.Range) |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given left hand side and right hand side of a binary boolean operator,
        /// and returns the corresponding expression built with buildExprKind as typed expression of type Bool.
        let buildBooleanOpWith verify shortCircuits buildExprKind (lhs, rhs) =
            let lhs = resolve lhs
            let rhs = resolve rhs
            verify lhs rhs |> List.iter diagnose

            if shortCircuits
            then VerifyConditionalExecution rhs |> List.iter diagnose

            let localQdependency =
                lhs.InferredInformation.HasLocalQuantumDependency
                || rhs.InferredInformation.HasLocalQuantumDependency

            (buildExprKind (lhs, rhs), ResolvedType.New Bool, localQdependency, this.Range)
            |> ExprWithoutTypeArgs false

        /// Resolves and verifies the given condition, left hand side, and right hand side of a conditional expression (if-else-shorthand),
        /// and returns the corresponding conditional expression as typed expression.
        let buildConditional (cond, ifTrue, ifFalse) =
            let cond = resolve cond
            let ifTrue = resolve ifTrue
            let ifFalse = resolve ifFalse
            inference.Unify(ResolvedType.New Bool, cond.ResolvedType) |> List.iter diagnose
            VerifyConditionalExecution ifTrue |> List.iter diagnose
            VerifyConditionalExecution ifFalse |> List.iter diagnose

            let exType =
                inference.Intersect(ifTrue.ResolvedType, ifFalse.ResolvedType)
                |> takeDiagnostics
                |> ResolvedType.withAllRanges (TypeRange.inferred this.Range)

            let localQdependency =
                [ cond; ifTrue; ifFalse ] |> Seq.exists (fun ex -> ex.InferredInformation.HasLocalQuantumDependency)

            (CONDITIONAL(cond, ifTrue, ifFalse), exType, localQdependency, this.Range)
            |> ExprWithoutTypeArgs false

        /// Resolves the given expression and verifies that its type is indeed a user defined type.
        /// Determines the underlying type of the user defined type and returns the corresponding UNWRAP expression as typed expression of that type.
        let buildUnwrap ex =
            let ex = resolve ex
            let exType = inference.Fresh this.RangeOrDefault
            inference.Constrain(ex.ResolvedType, Wrapped exType) |> List.iter diagnose

            (UnwrapApplication ex, exType, ex.InferredInformation.HasLocalQuantumDependency, this.Range)
            |> ExprWithoutTypeArgs false

        let rec partialArgType (argType: ResolvedType) =
            match argType.Resolution with
            | MissingType ->
                let param = inference.Fresh this.RangeOrDefault
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

                argType |> ResolvedType.withKind (ImmutableArray.CreateRange items |> TupleType), missing
            | _ -> argType, None

        /// Resolves and verifies the given left hand side and right hand side of a call expression,
        /// and returns the corresponding expression as typed expression.
        let buildCall (callable: QsExpression, arg: QsExpression) =
            let callable = resolve callable
            let arg = resolve arg
            let callExpression = CallLikeExpression(callable, arg)
            let argType, partialType = partialArgType arg.ResolvedType
            let isPartial = Option.isSome partialType

            if not isPartial then
                inference.Constrain
                    (callable.ResolvedType, Set.ofSeq symbols.RequiredFunctorSupport |> CanGenerateFunctors)
                |> List.iter diagnose

            let output = inference.Fresh this.RangeOrDefault

            if isPartial || context.IsInOperation then
                inference.Constrain(callable.ResolvedType, Callable(argType, output)) |> List.iter diagnose
            else
                // TODO: This error message could be improved. Calling an operation from a function is currently a type
                // mismatch error.
                inference.Unify
                    (QsTypeKind.Function(argType, output) |> ResolvedType.create (TypeRange.inferred callable.Range),
                     callable.ResolvedType)
                |> List.iter diagnose

            let resultType =
                match partialType with
                | Some missing ->
                    let result = inference.Fresh this.RangeOrDefault

                    inference.Constrain(callable.ResolvedType, HasPartialApplication(missing, result))
                    |> List.iter diagnose

                    result
                | None -> output

            let isFunction =
                match (inference.Resolve callable.ResolvedType).Resolution with
                | QsTypeKind.Function _ -> true
                | _ -> false // Be pessimistic and assume the callable is an operation.

            let hasQuantumDependency =
                if isPartial || isFunction then
                    callable.InferredInformation.HasLocalQuantumDependency
                    || arg.InferredInformation.HasLocalQuantumDependency
                else
                    true

            let info = InferredExpressionInformation.New(isMutable = false, quantumDep = hasQuantumDependency)
            TypedExpression.New(callExpression, callable.TypeParameterResolutions, resultType, info, this.Range)

        match this.Expression with
        | InvalidExpr ->
            (InvalidExpr, InvalidType |> ResolvedType.create (TypeRange.inferred this.Range), false, this.Range)
            |> ExprWithoutTypeArgs true // choosing the more permissive option here
        | MissingExpr ->
            (MissingExpr, MissingType |> ResolvedType.create (TypeRange.inferred this.Range), false, this.Range)
            |> ExprWithoutTypeArgs false
        | UnitValue ->
            (UnitValue, UnitType |> ResolvedType.create (TypeRange.inferred this.Range), false, this.Range)
            |> ExprWithoutTypeArgs false
        | Identifier (sym, tArgs) -> VerifyIdentifier inference symbols (sym, tArgs) |> takeDiagnostics
        | CallLikeExpression (method, arg) -> buildCall (method, arg)
        | AdjointApplication ex -> verifyAndBuildWith AdjointApplication (VerifyAdjointApplication inference) ex
        | ControlledApplication ex ->
            verifyAndBuildWith ControlledApplication (VerifyControlledApplication inference) ex
        | UnwrapApplication ex -> buildUnwrap ex
        | ValueTuple items -> buildTuple items
        | ArrayItem (arr, idx) -> buildArrayItem (arr, idx)
        | NamedItem (ex, acc) -> buildNamedItem (ex, acc)
        | ValueArray values -> buildValueArray values
        | NewArray (baseType, ex) -> buildNewArray (baseType, ex)
        | SizedArray (value, size) -> buildSizedArray value size
        | IntLiteral i ->
            (IntLiteral i, Int |> ResolvedType.create (TypeRange.inferred this.Range), false, this.Range)
            |> ExprWithoutTypeArgs false
        | BigIntLiteral b ->
            (BigIntLiteral b, BigInt |> ResolvedType.create (TypeRange.inferred this.Range), false, this.Range)
            |> ExprWithoutTypeArgs false
        | DoubleLiteral d ->
            (DoubleLiteral d, Double |> ResolvedType.create (TypeRange.inferred this.Range), false, this.Range)
            |> ExprWithoutTypeArgs false
        | BoolLiteral b ->
            (BoolLiteral b, Bool |> ResolvedType.create (TypeRange.inferred this.Range), false, this.Range)
            |> ExprWithoutTypeArgs false
        | ResultLiteral r ->
            (ResultLiteral r, Result |> ResolvedType.create (TypeRange.inferred this.Range), false, this.Range)
            |> ExprWithoutTypeArgs false
        | PauliLiteral p ->
            (PauliLiteral p, Pauli |> ResolvedType.create (TypeRange.inferred this.Range), false, this.Range)
            |> ExprWithoutTypeArgs false
        | StringLiteral (s, exs) -> buildStringLiteral (s, exs)
        | RangeLiteral (lhs, rEnd) -> buildRange (lhs, rEnd)
        | CopyAndUpdate (lhs, accEx, rhs) -> buildCopyAndUpdate (lhs, accEx, rhs)
        | CONDITIONAL (cond, ifTrue, ifFalse) -> buildConditional (cond, ifTrue, ifFalse)
        | ADD (lhs, rhs) -> buildAddition (lhs, rhs) // addition takes a special role since it is used for both arithmetic and concatenation expressions
        | SUB (lhs, rhs) -> buildArithmeticOp SUB (lhs, rhs)
        | MUL (lhs, rhs) -> buildArithmeticOp MUL (lhs, rhs)
        | DIV (lhs, rhs) -> buildArithmeticOp DIV (lhs, rhs)
        | LT (lhs, rhs) ->
            buildBooleanOpWith (fun lhs rhs -> VerifyArithmeticOp inference this.Range lhs rhs |> snd) false LT
                (lhs, rhs)
        | LTE (lhs, rhs) ->
            buildBooleanOpWith (fun lhs rhs -> VerifyArithmeticOp inference this.Range lhs rhs |> snd) false LTE
                (lhs, rhs)
        | GT (lhs, rhs) ->
            buildBooleanOpWith (fun lhs rhs -> VerifyArithmeticOp inference this.Range lhs rhs |> snd) false GT
                (lhs, rhs)
        | GTE (lhs, rhs) ->
            buildBooleanOpWith (fun lhs rhs -> VerifyArithmeticOp inference this.Range lhs rhs |> snd) false GTE
                (lhs, rhs)
        | POW (lhs, rhs) -> buildPower (lhs, rhs) // power takes a special role because you can raise integers and doubles to integer and double powers, but bigint only to integer powers
        | MOD (lhs, rhs) -> buildIntegralOp MOD (lhs, rhs)
        | LSHIFT (lhs, rhs) -> buildShiftOp LSHIFT (lhs, rhs)
        | RSHIFT (lhs, rhs) -> buildShiftOp RSHIFT (lhs, rhs)
        | BOR (lhs, rhs) -> buildIntegralOp BOR (lhs, rhs)
        | BAND (lhs, rhs) -> buildIntegralOp BAND (lhs, rhs)
        | BXOR (lhs, rhs) -> buildIntegralOp BXOR (lhs, rhs)
        | AND (lhs, rhs) -> buildBooleanOpWith (VerifyAreBooleans inference) true AND (lhs, rhs)
        | OR (lhs, rhs) -> buildBooleanOpWith (VerifyAreBooleans inference) true OR (lhs, rhs)
        | EQ (lhs, rhs) -> buildBooleanOpWith (VerifyEqualityComparison inference this.Range) false EQ (lhs, rhs)
        | NEQ (lhs, rhs) -> buildBooleanOpWith (VerifyEqualityComparison inference this.Range) false NEQ (lhs, rhs)
        | NEG ex -> verifyAndBuildWith NEG (VerifySupportsArithmetic inference) ex
        | BNOT ex -> verifyAndBuildWith BNOT (VerifyIsIntegral inference) ex
        | NOT ex ->
            ex
            |> verifyAndBuildWith NOT (fun expr ->
                   Bool |> ResolvedType.create (TypeRange.inferred this.Range),
                   inference.Unify(ResolvedType.New Bool, expr.ResolvedType))
