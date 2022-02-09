// -----------------------------------------------------------------------
// <copyright file="SelectInstruction.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Select instruction.</summary>
    public class SelectInstruction
        : Instruction
    {
        internal SelectInstruction(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
