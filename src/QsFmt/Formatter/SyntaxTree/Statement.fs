// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

type ParameterDeclaration = { Name: Terminal; Type: TypeAnnotation }

type ParameterBinding =
    | ParameterDeclaration of ParameterDeclaration
    | ParameterTuple of ParameterBinding Tuple

type SymbolBinding =
    | SymbolDeclaration of Terminal
    | SymbolTuple of SymbolBinding Tuple

module SymbolBinding =
    let mapPrefix mapper =
        function
        | SymbolDeclaration terminal -> terminal |> Terminal.mapPrefix mapper |> SymbolDeclaration
        | SymbolTuple tuple -> tuple |> Tuple.mapPrefix mapper |> SymbolTuple

type SingleQubit =
    {
        Qubit: Terminal
        OpenParen: Terminal
        CloseParen: Terminal
    }

type QubitArray =
    {
        Qubit: Terminal
        OpenBracket: Terminal
        Length: Expression
        CloseBracket: Terminal
    }

type QubitInitializer =
    | SingleQubit of SingleQubit
    | QubitArray of QubitArray
    | QubitTuple of QubitInitializer Tuple

type QubitBinding =
    {
        Name: SymbolBinding
        Equals: Terminal
        Initializer: QubitInitializer
    }

module QubitBinding =
    let mapPrefix mapper (binding : QubitBinding) =
        { binding with Name = SymbolBinding.mapPrefix mapper binding.Name }

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
        | BorrowBlock borrow ->
            { borrow with BorrowKeyword = borrow.BorrowKeyword |> Terminal.mapPrefix mapper } |> BorrowBlock
        | If ifs -> { ifs with IfKeyword = ifs.IfKeyword |> Terminal.mapPrefix mapper } |> If
        | Else elses -> { elses with ElseKeyword = elses.ElseKeyword |> Terminal.mapPrefix mapper } |> Else
        | Unknown terminal -> Terminal.mapPrefix mapper terminal |> Unknown
        
