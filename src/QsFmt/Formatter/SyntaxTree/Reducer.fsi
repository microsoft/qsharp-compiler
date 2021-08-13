// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

/// <summary>
/// Reduces a syntax tree to a single value.
/// </summary>
/// <typeparam name="result">The type of the reduced result.</typeparam>
[<AbstractClass>]
type internal 'result Reducer =
    /// <summary>
    /// Creates a new syntax tree <see cref="Reducer{result}"/>.
    /// </summary>
    new: unit -> 'result Reducer

    /// Combines two results into a single result.
    abstract Combine: 'result * 'result -> 'result

    /// <summary>
    /// Reduces a <see cref="Document"/> node.
    /// </summary>
    abstract Document: document:Document -> 'result
    default Document: document:Document -> 'result

    /// <summary>
    /// Reduces a <see cref="Namespace"/> node.
    /// </summary>
    abstract Namespace: ns:Namespace -> 'result
    default Namespace: ns:Namespace -> 'result

    /// <summary>
    /// Reduces a <see cref="NamespaceItem"/> node.
    /// </summary>
    abstract NamespaceItem: item:NamespaceItem -> 'result
    default NamespaceItem: item:NamespaceItem -> 'result

    /// <summary>
    /// Reduces an <see cref="Attribute"/> node.
    /// </summary>
    abstract Attribute: attribute:Attribute -> 'result
    default Attribute: attribute:Attribute -> 'result

    /// <summary>
    /// Reduces a <see cref="CallableDeclaration"/> node.
    /// </summary>
    abstract CallableDeclaration: callable:CallableDeclaration -> 'result
    default CallableDeclaration: callable:CallableDeclaration -> 'result

    /// <summary>
    /// Reduces a <see cref="TypeParameterBinding"/> node.
    /// </summary>
    abstract TypeParameterBinding: binding:TypeParameterBinding -> 'result
    default TypeParameterBinding: binding:TypeParameterBinding -> 'result

    /// <summary>
    /// Reduces a <see cref="Type"/> node.
    /// </summary>
    abstract Type: typ:Type -> 'result
    default Type: typ:Type -> 'result

    /// <summary>
    /// Reduces a <see cref="TypeAnnotation"/> node.
    /// </summary>
    abstract TypeAnnotation: annotation:TypeAnnotation -> 'result
    default TypeAnnotation: annotation:TypeAnnotation -> 'result

    /// <summary>
    /// Reduces an <see cref="ArrayType"/> node.
    /// </summary>
    abstract ArrayType: array:ArrayType -> 'result
    default ArrayType: array:ArrayType -> 'result

    /// <summary>
    /// Reduces a <see cref="CallableType"/> node.
    /// </summary>
    abstract CallableType: callable:CallableType -> 'result
    default CallableType: callable:CallableType -> 'result

    /// <summary>
    /// Reduces a <see cref="CharacteristicSection"/> node.
    /// </summary>
    abstract CharacteristicSection: section:CharacteristicSection -> 'result
    default CharacteristicSection: section:CharacteristicSection -> 'result

    /// <summary>
    /// Reduces a <see cref="CharacteristicGroup"/> node.
    /// </summary>
    abstract CharacteristicGroup: group:CharacteristicGroup -> 'result
    default CharacteristicGroup: group:CharacteristicGroup -> 'result

    /// <summary>
    /// Reduces a <see cref="Characteristic"/> node.
    /// </summary>
    abstract Characteristic: characteristic:Characteristic -> 'result
    default Characteristic: characteristic:Characteristic -> 'result

    /// <summary>
    /// Reduces a <see cref="CallableBody"/> node.
    /// </summary>
    abstract CallableBody: body:CallableBody -> 'result
    default CallableBody: body:CallableBody -> 'result

    /// <summary>
    /// Reduces a <see cref="Specialization"/> node.
    /// </summary>
    abstract Specialization: specialization:Specialization -> 'result
    default Specialization: specialization:Specialization -> 'result

    /// <summary>
    /// Reduces a <see cref="SpecializationGenerator"/> node.
    /// </summary>
    abstract SpecializationGenerator: generator:SpecializationGenerator -> 'result
    default SpecializationGenerator: generator:SpecializationGenerator -> 'result

    /// <summary>
    /// Reduces a <see cref="Statement"/> node.
    /// </summary>
    abstract Statement: statement:Statement -> 'result
    default Statement: statement:Statement -> 'result

    /// <summary>
    /// Reduces a <see cref="Let"/> statement node.
    /// </summary>
    abstract Let: lets:Let -> 'result
    default Let: lets:Let -> 'result

    /// <summary>
    /// Reduces a <see cref="Return"/> statement node.
    /// </summary>
    abstract Return: returns:Return -> 'result
    default Return: returns:Return -> 'result

    /// <summary>
    /// Reduces an <see cref="If"/> statement node.
    /// </summary>
    abstract If: ifs:If -> 'result
    default If: ifs:If -> 'result

    /// <summary>
    /// Reduces an <see cref="Else"/> statement node.
    /// </summary>
    abstract Else: elses:Else -> 'result
    default Else: elses:Else -> 'result

    /// <summary>
    /// Reduces a <see cref="SymbolBinding"/> node.
    /// </summary>
    abstract SymbolBinding: binding:SymbolBinding -> 'result
    default SymbolBinding: binding:SymbolBinding -> 'result

    /// <summary>
    /// Reduces a <see cref="SymbolDeclaration"/> node.
    /// </summary>
    abstract SymbolDeclaration: declaration:SymbolDeclaration -> 'result
    default SymbolDeclaration: declaration:SymbolDeclaration -> 'result

    /// <summary>
    /// Reduces an <see cref="InterpStringContent"/> node.
    /// </summary>
    abstract InterpStringContent: interpStringContent:InterpStringContent -> 'result
    default InterpStringContent: interpStringContent:InterpStringContent -> 'result

    /// <summary>
    /// Reduces an <see cref="InterpStringExpression"/> node.
    /// </summary>
    abstract InterpStringExpression: interpStringExpression:InterpStringExpression -> 'result
    default InterpStringExpression: interpStringExpression:InterpStringExpression -> 'result

    /// <summary>
    /// Reduces an <see cref="Expression"/> node.
    /// </summary>
    abstract Expression: expression:Expression -> 'result
    default Expression: expression:Expression -> 'result

    /// <summary>
    /// Reduces an <see cref="Identifier"/> expression node.
    /// </summary>
    abstract Identifier: identifier:Identifier -> 'result
    default Identifier: identifier:Identifier -> 'result

    /// <summary>
    /// Reduces an <see cref="InterpString"/> expression node.
    /// </summary>
    abstract InterpString: interpString:InterpString -> 'result
    default InterpString: interpString:InterpString -> 'result

    /// <summary>
    /// Reduces a <see cref="NewArray"/> expression node.
    /// </summary>
    abstract NewArray: newArray:NewArray -> 'result
    default NewArray: newArray:NewArray -> 'result

    /// <summary>
    /// Reduces a <see cref="NamedItemAccess"/> expression node.
    /// </summary>
    abstract NamedItemAccess: namedItemAccess:NamedItemAccess -> 'result
    default NamedItemAccess: namedItemAccess:NamedItemAccess -> 'result

    /// <summary>
    /// Reduces an <see cref="ArrayAccess"/> expression node.
    /// </summary>
    abstract ArrayAccess: arrayAccess:ArrayAccess -> 'result
    default ArrayAccess: arrayAccess:ArrayAccess -> 'result

    /// <summary>
    /// Reduces a <see cref="Call"/> expression node.
    /// </summary>
    abstract Call: call:Call -> 'result
    default Call: call:Call -> 'result

    /// <summary>
    /// Reduces a <see cref="Conditional"/> expression node.
    /// </summary>
    abstract Conditional: conditional:Conditional -> 'result
    default Conditional: conditional:Conditional -> 'result

    /// <summary>
    /// Reduces an <see cref="Update"/> expression node.
    /// </summary>
    abstract Update: update:Update -> 'result
    default Update: update:Update -> 'result

    /// <summary>
    /// Reduces a <see cref="Block{a}"/> node, given a reducer for the block contents.
    /// </summary>
    abstract Block: mapper:('a -> 'result) * block:'a Block -> 'result
    default Block: mapper:('a -> 'result) * block:'a Block -> 'result

    /// <summary>
    /// Reduces a <see cref="Tuple{a}"/> node, given a reducer for the tuple contents.
    /// </summary>
    abstract Tuple: mapper:('a -> 'result) * tuple:'a Tuple -> 'result
    default Tuple: mapper:('a -> 'result) * tuple:'a Tuple -> 'result

    /// <summary>
    /// Reduces a <see cref="SequenceItem{a}"/> node, given a reducer for the sequence items.
    /// </summary>
    abstract SequenceItem: mapper:('a -> 'result) * item:'a SequenceItem -> 'result
    default SequenceItem: mapper:('a -> 'result) * item:'a SequenceItem -> 'result

    /// <summary>
    /// Reduces a <see cref="PrefixOperator{a}"/> node, given a reducer for the operand.
    /// </summary>
    abstract PrefixOperator: mapper:('a -> 'result) * operator:'a PrefixOperator -> 'result
    default PrefixOperator: mapper:('a -> 'result) * operator:'a PrefixOperator -> 'result

    /// <summary>
    /// Reduces a <see cref="PostfixOperator{a}"/> node, given a reducer for the operand.
    /// </summary>
    abstract PostfixOperator: mapper:('a -> 'result) * operator:'a PostfixOperator -> 'result
    default PostfixOperator: mapper:('a -> 'result) * operator:'a PostfixOperator -> 'result

    /// <summary>
    /// Reduces an <see cref="InfixOperator{a}"/> node, given a reducer for the operands.
    /// </summary>
    abstract InfixOperator: mapper:('a -> 'result) * operator:'a InfixOperator -> 'result
    default InfixOperator: mapper:('a -> 'result) * operator:'a InfixOperator -> 'result

    /// <summary>
    /// Reduces a <see cref="Terminal"/> node.
    /// </summary>
    abstract Terminal: terminal:Terminal -> 'result
