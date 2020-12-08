// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// This module mustn't do any whitespace management - that is entirely up to the QsParsingPrimitives module!
/// The same holds for actually building the code fragments - use the routines defined in the QsSyntaxBuilding module instead.
/// Also: any strings in this module are only related to error messaging - for the parsing itself, all necessary keywords are to be defined in the keywords module!
module Microsoft.Quantum.QsCompiler.TextProcessing.CodeFragments

open System.Collections.Immutable
open System.Linq
open FParsec
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.TextProcessing.ExpressionParsing
open Microsoft.Quantum.QsCompiler.TextProcessing.Keywords
open Microsoft.Quantum.QsCompiler.TextProcessing.ParsingPrimitives
open Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxBuilder
open Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.TextProcessing.TypeParsing


// utils used for fragment construction

/// parses a (unqualified) symbol-like expression used as local identifier, raising a suitable error for an invalid symbol name
let private localIdentifier = symbolLike ErrorCode.InvalidIdentifierName // i.e. not a qualified name

/// parses a Q# type annotation (colon followed by a Q# type) using expectedQsType to generate suitable errors if the parsing fails
let private typeAnnotation continuation = colon >>. expectedQsType continuation

/// returns a QsSymbol representing an invalid symbol (i.e. syntax error on parsing)
let private invalidSymbol = (InvalidSymbol, Null) |> QsSymbol.New

/// returns a QsTupleItem representing an invalid argument declaration (i.e. syntax error on parsing)
let private invalidArgTupleItem = (invalidSymbol, invalidType) |> QsTupleItem

/// returns a QsInitializer representing an invalid initializer expression (i.e. syntax error on parsing)
let private invalidInitializer = (InvalidInitializer, Null) |> QsInitializer.New

/// returns a QsFunctorGenerator representing an invalid functor generator (i.e. syntax error on parsing)
let private unknownGenerator =
    (FunctorGenerationDirective InvalidGenerator, Null) |> QsSpecializationGenerator.New

/// Given an array of QsSymbols and a tuple with start and end position, builds a Q# SymbolTuple as QsSymbol.
let private buildSymbolTuple (items, range: Range) =
    (SymbolTuple items, range) |> QsSymbol.New

/// Given a continuation (parser), attempts to parse an unqualified QsSymbol
/// using localItentifier to generate suitable errors for invalid symbol names,
/// and returns the parsed symbol, or a QsSymbol representing an invalid symbol (parsing failure) if the parsing fails.
/// On failure, either raises an MissingIdentifierDeclaration if the given continuation succeeds at the current position,
/// or raises an InvalidIdentifierDeclaration and advances until the given continuation succeeds otherwise.
/// Does not apply the given continuation.
let private expectedIdentifierDeclaration continuation =
    expected
        localIdentifier
        ErrorCode.InvalidIdentifierDeclaration
        ErrorCode.MissingIdentifierDeclaration
        invalidSymbol
        continuation

/// Uses multiSegmentSymbol to parse the name of a namespace,
/// concatenates all path segments and the symbol with a dot, and returns a the concatenated string as Some.
/// Returns None if the path contains segments that are None (i.e. invalid).
/// Generates a suitable diagnostic if the namespace name ends in an underscore.
let internal namespaceName = // internal for testing purposes
    let asNamespaceName ((path, sym: string option), range: Range) =
        let names = path @ [ sym ]
        let namespaceStr = names |> List.choose id |> String.concat "."

        if names |> List.contains None then
            (None, range) |> preturn
        elif sym.Value.EndsWith "_" then
            buildError (preturn range) ErrorCode.InvalidUseOfUnderscorePattern >>% (None, range)
        else
            (Some namespaceStr, range) |> preturn

    multiSegmentSymbol ErrorCode.InvalidPathSegment >>= asNamespaceName

/// Given a continuation (parser), attempts to parse a qualified QsSymbol
/// using multiSegmentSymbol to generate suitable errors for invalid symbol and/or path names,
/// and returns the parsed symbol, or a QsSymbol representing an invalid symbol (parsing failure) if the parsing fails.
/// On failure, either raises an MissingQualifiedSymbol if the given continuation succeeds at the current position,
/// or raises an InvalidQualifiedSymbol and advances until the given continuation succeeds otherwise.
/// Does not apply the given continuation.
let private expectedNamespaceName continuation =
    let namespaceName =
        namespaceName
        |>> function
        | None, _ -> (InvalidSymbol, Null) |> QsSymbol.New
        | Some name, range -> (Symbol name, range) |> QsSymbol.New

    expected namespaceName ErrorCode.InvalidQualifiedSymbol ErrorCode.MissingQualifiedSymbol invalidSymbol continuation

