// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.Formatter.Printer

open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree
open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree.Trivia
open System

/// <summary>
/// Prints a <see cref="Trivia"/> node to a string.
/// </summary>
let printTrivia =
    function
    | Whitespace ws -> ws
    | NewLine -> Environment.NewLine
    | Comment comment -> comment

/// <summary>
/// Prints a <see cref="Trivia"/> prefix list to a string.
/// </summary>
let printPrefix = List.map printTrivia >> String.concat ""

let printer =
    { new Reducer<_>() with
        override _.Combine(x, y) = x + y

        override _.Terminal terminal =
            printPrefix terminal.Prefix + terminal.Text
    }
