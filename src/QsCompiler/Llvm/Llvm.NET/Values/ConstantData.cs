// -----------------------------------------------------------------------
// <copyright file="ConstantData.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>Base class for constants with no operands</summary>
    public class ConstantData
        : Constant
    {
        internal ConstantData( LLVMValueRef handle )
            : base( handle )
        {
        }
    }
}
