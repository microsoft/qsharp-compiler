// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.ParseTree

open Antlr4.Runtime
open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree
open Microsoft.Quantum.QsFmt.Parser
open System.Collections.Immutable

/// <summary>
/// Creates syntax tree <see cref="Type"/> nodes from a parse tree.
/// </summary>
type internal TypeVisitor =
    /// <summary>
    /// Creates a new <see cref="TypeVisitor"/> with the list of <paramref name="tokens"/>.
    /// </summary>
    new: tokens:IToken ImmutableArray -> TypeVisitor

    inherit Type QSharpParserBaseVisitor
