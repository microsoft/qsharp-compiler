// -----------------------------------------------------------------------
// <copyright file="DIModule.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Ubiquity.NET.Llvm.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>A source module that is imported by a compile unit</summary>
    public class DIModule
        : DIScope
    {
        /// <inheritdoc/>
        public override DIScope? Scope => GetOperand<DIScope>( 0 );

        /// <inheritdoc/>
        public override string Name => GetOperandString( 1 );

        /// <summary>Gets the configuration macros for the module</summary>
        public string ConfigurationMacros => GetOperandString( 2 );

        /// <summary>Gets the include path for the module</summary>
        public string IncludePath => GetOperandString( 3 );

        /// <summary>Gets the ISysRoot for the module</summary>
        public string SysRoot => GetOperandString( 4 );

        internal DIModule( LLVMMetadataRef handle )
            : base( handle )
        {
        }
    }
}
