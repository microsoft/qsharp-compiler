// -----------------------------------------------------------------------
// <copyright file="DebugPointerType.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using Ubiquity.NET.Llvm.Types;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Binding between a <see cref="DIDerivedType"/> and an <see cref="IPointerType"/></summary>
    /// <seealso href="xref:llvm_langref#diderivedtype">LLVM DIDerivedType</seealso>
    public class DebugPointerType
        : DebugType<IPointerType, DIDerivedType>,
        IPointerType
    {
        /// <summary>Initializes a new instance of the <see cref="DebugPointerType"/> class.</summary>
        /// <param name="debugElementType">Debug type of the pointee</param>
        /// <param name="dIBuilder"><see cref="DebugInfoBuilder"/> to use when constructing pointer type and debug info</param>
        /// <param name="addressSpace">Target address space for the pointer [Default: 0]</param>
        /// <param name="name">Name of the type [Default: null]</param>
        /// <param name="alignment">Alignment on pointer</param>
        public DebugPointerType(IDebugType<ITypeRef, DIType> debugElementType, DebugInfoBuilder dIBuilder, uint addressSpace = 0, string? name = null, uint alignment = 0)
            : this(
                debugElementType.NativeType,
                dIBuilder,
                debugElementType.DIType,
                addressSpace,
                name,
                alignment)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DebugPointerType"/> class.</summary>
        /// <param name="llvmElementType">Native type of the pointee</param>
        /// <param name="dIBuilder"><see cref="DebugInfoBuilder"/> to use when constructing the pointer type and debug info</param>
        /// <param name="elementType">Debug type of the pointee</param>
        /// <param name="addressSpace">Target address space for the pointer [Default: 0]</param>
        /// <param name="name">Name of the type [Default: null]</param>
        /// <param name="alignment">Alignment of pointer</param>
        public DebugPointerType(ITypeRef llvmElementType, DebugInfoBuilder dIBuilder, DIType? elementType, uint addressSpace = 0, string? name = null, uint alignment = 0)
            : this(
                llvmElementType.CreatePointerType(addressSpace),
                dIBuilder,
                elementType,
                name,
                alignment)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DebugPointerType"/> class.</summary>
        /// <param name="llvmPtrType">Native type of the pointer</param>
        /// <param name="dIBuilder"><see cref="DebugInfoBuilder"/> to use when constructing debug info</param>
        /// <param name="elementType">Debug type of the pointee</param>
        /// <param name="name">Name of the type [Default: null]</param>
        /// <param name="alignment">Alignment for pointer type</param>
        public DebugPointerType(IPointerType llvmPtrType, DebugInfoBuilder dIBuilder, DIType? elementType, string? name = null, uint alignment = 0)
            : base(
                llvmPtrType,
                dIBuilder.CreatePointerType(
                              elementType,
                              name,
                              dIBuilder.OwningModule.Layout.BitSizeOf(llvmPtrType),
                              alignment))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DebugPointerType"/> class.</summary>
        /// <param name="llvmPtrType">Native type of the pointer</param>
        /// <param name="debugType">Debug type for the pointer</param>
        /// <remarks>
        /// This constructor is typically used when building typedefs to a basic type
        /// to provide namespace scoping for the typedef for languages that support
        /// such a concept. This is needed because basic types don't have any namespace
        /// information in the LLVM Debug information (they are implicitly in the global
        /// namespace)
        /// </remarks>
        public DebugPointerType(IPointerType llvmPtrType, DIDerivedType debugType)
            : base(llvmPtrType, debugType)
        {
        }

        /// <inheritdoc/>
        public uint AddressSpace => this.NativeType.AddressSpace;

        /// <inheritdoc/>
        public ITypeRef ElementType => this.NativeType.ElementType;
    }
}
