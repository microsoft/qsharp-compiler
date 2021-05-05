// -----------------------------------------------------------------------
// <copyright file="DICompositeType.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Ubiquity.NET.Llvm.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug information for a composite type</summary>
    /// <seealso href="xref:llvm_langref#dicompositetype">LLVM DICompositeType</seealso>
    public class DICompositeType
        : DIType
    {
        /// <summary>Gets the base type for this type, if any</summary>
        public DIType? BaseType => GetOperand<DIType>( 3 );

        /// <summary>Gets the elements of this <see cref="DICompositeType"/></summary>
        public DINodeArray Elements => new DINodeArray( GetOperand<MDTuple>( 4 ) );

        /// <summary>Gets the type that holds the VTable for this type, if any</summary>
        public DIType? VTableHolder => GetOperand<DIType>( 5 );

        /// <summary>Gets the template parameters for this type, if any</summary>
        public DITemplateParameterArray? TemplateParameters
        {
            get
            {
                MDTuple? tuple = GetOperand<MDTuple>( 6 );
                return tuple == null ? null : new DITemplateParameterArray( tuple );
            }
        }

        /// <summary>Gets the identifier for this type</summary>
        public string Identifier => GetOperandString( 7 );

        /// <summary>Gets the Discriminator for the composite type</summary>
        public DIDerivedType? Discriminator => GetOperand<DIDerivedType>( 8 );

        /// <summary>Initializes a new instance of the <see cref="DICompositeType"/> class from an LLVM-C API Metadata handle</summary>
        /// <param name="handle">LLVM handle to wrap</param>
        internal DICompositeType( LLVMMetadataRef handle )
            : base( handle )
        {
        }
    }
}