/// Parses the condition e.g. for if, elif and until clauses.
/// Uses optTupleBrackets to raise the corresponding missing bracket errors if the condition is not within tuple brackets.
/// Uses expectedExpr to raise suitable errors for a missing or invalid expression.
let private expectedCondition continuation =
    optTupleBrackets (expectedExpr (rTuple <|> (continuation >>% ""))) |>> fst

/// Parses for a binding to a symbol tuple or a single symbol when
/// given a parser for the right hand side of the binding as well as the connector parser that connects the symbols with the right hand side.
/// Raises the given connectorErr without consuming input if the connector parser fails after the symbol parsing, and proceeds to parse to given right hand side.
/// Allows for assignment to discarded symbols (underscores).
/// Uses buildTupleItem to raise suitable errors. In particular, raises a MissingSymbolTupleDeclaration error for a missing symbol,
/// and an InvalidSymbolTupleDeclaration error for an invalid symbol.
/// Checks for an array of symbols within the symbol tuple, raising an InvalidAssignmentToExpression error for such symbol arrays.
let private symbolBinding connector connectorErr expectedRhs = // used for mutable and immutable bindings, and allocationScope headers (using and borrowing)
    let validContinuation = connector >>% () <|> isTupleContinuation
    let validSymbol = (discardedSymbol <|> localIdentifier) .>>? followedBy validContinuation // discarded needs to be first

    let invalid =
        let symbolArray =
            arrayBrackets (sepBy1 validSymbol (comma .>>? followedBy validSymbol) .>> opt comma) |>> snd // let's only specifically detect this particular scenario

        buildError (symbolArray .>>? followedBy validContinuation) ErrorCode.InvalidAssignmentToExpression
        >>% invalidSymbol

    let symbolTuple =
        buildTupleItem
            (validSymbol <|> invalid)
            buildSymbolTuple
            ErrorCode.InvalidSymbolTupleDeclaration
            ErrorCode.MissingSymbolTupleDeclaration
            invalidSymbol
            connector

    let expectedConnector = expected (connector >>% ()) connectorErr connectorErr () (preturn ())

    symbolTuple .>> expectedConnector .>>. expectedRhs

/// Parses the assignment of an initializer tuple or single initializer to a symbol tuple or a single symbol.
/// Uses symbolBinding to generate suitable errors for errors on the left hand side of the assignment.
/// Raises a MissingInitializerExpression error or an InvalidInitializerError for missing or invalid initializers.
/// Uses optTupleBrackets to raise the corresponding missing bracket errors if the entire assignment is not within tuple brackets.
let private allocationScope =
    let combineRangeAndBuild (r1, (kind, r2)) =
        // *needs* to be invalid if the combined range is Null!
        match r2 with
        | Value r2 -> { Initializer = kind; Range = Range.Span r1 r2 |> Value }
        | Null -> { Initializer = InvalidInitializer; Range = Null }

    let qRegisterAlloc =
        qsQubit.parse
        .>>. (arrayBrackets (expectedExpr eof) |>> fun (ex, r) -> QubitRegisterAllocation ex, Value r)
        |>> combineRangeAndBuild

    let qAlloc =
        qsQubit.parse .>>. unitValue
        |>> fun (r1, ex) -> (r1, (SingleQubitAllocation, ex.Range))
        |>> combineRangeAndBuild

    let validInitializer = attempt qAlloc <|> attempt qRegisterAlloc

    let buildInitializerTuple (items, range: Range) =
        (QubitTupleAllocation items, range) |> QsInitializer.New

    let initializerTuple =
        buildTupleItem
            validInitializer
            buildInitializerTuple
            ErrorCode.InvalidInitializerExpression
            ErrorCode.MissingInitializerExpression
            invalidInitializer
            isTupleContinuation

    optTupleBrackets (initializerTuple |> symbolBinding equal ErrorCode.ExpectingAssignment) |>> fst

