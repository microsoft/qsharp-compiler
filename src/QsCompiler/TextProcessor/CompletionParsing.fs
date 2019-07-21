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
    | NoIdentifier

/// Parses the end-of-transmission character.
let private eot =
    pchar '\u0004'

/// If `p1` succeeds and consumes all input, returns the result of `p1`. Otherwise, parses `p1` then `p2` and returns
/// the result of `p2`.
let private (?>>) p1 p2 =
    attempt (p1 .>> eof) <|> (p1 >>. p2)

/// Parses an expected identifier of the given kind. The identifier is optional if EOT occurs first.
let private expectedId kind p =
    (eot >>% kind) <|>
    attempt (p >>. previousCharSatisfiesNot Char.IsWhiteSpace >>. optional eot >>% kind) <|>
    (p >>% NoIdentifier)

/// Parses an expected operator. The operator is optional if EOT occurs first.
let private expectedOp p =
    (eot >>% NoIdentifier) <|> (p >>% NoIdentifier)

/// Tries all parsers in the sequence `ps`, backtracking to the initial state after each parser. Returns the list of
/// results from all parsers that succeeded.
let private tryAll (ps : seq<Parser<'a, 'u>>) =
    getCharStreamState >>= fun state stream ->
        let backtrack p : Reply<'a> =
            let reply = p stream
            stream.BacktrackTo state
            reply
        let results =
            ps |>
            Seq.map backtrack |>
            Seq.filter (fun reply -> reply.Status = ReplyStatus.Ok) |>
            Seq.map (fun reply -> reply.Result) |>
            Seq.toList
        new Reply<'a list> (results)

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
let rec private buildTuple item stream =
    tupleBrackets (sepBy1 item comma |>> List.last) stream

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
    expectedId (Keyword qsCharacteristics.id) qsCharacteristics.parse ?>>
    expectedId Characteristic (expectedCharacteristics eof)

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
    pchar '\'' >>. expectedId Type (term (symbolNameLike ErrorCode.InvalidTypeParameterName) |>> fst)

/// Parses a generic type parameter declaration.
let private typeParameterDeclaration =
    expectedId Declaration (pchar '\'') ?>>
    expectedId Declaration (term (symbolNameLike ErrorCode.InvalidTypeParameterName) |>> fst)

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

/// Parses a function signature.
let private functionSignature =
    let name = expectedId Declaration (symbolLike ErrorCode.InvalidIdentifierName)
    let typeAnnotation = expectedOp colon ?>> qsType
    let genericParamList =
        angleBrackets (sepBy typeParameterDeclaration comma |>> List.tryLast |>> Option.defaultValue Declaration)
    let argumentTuple = expectedOp unitValue <|> buildTuple (name ?>> typeAnnotation)
    name ?>> (opt genericParamList |>> Option.defaultValue NoIdentifier) ?>> argumentTuple ?>> typeAnnotation

/// Parses a function declaration.
let private functionDeclaration =
    let header = expectedId (Keyword fctDeclHeader.id) fctDeclHeader.parse
    header ?>> functionSignature

/// Parses an operation declaration.
let private operationDeclaration =
    let header = expectedId (Keyword opDeclHeader.id) opDeclHeader.parse
    header ?>> functionSignature ?>> characteristicsAnnotation

/// Parses the declaration of a function or operation.
let private callableDeclaration =
    tryAll [
        functionDeclaration
        operationDeclaration
    ]

/// Parses the possibly incomplete fragment text and returns a list of possible identifiers expected at the end of the
/// fragment.
///
/// Only function and operation declaration fragments are currently supported.
let identifierKinds text =
    match runParserOnString callableDeclaration [] "" (text + "\u0004") with
    | Success (result, _, _) -> result
    | Failure (_) -> []
