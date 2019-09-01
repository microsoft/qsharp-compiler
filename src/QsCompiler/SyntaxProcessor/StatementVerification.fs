﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.Statements

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Linq
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.Expressions
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.VerificationTools
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SymbolTracker
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace


// some utils for type checking statements

/// Given a verication function that takes an error logging function as well as an expression type and its range, 
/// first resolves the given expression, and then verifies its resolved type with the verification function. 
/// Returns the typed expression built upon resolution, the return value of the verification function, 
/// and an array with the diagnostics generated during resolution and verification. 
let private VerifyWith verification symbols (expr : QsExpression) = 
    let accumulatedDiagnostics = new List<QsCompilerDiagnostic>() 
    let addError code = QsCompilerDiagnostic.Error code >> accumulatedDiagnostics.Add
    let typedExpr = expr.Resolve symbols accumulatedDiagnostics.Add
    let resVerification = verification addError (typedExpr.ResolvedType, expr.RangeOrDefault)
    typedExpr, resVerification, accumulatedDiagnostics |> Seq.toArray

/// Resolves the given expression, and returns the typed expression built upon resolution, 
/// as well as an array with the diagnostics generated during resolution. 
let private Verify symbols expr = 
    VerifyWith (fun _ _ _ -> ()) symbols expr |> fun (ex,_,err) -> ex, err

/// If the given SymbolTracker specifies that an auto-inversion of the routine is requested, 
/// checks if the given typed expression has any local quantum dependencies. 
/// If it does, returns an array with suitable diagnostics. Returns an empty array otherwise. 
/// Remark: inversions can only be auto-generated if the only quantum dependencies occur within expresssion statements. 
let private onAutoInvertCheckQuantumDependency (symbols : SymbolTracker<_>) (ex : TypedExpression, range) = 
    if not (symbols.RequiredFunctorSupport.Contains QsFunctor.Adjoint && ex.InferredInformation.HasLocalQuantumDependency) then [||]
    else [| range |> QsCompilerDiagnostic.Error (ErrorCode.QuantumDependencyOutsideExprStatement, []) |] 

/// If the given SymbolTracker specifies that an auto-inversion of the routine is requested, 
/// returns an array with containing a diagnostic for the given range with the given error code.
/// Returns an empty array otherwise.
let private onAutoInvertGenerateError (errCode, range) (symbols : SymbolTracker<_>) = 
    if not (symbols.RequiredFunctorSupport.Contains QsFunctor.Adjoint) then [||]
    else [| range |> QsCompilerDiagnostic.Error errCode |] 


// utils for building QsStatements from QsFragmentKinds

/// Builds a Q# statement of the given kind at the given location,
/// given a sequence of all variables and type declared within that statement *only*.
let private asStatement comments location vars kind = QsStatement.New comments (Value location) (kind, vars)

/// Resolves and verifies the given Q# expression given a symbol tracker containing all currently declared symbols,
/// verifies that the resolved expression is indeed of type Unit, and builds a Q# expression-statement at the given location from it. 
/// Returns the built statement as well as an array of diagnostics generated during resolution and verification. 
let NewExpressionStatement comments location symbols expr = 
    let verifiedExpr, _, diagnostics = VerifyWith VerifyIsUnit symbols expr
    verifiedExpr |> QsExpressionStatement |> asStatement comments location [], diagnostics

/// Resolves and verifies the given Q# expression given a symbol tracker containing all currently declared symbols,
/// verifies that the resolved expression is indeed of type String, and builds a Q# fail-statement at the given location from it. 
/// Returns the built statement as well as an array of diagnostics generated during resolution and verification. 
let NewFailStatement comments location symbols expr = 
    let verifiedExpr, _, diagnostics = VerifyWith VerifyIsString symbols expr
    let autoGenErrs = (verifiedExpr, expr.RangeOrDefault) |> onAutoInvertCheckQuantumDependency symbols
    verifiedExpr |> QsFailStatement |> asStatement comments location [], Array.concat [diagnostics; autoGenErrs]

