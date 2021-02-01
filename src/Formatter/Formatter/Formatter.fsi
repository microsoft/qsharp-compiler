// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// The Q# formatter.
module QsFmt.Formatter.Formatter

/// Formats the given Q# source code.
[<CompiledName "Format">]
val format: string -> string
