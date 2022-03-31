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
        toBuiltIn (context.Auto().Symbol) (context.Semicolon().Symbol)

    override _.VisitSelfGenerator context =
        toBuiltIn (context.Self().Symbol) (context.Semicolon().Symbol)

    override _.VisitInvertGenerator context =
        toBuiltIn (context.Invert().Symbol) (context.Semicolon().Symbol)

    override _.VisitDistributeGenerator context =
        toBuiltIn (context.Distribute().Symbol) (context.Semicolon().Symbol)

    override _.VisitIntrinsicGenerator context =
        toBuiltIn (context.Intrinsic().Symbol) (context.Semicolon().Symbol)

    override _.VisitProvidedGenerator context =
        let toSpecializationParameters tokens (context: QSharpParser.SpecializationParameterTupleContext) =
            let parameters = context.specializationParameter () |> Seq.map (Node.toUnknown tokens)
            let commas = context.Comma() |> Seq.map (fun node -> Node.toTerminal tokens node.Symbol)

            {
                OpenParen = context.ParenLeft().Symbol |> Node.toTerminal tokens
                Items = Node.tupleItems parameters commas
                CloseParen = context.ParenRight().Symbol |> Node.toTerminal tokens
            }

        let provided = context.providedSpecialization ()
        let parameters = provided.specializationParameterTuple () |> Option.ofObj

        Provided(
            parameters = Option.map (toSpecializationParameters tokens) parameters,
            statements = statementVisitor.CreateBlock(provided.scope ())
        )

module NamespaceContext =
    let toAttribute tokens (context: QSharpParser.AttributeContext) =
        {
            At = context.At().Symbol |> Node.toTerminal tokens
            Expression = context.expression () |> (ExpressionVisitor tokens).Visit
        }

    let toTypeParameterBinding tokens (context: QSharpParser.TypeParameterBindingContext) =
        let parameters =
            Node.tupleItems
                (context.TypeParameter() |> Seq.map (fun node -> Node.toTerminal tokens node.Symbol))
                (context.Comma() |> Seq.map (fun node -> Node.toTerminal tokens node.Symbol))

        {
            OpenBracket = context.Less().Symbol |> Node.toTerminal tokens
            Parameters = parameters
            CloseBracket = context.Greater().Symbol |> Node.toTerminal tokens
        }

    let toSpecialization tokens (context: QSharpParser.SpecializationContext) =
        {
            Names = context.specializationName () |> Seq.map (Node.toUnknown tokens) |> Seq.toList
            Generator = context.specializationGenerator () |> (SpecializationGeneratorVisitor tokens).Visit
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
            Name = context.Identifier().Symbol |> Node.toTerminal tokens
            Type =
                {
                    Colon = context.Colon().Symbol |> Node.toTerminal tokens
                    Type = context.``type`` () |> typeVisitor.Visit
                }
        }
        |> ParameterDeclaration

    override visitor.VisitParameterTuple context =
        let parameters = context.parameter () |> Seq.map visitor.Visit
        let commas = context.Comma() |> Seq.map (fun node -> Node.toTerminal tokens node.Symbol)

        {
            OpenParen = context.ParenLeft().Symbol |> Node.toTerminal tokens
            Items = Node.tupleItems parameters commas
            CloseParen = context.ParenRight().Symbol |> Node.toTerminal tokens
        }
        |> ParameterTuple

type CallableBodyVisitor(tokens) =
    inherit QSharpParserBaseVisitor<CallableBody>()

    let statementVisitor = StatementVisitor tokens

    override _.DefaultResult = failwith "Unknown callable body."

    override _.VisitCallableStatements context =
        context.scope () |> statementVisitor.CreateBlock |> Statements

    override _.VisitCallableSpecializations context =
        {
            OpenBrace = context.BraceLeft().Symbol |> Node.toTerminal tokens
            Items = context.specialization () |> Seq.map (NamespaceContext.toSpecialization tokens) |> Seq.toList
            CloseBrace = context.BraceRight().Symbol |> Node.toTerminal tokens
        }
        |> Specializations

type UnderlyingTypeVisitor(tokens) =
    inherit QSharpParserBaseVisitor<UnderlyingType>()

    let typeVisitor = TypeVisitor tokens

    override _.DefaultResult = failwith "Unknown underlying type."

    override visitor.VisitTupleUnderlyingType context =
        context.typeDeclarationTuple () |> visitor.Visit

    override _.VisitUnnamedTypeItem context =
        context.``type`` () |> typeVisitor.Visit |> Type

    override _.VisitTypeDeclarationTuple context =
        let parameters = context.typeTupleItem () |> Seq.map (TypeTupleItemVisitor tokens).Visit
        let commas = context.Comma() |> Seq.map (fun node -> Node.toTerminal tokens node.Symbol)

        {
            OpenParen = context.ParenLeft().Symbol |> Node.toTerminal tokens
            Items = Node.tupleItems parameters commas
            CloseParen = context.ParenRight().Symbol |> Node.toTerminal tokens
        }
        |> TypeDeclarationTuple

