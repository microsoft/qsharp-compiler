﻿// Copyright (c) Microsoft Corporation.
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
            Parameters = rewriter.ParameterBinding(context, callable.Parameters)
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
        | Characteristic.InfixOperator operator ->
            rewriter.InfixOperator(context, rewriter.Characteristic, operator) |> Characteristic.InfixOperator

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
        | ExpressionStatement expr -> rewriter.ExpressionStatement(context, expr) |> ExpressionStatement
        | Return returns -> rewriter.Return(context, returns) |> Return
        | Fail fails -> rewriter.Fail(context, fails) |> Fail
        | Let lets -> rewriter.Let(context, lets) |> Let
        | Mutable mutables -> rewriter.Mutable(context, mutables) |> Mutable
        | SetStatement sets -> rewriter.SetStatement(context, sets) |> SetStatement
        | UpdateStatement updates -> rewriter.UpdateStatement(context, updates) |> UpdateStatement
        | SetWith withs -> rewriter.SetWith(context, withs) |> SetWith
        | If ifs -> rewriter.If(context, ifs) |> If
        | Elif elifs -> rewriter.Elif(context, elifs) |> Elif
        | Else elses -> rewriter.Else(context, elses) |> Else
        | For loop -> rewriter.For(context, loop) |> For
        | While whiles -> rewriter.While(context, whiles) |> While
        | QubitDeclaration decl -> rewriter.QubitDeclaration(context, decl) |> QubitDeclaration
        | Statement.Unknown terminal -> rewriter.Terminal(context, terminal) |> Statement.Unknown

    abstract ExpressionStatement : context: 'context * expr: ExpressionStatement -> ExpressionStatement

    default rewriter.ExpressionStatement(context, expr) =
        { Expression = rewriter.Expression(context, expr.Expression); Semicolon = rewriter.Terminal(context, expr.Semicolon) }

    abstract Return : context: 'context * returns: Return -> Return

    default rewriter.Return(context, returns) =
        {
            ReturnKeyword = rewriter.Terminal(context, returns.ReturnKeyword)
            Expression = rewriter.Expression(context, returns.Expression)
            Semicolon = rewriter.Terminal(context, returns.Semicolon)
        }

    abstract Fail : context: 'context * fails: Fail -> Fail

    default rewriter.Fail(context, fails) =
        {
            FailKeyword = rewriter.Terminal(context, fails.FailKeyword)
            Expression = rewriter.Expression(context, fails.Expression)
            Semicolon = rewriter.Terminal(context, fails.Semicolon)
        }

    abstract Let : context: 'context * lets: Let -> Let

    default rewriter.Let(context, lets) =
        {
            LetKeyword = rewriter.Terminal(context, lets.LetKeyword)
            Binding = rewriter.SymbolBinding(context, lets.Binding)
            Equals = rewriter.Terminal(context, lets.Equals)
            Value = rewriter.Expression(context, lets.Value)
            Semicolon = rewriter.Terminal(context, lets.Semicolon)
        }

    abstract Mutable : context: 'context * mutables: Mutable -> Mutable

    default rewriter.Mutable(context, mutables) =
        {
            MutableKeyword = rewriter.Terminal(context, mutables.MutableKeyword)
            Binding = rewriter.SymbolBinding(context, mutables.Binding)
            Equals = rewriter.Terminal(context, mutables.Equals)
            Value = rewriter.Expression(context, mutables.Value)
            Semicolon = rewriter.Terminal(context, mutables.Semicolon)
        }

    abstract SetStatement : context: 'context * sets: SetStatement -> SetStatement

    default rewriter.SetStatement(context, sets) =
        {
            SetKeyword = rewriter.Terminal(context, sets.SetKeyword)
            Binding = rewriter.SymbolBinding(context, sets.Binding)
            Equals = rewriter.Terminal(context, sets.Equals)
            Value = rewriter.Expression(context, sets.Value)
            Semicolon = rewriter.Terminal(context, sets.Semicolon)
        }

    abstract UpdateStatement : context: 'context * updates: UpdateStatement -> UpdateStatement

    default rewriter.UpdateStatement(context, updates) =
        {
            SetKeyword = rewriter.Terminal(context, updates.SetKeyword)
            Name = rewriter.Terminal(context, updates.Name)
            Operator = rewriter.Terminal(context, updates.Operator)
            Value = rewriter.Expression(context, updates.Value)
            Semicolon = rewriter.Terminal(context, updates.Semicolon)
        }

    abstract SetWith : context: 'context * withs: SetWith -> SetWith

    default rewriter.SetWith(context, withs) =
        {
            SetKeyword = rewriter.Terminal(context, withs.SetKeyword)
            Name = rewriter.Terminal(context, withs.Name)
            With = rewriter.Terminal(context, withs.With)
            Item = rewriter.Expression(context, withs.Item)
            Arrow = rewriter.Terminal(context, withs.Arrow)
            Value = rewriter.Expression(context, withs.Value)
            Semicolon = rewriter.Terminal(context, withs.Semicolon)
        }

    abstract If : context: 'context * ifs: If -> If

    default rewriter.If(context, ifs) =
        {
            IfKeyword = rewriter.Terminal(context, ifs.IfKeyword)
            Condition = rewriter.Expression(context, ifs.Condition)
            Block = rewriter.Block(context, rewriter.Statement, ifs.Block)
        }

    abstract Elif : context: 'context * elifs: Elif -> Elif

    default rewriter.Elif(context, elifs) =
        {
            ElifKeyword = rewriter.Terminal(context, elifs.ElifKeyword)
            Condition = rewriter.Expression(context, elifs.Condition)
            Block = rewriter.Block(context, rewriter.Statement, elifs.Block)
        }

    abstract Else : context: 'context * elses: Else -> Else

    default rewriter.Else(context, elses) =
        {
            ElseKeyword = rewriter.Terminal(context, elses.ElseKeyword)
            Block = rewriter.Block(context, rewriter.Statement, elses.Block)
        }

    abstract For : context: 'context * loop: For -> For

    default rewriter.For(context, loop) =
        {
            ForKeyword = rewriter.Terminal(context, loop.ForKeyword)
            OpenParen = loop.OpenParen |> Option.map (curry rewriter.Terminal context)
            Binding = rewriter.ForBinding(context, loop.Binding)
            CloseParen = loop.CloseParen |> Option.map (curry rewriter.Terminal context)
            Block = rewriter.Block(context, rewriter.Statement, loop.Block)
        }

    abstract While : context: 'context * whiles: While -> While

    default rewriter.While(context, whiles) =
        {
            WhileKeyword = rewriter.Terminal(context, whiles.WhileKeyword)
            Condition = rewriter.Expression(context, whiles.Condition)
            Block = rewriter.Block(context, rewriter.Statement, whiles.Block)
        }

    abstract QubitDeclaration : context: 'context * decl: QubitDeclaration -> QubitDeclaration

    default rewriter.QubitDeclaration(context, decl) =
        {
            Kind = decl.Kind
            Keyword = rewriter.Terminal(context, decl.Keyword)
            OpenParen = decl.OpenParen |> Option.map (curry rewriter.Terminal context)
            Binding = rewriter.QubitBinding(context, decl.Binding)
            CloseParen = decl.CloseParen |> Option.map (curry rewriter.Terminal context)
            Coda =
                match decl.Coda with
                | Semicolon semicolon -> rewriter.Terminal(context, semicolon) |> Semicolon
                | Block block -> rewriter.Block(context, rewriter.Statement, block) |> Block
        }

    abstract ParameterBinding : context: 'context * binding: ParameterBinding -> ParameterBinding

    default rewriter.ParameterBinding(context, binding) =
        match binding with
        | ParameterDeclaration declaration ->
            rewriter.ParameterDeclaration(context, declaration) |> ParameterDeclaration
        | ParameterTuple tuple -> rewriter.Tuple(context, rewriter.ParameterBinding, tuple) |> ParameterTuple

    abstract ParameterDeclaration : context: 'context * declaration: ParameterDeclaration -> ParameterDeclaration

    default rewriter.ParameterDeclaration(context, declaration) =
        {
            Name = rewriter.Terminal(context, declaration.Name)
            Type = rewriter.TypeAnnotation(context, declaration.Type)
        }

    abstract SymbolBinding : context: 'context * symbol: SymbolBinding -> SymbolBinding

    default rewriter.SymbolBinding(context, symbol) =
        match symbol with
        | SymbolDeclaration declaration -> rewriter.Terminal(context, declaration) |> SymbolDeclaration
        | SymbolTuple tuple -> rewriter.Tuple(context, rewriter.SymbolBinding, tuple) |> SymbolTuple

    abstract QubitBinding : context: 'context * binding: QubitBinding -> QubitBinding

    default rewriter.QubitBinding(context, binding) =
        {
            Name = rewriter.SymbolBinding(context, binding.Name)
            Equals = rewriter.Terminal(context, binding.Equals)
            Initializer = rewriter.QubitInitializer(context, binding.Initializer)
        }

    abstract ForBinding : context: 'context * binding: ForBinding -> ForBinding

    default rewriter.ForBinding(context, binding) =
        {
            Name = rewriter.SymbolBinding(context, binding.Name)
            In = rewriter.Terminal(context, binding.In)
            Value = rewriter.Expression(context, binding.Value)
        }

    abstract QubitInitializer : context: 'context * initializer: QubitInitializer -> QubitInitializer

    default rewriter.QubitInitializer(context, initializer) =
        match initializer with
        | SingleQubit singleQubit -> rewriter.SingleQubit(context, singleQubit) |> SingleQubit
        | QubitArray qubitArray -> rewriter.QubitArray(context, qubitArray) |> QubitArray
        | QubitTuple tuple -> rewriter.Tuple(context, rewriter.QubitInitializer, tuple) |> QubitTuple

    abstract SingleQubit : context: 'context * newQubit: SingleQubit -> SingleQubit

    default rewriter.SingleQubit(context, newQubit) =
        {
            Qubit = rewriter.Terminal(context, newQubit.Qubit)
            OpenParen = rewriter.Terminal(context, newQubit.OpenParen)
            CloseParen = rewriter.Terminal(context, newQubit.CloseParen)
        }

    abstract QubitArray : context: 'context * newQubits: QubitArray -> QubitArray

    default rewriter.QubitArray(context, newQubits) =
        {
            Qubit = rewriter.Terminal(context, newQubits.Qubit)
            OpenBracket = rewriter.Terminal(context, newQubits.OpenBracket)
            Length = rewriter.Expression(context, newQubits.Length)
            CloseBracket = rewriter.Terminal(context, newQubits.CloseBracket)
        }

    abstract InterpStringContent : context: 'context * interpStringContent: InterpStringContent -> InterpStringContent

    default rewriter.InterpStringContent(context, interpStringContent) =
        match interpStringContent with
        | Text text -> rewriter.Terminal(context, text) |> Text
        | Expression interpStringExpression ->
            rewriter.InterpStringExpression(context, interpStringExpression) |> Expression

    abstract InterpStringExpression :
        context: 'context * interpStringExpression: InterpStringExpression -> InterpStringExpression

    default rewriter.InterpStringExpression(context, interpStringExpression) =
        {
            OpenBrace = rewriter.Terminal(context, interpStringExpression.OpenBrace)
            Expression = rewriter.Expression(context, interpStringExpression.Expression)
            CloseBrace = rewriter.Terminal(context, interpStringExpression.CloseBrace)
        }

    abstract Expression : context: 'context * expression: Expression -> Expression

    default rewriter.Expression(context, expression) =
        match expression with
        | Missing terminal -> rewriter.Terminal(context, terminal) |> Missing
        | Literal literal -> rewriter.Terminal(context, literal) |> Literal
        | Identifier identifier -> rewriter.Identifier(context, identifier) |> Identifier
        | InterpString interp -> rewriter.InterpString(context, interp) |> InterpString
        | Tuple tuple -> rewriter.Tuple(context, rewriter.Expression, tuple) |> Tuple
        | NewArray newArray -> rewriter.NewArray(context, newArray) |> NewArray
        | NewSizedArray newSizedArray -> rewriter.NewSizedArray(context, newSizedArray) |> NewSizedArray
        | NamedItemAccess namedItemAccess -> rewriter.NamedItemAccess(context, namedItemAccess) |> NamedItemAccess
        | ArrayAccess arrayAccess -> rewriter.ArrayAccess(context, arrayAccess) |> ArrayAccess
        | Call call -> rewriter.Call(context, call) |> Call
        | PrefixOperator operator -> rewriter.PrefixOperator(context, rewriter.Expression, operator) |> PrefixOperator
        | PostfixOperator operator ->
            rewriter.PostfixOperator(context, rewriter.Expression, operator) |> PostfixOperator
        | InfixOperator operator -> rewriter.InfixOperator(context, rewriter.Expression, operator) |> InfixOperator
        | Conditional conditional -> rewriter.Conditional(context, conditional) |> Conditional
        | FullOpenRange fullOpenRange -> rewriter.Terminal(context, fullOpenRange) |> FullOpenRange
        | Update update -> rewriter.Update(context, update) |> Update
        | Expression.Unknown terminal -> rewriter.Terminal(context, terminal) |> Expression.Unknown

    abstract Identifier : context: 'context * identifier: Identifier -> Identifier

    default rewriter.Identifier(context, identifier) =
        {
            Name = rewriter.Terminal(context, identifier.Name)
            TypeArgs = identifier.TypeArgs |> Option.map (curry3 rewriter.Tuple context rewriter.Type)
        }

    abstract InterpString : context: 'context * interpString: InterpString -> InterpString

    default rewriter.InterpString(context, interpString) =
        {
            OpenQuote = rewriter.Terminal(context, interpString.OpenQuote)
            Content = interpString.Content |> List.map (curry rewriter.InterpStringContent context)
            CloseQuote = rewriter.Terminal(context, interpString.CloseQuote)
        }

    abstract NewArray : context: 'context * newArray: NewArray -> NewArray

    default rewriter.NewArray(context, newArray) =
        {
            New = rewriter.Terminal(context, newArray.New)
            ItemType = rewriter.Type(context, newArray.ItemType)
            OpenBracket = rewriter.Terminal(context, newArray.OpenBracket)
            Length = rewriter.Expression(context, newArray.Length)
            CloseBracket = rewriter.Terminal(context, newArray.CloseBracket)
        }

    abstract NewSizedArray : context: 'context * newSizedArray: NewSizedArray -> NewSizedArray

    default rewriter.NewSizedArray(context, newSizedArray) =
        {
            OpenBracket = rewriter.Terminal(context, newSizedArray.OpenBracket)
            Value = rewriter.Expression(context, newSizedArray.Value)
            Comma = rewriter.Terminal(context, newSizedArray.Comma)
            Size = rewriter.Terminal(context, newSizedArray.Size)
            Equals = rewriter.Terminal(context, newSizedArray.Equals)
            Length = rewriter.Expression(context, newSizedArray.Length)
            CloseBracket = rewriter.Terminal(context, newSizedArray.CloseBracket)
        }

    abstract NamedItemAccess : context: 'context * namedItemAccess: NamedItemAccess -> NamedItemAccess

    default rewriter.NamedItemAccess(context, namedItemAccess) =
        {
            Record = rewriter.Expression(context, namedItemAccess.Record)
            DoubleColon = rewriter.Terminal(context, namedItemAccess.DoubleColon)
            Name = rewriter.Terminal(context, namedItemAccess.Name)
        }

    abstract ArrayAccess : context: 'context * arrayAccess: ArrayAccess -> ArrayAccess

    default rewriter.ArrayAccess(context, arrayAccess) =
        {
            Array = rewriter.Expression(context, arrayAccess.Array)
            OpenBracket = rewriter.Terminal(context, arrayAccess.OpenBracket)
            Index = rewriter.Expression(context, arrayAccess.Index)
            CloseBracket = rewriter.Terminal(context, arrayAccess.CloseBracket)
        }

    abstract Call : context: 'context * call: Call -> Call

    default rewriter.Call(context, call) =
        {
            Callable = rewriter.Expression(context, call.Callable)
            Arguments = rewriter.Tuple(context, rewriter.Expression, call.Arguments)
        }

    abstract Conditional : context: 'context * conditional: Conditional -> Conditional

    default rewriter.Conditional(context, conditional) =
        {
            Condition = rewriter.Expression(context, conditional.Condition)
            Question = rewriter.Terminal(context, conditional.Question)
            IfTrue = rewriter.Expression(context, conditional.IfTrue)
            Pipe = rewriter.Terminal(context, conditional.Pipe)
            IfFalse = rewriter.Expression(context, conditional.IfFalse)
        }

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

    abstract PrefixOperator :
        context: 'context * mapper: ('context * 'a -> 'a) * operator: 'a PrefixOperator -> 'a PrefixOperator

    default rewriter.PrefixOperator(context, mapper, operator) =
        {
            PrefixOperator = rewriter.Terminal(context, operator.PrefixOperator)
            Operand = mapper (context, operator.Operand)
        }

    abstract PostfixOperator :
        context: 'context * mapper: ('context * 'a -> 'a) * operator: 'a PostfixOperator -> 'a PostfixOperator

    default rewriter.PostfixOperator(context, mapper, operator) =
        {
            Operand = mapper (context, operator.Operand)
            PostfixOperator = rewriter.Terminal(context, operator.PostfixOperator)
        }

    abstract InfixOperator :
        context: 'context * mapper: ('context * 'a -> 'a) * operator: 'a InfixOperator -> 'a InfixOperator

    default rewriter.InfixOperator(context, mapper, operator) =
        {
            Left = mapper (context, operator.Left)
            InfixOperator = rewriter.Terminal(context, operator.InfixOperator)
            Right = mapper (context, operator.Right)
        }

    abstract Terminal : context: 'context * terminal: Terminal -> Terminal
    default _.Terminal(_, terminal) = terminal
