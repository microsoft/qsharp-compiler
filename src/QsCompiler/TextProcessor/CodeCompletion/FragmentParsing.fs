// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// Parses incomplete fragment text and returns the kinds of tokens that are valid at the end of the fragment.
///
/// This parser is designed to be used with code completion. It assumes that the text it has seen so far is
/// syntactically valid but allows the fragment to end in the middle. For example, not all open brackets need to be
/// closed if it is still valid for the bracket to be closed later.
module Microsoft.Quantum.QsCompiler.TextProcessing.CodeCompletion.FragmentParsing

open FParsec
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.TextProcessing.Keywords
open Microsoft.Quantum.QsCompiler.TextProcessing.ParsingPrimitives
open Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxBuilder
open Microsoft.Quantum.QsCompiler.TextProcessing.CodeCompletion
open Microsoft.Quantum.QsCompiler.TextProcessing.CodeCompletion.ExpressionParsing
open Microsoft.Quantum.QsCompiler.TextProcessing.CodeCompletion.TypeParsing
open Microsoft.Quantum.QsCompiler.TextProcessing.CodeCompletion.ParsingPrimitives

#nowarn "40"


/// Parses a declaration modifier list.
let private modifiers = expectedKeyword qsInternal

/// Parses a callable signature.
let private callableSignature =
    let name = expectedId Declaration (term symbol)
    let typeAnnotation = expected colon ?>> qsType
    let typeParam = expected (pchar '\'') ?>> expectedId Declaration (term symbol)
    let typeParamList = brackets (lAngle, rAngle) (sepByLast typeParam comma)
    let argumentTuple = tuple (name ?>> typeAnnotation)

    name ?>> (typeParamList <|> (optional eot >>% [])) ?>> argumentTuple ?>> typeAnnotation

/// Parses a function declaration.
let private functionDeclaration = optR modifiers @>> expectedKeyword fctDeclHeader ?>> callableSignature

/// Parses an operation declaration.
let private operationDeclaration =
    optR modifiers @>> expectedKeyword opDeclHeader ?>> callableSignature ?>> characteristicsAnnotation

/// Parses a user-defined type declaration.
let private udtDeclaration =
    let name = expectedId Declaration (term symbol)

    let rec udt =
        parse {
            let namedItem = name ?>> expected colon ?>> qsType
            return! qsType <|>@ tuple1 (namedItem <|>@ udt)
        }

    optR modifiers @>> expectedKeyword typeDeclHeader ?>> name ?>> expected equal ?>> udt

/// Parses an open directive.
let private openDirective =
    expectedKeyword importDirectiveHeader
    ?>> expectedQualifiedSymbol Namespace
    ?>> expectedKeyword importedAs
    ?>> expectedQualifiedSymbol Declaration

/// Parses fragments that are valid at the top level of a namespace.
let private namespaceTopLevel =
    pcollect [ functionDeclaration
               operationDeclaration
               udtDeclaration
               openDirective ]
    .>> eotEof

/// Parses a symbol tuple containing identifiers of the given kind.
let rec private symbolTuple kind =
    parse {
        let declaration = expectedId kind (term symbol)

        return! declaration <|> tuple1 (declaration <|> symbolTuple kind)
    }

/// Parses a let statement.
let private letStatement =
    expectedKeyword qsImmutableBinding ?>> symbolTuple Declaration ?>> expected equal ?>> expression

/// Parses a mutable statement.
let private mutableStatement =
    expectedKeyword qsMutableBinding ?>> symbolTuple Declaration ?>> expected equal ?>> expression

/// Parses a set statement.
let private setStatement =
    let infixOp =
        operatorLike "="
        <|> pcollect [ expectedKeyword andOperator
                       expectedKeyword orOperator ]

    let assignment =
        symbolTuple MutableVariable
        ?>> (opt infixOp |>> Option.defaultValue [])
        ?>> expected equal
        ?>> expression

    let copyAndUpdate =
        expectedId MutableVariable (term symbol)
        ?>> expected (operator qsCopyAndUpdateOp.op "")
        ?>> expected equal
        ?>> (expression <|>@ expectedId NamedItem (term symbol))
        ?>> expected (operator qsCopyAndUpdateOp.cont "")
        ?>> expression

    expectedKeyword qsValueUpdate ?>> (attempt assignment <|> copyAndUpdate)

/// Parses a return statement.
let private returnStatement = expectedKeyword qsReturn ?>> expression

/// Parses a fail statement.
let private failStatement = expectedKeyword qsFail ?>> expression

/// Parses an if clause.
let private ifClause = expectedKeyword qsIf ?>> expectedBrackets (lTuple, rTuple) expression

/// Parses an elif clause.
let private elifClause = expectedKeyword qsElif ?>> expectedBrackets (lTuple, rTuple) expression

/// Parses an else clause.
let private elseClause = expectedKeyword qsElse

/// Parses a for-block intro.
let private forHeader =
    let binding = symbolTuple Declaration ?>> expectedKeyword qsRangeIter ?>> expression
    expectedKeyword qsFor ?>> expectedBrackets (lTuple, rTuple) binding