/// Resolves and verifies the given Q# expression given a symbol tracker containing all currently declared symbols.
/// Verifies that the type of the resolved expression is indeed compatible with the expected return type associated with the symbol tracker.
/// Uses VerifyAssignment to verify that this is indeed the case.
/// Builds the corresponding Q# return-statement at the given location, 
/// and returns it along with an array of diagnostics generated during resolution and verification. 
/// Errors due to the statement not satisfying the necessary conditions for the required auto-generation of specializations 
/// (specified by the given SymbolTracker) are also included in the returned diagnostics. 
let NewReturnStatement comments (location : QsLocation) (symbols : SymbolTracker<_>) expr =
    let VerifyIsReturnType = VerifyAssignment symbols.ExpectedReturnType symbols.Parent ErrorCode.TypeMismatchInReturn
    let verifiedExpr, _, diagnostics = VerifyWith VerifyIsReturnType symbols expr 
    let autoGenErrs = symbols |> onAutoInvertGenerateError ((ErrorCode.ReturnStatementWithinAutoInversion, []), location.Range)
    verifiedExpr |> QsReturnStatement |> asStatement comments location [], Array.concat [diagnostics; autoGenErrs]

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
let private VerifyBinding tryBuildDeclaration (qsSym, (rhsType, rhsRange)) warnOnDiscard = 
    let symbolTuple (items : _ list) = 
        if items.Length = 0 then ArgumentException "symbol tuple has to contain at least one item" |> raise
        elif items.Length = 1 then items.[0]
        else VariableNameTuple (items.ToImmutableArray())

    let withUnknownExprErr (s, d, errs) = s, d, Array.concat [errs; [| rhsRange |> QsCompilerDiagnostic.Error (ErrorCode.ExpressionOfUnknownType, []) |]]
    let rec GetBindings (sym : QsSymbol, exType : ResolvedType) = 
        match sym.Symbol with 
        | _ when exType.isMissing -> GetBindings (sym, InvalidType |> ResolvedType.New) |> withUnknownExprErr
        | QsSymbolKind.InvalidSymbol -> InvalidItem, [||], [||]
        | QsSymbolKind.MissingSymbol when warnOnDiscard -> DiscardedItem, [||], [| sym.RangeOrDefault |> QsCompilerDiagnostic.Warning (WarningCode.DiscardingItemInAssignment, []) |] 
        | QsSymbolKind.MissingSymbol -> DiscardedItem, [||], [||]
        | QsSymbolKind.OmittedSymbols 
        | QsSymbolKind.QualifiedSymbol _ -> InvalidItem, [||], [| sym.RangeOrDefault |> QsCompilerDiagnostic.Error (ErrorCode.ExpectingUnqualifiedSymbol, []) |] 
        | QsSymbolKind.Symbol name -> tryBuildDeclaration (name, sym.RangeOrDefault) (exType, rhsRange) |> function
            | Some decl, errs -> VariableName name, [|decl|], errs
            | None, errs -> VariableName name, [||], errs
        | QsSymbolKind.SymbolTuple syms -> exType |> function 
            | Tuple ts when syms.Length = ts.Length ->
                let (symItems, declarations, errs) = Seq.zip syms ts |> Seq.map GetBindings |> Seq.toList |> List.unzip3
                symbolTuple symItems, Array.concat declarations, Array.concat errs
            | _ ->
                let (symItems, declarations, errs) = syms |> Seq.map (fun s -> (s, InvalidType |> ResolvedType.New) |> GetBindings) |> Seq.toList |> List.unzip3
                let errs = if exType.isInvalid then errs else [| sym.RangeOrDefault |> QsCompilerDiagnostic.Error (ErrorCode.SymbolTupleShapeMismatch, [exType |> toString]) |] :: errs 
                symbolTuple symItems, Array.concat declarations, Array.concat errs
    GetBindings (qsSym, rhsType)

