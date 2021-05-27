// -----------------------------------------------------------------------
// <copyright file="DICompileUnit.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug Information Compile Unit, which acts as the containing parent for debug information in a module</summary>
    /// <seealso href="xref:llvm_langref#dicompileunit">LLVM DICompileUnit</seealso>
    public class DICompileUnit
        : DIScope
    {
        /// <summary>Gets the name of the producer of this unit</summary>
        public string Producer => this.GetOperandString(1);

        /// <summary>Gets the compilation flags for this unit</summary>
        public string Flags => this.GetOperandString(2);

        /// <summary>Gets the split debug file name for this unit</summary>
        public string SplitDebugFileName => this.GetOperandString(3);

        /// <summary>Gets the enum types in this unit</summary>
        public DICompositeTypeArray EnumTypes => new DICompositeTypeArray(this.GetOperand<MDTuple>(4));

        /// <summary>Gets the retained types for this unit</summary>
        public DIScopeArray RetainedTypes => new DIScopeArray(this.GetOperand<MDTuple>(5));

        /// <summary>Gets the global variable expressions for this unit</summary>
        public DIGlobalVariableExpressionArray GlobalVariables => new DIGlobalVariableExpressionArray(this.GetOperand<MDTuple>(6));

        /// <summary>Gets the imported entities for this unit</summary>
        public DIImportedEntityArray ImportedEntities => new DIImportedEntityArray(this.GetOperand<MDTuple>(7));

        /// <summary>Gets the macro information for the compile unit</summary>
        public DIMacroNodeArray Macros => new DIMacroNodeArray(this.GetOperand<MDTuple>(8));

        /// <summary>Initializes a new instance of the <see cref="DICompileUnit"/> class from a native <see cref="LLVMMetadataRef"/></summary>
        /// <param name="handle">native reference to wrap</param>
        internal DICompileUnit(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
