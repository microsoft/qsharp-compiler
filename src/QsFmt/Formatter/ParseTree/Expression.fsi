// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.ParseTree

open Antlr4.Runtime
open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree
open Microsoft.Quantum.QsFmt.Parser
open System.Collections.Immutable

/// <summary>
/// Creates syntax tree <see cref="Expression"/> nodes from a parse tree.
/// </summary>
type internal ExpressionVisitor =
    /// <summary>
    /// Creates a new <see cref="ExpressionVisitor"/> with the list of <paramref name="tokens"/>.
    /// </summary>
    new: tokens:IToken ImmutableArray -> ExpressionVisitor

    inherit Expression QSharpParserBaseVisitor
