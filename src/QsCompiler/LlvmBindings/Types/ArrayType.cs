// -----------------------------------------------------------------------
// <copyright file="ArrayType.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using LlvmBindings.Interop;

// Interface+internal type matches file name
#pragma warning disable SA1649

namespace LlvmBindings.Types
{
    /// <summary>Interface for an LLVM array type. </summary>
    public interface IArrayType
        : ISequenceType
    {
        /// <summary>Gets the length of the array.</summary>
        uint Length { get; }
    }

    /// <summary>Array type definition.</summary>
    /// <remarks>
    /// Array's in LLVM are fixed length sequences of elements.
    /// </remarks>
    internal class ArrayType
        : SequenceType,
        IArrayType
    {
        internal ArrayType(LLVMTypeRef typeRef)
            : base(typeRef)
        {
            if (typeRef.Kind != LLVMTypeKind.LLVMArrayTypeKind)
            {
                throw new ArgumentException();
            }
        }

        /// <inheritdoc/>
        public uint Length => this.TypeRefHandle.ArrayLength;
    }
}
