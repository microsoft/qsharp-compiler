// -----------------------------------------------------------------------
// <copyright file="ConstantArray.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using LLVMSharp.Interop;

using Ubiquity.NET.Llvm.Types;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>LLVM Constant Array.</summary>
    /// <remarks>
    /// Due to how LLVM treats constant arrays internally, creating a constant array
    /// with the From method overloads may not actually produce a ConstantArray
    /// instance. At the least it will produce a Constant. LLVM will determine the
    /// appropriate internal representation based on the input types and values.
    /// </remarks>
    public sealed class ConstantArray
        : ConstantAggregate
    {
        /// <summary>Create a constant array of values of a given type.</summary>
        /// <param name="elementType">Type of elements in the array.</param>
        /// <param name="values">Values to initialize the array.</param>
        /// <returns>Constant representing the array.</returns>
        public static Constant From(ITypeRef elementType, params Constant[] values)
        {
            return From(elementType, (IList<Constant>)values);
        }

        /// <summary>Create a constant array of values of a given type with a fixed size, zero filling any unspecified values.</summary>
        /// <param name="elementType">Type of elements in the array.</param>
        /// <param name="len">Length of the array.</param>
        /// <param name="values">Values to initialize the array.</param>
        /// <returns>Constant representing the array.</returns>
        /// <remarks>
        /// If the number of arguments provided for the values is less than <paramref name="len"/>
        /// then the remaining elements of the array are set with the null value for the <paramref name="elementType"/>.
        /// </remarks>
        public static Constant From(ITypeRef elementType, int len, params Constant[] values)
        {
            var zeroFilledValues = ZeroFill(elementType, len, values).ToList();
            return From(elementType, zeroFilledValues);
        }

        /// <summary>Create a constant array of values of a given type.</summary>
        /// <param name="elementType">Type of elements in the array.</param>
        /// <param name="values">Values to initialize the array.</param>
        /// <returns>Constant representing the array.</returns>
        public static unsafe Constant From(ITypeRef elementType, IList<Constant> values)
        {
            if (values.Any(v => v.NativeType.GetTypeRef() != elementType.GetTypeRef()))
            {
                throw new ArgumentException("One or more value(s) types do not match specified array element type");
            }

            var valueHandles = values.Select(v => v.ValueHandle).ToArray();
            fixed (LLVMValueRef* pValueHandles = valueHandles.AsSpan())
            {
                var handle = LLVM.ConstArray(elementType.GetTypeRef(), (LLVMOpaqueValue**)pValueHandles, (uint)valueHandles.Length);
                return FromHandle<Constant>(handle)!;
            }
        }

        internal ConstantArray(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }

        private static IEnumerable<Constant> ZeroFill(ITypeRef elementType, int len, IList<Constant> values)
        {
            foreach (var value in values)
            {
                yield return value;
            }

            var zeroVal = elementType.GetNullValue();
            for (int i = values.Count; i < len; ++i)
            {
                yield return zeroVal;
            }
        }
    }
}
