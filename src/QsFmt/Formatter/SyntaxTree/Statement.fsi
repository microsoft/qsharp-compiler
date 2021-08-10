// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

/// A declaration for a new parameter.
type internal ParameterDeclaration =
    {
        /// The name of the parameter.
        Name: Terminal

        /// The type of the parameter.
        Type: TypeAnnotation
    }

/// A binding for one or more new parameters.
type internal ParameterBinding =
    /// A declaration for a new parameter.
    | ParameterDeclaration of ParameterDeclaration

    /// A declaration for a tuple of new parameters.
    | ParameterTuple of ParameterBinding Tuple

/// A binding for one or more new symbols.
type internal SymbolBinding =
    /// A declaration for a new symbols.
    | SymbolDeclaration of Terminal

    /// A declaration for a tuple of new symbols.
    | SymbolTuple of SymbolBinding Tuple

module internal SymbolBinding =
    /// <summary>
    /// Maps a symbol binding by applying <paramref name="mapper"/> to its leftmost terminal's trivia prefix.
    /// </summary>
    val mapPrefix: mapper:(Trivia list -> Trivia list) -> SymbolBinding -> SymbolBinding

type internal SingleQubit = { Qubit: Terminal; OpenParen: Terminal; CloseParen: Terminal }
type internal QubitArray = { Qubit: Terminal; OpenBracket: Terminal; Length: Expression; CloseBracket: Terminal }
type internal QubitInitializer =
    | SingleQubit of SingleQubit
    | QubitArray of QubitArray
    | QubitTuple of QubitInitializer Tuple

type internal QubitBinding = { Name: SymbolBinding; Equals: Terminal; Initializer: QubitInitializer }

module internal QubitBinding =
    /// <summary>
    /// Maps <paramref name="binding"/> by applying <paramref name="mapper"/> to its leftmost terminal's trivia prefix.
    /// </summary>
    val mapPrefix: mapper:(Trivia list -> Trivia list) -> binding: QubitBinding -> QubitBinding

/// <summary>
/// A <c>let</c> statement.
/// </summary>
type internal Let =
    {
        /// <summary>
        /// The <c>let</c> keyword.
        /// </summary>
        LetKeyword: Terminal

        /// The symbol binding.
        Binding: SymbolBinding

        /// The equals symbol.
        Equals: Terminal

        /// The value of the symbol binding.
        Value: Expression

        /// The semicolon.
        Semicolon: Terminal
    }

/// <summary>
/// A <c>return</c> statement.
/// </summary>
type internal Return =
    {
        /// <summary>
        /// The <c>return</c> keyword.
        /// </summary>
        ReturnKeyword: Terminal

        /// The returned expression.
        Expression: Expression

        /// The semicolon.
        Semicolon: Terminal
    }

/// The kind of qubit declaration.
type internal QubitDeclarationKind =

    /// <summary>
    /// Indicates a <c>use</c> qubit declaration.
    /// </summary>
    | Use

    /// <summary>
    /// Indicates a <c>borrow</c> qubit declaration.
    /// </summary>
    | Borrow

/// The concluding section of a qubit declaration.
type internal QubitDeclarationCoda =

    /// The semicolon.
    | Semicolon of Terminal

    /// The block of statements after the declaration.
    | Block of Statement Block

/// A qubit declaration statement.
and internal QubitDeclaration =
    {
        /// The kind of qubit declaration.
        Kind: QubitDeclarationKind

        /// The keyword used in the declaration.
        Keyword: Terminal

        /// The qubit binding.
        Binding: QubitBinding

        /// Optional open parentheses.
        OpenParen: Terminal option

        /// Optional close parentheses.
        CloseParen: Terminal option

        /// The concluding section.
        Coda: QubitDeclarationCoda
    }

/// <summary>
/// An <c>if</c> statement.
/// </summary>
and internal If =
    {
        /// <summary>
        /// The <c>if</c> keyword.
        /// </summary>
        IfKeyword: Terminal

        /// The condition under which to execute the block.
        Condition: Expression

        /// The conditional block.
        Block: Statement Block
    }

/// <summary>
/// An <c>else</c> statement.
/// </summary>
and internal Else =
    {
        /// <summary>
        /// The <c>else</c> keyword.
        /// </summary>
        ElseKeyword: Terminal

        /// The conditional block.
        Block: Statement Block
    }

/// A statement.
and internal Statement =
    /// <summary>
    /// A <c>let</c> statement.
    /// </summary>
    | Let of Let

    /// <summary>
    /// A <c>return</c> statement.
    /// </summary>
    | Return of Return

    /// A qubit declaration statement.
    | QubitDeclaration of QubitDeclaration

    /// <summary>
    /// An <c>if</c> statement.
    /// </summary>
    | If of If

    /// <summary>
    /// An <c>else</c> statement.
    /// </summary>
    | Else of Else

    /// An unknown statement.
    | Unknown of Terminal

module internal Statement =
    /// <summary>
    /// Maps a statement by applying <paramref name="mapper"/> to its leftmost terminal's trivia prefix.
    /// </summary>
    val mapPrefix: mapper:(Trivia list -> Trivia list) -> Statement -> Statement
