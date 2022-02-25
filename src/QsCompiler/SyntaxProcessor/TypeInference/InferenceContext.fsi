﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxProcessing
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

/// A relationship between two types.
type internal 'a Relation

/// <summary>
/// Operators for <see cref="Relation"/>.
/// </summary>
module internal RelationOps =
    /// <summary>
    /// <paramref name="lhs"/> is a subtype of <paramref name="rhs"/>.
    /// </summary>
    val (<.): lhs: 'a -> rhs: 'a -> 'a Relation

    /// <summary>
    /// <paramref name="lhs"/> is equal to <paramref name="rhs"/>.
    /// </summary>
    val (.=.): lhs: 'a -> rhs: 'a -> 'a Relation

    /// <summary>
    /// <paramref name="lhs"/> is a supertype of <paramref name="rhs"/>.
    /// </summary>
    val (.>): lhs: 'a -> rhs: 'a -> 'a Relation

/// The inference context is an implementation of Hindley-Milner type inference. It is a source of fresh type parameters
/// and can unify types containing them.
type InferenceContext =
    /// <summary>
    /// Creates a new inference context using the given <see cref="SymbolTracker"/>.
    /// </summary>
    new: symbolTracker: SymbolTracker -> InferenceContext

    /// Diagnostics for all type variables that are missing substitutions.
    member AmbiguousDiagnostics: QsCompilerDiagnostic list

    /// Updates the position of the statement in which types are currently being inferred.
    member UseStatementPosition: position: Position -> unit

    /// Updates the the root node position and the position of the statement in which types are currently being inferred relative to that.
    member UseSyntaxTreeNodeLocation: rootNodePosition: Position * relativePosition: Position -> unit

    /// Gets the position of the statement relative to the root node in which types are currently being inferred.
    member internal GetRelativeStatementPosition: unit -> Position

    /// <summary>
    /// Creates a fresh type parameter originating from the given <paramref name="source"/> range.
    /// </summary>
    member internal Fresh: source: Range -> ResolvedType

    /// <summary>
    /// Matches the types in the <paramref name="relation"/> according to its ordering.
    /// </summary>
    /// <returns>
    /// Diagnostics if the types did not match. For error reporting purposes, the left-hand type is considered the
    /// expected type.
    /// </returns>
    member internal Match: relation: ResolvedType Relation -> QsCompilerDiagnostic list

    /// <summary>
    /// Returns a type that is a supertype of both types <paramref name="type1"/> and <paramref name="type2"/>, and that
    /// has a <see cref="TypeRange.Generated"/> range.
    /// </summary>
    member internal Intersect: type1: ResolvedType * type2: ResolvedType -> ResolvedType * QsCompilerDiagnostic list

    /// <summary>
    /// Constrains the given <paramref name="type_"/> to satisfy the <paramref name="constraint_"/>.
    /// </summary>
    member internal Constrain: type_: ResolvedType * constraint_: Constraint -> QsCompilerDiagnostic list

    /// <summary>
    /// Replaces each placeholder type parameter in the given <paramref name="type_"/> with its substitution if
    /// one exists.
    /// </summary>
    member internal Resolve: type_: ResolvedType -> ResolvedType

/// <summary>
/// Utility functions for <see cref="InferenceContext"/>.
/// </summary>
module InferenceContext =
    /// <summary>
    /// A syntax tree transformation that resolves types using the given inference <paramref name="context"/>.
    /// </summary>
    [<CompiledName "Resolver">]
    val resolver: context: InferenceContext -> SyntaxTreeTransformation
