﻿// Copyright (c) Microsoft Corporation.
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
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open System.Collections.Generic
open System.Collections.Immutable

// utils for verifying types in expressions

/// Returns the string representation of a type.
let private showType : ResolvedType -> _ = SyntaxTreeToQsharp.Default.ToCode

/// Returns true if the type is a function type.
let private isFunction (resolvedType: ResolvedType) =
    match resolvedType.Resolution with
    | QsTypeKind.Function _ -> true
    | _ -> false

/// Returns true if the type is an operation type.
let private isOperation (resolvedType: ResolvedType) =
    match resolvedType.Resolution with
    | QsTypeKind.Operation _ -> true
    | _ -> false

/// <summary>
/// Instantiates fresh type parameters for each missing type in <paramref name="argType"/>.
/// </summary>
/// <returns>
/// <list>
/// <item><paramref name="argType"/> with every missing type replaced with a fresh type parameter.</item>
/// <item>The type of the partially applied argument containing only those fresh type parameters, if any exist.</item>
/// </list>
/// </returns>
let rec private partialArgType (inference: InferenceContext) (argType: ResolvedType) =
    match argType.Resolution with
    | MissingType ->
        let param = inference.Fresh(argType.Range |> TypeRange.tryRange |> QsNullable.defaultValue Range.Zero)
        param, Some param
    | TupleType items ->
        let items, missing =
            (items |> Seq.map (partialArgType inference), ([], []))
            ||> Seq.foldBack (fun (item, params1) (items, params2) -> item :: items, Option.toList params1 @ params2)

        let missing =
            if List.isEmpty missing then
                None
            else
                ImmutableArray.CreateRange missing |> TupleType |> ResolvedType.New |> Some

        argType |> ResolvedType.withKind (ImmutableArray.CreateRange items |> TupleType), missing
    | _ -> argType, None

/// <summary>
/// Returns the range of <paramref name="expr"/>, or the empty range if <paramref name="expr"/> is an invalid expression
/// without a range.
/// </summary>
/// <exception cref="Exception"><paramref name="expr"/> is a valid expression without a range.</exception>
let internal rangeOrDefault expr =
    match expr.Expression, expr.Range with
    | _, Value range -> range
    | InvalidExpr, Null -> Range.Zero
    | _, Null -> failwith "valid expression without a range"

/// <summary>
/// Creates an <see cref="InferredExpressionInformation"/>.
/// </summary>
let private inferred isMutable quantumDep =
    InferredExpressionInformation.New(isMutable, quantumDep)

/// Creates a <see cref="TypedExpression"/> with empty type parameter resolutions.
let private exprWithoutTypeArgs range inferred (expr, resolvedType) =
    TypedExpression.New(expr, ImmutableDictionary.Empty, resolvedType, inferred, range)

/// <summary>
/// Returns a warning for short-circuiting of operation calls in <paramref name="expr"/>.
/// </summary>
let private verifyConditionalExecution (expr: TypedExpression) =
    let isOperationCall ex =
        match ex.Expression with
        | CallLikeExpression (callable, _) when not (TypedExpression.IsPartialApplication ex.Expression) ->
            match callable.ResolvedType.Resolution with
            | QsTypeKind.Operation _ -> true
            | _ -> false
        | _ -> false

    [
        if expr.Exists isOperationCall then
            QsCompilerDiagnostic.Warning(WarningCode.ConditionalEvaluationOfOperationCall, []) (rangeOrDefault expr)
    ]

/// <summary>
/// Verifies that <paramref name="resolvedType"/> is a user-defined type.
/// </summary>
/// <param name="processUdt">
/// Given a function to add diagnostics, and a user-defined type, extracts a resolved type from the user-defined type.
/// </param>
/// <param name="resolvedType">The resolved type to verify.</param>
/// <param name="range">The diagnostic range.</param>
/// <returns>The result of applying <paramref name="processUdt"/> to the UDT and the diagnostics.</returns>
let private verifyUdtWith processUdt (resolvedType: ResolvedType) range =
    match resolvedType.Resolution with
    | QsTypeKind.UserDefinedType udt ->
        let diagnostics = ResizeArray()
        let resultType = udt |> processUdt (fun error -> QsCompilerDiagnostic.Error error range |> diagnostics.Add)
        resultType, Seq.toList diagnostics
    | _ ->
        ResolvedType.New InvalidType,
        [
            QsCompilerDiagnostic.Error(ErrorCode.ExpectingUserDefinedType, [ showType resolvedType ]) range
        ]

