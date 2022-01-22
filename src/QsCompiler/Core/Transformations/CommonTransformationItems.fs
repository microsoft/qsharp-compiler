// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTree


type CommonTransformationItems internal () =

    abstract OnLocalNameDeclaration : string -> string
    default this.OnLocalNameDeclaration name = name

    abstract OnLocalName : string -> string
    default this.OnLocalName name = name

    abstract OnItemName : UserDefinedType * string -> string
    default this.OnItemName(parentType, itemName) = itemName

    abstract OnRelativeLocation : QsNullable<QsLocation> -> QsNullable<QsLocation>
    default this.OnRelativeLocation loc = loc

    abstract OnAbsoluteLocation : QsNullable<QsLocation> -> QsNullable<QsLocation>
    default this.OnAbsoluteLocation l = l

    abstract OnItemNameDeclaration : string -> string
    default this.OnItemNameDeclaration name = name
