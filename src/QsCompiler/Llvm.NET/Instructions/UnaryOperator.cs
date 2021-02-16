// -----------------------------------------------------------------------
// <copyright file="UnaryOperator.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Base class for a unary operator</summary>
    public class UnaryOperator
        : Instruction
    {
        internal UnaryOperator( LLVMValueRef valueRef )
            : base( valueRef )
        {
        }
    }
}