/// Resolves and verifies the given Q# expressions given a symbol tracker containing all currently declared symbols.
/// Verifies that the type of the resolved right hand side expression is indeed compatible with the resolved type of the left hand side expression.
/// Uses VerifyAssignment to verify that this is indeed the case. 
/// Builds the corresponding Q# set-statement at the given location, 
/// and returns it along with an array of diagnostics generated during resolution and verification. 
/// Errors due to the statement not satisfying the necessary conditions for the required auto-generation of specializations 
/// (specified by the given SymbolTracker) are also included in the returned diagnostics. 
let NewValueUpdate comments (location : QsLocation) symbols (lhs : QsExpression, rhs : QsExpression) =
    let verifiedLhs, lhsErrs = Verify symbols lhs
    let VerifyIsCorrectType = VerifyAssignment verifiedLhs.ResolvedType symbols.Parent ErrorCode.TypeMismatchInValueUpdate
    let verifiedRhs, _, rhsErrs = VerifyWith VerifyIsCorrectType symbols rhs
    let localQdep = verifiedRhs.InferredInformation.HasLocalQuantumDependency 
    let rec VerifyMutability = function
        | Tuple (exs : TypedExpression list) -> exs |> Seq.collect VerifyMutability |> Seq.toArray
        | Item (ex : TypedExpression) when ex.InferredInformation.IsMutable -> ex.Expression |> function
            | Identifier (LocalVariable id, Null) -> symbols.UpdateQuantumDependency id localQdep; [||]
            | _ -> [||]
        | Item (ex : TypedExpression) -> 
            let range = ex.Range.ValueOr QsCompilerDiagnostic.DefaultRange 
            [| range |> QsCompilerDiagnostic.Error (ErrorCode.UpdateOfImmutableIdentifier, []) |]
        | _ -> [||] // both missing and invalid expressions on the lhs are fine
    let refErrs = verifiedLhs |> VerifyMutability
    let autoGenErrs = symbols |> onAutoInvertGenerateError ((ErrorCode.ValueUpdateWithinAutoInversion, []), location.Range) 
    QsValueUpdate.New(verifiedLhs, verifiedRhs) |> QsValueUpdate |> asStatement comments location [], Array.concat [lhsErrs; refErrs; rhsErrs; autoGenErrs]

/// Adds a variable declaration with the given name, quantum dependency, and type at the given location to the given symbol tracker.
/// Generates the corresponding error(s) if a symbol with the same name is already visible on that scope and/or the given name is not a valid variable name. 
/// Generates an InvalidUseOfTypeParameterizedObject error if the given variable type contains external type parameters 
/// (i.e. type parameters that do no belong to the parent callable associated with the given symbol tracker).
/// Returns the pushed declaration as Some, if the declaration was successfully added to given symbol tracker, and None otherwise. 
let private TryAddDeclaration isMutable (symbols : SymbolTracker<_>) (name : NonNullable<string>, location, localQdep) (rhsType : ResolvedType, rhsRange) =
    let typeParametrizedRhs = rhsType.isTypeParametrized symbols.Parent
    let t, tpErr = 
        if not typeParametrizedRhs then rhsType, [||]
        else InvalidType |> ResolvedType.New, [| rhsRange |> QsCompilerDiagnostic.Error (ErrorCode.InvalidUseOfTypeParameterizedObject, []) |]
    let decl = LocalVariableDeclaration<_>.New isMutable (location, name, t, localQdep)
    let added, errs = symbols.TryAddVariableDeclartion decl
    (if added then Some decl else None), errs |> Array.append tpErr

