// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

/// <summary>
/// Reduces a syntax tree to a single value.
/// </summary>
/// <typeparam name="result">The type of the reduced result.</typeparam>
[<AbstractClass>]
type internal 'Result Reducer =
    /// <summary>
    /// Creates a new syntax tree <see cref="Reducer{result}"/>.
    /// </summary>
    new: unit -> 'Result Reducer

    /// Combines two results into a single result.
    abstract Combine: 'Result * 'Result -> 'Result

    /// <summary>
    /// Reduces a <see cref="Document"/> node.
    /// </summary>
    abstract Document: document: Document -> 'Result
    default Document: document: Document -> 'Result

    /// <summary>
    /// Reduces a <see cref="Namespace"/> node.
    /// </summary>
    abstract Namespace: ns: Namespace -> 'Result
    default Namespace: ns: Namespace -> 'Result

    /// <summary>
    /// Reduces a <see cref="NamespaceItem"/> node.
    /// </summary>
    abstract NamespaceItem: item: NamespaceItem -> 'Result
    default NamespaceItem: item: NamespaceItem -> 'Result

    /// <summary>
    /// Reduces an <see cref="OpenDirective"/> node.
    /// </summary>
    abstract OpenDirective: directive: OpenDirective -> 'Result
    default OpenDirective: directive: OpenDirective -> 'Result

    /// <summary>
    /// Reduces a <see cref="TypeDeclaration"/> node.
    /// </summary>
    abstract TypeDeclaration: declaration: TypeDeclaration -> 'Result
    default TypeDeclaration: declaration: TypeDeclaration -> 'Result

    /// <summary>
    /// Reduces an <see cref="Attribute"/> node.
    /// </summary>
    abstract Attribute: attribute: Attribute -> 'Result
    default Attribute: attribute: Attribute -> 'Result

    /// <summary>
    /// Reduces an <see cref="UnderlyingType"/> node.
    /// </summary>
    abstract UnderlyingType: underlying: UnderlyingType -> 'Result
    default UnderlyingType: underlying: UnderlyingType -> 'Result

    /// <summary>
    /// Reduces a <see cref="TypeTupleItem"/> node.
    /// </summary>
    abstract TypeTupleItem: item: TypeTupleItem -> 'Result
    default TypeTupleItem: item: TypeTupleItem -> 'Result

    /// <summary>
    /// Reduces a <see cref="CallableDeclaration"/> node.
    /// </summary>
    abstract CallableDeclaration: callable: CallableDeclaration -> 'Result
    default CallableDeclaration: callable: CallableDeclaration -> 'Result

    /// <summary>
    /// Reduces a <see cref="TypeParameterBinding"/> node.
    /// </summary>
    abstract TypeParameterBinding: binding: TypeParameterBinding -> 'Result
    default TypeParameterBinding: binding: TypeParameterBinding -> 'Result

    /// <summary>
    /// Reduces a <see cref="Type"/> node.
    /// </summary>
    abstract Type: typ: Type -> 'Result
    default Type: typ: Type -> 'Result

    /// <summary>
    /// Reduces a <see cref="TypeAnnotation"/> node.
    /// </summary>
    abstract TypeAnnotation: annotation: TypeAnnotation -> 'Result
    default TypeAnnotation: annotation: TypeAnnotation -> 'Result

    /// <summary>
    /// Reduces an <see cref="ArrayType"/> node.
    /// </summary>
    abstract ArrayType: array: ArrayType -> 'Result
    default ArrayType: array: ArrayType -> 'Result

    /// <summary>
    /// Reduces a <see cref="CallableType"/> node.
    /// </summary>
    abstract CallableType: callable: CallableType -> 'Result
    default CallableType: callable: CallableType -> 'Result

    /// <summary>
    /// Reduces a <see cref="CharacteristicSection"/> node.
    /// </summary>
    abstract CharacteristicSection: section: CharacteristicSection -> 'Result
    default CharacteristicSection: section: CharacteristicSection -> 'Result

    /// <summary>
    /// Reduces a <see cref="CharacteristicGroup"/> node.
    /// </summary>
    abstract CharacteristicGroup: group: CharacteristicGroup -> 'Result
    default CharacteristicGroup: group: CharacteristicGroup -> 'Result

    /// <summary>
    /// Reduces a <see cref="Characteristic"/> node.
    /// </summary>
    abstract Characteristic: characteristic: Characteristic -> 'Result
    default Characteristic: characteristic: Characteristic -> 'Result

    /// <summary>
    /// Reduces a <see cref="CallableBody"/> node.
    /// </summary>
    abstract CallableBody: body: CallableBody -> 'Result
    default CallableBody: body: CallableBody -> 'Result

    /// <summary>
    /// Reduces a <see cref="Specialization"/> node.
    /// </summary>
    abstract Specialization: specialization: Specialization -> 'Result
    default Specialization: specialization: Specialization -> 'Result

    /// <summary>
    /// Reduces a <see cref="SpecializationGenerator"/> node.
    /// </summary>
    abstract SpecializationGenerator: generator: SpecializationGenerator -> 'Result
    default SpecializationGenerator: generator: SpecializationGenerator -> 'Result

    /// <summary>
    /// Reduces a <see cref="Statement"/> node.
    /// </summary>
    abstract Statement: statement: Statement -> 'Result
    default Statement: statement: Statement -> 'Result

    /// <summary>
    /// Reduces an <see cref="ExpressionStatement"/> statement node.
    /// </summary>
    abstract ExpressionStatement: expr: ExpressionStatement -> 'Result
    default ExpressionStatement: expr: ExpressionStatement -> 'Result

    /// <summary>
    /// Reduces a <see cref="ReturnStatement"/> statement node.
    /// </summary>
    abstract ReturnStatement: returns: SimpleStatement -> 'Result
    default ReturnStatement: returns: SimpleStatement -> 'Result

    /// <summary>
    /// Reduces a <see cref="FailStatement"/> statement node.
    /// </summary>
    abstract FailStatement: fails: SimpleStatement -> 'Result
    default FailStatement: fails: SimpleStatement -> 'Result

    /// <summary>
    /// Reduces a <see cref="LetStatement"/> statement node.
    /// </summary>
    abstract LetStatement: lets: BindingStatement -> 'Result
    default LetStatement: lets: BindingStatement -> 'Result

    /// <summary>
    /// Reduces a <see cref="MutableStatement"/> declaration statement node.
    /// </summary>
    abstract MutableStatement: mutables: BindingStatement -> 'Result
    default MutableStatement: mutables: BindingStatement -> 'Result

    /// <summary>
    /// Reduces a <see cref="SetStatement"/> statement node.
    /// </summary>
    abstract SetStatement: sets: BindingStatement -> 'Result
    default SetStatement: sets: BindingStatement -> 'Result

    /// <summary>
    /// Reduces an <see cref="UpdateStatement"/> statement node.
    /// </summary>
    abstract UpdateStatement: updates: UpdateStatement -> 'Result
    default UpdateStatement: updates: UpdateStatement -> 'Result

    /// <summary>
    /// Reduces an <see cref="UpdateWithStatement"/> statement node.
    /// </summary>
    abstract UpdateWithStatement: withs: UpdateWithStatement -> 'Result
    default UpdateWithStatement: withs: UpdateWithStatement -> 'Result

    /// <summary>
    /// Reduces an <see cref="IfStatement"/> statement node.
    /// </summary>
    abstract IfStatement: ifs: ConditionalBlockStatement -> 'Result
    default IfStatement: ifs: ConditionalBlockStatement -> 'Result

    /// <summary>
    /// Reduces an <see cref="ElifStatement"/> statement node.
    /// </summary>
    abstract ElifStatement: elifs: ConditionalBlockStatement -> 'Result
    default ElifStatement: elifs: ConditionalBlockStatement -> 'Result

    /// <summary>
    /// Reduces an <see cref="ElseStatement"/> statement node.
    /// </summary>
    abstract ElseStatement: elses: BlockStatement -> 'Result
    default ElseStatement: elses: BlockStatement -> 'Result

    /// <summary>
    /// Reduces an <see cref="ForStatement"/> statement node.
    /// </summary>
    abstract ForStatement: loop: ForStatement -> 'Result
    default ForStatement: loop: ForStatement -> 'Result

    /// <summary>
    /// Reduces an <see cref="WhileStatement"/> statement node.
    /// </summary>
    abstract WhileStatement: whiles: ConditionalBlockStatement -> 'Result
    default WhileStatement: whiles: ConditionalBlockStatement -> 'Result

    /// <summary>
    /// Reduces an <see cref="RepeatStatement"/> statement node.
    /// </summary>
    abstract RepeatStatement: repeats: BlockStatement -> 'Result
    default RepeatStatement: repeats: BlockStatement -> 'Result

    /// <summary>
    /// Reduces an <see cref="UntilStatement"/> statement node.
    /// </summary>
    abstract UntilStatement: untils: UntilStatement -> 'Result
    default UntilStatement: untils: UntilStatement -> 'Result

    /// <summary>
    /// Reduces an <see cref="Fixup"/> node.
    /// </summary>
    abstract Fixup: fixup: BlockStatement -> 'Result
    default Fixup: fixup: BlockStatement -> 'Result

    /// <summary>
    /// Reduces an <see cref="WithinStatement"/> statement node.
    /// </summary>
    abstract WithinStatement: withins: BlockStatement -> 'Result
    default WithinStatement: withins: BlockStatement -> 'Result

    /// <summary>
    /// Reduces an <see cref="ApplyStatement"/> statement node.
    /// </summary>
    abstract ApplyStatement: apply: BlockStatement -> 'Result
    default ApplyStatement: apply: BlockStatement -> 'Result

    /// <summary>
    /// Reduces a <see cref="QubitDeclarationStatement"/> statement node.
    /// </summary>
    abstract QubitDeclarationStatement: decl: QubitDeclarationStatement -> 'Result
    default QubitDeclarationStatement: decl: QubitDeclarationStatement -> 'Result

    /// <summary>
    /// Reduces a <see cref="ParameterBinding"/> node.
    /// </summary>
    abstract ParameterBinding: binding: ParameterBinding -> 'Result
    default ParameterBinding: binding: ParameterBinding -> 'Result

    /// <summary>
    /// Reduces a <see cref="ParameterDeclaration"/> node.
    /// </summary>
    abstract ParameterDeclaration: declaration: ParameterDeclaration -> 'Result
    default ParameterDeclaration: declaration: ParameterDeclaration -> 'Result

    /// <summary>
    /// Reduces a <see cref="SymbolBinding"/> node.
    /// </summary>
    abstract SymbolBinding: symbol: SymbolBinding -> 'Result
    default SymbolBinding: symbol: SymbolBinding -> 'Result

    /// <summary>
    /// Reduces a <see cref="QubitBinding"/> node.
    /// </summary>
    abstract QubitBinding: binding: QubitBinding -> 'Result
    default QubitBinding: binding: QubitBinding -> 'Result

    /// <summary>
    /// Reduces a <see cref="ForBinding"/> node.
    /// </summary>
    abstract ForBinding: binding: ForBinding -> 'Result
    default ForBinding: binding: ForBinding -> 'Result

    /// <summary>
    /// Reduces a <see cref="QubitInitializer"/> node.
    /// </summary>
    abstract QubitInitializer: initializer: QubitInitializer -> 'Result
    default QubitInitializer: initializer: QubitInitializer -> 'Result

    /// <summary>
    /// Reduces a <see cref="SingleQubit"/> node.
    /// </summary>
    abstract SingleQubit: newQubit: SingleQubit -> 'Result
    default SingleQubit: newQubit: SingleQubit -> 'Result

    /// <summary>
    /// Reduces a <see cref="QubitArray"/> node.
    /// </summary>
    abstract QubitArray: newQubits: QubitArray -> 'Result
    default QubitArray: newQubits: QubitArray -> 'Result

    /// <summary>
    /// Reduces an <see cref="InterpStringContent"/> node.
    /// </summary>
    abstract InterpStringContent: interpStringContent: InterpStringContent -> 'Result
    default InterpStringContent: interpStringContent: InterpStringContent -> 'Result

    /// <summary>
    /// Reduces an <see cref="InterpStringExpression"/> node.
    /// </summary>
    abstract InterpStringExpression: interpStringExpression: InterpStringExpression -> 'Result
    default InterpStringExpression: interpStringExpression: InterpStringExpression -> 'Result

    /// <summary>
    /// Reduces an <see cref="Expression"/> node.
    /// </summary>
    abstract Expression: expression: Expression -> 'Result
    default Expression: expression: Expression -> 'Result

    /// <summary>
    /// Reduces an <see cref="Identifier"/> expression node.
    /// </summary>
    abstract Identifier: identifier: Identifier -> 'Result
    default Identifier: identifier: Identifier -> 'Result

    /// <summary>
    /// Reduces an <see cref="InterpString"/> expression node.
    /// </summary>
    abstract InterpString: interpString: InterpString -> 'Result
    default InterpString: interpString: InterpString -> 'Result

    /// <summary>
    /// Reduces a <see cref="NewArray"/> expression node.
    /// </summary>
    abstract NewArray: newArray: NewArray -> 'Result
    default NewArray: newArray: NewArray -> 'Result

    /// <summary>
    /// Reduces a <see cref="NewSizedArray"/> expression node.
    /// </summary>
    abstract NewSizedArray: newSizedArray: NewSizedArray -> 'Result
    default NewSizedArray: newSizedArray: NewSizedArray -> 'Result

    /// <summary>
    /// Reduces a <see cref="NamedItemAccess"/> expression node.
    /// </summary>
    abstract NamedItemAccess: namedItemAccess: NamedItemAccess -> 'Result
    default NamedItemAccess: namedItemAccess: NamedItemAccess -> 'Result

    /// <summary>
    /// Reduces an <see cref="ArrayAccess"/> expression node.
    /// </summary>
    abstract ArrayAccess: arrayAccess: ArrayAccess -> 'Result
    default ArrayAccess: arrayAccess: ArrayAccess -> 'Result

    /// <summary>
    /// Reduces a <see cref="Call"/> expression node.
    /// </summary>
    abstract Call: call: Call -> 'Result
    default Call: call: Call -> 'Result

    /// <summary>
    /// Reduces a <see cref="Conditional"/> expression node.
    /// </summary>
    abstract Conditional: conditional: Conditional -> 'Result
    default Conditional: conditional: Conditional -> 'Result

    /// <summary>
    /// Reduces an <see cref="Update"/> expression node.
    /// </summary>
    abstract Update: update: Update -> 'Result
    default Update: update: Update -> 'Result

    /// <summary>
    /// Reduces a <see cref="Lambda"/> expression node.
    /// </summary>
    abstract Lambda: lambda: Lambda -> 'Result
    default Lambda: lambda: Lambda -> 'Result

    /// <summary>
    /// Reduces a <see cref="Block{a}"/> node, given a reducer for the block contents.
    /// </summary>
    abstract Block: mapper: ('a -> 'Result) * block: 'a Block -> 'Result
    default Block: mapper: ('a -> 'Result) * block: 'a Block -> 'Result

    /// <summary>
    /// Reduces a <see cref="Tuple{a}"/> node, given a reducer for the tuple contents.
    /// </summary>
    abstract Tuple: mapper: ('a -> 'Result) * tuple: 'a Tuple -> 'Result
    default Tuple: mapper: ('a -> 'Result) * tuple: 'a Tuple -> 'Result

    /// <summary>
    /// Reduces a <see cref="SequenceItem{a}"/> node, given a reducer for the sequence items.
    /// </summary>
    abstract SequenceItem: mapper: ('a -> 'Result) * item: 'a SequenceItem -> 'Result
    default SequenceItem: mapper: ('a -> 'Result) * item: 'a SequenceItem -> 'Result

    /// <summary>
    /// Reduces a <see cref="PrefixOperator{a}"/> node, given a reducer for the operand.
    /// </summary>
    abstract PrefixOperator: mapper: ('a -> 'Result) * operator: 'a PrefixOperator -> 'Result
    default PrefixOperator: mapper: ('a -> 'Result) * operator: 'a PrefixOperator -> 'Result

    /// <summary>
    /// Reduces a <see cref="PostfixOperator{a}"/> node, given a reducer for the operand.
    /// </summary>
    abstract PostfixOperator: mapper: ('a -> 'Result) * operator: 'a PostfixOperator -> 'Result
    default PostfixOperator: mapper: ('a -> 'Result) * operator: 'a PostfixOperator -> 'Result

    /// <summary>
    /// Reduces an <see cref="InfixOperator{a}"/> node, given a reducer for the operands.
    /// </summary>
    abstract InfixOperator: mapper: ('a -> 'Result) * operator: 'a InfixOperator -> 'Result
    default InfixOperator: mapper: ('a -> 'Result) * operator: 'a InfixOperator -> 'Result

    /// <summary>
    /// Reduces a <see cref="Terminal"/> node.
    /// </summary>
    abstract Terminal: terminal: Terminal -> 'Result
