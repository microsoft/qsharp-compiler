// -----------------------------------------------------------------------
// <copyright file="PointerType.cs" company="Ubiquity.NET Contributors">
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
    /// <summary>Interface for a pointer type in LLVM</summary>
    public interface IPointerType
        : ISequenceType
    {
        /// <summary>Gets the address space the pointer refers to</summary>
        uint AddressSpace { get; }
    }

    /// <summary>LLVM pointer type</summary>
    internal class PointerType
        : SequenceType
        , IPointerType
    {
        /// <summary>Gets the address space the pointer refers to</summary>
        public uint AddressSpace => TypeRefHandle.PointerAddressSpace;

        internal PointerType( LLVMTypeRef typeRef )
            : base( typeRef )
        {
            if( typeRef.Kind != LLVMTypeKind.LLVMPointerTypeKind )
            {
                throw new ArgumentException( );
            }
        }
    }
}
