// -----------------------------------------------------------------------
// <copyright file="DIDerivedType.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
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
        /// <summary>Gets the base type of this type</summary>
        public DIType BaseType => this.GetOperand<DIType>(3)!;

        /// <summary>Gets the extra data, if any, attached to this derived type</summary>
        public LlvmMetadata? ExtraData => this.Operands[4];

        /// <summary>Gets the Class type extra data for a pointer to member type</summary>
        public DIType? ClassType => this.GetOperand<DIType>(4);

        /// <summary>Gets the ObjCProperty extra data</summary>
        public DIObjCProperty? ObjCProperty => this.GetOperand<DIObjCProperty>(4);

        /// <summary>Gets the storage offset of the type in bits</summary>
        /// <remarks>This provides the bit offset for a bit field and is <see langword="null"/>
        /// if <see cref="DebugInfoFlags.BitField"/> is not set in <see cref="DebugInfoFlags"/>
        /// </remarks>
        public Constant? StorageOffsetInBits
            => this.DebugInfoFlags.HasFlag(DebugInfoFlags.BitField)
                    ? this.GetOperandValue(4) as Constant
                    : null;

        /// <summary>Gets the constant for a static member</summary>
        public Constant? Constant
            => this.DebugInfoFlags.HasFlag(DebugInfoFlags.StaticMember)
                    ? this.GetOperandValue(4) as Constant
                    : null;

        /// <summary>Initializes a new instance of the <see cref="DIDerivedType"/> class from an <see cref="LLVMMetadataRef"/></summary>
        /// <param name="handle">Handle to wrap</param>
        internal DIDerivedType(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
