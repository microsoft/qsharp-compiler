// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

type Conditional =
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
    | BinaryOperator of Expression BinaryOperator
    | Conditional of Conditional
    | Update of Update
    | Unknown of Terminal
