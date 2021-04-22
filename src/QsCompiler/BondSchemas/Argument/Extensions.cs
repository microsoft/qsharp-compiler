// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using static Microsoft.Quantum.QsCompiler.BondSchemas.Util.Util;

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
    }
}
