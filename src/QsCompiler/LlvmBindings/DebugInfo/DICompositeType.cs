// -----------------------------------------------------------------------
// <copyright file="DICompositeType.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug information for a composite type</summary>
    /// <seealso href="xref:llvm_langref#dicompositetype">LLVM DICompositeType</seealso>
    public class DICompositeType
        : DIType
    {
        /// <summary>Gets the base type for this type, if any</summary>
        public DIType? BaseType => this.GetOperand<DIType>(3);

        /// <summary>Gets the elements of this <see cref="DICompositeType"/></summary>
        public DINodeArray Elements => new DINodeArray(this.GetOperand<MDTuple>(4));

        /// <summary>Gets the type that holds the VTable for this type, if any</summary>
        public DIType? VTableHolder => this.GetOperand<DIType>(5);

        /// <summary>Gets the template parameters for this type, if any</summary>
        public DITemplateParameterArray? TemplateParameters
        {
            get
            {
                MDTuple? tuple = this.GetOperand<MDTuple>(6);
                return tuple == null ? null : new DITemplateParameterArray(tuple);
            }
        }

        /// <summary>Gets the identifier for this type</summary>
        public string Identifier => this.GetOperandString(7);

        /// <summary>Gets the Discriminator for the composite type</summary>
        public DIDerivedType? Discriminator => this.GetOperand<DIDerivedType>(8);

        /// <summary>Initializes a new instance of the <see cref="DICompositeType"/> class from an LLVM-C API Metadata handle</summary>
        /// <param name="handle">LLVM handle to wrap</param>
        internal DICompositeType(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
