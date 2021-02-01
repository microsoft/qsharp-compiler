namespace QsFmt.Formatter.SyntaxTree

/// A copy-and-update expression.
type internal Update =
    {
        /// The record to update.
        Record: Expression

        /// <summary>
        /// The <c>w/</c> symbol.
        /// </summary>
        With: Terminal

        /// The item to update.
        Item: Expression

        /// The left arrow symbol.
        Arrow: Terminal

        /// The value to assign to the item.
        Value: Expression
    }

/// An expression.
and internal Expression =
    /// An expression that will be provided later.
    | Missing of Terminal

    /// A literal.
    | Literal of Terminal

    /// A tuple expression.
    | Tuple of Expression Tuple

    /// An operator applied to two expressions.
    | BinaryOperator of Expression BinaryOperator

    /// A copy-and-update expression.
    | Update of Update

    /// An unknown expression.
    | Unknown of Terminal
