// -----------------------------------------------------------------------
// <copyright file="VAArg.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Instruction to load an argument of a specified type from a variadic argument list.</summary>
    public class VaArg
        : UnaryInstruction
    {
        internal VaArg(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
