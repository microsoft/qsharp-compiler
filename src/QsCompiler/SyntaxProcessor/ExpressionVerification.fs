﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.Expressions

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxGenerator
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference.RelationOps
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.VerificationTools
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open System.Collections.Generic
open System.Collections.Immutable

// utils for verifying types in expressions

/// Returns the string representation of a type.
let private showType: ResolvedType -> _ = SyntaxTreeToQsharp.Default.ToCode

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

let private anyQuantumDep: TypedExpression seq -> _ =
    Seq.exists (fun e -> e.InferredInformation.HasLocalQuantumDependency)

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
            QsCompilerDiagnostic.Warning (WarningCode.ConditionalEvaluationOfOperationCall, []) (rangeOrDefault expr)
    ]

/// <summary>
/// Verifies that <paramref name="lhs"/> and <paramref name="rhs"/> have type Bool.
/// </summary>
let private verifyAreBooleans (inference: InferenceContext) lhs rhs =
    inference.Constrain(ResolvedType.New Bool .> lhs.ResolvedType)
    @ inference.Constrain(ResolvedType.New Bool .> rhs.ResolvedType)

/// <summary>
/// Verifies that <paramref name="lhs"/> and <paramref name="rhs"/> have type Int.
/// </summary>
let private verifyAreIntegers (inference: InferenceContext) lhs rhs =
    inference.Constrain(ResolvedType.New Int .> lhs.ResolvedType)
    @ inference.Constrain(ResolvedType.New Int .> rhs.ResolvedType)

/// <summary>
/// Verifies that <paramref name="expr"/> has type Int or BigInt.
/// </summary>
/// <returns>The type of <paramref name="expr"/> and the diagnostics.</returns>
let private verifyIsIntegral (inference: InferenceContext) expr =
    expr.ResolvedType, Integral expr.ResolvedType |> Class |> inference.Constrain

/// <summary>
/// Verifies that <paramref name="lhs"/> and <paramref name="rhs"/> have an intersecting integral type.
/// </summary>
/// <returns>The intersection type and the diagnostics.</returns>
let private verifyIntegralOp (inference: InferenceContext) range lhs rhs =
    let exType, intersectDiagnostics = inference.Intersect(lhs.ResolvedType, rhs.ResolvedType)
    let exType = exType |> ResolvedType.withAllRanges (TypeRange.inferred range)
    let constrainDiagnostics = Integral exType |> Class |> inference.Constrain
    exType, intersectDiagnostics @ constrainDiagnostics

/// <summary>
/// Verifies that <paramref name="expr"/> has a numeric type.
/// </summary>
/// <returns>The type of <paramref name="expr"/> and the diagnostics.</returns>
let private verifySupportsArithmetic (inference: InferenceContext) expr =
    expr.ResolvedType, Num expr.ResolvedType |> Class |> inference.Constrain

/// <summary>
/// Verifies that <paramref name="lhs"/> and <paramref name="rhs"/> have an intersecting numeric type.
/// </summary>
/// <returns>The intersection type and the diagnostics.</returns>
let private verifyArithmeticOp (inference: InferenceContext) range lhs rhs =
    let exType, intersectDiagnostics = inference.Intersect(lhs.ResolvedType, rhs.ResolvedType)
    let exType = exType |> ResolvedType.withAllRanges (TypeRange.inferred range)
    let constrainDiagnostics = Num exType |> Class |> inference.Constrain
    exType, intersectDiagnostics @ constrainDiagnostics

/// <summary>
/// Verifies that <paramref name="expr"/> has an iterable type.
/// </summary>
/// <returns>The iterable item type and the diagnostics.</returns>
let internal verifyIsIterable (inference: InferenceContext) expr =
    let range = rangeOrDefault expr
    let item = inference.Fresh range
    item, Iterable(expr.ResolvedType, item) |> Class |> inference.Constrain

/// <summary>
/// Verifies that <paramref name="lhs"/> and <paramref name="rhs"/> have an intersecting semigroup type.
/// </summary>
/// <returns>The intersection type and the diagnostics.</returns>
let private verifySemigroup (inference: InferenceContext) range lhs rhs =
    let exType, intersectDiagnostics = inference.Intersect(lhs.ResolvedType, rhs.ResolvedType)
    let exType = exType |> ResolvedType.withAllRanges (TypeRange.inferred range)
    let constrainDiagnostics = Semigroup exType |> Class |> inference.Constrain
    exType, intersectDiagnostics @ constrainDiagnostics

