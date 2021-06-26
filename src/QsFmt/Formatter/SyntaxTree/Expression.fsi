// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

/// A new array expression.
type internal NewArray =
    {
        /// The `new` keyword.
        New: Terminal

        /// The type of the creating array.
        ArrayType: Type

        /// <summary>
        /// The <c>[</c> symbol.
        /// </summary>
        OpenBracket: Terminal

        /// The length of the creating array.
        Length: Expression

        /// <summary>
        /// The <c>]</c> symbol.
        /// </summary>
        CloseBracket: Terminal
    }

/// A conditional expression.
and internal Conditional =
    {
        /// The condition.
        Condition: Expression

        /// <summary>
        /// The <c>?</c> symbol.
        /// </summary>
        Question: Terminal

        /// The expression value if the condition is true.
        IfTrue: Expression

        /// <summary>
        /// The <c>|</c> symbol.
        /// </summary>
        Pipe: Terminal

        /// The expression value if the condition is false.
        IfFalse: Expression
    }

/// A copy-and-update expression.
and internal Update =
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

    /// A new array expression.
    | NewArray of NewArray

    /// A prefix operator applied to an expression.
    | PrefixOperator of Expression PrefixOperator

    /// A postfix operator applied to an expression.
    | PostfixOperator of Expression PostfixOperator

    /// An operator applied to two expressions.
    | BinaryOperator of Expression BinaryOperator

    /// A conditional expression.
    | Conditional of Conditional

    /// A copy-and-update expression.
    | Update of Update

    /// An unknown expression.
    | Unknown of Terminal
