// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

open Microsoft.Quantum.QsFmt.Formatter.Utils

[<AbstractClass>]
type internal 'Result Reducer() as reducer =
    /// Reduces a list of results into a single result.
    let reduce = curry reducer.Combine |> List.reduce

    /// The default behavior to reduce a SimpleStatement.
    let defaultSimpleStatement (statement: SimpleStatement) =
        [
            reducer.Terminal statement.Keyword
            reducer.Expression statement.Expression
            reducer.Terminal statement.Semicolon
        ]
        |> reduce

    /// The default behavior to reduce a BindingStatement.
    let defaultBindingStatement (statement: BindingStatement) =
        [
            reducer.Terminal statement.Keyword
            reducer.SymbolBinding statement.Binding
            reducer.Terminal statement.Equals
            reducer.Expression statement.Value
            reducer.Terminal statement.Semicolon
        ]
        |> reduce

    /// The default behavior to reduce a ConditionalBlockStatement.
    let defaultConditionalBlockStatement (statement: ConditionalBlockStatement) =
        [
            reducer.Terminal statement.Keyword
            reducer.Expression statement.Condition
            reducer.Block(reducer.Statement, statement.Block)
        ]
        |> reduce

    /// The default behavior to reduce a BlockStatement.
    let defaultBlockStatement (statement: BlockStatement) =
        [
            reducer.Terminal statement.Keyword
            reducer.Block(reducer.Statement, statement.Block)
        ]
        |> reduce

    abstract Combine: 'Result * 'Result -> 'Result

    abstract Document: document: Document -> 'Result

    default _.Document document =
        (document.Namespaces |> List.map reducer.Namespace) @ [ reducer.Terminal document.Eof ] |> reduce

    abstract Namespace: ns: Namespace -> 'Result

    default _.Namespace ns =
        [
            reducer.Terminal ns.NamespaceKeyword
            reducer.Terminal ns.Name
            reducer.Block(reducer.NamespaceItem, ns.Block)
        ]
        |> reduce

    abstract NamespaceItem: item: NamespaceItem -> 'Result

    default _.NamespaceItem item =
        match item with
        | OpenDirective directive -> reducer.OpenDirective directive
        | TypeDeclaration delcaration -> reducer.TypeDeclaration delcaration
        | CallableDeclaration callable -> reducer.CallableDeclaration callable
        | Unknown terminal -> reducer.Terminal terminal

    abstract OpenDirective: directive: OpenDirective -> 'Result

    default _.OpenDirective directive =
        [ reducer.Terminal directive.OpenKeyword; reducer.Terminal directive.OpenName ]
        @ (directive.AsKeyword |> Option.map reducer.Terminal |> Option.toList)
          @ (directive.AsName |> Option.map reducer.Terminal |> Option.toList)
            @ [ reducer.Terminal directive.Semicolon ]
        |> reduce

    abstract TypeDeclaration: declaration: TypeDeclaration -> 'Result

    default _.TypeDeclaration declaration =
        (declaration.Attributes |> List.map reducer.Attribute)
        @ (declaration.Access |> Option.map reducer.Terminal |> Option.toList)
          @ [
              reducer.Terminal declaration.NewtypeKeyword
              reducer.Terminal declaration.DeclaredType
              reducer.Terminal declaration.Equals
              reducer.UnderlyingType declaration.UnderlyingType
              reducer.Terminal declaration.Semicolon
          ]
        |> reduce

    abstract Attribute: attribute: Attribute -> 'Result

    default _.Attribute attribute =
        [ reducer.Terminal attribute.At; reducer.Expression attribute.Expression ] |> reduce

    abstract UnderlyingType: underlying: UnderlyingType -> 'Result

    default _.UnderlyingType underlying =
        match underlying with
        | TypeDeclarationTuple tuple -> reducer.Tuple(reducer.TypeTupleItem, tuple)
        | Type _type -> reducer.Type _type

    abstract TypeTupleItem: item: TypeTupleItem -> 'Result

    default _.TypeTupleItem item =
        match item with
        | TypeBinding binding -> reducer.ParameterDeclaration binding
        | UnderlyingType underlying -> reducer.UnderlyingType underlying

    abstract CallableDeclaration: callable: CallableDeclaration -> 'Result

    default _.CallableDeclaration callable =
        [
            callable.Attributes |> List.map reducer.Attribute
            callable.Access |> Option.map reducer.Terminal |> Option.toList
            [ reducer.Terminal callable.CallableKeyword; reducer.Terminal callable.Name ]
            callable.TypeParameters |> Option.map reducer.TypeParameterBinding |> Option.toList
            [
                reducer.ParameterBinding callable.Parameters
                reducer.TypeAnnotation callable.ReturnType
            ]
            callable.CharacteristicSection |> Option.map reducer.CharacteristicSection |> Option.toList
            [ reducer.CallableBody callable.Body ]
        ]
        |> List.concat
        |> reduce

    abstract TypeParameterBinding: binding: TypeParameterBinding -> 'Result

    default _.TypeParameterBinding binding =
        reducer.Terminal binding.OpenBracket
        :: (binding.Parameters |> List.map (curry reducer.SequenceItem reducer.Terminal))
        @ [ reducer.Terminal binding.CloseBracket ]
        |> reduce

    abstract Type: typ: Type -> 'Result

    default _.Type typ =
        match typ with
        | Type.Missing missing -> reducer.Terminal missing
        | Parameter name
        | Type.BuiltIn name
        | UserDefined name -> reducer.Terminal name
        | Type.Tuple tuple -> reducer.Tuple(reducer.Type, tuple)
        | Array array -> reducer.ArrayType array
        | Callable callable -> reducer.CallableType callable
        | Type.Unknown terminal -> reducer.Terminal terminal

    abstract TypeAnnotation: annotation: TypeAnnotation -> 'Result

    default _.TypeAnnotation annotation =
        [ reducer.Terminal annotation.Colon; reducer.Type annotation.Type ] |> reduce

    abstract ArrayType: array: ArrayType -> 'Result

    default _.ArrayType array =
        [
            reducer.Type array.ItemType
            reducer.Terminal array.OpenBracket
            reducer.Terminal array.CloseBracket
        ]
        |> reduce

    abstract CallableType: callable: CallableType -> 'Result

    default _.CallableType callable =
        [
            reducer.Type callable.FromType
            reducer.Terminal callable.Arrow
            reducer.Type callable.ToType
        ]
        @ (callable.Characteristics |> Option.map reducer.CharacteristicSection |> Option.toList)
        |> reduce

    abstract CharacteristicSection: section: CharacteristicSection -> 'Result

    default _.CharacteristicSection section =
        [
            reducer.Terminal section.IsKeyword
            reducer.Characteristic section.Characteristic
        ]
        |> reduce

    abstract CharacteristicGroup: group: CharacteristicGroup -> 'Result

    default _.CharacteristicGroup group =
        [
            reducer.Terminal group.OpenParen
            reducer.Characteristic group.Characteristic
            reducer.Terminal group.CloseParen
        ]
        |> reduce

    abstract Characteristic: characteristic: Characteristic -> 'Result

    default _.Characteristic characteristic =
        match characteristic with
        | Adjoint adjoint -> reducer.Terminal adjoint
        | Controlled controlled -> reducer.Terminal controlled
        | Group group -> reducer.CharacteristicGroup group
        | Characteristic.InfixOperator operator -> reducer.InfixOperator(reducer.Characteristic, operator)

    abstract CallableBody: body: CallableBody -> 'Result

    default _.CallableBody body =
        match body with
        | Statements statements -> reducer.Block(reducer.Statement, statements)
        | Specializations specializations -> reducer.Block(reducer.Specialization, specializations)

    abstract Specialization: specialization: Specialization -> 'Result

    default _.Specialization specialization =
        (specialization.Names |> List.map reducer.Terminal)
        @ [ reducer.SpecializationGenerator specialization.Generator ]
        |> reduce

    abstract SpecializationGenerator: generator: SpecializationGenerator -> 'Result

    default _.SpecializationGenerator generator =
        match generator with
        | BuiltIn (name, semicolon) -> [ reducer.Terminal name; reducer.Terminal semicolon ] |> reduce
        | Provided (parameters, statements) ->
            (parameters |> Option.map (curry reducer.Tuple reducer.Terminal) |> Option.toList)
            @ [ reducer.Block(reducer.Statement, statements) ]
            |> reduce

    abstract Statement: statement: Statement -> 'Result

    default _.Statement statement =
        match statement with
        | ExpressionStatement expr -> reducer.ExpressionStatement expr
        | ReturnStatement returns -> reducer.ReturnStatement returns
        | FailStatement fails -> reducer.FailStatement fails
        | LetStatement lets -> reducer.LetStatement lets
        | MutableStatement mutables -> reducer.MutableStatement mutables
        | SetStatement sets -> reducer.SetStatement sets
        | UpdateStatement updates -> reducer.UpdateStatement updates
        | UpdateWithStatement withs -> reducer.UpdateWithStatement withs
        | IfStatement ifs -> reducer.IfStatement ifs
        | ElifStatement elifs -> reducer.ElifStatement elifs
        | ElseStatement elses -> reducer.ElseStatement elses
        | ForStatement loop -> reducer.ForStatement loop
        | WhileStatement whiles -> reducer.WhileStatement whiles
        | RepeatStatement repeats -> reducer.RepeatStatement repeats
        | UntilStatement untils -> reducer.UntilStatement untils
        | WithinStatement withins -> reducer.WithinStatement withins
        | ApplyStatement apply -> reducer.ApplyStatement apply
        | QubitDeclarationStatement decl -> reducer.QubitDeclarationStatement decl
        | Statement.Unknown terminal -> reducer.Terminal terminal

    abstract ExpressionStatement: expr: ExpressionStatement -> 'Result

    default _.ExpressionStatement expr =
        [ reducer.Expression expr.Expression; reducer.Terminal expr.Semicolon ] |> reduce

    abstract ReturnStatement: returns: SimpleStatement -> 'Result

    default _.ReturnStatement returns = defaultSimpleStatement returns

    abstract FailStatement: fails: SimpleStatement -> 'Result

    default _.FailStatement fails = defaultSimpleStatement fails

    abstract LetStatement: lets: BindingStatement -> 'Result

    default _.LetStatement lets = defaultBindingStatement lets

    abstract MutableStatement: mutables: BindingStatement -> 'Result

    default _.MutableStatement mutables = defaultBindingStatement mutables

    abstract SetStatement: sets: BindingStatement -> 'Result

    default _.SetStatement sets = defaultBindingStatement sets

    abstract UpdateStatement: updates: UpdateStatement -> 'Result

    default _.UpdateStatement updates =
        [
            reducer.Terminal updates.SetKeyword
            reducer.Terminal updates.Name
            reducer.Terminal updates.Operator
            reducer.Expression updates.Value
            reducer.Terminal updates.Semicolon
        ]
        |> reduce

    abstract UpdateWithStatement: withs: UpdateWithStatement -> 'Result

    default _.UpdateWithStatement withs =
        [
            reducer.Terminal withs.SetKeyword
            reducer.Terminal withs.Name
            reducer.Terminal withs.With
            reducer.Expression withs.Item
            reducer.Terminal withs.Arrow
            reducer.Expression withs.Value
            reducer.Terminal withs.Semicolon
        ]
        |> reduce

    abstract IfStatement: ifs: ConditionalBlockStatement -> 'Result

    default _.IfStatement ifs = defaultConditionalBlockStatement ifs

    abstract ElifStatement: elifs: ConditionalBlockStatement -> 'Result

    default _.ElifStatement elifs = defaultConditionalBlockStatement elifs

    abstract ElseStatement: elses: BlockStatement -> 'Result

    default _.ElseStatement elses = defaultBlockStatement elses

    abstract ForStatement: loop: ForStatement -> 'Result

    default _.ForStatement loop =
        [
            reducer.Terminal loop.ForKeyword |> Some
            loop.OpenParen |> Option.map reducer.Terminal
            reducer.ForBinding(loop.Binding) |> Some
            loop.CloseParen |> Option.map reducer.Terminal
            reducer.Block(reducer.Statement, loop.Block) |> Some
        ]
        |> List.choose id
        |> reduce

    abstract WhileStatement: whiles: ConditionalBlockStatement -> 'Result

    default _.WhileStatement whiles = defaultConditionalBlockStatement whiles

    abstract RepeatStatement: repeats: BlockStatement -> 'Result

    default _.RepeatStatement repeats = defaultBlockStatement repeats

    abstract UntilStatement: untils: UntilStatement -> 'Result

    default _.UntilStatement untils =
        [
            reducer.Terminal untils.UntilKeyword
            reducer.Expression untils.Condition
            match untils.Coda with
            | UntilStatementCoda.Semicolon semicolon -> reducer.Terminal semicolon
            | Fixup fixup -> reducer.Fixup fixup
        ]
        |> reduce

    abstract Fixup: fixup: BlockStatement -> 'Result

    default _.Fixup fixup = defaultBlockStatement fixup

    abstract WithinStatement: withins: BlockStatement -> 'Result

    default _.WithinStatement withins = defaultBlockStatement withins

    abstract ApplyStatement: apply: BlockStatement -> 'Result

    default _.ApplyStatement apply = defaultBlockStatement apply

    abstract QubitDeclarationStatement: decl: QubitDeclarationStatement -> 'Result

    default _.QubitDeclarationStatement decl =
        [
            reducer.Terminal decl.Keyword |> Some
            decl.OpenParen |> Option.map reducer.Terminal
            reducer.QubitBinding decl.Binding |> Some
            decl.CloseParen |> Option.map reducer.Terminal
            match decl.Coda with
            | Semicolon semicolon -> reducer.Terminal semicolon |> Some
            | Block block -> reducer.Block(reducer.Statement, block) |> Some
        ]
        |> List.choose id
        |> reduce

    abstract ParameterBinding: binding: ParameterBinding -> 'Result

    default _.ParameterBinding binding =
        match binding with
        | ParameterDeclaration declaration -> reducer.ParameterDeclaration declaration
        | ParameterTuple tuple -> reducer.Tuple(reducer.ParameterBinding, tuple)

    abstract ParameterDeclaration: declaration: ParameterDeclaration -> 'Result

    default _.ParameterDeclaration declaration =
        [ reducer.Terminal declaration.Name; reducer.TypeAnnotation declaration.Type ] |> reduce

    abstract SymbolBinding: symbol: SymbolBinding -> 'Result

    default _.SymbolBinding symbol =
        match symbol with
        | SymbolDeclaration declaration -> reducer.Terminal declaration
        | SymbolTuple tuple -> reducer.Tuple(reducer.SymbolBinding, tuple)

    abstract QubitBinding: binding: QubitBinding -> 'Result

    default _.QubitBinding binding =
        [
            reducer.SymbolBinding binding.Name
            reducer.Terminal binding.Equals
            reducer.QubitInitializer binding.Initializer
        ]
        |> reduce

    abstract ForBinding: binding: ForBinding -> 'Result

    default _.ForBinding binding =
        [
            reducer.SymbolBinding binding.Name
            reducer.Terminal binding.In
            reducer.Expression binding.Value
        ]
        |> reduce

    abstract QubitInitializer: initializer: QubitInitializer -> 'Result

    default _.QubitInitializer initializer =
        match initializer with
        | SingleQubit singleQubit -> reducer.SingleQubit singleQubit
        | QubitArray qubitArray -> reducer.QubitArray qubitArray
        | QubitTuple tuple -> reducer.Tuple(reducer.QubitInitializer, tuple)

    abstract SingleQubit: newQubit: SingleQubit -> 'Result

    default _.SingleQubit newQubit =
        [
            reducer.Terminal newQubit.Qubit
            reducer.Terminal newQubit.OpenParen
            reducer.Terminal newQubit.CloseParen
        ]
        |> reduce

    abstract QubitArray: newQubits: QubitArray -> 'Result

    default _.QubitArray newQubits =
        [
            reducer.Terminal newQubits.Qubit
            reducer.Terminal newQubits.OpenBracket
            reducer.Expression newQubits.Length
            reducer.Terminal newQubits.CloseBracket
        ]
        |> reduce

    abstract InterpStringContent: interpStringContent: InterpStringContent -> 'Result

    default _.InterpStringContent interpStringContent =
        match interpStringContent with
        | Text text -> reducer.Terminal text
        | Expression interpStringExpression -> reducer.InterpStringExpression interpStringExpression

    abstract InterpStringExpression: interpStringExpression: InterpStringExpression -> 'Result

    default _.InterpStringExpression interpStringExpression =
        [
            reducer.Terminal interpStringExpression.OpenBrace
            reducer.Expression interpStringExpression.Expression
            reducer.Terminal interpStringExpression.CloseBrace
        ]
        |> reduce

    abstract Expression: expression: Expression -> 'Result

    default _.Expression expression =
        match expression with
        | Missing terminal -> reducer.Terminal terminal
        | Literal literal -> reducer.Terminal literal
        | Identifier identifier -> reducer.Identifier identifier
        | InterpString interpString -> reducer.InterpString interpString
        | Tuple tuple -> reducer.Tuple(reducer.Expression, tuple)
        | NewArray newArray -> reducer.NewArray newArray
        | NewSizedArray newSizedArray -> reducer.NewSizedArray newSizedArray
        | NamedItemAccess namedItemAccess -> reducer.NamedItemAccess namedItemAccess
        | ArrayAccess arrayAccess -> reducer.ArrayAccess arrayAccess
        | Call call -> reducer.Call call
        | PrefixOperator operator -> reducer.PrefixOperator(reducer.Expression, operator)
        | PostfixOperator operator -> reducer.PostfixOperator(reducer.Expression, operator)
        | InfixOperator operator -> reducer.InfixOperator(reducer.Expression, operator)
        | Conditional conditional -> reducer.Conditional conditional
        | FullOpenRange fullOpenRange -> reducer.Terminal fullOpenRange
        | Update update -> reducer.Update update
        | Lambda lambda -> reducer.Lambda lambda
        | Expression.Unknown terminal -> reducer.Terminal terminal

    abstract Identifier: identifier: Identifier -> 'Result

    default _.Identifier identifier =
        reducer.Terminal identifier.Name
        :: (identifier.TypeArgs |> Option.map (curry reducer.Tuple reducer.Type) |> Option.toList)
        |> reduce

    abstract InterpString: interpString: InterpString -> 'Result

    default _.InterpString interpString =
        reducer.Terminal interpString.OpenQuote
        :: (interpString.Content |> List.map reducer.InterpStringContent)
        @ [ reducer.Terminal interpString.CloseQuote ]
        |> reduce

    abstract NewArray: newArray: NewArray -> 'Result

    default _.NewArray newArray =
        [
            reducer.Terminal newArray.New
            reducer.Type newArray.ItemType
            reducer.Terminal newArray.OpenBracket
            reducer.Expression newArray.Length
            reducer.Terminal newArray.CloseBracket
        ]
        |> reduce

    abstract NewSizedArray: newSizedArray: NewSizedArray -> 'Result

    default _.NewSizedArray newSizedArray =
        [
            reducer.Terminal newSizedArray.OpenBracket
            reducer.Expression newSizedArray.Value
            reducer.Terminal newSizedArray.Comma
            reducer.Terminal newSizedArray.Size
            reducer.Terminal newSizedArray.Equals
            reducer.Expression newSizedArray.Length
            reducer.Terminal newSizedArray.CloseBracket
        ]
        |> reduce

    abstract NamedItemAccess: namedItemAccess: NamedItemAccess -> 'Result

    default _.NamedItemAccess namedItemAccess =
        [
            reducer.Expression namedItemAccess.Record
            reducer.Terminal namedItemAccess.DoubleColon
            reducer.Terminal namedItemAccess.Name
        ]
        |> reduce

    abstract ArrayAccess: arrayAccess: ArrayAccess -> 'Result

    default _.ArrayAccess arrayAccess =
        [
            reducer.Expression arrayAccess.Array
            reducer.Terminal arrayAccess.OpenBracket
            reducer.Expression arrayAccess.Index
            reducer.Terminal arrayAccess.CloseBracket
        ]
        |> reduce

    abstract Call: call: Call -> 'Result

    default _.Call call =
        [
            reducer.Expression call.Callable
            reducer.Tuple(reducer.Expression, call.Arguments)
        ]
        |> reduce

    abstract Conditional: conditional: Conditional -> 'Result

    default _.Conditional conditional =
        [
            reducer.Expression conditional.Condition
            reducer.Terminal conditional.Question
            reducer.Expression conditional.IfTrue
            reducer.Terminal conditional.Pipe
            reducer.Expression conditional.IfFalse
        ]
        |> reduce

    abstract Update: update: Update -> 'Result

    default _.Update update =
        [
            reducer.Expression update.Record
            reducer.Terminal update.With
            reducer.Expression update.Item
            reducer.Terminal update.Arrow
            reducer.Expression update.Value
        ]
        |> reduce

    abstract Lambda: lambda: Lambda -> 'Result

    default _.Lambda lambda =
        [
            reducer.SymbolBinding lambda.Binding
            reducer.Terminal lambda.Arrow
            reducer.Expression lambda.Body
        ]
        |> reduce

    abstract Block: mapper: ('a -> 'Result) * block: 'a Block -> 'Result

    default _.Block(mapper, block) =
        reducer.Terminal block.OpenBrace :: (block.Items |> List.map mapper)
        @ [ reducer.Terminal block.CloseBrace ]
        |> reduce

    abstract Tuple: mapper: ('a -> 'Result) * tuple: 'a Tuple -> 'Result

    default _.Tuple(mapper, tuple) =
        reducer.Terminal tuple.OpenParen :: (tuple.Items |> List.map (curry reducer.SequenceItem mapper))
        @ [ reducer.Terminal tuple.CloseParen ]
        |> reduce

    abstract SequenceItem: mapper: ('a -> 'Result) * item: 'a SequenceItem -> 'Result

    default _.SequenceItem(mapper, item) =
        (item.Item |> Option.map mapper |> Option.toList)
        @ (item.Comma |> Option.map reducer.Terminal |> Option.toList)
        |> reduce

    abstract PrefixOperator: mapper: ('a -> 'Result) * operator: 'a PrefixOperator -> 'Result

    default _.PrefixOperator(mapper, operator) =
        [ reducer.Terminal operator.PrefixOperator; mapper operator.Operand ] |> reduce

    abstract PostfixOperator: mapper: ('a -> 'Result) * operator: 'a PostfixOperator -> 'Result

    default _.PostfixOperator(mapper, operator) =
        [ mapper operator.Operand; reducer.Terminal operator.PostfixOperator ] |> reduce

    abstract InfixOperator: mapper: ('a -> 'Result) * operator: 'a InfixOperator -> 'Result

    default _.InfixOperator(mapper, operator) =
        [
            mapper operator.Left
            reducer.Terminal operator.InfixOperator
            mapper operator.Right
        ]
        |> reduce

    abstract Terminal: terminal: Terminal -> 'Result