/// Parses a while-block intro.
let private whileHeader = expectedKeyword qsWhile ?>> expectedBrackets (lTuple, rTuple) expression

/// Parses a repeat-until-success block intro.
let private repeatHeader = expectedKeyword qsRepeat

/// Parses a repeat-until-success block outro.
let private untilFixup =
    expectedKeyword qsUntil
    ?>> expectedBrackets (lTuple, rTuple) expression
    ?>> expectedKeyword qsRUSfixup

/// Parses a within block intro.
let private withinHeader = expectedKeyword qsWithin

/// Parses an apply block intro.
let private applyHeader = expectedKeyword qsApply

/// Parses a qubit initializer tuple used to allocate qubits in using- and borrowing-blocks.
let rec private qubitInitializerTuple =
    parse {
        let item =
            expectedKeyword qsQubit ?>> (expected unitValue <|> expectedBrackets (lArray, rArray) expression)

        return! item <|> (tuple1 item <|> qubitInitializerTuple)
    }

/// Parses a using-block intro.
let private usingHeader =
    let binding = symbolTuple Declaration ?>> expected equal ?>> qubitInitializerTuple
    expectedKeyword qsUsing ?>> expectedBrackets (lTuple, rTuple) binding

/// Parses a borrowing-block intro.
let private borrowingHeader =
    let binding = symbolTuple Declaration ?>> expected equal ?>> qubitInitializerTuple
    expectedKeyword qsBorrowing ?>> expectedBrackets (lTuple, rTuple) binding

/// Parses an operation specialization declaration.
let private specializationDeclaration =
    let argumentTuple = tuple (expectedId Declaration (term symbol) <|> operator omittedSymbols.id "")

    let generator =
        pcollect [ expectedKeyword autoFunctorGenDirective
                   expectedKeyword selfFunctorGenDirective
                   expectedKeyword invertFunctorGenDirective
                   expectedKeyword distributeFunctorGenDirective
                   argumentTuple ]

    pcollect [ expectedKeyword ctrlDeclHeader ?>> (expectedKeyword adjDeclHeader ?>> generator <|>@ generator)
               expectedKeyword adjDeclHeader ?>> (expectedKeyword ctrlDeclHeader ?>> generator <|>@ generator)
               expectedKeyword bodyDeclHeader ?>> generator ]

/// Parses statements that are valid in both functions and operations.
let private callableStatement =
    pcollect [ letStatement
               mutableStatement
               setStatement
               returnStatement
               failStatement
               ifClause
               forHeader
               expression ]
    .>> eotEof

/// Parses a statement in a function.
let private functionStatement = whileHeader .>> eotEof <|>@ callableStatement

/// Parses a statement in a function that follows an if or elif clause in the same scope.
let private functionStatementFollowingIf = pcollect [ elifClause; elseClause ] .>> eotEof <|>@ functionStatement

/// Parses a statement in an operation.
let private operationStatement =
    pcollect [ repeatHeader
               withinHeader
               usingHeader
               borrowingHeader ]
    .>> eotEof
    <|>@ callableStatement

/// Parses a statement in an operation that follows an if or elif clause in the same scope.
let private operationStatementFollowingIf = pcollect [ elifClause; elseClause ] .>> eotEof <|>@ operationStatement

/// Parses a statement in the top-level scope of an operation.
let private operationTopLevel = specializationDeclaration .>> eotEof <|>@ operationStatement

/// Parses a statement in the top-level scope of an operation that follows an if or elif clause.
let private operationTopLevelFollowingIf = pcollect [ elifClause; elseClause ] .>> eotEof <|>@ operationTopLevel

/// Parses a namespace declaration.
let private namespaceDeclaration = expectedKeyword namespaceDeclHeader ?>> expectedQualifiedSymbol Namespace

/// Parses the fragment text assuming that it is in the given scope and follows the given previous fragment kind in the
/// same scope (or null if it is the first statement in the scope). Returns the set of completion kinds that are valid
/// at the end of the text.
let GetCompletionKinds scope previous text =
    let parser =
        match (scope, previous) with
        | (TopLevel, _) -> namespaceDeclaration
        | (NamespaceTopLevel, _) -> namespaceTopLevel
        | (Function, Value (IfClause _))
        | (Function, Value (ElifClause _)) -> functionStatementFollowingIf
        | (Function, _) -> functionStatement
        | (OperationTopLevel, Value (IfClause _))
        | (OperationTopLevel, Value (ElifClause _)) -> operationTopLevelFollowingIf
        | (OperationTopLevel, Value RepeatIntro)
        | (Operation, Value RepeatIntro) -> untilFixup .>> eotEof
        | (OperationTopLevel, Value WithinBlockIntro)
        | (Operation, Value WithinBlockIntro) -> applyHeader .>> eotEof
        | (OperationTopLevel, _) -> operationTopLevel
        | (Operation, Value (IfClause _))
        | (Operation, Value (ElifClause _)) -> operationStatementFollowingIf
        | (Operation, _) -> operationStatement

    match runParserOnString parser [] "" (text + "\u0004") with
    | ParserResult.Success (result, _, _) -> Success(Set.ofList result)
    | ParserResult.Failure (detail, _, _) -> Failure detail
