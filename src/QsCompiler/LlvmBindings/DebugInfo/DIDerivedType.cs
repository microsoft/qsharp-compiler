// -----------------------------------------------------------------------
// <copyright file="DIDerivedType.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;
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

        /// <summary>Gets the Class type extra data for a pointer to member type</summary>
        public DIType? ClassType => GetOperand<DIType>( 4 );

        /// <summary>Gets the ObjCProperty extra data</summary>
        public DIObjCProperty? ObjCProperty => GetOperand<DIObjCProperty>( 4 );

        /// <summary>Initializes a new instance of the <see cref="DIDerivedType"/> class from an <see cref="LLVMMetadataRef"/></summary>
        /// <param name="handle">Handle to wrap</param>
        internal DIDerivedType( LLVMMetadataRef handle )
            : base( handle )
        {
        }
    }
}
