// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module QsFmt.Formatter.Errors

open Antlr4.Runtime
open QsFmt.Parser

type SyntaxError =
    private
        {
            /// The line number.
            line: int

            /// The column number.
            column: int

            /// The token that caused the syntax error.
            token: IToken

            /// The error message.
            message: string
        }

        member error.Token = error.token

        override error.ToString() =
            sprintf "Line %d, column %d: %s" error.line error.column error.message

[<Sealed>]
type internal ErrorListListener() =
    let mutable errors = []

    member _.SyntaxErrors = errors

    interface IToken IAntlrErrorListener with
        override _.SyntaxError(_, _, token, line, column, message, _) =
            let error =
                {
                    line = line
                    column = column
                    token = token
                    message = message
                }

            errors <- error :: errors

/// <summary>
/// Duplicates <paramref name="token"/> and sets it channel to <see cref="QSharpLexer.Hidden"/>.
/// </summary>
let private hideToken (token: IToken): IToken =
    let token' = CommonToken token
    token'.Channel <- QSharpLexer.Hidden
    upcast token'

let internal hideTokens toHide =
    Seq.map <| fun token -> if toHide |> Seq.contains token then hideToken token else token