/// <summary>
/// Verifies that <paramref name="lhs"/> and <paramref name="rhs"/> have an intersecting equatable type.
/// </summary>
/// <returns>The intersection type and the diagnostics.</returns>
let private verifyEqualityComparison (inference: InferenceContext) range lhs rhs =
    let exType, intersectDiagnostics = inference.Intersect(lhs.ResolvedType, rhs.ResolvedType)
    let exType = exType |> ResolvedType.withAllRanges (TypeRange.inferred range)
    let constrainDiagnostics = Eq exType |> Class |> inference.Constrain
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
        |> Seq.reduce (fun left right ->
            let intersectionType, intersectionDiagnostics = inference.Intersect(left, right)
            intersectionDiagnostics |> List.iter diagnostics.Add
            intersectionType |> ResolvedType.withAllRanges right.Range)
        |> ResolvedType.withAllRanges (Inferred range)
        |> ArrayType
        |> ResolvedType.create (Inferred range),
        Seq.toList diagnostics

/// <summary>
/// Verifies that <paramref name="expr"/> has an adjointable type.
/// </summary>
/// <returns>The type of <paramref name="expr"/> and the diagnostics.</returns>
let private verifyAdjointApplication (inference: InferenceContext) expr =
    expr.ResolvedType, ClassConstraint.Adjointable expr.ResolvedType |> Class |> inference.Constrain

