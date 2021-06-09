// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

/// An attribute.
type internal Attribute =
    {
        /// The at symbol prefix.
        At: Terminal

        /// The attribute expression.
        Expression: Expression
    }

/// A callable declaration.
// TODO: Add type parameters, and specialization generators.
type internal CallableDeclaration =
    {
        /// The attributes attached to the callable.
        Attributes: Attribute list

        /// <summary>
        /// The declaration keyword (either <c>function</c> or <c>operation</c>).
        /// </summary>
        CallableKeyword: Terminal

        /// The name of the callable.
        Name: Terminal

        /// The parameters of the callable.
        Parameters: SymbolBinding

        /// The return type of the callable.
        ReturnType: TypeAnnotation

        /// The body of the callable.
        Block: Statement Block
    }

/// An item in a namespace.
type internal NamespaceItem =
    /// A callable declaration
    | CallableDeclaration of CallableDeclaration

    /// An unknown namespace item.
    | Unknown of Terminal

module internal NamespaceItem =
    /// <summary>
    /// Maps a namespace item by applying <paramref name="mapper"/> to its trivia prefix.
    /// </summary>
    val mapPrefix: mapper:(Trivia list -> Trivia list) -> NamespaceItem -> NamespaceItem

/// A namespace.
type internal Namespace =
    {
        /// <summary>
        /// The <c>namespace</c> keyword.
        /// </summary>
        NamespaceKeyword: Terminal

        /// The name of the namespace.
        Name: Terminal

        /// The body of the namespace.
        Block: NamespaceItem Block
    }

/// A document representing a Q# file.
type internal Document =
    {
        /// The namespaces in the document.
        Namespaces: Namespace list

        /// The end-of-file symbol.
        Eof: Terminal
    }
