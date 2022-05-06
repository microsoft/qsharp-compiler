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
    abstract Document: document: Document -> 'result
    default Document: document: Document -> 'result

    /// <summary>
    /// Reduces a <see cref="Namespace"/> node.
    /// </summary>
    abstract Namespace: ns: Namespace -> 'result
    default Namespace: ns: Namespace -> 'result

    /// <summary>
    /// Reduces a <see cref="NamespaceItem"/> node.
    /// </summary>
    abstract NamespaceItem: item: NamespaceItem -> 'result
    default NamespaceItem: item: NamespaceItem -> 'result

    /// <summary>
    /// Reduces an <see cref="OpenDirective"/> node.
    /// </summary>
    abstract OpenDirective: directive: OpenDirective -> 'result
    default OpenDirective: directive: OpenDirective -> 'result

    /// <summary>
    /// Reduces a <see cref="TypeDeclaration"/> node.
    /// </summary>
    abstract TypeDeclaration: declaration: TypeDeclaration -> 'result
    default TypeDeclaration: declaration: TypeDeclaration -> 'result

    /// <summary>
    /// Reduces an <see cref="Attribute"/> node.
    /// </summary>
    abstract Attribute: attribute: Attribute -> 'result
    default Attribute: attribute: Attribute -> 'result

    /// <summary>
    /// Reduces an <see cref="UnderlyingType"/> node.
    /// </summary>
    abstract UnderlyingType: underlying: UnderlyingType -> 'result
    default UnderlyingType: underlying: UnderlyingType -> 'result

    /// <summary>
    /// Reduces a <see cref="TypeTupleItem"/> node.
    /// </summary>
    abstract TypeTupleItem: item: TypeTupleItem -> 'result
    default TypeTupleItem: item: TypeTupleItem -> 'result

    /// <summary>
    /// Reduces a <see cref="CallableDeclaration"/> node.
    /// </summary>
    abstract CallableDeclaration: callable: CallableDeclaration -> 'result
    default CallableDeclaration: callable: CallableDeclaration -> 'result

    /// <summary>
    /// Reduces a <see cref="TypeParameterBinding"/> node.
    /// </summary>
    abstract TypeParameterBinding: binding: TypeParameterBinding -> 'result
    default TypeParameterBinding: binding: TypeParameterBinding -> 'result

    /// <summary>
    /// Reduces a <see cref="Type"/> node.
    /// </summary>
    abstract Type: typ: Type -> 'result
    default Type: typ: Type -> 'result

    /// <summary>
    /// Reduces a <see cref="TypeAnnotation"/> node.
    /// </summary>
    abstract TypeAnnotation: annotation: TypeAnnotation -> 'result
    default TypeAnnotation: annotation: TypeAnnotation -> 'result

    /// <summary>
    /// Reduces an <see cref="ArrayType"/> node.
    /// </summary>
    abstract ArrayType: array: ArrayType -> 'result
    default ArrayType: array: ArrayType -> 'result

    /// <summary>
    /// Reduces a <see cref="CallableType"/> node.
    /// </summary>
    abstract CallableType: callable: CallableType -> 'result
    default CallableType: callable: CallableType -> 'result

    /// <summary>
    /// Reduces a <see cref="CharacteristicSection"/> node.
    /// </summary>
    abstract CharacteristicSection: section: CharacteristicSection -> 'result
    default CharacteristicSection: section: CharacteristicSection -> 'result

    /// <summary>
    /// Reduces a <see cref="CharacteristicGroup"/> node.
    /// </summary>
    abstract CharacteristicGroup: group: CharacteristicGroup -> 'result
    default CharacteristicGroup: group: CharacteristicGroup -> 'result

    /// <summary>
    /// Reduces a <see cref="Characteristic"/> node.
    /// </summary>
    abstract Characteristic: characteristic: Characteristic -> 'result
    default Characteristic: characteristic: Characteristic -> 'result

    /// <summary>
    /// Reduces a <see cref="CallableBody"/> node.
    /// </summary>
    abstract CallableBody: body: CallableBody -> 'result
    default CallableBody: body: CallableBody -> 'result

    /// <summary>
    /// Reduces a <see cref="Specialization"/> node.
    /// </summary>
    abstract Specialization: specialization: Specialization -> 'result
    default Specialization: specialization: Specialization -> 'result

    /// <summary>
    /// Reduces a <see cref="SpecializationGenerator"/> node.
    /// </summary>
    abstract SpecializationGenerator: generator: SpecializationGenerator -> 'result
    default SpecializationGenerator: generator: SpecializationGenerator -> 'result

    /// <summary>
    /// Reduces a <see cref="Statement"/> node.
    /// </summary>
    abstract Statement: statement: Statement -> 'result
    default Statement: statement: Statement -> 'result

    /// <summary>
    /// Reduces an <see cref="ExpressionStatement"/> statement node.
    /// </summary>
    abstract ExpressionStatement: expr: ExpressionStatement -> 'result
    default ExpressionStatement: expr: ExpressionStatement -> 'result

    /// <summary>
    /// Reduces a <see cref="ReturnStatement"/> statement node.
    /// </summary>
    abstract ReturnStatement: returns: SimpleStatement -> 'result
    default ReturnStatement: returns: SimpleStatement -> 'result

    /// <summary>
    /// Reduces a <see cref="FailStatement"/> statement node.
    /// </summary>
    abstract FailStatement: fails: SimpleStatement -> 'result
    default FailStatement: fails: SimpleStatement -> 'result

    /// <summary>
    /// Reduces a <see cref="LetStatement"/> statement node.
    /// </summary>
    abstract LetStatement: lets: BindingStatement -> 'result
    default LetStatement: lets: BindingStatement -> 'result

    /// <summary>
    /// Reduces a <see cref="MutableStatement"/> declaration statement node.
    /// </summary>
    abstract MutableStatement: mutables: BindingStatement -> 'result
    default MutableStatement: mutables: BindingStatement -> 'result

    /// <summary>
    /// Reduces a <see cref="SetStatement"/> statement node.
    /// </summary>
    abstract SetStatement: sets: BindingStatement -> 'result
    default SetStatement: sets: BindingStatement -> 'result

    /// <summary>
    /// Reduces an <see cref="UpdateStatement"/> statement node.
    /// </summary>
    abstract UpdateStatement: updates: UpdateStatement -> 'result
    default UpdateStatement: updates: UpdateStatement -> 'result

    /// <summary>
    /// Reduces an <see cref="UpdateWithStatement"/> statement node.
    /// </summary>
    abstract UpdateWithStatement: withs: UpdateWithStatement -> 'result
    default UpdateWithStatement: withs: UpdateWithStatement -> 'result

    /// <summary>
    /// Reduces an <see cref="IfStatement"/> statement node.
    /// </summary>
    abstract IfStatement: ifs: ConditionalBlockStatement -> 'result
    default IfStatement: ifs: ConditionalBlockStatement -> 'result

    /// <summary>
    /// Reduces an <see cref="ElifStatement"/> statement node.
    /// </summary>
    abstract ElifStatement: elifs: ConditionalBlockStatement -> 'result
    default ElifStatement: elifs: ConditionalBlockStatement -> 'result

    /// <summary>
    /// Reduces an <see cref="ElseStatement"/> statement node.
    /// </summary>
    abstract ElseStatement: elses: BlockStatement -> 'result
    default ElseStatement: elses: BlockStatement -> 'result

    /// <summary>
    /// Reduces an <see cref="ForStatement"/> statement node.
    /// </summary>
    abstract ForStatement: loop: ForStatement -> 'result
    default ForStatement: loop: ForStatement -> 'result

    /// <summary>
    /// Reduces an <see cref="WhileStatement"/> statement node.
    /// </summary>
    abstract WhileStatement: whiles: ConditionalBlockStatement -> 'result
    default WhileStatement: whiles: ConditionalBlockStatement -> 'result

    /// <summary>
    /// Reduces an <see cref="RepeatStatement"/> statement node.
    /// </summary>
    abstract RepeatStatement: repeats: BlockStatement -> 'result
    default RepeatStatement: repeats: BlockStatement -> 'result

    /// <summary>
    /// Reduces an <see cref="UntilStatement"/> statement node.
    /// </summary>
    abstract UntilStatement: untils: UntilStatement -> 'result
    default UntilStatement: untils: UntilStatement -> 'result

    /// <summary>
    /// Reduces an <see cref="Fixup"/> node.
    /// </summary>
    abstract Fixup: fixup: BlockStatement -> 'result
    default Fixup: fixup: BlockStatement -> 'result

    /// <summary>
    /// Reduces an <see cref="WithinStatement"/> statement node.
    /// </summary>
    abstract WithinStatement: withins: BlockStatement -> 'result
    default WithinStatement: withins: BlockStatement -> 'result

    /// <summary>
    /// Reduces an <see cref="ApplyStatement"/> statement node.
    /// </summary>
    abstract ApplyStatement: apply: BlockStatement -> 'result
    default ApplyStatement: apply: BlockStatement -> 'result

    /// <summary>
    /// Reduces a <see cref="QubitDeclarationStatement"/> statement node.
    /// </summary>
    abstract QubitDeclarationStatement: decl: QubitDeclarationStatement -> 'result
    default QubitDeclarationStatement: decl: QubitDeclarationStatement -> 'result

    /// <summary>
    /// Reduces a <see cref="ParameterBinding"/> node.
    /// </summary>
    abstract ParameterBinding: binding: ParameterBinding -> 'result
    default ParameterBinding: binding: ParameterBinding -> 'result

    /// <summary>
    /// Reduces a <see cref="ParameterDeclaration"/> node.
    /// </summary>
    abstract ParameterDeclaration: declaration: ParameterDeclaration -> 'result
    default ParameterDeclaration: declaration: ParameterDeclaration -> 'result

    /// <summary>
    /// Reduces a <see cref="SymbolBinding"/> node.
    /// </summary>
    abstract SymbolBinding: symbol: SymbolBinding -> 'result
    default SymbolBinding: symbol: SymbolBinding -> 'result

    /// <summary>
    /// Reduces a <see cref="QubitBinding"/> node.
    /// </summary>
    abstract QubitBinding: binding: QubitBinding -> 'result
    default QubitBinding: binding: QubitBinding -> 'result

    /// <summary>
    /// Reduces a <see cref="ForBinding"/> node.
    /// </summary>
    abstract ForBinding: binding: ForBinding -> 'result
    default ForBinding: binding: ForBinding -> 'result

    /// <summary>
    /// Reduces a <see cref="QubitInitializer"/> node.
    /// </summary>
    abstract QubitInitializer: initializer: QubitInitializer -> 'result
    default QubitInitializer: initializer: QubitInitializer -> 'result

    /// <summary>
    /// Reduces a <see cref="SingleQubit"/> node.
    /// </summary>
    abstract SingleQubit: newQubit: SingleQubit -> 'result
    default SingleQubit: newQubit: SingleQubit -> 'result

    /// <summary>
    /// Reduces a <see cref="QubitArray"/> node.
    /// </summary>
    abstract QubitArray: newQubits: QubitArray -> 'result
    default QubitArray: newQubits: QubitArray -> 'result

    /// <summary>
    /// Reduces an <see cref="InterpStringContent"/> node.
    /// </summary>
    abstract InterpStringContent: interpStringContent: InterpStringContent -> 'result
    default InterpStringContent: interpStringContent: InterpStringContent -> 'result

    /// <summary>
    /// Reduces an <see cref="InterpStringExpression"/> node.
    /// </summary>
    abstract InterpStringExpression: interpStringExpression: InterpStringExpression -> 'result
    default InterpStringExpression: interpStringExpression: InterpStringExpression -> 'result

    /// <summary>
    /// Reduces an <see cref="Expression"/> node.
    /// </summary>
    abstract Expression: expression: Expression -> 'result
    default Expression: expression: Expression -> 'result

    /// <summary>
    /// Reduces an <see cref="Identifier"/> expression node.
    /// </summary>
    abstract Identifier: identifier: Identifier -> 'result
    default Identifier: identifier: Identifier -> 'result

    /// <summary>
    /// Reduces an <see cref="InterpString"/> expression node.
    /// </summary>
    abstract InterpString: interpString: InterpString -> 'result
    default InterpString: interpString: InterpString -> 'result

    /// <summary>
    /// Reduces a <see cref="NewArray"/> expression node.
    /// </summary>
    abstract NewArray: newArray: NewArray -> 'result
    default NewArray: newArray: NewArray -> 'result

    /// <summary>
    /// Reduces a <see cref="NewSizedArray"/> expression node.
    /// </summary>
    abstract NewSizedArray: newSizedArray: NewSizedArray -> 'result
    default NewSizedArray: newSizedArray: NewSizedArray -> 'result

    /// <summary>
    /// Reduces a <see cref="NamedItemAccess"/> expression node.
    /// </summary>
    abstract NamedItemAccess: namedItemAccess: NamedItemAccess -> 'result
    default NamedItemAccess: namedItemAccess: NamedItemAccess -> 'result

    /// <summary>
    /// Reduces an <see cref="ArrayAccess"/> expression node.
    /// </summary>
    abstract ArrayAccess: arrayAccess: ArrayAccess -> 'result
    default ArrayAccess: arrayAccess: ArrayAccess -> 'result

    /// <summary>
    /// Reduces a <see cref="Call"/> expression node.
    /// </summary>
    abstract Call: call: Call -> 'result
    default Call: call: Call -> 'result

    /// <summary>
    /// Reduces a <see cref="Conditional"/> expression node.
    /// </summary>
    abstract Conditional: conditional: Conditional -> 'result
    default Conditional: conditional: Conditional -> 'result

    /// <summary>
    /// Reduces an <see cref="Update"/> expression node.
    /// </summary>
    abstract Update: update: Update -> 'result
    default Update: update: Update -> 'result

    /// <summary>
    /// Reduces a <see cref="Lambda"/> expression node.
    /// </summary>
    abstract Lambda: lambda: Lambda -> 'result
    default Lambda: lambda: Lambda -> 'result

    /// <summary>
    /// Reduces a <see cref="Block{a}"/> node, given a reducer for the block contents.
    /// </summary>
    abstract Block: mapper: ('a -> 'result) * block: 'a Block -> 'result
    default Block: mapper: ('a -> 'result) * block: 'a Block -> 'result

    /// <summary>
    /// Reduces a <see cref="Tuple{a}"/> node, given a reducer for the tuple contents.
    /// </summary>
    abstract Tuple: mapper: ('a -> 'result) * tuple: 'a Tuple -> 'result
    default Tuple: mapper: ('a -> 'result) * tuple: 'a Tuple -> 'result

    /// <summary>
    /// Reduces a <see cref="SequenceItem{a}"/> node, given a reducer for the sequence items.
    /// </summary>
    abstract SequenceItem: mapper: ('a -> 'result) * item: 'a SequenceItem -> 'result
    default SequenceItem: mapper: ('a -> 'result) * item: 'a SequenceItem -> 'result

    /// <summary>
    /// Reduces a <see cref="PrefixOperator{a}"/> node, given a reducer for the operand.
    /// </summary>
    abstract PrefixOperator: mapper: ('a -> 'result) * operator: 'a PrefixOperator -> 'result
    default PrefixOperator: mapper: ('a -> 'result) * operator: 'a PrefixOperator -> 'result

    /// <summary>
    /// Reduces a <see cref="PostfixOperator{a}"/> node, given a reducer for the operand.
    /// </summary>
    abstract PostfixOperator: mapper: ('a -> 'result) * operator: 'a PostfixOperator -> 'result
    default PostfixOperator: mapper: ('a -> 'result) * operator: 'a PostfixOperator -> 'result

    /// <summary>
    /// Reduces an <see cref="InfixOperator{a}"/> node, given a reducer for the operands.
    /// </summary>
    abstract InfixOperator: mapper: ('a -> 'result) * operator: 'a InfixOperator -> 'result
    default InfixOperator: mapper: ('a -> 'result) * operator: 'a InfixOperator -> 'result

    /// <summary>
    /// Reduces a <see cref="Terminal"/> node.
    /// </summary>
    abstract Terminal: terminal: Terminal -> 'result
