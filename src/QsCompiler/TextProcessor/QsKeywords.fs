// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// The purpose of this module is to aggregate all keywords used throughout Qs such that they only need to adapted here when changed
module Microsoft.Quantum.QsCompiler.TextProcessing.Keywords

open System.Collections.Generic
open System.Collections.Immutable
open FParsec
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.ReservedKeywords
open Microsoft.Quantum.QsCompiler.TextProcessing.ParsingPrimitives


/// A Q# keyword consists of a parser that consumes that keyword and returns its start and end position as a tuple,
/// as well as a string containing the keyword itself.
type QsKeyword =
    { parse: Parser<Range, QsCompilerDiagnostic list>
      id: string }

/// contains all Q# keywords that cannot be used as a symbol name
let private _ReservedKeywords = new HashSet<string>()
/// contains all Q# keywords that cannot be used within an expression (strict subset of QsReservedKeywords, since it does not contain e.g. Q# literals)
let private _LanguageKeywords = new HashSet<string>()
/// contains all Q# keywords that denote the Q# fragment headers used as re-entry points upon parsing failures (strict subset of QsLanguageKeywords)
let private _FragmentHeaders = new HashSet<string>()

let private qsKeyword word = { parse = keyword word; id = word }

/// adds the given word to the list of QsReservedKeywords, and returns the corresponding keyword
let private addKeyword word =
    _ReservedKeywords.Add word |> ignore
    qsKeyword word

/// adds the given word to the list of QsReservedKeywords and QsLanguageKeywords, and returns the corresponding keyword
let private addLanguageKeyword word =
    _ReservedKeywords.Add word |> ignore
    _LanguageKeywords.Add word |> ignore
    qsKeyword word

/// adds the given word to the list of QsReservedKeywords, QsLanguageKeywords and QsFragmentHeaders, and returns the corresponding keyword
let private addFragmentHeader word =
    _ReservedKeywords.Add word |> ignore
    _LanguageKeywords.Add word |> ignore
    _FragmentHeaders.Add word |> ignore
    qsKeyword word

/// Given the keyword for two functors, constructs and returns the keyword for the combined functor
/// under the assumption tha the order of the functors does not matter.
/// Adds the keyword for the combined functor to the list of QsReservedKeywords, QsLanguageKeywords and QsFragmentHeaders.
let private addFunctorCombination (word1: QsKeyword, word2: QsKeyword) =
    let id = sprintf "%s %s" word1.id word2.id
    _ReservedKeywords.Add id |> ignore
    _LanguageKeywords.Add id |> ignore
    _FragmentHeaders.Add id |> ignore

    { id = id
      parse = (word1.parse .>> word2.parse) <|> (word2.parse .>> word1.parse) }


// Qs types

// keyword for a Q# transformation characteristics annotation (QsLanguageKeyword)
let qsCharacteristics = addLanguageKeyword Types.Characteristics
// keyword for a predefined set of Q# transformation characteristics (QsLanguageKeyword)
let qsAdjSet = addLanguageKeyword Types.AdjSet
// keyword for a predefined set of Q# transformation characteristics (QsLanguageKeyword)
let qsCtlSet = addLanguageKeyword Types.CtlSet

/// keyword for a predefined Q# type (QsLanguageKeyword)
let qsUnit = addLanguageKeyword Types.Unit
/// keyword for a predefined Q# type (QsLanguageKeyword)
let qsInt = addLanguageKeyword Types.Int
/// keyword for a predefined Q# type (QsLanguageKeyword)
let qsBigInt = addLanguageKeyword Types.BigInt
/// keyword for a predefined Q# type (QsLanguageKeyword)
let qsDouble = addLanguageKeyword Types.Double
/// keyword for a predefined Q# type (QsLanguageKeyword)
let qsBool = addLanguageKeyword Types.Bool
/// keyword for a predefined Q# type (QsLanguageKeyword)
let qsQubit = addLanguageKeyword Types.Qubit
/// keyword for a predefined Q# type (QsLanguageKeyword)
let qsResult = addLanguageKeyword Types.Result
/// keyword for a predefined Q# type (QsLanguageKeyword)
let qsPauli = addLanguageKeyword Types.Pauli
/// keyword for a predefined Q# type (QsLanguageKeyword)
let qsRange = addLanguageKeyword Types.Range
/// keyword for a predefined Q# type (QsLanguageKeyword)
let qsString = addLanguageKeyword Types.String

// Qs literals

/// keyword for a Q# literal (QsReserverKeyword)
let qsPauliX = addKeyword Literals.PauliX
/// keyword for a Q# literal (QsReserverKeyword)
let qsPauliY = addKeyword Literals.PauliY
/// keyword for a Q# literal (QsReserverKeyword)
let qsPauliZ = addKeyword Literals.PauliZ
/// keyword for a Q# literal (QsReserverKeyword)
let qsPauliI = addKeyword Literals.PauliI

