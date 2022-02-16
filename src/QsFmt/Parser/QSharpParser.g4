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
    | callableDeclaration # CallableElement
    ;

// Open Directive

openDirective : open='open' openName=qualifiedName (as='as' asName=qualifiedName)? semicolon=';';

// Declaration

attribute : at='@' expr=expression;

access : 'internal';

declarationPrefix : attributes+=attribute* access?;

// Type Declaration

typeDeclaration
    : prefix=declarationPrefix keyword='newtype' declared=Identifier equals='=' underlying=underlyingType semicolon=';'
    ;

underlyingType
    : typeDeclarationTuple # TupleUnderlyingType
    | type # UnnamedTypeItem
    ;

typeDeclarationTuple : openParen='(' (items+=typeTupleItem (commas+=',' items+=typeTupleItem)*)? closeParen=')';

typeTupleItem
    : namedItem # NamedTypeItem
    | underlyingType # UnderlyingTypeItem
    ;

namedItem : name=Identifier colon=':' itemType=type;

// Callable Declaration

callableDeclaration
    : prefix=declarationPrefix keyword=('function' | 'operation')
      name=Identifier typeParameters=typeParameterBinding? tuple=parameterTuple
      colon=':' returnType=type returnChar=characteristics?
      body=callableBody
    ;

typeParameterBinding
    : openBracket='<' (parameters+=TypeParameter (commas+=',' parameters+=TypeParameter)*)? closeBracket='>';

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
    : block=scope # CallableStatements
    | openBrace=BraceLeft specializations+=specialization* closeBrace=BraceRight # CallableSpecializations
    ;

specialization : names+=specializationName+ generator=specializationGenerator;

specializationName
    : 'body'
    | 'adjoint'
    | 'controlled'
    ;

specializationGenerator
    : auto='auto' semicolon=';' # AutoGenerator
    | self='self' semicolon=';' # SelfGenerator
    | invert='invert' semicolon=';' # InvertGenerator
    | distribute='distribute' semicolon=';' # DistributeGenerator
    | intrinsic='intrinsic' semicolon=';' # IntrinsicGenerator
    | provided=providedSpecialization # ProvidedGenerator
    ;

providedSpecialization : parameters=specializationParameterTuple? block=scope;

specializationParameterTuple
    : openParen='(' (parameters+=specializationParameter (commas+=',' parameters+=specializationParameter)*)? closeParen=')';

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
    : value=expression semicolon=';' # ExpressionStatement
    | return='return' value=expression semicolon=';' # ReturnStatement
    | fail='fail' value=expression semicolon=';' # FailStatement
    | let='let' binding=symbolBinding equals='=' value=expression semicolon=';' # LetStatement
    | mutable='mutable' binding=symbolBinding equals='=' value=expression semicolon=';' # MutableStatement
    | set='set' binding=symbolBinding equals='=' value=expression semicolon=';' # SetStatement
    | set='set' name=Identifier operator=updateOperator value=expression semicolon=';' # UpdateStatement
    | set='set' name=Identifier with='w/=' index=expression arrow='<-' value=expression semicolon=';' # UpdateWithStatement
    | if='if' condition=expression body=scope # IfStatement
    | elif='elif' condition=expression body=scope # ElifStatement
    | else='else' body=scope # ElseStatement
    | for='for' (binding=forBinding | openParen='(' binding=forBinding closeParen=')') body=scope # ForStatement
    | while='while' condition=expression body=scope # WhileStatement
    | repeat='repeat' body=scope # RepeatStatement
    | until='until' condition=expression (semicolon=';' | fixup='fixup' body=scope) # UntilStatement
    | within='within' body=scope # WithinStatement
    | apply='apply' body=scope # ApplyStatement
    | keyword=('use' | 'using' | 'borrow' | 'borrowing') (binding=qubitBinding | openParen='(' binding=qubitBinding closeParen=')') (body=scope | semicolon=';') # QubitDeclaration
    ;

scope : openBrace=BraceLeft statements+=statement* closeBrace=BraceRight;

symbolBinding
    : discard='_' # DiscardSymbol
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

