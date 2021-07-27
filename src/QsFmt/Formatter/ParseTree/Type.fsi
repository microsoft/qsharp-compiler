// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.ParseTree

open Antlr4.Runtime
open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree
open Microsoft.Quantum.QsFmt.Parser
open System.Collections.Immutable

/// <summary>
/// Creates syntax tree <see cref="Characteristic"/> nodes from a parse tree.
/// </summary>
type internal CharacteristicVisitor =
    /// <summary>
    /// Creates a new <see cref="CharacteristicVisitor"/> with the list of <paramref name="tokens"/>.
    /// </summary>
    new : tokens: IToken ImmutableArray -> CharacteristicVisitor

    inherit Characteristic QSharpParserBaseVisitor

/// <summary>
/// Creates syntax tree <see cref="Type"/> nodes from a parse tree.
/// </summary>
type internal TypeVisitor =
    /// <summary>
    /// Creates a new <see cref="TypeVisitor"/> with the list of <paramref name="tokens"/>.
    /// </summary>
    new : tokens: IToken ImmutableArray -> TypeVisitor

    inherit Type QSharpParserBaseVisitor

/// <summary>
/// Constructors for syntax tree <see cref="CharacteristicSection"/> node.
/// </summary>
module internal Type =
    /// <summary>
    /// Creates a syntax tree <see cref="CharacteristicSection"/> node from the parse tree <see cref="QSharpParser.CharacteristicsContext"/>
    /// node and the list of <paramref name="tokens"/>.
    /// </summary>
    val toCharacteristicSection :
        tokens: IToken ImmutableArray -> context: QSharpParser.CharacteristicsContext -> CharacteristicSection
