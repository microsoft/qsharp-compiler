// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

type Attribute = { At: Terminal; Expression: Expression }

module Attribute =
    let mapPrefix mapper attribute =
        { attribute with At = Terminal.mapPrefix mapper attribute.At }

type TypeParameterBinding =
    {
        OpenBracket: Terminal
        Parameters: Terminal SequenceItem list
        CloseBracket: Terminal
    }

type SpecializationGenerator =
    | BuiltIn of name: Terminal * semicolon: Terminal
    | Provided of parameters: Terminal Tuple option * statements: Statement Block

type Specialization = { Names: Terminal list; Generator: SpecializationGenerator }

type OpenDirective =
    {
        OpenKeyword: Terminal
        OpenName: Terminal
        AsKeyword: Terminal option
        AsName: Terminal option
        Semicolon: Terminal
    }

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
        Parameters: ParameterBinding
        ReturnType: TypeAnnotation
        CharacteristicSection: CharacteristicSection option
        Body: CallableBody
    }

type NamespaceItem =
    | OpenDirective of OpenDirective
    | CallableDeclaration of CallableDeclaration
    | Unknown of Terminal

module NamespaceItem =
    let mapPrefix mapper =
        function
        | OpenDirective openDirective ->
            { openDirective with OpenKeyword = Terminal.mapPrefix mapper openDirective.OpenKeyword }
            |> OpenDirective
        | CallableDeclaration callable ->
            { callable with
                CallableKeyword = Terminal.mapPrefix mapper callable.CallableKeyword
                Attributes = callable.Attributes |> List.map (Attribute.mapPrefix mapper)
            }
            |> CallableDeclaration
        | Unknown terminal -> Terminal.mapPrefix mapper terminal |> Unknown

type Namespace =
    {
        NamespaceKeyword: Terminal
        Name: Terminal
        Block: NamespaceItem Block
    }

type Document = { Namespaces: Namespace list; Eof: Terminal }
