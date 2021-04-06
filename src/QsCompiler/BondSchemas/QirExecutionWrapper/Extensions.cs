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
            if (executionWrapperB == null)
            {
                return false;
            }

            if (!executionWrapperA.EntryPoint.ValueEquals(executionWrapperB.EntryPoint))
            {
                return false;
            }

            return executionWrapperA.QirBytecode.Array
                .Skip(executionWrapperA.QirBytecode.Offset).Take(executionWrapperA.QirBytecode.Count)
                .SequenceEqual(executionWrapperB.QirBytecode.Array.Skip(executionWrapperB.QirBytecode.Offset).Take(executionWrapperB.QirBytecode.Count));
        }
    }
}
