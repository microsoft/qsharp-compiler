// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing

open System
open System.Diagnostics.CodeAnalysis
open Microsoft.Quantum.QsCompiler.SyntaxTokens

type SymbolOccurrence =
    internal
    | Declaration of QsSymbol
    | UsedType of QsType
    | UsedVariable of QsSymbol
    | UsedLiteral of QsExpression

    member Match :
        declaration: Func<QsSymbol, 'a>
        * usedType: Func<QsType, 'a>
        * usedVariable: Func<QsSymbol, 'a>
        * usedLiteral: Func<QsExpression, 'a> ->
        'a

    [<MaybeNull>]
    member AsDeclaration : QsSymbol

    [<MaybeNull>]
    member AsUsedType : QsType

    [<MaybeNull>]
    member AsUsedVariable : QsSymbol

    [<MaybeNull>]
    member AsUsedLiteral : QsExpression

module SymbolOccurrence =
    [<CompiledName "InFragment">]
    val inFragment : QsFragmentKind -> SymbolOccurrence list
