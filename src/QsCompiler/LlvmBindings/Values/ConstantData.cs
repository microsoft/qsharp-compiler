// -----------------------------------------------------------------------
// <copyright file="ConstantData.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LlvmBindings.Interop;

namespace LlvmBindings.Values
{
    /// <summary>Base class for constants with no operands.</summary>
    public class ConstantData
        : Constant
    {
        internal ConstantData(LLVMValueRef handle)
            : base(handle)
        {
        }
    }
}
