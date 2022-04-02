// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference

open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree

/// A type constraint is a set of types satisfying some property.
type internal ClassConstraint =
    /// An adjointable operation.
    | Adjointable of operation: ResolvedType

    /// <summary>
    /// A callable from <paramref name="input"/> to <paramref name="output"/>.
    /// </summary>
    | Callable of callable: ResolvedType * input: ResolvedType * output: ResolvedType

    /// <summary>
    /// A controllable operation that has the given <paramref name="controlled"/> type after the controlled functor is
    /// applied.
    /// </summary>
    | Controllable of operation: ResolvedType * controlled: ResolvedType

    /// A type that supports equality comparisons.
    | Eq of ty: ResolvedType

    /// <summary>
    /// A type that can be used in an operation that requires auto-generated specializations for the given
    /// <paramref name="functors"/>.
    /// </summary>
    | GenerateFunctors of callable: ResolvedType * functors: QsFunctor Set

    /// <summary>
    /// A type that can be accessed by an index of type <paramref name="index"/>, yielding an item of type
    /// <paramref name="item"/>.
    /// </summary>
    | Index of container: ResolvedType * index: ResolvedType * item: ResolvedType

    /// A type that represents an integer.
    | Integral of ty: ResolvedType

    /// <summary>
    /// A type that can be iterated over, yielding items of type <paramref name="item"/>.
    /// </summary>
    | Iterable of container: ResolvedType * item: ResolvedType

    /// A type that represents a number.
    | Num of ty: ResolvedType

    /// <summary>
    /// A callable that can be partially applied such that, given the remaining inputs as an argument of type
    /// <paramref name="missing"/>, it will yield an output of type <paramref name="result"/>.
    /// </summary>
    | PartialAp of callable: ResolvedType * missing: ResolvedType * result: ResolvedType

    /// A type with the associative semigroup operator +.
    | Semigroup of ty: ResolvedType

    /// <summary>
    /// A wrapped type that can be unwrapped to yield an item of type <paramref name="item"/>.
    /// </summary>
    | Unwrap of container: ResolvedType * item: ResolvedType

module internal ClassConstraint =
    /// <summary>
    /// Pretty prints a <see cref="ClassConstraint"/>.
    /// </summary>
    val pretty: ClassConstraint -> string

/// An ordering comparison between types.
type internal Ordering =
    /// The type is a subtype of another type.
    | Subtype
    /// The types are equal.
    | Equal
    /// The type is a supertype of another type.
    | Supertype

module internal Ordering =
    /// Reverses the direction of the ordering.
    val reverse: Ordering -> Ordering

type internal Constraint =
    | Class of ClassConstraint
    | Relation of ResolvedType * Ordering * ResolvedType

module internal Constraint =
    /// <summary>
    /// The types referenced by a <see cref="Constraint"/>.
    /// </summary>
    val types: Constraint -> ResolvedType list

/// <summary>
/// Operators for <see cref="Relation"/>.
/// </summary>
module internal RelationOps =
    /// <summary>
    /// lhs is a subtype of rhs.
    /// </summary>
    val (<.): ResolvedType -> ResolvedType -> Constraint

    /// <summary>
    /// lhs is equal to rhs.
    /// </summary>
    val (.=): ResolvedType -> ResolvedType -> Constraint

    /// <summary>
    /// lhs is a supertype of rhs.
    /// </summary>
    val (.>): ResolvedType -> ResolvedType -> Constraint
