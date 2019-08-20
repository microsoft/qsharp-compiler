// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// this module is *not* supposed to hardcode any qs keywords (i.e. there should not be any need for strings in here) - use the keywords module for that!
module Microsoft.Quantum.QsCompiler.TextProcessing.ExpressionParsing

open System.Collections.Immutable
open System.Globalization
open System.Numerics
open FParsec
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxTokens 
open Microsoft.Quantum.QsCompiler.TextProcessing.Keywords
open Microsoft.Quantum.QsCompiler.TextProcessing.ParsingPrimitives
open Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxBuilder
open Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.TextProcessing.TypeParsing


// operator precedence parsers for expressions and call arguments

/// returns a QsExpression representing an invalid expression (i.e. syntax error on parsing)
let internal unknownExpr = (InvalidExpr, Null) |> QsExpression.New

/// For an expression of the given kind that is built from a left and right hand expression with the given ranges,
/// builds the corresponding expression with its range set to the combined range. 
/// If either one of given ranges is Null, builds an invalid expression with its range set to Null. 
let private buildCombinedExpr kind (lRange, rRange) = 
    QsPositionInfo.WithCombinedRange (lRange, rRange) kind InvalidExpr |> QsExpression.New // *needs* to be an invalid expression if the combined range is Null!

/// operator precedence parser for Q# expressions
let private qsExpression = new OperatorPrecedenceParser<QsExpression,_,_>()
/// operator precedence parser for Q# call arguments 
/// processing all expressions handled by the Q# expression parser as well as omitted arguments
let private qsArgument = new OperatorPrecedenceParser<QsExpression,_,_>()

let private applyUnary operator _ (ex : QsExpression) = (operator ex, ex.Range) |> QsExpression.New // todo: not entirely correct range info, but for now will do
let internal applyBinary operator _ (left : QsExpression) (right : QsExpression) = 
    buildCombinedExpr (operator (left, right)) (left.Range, right.Range)
let internal applyTerinary operator (first : QsExpression) (second : QsExpression) (third : QsExpression) = 
    let buildOp (left, right) = operator (left, second, right)
    applyBinary buildOp () first third

let private deprecatedOp warning (parsedOp : string) =
    let preceedingRange (pos : Position) =
        let opLength = int64 (parsedOp.Length)
        let minusOldOpLength value = if value < opLength then 0L else value - opLength // value here should never be 0 or less (just a precaution)
        let preceedingPos = new Position(pos.StreamName, pos.Index |> minusOldOpLength, pos.Line, pos.Column |> minusOldOpLength)
        (preceedingPos, pos)
    buildWarning (getPosition |>> preceedingRange) warning

