// -----------------------------------------------------------------------
// <copyright file="ResumeInstruction.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Resume instruction</summary>
    public class ResumeInstruction
        : Terminator
    {
        internal ResumeInstruction( LLVMValueRef valueRef )
            : base( valueRef )
        {
        }
    }
}
