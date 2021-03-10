// -----------------------------------------------------------------------
// <copyright file="SIToFP.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Instruction for converting a signed integer value into a floating point value</summary>
    public class SIToFP
        : Cast
    {
        internal SIToFP( LLVMValueRef valueRef )
            : base( valueRef )
        {
        }
    }
}
