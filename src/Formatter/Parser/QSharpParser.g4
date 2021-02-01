// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
//
// This grammar is based on:
// https://github.com/microsoft/qsharp-language/blob/main/Specifications/Language/5_Grammar/QSharpParser.g4

parser grammar QSharpParser;

options {
    tokenVocab = QSharpLexer;
}

document : namespaces=namespace* eof=EOF;

// Namespace

namespace
    : keyword='namespace' name=qualifiedName openBrace=BraceLeft elements+=namespaceElement* closeBrace=BraceRight
    ;

qualifiedName : Identifier ('.' Identifier)*;

namespaceElement
    : openDirective # OpenElement
    | typeDeclaration # TypeElement
    | callable=callableDeclaration # CallableElement
    ;

// Open Directive

openDirective : 'open' qualifiedName ('as' qualifiedName)? ';';

// Declaration

attribute : '@' expression;

access : 'internal';

declarationPrefix : attribute* access?;

// Type Declaration

typeDeclaration : declarationPrefix 'newtype' Identifier '=' underlyingType ';';

underlyingType
    : typeDeclarationTuple
    | type
    ;

typeDeclarationTuple : '(' (typeTupleItem (',' typeTupleItem)*)? ')';

typeTupleItem
    : namedItem
    | underlyingType
    ;

namedItem : name=Identifier colon=':' itemType=type;

// Callable Declaration

callableDeclaration
    : declarationPrefix keyword=('function' | 'operation')
      name=Identifier typeParameterBinding? tuple=parameterTuple
      colon=':' returnType=type characteristics?
      body=callableBody
    ;

typeParameterBinding : '<' (TypeParameter (',' TypeParameter)*)? '>';

parameterTuple : openParen='(' (parameters+=parameter (commas+=',' parameters+=parameter)*)? closeParen=')';

parameter
    : namedItem # NamedParameter
    | parameterTuple # TupledParameter
    ;

characteristics : 'is' characteristicsExpression;

characteristicsExpression
    : 'Adj'
    | 'Ctl'
    | '(' characteristicsExpression ')'
    | characteristicsExpression '*' characteristicsExpression
    | characteristicsExpression '+' characteristicsExpression
    ;

callableBody
    : scope
    | BraceLeft specialization* BraceRight
    ;

specialization : specializationName+ specializationGenerator;

specializationName
    : 'body'
    | 'adjoint'
    | 'controlled'
    ;

specializationGenerator
    : 'auto' ';'
    | 'self' ';'
    | 'invert' ';'
    | 'distribute' ';'
    | 'intrinsic' ';'
    | providedSpecialization
    ;

providedSpecialization : specializationParameterTuple? scope;

specializationParameterTuple : '(' (specializationParameter (',' specializationParameter)*)? ')';

specializationParameter
    : Identifier
    | '...'
    ;

// Type

type
    : '_' # MissingType
    | '(' (type (',' type)* ','?)? ')' # TupleType
    | TypeParameter # TypeParameter
    | type '[' ']' # ArrayType
    | type ('->' | '=>') type characteristics? # CallableType
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
    | name=qualifiedName # UserDefinedType
    ;

// Statement

statement
    : expression ';' # ExpressionStatement
    | return='return' value=expression semicolon=';' # ReturnStatement
    | 'fail' expression ';' # FailStatement
    | let='let' binding=symbolBinding equals='=' value=expression semicolon=';' # LetStatement
    | 'mutable' symbolBinding '=' expression ';' # MutableStatement
    | 'set' symbolBinding '=' expression ';' # SetStatement
    | 'set' Identifier updateOperator expression ';' # SetUpdateStatement
    | 'set' Identifier 'w/=' expression '<-' expression ';' # SetWithStatement
    | if='if' condition=expression body=scope # IfStatement
    | 'elif' expression scope # ElifStatement
    | else='else' body=scope # ElseStatement
    | 'for' (forBinding | '(' forBinding ')') scope # ForStatement
    | 'while' expression scope # WhileStatement
    | 'repeat' scope # RepeatStatement
    | 'until' expression (';' | 'fixup' scope) # UntilStatement
    | 'within' scope # WithinStatement
    | 'apply' scope # ApplyStatement
    | ('use' | 'using') (qubitBinding | '(' qubitBinding ')') (';' | scope) # UseStatement
    | ('borrow' | 'borrowing') (qubitBinding | '(' qubitBinding ')') (';' | scope) # BorrowStatement
    ;

