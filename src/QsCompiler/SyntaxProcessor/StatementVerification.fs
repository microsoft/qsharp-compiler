// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.Statements

open System
open System.Collections.Immutable

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxProcessing
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.Expressions
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference.RelationOps
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace

// some utils for type checking statements

let private resolveExpr context (expr: QsExpression) =
    let diagnostics = ResizeArray()
    expr.Resolve context diagnostics.Add, diagnostics

/// If the given SymbolTracker specifies that an auto-inversion of the routine is requested,
/// checks if the given typed expression has any local quantum dependencies.
/// If it does, returns an array with suitable diagnostics. Returns an empty array otherwise.
/// Remark: inversions can only be auto-generated if the only quantum dependencies occur within expresssion statements.
let private onAutoInvertCheckQuantumDependency (symbols: SymbolTracker) (expr: TypedExpression) =
    [|
        if symbols.RequiredFunctorSupport.Contains Adjoint
           && expr.InferredInformation.HasLocalQuantumDependency then
            QsCompilerDiagnostic.Error (ErrorCode.QuantumDependencyOutsideExprStatement, []) (rangeOrDefault expr)
    |]

/// If the given SymbolTracker specifies that an auto-inversion of the routine is requested,
/// returns an array with containing a diagnostic for the given range with the given error code.
/// Returns an empty array otherwise.
let private onAutoInvertGenerateError (errCode, range) (symbols: SymbolTracker) =
    if symbols.RequiredFunctorSupport.Contains Adjoint then
        [| range |> QsCompilerDiagnostic.Error errCode |]
    else
        [||]

// utils for building QsStatements from QsFragmentKinds

/// Builds a Q# statement of the given kind at the given location,
/// given a sequence of all variables and type declared within that statement *only*.
let private asStatement comments location vars kind =
    QsStatement.New comments (Value location) (kind, vars)

/// Resolves and verifies the given Q# expression given a symbol tracker containing all currently declared symbols,
/// verifies that the resolved expression is indeed of type Unit, and builds a Q# expression-statement at the given location from it.
/// Returns the built statement as well as an array of diagnostics generated during resolution and verification.
let NewExpressionStatement comments location context expr =
    let expr, diagnostics = resolveExpr context expr

    if context.Inference.Constrain(ResolvedType.New UnitType .> expr.ResolvedType) |> List.isEmpty |> not then
        let ty = context.Inference.Resolve expr.ResolvedType |> SyntaxTreeToQsharp.Default.ToCode
        let range = QsNullable.defaultValue Range.Zero expr.Range
        QsCompilerDiagnostic.Error (ErrorCode.ValueImplicitlyIgnored, [ ty ]) range |> diagnostics.Add

    QsExpressionStatement expr |> asStatement comments location LocalDeclarations.Empty, diagnostics.ToArray()

/// Resolves and verifies the given Q# expression given the resolution context, verifies that the resolved expression is
/// indeed of type String, and builds a Q# fail-statement at the given location from it.
///
/// Returns the built statement as well as an array of diagnostics generated during resolution and verification.
let NewFailStatement comments location context expr =
    let expr, diagnostics = resolveExpr context expr
    context.Inference.Constrain(ResolvedType.New String .> expr.ResolvedType) |> diagnostics.AddRange
    onAutoInvertCheckQuantumDependency context.Symbols expr |> diagnostics.AddRange
    QsFailStatement expr |> asStatement comments location LocalDeclarations.Empty, diagnostics.ToArray()