/// Parses keywords that modify the visibility or behavior of a declaration.
let private modifiers =
    let accessModifier = (qsInternal.parse >>% Internal) <|>% DefaultAccess
    accessModifier |>> fun access -> { Access = access }

/// Parses a Q# operation or function signature.
/// Expects type annotations for each symbol in the argument tuple, and raises Missing- or InvalidTypeAnnotation errors otherwise.
/// Raises Missing- or InvalidArumentDeclaration error for entirely missing or invalid arguments,
/// returning an InvalidArgument at the corresponding place in the argument tuple instead.
/// Uses commaSep1 to generate suitable errors for missing or invalid type parameters in the type parameter list,
/// and uses typeParameterNameLike to generate suitable errors for invalid type parameter names (e.g. missing preceding tick).
/// Expects a return type annotation, generating a Missing- or InvalidReturnTypeAnnotation error otherwise.
/// Uses expectedIdentifierDeclaration to generate suitable errors for an invalid or missing callable name.
let private signature =
    let genericParamList =
        let genericParam =
            let invalidName =
                symbolNameLike ErrorCode.InvalidTypeParameterName .>> opt (pchar '\'') |> term |>> snd

            let invalid = buildError invalidName ErrorCode.InvalidTypeParameterName >>% None

            term (typeParameterNameLike <|> invalid)
            |>> function
            | Some sym, range -> (Symbol sym, range) |> QsSymbol.New
            | None, _ -> invalidSymbol

        let validList =
            let typeParams =
                commaSep1
                    genericParam
                    ErrorCode.InvalidTypeParameterDeclaration
                    ErrorCode.MissingTypeParameterDeclaration
                    invalidSymbol
                    eof

            let noTypeParams = angleBrackets emptySpace >>% () <|> notFollowedBy lAngle // allow (optional) empty angle brackets

            noTypeParams >>% ImmutableArray.Empty <|> (angleBrackets typeParams |>> fst)

        let invalidList =
            let buildErr = QsCompilerDiagnostic.NewError ErrorCode.InvalidTypeParameterList

            getRange (angleBrackets (advanceTo eof)) |>> snd |>> buildErr >>= pushDiagnostic
            >>% ImmutableArray.Empty

        attempt validList <|> invalidList

    let argumentTuple = // not unified with the argument tuple for user defined types, since the error handling needs to be different
        let invalidIdWithAnnotation =
            expectedIdentifierDeclaration (colon >>% () <|> isTupleContinuation)
            .>>. typeAnnotation isTupleContinuation

        let expectedTypeAnnotation =
            expected
                (typeAnnotation isTupleContinuation)
                ErrorCode.InvalidTypeAnnotation
                ErrorCode.MissingTypeAnnotation
                invalidType
                isTupleContinuation

        let argTupleItem =
            attempt (localIdentifier .>>. expectedTypeAnnotation) <|> attempt invalidIdWithAnnotation
            |>> QsTupleItem

        let argTuple =
            buildTuple
                argTupleItem
                (fst >> QsTuple)
                ErrorCode.InvalidArgumentDeclaration
                ErrorCode.MissingArgumentDeclaration
                invalidArgTupleItem

        unitValue >>% QsTuple ImmutableArray.Empty <|> argTuple

    let symbolDeclaration = expectedIdentifierDeclaration (lAngle <|> lTuple)

    let returnTypeAnnotation =
        expected
            (typeAnnotation eof)
            ErrorCode.InvalidReturnTypeAnnotation
            ErrorCode.MissingReturnTypeAnnotation
            invalidType
            eof

    let characteristicsAnnotation =
        opt (qsCharacteristics.parse >>. expectedCharacteristics eof)
        |>> Option.defaultValue ((EmptySet, Null) |> Characteristics.New)

    let signature =
        genericParamList .>>. (argumentTuple .>>. returnTypeAnnotation) .>>. characteristicsAnnotation
        |>> CallableSignature.New

    symbolDeclaration .>>. signature

