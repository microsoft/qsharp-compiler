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
    val mapPrefix: mapper: (Trivia list -> Trivia list) -> binding: QubitBinding -> QubitBinding

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
    val mapPrefix: mapper: (Trivia list -> Trivia list) -> binding: ForBinding -> ForBinding

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

/// A simple statement consisting of just a keyword, an expression, and a semicolon.
type internal SimpleStatement =
    {
        /// The keyword for the statement.
        Keyword: Terminal

        /// The returned expression.
        Expression: Expression

        /// The semicolon.
        Semicolon: Terminal
    }

/// A statement used to bind a value to a symbol.
type internal BindingStatement =
    {
        /// The keyword for the statement.
        Keyword: Terminal

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
/// An <c>update</c> statement, also known as an evaluate-and-reassign statement.
/// e.g. <c>set x += 3;</c>
/// </summary>
type internal UpdateStatement =
    {
        /// <summary>
        /// The <c>set</c> keyword.
        /// </summary>
        SetKeyword: Terminal

        /// The identifier being updated.
        Name: Terminal

        /// The update operator, e.g. <c>+=</c>.
        Operator: Terminal

        /// The value used as the left-hand side of the update operator.
        Value: Expression

        /// The semicolon.
        Semicolon: Terminal
    }

/// <summary>
/// A <c>set w/=</c> statement, also known as an evaluate-and-reassign statement.
/// </summary>
type internal UpdateWithStatement =
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

/// A statement consisting of a keyword, a condition, and a block.
type internal ConditionalBlockStatement =
    {

        /// The keyword for the statement.
        Keyword: Terminal

        /// The condition under which to execute the block.
        Condition: Expression

        /// The conditional block.
        Block: Statement Block
    }

/// A statement consisting of a keyword and a block.
and internal BlockStatement =
    {
        /// The keyword for the statement.
        Keyword: Terminal

        /// The block.
        Block: Statement Block
    }

/// <summary>
/// A <c>for</c> statement.
/// </summary>
and internal ForStatement =
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

/// <summary>
/// The concluding section of an <c>until</c> statement.
/// </summary>
and internal UntilStatementCoda =

    /// The semicolon.
    | Semicolon of Terminal

    /// <summary>
    /// The <c>fixup</c> of an <c>until</c> statement.
    /// </summary>
    | Fixup of BlockStatement

/// <summary>
/// An <c>until</c> statement.
/// </summary>
and internal UntilStatement =
    {
        /// <summary>
        /// The <c>until</c> keyword.
        /// </summary>
        UntilKeyword: Terminal

        /// <summary>
        /// The condition under which to exit the preceding <c>repeat</c> block.
        /// </summary>
        Condition: Expression

        /// <summary>
        /// The concluding section, possibly containing a <c>fixup</c> block.
        /// </summary>
        Coda: UntilStatementCoda
    }

/// The concluding section of a qubit declaration.
and internal QubitDeclarationStatementCoda =

    /// The semicolon.
    | Semicolon of Terminal

    /// The block of statements after the declaration.
    | Block of Statement Block

/// A qubit declaration statement.
and internal QubitDeclarationStatement =
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
        Coda: QubitDeclarationStatementCoda
    }

/// A statement.
and internal Statement =

    /// An expression statement.
    | ExpressionStatement of ExpressionStatement

    /// <summary>
    /// A <c>return</c> statement.
    /// </summary>
    | ReturnStatement of SimpleStatement

    /// <summary>
    /// A <c>fail</c> statement.
    /// </summary>
    | FailStatement of SimpleStatement

    /// <summary>
    /// A <c>let</c> statement.
    /// </summary>
    | LetStatement of BindingStatement

    /// <summary>
    /// A <c>mutable</c> declaration statement.
    /// </summary>
    | MutableStatement of BindingStatement

    /// <summary>
    /// A <c>set</c> statement.
    /// </summary>
    | SetStatement of BindingStatement

    /// <summary>
    /// An <c>update</c> statement, also known as an evaluate-and-reassign statement.
    /// e.g. <c>set x += 3;</c>
    /// </summary>
    | UpdateStatement of UpdateStatement

    /// <summary>
    /// A <c>set w/=</c> statement, also known as an evaluate-and-reassign statement.
    /// </summary>
    | UpdateWithStatement of UpdateWithStatement

    /// <summary>
    /// An <c>if</c> statement.
    /// </summary>
    | IfStatement of ConditionalBlockStatement

    /// <summary>
    /// An <c>elif</c> statement.
    /// </summary>
    | ElifStatement of ConditionalBlockStatement

    /// <summary>
    /// An <c>else</c> statement.
    /// </summary>
    | ElseStatement of BlockStatement

    /// <summary>
    /// A <c>for</c> statement.
    /// </summary>
    | ForStatement of ForStatement

    /// <summary>
    /// A <c>while</c> statement.
    /// </summary>
    | WhileStatement of ConditionalBlockStatement

    /// <summary>
    /// A <c>repeat</c> statement.
    /// </summary>
    | RepeatStatement of BlockStatement

    /// <summary>
    /// An <c>until</c> statement.
    /// </summary>
    | UntilStatement of UntilStatement

    /// <summary>
    /// A <c>within</c> statement.
    /// </summary>
    | WithinStatement of BlockStatement

    /// <summary>
    /// An <c>apply</c> statement.
    /// </summary>
    | ApplyStatement of BlockStatement

    /// A qubit declaration statement.
    | QubitDeclarationStatement of QubitDeclarationStatement

    /// An unknown statement.
    | Unknown of Terminal

module internal Statement =
    /// <summary>
    /// Maps a statement by applying <paramref name="mapper"/> to its leftmost terminal's trivia prefix.
    /// </summary>
    val mapPrefix: mapper: (Trivia list -> Trivia list) -> Statement -> Statement