/// Resolves and verifies the given Q# expression using the resolution context.
/// Verifies that the type of the resolved expression is indeed compatible with the expected return type of the parent callable.
/// Uses VerifyAssignment to verify that this is indeed the case.
/// Builds the corresponding Q# return-statement at the given location,
/// and returns it along with an array of diagnostics generated during resolution and verification.
/// Errors due to the statement not satisfying the necessary conditions for the required auto-generation of specializations
/// (specified by the given SymbolTracker) are also included in the returned diagnostics.
let NewReturnStatement comments (location: QsLocation) context expr =
    let expr, diagnostics = resolveExpr context expr
    context.Inference.Constrain(context.ReturnType .> expr.ResolvedType) |> diagnostics.AddRange

    onAutoInvertGenerateError ((ErrorCode.ReturnStatementWithinAutoInversion, []), location.Range) context.Symbols
    |> diagnostics.AddRange

    QsReturnStatement expr |> asStatement comments location LocalDeclarations.Empty, diagnostics.ToArray()

/// Resolves and verifies the given Q# expressions using the resolution context.
/// Verifies that the type of the resolved right hand side expression is indeed compatible with the resolved type of the left hand side expression.
/// Uses VerifyAssignment to verify that this is indeed the case.
/// Builds the corresponding Q# set-statement at the given location,
/// and returns it along with an array of diagnostics generated during resolution and verification.
/// Errors due to the statement not satisfying the necessary conditions for the required auto-generation of specializations
/// (specified by the given SymbolTracker) are also included in the returned diagnostics.
let NewValueUpdate comments (location: QsLocation) context (lhs, rhs) =
    let lhs, diagnostics = resolveExpr context lhs
    let rhs, rhsDiagnostics = resolveExpr context rhs
    let localQdep = rhs.InferredInformation.HasLocalQuantumDependency
    diagnostics.AddRange rhsDiagnostics
    context.Inference.Constrain(lhs.ResolvedType .> rhs.ResolvedType) |> diagnostics.AddRange

    let rec verifyMutability: TypedExpression -> _ =
        function
        | Tuple exs -> exs |> Seq.iter verifyMutability
        | Item ex when ex.InferredInformation.IsMutable ->
            match ex.Expression with
            | Identifier (LocalVariable id, Null) -> context.Symbols.UpdateQuantumDependency id localQdep
            | _ -> ()
        | Item ex ->
            QsCompilerDiagnostic.Error (ErrorCode.UpdateOfImmutableIdentifier, []) (ex.Range.ValueOr Range.Zero)
            |> diagnostics.Add
        | _ -> () // both missing and invalid expressions on the lhs are fine

    verifyMutability lhs

    onAutoInvertGenerateError ((ErrorCode.ValueUpdateWithinAutoInversion, []), location.Range) context.Symbols
    |> diagnostics.AddRange

    QsValueUpdate.New(lhs, rhs)
    |> QsValueUpdate
    |> asStatement comments location LocalDeclarations.Empty,
    diagnostics.ToArray()

/// Adds a variable declaration with the given name, quantum dependency, and type at the given location to the given symbol tracker.
/// Generates the corresponding error(s) if a symbol with the same name is already visible on that scope and/or the given name is not a valid variable name.
/// Generates an InvalidUseOfTypeParameterizedObject error if the given variable type contains external type parameters
/// (i.e. type parameters that do no belong to the parent callable associated with the given symbol tracker).
/// Returns the pushed declaration as Some, if the declaration was successfully added to given symbol tracker, and None otherwise.
let private tryAddDeclaration isMutable (symbols: SymbolTracker) (name: string, location, localQdep) rhsType =
    let t, tpErr = rhsType, [||]
    let decl = LocalVariableDeclaration.New isMutable (location, name, t, localQdep)
    let added, errs = symbols.TryAddVariableDeclartion decl
    (if added then Some decl else None), Array.append tpErr errs

