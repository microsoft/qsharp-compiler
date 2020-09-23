using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Quantum.QsCompiler.Transformations
{
    /// <summary>
    /// Helpful utility functions.
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Applies the action <paramref name="f"/> to the value <paramref name="x"/>.
        /// </summary>
        public static void Apply<T>(this T x, Action<T> f) => f(x);

        /// <summary>
        /// Applies the function <paramref name="f"/> to the value <paramref name="x"/> and returns the result.
        /// </summary>
        public static TOut Apply<TIn, TOut>(this TIn x, Func<TIn, TOut> f) => f(x);

        /// <summary>
        /// Projects each element of a sequence into a new form and discards null values.
        /// </summary>
        /// <remarks>Overload for reference types.</remarks>
        public static IEnumerable<TResult> SelectNotNull<TSource, TResult>(
            this IEnumerable<TSource> source, Func<TSource, TResult?> selector)
            where TResult : class =>
            source.SelectMany(item =>
                selector(item)?.Apply(result => new[] { result })
                ?? Enumerable.Empty<TResult>());

        /// <summary>
        /// Projects each element of a sequence into a new form and discards null values.
        /// </summary>
        /// <remarks>Overload for value types.</remarks>
        public static IEnumerable<TResult> SelectNotNull<TSource, TResult>(
            this IEnumerable<TSource> source, Func<TSource, TResult?> selector)
            where TResult : struct =>
            source.SelectMany(item =>
                selector(item)?.Apply(result => new[] { result })
                ?? Enumerable.Empty<TResult>());
    }
}
