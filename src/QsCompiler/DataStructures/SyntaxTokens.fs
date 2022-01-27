﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxTree

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes

// marker interface used for types on which tuple matching can be done
type ITuple =
    interface
    end

type SymbolTuple =
    /// indicates in invalid variable name
    | InvalidItem
    /// indicates a valid Q# variable name
    | VariableName of string
    /// indicates a tuple of Q# variable names or (nested) tuples of variable names
    | VariableNameTuple of ImmutableArray<SymbolTuple>
    /// indicates a place holder for a Q# variable that won't be used after the symbol tuple is bound to a value
    | DiscardedItem
    interface ITuple

/// used to represent information on typed expressions generated and/or tracked during compilation
type InferredExpressionInformation =
    {
        /// whether or not the value of this expression can be modified (true if it can)
        IsMutable: bool
        /// indicates whether the annotated expression directly or indirectly depends on an operation call within the surrounding implementation block
        /// -> it will be set to false for variables declared within the argument tuple
        /// -> using and borrowing are *not* considered to implicitly invoke a call to an operation, and are thus *not* considered to have a quantum dependency.
        HasLocalQuantumDependency: bool
    }
            
type LocalVariableDeclaration<'Name, 'Type> =
    {
        /// the name of the declared variable
        VariableName: 'Name
        /// the fully resolved type of the declared variable
        Type: 'Type
        /// contains information generated and/or tracked by the compiler
        /// -> in particular, contains the information about whether or not the symbol may be re-bound
        InferredInformation: InferredExpressionInformation
        /// Denotes the position where the variable is declared
        /// relative to the position of the specialization declaration within which the variable is declared.
        /// If the Position is Null, then the variable is not declared within a specialization (but belongs to a callable or type declaration).
        Position: QsNullable<Position>
        /// Denotes the range of the variable name relative to the position of the variable declaration.
        Range: Range
    }

