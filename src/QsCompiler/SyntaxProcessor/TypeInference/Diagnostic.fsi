// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTree

/// <summary>A type context stores information needed for error reporting of mismatched types.</summary>
/// <example>
/// <para>
/// While matching type <c>(A, B)</c> with type <c>(C, B)</c>, an error is encountered when <c>A</c> is recursively
/// matched with <c>C</c>. The error message might look something like: "Mismatched types <c>A</c> and <c>C</c>.
/// Expected: <c>(A, B)</c>. Actual: <c>(C, B)</c>."
/// </para>
/// <para>
/// In this example, <c>A</c> is <see cref="Expected" />, <c>(A, B)</c> is <see cref="ExpectedParent" />, <c>C</c> is
/// <see cref="Actual" />, and <c>(C, B)</c> is <see cref="ActualParent" />.
/// </para>
/// </example>
type internal TypeContext =
    {
        Expected: ResolvedType
        ExpectedParent: ResolvedType option
        Actual: ResolvedType
        ActualParent: ResolvedType option
    }

module internal TypeContext =
    val createOrphan: expected: ResolvedType -> actual: ResolvedType -> TypeContext

    val withParents: expected: ResolvedType -> actual: ResolvedType -> context: TypeContext -> TypeContext

type internal Diagnostic =
    | TypeMismatch of TypeContext
    | TypeIntersectionMismatch of Ordering * TypeContext
    | InfiniteType of TypeContext
    | CompilerDiagnostic of QsCompilerDiagnostic

module internal Diagnostic =
    /// <summary>
    /// Updates the parents in the diagnostic's type context if the type range of the new parents is the same as the old
    /// parents.
    /// </summary>
    /// <remarks>
    /// When updating diagnostic parents "inside out" (from the innermost nested types that caused the error to the
    /// outermost original types), the range checking behavior has the effect of finding the full type of the expression
    /// that is underlined by the diagnostic.
    /// </remarks>
    val withParents: expected: ResolvedType -> actual: ResolvedType -> diagnostic: Diagnostic -> Diagnostic

    val toCompilerDiagnostic: Diagnostic -> QsCompilerDiagnostic
