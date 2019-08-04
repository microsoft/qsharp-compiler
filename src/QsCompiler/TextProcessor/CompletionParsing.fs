/// Parses incomplete fragment text and returns all of the possible kinds of identifiers that can follow.
///
/// This parser is designed to be used with code completion. It assumes that the text it has seen so far is
/// syntactically valid but allows the fragment to end in the middle. For example, not all open brackets need to be
/// closed if it is still valid for the bracket to be closed later.
module Microsoft.Quantum.QsCompiler.TextProcessing.CompletionParsing

open System
open FParsec
open Microsoft.Quantum.QsCompiler.TextProcessing.ExpressionParsing
open Microsoft.Quantum.QsCompiler.TextProcessing.Keywords
open Microsoft.Quantum.QsCompiler.TextProcessing.ParsingPrimitives
open Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxBuilder
open Microsoft.Quantum.QsCompiler.TextProcessing.TypeParsing


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
    /// The identifier is a variable name.
    | Variable
    /// The identifier is a user-defined type name.
    | UserDefinedType
    /// The identifier is a type parameter.
    | TypeParameter
    /// The identifier is a new symbol declaration.
    | Declaration
    /// The identifier is a characteristic set name.
    | Characteristic
    /// The identifier is a namespace.
    | Namespace
    /// The identifier is a member of the given namespace and has the given kind.
    | Member of string * IdentifierKind

/// The exception that is thrown when the completion parser can't parse the text of a fragment.
exception CompletionParserError of detail : string * error : ParserError with
    override this.Message = this.error.ToString()
    override this.ToString() = this.detail

/// If `p1` succeeds and consumes all input, returns the result of `p1`. Otherwise, parses `p1` then `p2` and returns
/// the result of `p2`.
let private (?>>) p1 p2 =
    attempt (p1 .>> eof) <|> (p1 >>. p2)

/// `p1 <@> p2` parses `p1` then `p2` and concatenates the results.
let private (@>>) p1 p2 =
    p1 .>>. p2 |>> fun (list1, list2) -> list1 @ list2

/// Tries all parsers in the sequence `ps`, backtracking to the initial state after each parser. Concatenates the
/// results from all parsers that succeeded into a single list.
///
/// The parser state after running this parser is the state after running the first parser in the sequence that
/// succeeds.
let private pcollect ps stream =
    let successes =
        ps |>
        Seq.map (fun p -> (p, lookAhead p stream)) |>
        Seq.filter (fun (_, reply) -> reply.Status = Ok) |>
        Seq.toList
    if List.isEmpty successes then
        Reply (Error, ErrorMessageList (Message "No parser in pcollect succeeded"))
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

/// Parses a symbol. Includes reserved keywords if the keyword is followed immediately by EOT without any whitespace
/// (i.e., a possibly incomplete symbol that happens to start with a keyword). Does not consume whitespace.
let private symbol =
    let qsIdentifier =
        identifier <| IdentifierOptions (isAsciiIdStart = isSymbolStart, isAsciiIdContinue = isSymbolContinuation)
    notFollowedBy qsReservedKeyword >>. qsIdentifier <|> (qsIdentifier .>> followedBy eot)

/// Parses an expected operator. The `after` parser (if specified) must also succeed after the operator string `op` is
/// parsed. Returns the empty list.
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

/// Parses brackets around `p`. The right bracket is optional if EOT occurs first.
let private brackets (left, right) p =
    bracket left >>. p ?>> expected (bracket right)

/// Parses tuple brackets around `p`.
let private tupleBrackets p =
    brackets (lTuple, rTuple) p

/// Parses array brackets around `p`.
let private arrayBrackets p =
    brackets (lArray, rArray) p

/// Parses angle brackets around `p`.
let private angleBrackets p =
    brackets (lAngle, rAngle) p

/// Parses a tuple of items each parsed by `p`.
let private tuple p =
    tupleBrackets (sepBy1 p comma |>> List.last)

/// Parses an array of items each parsed by `p`.
let private array p =
    arrayBrackets (sepBy1 p comma |>> List.last)

/// Parses a type.
let (private qsType, private qsTypeImpl) = createParserForwardedToRef()

/// Parses the characteristics keyword followed by a characteristics expression.
let private characteristicsAnnotation =
    expectedKeyword qsCharacteristics ?>> expectedId Characteristic (expectedCharacteristics eof)

do qsTypeImpl :=
    let typeParameter = pchar '\'' >>. expectedId TypeParameter (term symbol)
    let operationType =
        let inOutType = qsType >>. opArrow >>. qsType
        tupleBrackets (attempt (tupleBrackets inOutType ?>> characteristicsAnnotation) <|>
                       attempt (inOutType ?>> characteristicsAnnotation) <|>
                       inOutType)
    let functionType = tupleBrackets (qsType >>. fctArrow >>. qsType)
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
    ] .>> many (arrayBrackets (emptySpace >>% []))

/// Parses a callable signature.
let private callableSignature =
    let name = expectedId Declaration (term symbol)
    let typeAnnotation = expected colon ?>> qsType
    let typeParam = expected (pchar '\'') ?>> expectedId Declaration (term symbol)
    let typeParamList = angleBrackets (sepBy typeParam comma |>> List.tryLast |>> Option.defaultValue [])
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
    let (udt, udtImpl) = createParserForwardedToRef ()
    do udtImpl :=
        let namedItem = name ?>> expected colon ?>> qsType
        qsType <|>@ tuple (namedItem <|>@ udt)
    expectedKeyword typeDeclHeader ?>> name ?>> expected equal ?>> udtTuple

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
    ]

/// Parses an expression.
let (private expression, private expressionImpl) = createParserForwardedToRef()

/// Parses any prefix operator in an expression.
let private prefixOp =
    expectedKeyword notOperator <|> operator qsNEGop.op None

/// Parses any infix operator in an expression.
let private infixOp =
    choice [
        expectedKeyword andOperator <|>@ expectedKeyword orOperator
        operator qsADDop.op None
        operator qsSUBop.op None
        operator qsRangeOp.op None
    ]

/// Parses any postfix operator in an expression.
let private postfixOp =
    choice [
        operator qsUnwrapModifier.op (Some (notFollowedBy (pchar '=')))
        operator qsOpenRangeOp.op None .>> optional eot
        (unitValue >>% [])
        tuple expression
        array expression
    ]
    
/// Parses any expression term.
let private expressionTerm =
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
        ] |> List.map expectedKeyword |> pcollect
    let functor = expectedKeyword qsAdjointFunctor <|>@ expectedKeyword qsControlledFunctor
    pcollect [
        tuple expression
        array expression
        keywordLiteral
        numericLiteral >>. optional eot >>% []
        manyR functor @>> expectedId Variable (term symbol)
    ]

/// Parses an expression.
do expressionImpl :=
    let termBundle = manyR prefixOp @>> expressionTerm ?>> manyLast postfixOp
    termBundle @>> manyLast (infixOp ?>> termBundle)

/// Parses a statement.
let private statement =
    expression

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
    | Success (result, _, _) -> Set.ofList result
    | Failure (detail, error, _) -> raise <| CompletionParserError (detail, error)
