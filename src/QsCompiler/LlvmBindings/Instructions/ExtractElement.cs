// -----------------------------------------------------------------------
// <copyright file="ExtractElement.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Instruction to extract a single scalar element from a vector at a specified index.</summary>
    /// <seealso href="xref:llvm_langref#extractelement-instruction">LLVM extractelement Instruction</seealso>
    public class ExtractElement
        : Instruction
    {
        internal ExtractElement(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
