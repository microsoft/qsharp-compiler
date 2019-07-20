module Microsoft.Quantum.QsCompiler.TextProcessing.CompletionParsing

open System
open FParsec
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.TextProcessing.Keywords
open Microsoft.Quantum.QsCompiler.TextProcessing.ParsingPrimitives
open Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxBuilder
open Microsoft.Quantum.QsCompiler.TextProcessing.TypeParsing


/// Describes the type of identifier that is expected at a particular position in the source code.
type Context =
    | DeclaredSymbol
    | Type
    | Characteristic
    | Keyword of string
    | Nothing

/// Parses the end-of-transmission character.
let private eot =
    pchar '\u0004'

/// If `p1` succeeds and consumes all input, returns the result of `p1`. Otherwise, parses `p1` then `p2` and returns
/// the result of `p2`.
let private (?>>) p1 p2 =
    attempt (p1 .>> eof) <|> (p1 >>. p2)

/// Returns a parser whose result is `context` if EOT occurs within the context of the identifier-like parser `p`, or
/// whose result is `Nothing` otherwise. `p` is optional if EOT occurs first.
let private withContext context p =
    (eot >>% context) <|>
    attempt (p >>. previousCharSatisfiesNot Char.IsWhiteSpace >>. optional eot >>% context) <|>
    (p >>% Nothing)

/// Returns a parser that resets the context to `Nothing` immediately before or after the parser `p`. `p` is optional if
/// EOT occurs first.
let private noContext p =
    (eot >>% Nothing) <|> (p >>% Nothing)

/// Parses the brackets around a tuple, where the inside of the tuple is parsed by `inside` and the right bracket is
/// optional if the stream ends first.
let private tupleBrackets inside =
    bracket lTuple >>. inside ?>> noContext (bracket rTuple)

/// Recursively parses a tuple where each item is parsed by `item` and the right bracket is optional if the stream ends
/// first.
let rec private buildTuple item stream =
    tupleBrackets (sepBy1 item comma |>> List.last) stream

/// Parses a type.
let (private qsType, private qsTypeImpl) = createParserForwardedToRef()

/// Parses the unit type.
let private unitType = 
    withContext Type qsUnit.parse

/// Parses an atomic (non-array, non-tuple, non-function, non-operation) type, excluding user-defined types.
let private atomicType = 
    withContext Type <| choice [
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
    withContext Type (multiSegmentSymbol ErrorCode.InvalidTypeName)

/// Parses a characteristics annotation (the characteristics keyword followed by a characteristics expression).
let private characteristicsAnnotation =
    withContext (Keyword qsCharacteristics.id) qsCharacteristics.parse ?>>
    withContext Characteristic (expectedCharacteristics eof)

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
    pchar '\'' >>. withContext Type (term (symbolNameLike ErrorCode.InvalidTypeParameterName) |>> fst)

do qsTypeImpl :=
    choice [
        attempt typeParameter
        attempt operationType
        attempt functionType 
        attempt unitType
        attempt tupleType 
        attempt atomicType
        attempt userDefinedType
    ] .>> many (arrayBrackets emptySpace)

/// Parses an operation declaration.
let private operationDeclaration =
    let header = withContext (Keyword opDeclHeader.id) opDeclHeader.parse
    let name = withContext DeclaredSymbol (symbolLike ErrorCode.InvalidIdentifierName)
    let typeAnnotation = noContext colon ?>> qsType
    let genericParamList = 
        let noTypeParams = angleBrackets emptySpace >>% () <|> notFollowedBy lAngle
        let typeParams = angleBrackets <| sepBy1 (withContext DeclaredSymbol typeParameterNameLike) comma
        noContext noTypeParams <|> (typeParams |>> fst |>> List.last)
    let argumentTuple = noContext unitValue <|> buildTuple (name ?>> typeAnnotation)
    header ?>> name ?>> genericParamList ?>> argumentTuple ?>> typeAnnotation ?>> characteristicsAnnotation

/// Parses the possibly incomplete fragment text and returns the context at the end of the fragment.
///
/// Only operation declaration fragments are currently supported.
let getContext text =
    runParserOnString operationDeclaration [] "" (text + "\u0004")