/// Given a Q# symbol, as well as the expression on the right hand side that is assigned to it, 
/// resolves and verifies the assignment using VerifyBinding and the given symbol tracker that contains all currently declared symbols.
/// Pushes all determined variable declarations into the current scope of the symbol tracker, 
/// generating the corresponding error(s) if a symbol with the same name is already visible on that scope and/or the symbol is not a valid variable name. 
/// Builds the corresponding Q# let- or mutable-statement (depending on the given kind) at the given location, 
/// and returns it along with an array of all generated diagnostics. 
let private NewBinding kind comments (location : QsLocation) (symbols : SymbolTracker<_>) (qsSym : QsSymbol, qsExpr : QsExpression) = 
    let rhs, rhsErrs = Verify symbols qsExpr
    let symTuple, varDeclarations, errs = 
        let isMutable = kind |> function | MutableBinding -> true | ImmutableBinding -> false                
        let localQdep = rhs.InferredInformation.HasLocalQuantumDependency
        let addDeclaration (name, range) = TryAddDeclaration isMutable symbols (name, (Value location.Offset, range), localQdep)
        VerifyBinding addDeclaration (qsSym, (rhs.ResolvedType, qsExpr.RangeOrDefault)) false
    let autoGenErrs = (rhs, qsExpr.RangeOrDefault) |> onAutoInvertCheckQuantumDependency symbols
    let binding = QsBinding<TypedExpression>.New kind (symTuple, rhs) |> QsVariableDeclaration
    binding |> asStatement comments location varDeclarations, Array.concat [rhsErrs; errs; autoGenErrs]

/// Resolves, verifies and builds the Q# let-statement at the given location binding the given expression to the given symbol.
/// Adds the corresponding local variable declarations to the given symbol tracker. 
/// Returns the built statement along with an array containing all diagnostics generated in the process. 
let NewImmutableBinding comments location symbols (qsSym, qsExpr) = 
    NewBinding QsBindingKind.ImmutableBinding comments location symbols (qsSym, qsExpr)

/// Resolves, verifies and builds the Q# mutable-statement at the given location binding the given expression to the given symbol.
/// Adds the corresponding local variable declarations to the given symbol tracker. 
/// Returns the built statement along with an array containing all diagnostics generated in the process. 
let NewMutableBinding comments location symbols (qsSym, qsExpr) = 
    NewBinding QsBindingKind.MutableBinding comments location symbols (qsSym, qsExpr)


type BlockStatement<'T> = delegate of QsScope -> 'T

/// Given the location of the statement header as well as a symbol tracker containing all currently declared symbols, 
/// builds the Q# for-statement at the given location, that iterates over the given expression, with the given symbol as loop variable(s). 
/// In order to do so, verifies the expression is indeed iterable, 
/// and verifies that the shape of the symbol tuple is compatible with the type of the iteration item using VerifyBinding. 
/// Adds the corresponding variable declarations to the given symbol tracker. 
/// Returns an array with all generated diagnostics, 
/// as well as a delegate that given a Q# scope returns the built for-statement with the given scope as the body. 
/// NOTE: the declared loop variables are *not* visible after the statements ends, hence they are *not* attaches as local declarations to the statement!
let NewForStatement comments (location : QsLocation) (symbols : SymbolTracker<_>) (qsSym : QsSymbol, qsExpr : QsExpression) = 
    let iterExpr, itemT, iterErrs = VerifyWith VerifyIsIterable symbols qsExpr
    let symTuple, _, varErrs = 
        let localQdep = iterExpr.InferredInformation.HasLocalQuantumDependency
        let addDeclaration (name, range) = TryAddDeclaration false symbols (name, (Value location.Offset, range), localQdep)
        VerifyBinding addDeclaration (qsSym, (itemT, qsExpr.RangeOrDefault)) false
    let autoGenErrs = (iterExpr, qsExpr.RangeOrDefault) |> onAutoInvertCheckQuantumDependency symbols
    let forLoop body = QsForStatement.New ((symTuple, itemT), iterExpr, body) |> QsForStatement
    new BlockStatement<_>(forLoop >> asStatement comments location []), Array.concat [iterErrs; varErrs; autoGenErrs] 

