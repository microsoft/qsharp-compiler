// -----------------------------------------------------------------------
// <copyright file="DIImportedEntity.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug information from an imported entity</summary>
    /// <seealso href="xref:llvm_langref#diimportedentity">LLVM DIImportedEntity</seealso>
    public class DIImportedEntity
        : DINode
    {
        /*
        uint Line {get;}
        */

        /// <summary>Gets the <see cref="DIScope"/> for the imported entity</summary>
        public DIScope Scope => this.GetOperand<DIScope>(0)!;

        /// <summary>Gets the entity imported</summary>
        public DINode Entity => this.GetOperand<DINode>(1)!;

        /// <summary>Gets the name of the node</summary>
        public string Name => this.GetOperandString(2);

        /// <summary>Gets the <see cref="DIFile"/> for the imported entity</summary>
        public DIFile File => this.GetOperand<DIFile>(3)!;

        internal DIImportedEntity(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
