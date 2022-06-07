// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing

open System
open System.Diagnostics.CodeAnalysis
open Microsoft.Quantum.QsCompiler.SyntaxTokens

/// An occurrence of a symbol in Q# code, and a tag describing how the symbol is used in context.
type SymbolOccurrence =
    internal
    | Declaration of QsSymbol
    | UsedType of QsType
    | UsedVariable of QsSymbol
    | UsedLiteral of QsExpression

    /// <summary>
    /// Matches the kind of symbol occurrence.
    /// </summary>
    /// <param name="declaration">Called when this occurrence is a declaration.</param>
    /// <param name="usedType">Called when this occurrence is a used type.</param>
    /// <param name="usedVariable">Called when this occurrence is a used variable.</param>
    /// <param name="usedLiteral">Called when this occurrence is a used literal.</param>
    /// <returns>The result of the called match function.</returns>
    member Match:
        declaration: Func<QsSymbol, 'a> *
        usedType: Func<QsType, 'a> *
        usedVariable: Func<QsSymbol, 'a> *
        usedLiteral: Func<QsExpression, 'a> ->
            'a

    /// <summary>
    /// Gets the occurring symbol if this occurrence is a declaration.
    /// </summary>
    member TryGetDeclaration: symbol: QsSymbol outref -> bool

    /// <summary>
    /// Gets the occurring type if this occurrence is a used type.
    /// </summary>
    member TryGetUsedType: ``type``: QsType outref -> bool

    /// <summary>
    /// Gets the occurring symbol if this occurrence is a used variable.
    /// </summary>
    member TryGetUsedVariable: symbol: QsSymbol outref -> bool

    /// <summary>
    /// Gets the occurring expression if this occurrence is a used literal.
    /// </summary>
    member TryGetUsedLiteral: expression: QsExpression outref -> bool

module SymbolOccurrence =
    /// <summary>
    /// A list of all symbols occurring in <paramref name="fragment"/>.
    /// </summary>
    /// <param name="fragment">The fragment to extract symbols from.</param>
    /// <returns>The list of symbols.</returns>
    [<CompiledName "InFragment">]
    val inFragment: fragment: QsFragmentKind -> SymbolOccurrence list