qsExpression.AddOperator (TernaryOperator(qsCopyAndUpdateOp.op, emptySpace, qsCopyAndUpdateOp.cont, emptySpace                        , qsCopyAndUpdateOp.prec, qsCopyAndUpdateOp.Associativity,     applyTerinary CopyAndUpdate))
qsExpression.AddOperator (TernaryOperator(qsConditionalOp.op  , emptySpace, qsConditionalOp.cont, emptySpace                          , qsConditionalOp.prec  , qsConditionalOp.Associativity  ,     applyTerinary CONDITIONAL  ))
qsExpression.AddOperator (InfixOperator  (qsRangeOp.op        , emptySpace                                                            , qsRangeOp.prec        , qsRangeOp.Associativity        , (), applyBinary   RangeLiteral ))
qsExpression.AddOperator (InfixOperator  (qsORop.op           , emptySpace                                                            , qsORop.prec           , qsORop.Associativity           , (), applyBinary   OR           ))
qsExpression.AddOperator (InfixOperator  (qsANDop.op          , emptySpace                                                            , qsANDop.prec          , qsANDop.Associativity          , (), applyBinary   AND          ))
qsExpression.AddOperator (InfixOperator  (qsBORop.op          , emptySpace                                                            , qsBORop.prec          , qsBORop.Associativity          , (), applyBinary   BOR          ))
qsExpression.AddOperator (InfixOperator  (qsBXORop.op         , emptySpace                                                            , qsBXORop.prec         , qsBXORop.Associativity         , (), applyBinary   BXOR         ))
qsExpression.AddOperator (InfixOperator  (qsBANDop.op         , emptySpace                                                            , qsBANDop.prec         , qsBANDop.Associativity         , (), applyBinary   BAND         ))
qsExpression.AddOperator (InfixOperator  (qsEQop.op           , emptySpace                                                            , qsEQop.prec           , qsEQop.Associativity           , (), applyBinary   EQ           ))
qsExpression.AddOperator (InfixOperator  (qsNEQop.op          , emptySpace                                                            , qsNEQop.prec          , qsNEQop.Associativity          , (), applyBinary   NEQ          ))
qsExpression.AddOperator (InfixOperator  (qsLTEop.op          , emptySpace                                                            , qsLTEop.prec          , qsLTEop.Associativity          , (), applyBinary   LTE          ))
qsExpression.AddOperator (InfixOperator  (qsGTEop.op          , emptySpace                                                            , qsGTEop.prec          , qsGTEop.Associativity          , (), applyBinary   GTE          ))
qsExpression.AddOperator (InfixOperator  (qsLTop.op           , notFollowedBy (pchar '-') >>. emptySpace                              , qsLTop.prec           , qsLTop.Associativity           , (), applyBinary   LT           ))
qsExpression.AddOperator (InfixOperator  (qsGTop.op           , emptySpace                                                            , qsGTop.prec           , qsGTop.Associativity           , (), applyBinary   GT           ))
qsExpression.AddOperator (InfixOperator  (qsRSHIFTop.op       , emptySpace                                                            , qsRSHIFTop.prec       , qsRSHIFTop.Associativity       , (), applyBinary   RSHIFT       ))
qsExpression.AddOperator (InfixOperator  (qsLSHIFTop.op       , emptySpace                                                            , qsLSHIFTop.prec       , qsLSHIFTop.Associativity       , (), applyBinary   LSHIFT       ))
qsExpression.AddOperator (InfixOperator  (qsADDop.op          , emptySpace                                                            , qsADDop.prec          , qsADDop.Associativity          , (), applyBinary   ADD          ))
qsExpression.AddOperator (InfixOperator  (qsSUBop.op          , emptySpace                                                            , qsSUBop.prec          , qsSUBop.Associativity          , (), applyBinary   SUB          ))
qsExpression.AddOperator (InfixOperator  (qsMULop.op          , emptySpace                                                            , qsMULop.prec          , qsMULop.Associativity          , (), applyBinary   MUL          ))
qsExpression.AddOperator (InfixOperator  (qsMODop.op          , emptySpace                                                            , qsMODop.prec          , qsMODop.Associativity          , (), applyBinary   MOD          ))
qsExpression.AddOperator (InfixOperator  (qsDIVop.op          , notFollowedBy (pchar '/') >>. emptySpace                              , qsDIVop.prec          , qsDIVop.Associativity          , (), applyBinary   DIV          ))
qsExpression.AddOperator (InfixOperator  (qsPOWop.op          , emptySpace                                                            , qsPOWop.prec          , qsPOWop.Associativity          , (), applyBinary   POW          ))
qsExpression.AddOperator (PrefixOperator (qsBNOTop.op         , emptySpace                                                            , qsBNOTop.prec         , qsBNOTop.isLeftAssociative     , (), applyUnary    BNOT         ))
qsExpression.AddOperator (PrefixOperator (qsNOTop.op          , notFollowedBy (many1Satisfy isSymbolContinuation) >>. emptySpace      , qsNOTop.prec          , qsNOTop.isLeftAssociative      , (), applyUnary    NOT          ))
qsExpression.AddOperator (PrefixOperator (qsNEGop.op          , emptySpace                                                            , qsNEGop.prec          , qsNEGop.isLeftAssociative      , (), applyUnary    NEG          ))
qsExpression.AddOperator (PrefixOperator ("!"                 , "!"  |> deprecatedOp WarningCode.DeprecatedNOToperator >>. emptySpace , qsNOTop.prec          , qsNOTop.isLeftAssociative      , (), applyUnary    NOT          ))
qsExpression.AddOperator (InfixOperator  ("||"                , "||" |> deprecatedOp WarningCode.DeprecatedORoperator  >>. emptySpace , qsORop.prec           , qsORop.Associativity           , (), applyBinary   OR           ))
qsExpression.AddOperator (InfixOperator  ("&&"                , "&&" |> deprecatedOp WarningCode.DeprecatedANDoperator >>. emptySpace , qsANDop.prec          , qsANDop.Associativity          , (), applyBinary   AND          ))

