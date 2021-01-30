/// The Q# formatter.
module QsFmt.Formatter.Formatter

/// Formats the given Q# source code.
[<CompiledName "Format">]
val format: string -> string
