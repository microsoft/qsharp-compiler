// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

open Microsoft.Quantum.QsFmt.Formatter.Utils

type 'context Rewriter() as rewriter =

    /// The default behavior to rewrite a SimpleStatement.
    let defaultSimpleStatement (context: 'context) (statement: SimpleStatement) =
        {
            Keyword = rewriter.Terminal(context, statement.Keyword)
            Expression = rewriter.Expression(context, statement.Expression)
            Semicolon = rewriter.Terminal(context, statement.Semicolon)
        }

    /// The default behavior to rewrite a BindingStatement.
    let defaultBindingStatement (context: 'context) (statement: BindingStatement) =
        {
            Keyword = rewriter.Terminal(context, statement.Keyword)
            Binding = rewriter.SymbolBinding(context, statement.Binding)
            Equals = rewriter.Terminal(context, statement.Equals)
            Value = rewriter.Expression(context, statement.Value)
            Semicolon = rewriter.Terminal(context, statement.Semicolon)
        }

    /// The default behavior to rewrite a ConditionalBlockStatement.
    let defaultConditionalBlockStatement (context: 'context) (statement: ConditionalBlockStatement) =
        {
            Keyword = rewriter.Terminal(context, statement.Keyword)
            Condition = rewriter.Expression(context, statement.Condition)
            Block = rewriter.Block(context, rewriter.Statement, statement.Block)
        }

    /// The default behavior to rewrite a BlockStatement.
    let defaultBlockStatement (context: 'context) (statement: BlockStatement) =
        {
            Keyword = rewriter.Terminal(context, statement.Keyword)
            Block = rewriter.Block(context, rewriter.Statement, statement.Block)
        }

    abstract Document: context: 'context * document: Document -> Document

    default _.Document(context, document) =
        {
            Namespaces = document.Namespaces |> List.map (curry rewriter.Namespace context)
            Eof = rewriter.Terminal(context, document.Eof)
        }

    abstract Namespace: context: 'context * ns: Namespace -> Namespace

    default _.Namespace(context, ns) =
        {
            NamespaceKeyword = rewriter.Terminal(context, ns.NamespaceKeyword)
            Name = rewriter.Terminal(context, ns.Name)
            Block = rewriter.Block(context, rewriter.NamespaceItem, ns.Block)
        }

    abstract NamespaceItem: context: 'context * item: NamespaceItem -> NamespaceItem

    default _.NamespaceItem(context, item) =
        match item with
        | OpenDirective directive -> rewriter.OpenDirective(context, directive) |> OpenDirective
        | TypeDeclaration declaration -> rewriter.TypeDeclaration(context, declaration) |> TypeDeclaration
        | CallableDeclaration callable -> rewriter.CallableDeclaration(context, callable) |> CallableDeclaration
        | Unknown terminal -> rewriter.Terminal(context, terminal) |> Unknown

    abstract OpenDirective: context: 'context * directive: OpenDirective -> OpenDirective

    default rewriter.OpenDirective(context, directive) =
        {
            OpenKeyword = rewriter.Terminal(context, directive.OpenKeyword)
            OpenName = rewriter.Terminal(context, directive.OpenName)
            AsKeyword = directive.AsKeyword |> Option.map (curry rewriter.Terminal context)
            AsName = directive.AsName |> Option.map (curry rewriter.Terminal context)
            Semicolon = rewriter.Terminal(context, directive.Semicolon)
        }

    abstract TypeDeclaration: context: 'context * declaration: TypeDeclaration -> TypeDeclaration

    default rewriter.TypeDeclaration(context, declaration) =
        {
            Attributes = declaration.Attributes |> List.map (curry rewriter.Attribute context)
            Access = declaration.Access |> Option.map (curry rewriter.Terminal context)
            NewtypeKeyword = rewriter.Terminal(context, declaration.NewtypeKeyword)
            DeclaredType = rewriter.Terminal(context, declaration.DeclaredType)
            Equals = rewriter.Terminal(context, declaration.Equals)
            UnderlyingType = rewriter.UnderlyingType(context, declaration.UnderlyingType)
            Semicolon = rewriter.Terminal(context, declaration.Semicolon)
        }

    abstract Attribute: context: 'context * attribute: Attribute -> Attribute

    default _.Attribute(context, attribute) =
        {
            At = rewriter.Terminal(context, attribute.At)
            Expression = rewriter.Expression(context, attribute.Expression)
        }

    abstract UnderlyingType: context: 'context * underlying: UnderlyingType -> UnderlyingType

    default rewriter.UnderlyingType(context, underlying) =
        match underlying with
        | TypeDeclarationTuple tuple -> rewriter.Tuple(context, rewriter.TypeTupleItem, tuple) |> TypeDeclarationTuple
        | Type _type -> rewriter.Type(context, _type) |> Type

    abstract TypeTupleItem: context: 'context * item: TypeTupleItem -> TypeTupleItem

    default rewriter.TypeTupleItem(context, item) =
        match item with
        | TypeBinding binding -> rewriter.ParameterDeclaration(context, binding) |> TypeBinding
        | UnderlyingType underlying -> rewriter.UnderlyingType(context, underlying) |> UnderlyingType

    abstract CallableDeclaration: context: 'context * callable: CallableDeclaration -> CallableDeclaration

    default _.CallableDeclaration(context, callable) =
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

    abstract TypeParameterBinding: context: 'context * binding: TypeParameterBinding -> TypeParameterBinding

    default _.TypeParameterBinding(context, binding) =
        {
            OpenBracket = rewriter.Terminal(context, binding.OpenBracket)
            Parameters = binding.Parameters |> List.map (curry3 rewriter.SequenceItem context rewriter.Terminal)
            CloseBracket = rewriter.Terminal(context, binding.CloseBracket)
        }

    abstract Type: context: 'context * typ: Type -> Type

    default _.Type(context, typ) =
        match typ with
        | Type.Missing missing -> rewriter.Terminal(context, missing) |> Type.Missing
        | Parameter name -> rewriter.Terminal(context, name) |> Parameter
        | Type.BuiltIn name -> rewriter.Terminal(context, name) |> Type.BuiltIn
        | UserDefined name -> rewriter.Terminal(context, name) |> UserDefined
        | Type.Tuple tuple -> rewriter.Tuple(context, rewriter.Type, tuple) |> Type.Tuple
        | Array array -> rewriter.ArrayType(context, array) |> Array
        | Type.Callable callable -> rewriter.CallableType(context, callable) |> Type.Callable
        | Type.Unknown terminal -> rewriter.Terminal(context, terminal) |> Type.Unknown

    abstract TypeAnnotation: context: 'context * annotation: TypeAnnotation -> TypeAnnotation

    default _.TypeAnnotation(context, annotation) =
        { Colon = rewriter.Terminal(context, annotation.Colon); Type = rewriter.Type(context, annotation.Type) }

    abstract ArrayType: context: 'context * array: ArrayType -> ArrayType

    default _.ArrayType(context, array) =
        {
            ItemType = rewriter.Type(context, array.ItemType)
            OpenBracket = rewriter.Terminal(context, array.OpenBracket)
            CloseBracket = rewriter.Terminal(context, array.CloseBracket)
        }

    abstract CallableType: context: 'context * callable: CallableType -> CallableType

    default _.CallableType(context, callable) =
        {
            FromType = rewriter.Type(context, callable.FromType)
            Arrow = rewriter.Terminal(context, callable.Arrow)
            ToType = rewriter.Type(context, callable.ToType)
            Characteristics = callable.Characteristics |> Option.map (curry rewriter.CharacteristicSection context)
        }

    abstract CharacteristicSection: context: 'context * section: CharacteristicSection -> CharacteristicSection

    default _.CharacteristicSection(context, section) =
        {
            IsKeyword = rewriter.Terminal(context, section.IsKeyword)
            Characteristic = rewriter.Characteristic(context, section.Characteristic)
        }

    abstract CharacteristicGroup: context: 'context * group: CharacteristicGroup -> CharacteristicGroup

    default _.CharacteristicGroup(context, group) =
        {
            OpenParen = rewriter.Terminal(context, group.OpenParen)
            Characteristic = rewriter.Characteristic(context, group.Characteristic)
            CloseParen = rewriter.Terminal(context, group.CloseParen)
        }

    abstract Characteristic: context: 'context * characteristic: Characteristic -> Characteristic

    default _.Characteristic(context, characteristic) =
        match characteristic with
        | Adjoint adjoint -> rewriter.Terminal(context, adjoint) |> Adjoint
        | Controlled controlled -> rewriter.Terminal(context, controlled) |> Controlled
        | Group group -> rewriter.CharacteristicGroup(context, group) |> Group
        | Characteristic.InfixOperator operator ->
            rewriter.InfixOperator(context, rewriter.Characteristic, operator) |> Characteristic.InfixOperator

    abstract CallableBody: context: 'context * body: CallableBody -> CallableBody

    default _.CallableBody(context, body) =
        match body with
        | Statements statements -> rewriter.Block(context, rewriter.Statement, statements) |> Statements
        | Specializations specializations ->
            rewriter.Block(context, rewriter.Specialization, specializations) |> Specializations

    abstract Specialization: context: 'context * specialization: Specialization -> Specialization

    default _.Specialization(context, specialization) =
        {
            Names = specialization.Names |> List.map (curry rewriter.Terminal context)
            Generator = rewriter.SpecializationGenerator(context, specialization.Generator)
        }

    abstract SpecializationGenerator: context: 'context * generator: SpecializationGenerator -> SpecializationGenerator

    default _.SpecializationGenerator(context, generator) =
        match generator with
        | BuiltIn (name, semicolon) ->
            BuiltIn(name = rewriter.Terminal(context, name), semicolon = rewriter.Terminal(context, semicolon))
        | Provided (parameters, statements) ->
            Provided(
                parameters = (parameters |> Option.map (curry3 rewriter.Tuple context rewriter.Terminal)),
                statements = rewriter.Block(context, rewriter.Statement, statements)
            )

    abstract Statement: context: 'context * statement: Statement -> Statement

    default _.Statement(context, statement) =
        match statement with
        | ExpressionStatement expr -> rewriter.ExpressionStatement(context, expr) |> ExpressionStatement
        | ReturnStatement returns -> rewriter.ReturnStatement(context, returns) |> ReturnStatement
        | FailStatement fails -> rewriter.FailStatement(context, fails) |> FailStatement
        | LetStatement lets -> rewriter.LetStatement(context, lets) |> LetStatement
        | MutableStatement mutables -> rewriter.MutableStatement(context, mutables) |> MutableStatement
        | SetStatement sets -> rewriter.SetStatement(context, sets) |> SetStatement
        | UpdateStatement updates -> rewriter.UpdateStatement(context, updates) |> UpdateStatement
        | UpdateWithStatement withs -> rewriter.UpdateWithStatement(context, withs) |> UpdateWithStatement
        | IfStatement ifs -> rewriter.IfStatement(context, ifs) |> IfStatement
        | ElifStatement elifs -> rewriter.ElifStatement(context, elifs) |> ElifStatement
        | ElseStatement elses -> rewriter.ElseStatement(context, elses) |> ElseStatement
        | ForStatement loop -> rewriter.ForStatement(context, loop) |> ForStatement
        | WhileStatement whiles -> rewriter.WhileStatement(context, whiles) |> WhileStatement
        | RepeatStatement repeats -> rewriter.RepeatStatement(context, repeats) |> RepeatStatement
        | UntilStatement untils -> rewriter.UntilStatement(context, untils) |> UntilStatement
        | WithinStatement withins -> rewriter.WithinStatement(context, withins) |> WithinStatement
        | ApplyStatement apply -> rewriter.ApplyStatement(context, apply) |> ApplyStatement
        | QubitDeclarationStatement decl ->
            rewriter.QubitDeclarationStatement(context, decl) |> QubitDeclarationStatement
        | Statement.Unknown terminal -> rewriter.Terminal(context, terminal) |> Statement.Unknown

    abstract ExpressionStatement: context: 'context * expr: ExpressionStatement -> ExpressionStatement

    default _.ExpressionStatement(context, expr) =
        {
            Expression = rewriter.Expression(context, expr.Expression)
            Semicolon = rewriter.Terminal(context, expr.Semicolon)
        }

    abstract ReturnStatement: context: 'context * returns: SimpleStatement -> SimpleStatement

    default _.ReturnStatement(context, returns) = defaultSimpleStatement context returns

    abstract FailStatement: context: 'context * fails: SimpleStatement -> SimpleStatement

    default _.FailStatement(context, fails) = defaultSimpleStatement context fails

    abstract LetStatement: context: 'context * lets: BindingStatement -> BindingStatement

    default _.LetStatement(context, lets) = defaultBindingStatement context lets

    abstract MutableStatement: context: 'context * mutables: BindingStatement -> BindingStatement

    default _.MutableStatement(context, mutables) =
        defaultBindingStatement context mutables

    abstract SetStatement: context: 'context * sets: BindingStatement -> BindingStatement

    default _.SetStatement(context, sets) = defaultBindingStatement context sets

    abstract UpdateStatement: context: 'context * updates: UpdateStatement -> UpdateStatement

    default _.UpdateStatement(context, updates) =
        {
            SetKeyword = rewriter.Terminal(context, updates.SetKeyword)
            Name = rewriter.Terminal(context, updates.Name)
            Operator = rewriter.Terminal(context, updates.Operator)
            Value = rewriter.Expression(context, updates.Value)
            Semicolon = rewriter.Terminal(context, updates.Semicolon)
        }

    abstract UpdateWithStatement: context: 'context * withs: UpdateWithStatement -> UpdateWithStatement

    default _.UpdateWithStatement(context, withs) =
        {
            SetKeyword = rewriter.Terminal(context, withs.SetKeyword)
            Name = rewriter.Terminal(context, withs.Name)
            With = rewriter.Terminal(context, withs.With)
            Item = rewriter.Expression(context, withs.Item)
            Arrow = rewriter.Terminal(context, withs.Arrow)
            Value = rewriter.Expression(context, withs.Value)
            Semicolon = rewriter.Terminal(context, withs.Semicolon)
        }

    abstract IfStatement: context: 'context * ifs: ConditionalBlockStatement -> ConditionalBlockStatement

    default _.IfStatement(context, ifs) =
        defaultConditionalBlockStatement context ifs

    abstract ElifStatement: context: 'context * elifs: ConditionalBlockStatement -> ConditionalBlockStatement

    default _.ElifStatement(context, elifs) =
        defaultConditionalBlockStatement context elifs

    abstract ElseStatement: context: 'context * elses: BlockStatement -> BlockStatement

    default _.ElseStatement(context, elses) = defaultBlockStatement context elses

    abstract ForStatement: context: 'context * loop: ForStatement -> ForStatement

    default _.ForStatement(context, loop) =
        {
            ForKeyword = rewriter.Terminal(context, loop.ForKeyword)
            OpenParen = loop.OpenParen |> Option.map (curry rewriter.Terminal context)
            Binding = rewriter.ForBinding(context, loop.Binding)
            CloseParen = loop.CloseParen |> Option.map (curry rewriter.Terminal context)
            Block = rewriter.Block(context, rewriter.Statement, loop.Block)
        }

    abstract WhileStatement: context: 'context * whiles: ConditionalBlockStatement -> ConditionalBlockStatement

    default _.WhileStatement(context, whiles) =
        defaultConditionalBlockStatement context whiles

    abstract RepeatStatement: context: 'context * repeats: BlockStatement -> BlockStatement

    default _.RepeatStatement(context, repeats) = defaultBlockStatement context repeats

    abstract UntilStatement: context: 'context * untils: UntilStatement -> UntilStatement

    default _.UntilStatement(context, untils) =
        {
            UntilKeyword = rewriter.Terminal(context, untils.UntilKeyword)
            Condition = rewriter.Expression(context, untils.Condition)
            Coda =
                match untils.Coda with
                | UntilStatementCoda.Semicolon semicolon ->
                    rewriter.Terminal(context, semicolon) |> UntilStatementCoda.Semicolon
                | Fixup fixup -> rewriter.Fixup(context, fixup) |> Fixup
        }

    abstract Fixup: context: 'context * fixup: BlockStatement -> BlockStatement

    default _.Fixup(context, fixup) = defaultBlockStatement context fixup

    abstract WithinStatement: context: 'context * withins: BlockStatement -> BlockStatement

    default _.WithinStatement(context, withins) = defaultBlockStatement context withins

    abstract ApplyStatement: context: 'context * apply: BlockStatement -> BlockStatement

    default _.ApplyStatement(context, apply) = defaultBlockStatement context apply

    abstract QubitDeclarationStatement: context: 'context * decl: QubitDeclarationStatement -> QubitDeclarationStatement

    default _.QubitDeclarationStatement(context, decl) =
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

    abstract ParameterBinding: context: 'context * binding: ParameterBinding -> ParameterBinding

    default _.ParameterBinding(context, binding) =
        match binding with
        | ParameterDeclaration declaration ->
            rewriter.ParameterDeclaration(context, declaration) |> ParameterDeclaration
        | ParameterTuple tuple -> rewriter.Tuple(context, rewriter.ParameterBinding, tuple) |> ParameterTuple

    abstract ParameterDeclaration: context: 'context * declaration: ParameterDeclaration -> ParameterDeclaration

    default _.ParameterDeclaration(context, declaration) =
        {
            Name = rewriter.Terminal(context, declaration.Name)
            Type = rewriter.TypeAnnotation(context, declaration.Type)
        }

    abstract SymbolBinding: context: 'context * symbol: SymbolBinding -> SymbolBinding

    default _.SymbolBinding(context, symbol) =
        match symbol with
        | SymbolDeclaration declaration -> rewriter.Terminal(context, declaration) |> SymbolDeclaration
        | SymbolTuple tuple -> rewriter.Tuple(context, rewriter.SymbolBinding, tuple) |> SymbolTuple

    abstract QubitBinding: context: 'context * binding: QubitBinding -> QubitBinding

    default _.QubitBinding(context, binding) =
        {
            Name = rewriter.SymbolBinding(context, binding.Name)
            Equals = rewriter.Terminal(context, binding.Equals)
            Initializer = rewriter.QubitInitializer(context, binding.Initializer)
        }

    abstract ForBinding: context: 'context * binding: ForBinding -> ForBinding

    default _.ForBinding(context, binding) =
        {
            Name = rewriter.SymbolBinding(context, binding.Name)
            In = rewriter.Terminal(context, binding.In)
            Value = rewriter.Expression(context, binding.Value)
        }

    abstract QubitInitializer: context: 'context * initializer: QubitInitializer -> QubitInitializer

    default _.QubitInitializer(context, initializer) =
        match initializer with
        | SingleQubit singleQubit -> rewriter.SingleQubit(context, singleQubit) |> SingleQubit
        | QubitArray qubitArray -> rewriter.QubitArray(context, qubitArray) |> QubitArray
        | QubitTuple tuple -> rewriter.Tuple(context, rewriter.QubitInitializer, tuple) |> QubitTuple

    abstract SingleQubit: context: 'context * newQubit: SingleQubit -> SingleQubit

    default _.SingleQubit(context, newQubit) =
        {
            Qubit = rewriter.Terminal(context, newQubit.Qubit)
            OpenParen = rewriter.Terminal(context, newQubit.OpenParen)
            CloseParen = rewriter.Terminal(context, newQubit.CloseParen)
        }

    abstract QubitArray: context: 'context * newQubits: QubitArray -> QubitArray

    default _.QubitArray(context, newQubits) =
        {
            Qubit = rewriter.Terminal(context, newQubits.Qubit)
            OpenBracket = rewriter.Terminal(context, newQubits.OpenBracket)
            Length = rewriter.Expression(context, newQubits.Length)
            CloseBracket = rewriter.Terminal(context, newQubits.CloseBracket)
        }

    abstract InterpStringContent: context: 'context * interpStringContent: InterpStringContent -> InterpStringContent

    default _.InterpStringContent(context, interpStringContent) =
        match interpStringContent with
        | Text text -> rewriter.Terminal(context, text) |> Text
        | Expression interpStringExpression ->
            rewriter.InterpStringExpression(context, interpStringExpression) |> Expression

    abstract InterpStringExpression:
        context: 'context * interpStringExpression: InterpStringExpression -> InterpStringExpression

    default _.InterpStringExpression(context, interpStringExpression) =
        {
            OpenBrace = rewriter.Terminal(context, interpStringExpression.OpenBrace)
            Expression = rewriter.Expression(context, interpStringExpression.Expression)
            CloseBrace = rewriter.Terminal(context, interpStringExpression.CloseBrace)
        }

    abstract Expression: context: 'context * expression: Expression -> Expression

    default _.Expression(context, expression) =
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
        | Lambda lambda -> rewriter.Lambda(context, lambda) |> Lambda
        | Expression.Unknown terminal -> rewriter.Terminal(context, terminal) |> Expression.Unknown

    abstract Identifier: context: 'context * identifier: Identifier -> Identifier

    default _.Identifier(context, identifier) =
        {
            Name = rewriter.Terminal(context, identifier.Name)
            TypeArgs = identifier.TypeArgs |> Option.map (curry3 rewriter.Tuple context rewriter.Type)
        }

    abstract InterpString: context: 'context * interpString: InterpString -> InterpString

    default _.InterpString(context, interpString) =
        {
            OpenQuote = rewriter.Terminal(context, interpString.OpenQuote)
            Content = interpString.Content |> List.map (curry rewriter.InterpStringContent context)
            CloseQuote = rewriter.Terminal(context, interpString.CloseQuote)
        }

    abstract NewArray: context: 'context * newArray: NewArray -> NewArray

    default _.NewArray(context, newArray) =
        {
            New = rewriter.Terminal(context, newArray.New)
            ItemType = rewriter.Type(context, newArray.ItemType)
            OpenBracket = rewriter.Terminal(context, newArray.OpenBracket)
            Length = rewriter.Expression(context, newArray.Length)
            CloseBracket = rewriter.Terminal(context, newArray.CloseBracket)
        }

    abstract NewSizedArray: context: 'context * newSizedArray: NewSizedArray -> NewSizedArray

    default _.NewSizedArray(context, newSizedArray) =
        {
            OpenBracket = rewriter.Terminal(context, newSizedArray.OpenBracket)
            Value = rewriter.Expression(context, newSizedArray.Value)
            Comma = rewriter.Terminal(context, newSizedArray.Comma)
            Size = rewriter.Terminal(context, newSizedArray.Size)
            Equals = rewriter.Terminal(context, newSizedArray.Equals)
            Length = rewriter.Expression(context, newSizedArray.Length)
            CloseBracket = rewriter.Terminal(context, newSizedArray.CloseBracket)
        }

    abstract NamedItemAccess: context: 'context * namedItemAccess: NamedItemAccess -> NamedItemAccess

    default _.NamedItemAccess(context, namedItemAccess) =
        {
            Record = rewriter.Expression(context, namedItemAccess.Record)
            DoubleColon = rewriter.Terminal(context, namedItemAccess.DoubleColon)
            Name = rewriter.Terminal(context, namedItemAccess.Name)
        }

    abstract ArrayAccess: context: 'context * arrayAccess: ArrayAccess -> ArrayAccess

    default _.ArrayAccess(context, arrayAccess) =
        {
            Array = rewriter.Expression(context, arrayAccess.Array)
            OpenBracket = rewriter.Terminal(context, arrayAccess.OpenBracket)
            Index = rewriter.Expression(context, arrayAccess.Index)
            CloseBracket = rewriter.Terminal(context, arrayAccess.CloseBracket)
        }

    abstract Call: context: 'context * call: Call -> Call

    default _.Call(context, call) =
        {
            Callable = rewriter.Expression(context, call.Callable)
            Arguments = rewriter.Tuple(context, rewriter.Expression, call.Arguments)
        }

    abstract Conditional: context: 'context * conditional: Conditional -> Conditional

    default _.Conditional(context, conditional) =
        {
            Condition = rewriter.Expression(context, conditional.Condition)
            Question = rewriter.Terminal(context, conditional.Question)
            IfTrue = rewriter.Expression(context, conditional.IfTrue)
            Pipe = rewriter.Terminal(context, conditional.Pipe)
            IfFalse = rewriter.Expression(context, conditional.IfFalse)
        }

    abstract Update: context: 'context * update: Update -> Update

    default _.Update(context, update) =
        {
            Record = rewriter.Expression(context, update.Record)
            With = rewriter.Terminal(context, update.With)
            Item = rewriter.Expression(context, update.Item)
            Arrow = rewriter.Terminal(context, update.Arrow)
            Value = rewriter.Expression(context, update.Value)
        }

    abstract Lambda: context: 'context * lambda: Lambda -> Lambda

    default _.Lambda(context, lambda) =
        {
            Binding = rewriter.SymbolBinding(context, lambda.Binding)
            Arrow = rewriter.Terminal(context, lambda.Arrow)
            Body = rewriter.Expression(context, lambda.Body)
        }

    abstract Block: context: 'context * mapper: ('context * 'a -> 'a) * block: 'a Block -> 'a Block

    default _.Block(context, mapper, block) =
        {
            OpenBrace = rewriter.Terminal(context, block.OpenBrace)
            Items = block.Items |> List.map (curry mapper context)
            CloseBrace = rewriter.Terminal(context, block.CloseBrace)
        }

    abstract Tuple: context: 'context * mapper: ('context * 'a -> 'a) * tuple: 'a Tuple -> 'a Tuple

    default _.Tuple(context, mapper, tuple) =
        {
            OpenParen = rewriter.Terminal(context, tuple.OpenParen)
            Items = tuple.Items |> List.map (curry3 rewriter.SequenceItem context mapper)
            CloseParen = rewriter.Terminal(context, tuple.CloseParen)
        }

    abstract SequenceItem: context: 'context * mapper: ('context * 'a -> 'a) * item: 'a SequenceItem -> 'a SequenceItem

    default _.SequenceItem(context, mapper, item) =
        {
            Item = item.Item |> Option.map (curry mapper context)
            Comma = item.Comma |> Option.map (curry rewriter.Terminal context)
        }

    abstract PrefixOperator:
        context: 'context * mapper: ('context * 'a -> 'a) * operator: 'a PrefixOperator -> 'a PrefixOperator

    default _.PrefixOperator(context, mapper, operator) =
        {
            PrefixOperator = rewriter.Terminal(context, operator.PrefixOperator)
            Operand = mapper (context, operator.Operand)
        }

    abstract PostfixOperator:
        context: 'context * mapper: ('context * 'a -> 'a) * operator: 'a PostfixOperator -> 'a PostfixOperator

    default _.PostfixOperator(context, mapper, operator) =
        {
            Operand = mapper (context, operator.Operand)
            PostfixOperator = rewriter.Terminal(context, operator.PostfixOperator)
        }

    abstract InfixOperator:
        context: 'context * mapper: ('context * 'a -> 'a) * operator: 'a InfixOperator -> 'a InfixOperator

    default _.InfixOperator(context, mapper, operator) =
        {
            Left = mapper (context, operator.Left)
            InfixOperator = rewriter.Terminal(context, operator.InfixOperator)
            Right = mapper (context, operator.Right)
        }

    abstract Terminal: context: 'context * terminal: Terminal -> Terminal
    default _.Terminal(_, terminal) = terminal
