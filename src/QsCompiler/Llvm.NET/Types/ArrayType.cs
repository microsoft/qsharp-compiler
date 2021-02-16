// -----------------------------------------------------------------------
// <copyright file="ArrayType.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;

using LLVMSharp.Interop;


// Interface+internal type matches file name
#pragma warning disable SA1649

namespace Ubiquity.NET.Llvm.Types
{
    /// <summary>Interface for an LLVM array type </summary>
    public interface IArrayType
        : ISequenceType
    {
        /// <summary>Gets the length of the array</summary>
        uint Length { get; }
    }

    /// <summary>Array type definition</summary>
    /// <remarks>
    /// Array's in LLVM are fixed length sequences of elements
    /// </remarks>
    internal class ArrayType
        : SequenceType
        , IArrayType
    {
        /// <inheritdoc/>
        public uint Length => TypeRefHandle.ArrayLength;

        internal ArrayType( LLVMTypeRef typeRef )
            : base( typeRef )
        {
            if( typeRef.Kind != LLVMTypeKind.LLVMArrayTypeKind )
            {
                throw new ArgumentException( );
            }
        }
    }
}
