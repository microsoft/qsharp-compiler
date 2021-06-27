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

and NamedItemAccess =
    {
        Object: Expression
        Colon: Terminal
        Name: Terminal
    }

and ArrayAccess =
    {
        Array: Expression
        OpenBracket: Terminal
        Index: Expression
        CloseBracket: Terminal
    }

and Call =
    {
        Function: Expression
        Arguments: Expression Tuple
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
    | NamedItemAccess of NamedItemAccess
    | ArrayAccess of ArrayAccess
    | Call of Call
    | PrefixOperator of Expression PrefixOperator
    | PostfixOperator of Expression PostfixOperator
    | BinaryOperator of Expression BinaryOperator
    | Conditional of Conditional
    | Update of Update
    | Unknown of Terminal
