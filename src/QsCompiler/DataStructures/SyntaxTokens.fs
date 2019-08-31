// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxTokens 

open System.Collections.Immutable
open System.Numerics
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.DataTypes

type ITuple = interface end // marker interface used for types on which tuple matching can be done


// Q# literals

type QsFunctor = 
| Adjoint
| Controlled

type QsResult = 
| Zero
| One

type QsPauli = 
| PauliX
| PauliY
| PauliZ
| PauliI


// Q# symbols

type QsSymbolKind<'Symbol> = 
| Symbol of NonNullable<string> // let's make the distinction for things that *have* to be an unqualified symbol
| QualifiedSymbol of NonNullable<string> * NonNullable<string>
| SymbolTuple of ImmutableArray<'Symbol> // for bindings
| OmittedSymbols // used for the arguments of the original method omitted upon functor gen declaration
| MissingSymbol // used to allow destructs of the form let (_,a) = ...
| InvalidSymbol

type QsSymbol = {
    Symbol : QsSymbolKind<QsSymbol>
    Range : QsRangeInfo
} // not an ITuple because currently, empty symbol tuples are used if no arguments are given to functor generators


// Q# types

type OpProperty =
| Adjointable
| Controllable

type CharacteristicsKind<'S> = 
| EmptySet
| SimpleSet of OpProperty // each set containing a single OpProperty is associated with the corresponding OpProperty
| Union of 'S * 'S
| Intersection of 'S * 'S
| InvalidSetExpr

type Characteristics = {
    Characteristics : CharacteristicsKind<Characteristics>
    Range : QsRangeInfo
}

type QsTypeKind<'Type,'UdtName,'TParam, 'Characteristics> = 
// Note: while templates need to be part of the type system, they cannot be identified at parsing time
| UnitType
| Int
| BigInt
| Double
| Bool
| String
| Qubit
| Result
| Pauli
| Range 
| ArrayType of 'Type
| TupleType of ImmutableArray<'Type>
| UserDefinedType of 'UdtName
| TypeParameter of 'TParam
| Operation of ('Type * 'Type) * 'Characteristics
| Function of 'Type * 'Type
| MissingType // used (only!) upon determining the type of expressions (for MissingExpr)
| InvalidType // to be used e.g. for parsing errors

type QsType = {
    Type : QsTypeKind<QsType, QsSymbol, QsSymbol, Characteristics>
    Range : QsRangeInfo
} with interface ITuple


// Q# expressions

type QsExpressionKind<'Expr, 'Symbol, 'Type> =
| UnitValue
| Identifier            of 'Symbol * QsNullable<ImmutableArray<'Type>> // the immutable array are the (optional) type parameters
| ValueTuple            of ImmutableArray<'Expr>
| IntLiteral            of int64
| BigIntLiteral         of BigInteger
| DoubleLiteral         of double
| BoolLiteral           of bool
| StringLiteral         of NonNullable<string> * ImmutableArray<'Expr> // used for both string and interpolated strings 
| ResultLiteral         of QsResult
| PauliLiteral          of QsPauli
| RangeLiteral          of 'Expr * 'Expr
| NewArray              of 'Type * 'Expr
| ValueArray            of ImmutableArray<'Expr>
| ArrayItem             of 'Expr * 'Expr // used for both array items and array slices
| NamedItem             of 'Expr * 'Symbol
| NEG                   of 'Expr       
| NOT                   of 'Expr 
| BNOT                  of 'Expr
| ADD                   of 'Expr * 'Expr
| SUB                   of 'Expr * 'Expr
| MUL                   of 'Expr * 'Expr
| DIV                   of 'Expr * 'Expr
| MOD                   of 'Expr * 'Expr          
| POW                   of 'Expr * 'Expr          
| EQ                    of 'Expr * 'Expr          
| NEQ                   of 'Expr * 'Expr          
| LT                    of 'Expr * 'Expr          
| LTE                   of 'Expr * 'Expr          
| GT                    of 'Expr * 'Expr          
| GTE                   of 'Expr * 'Expr          
| AND                   of 'Expr * 'Expr          
| OR                    of 'Expr * 'Expr          
| BOR                   of 'Expr * 'Expr
| BAND                  of 'Expr * 'Expr
| BXOR                  of 'Expr * 'Expr  
| LSHIFT                of 'Expr * 'Expr
| RSHIFT                of 'Expr * 'Expr
| CONDITIONAL           of 'Expr * 'Expr * 'Expr
| CopyAndUpdate         of 'Expr * 'Expr * 'Expr
| UnwrapApplication     of 'Expr // casts an expression of user defined type to its underlying type
| AdjointApplication    of 'Expr               
| ControlledApplication of 'Expr
| CallLikeExpression    of 'Expr * 'Expr
| MissingExpr // for partial application
| InvalidExpr