/// Parses a Q# functor generator directive.
/// For a user defined implementation, expects a tuple argument of items that can either be a localIdentifier or omittedSymbols.
/// Expects tuple brackets even if the argument consists of a single tuple item.
/// If the symbol tuple is empty or missing, returns a user defined implementation with an empty symbol tuple as argument without raising an error.
/// Errors for the cases where the parsed symbol does not match the one expected for the specialization need to be raised on type or context checking.
/// Uses buildTuple to generate Invalid- or MissingSymbolTupleDeclaration errors for a user defined implementation requiring an argument.
/// Raises an UnknownFunctorGenerator error if the end of the input stream or the next fragment header is only preceded by
/// a single symbol-like expression that is not inside tuple brackets and does not correspond to a predefined generator.
let private functorGenDirective = // parsing all possible directives for all functors, and complain on type or context checking
    let EndOfFragment = qsFragmentHeader >>% () <|> eof

    let functorGenArgs =
        let noArgOrUnitArg =
            ((followedByCode EndOfFragment) <|> (tupleBrackets emptySpace |>> snd))
            |>> fun r -> buildSymbolTuple (ImmutableArray.Empty, r)

        let tupledArgs =
            buildTuple
                (omittedSymbols <|> localIdentifier)
                buildSymbolTuple
                ErrorCode.InvalidSymbolTupleDeclaration
                ErrorCode.MissingSymbolTupleDeclaration
                invalidSymbol

        (noArgOrUnitArg <|> tupledArgs) .>> followedBy EndOfFragment // *always* require outer brackets, like for calls

    let unknownGenerator =
        let generatorName =
            symbolNameLike ErrorCode.UnknownFunctorGenerator |> term |>> snd .>>? followedBy EndOfFragment

        buildError generatorName ErrorCode.UnknownFunctorGenerator >>% unknownGenerator

    let userDefinedImplementation =
        functorGenArgs
        |>> fun arg -> (UserDefinedImplementation arg, arg.Range) |> QsSpecializationGenerator.New

    let buildGenerator kind (range: Range) =
        (kind, range) |> QsSpecializationGenerator.New

    choice [ attempt intrinsicFunctorGenDirective.parse |>> buildGenerator Intrinsic
             attempt autoFunctorGenDirective.parse |>> buildGenerator AutoGenerated
             attempt selfFunctorGenDirective.parse |>> buildGenerator (FunctorGenerationDirective SelfInverse)
             attempt invertFunctorGenDirective.parse |>> buildGenerator (FunctorGenerationDirective Invert)
             attempt distributeFunctorGenDirective.parse
             |>> buildGenerator (FunctorGenerationDirective Distribute)
             attempt userDefinedImplementation
             attempt unknownGenerator ]


// Q# fragments

// namespace and attribute parsing

/// Uses buildFragment to parse a Q# OpenDirective as QsFragment.
let private openDirective =
    let invalid = OpenDirective(invalidSymbol, Null)

    let nsNameAndAlias =
        let aliasOption = (importedAs.parse >>. expectedNamespaceName eof |>> Value) <|>% Null
        expectedNamespaceName importedAs.parse .>>. aliasOption

    buildFragment importDirectiveHeader.parse nsNameAndAlias invalid (fun _ -> OpenDirective) eof

/// Uses buildFragment to parse a Q# NamespaceDeclaration as QsFragment.
let private namespaceDeclaration =
    let invalid = NamespaceDeclaration invalidSymbol
    buildFragment namespaceDeclHeader.parse (expectedNamespaceName eof) invalid (fun _ -> NamespaceDeclaration) eof

/// Uses buildFragment to parse a Q# DeclarationAttribute as QsFragment.
let private attributeAnnotation =
    let invalid = DeclarationAttribute(invalidSymbol, unknownExpr)
    let attributeId = multiSegmentSymbol ErrorCode.InvalidIdentifierName |>> asQualifiedSymbol

    let expectedArgs =
        let invalidErr, missingErr = ErrorCode.InvalidAttributeArgument, ErrorCode.MissingAttributeArgument
        let args = unitValue <|> buildTuple expr buildTupleExpr invalidErr missingErr unknownExpr
        expected args invalidErr missingErr unknownExpr attributeIntro

    let expectedAttribute =
        let invalidAttribute = (invalidSymbol, unknownExpr)
        let invalidErr, missingErr = ErrorCode.InvalidAttributeIdentifier, ErrorCode.MissingAttributeIdentifier
        expected (attributeId .>>. expectedArgs) invalidErr missingErr invalidAttribute attributeIntro

    buildFragment attributeIntro expectedAttribute invalid (fun _ -> DeclarationAttribute) attributeIntro


