// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

open Microsoft.Quantum.QsFmt.Formatter.Utils

type 'context Rewriter() =
    abstract Document : context: 'context * document: Document -> Document

    default rewriter.Document(context, document) =
        {
            Namespaces = document.Namespaces |> List.map (curry rewriter.Namespace context)
            Eof = rewriter.Terminal(context, document.Eof)
        }

    abstract Namespace : context: 'context * ns: Namespace -> Namespace

    default rewriter.Namespace(context, ns) =
        {
            NamespaceKeyword = rewriter.Terminal(context, ns.NamespaceKeyword)
            Name = rewriter.Terminal(context, ns.Name)
            Block = rewriter.Block(context, rewriter.NamespaceItem, ns.Block)
        }

    abstract NamespaceItem : context: 'context * item: NamespaceItem -> NamespaceItem

    default rewriter.NamespaceItem(context, item) =
        match item with
        | CallableDeclaration callable -> rewriter.CallableDeclaration(context, callable) |> CallableDeclaration
        | Unknown terminal -> rewriter.Terminal(context, terminal) |> Unknown

    abstract Attribute : context: 'context * attribute: Attribute -> Attribute

    default rewriter.Attribute(context, attribute) =
        {
            At = rewriter.Terminal(context, attribute.At)
            Expression = rewriter.Expression(context, attribute.Expression)
        }

    abstract CallableDeclaration : context: 'context * callable: CallableDeclaration -> CallableDeclaration

    default rewriter.CallableDeclaration(context, callable) =
        {
            Attributes = callable.Attributes |> List.map (curry rewriter.Attribute context)
            Access = callable.Access |> Option.map (curry rewriter.Terminal context)
            CallableKeyword = rewriter.Terminal(context, callable.CallableKeyword)
            Name = rewriter.Terminal(context, callable.Name)
            TypeParameters = callable.TypeParameters |> Option.map (curry rewriter.TypeParameterBinding context)
            Parameters = rewriter.SymbolBinding(context, callable.Parameters)
            ReturnType = rewriter.TypeAnnotation(context, callable.ReturnType)
            CharacteristicSection =
                callable.CharacteristicSection |> Option.map (curry rewriter.CharacteristicSection context)
            Body = rewriter.CallableBody(context, callable.Body)
        }

    abstract TypeParameterBinding : context: 'context * binding: TypeParameterBinding -> TypeParameterBinding

    default rewriter.TypeParameterBinding(context, binding) =
        {
            OpenBracket = rewriter.Terminal(context, binding.OpenBracket)
            Parameters = binding.Parameters |> List.map (curry3 rewriter.SequenceItem context rewriter.Terminal)
            CloseBracket = rewriter.Terminal(context, binding.CloseBracket)
        }

    abstract Type : context: 'context * typ: Type -> Type

    default rewriter.Type(context, typ) =
        match typ with
        | Type.Missing missing -> rewriter.Terminal(context, missing) |> Type.Missing
        | Parameter name -> rewriter.Terminal(context, name) |> Parameter
        | Type.BuiltIn name -> rewriter.Terminal(context, name) |> Type.BuiltIn
        | UserDefined name -> rewriter.Terminal(context, name) |> UserDefined
        | Type.Tuple tuple -> rewriter.Tuple(context, rewriter.Type, tuple) |> Type.Tuple
        | Array array -> rewriter.ArrayType(context, array) |> Array
        | Type.Callable callable -> rewriter.CallableType(context, callable) |> Type.Callable
        | Type.Unknown terminal -> rewriter.Terminal(context, terminal) |> Type.Unknown

    abstract TypeAnnotation : context: 'context * annotation: TypeAnnotation -> TypeAnnotation

    default rewriter.TypeAnnotation(context, annotation) =
        { Colon = rewriter.Terminal(context, annotation.Colon); Type = rewriter.Type(context, annotation.Type) }

    abstract ArrayType : context: 'context * array: ArrayType -> ArrayType

    default rewriter.ArrayType(context, array) =
        {
            ItemType = rewriter.Type(context, array.ItemType)
            OpenBracket = rewriter.Terminal(context, array.OpenBracket)
            CloseBracket = rewriter.Terminal(context, array.CloseBracket)
        }

    abstract CallableType : context: 'context * callable: CallableType -> CallableType

    default rewriter.CallableType(context, callable) =
        {
            FromType = rewriter.Type(context, callable.FromType)
            Arrow = rewriter.Terminal(context, callable.Arrow)
            ToType = rewriter.Type(context, callable.ToType)
            Characteristics = callable.Characteristics |> Option.map (curry rewriter.CharacteristicSection context)
        }

    abstract CharacteristicSection : context: 'context * section: CharacteristicSection -> CharacteristicSection

    default rewriter.CharacteristicSection(context, section) =
        {
            IsKeyword = rewriter.Terminal(context, section.IsKeyword)
            Characteristic = rewriter.Characteristic(context, section.Characteristic)
        }

    abstract CharacteristicGroup : context: 'context * group: CharacteristicGroup -> CharacteristicGroup

    default rewriter.CharacteristicGroup(context, group) =
        {
            OpenParen = rewriter.Terminal(context, group.OpenParen)
            Characteristic = rewriter.Characteristic(context, group.Characteristic)
            CloseParen = rewriter.Terminal(context, group.CloseParen)
        }

    abstract Characteristic : context: 'context * characteristic: Characteristic -> Characteristic

    default rewriter.Characteristic(context, characteristic) =
        match characteristic with
        | Adjoint adjoint -> rewriter.Terminal(context, adjoint) |> Adjoint
        | Controlled controlled -> rewriter.Terminal(context, controlled) |> Controlled
        | Group group -> rewriter.CharacteristicGroup(context, group) |> Group
        | Characteristic.BinaryOperator operator ->
            rewriter.BinaryOperator(context, rewriter.Characteristic, operator) |> Characteristic.BinaryOperator

    abstract CallableBody : context: 'context * body: CallableBody -> CallableBody

    default rewriter.CallableBody(context, body) =
        match body with
        | Statements statements -> rewriter.Block(context, rewriter.Statement, statements) |> Statements
        | Specializations specializations ->
            rewriter.Block(context, rewriter.Specialization, specializations) |> Specializations

    abstract Specialization : context: 'context * specialization: Specialization -> Specialization

    default rewriter.Specialization(context, specialization) =
        {
            Names = specialization.Names |> List.map (curry rewriter.Terminal context)
            Generator = rewriter.SpecializationGenerator(context, specialization.Generator)
        }

    abstract SpecializationGenerator : context: 'context * generator: SpecializationGenerator -> SpecializationGenerator

    default rewriter.SpecializationGenerator(context, generator) =
        match generator with
        | BuiltIn (name, semicolon) ->
            BuiltIn(name = rewriter.Terminal(context, name), semicolon = rewriter.Terminal(context, semicolon))
        | Provided (parameters, statements) ->
            Provided(
                parameters = (parameters |> Option.map (curry rewriter.Terminal context)),
                statements = rewriter.Block(context, rewriter.Statement, statements)
            )

    abstract Statement : context: 'context * statement: Statement -> Statement

    default rewriter.Statement(context, statement) =
        match statement with
        | Let lets -> rewriter.Let(context, lets) |> Let
        | Return returns -> rewriter.Return(context, returns) |> Return
        | If ifs -> rewriter.If(context, ifs) |> If
        | Else elses -> rewriter.Else(context, elses) |> Else
        | Statement.Unknown terminal -> rewriter.Terminal(context, terminal) |> Statement.Unknown

    abstract Let : context: 'context * lets: Let -> Let

    default rewriter.Let(context, lets) =
        {
            LetKeyword = rewriter.Terminal(context, lets.LetKeyword)
            Binding = rewriter.SymbolBinding(context, lets.Binding)
            Equals = rewriter.Terminal(context, lets.Equals)
            Value = rewriter.Expression(context, lets.Value)
            Semicolon = rewriter.Terminal(context, lets.Semicolon)
        }

    abstract Return : context: 'context * returns: Return -> Return

    default rewriter.Return(context, returns) =
        {
            ReturnKeyword = rewriter.Terminal(context, returns.ReturnKeyword)
            Expression = rewriter.Expression(context, returns.Expression)
            Semicolon = rewriter.Terminal(context, returns.Semicolon)
        }

    abstract If : context: 'context * ifs: If -> If

    default rewriter.If(context, ifs) =
        {
            IfKeyword = rewriter.Terminal(context, ifs.IfKeyword)
            Condition = rewriter.Expression(context, ifs.Condition)
            Block = rewriter.Block(context, rewriter.Statement, ifs.Block)
        }

    abstract Else : context: 'context * elses: Else -> Else

    default rewriter.Else(context, elses) =
        {
            ElseKeyword = rewriter.Terminal(context, elses.ElseKeyword)
            Block = rewriter.Block(context, rewriter.Statement, elses.Block)
        }

    abstract SymbolBinding : context: 'context * binding: SymbolBinding -> SymbolBinding

    default rewriter.SymbolBinding(context, binding) =
        match binding with
        | SymbolDeclaration declaration -> rewriter.SymbolDeclaration(context, declaration) |> SymbolDeclaration
        | SymbolTuple tuple -> rewriter.Tuple(context, rewriter.SymbolBinding, tuple) |> SymbolTuple

    abstract SymbolDeclaration : context: 'context * declaration: SymbolDeclaration -> SymbolDeclaration

    default rewriter.SymbolDeclaration(context, declaration) =
        {
            Name = rewriter.Terminal(context, declaration.Name)
            Type = declaration.Type |> Option.map (curry rewriter.TypeAnnotation context)
        }

    abstract Expression : context: 'context * expression: Expression -> Expression

    default rewriter.Expression(context, expression) =
        match expression with
        | Missing terminal -> rewriter.Terminal(context, terminal) |> Missing
        | Literal literal -> rewriter.Terminal(context, literal) |> Literal
        | Tuple tuple -> rewriter.Tuple(context, rewriter.Expression, tuple) |> Tuple
        | BinaryOperator operator -> rewriter.BinaryOperator(context, rewriter.Expression, operator) |> BinaryOperator
        | Update update -> rewriter.Update(context, update) |> Update
        | Expression.Unknown terminal -> rewriter.Terminal(context, terminal) |> Expression.Unknown

    abstract Update : context: 'context * update: Update -> Update

    default rewriter.Update(context, update) =
        {
            Record = rewriter.Expression(context, update.Record)
            With = rewriter.Terminal(context, update.With)
            Item = rewriter.Expression(context, update.Item)
            Arrow = rewriter.Terminal(context, update.Arrow)
            Value = rewriter.Expression(context, update.Value)
        }

    abstract Block : context: 'context * mapper: ('context * 'a -> 'a) * block: 'a Block -> 'a Block

    default rewriter.Block(context, mapper, block) =
        {
            OpenBrace = rewriter.Terminal(context, block.OpenBrace)
            Items = block.Items |> List.map (curry mapper context)
            CloseBrace = rewriter.Terminal(context, block.CloseBrace)
        }

    abstract Tuple : context: 'context * mapper: ('context * 'a -> 'a) * tuple: 'a Tuple -> 'a Tuple

    default rewriter.Tuple(context, mapper, tuple) =
        {
            OpenParen = rewriter.Terminal(context, tuple.OpenParen)
            Items = tuple.Items |> List.map (curry3 rewriter.SequenceItem context mapper)
            CloseParen = rewriter.Terminal(context, tuple.CloseParen)
        }

    abstract SequenceItem : context: 'context * mapper: ('context * 'a -> 'a) * item: 'a SequenceItem -> 'a SequenceItem

    default rewriter.SequenceItem(context, mapper, item) =
        {
            Item = item.Item |> Option.map (curry mapper context)
            Comma = item.Comma |> Option.map (curry rewriter.Terminal context)
        }

    abstract BinaryOperator :
        context: 'context * mapper: ('context * 'a -> 'a) * operator: 'a BinaryOperator -> 'a BinaryOperator

    default rewriter.BinaryOperator(context, mapper, operator) =
        {
            Left = mapper (context, operator.Left)
            Operator = rewriter.Terminal(context, operator.Operator)
            Right = mapper (context, operator.Right)
        }

    abstract Terminal : context: 'context * terminal: Terminal -> Terminal
    default _.Terminal(_, terminal) = terminal
