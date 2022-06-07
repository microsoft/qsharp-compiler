// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.ParseTree

open System.Collections.Immutable
open Antlr4.Runtime
open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree
open Microsoft.Quantum.QsFmt.Parser

/// <summary>
/// Creates syntax tree <see cref="SymbolBinding"/> nodes from a parse tree and the list of tokens.
/// </summary>
type internal SymbolBindingVisitor =
    inherit SymbolBinding QSharpParserBaseVisitor

    /// <summary>
    /// Creates a new <see cref="SymbolBindingVisitor"/> with the list of <paramref name="tokens"/>.
    /// </summary>
    new: tokens: IToken ImmutableArray -> SymbolBindingVisitor

/// <summary>
/// Creates syntax tree <see cref="Expression"/> nodes from a parse tree.
/// </summary>
type internal ExpressionVisitor =
    inherit Expression QSharpParserBaseVisitor

    /// <summary>
    /// Creates a new <see cref="ExpressionVisitor"/> with the list of <paramref name="tokens"/>.
    /// </summary>
    new: tokens: IToken ImmutableArray -> ExpressionVisitor
