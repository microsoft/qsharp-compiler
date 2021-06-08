﻿// -----------------------------------------------------------------------
// <copyright file="DISubroutineType.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug information for a function signature</summary>
    /// <seealso href="xref:llvm_langref#disubroutinetype"/>
    public class DISubroutineType
        : DIType
    {
        /// <summary>Gets the types for the sub routine</summary>
        public DITypeArray TypeArray => new DITypeArray(this.GetOperand<MDTuple>(3));

        internal DISubroutineType(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
