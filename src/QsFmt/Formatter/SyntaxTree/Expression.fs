// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

type Identifier = { Name: Terminal; TypeArgs: Type Tuple Option }

type InterpStringExpression =
    {
        OpenBrace: Terminal
        Expression: Expression
        CloseBrace: Terminal
    }

and InterpStringContent =
    | Text of Terminal
    | Expression of InterpStringExpression

and InterpString =
    {
        OpenQuote: Terminal
        Content: InterpStringContent list
        CloseQuote: Terminal
    }

and NewArray =
    {
        New: Terminal
        ItemType: Type
        OpenBracket: Terminal
        Length: Expression
        CloseBracket: Terminal
    }

and NamedItemAccess =
    {
        Record: Expression
        DoubleColon: Terminal
        Name: Terminal
    }

and ArrayAccess =
    {
        Array: Expression
        OpenBracket: Terminal
        Index: Expression
        CloseBracket: Terminal
    }

and Call = { Callable: Expression; Arguments: Expression Tuple }

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
    | Identifier of Identifier
    | InterpString of InterpString
    | Tuple of Expression Tuple
    | NewArray of NewArray
    | NamedItemAccess of NamedItemAccess
    | ArrayAccess of ArrayAccess
    | Call of Call
    | PrefixOperator of Expression PrefixOperator
    | PostfixOperator of Expression PostfixOperator
    | InfixOperator of Expression InfixOperator
    | Conditional of Conditional
    | FullOpenRange of Terminal
    | Update of Update
    | Unknown of Terminal
