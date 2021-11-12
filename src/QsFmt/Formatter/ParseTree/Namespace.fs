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
        let tospecializationParameters tokens (context: QSharpParser.SpecializationParameterTupleContext) =
            let parameters = context._parameters |> Seq.map (Node.toUnknown tokens)
            let commas = context._commas |> Seq.map (Node.toTerminal tokens)

            {
                OpenParen = context.openParen |> Node.toTerminal tokens
                Items = Node.tupleItems parameters commas
                CloseParen = context.closeParen |> Node.toTerminal tokens
            }

        Provided(
            parameters = (Option.ofObj context.provided.parameters |> Option.map (tospecializationParameters tokens)),
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
    inherit QSharpParserBaseVisitor<ParameterBinding>()

    let typeVisitor = TypeVisitor tokens

    override _.DefaultResult = failwith "Unknown parameter symbol binding."

    override visitor.VisitNamedParameter context = context.namedItem () |> visitor.Visit

    override visitor.VisitTupledParameter context =
        context.parameterTuple () |> visitor.Visit

    override _.VisitNamedItem context =
        {
            Name = context.name |> Node.toTerminal tokens
            Type = { Colon = context.colon |> Node.toTerminal tokens; Type = context.itemType |> typeVisitor.Visit }
        }
        |> ParameterDeclaration

    override visitor.VisitParameterTuple context =
        let parameters = context._parameters |> Seq.map visitor.Visit
        let commas = context._commas |> Seq.map (Node.toTerminal tokens)

        {
            OpenParen = context.openParen |> Node.toTerminal tokens
            Items = Node.tupleItems parameters commas
            CloseParen = context.closeParen |> Node.toTerminal tokens
        }
        |> ParameterTuple

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

type UnderlyingTypeVistor(tokens) =
    inherit QSharpParserBaseVisitor<UnderlyingType>()

    let typeVisitor = TypeVisitor tokens

    override _.DefaultResult = failwith "Unknown underlying type."

    override visitor.VisitTupleUnderlyingType context =
        context.typeDeclarationTuple () |> visitor.Visit

    override _.VisitUnnamedTypeItem context =
        context.``type`` () |> typeVisitor.Visit |> Type

    override _.VisitTypeDeclarationTuple context =
        let parameters = context._items |> Seq.map (TypeTupleItemVistor tokens).Visit
        let commas = context._commas |> Seq.map (Node.toTerminal tokens)

        {
            OpenParen = context.openParen |> Node.toTerminal tokens
            Items = Node.tupleItems parameters commas
            CloseParen = context.closeParen |> Node.toTerminal tokens
        }
        |> TypeDeclarationTuple

and TypeTupleItemVistor(tokens) =
    inherit QSharpParserBaseVisitor<TypeTupleItem>()

    let typeVisitor = TypeVisitor tokens
    let underlyingTypeVistor = UnderlyingTypeVistor tokens

    override _.DefaultResult = failwith "Unknown type tuple item."

    override visitor.VisitNamedTypeItem context = context.namedItem () |> visitor.Visit

    override _.VisitUnderlyingTypeItem context =
        context.underlyingType () |> underlyingTypeVistor.Visit |> UnderlyingType

    override _.VisitNamedItem context =
        {
            Name = context.name |> Node.toTerminal tokens
            Type = { Colon = context.colon |> Node.toTerminal tokens; Type = context.itemType |> typeVisitor.Visit }
        }
        |> TypeBinding

/// <summary>
/// Creates syntax tree <see cref="NamespaceItem"/> nodes from a parse tree and the list of tokens.
/// </summary>
type NamespaceItemVisitor(tokens) =
    inherit QSharpParserBaseVisitor<NamespaceItem>()

    let parameterVisitor = ParameterVisitor tokens
    let typeVisitor = TypeVisitor tokens
    let callableBodyVisitor = CallableBodyVisitor tokens
    let underlyingTypeVistor = UnderlyingTypeVistor tokens

    override _.DefaultResult = failwith "Unknown namespace element."

    override _.VisitChildren node = Node.toUnknown tokens node |> Unknown

    override visitor.VisitOpenElement context =
        context.openDirective () |> visitor.Visit

    override visitor.VisitTypeElement context =
        context.typeDeclaration () |> visitor.Visit

    override visitor.VisitCallableElement context =
        context.callableDeclaration () |> visitor.Visit

    override _.VisitOpenDirective context =
        {
            OpenKeyword = context.``open`` |> Node.toTerminal tokens
            OpenName = context.openName |> Node.toUnknown tokens
            AsKeyword = context.``as`` |> Option.ofObj |> Option.map (Node.toTerminal tokens)
            AsName = context.asName |> Option.ofObj |> Option.map (Node.toUnknown tokens)
            Semicolon = context.semicolon |> Node.toTerminal tokens
        }
        |> OpenDirective

    override _.VisitTypeDeclaration context =
        {
            Attributes = context.prefix._attributes |> Seq.map (NamespaceContext.toAttribute tokens) |> Seq.toList
            Access = context.prefix.access () |> Option.ofObj |> Option.map (Node.toUnknown tokens)
            NewtypeKeyword = context.keyword |> Node.toTerminal tokens
            DeclaredType = context.declared |> Node.toTerminal tokens
            Equals = context.equals |> Node.toTerminal tokens
            UnderlyingType = context.underlying |> underlyingTypeVistor.Visit
            Semicolon = context.semicolon |> Node.toTerminal tokens
        }
        |> TypeDeclaration

    override _.VisitCallableDeclaration context =
        {
            Attributes = context.prefix._attributes |> Seq.map (NamespaceContext.toAttribute tokens) |> Seq.toList
            Access = context.prefix.access () |> Option.ofObj |> Option.map (Node.toUnknown tokens)
            CallableKeyword = context.keyword |> Node.toTerminal tokens
            Name = context.name |> Node.toTerminal tokens
            TypeParameters =
                Option.ofObj context.typeParameters |> Option.map (NamespaceContext.toTypeParameterBinding tokens)
            Parameters = parameterVisitor.Visit context.tuple
            ReturnType =
                { Colon = context.colon |> Node.toTerminal tokens; Type = typeVisitor.Visit context.returnType }
            CharacteristicSection = Option.ofObj context.returnChar |> Option.map (toCharacteristicSection tokens)
            Body = callableBodyVisitor.Visit context.body
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