and TypeTupleItemVisitor(tokens) =
    inherit QSharpParserBaseVisitor<TypeTupleItem>()

    let typeVisitor = TypeVisitor tokens
    let underlyingTypeVisitor = UnderlyingTypeVisitor tokens

    override _.DefaultResult = failwith "Unknown type tuple item."

    override visitor.VisitNamedTypeItem context = context.namedItem () |> visitor.Visit

    override _.VisitUnderlyingTypeItem context =
        context.underlyingType () |> underlyingTypeVisitor.Visit |> UnderlyingType

    override _.VisitNamedItem context =
        {
            Name = context.Identifier().Symbol |> Node.toTerminal tokens
            Type =
                {
                    Colon = context.Colon().Symbol |> Node.toTerminal tokens
                    Type = context.``type`` () |> typeVisitor.Visit
                }
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
    let underlyingTypeVisitor = UnderlyingTypeVisitor tokens

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
            OpenKeyword = context.Open().Symbol |> Node.toTerminal tokens
            OpenName = context.name |> Node.toUnknown tokens
            AsKeyword = context.As() |> Option.ofObj |> Option.map (fun node -> Node.toTerminal tokens node.Symbol)
            AsName = context.alias |> Option.ofObj |> Option.map (Node.toUnknown tokens)
            Semicolon = context.Semicolon().Symbol |> Node.toTerminal tokens
        }
        |> OpenDirective

    override _.VisitTypeDeclaration context =
        let prefix = context.declarationPrefix ()

        {
            Attributes = prefix.attribute () |> Seq.map (NamespaceContext.toAttribute tokens) |> Seq.toList
            Access = prefix.access () |> Option.ofObj |> Option.map (Node.toUnknown tokens)
            NewtypeKeyword = context.Newtype().Symbol |> Node.toTerminal tokens
            DeclaredType = context.Identifier().Symbol |> Node.toTerminal tokens
            Equals = context.Equal().Symbol |> Node.toTerminal tokens
            UnderlyingType = context.underlyingType () |> underlyingTypeVisitor.Visit
            Semicolon = context.Semicolon().Symbol |> Node.toTerminal tokens
        }
        |> TypeDeclaration

    override _.VisitCallableDeclaration context =
        let prefix = context.declarationPrefix ()

        {
            Attributes = prefix.attribute () |> Seq.map (NamespaceContext.toAttribute tokens) |> Seq.toList
            Access = prefix.access () |> Option.ofObj |> Option.map (Node.toUnknown tokens)
            CallableKeyword = context.keyword |> Node.toTerminal tokens
            Name = context.Identifier().Symbol |> Node.toTerminal tokens
            TypeParameters =
                context.typeParameterBinding ()
                |> Option.ofObj
                |> Option.map (NamespaceContext.toTypeParameterBinding tokens)
            Parameters = context.parameterTuple () |> parameterVisitor.Visit
            ReturnType =
                {
                    Colon = context.Colon().Symbol |> Node.toTerminal tokens
                    Type = typeVisitor.Visit context.returnType
                }
            CharacteristicSection =
                context.characteristics () |> Option.ofObj |> Option.map (toCharacteristicSection tokens)
            Body = context.callableBody () |> callableBodyVisitor.Visit
        }
        |> CallableDeclaration

module Namespace =
    /// <summary>
    /// Creates a syntax tree <see cref="Namespace"/> node from the parse tree
    /// <see cref="QSharpParser.NamespaceContext"/> node and the list of tokens.
    /// </summary>
    let toNamespace tokens (context: QSharpParser.NamespaceContext) =
        let visitor = NamespaceItemVisitor tokens
        let name = context.qualifiedName ()

        {
            NamespaceKeyword = context.Namespace().Symbol |> Node.toTerminal tokens
            Name = { Prefix = Node.prefix tokens name.Start.TokenIndex; Text = name.GetText() }
            Block =
                {
                    OpenBrace = context.BraceLeft().Symbol |> Node.toTerminal tokens
                    Items = context.namespaceElement () |> Seq.map visitor.Visit |> Seq.toList
                    CloseBrace = context.BraceRight().Symbol |> Node.toTerminal tokens
                }
        }

    let toDocument tokens (context: QSharpParser.DocumentContext) =
        let namespaces = context.``namespace`` () |> Array.toList |> List.map (toNamespace tokens)
        let eof = { (context.Eof().Symbol |> Node.toTerminal tokens) with Text = "" }
        { Namespaces = namespaces; Eof = eof }
