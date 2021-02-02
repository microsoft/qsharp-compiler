/// Parse tree error handling.
module Microsoft.Quantum.QsFmt.Formatter.Errors

open Antlr4.Runtime

/// A syntax error that can occur during parsing.
type SyntaxError

/// Builds a list of syntax errors that occur during parsing.
[<Sealed>]
type internal ErrorListListener =
    /// <summary>
    /// Creates a new <see cref="ErrorListListener"/>.
    /// </summary>
    new: unit -> ErrorListListener

    /// The accumulated syntax errors.
    member SyntaxErrors: SyntaxError list

    interface IToken IAntlrErrorListener
