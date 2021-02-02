/// Parse tree error handling.
module QsFmt.Formatter.Errors

open Antlr4.Runtime

/// A syntax error that can occur during parsing.
[<Sealed>]
type SyntaxError =
    /// The token that caused the syntax error.
    member internal Token: IToken

/// Builds a list of tokens that occur in syntax errors.
[<Sealed>]
type internal ErrorListListener =
    /// <summary>
    /// Creates a new <see cref="ErrorListListener"/>.
    /// </summary>
    new: unit -> ErrorListListener

    /// The accumulated syntax errors.
    member SyntaxErrors: SyntaxError list

    interface IToken IAntlrErrorListener

/// <summary>
/// <c>hideTokens toHide tokens</c> maps a <c>tokens</c> to a sequence where all <see cref="IToken"/>s equal to any
/// token in <c>toHide</c> are moved to the <see cref="QSharpLexer.Hidden"/> channel.
/// </summary>
val internal hideTokens: IToken seq -> (IToken seq -> IToken seq)