module LocalVariableDeclaration =
    let New isMutable ((pos, range), vName: 'Name, t, hasLocalQuantumDependency) =
        {
            VariableName = vName
            Type = t
            InferredInformation = {IsMutable = isMutable; HasLocalQuantumDependency = hasLocalQuantumDependency}
            Position = pos
            Range = range
        }
    

namespace Microsoft.Quantum.QsCompiler.SyntaxTokens

#nowarn "44" // AccessModifier is deprecated.

open System
open System.Collections.Immutable
open System.Numerics
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTree


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
    /// For symbols that *have* to be an unqualified.
    | Symbol of string
    /// For qualified symbols.
    | QualifiedSymbol of string * string
    /// For bindings.
    | SymbolTuple of ImmutableArray<'Symbol>
    /// Used for the arguments of the original method omitted upon functor gen declaration.
    | OmittedSymbols
    /// Used to allow destructs of the form let (_,a) = ...
    | MissingSymbol
    | InvalidSymbol

/// A collection of one or more symbol bindings for a tuple.
[<CustomComparison>]
[<CustomEquality>]
type QsSymbol =
    // not an ITuple because currently, empty symbol tuples are used if no arguments are given to functor generators
    {
        /// The symbol bindings.
        Symbol: QsSymbolKind<QsSymbol>

        /// <summary>
        /// The source code range of the symbol. This is ignored when comparing <see cref="QsSymbol"/>s.
        /// </summary>
        Range: QsNullable<Range>
    }

    override symbol1.Equals symbol2 =
        match symbol2 with
        | :? QsSymbol as symbol2 -> symbol1.Symbol = symbol2.Symbol
        | _ -> false

    override symbol.GetHashCode() = hash symbol.Symbol

    interface IComparable with
        member symbol1.CompareTo symbol2 =
            match symbol2 with
            | :? QsSymbol as symbol2 -> compare symbol1.Symbol symbol2.Symbol
            | _ -> ArgumentException "Types are different." |> raise


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

type Characteristics = { Characteristics: CharacteristicsKind<Characteristics>; Range: QsNullable<Range> }

type QsTypeKind<'Type, 'UdtName, 'TParam, 'Characteristics> =
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

type QsType =
    {
        Type: QsTypeKind<QsType, QsSymbol, QsSymbol, Characteristics>
        Range: QsNullable<Range>
    }
    interface ITuple


// Q# expressions

/// Represents whether a lambda is a function or operation.
type LambdaKind =
    /// The lambda is a function.
    | Function
    /// The lambda is an operation.
    | Operation

/// A lambda expression.
type Lambda<'Expr, 'Type> =
    private
        {
            kind: LambdaKind
            param: SymbolTuple // FIXME: ArgumentTuple: QsTuple<LocalVariableDeclaration<QsLocalSymbol>>
            body: 'Expr
            variableDeclarations: ImmutableArray<LocalVariableDeclaration<string, 'Type>>
        }

    /// Represents whether a lambda is a function or operation.
    member lambda.Kind = lambda.kind

    /// The symbol tuple for the lambda's parameter.
    member lambda.Param = lambda.param // FIXME: CHANGE TO ARGTUPLE

    /// The body of the lambda.
    member lambda.Body = lambda.body

    member lambda.ArgumentDeclarations = lambda.variableDeclarations

module Lambda =
    /// Creates a lambda expression.
    [<CompiledName "Create">]
    let create kind param body varDecl =
        {
            kind = kind
            param = param
            body = body
            variableDeclarations = varDecl
        }

    let createUnchecked kind (argTuple : QsSymbol) body =
        let varDecl = ImmutableArray.CreateBuilder()
        let rec mapSymbol (sym : QsSymbol) =
            match sym.Symbol with
            | Symbol name ->
                varDecl.Add {
                    VariableName = name;
                    Type = { Type = MissingType; Range = Null };
                    InferredInformation = { IsMutable = false; HasLocalQuantumDependency = false }; // fixme was true before
                    Position = Null;
                    Range = sym.Range.ValueOrApply (fun _ -> failwith "should never occur") // fixme: handling
                }
                VariableName name
            | SymbolTuple syms -> syms |> Seq.map mapSymbol |> ImmutableArray.CreateRange |> VariableNameTuple // TODO: CHECK IF EMPTY TUPLES ARE FINE
            | QualifiedSymbol _ // FIXME: RELIES ON THERE HAVING BEEN A PROPER ERROR ALREADY...
            | OmittedSymbols
            | MissingSymbol // TODO: this could be considered valid
            | InvalidSymbol -> InvalidItem
        let param = mapSymbol argTuple
        varDecl.ToImmutable() |> create kind param body

type QsExpressionKind<'Expr, 'Symbol, 'Type> =
    | UnitValue
    /// The immutable array is the (optional) type parameters.
    | Identifier of 'Symbol * QsNullable<ImmutableArray<'Type>>
    | ValueTuple of ImmutableArray<'Expr>
    | IntLiteral of int64
    | BigIntLiteral of BigInteger
    | DoubleLiteral of double
    | BoolLiteral of bool
    /// Used for both string and interpolated strings.
    | StringLiteral of string * ImmutableArray<'Expr>
    | ResultLiteral of QsResult
    | PauliLiteral of QsPauli
    | RangeLiteral of 'Expr * 'Expr
    | NewArray of 'Type * 'Expr
    | ValueArray of ImmutableArray<'Expr>
    /// Used for both array items and array slices.
    | ArrayItem of 'Expr * 'Expr
    | NamedItem of 'Expr * 'Symbol
    | NEG of 'Expr
    | NOT of 'Expr
    | BNOT of 'Expr
    | ADD of 'Expr * 'Expr
    | SUB of 'Expr * 'Expr
    | MUL of 'Expr * 'Expr
    | DIV of 'Expr * 'Expr
    | MOD of 'Expr * 'Expr
    | POW of 'Expr * 'Expr
    | EQ of 'Expr * 'Expr
    | NEQ of 'Expr * 'Expr
    | LT of 'Expr * 'Expr
    | LTE of 'Expr * 'Expr
    | GT of 'Expr * 'Expr
    | GTE of 'Expr * 'Expr
    | AND of 'Expr * 'Expr
    | OR of 'Expr * 'Expr
    | BOR of 'Expr * 'Expr
    | BAND of 'Expr * 'Expr
    | BXOR of 'Expr * 'Expr
    | LSHIFT of 'Expr * 'Expr
    | RSHIFT of 'Expr * 'Expr
    | CONDITIONAL of 'Expr * 'Expr * 'Expr
    | CopyAndUpdate of 'Expr * 'Expr * 'Expr
    /// Casts an expression of user defined type to its underlying type.
    | UnwrapApplication of 'Expr
    | AdjointApplication of 'Expr
    | ControlledApplication of 'Expr
    | CallLikeExpression of 'Expr * 'Expr
    /// For partial application.
    | MissingExpr
    | InvalidExpr
    | SizedArray of value: 'Expr * size: 'Expr
    | Lambda of Lambda<'Expr, 'Type>

type QsExpression =
    {
        Expression: QsExpressionKind<QsExpression, QsSymbol, QsType>
        Range: QsNullable<Range>
    }
    interface ITuple


// Q# initializers

type QsInitializerKind<'Initializer, 'Expr> =
    | SingleQubitAllocation
    | QubitRegisterAllocation of 'Expr
    | QubitTupleAllocation of ImmutableArray<'Initializer>
    | InvalidInitializer

type QsInitializer =
    {
        Initializer: QsInitializerKind<QsInitializer, QsExpression>
        Range: QsNullable<Range>
    }
    interface ITuple


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

type QsSpecializationGenerator =
    {
        TypeArguments: QsNullable<ImmutableArray<QsType>>
        Generator: QsSpecializationGeneratorKind<QsSymbol>
        Range: QsNullable<Range>
    }


// Q# fragments

type QsTuple<'Item> =
    | QsTupleItem of 'Item
    | QsTuple of ImmutableArray<QsTuple<'Item>>

type CallableSignature =
    {
        TypeParameters: ImmutableArray<QsSymbol>
        Argument: QsTuple<QsSymbol * QsType>
        ReturnType: QsType
        Characteristics: Characteristics
    }

/// Defines where a global declaration may be accessed.
[<Obsolete "Use Access instead.">]
[<Struct>]
type AccessModifier =
    /// For callables and types, the default access modifier is public, which means the type or callable can be used
    /// from anywhere. For specializations, the default access modifier is the same as the parent callable.
    | DefaultAccess

    /// Internal access means that a type or callable may only be used from within the compilation unit in which it is
    /// declared.
    | Internal

/// Used to represent Q# keywords that may be attached to a declaration to modify its visibility or behavior.
[<Obsolete "Use Access instead.">]
[<Struct>]
type Modifiers =
    {
        /// Defines where a global declaration may be accessed.
        Access: AccessModifier
    }

/// The accessibility of a symbol that limits where the symbol can be used from.
type Access =
    /// The symbol can be seen used within the compilation unit or assembly in which it is declared.
    | Internal

    /// The symbol can be used from anywhere.
    | Public

/// The relative proximity of one code location to another in terms that are relevant to symbol accessibility.
type Proximity =
    /// The code locations are in the same compilation unit or assembly.
    | SameAssembly

    /// The code locations are in different compilation units or assemblies.
    | OtherAssembly

module Access =
    /// <summary>
    /// Returns true if symbols with a given accessibility are accessible from <paramref name="proximity"/>.
    /// </summary>
    [<CompiledName "IsAccessibleFrom">]
    let isAccessibleFrom proximity =
        function
        | Internal -> proximity = SameAssembly
        | Public -> true

type Access with
    /// <summary>
    /// Returns true if symbols with this accessibility are accessible from <paramref name="proximity"/>.
    /// </summary>
    member access.IsAccessibleFrom proximity =
        access |> Access.isAccessibleFrom proximity

module AccessModifier =
    [<CompiledName "ToAccess">]
    [<Obsolete "Use Access instead.">]
    let toAccess defaultAccess =
        function
        | DefaultAccess -> defaultAccess
        | AccessModifier.Internal -> Internal

    [<CompiledName "FromAccess">]
    [<Obsolete "Use Access instead.">]
    let ofAccess =
        function
        | Public -> DefaultAccess
        | Internal -> AccessModifier.Internal

/// A callable declaration.
type CallableDeclaration =
    private
        {
            name: QsSymbol
            access: Access QsNullable
            signature: CallableSignature
        }

    static member Create(name, access, signature) =
        {
            name = name
            access = access
            signature = signature
        }

    /// The name of the callable.
    member callable.Name = callable.name

    /// The accessibility of the callable, or Null if the callable has the default accessibility.
    member callable.Access = callable.access

    /// The signature of the callable.
    member callable.Signature = callable.signature

/// A type definition.
type TypeDefinition =
    private
        {
            name: QsSymbol
            access: Access QsNullable
            underlyingType: (QsSymbol * QsType) QsTuple
        }

    static member Create(name, access, underlyingType) =
        {
            name = name
            access = access
            underlyingType = underlyingType
        }

    /// The name of the type.
    member typeDef.Name = typeDef.name

    /// The accessibility of the type, or Null if the type has the default accessibility.
    member typeDef.Access = typeDef.access

    /// The type's underlying type.
    member typeDef.UnderlyingType = typeDef.underlyingType

type QsFragmentKind =
    | ExpressionStatement of QsExpression
    | ReturnStatement of QsExpression
    | FailStatement of QsExpression
    | ImmutableBinding of QsSymbol * QsExpression // QsSymbol can be a symbol tuple
    | MutableBinding of QsSymbol * QsExpression // QsSymbol can be a symbol tuple
    | ValueUpdate of QsExpression * QsExpression
    | IfClause of QsExpression
    | ElifClause of QsExpression
    | ElseClause
    | ForLoopIntro of QsSymbol * QsExpression // QsSymbol can be a symbol tuple
    | WhileLoopIntro of QsExpression
    | RepeatIntro
    | UntilSuccess of QsExpression * bool // true if a fixup is included
    | WithinBlockIntro
    | ApplyBlockIntro
    | UsingBlockIntro of QsSymbol * QsInitializer
    | BorrowingBlockIntro of QsSymbol * QsInitializer
    | BodyDeclaration of QsSpecializationGenerator
    | AdjointDeclaration of QsSpecializationGenerator
    | ControlledDeclaration of QsSpecializationGenerator
    | ControlledAdjointDeclaration of QsSpecializationGenerator
    | OperationDeclaration of CallableDeclaration
    | FunctionDeclaration of CallableDeclaration
    | TypeDefinition of TypeDefinition
    | DeclarationAttribute of QsSymbol * QsExpression
    | OpenDirective of QsSymbol * QsNullable<QsSymbol>
    | NamespaceDeclaration of QsSymbol
    | InvalidFragment

    /// returns the error code for an invalid fragment of the given kind
    member this.ErrorCode =
        match this with
        | ExpressionStatement _ -> ErrorCode.InvalidExpressionStatement
        | ReturnStatement _ -> ErrorCode.InvalidReturnStatement
        | FailStatement _ -> ErrorCode.InvalidFailStatement
        | ImmutableBinding _ -> ErrorCode.InvalidImmutableBinding
        | MutableBinding _ -> ErrorCode.InvalidMutableBinding
        | ValueUpdate _ -> ErrorCode.InvalidValueUpdate
        | IfClause _ -> ErrorCode.InvalidIfClause
        | ElifClause _ -> ErrorCode.InvalidElifClause
        | ElseClause _ -> ErrorCode.InvalidElseClause
        | ForLoopIntro _ -> ErrorCode.InvalidForLoopIntro
        | WhileLoopIntro _ -> ErrorCode.InvalidWhileLoopIntro
        | RepeatIntro _ -> ErrorCode.InvalidRepeatIntro
        | UntilSuccess _ -> ErrorCode.InvalidUntilClause
        | WithinBlockIntro _ -> ErrorCode.InvalidWithinBlockIntro
        | ApplyBlockIntro _ -> ErrorCode.InvalidApplyBlockIntro
        | UsingBlockIntro _ -> ErrorCode.InvalidUsingBlockIntro
        | BorrowingBlockIntro _ -> ErrorCode.InvalidBorrowingBlockIntro
        | BodyDeclaration _ -> ErrorCode.InvalidBodyDeclaration
        | AdjointDeclaration _ -> ErrorCode.InvalidAdjointDeclaration
        | ControlledDeclaration _ -> ErrorCode.InvalidControlledDeclaration
        | ControlledAdjointDeclaration _ -> ErrorCode.InvalidControlledAdjointDeclaration
        | OperationDeclaration _ -> ErrorCode.InvalidOperationDeclaration
        | FunctionDeclaration _ -> ErrorCode.InvalidFunctionDeclaration
        | DeclarationAttribute _ -> ErrorCode.InvalidDeclarationAttribute
        | TypeDefinition _ -> ErrorCode.InvalidTypeDefinition
        | NamespaceDeclaration _ -> ErrorCode.InvalidNamespaceDeclaration
        | OpenDirective _ -> ErrorCode.InvalidOpenDirective
        | InvalidFragment _ -> ErrorCode.UnknownCodeFragment

    /// returns the error code for an invalid fragment ending on the given kind
    member this.InvalidEnding =
        match this with
        | ExpressionStatement _
        | ReturnStatement _
        | FailStatement _
        | UntilSuccess (_, false)
        | ImmutableBinding _
        | MutableBinding _
        | ValueUpdate _ -> ErrorCode.ExpectingSemicolon
        | IfClause _
        | ElifClause _
        | ElseClause _
        | ForLoopIntro _
        | WhileLoopIntro _
        | RepeatIntro _
        | UntilSuccess (_, true)
        | WithinBlockIntro _
        | ApplyBlockIntro _ -> ErrorCode.ExpectingOpeningBracket
        | UsingBlockIntro _
        | BorrowingBlockIntro _ -> ErrorCode.ExpectingOpeningBracketOrSemicolon
        | BodyDeclaration gen
        | AdjointDeclaration gen
        | ControlledDeclaration gen
        | ControlledAdjointDeclaration gen ->
            match gen.Generator with
            | UserDefinedImplementation _ -> ErrorCode.ExpectingOpeningBracket
            | _ -> ErrorCode.ExpectingSemicolon
        | OperationDeclaration _
        | FunctionDeclaration _ -> ErrorCode.ExpectingOpeningBracket
        | TypeDefinition _ -> ErrorCode.ExpectingSemicolon
        | DeclarationAttribute _ -> ErrorCode.UnexpectedFragmentDelimiter
        | NamespaceDeclaration _ -> ErrorCode.ExpectingOpeningBracket
        | OpenDirective _ -> ErrorCode.ExpectingSemicolon
        | InvalidFragment _ -> ErrorCode.UnexpectedFragmentDelimiter


type QsFragment =
    {
        Kind: QsFragmentKind
        Range: Range
        Diagnostics: ImmutableArray<QsCompilerDiagnostic>
        Text: string
    }
