// -----------------------------------------------------------------------
// <copyright file="DataLayout.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

using LLVMSharp.Interop;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm
{
    /// <summary>Byte ordering for target code generation and data type layout</summary>
    public enum ByteOrdering
    {
        /// <summary>Little-Endian layout format</summary>
        LittleEndian = LLVMByteOrdering.LLVMLittleEndian,

        /// <summary>Big-Endian layout format</summary>
        BigEndian = LLVMByteOrdering.LLVMBigEndian,
    }

    /// <summary>Provides access to LLVM target data layout information</summary>
    /// <remarks>
    /// <para>There is a distinction between various sizes and alignment for a given type
    /// that are target dependent.</para>
    /// <para>The following table illustrates the differences in sizes and their meaning
    ///  for a sample set of types.</para>
    /// |   Type  | SizeInBits | StoreSizeInBits | AbiSizeInBits |
    /// |---------|------------|-----------------|---------------|
    /// | i1      | 1          | 8               | 8             |
    /// | i8      | 8          | 8               | 8             |
    /// | i19     | 19         | 24              | 32            |
    /// | i32     | 32         | 32              | 32            |
    /// | i10     | 100        | 104             | 128           |
    /// | i128    | 128        | 128             | 128           |
    /// | Float   | 32         | 32              | 32            |
    /// | Double  | 64         | 64              | 64            |
    /// | X86_FP80| 80         | 80              | 96            |
    ///
    /// <note type="note">
    /// The allocation size depends on the alignment, and thus on the target.
    /// The values in the example table are for x86-32-linux.
    /// </note>
    /// |   Property      | Definition |
    /// |-----------------|------------|
    /// | SizeInBits      | Minimum number of bits needed to represent the full range of values for the type |
    /// | StoreSizeInBits | Minimum number of bits needed to actually store a *single* value of the type |
    /// | AbiSizeInBits   | Total number of bits used to store a value in a sequence, including any alignment padding |
    ///
    /// The allocation size determines the total size of each entry in a sequence so that the "next" element is computed
    /// by adding the size to the start address of the current element.
    /// </remarks>
    public sealed class DataLayout
    {
        /// <summary>Gets the byte ordering for this target</summary>
        public ByteOrdering Endianess => (ByteOrdering)this.DataLayoutHandle.ByteOrder();

        /// <summary>Gets the size of a pointer for the default address space of the target</summary>
        /// <returns>Size of a pointer to the default address space</returns>
        public uint PointerSize() => this.DataLayoutHandle.PointerSize();

        /// <summary>Retrieves the size of a pointer for a given address space of the target</summary>
        /// <param name="addressSpace">Address space for the pointer</param>
        /// <returns>Size of a pointer</returns>
        public uint PointerSize(uint addressSpace) => this.DataLayoutHandle.PointerSizeForAS(addressSpace);

        /// <summary>Retrieves an LLVM integer type with the same bit width as a pointer for the default address space of the target</summary>
        /// <param name="context">LLVM <see cref="Context"/> that owns the definition of the pointer type to retrieve</param>
        /// <returns>Integer type matching the bit width of a native pointer in the target's default address space</returns>
        public ITypeRef IntPtrType(Context context)
        {
            LLVMTypeRef typeRef = context.ContextHandle.GetIntPtrType(this.DataLayoutHandle);
            return TypeRef.FromHandle(typeRef)!;
        }

        /// <summary>Retrieves an LLVM integer type with the same bit width as
        /// a pointer for the given address space of the target</summary>
        /// <param name="context">LLVM <see cref="Context"/> that owns the definition of the pointer type to retrieve</param>
        /// <param name="addressSpace">Address space for the pointer</param>
        /// <returns>Integer type matching the bit width of a native pointer in the target's address space</returns>
        public ITypeRef IntPtrType(Context context, uint addressSpace)
        {
            var typeHandle = context.ContextHandle.GetIntPtrTypeForAS(this.DataLayoutHandle, addressSpace);
            return TypeRef.FromHandle(typeHandle)!;
        }

        /// <summary>Returns the number of bits necessary to hold the specified type.</summary>
        /// <param name="typeRef">Type to retrieve the size of</param>
        /// <remarks>
        /// <para>This method determines the bit size of a type (e.g. the minimum number of
        /// bits required to represent any value of the given type.) This is distinct from the storage
        /// and stack size due to various target alignment requirements.</para>
        /// </remarks>
        /// <returns>Size of the type in bits</returns>
        public ulong BitSizeOf(ITypeRef typeRef)
        {
            VerifySized(typeRef, nameof(typeRef));
            return this.DataLayoutHandle.SizeOfTypeInBits(typeRef.GetTypeRef());
        }

        /// <summary>Retrieves the number of bits required to store a value of the given type</summary>
        /// <param name="typeRef">Type to retrieve the storage size of</param>
        /// <returns>Number of bits required to store a value of the given type in the target</returns>
        /// <remarks>This method retrieves the storage size in bits of a given type. The storage size
        /// includes any trailing padding bits that may be needed if the target requires reading a wider
        /// word size. (e.g. most systems can't write a single bit value for an LLVM i1, thus the
        /// storage size is whatever the minimum number of bits that the target requires to store a value
        /// of the given type)
        /// </remarks>
        public ulong StoreSizeOf(ITypeRef typeRef)
        {
            VerifySized(typeRef, nameof(typeRef));
            return this.DataLayoutHandle.StoreSizeOfType(typeRef.GetTypeRef());
        }

        /// <summary>Retrieves the ABI specified size of the given type</summary>
        /// <param name="typeRef">Type to get the size from</param>
        /// <returns>Size of the type</returns>
        /// <remarks>
        /// Returns the offset in bytes between successive objects of the
        /// specified type, including alignment padding
        /// </remarks>
        public ulong AbiSizeOf(ITypeRef typeRef)
        {
            VerifySized(typeRef, nameof(typeRef));
            return this.DataLayoutHandle.ABISizeOfType(typeRef.GetTypeRef());
        }

        /// <summary>Retrieves the ABI specified alignment, in bytes, for a specified type</summary>
        /// <param name="typeRef">Type to get the alignment for</param>
        /// <returns>ABI specified alignment</returns>
        public uint AbiAlignmentOf(ITypeRef typeRef)
        {
            VerifySized(typeRef, nameof(typeRef));
            return this.DataLayoutHandle.ABIAlignmentOfType(typeRef.GetTypeRef());
        }

        /// <summary>Retrieves the call frame alignment for a given type</summary>
        /// <param name="typeRef">type to get the alignment of</param>
        /// <returns>Alignment for the type</returns>
        public uint CallFrameAlignmentOf(ITypeRef typeRef)
        {
            VerifySized(typeRef, nameof(typeRef));
            return this.DataLayoutHandle.CallFrameAlignmentOfType(typeRef.GetTypeRef());
        }

        /// <summary>Gets the preferred alignment for an LLVM type</summary>
        /// <param name="typeRef">Type to get the alignment of</param>
        /// <returns>Preferred alignment</returns>
        public uint PreferredAlignmentOf(ITypeRef typeRef)
        {
            VerifySized(typeRef, nameof(typeRef));
            return this.DataLayoutHandle.PreferredAlignmentOfType(typeRef.GetTypeRef());
        }

        /// <summary>Gets the preferred alignment for a <see cref="Value"/></summary>
        /// <param name="value">Value to get the alignment of</param>
        /// <returns>Preferred alignment</returns>
        public uint PreferredAlignmentOf(Value value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            VerifySized(value.NativeType, nameof(value));
            return this.DataLayoutHandle.PreferredAlignmentOfGlobal(value.ValueHandle);
        }

        /// <summary>Gets the element index for a specific offset in a given structure</summary>
        /// <param name="structType">Type of the structure</param>
        /// <param name="offset">Offset to determine the index of</param>
        /// <returns>Index of the element</returns>
        public ulong ElementAtOffset(IStructType structType, ulong offset)
        {
            VerifySized(structType, nameof(structType));
            return this.DataLayoutHandle.ElementAtOffset(structType.GetTypeRef(), offset);
        }

        /// <summary>Gets the offset of an element in a structure</summary>
        /// <param name="structType">Type of the structure</param>
        /// <param name="element">index of the element in the structure</param>
        /// <returns>Offset of the element from the beginning of the structure</returns>
        public ulong OffsetOfElement(IStructType structType, uint element)
        {
            VerifySized(structType, nameof(structType));
            return this.DataLayoutHandle.OffsetOfElement(structType.GetTypeRef(), element);
        }

        /// <summary>Converts the layout to a string representation of the layout data</summary>
        /// <returns>Data layout as a string</returns>
        /// <seealso href="xref:llvm_langref#data-layout">DICompositeType</seealso>
        public override string ToString() => this.DataLayoutHandle.CopyStringRepOfTargetData();

        /// <summary>Gets the byte size of a type</summary>
        /// <param name="llvmType">Type to determine the size of</param>
        /// <returns>Size of the type in bytes</returns>
        public ulong ByteSizeOf(ITypeRef llvmType) => this.BitSizeOf(llvmType) / 8u;

        /// <summary>Gets the preferred alignment of the type in bits</summary>
        /// <param name="llvmType">Type to get the alignment of</param>
        /// <returns>Alignment of the type</returns>
        public uint PreferredBitAlignmentOf(ITypeRef llvmType) => this.PreferredAlignmentOf(llvmType) * 8;

        /// <summary>Gets the ABI alignment of the type in bits</summary>
        /// <param name="llvmType">Type to get the alignment of</param>
        /// <returns>Alignment of the type</returns>
        public uint AbiBitAlignmentOf(ITypeRef llvmType) => this.AbiAlignmentOf(llvmType) * 8;

        /// <summary>Gets the offset of a structure element in bits</summary>
        /// <param name="llvmType">Structure type to get the element offset of</param>
        /// <param name="element">Index of the element in the structure</param>
        /// <returns>Offset of the element in bits</returns>
        public ulong BitOffsetOfElement(IStructType llvmType, uint element) => this.OffsetOfElement(llvmType, element) * 8;

        internal DataLayout(LLVMTargetDataRef targetDataHandle)
        {
            this.DataLayoutHandle = targetDataHandle;
        }

        internal static DataLayout FromHandle(LLVMTargetDataRef targetDataRef)
        {
            lock (TargetDataMap)
            {
                if (TargetDataMap.TryGetValue(targetDataRef, out DataLayout retVal))
                {
                    return retVal;
                }

                retVal = new DataLayout(targetDataRef);
                TargetDataMap.Add(targetDataRef, retVal);

                return retVal;
            }
        }

        internal LLVMTargetDataRef DataLayoutHandle { get; }

        private static void VerifySized(ITypeRef type, string name)
        {
            if (type == null)
            {
                throw new ArgumentNullException(name);
            }

            if (!type.IsSized)
            {
                throw new ArgumentException();
            }
        }

        private static readonly Dictionary<LLVMTargetDataRef, DataLayout> TargetDataMap = new Dictionary<LLVMTargetDataRef, DataLayout>();
    }
}
