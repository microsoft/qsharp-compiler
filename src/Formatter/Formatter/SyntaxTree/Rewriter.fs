namespace QsFmt.Formatter.SyntaxTree

open QsFmt.Formatter.Utils

/// <summary>
/// Rewrites a syntax tree.
/// </summary>
/// <typeparam name="context">The type of the context to use during recursive descent into the syntax tree.</typeparam>
type internal 'context Rewriter() =
    /// <summary>
    /// Rewrites a <see cref="Document"/> node.
    /// </summary>
    abstract Document: 'context * Document -> Document

    /// <summary>
    /// Rewrites a <see cref="Namespace"/> node.
    /// </summary>
    abstract Namespace: 'context * Namespace -> Namespace

    /// <summary>
    /// Rewrites a <see cref="NamespaceItem"/> node.
    /// </summary>
    abstract NamespaceItem: 'context * NamespaceItem -> NamespaceItem

    /// <summary>
    /// Rewrites a <see cref="CallableDeclaration"/> node.
    /// </summary>
    abstract CallableDeclaration: 'context * CallableDeclaration -> CallableDeclaration

    /// <summary>
    /// Rewrites a <see cref="Type"/> node.
    /// </summary>
    abstract Type: 'context * Type -> Type

    /// <summary>
    /// Rewrites a <see cref="TypeAnnotation"/> node.
    /// </summary>
    abstract TypeAnnotation: 'context * TypeAnnotation -> TypeAnnotation

    /// <summary>
    /// Rewrites an <see cref="ArrayType"/> node.
    /// </summary>
    abstract ArrayType: 'context * ArrayType -> ArrayType

    /// <summary>
    /// Rewrites a <see cref="CallableType"/> node.
    /// </summary>
    abstract CallableType: 'context * CallableType -> CallableType

    /// <summary>
    /// Rewrites a <see cref="CharacteristicSection"/> node.
    /// </summary>
    abstract CharacteristicSection: 'context * CharacteristicSection -> CharacteristicSection

    /// <summary>
    /// Rewrites a <see cref="CharacteristicGroup"/> node.
    /// </summary>
    abstract CharacteristicGroup: 'context * CharacteristicGroup -> CharacteristicGroup

    /// <summary>
    /// Rewrites a <see cref="Characteristic"/> node.
    /// </summary>
    abstract Characteristic: 'context * Characteristic -> Characteristic

    /// <summary>
    /// Rewrites a <see cref="Statement"/> node.
    /// </summary>
    abstract Statement: 'context * Statement -> Statement

    /// <summary>
    /// Rewrites a <see cref="Let"/> statement node.
    /// </summary>
    abstract Let: 'context * Let -> Let

    /// <summary>
    /// Rewrites a <see cref="Return"/> statement node.
    /// </summary>
    abstract Return: 'context * Return -> Return

    /// <summary>
    /// Rewrites an <see cref="If"/> statement node.
    /// </summary>
    abstract If: 'context * If -> If

    /// <summary>
    /// Rewrites an <see cref="Else"/> statement node.
    /// </summary>
    abstract Else: 'context * Else -> Else

    /// <summary>
    /// Rewrites a <see cref="SymbolBinding"/> node.
    /// </summary>
    abstract SymbolBinding: 'context * SymbolBinding -> SymbolBinding

    /// <summary>
    /// Rewrites a <see cref="SymbolDeclaration"/> node.
    /// </summary>
    abstract SymbolDeclaration: 'context * SymbolDeclaration -> SymbolDeclaration

    /// <summary>
    /// Rewrites an <see cref="Expression"/> node.
    /// </summary>
    abstract Expression: 'context * Expression -> Expression

    /// <summary>
    /// Rewrites an <see cref="Update"/> expression node.
    /// </summary>
    abstract Update: 'context * Update -> Update

    /// <summary>
    /// Rewrites a <see cref="Block{a}"/> node, given a rewriter for the block contents.
    /// </summary>
    abstract Block: 'context * ('context * 'a -> 'a) * 'a Block -> 'a Block

    /// <summary>
    /// Rewrites a <see cref="Tuple{a}"/> node, given a rewriter for the tuple contents.
    /// </summary>
    abstract Tuple: 'context * ('context * 'a -> 'a) * 'a Tuple -> 'a Tuple

    /// <summary>
    /// Rewrites a <see cref="SequenceItem{a}"/> node, given a rewriter for the sequence items.
    /// </summary>
    abstract SequenceItem: 'context * ('context * 'a -> 'a) * 'a SequenceItem -> 'a SequenceItem

    /// <summary>
    /// Rewrites a <see cref="BinaryOperator{a}"/> node, given a rewriter for the operands.
    /// </summary>
    abstract BinaryOperator: 'context * ('context * 'a -> 'a) * 'a BinaryOperator -> 'a BinaryOperator

    /// <summary>
    /// Rewrites a <see cref="Terminal"/> node.
    /// </summary>
    abstract Terminal: 'context * Terminal -> Terminal

    default rewriter.Document(context, document) =
        { Namespaces =
              document.Namespaces
              |> List.map (curry rewriter.Namespace context)
          Eof = rewriter.Terminal(context, document.Eof) }

    default rewriter.Namespace(context, ns) =
        { NamespaceKeyword = rewriter.Terminal(context, ns.NamespaceKeyword)
          Name = rewriter.Terminal(context, ns.Name)
          Block = rewriter.Block(context, rewriter.NamespaceItem, ns.Block) }

    default rewriter.NamespaceItem(context, item) =
        match item with
        | CallableDeclaration callable ->
            rewriter.CallableDeclaration(context, callable)
            |> CallableDeclaration
        | Unknown terminal -> rewriter.Terminal(context, terminal) |> Unknown

    default rewriter.CallableDeclaration(context, callable) =
        { CallableKeyword = rewriter.Terminal(context, callable.CallableKeyword)
          Name = rewriter.Terminal(context, callable.Name)
          Parameters = rewriter.SymbolBinding(context, callable.Parameters)
          ReturnType = rewriter.TypeAnnotation(context, callable.ReturnType)
          Block = rewriter.Block(context, rewriter.Statement, callable.Block) }

    default rewriter.Type(context, typ) =
        match typ with
        | Type.Missing missing ->
            rewriter.Terminal(context, missing)
            |> Type.Missing
        | Parameter name -> rewriter.Terminal(context, name) |> Parameter
        | BuiltIn name -> rewriter.Terminal(context, name) |> BuiltIn
        | UserDefined name -> rewriter.Terminal(context, name) |> UserDefined
        | Type.Tuple tuple ->
            rewriter.Tuple(context, rewriter.Type, tuple)
            |> Type.Tuple
        | Array array -> rewriter.ArrayType(context, array) |> Array
        | Type.Callable callable ->
            rewriter.CallableType(context, callable)
            |> Type.Callable
        | Type.Unknown terminal ->
            rewriter.Terminal(context, terminal)
            |> Type.Unknown

    default rewriter.TypeAnnotation(context, annotation) =
        { Colon = rewriter.Terminal(context, annotation.Colon)
          Type = rewriter.Type(context, annotation.Type) }

    default rewriter.ArrayType(context, array) =
        { ItemType = rewriter.Type(context, array.ItemType)
          OpenBracket = rewriter.Terminal(context, array.OpenBracket)
          CloseBracket = rewriter.Terminal(context, array.CloseBracket) }

    default rewriter.CallableType(context, callable) =
        { FromType = rewriter.Type(context, callable.FromType)
          Arrow = rewriter.Terminal(context, callable.Arrow)
          ToType = rewriter.Type(context, callable.ToType)
          Characteristics =
              callable.Characteristics
              |> Option.map (curry rewriter.CharacteristicSection context) }

    default rewriter.CharacteristicSection(context, section) =
        { IsKeyword = rewriter.Terminal(context, section.IsKeyword)
          Characteristic = rewriter.Characteristic(context, section.Characteristic) }

    default rewriter.CharacteristicGroup(context, group) =
        { OpenParen = rewriter.Terminal(context, group.OpenParen)
          Characteristic = rewriter.Characteristic(context, group.Characteristic)
          CloseParen = rewriter.Terminal(context, group.CloseParen) }

    default rewriter.Characteristic(context, characteristic) =
        match characteristic with
        | Adjoint adjoint -> rewriter.Terminal(context, adjoint) |> Adjoint
        | Controlled controlled ->
            rewriter.Terminal(context, controlled)
            |> Controlled
        | Group group ->
            rewriter.CharacteristicGroup(context, group)
            |> Group
        | Characteristic.BinaryOperator operator ->
            rewriter.BinaryOperator(context, rewriter.Characteristic, operator)
            |> Characteristic.BinaryOperator

    default rewriter.Statement(context, statement) =
        match statement with
        | Let lets -> rewriter.Let(context, lets) |> Let
        | Return returns -> rewriter.Return(context, returns) |> Return
        | If ifs -> rewriter.If(context, ifs) |> If
        | Else elses -> rewriter.Else(context, elses) |> Else
        | Statement.Unknown terminal ->
            rewriter.Terminal(context, terminal)
            |> Statement.Unknown

    default rewriter.Let(context, lets) =
        { LetKeyword = rewriter.Terminal(context, lets.LetKeyword)
          Binding = rewriter.SymbolBinding(context, lets.Binding)
          Equals = rewriter.Terminal(context, lets.Equals)
          Value = rewriter.Expression(context, lets.Value)
          Semicolon = rewriter.Terminal(context, lets.Semicolon) }

    default rewriter.Return(context, returns) =
        { ReturnKeyword = rewriter.Terminal(context, returns.ReturnKeyword)
          Expression = rewriter.Expression(context, returns.Expression)
          Semicolon = rewriter.Terminal(context, returns.Semicolon) }

    default rewriter.If(context, ifs) =
        { IfKeyword = rewriter.Terminal(context, ifs.IfKeyword)
          Condition = rewriter.Expression(context, ifs.Condition)
          Block = rewriter.Block(context, rewriter.Statement, ifs.Block) }

    default rewriter.Else(context, elses) =
        { ElseKeyword = rewriter.Terminal(context, elses.ElseKeyword)
          Block = rewriter.Block(context, rewriter.Statement, elses.Block) }

    default rewriter.SymbolBinding(context, binding) =
        match binding with
        | SymbolDeclaration declaration ->
            rewriter.SymbolDeclaration(context, declaration)
            |> SymbolDeclaration
        | SymbolTuple tuple ->
            rewriter.Tuple(context, rewriter.SymbolBinding, tuple)
            |> SymbolTuple

    default rewriter.SymbolDeclaration(context, declaration) =
        { Name = rewriter.Terminal(context, declaration.Name)
          Type =
              declaration.Type
              |> Option.map (curry rewriter.TypeAnnotation context) }

    default rewriter.Expression(context, expression) =
        match expression with
        | Missing terminal -> rewriter.Terminal(context, terminal) |> Missing
        | Literal literal -> rewriter.Terminal(context, literal) |> Literal
        | Tuple tuple ->
            rewriter.Tuple(context, rewriter.Expression, tuple)
            |> Tuple
        | BinaryOperator operator ->
            rewriter.BinaryOperator(context, rewriter.Expression, operator)
            |> BinaryOperator
        | Update update -> rewriter.Update(context, update) |> Update
        | Expression.Unknown terminal ->
            rewriter.Terminal(context, terminal)
            |> Expression.Unknown

    default rewriter.Update(context, update) =
        { Record = rewriter.Expression(context, update.Record)
          With = rewriter.Terminal(context, update.With)
          Item = rewriter.Expression(context, update.Item)
          Arrow = rewriter.Terminal(context, update.Arrow)
          Value = rewriter.Expression(context, update.Value) }

    default rewriter.Block(context, mapper, block) =
        { OpenBrace = rewriter.Terminal(context, block.OpenBrace)
          Items = block.Items |> List.map (curry mapper context)
          CloseBrace = rewriter.Terminal(context, block.CloseBrace) }

    default rewriter.Tuple(context, mapper, tuple) =
        { OpenParen = rewriter.Terminal(context, tuple.OpenParen)
          Items =
              tuple.Items
              |> List.map (fun item -> rewriter.SequenceItem(context, mapper, item))
          CloseParen = rewriter.Terminal(context, tuple.CloseParen) }

    default rewriter.SequenceItem(context, mapper, item) =
        { Item = item.Item |> Option.map (curry mapper context)
          Comma =
              item.Comma
              |> Option.map (curry rewriter.Terminal context) }

    default rewriter.BinaryOperator(context, mapper, operator) =
        { Left = mapper (context, operator.Left)
          Operator = rewriter.Terminal(context, operator.Operator)
          Right = mapper (context, operator.Right) }

    default _.Terminal(_, terminal) = terminal
