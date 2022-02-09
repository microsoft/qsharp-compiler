// -----------------------------------------------------------------------
// <copyright file="DINamespace.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug information namespace scope</summary>
    /// <seealso href="xref:llvm_langref#dinamespace"/>
    public class DINamespace
        : DIScope
    {
        /// <inheritdoc/>
        public override DIScope? Scope => this.GetOperand<DIScope>(1);

        /// <inheritdoc/>
        public override string Name => this.GetOperandString(2);

        internal DINamespace(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
