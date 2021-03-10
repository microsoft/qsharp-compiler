// -----------------------------------------------------------------------
// <copyright file="InsertValue.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Instruction to insert a value into a member field in an aggregate value</summary>
    /// <seealso href="xref:llvm_langref#insertvalue-instruction">LLVM insertvalue Instruction</seealso>
    public class InsertValue
        : Instruction
    {
        internal InsertValue( LLVMValueRef valueRef )
            : base( valueRef )
        {
        }
    }
}
