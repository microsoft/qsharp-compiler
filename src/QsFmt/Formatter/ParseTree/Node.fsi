// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// Tools for creating syntax tree nodes from parse tree nodes.
module internal Microsoft.Quantum.QsFmt.Formatter.ParseTree.Node

open Antlr4.Runtime
open Antlr4.Runtime.Tree
open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree
open System.Collections.Immutable

/// <summary>
/// The <see cref="Trivia"/> tokens that occur before the token with the given <paramref name="index"/> in
/// <paramref name="tokens"/>.
/// </summary>
val prefix: tokens:IToken ImmutableArray -> index:int -> Trivia list

/// <summary>
/// Creates a syntax tree <see cref="Terminal"/> node from the given parse tree <paramref name="terminal"/>.
/// </summary>
val toTerminal: tokens:IToken ImmutableArray -> terminal:IToken -> Terminal

/// <summary>
/// Creates a syntax tree <see cref="Terminal"/> node from the given parse tree <paramref name="node"/> that represents
/// unknown or not yet supported syntax.
/// </summary>
val toUnknown: tokens:IToken ImmutableArray -> node:IRuleNode -> Terminal

/// <summary>
/// Creates a list of sequence items by pairing each item in <paramref name="items"/> with its respective comma in
/// <paramref name="commas"/>.
/// </summary>
val tupleItems: items:'a seq -> commas:Terminal seq -> 'a SequenceItem list
