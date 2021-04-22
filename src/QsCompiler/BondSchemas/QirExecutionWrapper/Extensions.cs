// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Linq;
using static Microsoft.Quantum.QsCompiler.BondSchemas.Util.Util;

namespace Microsoft.Quantum.QsCompiler.BondSchemas.QirExecutionWrapper
{
    /// <summary>
    /// This class provides extension methods for objects in the Microsoft.Quantum.QsCompiler.BondSchemas.QirExecutionWrapper namespace.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Determine whether the values of two object instances are equal.
        /// </summary>
        public static bool ValueEquals(this QirExecutionWrapper @this, QirExecutionWrapper other)
        {
            return ValueEquals(@this.QirBytecode, other.QirBytecode) &&
                   AreNullablesEqual(@this.EntryPoints, other.EntryPoints, (a, b) => AreCollectionsEqual(a, b, ValueEquals));
        }

        private static bool ValueEquals(this ArraySegment<byte> @this, ArraySegment<byte> other)
        {
            return @this.Array.Skip(@this.Offset).Take(@this.Count).SequenceEqual(other.Array.Skip(other.Offset).Take(other.Count));
        }

        private static bool ValueEquals(this EntryPointOperation @this, EntryPointOperation other)
        {
            return AreNullablesEqual(@this.Name, other.Name, string.Equals) &&
                   AreNullablesEqual(@this.Arguments, other.Arguments, (a, b) => AreCollectionsEqual(a, b, Argument.Extensions.ValueEquals));
        }
    }
}
