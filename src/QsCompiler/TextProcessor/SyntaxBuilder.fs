// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// This module is responsible for making sure decent error messages are created for each code fragment. 
/// The actual parsing of all fragments is implemented in the CodeFragmentParsing module, then this module is called to actually build the fragments. 
module Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxBuilder

open System.Collections.Immutable
open FParsec
open Microsoft.CodeAnalysis.CSharp
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.ReservedKeywords
open Microsoft.Quantum.QsCompiler.SyntaxTokens 
open Microsoft.Quantum.QsCompiler.TextProcessing.ParsingPrimitives
open Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxExtensions


// some general purpose utils 

/// returns the current state of the char stream
let getCharStreamState (stream : CharStream<_>) = Reply stream.State
/// given a char stream state, returns the chars between that and the current stream position as string
let getSubstring (start : CharStreamState<_>) = 
    fun (stream : CharStream<_>) -> Reply (stream.ReadFrom (start, false))

/// Given a char stream state and a parser, runs the parser on the substream 
/// between the given char stream state and the current stream position. 
/// NOTE: Anything run on the substream will be processed with the user state of the original stream,
/// and any updates to the user state will be reflected in the original stream.
let runOnSubstream (start : CharStreamState<_>) (parser : Parser<'A,_>) : Parser<'A,_> = 
    let parserAndState = parser .>>. getUserState
    let subparser (stream : CharStream<_>) = 
        let substream = stream.CreateSubstream start 
        substream.UserState <- stream.UserState
        substream |> parserAndState 
    subparser >>= fun (res, ustate) -> setUserState ustate >>% res 

/// Skips ahead until the given target parser succeeds, or the end of the stream is reached.
/// Anything processed by the nonBreakingPieces parser will be skipped as a block. 
let skipPiecesUntil nonBreakingPieces target =
    let pieceDelimiter = (attempt target >>% ()) <|> (nonBreakingPieces >>% ())
    let grabCode = manyCharsTill anyChar (followedBy pieceDelimiter <|> eof)
    sepBy grabCode (notFollowedBy target >>. nonBreakingPieces) >>% ()


// some Q# specific utils

/// Parses any word in the given set as term and returns its range.
/// A word consists of ascii letters and digits only, and is not followed by a symbol continuation. 
/// Fails without consuming input if the parsing fails. 
let private wordContainedIn (strings : ImmutableHashSet<_>) = 
    let inStrings w = if strings.Contains w then preturn w else fail ""
    let keywordLike = manyChars (asciiLetter <|> digit) .>> nextCharSatisfiesNot isSymbolContinuation
    term (keywordLike >>= inStrings) |>> snd

/// parses any QsFragmentHeader and return unit
let internal qsFragmentHeader = previousCharSatisfiesNot isLetter >>. wordContainedIn Keywords.FragmentHeaders
/// parses any QsLanguageKeyword and return unit
let internal qsLanguageKeyword = previousCharSatisfiesNot isLetter >>. wordContainedIn Keywords.LanguageKeywords
/// parses any QsReservedKeyword and return unit
let internal qsReservedKeyword = previousCharSatisfiesNot isLetter >>. wordContainedIn Keywords.ReservedKeywords

/// adds the given diagnostic to the user state
let internal pushDiagnostic newDiagnostic = 
    updateUserState (fun diagnostics -> newDiagnostic :: diagnostics)
/// adds the given diagnostics to the user state
let internal pushDiagnostics newDiagnostics = 
    updateUserState (fun diagnostics -> newDiagnostics @ diagnostics)
/// clears all diagnostics from the user state
let private clearDiagnostics = 
    updateUserState (fun _ -> [])

/// Applies the given body parser and 
/// uses the returned start and end position to generate an error with the given error code.
/// Returns the obtained start and end position. 
let internal buildError body errCode = 
    body >>= fun range -> 
    preturn range |>> QsCompilerDiagnostic.NewError errCode >>= pushDiagnostic
    >>% range

/// Applies the given body parser and 
/// uses the returned start and end position to generate a warning with the given warning code.
/// Returns the obtained start and end position. 
let internal buildWarning body wrnCode = 
    body >>= fun range -> 
    preturn range |>> QsCompilerDiagnostic.NewWarning wrnCode >>= pushDiagnostic 
    >>% range


// bracket handling and related stuff

/// Parses a left tuple bracket "(".
/// IMPORTANT: Does *not* handle whitespace -> use in combination with "bracket" for proper whitespace handling!
let internal lTuple = pstring "("
/// Parses a right tuple bracket ")".
/// IMPORTANT: Does *not* handle whitespace -> use in combination with "bracket" for proper whitespace handling!
let internal rTuple = pstring ")"
/// Parses a left array bracket "[".
/// IMPORTANT: Does *not* handle whitespace -> use in combination with "bracket" for proper whitespace handling!
let internal lArray = pstring "["
/// Parses a right array bracket "]".
/// IMPORTANT: Does *not* handle whitespace -> use in combination with "bracket" for proper whitespace handling!
let internal rArray = pstring "]"
/// Parses a left angle bracket "<".
/// IMPORTANT: Does *not* handle whitespace -> use in combination with "bracket" for proper whitespace handling!
let internal lAngle = pstring "<"
/// Parses a right angle bracket ">".
/// IMPORTANT: Does *not* handle whitespace -> use in combination with "bracket" for proper whitespace handling!
let internal rAngle = pstring ">"
/// Parses a left curly bracket "{".
/// IMPORTANT: Does *not* handle whitespace -> use in combination with "bracket" for proper whitespace handling!
let internal lCurly = pstring "{"
/// Parses a right curly bracket "}".
/// IMPORTANT: Does *not* handle whitespace -> use in combination with "bracket" for proper whitespace handling!
let internal rCurly = pstring "}"

/// parser used for properly processing single brackets (handles the whitespace management)
let internal bracket p = term p >>% ()


let (private stringContent, private stringContentImpl) = createParserForwardedToRef()

/// Parses the given core parser with de-facto optional left and right bracket around it.
/// However, raises the given lbracketErr or rbracketErr if the left, respectively right, bracket are missing. 
/// Fails without consuming input it the parsing fails. 
/// IMPORTANT: This parser does does *not* handle whitespace and needs to be wrapped into a term parser for proper processing of all whitespace.
let private contentDefinedBrackets core (lbracket, rbracket) (lbracketErr, rbracketErr) = 
    let missingLeftAndCore = 
        getPosition .>>. getPosition |>> QsCompilerDiagnostic.NewError lbracketErr // only push after core succeeded
        .>>. core >>= fun (err, res) -> pushDiagnostic err >>% res
    let missingRight = 
        getPosition .>>. getPosition |>> QsCompilerDiagnostic.NewError rbracketErr 
        >>= pushDiagnostic
    
    let leftAndCore = attempt (bracket lbracket >>. core) <|> missingLeftAndCore
    let closingRight = attempt rbracket >>% () <|> missingRight // rbracket only, to get correct position info upon applying term
    attempt (leftAndCore .>> closingRight)

/// Parses the given core parser with de-facto optional tuple brackets around it.
/// However, raises the corresponding error if the left and/or right bracket are missing.
/// Returns the parsed value, as well as a tuple with the start and end position of the (potentially tuple-wrapped) core parser.
/// Fails without consuming input it the parsing fails. 
let internal optTupleBrackets core = 
    contentDefinedBrackets core (lTuple, rTuple) (ErrorCode.MissingLTupleBracket, ErrorCode.MissingRTupleBracket) |> term

/// Given a lbracket and matching rbracket parser, parses the lbracket then looks for the matching rbracket 
/// (recursively such that potential inner brackets are matched properly as well), and then applies the given core parser 
/// only on the substream between the two matching brackets. 
/// Fails without consuming input it the parsing fails. 
/// IMPORTANT: This parser does does *not* handle whitespace and needs to be wrapped into a term parser for proper processing of all whitespace.
let private bracketDefinedContent core (lbracket, rbracket) =
    let nextRbracket = attempt (skipPiecesUntil stringContent (lbracket <|> rbracket) .>> followedBy rbracket)
    let nextLbracket = attempt (skipPiecesUntil stringContent (lbracket <|> rbracket) .>> followedBy lbracket)
    let rec findMatching stream = 
        let recur = nextLbracket >>. bracket lbracket >>. findMatching .>> bracket rbracket
        stream |> (many recur >>. nextRbracket)
    let processCore = 
        getCharStreamState >>= fun state -> 
        findMatching >>. ((core .>> followedBy eof) |> runOnSubstream state)
    attempt (bracket lbracket >>. processCore .>> rbracket) // rbracket only so that the position info given by term is accurate 

/// Starting with a left tuple bracket, extracts the matching right tuple bracket and then applies the given core parser to the middle part.
/// Fails without consuming input it the parsing fails. 
let internal tupleBrackets core = ((lTuple, rTuple) |> bracketDefinedContent core) |> term
/// Starting with a left array bracket, extracts the matching right array bracket and then applies the given core parser to the middle part.
/// Fails without consuming input it the parsing fails. 
let internal arrayBrackets core = ((lArray, rArray) |> bracketDefinedContent core) |> term
/// Starting with a left angle bracket, extracts the matching right angle bracket and then applies the given core parser to the middle part.
/// Fails without consuming input it the parsing fails. 
let internal angleBrackets core = ((lAngle, rAngle) |> bracketDefinedContent core) |> term
/// Starting with a left curly bracket, extracts the matching right curly bracket and then applies the given core parser to the middle part.
/// Fails without consuming input it the parsing fails. 
let internal curlyBrackets core = ((lCurly, rCurly) |> bracketDefinedContent core) |> term

/// Parses a string with or without interpolation. Returns the parsed string.
/// Parses the interpolation arguments with the given parser.
/// Fails without consuming input if neither a string with or without interpolation can be successfully parsed. 
/// IMPORTANT: Parses *precicely* the string literal and does *not* handle whitespace! 
let internal getStringContent interpolArg = 
    let notPrecededBySlash = previousCharSatisfiesNot (fun c -> c.Equals '\\')
    let startDelimiter = pstring "\""
    let endDelimiter = notPrecededBySlash >>. startDelimiter
    let interpolatedString = 
        let contentDelimiter = endDelimiter <|> (notPrecededBySlash >>. lCurly)
        let nonInterpol = manyChars (notFollowedBy contentDelimiter >>. anyChar)
        let interpol = (lCurly, rCurly) |> bracketDefinedContent interpolArg 
        let content = nonInterpol .>>. many (interpol .>>. nonInterpol)
        pchar '$' >>. startDelimiter >>. content .>> endDelimiter |>> fun (h, items) -> 
            let mutable str = h 
            items |> List.map snd |> List.iteri (fun i part -> str <- sprintf "%s{%i}%s" str i part)
            str, items |> List.map fst
    let nonInterpolatedString = 
        let content = manyChars (notFollowedBy endDelimiter >>. anyChar)
        startDelimiter >>. content .>> endDelimiter |>> fun str -> (str, [])
    attempt interpolatedString <|> attempt nonInterpolatedString

do stringContentImpl := getStringContent (manyChars anyChar) >>% ()

/// Skips ahead until the given target parser succeeds, or the end of the stream is reached.
/// Skips strings, tuple brackets (and their content), array brackets (and their content), and curly brackets (and their content) as a block - 
/// i.e. never breaks apart strings or matching brackets with the exception of angle brackets that may occur separately. 
/// NOTE: Does *not* process whitespace, such that it is possible to advance to the next whitespace character. 
let internal advanceTo target = 
    let grabAny = manyCharsTill anyChar eof
    let nonBreakingPieces = 
        choice[ // leaving whitespace to the right such that it is possible to advance to whitespace
            stringContent                                     >>% ()
            (lTuple, rTuple) |> bracketDefinedContent grabAny >>% ()
            (lArray, rArray) |> bracketDefinedContent grabAny >>% ()
            (lCurly, rCurly) |> bracketDefinedContent grabAny >>% ()
        ] // langle and rangle may occur separately, whereas the left and right part of the listed brackets above may not
    skipPiecesUntil nonBreakingPieces target

/// Implements a hack to get around limitations for processing left recursions:
/// First skips ahead until the given breakingDelimiter parser succeeds or the end of the stream is reached, 
/// then applies the given before parser to the skipped substream, 
/// and finally applies the breakingDelimiter followed by the given after parser to the current stream position. 
/// Fails without consuming input it the parsing fails. 
/// NOTE: This routine is only suitable for processing short streams, 
/// since it will advance all the way to the end of the stream (and backtrack) if it does not find the breakingDelimiter!
let leftRecursionByInfix breakingDelimiter before after = // before and after breaking delimiter
    let advanceToInfix = advanceTo (breakingDelimiter >>% () <|> eof) >>. followedBy breakingDelimiter
    getCharStreamState >>= fun state -> 
    attempt (advanceToInfix >>. (before .>> followedBy eof |> runOnSubstream state) .>> breakingDelimiter .>>. after)


// routines that dictate how parsing is handled accross all fragments and expression

/// Consumes whitespace characters until the given parser succeeds.
/// Does *not* apply the given parser, and the given parser is guaranteed to succeed if this parser succeeds.
/// Returns an empty range at the beginning of the skipped whitespace sequence.
/// Fails without consuming input if the parsing fails.
let internal followedByCode p = 
    getPosition .>>. getPosition .>>
    attempt (manyCharsTill (satisfy Text.IsWhitespace) (followedBy p))

/// Advances until the given parser succeeds, or the end of the input stream is reached.
/// Returns the start and end position of the skipped non-whitespace code. 
let internal skipInvalidUntil p = 
    let getRange core = getPosition .>> core .>>. getPosition
    advanceTo (followedByCode p) |> getRange // range of the invalid (non-whitespace) code
    .>> (followedByCode p >>% () <|> eof) 

/// Attempts to parse the given body. If that fails, returns the given fallback and 
/// generates an error with the given missingCode if continuation succeeds at the current position. 
/// Advances to the given continuation *or the next QsFragmentHeader, or the end of the stream* otherwise, 
/// while generating an error with the given errCode, and returning the given fallback. 
let internal expected body errCode missingCode fallback continuation = 
    let invalid = 
        let nextExpected = (attempt continuation >>% ()) <|> (qsFragmentHeader >>% ())
        let invalid = buildError (skipInvalidUntil nextExpected) errCode
        let missing = buildError (followedByCode nextExpected) missingCode 
        (attempt missing <|> invalid) >>% fallback
    attempt body <|> invalid

/// Fails if nextExpected succeeds at the current position.
/// If nextExpected fails at the current position, advances to nextExpected, 
/// generating an error with the given error code, and returns fallback.
/// Fails without consuming input if the parsing fails. 
let internal checkForInvalid nextExpected errCode =
    notFollowedBy nextExpected >>. buildError (skipInvalidUntil nextExpected) errCode 

/// Applied the given parser, and skips ahead until nextExpected succeeds or the end of the input stream is reached. 
/// Generates an ExcessContinuation error for the skipped piece if it is non-empty. 
/// Returns the content generated by the given parser. 
let internal withExcessContinuation nextExpected orig = 
    let consumeExcess = buildError (skipInvalidUntil nextExpected) ErrorCode.ExcessContinuation >>% ()
    orig .>> (followedBy nextExpected <|> consumeExcess)

/// Tries to apply the comma parser and optionally returns its content. 
/// If a comma has been consumed, generates an ExcessComma warning at the current position. 
let internal warnOnComma =  
    getPosition .>>. getPosition .>> comma 
    |>> QsCompilerDiagnostic.NewWarning WarningCode.ExcessComma >>= pushDiagnostic
    |> opt

/// Parses a comma separated sequence of items, expecting at least one item, and returns the parsed items as a list. 
/// Generates an error with the given missingCode if nothing (but whitespace) precedes the next comma, 
/// inserting the given fallback at that position in the list.
/// If there is non-whitespace text preceding the next comma but validItem does not succeed, 
/// advances until the given delimiter or comma succeeds, or the end of the stream is reached, and 
/// generates an error with the given errorCode, while inserting the given fallback at that position in the list. 
/// If validItem succeeds after a comma, but is not followed by the next comma or the given delimiter, 
/// advances until comma or the given delimiter succeeds, generating an ExcessContinuationError for the skipped piece. 
/// Generates am ExcessComma warning if the there is no validItem following the last comma, but the given delimiter succeeds. 
let internal commaSep1 validItem errCode missingCode fallback delimiter =
    let delimiter = (delimiter >>% ()) <|> (comma >>% ()) 
    let item = expected validItem errCode missingCode fallback delimiter
    let invalidLast = checkForInvalid delimiter errCode >>% fallback
    let piece = (item .>>? followedBy comma) <|> attempt validItem <|> invalidLast
    sepBy1 (piece |> withExcessContinuation delimiter) (comma .>>? followedBy piece) .>> warnOnComma
    |>> fun x -> x.ToImmutableArray()

/// parser succeeds without consuming input or changing the parser state if the next char is a comma, 
/// a right tuple bracket, or if we are at the end of the input stream
let internal isTupleContinuation = followedBy (comma <|> rTuple <|> (eof >>% ""))

/// Builds a nested non-empty tuple using bundle from a list of comma separated validSingle items. 
/// Uses commaSep1 to generate suitable errors for missing or invalid items, replacing them with fallback if needed. 
/// NOTE: This parser behaves along the lines of "delimiter defined content" - 
/// i.e. it will assume that an item is invalid and advance to the next tuple continuation (comma, closing bracket or end of input stream) 
/// in cases where validSingle or the corresponding tuple parser does not succeed. 
let internal buildTuple validSingle bundle errCode missingCode fallback = // "bracketDefinedContent"
    let rec tuple stream = 
        let inner = commaSep1 (validSingle <|> tuple) errCode missingCode fallback isTupleContinuation // for cases where we have singleton tuples and operators possibly connecting them, single needs to be first!
        (tupleBrackets inner |>> bundle) stream // don't allow empty tuples -> something like unit must be incorporated in single
    tuple 

/// Either parses validSingle, or builds a nested non-empty tuple using bundle from a list of comma separated validSingle items. 
/// Uses commaSep1 to generate suitable errors for missing or invalid tuple items, replacing them with fallback if needed. 
/// If both validSingle and the tuple parser fail, parses an expected validSingle to generate suitable errors while advancing to the given continuation.
/// NOTE: The tuple parser behaves along the lines of "delimiter defined content" - 
/// i.e. it will assume that an item is invalid and advance to the next tuple continuation (comma, closing bracket or end of input stream) 
/// in cases where validSingle or the corresponding tuple parser does not succeed. 
let internal buildTupleItem validSingle bundle errCode missingCode fallback continuation = 
    validSingle <|> buildTuple validSingle bundle errCode missingCode fallback 
    <|> expected validSingle errCode missingCode fallback continuation


// routines related to permissive symbol and name parsing

/// Parses a symbol name - i.e. any identifier that starts with an ascii letter and continues with isSymbolContinuation, 
/// and is not a reserved keyword in Q#. Fails without consuming input if qsReservedKeyword succeeds at the current position.
/// Returns None and logs an InvalidUseOfReservedKeyword error if the parsed symbol name is reserved. 
/// Returns None and logs an error with the given error code if the parsed symbol name is not valid for other reasons.
/// Returns the parsed symbol name as Some otherwise. 
/// IMPORTANT: this routines handles the name *only* and does *not* handle whitespace! 
/// In order to guarantee correct whitespace management, the name needs to be parsed as a term.
let internal symbolNameLike errCode = 
    let identifier = 
        let id = IdentifierOptions(isAsciiIdStart = isSymbolStart, isAsciiIdContinue = isSymbolContinuation) |> identifier
        getPosition .>>. id .>>. getPosition |>> fun ((p1, name), p2) -> name, (p1,p2)
    let whenValid (name : string, range) = 
        let isReserved = name.StartsWith "__" && name.EndsWith "__" || InternalUse.CsKeywords.Contains name
        let isCsKeyword = SyntaxFacts.IsKeywordKind (SyntaxFacts.GetKeywordKind name)
        let moreThanUnderscores = name.TrimStart('_').Length <> 0
        if isCsKeyword || isReserved then buildError (preturn range) ErrorCode.InvalidUseOfReservedKeyword >>% None
        else if moreThanUnderscores then preturn name |>> Some
        else buildError (preturn range) errCode >>% None
    let invalid = 
        let invalidName = pchar '\'' |> opt >>. manySatisfy isDigit >>. identifier 
        buildError (getPosition .>> invalidName .>>. getPosition) errCode >>% None
    notFollowedBy qsReservedKeyword >>. attempt (identifier >>= whenValid <|> invalid) // NOTE: *needs* to fail on reserverd keywords here!

/// Handles permissive parsing of a symbol:
/// Uses symbolNameLike to generate suitable errors if the current symbol-like text is not a valid symbol in Q#
/// and returns the QsSymbol corresponding to an invalid symbol.
/// Returned the parsed (unqualified) Symbol as QsSymbol otherwise. 
let internal symbolLike errCode = 
    term (symbolNameLike errCode) |>> function 
        | Some sym, range -> (Symbol (NonNullable<string>.New sym), range) |> QsSymbol.New
        | None, _ -> (InvalidSymbol, Null) |> QsSymbol.New
        
/// Given the path, the symbol and the range parsed by multiSegmentSymbol, 
/// returns a simple Symbol as QsSymbol if the given path is empty, 
/// or a QualifiedSymbol as QsSymbol if the given path is non-empty and valid. 
/// Returns a QsSymbol corresponding to an invalid symbol if the path contains segments that are None (i.e. invalid).
let internal asQualifiedSymbol ((path, sym), range : Position * Position) = 
    let names = [for segment in path do yield segment ] @ [sym]
    if names |> List.contains None then (InvalidSymbol, Null) |> QsSymbol.New
    else 
        names |> List.choose id |> function 
        | [sym] -> (Symbol (NonNullable<string>.New sym), range) |> QsSymbol.New
        | parts -> 
            let (ns, sym) = (String.concat "." parts.[0..parts.Length-2]) |> NonNullable<string>.New, parts.[parts.Length-1] |> NonNullable<string>.New
            (QualifiedSymbol (ns, sym), range) |> QsSymbol.New

/// Given the path, the symbol and the range parsed by multiSegmentSymbol, 
/// concatenates all path segments and the symbol with a dot, and returns a simple Symbol of the concatenated string as QsSymbol. 
/// Returns a QsSymbol corresponding to an invalid symbol if the path contains segments that are None (i.e. invalid).
let internal asSymbol ((path, sym), range : Position * Position) = 
    let names = [for segment in path do yield segment ] @ [sym]
    if names |> List.contains None then (InvalidSymbol, Null) |> QsSymbol.New
    else (names |> List.choose id |> String.concat "." |> NonNullable<string>.New |> Symbol, range) |> QsSymbol.New

/// Handles permissive parsing of a qualified symbol:
/// Uses symbolNameLike for each path fragment separated by dots 
/// to generate suitable errors if the symbol-like fragment text is not a valid symbol in Q#.
/// Returns the QsSymbol corresponding to an invalid symbol if any of the path fragments is not a valid symbol.
/// Returned the parsed strings as a QsSymbol of kind QualifiedSymbol otherwise. 
let internal multiSegmentSymbol errCode = 
    let singleDot = pchar '.' .>>? notFollowedBy (pchar '.')
    let pathSegment = symbolNameLike ErrorCode.InvalidPathSegment .>>? followedBy singleDot
    let localIdentifier = symbolNameLike errCode
    let expectedIdentifier = localIdentifier <|> (buildError (term (preturn ()) |>> snd) ErrorCode.MissingIdentifer >>% None)
    let qualifiedIdentifer = (sepEndBy1 pathSegment singleDot .>>. expectedIdentifier) <|> (preturn [] .>>. localIdentifier)
    term qualifiedIdentifer

/// Parses a Q# type parameter name - i.e. a tick (') followed by a symbol name. 
/// Fails without consuming input if the parsing fails.
/// Uses symbolNameLike to generate suitable errors if the symbol-like text follwing a tick (') is not a valid symbol name in Q#, 
/// and returns None in that case. Returns the parsed type parameter name without the preceding tick as string otherwise.
/// IMPORTANT: this routines handles the name *only* and does *not* handle whitespace! 
/// In order to guarantee correct whitespace management, the name needs to be parsed within as a term.
let internal typeParameterNameLike = 
    pchar '\'' >>? symbolNameLike ErrorCode.InvalidTypeParameterName

/// Handles permissive parsing of a Q# type parameter name:
/// Uses typeParameterNameLike to generate suitable errors if the current symbol-like text is not a valid type parameter name in Q#
/// and returns the QsType corresponding to an invalid type.
/// Returned the parsed TypeParameter as QsType otherwise. 
let internal typeParameterLike = 
    term typeParameterNameLike |>> function 
        | Some sym, range -> ((NonNullable<string>.New sym |> Symbol, range) |> QsSymbol.New |> TypeParameter, range) |> QsType.New
        | None, _ -> (InvalidType, Null) |> QsType.New 


// building an abstract representation of Q# code fragments

/// Filters out any errors that are not worth raising due to their overlap with, or them being q consequence of other existing errors.
/// Returns diagnostics with updated ranges such that the start and end position of each diagnostic is at most the given endPos.
let private filterAndAdapt (diagnostics : QsCompilerDiagnostic list) endPos = 
    // opting to only actually raise ExcessContinuation errors if no other errors overlap with them
    let excessCont, remainingDiagnostics = 
        diagnostics |> List.partition (fun x -> x.Diagnostic |> function | Error (ErrorCode.ExcessContinuation) -> true | _ -> false)
    let remainingErrs = remainingDiagnostics |> List.filter (fun x -> x.Diagnostic |> function | Error _ -> true | _ -> false)
    let hasOverlap (x : QsCompilerDiagnostic) = remainingErrs |> List.exists (fun y -> fst y.Range <= fst x.Range && snd y.Range >= fst x.Range) 
    let filteredExcessCont = excessCont |> List.filter (not << hasOverlap)

    let rangeWithinFragment range = 
        let min (a,b) = if a < b then a else b
        (min (endPos, fst range), min(endPos, snd range))
    filteredExcessCont @ remainingDiagnostics
    |> List.map (fun diagnostic -> diagnostic.WithRange(rangeWithinFragment diagnostic.Range))

/// Constructs a QsCodeFragment.
/// If the given header succeeds, attempts the given body parser to obtain the argument to construct the QsFragment using the given
/// fragmentKind constructor, or defaults to the given invalid fragment. 
/// Generates a suitable error if the body parser fails. 
/// Determines the Range and Text for the fragment and attaches all current diagnostics saved in the user state to the QsFragment. 
/// Upon fragment construction, clears all diagnostics currently stored in the UserState.
let internal buildFragment header body (invalid : QsFragmentKind) (fragmentKind : 'a -> QsFragmentKind) =
    let build (kind, (startPos, (text, endPos))) = 
        getUserState .>> clearDiagnostics 
        |>> fun diagnostics -> 
            QsFragment.New(kind, (startPos, endPos), (filterAndAdapt diagnostics (endPos |> QsPositionInfo.New)).ToImmutableArray(), NonNullable<string>.New text)

    let buildDiagnostic (errPos, (startPos, (text, endPos))) = 
        let errPos = if endPos < errPos then endPos else errPos
        QsCompilerDiagnostic.NewError invalid.ErrorCode (errPos, endPos) |> pushDiagnostic >>. 
        preturn (invalid, (startPos, (text, endPos))) 

    let delimiters state = 
        let fragmentEnd = 
            let allWS = emptySpace .>>? eof
            manyCharsTill anyChar (followedBy allWS) .>>. getPosition
        (getPosition .>>. fragmentEnd) |> runOnSubstream state
    
    let validBody state = 
        let processExcessCode = buildError (skipInvalidUntil qsFragmentHeader) ErrorCode.ExcessContinuation >>% ()
        (body .>>? followedBy (qsFragmentHeader >>% () <|> eof)) <|> (body .>> processExcessCode)
        |>> fragmentKind .>>. delimiters state
    let invalidBody state = getPosition .>> (advanceTo qsFragmentHeader) .>>. delimiters state >>= buildDiagnostic

    getCharStreamState >>= fun state -> 
        header >>. (attempt (validBody state) <|> invalidBody state) >>= build




