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
    abstract Document: context: 'context * document: Document -> Document
    default Document: context: 'context * document: Document -> Document

    /// <summary>
    /// Rewrites a <see cref="Namespace"/> node.
    /// </summary>
    abstract Namespace: context: 'context * ns: Namespace -> Namespace
    default Namespace: context: 'context * ns: Namespace -> Namespace

    /// <summary>
    /// Rewrites a <see cref="NamespaceItem"/> node.
    /// </summary>
    abstract NamespaceItem: context: 'context * item: NamespaceItem -> NamespaceItem
    default NamespaceItem: context: 'context * item: NamespaceItem -> NamespaceItem

    /// <summary>
    /// Rewrites an <see cref="OpenDirective"/> node.
    /// </summary>
    abstract OpenDirective: context: 'context * directive: OpenDirective -> OpenDirective
    default OpenDirective: context: 'context * directive: OpenDirective -> OpenDirective

    /// <summary>
    /// Rewrites a <see cref="TypeDeclaration"/> node.
    /// </summary>
    abstract TypeDeclaration: context: 'context * declaration: TypeDeclaration -> TypeDeclaration
    default TypeDeclaration: context: 'context * declaration: TypeDeclaration -> TypeDeclaration

    /// <summary>
    /// Rewrites an <see cref="Attribute"/> node.
    /// </summary>
    abstract Attribute: context: 'context * attribute: Attribute -> Attribute
    default Attribute: context: 'context * attribute: Attribute -> Attribute

    /// <summary>
    /// Rewrites an <see cref="UnderlyingType"/> node.
    /// </summary>
    abstract UnderlyingType: context: 'context * underlying: UnderlyingType -> UnderlyingType
    default UnderlyingType: context: 'context * underlying: UnderlyingType -> UnderlyingType

    /// <summary>
    /// Rewrites a <see cref="TypeTupleItem"/> node.
    /// </summary>
    abstract TypeTupleItem: context: 'context * item: TypeTupleItem -> TypeTupleItem
    default TypeTupleItem: context: 'context * item: TypeTupleItem -> TypeTupleItem

    /// <summary>
    /// Rewrites a <see cref="CallableDeclaration"/> node.
    /// </summary>
    abstract CallableDeclaration: context: 'context * callable: CallableDeclaration -> CallableDeclaration
    default CallableDeclaration: context: 'context * callable: CallableDeclaration -> CallableDeclaration

    /// <summary>
    /// Rewrites a <see cref="TypeParameterBinding"/> node.
    /// </summary>
    abstract TypeParameterBinding: context: 'context * binding: TypeParameterBinding -> TypeParameterBinding
    default TypeParameterBinding: context: 'context * binding: TypeParameterBinding -> TypeParameterBinding

    /// <summary>
    /// Rewrites a <see cref="Type"/> node.
    /// </summary>
    abstract Type: context: 'context * typ: Type -> Type
    default Type: context: 'context * typ: Type -> Type

    /// <summary>
    /// Rewrites a <see cref="TypeAnnotation"/> node.
    /// </summary>
    abstract TypeAnnotation: context: 'context * annotation: TypeAnnotation -> TypeAnnotation
    default TypeAnnotation: context: 'context * annotation: TypeAnnotation -> TypeAnnotation

    /// <summary>
    /// Rewrites an <see cref="ArrayType"/> node.
    /// </summary>
    abstract ArrayType: context: 'context * array: ArrayType -> ArrayType
    default ArrayType: context: 'context * array: ArrayType -> ArrayType

    /// <summary>
    /// Rewrites a <see cref="CallableType"/> node.
    /// </summary>
    abstract CallableType: context: 'context * callable: CallableType -> CallableType
    default CallableType: context: 'context * callable: CallableType -> CallableType

    /// <summary>
    /// Rewrites a <see cref="CharacteristicSection"/> node.
    /// </summary>
    abstract CharacteristicSection: context: 'context * section: CharacteristicSection -> CharacteristicSection
    default CharacteristicSection: context: 'context * section: CharacteristicSection -> CharacteristicSection

    /// <summary>
    /// Rewrites a <see cref="CharacteristicGroup"/> node.
    /// </summary>
    abstract CharacteristicGroup: context: 'context * group: CharacteristicGroup -> CharacteristicGroup
    default CharacteristicGroup: context: 'context * group: CharacteristicGroup -> CharacteristicGroup

    /// <summary>
    /// Rewrites a <see cref="Characteristic"/> node.
    /// </summary>
    abstract Characteristic: context: 'context * characteristic: Characteristic -> Characteristic
    default Characteristic: context: 'context * characteristic: Characteristic -> Characteristic

    /// <summary>
    /// Rewrites a <see cref="CallableBody"/> node.
    /// </summary>
    abstract CallableBody: context: 'context * body: CallableBody -> CallableBody
    default CallableBody: context: 'context * body: CallableBody -> CallableBody

    /// <summary>
    /// Rewrites a <see cref="Specialization"/> node.
    /// </summary>
    abstract Specialization: context: 'context * specialization: Specialization -> Specialization
    default Specialization: context: 'context * specialization: Specialization -> Specialization

    /// <summary>
    /// Rewrites a <see cref="SpecializationGenerator"/> node.
    /// </summary>
    abstract SpecializationGenerator: context: 'context * generator: SpecializationGenerator -> SpecializationGenerator
    default SpecializationGenerator: context: 'context * generator: SpecializationGenerator -> SpecializationGenerator

    /// <summary>
    /// Rewrites a <see cref="Statement"/> node.
    /// </summary>
    abstract Statement: context: 'context * statement: Statement -> Statement
    default Statement: context: 'context * statement: Statement -> Statement

    /// <summary>
    /// Rewrites an <see cref="ExpressionStatement"/> statement node.
    /// </summary>
    abstract ExpressionStatement: context: 'context * expr: ExpressionStatement -> ExpressionStatement
    default ExpressionStatement: context: 'context * expr: ExpressionStatement -> ExpressionStatement

    /// <summary>
    /// Rewrites a <see cref="ReturnStatement"/> statement node.
    /// </summary>
    abstract ReturnStatement: context: 'context * returns: SimpleStatement -> SimpleStatement
    default ReturnStatement: context: 'context * returns: SimpleStatement -> SimpleStatement

    /// <summary>
    /// Rewrites a <see cref="FailStatement"/> statement node.
    /// </summary>
    abstract FailStatement: context: 'context * fails: SimpleStatement -> SimpleStatement
    default FailStatement: context: 'context * fails: SimpleStatement -> SimpleStatement

    /// <summary>
    /// Rewrites a <see cref="LetStatement"/> statement node.
    /// </summary>
    abstract LetStatement: context: 'context * lets: BindingStatement -> BindingStatement
    default LetStatement: context: 'context * lets: BindingStatement -> BindingStatement

    /// <summary>
    /// Rewrites a <see cref="MutableStatement"/> declaration statement node.
    /// </summary>
    abstract MutableStatement: context: 'context * mutables: BindingStatement -> BindingStatement
    default MutableStatement: context: 'context * mutables: BindingStatement -> BindingStatement

    /// <summary>
    /// Rewrites a <see cref="SetStatement"/> statement node.
    /// </summary>
    abstract SetStatement: context: 'context * sets: BindingStatement -> BindingStatement
    default SetStatement: context: 'context * sets: BindingStatement -> BindingStatement

    /// <summary>
    /// Rewrites an <see cref="UpdateStatement"/> statement node.
    /// </summary>
    abstract UpdateStatement: context: 'context * updates: UpdateStatement -> UpdateStatement
    default UpdateStatement: context: 'context * updates: UpdateStatement -> UpdateStatement

    /// <summary>
    /// Rewrites an <see cref="UpdateWithStatement"/> statement node.
    /// </summary>
    abstract UpdateWithStatement: context: 'context * withs: UpdateWithStatement -> UpdateWithStatement
    default UpdateWithStatement: context: 'context * withs: UpdateWithStatement -> UpdateWithStatement

    /// <summary>
    /// Rewrites an <see cref="IfStatement"/> statement node.
    /// </summary>
    abstract IfStatement: context: 'context * ifs: ConditionalBlockStatement -> ConditionalBlockStatement
    default IfStatement: context: 'context * ifs: ConditionalBlockStatement -> ConditionalBlockStatement

    /// <summary>
    /// Rewrites an <see cref="ElifStatement"/> statement node.
    /// </summary>
    abstract ElifStatement: context: 'context * elifs: ConditionalBlockStatement -> ConditionalBlockStatement
    default ElifStatement: context: 'context * elifs: ConditionalBlockStatement -> ConditionalBlockStatement

    /// <summary>
    /// Rewrites an <see cref="ElseStatement"/> statement node.
    /// </summary>
    abstract ElseStatement: context: 'context * elses: BlockStatement -> BlockStatement
    default ElseStatement: context: 'context * elses: BlockStatement -> BlockStatement

    /// <summary>
    /// Rewrites a <see cref="ForStatement"/> statement node.
    /// </summary>
    abstract ForStatement: context: 'context * loop: ForStatement -> ForStatement
    default ForStatement: context: 'context * loop: ForStatement -> ForStatement

    /// <summary>
    /// Rewrites a <see cref="WhileStatement"/> statement node.
    /// </summary>
    abstract WhileStatement: context: 'context * whiles: ConditionalBlockStatement -> ConditionalBlockStatement
    default WhileStatement: context: 'context * whiles: ConditionalBlockStatement -> ConditionalBlockStatement

    /// <summary>
    /// Rewrites a <see cref="RepeatStatement"/> statement node.
    /// </summary>
    abstract RepeatStatement: context: 'context * repeats: BlockStatement -> BlockStatement
    default RepeatStatement: context: 'context * repeats: BlockStatement -> BlockStatement

    /// <summary>
    /// Rewrites a <see cref="UntilStatement"/> statement node.
    /// </summary>
    abstract UntilStatement: context: 'context * untils: UntilStatement -> UntilStatement
    default UntilStatement: context: 'context * untils: UntilStatement -> UntilStatement

    /// <summary>
    /// Rewrites a <see cref="Fixup"/> node.
    /// </summary>
    abstract Fixup: context: 'context * fixup: BlockStatement -> BlockStatement
    default Fixup: context: 'context * fixup: BlockStatement -> BlockStatement

    /// <summary>
    /// Rewrites a <see cref="WithinStatement"/> statement node.
    /// </summary>
    abstract WithinStatement: context: 'context * withins: BlockStatement -> BlockStatement
    default WithinStatement: context: 'context * withins: BlockStatement -> BlockStatement

    /// <summary>
    /// Rewrites a <see cref="ApplyStatement"/> statement node.
    /// </summary>
    abstract ApplyStatement: context: 'context * apply: BlockStatement -> BlockStatement
    default ApplyStatement: context: 'context * apply: BlockStatement -> BlockStatement

    /// <summary>
    /// Rewrites a <see cref="QubitDeclarationStatement"/> statement node.
    /// </summary>
    abstract QubitDeclarationStatement: context: 'context * decl: QubitDeclarationStatement -> QubitDeclarationStatement
    default QubitDeclarationStatement: context: 'context * decl: QubitDeclarationStatement -> QubitDeclarationStatement

    /// <summary>
    /// Rewrites a <see cref="ParameterBinding"/> node.
    /// </summary>
    abstract ParameterBinding: context: 'context * binding: ParameterBinding -> ParameterBinding
    default ParameterBinding: context: 'context * binding: ParameterBinding -> ParameterBinding

    /// <summary>
    /// Rewrites a <see cref="ParameterDeclaration"/> node.
    /// </summary>
    abstract ParameterDeclaration: context: 'context * declaration: ParameterDeclaration -> ParameterDeclaration
    default ParameterDeclaration: context: 'context * declaration: ParameterDeclaration -> ParameterDeclaration

    /// <summary>
    /// Rewrites a <see cref="SymbolBinding"/> node.
    /// </summary>
    abstract SymbolBinding: context: 'context * symbol: SymbolBinding -> SymbolBinding
    default SymbolBinding: context: 'context * symbol: SymbolBinding -> SymbolBinding

    /// <summary>
    /// Rewrites a <see cref="QubitBinding"/> node.
    /// </summary>
    abstract QubitBinding: context: 'context * binding: QubitBinding -> QubitBinding
    default QubitBinding: context: 'context * binding: QubitBinding -> QubitBinding

    /// <summary>
    /// Rewrites a <see cref="ForBinding"/> node.
    /// </summary>
    abstract ForBinding: context: 'context * binding: ForBinding -> ForBinding
    default ForBinding: context: 'context * binding: ForBinding -> ForBinding

    /// <summary>
    /// Rewrites a <see cref="QubitInitializer"/> node.
    /// </summary>
    abstract QubitInitializer: context: 'context * initializer: QubitInitializer -> QubitInitializer
    default QubitInitializer: context: 'context * initializer: QubitInitializer -> QubitInitializer

    /// <summary>
    /// Rewrites a <see cref="SingleQubit"/> node.
    /// </summary>
    abstract SingleQubit: context: 'context * newQubit: SingleQubit -> SingleQubit
    default SingleQubit: context: 'context * newQubit: SingleQubit -> SingleQubit

    /// <summary>
    /// Rewrites a <see cref="QubitArray"/> node.
    /// </summary>
    abstract QubitArray: context: 'context * newQubits: QubitArray -> QubitArray
    default QubitArray: context: 'context * newQubits: QubitArray -> QubitArray

    /// <summary>
    /// Rewrites an <see cref="InterpStringContent"/> node.
    /// </summary>
    abstract InterpStringContent: context: 'context * interpStringContent: InterpStringContent -> InterpStringContent
    default InterpStringContent: context: 'context * interpStringContent: InterpStringContent -> InterpStringContent

    /// <summary>
    /// Rewrites an <see cref="InterpStringExpression"/> node.
    /// </summary>
    abstract InterpStringExpression:
        context: 'context * interpStringExpression: InterpStringExpression -> InterpStringExpression
    default InterpStringExpression:
        context: 'context * interpStringExpression: InterpStringExpression -> InterpStringExpression

    /// <summary>
    /// Rewrites an <see cref="Expression"/> node.
    /// </summary>
    abstract Expression: context: 'context * expression: Expression -> Expression
    default Expression: context: 'context * expression: Expression -> Expression

    /// <summary>
    /// Rewrites an <see cref="Identifier"/> expression node.
    /// </summary>
    abstract Identifier: context: 'context * identifier: Identifier -> Identifier
    default Identifier: context: 'context * identifier: Identifier -> Identifier

    /// <summary>
    /// Rewrites an <see cref="InterpString"/> expression node.
    /// </summary>
    abstract InterpString: context: 'context * interpString: InterpString -> InterpString
    default InterpString: context: 'context * interpString: InterpString -> InterpString

    /// <summary>
    /// Rewrites a <see cref="NewArray"/> expression node.
    /// </summary>
    abstract NewArray: context: 'context * newArray: NewArray -> NewArray
    default NewArray: context: 'context * newArray: NewArray -> NewArray

    /// <summary>
    /// Rewrites a <see cref="NewSizedArray"/> expression node.
    /// </summary>
    abstract NewSizedArray: context: 'context * newSizedArray: NewSizedArray -> NewSizedArray
    default NewSizedArray: context: 'context * newSizedArray: NewSizedArray -> NewSizedArray

    /// <summary>
    /// Rewrites a <see cref="NamedItemAccess"/> expression node.
    /// </summary>
    abstract NamedItemAccess: context: 'context * namedItemAccess: NamedItemAccess -> NamedItemAccess
    default NamedItemAccess: context: 'context * namedItemAccess: NamedItemAccess -> NamedItemAccess

    /// <summary>
    /// Rewrites an <see cref="ArrayAccess"/> expression node.
    /// </summary>
    abstract ArrayAccess: context: 'context * arrayAccess: ArrayAccess -> ArrayAccess
    default ArrayAccess: context: 'context * arrayAccess: ArrayAccess -> ArrayAccess

    /// <summary>
    /// Rewrites a <see cref="Call"/> expression node.
    /// </summary>
    abstract Call: context: 'context * call: Call -> Call
    default Call: context: 'context * call: Call -> Call

    /// <summary>
    /// Rewrites a <see cref="Conditional"/> expression node.
    /// </summary>
    abstract Conditional: context: 'context * conditional: Conditional -> Conditional
    default Conditional: context: 'context * conditional: Conditional -> Conditional

    /// <summary>
    /// Rewrites an <see cref="Update"/> expression node.
    /// </summary>
    abstract Update: context: 'context * update: Update -> Update
    default Update: context: 'context * update: Update -> Update

    /// <summary>
    /// Rewrites a <see cref="Lambda"/> expression node.
    /// </summary>
    abstract Lambda: context: 'context * lambda: Lambda -> Lambda
    default Lambda: context: 'context * lambda: Lambda -> Lambda

    /// <summary>
    /// Rewrites a <see cref="Block{a}"/> node, given a rewriter for the block contents.
    /// </summary>
    abstract Block: context: 'context * mapper: ('context * 'a -> 'a) * block: 'a Block -> 'a Block
    default Block: context: 'context * mapper: ('context * 'a -> 'a) * block: 'a Block -> 'a Block

    /// <summary>
    /// Rewrites a <see cref="Tuple{a}"/> node, given a rewriter for the tuple contents.
    /// </summary>
    abstract Tuple: context: 'context * mapper: ('context * 'a -> 'a) * tuple: 'a Tuple -> 'a Tuple
    default Tuple: context: 'context * mapper: ('context * 'a -> 'a) * tuple: 'a Tuple -> 'a Tuple

    /// <summary>
    /// Rewrites a <see cref="SequenceItem{a}"/> node, given a rewriter for the sequence items.
    /// </summary>
    abstract SequenceItem: context: 'context * mapper: ('context * 'a -> 'a) * item: 'a SequenceItem -> 'a SequenceItem
    default SequenceItem: context: 'context * mapper: ('context * 'a -> 'a) * item: 'a SequenceItem -> 'a SequenceItem

    /// <summary>
    /// Rewrites a <see cref="PrefixOperator{a}"/> node, given a rewriter for the operand.
    /// </summary>
    abstract PrefixOperator:
        context: 'context * mapper: ('context * 'a -> 'a) * operator: 'a PrefixOperator -> 'a PrefixOperator
    default PrefixOperator:
        context: 'context * mapper: ('context * 'a -> 'a) * operator: 'a PrefixOperator -> 'a PrefixOperator

    /// <summary>
    /// Rewrites a <see cref="PostfixOperator{a}"/> node, given a rewriter for the operand.
    /// </summary>
    abstract PostfixOperator:
        context: 'context * mapper: ('context * 'a -> 'a) * operator: 'a PostfixOperator -> 'a PostfixOperator
    default PostfixOperator:
        context: 'context * mapper: ('context * 'a -> 'a) * operator: 'a PostfixOperator -> 'a PostfixOperator

    /// <summary>
    /// Rewrites an <see cref="InfixOperator{a}"/> node, given a rewriter for the operands.
    /// </summary>
    abstract InfixOperator:
        context: 'context * mapper: ('context * 'a -> 'a) * operator: 'a InfixOperator -> 'a InfixOperator
    default InfixOperator:
        context: 'context * mapper: ('context * 'a -> 'a) * operator: 'a InfixOperator -> 'a InfixOperator

    /// <summary>
    /// Rewrites a <see cref="Terminal"/> node.
    /// </summary>
    abstract Terminal: context: 'context * terminal: Terminal -> Terminal
    default Terminal: context: 'context * terminal: Terminal -> Terminal
