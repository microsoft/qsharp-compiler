// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

type Attribute = { At: Terminal; Expression: Expression }

type TypeParameterBinding =
    {
        OpenBracket: Terminal
        Parameters: Terminal SequenceItem list
        CloseBracket: Terminal
    }

type SpecializationGenerator =
    | BuiltIn of name: Terminal * semicolon: Terminal
    | Provided of parameters: Terminal option * statements: Statement Block

type Specialization = { Names: Terminal list; Generator: SpecializationGenerator }

type CallableBody =
    | Statements of Statement Block
    | Specializations of Specialization Block

type CallableDeclaration =
    {
        Attributes: Attribute list
        Access: Terminal option
        CallableKeyword: Terminal
        Name: Terminal
        TypeParameters: TypeParameterBinding option
        Parameters: SymbolBinding
        ReturnType: TypeAnnotation
        CharacteristicSection: CharacteristicSection option
        Body: CallableBody
    }

type NamespaceItem =
    | CallableDeclaration of CallableDeclaration
    | Unknown of Terminal

module NamespaceItem =
    let mapPrefix mapper =
        function
        | CallableDeclaration callable ->
            { callable with CallableKeyword = Terminal.mapPrefix mapper callable.CallableKeyword }
            |> CallableDeclaration
        | Unknown terminal -> Terminal.mapPrefix mapper terminal |> Unknown

type Namespace =
    {
        NamespaceKeyword: Terminal
        Name: Terminal
        Block: NamespaceItem Block
    }

type Document = { Namespaces: Namespace list; Eof: Terminal }
