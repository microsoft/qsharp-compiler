// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Quantum.QsCompiler.BondSchemas.EntryPoint
{
    /// <summary>
    /// This class provides extension methods for objects in the Microsoft.Quantum.QsCompiler.BondSchemas.EntryPoint namespace.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Determine whether the values of two object instances are equal.
        /// </summary>
        public static bool ValueEquals(this EntryPointOperation entryPointA, EntryPointOperation entryPointB)
        {
            if (!entryPointA.Name.Equals(entryPointB.Name))
            {
                return false;
            }

            if (!AreCollectionsEqual(entryPointA.Arguments, entryPointB.Arguments, ValueEquals))
            {
                return false;
            }

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

        private static bool ValueEquals(this ArrayValue arrayValueA, ArrayValue arrayValueB)
        {
            static bool AreBoolListsEqual(List<bool> listA, List<bool> listB) => AreCollectionsEqual(listA, listB, (a, b) => a == b);
            if (!AreNullablesEqual(arrayValueA.Bool, arrayValueB.Bool, AreBoolListsEqual))
            {
                return false;
            }

            static bool AreIntegerListsEqual(List<long> listA, List<long> listB) => AreCollectionsEqual(listA, listB, (a, b) => a == b);
            if (!AreNullablesEqual(arrayValueA.Integer, arrayValueB.Integer, AreIntegerListsEqual))
            {
                return false;
            }

            static bool AreDoubleListsEqual(List<double> listA, List<double> listB) => AreCollectionsEqual(listA, listB, (a, b) => a == b);
            if (!AreNullablesEqual(arrayValueA.Double, arrayValueB.Double, AreDoubleListsEqual))
            {
                return false;
            }

            static bool ArePauliListsEqual(List<PauliValue> listA, List<PauliValue> listB) => AreCollectionsEqual(listA, listB, (a, b) => a == b);
            if (!AreNullablesEqual(arrayValueA.Pauli, arrayValueB.Pauli, ArePauliListsEqual))
            {
                return false;
            }

            static bool AreRangeListsEqual(List<RangeValue> listA, List<RangeValue> listB) => AreCollectionsEqual(listA, listB, ValueEquals);
            if (!AreNullablesEqual(arrayValueA.Range, arrayValueB.Range, AreRangeListsEqual))
            {
                return false;
            }

            static bool AreResultListsEqual(List<ResultValue> listA, List<ResultValue> listB) => AreCollectionsEqual(listA, listB, (a, b) => a == b);
            if (!AreNullablesEqual(arrayValueA.Result, arrayValueB.Result, AreResultListsEqual))
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
