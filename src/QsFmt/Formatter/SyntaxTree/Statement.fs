// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

type SymbolDeclaration = { Name: Terminal; Type: TypeAnnotation option }

type SymbolBinding =
    | SymbolDeclaration of SymbolDeclaration
    | SymbolTuple of SymbolBinding Tuple

type QubitSymbolBinding =
    | QubitSymbolDeclaration of Terminal
    | QubitSymbolTuple of QubitSymbolBinding Tuple

type SingleQubit = { Qubit: Terminal; OpenParen: Terminal; CloseParen: Terminal }
type QubitArray = { Qubit: Terminal; OpenBracket: Terminal; Length: Expression; CloseBracket: Terminal }

type QubitInitializer =
    | SingleQubit of SingleQubit
    | QubitArray of QubitArray
    | QubitTuple of QubitInitializer Tuple

type QubitBinding = { Name: QubitSymbolBinding; Equals: Terminal; Initializer: QubitInitializer }

type Let =
    {
        LetKeyword: Terminal
        Binding: SymbolBinding
        Equals: Terminal
        Value: Expression
        Semicolon: Terminal
    }

type Return =
    {
        ReturnKeyword: Terminal
        Expression: Expression
        Semicolon: Terminal
    }

type Use =
    {
        UseKeyword: Terminal
        Binding: QubitBinding
        OpenParen: Terminal option
        CloseParen: Terminal option
        Semicolon: Terminal
    }

type Borrow =
    {
        BorrowKeyword: Terminal
        Binding: QubitBinding
        OpenParen: Terminal option
        CloseParen: Terminal option
        Semicolon: Terminal
    }

type UseBlock =
    {
        UseKeyword: Terminal
        Binding: QubitBinding
        OpenParen: Terminal option
        CloseParen: Terminal option
        Block: Statement Block
    }

and BorrowBlock =
    {
        BorrowKeyword: Terminal
        Binding: QubitBinding
        OpenParen: Terminal option
        CloseParen: Terminal option
        Block: Statement Block
    }

and If =
    {
        IfKeyword: Terminal
        Condition: Expression
        Block: Statement Block
    }

and Else = { ElseKeyword: Terminal; Block: Statement Block }

and Statement =
    | Let of Let
    | Return of Return
    | Use of Use
    | UseBlock of UseBlock
    | Borrow of Borrow
    | BorrowBlock of BorrowBlock
    | If of If
    | Else of Else
    | Unknown of Terminal

module Statement =
    let mapPrefix mapper =
        function
        | Let lets -> { lets with LetKeyword = lets.LetKeyword |> Terminal.mapPrefix mapper } |> Let
        | Return returns ->
            { returns with ReturnKeyword = returns.ReturnKeyword |> Terminal.mapPrefix mapper } |> Return
        | Use ``use`` -> { ``use`` with UseKeyword = ``use``.UseKeyword |> Terminal.mapPrefix mapper } |> Use
        | UseBlock ``use`` -> { ``use`` with UseKeyword = ``use``.UseKeyword |> Terminal.mapPrefix mapper } |> UseBlock
        | Borrow borrow -> { borrow with BorrowKeyword = borrow.BorrowKeyword |> Terminal.mapPrefix mapper } |> Borrow
        | BorrowBlock borrow -> { borrow with BorrowKeyword = borrow.BorrowKeyword |> Terminal.mapPrefix mapper } |> BorrowBlock
        | If ifs -> { ifs with IfKeyword = ifs.IfKeyword |> Terminal.mapPrefix mapper } |> If
        | Else elses -> { elses with ElseKeyword = elses.ElseKeyword |> Terminal.mapPrefix mapper } |> Else
        | Unknown terminal -> Terminal.mapPrefix mapper terminal |> Unknown
