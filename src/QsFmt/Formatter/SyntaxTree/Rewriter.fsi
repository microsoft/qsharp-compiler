// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

/// <summary>
/// Rewrites a syntax tree.
/// </summary>
/// <typeparam name="context">The type of the context to use during recursive descent into the syntax tree.</typeparam>
type internal 'context Rewriter =
    /// <summary>
    /// Creates a new syntax tree <see cref="Rewriter{context}"/>.
    /// </summary>
    new: unit -> 'context Rewriter

    /// <summary>
    /// Rewrites a <see cref="Document"/> node.
    /// </summary>
    abstract Document: context:'context * document:Document -> Document
    default Document: context:'context * document:Document -> Document

    /// <summary>
    /// Rewrites a <see cref="Namespace"/> node.
    /// </summary>
    abstract Namespace: context:'context * ns:Namespace -> Namespace
    default Namespace: context:'context * ns:Namespace -> Namespace

    /// <summary>
    /// Rewrites a <see cref="NamespaceItem"/> node.
    /// </summary>
    abstract NamespaceItem: context:'context * item:NamespaceItem -> NamespaceItem
    default NamespaceItem: context:'context * item:NamespaceItem -> NamespaceItem

    /// <summary>
    /// Rewrites a <see cref="CallableDeclaration"/> node.
    /// </summary>
    abstract CallableDeclaration: context:'context * callable:CallableDeclaration -> CallableDeclaration
    default CallableDeclaration: context:'context * callable:CallableDeclaration -> CallableDeclaration

    /// <summary>
    /// Rewrites a <see cref="Type"/> node.
    /// </summary>
    abstract Type: context:'context * typ:Type -> Type
    default Type: context:'context * typ:Type -> Type

    /// <summary>
    /// Rewrites a <see cref="TypeAnnotation"/> node.
    /// </summary>
    abstract TypeAnnotation: context:'context * annotation:TypeAnnotation -> TypeAnnotation
    default TypeAnnotation: context:'context * annotation:TypeAnnotation -> TypeAnnotation

    /// <summary>
    /// Rewrites an <see cref="ArrayType"/> node.
    /// </summary>
    abstract ArrayType: context:'context * array:ArrayType -> ArrayType
    default ArrayType: context:'context * array:ArrayType -> ArrayType

    /// <summary>
    /// Rewrites a <see cref="CallableType"/> node.
    /// </summary>
    abstract CallableType: context:'context * callable:CallableType -> CallableType
    default CallableType: context:'context * callable:CallableType -> CallableType

    /// <summary>
    /// Rewrites a <see cref="CharacteristicSection"/> node.
    /// </summary>
    abstract CharacteristicSection: context:'context * section:CharacteristicSection -> CharacteristicSection
    default CharacteristicSection: context:'context * section:CharacteristicSection -> CharacteristicSection

    /// <summary>
    /// Rewrites a <see cref="CharacteristicGroup"/> node.
    /// </summary>
    abstract CharacteristicGroup: context:'context * group:CharacteristicGroup -> CharacteristicGroup
    default CharacteristicGroup: context:'context * group:CharacteristicGroup -> CharacteristicGroup

    /// <summary>
    /// Rewrites a <see cref="Characteristic"/> node.
    /// </summary>
    abstract Characteristic: context:'context * characteristic:Characteristic -> Characteristic
    default Characteristic: context:'context * characteristic:Characteristic -> Characteristic

    /// <summary>
    /// Rewrites a <see cref="Statement"/> node.
    /// </summary>
    abstract Statement: context:'context * statement:Statement -> Statement
    default Statement: context:'context * statement:Statement -> Statement

    /// <summary>
    /// Rewrites a <see cref="Let"/> statement node.
    /// </summary>
    abstract Let: context:'context * lets:Let -> Let
    default Let: context:'context * lets:Let -> Let

    /// <summary>
    /// Rewrites a <see cref="Return"/> statement node.
    /// </summary>
    abstract Return: context:'context * returns:Return -> Return
    default Return: context:'context * returns:Return -> Return

    /// <summary>
    /// Rewrites an <see cref="If"/> statement node.
    /// </summary>
    abstract If: context:'context * ifs:If -> If
    default If: context:'context * ifs:If -> If

    /// <summary>
    /// Rewrites an <see cref="Else"/> statement node.
    /// </summary>
    abstract Else: context:'context * elses:Else -> Else
    default Else: context:'context * elses:Else -> Else

    /// <summary>
    /// Rewrites a <see cref="SymbolBinding"/> node.
    /// </summary>
    abstract SymbolBinding: context:'context * binding:SymbolBinding -> SymbolBinding
    default SymbolBinding: context:'context * binding:SymbolBinding -> SymbolBinding

    /// <summary>
    /// Rewrites a <see cref="SymbolDeclaration"/> node.
    /// </summary>
    abstract SymbolDeclaration: context:'context * declaration:SymbolDeclaration -> SymbolDeclaration
    default SymbolDeclaration: context:'context * declaration:SymbolDeclaration -> SymbolDeclaration

    /// <summary>
    /// Rewrites an <see cref="Expression"/> node.
    /// </summary>
    abstract Expression: context:'context * expression:Expression -> Expression
    default Expression: context:'context * expression:Expression -> Expression

    /// <summary>
    /// Rewrites an <see cref="Update"/> expression node.
    /// </summary>
    abstract Update: context:'context * update:Update -> Update
    default Update: context:'context * update:Update -> Update

    /// <summary>
    /// Rewrites a <see cref="Block{a}"/> node, given a rewriter for the block contents.
    /// </summary>
    abstract Block: context:'context * mapper:('context * 'a -> 'a) * block:'a Block -> 'a Block
    default Block: context:'context * mapper:('context * 'a -> 'a) * block:'a Block -> 'a Block

    /// <summary>
    /// Rewrites a <see cref="Tuple{a}"/> node, given a rewriter for the tuple contents.
    /// </summary>
    abstract Tuple: context:'context * mapper:('context * 'a -> 'a) * tuple:'a Tuple -> 'a Tuple
    default Tuple: context:'context * mapper:('context * 'a -> 'a) * tuple:'a Tuple -> 'a Tuple

    /// <summary>
    /// Rewrites a <see cref="SequenceItem{a}"/> node, given a rewriter for the sequence items.
    /// </summary>
    abstract SequenceItem: context:'context * mapper:('context * 'a -> 'a) * item:'a SequenceItem -> 'a SequenceItem
    default SequenceItem: context:'context * mapper:('context * 'a -> 'a) * item:'a SequenceItem -> 'a SequenceItem

    /// <summary>
    /// Rewrites a <see cref="BinaryOperator{a}"/> node, given a rewriter for the operands.
    /// </summary>
    abstract BinaryOperator: context:'context
                             * mapper:('context * 'a -> 'a)
                             * operator:'a BinaryOperator
                             -> 'a BinaryOperator
    default BinaryOperator: context:'context
                            * mapper:('context * 'a -> 'a)
                            * operator:'a BinaryOperator
                            -> 'a BinaryOperator

    /// <summary>
    /// Rewrites a <see cref="Terminal"/> node.
    /// </summary>
    abstract Terminal: context:'context * terminal:Terminal -> Terminal
    default Terminal: context:'context * terminal:Terminal -> Terminal
