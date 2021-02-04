// -----------------------------------------------------------------------
// <copyright file="MemIntrinsic.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Base class for memory intrinsic instructions</summary>
    public class MemIntrinsic
        : Intrinsic
    {
        internal MemIntrinsic( LLVMValueRef valueRef )
            : base( valueRef )
        {
        }
    }
}
