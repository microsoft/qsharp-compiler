// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.ParseTree

open System.Collections.Immutable
open Antlr4.Runtime
open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree
open Microsoft.Quantum.QsFmt.Parser

/// <summary>
/// Constructors for syntax tree <see cref="Namespace"/> and <see cref="Document"/> nodes.
/// </summary>
module internal Namespace =
    /// <summary>
    /// Creates a syntax tree <see cref="Document"/> node from the parse tree <see cref="QSharpParser.DocumentContext"/>
    /// node and the list of <paramref name="tokens"/>.
    /// </summary>
    val toDocument: tokens: IToken ImmutableArray -> context: QSharpParser.DocumentContext -> Document
