// -----------------------------------------------------------------------
// <copyright file="DISubRange.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

using Ubiquity.NET.Llvm.Interop;
using Ubiquity.NET.Llvm.Values;

using static Ubiquity.NET.Llvm.Interop.NativeMethods;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Sub range</summary>
    /// <seealso href="xref:llvm_langref#disubrange">LLVM DISubRange</seealso>
    public class DISubRange
        : DINode
    {
        /// <summary>Gets a value for the lower bound of the range</summary>
        public Int64 LowerBound => LibLLVMDISubRangeGetLowerBounds( MetadataHandle );

        /// <summary>Gets a, potentially null, constant value for the count of the subrange</summary>
        /// <remarks>
        /// Count (length) of a DISubrange is either a <see cref="ConstantInt"/>
        /// wrapped in a <see cref="ConstantAsMetadata"/> or it is a <see cref="DIVariable"/>. This property
        /// extracts the count as a constant integral value (if present). If this is <see langword="null"/>
        /// then <see cref="VariableCount"/> is not. (and vice versa)
        /// </remarks>
        public long? ConstantCount
            => (Operands[ 0 ] is ConstantAsMetadata constMetadata) ? ((ConstantInt)constMetadata!).SignExtendedValue : (long?)null;

        /// <summary>Gets a, potentially null, <see cref="DIVariable"/> for the count of the subrange</summary>
        /// <remarks>
        /// Count (length) of a DISubrange is either a <see cref="ConstantInt"/>
        /// wrapped in a <see cref="ConstantAsMetadata"/> or it is a <see cref="DIVariable"/>. This property
        /// extracts the count as a <see cref="DIVariable"/> value (if present). If this is <see langword="null"/>
        /// then <see cref="ConstantCount"/> is not. (and vice versa)
        /// </remarks>
        public DIVariable? VariableCount
            => ( Operands[ 0 ] is DIVariable variable ) ? variable : null;

        internal DISubRange( LLVMMetadataRef handle )
            : base( handle )
        {
        }
    }
}