for op in qsExpression.Operators do qsArgument.AddOperator op


// processing modifiers (functor application and unwrap directives)
// -> modifiers basically act as unary operators with infinite precedence that can only be applied to certain expressions

/// Given an expression which (potentially) supports the application of modifiers, 
/// processes the expression and all its leading and trailing modifiers, applies all modifiers, and builds the corresponding Q# expression.
/// Expression modifiers are functor application and/or unwrap directives. 
/// All trailing modifiers (unwrap directives) take precedence over all leading ones (functor applications). 
/// Trailing modifiers are left-associative, and leading modifier are right-associative. 
let private withModifiers modifiableExpr = 
    let rec applyUnwraps unwraps (core : QsExpression) = unwraps |> function 
        | [] -> core
        | range :: tail -> buildCombinedExpr (UnwrapApplication core) (core.Range, range) |> applyUnwraps tail
    let unwrapOperator = term (pstring qsUnwrapModifier.op .>> notFollowedBy (pchar '=')) |>> snd |>> QsPositionInfo.Range 
    let rec applyFunctors functors (core : QsExpression) = functors |> function
        | [] -> core 
        | (range, kind) :: tail -> buildCombinedExpr (kind core) (QsPositionInfo.Range range, core.Range) |> applyFunctors tail
    let functorApplication = 
        let adjointApplication = qsAdjointFunctor.parse .>>. preturn AdjointApplication
        let controlledApplication = qsControlledFunctor.parse .>>. preturn ControlledApplication
        adjointApplication <|> controlledApplication
    attempt (many functorApplication .>>. modifiableExpr) .>>. many unwrapOperator // NOTE: do *not* replace by an expected expression even if there are preceeding functors!
    |>> fun ((functors, ex), unwraps) -> applyUnwraps unwraps ex |> applyFunctors (functors |> List.rev)


// utils for building expressions

/// Parses for a Q# call argument. 
/// Fails on fragment headers, and raises an error for other Q# language keywords returning an invalid expression. 
let private argument = 
    let keyword = buildError qsLanguageKeyword ErrorCode.InvalidKeywordWithinExpression >>% unknownExpr 
    notFollowedBy qsFragmentHeader >>. (keyword <|> qsArgument.ExpressionParser) // keyword *needs* to be first here!     

/// Parses for an arbitrary Q# expression. 
/// Fails on fragment headers, and raises an error for other Q# language keywords returning an invalid expression. 
let internal expr = 
    let keyword = buildError qsLanguageKeyword ErrorCode.InvalidKeywordWithinExpression >>% unknownExpr 
    notFollowedBy qsFragmentHeader >>. (keyword <|> qsExpression.ExpressionParser) // keyword *needs* to be first here! 

/// Given a continuation (parser), attempts to parse a Q# expression, 
/// returning the parsed Q# expression, or a Q# expression representing an invalid expression (parsing failure) if the parsing fails.
/// On failure, either raises a MissingExpression error if the given continuation succeeds at the current position, 
/// or raises an InvalidExpression error and advances until the given continuation succeeds otherwise. 
/// Does not apply the given continuation. 
let internal expectedExpr continuation = 
    expected expr ErrorCode.InvalidExpression ErrorCode.MissingExpression unknownExpr continuation 

/// Given a core parser returning a QsExpressionKind, parses it as term an builds a QsExpression.
/// In particular, takes care of the whitespace management and determines a suitable range for the expression.
let private asExpression core = 
    term core |>> QsExpression.New

let private buildExpression ex (range : Position * Position) = 
    (ex, range) |> QsExpression.New


// Qs literals

