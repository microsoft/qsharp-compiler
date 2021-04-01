// -----------------------------------------------------------------------
// <copyright file="AddressSpaceCast.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Address space cast instruction.</summary>
    /// <seealso href="xref:llvm_langref#addrspaceast-to-instruction">LLVM addrspacecast .. to</seealso>
    public class AddressSpaceCast : Cast
    {
        internal AddressSpaceCast(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
