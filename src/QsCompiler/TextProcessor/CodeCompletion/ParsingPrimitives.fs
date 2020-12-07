// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module internal Microsoft.Quantum.QsCompiler.TextProcessing.CodeCompletion.ParsingPrimitives

open System
open FParsec
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.TextProcessing.CodeCompletion
open Microsoft.Quantum.QsCompiler.TextProcessing.Keywords
open Microsoft.Quantum.QsCompiler.TextProcessing.ParsingPrimitives
open Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxBuilder


/// `p1 ?>> p2` attempts `p1`. If `p1` succeeds and consumes all input, it returns the result of `p1` without using
/// `p2`. Otherwise, it continues parsing with `p2` and returns the result of `p2`.
let (?>>) p1 p2 = attempt (p1 .>> eof) <|> (p1 >>. p2)

/// `p1 @>> p2` parses `p1`, then `p2` (if `p1` did not consume all input), and concatenates the results.
let (@>>) p1 p2 =
    p1 .>>. (eof >>% [] <|> p2)
    |>> fun (list1, list2) -> list1 @ list2

/// Merges the error messages from all replies in the list.
let rec private toErrorMessageList (replies: Reply<'a> list) =
    match replies with
    | reply :: tail when reply.Status = Ok -> toErrorMessageList tail
    | reply :: tail -> ErrorMessageList.Merge(reply.Error, toErrorMessageList tail)
    | [] -> null

/// Tries all parsers in the sequence `ps`, backtracking to the initial state after each parser. Concatenates the
/// results from all parsers that succeeded into a single list.
///
/// The parser state after running this parser is the state after running the first parser in the sequence that
/// succeeds.
let pcollect ps stream =
    let replies =
        ps
        |> Seq.map (fun p -> (p, lookAhead p stream))
        |> Seq.toList

    let successes =
        replies
        |> List.filter (fun (_, reply) -> reply.Status = Ok)

    if List.isEmpty successes then
        Reply(Error, toErrorMessageList (List.map snd replies))
    else
        let results =
            successes
            |> List.map snd
            |> List.collect (fun reply -> reply.Result)

        let p = List.head successes |> fst
        (p >>% results) stream

/// `p1 <|>@ p2` is equivalent to `pcollect [p1; p2]`.
let (<|>@) p1 p2 = pcollect [ p1; p2 ]

/// Parses the end-of-transmission character.
let eot = pchar '\u0004'

/// Parses EOT (or checks if EOT was the last character consumed), followed by EOF.
let eotEof =
    previousCharSatisfies ((=) '\u0004')
    <|> (eot >>% ())
    .>> eof

/// Parses a symbol. Includes reserved keywords if the keyword is followed immediately by EOT without any whitespace
/// (i.e., a possibly incomplete symbol that happens to start with a keyword). Does not consume whitespace.
let symbol =
    let qsIdentifier =
        identifier
        <| IdentifierOptions(isAsciiIdStart = isSymbolStart, isAsciiIdContinue = isSymbolContinuation)

    notFollowedBy qsReservedKeyword >>. qsIdentifier
    <|> (qsIdentifier .>> followedBy eot)

/// Parses an operator one character at a time. Fails if the operator is followed by any of the characters in
/// `notAfter`. Returns the empty list.
let operator (op: string) notAfter =
    // For operators that start with letters (e.g., "w/"), group the letters together with the first operator character
    // to avoid a conflict with keyword or symbol parsers.
    let numLetters =
        Seq.length (Seq.takeWhile Char.IsLetter op)

    let charByChar p c =
        p ?>> ((eot >>% ()) <|> (pchar c >>% ()))

    let p =
        Seq.fold charByChar (pstring op.[..numLetters] >>% ()) op.[numLetters + 1..]

    attempt
        (p
         ?>> nextCharSatisfiesNot (fun c -> Seq.contains c notAfter))
    >>. (emptySpace >>% ())
    >>% []

/// Parses a sequence of operator-like characters (even if those characters are not a valid operator), excluding
/// characters in `excluded`. Consumes whitespace.
let operatorLike excluded =
    let isOperatorChar c =
        not
            (isSymbolContinuation c
             || Char.IsWhiteSpace c
             || Seq.exists ((=) c) "()[]{}\u0004")
        && not (Seq.exists ((=) c) excluded)

    many1Chars (satisfy isOperatorChar) >>. emptySpace
    >>% []

/// Parses `p` unless EOT occurs first. Returns the empty list.
let expected p = eot >>% [] <|> (p >>% [])

/// Parses an expected identifier. The identifier is optional if EOT occurs first. Returns `[kind]` if EOT occurs first
/// or if `p` did not end in whitespace; otherwise, returns `[]`.
let expectedId kind p =
    eot >>% [ kind ]
    <|> attempt
            (p
             >>. previousCharSatisfiesNot Char.IsWhiteSpace
             >>. optional eot
             >>% [ kind ])
    <|> (p >>% [])

/// Parses an expected keyword. If the keyword parser fails, this parser can still succeed if the next token is symbol-
/// like and occurs immediately before EOT (i.e., a possibly incomplete keyword).
let expectedKeyword keyword =
    expectedId (Keyword keyword.id) keyword.parse
    <|> attempt (symbol >>. eot >>% [ Keyword keyword.id ])

/// Parses an expected qualified symbol. The identifier is optional if EOT occurs first. Returns `[kind]` if EOT occurs
/// first or there is no whitespace after the qualified symbol; otherwise, returns `[]`.
let expectedQualifiedSymbol kind =
    let withoutLast list = List.take (List.length list - 1) list

    let asMember =
        function
        | [] -> [ kind ]
        | symbols -> [ Member(String.Join(".", symbols), kind) ]

    let qualifiedSymbol =
        attempt (sepBy1 symbol (pchar '.'))
        |>> withoutLast
        |>> asMember
        <|> (sepEndBy1 symbol (pchar '.') |>> asMember)

    eot >>% [ kind ]
    <|> attempt
            (term qualifiedSymbol |>> fst
             .>> previousCharSatisfiesNot Char.IsWhiteSpace
             .>> optional eot)
    <|> (term qualifiedSymbol >>% [])

/// Optionally parses `p`, backtracking if it consumes EOT so another parser can try, too. Best if used with `@>>`,
/// e.g., `optR foo @>> bar`.
let optR p =
    attempt (p .>> previousCharSatisfies ((<>) '\u0004'))
    <|> lookAhead p
    <|>% []

/// `manyR p shouldBacktrack` is like `many p` but is reentrant on the last item if
///     1. The last item is followed by EOT; and
///     2. `shouldBacktrack` succeeds at the beginning of the last item, or the last item consists of only EOT.
///
/// This is useful if it is ambiguous whether the last item belongs to `p` or the parser that comes after `manyR`.
let manyR p shouldBacktrack stream =
    let last =
        (many1 (getCharStreamState .>>. attempt p)
         |>> List.last) stream

    if last.Status <> Ok then
        Reply []
    else
        let after = (getCharStreamState stream).Result

        let consumedEot =
            (previousCharSatisfies ((<>) '\u0004') stream)
                .Status = Ok

        stream.BacktrackTo(fst last.Result)

        if ((notFollowedBy shouldBacktrack
             >>. notFollowedBy eot) stream)
            .Status = Ok
           || consumedEot then
            stream.BacktrackTo after |> ignore

        Reply(snd last.Result)

/// `manyLast p` is like `many p` but returns only the result of the last item, or the empty list if no items were
/// parsed.
let manyLast p =
    many p |>> List.tryLast |>> Option.defaultValue []

/// Parses zero or more occurrences of `p` separated by `sep` and returns only the result of the last item, or the empty
/// list if no items were parsed.
let sepByLast p sep =
    sepBy p sep
    |>> List.tryLast
    |>> Option.defaultValue []

/// Parses brackets around `p`. The right bracket is optional if EOT occurs first. Use `brackets` instead of
/// `expectedBrackets` if there are other parsers besides a left bracket that can follow and you want this parser to
/// fail if the stream has ended.
let brackets (left, right) p =
    bracket left >>. p ?>> expected (bracket right)

/// Parses brackets around `p`. Both the left and right brackets are optional if EOT occurs first. Use
/// `expectedBrackets` instead of `brackets` if a left bracket is the only thing that can follow and you want this
/// parser to still succeed if the stream has ended.
let expectedBrackets (left, right) p =
    expected (bracket left)
    ?>> p
    ?>> expected (bracket right)

/// Parses a tuple of zero or more items each parsed by `p`.
let tuple p =
    brackets
        (lTuple, rTuple)
        (sepBy p comma
         |>> List.tryLast
         |>> Option.defaultValue [])

/// Parses a tuple of one or more items each parsed by `p`.
let tuple1 p =
    brackets (lTuple, rTuple) (sepBy1 p comma |>> List.last)

/// Parses an array of items each parsed by `p`.
let array p =
    brackets (lArray, rArray) (sepBy1 p comma |>> List.last)

/// The missing expression keyword.
let missingExpr = { parse = keyword "_"; id = "_" }

/// The omitted symbols keyword.
let omittedSymbols = { parse = keyword "..."; id = "..." }

/// Parses the unit value. The right bracket is optional if EOT occurs first.
let unitValue: Parser<CompletionKind list, QsCompilerDiagnostic list> =
    term (pchar '(' >>. emptySpace >>. expected (pchar ')'))
    |>> fst

/// Creates an expression parser using the given prefix operator, infix operator, postfix operator, and term parsers.
let createExpressionParser prefixOp infixOp postfixOp expTerm =
    let termBundle =
        manyR prefixOp symbol @>> expTerm
        ?>> manyLast postfixOp

    termBundle @>> manyLast (infixOp ?>> termBundle)
