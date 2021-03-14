// -----------------------------------------------------------------------
// <copyright file="ConstantDataArray.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>Array of constant data</summary>
    public class ConstantDataArray
        : ConstantDataSequential
    {
        internal ConstantDataArray( LLVMValueRef valueRef )
            : base( valueRef )
        {
        }
    }
}
