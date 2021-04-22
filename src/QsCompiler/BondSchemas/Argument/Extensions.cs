// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Quantum.QsCompiler.BondSchemas.Argument
{
    /// <summary>
    /// This class provides extension methods for objects in the Microsoft.Quantum.QsCompiler.BondSchemas.Argument namespace.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Determine whether the values of two object instances are equal.
        /// </summary>
        public static bool ValueEquals(this Argument @this, Argument other)
        {
            if (!@this.Name.Equals(other.Name))
            {
                return false;
            }

            // TODO implement.
            return true;
        }

        private static bool AreCollectionsEqual<T>(ICollection<T> collectionA, ICollection<T> collectionB, Func<T, T, bool> equalityFunction)
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

        private static bool AreNullablesEqual<T>(T? itemA, T? itemB, Func<T, T, bool> equalityFunction)
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

        private static bool ValueEquals(this Argument argumentA, Argument argumentB)
        {
            if (argumentA.Type != argumentB.Type)
            {
                return false;
            }

            if (!argumentA.Name.Equals(argumentB.Name))
            {
                return false;
            }

            if (argumentA.Position != argumentB.Position)
            {
                return false;
            }

            if (argumentA.ArrayType != argumentB.ArrayType)
            {
                return false;
            }

            if (!AreCollectionsEqual(argumentA.Values, argumentB.Values, ValueEquals))
            {
                return false;
            }

            return true;
        }

        private static bool ValueEquals(this ArgumentValue argumentValueA, ArgumentValue argumentValueB)
        {
            if (argumentValueA.Bool != argumentValueB.Bool)
            {
                return false;
            }

            if (argumentValueA.Integer != argumentValueB.Integer)
            {
                return false;
            }

            if (argumentValueA.Double != argumentValueB.Double)
            {
                return false;
            }

            if (argumentValueA.Pauli != argumentValueB.Pauli)
            {
                return false;
            }

            if (!AreNullablesEqual(argumentValueA.Range, argumentValueB.Range, ValueEquals))
            {
                return false;
            }

            if (argumentValueA.Result != argumentValueB.Result)
            {
                return false;
            }

            if (!AreNullablesEqual(argumentValueA.String, argumentValueB.String, Equals))
            {
                return false;
            }

            if (!AreNullablesEqual(argumentValueA.Array, argumentValueB.Array, ValueEquals))
            {
                return false;
            }

            return true;
        }

        private static bool ValueEquals(this RangeValue rangeValueA, RangeValue rangeValueB)
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
    }
}