/// Given a Q# symbol, as well as the expression on the right hand side that is assigned to it,
/// resolves and verifies the assignment using VerifyBinding and the resolution context.
/// Pushes all determined variable declarations into the current scope of the symbol tracker,
/// generating the corresponding error(s) if a symbol with the same name is already visible on that scope and/or the symbol is not a valid variable name.
/// Builds the corresponding Q# let- or mutable-statement (depending on the given kind) at the given location,
/// and returns it along with an array of all generated diagnostics.
let private newBinding kind comments (location: QsLocation) context (symbol, expr) =
    let expr, diagnostics = resolveExpr context expr

    let symbolTuple, varDeclarations, bindingDiagnostics =
        let addDeclaration (name, range) =
            tryAddDeclaration
                (kind = MutableBinding)
                context.Symbols
                (name, (Value location.Offset, range), expr.InferredInformation.HasLocalQuantumDependency)

        verifyBinding context.Inference addDeclaration (symbol, expr.ResolvedType) false

    diagnostics.AddRange bindingDiagnostics
    onAutoInvertCheckQuantumDependency context.Symbols expr |> diagnostics.AddRange

    QsBinding.New kind (symbolTuple, expr)
    |> QsVariableDeclaration
    |> asStatement comments location (LocalDeclarations.New varDeclarations),
    diagnostics.ToArray()

/// Resolves, verifies and builds the Q# let-statement at the given location binding the given expression to the given symbol.
/// Adds the corresponding local variable declarations to the given symbol tracker.
/// Returns the built statement along with an array containing all diagnostics generated in the process.
let NewImmutableBinding comments location symbols (symbol, expr) =
    newBinding QsBindingKind.ImmutableBinding comments location symbols (symbol, expr)

/// Resolves, verifies and builds the Q# mutable-statement at the given location binding the given expression to the given symbol.
/// Adds the corresponding local variable declarations to the given symbol tracker.
/// Returns the built statement along with an array containing all diagnostics generated in the process.
let NewMutableBinding comments location symbols (symbol, expr) =
    newBinding QsBindingKind.MutableBinding comments location symbols (symbol, expr)

type BlockStatement<'T> = delegate of QsScope -> 'T

/// Given the location of the statement header and the resolution context,
/// builds the Q# for-statement at the given location, that iterates over the given expression, with the given symbol as loop variable(s).
/// In order to do so, verifies the expression is indeed iterable,
/// and verifies that the shape of the symbol tuple is compatible with the type of the iteration item using VerifyBinding.
/// Adds the corresponding variable declarations to the given symbol tracker.
/// Returns an array with all generated diagnostics,
/// as well as a delegate that given a Q# scope returns the built for-statement with the given scope as the body.
/// NOTE: the declared loop variables are *not* visible after the statements ends, hence they are *not* attaches as local declarations to the statement!
let NewForStatement comments (location: QsLocation) context (symbol, expr) =
    let expr, diagnostics = resolveExpr context expr
    let itemType, iterableDiagnostics = verifyIsIterable context.Inference expr
    diagnostics.AddRange iterableDiagnostics

    let symbolTuple, _, bindingDiagnostics =
        let addDeclaration (name, range) =
            tryAddDeclaration
                false
                context.Symbols
                (name, (Value location.Offset, range), expr.InferredInformation.HasLocalQuantumDependency)

        verifyBinding context.Inference addDeclaration (symbol, itemType) false

    diagnostics.AddRange bindingDiagnostics
    onAutoInvertCheckQuantumDependency context.Symbols expr |> diagnostics.AddRange

    let forLoop body =
        QsForStatement.New((symbolTuple, itemType), expr, body) |> QsForStatement

    BlockStatement(forLoop >> asStatement comments location LocalDeclarations.Empty), diagnostics.ToArray()

/// Given the location of the statement header and the resolution context,
/// builds the Q# while-statement at the given location with the given expression as condition.
/// Verifies the expression is indeed of type Bool, and returns an array with all generated diagnostics,
/// as well as a delegate that given a Q# scope returns the built while-statement with the given scope as the body.
let NewWhileStatement comments (location: QsLocation) context condition =
    let condition, diagnostics = resolveExpr context condition
    context.Inference.Constrain(ResolvedType.New Bool .> condition.ResolvedType) |> diagnostics.AddRange

    let whileLoop body =
        QsWhileStatement.New(condition, body) |> QsWhileStatement

    BlockStatement(whileLoop >> asStatement comments location LocalDeclarations.Empty), diagnostics.ToArray()

