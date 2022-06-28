// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput

type TypeContext =
    {
        Expected: ResolvedType
        ExpectedParent: ResolvedType option
        Actual: ResolvedType
        ActualParent: ResolvedType option
    }

module TypeContext =
    let createOrphan expected actual =
        {
            Expected = expected
            ExpectedParent = None
            Actual = actual
            ActualParent = None
        }

    let withParents expected actual context =
        { context with ExpectedParent = Some expected; ActualParent = Some actual }

type Diagnostic =
    | TypeMismatch of TypeContext
    | TypeIntersectionMismatch of Ordering * TypeContext
    | InfiniteType of TypeContext
    | CompilerDiagnostic of QsCompilerDiagnostic

module Diagnostic =
    let withParents expected actual diagnostic =
        let hasSameRange (type1: ResolvedType) : ResolvedType option -> _ =
            Option.forall (fun type2 -> type1.Range = type2.Range)

        let checkActualRange context =
            if hasSameRange actual context.ActualParent then
                context |> TypeContext.withParents expected actual
            else
                context

        let checkBothRanges context =
            if hasSameRange expected context.ExpectedParent && hasSameRange actual context.ActualParent then
                context |> TypeContext.withParents expected actual
            else
                context

        // For diagnostics whose range corresponds to the actual type of the provided expression, only check the actual
        // type's range. For diagnostics whose range spans both the expected and actual types, check both ranges. See
        // `toCompilerDiagnostic` for which range is used by each diagnostic case.
        match diagnostic with
        | TypeMismatch context -> checkActualRange context |> TypeMismatch
        | TypeIntersectionMismatch (ordering, context) -> TypeIntersectionMismatch(ordering, checkBothRanges context)
        | InfiniteType context -> checkActualRange context |> InfiniteType
        | CompilerDiagnostic diagnostic -> CompilerDiagnostic diagnostic

    let private describeType (resolvedType: ResolvedType) =
        match resolvedType.Resolution with
        | TypeParameter param ->
            sprintf "parameter %s (bound by %s)" (SyntaxTreeToQsharp.Default.ToCode resolvedType) param.Origin.Name
        | _ -> SyntaxTreeToQsharp.Default.ToCode resolvedType

    let private typeContextArgs context =
        let expectedParent = context.ExpectedParent |> Option.defaultValue context.Expected
        let actualParent = context.ActualParent |> Option.defaultValue context.Actual

        [
            describeType context.Expected
            SyntaxTreeToQsharp.Default.ToCode expectedParent
            describeType context.Actual
            SyntaxTreeToQsharp.Default.ToCode actualParent
        ]

    let toCompilerDiagnostic =
        function
        | TypeMismatch context ->
            let range = TypeRange.tryRange context.Actual.Range |> QsNullable.defaultValue Range.Zero
            QsCompilerDiagnostic.Error (ErrorCode.TypeMismatch, typeContextArgs context) range
        | TypeIntersectionMismatch (ordering, context) ->
            let orderingString =
                match ordering with
                | Subtype -> "share a subtype with"
                | Equal -> "equal"
                | Supertype -> "share a supertype with"

            let range =
                (TypeRange.tryRange context.Expected.Range, TypeRange.tryRange context.Actual.Range)
                ||> QsNullable.Map2 Range.Span
                |> QsNullable.defaultValue Range.Zero

            let args = orderingString :: typeContextArgs context
            QsCompilerDiagnostic.Error (ErrorCode.TypeIntersectionMismatch, args) range
        | InfiniteType context ->
            let range = TypeRange.tryRange context.Actual.Range |> QsNullable.defaultValue Range.Zero
            QsCompilerDiagnostic.Error (ErrorCode.InfiniteType, typeContextArgs context) range
        | CompilerDiagnostic diagnostic -> diagnostic
