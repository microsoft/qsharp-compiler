// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference

open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree

/// A type constraint is a set of types satisfying some property.
type internal Constraint =
    /// An adjointable operation.
    | Adjointable

    /// <summary>
    /// A callable from <paramref name="input"/> to <paramref name="output"/>.
    /// </summary>
    | Callable of input: ResolvedType * output: ResolvedType

    /// <summary>
    /// A type that can be used in an operation that requires auto-generated specializations for the given
    /// <paramref name="functors"/>.
    /// </summary>
    | CanGenerateFunctors of functors: QsFunctor Set

    /// <summary>
    /// A controllable operation that has the given <paramref name="controlled"/> type after the controlled functor is
    /// applied.
    /// </summary>
    | Controllable of controlled: ResolvedType

    /// A type that supports equality comparisons.
    | Equatable

    /// <summary>
    /// A callable that can be partially applied such that, given the remaining inputs as an argument of type
    /// <paramref name="missing"/>, it will yield an output of type <paramref name="result"/>.
    /// </summary>
    | HasPartialApplication of missing: ResolvedType * result: ResolvedType

    /// <summary>
    /// A type that can be accessed by an index of type <paramref name="index"/>, yielding an item of type
    /// <paramref name="item"/>.
    /// </summary>
    | Indexed of index: ResolvedType * item: ResolvedType

    /// A type that represents an integer.
    | Integral

    /// <summary>
    /// A type that can be iterated over, yielding items of type <paramref name="item"/>.
    /// </summary>
    | Iterable of item: ResolvedType

    /// A type that represents a number.
    | Numeric

    /// A type with the associative semigroup operator +.
    | Semigroup

    /// <summary>
    /// A wrapped type that can be unwrapped to yield an item of type <paramref name="item"/>.
    /// </summary>
    | Wrapped of item: ResolvedType

module internal Constraint =
    /// The list of types contained in a constraint.
    val types: Constraint -> ResolvedType list
