// -----------------------------------------------------------------------
// <copyright file="DIVariable.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug information for a variable</summary>
    public class DIVariable
        : DINode
    {
        /// <summary>Gets the line for the variable</summary>
        public uint Line => this.MetadataHandle.DIVariableGetLine();

        /// <summary>Gets the Debug information scope for this variable</summary>
        public DIScope? Scope => FromHandle<DIScope>(this.MetadataHandle.DIVariableGetScope());

        /// <summary>Gets the Debug information name for this variable</summary>
        public string Name => (this.Operands[1] as MDString)?.ToString() ?? string.Empty;

        /// <summary>Gets the Debug information file for this variable</summary>
        public DIFile? File => FromHandle<DIFile>(this.MetadataHandle.DIVariableGetFile());

        /// <summary>Gets the Debug information type for this variable</summary>
        public DIType? DIType => this.GetOperand<DIType>(3);

        internal DIVariable(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
