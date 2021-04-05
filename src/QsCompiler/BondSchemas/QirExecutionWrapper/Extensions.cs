// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.Quantum.QsCompiler.BondSchemas.EntryPoint;

namespace Microsoft.Quantum.QsCompiler.BondSchemas.QirExecutionWrapper
{
    /// <summary>
    /// This class provides extension methods for objects in the Microsoft.Quantum.QsCompiler.BondSchemas.QirExecutionWrapper namespace.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Determine whether the values of two object instances are equal.
        /// </summary>
        public static bool ValueEquals(this QirExecutionWrapper executionWrapperA, QirExecutionWrapper executionWrapperB)
        {
            if (executionWrapperA == null && executionWrapperB == null)
            {
                return true;
            }

            if (executionWrapperA == null || executionWrapperB == null)
            {
                return false;
            }

            if (!executionWrapperA.EntryPoint.ValueEquals(executionWrapperB.EntryPoint))
            {
                return false;
            }

            var bytesA = executionWrapperA.QirBytes.Deserialize();
            var bytesB = executionWrapperB.QirBytes.Deserialize();
            return bytesA.Data.Array
                .Skip(bytesA.Data.Offset).Take(bytesA.Data.Count)
                .SequenceEqual(bytesB.Data.Array.Skip(bytesB.Data.Offset).Take(bytesB.Data.Count));
        }
    }
}
