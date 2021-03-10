// -----------------------------------------------------------------------
// <copyright file="Terminator.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Base class for all terminator instructions</summary>
    public class Terminator
        : Instruction
    {
        internal Terminator( LLVMValueRef valueRef )
            : base( valueRef )
        {
        }
    }
}
