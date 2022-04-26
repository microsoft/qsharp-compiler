// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

type Identifier = { Name: Terminal; TypeArgs: Type Tuple Option }

type SymbolBinding =
    | SymbolDeclaration of Terminal
    | SymbolTuple of SymbolBinding Tuple

module SymbolBinding =
    let mapPrefix mapper =
        function
        | SymbolDeclaration terminal -> terminal |> Terminal.mapPrefix mapper |> SymbolDeclaration
        | SymbolTuple tuple -> tuple |> Tuple.mapPrefix mapper |> SymbolTuple

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

and NewSizedArray =
    {
        OpenBracket: Terminal
        Value: Expression
        Comma: Terminal
        Size: Terminal
        Equals: Terminal
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

and Lambda =
    {
        Binding: SymbolBinding
        Arrow: Terminal
        Body: Expression
    }

and Expression =
    | Missing of Terminal
    | Literal of Terminal
    | Identifier of Identifier
    | InterpString of InterpString
    | Tuple of Expression Tuple
    | NewArray of NewArray
    | NewSizedArray of NewSizedArray
    | NamedItemAccess of NamedItemAccess
    | ArrayAccess of ArrayAccess
    | Call of Call
    | PrefixOperator of Expression PrefixOperator
    | PostfixOperator of Expression PostfixOperator
    | InfixOperator of Expression InfixOperator
    | Conditional of Conditional
    | FullOpenRange of Terminal
    | Update of Update
    | Lambda of Lambda
    | Unknown of Terminal

module Expression =
    let rec mapPrefix mapper =
        function
        | Missing terminal -> Terminal.mapPrefix mapper terminal |> Unknown
        | Literal terminal -> Terminal.mapPrefix mapper terminal |> Unknown
        | Identifier identifier -> { identifier with Name = identifier.Name |> Terminal.mapPrefix mapper } |> Identifier
        | InterpString interpString ->
            { interpString with OpenQuote = interpString.OpenQuote |> Terminal.mapPrefix mapper }
            |> InterpString
        | Tuple tuple -> { tuple with OpenParen = tuple.OpenParen |> Terminal.mapPrefix mapper } |> Tuple
        | NewArray newArray -> { newArray with New = newArray.New |> Terminal.mapPrefix mapper } |> NewArray
        | NewSizedArray newSizedArray ->
            { newSizedArray with OpenBracket = newSizedArray.OpenBracket |> Terminal.mapPrefix mapper }
            |> NewSizedArray
        | NamedItemAccess namedItemAccess ->
            { namedItemAccess with Record = namedItemAccess.Record |> mapPrefix mapper } |> NamedItemAccess
        | ArrayAccess arrayAccess -> { arrayAccess with Array = arrayAccess.Array |> mapPrefix mapper } |> ArrayAccess
        | Call call -> { call with Callable = call.Callable |> mapPrefix mapper } |> Call
        | PrefixOperator prefixOperator ->
            { prefixOperator with PrefixOperator = prefixOperator.PrefixOperator |> Terminal.mapPrefix mapper }
            |> PrefixOperator
        | PostfixOperator postfixOperator ->
            { postfixOperator with Operand = postfixOperator.Operand |> mapPrefix mapper } |> PostfixOperator
        | InfixOperator infixOperator ->
            { infixOperator with Left = infixOperator.Left |> mapPrefix mapper } |> InfixOperator
        | Conditional conditional ->
            { conditional with Condition = conditional.Condition |> mapPrefix mapper } |> Conditional
        | FullOpenRange terminal -> Terminal.mapPrefix mapper terminal |> Unknown
        | Update update -> { update with Record = update.Record |> mapPrefix mapper } |> Update
        | Lambda lambda -> { lambda with Binding = SymbolBinding.mapPrefix mapper lambda.Binding } |> Lambda
        | Unknown terminal -> Terminal.mapPrefix mapper terminal |> Unknown
