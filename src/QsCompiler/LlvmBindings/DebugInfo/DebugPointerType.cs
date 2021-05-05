// -----------------------------------------------------------------------
// <copyright file="DebugPointerType.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Ubiquity.ArgValidators;
using Ubiquity.NET.Llvm.Types;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Binding between a <see cref="DIDerivedType"/> and an <see cref="IPointerType"/></summary>
    /// <seealso href="xref:llvm_langref#diderivedtype">LLVM DIDerivedType</seealso>
    public class DebugPointerType
        : DebugType<IPointerType, DIDerivedType>
        , IPointerType
    {
        /// <summary>Initializes a new instance of the <see cref="DebugPointerType"/> class.</summary>
        /// <param name="debugElementType">Debug type of the pointee</param>
        /// <param name="module"><see cref="BitcodeModule"/> used for creating the pointer type and debug information</param>
        /// <param name="addressSpace">Target address space for the pointer [Default: 0]</param>
        /// <param name="name">Name of the type [Default: null]</param>
        /// <param name="alignment">Alignment on pointer</param>
        public DebugPointerType( IDebugType<ITypeRef, DIType> debugElementType, BitcodeModule module, uint addressSpace = 0, string? name = null, uint alignment = 0 )
            : this( debugElementType.ValidateNotNull( nameof( debugElementType ) ).NativeType
                  , module
                  , debugElementType.DIType
                  , addressSpace
                  , name
                  , alignment
                  )
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DebugPointerType"/> class.</summary>
        /// <param name="llvmElementType">Native type of the pointee</param>
        /// <param name="module"><see cref="BitcodeModule"/> used for creating the pointer type and debug information</param>
        /// <param name="elementType">Debug type of the pointee</param>
        /// <param name="addressSpace">Target address space for the pointer [Default: 0]</param>
        /// <param name="name">Name of the type [Default: null]</param>
        /// <param name="alignment">Alignment of pointer</param>
        public DebugPointerType( ITypeRef llvmElementType, BitcodeModule module, DIType? elementType, uint addressSpace = 0, string? name = null, uint alignment = 0 )
            : this( llvmElementType.ValidateNotNull( nameof( llvmElementType ) ).CreatePointerType( addressSpace )
                  , module
                  , elementType
                  , name
                  , alignment
                  )
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DebugPointerType"/> class.</summary>
        /// <param name="llvmPtrType">Native type of the pointer</param>
        /// <param name="module"><see cref="BitcodeModule"/> used for creating the pointer type and debug information</param>
        /// <param name="elementType">Debug type of the pointee</param>
        /// <param name="name">Name of the type [Default: null]</param>
        /// <param name="alignment">Alignment for pointer type</param>
        public DebugPointerType( IPointerType llvmPtrType, BitcodeModule module, DIType? elementType, string? name = null, uint alignment = 0 )
            : base( llvmPtrType,
                    module.ValidateNotNull( nameof( module ) )
                          .DIBuilder
                          .CreatePointerType( elementType
                                            , name
                                            , module.Layout.BitSizeOf( llvmPtrType )
                                            , alignment
                                            )
                  )
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
        public DebugPointerType( IPointerType llvmPtrType, DIDerivedType debugType )
            : base( llvmPtrType, debugType )
        {
        }

        /// <inheritdoc/>
        public uint AddressSpace => NativeType.AddressSpace;

        /// <inheritdoc/>
        public ITypeRef ElementType => NativeType.ElementType;
    }
}
