// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
//
// This grammar is based on:
// https://github.com/microsoft/qsharp-language/blob/main/Specifications/Language/5_Grammar/QSharpParser.g4

parser grammar QSharpParser;

options {
    tokenVocab = QSharpLexer;
}

document : namespace* EOF;

// Namespace

namespace : 'namespace' qualifiedName BraceLeft namespaceElement* BraceRight;

qualifiedName : Identifier ('.' Identifier)*;

namespaceElement
    : openDirective # OpenElement
    | typeDeclaration # TypeElement
    | callableDeclaration # CallableElement
    ;

// Open Directive

openDirective : 'open' name=qualifiedName ('as' alias=qualifiedName)? ';';

// Declaration

attribute : '@' expression;

access : 'internal';

declarationPrefix : attribute* access?;

// Type Declaration

typeDeclaration : declarationPrefix 'newtype' Identifier '=' underlyingType ';';

underlyingType
    : typeDeclarationTuple # TupleUnderlyingType
    | type # UnnamedTypeItem
    ;

typeDeclarationTuple : '(' (typeTupleItem (',' typeTupleItem)*)? ')';

typeTupleItem
    : namedItem # NamedTypeItem
    | underlyingType # UnderlyingTypeItem
    ;

namedItem : Identifier ':' type;

// Callable Declaration

callableDeclaration
    : declarationPrefix keyword=('function' | 'operation')
      Identifier typeParameterBinding? parameterTuple
      ':' returnType=type characteristics?
      callableBody
    ;

typeParameterBinding : '<' (TypeParameter (',' TypeParameter)*)? '>';

parameterTuple : '(' (parameter (',' parameter)*)? ')';

parameter
    : namedItem # NamedParameter
    | parameterTuple # TupledParameter
    ;

characteristics : 'is' characteristicsExpression;

characteristicsExpression
    : 'Adj' # AdjointCharacteristics
    | 'Ctl' # ControlledCharacteristics
    | '(' characteristicsExpression ')' # CharacteristicGroup
    | left=characteristicsExpression '*' right=characteristicsExpression # IntersectCharacteristics
    | left=characteristicsExpression '+' right=characteristicsExpression # UnionCharacteristics
    ;

callableBody
    : scope # CallableStatements
    | BraceLeft specialization* BraceRight # CallableSpecializations
    ;

specialization : specializationName+ specializationGenerator;

specializationName : 'body' | 'adjoint' | 'controlled';

specializationGenerator
    : 'auto' ';' # AutoGenerator
    | 'self' ';' # SelfGenerator
    | 'invert' ';' # InvertGenerator
    | 'distribute' ';' # DistributeGenerator
    | 'intrinsic' ';' # IntrinsicGenerator
    | providedSpecialization # ProvidedGenerator
    ;

providedSpecialization : specializationParameterTuple? scope;

specializationParameterTuple : '(' (specializationParameter (',' specializationParameter)*)? ')';

specializationParameter : Identifier | '...';

// Type

type
    : '_' # MissingType
    | '(' (type (',' type)* ','?)? ')' # TupleType
    | TypeParameter # TypeParameter
    | type '[' ']' # ArrayType
    | from=type arrow=('->' | '=>') to=type characteristics? # CallableType
    | 'BigInt' # BigIntType
    | 'Bool' # BoolType
    | 'Double' # DoubleType
    | 'Int' # IntType
    | 'Pauli' # PauliType
    | 'Qubit' # QubitType
    | 'Range' # RangeType
    | 'Result' # ResultType
    | 'String' # StringType
    | 'Unit' # UnitType
    | qualifiedName # UserDefinedType
    ;

// Statement

statement
    : expression ';' # ExpressionStatement
    | 'return' expression ';' # ReturnStatement
    | 'fail' expression ';' # FailStatement
    | 'let' symbolBinding '=' expression ';' # LetStatement
    | 'mutable' symbolBinding '=' expression ';' # MutableStatement
    | 'set' symbolBinding '=' expression ';' # SetStatement
    | 'set' Identifier updateOperator expression ';' # UpdateStatement
    | 'set' Identifier 'w/=' index=expression '<-' value=expression ';' # UpdateWithStatement
    | 'if' expression scope # IfStatement
    | 'elif' expression scope # ElifStatement
    | 'else' scope # ElseStatement
    | 'for' (forBinding | '(' forBinding ')') scope # ForStatement
    | 'while' expression scope # WhileStatement
    | 'repeat' scope # RepeatStatement
    | 'until' expression (';' | 'fixup' scope) # UntilStatement
    | 'within' scope # WithinStatement
    | 'apply' scope # ApplyStatement
    | keyword=('use' | 'using' | 'borrow' | 'borrowing') (qubitBinding | '(' qubitBinding ')') (scope | ';') # QubitDeclaration
    ;

