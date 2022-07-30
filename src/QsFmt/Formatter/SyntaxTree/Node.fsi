// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

/// A trivia node is any piece of the source code that isn't relevant to the grammar.
type internal Trivia

module internal Trivia =
    /// <summary>
    /// Active pattern for <see cref="Trivia"/> nodes.
    ///
    /// <list type="table">
    ///   <item>
    ///     <term><see cref="Whitespace"/></term>
    ///     <description>A contiguous region of whitespace.</description>
    ///   </item>
    ///   <item>
    ///     <term><see cref="NewLine"/></term>
    ///     <description>A new line character.</description>
    ///   </item>
    ///   <item>
    ///     <term><see cref="Comment"/></term>
    ///     <description>A comment.</description>
    ///   </item>
    /// </list>
    /// </summary>
    val (|Whitespace|NewLine|Comment|): Trivia -> Choice<string, string, string>

    /// <summary>
    /// A <see cref="Trivia"/> node containing <paramref name="count"/> number of space characters.
    /// </summary>
    val spaces: count: int -> Trivia

    /// <summary>
    /// The new line <see cref="Trivia"/> node containing the default new line character for the current platform..
    /// </summary>
    val newLine: Trivia

    /// Determine whether a Trivia is a NewLine
    val isNewLine: Trivia -> bool

    /// Replaces each occurrence of more than one whitespace character in a row with a single space.
    val collapseSpaces: (Trivia -> Trivia)

    /// <summary>
    /// Converts a string into a list of <see cref="Trivia"/> nodes.
    /// </summary>
    /// <exception cref="System.Exception">The string contains invalid trivia.</exception>
    val ofString: string -> Trivia list

/// A terminal symbol has no child nodes and represents a token in the source code.
type internal Terminal =
    {
        /// The trivia preceding the terminal.
        Prefix: Trivia list

        /// The text content of the terminal.
        Text: string
    }

module internal Terminal =
    /// <summary>
    /// Maps <paramref name="terminal"/> by applying <paramref name="mapper"/> to its trivia prefix.
    /// </summary>
    val mapPrefix: mapper: (Trivia list -> Trivia list) -> terminal: Terminal -> Terminal

/// An item in a comma-separated sequence.
type internal 'T SequenceItem =
    {
        /// The item.
        Item: 'T option

        /// The comma following the item.
        Comma: Terminal option
    }

/// A tuple.
type internal 'T Tuple =
    {
        /// The opening parenthesis.
        OpenParen: Terminal

        /// The items in the tuple.
        Items: 'T SequenceItem list

        /// The closing parenthesis.
        CloseParen: Terminal
    }

module internal Tuple =
    /// <summary>
    /// Maps <paramref name="tuple"/> by applying <paramref name="mapper"/> to its leftmost terminal's trivia prefix.
    /// </summary>
    val mapPrefix: mapper: (Trivia list -> Trivia list) -> tuple: 'T Tuple -> 'T Tuple

/// A prefix operator. The operator is in the front of the operand.
type internal 'T PrefixOperator =
    {
        /// The operator.
        PrefixOperator: Terminal

        /// The operand.
        Operand: 'T
    }

/// A prefix operator. The operator is after the operand.
type internal 'T PostfixOperator =
    {
        /// The operand.
        Operand: 'T

        /// The operator.
        PostfixOperator: Terminal
    }

/// An infix operator.
type internal 'T InfixOperator =
    {
        /// The left-hand side.
        Left: 'T

        /// The operator.
        InfixOperator: Terminal

        /// The right-hand side.
        Right: 'T
    }

/// A block.
type internal 'T Block =
    {
        /// The opening brace.
        OpenBrace: Terminal

        /// The items in the block.
        Items: 'T list

        /// The closing brace.
        CloseBrace: Terminal
    }

module internal Block =
    /// <summary>
    /// Maps <paramref name="block"/> by applying <paramref name="mapper"/> to its leftmost terminal's trivia prefix.
    /// </summary>
    val mapPrefix: mapper: (Trivia list -> Trivia list) -> block: 'T Block -> 'T Block
