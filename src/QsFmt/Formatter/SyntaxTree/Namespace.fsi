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

/// A type parameter binding sequence.
type internal TypeParameterBinding =
    {
        /// The opening angle bracket.
        OpenBracket: Terminal

        /// The type parameters.
        Parameters: Terminal SequenceItem list

        /// The closing angle bracket.
        CloseBracket: Terminal
    }

/// A specialization generator.
type internal SpecializationGenerator =
    /// A built-in generator.
    | BuiltIn of name: Terminal * semicolon: Terminal

    /// A provided specialization.
    | Provided of parameters: Terminal option * statements: Statement Block

/// A specialization.
type internal Specialization =
    {
        /// The names of the specialization.
        Names: Terminal list

        /// The specialization generator.
        Generator: SpecializationGenerator
    }

/// The body of a callable declaration.
type internal CallableBody =
    /// An implicit body specialization with statements.
    | Statements of Statement Block

    /// A block of specializations.
    | Specializations of Specialization Block

/// A callable declaration.
// TODO: Add specialization generators.
type internal CallableDeclaration =
    {
        /// The attributes attached to the callable.
        Attributes: Attribute list

        /// The access modifier for the callable.
        Access: Terminal option

        /// <summary>
        /// The declaration keyword (either <c>function</c> or <c>operation</c>).
        /// </summary>
        CallableKeyword: Terminal

        /// The name of the callable.
        Name: Terminal

        /// The type parameters of the callable.
        TypeParameters: TypeParameterBinding option

        /// The parameters of the callable.
        Parameters: SymbolBinding

        /// The return type of the callable.
        ReturnType: TypeAnnotation

        /// The characteristic section of the callable.
        CharacteristicSection: CharacteristicSection option

        /// The body of the callable.
        Body: CallableBody
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
