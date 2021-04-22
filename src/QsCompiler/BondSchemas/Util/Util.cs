// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Quantum.QsCompiler.BondSchemas.Util
{
    /// <summary>
    /// This class provides extension methods for objects in the Microsoft.Quantum.QsCompiler.BondSchemas.Argument namespace.
    /// </summary>
    internal static class Util
    {
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
    }
}
