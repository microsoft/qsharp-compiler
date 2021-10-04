﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// The Q# formatter.
module Microsoft.Quantum.QsFmt.Formatter.Formatter

open Microsoft.Quantum.QsFmt.Formatter.Errors

/// Formats the given Q# source code.
[<CompiledName "Format">]
val format: string -> Result<string, SyntaxError list>

/// Updates deprecated syntax in the given source code.
/// Takes a Q# file name and source code.
[<CompiledName "Update">]
val update: string -> string -> Result<string, SyntaxError list>

/// Parses then un-parses the given Q# source code without formatting.
[<CompiledName "Identity">]
val identity: string -> Result<string, SyntaxError list>
