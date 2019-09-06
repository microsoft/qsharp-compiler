﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.TextProcessing.Parsing

open System.Collections.Immutable
open FParsec
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens 
open Microsoft.Quantum.QsCompiler.ReservedKeywords
open Microsoft.Quantum.QsCompiler.TextProcessing.CodeFragments
open Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.TextProcessing.ParsingPrimitives
open Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxBuilder


// some utils for text processing

/// one-based start position of a stream 
let streamStart = { Line = 1; Column = 1 } 

/// Given a position relative to a start position, computes the corresponding absolute position - 
/// i.e. "adds" the relative position to the given initial position.
/// IMPORTANT: Both positions are assumed to be one-based, such that this routine is compatible with the position information given by FParsec.
let private AddRelativePosition (pos : QsPositionInfo, relativePos : QsPositionInfo) = 
    let line = pos.Line + relativePos.Line - 1 
    let column = if relativePos.Line > 1 then relativePos.Column else pos.Column + relativePos.Column - 1 
    {Line = line; Column = column}

/// Applies the given parser to the given text and returns either the parsing result if the parsing succeeds
/// or the return value of onError if the parsing fails. 
let private ParseWith parser onError text = 
    runParserOnString parser [] "" text |> function
    | Success (res, _, _) -> res
    | Failure _ -> onError()

/// Applies the given parser to the given text and, 
/// if the parsing succeeds, returns the parsed start and end position as QsPositionInfo * QsPositionInfo tuple. 
/// If the parsing fails, returns the default stream start position for both start and end position. 
let private GetDelimiters parser text = 
    let onError _ = streamStart, streamStart
    text |> ParseWith parser onError 

let private posInfo = getPosition |>> QsPositionInfo.New
let private remainingText = posInfo .>>. manyCharsTill anyChar eof

/// Returns an UnknownFragment as QsFragment for the given text, 
/// as well as a tuple of the text end position and an empty string. 
let private BuildUnknown text =
    let unknownFragment text (first,last) = {
            Kind = InvalidFragment
            Range = (first, last)
            Diagnostics = ImmutableArray.Create (QsCompilerDiagnostic.Error (InvalidFragment.ErrorCode, []) (first, last))
            Text = text
        } 
    let range = GetDelimiters (remainingText |>> fst .>>. posInfo) text
    let unknownStatement = unknownFragment (NonNullable<string>.New text) range
    unknownStatement, (snd unknownStatement.Range, "")


// routines used to translate text to Q# syntax

/// Processes the next fragment of the given text under the assumption that there is no leading whitespace preceding the fragment. 
/// Returns the processed fragment as Value as well as a tuple consisting of the position up to which text has been processed and the remaining text. 
/// If there is no text remaining returns Null, the position within the (empty) text, as well as an empty string. 
let private NextFragment text = 
    let fragment = codeFragment .>> ParsingPrimitives.emptySpace |>> Value 
    let next =
        let noMoreFragments = eof >>. posInfo |>> fun pos -> (Null, (pos, "")) 
        noMoreFragments <|> (fragment .>>. remainingText) // noMoreFragments needs to be first here!
    let onError _ = 
        QsCompilerError.Raise "error on parsing"
        let unknown, (pos, remaining) = BuildUnknown text
        Value unknown, (pos, remaining)
    text |> ParseWith next onError

/// Extracts all QsFragments from the given string and returns them as a list. 
/// IMPORTANT: The fragment Range is relative to the start position of the string for all fragments, 
/// whereas any position info within a fragment is always relative to the start position of that fragment.
let ProcessFragments text = 
    let (initialPos, text) = 
        let cutLeadingWS = ParsingPrimitives.emptySpace >>. remainingText
        let onError _ = streamStart, text
        text |> ParseWith cutLeadingWS onError 
    let rec doProcessing fragments (startPos,str) =
        let build (frag : QsFragment) = 
            let absoluteRange = (AddRelativePosition (startPos, fst frag.Range), AddRelativePosition (startPos, snd frag.Range))
            frag.WithRange(absoluteRange) 
        match NextFragment str with 
        | Null, _ -> fragments |> List.rev
        | Value frag, (pos, remaining) when str <> remaining ->
            doProcessing (build frag :: fragments) (AddRelativePosition (startPos, pos), remaining)
        | _ ->  // adding a protection against an infinite loop in case someone messes up the fragment processing...
            QsCompilerError.Raise "fragment has been built but no input was consumed"
            (BuildUnknown str |> fst |> build) :: fragments |> List.rev
    doProcessing [] (initialPos, text)


// routines used by external tools

type FragmentsProcessor = delegate of string -> QsFragment[]


/// Returns a delegate that extracts all QsFragments from a given string and returns them as a list. 
/// IMPORTANT: The fragment Range is relative to the start position of the string for all fragments, 
/// whereas any position info within a fragment is always relative to the start position of that fragment.
let ProcessCodeFragment = 
    new FragmentsProcessor(ProcessFragments >> List.toArray)

/// Given a string, determines the start and end position of the first nrHeaders of words consisting only of letters.
let HeaderDelimiters nrHeaders = 
    let splitHeaders = 
        let letters = many1Satisfy (System.Char.IsLetter)
        let header = letters >>. posInfo .>> spaces
        spaces >>. posInfo .>>. many header .>>. posInfo
    splitHeaders 
    |>> fun ((pos1, following), pos2) -> 
        if nrHeaders <= following.Length then (pos1, following.[nrHeaders-1]) else (pos1, pos2)
    |> GetDelimiters

/// Parse an illegal array item update set statement for use by the copy-and-update recommendation code action.
/// Returns the ending position, array identifier and index, and right-hand side.
let ProcessUpdateOfArrayItemExpr =
    let ParseUpdateOfArrayItemExpr parser text = 
        let onError _ = streamStart, "", "", ""
        text |> ParseWith parser onError
    // Parses a single-line, single-dimension array item update, ex: set arr[0] = i
    let parseSimpleUpdateOfArrayItem =
        let nonNewLineChars = many1Satisfy (fun c -> c <> '\n' && c <> '\r')
        let setStatement = spaces >>. pstring Statements.Set .>> spaces
        let arrIdentifier = many1Satisfy (isSymbolContinuation) .>> spaces
        let equalOrWs = spaces >>. equal
        let simpleArrIndex = (arrayBrackets nonNewLineChars |>> fst) .>> followedBy equalOrWs
        let rhsContents = equalOrWs >>. (remainingText |>> snd)
        setStatement >>. arrIdentifier .>>. simpleArrIndex .>>. rhsContents .>>. posInfo
    parseSimpleUpdateOfArrayItem
    |>> fun (((ident, idx), rhs), pos4) -> (pos4, ident, idx, rhs)
    |> ParseUpdateOfArrayItemExpr