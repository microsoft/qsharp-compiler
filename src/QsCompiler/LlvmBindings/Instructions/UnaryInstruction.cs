// -----------------------------------------------------------------------
// <copyright file="UnaryInstruction.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Base class for unary operator instructions.</summary>
    public class UnaryInstruction
        : Instruction
    {
        internal UnaryInstruction(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
