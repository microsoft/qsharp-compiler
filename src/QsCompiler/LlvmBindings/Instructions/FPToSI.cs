// -----------------------------------------------------------------------
// <copyright file="FPToSI.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Instruction to convert a floating point value to a signed integer type.</summary>
    /// <seealso href="xref:llvm_langref#fptosi-to-instruction">LLVM fptosi .. to Instruction</seealso>
    public class FPToSI
        : Cast
    {
        internal FPToSI(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
