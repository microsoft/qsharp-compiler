// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// The Q# formatter.
module Microsoft.Quantum.QsFmt.Formatter.Formatter

open Microsoft.Quantum.QsFmt.Formatter.Errors

/// Parses the Q# source code into a <see cref="QsFmt.Formatter.SyntaxTree.Document"/>.
val parse: string -> Result<SyntaxTree.Document, SyntaxError list>

/// UnParses a <see cref="QsFmt.Formatter.SyntaxTree.Document"/> into Q# code.
val unparse: SyntaxTree.Document -> string

/// Formats a <see cref="QsFmt.Formatter.SyntaxTree.Document"/> and unparses it into Q# code.
val formatDocument: SyntaxTree.Document -> string

/// Formats the given Q# source code.
[<CompiledName "Format">]
val format: string -> Result<string, SyntaxError list>

/// Parses then un-parses the given Q# source code without formatting.
[<CompiledName "Identity">]
val identity: string -> Result<string, SyntaxError list>