/// <summary>
/// Verifies that <paramref name="lhs"/> and <paramref name="rhs"/> have type Bool.
/// </summary>
let private verifyAreBooleans (inference: InferenceContext) lhs rhs =
    inference.Unify(ResolvedType.New Bool, lhs.ResolvedType)
    @ inference.Unify(ResolvedType.New Bool, rhs.ResolvedType)

/// <summary>
/// Verifies that <paramref name="lhs"/> and <paramref name="rhs"/> have type Int.
/// </summary>
let private verifyAreIntegers (inference: InferenceContext) lhs rhs =
    inference.Unify(ResolvedType.New Int, lhs.ResolvedType)
    @ inference.Unify(ResolvedType.New Int, rhs.ResolvedType)

/// <summary>
/// Verifies that <paramref name="expr"/> has type Int or BigInt.
/// </summary>
/// <returns>The type of <paramref name="expr"/> and the diagnostics.</returns>
let private verifyIsIntegral (inference: InferenceContext) expr =
    expr.ResolvedType, inference.Constrain(expr.ResolvedType, Integral)

/// <summary>
/// Verifies that <paramref name="lhs"/> and <paramref name="rhs"/> have an intersecting integral type.
/// </summary>
/// <returns>The intersection type and the diagnostics.</returns>
let private verifyIntegralOp (inference: InferenceContext) range lhs rhs =
    let exType, intersectDiagnostics = inference.Intersect(lhs.ResolvedType, rhs.ResolvedType)
    let exType = exType |> ResolvedType.withAllRanges (TypeRange.inferred range)
    let constrainDiagnostics = inference.Constrain(exType, Integral)
    exType, intersectDiagnostics @ constrainDiagnostics

/// <summary>
/// Verifies that <paramref name="expr"/> has a numeric type.
/// </summary>
/// <returns>The type of <paramref name="expr"/> and the diagnostics.</returns>
let private verifySupportsArithmetic (inference: InferenceContext) expr =
    expr.ResolvedType, inference.Constrain(expr.ResolvedType, Numeric)

/// <summary>
/// Verifies that <paramref name="lhs"/> and <paramref name="rhs"/> have an intersecting numeric type.
/// </summary>
/// <returns>The intersection type and the diagnostics.</returns>
let private verifyArithmeticOp (inference: InferenceContext) range lhs rhs =
    let exType, intersectDiagnostics = inference.Intersect(lhs.ResolvedType, rhs.ResolvedType)
    let exType = exType |> ResolvedType.withAllRanges (TypeRange.inferred range)
    let constrainDiagnostics = inference.Constrain(exType, Numeric)
    exType, intersectDiagnostics @ constrainDiagnostics

/// <summary>
/// Verifies that <paramref name="expr"/> has an iterable type.
/// </summary>
/// <returns>The iterable item type and the diagnostics.</returns>
let internal verifyIsIterable (inference: InferenceContext) expr =
    let range = rangeOrDefault expr
    let item = inference.Fresh range
    item, inference.Constrain(expr.ResolvedType, Iterable item)

/// <summary>
/// Verifies that <paramref name="lhs"/> and <paramref name="rhs"/> have an intersecting semigroup type.
/// </summary>
/// <returns>The intersection type and the diagnostics.</returns>
let private verifySemigroup (inference: InferenceContext) range lhs rhs =
    let exType, intersectDiagnostics = inference.Intersect(lhs.ResolvedType, rhs.ResolvedType)
    let exType = exType |> ResolvedType.withAllRanges (TypeRange.inferred range)
    let constrainDiagnostics = inference.Constrain(exType, Semigroup)
    exType, intersectDiagnostics @ constrainDiagnostics

/// <summary>
/// Verifies that <paramref name="lhs"/> and <paramref name="rhs"/> have an intersecting equatable type.
/// </summary>
/// <returns>The intersection type and the diagnostics.</returns>
let private verifyEqualityComparison (inference: InferenceContext) range lhs rhs =
    let exType, intersectDiagnostics = inference.Intersect(lhs.ResolvedType, rhs.ResolvedType)
    let exType = exType |> ResolvedType.withAllRanges (TypeRange.inferred range)
    let constrainDiagnostics = inference.Constrain(exType, Equatable)
    intersectDiagnostics @ constrainDiagnostics

/// <summary>
/// Verifies that <paramref name="exprs"/> can form an array.
/// </summary>
/// <returns>The type of the array and the diagnostics.</returns>
let private verifyValueArray (inference: InferenceContext) range exprs =
    let types = exprs |> Seq.map (fun expr -> expr.ResolvedType)

    if Seq.isEmpty types then
        inference.Fresh range |> ArrayType |> ResolvedType.create (Inferred range), []
    else
        let diagnostics = ResizeArray()

        types
        |> Seq.reduce
            (fun left right ->
                let intersectionType, intersectionDiagnostics = inference.Intersect(left, right)
                intersectionDiagnostics |> List.iter diagnostics.Add
                intersectionType |> ResolvedType.withAllRanges right.Range)
        |> ResolvedType.withAllRanges (Inferred range)
        |> ArrayType
        |> ResolvedType.create (Inferred range),
        Seq.toList diagnostics

