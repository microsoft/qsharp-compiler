// -----------------------------------------------------------------------
// <copyright file="ConstantInt.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;

using LLVMSharp.Interop;


namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>Represents an arbitrary bit width integer constant in LLVM</summary>
    /// <remarks>
    /// Note - for integers, in LLVM, signed or unsigned is not part of the type of
    /// the integer. The distinction between them is determined entirely by the
    /// instructions used on the integer values.
    /// </remarks>
    public sealed class ConstantInt
        : ConstantData
    {
        /// <summary>Gets the number of bits in this integer constant</summary>
        public uint BitWidth => NativeType.IntegerBitWidth;

        /// <summary>Gets the value of the constant zero extended to a 64 bit value</summary>
        /// <exception cref="InvalidOperationException">If <see cref="BitWidth"/> is greater than 64 bits</exception>
        public UInt64 ZeroExtendedValue
            => BitWidth <= 64 ? ValueHandle.ConstIntZExt
                              : throw new InvalidOperationException( );

        /// <summary>Gets the value of the constant sign extended to a 64 bit value</summary>
        /// <exception cref="InvalidOperationException">If <see cref="BitWidth"/> is greater than 64 bits</exception>
        public Int64 SignExtendedValue
            => BitWidth <= 64 ? ValueHandle.ConstIntSExt
                              : throw new InvalidOperationException( );

        internal ConstantInt( LLVMValueRef valueRef )
            : base( valueRef )
        {
        }
    }
}
