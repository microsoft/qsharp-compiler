// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// The Q# formatter.
module Microsoft.Quantum.QsFmt.Formatter.Formatter

open System
open Microsoft.Quantum.QsFmt.Formatter.Errors

/// Performs a format of the given Q# source code.
/// If the contents were changed, writes to file, and returns true.
val performFormat: string -> Version option -> string -> Result<bool, SyntaxError list>

/// Formats the given Q# source code.
[<CompiledName "Format">]
val format: Version option -> string -> Result<string, SyntaxError list>

/// Performs an update of the deprecated syntax in the given source code.
/// If the contents were changed, writes to file, and returns true.
val performUpdate: string -> Version option -> string -> Result<bool, SyntaxError list>

/// Updates deprecated syntax in the given source code.
[<CompiledName "Update">]
val update: string -> Version option -> string -> Result<string, SyntaxError list>

/// Performs an update of deprecated syntax in the given source code and then performs a format of it.
/// If the contents were changed, writes to file.
/// If the update changed the contents, the first returned boolean is true.
/// If the format changed the contents, the second returned boolean is true.
val performUpdateAndFormat: string -> Version option -> string -> Result<bool * bool, SyntaxError list>

/// Updates deprecated syntax in the given source code and formats it.
[<CompiledName "UpdateAndFormat">]
val updateAndFormat: string -> Version option -> string -> Result<string, SyntaxError list>

/// Parses then un-parses the given Q# source code without formatting.
[<CompiledName "Identity">]
val identity: string -> Result<string, SyntaxError list>
