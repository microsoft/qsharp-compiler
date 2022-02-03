// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.SyntaxTokens

type QsArgumentTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>


/// <summary>
/// Contains virtual methods that are invoked
/// by several different kinds of nodes in the syntax tree.
/// </summary>
type CommonTransformationNodes internal () =

    abstract OnLocalNameDeclaration : string -> string
    default this.OnLocalNameDeclaration name = name

    abstract OnLocalName : string -> string
    default this.OnLocalName name = name

    abstract OnItemNameDeclaration : string -> string
    default this.OnItemNameDeclaration name = name

    abstract OnItemName : UserDefinedType * string -> string
    default this.OnItemName(parentType, itemName) = itemName

    abstract OnArgumentTuple : QsArgumentTuple -> QsArgumentTuple
    default this.OnArgumentTuple argTuple = argTuple

    abstract OnAbsoluteLocation : QsNullable<QsLocation> -> QsNullable<QsLocation>
    default this.OnAbsoluteLocation loc = loc

    abstract OnRelativeLocation : QsNullable<QsLocation> -> QsNullable<QsLocation>
    default this.OnRelativeLocation loc = loc

    abstract OnSymbolLocation : QsNullable<Position> * Range -> QsNullable<Position> * Range
    default this.OnSymbolLocation (offset, range) = (offset, range)

    abstract OnExpressionRange : QsNullable<Range> -> QsNullable<Range>
    default this.OnExpressionRange range = range

    abstract OnTypeRange : TypeRange -> TypeRange
    default this.OnTypeRange range = range
