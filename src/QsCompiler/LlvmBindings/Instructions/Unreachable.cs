// -----------------------------------------------------------------------
// <copyright file="Unreachable.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Instruction to indicate an unreachable location.</summary>
    public class Unreachable
        : Terminator
    {
        internal Unreachable(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
