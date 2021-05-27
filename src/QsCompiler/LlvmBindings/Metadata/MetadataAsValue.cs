// -----------------------------------------------------------------------
// <copyright file="MetadataAsValue.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;
using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm
{
    /// <summary>Wraps metadata as a <see cref="Value"/></summary>
    public class MetadataAsValue
        : Value
    {
        /// <summary>Gets a list of the operands for the Metadata</summary>
        public ValueOperandListCollection<Value> Operands { get; }

        internal MetadataAsValue(LLVMValueRef valueRef)
            : base(valueRef)
        {
            this.Operands = new ValueOperandListCollection<Value>(this);
        }

        internal static LLVMValueRef IsAMetadataAsValue(LLVMValueRef value)
        {
            return value == default
                   ? value
                   : value.Kind == LLVMValueKind.LLVMMetadataAsValueValueKind ? value : default;
        }
    }
}
