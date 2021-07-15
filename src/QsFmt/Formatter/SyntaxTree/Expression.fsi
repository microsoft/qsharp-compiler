﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

/// An identifier expression.
/// It may represent a callable name and the type arguments specified
/// to call the callable.
type internal Identifier =
    {
        /// The name of the identifier
        Name: Terminal

        /// Optional type arguments
        TypeArgs: Type Tuple Option
    }

/// An escaped expression in an interpolated string
type internal InterpStringExpression =
    {
        /// <summary>
        /// The <c>{</c> symbol.
        /// </summary>
        OpenBrace: Terminal

        /// The escaped expression
        Expression: Expression

        /// <summary>
        /// The <c>}</c> symbol.
        /// </summary>
        CloseBrace: Terminal
    }

and internal InterpStringContent =
    /// A literal.
    | Text of Terminal

    /// An escaped expression
    | Expression of InterpStringExpression

/// An interpolated string
and internal InterpString =
    {
        /// <summary>
        /// The <c>"</c> symbol.
        /// </summary>
        OpenQuote: Terminal

        /// The content of the interpolated string
        Content: InterpStringContent list

        /// <summary>
        /// The <c>"</c> symbol.
        /// </summary>
        CloseQuote: Terminal
    }

/// A new array expression.
and internal NewArray =
    {
        /// The `new` keyword.
        New: Terminal

        /// The type of the created array.
        ItemType: Type

        /// <summary>
        /// The <c>[</c> symbol.
        /// </summary>
        OpenBracket: Terminal

        /// The length of the created array.
        Length: Expression

        /// <summary>
        /// The <c>]</c> symbol.
        /// </summary>
        CloseBracket: Terminal
    }

/// A named-item-access expression.
and internal NamedItemAccess =
    {
        /// The accessing object
        Record: Expression

        /// <summary>
        /// The <c>::</c> symbol.
        /// </summary>
        DoubleColon: Terminal

        /// The accessed item name
        Name: Terminal
    }

/// An array-item-access expression.
and internal ArrayAccess =
    {
        /// The array
        Array: Expression

        /// <summary>
        /// The <c>[</c> symbol.
        /// </summary>
        OpenBracket: Terminal

        /// The index of the accessed item.
        Index: Expression

        /// <summary>
        /// The <c>]</c> symbol.
        /// </summary>
        CloseBracket: Terminal
    }

/// A callable-call expression.
and internal Call =
    {
        /// The callable being called.
        Callable: Expression

        /// The argument list of the callable call.
        Arguments: Expression Tuple
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

    /// An identifier expression.
    | Identifier of Identifier

    /// An interpolated string
    | InterpString of InterpString

    /// A tuple expression.
    | Tuple of Expression Tuple

    /// A new array expression.
    | NewArray of NewArray

    /// A named-item-access expression.
    | NamedItemAccess of NamedItemAccess

    /// An array-item-access expression.
    | ArrayAccess of ArrayAccess

    /// A callable-call expression.
    | Call of Call

    /// A prefix operator applied to an expression.
    | PrefixOperator of Expression PrefixOperator

    /// A postfix operator applied to an expression.
    | PostfixOperator of Expression PostfixOperator

    /// An infix operator applied to two expressions.
    | InfixOperator of Expression InfixOperator

    /// A conditional expression.
    | Conditional of Conditional

    /// A full-open-range expression.
    | FullOpenRange of Terminal

    /// A copy-and-update expression.
    | Update of Update

    /// An unknown expression.
    | Unknown of Terminal
