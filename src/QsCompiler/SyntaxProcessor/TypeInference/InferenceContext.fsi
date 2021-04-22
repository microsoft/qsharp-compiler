﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxProcessing
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

/// The inference context is an implementation of Hindley-Milner type inference. It is a source of fresh type parameters
/// and can unify types containing them.
type InferenceContext =
    /// <summary>
    /// Creates a new inference context using the given <see cref="SymbolTracker"/>.
    /// </summary>
    new: symbolTracker:SymbolTracker -> InferenceContext

    /// Diagnostics for all type variables that are missing substitutions.
    member AmbiguousDiagnostics: QsCompilerDiagnostic list

    /// Updates the position of the statement in which types are currently being inferred.
    member UseStatementPosition: position:Position -> unit

    /// <summary>
    /// Creates a fresh type parameter originating from the given <paramref name="source"/> range.
    /// </summary>
    member internal Fresh: source:Range -> ResolvedType

    /// <summary>
    /// Unifies the <paramref name="expected"/> type with the <paramref name="actual"/> type. Fails if
    /// <paramref name="actual"/> is not a subtype of <paramref name="expected"/>.
    /// </summary>
    member internal Unify: expected:ResolvedType * actual:ResolvedType -> QsCompilerDiagnostic list

    /// <summary>
    /// Returns a type that is a supertype of both types <paramref name="left"/> and <paramref name="right"/>, and that
    /// has a <see cref="TypeRange.Generated"/> range.
    /// </summary>
    member internal Intersect: left:ResolvedType * right:ResolvedType -> ResolvedType * QsCompilerDiagnostic list

    /// <summary>
    /// Constrains the given <paramref name="resolvedType"/> to satisfy the <paramref name="typeConstraint"/>.
    /// </summary>
    member internal Constrain: resolvedType:ResolvedType * typeConstraint:Constraint -> QsCompilerDiagnostic list

    /// <summary>
    /// Replaces each placeholder type parameter in the given <paramref name="resolvedType"/> with its substitution if
    /// one exists.
    /// </summary>
    member internal Resolve: resolvedType:ResolvedType -> ResolvedType

module InferenceContext =
    /// <summary>
    /// A syntax tree transformation that resolves types using the given inference <paramref name="context"/>.
    /// </summary>
    [<CompiledName "Resolver">]
    val resolver: context:InferenceContext -> SyntaxTreeTransformation