// operation and function parsing

/// Uses buildFragment to parse a Q# BodyDeclaration as QsFragment.
let private bodyDeclaration =
    let invalid = BodyDeclaration unknownGenerator
    buildFragment bodyDeclHeader.parse functorGenDirective invalid (fun _ -> BodyDeclaration) eof

/// Uses buildFragment to parse a Q# AdjointDeclaration as QsFragment.
let private adjointDeclaration =
    let invalid = AdjointDeclaration unknownGenerator
    buildFragment adjDeclHeader.parse functorGenDirective invalid (fun _ -> AdjointDeclaration) eof

/// Uses buildFragment to parse a Q# ControlledDeclaration as QsFragment.
let private controlledDeclaration =
    let invalid = ControlledDeclaration unknownGenerator
    buildFragment ctrlDeclHeader.parse functorGenDirective invalid (fun _ -> ControlledDeclaration) eof

/// Uses buildFragment to parse a Q# ControlledAdjointDeclaration as QsFragment.
let private controlledAdjointDeclaration =
    let invalid = ControlledAdjointDeclaration unknownGenerator

    buildFragment (attempt ctrlAdjDeclHeader.parse) functorGenDirective invalid (fun _ -> ControlledAdjointDeclaration)
        eof

/// Uses buildFragment to parse a Q# OperationDeclaration as QsFragment.
let private operationDeclaration =
    let invalid =
        OperationDeclaration({ Access = DefaultAccess }, invalidSymbol, CallableSignature.Invalid)

    buildFragment (modifiers .>> opDeclHeader.parse |> attempt) signature invalid (fun mods (symbol, signature) ->
        OperationDeclaration(mods, symbol, signature)) eof

/// Uses buildFragment to parse a Q# FunctionDeclaration as QsFragment.
let private functionDeclaration =
    let invalid =
        FunctionDeclaration({ Access = DefaultAccess }, invalidSymbol, CallableSignature.Invalid)

    buildFragment (modifiers .>> fctDeclHeader.parse |> attempt) signature invalid (fun mods (symbol, signature) ->
        FunctionDeclaration(mods, symbol, signature)) eof

/// Uses buildFragment to parse a Q# TypeDefinition as QsFragment.
let private udtDeclaration =
    let invalid = TypeDefinition({ Access = DefaultAccess }, invalidSymbol, invalidArgTupleItem)

    let udtTuple = // not unified with the argument tuple for callable declarations, since the error handling needs to be different
        let asAnonymousItem t =
            QsTupleItem((MissingSymbol, Null) |> QsSymbol.New, t)

        let namedItem =
            let delimiter = colon >>% () <|> isTupleContinuation

            let expectedItemName =
                expected
                    localIdentifier
                    ErrorCode.InvalidUdtItemNameDeclaration
                    ErrorCode.MissingUdtItemNameDeclaration
                    invalidSymbol
                    delimiter

            expectedItemName .>>. typeAnnotation isTupleContinuation |>> QsTupleItem

        let udtTupleItem =
            let typeTuple = tupleType .>>? followedBy (arrayBrackets emptySpace) // *only* process tuple types as part of arrays!
            let tupleItem = attempt namedItem <|> (typeParser typeTuple |>> asAnonymousItem) // namedItem needs to be first, and we can't be permissive for tuple types!

            buildTupleItem
                tupleItem
                (fst >> QsTuple)
                ErrorCode.InvalidUdtItemDeclaration
                ErrorCode.MissingUdtItemDeclaration
                invalidArgTupleItem
                eof

        let invalidNamedSingle = followedBy namedItem >>. optTupleBrackets namedItem |>> fst

        invalidNamedSingle <|> udtTupleItem // require parenthesis for a single named item

    let declBody = expectedIdentifierDeclaration equal .>> equal .>>. udtTuple

    buildFragment (modifiers .>> typeDeclHeader.parse |> attempt) declBody invalid (fun mods (symbol, underlyingType) ->
        TypeDefinition(mods, symbol, underlyingType)) eof


// statement parsing

