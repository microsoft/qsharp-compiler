// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// Syntax tree printing.
module internal Microsoft.Quantum.QsFmt.Formatter.Printer

open System

open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

/// <summary>
/// Prints a <see cref="Trivia"/> node to a string.
/// </summary>
let private printTrivia =
    function
    | Whitespace ws -> Whitespace.toString ws
    | NewLine -> Environment.NewLine
    | Comment comment -> Comment.toString comment

/// <summary>
/// Prints a <see cref="Trivia"/> prefix list to a string.
/// </summary>
let private printPrefix = List.map printTrivia >> String.concat ""

/// Prints a syntax tree to a string.
let printer =
    { new Reducer<_>() with
        override _.Combine(x, y) = x + y

        override _.Terminal terminal =
            printPrefix terminal.Prefix + terminal.Text
    }
