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
      colon=':' returnType=type returnChar=characteristics?
      body=callableBody
    ;

typeParameterBinding : '<' (TypeParameter (',' TypeParameter)*)? '>';

parameterTuple : openParen='(' (parameters+=parameter (commas+=',' parameters+=parameter)*)? closeParen=')';

parameter
    : namedItem # NamedParameter
    | parameterTuple # TupledParameter
    ;

characteristics : is='is' charExp=characteristicsExpression;

characteristicsExpression
    : 'Adj' # AdjointCharacteristics
    | 'Ctl' # ControlledCharacteristics
    | openParen='(' charExp=characteristicsExpression closeParen=')' # CharacteristicGroup
    | left=characteristicsExpression '*' right=characteristicsExpression # IntersectCharacteristics
    | left=characteristicsExpression '+' right=characteristicsExpression # UnionCharacteristics
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
    | openParen='(' (items+=type (commas+=',' items+=type)* commas+=','?)? closeParen=')' # TupleType
    | typeParameter=TypeParameter # TypeParameter
    | itemType=type openBracket='[' closeBracket=']' # ArrayType
    | fromType=type arrow=('->' | '=>') toType=type character=characteristics? # CallableType
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
    | value=BigIntegerLiteral # BigIntegerExpression
    | value=DoubleLiteral # DoubleExpression
    | DoubleQuote stringContent* StringDoubleQuote # StringExpression
    | DollarQuote interpStringContent* InterpDoubleQuote # InterpStringExpression
    | boolLiteral # BoolExpression
    | resultLiteral # ResultExpression
    | pauliLiteral # PauliExpression
    | openParen='(' (items+=expression (commas+=',' items+=expression)* commas+=','?)? closeParen=')' # TupleExpression
    | '[' (expression (',' expression)* ','?)? ']' # ArrayExpression
    | 'new' type '[' expression ']' # NewArrayExpression
    | expression ('::' Identifier | '[' expression ']') # ItemAccessExpression
    | operand=expression operator='!' # UnwrapExpression
    | <assoc=right> functor='Controlled' operation=expression # ControlledExpression
    | <assoc=right> functor='Adjoint' operation=expression # AdjointExpression
    | expression '(' (expression (',' expression)* ','?)? ')' # CallExpression
    | <assoc=right> operator=('-' | 'not' | '~~~') operand=expression # NegationExpression
    | <assoc=right> left=expression operator='^' right=expression # ExponentExpression
    | left=expression operator=('*' | '/' | '%') right=expression # MultiplyExpression
    | left=expression operator=('+' | '-') right=expression # AddExpression
    | left=expression operator=('>>>' | '<<<') right=expression # ShiftExpression
    | left=expression operator=('>' | '<' | '>=' | '<=') right=expression # CompareExpression
    | left=expression operator=('==' | '!=') right=expression # EqualsExpression
    | left=expression operator='&&&' right=expression # BitwiseAndExpression
    | left=expression operator='^^^' right=expression # BitwiseXorExpression
    | left=expression operator='|||' right=expression # BitwiseOrExpression
    | left=expression operator='and' right=expression # AndExpression
    | left=expression operator='or' right=expression # OrExpression
    | <assoc=right> cond=expression question='?' ifTrue=expression pipe='|' ifFalse=expression # ConditionalExpression
    | left=expression ellipsis='..' right=expression # RangeExpression
    | left=expression ellipsis='...' # RightOpenRangeExpression
    | ellipsis='...' right=expression # LeftOpenRangeExpression
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