type QsExpression = {
    Expression : QsExpressionKind<QsExpression, QsSymbol, QsType>
    Range : QsRangeInfo
} with interface ITuple


// Q# initializers

type QsInitializerKind<'Initializer, 'Expr> = 
| SingleQubitAllocation
| QubitRegisterAllocation of 'Expr
| QubitTupleAllocation of ImmutableArray<'Initializer>
| InvalidInitializer

type QsInitializer = {
    Initializer : QsInitializerKind<QsInitializer, QsExpression>
    Range : QsRangeInfo
} with interface ITuple


// Q# functor generators

type QsGeneratorDirective = 
| SelfInverse
| Invert
| Distribute
| InvalidGenerator

type QsSpecializationGeneratorKind<'Symbol> = 
| Intrinsic
| AutoGenerated
| FunctorGenerationDirective of QsGeneratorDirective
| UserDefinedImplementation of 'Symbol

type QsSpecializationGenerator = {
    TypeArguments : QsNullable<ImmutableArray<QsType>> 
    Generator : QsSpecializationGeneratorKind<QsSymbol>
    Range : QsRangeInfo
}


// Q# fragments

type QsTuple<'Item> = 
| QsTupleItem of 'Item
| QsTuple of ImmutableArray<QsTuple<'Item>> 

type CallableSignature = { 
    TypeParameters : ImmutableArray<QsSymbol>
    Argument : QsTuple<QsSymbol * QsType>
    ReturnType : QsType
    Characteristics : Characteristics
}

type QsFragmentKind = 
| ExpressionStatement           of QsExpression
| ReturnStatement               of QsExpression             
| FailStatement                 of QsExpression
| ImmutableBinding              of QsSymbol * QsExpression // QsSymbol can be a symbol tuple
| MutableBinding                of QsSymbol * QsExpression // QsSymbol can be a symbol tuple
| ValueUpdate                   of QsExpression * QsExpression
| IfClause                      of QsExpression             
| ElifClause                    of QsExpression
| ElseClause                          
| ForLoopIntro                  of QsSymbol * QsExpression // QsSymbol can be a symbol tuple
| WhileLoopIntro                of QsExpression
| RepeatIntro                   
| UntilSuccess                  of QsExpression * bool // true if a fixup is included
| UsingBlockIntro               of QsSymbol * QsInitializer
| BorrowingBlockIntro           of QsSymbol * QsInitializer
| BodyDeclaration               of QsSpecializationGenerator
| AdjointDeclaration            of QsSpecializationGenerator
| ControlledDeclaration         of QsSpecializationGenerator
| ControlledAdjointDeclaration  of QsSpecializationGenerator
| AttributeDeclaration          of QsExpression * QsExpression
| OperationDeclaration          of QsSymbol * CallableSignature
| FunctionDeclaration           of QsSymbol * CallableSignature
| TypeDefinition                of QsSymbol * QsTuple<QsSymbol * QsType>
| OpenDirective                 of QsSymbol * QsNullable<QsSymbol>
| NamespaceDeclaration          of QsSymbol
| InvalidFragment

