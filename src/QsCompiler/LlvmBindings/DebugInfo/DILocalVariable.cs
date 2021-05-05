// -----------------------------------------------------------------------
// <copyright file="DILocalVariable.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Ubiquity.NET.Llvm.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug information for a local variable</summary>
    /// <seealso href="xref:llvm_langref#dilocalvariable">LLVM DILocalVariable</seealso>
    public class DILocalVariable
        : DIVariable
    {
        // ReSharper disable IdentifierTypo
        /* TODO: non-operand properties
        public DebugInfoFlags => LLVMDILocalVariableGetFlags( MetadataHandle );
        public UInt16 ArgIndex => LLVMDILocalVariableGetArg( MetadataHandle );
        public bool IsParameter => ArgIndex != 0;
        */

        /// <summary>Gets the local scope containing this variable</summary>
        public new DILocalScope Scope => ( DILocalScope )base.Scope!;

        internal DILocalVariable( LLVMMetadataRef handle )
            : base( handle )
        {
        }
    }
}
