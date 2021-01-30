/// Parse tree error handling.
module internal QsFmt.Formatter.Errors

open Antlr4.Runtime
open QsFmt.Parser

/// Builds a list of tokens that occur in syntax errors.
type ErrorListListener() =
    let mutable errorTokens = []

    /// The accumulated tokens that have occurred in a syntax error.
    member _.ErrorTokens = errorTokens

    interface IToken IAntlrErrorListener with
        override _.SyntaxError(_, _, token, _, _, _, _) = errorTokens <- token :: errorTokens

/// <summary>
/// Duplicates <paramref name="token"/> and sets it channel to <see cref="QSharpLexer.Hidden"/>.
/// </summary>
let private hideToken (token: IToken): IToken =
    let token' = CommonToken token
    token'.Channel <- QSharpLexer.Hidden
    upcast token'

/// <summary>
/// Maps an <see cref="IToken"/> sequence to a sequence where all <see cref="IToken"/>s equal to any token in
/// <paramref name="toHide"/> are moved to the <see cref="QSharpLexer.Hidden"/> channel.
/// </summary>
let hideTokens toHide =
    Seq.map
    <| fun token -> if toHide |> Seq.contains token then hideToken token else token