/// <summary>
/// Verifies that <paramref name="container"/> has a type that can be indexed by a value of type
/// <paramref name="indexType"/>.
/// </summary>
/// <returns>The item type and the diagnostics.</returns>
let private verifyIndexedItem (inference: InferenceContext) container indexType =
    let range = rangeOrDefault container
    let itemType = inference.Fresh range
    itemType, inference.Constrain(container.ResolvedType, Indexed(indexType, itemType))

/// <summary>
/// Verifies that <paramref name="expr"/> has an adjointable type.
/// </summary>
/// <returns>The type of <paramref name="expr"/> and the diagnostics.</returns>
let private verifyAdjointApplication (inference: InferenceContext) expr =
    expr.ResolvedType, inference.Constrain(expr.ResolvedType, Constraint.Adjointable)

/// <summary>
/// Verifies that <paramref name="expr"/> has a controllable type.
/// </summary>
/// <returns>The type of the controlled specialization of <paramref name="expr"/> and the diagnostics.</returns>
let private verifyControlledApplication (inference: InferenceContext) expr =
    let range = rangeOrDefault expr
    let controlled = inference.Fresh range
    controlled, inference.Constrain(expr.ResolvedType, Constraint.Controllable controlled)

// utils for verifying identifiers, call expressions, and resolving type parameters

/// <summary>
/// Verifies that <paramref name="symbol"/> and its associated <paramref name="typeArgs"/> form a valid identifier.
/// </summary>
/// <returns>The resolved identifier expression and the diagnostics.</returns>
let private verifyIdentifier (inference: InferenceContext) (symbols: SymbolTracker) symbol typeArgs =
    let diagnostics = ResizeArray()

    let resolvedTargs =
        typeArgs
        |> QsNullable<_>.Map
            (fun (args: ImmutableArray<QsType>) ->
                args
                |> Seq.map
                    (fun tArg ->
                        match tArg.Type with
                        | MissingType -> ResolvedType.New MissingType
                        | _ -> symbols.ResolveType diagnostics.Add tArg))
        |> QsNullable<_>.Map (fun args -> args.ToImmutableArray())

    let resId, typeParams = symbols.ResolveIdentifier diagnostics.Add symbol
    let identifier, info = Identifier(resId.VariableName, resolvedTargs), resId.InferredInformation

    // resolve type parameters (if any) with the given type arguments
    // Note: type parameterized objects are never mutable - remember they are not the same as an identifier containing a template...!
    let invalidWithoutTargs mut =
        (identifier, ResolvedType.New InvalidType)
        |> exprWithoutTypeArgs symbol.Range (inferred mut info.HasLocalQuantumDependency)

    match resId.VariableName, resolvedTargs with
    | InvalidIdentifier, Null -> invalidWithoutTargs true, Seq.toList diagnostics
    | InvalidIdentifier, Value _ -> invalidWithoutTargs false, Seq.toList diagnostics
    | LocalVariable _, Null -> (identifier, resId.Type) |> exprWithoutTypeArgs symbol.Range info, Seq.toList diagnostics
    | LocalVariable _, Value _ ->
        invalidWithoutTargs false,
        QsCompilerDiagnostic.Error(ErrorCode.IdentifierCannotHaveTypeArguments, []) symbol.RangeOrDefault
        :: Seq.toList diagnostics
    | GlobalCallable _, Value res when res.Length <> typeParams.Length ->
        invalidWithoutTargs false,
        QsCompilerDiagnostic.Error
            (ErrorCode.WrongNumberOfTypeArguments, [ string typeParams.Length ])
            symbol.RangeOrDefault
        :: Seq.toList diagnostics
    | GlobalCallable name, _ ->
        let typeParams =
            typeParams
            |> Seq.choose
                (function
                | ValidName param -> Some(name, param)
                | InvalidName -> None)

        let typeArgs = resolvedTargs |> QsNullable.defaultValue ImmutableArray.Empty

        let resolutions =
            typeParams
            |> Seq.mapi
                (fun i param ->
                    if i < typeArgs.Length && typeArgs.[i].Resolution <> MissingType then
                        KeyValuePair(param, typeArgs.[i])
                    else
                        KeyValuePair(param, inference.Fresh symbol.RangeOrDefault))
            |> ImmutableDictionary.CreateRange

        let identifier =
            if resolutions.IsEmpty then
                identifier
            else
                Identifier(GlobalCallable name, ImmutableArray.CreateRange resolutions.Values |> Value)

        let exInfo = InferredExpressionInformation.New(isMutable = false, quantumDep = info.HasLocalQuantumDependency)
        TypedExpression.New(identifier, resolutions, resId.Type, exInfo, symbol.Range), Seq.toList diagnostics

