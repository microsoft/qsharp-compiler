// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

type ParameterDeclaration = { Name: Terminal; Type: TypeAnnotation }

type ParameterBinding =
    | ParameterDeclaration of ParameterDeclaration
    | ParameterTuple of ParameterBinding Tuple

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

// Update-With Statement

type UpdateWithStatement =
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

and ForStatement =
    {
        ForKeyword: Terminal
        OpenParen: Terminal option
        Binding: ForBinding
        CloseParen: Terminal option
        Block: Statement Block
    }

// Until Statement

and UntilStatementCoda =
    | Semicolon of Terminal
    | Fixup of BlockStatement

and UntilStatement =
    {
        UntilKeyword: Terminal
        Condition: Expression
        Coda: UntilStatementCoda
    }

// Qubit Declaration Statement

and QubitDeclarationStatementCoda =
    | Semicolon of Terminal
    | Block of Statement Block

and QubitDeclarationStatement =
    {
        Kind: QubitDeclarationKind
        Keyword: Terminal
        OpenParen: Terminal option
        Binding: QubitBinding
        CloseParen: Terminal option
        Coda: QubitDeclarationStatementCoda
    }

// Statement

and Statement =
    | ExpressionStatement of ExpressionStatement
    | ReturnStatement of SimpleStatement
    | FailStatement of SimpleStatement
    | LetStatement of BindingStatement
    | MutableStatement of BindingStatement
    | SetStatement of BindingStatement
    | UpdateStatement of UpdateStatement
    | UpdateWithStatement of UpdateWithStatement
    | IfStatement of ConditionalBlockStatement
    | ElifStatement of ConditionalBlockStatement
    | ElseStatement of BlockStatement
    | ForStatement of ForStatement
    | WhileStatement of ConditionalBlockStatement
    | RepeatStatement of BlockStatement
    | UntilStatement of UntilStatement
    | WithinStatement of BlockStatement
    | ApplyStatement of BlockStatement
    | QubitDeclarationStatement of QubitDeclarationStatement
    | Unknown of Terminal

module Statement =
    let mapPrefix mapper =
        function
        | ExpressionStatement expr ->
            { expr with Expression = expr.Expression |> Expression.mapPrefix mapper } |> ExpressionStatement
        | ReturnStatement returns ->
            { returns with Keyword = returns.Keyword |> Terminal.mapPrefix mapper } |> ReturnStatement
        | FailStatement fails -> { fails with Keyword = fails.Keyword |> Terminal.mapPrefix mapper } |> FailStatement
        | LetStatement lets -> { lets with Keyword = lets.Keyword |> Terminal.mapPrefix mapper } |> LetStatement
        | MutableStatement mutables ->
            { mutables with Keyword = mutables.Keyword |> Terminal.mapPrefix mapper } |> MutableStatement
        | SetStatement sets -> { sets with Keyword = sets.Keyword |> Terminal.mapPrefix mapper } |> SetStatement
        | UpdateStatement updates ->
            { updates with SetKeyword = updates.SetKeyword |> Terminal.mapPrefix mapper } |> UpdateStatement
        | UpdateWithStatement withs ->
            { withs with SetKeyword = withs.SetKeyword |> Terminal.mapPrefix mapper } |> UpdateWithStatement
        | IfStatement ifs -> { ifs with Keyword = ifs.Keyword |> Terminal.mapPrefix mapper } |> IfStatement
        | ElifStatement elifs -> { elifs with Keyword = elifs.Keyword |> Terminal.mapPrefix mapper } |> ElifStatement
        | ElseStatement elses -> { elses with Keyword = elses.Keyword |> Terminal.mapPrefix mapper } |> ElseStatement
        | ForStatement loop -> { loop with ForKeyword = loop.ForKeyword |> Terminal.mapPrefix mapper } |> ForStatement
        | WhileStatement whiles ->
            { whiles with Keyword = whiles.Keyword |> Terminal.mapPrefix mapper } |> WhileStatement
        | RepeatStatement repeats ->
            { repeats with Keyword = repeats.Keyword |> Terminal.mapPrefix mapper } |> RepeatStatement
        | UntilStatement untils ->
            { untils with UntilKeyword = untils.UntilKeyword |> Terminal.mapPrefix mapper } |> UntilStatement
        | WithinStatement withins ->
            { withins with Keyword = withins.Keyword |> Terminal.mapPrefix mapper } |> WithinStatement
        | ApplyStatement apply -> { apply with Keyword = apply.Keyword |> Terminal.mapPrefix mapper } |> ApplyStatement
        | QubitDeclarationStatement decl ->
            { decl with Keyword = decl.Keyword |> Terminal.mapPrefix mapper } |> QubitDeclarationStatement
        | Unknown terminal -> Terminal.mapPrefix mapper terminal |> Unknown
