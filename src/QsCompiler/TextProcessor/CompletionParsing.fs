module Microsoft.Quantum.QsCompiler.TextProcessing.CompletionParsing

open System
open FParsec
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.TextProcessing.Keywords
open Microsoft.Quantum.QsCompiler.TextProcessing.ParsingPrimitives
open Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxBuilder
open Microsoft.Quantum.QsCompiler.TextProcessing.TypeParsing


/// Describes the kind of identifier that is expected at a particular position in the source code.
type IdentifierKind =
    | Declaration
    | Type
    | Characteristic
    | Keyword of string

/// Parses the end-of-transmission character.
let private eot =
    pchar '\u0004'

/// Parses a symbol. Includes reserved keywords if the keyword is followed immediately by EOT without any whitespace
/// (i.e., a possibly incomplete symbol that happens to start with a keyword). Does not consume whitespace.
let private symbol =
    let qsIdentifier =
        identifier <| IdentifierOptions (isAsciiIdStart = (fun c -> isAsciiLetter c || c = '_'),
                                         isAsciiIdContinue = isSymbolContinuation)
    notFollowedBy qsReservedKeyword .>> qsIdentifier <|>
    (qsReservedKeyword >>. previousCharSatisfiesNot Char.IsWhiteSpace >>. followedBy eot)

/// If `p1` succeeds and consumes all input, returns the result of `p1`. Otherwise, parses `p1` then `p2` and returns
/// the result of `p2`.
let private (?>>) p1 p2 =
    attempt (p1 .>> eof) <|> (p1 >>. p2)

/// Parses an expected identifier of the given kind. The identifier is optional if EOT occurs first.
let private expectedId kind p =
    (eot >>% [kind]) <|>
    attempt (p >>. previousCharSatisfiesNot Char.IsWhiteSpace >>. optional eot >>% [kind]) <|>
    (p >>% [])

/// Parses an expected operator. The operator is optional if EOT occurs first.
let private expectedOp p =
    (eot >>% []) <|> (p >>% [])

/// Parses an expected keyword. If the keyword parser fails, this parser can still succeed if the next token is symbol-
/// like and occurs immediately before EOT (i.e., a possibly incomplete keyword).
let private expectedKeyword keyword =
    expectedId (Keyword keyword.id) keyword.parse <|> (symbol >>. eot >>% [Keyword keyword.id])

/// Tries all parsers in the sequence `ps`, backtracking to the initial state after each parser. Concatenates the
/// results from all parsers that succeeded into a single list.
///
/// The parser state after running this parser is the state after running the first parser in the sequence that
/// succeeds.
let private pcollect (ps : seq<Parser<'a list, 'u>>) =
    getCharStreamState >>= fun state stream ->
        let backtrack p =
            let reply = p stream
            stream.BacktrackTo state
            reply
        let results =
            ps |>
            Seq.map backtrack |>
            Seq.filter (fun reply -> reply.Status = ReplyStatus.Ok) |>
            Seq.collect (fun reply -> reply.Result) |>
            Seq.toList
        choice (Seq.map attempt ps) >>% results <| stream

/// `p1 <||> p2` is equivalent to `pcollect [p1; p2]`.
let private (<||>) p1 p2 =
    pcollect [p1; p2]

/// Parses the brackets around a tuple, where the inside of the tuple is parsed by `inside` and the right bracket is
/// optional if the stream ends first.
let private tupleBrackets inside =
    bracket lTuple >>. inside ?>> expectedOp (bracket rTuple)

/// Parses angle brackets, where the inside of the brackets is parsed by `inside` and the right bracket is optional if
/// the stream ends first.
let private angleBrackets inside =
    bracket lAngle >>. inside ?>> expectedOp (bracket rAngle)

/// Recursively parses a tuple where each item is parsed by `item` and the right bracket is optional if the stream ends
/// first.
let rec private buildTuple item =
    tupleBrackets (sepBy1 item comma |>> List.last)

/// Parses a type.
let (private qsType, private qsTypeImpl) = createParserForwardedToRef()

/// Parses the unit type.
let private unitType = 
    expectedId Type qsUnit.parse

/// Parses an atomic (non-array, non-tuple, non-function, non-operation) type, excluding user-defined types.
let private atomicType = 
    expectedId Type <| choice [
        qsBigInt.parse
        qsBool.parse
        qsDouble.parse
        qsInt.parse
        qsPauli.parse
        qsQubit.parse
        qsRange.parse
        qsResult.parse
        qsString.parse
        qsUnit.parse
    ]

/// Parses a user-defined type.
let private userDefinedType = 
    expectedId Type (multiSegmentSymbol ErrorCode.InvalidTypeName)

/// Parses a characteristics annotation (the characteristics keyword followed by a characteristics expression).
let private characteristicsAnnotation =
    expectedKeyword qsCharacteristics ?>> expectedId Characteristic (expectedCharacteristics eof)

/// Parses an operation type.
let private operationType =
    let inOutType = qsType >>. opArrow >>. qsType
    let inside =
        attempt (tupleBrackets inOutType ?>> characteristicsAnnotation) <|>
        attempt (inOutType ?>> characteristicsAnnotation) <|>
        inOutType
    tupleBrackets inside

/// Parses a function type.
let private functionType =
    tupleBrackets (qsType >>. fctArrow >>. qsType)

/// Parses a tuple type.
let private tupleType =
    buildTuple qsType

/// Parses a generic type parameter.
let private typeParameter = 
    pchar '\'' >>. expectedId Type (term symbol)

/// Parses a generic type parameter declaration.
let private typeParameterDeclaration =
    expectedId Declaration (pchar '\'') ?>> expectedId Declaration (term symbol)

do qsTypeImpl :=
    choice <| List.map attempt [
        typeParameter
        operationType
        functionType 
        unitType
        tupleType 
        atomicType
        userDefinedType
    ] .>> many (arrayBrackets emptySpace)

/// Parses a callable signature.
let private callableSignature =
    let name = expectedId Declaration (term symbol)
    let typeAnnotation = expectedOp colon ?>> qsType
    let genericParamList =
        angleBrackets (sepBy typeParameterDeclaration comma |>> List.tryLast |>> Option.defaultValue [Declaration])
    let argumentTuple = expectedOp unitValue <|> buildTuple (name ?>> typeAnnotation)
    name ?>> (opt genericParamList |>> Option.defaultValue []) ?>> argumentTuple ?>> typeAnnotation

/// Parses a function declaration.
let private functionDeclaration =
    expectedKeyword fctDeclHeader ?>> callableSignature

/// Parses an operation declaration.
let private operationDeclaration =
    expectedKeyword opDeclHeader ?>> callableSignature ?>> characteristicsAnnotation

/// Parses a user-defined type declaration.
let private udtDeclaration = 
    let name = expectedId Declaration (term symbol)
    let udtTuple =
        let namedItem = name ?>> expectedOp colon ?>> qsType
        qsType <||> buildTuple (namedItem <||> qsType)
    expectedKeyword typeDeclHeader ?>> name ?>> expectedOp equal ?>> udtTuple

/// Parses fragments that are valid at the top level of a namespace.
let private insideNamespace =
    pcollect [
        functionDeclaration
        operationDeclaration
        udtDeclaration
    ]

/// Parses the possibly incomplete fragment text and returns the set of possible identifiers expected at the end of the
/// fragment.
///
/// Only top-level namespace fragments (except open) are currently supported.
let GetExpectedIdentifiers text =
    match runParserOnString insideNamespace [] "" (text + "\u0004") with
    | Success (result, _, _) -> Set.ofList result
    | Failure (_) -> Set.empty
