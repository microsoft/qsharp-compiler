// -----------------------------------------------------------------------
// <copyright file="MetadataAsValue.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Ubiquity.NET.Llvm.Interop;
using Ubiquity.NET.Llvm.Values;

using static Ubiquity.NET.Llvm.Interop.NativeMethods;

namespace Ubiquity.NET.Llvm
{
    /// <summary>Wraps metadata as a <see cref="Value"/></summary>
    public class MetadataAsValue
        : Value
    {
        /// <summary>Gets a list of the operands for the Metadata</summary>
        public ValueOperandListCollection<Value> Operands { get; }

        internal MetadataAsValue( LLVMValueRef valueRef )
            : base( valueRef )
        {
            Operands = new ValueOperandListCollection<Value>( this );
        }

        internal static LLVMValueRef IsAMetadataAsValue( LLVMValueRef value )
        {
            return value == default
                   ? value
                   : LibLLVMGetValueKind( value ) == LibLLVMValueKind.MetadataAsValueKind ? value : default;
        }

        /*
        //public static implicit operator Metadata( MetadataAsValue self )
        //{
        //    // TODO: Add support to get the metadata ref from the value...
        //    // e.g. call C++ MetadataAsValue.getMetadata()
        //    throw new NotImplementedException();
        //}
        */
    }
}
