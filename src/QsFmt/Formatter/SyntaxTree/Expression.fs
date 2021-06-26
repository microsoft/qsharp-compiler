// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

type NewArray =
    {
        New: Terminal
        ArrayType: Type
        OpenBracket: Terminal
        Length: Expression
        CloseBracket: Terminal
    }

and Conditional =
    {
        Condition: Expression
        Question: Terminal
        IfTrue: Expression
        Pipe: Terminal
        IfFalse: Expression
    }

and Update =
    {
        Record: Expression
        With: Terminal
        Item: Expression
        Arrow: Terminal
        Value: Expression
    }

and Expression =
    | Missing of Terminal
    | Literal of Terminal
    | Tuple of Expression Tuple
    | NewArray of NewArray
    | PrefixOperator of Expression PrefixOperator
    | PostfixOperator of Expression PostfixOperator
    | BinaryOperator of Expression BinaryOperator
    | Conditional of Conditional
    | Update of Update
    | Unknown of Terminal
