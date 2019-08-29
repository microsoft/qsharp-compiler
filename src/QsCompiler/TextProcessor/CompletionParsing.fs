/// Parses incomplete fragment text and returns all of the possible kinds of identifiers that can follow.
///
/// This parser is designed to be used with code completion. It assumes that the text it has seen so far is
/// syntactically valid but allows the fragment to end in the middle. For example, not all open brackets need to be
/// closed if it is still valid for the bracket to be closed later.
module Microsoft.Quantum.QsCompiler.TextProcessing.CompletionParsing

open System
open System.Collections.Generic
open FParsec
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.TextProcessing.ExpressionParsing
open Microsoft.Quantum.QsCompiler.TextProcessing.Keywords
open Microsoft.Quantum.QsCompiler.TextProcessing.ParsingPrimitives
open Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxBuilder

#nowarn "40"


/// Describes the scope of a code fragment in terms of what kind of completions are available.
type CompletionScope =
    /// The code fragment is inside a namespace but outside any callable.
    | NamespaceTopLevel
    /// The code fragment is inside a function.
    | Function
    /// The code fragment is inside an operation at the top-level scope.
    | OperationTopLevel
    /// The code fragment is inside another scope within an operation.
    | Operation

/// Describes the kind of completion that is expected at a position in the source code.
type CompletionKind =
    /// The completion is the given keyword.
    | Keyword of string
    /// The completion is a variable.
    | Variable
    /// The completion is a mutable variable.
    | MutableVariable
    /// The completion is a callable.
    | Callable
    /// The completion is a user-defined type.
    | UserDefinedType
    /// The completion is a type parameter.
    | TypeParameter
    /// The completion is a new symbol declaration.
    | Declaration
    /// The completion is a namespace.
    | Namespace
    /// The completion is a member of the given namespace and has the given kind.
    | Member of string * CompletionKind
    /// The completion is a named item in a user-defined type.
    // TODO: Add information so completion knows the type being accessed.
    | NamedItem

/// The result of parsing a code fragment for completions.
type CompletionResult =
    /// The set of completion kinds is expected at the end of the code fragment.
    | Success of IEnumerable<CompletionKind>
    /// Parsing failed with an error message.
    | Failure of string

