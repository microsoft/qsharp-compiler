// -----------------------------------------------------------------------
// <copyright file="ConstantAggregate.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>Base class for aggregate constants (with operands).</summary>
    public class ConstantAggregate
        : Constant
    {
        internal ConstantAggregate( LLVMValueRef handle )
            : base( handle )
        {
        }
    }
}
