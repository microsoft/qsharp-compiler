// -----------------------------------------------------------------------
// <copyright file="FCmp.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Instruction to perform comparison of floating point values.</summary>
    /// <seealso href="xref:llvm_langref#fcmp-instruction">LLVM fcmp Instruction</seealso>
    public class FCmp
        : Cmp
    {
        internal FCmp(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