/// Merges the error messages from all replies in the list.
let rec private toErrorMessageList (replies : Reply<'a> list) =
    match replies with
    | reply :: tail when reply.Status = Ok -> toErrorMessageList tail
    | reply :: tail -> ErrorMessageList.Merge (reply.Error, toErrorMessageList tail)
    | [] -> null

/// The missing expression keyword.
let private missingExpr =
    { parse = keyword "_"; id = "_" }

/// The omitted symbols keyword.
let private omittedSymbols =
    { parse = keyword "..."; id = "..." }

/// `p1 ?>> p2` attempts `p1`. If `p1` succeeds and consumes all input, it returns the result of `p1` without using
/// `p2`. Otherwise, it continues parsing with `p2` and returns the result of `p2`.
let private (?>>) p1 p2 =
    attempt (p1 .>> eof) <|> (p1 >>. p2)

/// `p1 @>> p2` parses `p1`, then `p2` (if `p1` did not consume all input), and concatenates the results.
let private (@>>) p1 p2 =
    p1 .>>. (eof >>% [] <|> p2) |>> fun (list1, list2) -> list1 @ list2

/// Tries all parsers in the sequence `ps`, backtracking to the initial state after each parser. Concatenates the
/// results from all parsers that succeeded into a single list.
///
/// The parser state after running this parser is the state after running the first parser in the sequence that
/// succeeds.
let private pcollect ps stream =
    let replies = ps |> Seq.map (fun p -> (p, lookAhead p stream)) |> Seq.toList
    let successes = replies |> List.filter (fun (_, reply) -> reply.Status = Ok)
    if List.isEmpty successes then
        Reply (Error, toErrorMessageList (List.map snd replies))
    else
        let results = successes |> List.map snd |> List.collect (fun reply -> reply.Result)
        let p = List.head successes |> fst
        (p >>% results) stream

/// `p1 <|>@ p2` is equivalent to `pcollect [p1; p2]`.
let private (<|>@) p1 p2 =
    pcollect [p1; p2]

/// Parses the end-of-transmission character.
let private eot =
    pchar '\u0004'

/// Parses EOT (or checks if EOT was the last character consumed), followed by EOF.
let private eotEof =
    previousCharSatisfies ((=) '\u0004') <|> (eot >>% ()) .>> eof

/// Parses a symbol. Includes reserved keywords if the keyword is followed immediately by EOT without any whitespace
/// (i.e., a possibly incomplete symbol that happens to start with a keyword). Does not consume whitespace.
let private symbol =
    let qsIdentifier =
        identifier <| IdentifierOptions (isAsciiIdStart = isSymbolStart, isAsciiIdContinue = isSymbolContinuation)
    notFollowedBy qsReservedKeyword >>. qsIdentifier <|> (qsIdentifier .>> followedBy eot)

/// Parses an operator one character at a time. Fails if the operator is followed by any of the characters in
/// `notAfter`. Returns the empty list.
let private operator (op: string) notAfter =
    // For operators that start with letters (e.g., "w/"), group the letters together with the first operator character
    // to avoid a conflict with keyword or symbol parsers.
    let numLetters = Seq.length (Seq.takeWhile Char.IsLetter op)
    let charByChar p c = p ?>> ((eot >>% ()) <|> (pchar c >>% ()))
    let p = Seq.fold charByChar (pstring op.[..numLetters] >>% ()) op.[numLetters + 1..]
    attempt (p ?>> nextCharSatisfiesNot (fun c -> Seq.contains c notAfter)) >>. (emptySpace >>% ()) >>% []

/// Parses `p` unless EOT occurs first. Returns the empty list.
let private expected p =
    eot >>% [] <|> (p >>% [])

/// Parses an expected identifier. The identifier is optional if EOT occurs first. Returns `[kind]` if EOT occurs first
/// or if `p` did not end in whitespace; otherwise, returns `[]`.
let private expectedId kind p =
    eot >>% [kind] <|>
    attempt (p >>. previousCharSatisfiesNot Char.IsWhiteSpace >>. optional eot >>% [kind]) <|>
    (p >>% [])

/// Parses an expected keyword. If the keyword parser fails, this parser can still succeed if the next token is symbol-
/// like and occurs immediately before EOT (i.e., a possibly incomplete keyword).
let private expectedKeyword keyword =
    expectedId (Keyword keyword.id) keyword.parse <|> attempt (symbol >>. eot >>% [Keyword keyword.id])

/// Parses an expected qualified symbol. The identifier is optional if EOT occurs first. Returns `[kind]` if EOT occurs
/// first or there is no whitespace after the qualified symbol; otherwise, returns `[]`.
let private expectedQualifiedSymbol kind =
    let withoutLast list = List.take (List.length list - 1) list
    let asMember = function
        | [] -> [kind]
        | symbols -> [Member (String.Join (".", symbols), kind)]
    let qualifiedSymbol =
        attempt (sepBy1 symbol (pchar '.')) |>> withoutLast |>> asMember <|>
        (sepEndBy1 symbol (pchar '.') |>> asMember)
    eot >>% [kind] <|>
    attempt (term qualifiedSymbol |>> fst .>> previousCharSatisfiesNot Char.IsWhiteSpace .>> optional eot) <|>
    (term qualifiedSymbol >>% [])

/// Parses the unit value. The right bracket is optional if EOT occurs first.
let private unitValue : Parser<CompletionKind list, QsCompilerDiagnostic list> =
    term (pchar '(' >>. emptySpace >>. expected (pchar ')')) |>> fst

/// `manyR p shouldBacktrack` is like `many p` but is reentrant on the last item if
///     1. The last item is followed by EOT; and
///     2. `shouldBacktrack` succeeds at the beginning of the last item, or the last item consists of only EOT.
///
/// This is useful if it is ambiguous whether the last item belongs to `p` or the parser that comes after `manyR`.
let private manyR p shouldBacktrack stream =
    let last = (many1 (getCharStreamState .>>. attempt p) |>> List.last) stream
    if last.Status <> Ok then
        Reply []
    else
        let after = (getCharStreamState stream).Result
        let consumedEot = (previousCharSatisfies ((<>) '\u0004') stream).Status = Ok
        stream.BacktrackTo (fst last.Result)
        if ((notFollowedBy shouldBacktrack >>. notFollowedBy eot) stream).Status = Ok || consumedEot then
            stream.BacktrackTo after |> ignore
        Reply (snd last.Result)

/// `manyLast p` is like `many p` but returns only the result of the last item, or the empty list if no items were
/// parsed.
let private manyLast p =
    many p |>> List.tryLast |>> Option.defaultValue []

/// Parses zero or more occurrences of `p` separated by `sep` and returns only the result of the last item, or the empty
/// list if no items were parsed.
let private sepByLast p sep =
    sepBy p sep |>> List.tryLast |>> Option.defaultValue []

/// Parses brackets around `p`. The right bracket is optional if EOT occurs first. Use `brackets` instead of
/// `expectedBrackets` if there are other parsers besides a left bracket that can follow and you want this parser to
/// fail if the stream has ended.
let private brackets (left, right) p =
    bracket left >>. p ?>> expected (bracket right)

/// Parses brackets around `p`. Both the left and right brackets are optional if EOT occurs first. Use
/// `expectedBrackets` instead of `brackets` if a left bracket is the only thing that can follow and you want this
/// parser to still succeed if the stream has ended.
let private expectedBrackets (left, right) p =
    expected (bracket left) ?>> p ?>> expected (bracket right)

/// Parses a tuple of zero or more items each parsed by `p`.
let private tuple p =
    brackets (lTuple, rTuple) (sepBy p comma |>> List.tryLast |>> Option.defaultValue [])

/// Parses a tuple of one or more items each parsed by `p`.
let private tuple1 p =
    brackets (lTuple, rTuple) (sepBy1 p comma |>> List.last)

/// Parses an array of items each parsed by `p`.
let private array p =
    brackets (lArray, rArray) (sepBy1 p comma |>> List.last)

/// Creates an expression parser using the given prefix operator, infix operator, postfix operator, and term parsers.
let private createExpressionParser prefixOp infixOp postfixOp expTerm =
    let termBundle = manyR prefixOp symbol @>> expTerm ?>> manyLast postfixOp
    termBundle @>> manyLast (infixOp ?>> termBundle)

/// Parses the characteristics keyword followed by a characteristics expression.
let private characteristicsAnnotation =
    let rec characteristicsExpr = parse {
        let infixOp = operator qsSetUnion.op "" <|> operator qsSetIntersection.op ""
        let expTerm = pcollect [
            brackets (lTuple, rTuple) characteristicsExpr
            expectedKeyword qsAdjSet
            expectedKeyword qsCtlSet
        ]
        return! createExpressionParser pzero infixOp pzero expTerm
    }
    expectedKeyword qsCharacteristics ?>> characteristicsExpr

/// Parses a type.
let rec private qsType =
    parse {
        return! nonArrayType .>> many (brackets (lArray, rArray) (emptySpace >>% []))
    }

/// Parses types where the top-level type is not an array type.
and nonArrayType =
    let typeParameter = pchar '\'' >>. expectedId TypeParameter (term symbol)
    let operationType =
        let inOutType = qsType >>. opArrow >>. qsType
        brackets (lTuple, rTuple) (attempt (brackets (lTuple, rTuple) inOutType ?>> characteristicsAnnotation) <|>
                                   attempt (inOutType ?>> characteristicsAnnotation) <|>
                                   inOutType)
    let functionType = brackets (lTuple, rTuple) (qsType >>. fctArrow >>. qsType)
    let keywordType =
        [
            qsBigInt
            qsBool
            qsDouble
            qsInt
            qsPauli
            qsQubit
            qsRange
            qsResult
            qsString
            qsUnit
        ] |> List.map expectedKeyword |> pcollect
    choice [
        attempt typeParameter
        attempt operationType
        attempt functionType
        attempt (tuple1 qsType)
        keywordType <|>@ expectedQualifiedSymbol UserDefinedType
    ]

/// Parses a callable signature.
let private callableSignature =
    let name = expectedId Declaration (term symbol)
    let typeAnnotation = expected colon ?>> qsType
    let typeParam = expected (pchar '\'') ?>> expectedId Declaration (term symbol)
    let typeParamList = brackets (lAngle, rAngle) (sepByLast typeParam comma)
    let argumentTuple = tuple (name ?>> typeAnnotation)
    name ?>> (typeParamList <|> (optional eot >>% [])) ?>> argumentTuple ?>> typeAnnotation

/// Parses a function declaration.
let private functionDeclaration =
    expectedKeyword fctDeclHeader ?>> callableSignature

/// Parses an operation declaration.
let private operationDeclaration =
    expectedKeyword opDeclHeader ?>> callableSignature ?>> characteristicsAnnotation

/// Parses a user-defined type declaration.
let private udtDeclaration = 
    let name = expectedId Declaration (term symbol)
    let rec udt = parse {
        let namedItem = name ?>> expected colon ?>> qsType
        return! qsType <|>@ tuple1 (namedItem <|>@ udt)
    }
    expectedKeyword typeDeclHeader ?>> name ?>> expected equal ?>> udt

/// Parses an open directive.
let private openDirective =
    expectedKeyword importDirectiveHeader ?>>
    expectedQualifiedSymbol Namespace ?>>
    expectedKeyword importedAs ?>>
    expectedQualifiedSymbol Declaration

/// Parses fragments that are valid at the top level of a namespace.
let private namespaceTopLevel =
    pcollect [
        functionDeclaration
        operationDeclaration
        udtDeclaration
        openDirective
    ] .>> eotEof

/// Parses a sequence of operator-like characters (even if those characters are not a valid operator), excluding
/// characters in `excluded`. Consumes whitespace.
let private operatorLike excluded =
    let isOperatorChar c =
        not (isSymbolContinuation c || Char.IsWhiteSpace c || Seq.exists ((=) c) "()[]{}\u0004") &&
        not (Seq.exists ((=) c) excluded)
    many1Chars (satisfy isOperatorChar) >>. emptySpace >>% []

/// Parses an expression.
let rec private expression = parse {
    let prefixOp = expectedKeyword notOperator <|> operator qsNEGop.op "" <|> operator qsBNOTop.op ""
    
    let infixOp = operatorLike "" <|> pcollect [expectedKeyword andOperator; expectedKeyword orOperator]

    let postfixOp =
        let copyAndUpdate =
            operator qsCopyAndUpdateOp.op "" >>.
            (expression <|>@ expectedId NamedItem (term symbol)) ?>>
            expected (operator qsCopyAndUpdateOp.cont "") ?>>
            expression
        let typeParamListOrLessThan =
            // This is a parsing hack for the < operator, which can be either less-than or the start of a type parameter
            // list.
            operator qsLTop.op "=-" ?>> ((sepByLast qsType comma ?>> expected (bracket rAngle)) <|>@ expression)
        choice [
            operator qsUnwrapModifier.op ""
            pstring qsOpenRangeOp.op .>> emptySpace >>. optional eot >>% []
            operator qsNamedItemCombinator.op "" ?>> expectedId NamedItem (term symbol)
            copyAndUpdate
            tuple expression
            array expression
            typeParamListOrLessThan
        ]
        
    let expTerm =
        let newArray =
            expectedKeyword arrayDecl ?>>
            nonArrayType ?>>
            manyR (brackets (lArray, rArray) (emptySpace >>% [])) (preturn ()) @>>
            array expression
        let keywordLiteral =
            [
                qsPauliX
                qsPauliY
                qsPauliZ
                qsPauliI
                qsZero
                qsOne
                qsTrue
                qsFalse
                missingExpr
            ] |> List.map expectedKeyword |> pcollect
        let stringLiteral =
            let unescaped p = previousCharSatisfies ((<>) '\\') >>. p
            let quote = pstring "\""
            let interpolated =
                let text =
                    manyChars (notFollowedBy (unescaped quote <|> unescaped lCurly) >>. anyChar) >>.
                    optional eot >>. preturn []
                let code = brackets (lCurly, rCurly) expression
                pchar '$' >>. expected quote ?>> text ?>> manyLast (code ?>> text) ?>> expected quote
            let uninterpolated =
                let text = manyChars (notFollowedBy (unescaped quote) >>. anyChar) >>. optional eot >>. preturn []
                quote >>. text ?>> expected quote
            interpolated <|> uninterpolated
        let functor = expectedKeyword qsAdjointFunctor <|>@ expectedKeyword qsControlledFunctor
        pcollect [
            operator qsOpenRangeOp.op "" >>. opt expression |>> Option.defaultValue []
            newArray
            tuple1 expression
            array expression
            keywordLiteral
            numericLiteral >>. (previousCharSatisfiesNot Char.IsWhiteSpace >>. optional eot >>% [] <|> preturn [])
            stringLiteral
            manyR functor symbol @>> expectedQualifiedSymbol Callable
            expectedId Variable (term symbol) .>> nextCharSatisfiesNot ((=) '.')
        ]
    
    return! createExpressionParser prefixOp infixOp postfixOp expTerm
}

/// Parses a symbol tuple containing identifiers of the given kind.
let rec private symbolTuple kind = parse {
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
    let infixOp = operatorLike "=" <|> pcollect [expectedKeyword andOperator; expectedKeyword orOperator]
    let assignment =
        symbolTuple MutableVariable ?>> (opt infixOp |>> Option.defaultValue []) ?>> expected equal ?>> expression
    let copyAndUpdate =
        expectedId MutableVariable (term symbol) ?>>
        expected (operator qsCopyAndUpdateOp.op "") ?>>
        expected equal ?>>
        (expression <|>@ expectedId NamedItem (term symbol)) ?>>
        expected (operator qsCopyAndUpdateOp.cont "") ?>>
        expression
    expectedKeyword qsValueUpdate ?>> (attempt assignment <|> copyAndUpdate)

/// Parses a return statement.
let private returnStatement =
    expectedKeyword qsReturn ?>> expression

/// Parses a fail statement.
let private failStatement =
    expectedKeyword qsFail ?>> expression

/// Parses an if clause.
let private ifClause =
    expectedKeyword qsIf ?>> expectedBrackets (lTuple, rTuple) expression

/// Parses an elif clause.
let private elifClause =
    expectedKeyword qsElif ?>> expectedBrackets (lTuple, rTuple) expression

/// Parses an else clause.
let private elseClause =
    expectedKeyword qsElse

/// Parses a for-block intro.
let private forHeader =
    let binding = symbolTuple Declaration ?>> expectedKeyword qsRangeIter ?>> expression
    expectedKeyword qsFor ?>> expectedBrackets (lTuple, rTuple) binding

/// Parses a while-block intro.
let private whileHeader =
    expectedKeyword qsWhile ?>> expectedBrackets (lTuple, rTuple) expression

/// Parses a repeat-until-success block intro.
let private repeatHeader =
    expectedKeyword qsRepeat

/// Parses a repeat-until success block outro.
let private untilFixup =
    expectedKeyword qsUntil ?>> expectedBrackets (lTuple, rTuple) expression ?>> expectedKeyword qsRUSfixup

/// Parses a qubit initializer tuple used to allocate qubits in using- and borrowing-blocks.
let rec private qubitInitializerTuple = parse {
    let item = expectedKeyword qsQubit ?>> (expected unitValue <|> expectedBrackets (lArray, rArray) expression)
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
        pcollect [
            expectedKeyword intrinsicFunctorGenDirective
            expectedKeyword autoFunctorGenDirective
            expectedKeyword selfFunctorGenDirective
            expectedKeyword invertFunctorGenDirective
            expectedKeyword distributeFunctorGenDirective
            argumentTuple
        ]
    pcollect [
        expectedKeyword ctrlDeclHeader ?>> (expectedKeyword adjDeclHeader ?>> generator <|>@ generator)
        expectedKeyword adjDeclHeader ?>> (expectedKeyword ctrlDeclHeader ?>> generator <|>@ generator)
        expectedKeyword bodyDeclHeader ?>> generator
    ]

/// Parses statements that are valid in both functions and operations.
let private callableStatement =
    pcollect [
        letStatement
        mutableStatement
        setStatement
        returnStatement
        failStatement
        ifClause
        forHeader
        expression
    ] .>> eotEof

/// Parses a statement in a function.
let private functionStatement =
    whileHeader .>> eotEof <|>@ callableStatement

/// Parses a statement in a function that follows an if or elif clause in the same scope.
let private functionStatementFollowingIf =
    pcollect [
        elifClause
        elseClause
    ] .>> eotEof <|>@ functionStatement

/// Parses a statement in an operation.
let private operationStatement =
    pcollect [
        repeatHeader
        usingHeader
        borrowingHeader
    ] .>> eotEof <|>@ callableStatement

/// Parses a statement in an operation that follows an if or elif clause in the same scope.
let private operationStatementFollowingIf =
    pcollect [
        elifClause
        elseClause
    ] .>> eotEof <|>@ operationStatement

/// Parses a statement in the top-level scope of an operation.
let private operationTopLevel =
    specializationDeclaration .>> eotEof <|>@ operationStatement

/// Parses a statement in the top-level scope of an operation that follows an if or elif clause.
let private operationTopLevelFollowingIf =
    pcollect [
        elifClause
        elseClause
    ] .>> eotEof <|>@ operationTopLevel

/// Parses a statement in an operation that follows a repeat header in the same scope.
let private operationStatementFollowingRepeat =
    untilFixup .>> eotEof

/// Parses the fragment text assuming that it is in the given scope and follows the given previous fragment kind in the
/// same scope (or null if it is the first statement in the scope). Returns the set of completion kinds that are valid
/// at the end of the text.
let GetCompletionKinds scope previous text =
    let parser =
        match (scope, previous) with
        | (NamespaceTopLevel, _) -> namespaceTopLevel
        | (Function, Value (IfClause _)) | (Function, Value (ElifClause _)) -> functionStatementFollowingIf
        | (Function, _) -> functionStatement
        | (OperationTopLevel, Value (IfClause _))
        | (OperationTopLevel, Value (ElifClause _)) -> operationTopLevelFollowingIf
        | (OperationTopLevel, Value (RepeatIntro _)) | (Operation, Value (RepeatIntro _)) ->
            operationStatementFollowingRepeat
        | (OperationTopLevel, _) -> operationTopLevel
        | (Operation, Value (IfClause _)) | (Operation, Value (ElifClause _)) -> operationStatementFollowingIf
        | (Operation, _) -> operationStatement
    match runParserOnString parser [] "" (text + "\u0004") with
    | ParserResult.Success (result, _, _) -> Success (Set.ofList result)
    | ParserResult.Failure (detail, _, _) -> Failure detail