scope : openBrace=BraceLeft statements+=statement* closeBrace=BraceRight;

symbolBinding
    : '_' # DiscardSymbol
    | name=Identifier # SymbolName
    | openParen='(' (bindings+=symbolBinding (commas+=',' bindings+=symbolBinding)* ','?)? closeParen=')' # SymbolTuple
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
    : 'Qubit' '(' ')'
    | 'Qubit' '[' expression ']'
    | '(' (qubitInitializer (',' qubitInitializer)* ','?)? ')'
    ;

// Expression

expression
    : '_' # MissingExpression
    | name=qualifiedName ('<' (type (',' type)* ','?)? '>')? # IdentifierExpression
    | value=IntegerLiteral # IntegerExpression
    | BigIntegerLiteral # BigIntegerExpression
    | DoubleLiteral # DoubleExpression
    | DoubleQuote stringContent* StringDoubleQuote # StringExpression
    | DollarQuote interpStringContent* InterpDoubleQuote # InterpStringExpression
    | boolLiteral # BoolExpression
    | resultLiteral # ResultExpression
    | pauliLiteral # PauliExpression
    | openParen='(' (items+=expression (commas+=',' items+=expression)* commas+=','?)? closeParen=')' # TupleExpression
    | '[' (expression (',' expression)* ','?)? ']' # ArrayExpression
    | 'new' type '[' expression ']' # NewArrayExpression
    | expression ('::' Identifier | '[' expression ']') # ItemAccessExpression
    | expression '!' # UnwrapExpression
    | <assoc=right> 'Controlled' expression # ControlledExpression
    | <assoc=right> 'Adjoint' expression # AdjointExpression
    | expression '(' (expression (',' expression)* ','?)? ')' # CallExpression
    | <assoc=right> ('-' | 'not' | '~~~') expression # NegationExpression
    | <assoc=right> expression '^' expression # ExponentExpression
    | expression ('*' | '/' | '%') expression # MultiplyExpression
    | left=expression operator=('+' | '-') right=expression # AddExpression
    | expression ('>>>' | '<<<') expression # ShiftExpression
    | expression ('>' | '<' | '>=' | '<=') expression # CompareExpression
    | left=expression operator=('==' | '!=') right=expression # EqualsExpression
    | expression '&&&' expression # BitwiseAndExpression
    | expression '^^^' expression # BitwiseXorExpression
    | expression '|||' expression # BitwiseOrExpression
    | expression 'and' expression # AndExpression
    | expression 'or' expression # OrExpression
    | <assoc=right> expression '?' expression '|' expression # ConditionalExpression
    | expression '..' expression # RangeExpression
    | expression '...' # RightOpenRangeExpression
    | '...' expression # LeftOpenRangeExpression
    | '...' # OpenRangeExpression
    | record=expression with='w/' item=expression arrow='<-' value=expression # UpdateExpression
    ;

boolLiteral
    : 'false'
    | 'true'
    ;

resultLiteral
    : 'Zero'
    | 'One'
    ;

pauliLiteral
    : 'PauliI'
    | 'PauliX'
    | 'PauliY'
    | 'PauliZ'
    ;

stringContent
    : StringEscape
    | StringText
    ;

interpStringContent
    : InterpStringEscape
    | InterpBraceLeft expression BraceRight
    | InterpStringText
    ;
