// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Quantum.QsCompiler.BondSchemas.Execution
{
    /// <summary>
    /// This class provides extension methods for objects in the Microsoft.Quantum.QsCompiler.BondSchemas.Execution namespace.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Determine whether the values of two object instances are equal.
        /// </summary>
        public static bool ValueEquals(this Argument @this, Argument other)
        {
            return @this.Name.Equals(other.Name) &&
                    @this.Type == other.Type &&
                    @this.Bool == other.Bool &&
                    @this.Double == other.Double &&
                    @this.Integer == other.Integer &&
                    @this.Pauli == other.Pauli &&
                    @this.Result == other.Result &&
                    AreNullablesEqual(@this.String, other.String, string.Equals) &&
                    AreNullablesEqual(@this.Range, other.Range, ValueEquals) &&
                    AreNullablesEqual(@this.Array, other.Array, (a, b) => AreCollectionsEqual(a, b, ValueEquals));
        }

        /// <summary>
        /// Determine whether the values of two object instances are equal.
        /// </summary>
        public static bool ValueEquals(this QirExecutionWrapper @this, QirExecutionWrapper other)
        {
            return ValueEquals(@this.QirBytecode, other.QirBytecode) &&
                   AreNullablesEqual(@this.EntryPoints, other.EntryPoints, (a, b) => AreCollectionsEqual(a, b, ValueEquals));
        }

        public static bool ValueEquals(this RangeValue rangeValueA, RangeValue rangeValueB)
        {
            if (rangeValueA.Start != rangeValueB.Start)
            {
                return false;
            }
            else if (rangeValueA.Step != rangeValueB.Step)
            {
                return false;
            }
            else if (rangeValueA.End != rangeValueB.End)
            {
                return false;
            }

            return true;
        }

        public static bool AreCollectionsEqual<T>(ICollection<T> collectionA, ICollection<T> collectionB, Func<T, T, bool> equalityFunction)
        {
            if (collectionA.Count != collectionB.Count)
            {
                return false;
            }

            using (var enumeratorA = collectionA.GetEnumerator())
            using (var enumeratorB = collectionB.GetEnumerator())
            {
                while (enumeratorA.MoveNext() && enumeratorB.MoveNext())
                {
                    if (!equalityFunction(enumeratorA.Current, enumeratorB.Current))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool AreNullablesEqual<T>(T? itemA, T? itemB, Func<T, T, bool> equalityFunction)
            where T : class
        {
            if ((itemA == null) && (itemB != null))
            {
                return false;
            }

            if ((itemA != null) && (itemB == null))
            {
                return false;
            }

            if ((itemA != null) && (itemB != null))
            {
                return equalityFunction(itemA, itemB);
            }

            return true;
        }

        private static bool ValueEquals(this ArraySegment<byte> @this, ArraySegment<byte> other)
        {
            return @this.Array.Skip(@this.Offset).Take(@this.Count).SequenceEqual(other.Array.Skip(other.Offset).Take(other.Count));
        }

        private static bool ValueEquals(this EntryPointOperation @this, EntryPointOperation other)
        {
            return AreNullablesEqual(@this.Name, other.Name, string.Equals) &&
                   AreNullablesEqual(@this.Arguments, other.Arguments, (a, b) => AreCollectionsEqual(a, b, ValueEquals));
        }
    }
}
