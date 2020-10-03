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
    Range : QsNullable<Range>
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
    with
    member this.TryGetSimpleSet(simpleSet: OpProperty byref) =
        match this with
        | SimpleSet value -> simpleSet <- value; true
        | _ -> false
    member this.TryGetUnion(union: ('S * 'S) byref) =
        match this with
        | Union(setA , setB) -> union <- (setA, setB); true
        | _ -> false
    member this.TryGetIntersection(intersection: ('S * 'S) byref) =
        match this with
        | Intersection(setA , setB) -> intersection <- (setA, setB); true
        | _ -> false

type Characteristics = {
    Characteristics : CharacteristicsKind<Characteristics>
    Range : QsNullable<Range>
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
    with
    static member CreateUnitType() = UnitType
    static member CreateInt() = Int
    static member CreateBigInt() = BigInt
    static member CreateDouble() = Double
    static member CreateBool() = Bool
    static member CreateString() = String
    static member CreateQubit() = Qubit
    static member CreateResult() = Result
    static member CreatePauli() = Pauli
    static member CreateRange() = Range
    static member CreateMissingType() = MissingType
    static member CreateInvalidType() = InvalidType
    member this.TryGetArrayType(arrayType: 'Type byref) =
        match this with
        | ArrayType value -> arrayType <- value; true
        | _ -> false
    member this.TryGetTupleType(tupleType: ImmutableArray<'Type> byref) =
        match this with
        | TupleType value -> tupleType <- value; true
        | _ -> false
    member this.TryGetUserDefinedType(userDefinedType: 'UdtName byref) =
        match this with
        | UserDefinedType value -> userDefinedType <- value; true
        | _ -> false
    member this.TryGetTypeParameter(typeParameter: 'TParam byref) =
        match this with
        | TypeParameter value -> typeParameter <- value; true
        | _ -> false
    member this.TryGetOperation(operation: (('Type * 'Type) * 'Characteristics) byref) =
        match this with
        | Operation((typeA ,typeB), characteristics) -> operation <- ((typeA, typeB), characteristics); true
        | _ -> false
    member this.TryGetFunction(functionObject: ('Type * 'Type) byref) =
        match this with
        | Function(typeA ,typeB) -> functionObject <- (typeA ,typeB); true
        | _ -> false

type QsType = {
    Type : QsTypeKind<QsType, QsSymbol, QsSymbol, Characteristics>
    Range : QsNullable<Range>
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
    with
    static member CreateUnitValue() = UnitValue
    static member CreateMissingExpr() = MissingExpr
    static member CreateInvalidExpr() = InvalidExpr
    member this.TryGetIdentifier(identifier: ('Symbol * QsNullable<ImmutableArray<'Type>>) byref) =
        match this with
        | Identifier(symbol, typeParameters) -> identifier <- (symbol, typeParameters); true
        | _ -> false
    member this.TryGetValueTuple(valueTuple: ImmutableArray<'Expr> byref) =
        match this with
        | ValueTuple value -> valueTuple <- value; true
        | _ -> false
    member this.TryGetIntLiteral(intLiteral: int64 byref) =
        match this with
        | IntLiteral value -> intLiteral <- value; true
        | _ -> false
    member this.TryGetBigIntLiteral(bigIntLiteral: BigInteger byref) =
        match this with
        | BigIntLiteral value -> bigIntLiteral <- value; true
        | _ -> false
    member this.TryGetDoubleLiteral(doubleLiteral: double byref) =
        match this with
        | DoubleLiteral value -> doubleLiteral <- value; true
        | _ -> false
    member this.TryGetBoolLiteral(boolLiteral: bool byref) =
        match this with
        | BoolLiteral value -> boolLiteral <- value; true
        | _ -> false
    member this.TryGetStringLiteral(stringLiteral: (NonNullable<string> * ImmutableArray<'Expr>) byref) =
        match this with
        | StringLiteral(str, expressions) -> stringLiteral <- (str, expressions); true
        | _ -> false
    member this.TryGetResultLiteral(resultLiteral: QsResult byref) =
        match this with
        | ResultLiteral value -> resultLiteral <- value; true
        | _ -> false
    member this.TryGetPauliLiteral(pauliLiteral: QsPauli byref) =
        match this with
        | PauliLiteral value -> pauliLiteral <- value; true
        | _ -> false
    member this.TryGetRangeLiteral(rangeLiteral: ('Expr * 'Expr) byref) =
        match this with
        | RangeLiteral(expression1, expression2) -> rangeLiteral <- (expression1, expression2); true
        | _ -> false
    member this.TryGetNewArray(newArray: ('Type * 'Expr) byref) =
        match this with
        | NewArray(t, expression) -> newArray <- (t, expression); true
        | _ -> false
    member this.TryGetValueArray(valueArray: ImmutableArray<'Expr> byref) =
        match this with
        | ValueArray value -> valueArray <- value; true
        | _ -> false
    member this.TryGetArrayItem(arrayItem: ('Expr * 'Expr) byref) =
        match this with
        | ArrayItem(expression1, expression2) -> arrayItem <- (expression1, expression2); true
        | _ -> false
    member this.TryGetNamedItem(namedItem: ('Expr * 'Symbol) byref) =
        match this with
        | NamedItem(expression, symbol) -> namedItem <- (expression, symbol); true
        | _ -> false
    member this.TryGetNEG(neg: 'Expr byref) =
        match this with
        | NEG value -> neg <- value; true
        | _ -> false
    member this.TryGetNOT(not: 'Expr byref) =
        match this with
        | NOT value -> not <- value; true
        | _ -> false
    member this.TryGetBNOT(bnot: 'Expr byref) =
        match this with
        | BNOT value -> bnot <- value; true
        | _ -> false
    member this.TryGetADD(add: ('Expr * 'Expr) byref) =
        match this with
        | ADD(expression1, expression2) -> add <- (expression1, expression2); true
        | _ -> false
    member this.TryGetSUB(sub: ('Expr * 'Expr) byref) =
        match this with
        | SUB(expression1, expression2) -> sub <- (expression1, expression2); true
        | _ -> false
    member this.TryGetMUL(mul: ('Expr * 'Expr) byref) =
        match this with
        | MUL(expression1, expression2) -> mul <- (expression1, expression2); true
        | _ -> false
    member this.TryGetDIV(div: ('Expr * 'Expr) byref) =
        match this with
        | DIV(expression1, expression2) -> div <- (expression1, expression2); true
        | _ -> false
    member this.TryGetMOD(modValue: ('Expr * 'Expr) byref) =
        match this with
        | MOD(expression1, expression2) -> modValue <- (expression1, expression2); true
        | _ -> false
    member this.TryGetPOW(pow: ('Expr * 'Expr) byref) =
        match this with
        | POW(expression1, expression2) -> pow <- (expression1, expression2); true
        | _ -> false
    member this.TryGetEQ(eq: ('Expr * 'Expr) byref) =
        match this with
        | EQ(expression1, expression2) -> eq <- (expression1, expression2); true
        | _ -> false
    member this.TryGetNEQ(neq: ('Expr * 'Expr) byref) =
        match this with
        | NEQ(expression1, expression2) -> neq <- (expression1, expression2); true
        | _ -> false
    member this.TryGetLT(lt: ('Expr * 'Expr) byref) =
        match this with
        | LT(expression1, expression2) -> lt <- (expression1, expression2); true
        | _ -> false
    member this.TryGetLTE(lte: ('Expr * 'Expr) byref) =
        match this with
        | LTE(expression1, expression2) -> lte <- (expression1, expression2); true
        | _ -> false
    member this.TryGetGT(gt: ('Expr * 'Expr) byref) =
        match this with
        | GT(expression1, expression2) -> gt <- (expression1, expression2); true
        | _ -> false
    member this.TryGetGTE(gte: ('Expr * 'Expr) byref) =
        match this with
        | GTE(expression1, expression2) -> gte <- (expression1, expression2); true
        | _ -> false
    member this.TryGetAND(andValue: ('Expr * 'Expr) byref) =
        match this with
        | AND(expression1, expression2) -> andValue <- (expression1, expression2); true
        | _ -> false
    member this.TryGetOR(orValue: ('Expr * 'Expr) byref) =
        match this with
        | OR(expression1, expression2) -> orValue <- (expression1, expression2); true
        | _ -> false
    member this.TryGetBOR(bor: ('Expr * 'Expr) byref) =
        match this with
        | BOR(expression1, expression2) -> bor <- (expression1, expression2); true
        | _ -> false
    member this.TryGetBAND(band: ('Expr * 'Expr) byref) =
        match this with
        | BAND(expression1, expression2) -> band <- (expression1, expression2); true
        | _ -> false
    member this.TryGetBXOR(bxor: ('Expr * 'Expr) byref) =
        match this with
        | BXOR(expression1, expression2) -> bxor <- (expression1, expression2); true
        | _ -> false
    member this.TryGetLSHIFT(lshift: ('Expr * 'Expr) byref) =
        match this with
        | LSHIFT(expression1, expression2) -> lshift <- (expression1, expression2); true
        | _ -> false
    member this.TryGetRSHIFT(rshift: ('Expr * 'Expr) byref) =
        match this with
        | RSHIFT(expression1, expression2) -> rshift <- (expression1, expression2); true
        | _ -> false
    member this.TryGetCONDITIONAL(conditional: ('Expr * 'Expr * 'Expr) byref) =
        match this with
        | CONDITIONAL(expression1, expression2, expression3) -> conditional <- (expression1, expression2, expression3); true
        | _ -> false
    member this.TryGetCopyAndUpdate(copyAndUpdate: ('Expr * 'Expr * 'Expr) byref) =
        match this with
        | CopyAndUpdate(expression1, expression2, expression3) -> copyAndUpdate <- (expression1, expression2, expression3); true
        | _ -> false
    member this.TryGetUnwrapApplication(unwrapApplication: 'Expr byref) =
        match this with
        | UnwrapApplication value -> unwrapApplication <- value; true
        | _ -> false
    member this.TryGetAdjointApplication(adjointApplication: 'Expr byref) =
        match this with
        | AdjointApplication value -> adjointApplication <- value; true
        | _ -> false
    member this.TryGetControlledApplication(controlledApplication: 'Expr byref) =
        match this with
        | ControlledApplication value -> controlledApplication <- value; true
        | _ -> false
    member this.TryGetCallLikeExpression(callLikeExpression: ('Expr * 'Expr) byref) =
        match this with
        | CallLikeExpression(expression1, expression2) -> callLikeExpression <- (expression1, expression2); true
        | _ -> false

type QsExpression = {
    Expression : QsExpressionKind<QsExpression, QsSymbol, QsType>
    Range : QsNullable<Range>
} with interface ITuple


// Q# initializers

type QsInitializerKind<'Initializer, 'Expr> = 
| SingleQubitAllocation
| QubitRegisterAllocation of 'Expr
| QubitTupleAllocation of ImmutableArray<'Initializer>
| InvalidInitializer
    with
    member this.TryGetQubitRegisterAllocation(qubitRegisterAllocation: 'Expr byref) =
        match this with
        | QubitRegisterAllocation value -> qubitRegisterAllocation <- value; true
        | _ -> false
    member this.TryGetQubitTupleAllocation(qubitTupleAllocation: ImmutableArray<'Initializer> byref) =
        match this with
        | QubitTupleAllocation value -> qubitTupleAllocation <- value; true
        | _ -> false

type QsInitializer = {
    Initializer : QsInitializerKind<QsInitializer, QsExpression>
    Range : QsNullable<Range>
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
    Range : QsNullable<Range>
}


// Q# fragments

type QsTuple<'Item> = 
| QsTupleItem of 'Item
| QsTuple of ImmutableArray<QsTuple<'Item>> 
    with
    member this.TryGetQsTupleItem(qsTupleItem: 'Item byref) =
        match this with
        | QsTupleItem value -> qsTupleItem <- value; true
        | _ -> false
    member this.TryGetQsTuple(qsTuple: ImmutableArray<QsTuple<'Item>> byref) =
        match this with
        | QsTuple value -> qsTuple <- value; true
        | _ -> false

type CallableSignature = { 
    TypeParameters : ImmutableArray<QsSymbol>
    Argument : QsTuple<QsSymbol * QsType>
    ReturnType : QsType
    Characteristics : Characteristics
}

/// Defines where a global declaration may be accessed.
[<Struct>]
type AccessModifier =
    /// For callables and types, the default access modifier is public, which means the type or callable can be used
    /// from anywhere. For specializations, the default access modifier is the same as the parent callable.
    | DefaultAccess
    /// Internal access means that a type or callable may only be used from within the compilation unit in which it is
    /// declared.
    | Internal

/// Used to represent Q# keywords that may be attached to a declaration to modify its visibility or behavior.
[<Struct>]
type Modifiers = {
    /// Defines where a global declaration may be accessed.
    Access : AccessModifier
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
| WithinBlockIntro
| ApplyBlockIntro
| UsingBlockIntro               of QsSymbol * QsInitializer
| BorrowingBlockIntro           of QsSymbol * QsInitializer
| BodyDeclaration               of QsSpecializationGenerator
| AdjointDeclaration            of QsSpecializationGenerator
| ControlledDeclaration         of QsSpecializationGenerator
| ControlledAdjointDeclaration  of QsSpecializationGenerator
| OperationDeclaration          of Modifiers * QsSymbol * CallableSignature
| FunctionDeclaration           of Modifiers * QsSymbol * CallableSignature
| TypeDefinition                of Modifiers * QsSymbol * QsTuple<QsSymbol * QsType>
| DeclarationAttribute          of QsSymbol * QsExpression
| OpenDirective                 of QsSymbol * QsNullable<QsSymbol>
| NamespaceDeclaration          of QsSymbol
| InvalidFragment

with
    /// returns the error code for an invalid fragment of the given kind
    member this.ErrorCode = 
        match this with
        | ExpressionStatement          _ -> ErrorCode.InvalidExpressionStatement    
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
        | WithinBlockIntro             _ -> ErrorCode.InvalidWithinBlockIntro
        | ApplyBlockIntro              _ -> ErrorCode.InvalidApplyBlockIntro
        | UsingBlockIntro              _ -> ErrorCode.InvalidUsingBlockIntro             
        | BorrowingBlockIntro          _ -> ErrorCode.InvalidBorrowingBlockIntro         
        | BodyDeclaration              _ -> ErrorCode.InvalidBodyDeclaration             
        | AdjointDeclaration           _ -> ErrorCode.InvalidAdjointDeclaration          
        | ControlledDeclaration        _ -> ErrorCode.InvalidControlledDeclaration       
        | ControlledAdjointDeclaration _ -> ErrorCode.InvalidControlledAdjointDeclaration
        | OperationDeclaration         _ -> ErrorCode.InvalidOperationDeclaration        
        | FunctionDeclaration          _ -> ErrorCode.InvalidFunctionDeclaration       
        | DeclarationAttribute         _ -> ErrorCode.InvalidDeclarationAttribute
        | TypeDefinition               _ -> ErrorCode.InvalidTypeDefinition              
        | NamespaceDeclaration         _ -> ErrorCode.InvalidNamespaceDeclaration        
        | OpenDirective                _ -> ErrorCode.InvalidOpenDirective               
        | InvalidFragment              _ -> ErrorCode.UnknownCodeFragment                

    /// returns the error code for an invalid fragment ending on the given kind
    member this.InvalidEnding = 
        match this with
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
        | UntilSuccess                  (_, true)
        | WithinBlockIntro               _
        | ApplyBlockIntro                _
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
        | DeclarationAttribute           _ -> ErrorCode.UnexpectedFragmentDelimiter
        | NamespaceDeclaration           _ -> ErrorCode.ExpectingOpeningBracket
        | OpenDirective                  _ -> ErrorCode.ExpectingSemicolon     
        | InvalidFragment                _ -> ErrorCode.UnexpectedFragmentDelimiter

           
type QsFragment = {
    Kind : QsFragmentKind
    Range : Range
    Diagnostics : ImmutableArray<QsCompilerDiagnostic>
    Text : NonNullable<string>
}
