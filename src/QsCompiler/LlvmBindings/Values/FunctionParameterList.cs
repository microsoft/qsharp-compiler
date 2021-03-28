// -----------------------------------------------------------------------
// <copyright file="FunctionParameterList.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>Support class to provide read only list semantics to the parameters of a method.</summary>
    internal class FunctionParameterList
        : IReadOnlyList<Argument>
    {
        private readonly IrFunction owningFunction;

        internal FunctionParameterList(IrFunction owningFunction)
        {
            this.owningFunction = owningFunction;
        }

        public int Count
        {
            get
            {
                uint count = this.owningFunction.ValueHandle.ParamsCount;
                return (int)Math.Min(count, int.MaxValue);
            }
        }

        public Argument this[int index]
        {
            get
            {
                if (index >= this.Count || index < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                LLVMValueRef valueRef = this.owningFunction.ValueHandle.GetParam((uint)index);
                return Value.FromHandle<Argument>(valueRef)!;
            }
        }

        public IEnumerator<Argument> GetEnumerator()
        {
            for (uint i = 0; i < this.Count; ++i)
            {
                LLVMValueRef val = this.owningFunction.ValueHandle.GetParam(i);
                yield return Value.FromHandle<Argument>(val)!;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
