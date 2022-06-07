// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.SyntaxTokens

type internal QsArgumentTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>

type internal ICommonTransformation =
    abstract OnLocalNameDeclaration: name: string -> string

    abstract OnLocalName: name: string -> string

    abstract OnItemNameDeclaration: name: string -> string

    abstract OnItemName: parentType: UserDefinedType * itemName: string -> string

    abstract OnArgumentTuple: argTuple: QsArgumentTuple -> QsArgumentTuple

    abstract OnAbsoluteLocation: location: QsNullable<QsLocation> -> QsNullable<QsLocation>

    abstract OnRelativeLocation: location: QsNullable<QsLocation> -> QsNullable<QsLocation>

    abstract OnSymbolLocation: offset: QsNullable<Position> * range: Range -> QsNullable<Position> * Range

    abstract OnExpressionRange: range: QsNullable<Range> -> QsNullable<Range>

    abstract OnTypeRange: range: TypeRange -> TypeRange

module internal CommonTransformation =
    let identity =
        { new ICommonTransformation with
            member _.OnLocalNameDeclaration name = name
            member _.OnLocalName name = name
            member _.OnItemNameDeclaration name = name
            member _.OnItemName(_, itemName) = itemName
            member _.OnArgumentTuple argTuple = argTuple
            member _.OnAbsoluteLocation location = location
            member _.OnRelativeLocation location = location
            member _.OnSymbolLocation(offset, range) = (offset, range)
            member _.OnExpressionRange range = range
            member _.OnTypeRange range = range
        }
