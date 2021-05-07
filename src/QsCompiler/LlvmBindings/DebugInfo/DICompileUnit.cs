// -----------------------------------------------------------------------
// <copyright file="DICompileUnit.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
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
        /* TODO: non-operand properties
        SourceLanguage SourceLanguage {get;}
        bool IsOptimized {get;}
        uint RunTimeVersion {get;}
        ?? EmissionKind { get; }
        */

        /// <summary>Gets the name of the producer of this unit</summary>
        public string Producer => GetOperandString( 1 );

        /// <summary>Gets the compilation flags for this unit</summary>
        public string Flags => GetOperandString( 2 );

        /// <summary>Gets the split debug file name for this unit</summary>
        public string SplitDebugFileName => GetOperandString( 3 );

        /// <summary>Gets the enum types in this unit</summary>
        public DICompositeTypeArray EnumTypes => new DICompositeTypeArray( GetOperand<MDTuple>( 4 ) );

        /// <summary>Gets the retained types for this unit</summary>
        public DIScopeArray RetainedTypes => new DIScopeArray( GetOperand<MDTuple>( 5 ) );

        /// <summary>Gets the global variable expressions for this unit</summary>
        public DIGlobalVariableExpressionArray GlobalVariables => new DIGlobalVariableExpressionArray( GetOperand<MDTuple>( 6 ) );

        /// <summary>Gets the imported entities for this unit</summary>
        public DIImportedEntityArray ImportedEntities => new DIImportedEntityArray( GetOperand<MDTuple>( 7 ) );

        /// <summary>Gets the macro information for the compile unit</summary>
        public DIMacroNodeArray Macros => new DIMacroNodeArray( GetOperand<MDTuple>( 8 ) );

        /// <summary>Initializes a new instance of the <see cref="DICompileUnit"/> class from a native <see cref="LLVMMetadataRef"/></summary>
        /// <param name="handle">native reference to wrap</param>
        internal DICompileUnit( LLVMMetadataRef handle )
            : base( handle )
        {
        }
    }
}
