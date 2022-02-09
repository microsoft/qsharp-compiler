// -----------------------------------------------------------------------
// <copyright file="FPToUI.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Instruction to convert a floating point value to an unsigned integer type.</summary>
    /// <seealso href="xref:llvm_langref#fptoui-to-instruction">LLVM fptoui .. to Instruction</seealso>
    public class FPToUI : Cast
    {
        internal FPToUI(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