/// keyword for a Q# literal (QsReserverKeyword)
let qsZero = addKeyword Literals.Zero
/// keyword for a Q# literal (QsReserverKeyword)
let qsOne = addKeyword Literals.One

/// keyword for a Q# literal (QsReserverKeyword)
let qsTrue = addKeyword Literals.True
/// keyword for a Q# literal (QsReserverKeyword)
let qsFalse = addKeyword Literals.False

// Qs functors

/// keyword for a Q# functor application (QsReserverKeyword)
let qsAdjointFunctor = addKeyword Functors.Adjoint
/// keyword for a Q# functor application (QsReserverKeyword)
let qsControlledFunctor = addKeyword Functors.Controlled

// statements

/// keyword for a Q# statement header (QsFragmentHeader)
let qsReturn = addFragmentHeader Statements.Return
/// keyword for a Q# statement header (QsFragmentHeader)
let qsFail = addFragmentHeader Statements.Fail
/// keyword for a Q# statement header (QsFragmentHeader)
let qsImmutableBinding = addFragmentHeader Statements.Let
/// keyword for a Q# statement header (QsFragmentHeader)
let qsMutableBinding = addFragmentHeader Statements.Mutable
/// keyword for a Q# statement header (QsFragmentHeader)
let qsValueUpdate = addFragmentHeader Statements.Set

// control flow statements

/// keyword for a Q# control flow statement (QsFragmentHeader)
let qsIf = addFragmentHeader Statements.If
/// keyword for a Q# control flow statement (QsFragmentHeader)
let qsElif = addFragmentHeader Statements.Elif
/// keyword for a Q# control flow statement (QsFragmentHeader)
let qsElse = addFragmentHeader Statements.Else

/// keyword for a Q# control flow statement (QsFragmentHeader)
let qsFor = addFragmentHeader Statements.For
/// keyword for a Q# control flow statement (QsLanguageKeyword)
let qsRangeIter = addLanguageKeyword Statements.In

/// keyword for a Q# control flow statement (QsFragmentHeader)
let qsWhile = addFragmentHeader Statements.While

/// keyword for a Q# control flow statement (QsFragmentHeader)
let qsRepeat = addFragmentHeader Statements.Repeat
/// keyword for a Q# control flow statement (QsFragmentHeader)
let qsUntil = addFragmentHeader Statements.Until
/// keyword for a Q# control flow statement (QsLanguageKeyword)
let qsRUSfixup = addLanguageKeyword Statements.Fixup

// block statements

/// keyword for a Q# transformation pattern (QsFragmentHeader)
let qsWithin = addFragmentHeader Statements.Within
/// keyword for a Q# transformation pattern (QsFragmentHeader)
let qsApply = addFragmentHeader Statements.Apply

/// keyword for a Q# allocation statement (QsFragmentHeader)
let qsUsing = addFragmentHeader Statements.Using
/// keyword for a Q# allocation statement (QsFragmentHeader)
let qsBorrowing = addFragmentHeader Statements.Borrowing

// expression related keywords

/// keyword used within a Q# new-array-expression (QsReservedKeyword)
let arrayDecl = addKeyword Expressions.New
/// keyword used as operator for Q# expressions (QsReservedKeyword)
let notOperator = addKeyword Expressions.Not
/// keyword used as operator for Q# expressions (QsReservedKeyword)
let andOperator = addKeyword Expressions.And
/// keyword used as operator for Q# expressions (QsReservedKeyword)
let orOperator = addKeyword Expressions.Or

// declarations

/// keyword for a Q# declaration (QsFragmentHeader)
let bodyDeclHeader = addFragmentHeader Declarations.Body
/// keyword for a Q# declaration (QsFragmentHeader)
let adjDeclHeader = addFragmentHeader Declarations.Adjoint
/// keyword for a Q# declaration (QsFragmentHeader)
let ctrlDeclHeader =
    addFragmentHeader Declarations.Controlled
/// keyword for a Q# declaration (QsFragmentHeader)
let ctrlAdjDeclHeader =
    addFunctorCombination (ctrlDeclHeader, adjDeclHeader)

/// keyword for a Q# declaration (QsFragmentHeader)
let opDeclHeader = addFragmentHeader Declarations.Operation
/// keyword for a Q# declaration (QsFragmentHeader)
let fctDeclHeader = addFragmentHeader Declarations.Function
/// keyword for a Q# declaration (QsFragmentHeader)
let typeDeclHeader = addFragmentHeader Declarations.Type

/// keyword for a Q# declaration (QsFragmentHeader)
let namespaceDeclHeader = addFragmentHeader Declarations.Namespace

/// keyword for a Q# declaration modifier (QsFragmentHeader)
let qsInternal = addFragmentHeader Declarations.Internal

// directives

