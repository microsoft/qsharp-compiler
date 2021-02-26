// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

type SymbolDeclaration = { Name: Terminal; Type: TypeAnnotation option }

type SymbolBinding =
    | SymbolDeclaration of SymbolDeclaration
    | SymbolTuple of SymbolBinding Tuple

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

type If =
    {
        IfKeyword: Terminal
        Condition: Expression
        Block: Statement Block
    }

and Else = { ElseKeyword: Terminal; Block: Statement Block }

and Statement =
    | Let of Let
    | Return of Return
    | If of If
    | Else of Else
    | Unknown of Terminal

module Statement =
    let mapPrefix mapper =
        function
        | Let lets -> { lets with LetKeyword = lets.LetKeyword |> Terminal.mapPrefix mapper } |> Let
        | Return returns ->
            { returns with ReturnKeyword = returns.ReturnKeyword |> Terminal.mapPrefix mapper } |> Return
        | If ifs -> { ifs with IfKeyword = ifs.IfKeyword |> Terminal.mapPrefix mapper } |> If
        | Else elses -> { elses with ElseKeyword = elses.ElseKeyword |> Terminal.mapPrefix mapper } |> Else
        | Unknown terminal -> Terminal.mapPrefix mapper terminal |> Unknown
