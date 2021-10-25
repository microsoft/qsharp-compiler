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
    let mapPrefix mapper (binding: QubitBinding) =
        { binding with Name = SymbolBinding.mapPrefix mapper binding.Name }

type QubitDeclarationKind =
    | Use
    | Borrow

type ForBinding =
    {
        Name: SymbolBinding
        In: Terminal
        Value: Expression
    }

module ForBinding =
    let mapPrefix mapper (binding: ForBinding) =
        { binding with Name = SymbolBinding.mapPrefix mapper binding.Name }

// Expression Statement

type ExpressionStatement = { Expression: Expression; Semicolon: Terminal }

// Return Statement

type Return =
    {
        ReturnKeyword: Terminal
        Expression: Expression
        Semicolon: Terminal
    }

// Fail Statement

type Fail =
    {
        FailKeyword: Terminal
        Expression: Expression
        Semicolon: Terminal
    }

// Let Statement

type Let =
    {
        LetKeyword: Terminal
        Binding: SymbolBinding
        Equals: Terminal
        Value: Expression
        Semicolon: Terminal
    }

// Mutable Statement

type Mutable =
    {
        MutableKeyword: Terminal
        Binding: SymbolBinding
        Equals: Terminal
        Value: Expression
        Semicolon: Terminal
    }

// Set Statement

type SetStatement =
    {
        SetKeyword: Terminal
        Binding: SymbolBinding
        Equals: Terminal
        Value: Expression
        Semicolon: Terminal
    }

// Update Statement

type UpdateStatement =
    {
        SetKeyword: Terminal
        Name: Terminal
        Operator: Terminal
        Value: Expression
        Semicolon: Terminal
    }

// Set-With Statement

type SetWith =
    {
        SetKeyword: Terminal
        Name: Terminal
        With: Terminal
        Item: Expression
        Arrow: Terminal
        Value: Expression
        Semicolon: Terminal
    }

// If Statement

type If =
    {
        IfKeyword: Terminal
        Condition: Expression
        Block: Statement Block
    }

// Elif Statement

and Elif =
    {
        ElifKeyword: Terminal
        Condition: Expression
        Block: Statement Block
    }

// Else Statement

and Else = { ElseKeyword: Terminal; Block: Statement Block }

// For Statement

and For =
    {
        ForKeyword: Terminal
        OpenParen: Terminal option
        Binding: ForBinding
        CloseParen: Terminal option
        Block: Statement Block
    }

// While Statement

and While =
    {
        WhileKeyword: Terminal
        Condition: Expression
        Block: Statement Block
    }

// Repeat Statement

// ToDo

// Until Statement

// ToDo

// Within Statement

// ToDo

// Apply Statement

// ToDo

// Qubit Declaration Statement

and QubitDeclarationCoda =
    | Semicolon of Terminal
    | Block of Statement Block

and QubitDeclaration =
    {
        Kind: QubitDeclarationKind
        Keyword: Terminal
        OpenParen: Terminal option
        Binding: QubitBinding
        CloseParen: Terminal option
        Coda: QubitDeclarationCoda
    }

// Statement

and Statement =
    | ExpressionStatement of ExpressionStatement
    | Return of Return
    | Fail of Fail
    | Let of Let
    | Mutable of Mutable
    | SetStatement of SetStatement
    | UpdateStatement of UpdateStatement
    | SetWith of SetWith
    | If of If
    | Elif of Elif
    | Else of Else
    | For of For
    | While of While
    | QubitDeclaration of QubitDeclaration
    | Unknown of Terminal

module Statement =
    let mapPrefix mapper =
        function
        | ExpressionStatement expr ->
            { expr with Expression = expr.Expression |> Expression.mapPrefix mapper } |> ExpressionStatement
        | Return returns ->
            { returns with ReturnKeyword = returns.ReturnKeyword |> Terminal.mapPrefix mapper } |> Return
        | Fail fails -> { fails with FailKeyword = fails.FailKeyword |> Terminal.mapPrefix mapper } |> Fail
        | Let lets -> { lets with LetKeyword = lets.LetKeyword |> Terminal.mapPrefix mapper } |> Let
        | Mutable mutables ->
            { mutables with MutableKeyword = mutables.MutableKeyword |> Terminal.mapPrefix mapper } |> Mutable
        | SetStatement sets ->
            { sets with SetKeyword = sets.SetKeyword |> Terminal.mapPrefix mapper } |> SetStatement
        | UpdateStatement updates ->
            { updates with SetKeyword = updates.SetKeyword |> Terminal.mapPrefix mapper } |> UpdateStatement
        | SetWith withs ->
            { withs with SetKeyword = withs.SetKeyword |> Terminal.mapPrefix mapper } |> SetWith
        | If ifs -> { ifs with IfKeyword = ifs.IfKeyword |> Terminal.mapPrefix mapper } |> If
        | Elif elifs -> { elifs with ElifKeyword = elifs.ElifKeyword |> Terminal.mapPrefix mapper } |> Elif
        | Else elses -> { elses with ElseKeyword = elses.ElseKeyword |> Terminal.mapPrefix mapper } |> Else
        | For loop -> { loop with ForKeyword = loop.ForKeyword |> Terminal.mapPrefix mapper } |> For
        | QubitDeclaration decl -> { decl with Keyword = decl.Keyword |> Terminal.mapPrefix mapper } |> QubitDeclaration
        | Unknown terminal -> Terminal.mapPrefix mapper terminal |> Unknown