/// Parses a Q# pauli literal as QsExpression. 
let private pauliLiteral = 
    choice [ 
        qsPauliX.parse |>> buildExpression (PauliX |> PauliLiteral)
        qsPauliY.parse |>> buildExpression (PauliY |> PauliLiteral)
        qsPauliZ.parse |>> buildExpression (PauliZ |> PauliLiteral)
        qsPauliI.parse |>> buildExpression (PauliI |> PauliLiteral)
    ]

/// Parses a Q# result literal as QsExpression. 
let private resultLiteral = 
    choice [
        qsZero.parse |>> buildExpression (Zero |> ResultLiteral)
        qsOne.parse  |>> buildExpression (One  |> ResultLiteral)
    ]

/// Parses a Q# boolean literal as QsExpression. 
let private boolLiteral = 
    choice [
        qsTrue.parse  |>> buildExpression (true  |> BoolLiteral)
        qsFalse.parse |>> buildExpression (false |> BoolLiteral)
    ]

/// Parses a Q# int or double literal as QsExpression. 
let internal numericLiteral =
    let verifyAndBuild (nl : NumberLiteral, range) = 
        let format = 
            if   nl.IsBinary      then 2
            elif nl.IsOctal       then 8 
            elif nl.IsDecimal     then 10 
            elif nl.IsHexadecimal then 16
            else 0
        let str = 
            let trimmed = nl.String.TrimStart '+'
            if format = 10 || format = 0 then trimmed else trimmed.Substring 2 |> sprintf "0%s" // leading 0 is required to keep numbers positive
        let isInt    = nl.IsInteger     && format <> 0                  && nl.SuffixLength = 0 // any format is fine here
        let isBigInt = nl.IsInteger     && (format = 10 || format = 16) && nl.SuffixLength = 1 && System.Char.ToUpperInvariant(nl.SuffixChar1) = 'L'
        let isDouble = not nl.IsInteger && format = 10                  && nl.SuffixLength = 0 
        let returnWithRange kind = preturn (kind, range)
        try if isInt then 
                let value = System.Convert.ToUInt64 (str, format) // convert to uint64 to allow proper handling of Int64.MinValue
                if value = uint64(-System.Int64.MinValue) then System.Int64.MinValue |> IntLiteral |> preturn |> asExpression >>= (NEG >> returnWithRange)
                elif value > (uint64)System.Int64.MaxValue then // needs to be after the first check above
                    buildError (preturn range) ErrorCode.IntOverflow >>. returnWithRange ((int64)value |> IntLiteral)                
                else (int64)value |> IntLiteral |> returnWithRange
            elif isBigInt then 
                if format = 16 then BigInteger.Parse(str, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture) |> BigIntLiteral |> returnWithRange
                else BigInteger.Parse (nl.String, CultureInfo.InvariantCulture) |> BigIntLiteral |> returnWithRange
            elif isDouble then 
                try System.Convert.ToDouble (nl.String, CultureInfo.InvariantCulture) |> DoubleLiteral |> returnWithRange
                with | :? System.OverflowException -> buildError (preturn range) ErrorCode.DoubleOverflow >>. returnWithRange (System.Double.NaN |> DoubleLiteral)
            else fail "invalid number format"
        with | _ -> fail "failed to initialize number literal"         
    let format =  // not allowed are: infinity, NaN
        NumberLiteralOptions.AllowPlusSign + // minus signs are processed by the corresponding unary operator
        NumberLiteralOptions.AllowBinary + NumberLiteralOptions.AllowOctal + NumberLiteralOptions.AllowHexadecimal +
        NumberLiteralOptions.AllowExponent + NumberLiteralOptions.AllowFraction + NumberLiteralOptions.AllowFractionWOIntegerPart + 
        NumberLiteralOptions.AllowSuffix
    let literal =
        // Unfortunately, dealing with the range operator makes the whole thing a bit of a mess. 
        // If we parse a double ending in . we need to check if it might belong to a range operator and if it does, 
        // re-parse making sure to not process fractions ...
        let number omitFraction = 
            numberLiteral (if omitFraction then format - NumberLiteralOptions.AllowFraction else format) "number literal" >>= fun nl -> 
                if nl.String.Chars(nl.String.Length-1) <> '.' then preturn nl 
                else notFollowedBy (pchar '.') >>. preturn nl
        // note that on a first pass, all options *need* to be parsed together -> don't split into double and int!!
        attempt (number false) <|> attempt (number true) 
    term literal >>= verifyAndBuild |>> fun (exKind, range) -> range |> buildExpression exKind

