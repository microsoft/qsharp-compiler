// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace QsFmt.Formatter.SyntaxTree

/// A parenthesized characteristic.
type internal CharacteristicGroup =
    {
        /// The opening parenthesis.
        OpenParen: Terminal

        /// The characteristic.
        Characteristic: Characteristic

        /// The closing parenthesis.
        CloseParen: Terminal
    }

/// A callable characteristic.
and internal Characteristic =
    /// The callable is adjointable.
    | Adjoint of Terminal

    /// The callable is controllable.
    | Controlled of Terminal

    /// A parenthesized characteristic.
    | Group of CharacteristicGroup

    /// A binary operator applied to two characteristics.
    | BinaryOperator of Characteristic BinaryOperator

/// A section attached to a callable type or declaration describing its characteristics.
type internal CharacteristicSection =
    {
        /// <summary>
        /// The <c>is</c> keyword.
        /// </summary>
        IsKeyword: Terminal

        /// The characteristic.
        Characteristic: Characteristic
    }

/// An array type.
and internal ArrayType =
    {
        /// The type of items in the array.
        ItemType: Type

        /// The opening bracket
        OpenBracket: Terminal

        /// The closing bracket.
        CloseBracket: Terminal
    }

/// A callable type.
and internal CallableType =
    {
        /// The input type.
        FromType: Type

        /// The arrow separating input and output types.
        Arrow: Terminal

        /// The output type.
        ToType: Type

        /// The characteristics.
        Characteristics: CharacteristicSection option
    }

/// A type.
and internal Type =
    /// A type that will be provided later.
    | Missing of Terminal

    /// A type parameter.
    | Parameter of Terminal

    /// A built-in type.
    | BuiltIn of Terminal

    /// A user-defined type.
    | UserDefined of Terminal

    /// A tuple type.
    | Tuple of Type Tuple

    /// An array type.
    | Array of ArrayType

    /// A callable type.
    | Callable of CallableType

    /// An unknown type.
    | Unknown of Terminal

/// A type annotation attached to a binding.
type internal TypeAnnotation =
    {
        /// The colon between the binding and the type.
        Colon: Terminal

        /// The type of the binding.
        Type: Type
    }
