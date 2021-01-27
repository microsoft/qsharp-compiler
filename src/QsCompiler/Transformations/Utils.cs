// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Quantum.QsCompiler.Transformations
{
    /// <summary>
    /// Helpful utility functions.
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// Applies the action <paramref name="f"/> to the value <paramref name="x"/>.
        /// </summary>
        internal static void Apply<T>(this T x, Action<T> f) => f(x);

        /// <summary>
        /// Applies the function <paramref name="f"/> to the value <paramref name="x"/> and returns the result.
        /// </summary>
        internal static TOut Apply<TIn, TOut>(this TIn x, Func<TIn, TOut> f) => f(x);

        /// <summary>
        /// Projects each element of a sequence into a new form and discards null values.
        /// </summary>
        /// <remarks>Overload for reference types.</remarks>
        internal static IEnumerable<TResult> SelectNotNull<TSource, TResult>(
            this IEnumerable<TSource> source, Func<TSource, TResult?> selector)
            where TResult : class =>
            source.SelectMany(item =>
                selector(item)?.Apply(result => new[] { result })
                ?? Enumerable.Empty<TResult>());

        /// <summary>
        /// Projects each element of a sequence into a new form and discards null values.
        /// </summary>
        /// <remarks>Overload for value types.</remarks>
        internal static IEnumerable<TResult> SelectNotNull<TSource, TResult>(
            this IEnumerable<TSource> source, Func<TSource, TResult?> selector)
            where TResult : struct =>
            source.SelectMany(item =>
                selector(item)?.Apply(result => new[] { result })
                ?? Enumerable.Empty<TResult>());
    }
}