/// Parses a Q# string literal as QsExpression. 
/// Handles both interpolates and non-interpolated strings.
let internal stringLiteral =
    let strExpr = getStringContent (expectedExpr eof) |>> fun (str, items) -> 
        StringLiteral (str |> NonNullable<string>.New, items.ToImmutableArray()) 
    attempt strExpr |> asExpression

/// Parses an identifier (qualified or unqualified symbol) as QsExpression.
/// Uses multiSegmentSymbol to raise suitable errors if a symbol-like expression cannot be used as identifier.
/// Q# identifiers support modifications - 
/// i.e. they can be preceded by functor application directives, and followed by unwrap application directives. 
let private identifier =
    let typeArgs = 
        let withinAngleBrackets inner = term lAngle >>. inner .>> term rAngle // NOTE: we can't use angleBrackets here, due to < and > being used as operators within expressions!!
        let typeArgs = 
            let typeArg = missingType <|> qsType // missing needs to be first
            let typeList = sepBy typeArg (comma .>>? followedBy typeArg) .>> warnOnComma |>> fun x -> x.ToImmutableArray()
            attempt typeList
        withinAngleBrackets typeArgs <|> (withinAngleBrackets emptySpace >>% ImmutableArray.Empty)
    let identifierName = multiSegmentSymbol ErrorCode.InvalidIdentifierName |>> asQualifiedSymbol 
    identifierName .>>. opt (attempt typeArgs) 
    |>> fun (sym, tArgs) -> sym.Symbol |> function 
        | InvalidSymbol -> unknownExpr
        | _ -> ((sym, tArgs |> function | Some args -> Value args | None -> Null) |> Identifier, sym.Range) |> QsExpression.New
    |> withModifiers


// composite expressions

/// Parser used to generate an ExpectingComma error if bracket content is not comma separated as expected. 
/// Parses the given lbracket followed by an expression, and if this succeeds raises an ExpectingComma error. 
/// Finally advances to the given rbracket (skipping matching brackets as blocks) or the end of the input stream, 
/// consuming the rbracket if such a bracket exists.
/// Fails without consuming input if the parser fails. 
let private bracketDefinedCommaSepExpr (lbracket, rbracket) = // used for arrays and tuples
    let invalidSeparator = eof >>% "" <|> pstring "."
    let upToSeparator = bracket lbracket >>. expr .>> (buildError (term invalidSeparator |>> snd) ErrorCode.ExpectingComma >>% unknownExpr)
    let grabRest = advanceTo (eof >>% "" <|> rbracket) .>> opt (bracket rbracket)
    attempt (upToSeparator .>> grabRest)

/// Parses an arity-1 tuple expression ("parenthesis expression") containing the given tuple item, 
/// and returns the item as Q# value tuple containing a single item.
/// Raises a MissingExpression error if empty tuple brackets exist at the current position.
/// If tuple brackets exist but the given item parser fails within these brackets, raises an InvalidExpression error. 
/// If the item parser succeeds within tuple brackets, but does not consume the entire content within the brackets, 
/// generates an ExcessContinuation error for the remaining content. 
/// Q# arity-1 tuples support modifications, since they are equivalent to their content - 
/// i.e. they can be preceded by functor application directives, and followed by unwrap application directives. 
let private tupledItem item =
    let invalid = checkForInvalid isTupleContinuation ErrorCode.InvalidExpression >>% unknownExpr // fails if isTupleContinuation succeeds
    let expectedItem = expected item ErrorCode.InvalidExpression ErrorCode.MissingExpression unknownExpr isTupleContinuation
    let content = expectedItem |> withExcessContinuation isTupleContinuation <|> invalid .>> warnOnComma 
    tupleBrackets content |>> (fun (item, range) -> (ImmutableArray.Create item |> ValueTuple, range) |> QsExpression.New )
    |> withModifiers