/// Resolves and verifies the given Q# expression using the resolution context.
/// Verifies that the type of the resolved expression is indeed of kind Bool.
/// Returns an array of all diagnostics generated during resolution and verification,
/// as well as a delegate that given a positioned block of Q# statements returns the corresponding conditional block.
let NewConditionalBlock comments location context condition =
    let condition, diagnostics = resolveExpr context condition
    context.Inference.Constrain(ResolvedType.New Bool .> condition.ResolvedType) |> diagnostics.AddRange
    onAutoInvertCheckQuantumDependency context.Symbols condition |> diagnostics.AddRange
    BlockStatement(fun body -> condition, QsPositionedBlock.New comments (Value location) body), diagnostics.ToArray()

/// <summary>
/// Given a conditional block for the if-clause of a Q# if-statement, a sequence of conditional blocks for the elif-clauses,
/// as well as optionally a positioned block of Q# statements and its location for the else-clause, builds and returns the complete if-statement.
/// </summary>
/// <exception cref="ArgumentException"><paramref name="ifBlock"/> contains no location information.</exception>
let NewIfStatement (ifBlock: TypedExpression * QsPositionedBlock) elifBlocks elseBlock =
    let location =
        match (snd ifBlock).Location with
        | Null -> ArgumentException "No location is set for the given if-block." |> raise
        | Value location -> location

    let condBlocks = Seq.append (Seq.singleton ifBlock) elifBlocks

    QsConditionalStatement.New(condBlocks, elseBlock)
    |> QsConditionalStatement
    |> asStatement QsComments.Empty location LocalDeclarations.Empty

/// Given a positioned block of Q# statements for the repeat-block of a Q# RUS-statement, a typed expression containing the success condition,
/// as well as a positioned block of Q# statements for the fixup-block, builds the complete RUS-statement at the given location and returns it.
/// Returns an array with diagnostics generated if the statement does not satisfy the necessary conditions
/// for the required auto-generation of specializations (specified by the given SymbolTracker).
let NewRepeatStatement (symbols: SymbolTracker) (repeatBlock: QsPositionedBlock, successCondition, fixupBlock) =
    let location =
        match repeatBlock.Location with
        | Null -> ArgumentException "no location is set for the given repeat-block" |> raise
        | Value loc -> loc

    let autoGenErrs =
        symbols |> onAutoInvertGenerateError ((ErrorCode.RUSloopWithinAutoInversion, []), location.Range)

    QsRepeatStatement.New(repeatBlock, successCondition, fixupBlock)
    |> QsRepeatStatement
    |> asStatement QsComments.Empty location LocalDeclarations.Empty,
    autoGenErrs

/// <summary>
/// Given a positioned block of Q# statements specifying the transformation to conjugate (inner transformation V),
/// as well as a positioned block of Q# statements specifying the transformation to conjugate it with (outer transformation U),
/// builds and returns the corresponding conjugation statement representing the patter U*VU where the order of application is right to left and U* is the adjoint of U.
/// Returns an array with diagnostics and the corresponding statement offset for all invalid variable reassignments in the apply-block.
/// </summary>
/// <exception cref="ArgumentException"><paramref name="outer"/> contains no location information.</exception>
let NewConjugation (outer: QsPositionedBlock, inner: QsPositionedBlock) =
    let location =
        match outer.Location with
        | Null ->
            ArgumentException "no location is set for the given within-block defining the conjugating transformation"
            |> raise
        | Value loc -> loc

    let usedInOuter =
        let identifiers = AccumulateIdentifiers()
        identifiers.Statements.OnScope outer.Body |> ignore
        identifiers.SharedState.UsedLocalVariables

    let updatedInInner =
        let identifiers = AccumulateIdentifiers()
        identifiers.Statements.OnScope inner.Body |> ignore
        identifiers.SharedState.ReassignedVariables

    let updateErrs =
        updatedInInner
        |> Seq.filter (fun updated -> usedInOuter.Contains updated.Key)
        |> Seq.collect id
        |> Seq.map (fun loc ->
            loc.Offset + loc.Range |> QsCompilerDiagnostic.Error(ErrorCode.InvalidReassignmentInApplyBlock, []))
        |> Seq.toArray

    QsConjugation.New(outer, inner)
    |> QsConjugation
    |> asStatement QsComments.Empty location LocalDeclarations.Empty,
    updateErrs

