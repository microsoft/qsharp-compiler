// -----------------------------------------------------------------------
// <copyright file="Cast.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Base class for cast instructions.</summary>
    public class Cast
        : UnaryInstruction
    {
        /// <summary>Gets the source type of the cast.</summary>
        public ITypeRef FromType => this.Operands.GetOperand<Value>(0)!.NativeType;

        /// <summary>Gets the destination type of the cast.</summary>
        public ITypeRef ToType => this.NativeType;

        internal Cast(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