/// Uses buildFragment to parse a Q# return-statement as QsFragment.
let private returnStatement =
    let invalid = ReturnStatement unknownExpr
    buildFragment qsReturn.parse (expectedExpr eof) invalid (fun _ -> ReturnStatement) eof

/// Uses buildFragment to parse a Q# fail-statement as QsFragment.
let private failStatement =
    let invalid = FailStatement unknownExpr
    buildFragment qsFail.parse (expectedExpr eof) invalid (fun _ -> FailStatement) eof


/// Uses buildFragment to parse a Q# immutable binding (i.e. let-statement) as QsFragment.
let private letStatement =
    let invalid = ImmutableBinding(invalidSymbol, unknownExpr)

    buildFragment qsImmutableBinding.parse (expectedExpr eof |> symbolBinding equal ErrorCode.ExpectingAssignment)
        invalid (fun _ -> ImmutableBinding) eof

/// Uses buildFragment to parse a Q# mutable binding (i.e. mutable-statement) as QsFragment.
let private mutableStatement =
    let invalid = MutableBinding(invalidSymbol, unknownExpr)

    buildFragment qsMutableBinding.parse (expectedExpr eof |> symbolBinding equal ErrorCode.ExpectingAssignment) invalid (fun _ ->
        MutableBinding) eof


/// Uses buildFragment to parse a Q# value update (i.e. set-statement) as QsFragment.
let private setStatement =
    let applyAndReassignOp =
        let updateAndReassign id =
            let update = pstring qsCopyAndUpdateOp.cont |> term
            let updateExpr = expectedExpr update .>> update .>>. expectedExpr eof

            expected
                updateExpr
                ErrorCode.ExpectingUpdateExpression
                ErrorCode.ExpectingUpdateExpression
                (unknownExpr, unknownExpr)
                eof
            |>> fun (accEx, rhs) -> id, applyTerinary CopyAndUpdate id accEx rhs

        let applyAndReassignExpr buildEx id =
            expectedExpr eof |>> fun ex -> id, applyBinary buildEx () id ex

        // parser that returns function that takes QsExpr and returns a parser
        choice [ pstring qsCopyAndUpdateOp.op >>. equal >>% updateAndReassign
                 pstring qsBANDop.op >>. equal >>% applyAndReassignExpr BAND
                 pstring qsBORop.op >>. equal >>% applyAndReassignExpr BOR
                 pstring qsBXORop.op >>. equal >>% applyAndReassignExpr BXOR
                 pstring qsLSHIFTop.op >>. equal >>% applyAndReassignExpr LSHIFT
                 pstring qsRSHIFTop.op >>. equal >>% applyAndReassignExpr RSHIFT
                 // note: the binary operators need to be parsed first!
                 pstring qsADDop.op >>. equal >>% applyAndReassignExpr ADD
                 pstring qsSUBop.op >>. equal >>% applyAndReassignExpr SUB
                 pstring qsMULop.op >>. equal >>% applyAndReassignExpr MUL
                 pstring qsDIVop.op >>. equal >>% applyAndReassignExpr DIV
                 pstring qsMODop.op >>. equal >>% applyAndReassignExpr MOD
                 pstring qsPOWop.op >>. equal >>% applyAndReassignExpr POW
                 pstring qsANDop.op >>. equal >>% applyAndReassignExpr AND
                 pstring qsORop.op >>. equal >>% applyAndReassignExpr OR ]

    let identifierExpr continuation =
        let asIdentifier (sym: QsSymbol) =
            match sym.Symbol with
            | InvalidSymbol -> preturn unknownExpr
            | Symbol _ -> (Identifier(sym, Null), sym.Range) |> QsExpression.New |> preturn
            | _ -> fail "symbol is not a local identifier"

        let validItem = (missingExpr <|> (localIdentifier >>= asIdentifier)) .>> followedBy continuation // missingExpr needs to be first

        let exprError (ex: QsExpression) =
            ex.Range
            |> function
            | Value range ->
                range
                |> QsCompilerDiagnostic.Error(ErrorCode.InvalidIdentifierExprInUpdate, [])
                |> preturn
                >>= pushDiagnostic
            | Null -> fail "expression without range info"

        let errorOnArrayItem =
            let toRange (start, arrs: (_ * Range) list) =
                Range.Create start ((List.last arrs |> snd).End)

            let arrItem =
                getPosition .>> localIdentifier .>>. many1 (arrayBrackets (expectedExpr eof))
                .>> followedBy continuation

            buildError (arrItem |>> toRange) ErrorCode.UpdateOfArrayItemExpr >>% unknownExpr

        let nonTupleExpr = notFollowedBy continuation >>. expr >>= exprError >>% unknownExpr

        choice [ attempt validItem
                 attempt errorOnArrayItem
                 attempt nonTupleExpr ]

    let applyAndReassign =
        let applyOperator (sym, p) = preturn sym >>= p

        identifierExpr applyAndReassignOp .>>. applyAndReassignOp >>= applyOperator

    let symbolUpdate =
        let continuation = isTupleContinuation >>% "" <|> equal <|> lTuple // need lTuple here to make sure tuples are not parsed as expressions!
        let invalidErr, missingErr = ErrorCode.InvalidIdentifierExprInUpdate, ErrorCode.MissingIdentifierExprInUpdate

        let symbolTuple =
            buildTupleItem (identifierExpr continuation) buildTupleExpr invalidErr missingErr unknownExpr equal

        let expectedEqual =
            expected equal ErrorCode.ExpectingAssignment ErrorCode.ExpectingAssignment "" (preturn ())

        symbolTuple .>> expectedEqual .>>. expectedExpr eof

    let invalid = ValueUpdate(unknownExpr, unknownExpr)
    buildFragment qsValueUpdate.parse (attempt applyAndReassign <|> symbolUpdate) invalid (fun _ -> ValueUpdate) eof