scope : BraceLeft statement* BraceRight;

symbolBinding
    : '_' # DiscardSymbol
    | Identifier # SymbolName
    | '(' (symbolBinding (',' symbolBinding)* ','?)? ')' # SymbolTuple
    ;

updateOperator
    : '^='
    | '*='
    | '/='
    | '%='
    | '+='
    | '-='
    | '>>>='
    | '<<<='
    | '&&&='
    | '^^^='
    | '|||='
    | 'and='
    | 'or='
    ;

forBinding : symbolBinding 'in' expression;

qubitBinding : symbolBinding '=' qubitInitializer;

qubitInitializer
    : 'Qubit' '(' ')' # SingleQubit
    | 'Qubit' '[' length=expression ']' # QubitArray
    | '(' (qubitInitializer (',' qubitInitializer)* ','?)? ')' # QubitTuple
    ;

// Expression

expression
    : '_' # MissingExpression
    | qualifiedName typeTuple? # IdentifierExpression
    | IntegerLiteral # IntegerExpression
    | BigIntegerLiteral # BigIntegerExpression
    | DoubleLiteral # DoubleExpression
    | DoubleQuote stringContent* StringDoubleQuote # StringExpression
    | DollarQuote interpStringContent* InterpDoubleQuote # InterpStringExpression
    | boolLiteral # BoolExpression
    | resultLiteral # ResultExpression
    | pauliLiteral # PauliExpression
    | '(' (expression (',' expression)* ','?)? ')' # TupleExpression
    | '[' (expression (',' expression)* ','?)? ']' # ArrayExpression
    | '[' value=expression ',' size '=' length=expression ']' # SizedArrayExpression
    | 'new' type '[' length=expression ']' # NewArrayExpression
    | expression '::' Identifier # NamedItemAccessExpression
    | array=expression '[' index=expression ']' # ArrayAccessExpression
    | expression '!' # UnwrapExpression
    | <assoc=right> 'Controlled' expression # ControlledExpression
    | <assoc=right> 'Adjoint' expression # AdjointExpression
    | callable=expression '(' (args+=expression (',' args+=expression)* ','?)? ')' # CallExpression
    | <assoc=right> op=('!' | '+' | '-' | 'not' | '~~~') expression # PrefixOpExpression
    | <assoc=right> left=expression '^' right=expression # ExponentExpression
    | left=expression op=('*' | '/' | '%') right=expression # MultiplyExpression
    | left=expression op=('+' | '-') right=expression # AddExpression
    | left=expression op=('>>>' | '<<<') right=expression # ShiftExpression
    | left=expression op=('>' | '<' | '>=' | '<=') right=expression # CompareExpression
    | left=expression op=('==' | '!=') right=expression # EqualsExpression
    | left=expression '&&&' right=expression # BitwiseAndExpression
    | left=expression '^^^' right=expression # BitwiseXorExpression
    | left=expression '|||' right=expression # BitwiseOrExpression
    | left=expression op=('&&' | 'and') right=expression # AndExpression
    | left=expression op=('||' | 'or') right=expression # OrExpression
    | <assoc=right> cond=expression '?' then=expression '|' else=expression # ConditionalExpression
    | left=expression '..' right=expression # RangeExpression
    | expression '...' # RightOpenRangeExpression
    | '...' expression # LeftOpenRangeExpression
    | '...' # OpenRangeExpression
    | record=expression 'w/' index=expression '<-' value=expression # UpdateExpression
    ;

size : Identifier { _localctx.Identifier().Symbol.Text == "size" }?;

typeTuple : '<' (type (',' type)* ','?)? '>';

boolLiteral : 'false' | 'true';
resultLiteral : 'Zero' | 'One';
pauliLiteral : 'PauliI' | 'PauliX' | 'PauliY' | 'PauliZ';

stringContent : StringEscape | StringText;

interpStringContent
    : InterpStringEscape # InterpStringEscapeContent
    | InterpBraceLeft expression BraceRight # InterpExpressionContent
    | InterpStringText # InterpTextContent
    ;
