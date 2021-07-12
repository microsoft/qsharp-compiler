// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.ParseTree

open Microsoft.Quantum.QsFmt.Formatter.ParseTree.Type
open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree
open Microsoft.Quantum.QsFmt.Parser

type SpecializationGeneratorVisitor(tokens) =
    inherit QSharpParserBaseVisitor<SpecializationGenerator>()

    let statementVisitor = StatementVisitor tokens

    let toBuiltIn name semicolon =
        BuiltIn(name = Node.toTerminal tokens name, semicolon = Node.toTerminal tokens semicolon)

    override _.DefaultResult = failwith "Unknown specialization generator."

    override _.VisitAutoGenerator context =
        toBuiltIn context.auto context.semicolon

    override _.VisitSelfGenerator context =
        toBuiltIn context.self context.semicolon

    override _.VisitInvertGenerator context =
        toBuiltIn context.invert context.semicolon

    override _.VisitDistributeGenerator context =
        toBuiltIn context.distribute context.semicolon

    override _.VisitIntrinsicGenerator context =
        toBuiltIn context.intrinsic context.semicolon

    override _.VisitProvidedGenerator context =
        Provided(
            parameters = (Option.ofObj context.provided.parameters |> Option.map (Node.toUnknown tokens)),
            statements =
                {
                    OpenBrace = context.provided.block.openBrace |> Node.toTerminal tokens
                    Items = context.provided.block._statements |> Seq.map statementVisitor.Visit |> Seq.toList
                    CloseBrace = context.provided.block.closeBrace |> Node.toTerminal tokens
                }
        )

module NamespaceContext =
    let toAttribute tokens (context: QSharpParser.AttributeContext) =
        { At = Node.toTerminal tokens context.at; Expression = (ExpressionVisitor tokens).Visit context.expr }

    let toTypeParameterBinding tokens (context: QSharpParser.TypeParameterBindingContext) =
        let parameters =
            Node.tupleItems
                (context._parameters |> Seq.map (Node.toTerminal tokens))
                (context._commas |> Seq.map (Node.toTerminal tokens))

        {
            OpenBracket = Node.toTerminal tokens context.openBracket
            Parameters = parameters
            CloseBracket = Node.toTerminal tokens context.closeBracket
        }

    let toSpecialization tokens (context: QSharpParser.SpecializationContext) =
        {
            Names = context._names |> Seq.map (Node.toUnknown tokens) |> Seq.toList
            Generator = (SpecializationGeneratorVisitor tokens).Visit context.generator
        }

/// <summary>
/// Creates syntax tree <see cref="SymbolBinding"/> nodes for callable parameters from a parse tree and the list of
/// tokens.
/// </summary>
type ParameterVisitor(tokens) =
    inherit QSharpParserBaseVisitor<SymbolBinding>()

    let typeVisitor = TypeVisitor tokens

    override _.DefaultResult = failwith "Unknown parameter symbol binding."

    override visitor.VisitNamedParameter context = context.namedItem () |> visitor.Visit

    override visitor.VisitTupledParameter context =
        context.parameterTuple () |> visitor.Visit

    override _.VisitNamedItem context =
        {
            Name = context.name |> Node.toTerminal tokens
            Type =
                { Colon = context.colon |> Node.toTerminal tokens; Type = context.itemType |> typeVisitor.Visit }
                |> Some
        }
        |> SymbolDeclaration

    override visitor.VisitParameterTuple context =
        let parameters = context._parameters |> Seq.map visitor.Visit
        let commas = context._commas |> Seq.map (Node.toTerminal tokens)

        {
            OpenParen = context.openParen |> Node.toTerminal tokens
            Items = Node.tupleItems parameters commas
            CloseParen = context.closeParen |> Node.toTerminal tokens
        }
        |> SymbolTuple

type CallableBodyVisitor(tokens) =
    inherit QSharpParserBaseVisitor<CallableBody>()

    let statementVisitor = StatementVisitor tokens

    override _.DefaultResult = failwith "Unknown callable body."

    override _.VisitCallableStatements context =
        {
            OpenBrace = context.block.openBrace |> Node.toTerminal tokens
            Items = context.block._statements |> Seq.map statementVisitor.Visit |> Seq.toList
            CloseBrace = context.block.closeBrace |> Node.toTerminal tokens
        }
        |> Statements

    override _.VisitCallableSpecializations context =
        {
            OpenBrace = context.openBrace |> Node.toTerminal tokens
            Items = context._specializations |> Seq.map (NamespaceContext.toSpecialization tokens) |> Seq.toList
            CloseBrace = context.closeBrace |> Node.toTerminal tokens
        }
        |> Specializations

/// <summary>
/// Creates syntax tree <see cref="NamespaceItem"/> nodes from a parse tree and the list of tokens.
/// </summary>
type NamespaceItemVisitor(tokens) =
    inherit QSharpParserBaseVisitor<NamespaceItem>()

    let parameterVisitor = ParameterVisitor tokens
    let typeVisitor = TypeVisitor tokens
    let callableBodyVisitor = CallableBodyVisitor tokens

    override _.DefaultResult = failwith "Unknown namespace element."

    override _.VisitChildren node = Node.toUnknown tokens node |> Unknown

    override _.VisitCallableElement context =
        {
            Attributes =
                context.callable.prefix._attributes |> Seq.map (NamespaceContext.toAttribute tokens) |> Seq.toList
            Access = context.callable.prefix.access () |> Option.ofObj |> Option.map (Node.toUnknown tokens)
            CallableKeyword = context.callable.keyword |> Node.toTerminal tokens
            Name = context.callable.name |> Node.toTerminal tokens
            TypeParameters =
                Option.ofObj context.callable.typeParameters
                |> Option.map (NamespaceContext.toTypeParameterBinding tokens)
            Parameters = parameterVisitor.Visit context.callable.tuple
            ReturnType =
                {
                    Colon = context.callable.colon |> Node.toTerminal tokens
                    Type = typeVisitor.Visit context.callable.returnType
                }
            CharacteristicSection =
                Option.ofObj context.callable.returnChar |> Option.map (toCharacteristicSection tokens)
            Body = callableBodyVisitor.Visit context.callable.body
        }
        |> CallableDeclaration

module Namespace =
    /// <summary>
    /// Creates a syntax tree <see cref="Namespace"/> node from the parse tree
    /// <see cref="QSharpParser.NamespaceContext"/> node and the list of tokens.
    /// </summary>
    let toNamespace tokens (context: QSharpParser.NamespaceContext) =
        let visitor = NamespaceItemVisitor tokens

        {
            NamespaceKeyword = context.keyword |> Node.toTerminal tokens
            Name = { Prefix = Node.prefix tokens context.name.Start.TokenIndex; Text = context.name.GetText() }
            Block =
                {
                    OpenBrace = context.openBrace |> Node.toTerminal tokens
                    Items = context._elements |> Seq.map visitor.Visit |> Seq.toList
                    CloseBrace = context.closeBrace |> Node.toTerminal tokens
                }
        }

    let toDocument tokens (context: QSharpParser.DocumentContext) =
        let namespaces = context.``namespace`` () |> Array.toList |> List.map (toNamespace tokens)
        let eof = { (context.eof |> Node.toTerminal tokens) with Text = "" }
        { Namespaces = namespaces; Eof = eof }