/// Uses buildFragment to parse a Q# if clause as QsFragment.
let private ifClause =
    let invalid = IfClause unknownExpr
    buildFragment qsIf.parse (expectedCondition eof) invalid (fun _ -> IfClause) eof

/// Uses buildFragment to parse a Q# elif clause as QsFragment.
let private elifClause =
    let invalid = ElifClause unknownExpr
    buildFragment qsElif.parse (expectedCondition eof) invalid (fun _ -> ElifClause) eof

/// Uses buildFragment to parse a Q# else clause as QsFragment.
let private elseClause = buildFragment qsElse.parse (preturn "") ElseClause (fun _ _ -> ElseClause) eof


/// Uses buildFragment to parse a Q# for-statement intro (for-statement without the body) as QsFragment.
let private forHeader =
    let invalid = ForLoopIntro(invalidSymbol, unknownExpr)

    let loopVariableBinding =
        expectedExpr rTuple |> symbolBinding qsRangeIter.parse ErrorCode.ExpectingIteratorItemAssignment

    let forBody = optTupleBrackets loopVariableBinding |>> fst
    buildFragment qsFor.parse forBody invalid (fun _ -> ForLoopIntro) eof


/// Uses buildFragment to parse a Q# while-statement intro (while-statement without the body) as QsFragment.
let private whileHeader =
    let invalid = WhileLoopIntro unknownExpr
    let whileBody = optTupleBrackets (expectedExpr isTupleContinuation) |>> fst
    buildFragment qsWhile.parse whileBody invalid (fun _ -> WhileLoopIntro) eof


/// Uses buildFragment to parse a Q# repeat intro as QsFragment.
let private repeatHeader =
    buildFragment qsRepeat.parse (preturn "") RepeatIntro (fun _ _ -> RepeatIntro) eof

/// Uses buildFragment to parse a Q# until success clause as QsFragment.
let private untilSuccess =
    let invalid = UntilSuccess(unknownExpr, false)
    let optionalFixup = qsRUSfixup.parse >>% true <|> preturn false

    buildFragment qsUntil.parse (expectedCondition qsRUSfixup.parse .>>. optionalFixup) invalid (fun _ -> UntilSuccess)
        eof


/// Uses buildFragment to parse a Q# within-block intro as QsFragment.
let private withinHeader =
    buildFragment qsWithin.parse (preturn "") WithinBlockIntro (fun _ _ -> WithinBlockIntro) eof

/// Uses buildFragment to parse a Q# apply block intro as QsFragment.
let private applyHeader =
    buildFragment qsApply.parse (preturn "") ApplyBlockIntro (fun _ _ -> ApplyBlockIntro) eof