/// Given the location of the statement header and the resolution context,
/// builds the Q# using- or borrowing-statement (depending on the given kind) at the given location
/// that initializes the qubits in the given initializer expression and assigns them to the given symbol.
/// In order to do so, resolves and verifies the initializer expression, as well as its binding to the given symbol using VerifyBinding.
/// Adds the corresponding variable declarations to the given symbol tracker.
/// Returns an array with all generated diagnostics,
/// as well as a delegate that given a Q# scope returns the built using- or borrowing-statement with the given scope as the body.
/// NOTE: the declared variables are *not* visible after the statements ends, hence they are *not* attaches as local declarations to the statement!
let private newBindingScope kind comments (location: QsLocation) context (symbol: QsSymbol, init: QsInitializer) =
    let rec verifyInitializer init =
        match init.Initializer with
        | SingleQubitAllocation ->
            SingleQubitAllocation |> ResolvedInitializer.create (TypeRange.inferred init.Range), Seq.empty
        | QubitRegisterAllocation size ->
            let size, diagnostics = resolveExpr context size
            context.Inference.Constrain(ResolvedType.New Int .> size.ResolvedType) |> diagnostics.AddRange
            onAutoInvertCheckQuantumDependency context.Symbols size |> diagnostics.AddRange

            QubitRegisterAllocation size |> ResolvedInitializer.create (TypeRange.inferred init.Range),
            upcast diagnostics
        | QubitTupleAllocation items ->
            let items, diagnostics = items |> Seq.map verifyInitializer |> Seq.toList |> List.unzip

            ImmutableArray.CreateRange items
            |> QubitTupleAllocation
            |> ResolvedInitializer.create (TypeRange.inferred init.Range),
            Seq.concat diagnostics
        | InvalidInitializer ->
            InvalidInitializer |> ResolvedInitializer.create (TypeRange.inferred init.Range), Seq.empty

    let init, initDiagnostics = verifyInitializer init

    let symbolTuple, _, bindingDiagnostics =
        let addDeclaration (name, range) =
            tryAddDeclaration false context.Symbols (name, (Value location.Offset, range), false)

        verifyBinding context.Inference addDeclaration (symbol, init.Type) true

    let bindingScope body =
        QsQubitScope.New kind ((symbolTuple, init), body) |> QsQubitScope

    BlockStatement(bindingScope >> asStatement comments location LocalDeclarations.Empty),
    Seq.append initDiagnostics bindingDiagnostics |> Seq.toArray

/// Resolves, verifies and builds the Q# using-statement at the given location binding the given initializer to the given symbol.
/// Adds the corresponding local variable declarations to the given symbol tracker.
/// Returns the built statement along with an array containing all diagnostics generated in the process.
let NewAllocateScope comments location symbols (symbol, init) =
    newBindingScope QsQubitScopeKind.Allocate comments location symbols (symbol, init)

/// Resolves, verifies and builds the Q# borrowing-statement at the given location binding the given initializer to the given symbol.
/// Adds the corresponding local variable declarations to the given symbol tracker.
/// Returns the built statement along with an array containing all diagnostics generated in the process.
let NewBorrowScope comments location symbols (symbol, init) =
    newBindingScope QsQubitScopeKind.Borrow comments location symbols (symbol, init)
