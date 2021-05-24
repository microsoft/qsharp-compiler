// <copyright file="LLVMValueMetadataEntryRef.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>

using System;

namespace LLVMSharp.Interop
{
    /// <summary>Support for refs to LLVMOpaqueValueMetadataEntry*.</summary>
    public unsafe partial struct LLVMValueMetadataEntryRef : IEquatable<LLVMValueMetadataEntryRef>
    {
        /// <summary>Pointer to the underlying native type.</summary>
        public IntPtr Handle { get; set; }

        /// <summary>Initializes a new instance of the <see cref="LLVMValueMetadataEntryRef"/> struct.</summary>
        public LLVMValueMetadataEntryRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        /// <summary>Conversion support for LLVMOpaqueValueMetadataEntry*.</summary>
        public static implicit operator LLVMValueMetadataEntryRef(LLVMOpaqueValueMetadataEntry* value) => new LLVMValueMetadataEntryRef((IntPtr)value);

        /// <summary>Conversion support for LLVMOpaqueValueMetadataEntry*.</summary>
        public static implicit operator LLVMOpaqueValueMetadataEntry*(LLVMValueMetadataEntryRef value) => (LLVMOpaqueValueMetadataEntry*)value.Handle;

        /// <summary>Basic equality comparison support.</summary>
        public static bool operator ==(LLVMValueMetadataEntryRef left, LLVMValueMetadataEntryRef right) => left.Handle == right.Handle;

        /// <summary>Basic inequality comparison support.</summary>
        public static bool operator !=(LLVMValueMetadataEntryRef left, LLVMValueMetadataEntryRef right) => !(left == right);

        /// <summary>Basic equality comparison support.</summary>
        public override bool Equals(object obj) => (obj is LLVMValueMetadataEntryRef other) && this.Equals(other);

        /// <summary>Basic equality comparison support.</summary>
        public bool Equals(LLVMValueMetadataEntryRef other) => this == other;

        /// <summary>Basic hash code support.</summary>
        public override int GetHashCode() => this.Handle.GetHashCode();

        /// <summary>Basic string representation support.</summary>
        public override string ToString() => $"{nameof(LLVMValueMetadataEntryRef)}: {this.Handle:X}";

        /// <summary>Convenience wrapper for <see cref="LLVM.ValueMetadataEntriesGetMetadata"/>.</summary>
        public LLVMMetadataRef ValueMetadataEntriesGetMetadata(uint i) => (this.Handle != default) ? LLVM.ValueMetadataEntriesGetMetadata(this, i) : default;
    }
}
