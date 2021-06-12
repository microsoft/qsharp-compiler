﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.ParseTree

open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree
open Microsoft.Quantum.QsFmt.Parser

type CharacteristicVisitor(tokens) =
    inherit QSharpParserBaseVisitor<Characteristic>()

    override _.DefaultResult = failwith "Unknown characteristic."

    override _.VisitAdjointCharacteristics context =
        context.Adj().Symbol |> Node.toTerminal tokens |> Adjoint

    override _.VisitControlledCharacteristics context =
        context.Ctl().Symbol |> Node.toTerminal tokens |> Controlled

    override visitor.VisitCharacteristicGroup context =
        {
            OpenParen = context.openParen |> Node.toTerminal tokens
            Characteristic = visitor.Visit context.charExp
            CloseParen = context.closeParen |> Node.toTerminal tokens
        }
        |> Group

    override visitor.VisitIntersectCharacteristics context =
        {
            Left = visitor.Visit context.left
            Operator = context.Asterisk().Symbol |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> Characteristic.BinaryOperator

    override visitor.VisitUnionCharacteristics context =
        {
            Left = visitor.Visit context.left
            Operator = context.Plus().Symbol |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> Characteristic.BinaryOperator

module Type =
    let toCharacteristicSection tokens (context: QSharpParser.CharacteristicsContext) =
        {
            IsKeyword = context.is |> Node.toTerminal tokens
            Characteristic = (CharacteristicVisitor tokens).Visit context.charExp
        }

type TypeVisitor(tokens) =
    inherit QSharpParserBaseVisitor<Type>()

    override _.DefaultResult = failwith "Unknown type."

    override _.VisitChildren node =
        Node.toUnknown tokens node |> Type.Unknown

    override _.VisitIntType context =
        context.Int().Symbol |> Node.toTerminal tokens |> BuiltIn

    override _.VisitUserDefinedType context =
        { Prefix = Node.prefix tokens context.name.Start.TokenIndex; Text = context.name.GetText() }
        |> UserDefined

    override visitor.VisitTupleType context =
        let items = context._items |> Seq.map visitor.Visit
        let commas = context._commas |> Seq.map (Node.toTerminal tokens)

        {
            OpenParen = context.openParen |> Node.toTerminal tokens
            Items = Node.tupleItems items commas
            CloseParen = context.closeParen |> Node.toTerminal tokens
        }
        |> Type.Tuple

    override visitor.VisitArrayType context =
        {
            ItemType = visitor.Visit context.item
            OpenBracket = context.openBracket |> Node.toTerminal tokens
            CloseBracket = context.closeBracket |> Node.toTerminal tokens
        }
        |> Array

    override visitor.VisitCallableType context =
        {
            FromType = visitor.Visit context.fromType
            Arrow = context.arrow |> Node.toTerminal tokens
            ToType = visitor.Visit context.toType
            Characteristics = Option.ofObj context.character |> Option.map (Type.toCharacteristicSection tokens)
        }
        |> Type.Callable
