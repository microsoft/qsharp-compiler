// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

type CallableDeclaration =
    {
        CallableKeyword: Terminal
        Name: Terminal
        Parameters: SymbolBinding
        ReturnType: TypeAnnotation
        Block: Statement Block
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
