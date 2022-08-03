// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

/// <summary>
/// Rewrites a syntax tree.
/// </summary>
/// <typeparam name="context">The type of the context to use during recursive descent into the syntax tree.</typeparam>
type internal 'Context Rewriter =
    /// <summary>
    /// Creates a new syntax tree <see cref="Rewriter{context}"/>.
    /// </summary>
    new: unit -> 'Context Rewriter

    /// <summary>
    /// Rewrites a <see cref="Document"/> node.
    /// </summary>
    abstract Document: context: 'Context * document: Document -> Document
    default Document: context: 'Context * document: Document -> Document

    /// <summary>
    /// Rewrites a <see cref="Namespace"/> node.
    /// </summary>
    abstract Namespace: context: 'Context * ns: Namespace -> Namespace
    default Namespace: context: 'Context * ns: Namespace -> Namespace

    /// <summary>
    /// Rewrites a <see cref="NamespaceItem"/> node.
    /// </summary>
    abstract NamespaceItem: context: 'Context * item: NamespaceItem -> NamespaceItem
    default NamespaceItem: context: 'Context * item: NamespaceItem -> NamespaceItem

    /// <summary>
    /// Rewrites an <see cref="OpenDirective"/> node.
    /// </summary>
    abstract OpenDirective: context: 'Context * directive: OpenDirective -> OpenDirective
    default OpenDirective: context: 'Context * directive: OpenDirective -> OpenDirective

    /// <summary>
    /// Rewrites a <see cref="TypeDeclaration"/> node.
    /// </summary>
    abstract TypeDeclaration: context: 'Context * declaration: TypeDeclaration -> TypeDeclaration
    default TypeDeclaration: context: 'Context * declaration: TypeDeclaration -> TypeDeclaration

    /// <summary>
    /// Rewrites an <see cref="Attribute"/> node.
    /// </summary>
    abstract Attribute: context: 'Context * attribute: Attribute -> Attribute
    default Attribute: context: 'Context * attribute: Attribute -> Attribute

    /// <summary>
    /// Rewrites an <see cref="UnderlyingType"/> node.
    /// </summary>
    abstract UnderlyingType: context: 'Context * underlying: UnderlyingType -> UnderlyingType
    default UnderlyingType: context: 'Context * underlying: UnderlyingType -> UnderlyingType

    /// <summary>
    /// Rewrites a <see cref="TypeTupleItem"/> node.
    /// </summary>
    abstract TypeTupleItem: context: 'Context * item: TypeTupleItem -> TypeTupleItem
    default TypeTupleItem: context: 'Context * item: TypeTupleItem -> TypeTupleItem

    /// <summary>
    /// Rewrites a <see cref="CallableDeclaration"/> node.
    /// </summary>
    abstract CallableDeclaration: context: 'Context * callable: CallableDeclaration -> CallableDeclaration
    default CallableDeclaration: context: 'Context * callable: CallableDeclaration -> CallableDeclaration

    /// <summary>
    /// Rewrites a <see cref="TypeParameterBinding"/> node.
    /// </summary>
    abstract TypeParameterBinding: context: 'Context * binding: TypeParameterBinding -> TypeParameterBinding
    default TypeParameterBinding: context: 'Context * binding: TypeParameterBinding -> TypeParameterBinding

    /// <summary>
    /// Rewrites a <see cref="Type"/> node.
    /// </summary>
    abstract Type: context: 'Context * typ: Type -> Type
    default Type: context: 'Context * typ: Type -> Type

    /// <summary>
    /// Rewrites a <see cref="TypeAnnotation"/> node.
    /// </summary>
    abstract TypeAnnotation: context: 'Context * annotation: TypeAnnotation -> TypeAnnotation
    default TypeAnnotation: context: 'Context * annotation: TypeAnnotation -> TypeAnnotation

    /// <summary>
    /// Rewrites an <see cref="ArrayType"/> node.
    /// </summary>
    abstract ArrayType: context: 'Context * array: ArrayType -> ArrayType
    default ArrayType: context: 'Context * array: ArrayType -> ArrayType

    /// <summary>
    /// Rewrites a <see cref="CallableType"/> node.
    /// </summary>
    abstract CallableType: context: 'Context * callable: CallableType -> CallableType
    default CallableType: context: 'Context * callable: CallableType -> CallableType

    /// <summary>
    /// Rewrites a <see cref="CharacteristicSection"/> node.
    /// </summary>
    abstract CharacteristicSection: context: 'Context * section: CharacteristicSection -> CharacteristicSection
    default CharacteristicSection: context: 'Context * section: CharacteristicSection -> CharacteristicSection

    /// <summary>
    /// Rewrites a <see cref="CharacteristicGroup"/> node.
    /// </summary>
    abstract CharacteristicGroup: context: 'Context * group: CharacteristicGroup -> CharacteristicGroup
    default CharacteristicGroup: context: 'Context * group: CharacteristicGroup -> CharacteristicGroup

    /// <summary>
    /// Rewrites a <see cref="Characteristic"/> node.
    /// </summary>
    abstract Characteristic: context: 'Context * characteristic: Characteristic -> Characteristic
    default Characteristic: context: 'Context * characteristic: Characteristic -> Characteristic

    /// <summary>
    /// Rewrites a <see cref="CallableBody"/> node.
    /// </summary>
    abstract CallableBody: context: 'Context * body: CallableBody -> CallableBody
    default CallableBody: context: 'Context * body: CallableBody -> CallableBody

    /// <summary>
    /// Rewrites a <see cref="Specialization"/> node.
    /// </summary>
    abstract Specialization: context: 'Context * specialization: Specialization -> Specialization
    default Specialization: context: 'Context * specialization: Specialization -> Specialization

    /// <summary>
    /// Rewrites a <see cref="SpecializationGenerator"/> node.
    /// </summary>
    abstract SpecializationGenerator: context: 'Context * generator: SpecializationGenerator -> SpecializationGenerator
    default SpecializationGenerator: context: 'Context * generator: SpecializationGenerator -> SpecializationGenerator

    /// <summary>
    /// Rewrites a <see cref="Statement"/> node.
    /// </summary>
    abstract Statement: context: 'Context * statement: Statement -> Statement
    default Statement: context: 'Context * statement: Statement -> Statement

    /// <summary>
    /// Rewrites an <see cref="ExpressionStatement"/> statement node.
    /// </summary>
    abstract ExpressionStatement: context: 'Context * expr: ExpressionStatement -> ExpressionStatement
    default ExpressionStatement: context: 'Context * expr: ExpressionStatement -> ExpressionStatement

    /// <summary>
    /// Rewrites a <see cref="ReturnStatement"/> statement node.
    /// </summary>
    abstract ReturnStatement: context: 'Context * returns: SimpleStatement -> SimpleStatement
    default ReturnStatement: context: 'Context * returns: SimpleStatement -> SimpleStatement

    /// <summary>
    /// Rewrites a <see cref="FailStatement"/> statement node.
    /// </summary>
    abstract FailStatement: context: 'Context * fails: SimpleStatement -> SimpleStatement
    default FailStatement: context: 'Context * fails: SimpleStatement -> SimpleStatement

    /// <summary>
    /// Rewrites a <see cref="LetStatement"/> statement node.
    /// </summary>
    abstract LetStatement: context: 'Context * lets: BindingStatement -> BindingStatement
    default LetStatement: context: 'Context * lets: BindingStatement -> BindingStatement

    /// <summary>
    /// Rewrites a <see cref="MutableStatement"/> declaration statement node.
    /// </summary>
    abstract MutableStatement: context: 'Context * mutables: BindingStatement -> BindingStatement
    default MutableStatement: context: 'Context * mutables: BindingStatement -> BindingStatement

    /// <summary>
    /// Rewrites a <see cref="SetStatement"/> statement node.
    /// </summary>
    abstract SetStatement: context: 'Context * sets: BindingStatement -> BindingStatement
    default SetStatement: context: 'Context * sets: BindingStatement -> BindingStatement

    /// <summary>
    /// Rewrites an <see cref="UpdateStatement"/> statement node.
    /// </summary>
    abstract UpdateStatement: context: 'Context * updates: UpdateStatement -> UpdateStatement
    default UpdateStatement: context: 'Context * updates: UpdateStatement -> UpdateStatement

    /// <summary>
    /// Rewrites an <see cref="UpdateWithStatement"/> statement node.
    /// </summary>
    abstract UpdateWithStatement: context: 'Context * withs: UpdateWithStatement -> UpdateWithStatement
    default UpdateWithStatement: context: 'Context * withs: UpdateWithStatement -> UpdateWithStatement

    /// <summary>
    /// Rewrites an <see cref="IfStatement"/> statement node.
    /// </summary>
    abstract IfStatement: context: 'Context * ifs: ConditionalBlockStatement -> ConditionalBlockStatement
    default IfStatement: context: 'Context * ifs: ConditionalBlockStatement -> ConditionalBlockStatement

    /// <summary>
    /// Rewrites an <see cref="ElifStatement"/> statement node.
    /// </summary>
    abstract ElifStatement: context: 'Context * elifs: ConditionalBlockStatement -> ConditionalBlockStatement
    default ElifStatement: context: 'Context * elifs: ConditionalBlockStatement -> ConditionalBlockStatement

    /// <summary>
    /// Rewrites an <see cref="ElseStatement"/> statement node.
    /// </summary>
    abstract ElseStatement: context: 'Context * elses: BlockStatement -> BlockStatement
    default ElseStatement: context: 'Context * elses: BlockStatement -> BlockStatement

    /// <summary>
    /// Rewrites a <see cref="ForStatement"/> statement node.
    /// </summary>
    abstract ForStatement: context: 'Context * loop: ForStatement -> ForStatement
    default ForStatement: context: 'Context * loop: ForStatement -> ForStatement

    /// <summary>
    /// Rewrites a <see cref="WhileStatement"/> statement node.
    /// </summary>
    abstract WhileStatement: context: 'Context * whiles: ConditionalBlockStatement -> ConditionalBlockStatement
    default WhileStatement: context: 'Context * whiles: ConditionalBlockStatement -> ConditionalBlockStatement

    /// <summary>
    /// Rewrites a <see cref="RepeatStatement"/> statement node.
    /// </summary>
    abstract RepeatStatement: context: 'Context * repeats: BlockStatement -> BlockStatement
    default RepeatStatement: context: 'Context * repeats: BlockStatement -> BlockStatement

    /// <summary>
    /// Rewrites a <see cref="UntilStatement"/> statement node.
    /// </summary>
    abstract UntilStatement: context: 'Context * untils: UntilStatement -> UntilStatement
    default UntilStatement: context: 'Context * untils: UntilStatement -> UntilStatement

    /// <summary>
    /// Rewrites a <see cref="Fixup"/> node.
    /// </summary>
    abstract Fixup: context: 'Context * fixup: BlockStatement -> BlockStatement
    default Fixup: context: 'Context * fixup: BlockStatement -> BlockStatement

    /// <summary>
    /// Rewrites a <see cref="WithinStatement"/> statement node.
    /// </summary>
    abstract WithinStatement: context: 'Context * withins: BlockStatement -> BlockStatement
    default WithinStatement: context: 'Context * withins: BlockStatement -> BlockStatement

    /// <summary>
    /// Rewrites a <see cref="ApplyStatement"/> statement node.
    /// </summary>
    abstract ApplyStatement: context: 'Context * apply: BlockStatement -> BlockStatement
    default ApplyStatement: context: 'Context * apply: BlockStatement -> BlockStatement

    /// <summary>
    /// Rewrites a <see cref="QubitDeclarationStatement"/> statement node.
    /// </summary>
    abstract QubitDeclarationStatement: context: 'Context * decl: QubitDeclarationStatement -> QubitDeclarationStatement
    default QubitDeclarationStatement: context: 'Context * decl: QubitDeclarationStatement -> QubitDeclarationStatement

    /// <summary>
    /// Rewrites a <see cref="ParameterBinding"/> node.
    /// </summary>
    abstract ParameterBinding: context: 'Context * binding: ParameterBinding -> ParameterBinding
    default ParameterBinding: context: 'Context * binding: ParameterBinding -> ParameterBinding

    /// <summary>
    /// Rewrites a <see cref="ParameterDeclaration"/> node.
    /// </summary>
    abstract ParameterDeclaration: context: 'Context * declaration: ParameterDeclaration -> ParameterDeclaration
    default ParameterDeclaration: context: 'Context * declaration: ParameterDeclaration -> ParameterDeclaration

    /// <summary>
    /// Rewrites a <see cref="SymbolBinding"/> node.
    /// </summary>
    abstract SymbolBinding: context: 'Context * symbol: SymbolBinding -> SymbolBinding
    default SymbolBinding: context: 'Context * symbol: SymbolBinding -> SymbolBinding

    /// <summary>
    /// Rewrites a <see cref="QubitBinding"/> node.
    /// </summary>
    abstract QubitBinding: context: 'Context * binding: QubitBinding -> QubitBinding
    default QubitBinding: context: 'Context * binding: QubitBinding -> QubitBinding

    /// <summary>
    /// Rewrites a <see cref="ForBinding"/> node.
    /// </summary>
    abstract ForBinding: context: 'Context * binding: ForBinding -> ForBinding
    default ForBinding: context: 'Context * binding: ForBinding -> ForBinding

    /// <summary>
    /// Rewrites a <see cref="QubitInitializer"/> node.
    /// </summary>
    abstract QubitInitializer: context: 'Context * initializer: QubitInitializer -> QubitInitializer
    default QubitInitializer: context: 'Context * initializer: QubitInitializer -> QubitInitializer

    /// <summary>
    /// Rewrites a <see cref="SingleQubit"/> node.
    /// </summary>
    abstract SingleQubit: context: 'Context * newQubit: SingleQubit -> SingleQubit
    default SingleQubit: context: 'Context * newQubit: SingleQubit -> SingleQubit

    /// <summary>
    /// Rewrites a <see cref="QubitArray"/> node.
    /// </summary>
    abstract QubitArray: context: 'Context * newQubits: QubitArray -> QubitArray
    default QubitArray: context: 'Context * newQubits: QubitArray -> QubitArray

    /// <summary>
    /// Rewrites an <see cref="InterpStringContent"/> node.
    /// </summary>
    abstract InterpStringContent: context: 'Context * interpStringContent: InterpStringContent -> InterpStringContent
    default InterpStringContent: context: 'Context * interpStringContent: InterpStringContent -> InterpStringContent

    /// <summary>
    /// Rewrites an <see cref="InterpStringExpression"/> node.
    /// </summary>
    abstract InterpStringExpression:
        context: 'Context * interpStringExpression: InterpStringExpression -> InterpStringExpression
    default InterpStringExpression:
        context: 'Context * interpStringExpression: InterpStringExpression -> InterpStringExpression

    /// <summary>
    /// Rewrites an <see cref="Expression"/> node.
    /// </summary>
    abstract Expression: context: 'Context * expression: Expression -> Expression
    default Expression: context: 'Context * expression: Expression -> Expression

    /// <summary>
    /// Rewrites an <see cref="Identifier"/> expression node.
    /// </summary>
    abstract Identifier: context: 'Context * identifier: Identifier -> Identifier
    default Identifier: context: 'Context * identifier: Identifier -> Identifier

    /// <summary>
    /// Rewrites an <see cref="InterpString"/> expression node.
    /// </summary>
    abstract InterpString: context: 'Context * interpString: InterpString -> InterpString
    default InterpString: context: 'Context * interpString: InterpString -> InterpString

    /// <summary>
    /// Rewrites a <see cref="NewArray"/> expression node.
    /// </summary>
    abstract NewArray: context: 'Context * newArray: NewArray -> NewArray
    default NewArray: context: 'Context * newArray: NewArray -> NewArray

    /// <summary>
    /// Rewrites a <see cref="NewSizedArray"/> expression node.
    /// </summary>
    abstract NewSizedArray: context: 'Context * newSizedArray: NewSizedArray -> NewSizedArray
    default NewSizedArray: context: 'Context * newSizedArray: NewSizedArray -> NewSizedArray

    /// <summary>
    /// Rewrites a <see cref="NamedItemAccess"/> expression node.
    /// </summary>
    abstract NamedItemAccess: context: 'Context * namedItemAccess: NamedItemAccess -> NamedItemAccess
    default NamedItemAccess: context: 'Context * namedItemAccess: NamedItemAccess -> NamedItemAccess

    /// <summary>
    /// Rewrites an <see cref="ArrayAccess"/> expression node.
    /// </summary>
    abstract ArrayAccess: context: 'Context * arrayAccess: ArrayAccess -> ArrayAccess
    default ArrayAccess: context: 'Context * arrayAccess: ArrayAccess -> ArrayAccess

    /// <summary>
    /// Rewrites a <see cref="Call"/> expression node.
    /// </summary>
    abstract Call: context: 'Context * call: Call -> Call
    default Call: context: 'Context * call: Call -> Call

    /// <summary>
    /// Rewrites a <see cref="Conditional"/> expression node.
    /// </summary>
    abstract Conditional: context: 'Context * conditional: Conditional -> Conditional
    default Conditional: context: 'Context * conditional: Conditional -> Conditional

    /// <summary>
    /// Rewrites an <see cref="Update"/> expression node.
    /// </summary>
    abstract Update: context: 'Context * update: Update -> Update
    default Update: context: 'Context * update: Update -> Update

    /// <summary>
    /// Rewrites a <see cref="Lambda"/> expression node.
    /// </summary>
    abstract Lambda: context: 'Context * lambda: Lambda -> Lambda
    default Lambda: context: 'Context * lambda: Lambda -> Lambda

    /// <summary>
    /// Rewrites a <see cref="Block{a}"/> node, given a rewriter for the block contents.
    /// </summary>
    abstract Block: context: 'Context * mapper: ('Context * 'a -> 'a) * block: 'a Block -> 'a Block
    default Block: context: 'Context * mapper: ('Context * 'a -> 'a) * block: 'a Block -> 'a Block

    /// <summary>
    /// Rewrites a <see cref="Tuple{a}"/> node, given a rewriter for the tuple contents.
    /// </summary>
    abstract Tuple: context: 'Context * mapper: ('Context * 'a -> 'a) * tuple: 'a Tuple -> 'a Tuple
    default Tuple: context: 'Context * mapper: ('Context * 'a -> 'a) * tuple: 'a Tuple -> 'a Tuple

    /// <summary>
    /// Rewrites a <see cref="SequenceItem{a}"/> node, given a rewriter for the sequence items.
    /// </summary>
    abstract SequenceItem: context: 'Context * mapper: ('Context * 'a -> 'a) * item: 'a SequenceItem -> 'a SequenceItem
    default SequenceItem: context: 'Context * mapper: ('Context * 'a -> 'a) * item: 'a SequenceItem -> 'a SequenceItem

    /// <summary>
    /// Rewrites a <see cref="PrefixOperator{a}"/> node, given a rewriter for the operand.
    /// </summary>
    abstract PrefixOperator:
        context: 'Context * mapper: ('Context * 'a -> 'a) * operator: 'a PrefixOperator -> 'a PrefixOperator
    default PrefixOperator:
        context: 'Context * mapper: ('Context * 'a -> 'a) * operator: 'a PrefixOperator -> 'a PrefixOperator

    /// <summary>
    /// Rewrites a <see cref="PostfixOperator{a}"/> node, given a rewriter for the operand.
    /// </summary>
    abstract PostfixOperator:
        context: 'Context * mapper: ('Context * 'a -> 'a) * operator: 'a PostfixOperator -> 'a PostfixOperator
    default PostfixOperator:
        context: 'Context * mapper: ('Context * 'a -> 'a) * operator: 'a PostfixOperator -> 'a PostfixOperator

    /// <summary>
    /// Rewrites an <see cref="InfixOperator{a}"/> node, given a rewriter for the operands.
    /// </summary>
    abstract InfixOperator:
        context: 'Context * mapper: ('Context * 'a -> 'a) * operator: 'a InfixOperator -> 'a InfixOperator
    default InfixOperator:
        context: 'Context * mapper: ('Context * 'a -> 'a) * operator: 'a InfixOperator -> 'a InfixOperator

    /// <summary>
    /// Rewrites a <see cref="Terminal"/> node.
    /// </summary>
    abstract Terminal: context: 'Context * terminal: Terminal -> Terminal
    default Terminal: context: 'Context * terminal: Terminal -> Terminal