/// Given the location of the statement header as well as a symbol tracker containing all currently declared symbols, 
/// builds the Q# while-statement at the given location with the given expression as condition. 
/// Verifies the expression is indeed of type Bool, and returns an array with all generated diagnostics, 
/// as well as a delegate that given a Q# scope returns the built while-statement with the given scope as the body. 
let NewWhileStatement comments (location : QsLocation) (symbols : SymbolTracker<_>) (qsExpr : QsExpression) =  
    let cond, _, errs = VerifyWith VerifyIsBoolean symbols qsExpr
    let whileLoop body = QsWhileStatement.New (cond, body) |> QsWhileStatement
    new BlockStatement<_>(whileLoop >> asStatement comments location []), errs

/// Resolves and verifies the given Q# expression given a symbol tracker containing all currently declared symbols.
/// Verifies that the type of the resolved expression is indeed of kind Bool.
/// Returns an array of all diagnostics generated during resolution and verification,
/// as well as a delegate that given a positioned block of Q# statements returns the corresponding conditional block. 
let NewConditionalBlock comments location (symbols : SymbolTracker<_>) (qsExpr : QsExpression) = 
    let condition, _, errs = VerifyWith VerifyIsBoolean symbols qsExpr
    let autoGenErrs = (condition, qsExpr.RangeOrDefault) |> onAutoInvertCheckQuantumDependency symbols
    let block body = condition, QsPositionedBlock.New comments (Value location) body
    new BlockStatement<_>(block), Array.concat [errs; autoGenErrs]

/// Given a conditional block for the if-clause of a Q# if-statement, a sequence of conditional blocks for the elif-clauses, 
/// as well as optionally a positioned block of Q# statements and its location for the else-clause, builds and returns the complete if-statement.  
/// Throws an ArgumentException if the given if-block contains no location information. 
let NewIfStatement (ifBlock : TypedExpression * QsPositionedBlock, elifBlocks, elseBlock : QsNullable<QsPositionedBlock>) = 
    let location = (snd ifBlock).Location |> function 
        | Null -> ArgumentException "no location is set for the given if-block" |> raise
        | Value loc -> loc 
    let condBlocks = seq { yield ifBlock; yield! elifBlocks; }
    QsConditionalStatement.New (condBlocks, elseBlock) |> QsConditionalStatement |> asStatement QsComments.Empty location []

/// Given a positioned block of Q# statements for the repeat-block of a Q# RUS-statement, a typed expression containing the success condition, 
/// as well as a positioned block of Q# statements for the fixup-block, builds the complete RUS-statement at the given location and returns it.
/// Returns an array with diagnostics generated if the statement does not satisfy the necessary conditions 
/// for the required auto-generation of specializations (specified by the given SymbolTracker). 
let NewRepeatStatement (symbols : SymbolTracker<_>) (repeatBlock : QsPositionedBlock, successCondition, fixupBlock) = 
    let location = repeatBlock.Location |> function 
        | Null -> ArgumentException "no location is set for the given repeat-block" |> raise
        | Value loc -> loc 
    let autoGenErrs = symbols |> onAutoInvertGenerateError ((ErrorCode.RUSloopWithinAutoInversion, []), location.Range) 
    QsRepeatStatement.New (repeatBlock, successCondition, fixupBlock) |> QsRepeatStatement |> asStatement QsComments.Empty location [], autoGenErrs

/// Given a positioned block of Q# statements specifying the transformation to conjugate (inner transformation V), 
/// as well as a positioned block of Q# statements specifying the transformation to conjugate it with (outer transformation U), 
/// builds and returns the corresponding conjugation statement representing the patter U*VU where the order of application is right to left and U* is the adjoint of U. 
/// Returns an array with diagnostics and the corresponding statement offset for all invalid variable reassignments in the apply-block. 
/// Throws an ArgumentException if the given block specifying the outer transformation contains no location information. 
let NewConjugation (outer : QsPositionedBlock, inner : QsPositionedBlock) = 
    let location = outer.Location |> function
        | Null -> ArgumentException "no location is set for the given within-block defining the conjugating transformation" |> raise
        | Value loc -> loc
    let usedInOuter = 
        let accumulate = new AccumulateIdentifiers()
        accumulate.Transform outer.Body |> ignore
        accumulate.UsedLocalVariables
    let updatedInInner = 
        let accumulate = new AccumulateIdentifiers()
        accumulate.Transform inner.Body |> ignore
        accumulate.ReassignedVariables
    let updateErrs = 
        updatedInInner |> Seq.filter (fun updated -> usedInOuter.Contains updated.Key) |> Seq.collect id
        |> Seq.map (fun loc -> (loc.Offset, loc.Range |> QsCompilerDiagnostic.Error (ErrorCode.InvalidReassignmentInApplyBlock, []))) |> Seq.toArray
    QsConjugation.New (outer, inner) |> QsConjugation |> asStatement QsComments.Empty location [], updateErrs