forBinding : binding=symbolBinding in='in' value=expression;

qubitBinding : binding=symbolBinding equals='=' value=qubitInitializer;

qubitInitializer
    : qubit='Qubit' openParen='(' closeParen=')' # SingleQubit
    | qubit='Qubit' openBracket='[' length=expression closeBracket=']' # QubitArray
    | openParen='(' (initializers+=qubitInitializer (commas+=',' initializers+=qubitInitializer)* ','?)? closeParen=')' # QubitTuple
    ;

// Expression

expression
    : '_' # MissingExpression
    | name=qualifiedName types=typeTuple? # IdentifierExpression
    | value=IntegerLiteral # IntegerExpression
    | value=BigIntegerLiteral # BigIntegerExpression
    | value=DoubleLiteral # DoubleExpression
    | DoubleQuote stringContent* StringDoubleQuote # StringExpression
    | openQuote=DollarQuote content+=interpStringContent* closeQuote=InterpDoubleQuote # InterpStringExpression
    | value=boolLiteral # BoolExpression
    | value=resultLiteral # ResultExpression
    | value=pauliLiteral # PauliExpression
    | openParen='(' (items+=expression (commas+=',' items+=expression)* commas+=','?)? closeParen=')' # TupleExpression
    | openBracket='[' (items+=expression (commas+=',' items+=expression)* commas+=','?)? closeBracket=']' # ArrayExpression
    | openBracket='[' value=expression comma=',' size=sizeKey equals='=' length=expression closeBracket=']' # SizedArrayExpression
    | new='new' itemType=type openBracket='[' length=expression closeBracket=']' # NewArrayExpression
    | record=expression colon='::' name=Identifier # NamedItemAccessExpression
    | array=expression openBracket='[' index=expression closeBracket=']' # ArrayAccessExpression
    | operand=expression operator='!' # UnwrapExpression
    | <assoc=right> functor='Controlled' operation=expression # ControlledExpression
    | <assoc=right> functor='Adjoint' operation=expression # AdjointExpression
    | callable=expression openParen='(' (arguments+=expression (commas+=',' arguments+=expression)* commas+=','?)? closeParen=')' # CallExpression
    | <assoc=right> operator=('!' | '+' | '-' | 'not' | '~~~') operand=expression # PrefixOpExpression
    | <assoc=right> left=expression operator='^' right=expression # ExponentExpression
    | left=expression operator=('*' | '/' | '%') right=expression # MultiplyExpression
    | left=expression operator=('+' | '-') right=expression # AddExpression
    | left=expression operator=('>>>' | '<<<') right=expression # ShiftExpression
    | left=expression operator=('>' | '<' | '>=' | '<=') right=expression # CompareExpression
    | left=expression operator=('==' | '!=') right=expression # EqualsExpression
    | left=expression operator='&&&' right=expression # BitwiseAndExpression
    | left=expression operator='^^^' right=expression # BitwiseXorExpression
    | left=expression operator='|||' right=expression # BitwiseOrExpression
    | left=expression operator=('&&' | 'and') right=expression # AndExpression
    | left=expression operator=('||' | 'or') right=expression # OrExpression
    | <assoc=right> cond=expression question='?' ifTrue=expression pipe='|' ifFalse=expression # ConditionalExpression
    | left=expression ellipsis='..' right=expression # RangeExpression
    | left=expression ellipsis='...' # RightOpenRangeExpression
    | ellipsis='...' right=expression # LeftOpenRangeExpression
    | '...' # OpenRangeExpression
    | record=expression with='w/' item=expression arrow='<-' value=expression # UpdateExpression
    ;

sizeKey : terminal=Identifier {_localctx.terminal.Text == "size"}?;

typeTuple : openBracket='<' (typeArgs+=type (commas+=',' typeArgs+=type)* commas+=','?)? closeBracket='>';

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
    : InterpStringEscape # InterpStringEscapeContent
    | openBrace=InterpBraceLeft exp=expression closeBrace=BraceRight # InterpExpressionContent
    | InterpStringText # InterpTextContent
    ;
