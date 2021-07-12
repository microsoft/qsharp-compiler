// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

open Microsoft.Quantum.QsFmt.Formatter.Utils

[<AbstractClass>]
type internal 'result Reducer() as reducer =
    /// Reduces a list of results into a single result.
    let reduce = curry reducer.Combine |> List.reduce

    abstract Combine : 'result * 'result -> 'result

    abstract Document : document: Document -> 'result

    default _.Document document =
        (document.Namespaces |> List.map reducer.Namespace) @ [ reducer.Terminal document.Eof ] |> reduce

    abstract Namespace : ns: Namespace -> 'result

    default _.Namespace ns =
        [
            reducer.Terminal ns.NamespaceKeyword
            reducer.Terminal ns.Name
            reducer.Block(reducer.NamespaceItem, ns.Block)
        ]
        |> reduce

    abstract NamespaceItem : item: NamespaceItem -> 'result

    default _.NamespaceItem item =
        match item with
        | CallableDeclaration callable -> reducer.CallableDeclaration callable
        | Unknown terminal -> reducer.Terminal terminal

    abstract Attribute : attribute: Attribute -> 'result

    default _.Attribute attribute =
        [ reducer.Terminal attribute.At; reducer.Expression attribute.Expression ] |> reduce

    abstract CallableDeclaration : callable: CallableDeclaration -> 'result

    default _.CallableDeclaration callable =
        [
            callable.Attributes |> List.map reducer.Attribute
            callable.Access |> Option.map reducer.Terminal |> Option.toList
            [ reducer.Terminal callable.CallableKeyword; reducer.Terminal callable.Name ]
            callable.TypeParameters |> Option.map reducer.TypeParameterBinding |> Option.toList
            [
                reducer.SymbolBinding callable.Parameters
                reducer.TypeAnnotation callable.ReturnType
            ]
            callable.CharacteristicSection |> Option.map reducer.CharacteristicSection |> Option.toList
            [ reducer.CallableBody callable.Body ]
        ]
        |> List.concat
        |> reduce

    abstract TypeParameterBinding : binding: TypeParameterBinding -> 'result

    default _.TypeParameterBinding binding =
        reducer.Terminal binding.OpenBracket
        :: (binding.Parameters |> List.map (curry reducer.SequenceItem reducer.Terminal))
        @ [ reducer.Terminal binding.CloseBracket ]
        |> reduce

    abstract Type : typ: Type -> 'result

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

    abstract TypeAnnotation : annotation: TypeAnnotation -> 'result

    default _.TypeAnnotation annotation =
        [ reducer.Terminal annotation.Colon; reducer.Type annotation.Type ] |> reduce

    abstract ArrayType : array: ArrayType -> 'result

    default _.ArrayType array =
        [
            reducer.Type array.ItemType
            reducer.Terminal array.OpenBracket
            reducer.Terminal array.CloseBracket
        ]
        |> reduce

    abstract CallableType : callable: CallableType -> 'result

    default _.CallableType callable =
        [
            reducer.Type callable.FromType
            reducer.Terminal callable.Arrow
            reducer.Type callable.ToType
        ]
        @ (callable.Characteristics |> Option.map reducer.CharacteristicSection |> Option.toList)
        |> reduce

    abstract CharacteristicSection : section: CharacteristicSection -> 'result

    default _.CharacteristicSection section =
        [
            reducer.Terminal section.IsKeyword
            reducer.Characteristic section.Characteristic
        ]
        |> reduce

    abstract CharacteristicGroup : group: CharacteristicGroup -> 'result

    default _.CharacteristicGroup group =
        [
            reducer.Terminal group.OpenParen
            reducer.Characteristic group.Characteristic
            reducer.Terminal group.CloseParen
        ]
        |> reduce

    abstract Characteristic : characteristic: Characteristic -> 'result

    default _.Characteristic characteristic =
        match characteristic with
        | Adjoint adjoint -> reducer.Terminal adjoint
        | Controlled controlled -> reducer.Terminal controlled
        | Group group -> reducer.CharacteristicGroup group
        | Characteristic.BinaryOperator operator -> reducer.BinaryOperator(reducer.Characteristic, operator)

    abstract CallableBody : body: CallableBody -> 'result

    default _.CallableBody body =
        match body with
        | Statements statements -> reducer.Block(reducer.Statement, statements)
        | Specializations specializations -> reducer.Block(reducer.Specialization, specializations)

    abstract Specialization : specialization: Specialization -> 'result

    default _.Specialization specialization =
        (specialization.Names |> List.map reducer.Terminal)
        @ [ reducer.SpecializationGenerator specialization.Generator ]
        |> reduce

    abstract SpecializationGenerator : generator: SpecializationGenerator -> 'result

    default _.SpecializationGenerator generator =
        match generator with
        | BuiltIn (name, semicolon) -> [ reducer.Terminal name; reducer.Terminal semicolon ] |> reduce
        | Provided (parameters, statements) ->
            (parameters |> Option.map reducer.Terminal |> Option.toList)
            @ [ reducer.Block(reducer.Statement, statements) ]
            |> reduce

    abstract Statement : statement: Statement -> 'result

    default _.Statement statement =
        match statement with
        | Let lets -> reducer.Let lets
        | Return returns -> reducer.Return returns
        | If ifs -> reducer.If ifs
        | Else elses -> reducer.Else elses
        | Statement.Unknown terminal -> reducer.Terminal terminal

    abstract Let : lets: Let -> 'result

    default _.Let lets =
        [
            reducer.Terminal lets.LetKeyword
            reducer.SymbolBinding lets.Binding
            reducer.Terminal lets.Equals
            reducer.Expression lets.Value
            reducer.Terminal lets.Semicolon
        ]
        |> reduce

    abstract Return : returns: Return -> 'result

    default _.Return returns =
        [
            reducer.Terminal returns.ReturnKeyword
            reducer.Expression returns.Expression
            reducer.Terminal returns.Semicolon
        ]
        |> reduce

    abstract If : ifs: If -> 'result

    default _.If ifs =
        [
            reducer.Terminal ifs.IfKeyword
            reducer.Expression ifs.Condition
            reducer.Block(reducer.Statement, ifs.Block)
        ]
        |> reduce

    abstract Else : elses: Else -> 'result

    default _.Else elses =
        [
            reducer.Terminal elses.ElseKeyword
            reducer.Block(reducer.Statement, elses.Block)
        ]
        |> reduce

    abstract SymbolBinding : binding: SymbolBinding -> 'result

    default _.SymbolBinding binding =
        match binding with
        | SymbolDeclaration declaration -> reducer.SymbolDeclaration declaration
        | SymbolTuple tuple -> reducer.Tuple(reducer.SymbolBinding, tuple)

    abstract SymbolDeclaration : declaration: SymbolDeclaration -> 'result

    default _.SymbolDeclaration declaration =
        reducer.Terminal declaration.Name
        :: (declaration.Type |> Option.map reducer.TypeAnnotation |> Option.toList)
        |> reduce

    abstract Expression : expression: Expression -> 'result

    default _.Expression expression =
        match expression with
        | Missing terminal -> reducer.Terminal terminal
        | Literal literal -> reducer.Terminal literal
        | Tuple tuple -> reducer.Tuple(reducer.Expression, tuple)
        | BinaryOperator operator -> reducer.BinaryOperator(reducer.Expression, operator)
        | Update update -> reducer.Update update
        | Expression.Unknown terminal -> reducer.Terminal terminal

    abstract Update : update: Update -> 'result

    default _.Update update =
        [
            reducer.Expression update.Record
            reducer.Terminal update.With
            reducer.Expression update.Item
            reducer.Terminal update.Arrow
            reducer.Expression update.Value
        ]
        |> reduce

    abstract Block : mapper: ('a -> 'result) * block: 'a Block -> 'result

    default _.Block(mapper, block) =
        reducer.Terminal block.OpenBrace :: (block.Items |> List.map mapper)
        @ [ reducer.Terminal block.CloseBrace ]
        |> reduce

    abstract Tuple : mapper: ('a -> 'result) * tuple: 'a Tuple -> 'result

    default _.Tuple(mapper, tuple) =
        reducer.Terminal tuple.OpenParen :: (tuple.Items |> List.map (curry reducer.SequenceItem mapper))
        @ [ reducer.Terminal tuple.CloseParen ]
        |> reduce

    abstract SequenceItem : mapper: ('a -> 'result) * item: 'a SequenceItem -> 'result

    default _.SequenceItem(mapper, item) =
        (item.Item |> Option.map mapper |> Option.toList)
        @ (item.Comma |> Option.map reducer.Terminal |> Option.toList)
        |> reduce

    abstract BinaryOperator : mapper: ('a -> 'result) * operator: 'a BinaryOperator -> 'result

    default _.BinaryOperator(mapper, operator) =
        [
            mapper operator.Left
            reducer.Terminal operator.Operator
            mapper operator.Right
        ]
        |> reduce

    abstract Terminal : terminal: Terminal -> 'result
