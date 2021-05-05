// -----------------------------------------------------------------------
// <copyright file="DIDerivedType.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Ubiquity.NET.Llvm.Interop;
using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Derived type</summary>
    /// <remarks>
    /// Debug information for a type derived from an existing type
    /// </remarks>
    /// <seealso href="xref:llvm_langref#diderivedtype">LLVM DIDerivedType</seealso>
    public class DIDerivedType
        : DIType
    {
        /* TODO: non-operand properties
            uint? AddressSpace { get; }
        */

        /// <summary>Gets the base type of this type</summary>
        public DIType BaseType => GetOperand<DIType>( 3 )!;

        /// <summary>Gets the extra data, if any, attached to this derived type</summary>
        public LlvmMetadata? ExtraData => Operands[ 4 ];

        /// <summary>Gets the Class type extra data for a pointer to member type, if any</summary>
        public DIType? ClassType => Tag != Tag.PointerToMemberType ? null : GetOperand<DIType>( 4 );

        /// <summary>Gets the ObjCProperty extra data</summary>
        public DIObjCProperty? ObjCProperty => GetOperand<DIObjCProperty>( 4 );

        /// <summary>Gets the storage offset of the type in bits</summary>
        /// <remarks>This provides the bit offset for a bit field and is <see langword="null"/>
        /// if <see cref="DebugInfoFlags.BitField"/> is not set in <see cref="DebugInfoFlags"/>
        /// </remarks>
        public Constant? StorageOffsetInBits
            => Tag == Tag.Member && DebugInfoFlags.HasFlag( DebugInfoFlags.BitField )
                    ? GetOperand<ConstantAsMetadata>( 4 )?.Value as Constant
                    : null;

        /// <summary>Gets the constant for a static member</summary>
        public Constant? Constant
            => Tag == Tag.Member && DebugInfoFlags.HasFlag( DebugInfoFlags.StaticMember )
                    ? GetOperand<ConstantAsMetadata>( 4 )?.Value as Constant
                    : null;

        /// <summary>Initializes a new instance of the <see cref="DIDerivedType"/> class from an <see cref="LLVMMetadataRef"/></summary>
        /// <param name="handle">Handle to wrap</param>
        internal DIDerivedType( LLVMMetadataRef handle )
            : base( handle )
        {
        }
    }
}
