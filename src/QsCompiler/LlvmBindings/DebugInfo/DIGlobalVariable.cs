// -----------------------------------------------------------------------
// <copyright file="DIGlobalVariable.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug information for a global variable</summary>
    /// <seealso href="xref:llvm_langref#diglobalvariable">LLVM DIGlobalVariable</seealso>
    public class DIGlobalVariable
        : DIVariable
    {
        /// <summary>Gets the display name for the variable</summary>
        public string DisplayName => this.GetOperandString(4);

        /// <summary>Gets the linkage name for the variable</summary>
        public string LinkageName => this.GetOperandString(5);

        /// <summary>Gets the static data member declaration for the variable</summary>
        public DIDerivedType? StaticDataMemberDeclaration => this.GetOperand<DIDerivedType>(6);

        internal DIGlobalVariable(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
