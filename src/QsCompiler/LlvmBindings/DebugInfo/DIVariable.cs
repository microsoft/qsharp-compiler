// -----------------------------------------------------------------------
// <copyright file="DIVariable.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

using Ubiquity.NET.Llvm.Interop;

using static Ubiquity.NET.Llvm.Interop.NativeMethods;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug information for a variable</summary>
    public class DIVariable
        : DINode
    {
        /// <summary>Gets the line for the variable</summary>
        public UInt32 Line => LLVMDIVariableGetLine( MetadataHandle );

        /// <summary>Gets the Debug information scope for this variable</summary>
        public DIScope? Scope => FromHandle<DIScope>( LLVMDIVariableGetScope( MetadataHandle ) );

        /// <summary>Gets the Debug information name for this variable</summary>
        public string Name => ( Operands[ 1 ] as MDString )?.ToString( ) ?? string.Empty;

        /// <summary>Gets the Debug information file for this variable</summary>
        public DIFile? File => FromHandle<DIFile>( LLVMDIVariableGetFile( MetadataHandle ) );

        /// <summary>Gets the Debug information type for this variable</summary>
        public DIType? DIType => GetOperand<DIType>( 3 );

        internal DIVariable( LLVMMetadataRef handle )
            : base( handle )
        {
        }
    }
}
