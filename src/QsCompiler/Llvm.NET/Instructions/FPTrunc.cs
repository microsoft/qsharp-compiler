// -----------------------------------------------------------------------
// <copyright file="FPTrunc.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Instruction to truncate a floating point value to another floating point type</summary>
    /// <seealso href="xref:llvm_langref#fptruncto-to-instruction">LLVM fptruncto .. to Instruction</seealso>
    public class FPTrunc
        : Cast
    {
        internal FPTrunc( LLVMValueRef valueRef )
            : base( valueRef )
        {
        }
    }
}
