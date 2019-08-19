/// Parses incomplete fragment text and returns all of the possible kinds of identifiers that can follow.
///
/// This parser is designed to be used with code completion. It assumes that the text it has seen so far is
/// syntactically valid but allows the fragment to end in the middle. For example, not all open brackets need to be
/// closed if it is still valid for the bracket to be closed later.
module Microsoft.Quantum.QsCompiler.TextProcessing.CompletionParsing

#nowarn "40"

open System
open System.Collections.Generic
open FParsec
open Microsoft.Quantum.QsCompiler.TextProcessing.ExpressionParsing
open Microsoft.Quantum.QsCompiler.TextProcessing.Keywords
open Microsoft.Quantum.QsCompiler.TextProcessing.ParsingPrimitives
open Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxBuilder


/// Describes the environment of a code fragment in terms of what kind of completions are available.
type CompletionEnvironment =
    /// The code fragment is inside a namespace but outside any callable.
    | NamespaceTopLevel
    /// The code fragment is a statement inside a callable.
    | Statement

/// Describes the kind of identifier that is expected at a particular position in the source code.
type IdentifierKind =
    /// The identifier is the given keyword.
    | Keyword of string
    /// The identifier is a variable or callable name.
    | Variable
    /// The identifier is a user-defined type name.
    | UserDefinedType
    /// The identifier is a type parameter.
    | TypeParameter
    /// The identifier is a new symbol declaration.
    | Declaration
    /// The identifier is a namespace.
    | Namespace
    /// The identifier is a member of the given namespace and has the given kind.
    | Member of string * IdentifierKind
    /// The identifier is a named item in a user-defined type.
    // TODO: Add information so completion knows the type being accessed.
    | NamedItem

/// The result of parsing a code fragment for completions.
type CompletionResult =
    /// The set of identifier kinds is expected at the end of the code fragment.
    | Success of IEnumerable<IdentifierKind>
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

/// `p1 ?>> p2` attempts `p1`. If `p1` succeeds and consumes all input, it returns the result of `p1` without using
/// `p2`. Otherwise, it continues parsing with `p2` and returns the result of `p2`.
let private (?>>) p1 p2 =
    attempt (p1 .>> eof) <|> (p1 >>. p2)

/// `p1 @>> p2` parses `p1` then `p2` and concatenates the results.
let private (@>>) p1 p2 =
    p1 .>>. p2 |>> fun (list1, list2) -> list1 @ list2

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

/// Parses an operator. The `after` parser (if specified) must also succeed after the operator string `op` is parsed.
/// Returns the empty list.
let private operator op after =
    term (pstring op >>. Option.defaultValue (preturn ()) after) >>% []

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

/// `manyR p` is like `many p` but is reentrant on the last item if the last item is followed by EOT. This is useful if,
/// for example, the last item is incomplete such that it is ambiguous whether the last item is part of the list or is
/// actually a delimiter.
let private manyR p stream =
    let last = (p .>> previousCharSatisfies ((<>) '\u0004') |> attempt |> many1 |>> List.last) stream
    let next = (p .>> previousCharSatisfies ((=) '\u0004') |> lookAhead) stream
    if next.Status = Ok then next
    elif last.Status = Ok then last
    else Reply []

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

/// Parses a tuple of items each parsed by `p`.
let private tuple p =
    brackets (lTuple, rTuple) (sepBy1 p comma |>> List.last)

/// Parses an array of items each parsed by `p`.
let private array p =
    brackets (lArray, rArray) (sepBy1 p comma |>> List.last)

/// Creates an expression parser using the given prefix operator, infix operator, postfix operator, and term parsers.
let private createExpressionParser prefixOp infixOp postfixOp expTerm =
    let termBundle = manyR prefixOp @>> expTerm ?>> manyLast postfixOp
    termBundle @>> manyLast (infixOp ?>> termBundle)

/// Parses the characteristics keyword followed by a characteristics expression.
let private characteristicsAnnotation =
    let rec characteristicsExpr = parse {
        let infixOp = operator qsSetUnion.op None <|> operator qsSetIntersection.op None
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
        attempt (tuple qsType)
        keywordType <|>@ expectedQualifiedSymbol UserDefinedType
    ]

/// Parses a callable signature.
let private callableSignature =
    let name = expectedId Declaration (term symbol)
    let typeAnnotation = expected colon ?>> qsType
    let typeParam = expected (pchar '\'') ?>> expectedId Declaration (term symbol)
    let typeParamList = brackets (lAngle, rAngle) (sepByLast typeParam comma)
    let argumentTuple = expected unitValue <|> tuple (name ?>> typeAnnotation)
    name ?>> (typeParamList <|>% []) ?>> argumentTuple ?>> typeAnnotation

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
        return! qsType <|>@ tuple (namedItem <|>@ udt)
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

