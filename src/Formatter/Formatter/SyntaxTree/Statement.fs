namespace QsFmt.Formatter.SyntaxTree

/// A declaration for a new symbol.
type internal SymbolDeclaration =
    { /// The name of the symbol.
      Name: Terminal

      /// The type of the symbol.
      Type: TypeAnnotation option }

/// A binding for one or more new symbols.
type internal SymbolBinding =
    /// A declaration for a new symbol.
    | SymbolDeclaration of SymbolDeclaration

    /// A declaration for a tuple of new symbols.
    | SymbolTuple of SymbolBinding Tuple

/// <summary>
/// A <c>let</c> statement.
/// </summary>
type internal Let =
    { /// <summary>
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
    { /// <summary>
      /// The <c>return</c> keyword.
      /// </summary>
      ReturnKeyword: Terminal

      /// The returned expression.
      Expression: Expression

      /// The semicolon.
      Semicolon: Terminal }

/// <summary>
/// An <c>if</c> statement.
/// </summary>
type internal If =
    { /// <summary>
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
    { /// <summary>
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
    let mapPrefix mapper =
        function
        | Let lets ->
            { lets with
                  LetKeyword = lets.LetKeyword |> Terminal.mapPrefix mapper }
            |> Let
        | Return returns ->
            { returns with
                  ReturnKeyword = returns.ReturnKeyword |> Terminal.mapPrefix mapper }
            |> Return
        | If ifs ->
            { ifs with
                  IfKeyword = ifs.IfKeyword |> Terminal.mapPrefix mapper }
            |> If
        | Else elses ->
            { elses with
                  ElseKeyword = elses.ElseKeyword |> Terminal.mapPrefix mapper }
            |> Else
        | Unknown terminal -> Terminal.mapPrefix mapper terminal |> Unknown
