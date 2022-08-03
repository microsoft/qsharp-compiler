// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.Formatter.Errors

open Antlr4.Runtime

type SyntaxError =
    {
        /// The line number.
        Line: int

        /// The character number.
        Character: int

        /// The error message.
        Message: string
    }

    override error.ToString() =
        sprintf "Line %d, Character %d: %s" error.Line error.Character error.Message

[<Sealed>]
type ListErrorListener() =
    let mutable errors = []

    member _.SyntaxErrors = errors

    interface IToken IAntlrErrorListener with
        member _.SyntaxError(_, _, _, line, character, message, _) =
            let error =
                {
                    Line = line
                    Character = character
                    Message = message
                }

            errors <- error :: errors
