// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

/// A declaration for a new symbol.
type internal SymbolDeclaration =
    {
      /// The name of the symbol.
      Name: Terminal

      /// The type of the symbol.
      Type: TypeAnnotation option }

/// A binding for one or more new symbols.
type internal SymbolBinding =
    /// A declaration for a new symbol.
    | SymbolDeclaration of SymbolDeclaration

    /// A declaration for a tuple of new symbols.
    | SymbolTuple of SymbolBinding Tuple

type internal QubitSymbolBinding =
    | QubitSymbolDeclaration of Terminal
    | QubitSymbolTuple of QubitSymbolBinding Tuple

type internal SingleQubit =
    { Qubit: Terminal
      OpenParen: Terminal
      CloseParen: Terminal }

type internal QubitArray =
    { Qubit: Terminal
      OpenBracket: Terminal
      Length: Expression
      CloseBracket: Terminal }

type internal QubitInitializer =
    | SingleQubit of SingleQubit
    | QubitArray of QubitArray
    | QubitTuple of QubitInitializer Tuple

type internal QubitBinding =
    { Name: QubitSymbolBinding
      Equals: Terminal
      Initializer: QubitInitializer }

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
      Semicolon: Terminal }

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
      Semicolon: Terminal }

/// <summary>
/// A <c>use</c> statement.
/// </summary>
type internal Use =
    {
      /// <summary>
      /// The <c>use</c> keyword.
      /// </summary>
      UseKeyword: Terminal

      /// The qubit binding.
      Binding: QubitBinding

      /// Optional open parentheses
      OpenParen: Terminal option

      /// Optional close parentheses
      CloseParen: Terminal option

      /// The semicolon.
      Semicolon: Terminal }

/// <summary>
/// A <c>borrow</c> statement.
/// </summary>
type internal Borrow =
    {
      /// <summary>
      /// The <c>borrow</c> keyword.
      /// </summary>
      BorrowKeyword: Terminal

      /// The qubit binding.
      Binding: QubitBinding

      /// Optional open parentheses
      OpenParen: Terminal option

      /// Optional close parentheses
      CloseParen: Terminal option

      /// The semicolon.
      Semicolon: Terminal }

/// <summary>
/// A <c>use</c> statement preceding a block.
/// </summary>
type internal UseBlock =
    {
      /// <summary>
      /// The <c>use</c> keyword.
      /// </summary>
      UseKeyword: Terminal

      /// The qubit binding.
      Binding: QubitBinding

      /// Optional open parentheses
      OpenParen: Terminal option

      /// Optional close parentheses
      CloseParen: Terminal option

      /// The block of statements after the use.
      Block: Statement Block }

/// <summary>
/// A <c>borrow</c> statement preceding a block.
/// </summary>
and internal BorrowBlock =
    {
      /// <summary>
      /// The <c>borrow</c> keyword.
      /// </summary>
      BorrowKeyword: Terminal

      /// The qubit binding.
      Binding: QubitBinding

      /// Optional open parentheses
      OpenParen: Terminal option

      /// Optional close parentheses
      CloseParen: Terminal option

      /// The block of statements after the borrow.
      Block: Statement Block }

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
      Block: Statement Block }

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
      Block: Statement Block }

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

    /// <summary>
    /// A <c>use</c> statement.
    /// </summary>
    | Use of Use

    /// <summary>
    /// A <c>use</c> statement preceding a block.
    /// </summary>
    | UseBlock of UseBlock

    /// <summary>
    /// A <c>borrow</c> statement.
    /// </summary>
    | Borrow of Borrow

    /// <summary>
    /// A <c>borrow</c> statement preceding a block.
    /// </summary>
    | BorrowBlock of BorrowBlock

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
    /// Maps a statement by applying <paramref name="mapper"/> to its trivia prefix.
    /// </summary>
    val mapPrefix : mapper: (Trivia list -> Trivia list) -> Statement -> Statement
