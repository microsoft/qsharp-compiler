// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.VerificationTools

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree


// utils for type checking

type QsSymbol with
    member internal this.RangeOrDefault =
        let onNull () =
            let isInvalidSym =
                match this.Symbol with
                | InvalidSymbol -> true
                | _ -> false

            QsCompilerError.Verify(isInvalidSym, "valid symbol without a range")
            Range.Zero

        this.Range.ValueOrApply onNull

type QsSpecializationGenerator with
    member internal this.RangeOrDefault =
        let onNull () =
            let isInvalidGen =
                match this.Generator with
                | FunctorGenerationDirective dir ->
                    match dir with
                    | InvalidGenerator -> true
                    | _ -> false
                | _ -> false

            QsCompilerError.Verify(isInvalidGen, "valid generator without a range")
            Range.Zero

        this.Range.ValueOrApply onNull

type QsExpression with
    member internal this.RangeOrDefault =
        let onNull () =
            let isInvalidExpr =
                match this.Expression with
                | InvalidExpr -> true
                | _ -> false

            QsCompilerError.Verify(isInvalidExpr, "valid expression without a range")
            Range.Zero

        this.Range.ValueOrApply onNull

    member this.isInvalid =
        match this with
        | Tuple _
        | Item _
        | Missing -> false
        | _ -> true

    member this.isMissing =
        match this with
        | Missing -> true
        | _ -> false

type ResolvedType with

    member this.isInvalid =
        match this.Resolution with
        | InvalidType -> true
        | _ -> false

    member this.isMissing =
        match this.Resolution with
        | MissingType -> true
        | _ -> false

    member this.isTypeParameter =
        match this.Resolution with
        | TypeParameter _ -> true
        | _ -> false

    /// Returns true if the given type contains external type parameters,
    /// i.e. type parameters that do not belong to the given symbol parent.
    /// Returns false otherwise.
    member this.isTypeParametrized parent =
        let condition =
            function
            | QsTypeKind.TypeParameter tp -> tp.Origin <> parent
            | _ -> false

        this.Exists condition

    /// If the given type supports equality comparison,
    /// returns the type of an equality comparison expression as Some (which is always Bool).
    /// Returns None otherwise.
    member this.supportsEqualityComparison =
        match this.Resolution with
        | Int
        | BigInt
        | Double
        | Bool
        | Qubit
        | String
        | Result
        | Pauli -> Some(Bool |> ResolvedType.New) // excludes Range
        | _ -> None

    /// If the given type supports arithmetic operations,
    /// returns the type of an arithmetic expression as Some.
    /// Returns None otherwise.
    member this.supportsArithmetic =
        match this.Resolution with
        | Int
        | BigInt
        | Double -> Some this
        | _ -> None

    /// If the given type supports concatenations,
    /// returns the type of the concatenation expression as Some (which is the same as the given type).
    /// Returns None otherwise.
    member this.supportsConcatenation =
        match this.Resolution with
        | String
        | ArrayType _ -> Some this
        | _ -> None

    /// If the given type supports iteration,
    /// returns the type of the iteration item as Some -
    /// i.e. for an expression of type array its base type, and for a Range expression Int.
    /// Returns None otherwise.
    member this.supportsIteration =
        match this.Resolution with
        | Range -> Some(Int |> ResolvedType.New)
        | ArrayType bt -> Some bt
        | _ -> None
