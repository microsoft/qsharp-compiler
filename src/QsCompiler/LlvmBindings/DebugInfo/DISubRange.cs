// -----------------------------------------------------------------------
// <copyright file="DISubRange.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
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
        /// <summary>Gets a, potentially null, <see cref="DIVariable"/> for the count of the subrange</summary>
        /// <remarks>
        /// Count (length) of a DISubrange is either a <see cref="ConstantInt"/>
        /// wrapped in a <see cref="ConstantAsMetadata"/> or it is a <see cref="DIVariable"/>. This property
        /// extracts the count as a <see cref="DIVariable"/> value (if present).
        /// </remarks>
        public DIVariable? VariableCount
            => ( Operands[ 0 ] is DIVariable variable ) ? variable : null;

        internal DISubRange( LLVMMetadataRef handle )
            : base( handle )
        {
        }
    }
}
