// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.Formatter.Errors

open Antlr4.Runtime

type SyntaxError =
    {
        /// The line number.
        line: int

        /// The character number.
        character: int

        /// The error message.
        message: string
    }

    override error.ToString() =
        sprintf "Line %d, character %d: %s" error.line error.character error.message

[<Sealed>]
type ListErrorListener() =
    let mutable errors = []

    member _.SyntaxErrors = errors

    interface IToken IAntlrErrorListener with
        member _.SyntaxError(_, _, _, line, character, message, _) =
            let error =
                {
                    line = line
                    character = character
                    message = message
                }

            errors <- error :: errors