/// Given an array of QsExpressions and a tuple with start and end position, builds a Q# ValueTuple as QsExpression.
let internal buildTupleExpr (items, range : Position * Position) = 
    (ValueTuple items, range) |> QsExpression.New

/// Parses a Q# value tuple as QsExpression using the given item parser to process tuple items.
/// Uses buildTuple to generate suitable errors for invalid or missing expressions within the tuple. 
let private valueTuple item = // allows something like (a,(),b)
    let invalid = buildError (skipInvalidUntil qsFragmentHeader) ErrorCode.InvalidValueTuple >>% unknownExpr // used for processing e.g. (,)
    let validTuple = buildTuple item buildTupleExpr ErrorCode.InvalidExpression ErrorCode.MissingExpression unknownExpr
    tupledItem item <|> validTuple <|> (tupleBrackets invalid |>> fst) <|> bracketDefinedCommaSepExpr (lTuple, rTuple)

/// Parses a Q# value array as QsExpression.
/// Uses commaSep1 to generate suitable errors for invalid or missing expressions within the array. 
let private valueArray = // this disallows []
    let content = 
        let items = commaSep1 expr ErrorCode.InvalidExpression ErrorCode.MissingExpression unknownExpr eof |>> ValueArray
        expected items ErrorCode.InvalidValueArray ErrorCode.EmptyValueArray InvalidExpr eof // adapt error message if [] are allowed
    arrayBrackets content |>> QsExpression.New <|> bracketDefinedCommaSepExpr (lArray, rArray)

/// Parses a Q# array declaration as QsExpression.
/// Raises an InvalidContructorExpression if the array declaration keyword is not followed by a valid array constructor, 
/// and advances to the next whitespace character or QsFragmentHeader.
let private newArray =
    let body = expectedQsType (lArray >>% ()) .>>. (arrayBrackets (expectedExpr eof) |>> fst) |>> NewArray
    let invalid = checkForInvalid (qsFragmentHeader >>% "" <|> (nextCharSatisfies Text.IsWhitespace >>. emptySpace)) ErrorCode.InvalidConstructorExpression >>% unknownExpr
    arrayDecl.parse >>. (term body |>> QsExpression.New <|> (term invalid |>> fst))

/// used to temporarily store item accessors for both array item and named item access expressions
type private ItemAccessor = 
| ArrayItemAccessor of QsExpression * (Position * Position)
| NamedItemAccessor of QsSymbol

