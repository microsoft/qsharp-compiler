// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.ParseTree

open System.Collections.Immutable
open Antlr4.Runtime
open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree
open Microsoft.Quantum.QsFmt.Parser

/// <summary>
/// Creates syntax tree <see cref="Statement"/> nodes from a parse tree.
/// </summary>
type internal StatementVisitor =
    inherit Statement QSharpParserBaseVisitor

    /// <summary>
    /// Creates a new <see cref="StatementVisitor"/> with the list of <paramref name="tokens"/>.
    /// </summary>
    new: tokens: IToken ImmutableArray -> StatementVisitor

    /// <summary>
    /// Creates a <see cref="Block"/> of statements from the <paramref name="scope"/>.
    /// </summary>
    member internal CreateBlock: scope: QSharpParser.ScopeContext -> Statement Block
