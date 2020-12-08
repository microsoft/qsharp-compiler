// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// All parsers in this module, and all parsers returned by functions in this module,
/// are atomic in the sense that they either succeed or fail without consuming input.
/// The purpose of this module is to hide all whitespace management from the remaining parsing modules
/// Note in case of future edits: parsers are expected to handle whitespace to the right, but won't handle whitespace to the left!
/// (choosing it this way round because of the operator precedence parser)
module Microsoft.Quantum.QsCompiler.TextProcessing.ParsingPrimitives

open FParsec
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxExtensions


// utils

/// takes two strings as argument and returns the concatenated string as parser
let internal concat (a: string, b: string) = preturn (a + b)
/// parses zero or more whitespace characters and returns them as string
let internal emptySpace = manySatisfy Text.IsWhitespace

// whitespace management

let private rws p: Parser<_, QsCompilerDiagnostic list> = attempt p .>> emptySpace // making all parsers non-generic here for the sake of performance!
let private rwstr s = pstring s |> rws

// symbols and names

let private buildQsExpression kind (range: Range) = (kind, range) |> QsExpression.New
let private buildQsSymbol kind (range: Range) = (kind, range) |> QsSymbol.New
let private buildQsType kind (range: Range) = (kind, range) |> QsType.New

/// returns true if the given char is a valid start symbol for an identifier: Underscore or a Unicode character of classes Lu, Ll, Lt, Lm, Lo, Nl.
/// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/lexical-structure#identifiers
let internal isSymbolStart c =
    c = '_'
    || System.Char.IsLetter(c) // Covers Lu, Ll, Lt, Lm, Lo
    || System.Char.GetUnicodeCategory(c) = System.Globalization.UnicodeCategory.LetterNumber // Nl

/// returns true if the given char is a valid part symbol for an identifier: Unicode character of classes Lu, Ll, Lt, Lm, Lo, Nl; Nd; Pc; Mn, Mc; Cf.
/// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/lexical-structure#identifiers
let internal isSymbolContinuation c =
    System.Char.IsLetter(c) // Covers Lu, Ll, Lt, Lm, Lo
    || List.contains
        (System.Char.GetUnicodeCategory c)
        [ System.Globalization.UnicodeCategory.LetterNumber // Nl
          System.Globalization.UnicodeCategory.DecimalDigitNumber // Nd
          System.Globalization.UnicodeCategory.ConnectorPunctuation // Pc (includes underscore)
          System.Globalization.UnicodeCategory.NonSpacingMark // Mn
          System.Globalization.UnicodeCategory.SpacingCombiningMark // Mc
          System.Globalization.UnicodeCategory.Format ] // Cf

/// Returns the current position in the input stream.
let internal getPosition =
    CharParsers.getPosition |>> fun p -> Position.Create (int p.Line - 1) (int p.Column - 1)

/// Returns the result of 'p' and the character range that 'p' consumed.
let internal getRange p =
    getPosition .>>. p .>>. getPosition
    |>> fun ((start, result), end') -> result, Range.Create start end'

/// Returns an empty range at the current position in the input stream.
let internal getEmptyRange = getRange (preturn ()) |>> snd

/// Gets the char stream position before and after applying the given parser,
/// and returns the result of the given parser as well as a tuple of the two positions.
/// If the given parser fails, returns to the initial char stream state an reports a non-fatal error.
/// Consumes any whitespace to the right.
let internal term p = getRange p |> rws

/// Parses the given string and verifies that the next following character is not a valid symbol continuation.
/// Returns to the initial char stream state an reports a non-fatal error if the verification fails.
/// Gets the char stream position before and after parsing the given string, and returns a tuple of the two positions.
/// Consumes any whitespace to the right.
let internal keyword s =
    pstring s .>> nextCharSatisfiesNot isSymbolContinuation |> term |>> snd

// externally accessible parsers

/// parses a comma consuming whitespace to the right
let internal comma = rwstr ","
/// parses a colon consuming whitespace to the right
let internal colon = rwstr ":"
/// parses an equal sign consuming whitespace to the right
let internal equal = rwstr "="

/// parses an arrow "=>" consuming whitespace to the right
let internal opArrow = rwstr "=>"
/// parses an arrow "->" consuming whitespace to the right
let internal fctArrow = rwstr "->"

/// parses a unit value as a term and returnes the corresponding Q# expression
let internal unitValue =
    term (pstring "(" >>. emptySpace .>> pstring ")") |>> snd |>> buildQsExpression UnitValue
/// parses a missing type as term and returns the corresponding Q# type
let internal missingType = keyword "_" |>> buildQsType MissingType
/// parses a missing expression as a term and returnes the corresponding Q# expression
let internal missingExpr = keyword "_" |>> buildQsExpression MissingExpr
/// parses a discarded symbol as a term and returnes the corresponding Q# expression
let internal discardedSymbol = keyword "_" |>> buildQsSymbol MissingSymbol
/// parses an omitted-symbols-indicator ("...") as a term and returnes the corresponding Q# expression
let internal omittedSymbols = keyword "..." |>> buildQsSymbol OmittedSymbols
/// parses the introductory char to a Q# attribute as a term and returns its range
let internal attributeIntro = term (pchar '@') |>> snd
