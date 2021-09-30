// -----------------------------------------------------------------------
// <copyright file="DISubRange.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;

using LLVMSharp.Interop;
using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Sub range</summary>
    /// <seealso href="xref:llvm_langref#disubrange">LLVM DISubRange</seealso>
    public class DISubRange
        : DINode
    {
        /// <summary>Gets a, potentially null, constant value for the count of the subrange</summary>
        /// <remarks>
        /// Count (length) of a DISubrange is either a <see cref="ConstantInt"/>
        /// wrapped in a <see cref="ConstantAsMetadata"/> or it is a <see cref="DIVariable"/>. This property
        /// extracts the count as a constant integral value (if present). If this is <see langword="null"/>
        /// then <see cref="VariableCount"/> is not. (and vice versa)
        /// </remarks>
        public long? ConstantCount => (this.Operands[0] is ConstantAsMetadata)
            ? ((ConstantInt)this.Operands.GetOperandValue(0)!).SignExtendedValue
            : (long?)null;

        /// <summary>Gets a, potentially null, <see cref="DIVariable"/> for the count of the subrange</summary>
        /// <remarks>
        /// Count (length) of a DISubrange is either a <see cref="ConstantInt"/>
        /// wrapped in a <see cref="ConstantAsMetadata"/> or it is a <see cref="DIVariable"/>. This property
        /// extracts the count as a <see cref="DIVariable"/> value (if present). If this is <see langword="null"/>
        /// then <see cref="ConstantCount"/> is not. (and vice versa)
        /// </remarks>
        public DIVariable? VariableCount
            => (this.Operands[0] is DIVariable variable) ? variable : null;

        internal DISubRange(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