/// Given the location of the statement header as well as a symbol tracker containing all currently declared symbols, 
/// builds the Q# using- or borrowing-statement (depending on the given kind) at the given location
/// that initializes the qubits in the given initializer expression and assigns them to the given symbol. 
/// In order to do so, resolves and verifies the initializer expression, as well as its binding to the given symbol using VerifyBinding. 
/// Adds the corresponding variable declarations to the given symbol tracker. 
/// Returns an array with all generated diagnostics, 
/// as well as a delegate that given a Q# scope returns the built using- or borrowing-statement with the given scope as the body. 
/// NOTE: the declared variables are *not* visible after the statements ends, hence they are *not* attaches as local declarations to the statement!
let private NewBindingScope kind comments (location : QsLocation) (symbols : SymbolTracker<_>) (qsSym : QsSymbol, qsInit : QsInitializer) = 
    let rec VerifyInitializer (init : QsInitializer) = init.Initializer |> function
        | SingleQubitAllocation -> SingleQubitAllocation |> ResolvedInitializer.New, [||]
        | QubitRegisterAllocation nr -> 
            let verifiedNr, _, err = VerifyWith VerifyIsInteger symbols nr
            let autoGenErrs = (verifiedNr, nr.RangeOrDefault) |> onAutoInvertCheckQuantumDependency symbols
            QubitRegisterAllocation verifiedNr |> ResolvedInitializer.New, Array.concat [err; autoGenErrs]
        | QubitTupleAllocation is -> 
            let items, errs = is |> Seq.map VerifyInitializer |> Seq.toList |> List.unzip
            QubitTupleAllocation (items.ToImmutableArray()) |> ResolvedInitializer.New, Array.concat errs
        | InvalidInitializer -> InvalidInitializer |> ResolvedInitializer.New, [||]

    let initializer, initErrs = VerifyInitializer qsInit
    let symTuple, _, varErrs = 
        let rhsRange = qsInit.Range.ValueOr QsCompilerDiagnostic.DefaultRange
        let addDeclaration (name, range) = TryAddDeclaration false symbols (name, (Value location.Offset, range), false)
        VerifyBinding addDeclaration (qsSym, (initializer.Type, rhsRange)) true
    let bindingScope body = QsQubitScope.New kind ((symTuple, initializer), body) |> QsQubitScope
    new BlockStatement<_>(bindingScope >> asStatement comments location []), Array.concat [initErrs; varErrs]

/// Resolves, verifies and builds the Q# using-statement at the given location binding the given initializer to the given symbol.
/// Adds the corresponding local variable declarations to the given symbol tracker. 
/// Returns the built statement along with an array containing all diagnostics generated in the process. 
let NewAllocateScope comments location symbols (qsSym, qsInit) = NewBindingScope QsQubitScopeKind.Allocate comments location symbols (qsSym, qsInit)

/// Resolves, verifies and builds the Q# borrowing-statement at the given location binding the given initializer to the given symbol.
/// Adds the corresponding local variable declarations to the given symbol tracker. 
/// Returns the built statement along with an array containing all diagnostics generated in the process. 
let NewBorrowScope comments location symbols (qsSym, qsInit)   = NewBindingScope QsQubitScopeKind.Borrow comments location symbols (qsSym, qsInit)

