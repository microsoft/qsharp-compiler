// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference

open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree

/// A class constraint is a set of types or a relation between types that have particular properties.
type internal ClassConstraint =
    /// An adjointable operation.
    | Adjointable of operation: ResolvedType

    /// A callable from an input type to an output type.
    | Callable of callable: ResolvedType * input: ResolvedType * output: ResolvedType

    /// A controllable operation that has the controlled type after the controlled functor is applied.
    | Controllable of operation: ResolvedType * controlled: ResolvedType

    /// A type that supports equality.
    | Eq of ResolvedType

    /// If the callable is an operation, then it supports all functors in the set. Types other than operations are
    /// automatically members of this class.
    | HasFunctorsIfOperation of callable: ResolvedType * functors: QsFunctor Set

    /// A callable that can be partially applied, yielding a new callable that has the same output type and the missing
    /// parameters as its input type.
    | HasPartialApplication of callable: ResolvedType * missing: ResolvedType * callable': ResolvedType

    /// A container type that can be indexed, yielding the item type.
    | Index of container: ResolvedType * index: ResolvedType * item: ResolvedType

    /// A type that represents an integer.
    | Integral of ResolvedType

    /// A container type that can be iterated over, yielding the item type.
    | Iterable of container: ResolvedType * item: ResolvedType

    /// A type that represents a number.
    | Num of ResolvedType

    /// A type with an associative binary operation.
    | Semigroup of ResolvedType

    /// An container type that can be unwrapped to yield a single item type.
    | Unwrap of container: ResolvedType * item: ResolvedType

/// An ordering comparison between types.
type internal Ordering =
    /// The type is a subtype of the other type.
    | Subtype
    /// The types are equal.
    | Equal
    /// The type is a supertype of the other type.
    | Supertype

module internal Ordering =
    /// Reverses the direction of the ordering.
    val reverse: Ordering -> Ordering

/// A type constraint.
type internal Constraint =
    /// A class constraint.
    | Class of ClassConstraint
    /// A relational constraint on the ordering between two types.
    | Relation of ResolvedType * Ordering * ResolvedType

module internal Constraint =
    /// <summary>
    /// The types referenced by a constraint.
    /// </summary>
    val types: Constraint -> ResolvedType list

/// <summary>
/// Operators for creating relational constraints.
/// </summary>
module internal RelationOps =
    /// <summary>
    /// <paramref name="lhs"/> is a subtype of <paramref name="rhs"/>.
    /// </summary>
    val (<.): lhs: ResolvedType -> rhs: ResolvedType -> Constraint

    /// <summary>
    /// <paramref name="lhs"/> is equal to <paramref name="rhs"/>.
    /// </summary>
    val (.=): lhs: ResolvedType -> rhs: ResolvedType -> Constraint

    /// <summary>
    /// <paramref name="lhs"/> is a supertype of <paramref name="rhs"/>.
    /// </summary>
    val (.>): lhs: ResolvedType -> rhs: ResolvedType -> Constraint