with
    /// returns the error code for an invalid fragment of the given kind
    member this.ErrorCode = 
        match this with
        | ExpressionStatement          _ -> ErrorCode.InvalidExpressionStatement    
        | AttributeDeclaration         _ -> ErrorCode.InvalidAttribute
        | ReturnStatement              _ -> ErrorCode.InvalidReturnStatement             
        | FailStatement                _ -> ErrorCode.InvalidFailStatement               
        | ImmutableBinding             _ -> ErrorCode.InvalidImmutableBinding                 
        | MutableBinding               _ -> ErrorCode.InvalidMutableBinding              
        | ValueUpdate                  _ -> ErrorCode.InvalidValueUpdate                 
        | IfClause                     _ -> ErrorCode.InvalidIfClause                    
        | ElifClause                   _ -> ErrorCode.InvalidElifClause                  
        | ElseClause                   _ -> ErrorCode.InvalidElseClause                  
        | ForLoopIntro                 _ -> ErrorCode.InvalidForLoopIntro
        | WhileLoopIntro               _ -> ErrorCode.InvalidWhileLoopIntro
        | RepeatIntro                  _ -> ErrorCode.InvalidRepeatIntro                 
        | UntilSuccess                 _ -> ErrorCode.InvalidUntilClause                 
        | UsingBlockIntro              _ -> ErrorCode.InvalidUsingBlockIntro             
        | BorrowingBlockIntro          _ -> ErrorCode.InvalidBorrowingBlockIntro         
        | BodyDeclaration              _ -> ErrorCode.InvalidBodyDeclaration             
        | AdjointDeclaration           _ -> ErrorCode.InvalidAdjointDeclaration          
        | ControlledDeclaration        _ -> ErrorCode.InvalidControlledDeclaration       
        | ControlledAdjointDeclaration _ -> ErrorCode.InvalidControlledAdjointDeclaration
        | OperationDeclaration         _ -> ErrorCode.InvalidOperationDeclaration        
        | FunctionDeclaration          _ -> ErrorCode.InvalidFunctionDeclaration         
        | TypeDefinition               _ -> ErrorCode.InvalidTypeDefinition              
        | NamespaceDeclaration         _ -> ErrorCode.InvalidNamespaceDeclaration        
        | OpenDirective                _ -> ErrorCode.InvalidOpenDirective               
        | InvalidFragment              _ -> ErrorCode.UnknownCodeFragment                

    /// returns the error code for an invalid fragment ending on the given kind
    member this.InvalidEnding = 
        match this with
        | AttributeDeclaration           _ -> ErrorCode.UnexpectedFragmentDelimiter
        | ExpressionStatement            _ 
        | ReturnStatement                _ 
        | FailStatement                  _ 
        | UntilSuccess                  (_, false)
        | ImmutableBinding               _ 
        | MutableBinding                 _ 
        | ValueUpdate                    _ -> ErrorCode.ExpectingSemicolon     
        | IfClause                       _  
        | ElifClause                     _  
        | ElseClause                     _  
        | ForLoopIntro                   _ 
        | WhileLoopIntro                 _
        | RepeatIntro                    _
        | UntilSuccess                  (_,true)
        | UsingBlockIntro                _  
        | BorrowingBlockIntro            _ -> ErrorCode.ExpectingOpeningBracket
        | BodyDeclaration              gen
        | AdjointDeclaration           gen
        | ControlledDeclaration        gen  
        | ControlledAdjointDeclaration gen -> 
            match gen.Generator with   
            | UserDefinedImplementation  _ -> ErrorCode.ExpectingOpeningBracket
            | _                            -> ErrorCode.ExpectingSemicolon     
        | OperationDeclaration           _  
        | FunctionDeclaration            _ -> ErrorCode.ExpectingOpeningBracket
        | TypeDefinition                 _ -> ErrorCode.ExpectingSemicolon     
        | NamespaceDeclaration           _ -> ErrorCode.ExpectingOpeningBracket
        | OpenDirective                  _ -> ErrorCode.ExpectingSemicolon     
        | InvalidFragment                _ -> ErrorCode.UnexpectedFragmentDelimiter

           
type QsFragment = {
    Kind : QsFragmentKind
    Range : QsPositionInfo * QsPositionInfo
    Diagnostics : ImmutableArray<QsCompilerDiagnostic>
    Text : NonNullable<string>
}

        