/// Verifies that an expression of the given rhsType, used within the given parent (i.e. specialization declaration),
/// can be used when an expression of expectedType is expected by callaing TypeMatchArgument.
/// Generates an error with the given error code mismatchErr for the given range if this is not the case.
/// Verifies that any internal type parameters are "matched" only with themselves (or with an invalid type),
/// and generates a ConstrainsTypeParameter error if this is not the case.
/// If the given rhsEx is Some value, verifies whether it contains an identifier referring to the parent
/// that is not part of a call-like expression but does not specify all needed type arguments.
/// Calls the given function addError on all generated errors.
/// IMPORTANT: ignores any external type parameter occuring in expectedType without raising an error!
let internal verifyAssignment (inference: InferenceContext) expectedType mismatchErr rhs =
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
                (InvalidExpr, ResolvedType.New InvalidType)
                |> exprWithoutTypeArgs Null (inferred false array.InferredInformation.HasLocalQuantumDependency)

            let validSlicing step =
                match array.ResolvedType.Resolution with
                | ArrayType _ ->
                    step |> Option.forall (fun expr -> Int = (inference.Resolve expr.ResolvedType).Resolution)
                | _ -> false

            let conditionalIntExpr (cond: TypedExpression) ifTrue ifFalse =
                let quantumDep =
                    [ cond; ifTrue; ifFalse ]
                    |> List.exists (fun ex -> ex.InferredInformation.HasLocalQuantumDependency)

                (CONDITIONAL(cond, ifTrue, ifFalse), ResolvedType.New Int)
                |> exprWithoutTypeArgs Null (inferred false quantumDep)

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

            (StringLiteral(literal, resInterpol), String |> ResolvedType.create (TypeRange.inferred this.Range))
            |> exprWithoutTypeArgs this.Range (inferred false localQdependency)

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
                (ValueTuple items, TupleType types |> ResolvedType.create (TypeRange.inferred this.Range))
                |> exprWithoutTypeArgs this.Range (inferred false localQdependency)

        /// Resolves and verifies the given array base type and the expression denoting the length of the array,
        /// and returns the corrsponding NewArray expression as typed expression
        let buildNewArray (bType, ex: QsExpression) =
            let ex = resolve ex
            inference.Unify(ResolvedType.New Int, ex.ResolvedType) |> List.iter diagnose

            let resolvedBase = symbols.ResolveType diagnose bType
            let arrType = resolvedBase |> ArrayType |> ResolvedType.create (TypeRange.inferred this.Range)
            let quantumDep = ex.InferredInformation.HasLocalQuantumDependency
            (NewArray(resolvedBase, ex), arrType) |> exprWithoutTypeArgs this.Range (inferred false quantumDep)

        /// Resolves and verifies all given items of a value array literal, and returns the corresponding ValueArray as typed expression.
        let buildValueArray values =
            let values = values |> Seq.map resolve |> ImmutableArray.CreateRange
            let resolvedType = values |> verifyValueArray inference this.RangeOrDefault |> takeDiagnostics
            let localQdependency = values |> Seq.exists (fun item -> item.InferredInformation.HasLocalQuantumDependency)

            (ValueArray values, resolvedType)
            |> exprWithoutTypeArgs this.Range (inferred false localQdependency)

        /// Resolves and verifies the sized array constructor expression and returns it as a typed expression.
        let buildSizedArray value (size: QsExpression) =
            let value = resolve value
            let arrayType = ArrayType value.ResolvedType |> ResolvedType.create (TypeRange.inferred this.Range)
            let size = resolve size
            inference.Unify(ResolvedType.New Int, size.ResolvedType) |> List.iter diagnose

            let quantumDependency =
                value.InferredInformation.HasLocalQuantumDependency
                || size.InferredInformation.HasLocalQuantumDependency

            (SizedArray(value, size), arrayType)
            |> exprWithoutTypeArgs this.Range (inferred false quantumDependency)

        /// Resolves and verifies the given array expression and index expression of an array item access expression,
        /// and returns the corresponding ArrayItem expression as typed expression.
        let buildArrayItem (array, index: QsExpression) =
            let array = resolve array

            match resolveSlicing array index with
            | None ->
                { array with
                    ResolvedType = verifyIndexedItem inference array (ResolvedType.New Range) |> takeDiagnostics
                }
            | Some index ->
                let resolvedType = verifyIndexedItem inference array index.ResolvedType |> takeDiagnostics

                let localQdependency =
                    array.InferredInformation.HasLocalQuantumDependency
                    || index.InferredInformation.HasLocalQuantumDependency

                (ArrayItem(array, index), resolvedType)
                |> exprWithoutTypeArgs this.Range (inferred false localQdependency)

        /// Given a symbol used to represent an item name in an item access or update expression,
        /// returns the an identifier that can be used to represent the corresponding item name.
        /// Adds an error if the given symbol is not either invalid or an unqualified symbol.
        let buildItemName (sym: QsSymbol) =
            match sym.Symbol with
            | InvalidSymbol -> InvalidIdentifier
            | Symbol name -> LocalVariable name
            | _ ->
                QsCompilerDiagnostic.Error(ErrorCode.ExpectingItemName, []) sym.RangeOrDefault |> diagnose
                InvalidIdentifier

        /// Resolves and verifies the given expression and item name of a named item access expression,
        /// and returns the corresponding NamedItem expression as typed expression.
        let buildNamedItem (ex, acc: QsSymbol) =
            let ex = resolve ex
            let itemName = acc |> buildItemName
            let udtType = inference.Resolve ex.ResolvedType
            let exType = verifyUdtWith (symbols.GetItemType itemName) udtType (rangeOrDefault ex) |> takeDiagnostics
            let localQdependency = ex.InferredInformation.HasLocalQuantumDependency

            (NamedItem(ex, itemName), exType)
            |> exprWithoutTypeArgs this.Range (inferred false localQdependency)

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

                (CopyAndUpdate(lhs, accEx, rhs), lhs.ResolvedType)
                |> exprWithoutTypeArgs this.Range (inferred false localQdependency)

            match (lhs.ResolvedType.Resolution, accEx.Expression) with
            | UserDefinedType _, Identifier (sym, Null) ->
                let itemName = buildItemName sym

                let itemType =
                    verifyUdtWith (symbols.GetItemType itemName) lhs.ResolvedType (rangeOrDefault lhs)
                    |> takeDiagnostics

                verifyAssignment inference itemType ErrorCode.TypeMismatchInCopyAndUpdateExpr rhs
                |> List.iter diagnose

                let resAccEx =
                    (Identifier(itemName, Null), itemType)
                    |> exprWithoutTypeArgs sym.Range (inferred false lhs.InferredInformation.HasLocalQuantumDependency)

                resAccEx |> resolvedCopyAndUpdateExpr
            | _ -> // by default, assume that the update expression is supposed to be for an array
                match resolveSlicing lhs accEx with
                | None -> // indicates a trivial slicing of the form "..." resulting in a complete replacement
                    let expectedRhs = verifyIndexedItem inference lhs (ResolvedType.New Range) |> takeDiagnostics

                    verifyAssignment inference expectedRhs ErrorCode.TypeMismatchInCopyAndUpdateExpr rhs
                    |> List.iter diagnose

                    { rhs with ResolvedType = expectedRhs }
                | Some resAccEx -> // indicates either a index or index range to update
                    let expectedRhs = verifyIndexedItem inference lhs resAccEx.ResolvedType |> takeDiagnostics

                    verifyAssignment inference expectedRhs ErrorCode.TypeMismatchInCopyAndUpdateExpr rhs
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
                    verifyAreIntegers inference start step |> List.iter diagnose

                    let localQdependency =
                        start.InferredInformation.HasLocalQuantumDependency
                        || step.InferredInformation.HasLocalQuantumDependency

                    (RangeLiteral(start, step), Range |> ResolvedType.create (TypeRange.inferred this.Range))
                    |> exprWithoutTypeArgs this.Range (inferred false localQdependency)
                | _ ->
                    resolve lhs
                    |> (fun resStart ->
                        inference.Unify(ResolvedType.New Int, resStart.ResolvedType) |> List.iter diagnose
                        resStart)

            let localQdependency =
                lhs.InferredInformation.HasLocalQuantumDependency
                || rhs.InferredInformation.HasLocalQuantumDependency

            (RangeLiteral(lhs, rhs), Range |> ResolvedType.create (TypeRange.inferred this.Range))
            |> exprWithoutTypeArgs this.Range (inferred false localQdependency)

        /// Resolves and verifies the given expression with the given verification function,
        /// and returns the corresponding expression built with buildExprKind as typed expression.
        let verifyAndBuildWith buildExprKind verify (ex: QsExpression) =
            let ex = resolve ex
            let exType = verify ex |> takeDiagnostics

            (buildExprKind ex, exType)
            |> exprWithoutTypeArgs this.Range (inferred false ex.InferredInformation.HasLocalQuantumDependency)

        /// Resolves and verifies the given left hand side and right hand side of an arithmetic operator,
        /// and returns the corresponding expression built with buildExprKind as typed expression.
        let buildArithmeticOp buildExprKind (lhs, rhs) =
            let lhs = resolve lhs
            let rhs = resolve rhs
            let resolvedType = verifyArithmeticOp inference this.Range lhs rhs |> takeDiagnostics

            let localQdependency =
                lhs.InferredInformation.HasLocalQuantumDependency
                || rhs.InferredInformation.HasLocalQuantumDependency

            (buildExprKind (lhs, rhs), resolvedType)
            |> exprWithoutTypeArgs this.Range (inferred false localQdependency)

        /// Resolves and verifies the given left hand side and right hand side of an addition operator,
        /// and returns the corresponding ADD expression as typed expression.
        /// Note: ADD is used for both arithmetic expressions as well as concatenation expressions.
        /// If the resolved type of the given lhs supports concatenation, then the verification is done for a concatenation expression,
        /// and otherwise it is done for an arithmetic expression.
        let buildAddition (lhs, rhs) =
            let lhs = resolve lhs
            let rhs = resolve rhs
            let resolvedType = verifySemigroup inference this.Range lhs rhs |> takeDiagnostics

            let localQdependency =
                lhs.InferredInformation.HasLocalQuantumDependency
                || rhs.InferredInformation.HasLocalQuantumDependency

            (ADD(lhs, rhs), resolvedType) |> exprWithoutTypeArgs this.Range (inferred false localQdependency)

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
                    verifyArithmeticOp inference this.Range lhs rhs |> takeDiagnostics

            let localQdependency =
                lhs.InferredInformation.HasLocalQuantumDependency
                || rhs.InferredInformation.HasLocalQuantumDependency

            (POW(lhs, rhs), resolvedType) |> exprWithoutTypeArgs this.Range (inferred false localQdependency)

        /// Resolves and verifies the given left hand side and right hand side of a binary integral operator,
        /// and returns the corresponding expression built with buildExprKind as typed expression of type Int or BigInt, as appropriate.
        let buildIntegralOp buildExprKind (lhs, rhs) =
            let lhs = resolve lhs
            let rhs = resolve rhs
            let resolvedType = verifyIntegralOp inference this.Range lhs rhs |> takeDiagnostics

            let localQdependency =
                lhs.InferredInformation.HasLocalQuantumDependency
                || rhs.InferredInformation.HasLocalQuantumDependency

            (buildExprKind (lhs, rhs), resolvedType)
            |> exprWithoutTypeArgs this.Range (inferred false localQdependency)

        /// Resolves and verifies the given left hand side and right hand side of a shift operator,
        /// and returns the corresponding expression built with buildExprKind as typed expression of type Int or BigInt, as appropriate.
        let buildShiftOp buildExprKind (lhs, rhs) =
            let lhs = resolve lhs
            let rhs = resolve rhs
            let resolvedType = verifyIsIntegral inference lhs |> takeDiagnostics
            inference.Unify(ResolvedType.New Int, rhs.ResolvedType) |> List.iter diagnose

            let localQdependency =
                lhs.InferredInformation.HasLocalQuantumDependency
                || rhs.InferredInformation.HasLocalQuantumDependency

            (buildExprKind (lhs, rhs), resolvedType)
            |> exprWithoutTypeArgs this.Range (inferred false localQdependency)

        /// Resolves and verifies the given left hand side and right hand side of a binary boolean operator,
        /// and returns the corresponding expression built with buildExprKind as typed expression of type Bool.
        let buildBooleanOpWith verify shortCircuits buildExprKind (lhs, rhs) =
            let lhs = resolve lhs
            let rhs = resolve rhs
            verify lhs rhs |> List.iter diagnose

            if shortCircuits then verifyConditionalExecution rhs |> List.iter diagnose

            let localQdependency =
                lhs.InferredInformation.HasLocalQuantumDependency
                || rhs.InferredInformation.HasLocalQuantumDependency

            (buildExprKind (lhs, rhs), ResolvedType.New Bool)
            |> exprWithoutTypeArgs this.Range (inferred false localQdependency)

        /// Resolves and verifies the given condition, left hand side, and right hand side of a conditional expression (if-else-shorthand),
        /// and returns the corresponding conditional expression as typed expression.
        let buildConditional (cond, ifTrue, ifFalse) =
            let cond = resolve cond
            let ifTrue = resolve ifTrue
            let ifFalse = resolve ifFalse
            inference.Unify(ResolvedType.New Bool, cond.ResolvedType) |> List.iter diagnose
            verifyConditionalExecution ifTrue |> List.iter diagnose
            verifyConditionalExecution ifFalse |> List.iter diagnose

            let exType =
                inference.Intersect(ifTrue.ResolvedType, ifFalse.ResolvedType)
                |> takeDiagnostics
                |> ResolvedType.withAllRanges (TypeRange.inferred this.Range)

            let localQdependency =
                [ cond; ifTrue; ifFalse ] |> Seq.exists (fun ex -> ex.InferredInformation.HasLocalQuantumDependency)

            (CONDITIONAL(cond, ifTrue, ifFalse), exType)
            |> exprWithoutTypeArgs this.Range (inferred false localQdependency)

        /// Resolves the given expression and verifies that its type is indeed a user defined type.
        /// Determines the underlying type of the user defined type and returns the corresponding UNWRAP expression as typed expression of that type.
        let buildUnwrap ex =
            let ex = resolve ex
            let exType = inference.Fresh this.RangeOrDefault
            inference.Constrain(ex.ResolvedType, Wrapped exType) |> List.iter diagnose

            (UnwrapApplication ex, exType)
            |> exprWithoutTypeArgs this.Range (inferred false ex.InferredInformation.HasLocalQuantumDependency)

        /// Resolves and verifies the given left hand side and right hand side of a call expression,
        /// and returns the corresponding expression as typed expression.
        let buildCall callable arg =
            let callable = resolve callable
            let arg = resolve arg
            let callExpression = CallLikeExpression(callable, arg)
            let argType, partialType = partialArgType inference arg.ResolvedType

            if Option.isNone partialType then
                inference.Constrain(
                    callable.ResolvedType,
                    Set.ofSeq symbols.RequiredFunctorSupport |> CanGenerateFunctors
                )
                |> List.iter diagnose

            let output = inference.Fresh this.RangeOrDefault

            if Option.isSome partialType || context.IsInOperation then
                inference.Constrain(callable.ResolvedType, Callable(argType, output)) |> List.iter diagnose
            else
                let diagnostics =
                    inference.Unify(QsTypeKind.Function(argType, output) |> ResolvedType.New, callable.ResolvedType)

                if inference.Resolve callable.ResolvedType |> isOperation then
                    QsCompilerDiagnostic.Error(ErrorCode.OperationCallOutsideOfOperation, []) this.RangeOrDefault
                    |> diagnose
                else
                    List.iter diagnose diagnostics

            let resultType =
                match partialType with
                | Some missing ->
                    let result = inference.Fresh this.RangeOrDefault

                    inference.Constrain(callable.ResolvedType, HasPartialApplication(missing, result))
                    |> List.iter diagnose

                    result
                | None -> output

            let hasQuantumDependency =
                if Option.isSome partialType || inference.Resolve callable.ResolvedType |> isFunction then
                    callable.InferredInformation.HasLocalQuantumDependency
                    || arg.InferredInformation.HasLocalQuantumDependency
                else
                    true

            let info = InferredExpressionInformation.New(isMutable = false, quantumDep = hasQuantumDependency)
            TypedExpression.New(callExpression, callable.TypeParameterResolutions, resultType, info, this.Range)

        match this.Expression with
        | InvalidExpr ->
            (InvalidExpr, InvalidType |> ResolvedType.create (TypeRange.inferred this.Range))
            |> exprWithoutTypeArgs this.Range (inferred true false) // choosing the more permissive option here
        | MissingExpr ->
            (MissingExpr, MissingType |> ResolvedType.create (TypeRange.inferred this.Range))
            |> exprWithoutTypeArgs this.Range (inferred false false)
        | UnitValue ->
            (UnitValue, UnitType |> ResolvedType.create (TypeRange.inferred this.Range))
            |> exprWithoutTypeArgs this.Range (inferred false false)
        | Identifier (sym, tArgs) -> verifyIdentifier inference symbols sym tArgs |> takeDiagnostics
        | CallLikeExpression (callable, arg) -> buildCall callable arg
        | AdjointApplication ex -> verifyAndBuildWith AdjointApplication (verifyAdjointApplication inference) ex
        | ControlledApplication ex ->
            verifyAndBuildWith ControlledApplication (verifyControlledApplication inference) ex
        | UnwrapApplication ex -> buildUnwrap ex
        | ValueTuple items -> buildTuple items
        | ArrayItem (arr, idx) -> buildArrayItem (arr, idx)
        | NamedItem (ex, acc) -> buildNamedItem (ex, acc)
        | ValueArray values -> buildValueArray values
        | NewArray (baseType, ex) -> buildNewArray (baseType, ex)
        | SizedArray (value, size) -> buildSizedArray value size
        | IntLiteral i ->
            (IntLiteral i, Int |> ResolvedType.create (TypeRange.inferred this.Range))
            |> exprWithoutTypeArgs this.Range (inferred false false)
        | BigIntLiteral b ->
            (BigIntLiteral b, BigInt |> ResolvedType.create (TypeRange.inferred this.Range))
            |> exprWithoutTypeArgs this.Range (inferred false false)
        | DoubleLiteral d ->
            (DoubleLiteral d, Double |> ResolvedType.create (TypeRange.inferred this.Range))
            |> exprWithoutTypeArgs this.Range (inferred false false)
        | BoolLiteral b ->
            (BoolLiteral b, Bool |> ResolvedType.create (TypeRange.inferred this.Range))
            |> exprWithoutTypeArgs this.Range (inferred false false)
        | ResultLiteral r ->
            (ResultLiteral r, Result |> ResolvedType.create (TypeRange.inferred this.Range))
            |> exprWithoutTypeArgs this.Range (inferred false false)
        | PauliLiteral p ->
            (PauliLiteral p, Pauli |> ResolvedType.create (TypeRange.inferred this.Range))
            |> exprWithoutTypeArgs this.Range (inferred false false)
        | StringLiteral (s, exs) -> buildStringLiteral (s, exs)
        | RangeLiteral (lhs, rEnd) -> buildRange (lhs, rEnd)
        | CopyAndUpdate (lhs, accEx, rhs) -> buildCopyAndUpdate (lhs, accEx, rhs)
        | CONDITIONAL (cond, ifTrue, ifFalse) -> buildConditional (cond, ifTrue, ifFalse)
        | ADD (lhs, rhs) -> buildAddition (lhs, rhs) // addition takes a special role since it is used for both arithmetic and concatenation expressions
        | SUB (lhs, rhs) -> buildArithmeticOp SUB (lhs, rhs)
        | MUL (lhs, rhs) -> buildArithmeticOp MUL (lhs, rhs)
        | DIV (lhs, rhs) -> buildArithmeticOp DIV (lhs, rhs)
        | LT (lhs, rhs) ->
            buildBooleanOpWith
                (fun lhs rhs -> verifyArithmeticOp inference this.Range lhs rhs |> snd)
                false
                LT
                (lhs, rhs)
        | LTE (lhs, rhs) ->
            buildBooleanOpWith
                (fun lhs rhs -> verifyArithmeticOp inference this.Range lhs rhs |> snd)
                false
                LTE
                (lhs, rhs)
        | GT (lhs, rhs) ->
            buildBooleanOpWith
                (fun lhs rhs -> verifyArithmeticOp inference this.Range lhs rhs |> snd)
                false
                GT
                (lhs, rhs)
        | GTE (lhs, rhs) ->
            buildBooleanOpWith
                (fun lhs rhs -> verifyArithmeticOp inference this.Range lhs rhs |> snd)
                false
                GTE
                (lhs, rhs)
        | POW (lhs, rhs) -> buildPower (lhs, rhs) // power takes a special role because you can raise integers and doubles to integer and double powers, but bigint only to integer powers
        | MOD (lhs, rhs) -> buildIntegralOp MOD (lhs, rhs)
        | LSHIFT (lhs, rhs) -> buildShiftOp LSHIFT (lhs, rhs)
        | RSHIFT (lhs, rhs) -> buildShiftOp RSHIFT (lhs, rhs)
        | BOR (lhs, rhs) -> buildIntegralOp BOR (lhs, rhs)
        | BAND (lhs, rhs) -> buildIntegralOp BAND (lhs, rhs)
        | BXOR (lhs, rhs) -> buildIntegralOp BXOR (lhs, rhs)
        | AND (lhs, rhs) -> buildBooleanOpWith (verifyAreBooleans inference) true AND (lhs, rhs)
        | OR (lhs, rhs) -> buildBooleanOpWith (verifyAreBooleans inference) true OR (lhs, rhs)
        | EQ (lhs, rhs) -> buildBooleanOpWith (verifyEqualityComparison inference this.Range) false EQ (lhs, rhs)
        | NEQ (lhs, rhs) -> buildBooleanOpWith (verifyEqualityComparison inference this.Range) false NEQ (lhs, rhs)
        | NEG ex -> verifyAndBuildWith NEG (verifySupportsArithmetic inference) ex
        | BNOT ex -> verifyAndBuildWith BNOT (verifyIsIntegral inference) ex
        | NOT ex ->
            ex
            |> verifyAndBuildWith
                NOT
                (fun expr ->
                    Bool |> ResolvedType.create (TypeRange.inferred this.Range),
                    inference.Unify(ResolvedType.New Bool, expr.ResolvedType))
