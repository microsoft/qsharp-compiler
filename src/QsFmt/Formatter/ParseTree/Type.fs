// Copyright (c) Microsoft Corporation.
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
            InfixOperator = context.Asterisk().Symbol |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> Characteristic.InfixOperator

    override visitor.VisitUnionCharacteristics context =
        {
            Left = visitor.Visit context.left
            InfixOperator = context.Plus().Symbol |> Node.toTerminal tokens
            Right = visitor.Visit context.right
        }
        |> Characteristic.InfixOperator

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

    override _.VisitMissingType context =
        context.Underscore().Symbol |> Node.toTerminal tokens |> Type.Missing

    override _.VisitBigIntType context =
        context.BigInt().Symbol |> Node.toTerminal tokens |> Type.BuiltIn

    override _.VisitBoolType context =
        context.Bool().Symbol |> Node.toTerminal tokens |> Type.BuiltIn

    override _.VisitDoubleType context =
        context.Double().Symbol |> Node.toTerminal tokens |> Type.BuiltIn

    override _.VisitIntType context =
        context.Int().Symbol |> Node.toTerminal tokens |> Type.BuiltIn

    override _.VisitPauliType context =
        context.Pauli().Symbol |> Node.toTerminal tokens |> Type.BuiltIn

    override _.VisitQubitType context =
        context.Qubit().Symbol |> Node.toTerminal tokens |> Type.BuiltIn

    override _.VisitRangeType context =
        context.Range().Symbol |> Node.toTerminal tokens |> Type.BuiltIn

    override _.VisitResultType context =
        context.Result().Symbol |> Node.toTerminal tokens |> Type.BuiltIn

    override _.VisitStringType context =
        context.String().Symbol |> Node.toTerminal tokens |> Type.BuiltIn

    override _.VisitUnitType context =
        context.Unit().Symbol |> Node.toTerminal tokens |> Type.BuiltIn

    override _.VisitTypeParameter context =
        context.typeParameter |> Node.toTerminal tokens |> Parameter

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
            ItemType = visitor.Visit context.itemType
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