/// Uses buildFragment to parse a Q# using block intro as QsFragment.
let private usingHeader =
    let invalid = UsingBlockIntro(invalidSymbol, invalidInitializer)
    buildFragment qsUsing.parse allocationScope invalid (fun _ -> UsingBlockIntro) eof

/// Uses buildFragment to parse a Q# borrowing block intro as QsFragment.
let private borrowingHeader =
    let invalid = BorrowingBlockIntro(invalidSymbol, invalidInitializer)
    buildFragment qsBorrowing.parse allocationScope invalid (fun _ -> BorrowingBlockIntro) eof

/// Always builds an invalid fragment after parsing the given fragment header.
let private buildInvalidFragment header =
    buildFragment header (fail "invalid syntax") InvalidFragment (fun _ _ -> InvalidFragment) attributeIntro

/// Fragment header keywords and their corresponding fragment parsers.
let private fragments =
    [ (qsImmutableBinding, letStatement)
      (qsMutableBinding, mutableStatement)
      (qsValueUpdate, setStatement)
      (qsReturn, returnStatement)
      (qsFail, failStatement)
      (qsIf, ifClause)
      (qsElif, elifClause)
      (qsElse, elseClause)
      (qsFor, forHeader)
      (qsWhile, whileHeader)
      (qsRepeat, repeatHeader)
      (qsUntil, untilSuccess)
      (qsWithin, withinHeader)
      (qsApply, applyHeader)
      (qsUsing, usingHeader)
      (qsBorrowing, borrowingHeader)
      (namespaceDeclHeader, namespaceDeclaration)
      (typeDeclHeader, udtDeclaration)
      (opDeclHeader, operationDeclaration)
      (fctDeclHeader, functionDeclaration)
      (ctrlAdjDeclHeader, controlledAdjointDeclaration) // needs to be before adjointDeclaration and controlledDeclaration!
      (adjDeclHeader, adjointDeclaration)
      (ctrlDeclHeader, controlledDeclaration)
      (bodyDeclHeader, bodyDeclaration)
      (importDirectiveHeader, openDirective)

      // This fragment header does not have its own fragment kind. Instead, it is only parsed as part of another
      // fragment kind. If this header occurs by itself, without the other header it's a part of, an invalid fragment
      // should be created. Since it's at the end of the list, we know that all of the other fragment kinds have been
      // tried first.
      (qsInternal, buildInvalidFragment qsInternal.parse) ]

// Make sure all of the fragment header keywords are listed above.
do let implementedHeaders = (List.map (fun (keyword, _) -> keyword.id) fragments).ToImmutableHashSet()
   let existingHeaders = Keywords.FragmentHeaders.ToImmutableHashSet()

   if (implementedHeaders.SymmetricExcept existingHeaders).Count <> 0 then
       System.NotImplementedException "mismatch between existing Q# fragments and implemented Q# fragments"
       |> raise


/// Uses buildFragment to parse a Q# expression statement as QsFragment.
/// Raises a NonCallExprAsStatement error if the expression is not a call-like expression, and returns UnknownStatement as QsFragment instead.
/// Raising an error for partial applications (that can't possibly be of type unit) is up to the type checking.
let private expressionStatement =
    let invalid = ExpressionStatement unknownExpr

    let valid =
        let errOnNonCallLike (pos, ex: QsExpression) =
            let errRange =
                match ex.Range with
                | Value range -> range
                | Null -> Range.Create pos pos

            errRange
            |> QsCompilerDiagnostic.Error(ErrorCode.NonCallExprAsStatement, [])
            |> preturn
            >>= pushDiagnostic

        let anyExpr = getPosition .>>. expr >>= errOnNonCallLike >>% InvalidFragment // keeping this as unknown fragment so no further type checking is done
        attempt callLikeExpr |>> ExpressionStatement <|> anyExpr

    buildFragment (lookAhead valid) valid invalid (fun _ -> id) eof // let's limit this to call like expressions

// externally called routines

/// Parses a Q# code fragment.
/// Raises a suitable error and returns UnknownStatement as QsFragment if the parsing fails.
let internal codeFragment =
    let validFragment =
        choice (fragments |> List.map snd) <|> attributeAnnotation <|> expressionStatement // the expressionStatement needs to be last

    attempt validFragment <|> buildInvalidFragment (preturn ())