/// Parses a Q# ArrayItem as QsExpression.
/// Q# array item expressions support modifications - 
/// i.e. they can be preceded by functor application directives, and followed by unwrap application directives. 
/// Note that this parser has a dependency on the identifier, tupleItem expr, and valueArray parsers - 
/// meaning they process the left-most part of the array item expression and thus need to be evaluated *after* the arrayItemExpr parser. 
let private itemAccessExpr = 
    let rec applyAccessors (ex : QsExpression, item) = 
        match item with
        | [] -> ex
        | ArrayItemAccessor (idx, range) :: tail -> 
            let arrItemEx = buildCombinedExpr (ArrayItem (ex, idx)) (ex.Range, range |> QsPositionInfo.Range)
            applyAccessors (arrItemEx, tail)
        | NamedItemAccessor sym :: tail -> 
            let namedItemEx = buildCombinedExpr (NamedItem (ex, sym)) (ex.Range, sym.Range)
            applyAccessors (namedItemEx, tail)
    let accessor = 
        let missingEx pos = (MissingExpr, (pos, pos)) |> QsExpression.New
        let openRange = pstring qsOpenRangeOp.op |> term
        let fullyOpenRange = openRange |>> snd .>>? followedBy eof |>> fun (p1, p2) -> 
            let lhs, rhs = missingEx p1, missingEx p2
            buildCombinedExpr (RangeLiteral (lhs, rhs)) (lhs.Range, rhs.Range)
        let closedOrHalfOpenRange = 
            let skipToTailingRangeOrEnd = followedBy (opt openRange >>? eof) |> manyCharsTill anyChar
            let slicingExpr state = (opt openRange .>>. expectedExpr eof |> runOnSubstream state) .>>. opt openRange 
            getCharStreamState >>= fun state -> skipToTailingRangeOrEnd >>. slicingExpr state
            |>> fun ((pre, core : QsExpression), post) -> 
                let applyPost ex = post |> function 
                    | None -> ex
                    | Some (_, (rstart, rend)) -> buildCombinedExpr (RangeLiteral (ex, missingEx rend)) (ex.Range, QsPositionInfo.Range (rstart, rend))
                match pre with
                // we potentially need to re-construct the expression following the open range operator
                // to get the correct behavior (in terms of associativity and precedence)
                | Some (_, (lstart, lend)) -> 
                    let combineWith right left = buildCombinedExpr (RangeLiteral (left, right)) (left.Range, right.Range)
                    match core.Expression with
                    // range literals are the only expressions that need to be re-constructed, since only copy-and-update expressions have lower precedence, 
                    // but there is no way to get a correct slicing expression when ex is a copy-and-update expression unless there are prentheses around it. 
                    | RangeLiteral (lex, rex) -> buildCombinedExpr (RangeLiteral (missingEx lend, lex)) (QsPositionInfo.Range (lstart, lend), lex.Range) |> combineWith rex 
                    | _ -> missingEx lend |> combineWith core |> applyPost
                | None -> core |> applyPost
        let arrayItemAccess = arrayBrackets (fullyOpenRange <|> closedOrHalfOpenRange) |>> ArrayItemAccessor
        let namedItemAccess = term (pstring qsNamedItemCombinator.op) >>. symbolLike ErrorCode.ExpectingUnqualifiedSymbol |>> NamedItemAccessor
        arrayItemAccess <|> namedItemAccess
    let arrItem = 
        // ideally, this would also "depend" on callLikeExpression and arrayItemExpr (i.e. try them as an lhs expression)
        // but that requires handling the cyclic dependency ...
        (identifier <|> tupledItem expr <|> valueArray) .>>. many1 accessor // allowing new Int[1][2] (i.e. array items on the lhs) would just be confusing
    attempt arrItem |>> applyAccessors
    |> withModifiers

/// Parses a Q# argument tuple - i.e. an expression tuple that may contain Missing expressions.
/// If the parsed argument tuple is not a unit value, 
/// uses buildTuple to generate a MissingArgument error if a tuple item is missing, or an InvalidArgument error for invalid items. 
let private argumentTuple = 
    let tupleArg = buildTuple argument buildTupleExpr ErrorCode.InvalidArgument ErrorCode.MissingArgument unknownExpr
    unitValue <|> tupleArg

/// Parses a Q# call-like expression as QsExpression.
/// This includes operation and function calls, user defined type constructor calls, and partial applications.
/// Expects tuple brackets around the argument even if the argument consists of a single tuple item. 
/// Note that this parser has a dependency on the arrayItemExpr, identifier, and tupleItem expr parsers - 
/// meaning they process the left-most part of the call-like expression and thus need to be evaluated *after* the callLikeExpr parser. 
let internal callLikeExpr = 
    attempt ((itemAccessExpr <|> identifier <|> tupledItem expr) .>>. argumentTuple) // identifier needs to come *after* arrayItemExpr
    |>> fun (callable, arg) -> applyBinary CallLikeExpression () callable arg


// processing terms of operator precedence parsers

let private termParser tupleExpr = 
    choice [
        // IMPORTANT: any parser here needs to be wrapped in a term parser, such that whitespace is processed properly. 
        attempt unitValue        
        attempt newArray         
        attempt callLikeExpr     
        attempt itemAccessExpr // needs to be after callLikeExpr
        attempt valueArray // needs to be after arryItemExpr
        attempt tupleExpr // needs to be after unitValue, arrayItemExpr, and callLikeExpr
        attempt pauliLiteral     
        attempt resultLiteral    
        attempt numericLiteral   
        attempt boolLiteral      
        attempt stringLiteral    
        attempt identifier // needs to be at the very end
    ]

qsExpression.TermParser <- termParser (valueTuple expr)
qsArgument.TermParser <- missingExpr <|> termParser (valueTuple argument) // missing needs to be first