/// Parses an expression.
let rec private expression = parse {
    let prefixOp = expectedKeyword notOperator <|> operator qsNEGop.op None <|> operator qsBNOTop.op None
    
    let infixOp =
        // Do not include LT here; it is parsed as a postfix operator instead.
        choice [
            expectedKeyword andOperator <|>@ expectedKeyword orOperator
            operator qsRangeOp.op None
            operator qsBORop.op None
            operator qsBXORop.op None
            operator qsBANDop.op None
            operator qsEQop.op None
            operator qsNEQop.op None
            operator qsLTEop.op None
            operator qsGTEop.op None
            operator qsGTop.op None
            operator qsRSHIFTop.op None
            operator qsADDop.op None
            operator qsSUBop.op None
            operator qsMULop.op None
            operator qsMODop.op None
            operator qsDIVop.op (Some (notFollowedBy (pchar '/')))
            operator qsPOWop.op None
        ]
    
    let postfixOp =
        let copyAndUpdate =
            operator qsCopyAndUpdateOp.op None >>.
            (expression <|>@ expectedId NamedItem (term symbol)) ?>>
            expected (operator qsCopyAndUpdateOp.cont None) ?>>
            expression
        let conditional =
            operator qsConditionalOp.op None >>.
            expression ?>>
            expected (operator qsConditionalOp.cont None) ?>>
            expression
        let typeParamListOrLessThan =
            // This is a parsing hack for the < operator, which can be either less-than or the start of a type parameter
            // list.
            brackets (lAngle, rAngle) (sepByLast qsType comma) <|>@ (operator qsLTop.op None >>. expression)
        choice [
            operator qsUnwrapModifier.op (Some (notFollowedBy (pchar '=')))
            operator qsOpenRangeOp.op None .>> optional eot
            operator qsNamedItemCombinator.op None >>. expectedId NamedItem (term symbol)
            copyAndUpdate
            conditional
            (unitValue >>% [])
            tuple expression
            array expression
            typeParamListOrLessThan
        ]
        
    let expTerm =
        let newArray =
            expectedKeyword arrayDecl ?>>
            nonArrayType ?>>
            manyR (brackets (lArray, rArray) (emptySpace >>% [])) @>>
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
            operator qsOpenRangeOp.op None >>. opt expression |>> Option.defaultValue []
            newArray
            tuple expression
            array expression
            keywordLiteral
            numericLiteral >>. (previousCharSatisfiesNot Char.IsWhiteSpace >>. optional eot >>% [] <|> preturn [])
            stringLiteral
            manyR functor @>> expectedQualifiedSymbol Variable
        ]
    
    return! createExpressionParser prefixOp infixOp postfixOp expTerm
}

/// Parses a symbol declaration tuple.
let rec private symbolTuple = parse {
    let declaration = expectedId Declaration (term symbol)
    return! declaration <|> tuple (declaration <|> symbolTuple)
}

/// Parses a let statement.
let private letStatement =
    expectedKeyword qsImmutableBinding ?>> symbolTuple ?>> expected equal ?>> expression

/// Parses a mutable statement.
let private mutableStatement =
    expectedKeyword qsMutableBinding ?>> symbolTuple ?>> expected equal ?>> expression

/// Parses a return statement.
let private returnStatement =
    expectedKeyword qsReturn ?>> expression

/// Parses a fail statement.
let private failStatement =
    expectedKeyword qsFail ?>> expression

/// Parses an if clause.
let private ifClause =
    expectedKeyword qsIf ?>> expectedBrackets (lTuple, rTuple) expression

/// Parses a for-block intro.
let private forHeader =
    let binding = symbolTuple ?>> expectedKeyword qsRangeIter ?>> expression
    expectedKeyword qsFor ?>> expectedBrackets (lTuple, rTuple) binding

/// Parses a statement.
let private statement =
    pcollect [
        letStatement
        mutableStatement
        returnStatement
        failStatement
        ifClause
        forHeader
        expression
    ] .>> eotEof

/// Parses the fragment text, which may be incomplete, and returns the set of possible identifiers expected at the end
/// of the text.
///
/// Raises a CompletionParseError if the text cannot be parsed.
let GetExpectedIdentifiers env text =
    let parser =
        match env with
        | NamespaceTopLevel -> namespaceTopLevel
        | Statement -> statement
    match runParserOnString parser [] "" (text + "\u0004") with
    | ParserResult.Success (result, _, _) -> Success (Set.ofList result)
    | ParserResult.Failure (detail, _, _) -> Failure detail
