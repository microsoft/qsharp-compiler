// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

type internal CharacteristicGroup =
    {
        OpenParen: Terminal
        Characteristic: Characteristic
        CloseParen: Terminal
    }

and internal Characteristic =
    | Adjoint of Terminal
    | Controlled of Terminal
    | Group of CharacteristicGroup
    | BinaryOperator of Characteristic BinaryOperator

type internal CharacteristicSection = { IsKeyword: Terminal; Characteristic: Characteristic }

and internal ArrayType =
    {
        ItemType: Type
        OpenBracket: Terminal
        CloseBracket: Terminal
    }

and internal CallableType =
    {
        FromType: Type
        Arrow: Terminal
        ToType: Type
        Characteristics: CharacteristicSection option
    }

and internal Type =
    | Missing of Terminal
    | Parameter of Terminal
    | BuiltIn of Terminal
    | UserDefined of Terminal
    | Tuple of Type Tuple
    | Array of ArrayType
    | Callable of CallableType
    | Unknown of Terminal

type internal TypeAnnotation = { Colon: Terminal; Type: Type }
