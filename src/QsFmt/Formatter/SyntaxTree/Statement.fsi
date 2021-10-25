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

/// Initializer for a single qubit.
type internal SingleQubit =
    {
        /// <summary>
        /// The <c>Qubit</c> type.
        /// </summary>
        Qubit: Terminal

        /// The opening parenthesis.
        OpenParen: Terminal

        /// The closing parenthesis.
        CloseParen: Terminal
    }

/// Initializer for an array of qubits.
type internal QubitArray =
    {
        /// <summary>
        /// The <c>Qubit</c> type.
        /// </summary>
        Qubit: Terminal

        /// The opening bracket.
        OpenBracket: Terminal

        /// The length of the created array.
        Length: Expression

        /// The closing bracket.
        CloseBracket: Terminal
    }

/// An initializer for one or more qubits.
type internal QubitInitializer =
    /// Initializes a single qubit.
    | SingleQubit of SingleQubit

    /// Initializes an array of qubits.
    | QubitArray of QubitArray

    /// Initializes a tuple of qubits.
    | QubitTuple of QubitInitializer Tuple

/// A qubit binding statement.
type internal QubitBinding =
    {
        /// The symbol binding.
        Name: SymbolBinding

        /// The equals symbol.
        Equals: Terminal

        /// The qubit initializer.
        Initializer: QubitInitializer
    }

module internal QubitBinding =
    /// <summary>
    /// Maps <paramref name="binding"/> by applying <paramref name="mapper"/> to its leftmost terminal's trivia prefix.
    /// </summary>
    val mapPrefix: mapper:(Trivia list -> Trivia list) -> binding: QubitBinding -> QubitBinding

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

/// The binding for a for-loop variable.
type internal ForBinding =
    {
        /// The symbol binding.
        Name: SymbolBinding

        /// The <c>in</c> keyword.
        In: Terminal

        /// The value of the symbol binding.
        Value: Expression
    }
    
module internal ForBinding =
    /// <summary>
    /// Maps <paramref name="binding"/> by applying <paramref name="mapper"/> to its leftmost terminal's trivia prefix.
    /// </summary>
    val mapPrefix: mapper:(Trivia list -> Trivia list) -> binding: ForBinding -> ForBinding

/// <summary>
/// An expression statement.
/// </summary>
type internal ExpressionStatement =
    {
        /// The inner expression of the statement.
        Expression: Expression

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

/// <summary>
/// A <c>fail</c> statement.
/// </summary>
type internal Fail =
    {
        /// <summary>
        /// The <c>fail</c> keyword.
        /// </summary>
        FailKeyword: Terminal

        /// The inner expression of the statement.
        Expression: Expression

        /// The semicolon.
        Semicolon: Terminal
    }

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
/// A <c>mutable</c> declaration statement.
/// </summary>
type internal Mutable =
    {
        /// <summary>
        /// The <c>mutable</c> keyword.
        /// </summary>
        MutableKeyword: Terminal

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
/// A <c>set</c> statement.
/// </summary>
type internal SetStatement =
    {
        /// <summary>
        /// The <c>set</c> keyword.
        /// </summary>
        SetKeyword: Terminal

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
/// An <c>update</c> statement.
/// </summary>
type internal UpdateStatement =
    {
        /// <summary>
        /// The <c>set</c> keyword.
        /// </summary>
        SetKeyword: Terminal

        /// The identifier being updated.
        Name: Terminal

        /// The update operator.
        Operator: Terminal

        /// The value used as the left-hand side of the update operator.
        Value: Expression

        /// The semicolon.
        Semicolon: Terminal
    }

/// <summary>
/// A <c>set w/=</c> statement.
/// </summary>
type internal SetWith =
    {
        /// <summary>
        /// The <c>set</c> keyword.
        /// </summary>
        SetKeyword: Terminal

        /// The identifier being updated.
        Name: Terminal

        /// <summary>
        /// The <c>w/=</c> operator.
        /// </summary>
        With: Terminal

        /// The expression for the index that is updated.
        Item: Expression

        /// The left arrow symbol.
        Arrow: Terminal

        /// The value used as the left-hand side of the update operator.
        Value: Expression

        /// The semicolon.
        Semicolon: Terminal
    }

/// <summary>
/// An <c>if</c> statement.
/// </summary>
type internal If =
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
/// An <c>elif</c> statement.
/// </summary>
and internal Elif =
    {
        /// <summary>
        /// The <c>elif</c> keyword.
        /// </summary>
        ElifKeyword: Terminal

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
    
/// <summary>
/// A <c>for</c> statement.
/// </summary>
and internal For =
    {
        /// <summary>
        /// The <c>for</c> keyword.
        /// </summary>
        ForKeyword: Terminal

        /// The optional open parenthesis.
        OpenParen: Terminal option

        /// The binding for loop variable.
        Binding: ForBinding

        /// The optional close parenthesis.
        CloseParen: Terminal option

        /// The loop body.
        Block: Statement Block
    }


/// The concluding section of a qubit declaration.
and internal QubitDeclarationCoda =

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

        /// Optional open parentheses.
        OpenParen: Terminal option

        /// The qubit binding.
        Binding: QubitBinding

        /// Optional close parentheses.
        CloseParen: Terminal option

        /// The concluding section.
        Coda: QubitDeclarationCoda
    }

/// A statement.
and internal Statement =

    /// An expression statement.
    | ExpressionStatement of ExpressionStatement

    /// <summary>
    /// A <c>return</c> statement.
    /// </summary>
    | Return of Return

    /// <summary>
    /// A <c>fail</c> statement.
    /// </summary>
    | Fail of Fail

    /// <summary>
    /// A <c>let</c> statement.
    /// </summary>
    | Let of Let

    /// <summary>
    /// A <c>mutable</c> declaration statement.
    /// </summary>
    | Mutable of Mutable

    /// <summary>
    /// A <c>set</c> statement.
    /// </summary>
    | SetStatement of SetStatement
    
    /// <summary>
    /// An <c>update</c> statement.
    /// </summary>
    | UpdateStatement of UpdateStatement

    /// <summary>
    /// A <c>set w/=</c> statement.
    /// </summary>
    | SetWith of SetWith

    /// <summary>
    /// An <c>if</c> statement.
    /// </summary>
    | If of If

    /// <summary>
    /// An <c>elif</c> statement.
    /// </summary>
    | Elif of Elif

    /// <summary>
    /// An <c>else</c> statement.
    /// </summary>
    | Else of Else

    /// <summary>
    /// A <c>for</c> statement.
    /// </summary>
    | For of For

    /// A qubit declaration statement.
    | QubitDeclaration of QubitDeclaration

    /// An unknown statement.
    | Unknown of Terminal

module internal Statement =
    /// <summary>
    /// Maps a statement by applying <paramref name="mapper"/> to its leftmost terminal's trivia prefix.
    /// </summary>
    val mapPrefix: mapper:(Trivia list -> Trivia list) -> Statement -> Statement