/// <summary>
/// Verifies that <paramref name="expr"/> has a controllable type.
/// </summary>
/// <returns>The type of the controlled specialization of <paramref name="expr"/> and the diagnostics.</returns>
let private verifyControlledApplication (inference: InferenceContext) expr =
    let range = rangeOrDefault expr
    let controlled = inference.Fresh range
    controlled, ClassConstraint.Controllable(expr.ResolvedType, controlled) |> Class |> inference.Constrain

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
                |> Seq.map (fun tArg ->
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
        QsCompilerDiagnostic.Error (ErrorCode.IdentifierCannotHaveTypeArguments, []) symbol.RangeOrDefault
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
            |> Seq.choose (function
                | ValidName param -> Some(name, param)
                | InvalidName -> None)

        let typeArgs = resolvedTargs |> QsNullable.defaultValue ImmutableArray.Empty

        let resolutions =
            typeParams
            |> Seq.mapi (fun i param ->
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

        let exInfo =
            InferredExpressionInformation.New(isMutable = false, quantumDep = info.HasLocalQuantumDependency)

        TypedExpression.New(identifier, resolutions, resId.Type, exInfo, symbol.Range), Seq.toList diagnostics

/// Given a Q# symbol, as well as the resolved type of the right hand side that is assigned to it,
/// shape matches the symbol tuple with the type to determine whether the assignment is valid, and
/// calls the given function tryBuildDeclaration on each symbol item and its matched type.
/// The passed function tryBuildDeclaration is expected to take a symbol name and type as well as their respective ranges as argument,
/// and return either the built declaration as Some - if it was built successfully - or None, as well as an array with diagnostics.
/// Generates an ExpectingUnqualifiedSymbol error if the given symbol contains qualified symbol items.
/// Generates a SymbolTupleShapeMismatch error for the corresponding range if the shape matching fails.
/// Generates an ExpressionOfUnknownType error if the given type of the right hand side contains a missing type.
/// If warnOnDiscard is set to true, generates a DiscardingItemInAssignment warning if a symbol on the left hand side is missing.
/// Returns the resolved SymbolTuple, as well as an array with all local variable declarations returned by tryBuildDeclaration,
/// along with an array containing all generated diagnostics.
let rec internal verifyBinding (inference: InferenceContext) tryBuildDeclaration (symbol, rhsType) warnOnDiscard =
    match symbol.Symbol with
    | InvalidSymbol -> InvalidItem, [||], [||]
    | MissingSymbol when warnOnDiscard ->
        let warning =
            QsCompilerDiagnostic.Warning (WarningCode.DiscardingItemInAssignment, []) symbol.RangeOrDefault

        DiscardedItem, [||], [| warning |]
    | MissingSymbol -> DiscardedItem, [||], [||]
    | OmittedSymbols
    | QualifiedSymbol _ ->
        let error = QsCompilerDiagnostic.Error (ErrorCode.ExpectingUnqualifiedSymbol, []) symbol.RangeOrDefault
        InvalidItem, [||], [| error |]
    | Symbol name ->
        match tryBuildDeclaration (name, symbol.RangeOrDefault) rhsType with
        | Some declaration, diagnostics -> VariableName name, [| declaration |], diagnostics
        | None, diagnostics -> InvalidItem, [||], diagnostics
    | SymbolTuple symbols ->
        let types = symbols |> Seq.map (fun symbol -> inference.Fresh symbol.RangeOrDefault) |> Seq.toList

        let tupleType =
            if List.isEmpty types then UnitType else ImmutableArray.CreateRange types |> TupleType
            |> ResolvedType.create (TypeRange.inferred symbol.Range)

        let unifyDiagnostics = inference.Constrain(tupleType .> rhsType)

        let verify symbol symbolType =
            verifyBinding inference tryBuildDeclaration (symbol, symbolType) warnOnDiscard

        let combine (item, declarations1, diagnostics1) (items, declarations2, diagnostics2) =
            item :: items, Array.append declarations1 declarations2, Array.append diagnostics1 diagnostics2

        let items, declarations, diagnostics =
            Seq.foldBack combine (Seq.map2 verify symbols types) ([], [||], [||])

        let symbolTuple =
            match items with
            | [ item ] -> item
            | _ -> ImmutableArray.CreateRange items |> VariableNameTuple

        symbolTuple, declarations, List.toArray unifyDiagnostics |> Array.append diagnostics

let private characteristicsSet info =
    info.Characteristics.SupportedFunctors
    |> QsNullable.defaultValue ImmutableHashSet.Empty
    |> Seq.map (function
        | Adjoint -> Adjointable
        | Controlled -> Controllable)
    |> Set.ofSeq

let private lambdaCharacteristics (inference: InferenceContext) (body: TypedExpression) =
    // Start with the universe of characteristics if the operation returns unit, or the empty set otherwise.
    let mutable characteristics =
        if inference.Resolve(body.ResolvedType).Resolution = UnitType then
            Set.ofList [ Adjointable; Controllable ]
        else
            Set.empty

    // The lambda's characteristics are the intersection of the characteristics of every operation called by the lambda.
    let onCall callableType =
        match inference.Resolve(callableType).Resolution with
        | QsTypeKind.Operation (_, info) -> characteristics <- characteristicsSet info |> Set.intersect characteristics
        | TypeParameter _ ->
            // When a callable type can't be resolved based on the current knowledge of the inference context,
            // pessimistically assume that it is an operation that supports no characteristics. This limitation exists
            // by design to make characteristics inference easier.
            characteristics <- Set.empty
        | _ -> ()

    let transformation =
        { new ExpressionKindTransformation() with
            override _.OnCallLikeExpression(callable, arg) =
                onCall callable.ResolvedType
                ``base``.OnCallLikeExpression(callable, arg)

            // Call expressions in nested lambdas don't affect our characteristics, so don't visit nested lambda bodies.
            override _.OnLambda lambda = Lambda lambda
        }

    transformation.OnExpressionKind body.Expression |> ignore
    characteristics

let private inferLambda inference range kind inputType body =
    let inOutTypes = inputType, body.ResolvedType

    let typeKind =
        match kind with
        | LambdaKind.Function -> QsTypeKind.Function inOutTypes
        | LambdaKind.Operation ->
            let characteristics = lambdaCharacteristics inference body |> ResolvedCharacteristics.FromProperties
            let info = CallableInformation.New(characteristics, InferredCallableInformation.NoInformation)
            QsTypeKind.Operation(inOutTypes, info)

    ResolvedType.create (TypeRange.inferred range) typeKind

// utils for building TypedExpressions from QsExpressions

type QsExpression with
    /// Given a SymbolTracker containing all the symbols which are currently defined,
    /// recursively computes the corresponding typed expression for a Q# expression.
    /// Calls addDiagnostic on each diagnostic generated during the resolution.
    /// Returns the computed typed expression.
    member this.Resolve ({ Symbols = symbols; Inference = inference } as context) diagnose =
        let resolve context' (item: QsExpression) = item.Resolve context' diagnose

        let takeDiagnostics (value, diagnostics) =
            List.iter diagnose diagnostics
            value

        /// Given and expression used for array slicing, as well as the type of the sliced expression,
        /// generates suitable boundaries for open ended ranges and returns the resolved slicing expression.
        /// NOTE: Does *not* generated any diagnostics related to the given type for the array to slice.
        let resolveSlicing array (index: QsExpression) =
            let array = { array with ResolvedType = inference.Resolve array.ResolvedType }

            let invalidRangeDelimiter =
                (InvalidExpr, ResolvedType.New InvalidType)
                |> exprWithoutTypeArgs Null (inferred false array.InferredInformation.HasLocalQuantumDependency)

            let validSlicing step =
                match array.ResolvedType.Resolution with
                | ArrayType _ -> step |> Option.forall (fun expr -> Int = expr.ResolvedType.Resolution)
                | _ -> false

            let conditionalIntExpr (cond: TypedExpression) ifTrue ifFalse =
                (CONDITIONAL(cond, ifTrue, ifFalse), ResolvedType.New Int)
                |> exprWithoutTypeArgs Null (inferred false (anyQuantumDep [ cond; ifTrue; ifFalse ]))

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

            let resolveSlicingRange start step finish =
                let toResolvedExpr ex =
                    let ex = resolve context ex
                    { ex with ResolvedType = inference.Resolve ex.ResolvedType }

                let resolvedStep = step |> Option.map toResolvedExpr

                let resolveWith build (ex: QsExpression) =
                    if ex.IsMissing then build resolvedStep else toResolvedExpr ex

                let resolvedStart, resolvedEnd =
                    start |> resolveWith openStartInSlicing, finish |> resolveWith openEndInSlicing

                match resolvedStep with
                | Some resolvedStep ->
                    SyntaxGenerator.RangeLiteral(SyntaxGenerator.RangeLiteral(resolvedStart, resolvedStep), resolvedEnd)
                | None -> SyntaxGenerator.RangeLiteral(resolvedStart, resolvedEnd)

            match index.Expression with
            | RangeLiteral (lhs, finish) ->
                match lhs.Expression with
                | RangeLiteral (start, step) ->
                    // Cases: xs[...step..finish], xs[start..step...], xs[start..step..finish], xs[...step...].
                    resolveSlicingRange start (Some step) finish
                | _ ->
                    // Cases: xs[...finish], xs[start...], xs[start..finish], xs[...].
                    resolveSlicingRange lhs None finish
            | _ ->
                // Case: xs[i].
                resolve context index

        /// Resolves and verifies the interpolated expressions, and returns the StringLiteral as typed expression.
        let buildStringLiteral (literal, interpolated) =
            let resInterpol = interpolated |> Seq.map (resolve context) |> ImmutableArray.CreateRange

            (StringLiteral(literal, resInterpol), String |> ResolvedType.create (TypeRange.inferred this.Range))
            |> exprWithoutTypeArgs this.Range (inferred false (anyQuantumDep resInterpol))

        /// <summary>
        /// Resolves and verifies all given items, and returns the corresponding ValueTuple as typed expression.
        /// If the ValueTuple contains only one item, the item is returned instead (i.e. arity-1 tuple expressions are stripped).
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="items"/> is empty.</exception>
        let buildTuple items =
            let items = items |> Seq.map (resolve context) |> ImmutableArray.CreateRange
            let types = items |> Seq.map (fun x -> x.ResolvedType) |> ImmutableArray.CreateRange

            if items.IsEmpty then
                failwith "tuple expression requires at least one tuple item"
            elif items.Length = 1 then
                items.[0]
            else
                (ValueTuple items, TupleType types |> ResolvedType.create (TypeRange.inferred this.Range))
                |> exprWithoutTypeArgs this.Range (inferred false (anyQuantumDep items))

        /// Resolves and verifies the given array base type and the expression denoting the length of the array,
        /// and returns the corrsponding NewArray expression as typed expression
        let buildNewArray (bType, ex) =
            let ex = resolve context ex
            inference.Constrain(ResolvedType.New Int .> ex.ResolvedType) |> List.iter diagnose

            let resolvedBase = symbols.ResolveType diagnose bType
            let arrType = resolvedBase |> ArrayType |> ResolvedType.create (TypeRange.inferred this.Range)
            let quantumDep = ex.InferredInformation.HasLocalQuantumDependency
            (NewArray(resolvedBase, ex), arrType) |> exprWithoutTypeArgs this.Range (inferred false quantumDep)

        /// Resolves and verifies all given items of a value array literal, and returns the corresponding ValueArray as typed expression.
        let buildValueArray values =
            let values = values |> Seq.map (resolve context) |> ImmutableArray.CreateRange
            let resolvedType = values |> verifyValueArray inference this.RangeOrDefault |> takeDiagnostics

            (ValueArray values, resolvedType)
            |> exprWithoutTypeArgs this.Range (inferred false (anyQuantumDep values))

        /// Resolves and verifies the sized array constructor expression and returns it as a typed expression.
        let buildSizedArray value size =
            let value = resolve context value
            let arrayType = ArrayType value.ResolvedType |> ResolvedType.create (TypeRange.inferred this.Range)
            let size = resolve context size
            inference.Constrain(ResolvedType.New Int .> size.ResolvedType) |> List.iter diagnose

            (SizedArray(value, size), arrayType)
            |> exprWithoutTypeArgs this.Range (inferred false (anyQuantumDep [ value; size ]))

        /// Resolves and verifies the given array expression and index expression of an array item access expression,
        /// and returns the corresponding ArrayItem expression as typed expression.
        let buildArrayItem (array, index: QsExpression) =
            let array = resolve context array
            let index = resolveSlicing array index
            let itemType = inference.Fresh this.RangeOrDefault

            inference.Constrain(HasIndex(array.ResolvedType, index.ResolvedType, itemType) |> Class)
            |> List.iter diagnose

            (ArrayItem(array, index), itemType)
            |> exprWithoutTypeArgs this.Range (inferred false (anyQuantumDep [ array; index ]))

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
        let buildNamedItem (ex, acc) =
            let ex = resolve context ex
            let itemName = buildItemName acc
            let itemType = inference.Fresh this.RangeOrDefault
            HasField(ex.ResolvedType, itemName, itemType) |> Class |> inference.Constrain |> List.iter diagnose

            (NamedItem(ex, itemName), itemType)
            |> exprWithoutTypeArgs this.Range (inferred false ex.InferredInformation.HasLocalQuantumDependency)

        /// Resolves and verifies the given left hand side, access expression, and right hand side of a copy-and-update expression,
        /// and returns the corresponding copy-and-update expression as typed expression.
        let buildCopyAndUpdate (container, accessor: QsExpression, item) =
            let container = resolve context container
            let item = resolve context item
            let itemType = inference.Fresh this.RangeOrDefault

            let unqualifiedSymbol, isRecordUpdate =
                match accessor.Expression with
                | Identifier ({ Symbol = Symbol name } as symbol, Null) ->
                    Some symbol, Seq.forall (fun v -> v.VariableName <> name) symbols.CurrentDeclarations.Variables
                | _ -> None, false

            let recordUpdate field =
                let itemName = buildItemName field

                (Identifier(itemName, Null), itemType) |> exprWithoutTypeArgs field.Range (inferred false false),
                HasField(container.ResolvedType, itemName, itemType)

            let arrayUpdate index =
                index, HasIndex(container.ResolvedType, index.ResolvedType, itemType)

            let accessor, cls =
                match unqualifiedSymbol with
                | Some symbol when isRecordUpdate -> recordUpdate symbol
                | _ -> resolveSlicing container accessor |> arrayUpdate

            inference.Constrain(Class cls) |> List.iter diagnose
            inference.Constrain(itemType .> item.ResolvedType) |> List.iter diagnose

            (CopyAndUpdate(container, accessor, item), container.ResolvedType)
            |> exprWithoutTypeArgs this.Range (inferred false ([ container; accessor; item ] |> anyQuantumDep))

        /// Resolves and verifies the given left hand side and right hand side of a range operator,
        /// and returns the corresponding RANGE expression as typed expression.
        /// NOTE: handles both the case of a range with and without explicitly specified step size
        /// *under the assumption* that the range operator is left associative.
        let buildRange (lhs: QsExpression, rhs) =
            let rhs = resolve context rhs
            inference.Constrain(ResolvedType.New Int .> rhs.ResolvedType) |> List.iter diagnose

            let lhs =
                match lhs.Expression with
                | RangeLiteral (start, step) ->
                    let start = resolve context start
                    let step = resolve context step
                    verifyAreIntegers inference start step |> List.iter diagnose

                    (RangeLiteral(start, step), Range |> ResolvedType.create (TypeRange.inferred this.Range))
                    |> exprWithoutTypeArgs this.Range (inferred false (anyQuantumDep [ start; step ]))
                | _ ->
                    resolve context lhs
                    |> (fun resStart ->
                        inference.Constrain(ResolvedType.New Int .> resStart.ResolvedType) |> List.iter diagnose
                        resStart)

            (RangeLiteral(lhs, rhs), Range |> ResolvedType.create (TypeRange.inferred this.Range))
            |> exprWithoutTypeArgs this.Range (inferred false (anyQuantumDep [ lhs; rhs ]))

        /// Resolves and verifies the given expression with the given verification function,
        /// and returns the corresponding expression built with buildExprKind as typed expression.
        let verifyAndBuildWith context' buildExprKind verify ex =
            let ex = resolve context' ex
            let exType = verify ex |> takeDiagnostics

            (buildExprKind ex, exType)
            |> exprWithoutTypeArgs this.Range (inferred false ex.InferredInformation.HasLocalQuantumDependency)

        /// Resolves and verifies the given left hand side and right hand side of an arithmetic operator,
        /// and returns the corresponding expression built with buildExprKind as typed expression.
        let buildArithmeticOp buildExprKind (lhs, rhs) =
            let lhs = resolve context lhs
            let rhs = resolve context rhs
            let resolvedType = verifyArithmeticOp inference this.Range lhs rhs |> takeDiagnostics

            (buildExprKind (lhs, rhs), resolvedType)
            |> exprWithoutTypeArgs this.Range (inferred false (anyQuantumDep [ lhs; rhs ]))

        /// Resolves and verifies the given left hand side and right hand side of an addition operator,
        /// and returns the corresponding ADD expression as typed expression.
        /// Note: ADD is used for both arithmetic expressions as well as concatenation expressions.
        /// If the resolved type of the given lhs supports concatenation, then the verification is done for a concatenation expression,
        /// and otherwise it is done for an arithmetic expression.
        let buildAddition (lhs, rhs) =
            let lhs = resolve context lhs
            let rhs = resolve context rhs
            let resolvedType = verifySemigroup inference this.Range lhs rhs |> takeDiagnostics

            (ADD(lhs, rhs), resolvedType)
            |> exprWithoutTypeArgs this.Range (inferred false (anyQuantumDep [ lhs; rhs ]))

        /// Resolves and verifies the given left hand side and right hand side of a power operator,
        /// and returns the corresponding POW expression as typed expression.
        /// Note: POW can take two integers or two doubles, in which case the result is a double, or it can take a big
        /// integer and an integer, in which case the result is a big integer.
        let buildPower (lhs, rhs) =
            let lhs = resolve context lhs
            let rhs = resolve context rhs

            let resolvedType =
                if inference.Resolve(lhs.ResolvedType).Resolution = BigInt then
                    inference.Constrain(ResolvedType.New Int .> rhs.ResolvedType) |> List.iter diagnose
                    lhs.ResolvedType
                else
                    verifyArithmeticOp inference this.Range lhs rhs |> takeDiagnostics

            (POW(lhs, rhs), resolvedType)
            |> exprWithoutTypeArgs this.Range (inferred false (anyQuantumDep [ lhs; rhs ]))

        /// Resolves and verifies the given left hand side and right hand side of a binary integral operator,
        /// and returns the corresponding expression built with buildExprKind as typed expression of type Int or BigInt, as appropriate.
        let buildIntegralOp buildExprKind (lhs, rhs) =
            let lhs = resolve context lhs
            let rhs = resolve context rhs
            let resolvedType = verifyIntegralOp inference this.Range lhs rhs |> takeDiagnostics

            (buildExprKind (lhs, rhs), resolvedType)
            |> exprWithoutTypeArgs this.Range (inferred false (anyQuantumDep [ lhs; rhs ]))

        /// Resolves and verifies the given left hand side and right hand side of a shift operator,
        /// and returns the corresponding expression built with buildExprKind as typed expression of type Int or BigInt, as appropriate.
        let buildShiftOp buildExprKind (lhs, rhs) =
            let lhs = resolve context lhs
            let rhs = resolve context rhs
            let resolvedType = verifyIsIntegral inference lhs |> takeDiagnostics
            inference.Constrain(ResolvedType.New Int .> rhs.ResolvedType) |> List.iter diagnose

            (buildExprKind (lhs, rhs), resolvedType)
            |> exprWithoutTypeArgs this.Range (inferred false (anyQuantumDep [ lhs; rhs ]))

        /// Resolves and verifies the given left hand side and right hand side of a binary boolean operator,
        /// and returns the corresponding expression built with buildExprKind as typed expression of type Bool.
        let buildBooleanOpWith verify shortCircuits buildExprKind (lhs, rhs) =
            let lhs = resolve context lhs
            let rhs = resolve context rhs
            verify lhs rhs |> List.iter diagnose

            if shortCircuits then verifyConditionalExecution rhs |> List.iter diagnose

            (buildExprKind (lhs, rhs), ResolvedType.New Bool)
            |> exprWithoutTypeArgs this.Range (inferred false (anyQuantumDep [ lhs; rhs ]))

        /// Resolves and verifies the given condition, left hand side, and right hand side of a conditional expression (if-else-shorthand),
        /// and returns the corresponding conditional expression as typed expression.
        let buildConditional (cond, ifTrue, ifFalse) =
            let cond = resolve context cond
            let ifTrue = resolve context ifTrue
            let ifFalse = resolve context ifFalse
            inference.Constrain(ResolvedType.New Bool .> cond.ResolvedType) |> List.iter diagnose
            verifyConditionalExecution ifTrue |> List.iter diagnose
            verifyConditionalExecution ifFalse |> List.iter diagnose

            let exType =
                inference.Intersect(ifTrue.ResolvedType, ifFalse.ResolvedType)
                |> takeDiagnostics
                |> ResolvedType.withAllRanges (TypeRange.inferred this.Range)

            (CONDITIONAL(cond, ifTrue, ifFalse), exType)
            |> exprWithoutTypeArgs this.Range (inferred false (anyQuantumDep [ cond; ifTrue; ifFalse ]))

        /// Resolves the given expression and verifies that its type is indeed a user defined type.
        /// Determines the underlying type of the user defined type and returns the corresponding UNWRAP expression as typed expression of that type.
        let buildUnwrap ex =
            let ex = resolve context ex
            let exType = inference.Fresh this.RangeOrDefault
            Unwrap(ex.ResolvedType, exType) |> Class |> inference.Constrain |> List.iter diagnose

            (UnwrapApplication ex, exType)
            |> exprWithoutTypeArgs this.Range (inferred false ex.InferredInformation.HasLocalQuantumDependency)

        /// Resolves and verifies the given left hand side and right hand side of a call expression,
        /// and returns the corresponding expression as typed expression.
        let buildCall callable arg =
            let callable = resolve context callable
            let arg = resolve context arg
            let callExpression = CallLikeExpression(callable, arg)
            let argType, partialType = partialArgType inference arg.ResolvedType

            if Option.isNone partialType then
                HasFunctorsIfOperation(callable.ResolvedType, Set.ofSeq symbols.RequiredFunctorSupport)
                |> Class
                |> inference.Constrain
                |> List.iter diagnose

            let output = inference.Fresh this.RangeOrDefault

            if Option.isSome partialType || context.IsInOperation then
                Callable(callable.ResolvedType, argType, output)
                |> Class
                |> inference.Constrain
                |> List.iter diagnose
            else
                let functionType = ResolvedType.withKind (QsTypeKind.Function(argType, output)) callable.ResolvedType
                let diagnostics = inference.Constrain(callable.ResolvedType <. functionType)

                if inference.Resolve callable.ResolvedType |> isOperation then
                    QsCompilerDiagnostic.Error (ErrorCode.OperationCallOutsideOfOperation, []) this.RangeOrDefault
                    |> diagnose
                else
                    List.iter diagnose diagnostics

            let resultType =
                match partialType with
                | Some missing ->
                    let result = inference.Fresh this.RangeOrDefault

                    HasPartialApplication(callable.ResolvedType, missing, result)
                    |> Class
                    |> inference.Constrain
                    |> List.iter diagnose

                    result
                | None -> output

            let hasQuantumDependency =
                if Option.isSome partialType || inference.Resolve callable.ResolvedType |> isFunction then
                    anyQuantumDep [ callable; arg ]
                else
                    true

            let info = InferredExpressionInformation.New(isMutable = false, quantumDep = hasQuantumDependency)
            TypedExpression.New(callExpression, callable.TypeParameterResolutions, resultType, info, this.Range)

        let buildLambda (lambda: Lambda<QsExpression, QsType>) =
            symbols.BeginScope ImmutableHashSet.Empty
            let freeVars = Context.freeVariables this

            let diagnoseMutable name range =
                QsNullable.defaultValue Range.Zero range
                |> QsCompilerDiagnostic.Error(ErrorCode.MutableClosure, [ name ])
                |> diagnose

            for var in symbols.CurrentDeclarations.Variables do
                if var.InferredInformation.IsMutable then
                    Map.tryFind var.VariableName freeVars |> Option.iter (diagnoseMutable var.VariableName |> Seq.iter)

            let rec mapArgumentTuple =
                function
                | QsTupleItem (decl: LocalVariableDeclaration<_, _>) ->
                    let var: LocalVariableDeclaration<QsLocalSymbol, ResolvedType> =
                        let resDecl = decl.WithPosition(inference.GetRelativeStatementPosition() |> Value)
                        resDecl.WithType(inference.Fresh decl.Range)

                    let added, diagnostics = symbols.TryAddVariableDeclartion var
                    Array.iter diagnose diagnostics
                    if added then QsTupleItem var else QsTupleItem(var.WithName InvalidName)
                | QsTuple tuple -> tuple |> Seq.map mapArgumentTuple |> ImmutableArray.CreateRange |> QsTuple

            let argTuple = mapArgumentTuple lambda.ArgumentTuple

            let rec getArgumentTupleType =
                function
                | QsTupleItem (decl: LocalVariableDeclaration<_, _>) -> decl.Type
                | QsTuple tuple ->
                    tuple |> Seq.map getArgumentTupleType |> ImmutableArray.CreateRange |> TupleType |> ResolvedType.New

            let inputType =
                match argTuple with
                | QsTuple tuple when tuple.Length = 0 -> UnitType |> ResolvedType.New
                | _ -> getArgumentTupleType argTuple

            let lambda' =
                verifyAndBuildWith
                    { context with IsInOperation = lambda.Kind = LambdaKind.Operation }
                    (fun body' -> Lambda.create lambda.Kind argTuple body' |> Lambda)
                    (fun body' -> inferLambda inference this.Range lambda.Kind inputType body', [])
                    lambda.Body

            symbols.EndScope()
            lambda'

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
        | AdjointApplication ex -> verifyAndBuildWith context AdjointApplication (verifyAdjointApplication inference) ex
        | ControlledApplication ex ->
            verifyAndBuildWith context ControlledApplication (verifyControlledApplication inference) ex
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
        | NEG ex -> verifyAndBuildWith context NEG (verifySupportsArithmetic inference) ex
        | BNOT ex -> verifyAndBuildWith context BNOT (verifyIsIntegral inference) ex
        | NOT ex ->
            verifyAndBuildWith
                context
                NOT
                (fun ex' ->
                    Bool |> ResolvedType.create (TypeRange.inferred this.Range),
                    inference.Constrain(ResolvedType.New Bool .> ex'.ResolvedType))
                ex
        | Lambda lambda -> buildLambda lambda
