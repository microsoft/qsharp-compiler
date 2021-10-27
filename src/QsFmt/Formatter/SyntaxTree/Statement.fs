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

// Return and Fail Statements

type SimpleStatement =
    {
        Keyword: Terminal
        Expression: Expression
        Semicolon: Terminal
    }

// Let, Mutable, and Set Statements

type BindingStatement =
    {
        Keyword: Terminal
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

// If, Elif, and While Statements

type ConditionalBlockStatement =
    {
        Keyword: Terminal
        Condition: Expression
        Block: Statement Block
    }

// Else, Repeat, Within, and Apply Statements

and BlockStatement = { Keyword: Terminal; Block: Statement Block }

// For Statement

and For =
    {
        ForKeyword: Terminal
        OpenParen: Terminal option
        Binding: ForBinding
        CloseParen: Terminal option
        Block: Statement Block
    }

// Until Statement

and UntilCoda =
    | Semicolon of Terminal
    | Fixup of BlockStatement

and Until =
    {
        UntilKeyword: Terminal
        Condition: Expression
        Coda: UntilCoda
    }

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
    | Return of SimpleStatement
    | Fail of SimpleStatement
    | Let of BindingStatement
    | Mutable of BindingStatement
    | SetStatement of BindingStatement
    | UpdateStatement of UpdateStatement
    | SetWith of SetWith
    | If of ConditionalBlockStatement
    | Elif of ConditionalBlockStatement
    | Else of BlockStatement
    | For of For
    | While of ConditionalBlockStatement
    | Repeat of BlockStatement
    | Until of Until
    | Within of BlockStatement
    | Apply of BlockStatement
    | QubitDeclaration of QubitDeclaration
    | Unknown of Terminal

module Statement =
    let mapPrefix mapper =
        function
        | ExpressionStatement expr ->
            { expr with Expression = expr.Expression |> Expression.mapPrefix mapper } |> ExpressionStatement
        | Return returns -> { returns with Keyword = returns.Keyword |> Terminal.mapPrefix mapper } |> Return
        | Fail fails -> { fails with Keyword = fails.Keyword |> Terminal.mapPrefix mapper } |> Fail
        | Let lets -> { lets with Keyword = lets.Keyword |> Terminal.mapPrefix mapper } |> Let
        | Mutable mutables -> { mutables with Keyword = mutables.Keyword |> Terminal.mapPrefix mapper } |> Mutable
        | SetStatement sets -> { sets with Keyword = sets.Keyword |> Terminal.mapPrefix mapper } |> SetStatement
        | UpdateStatement updates ->
            { updates with SetKeyword = updates.SetKeyword |> Terminal.mapPrefix mapper } |> UpdateStatement
        | SetWith withs -> { withs with SetKeyword = withs.SetKeyword |> Terminal.mapPrefix mapper } |> SetWith
        | If ifs -> { ifs with Keyword = ifs.Keyword |> Terminal.mapPrefix mapper } |> If
        | Elif elifs -> { elifs with Keyword = elifs.Keyword |> Terminal.mapPrefix mapper } |> Elif
        | Else elses -> { elses with Keyword = elses.Keyword |> Terminal.mapPrefix mapper } |> Else
        | For loop -> { loop with ForKeyword = loop.ForKeyword |> Terminal.mapPrefix mapper } |> For
        | While whiles -> { whiles with Keyword = whiles.Keyword |> Terminal.mapPrefix mapper } |> While
        | Repeat repeats -> { repeats with Keyword = repeats.Keyword |> Terminal.mapPrefix mapper } |> Repeat
        | Until untils -> { untils with UntilKeyword = untils.UntilKeyword |> Terminal.mapPrefix mapper } |> Until
        | Within withins -> { withins with Keyword = withins.Keyword |> Terminal.mapPrefix mapper } |> Within
        | Apply apply -> { apply with Keyword = apply.Keyword |> Terminal.mapPrefix mapper } |> Apply
        | QubitDeclaration decl -> { decl with Keyword = decl.Keyword |> Terminal.mapPrefix mapper } |> QubitDeclaration
        | Unknown terminal -> Terminal.mapPrefix mapper terminal |> Unknown