/// keyword for a Q# directive (QsFragmentHeader)
let importDirectiveHeader = addFragmentHeader Directives.Open
/// keyword for a Q# directive (QsLanguageKeyword)
let importedAs = addLanguageKeyword Directives.OpenedAs

/// keyword for a Q# directive (QsLanguageKeyword)
let autoFunctorGenDirective = addLanguageKeyword Directives.Auto
/// keyword for a Q# directive (QsLanguageKeyword)
let intrinsicFunctorGenDirective = addLanguageKeyword Directives.Intrinsic
/// keyword for a Q# directive (QsLanguageKeyword)
let selfFunctorGenDirective = addLanguageKeyword Directives.Self
/// keyword for a Q# directive (QsLanguageKeyword)
let invertFunctorGenDirective = addLanguageKeyword Directives.Invert
/// keyword for a Q# directive (QsLanguageKeyword)
let distributeFunctorGenDirective = addLanguageKeyword Directives.Distribute


// external access to Q# keywords

/// contains all Q# keywords that cannot be used as a symbol name
let public ReservedKeywords = _ReservedKeywords.ToImmutableHashSet()
/// contains all Q# keywords that cannot be used within an expression (strict subset of QsReservedKeywords, since it does not contain e.g. Q# literals)
let internal LanguageKeywords = _LanguageKeywords.ToImmutableHashSet()
/// contains all Q# keywords that denote the Q# fragment headers used as re-entry points upon parsing failures (strict subset of QsLanguageKeywords)
let internal FragmentHeaders = _FragmentHeaders.ToImmutableHashSet()


// Q# operators

type QsOperator =
    { op: string
      cont: string
      prec: int
      isLeftAssociative: bool }
    member internal this.Associativity =
        if this.isLeftAssociative then Associativity.Left else Associativity.Right

    static member New(str, p, assoc) =
        { op = str
          cont = null
          prec = p
          isLeftAssociative = assoc }

    static member New(str, rstr, p, assoc) =
        { op = str
          cont = rstr
          prec = p
          isLeftAssociative = assoc }

let qsCopyAndUpdateOp = QsOperator.New("w/", "<-", 1, true) // *needs* to have lowest precedence!
let qsOpenRangeOp = QsOperator.New("...", 2, true) // only valid as part of certain contextual expressions!
let qsRangeOp = QsOperator.New("..", 2, true) // second lowest precedence due to the contextual open range operator
let qsConditionalOp = QsOperator.New("?", "|", 5, false)
let qsORop = QsOperator.New(orOperator.id, 10, true)
let qsANDop = QsOperator.New(andOperator.id, 11, true)
let qsBORop = QsOperator.New("|||", 12, true)
let qsBXORop = QsOperator.New("^^^", 13, true)
let qsBANDop = QsOperator.New("&&&", 14, true)
let qsEQop = QsOperator.New("==", 20, true)
let qsNEQop = QsOperator.New("!=", 20, true)
let qsLTEop = QsOperator.New("<=", 25, true)
let qsGTEop = QsOperator.New(">=", 25, true)
let qsLTop = QsOperator.New("<", 25, true)
let qsGTop = QsOperator.New(">", 25, true)
let qsRSHIFTop = QsOperator.New(">>>", 28, true)
let qsLSHIFTop = QsOperator.New("<<<", 28, true)
let qsADDop = QsOperator.New("+", 30, true)
let qsSUBop = QsOperator.New("-", 30, true)
let qsMULop = QsOperator.New("*", 35, true)
let qsMODop = QsOperator.New("%", 35, true)
let qsDIVop = QsOperator.New("/", 35, true)
let qsPOWop = QsOperator.New("^", 40, false)
let qsBNOTop = QsOperator.New("~~~", 45, false)

let qsNOTop =
    QsOperator.New(notOperator.id, 45, false)

let qsNEGop = QsOperator.New("-", 45, false)

let qsSetUnion = QsOperator.New("+", 10, true)
let qsSetIntersection = QsOperator.New("*", 20, true)

// As far as the precedence rules of Q# go,
// there are operators (the things above, processed by an operator precedence parser),
// modifiers (functors and unwrap, processed manually as part of certain expressions),
// and combinators (like calls and array items, that are processed as part of the expression system).
// The call combinator binds stronger than all operators.
// All modifiers bind stronger than the call combinator.
// The array item combinator binds stronger than all modifiers.
let qsCallCombinator = QsOperator.New("(", ")", 900, true) // Op()() is fine

let qsAdjointModifier =
    QsOperator.New(qsAdjointFunctor.id, 950, false)

let qsControlledModifier =
    QsOperator.New(qsControlledFunctor.id, 951, false)

let qsUnwrapModifier = QsOperator.New("!", 1000, true)
let qsArrayAccessCombinator = QsOperator.New("[", "]", 1100, true) // arr[i][j] is fine
let qsNamedItemCombinator = QsOperator.New("::", 1100, true) // any combination of named and array item acces is fine
